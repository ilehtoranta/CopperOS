/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Native automatic online help for the documented MorphOS HELP-key route.
// Help attributes and object ancestry remain in named MUI sidecar/binding
// records; the only AmigaGuide ABI record used here is NewAmigaGuide.
internal static class MuiNativeApplicationHelp
{
	private const uint MaximumStringLength = 65536;

	internal static bool IsHelpKeyPress(MuiIntuiPointerMessage message) =>
		message.Class == MuiIntuiMessageCodec.RawKeyClass &&
		message.Code == MuiNativeApplicationWindowEvents.RawKeyHelp;

	internal static bool TryHandleHelpKey(
		ref MuiNativeClassPlatform platform, APTR publicObjects,
		APTR ownerRoot, APTR application, APTR windowObject,
		APTR windowSidecar, APTR intuiMessage)
	{
		var memory = default(MuiNativeClassMemory);
		if (!MuiIntuiMessageCodec.TryReadPointerRecord(ref memory, intuiMessage,
			out var message) || !IsHelpKeyPress(message) ||
			!MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
			ownerRoot, application, out _) ||
			!MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
				ownerRoot, windowObject, out var liveWindow) ||
			liveWindow.Sidecar != windowSidecar ||
			!MuiNativePublicObjectCore.GetAttribute(ref platform, publicObjects,
				ownerRoot, application,
				MuiApplicationWindowCore.ApplicationHelpFile,
				out var helpFileRaw)) return false;
		var helpFile = APTR.FromPointer(helpFileRaw);
		if (helpFile.IsNull || !TryResolveObjectHelp(ref memory,
			publicObjects, ownerRoot, windowSidecar, out var node, out var line) ||
			node.IsNull ||
			!CStringCodec.TryReadLength(ref memory, helpFile,
				MaximumStringLength, out _) ||
			!CStringCodec.TryReadLength(ref memory, node,
				MaximumStringLength, out _) ||
			!TryReadWindowScreen(ref memory, windowSidecar, out var screen))
			return false;
		var sleepScope = default(MuiNativeApplicationSleepScope);
		if (!MuiNativeApplicationSleep.TryChangeApplicationSleep(ref platform,
			publicObjects, ownerRoot, application, true, ref sleepScope))
			return false;
		var shown = MuiNativeAmigaGuidePresentation.Show(ref platform, screen,
			helpFile, node, line);
		var woke = MuiNativeApplicationSleep.TryChangeApplicationSleep(
			ref platform, publicObjects, ownerRoot, application, false,
			ref sleepScope);
		if (!woke)
			woke = MuiNativeApplicationSleep.TryChangeApplicationSleep(
				ref platform, publicObjects, ownerRoot, application, false,
				ref sleepScope);
		MuiNativeApplicationSleep.Release(ref platform, ref sleepScope);
		// If wake-up could not be completed, keep the HELP event out of ordinary
		// handlers: those would run against the still-sleeping native object tree.
		return shown || !woke;
	}

	private static bool TryResolveObjectHelp<TMemory>(ref TMemory memory,
		APTR publicObjects, APTR ownerRoot, APTR windowSidecar,
		out APTR node, out int line)
		where TMemory : struct, IMuiGuestMemory
	{
		node = APTR.Null;
		line = 0;
		if (!MuiNativeObjectStateCore.TryGetAttribute(ref memory, windowSidecar,
			MuiWindowPublicCore.MouseObject, out var mouseObjectRaw)) return false;
		var current = APTR.FromPointer(mouseObjectRaw);
		if (current.IsNull) return false;

		var nodeFound = false;
		var lineFound = false;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
				ownerRoot, current, out var binding)) return false;
			if (!nodeFound && MuiNativeObjectStateCore.TryGetAttribute(ref memory,
				binding.Sidecar, MuiHelpStateCore.HelpNode, out var nodeRaw) &&
				nodeRaw != 0)
			{
				node = APTR.FromPointer(nodeRaw);
				nodeFound = true;
			}
			if (!lineFound && MuiNativeObjectStateCore.TryGetAttribute(ref memory,
				binding.Sidecar, MuiHelpStateCore.HelpLine, out var lineRaw))
			{
				line = unchecked((int)lineRaw);
				lineFound = true;
			}
			if (nodeFound && lineFound) return true;
			current = binding.Parent;
		}
		return current.IsNull && nodeFound;
	}

	private static bool TryReadWindowScreen<TMemory>(ref TMemory memory,
		APTR windowSidecar, out APTR screen)
		where TMemory : struct, IMuiGuestMemory
	{
		screen = APTR.Null;
		if (!MuiNativeMuiObjectCodec.TryRead(ref memory, windowSidecar,
			out var sidecar) || sidecar.NativeWindow.IsNull ||
			!memory.IsMapped(sidecar.NativeWindow, Window.Size)) return false;
		var window = IntuitionScreenWindowGuestCodec.ReadWindow(ref memory,
			sidecar.NativeWindow);
		screen = window.Screen;
		return true;
	}
}

