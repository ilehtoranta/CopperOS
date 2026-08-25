/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// The Intuition InputEvent is a fixed-size guest record.  Keep the public
// value typed as Amiga.InputEvent; only this codec knows the packed guest
// layout required to cross the memory interface.  MUI window state stores the
// validated pointer, never a managed copy of this value.
internal static class MuiWindowInputEventCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out InputEvent value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address, InputEvent.Size))
			return false;
		value.NextEvent = APTR.FromPointer(platform.ReadUInt32(address,
			InputEventLayout.NextEvent));
		value.Class = (InputEventClass)platform.ReadUInt8(address,
			InputEventLayout.Class);
		value.SubClass = (InputEventSubClass)platform.ReadUInt8(address,
			InputEventLayout.SubClass);
		value.Code = platform.ReadUInt16(address, InputEventLayout.Code);
		value.Qualifier = (InputEventQualifier)platform.ReadUInt16(address,
			InputEventLayout.Qualifier);
		value.Position = unchecked((int)platform.ReadUInt32(address,
			InputEventLayout.Position));
		value.TimeStamp.Seconds = platform.ReadUInt32(address,
			InputEventLayout.Seconds);
		value.TimeStamp.Microseconds = platform.ReadUInt32(address,
			InputEventLayout.Microseconds);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		InputEvent value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address, InputEvent.Size))
			return false;
		platform.WriteUInt32(address, InputEventLayout.NextEvent,
			value.NextEvent.Raw);
		platform.WriteUInt8(address, InputEventLayout.Class,
			(byte)value.Class);
		platform.WriteUInt8(address, InputEventLayout.SubClass,
			(byte)value.SubClass);
		platform.WriteUInt16(address, InputEventLayout.Code, value.Code);
		platform.WriteUInt16(address, InputEventLayout.Qualifier,
			(ushort)value.Qualifier);
		platform.WriteUInt32(address, InputEventLayout.Position,
			unchecked((uint)value.Position));
		platform.WriteUInt32(address, InputEventLayout.Seconds,
			value.TimeStamp.Seconds);
		platform.WriteUInt32(address, InputEventLayout.Microseconds,
			value.TimeStamp.Microseconds);
		return true;
	}
}
