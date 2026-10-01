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
	private struct ShellLaunchResources
	{
		public BPTR Input;
		public BPTR Output;
		public BPTR Error;
		public BPTR Window;
		public BPTR From;
	}

	private struct ShellLaunchTextWriter
	{
		public APTR Buffer;
		public uint Capacity;
		public uint Position;
	}

	private const uint TagBytes = TagItem.Size * 5;
	private const uint DefaultStack = 4096;
	private const uint MaximumStack = 16u * 1024u * 1024u;
	private const uint DefaultShellStartupLength = 15;
	private const uint DefaultShellStartupStorage =
		DefaultShellStartupLength + 1;

	public static bool TryCreateShell(
		CopperSharpNativeDosPlatform dos, APTR state, APTR execBase,
		APTR parentCli, ShellLaunchKind kind, BPTR input, BPTR output,
		BPTR error, BPTR currentDirectory, APTR continuation, APTR window,
		uint windowLength, APTR from, uint fromLength)
	{
		if (execBase.IsNull || state.IsNull || parentCli.IsNull ||
			(kind != ShellLaunchKind.NewCli && kind != ShellLaunchKind.NewShell &&
			 kind != ShellLaunchKind.Cli) ||
			(window.IsNull ? windowLength != 0 : windowLength == 0) ||
			(from.IsNull && fromLength != 0) || fromLength > 255 ||
			(from.IsNotNull && (from.Raw > uint.MaxValue - fromLength - 1 ||
				!dos.IsMapped(from, fromLength + 1) ||
				dos.ReadUInt8(from, unchecked((int)fromLength)) != 0)))
			return false;
		if (!DosCore.TryGetShellProcessContext(ref dos, state, parentCli,
			out var processContext)) return false;
		if (!TryResolveChildStack(processContext.CliState.DefaultStack,
			0, 0, out var childStack)) return false;
		var startupScript = APTR.Null;
		var startupScriptLength = 0u;
		var ownedStartupScript = APTR.Null;
		if (kind == ShellLaunchKind.NewShell)
		{
			startupScript = from;
			startupScriptLength = fromLength;
			if (startupScript.IsNull)
			{
				ownedStartupScript = dos.AllocateGuest(
					DefaultShellStartupStorage);
				if (ownedStartupScript.IsNull || !dos.IsMapped(
					ownedStartupScript, DefaultShellStartupStorage))
				{
					Free(ref dos, ownedStartupScript,
						DefaultShellStartupStorage);
					return false;
				}
				WriteDefaultShellStartup(ref dos, ownedStartupScript);
				startupScript = ownedStartupScript;
				startupScriptLength = DefaultShellStartupLength;
			}
		}
		var resources = new ShellLaunchResources
		{
			Input = input,
			Output = output,
			Error = error,
		};
		if (window.IsNotNull)
		{
			resources.Window = BPTR.FromRaw(DosCore.OpenNativeConsole(state,
				window, windowLength));
			if (resources.Window.IsNull)
			{
				Free(ref dos, ownedStartupScript,
					DefaultShellStartupStorage);
				return false;
			}
			resources.Input = resources.Window;
			resources.Output = resources.Window;
			resources.Error = resources.Window;
		}
		if (kind == ShellLaunchKind.NewCli && from.IsNotNull)
		{
			resources.From = BPTR.FromRaw(DosCore.OpenNativeConsole(state,
				from, fromLength));
			if (resources.From.IsNull)
			{
				CloseLaunchHandles(state, in resources);
				Free(ref dos, ownedStartupScript,
					DefaultShellStartupStorage);
				return false;
			}
			resources.Input = resources.From;
			if (resources.Window.IsNull)
			{
				resources.Output = resources.From;
				resources.Error = resources.From;
			}
		}
		if (resources.Window.IsNull && resources.From.IsNull)
		{
			resources.Window = DosCore.OpenDefaultConsole(ref dos, state);
			if (resources.Window.IsNull)
			{
				CloseLaunchHandles(state, in resources);
				Free(ref dos, ownedStartupScript,
					DefaultShellStartupStorage);
				return false;
			}
			resources.Input = resources.Window;
			resources.Output = resources.Window;
			resources.Error = resources.Window;
		}
		var childEntry = DosShellNativeEntrypoints.AddressOfShellChild();
		if (childEntry.IsNull)
		{
			CloseLaunchHandles(state, in resources);
			Free(ref dos, ownedStartupScript, DefaultShellStartupStorage);
			return false;
		}
		const uint tagBytes = TagItem.Size * 5;
		var tags = dos.AllocateGuest(tagBytes);
		if (tags.IsNull || !dos.IsMapped(tags, tagBytes))
		{
			Free(ref dos, tags, tagBytes);
			CloseLaunchHandles(state, in resources);
			Free(ref dos, ownedStartupScript, DefaultShellStartupStorage);
			return false;
		}
		dos.Clear(tags, tagBytes);
		WriteTag(ref dos, tags, 0, ExecConstants.TaskTagProgramCounter,
			childEntry.Raw);
		WriteTag(ref dos, tags, 1, ExecConstants.TaskTagM68kStackSize,
			childStack);
		WriteTag(ref dos, tags, 2, ExecConstants.TagDone, 0);
		WriteTag(ref dos, tags, 3, ExecConstants.TagDone, 0);
		WriteTag(ref dos, tags, 4, ExecConstants.TagDone, 0);
		DosChildCliStartup startup = new()
		{
			CommandFile = startupScript,
			CommandFileLength = startupScriptLength,
			ParentCli = parentCli,
			InteractivePresent = 1,
			Interactive = 1,
		};
		var inherited = new DosChildInheritedResources(resources.Input,
			resources.Output, resources.Error, processContext.CurrentDirectory);
		var task = DosChildProcessLaunchCore.PrepareShellNative(dos, execBase,
			state, tags, continuation, in inherited, startup);
		Free(ref dos, tags, tagBytes);
		Free(ref dos, ownedStartupScript, DefaultShellStartupStorage);
		var published = false;
		if (task.IsNotNull)
		{
			var child = new DosShellPreparedChild
			{
				State = state, ExecBase = execBase, Task = task, Entry = childEntry,
				Continuation = continuation,
				CommandArguments = 0,
			};
			published = DosShellPreparedChildCore.Publish(ref dos, in child);
		}
		var launchError = DosCore.IoErr(ref dos, state);
		CloseLaunchHandles(state, in resources);
		if (!published) DosCore.SetIoErr(ref dos, state, launchError);
		return published;
	}

	public static bool TryRunCommand(
		CopperSharpNativeDosPlatform dos, APTR state, APTR execBase,
		APTR cli, BPTR input, BPTR output, BPTR error, BPTR currentDirectory,
		APTR continuation, APTR command, uint commandLength, uint detach,
		uint quiet, uint stack, uint stackPresent, int priority,
		uint priorityPresent, out bool rollbackComplete)
	{
		rollbackComplete = false;
		_ = detach;
		_ = quiet;
		if (execBase.IsNull || cli.IsNull || command.IsNull || commandLength == 0 ||
			commandLength > 65_535 || command.Raw > uint.MaxValue - commandLength ||
			!dos.IsMapped(command, commandLength)) return false;
		if (!DosCore.TryGetShellProcessContext(ref dos, state, cli,
			out var processContext)) return false;
		if (!TryResolveChildStack(processContext.CliState.DefaultStack,
			stack, stackPresent, out var requestedStack)) return false;

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
			nameLength, path, 512, out var lookup);
		if (!found || lookup.PathLength == 0 || lookup.Kind ==
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
		if (lookup.Kind == DosShellNativeBridge.LookupKind.Resident)
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
		else if (lookup.Kind != DosShellNativeBridge.LookupKind.File &&
			lookup.Kind != DosShellNativeBridge.LookupKind.CurrentDirectory &&
			lookup.Kind != DosShellNativeBridge.LookupKind.CommandPath)
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
		WriteTag(ref dos, tags, 1, ExecConstants.TaskTagM68kStackSize,
			requestedStack);
		WriteTag(ref dos, tags, 2, ExecConstants.TaskTagName, name.Raw);
		// Keep required fields before the optional slot, which is TagDone when absent.
		if (priorityPresent != 0)
			WriteTag(ref dos, tags, 3, ExecConstants.TaskTagPriority,
				unchecked((uint)priority));
		WriteTag(ref dos, tags, 4, ExecConstants.TagDone, 0);

		DosChildCliStartup startup = default;
		startup.CommandName = name;
		startup.CommandNameLength = nameLength;
		startup.CommandFile = path;
		startup.CommandFileLength = lookup.PathLength;
		startup.ParentCli = cli;
		var inherited = new DosChildInheritedResources(input, output, error,
			processContext.CurrentDirectory);
		var task = DosChildProcessLaunchCore.PrepareFromImageWithStartupNative(
			dos, execBase, state,
			tags, segment, continuation, in inherited,
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
		var argumentStart = nameLength;
		while (argumentStart < commandLength &&
			dos.ReadUInt8(command, unchecked((int)argumentStart)) is
				(byte)' ' or (byte)'\t') argumentStart++;
		var child = new DosShellPreparedChild
		{
			State = state, ExecBase = execBase, Task = task,
			Entry = image.EntryPoint, Continuation = continuation,
			Arguments = APTR.FromPointer(argumentStart < commandLength
				? command.Raw + argumentStart : 0u),
			ArgumentLength = commandLength - argumentStart,
			CommandArguments = 1,
			Segment = segment, ResidentEntry = residentEntry,
		};
		return DosShellPreparedChildCore.Publish(ref dos, in child,
			out rollbackComplete);
	}

	public static bool TryReleaseContinuation(
		CopperSharpNativeDosPlatform dos, APTR state, APTR execBase,
		APTR cli, APTR continuation, uint ownedFlags)
	{
		if (cli.IsNull || continuation.IsNull ||
			!DosChildContinuationCodec.TryRead(ref dos, continuation,
				out var current) || current.ParentCli != cli ||
			(current.Flags & (uint)DosChildContinuationFlags.ResourcesClosed) == 0 ||
			ownedFlags != (current.Flags & ~(uint)
			DosChildContinuationFlags.ResourcesClosed)) return false;
		// Saved child identities and handles are historical snapshots. DOS
		// retirement owns their lifetime; acknowledgement only drops the result.
		if ((current.Flags & (uint)DosChildContinuationFlags.RecordOwned) != 0)
			return DosShellNativeBridge.TryGetScriptFrame(ref dos, state, cli,
				out var frame) && DosChildCompletionCore.TryAcknowledgeScriptRecord(
					ref dos, state, cli, continuation, frame);
		// Caller-owned Run/NewShell records remain caller-owned.
		return DosChildCompletionCore.TryAcknowledge(ref dos, state, cli,
			continuation);
	}

	/// <summary>
	/// Starts one external command discovered by the DOS-owned script lookup.
	/// The command line, continuation record, loaded image, and child CLI are
	/// all guest-owned; typed invocation and lookup records carry the parsed
	/// command and DOS resolution metadata into the native launch path.
	/// </summary>
	public static bool TryExecuteScriptCommand(
		CopperSharpNativeDosPlatform dos, APTR state, APTR execBase, APTR cli,
		APTR frame, in CopperOS.Shell.ShellScriptCommandInvocation command,
		in CopperOS.Shell.ShellScriptLookupResult lookup,
		BPTR input, BPTR output, BPTR error,
		out int result, out APTR continuation)
	{
		result = (int)CopperOS.Shell.ShellCommandResult.Error;
		continuation = APTR.Null;
		if (lookup.Kind == CopperOS.Shell.ShellScriptLookupKind.Script)
			return TryLaunchScriptThroughExecute(ref dos, state, execBase, cli,
				frame, in command, in lookup, input, output, error,
				out result, out continuation);
		if (execBase.IsNull || cli.IsNull || frame.IsNull ||
			command.Line.IsNull || command.LineLength == 0 ||
			command.LineLength > 65_535 ||
			command.Line.Raw > uint.MaxValue - command.LineLength ||
			!dos.IsMapped(command.Line, command.LineLength) ||
			command.CommandName.IsNull || command.CommandNameLength == 0 ||
			command.CommandNameLength > 65_535 ||
			command.CommandName.Raw > uint.MaxValue - command.CommandNameLength ||
			!dos.IsMapped(command.CommandName, command.CommandNameLength) ||
			!command.Arguments.IsContainedBy(command.Line, command.LineLength) ||
			lookup.ResolvedPath.IsNull ||
			lookup.PathLength == 0 || lookup.PathLength > 65_535 ||
			lookup.ResolvedPath.Raw > uint.MaxValue - lookup.PathLength ||
			!dos.IsMapped(lookup.ResolvedPath, lookup.PathLength) ||
			(lookup.Kind != CopperOS.Shell.ShellScriptLookupKind.ExplicitFile &&
			 lookup.Kind != CopperOS.Shell.ShellScriptLookupKind.CurrentDirectory &&
			 lookup.Kind != CopperOS.Shell.ShellScriptLookupKind.CommandPath &&
			 lookup.Kind != CopperOS.Shell.ShellScriptLookupKind.Resident))
			return false;
		if (!DosCore.TryGetShellProcessContext(ref dos, state, cli,
			out var processContext)) return false;
		if (!TryResolveChildStack(processContext.CliState.DefaultStack,
			0, 0, out var childStack)) return false;

		var nameLength = command.CommandNameLength;
		var name = dos.AllocateGuest(nameLength + 1);
		var path = dos.AllocateGuest(lookup.PathLength + 1);
		if (name.IsNull || path.IsNull || !dos.IsMapped(name, nameLength + 1) ||
			!dos.IsMapped(path, lookup.PathLength + 1))
		{
			Free(ref dos, name, nameLength + 1);
			Free(ref dos, path, lookup.PathLength + 1);
			return false;
		}
		dos.Copy(command.CommandName, name, nameLength);
		dos.WriteUInt8(name, unchecked((int)nameLength), 0);
		dos.Copy(lookup.ResolvedPath, path, lookup.PathLength);
		dos.WriteUInt8(path, unchecked((int)lookup.PathLength), 0);

		var residentEntryRaw = 0u;
		var residentAcquired = false;
		BPTR segment;
		if (lookup.Kind == CopperOS.Shell.ShellScriptLookupKind.Resident)
		{
			residentEntryRaw = DosShellNativeBridge.LookupResidentNativeRaw(
				ref dos, state, name, nameLength);
			var residentSegmentRaw = residentEntryRaw == 0 ? 0u :
				DosShellNativeBridge.AcquireResidentNativeRaw(ref dos, state, name,
					nameLength, residentEntryRaw);
			if (residentSegmentRaw == 0)
			{
				Free(ref dos, name, nameLength + 1);
				Free(ref dos, path, lookup.PathLength + 1);
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
			Free(ref dos, path, lookup.PathLength + 1);
			return false;
		}

		var commandStorage = command.LineLength + 1;
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
			Free(ref dos, path, lookup.PathLength + 1);
			return false;
		}
		dos.Copy(command.Line, storedCommand, command.LineLength);
		dos.WriteUInt8(storedCommand, unchecked((int)command.LineLength), 0);
		var initial = new DosChildContinuationRecord
		{
			ParentCli = cli,
			Command = storedCommand,
			CommandLength = command.LineLength,
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
			Free(ref dos, path, lookup.PathLength + 1);
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
			Free(ref dos, path, lookup.PathLength + 1);
			return false;
		}
		dos.Clear(tags, tagBytes);
		WriteTag(ref dos, tags, 0, ExecConstants.TaskTagProgramCounter,
			image.EntryPoint.Raw);
		WriteTag(ref dos, tags, 1, ExecConstants.TaskTagM68kStackSize, childStack);
		WriteTag(ref dos, tags, 2, ExecConstants.TaskTagName, name.Raw);
		WriteTag(ref dos, tags, 3, ExecConstants.TagDone, 0);
		WriteTag(ref dos, tags, 4, ExecConstants.TagDone, 0);
		var startup = new DosChildCliStartup(APTR.Null, 0, name, nameLength,
			path, lookup.PathLength, APTR.Null, 0);
		startup.ParentCli = cli;
		var inherited = new DosChildInheritedResources(input, output, error,
			processContext.CurrentDirectory);
		var task = DosChildProcessLaunchCore.PrepareFromImageWithStartupNative(
			dos, execBase, state, tags, segment, record, in inherited, startup);
		Free(ref dos, tags, tagBytes);
		Free(ref dos, name, nameLength + 1);
		Free(ref dos, path, lookup.PathLength + 1);
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

		var arguments = command.Arguments;
		while (arguments.Length != 0 &&
			dos.ReadUInt8(arguments.Data, 0) is (byte)' ' or (byte)'\t')
		{
			arguments.Data = APTR.FromPointer(arguments.Data.Raw + 1);
			arguments.Length--;
		}
		var child = new DosShellPreparedChild
		{
			State = state, ExecBase = execBase, Task = task,
			Entry = image.EntryPoint, Continuation = record,
			Arguments = arguments.Length == 0 ? APTR.Null : arguments.Data,
			ArgumentLength = arguments.Length,
			CommandArguments = 1,
			Segment = segment,
			ResidentEntry = APTR.FromPointer(residentEntryRaw),
		};
		if (!DosShellPreparedChildCore.Publish(ref dos, in child, out var rollbackComplete))
		{
			if (rollbackComplete)
				DosShellNativeBridge.FreeScriptRecord(ref dos, state, frame, record, 3);
			return false;
		}
		continuation = record;
		result = (int)CopperOS.Shell.ShellCommandResult.Ok;
		return true;
	}

	private static bool TryLaunchScriptThroughExecute(
		ref CopperSharpNativeDosPlatform dos, APTR state, APTR execBase,
		APTR cli, APTR frame,
		in CopperOS.Shell.ShellScriptCommandInvocation command,
		in CopperOS.Shell.ShellScriptLookupResult lookup,
		BPTR input, BPTR output, BPTR error,
		out int result, out APTR continuation)
	{
		result = (int)CopperOS.Shell.ShellCommandResult.Error;
		continuation = APTR.Null;
		if (execBase.IsNull || state.IsNull || cli.IsNull || frame.IsNull ||
			lookup.Origin != CopperOS.Shell.ShellScriptLookupOrigin.CommandPath ||
			(lookup.Protection & FileProtection.Script) == 0 ||
			lookup.ResolvedPath.IsNull || lookup.PathLength == 0 ||
			lookup.PathLength > 65_535 ||
			lookup.ResolvedPath.Raw > uint.MaxValue - lookup.PathLength ||
			!dos.IsMapped(lookup.ResolvedPath, lookup.PathLength) ||
			command.Line.IsNull || command.LineLength == 0 ||
			command.LineLength > 65_535 ||
			command.Line.Raw > uint.MaxValue - command.LineLength ||
			!dos.IsMapped(command.Line, command.LineLength) ||
			!command.Arguments.IsContainedBy(command.Line, command.LineLength))
			return false;

		// MorphOS documents automatic execution for an S-protected file found
		// in the CLI command path. Invoke the ordinary Execute HUNK with the
		// resolved filename as FILE/A; do not load the script as a HUNK.
		var arguments = command.Arguments;
		while (arguments.Length != 0 &&
			dos.ReadUInt8(arguments.Data, 0) is
			(byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
		{
			arguments.Data = APTR.FromPointer(arguments.Data.Raw + 1);
			arguments.Length--;
		}
		var argumentLength = arguments.Length;
		var quotedPathLength = lookup.PathLength;
		for (var index = 0u; index < lookup.PathLength; index++)
		{
			var value = dos.ReadUInt8(lookup.ResolvedPath,
				unchecked((int)index));
			if (value == 0) return false;
			if (value is (byte)'*' or (byte)'"')
			{
				if (quotedPathLength == uint.MaxValue) return false;
				quotedPathLength++;
			}
		}
		const uint executeNameLength = 7;
		var executeLineLength = executeNameLength + 1 + 1 +
			quotedPathLength + 1;
		if (argumentLength != 0)
		{
			if (executeLineLength >= 65_535 ||
				argumentLength > 65_535 - executeLineLength - 1)
				return false;
			executeLineLength += 1 + argumentLength;
		}
		if (executeLineLength == 0 || executeLineLength > 65_535)
			return false;

		var record = DosShellNativeBridge.AllocateScriptRecord(ref dos, state,
			frame, DosChildContinuationCodec.Size, 3, executeLineLength + 1,
			out var executeCommand);
		if (record.IsNull || executeCommand.IsNull)
		{
			if (record.IsNotNull)
				DosShellNativeBridge.FreeScriptRecord(ref dos, state, frame,
					record, 3);
			return false;
		}

		var writer = new ShellLaunchTextWriter
		{
			Buffer = executeCommand,
			Capacity = executeLineLength + 1,
		};
		var writeSucceeded = TryAppendExecutePrefix(ref dos, ref writer);
		for (var index = 0u; index < lookup.PathLength; index++)
		{
			if (!writeSucceeded) break;
			var value = dos.ReadUInt8(lookup.ResolvedPath,
				unchecked((int)index));
			if (value == 0 ||
				((value is (byte)'*' or (byte)'"') &&
					!TryAppendLaunchByte(ref dos, ref writer, (byte)'*')) ||
				!TryAppendLaunchByte(ref dos, ref writer, value))
				writeSucceeded = false;
		}
		writeSucceeded = writeSucceeded &&
			TryAppendLaunchByte(ref dos, ref writer, (byte)'"');
		if (writeSucceeded && argumentLength != 0)
			writeSucceeded = TryAppendLaunchByte(ref dos, ref writer,
				(byte)' ') && TryAppendLaunchBytes(ref dos, ref writer,
				arguments.Data, argumentLength);
		if (!writeSucceeded ||
			!TryAppendLaunchByte(ref dos, ref writer, 0) ||
			writer.Position != executeLineLength + 1)
		{
			DosShellNativeBridge.FreeScriptRecord(ref dos, state, frame,
				record, 3);
			return false;
		}
		var initialContinuation = new DosChildContinuationRecord
		{
			ParentCli = cli,
			Command = executeCommand,
			CommandLength = executeLineLength,
			State = DosChildContinuationState.Pending,
			Flags = (uint)DosChildContinuationFlags.RecordOwned,
		};
		if (!DosChildContinuationCodec.Initialize(ref dos, record,
			in initialContinuation))
		{
			DosShellNativeBridge.FreeScriptRecord(ref dos, state, frame,
				record, 3);
			return false;
		}

		var launched = TryRunCommand(dos, state, execBase, cli, input, output,
			error, BPTR.Null, record, executeCommand, executeLineLength,
			0, 0, 0, 0, 0, 0, out var rollbackComplete);
		if (!launched)
		{
			if (rollbackComplete)
				DosShellNativeBridge.FreeScriptRecord(ref dos, state, frame,
					record, 3);
			return false;
		}
		continuation = record;
		result = (int)CopperOS.Shell.ShellCommandResult.Ok;
		return true;
	}

	private static bool TryAppendExecutePrefix(
		ref CopperSharpNativeDosPlatform dos,
		ref ShellLaunchTextWriter writer) =>
		TryAppendLaunchByte(ref dos, ref writer, (byte)'E') &&
		TryAppendLaunchByte(ref dos, ref writer, (byte)'x') &&
		TryAppendLaunchByte(ref dos, ref writer, (byte)'e') &&
		TryAppendLaunchByte(ref dos, ref writer, (byte)'c') &&
		TryAppendLaunchByte(ref dos, ref writer, (byte)'u') &&
		TryAppendLaunchByte(ref dos, ref writer, (byte)'t') &&
		TryAppendLaunchByte(ref dos, ref writer, (byte)'e') &&
		TryAppendLaunchByte(ref dos, ref writer, (byte)' ') &&
		TryAppendLaunchByte(ref dos, ref writer, (byte)'"');

	private static bool TryAppendLaunchByte(
		ref CopperSharpNativeDosPlatform dos,
		ref ShellLaunchTextWriter writer, byte value)
	{
		if (writer.Buffer.IsNull || writer.Position >= writer.Capacity ||
			writer.Position > int.MaxValue ||
			writer.Buffer.Raw > uint.MaxValue - writer.Position)
			return false;
		var destination = APTR.FromPointer(writer.Buffer.Raw + writer.Position);
		if (!dos.IsMapped(destination, 1)) return false;
		dos.WriteUInt8(writer.Buffer, (int)writer.Position, value);
		writer.Position++;
		return true;
	}

	private static bool TryAppendLaunchBytes(
		ref CopperSharpNativeDosPlatform dos,
		ref ShellLaunchTextWriter writer, APTR source, uint length)
	{
		if (length == 0) return true;
		if (source.IsNull || source.Raw > uint.MaxValue - length ||
			!dos.IsMapped(source, length) || writer.Buffer.IsNull ||
			writer.Position > writer.Capacity ||
			length > writer.Capacity - writer.Position ||
			writer.Position > int.MaxValue ||
			writer.Buffer.Raw > uint.MaxValue - writer.Position)
			return false;
		var destination = APTR.FromPointer(writer.Buffer.Raw + writer.Position);
		if (!dos.IsMapped(destination, length)) return false;
		dos.Copy(source, destination, length);
		writer.Position += length;
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
		UtilityTagItemCodec.Write(ref dos, item, new TagItem { Tag = tag, Data = data });
	}

	internal static bool TryResolveChildStack(int inheritedStack,
		uint requestedStack, uint requestedPresent, out uint stackBytes)
	{
		stackBytes = 0;
		if (requestedPresent > 1 ||
			(requestedPresent == 0 && inheritedStack < 0)) return false;
		var selected = requestedPresent != 0 ? requestedStack :
			inheritedStack == 0 ? DefaultStack : unchecked((uint)inheritedStack);
		if (selected < 64 || selected > MaximumStack) return false;
		stackBytes = selected;
		return true;
	}

	internal static void WriteDefaultShellStartup<TMemory>(
		ref TMemory memory, APTR destination)
		where TMemory : struct, IAmigaGuestMemory
	{
		memory.WriteUInt8(destination, 0, (byte)'S');
		memory.WriteUInt8(destination, 1, (byte)':');
		memory.WriteUInt8(destination, 2, (byte)'S');
		memory.WriteUInt8(destination, 3, (byte)'h');
		memory.WriteUInt8(destination, 4, (byte)'e');
		memory.WriteUInt8(destination, 5, (byte)'l');
		memory.WriteUInt8(destination, 6, (byte)'l');
		memory.WriteUInt8(destination, 7, (byte)'-');
		memory.WriteUInt8(destination, 8, (byte)'S');
		memory.WriteUInt8(destination, 9, (byte)'t');
		memory.WriteUInt8(destination, 10, (byte)'a');
		memory.WriteUInt8(destination, 11, (byte)'r');
		memory.WriteUInt8(destination, 12, (byte)'t');
		memory.WriteUInt8(destination, 13, (byte)'u');
		memory.WriteUInt8(destination, 14, (byte)'p');
		memory.WriteUInt8(destination, 15, 0);
	}

	private static void CloseLaunchHandles(APTR state,
		in ShellLaunchResources resources)
	{
		if (resources.Window.IsNotNull)
			DosCore.CloseNativeHandle(state, resources.Window.Raw);
		if (resources.From.IsNotNull)
			DosCore.CloseNativeHandle(state, resources.From.Raw);
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
