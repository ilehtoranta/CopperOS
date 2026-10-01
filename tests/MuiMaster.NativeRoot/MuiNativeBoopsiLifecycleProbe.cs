using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster.NativeRoot;

// Opt-in simulated superclass provider. This does not stand in for production
// Intuition; it supplies observable guest allocation and callback arguments to
// the freestanding constructor/destructor regression.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeBoopsiLifecycleProbe
{
	internal const uint Size = 32;
	internal const uint Cookie = 0x424C4350;
	internal uint Magic;
	internal uint Calls;
	internal uint FailNew;
	internal APTR LastClass;
	internal APTR LastOperand;
	internal APTR LastMessage;
	internal uint LastMethod;
	internal APTR LastResult;
}

internal static class MuiNativeBoopsiLifecycleProbeCodec
{
	internal static bool TryRead(ref MuiNativeHeadlessPlatform platform, APTR address,
		out MuiNativeBoopsiLifecycleProbe value)
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address, MuiNativeBoopsiLifecycleProbe.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor, out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor, out value.Calls) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor, out value.FailNew) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor, out var cls) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor, out var operand) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor, out var message) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor, out value.LastMethod) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor, out var result)) return false;
		value.LastClass = APTR.FromPointer(cls);
		value.LastOperand = APTR.FromPointer(operand);
		value.LastMessage = APTR.FromPointer(message);
		value.LastResult = APTR.FromPointer(result);
		return value.Magic == MuiNativeBoopsiLifecycleProbe.Cookie && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write(ref MuiNativeHeadlessPlatform platform, APTR address,
		MuiNativeBoopsiLifecycleProbe value)
	{
		return MuiGuestStructCursor.TryCreate(ref platform, address, MuiNativeBoopsiLifecycleProbe.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, value.Calls) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, value.FailNew) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, value.LastClass.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, value.LastOperand.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, value.LastMessage.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, value.LastMethod) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, value.LastResult.Raw) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiNativeBoopsiLifecycleProbeCore
{
	internal static uint Dispatch(ref MuiNativeHeadlessPlatform platform,
		APTR cls, APTR operand, APTR message)
	{
		var address = platform.BoopsiLifecycleProbe;
		if (address.IsNull || !MuiNativeBoopsiLifecycleProbeCodec.TryRead(ref platform,
			address, out var probe)) return 1;
		if (!platform.IsMapped(message, BOOPSIGuestCodec.MethodMessageSize)) return 0;
		probe.Calls++;
		probe.LastClass = cls;
		probe.LastOperand = operand;
		probe.LastMessage = message;
		probe.LastMethod = BOOPSIGuestCodec.ReadMethodId(ref platform, message);
		probe.LastResult = APTR.Null;
		if (probe.LastMethod == BOOPSI.OM_NEW && probe.FailNew == 0 && platform.IsMapped(message, opSet.Size))
		{
			var packet = BOOPSIGuestCodec.ReadOpSet(ref platform, message);
			probe.LastResult = platform.NewObject(operand, packet.ops_AttrList);
		}
		if (probe.LastMethod == BOOPSI.OM_DISPOSE) platform.DisposeObject(operand);
		if (!MuiNativeBoopsiLifecycleProbeCodec.Write(ref platform, address, probe)) return 0;
		return probe.LastResult.Raw;
	}
}