internal static class MuiNativeNewAmigaGuideCodec
{
	internal static bool TryRead<TMemory>(ref TMemory memory, APTR address,
		out NewAmigaGuide value)
		where TMemory : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			NewAmigaGuide.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var lockValue) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var name) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var screen) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var publicScreen) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var hostPort) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var clientPort) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var baseName) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Flags) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var context) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var node) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var line) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var extensions) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var client) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		value.Lock = BPTR.FromRaw(lockValue);
		value.Name = STRPTR.FromPointer(name);
		value.Screen = APTR.FromPointer(screen);
		value.PublicScreen = STRPTR.FromPointer(publicScreen);
		value.HostPort = STRPTR.FromPointer(hostPort);
		value.ClientPort = STRPTR.FromPointer(clientPort);
		value.BaseName = STRPTR.FromPointer(baseName);
		value.Context = APTR.FromPointer(context);
		value.Node = STRPTR.FromPointer(node);
		value.Line = unchecked((int)line);
		value.Extensions = APTR.FromPointer(extensions);
		value.Client = APTR.FromPointer(client);
		return true;
	}

	internal static bool TryWrite<TMemory>(ref TMemory memory, APTR address,
		NewAmigaGuide value)
		where TMemory : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref memory, address, NewAmigaGuide.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Lock.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Name.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Screen.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.PublicScreen.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.HostPort.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.ClientPort.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.BaseName.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Flags) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Context.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Node.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			unchecked((uint)value.Line)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Extensions.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Client.Raw) && MuiGuestStructCursor.IsComplete(cursor);
}

internal static class MuiNativeAmigaGuidePresentation
{
	private const ushort MinimumAmigaGuideVersion = 45;

	internal static bool Show(ref MuiNativeClassPlatform platform, APTR screen,
		APTR name, APTR node, int line)
	{
		var memory = default(MuiNativeClassMemory);
		if (name.IsNull || !CStringCodec.TryReadLength(ref memory, name,
			65536, out _) || node.IsNotNull &&
			!CStringCodec.TryReadLength(ref memory, node, 65536, out _)) return false;
		var request = platform.Allocate(NewAmigaGuide.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (request.IsNull) return false;
		var guide = default(NewAmigaGuide);
		guide.Name = STRPTR.FromAddress(name);
		guide.Screen = screen;
		guide.Node = STRPTR.FromAddress(node);
		guide.Line = line;
		if (!MuiNativeNewAmigaGuideCodec.TryWrite(ref platform, request, guide))
		{
			platform.Free(request, NewAmigaGuide.Size);
			return false;
		}

		var guideBase = Exec.OpenLibraryRaw(CString.FromLiteral(
			AmigaGuide.Name), MinimumAmigaGuideVersion);
		if (guideBase.IsNull)
		{
			platform.Clear(request, NewAmigaGuide.Size);
			platform.Free(request, NewAmigaGuide.Size);
			return false;
		}
		AmigaGuide.AmigaGuideLibraryBase = guideBase;
		var context = AmigaGuide.OpenAmigaGuideA(request, APTR.Null);
		var shown = context.IsNotNull;
		if (context.IsNotNull) AmigaGuide.CloseAmigaGuide(context);
		AmigaGuide.AmigaGuideLibraryBase = APTR.Null;
		Exec.CloseLibrary(guideBase);
		platform.Clear(request, NewAmigaGuide.Size);
		platform.Free(request, NewAmigaGuide.Size);
		return shown;
	}
}
