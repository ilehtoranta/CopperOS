/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// The native loop needs only these bounded capabilities. The production
// implementation uses Exec and the BOOPSI dispatcher; the host fake exercises
// the same loop state machine without introducing a native runtime dependency.
internal interface IMuiNativeApplicationLoopPlatform : IMuiGuestMemory
{
	APTR Allocate(uint size, uint flags);
	void Free(APTR address, uint size);
	uint DoMethod(APTR obj, APTR message);
	uint WaitMuiSignals(uint signalMask);
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeApplicationLoopFrameRecord
{
	internal const uint Size = MuiApplicationInputMessage.Size +
		MuiApplicationWindowSignalStorage.Size;
	internal MuiApplicationInputMessage InputMessage;
	internal MuiApplicationWindowSignalStorage SignalStorage;
}

internal static class MuiNativeApplicationLoopFrameCodec
{
	internal static bool TryInitialize<TMemory>(ref TMemory memory, APTR address,
		out APTR inputMessageAddress, out APTR signalStorageAddress)
		where TMemory : struct, IMuiGuestMemory
	{
		inputMessageAddress = APTR.Null;
		signalStorageAddress = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeApplicationLoopFrameRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiApplicationInputMessage.Size, out inputMessageAddress) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiApplicationWindowSignalStorage.Size,
				out signalStorageAddress) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;

		var signals = new MuiApplicationWindowSignalStorage { Signals = 0 };
		var input = new MuiApplicationInputMessage
		{
			MethodId = MuiApplicationDispatcher.ApplicationNewInputMethod,
			SignalStorage = signalStorageAddress,
		};
		return MuiApplicationWindowSignalStorageCodec.Write(ref memory,
			signalStorageAddress, signals) &&
			MuiApplicationInputMessageCodec.Write(ref memory, inputMessageAddress,
				input);
	}
}

internal static class MuiNativeApplicationLoopCore
{
	internal const uint ReturnIdQuit = uint.MaxValue;
	internal const uint SignalBreakCtrlC = 0x00001000;

	internal static bool TryDispatch(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR application, APTR message,
		uint method, out uint result)
	{
		result = 0;
		if ((method != MuiApplicationLoopMessage.ExecuteMethodId &&
			method != MuiApplicationLoopMessage.RunMethodId) ||
			!MuiApplicationLoopMessageCodec.TryRead(ref platform, message,
				out var packet) || packet.MethodId != method ||
			!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
				ownerRoot, application, out _)) return false;
		result = Run(ref platform, application);
		return true;
	}

	internal static uint Run<TPlatform>(ref TPlatform platform, APTR application)
		where TPlatform : struct, IMuiNativeApplicationLoopPlatform
	{
		if (application.IsNull) return 0;
		var frame = platform.Allocate(MuiNativeApplicationLoopFrameRecord.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (frame.IsNull) return 0;

		if (!MuiNativeApplicationLoopFrameCodec.TryInitialize(ref platform, frame,
			out var inputMessage, out var signalStorage))
		{
			platform.Clear(frame, MuiNativeApplicationLoopFrameRecord.Size);
			platform.Free(frame, MuiNativeApplicationLoopFrameRecord.Size);
			return 0;
		}

		var result = 0u;
		var running = true;
		while (running)
		{
			var inputResult = platform.DoMethod(application, inputMessage);
			if (inputResult == ReturnIdQuit)
			{
				result = ReturnIdQuit;
				running = false;
				continue;
			}
			if (!MuiApplicationWindowSignalStorageCodec.TryRead(ref platform,
				signalStorage, out var waitState))
			{
				running = false;
				continue;
			}

			var receivedSignals = platform.WaitMuiSignals(waitState.Signals |
				SignalBreakCtrlC);
			if ((receivedSignals & SignalBreakCtrlC) != 0)
			{
				result = ReturnIdQuit;
				running = false;
				continue;
			}
			waitState.Signals = receivedSignals;
			if (!MuiApplicationWindowSignalStorageCodec.Write(ref platform,
				signalStorage, waitState)) running = false;
		}

		platform.Clear(frame, MuiNativeApplicationLoopFrameRecord.Size);
		platform.Free(frame, MuiNativeApplicationLoopFrameRecord.Size);
		return result;
	}
}
