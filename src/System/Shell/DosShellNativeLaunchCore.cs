using Amiga;
using CopperStart.Dos;
using CopperStart.Exec;

namespace CopperOS.Shell.Dos;

/// <summary>
/// Fixed-width native child launch helpers.  This is deliberately separate
/// from <see cref="DosShellPlatform{TDosPlatform}"/>: CopperSharp's native
/// profile accepts only scalar locals, while the managed adapter is a larger
/// value assembled from the same DOS-owned state.
/// </summary>
internal static class DosShellNativeLaunchCore
{
	private const uint TagBytes = TagItem.Size * 5;
	private const uint DefaultStack = 4096;
	private const uint MaximumStack = 16u * 1024u * 1024u;

	public static bool TryCreateShell(
		CopperSharpNativeDosPlatform dos, APTR state, APTR execBase,
		APTR parentCli, ShellLaunchKind kind, BPTR input, BPTR output,
		BPTR error, BPTR currentDirectory, APTR continuation, APTR window,
		uint windowLength, APTR from, uint fromLength)
	{
		_ = error;
		if (execBase.IsNull || state.IsNull || parentCli.IsNull ||
			(kind != ShellLaunchKind.NewCli && kind != ShellLaunchKind.NewShell) ||
			(window.IsNull ? windowLength != 0 : windowLength == 0) ||
			(from.IsNull && fromLength != 0) || fromLength > 255 ||
			(from.IsNotNull && (from.Raw > uint.MaxValue - fromLength - 1 ||
				!dos.IsMapped(from, fromLength + 1) ||
				dos.ReadUInt8(from, unchecked((int)fromLength)) != 0)))
			return false;
		var windowHandleRaw = 0u;
		if (window.IsNotNull)
		{
			windowHandleRaw = DosCore.OpenNativeConsole(state, window,
				windowLength);
			if (windowHandleRaw == 0) return false;
		}
		var childEntry = DosShellNativeEntrypoints.AddressOfShellChild();
		if (childEntry.IsNull)
		{
			DosCore.CloseNativeHandle(state, windowHandleRaw);
			return false;
		}
		const uint tagBytes = TagItem.Size * 5;
		var tags = dos.AllocateGuest(tagBytes);
		if (tags.IsNull || !dos.IsMapped(tags, tagBytes))
		{
			Free(ref dos, tags, tagBytes);
			DosCore.CloseNativeHandle(state, windowHandleRaw);
			return false;
		}
		dos.Clear(tags, tagBytes);
		WriteTag(ref dos, tags, 0, ExecConstants.TaskTagProgramCounter,
			childEntry.Raw);
		WriteTag(ref dos, tags, 1, ExecConstants.TaskTagM68kStackSize,
			DefaultStack);
		WriteTag(ref dos, tags, 2, ExecConstants.TagDone, 0);
		WriteTag(ref dos, tags, 3, ExecConstants.TagDone, 0);
		WriteTag(ref dos, tags, 4, ExecConstants.TagDone, 0);
		var startup = new DosChildCliStartup(APTR.Null, 0, APTR.Null, 0,
			from, fromLength, APTR.Null, 0);
		var launchInputRaw = windowHandleRaw != 0 ? windowHandleRaw : input.Raw;
		var launchOutputRaw = windowHandleRaw != 0 ? windowHandleRaw : output.Raw;
		var task = DosChildProcessLaunchCore.CreateShellNative(dos, execBase,
			state, tags, continuation, BPTR.FromRaw(launchInputRaw),
			BPTR.FromRaw(launchOutputRaw),
			currentDirectory, startup);
		Free(ref dos, tags, tagBytes);
		DosCore.CloseNativeHandle(state, windowHandleRaw);
		return task.IsNotNull;
	}

