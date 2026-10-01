using System.Runtime.InteropServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.NativeRoot;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiNativeApplicationLoopProbeRecord
{
	public const uint Address = 0x0004F140;
	public const uint Size = 28;
	public uint PendingSignals;
	public uint LastWaitMask;
	public uint WaitCount;
	public uint DispatchCount;
	public uint AllocationCount;
	public uint FreeCount;
	public APTR FreedFrame;
}

public static class MuiNativeApplicationLoopProbeCodec
{
	public static bool TryRead<TPlatform>(ref TPlatform platform,
		out MuiNativeApplicationLoopProbeRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform,
			APTR.FromPointer(MuiNativeApplicationLoopProbeRecord.Address),
			MuiNativeApplicationLoopProbeRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.PendingSignals) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.LastWaitMask) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.WaitCount) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.DispatchCount) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.AllocationCount) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.FreeCount) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var freedFrame)) return false;
		value.FreedFrame = APTR.FromPointer(freedFrame);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	public static bool Write<TPlatform>(ref TPlatform platform,
		MuiNativeApplicationLoopProbeRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform,
			APTR.FromPointer(MuiNativeApplicationLoopProbeRecord.Address),
			MuiNativeApplicationLoopProbeRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.PendingSignals) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.LastWaitMask) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.WaitCount) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.DispatchCount) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.AllocationCount) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.FreeCount) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.FreedFrame.Raw) && MuiGuestStructCursor.IsComplete(cursor);
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiNativeApplicationLoopTestPlatform :
	IMuiNativeApplicationLoopPlatform
{
	public const uint FrameAddress = 0x0004F200;
	private uint _abiSlot;

	public bool IsMapped(APTR address, uint size)
	{
		_ = _abiSlot;
		return address.IsNotNull && size != 0 &&
			address.Raw <= uint.MaxValue - size;
	}

	public byte ReadUInt8(APTR address, int offset = 0) =>
		APTR.ReadUInt8(address, offset);

	public ushort ReadUInt16(APTR address, int offset = 0) =>
		APTR.ReadUInt16(address, offset);

	public uint ReadUInt32(APTR address, int offset = 0) =>
		APTR.ReadUInt32(address, offset);

	public void WriteUInt8(APTR address, int offset, byte value) =>
		APTR.WriteUInt8(address, offset, value);

	public void WriteUInt16(APTR address, int offset, ushort value) =>
		APTR.WriteUInt16(address, offset, value);

	public void WriteUInt32(APTR address, int offset, uint value) =>
		APTR.WriteUInt32(address, offset, value);

	public void Clear(APTR address, uint count)
	{
		if (!IsMapped(address, count)) return;
		for (var index = 0u; index < count; index++)
			APTR.WriteUInt8(APTR.FromPointer(address.Raw + index), 0, 0);
	}

	public void Copy(APTR source, APTR destination, uint count)
	{
		if (!IsMapped(source, count) || !IsMapped(destination, count)) return;
		if (destination.Raw > source.Raw)
		{
			for (var index = count; index != 0;)
			{
				index--;
				APTR.WriteUInt8(APTR.FromPointer(destination.Raw + index), 0,
					APTR.ReadUInt8(APTR.FromPointer(source.Raw + index), 0));
			}
			return;
		}
		for (var index = 0u; index < count; index++)
			APTR.WriteUInt8(APTR.FromPointer(destination.Raw + index), 0,
				APTR.ReadUInt8(APTR.FromPointer(source.Raw + index), 0));
	}

	public APTR Allocate(uint size, uint flags)
	{
		if (size != MuiNativeApplicationLoopFrameRecord.Size) return APTR.Null;
		var frame = APTR.FromPointer(FrameAddress);
		if (!IsMapped(frame, size)) return APTR.Null;
		Clear(frame, size);
		if (!MuiNativeApplicationLoopProbeCodec.TryRead(ref this,
			out var probe)) return APTR.Null;
		if (probe.AllocationCount != uint.MaxValue) probe.AllocationCount++;
		return MuiNativeApplicationLoopProbeCodec.Write(ref this, probe) ?
			frame : APTR.Null;
	}

	public void Free(APTR address, uint size)
	{
		if (size != MuiNativeApplicationLoopFrameRecord.Size ||
			address.Raw != FrameAddress ||
			!MuiNativeApplicationLoopProbeCodec.TryRead(ref this,
				out var probe)) return;
		if (probe.FreeCount != uint.MaxValue) probe.FreeCount++;
		probe.FreedFrame = address;
		MuiNativeApplicationLoopProbeCodec.Write(ref this, probe);
	}

	public uint DoMethod(APTR obj, APTR message)
	{
		if (!MuiApplicationInputMessageCodec.TryRead(ref this, message,
			out var input) || input.MethodId !=
			MuiApplicationDispatcher.ApplicationNewInputMethod ||
			input.SignalStorage.IsNull ||
			!MuiNativeApplicationLoopProbeCodec.TryRead(ref this,
				out var probe)) return MuiNativeApplicationLoopCore.ReturnIdQuit;
		if (probe.DispatchCount != uint.MaxValue) probe.DispatchCount++;
		var clearedSignals = default(MuiApplicationWindowSignalStorage);
		return MuiNativeApplicationLoopProbeCodec.Write(ref this, probe) &&
			MuiApplicationWindowSignalStorageCodec.Write(ref this,
				input.SignalStorage, clearedSignals) ? 0u :
			MuiNativeApplicationLoopCore.ReturnIdQuit;
	}

	public uint WaitMuiSignals(uint signalMask)
	{
		if (!MuiNativeApplicationLoopProbeCodec.TryRead(ref this,
			out var probe)) return 0;
		if (probe.WaitCount != uint.MaxValue) probe.WaitCount++;
		probe.LastWaitMask = signalMask;
		var received = probe.PendingSignals & signalMask;
		probe.PendingSignals &= ~received;
		return MuiNativeApplicationLoopProbeCodec.Write(ref this, probe) ?
			received : 0;
	}
}

public static class MuiNativeApplicationLoopRoot
{
	public static uint CtrlCStopsRunWithNoApplicationSignals()
	{
		var platform = default(MuiNativeApplicationLoopTestPlatform);
		var probe = new MuiNativeApplicationLoopProbeRecord
		{
			PendingSignals = MuiNativeApplicationLoopCore.SignalBreakCtrlC,
		};
		if (!MuiNativeApplicationLoopProbeCodec.Write(ref platform, probe))
			return 1;
		var result = MuiNativeApplicationLoopCore.Run(ref platform,
			APTR.FromPointer(0x00001200));
		if (result != MuiNativeApplicationLoopCore.ReturnIdQuit ||
			!MuiNativeApplicationLoopProbeCodec.TryRead(ref platform,
				out probe) || probe.WaitCount != 1 || probe.DispatchCount != 1 ||
			probe.LastWaitMask != MuiNativeApplicationLoopCore.SignalBreakCtrlC ||
			probe.PendingSignals != 0 || probe.AllocationCount != 1 ||
			probe.FreeCount != 1 ||
			probe.FreedFrame.Raw != MuiNativeApplicationLoopTestPlatform.FrameAddress)
			return 2;
		return 42;
	}
}