	public static bool TryRunCommand(
		CopperSharpNativeDosPlatform dos, APTR state, APTR execBase,
		APTR cli, BPTR input, BPTR output, BPTR error, BPTR currentDirectory,
		APTR continuation, APTR command, uint commandLength, uint detach,
		uint quiet, uint stack, uint stackPresent, int priority,
		uint priorityPresent)
	{
		_ = detach;
		_ = quiet;
		if (execBase.IsNull || cli.IsNull || command.IsNull || commandLength == 0 ||
			commandLength > 65_535 || command.Raw > uint.MaxValue - commandLength ||
			!dos.IsMapped(command, commandLength)) return false;

		var nameLength = FirstTokenLength(ref dos, command, commandLength);
		if (nameLength == 0) return false;
		var name = dos.AllocateGuest(nameLength + 1);
		var path = dos.AllocateGuest(512);
		if (name.IsNull || path.IsNull || !dos.IsMapped(name, nameLength + 1) ||
			!dos.IsMapped(path, 512))
		{
			Free(ref dos, name, nameLength + 1);
			Free(ref dos, path, 512);
			return false;
		}
		dos.Copy(command, name, nameLength);
		dos.WriteUInt8(name, unchecked((int)nameLength), 0);

		var found = DosShellNativeBridge.LookupCommand(ref dos, state, cli, name,
			nameLength, path, 512, out var kind, out var pathLength);
		if (!found || pathLength == 0 || kind ==
			DosShellNativeBridge.LookupKind.Script)
		{
			Free(ref dos, name, nameLength + 1);
			Free(ref dos, path, 512);
			return false;
		}

		var residentEntry = APTR.Null;
		var residentEntryRaw = 0u;
		var residentAcquired = false;
		BPTR segment;
		if (kind == DosShellNativeBridge.LookupKind.Resident)
		{
			residentEntryRaw = DosShellNativeBridge.LookupResidentNativeRaw(ref dos,
				state, name, nameLength);
			var residentSegmentRaw = residentEntryRaw == 0 ? 0u :
				DosShellNativeBridge.AcquireResidentNativeRaw(ref dos, state, name,
					nameLength, residentEntryRaw);
			if (residentSegmentRaw == 0)
			{
				Free(ref dos, name, nameLength + 1);
				Free(ref dos, path, 512);
				return false;
			}
			segment = BPTR.FromRaw(residentSegmentRaw);
			residentEntry = APTR.FromPointer(residentEntryRaw);
			residentAcquired = true;
		}
		else if (kind != DosShellNativeBridge.LookupKind.File)
		{
			Free(ref dos, name, nameLength + 1);
			Free(ref dos, path, 512);
			return false;
		}
		else
			segment = DosSegmentLoaderCore.Load(ref dos, state, path);

		if (segment.IsNull || !DosCommandImageCore.TryInspect(ref dos, state,
			segment, out var image))
		{
			if (residentAcquired)
				DosShellNativeBridge.ReleaseResidentNativeRaw(ref dos, state,
					residentEntryRaw);
			else
				ReleaseImage(ref dos, state, segment);
			Free(ref dos, name, nameLength + 1);
			Free(ref dos, path, 512);
			return false;
		}

		var tags = dos.AllocateGuest(TagBytes);
		if (tags.IsNull || !dos.IsMapped(tags, TagBytes))
		{
			Free(ref dos, tags, TagBytes);
			if (residentAcquired)
				DosShellNativeBridge.ReleaseResidentNativeRaw(ref dos, state,
					residentEntryRaw);
			else
				ReleaseImage(ref dos, state, segment);
			Free(ref dos, name, nameLength + 1);
			Free(ref dos, path, 512);
			return false;
		}
		dos.Clear(tags, TagBytes);
		WriteTag(ref dos, tags, 0, ExecConstants.TaskTagProgramCounter,
			image.EntryPoint.Raw);
		var requestedStack = stackPresent != 0 ? stack : DefaultStack;
		if (requestedStack < 64 || requestedStack > MaximumStack)
		{
			Free(ref dos, tags, TagBytes);
			if (residentAcquired)
				DosShellNativeBridge.ReleaseResidentNativeRaw(ref dos, state,
					residentEntryRaw);
			else
				ReleaseImage(ref dos, state, segment);
			Free(ref dos, name, nameLength + 1);
			Free(ref dos, path, 512);
			return false;
		}
		WriteTag(ref dos, tags, 1, ExecConstants.TaskTagM68kStackSize,
			requestedStack);
		if (priorityPresent != 0)
			WriteTag(ref dos, tags, 2, ExecConstants.TaskTagPriority,
				unchecked((uint)priority));
		WriteTag(ref dos, tags, 3, ExecConstants.TaskTagName, name.Raw);
		WriteTag(ref dos, tags, 4, ExecConstants.TagDone, 0);

		DosChildCliStartup startup = default;
		startup.CommandName = name;
		startup.CommandNameLength = nameLength;
		startup.CommandFile = path;
		startup.CommandFileLength = pathLength;
		var task = DosChildProcessLaunchCore.CreateFromImageWithStartupNative(
			dos, execBase, state,
			tags, segment, continuation, input, output, currentDirectory,
			startup);
		Free(ref dos, tags, TagBytes);
		Free(ref dos, name, nameLength + 1);
		Free(ref dos, path, 512);
		if (task.IsNull)
		{
			if (residentAcquired)
				DosShellNativeBridge.ReleaseResidentNativeRaw(ref dos, state,
					residentEntryRaw);
			else
				ReleaseImage(ref dos, state, segment);
			return false;
		}
		if (residentAcquired && !DosProcessImageCore.BindResident(ref dos,
			state, task, residentEntry, segment))
		{
			DosProcessCodec.WriteSegmentList(ref dos, task, BPTR.Null);
			DosProcessLifecycleCore.Terminate<CopperSharpNativeDosPlatform,
				ClassicPolicy>(ref dos, execBase, state, task);
			DosShellNativeBridge.ReleaseResidentNativeRaw(ref dos, state,
				residentEntryRaw);
			return false;
		}
		return true;
	}

	public static bool TryReleaseContinuation(
		CopperSharpNativeDosPlatform dos, APTR state, APTR execBase,
		APTR cli, APTR continuation, uint ownedFlags)
	{
		if (execBase.IsNull || cli.IsNull || continuation.IsNull ||
			!DosChildContinuationCodec.TryRead(ref dos, continuation,
				out var current) || current.ChildCli.IsNull ||
			(current.Flags & (uint)DosChildContinuationFlags.ResourcesClosed) == 0 ||
			ownedFlags != (current.Flags & ~(uint)
			DosChildContinuationFlags.ResourcesClosed) ||
			!DosCore.TryFindProcessTaskByCli(ref dos, state, current.ChildCli,
				out var task)) return false;
		return DosChildContinuationCore.TryReleaseAfterShellMark<
			CopperSharpNativeDosPlatform, ClassicPolicy>(ref dos, execBase, state,
			task, continuation);
	}

	/// <summary>
	/// Starts one external command discovered by the DOS-owned script lookup.
	/// The command line, continuation record, loaded image, and child CLI are
	/// all guest-owned; this scalar variant deliberately mirrors the managed
	/// adapter without introducing a managed object or callback into the native
	/// launch path.
	/// </summary>
	public static bool TryExecuteScriptCommand(
		CopperSharpNativeDosPlatform dos, APTR state, APTR execBase, APTR cli,
		APTR frame, APTR line, uint lineLength,
		CopperOS.Shell.ShellScriptLookupKind lookupKind, APTR resolvedPath,
		uint resolvedPathLength, BPTR input, BPTR output, BPTR error,
		out int result, out APTR continuation)
	{
		result = (int)CopperOS.Shell.ShellCommandResult.Error;
		continuation = APTR.Null;
		_ = error;
		if (execBase.IsNull || cli.IsNull || frame.IsNull || line.IsNull ||
			lineLength == 0 || lineLength > 65_535 ||
			line.Raw > uint.MaxValue - lineLength ||
			!dos.IsMapped(line, lineLength) || resolvedPath.IsNull ||
			resolvedPathLength == 0 || resolvedPathLength > 65_535 ||
			resolvedPath.Raw > uint.MaxValue - resolvedPathLength ||
			!dos.IsMapped(resolvedPath, resolvedPathLength) ||
			(lookupKind != CopperOS.Shell.ShellScriptLookupKind.ExplicitFile &&
			 lookupKind != CopperOS.Shell.ShellScriptLookupKind.Resident))
			return false;

		var nameLength = FirstTokenLength(ref dos, line, lineLength);
		if (nameLength == 0) return false;
		var name = dos.AllocateGuest(nameLength + 1);
		var path = dos.AllocateGuest(resolvedPathLength + 1);
		if (name.IsNull || path.IsNull || !dos.IsMapped(name, nameLength + 1) ||
			!dos.IsMapped(path, resolvedPathLength + 1))
		{
			Free(ref dos, name, nameLength + 1);
			Free(ref dos, path, resolvedPathLength + 1);
			return false;
		}
		dos.Copy(line, name, nameLength);
		dos.WriteUInt8(name, unchecked((int)nameLength), 0);
		dos.Copy(resolvedPath, path, resolvedPathLength);
		dos.WriteUInt8(path, unchecked((int)resolvedPathLength), 0);

		var residentEntryRaw = 0u;
		var residentAcquired = false;
		BPTR segment;
		if (lookupKind == CopperOS.Shell.ShellScriptLookupKind.Resident)
		{
			residentEntryRaw = DosShellNativeBridge.LookupResidentNativeRaw(
				ref dos, state, name, nameLength);
			var residentSegmentRaw = residentEntryRaw == 0 ? 0u :
				DosShellNativeBridge.AcquireResidentNativeRaw(ref dos, state, name,
					nameLength, residentEntryRaw);
			if (residentSegmentRaw == 0)
			{
				Free(ref dos, name, nameLength + 1);
				Free(ref dos, path, resolvedPathLength + 1);
				return false;
			}
			segment = BPTR.FromRaw(residentSegmentRaw);
			residentAcquired = true;
		}
		else
			segment = DosSegmentLoaderCore.Load(ref dos, state, path);

		if (segment.IsNull || !DosCommandImageCore.TryInspect(ref dos, state,
			segment, out var image))
		{
			if (residentAcquired)
				DosShellNativeBridge.ReleaseResidentNativeRaw(ref dos, state,
					residentEntryRaw);
			else if (segment.IsNotNull)
				DosSegmentLoaderCore.Unload(ref dos, state, segment);
			Free(ref dos, name, nameLength + 1);
			Free(ref dos, path, resolvedPathLength + 1);
			return false;
		}

		var commandStorage = lineLength + 1;
		var record = DosShellNativeBridge.AllocateScriptRecord(ref dos, state,
			frame, DosChildContinuationCodec.Size, 3, commandStorage,
			out var storedCommand);
		if (record.IsNull || storedCommand.IsNull)
		{
			if (record.IsNotNull)
				DosShellNativeBridge.FreeScriptRecord(ref dos, state, frame, record, 3);
			if (residentAcquired)
				DosShellNativeBridge.ReleaseResidentNativeRaw(ref dos, state,
					residentEntryRaw);
			else if (segment.IsNotNull)
				DosSegmentLoaderCore.Unload(ref dos, state, segment);
			Free(ref dos, name, nameLength + 1);
			Free(ref dos, path, resolvedPathLength + 1);
			return false;
		}
		dos.Copy(line, storedCommand, lineLength);
		dos.WriteUInt8(storedCommand, unchecked((int)lineLength), 0);
		var initial = new DosChildContinuationRecord
		{
			ParentCli = cli,
			Command = storedCommand,
			CommandLength = lineLength,
			State = DosChildContinuationState.Pending,
			Flags = (uint)DosChildContinuationFlags.RecordOwned,
		};
		if (!DosChildContinuationCodec.Initialize(ref dos, record, in initial))
		{
			DosShellNativeBridge.FreeScriptRecord(ref dos, state, frame, record, 3);
			if (residentAcquired)
				DosShellNativeBridge.ReleaseResidentNativeRaw(ref dos, state,
					residentEntryRaw);
			else if (segment.IsNotNull)
				DosSegmentLoaderCore.Unload(ref dos, state, segment);
			Free(ref dos, name, nameLength + 1);
			Free(ref dos, path, resolvedPathLength + 1);
			return false;
		}

		const uint tagBytes = TagItem.Size * 5;
		var tags = dos.AllocateGuest(tagBytes);
		if (tags.IsNull || !dos.IsMapped(tags, tagBytes))
		{
			Free(ref dos, tags, tagBytes);
			DosShellNativeBridge.FreeScriptRecord(ref dos, state, frame, record, 3);
			if (residentAcquired)
				DosShellNativeBridge.ReleaseResidentNativeRaw(ref dos, state,
					residentEntryRaw);
			else if (segment.IsNotNull)
				DosSegmentLoaderCore.Unload(ref dos, state, segment);
			Free(ref dos, name, nameLength + 1);
			Free(ref dos, path, resolvedPathLength + 1);
			return false;
		}
		dos.Clear(tags, tagBytes);
		WriteTag(ref dos, tags, 0, ExecConstants.TaskTagProgramCounter,
			image.EntryPoint.Raw);
		WriteTag(ref dos, tags, 1, ExecConstants.TaskTagM68kStackSize, DefaultStack);
		WriteTag(ref dos, tags, 2, ExecConstants.TaskTagName, name.Raw);
		WriteTag(ref dos, tags, 3, ExecConstants.TagDone, 0);
		WriteTag(ref dos, tags, 4, ExecConstants.TagDone, 0);
		var startup = new DosChildCliStartup(name, nameLength, name, nameLength,
			path, resolvedPathLength, APTR.Null, 0);
		var cliValue = DosCommandLineInterfaceCodec.Read(ref dos, cli);
		var task = DosChildProcessLaunchCore.CreateFromImageWithStartupNative(
			dos, execBase, state, tags, segment, record, input, output,
			cliValue.CurrentDirectoryName, startup);
		Free(ref dos, tags, tagBytes);
		Free(ref dos, name, nameLength + 1);
		Free(ref dos, path, resolvedPathLength + 1);
		if (task.IsNull)
		{
			DosShellNativeBridge.FreeScriptRecord(ref dos, state, frame, record, 3);
			if (residentAcquired)
				DosShellNativeBridge.ReleaseResidentNativeRaw(ref dos, state,
					residentEntryRaw);
			else if (segment.IsNotNull)
				DosSegmentLoaderCore.Unload(ref dos, state, segment);
			return false;
		}

		var argumentStart = nameLength;
		while (argumentStart < lineLength &&
			dos.ReadUInt8(storedCommand, unchecked((int)argumentStart)) is
			(byte)' ' or (byte)'\t') argumentStart++;
		DosProcessCodec.WriteArguments(ref dos, task,
			argumentStart < lineLength
				? APTR.FromPointer(storedCommand.Raw + argumentStart)
				: APTR.Null);
		if (residentAcquired && !DosProcessImageCore.BindResident(ref dos, state,
			task, APTR.FromPointer(residentEntryRaw), segment))
		{
			DosProcessCodec.WriteSegmentList(ref dos, task, BPTR.Null);
			DosProcessLifecycleCore.Terminate<CopperSharpNativeDosPlatform,
				ClassicPolicy>(ref dos, execBase, state, task);
			DosShellNativeBridge.FreeScriptRecord(ref dos, state, frame, record, 3);
			DosShellNativeBridge.ReleaseResidentNativeRaw(ref dos, state,
				residentEntryRaw);
			return false;
		}
		continuation = record;
		result = (int)CopperOS.Shell.ShellCommandResult.Ok;
		return true;
	}

	private static uint FirstTokenLength<TMemory>(ref TMemory memory,
		APTR source, uint length) where TMemory : struct, IAmigaGuestMemory
	{
		var count = 0u;
		while (count < length)
		{
			var value = memory.ReadUInt8(source, unchecked((int)count));
			if (value is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
				break;
			count++;
		}
		return count;
	}

	private static void WriteTag(ref CopperSharpNativeDosPlatform dos,
		APTR tags, uint index, uint tag, uint data)
	{
		var item = APTR.FromPointer(tags.Raw + index * TagItem.Size);
		dos.WriteUInt32(item, 0, tag);
		dos.WriteUInt32(item, 4, data);
	}

	private static void Free(ref CopperSharpNativeDosPlatform dos, APTR address,
		uint size)
	{
		if (address.IsNotNull) dos.FreeGuest(address, size);
	}

	private static void ReleaseImage(ref CopperSharpNativeDosPlatform dos,
		APTR state, BPTR segment)
	{
		if (segment.IsNotNull)
			DosSegmentLoaderCore.Unload(ref dos, state, segment);
	}
}
