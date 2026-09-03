/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Fixed MorphOS Area context-menu packets.  The public packet shapes stay
// named; only this guest codec contains the unavoidable packed ABI boundary.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaContextMenuAddMessage
{
	internal const uint Size = 24;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint MenuStripOffset = 4;
	internal const uint MouseXOffset = 8;
	internal const uint MouseYOffset = 12;
	internal const uint MouseXPointerOffset = 16;
	internal const uint MouseYPointerOffset = 20;
	internal uint MethodId;
	internal APTR MenuStrip;
	internal int MouseX;
	internal int MouseY;
	internal APTR MouseXPointer;
	internal APTR MouseYPointer;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaContextMenuBuildMessage
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint MouseXOffset = 4;
	internal const uint MouseYOffset = 8;
	internal uint MethodId;
	internal int MouseX;
	internal int MouseY;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaContextMenuChoiceMessage
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint ItemOffset = 4;
	internal uint MethodId;
	internal APTR Item;
}

internal enum MuiAreaContextMenuPacketKind : byte
{
	Add,
	Build,
	Choice,
}

internal enum MuiAreaContextMenuMessageField : byte
{
	MethodId,
	MenuStrip,
	MouseX,
	MouseY,
	MouseXPointer,
	MouseYPointer,
	Item,
}

// Struct-first guest-memory adapter for the fixed Add/Build/Choice packets.
// The packet kind owns its complete record span and the field enum names each
// member, keeping packed MorphOS ABI translation in one bounded layer.
internal static class MuiAreaContextMenuMessageMemoryCodec
{
	private static bool TryResolve(MuiAreaContextMenuPacketKind packet,
		MuiAreaContextMenuMessageField field, out uint offset, out uint size)
	{
		switch (packet)
		{
			case MuiAreaContextMenuPacketKind.Add:
				size = MuiAreaContextMenuAddMessage.Size;
				if (field == MuiAreaContextMenuMessageField.MethodId)
					offset = MuiAreaContextMenuAddMessage.MethodIdOffset;
				else if (field == MuiAreaContextMenuMessageField.MenuStrip)
					offset = MuiAreaContextMenuAddMessage.MenuStripOffset;
				else if (field == MuiAreaContextMenuMessageField.MouseX)
					offset = MuiAreaContextMenuAddMessage.MouseXOffset;
				else if (field == MuiAreaContextMenuMessageField.MouseY)
					offset = MuiAreaContextMenuAddMessage.MouseYOffset;
				else if (field == MuiAreaContextMenuMessageField.MouseXPointer)
					offset = MuiAreaContextMenuAddMessage.MouseXPointerOffset;
				else if (field == MuiAreaContextMenuMessageField.MouseYPointer)
					offset = MuiAreaContextMenuAddMessage.MouseYPointerOffset;
				else
				{
					offset = 0;
					size = 0;
					return false;
				}
				return true;
			case MuiAreaContextMenuPacketKind.Build:
				size = MuiAreaContextMenuBuildMessage.Size;
				if (field == MuiAreaContextMenuMessageField.MethodId)
					offset = MuiAreaContextMenuBuildMessage.MethodIdOffset;
				else if (field == MuiAreaContextMenuMessageField.MouseX)
					offset = MuiAreaContextMenuBuildMessage.MouseXOffset;
				else if (field == MuiAreaContextMenuMessageField.MouseY)
					offset = MuiAreaContextMenuBuildMessage.MouseYOffset;
				else
				{
					offset = 0;
					size = 0;
					return false;
				}
				return true;
			case MuiAreaContextMenuPacketKind.Choice:
				size = MuiAreaContextMenuChoiceMessage.Size;
				if (field == MuiAreaContextMenuMessageField.MethodId)
					offset = MuiAreaContextMenuChoiceMessage.MethodIdOffset;
				else if (field == MuiAreaContextMenuMessageField.Item)
					offset = MuiAreaContextMenuChoiceMessage.ItemOffset;
				else
				{
					offset = 0;
					size = 0;
					return false;
				}
				return true;
		}
		offset = 0;
		size = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaContextMenuPacketKind packet,
		MuiAreaContextMenuMessageField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(packet, field, out var offset, out var size) ||
			message.IsNull || message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, size)) return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, MuiAreaContextMenuAddMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaContextMenuPacketKind packet,
		MuiAreaContextMenuMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (packet == MuiAreaContextMenuPacketKind.Add)
		{
			if (!MuiAreaContextMenuAddMessageCodec.TryReadStructural(ref platform,
				message, out var add)) return false;
			if (field == MuiAreaContextMenuMessageField.MethodId)
				value = add.MethodId;
			else if (field == MuiAreaContextMenuMessageField.MenuStrip)
				value = add.MenuStrip.Raw;
			else if (field == MuiAreaContextMenuMessageField.MouseX)
				value = unchecked((uint)add.MouseX);
			else if (field == MuiAreaContextMenuMessageField.MouseY)
				value = unchecked((uint)add.MouseY);
			else if (field == MuiAreaContextMenuMessageField.MouseXPointer)
				value = add.MouseXPointer.Raw;
			else if (field == MuiAreaContextMenuMessageField.MouseYPointer)
				value = add.MouseYPointer.Raw;
			else
				return false;
			return true;
		}
		if (packet == MuiAreaContextMenuPacketKind.Build)
		{
			if (!MuiAreaContextMenuBuildMessageCodec.TryReadStructural(ref platform,
				message, out var build)) return false;
			if (field == MuiAreaContextMenuMessageField.MethodId)
				value = build.MethodId;
			else if (field == MuiAreaContextMenuMessageField.MouseX)
				value = unchecked((uint)build.MouseX);
			else if (field == MuiAreaContextMenuMessageField.MouseY)
				value = unchecked((uint)build.MouseY);
			else
				return false;
			return true;
		}
		if (packet == MuiAreaContextMenuPacketKind.Choice)
		{
			if (!MuiAreaContextMenuChoiceMessageCodec.TryReadStructural(ref platform,
				message, out var choice)) return false;
			if (field == MuiAreaContextMenuMessageField.MethodId)
				value = choice.MethodId;
			else if (field == MuiAreaContextMenuMessageField.Item)
				value = choice.Item.Raw;
			else
				return false;
			return true;
		}
		return false;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaContextMenuPacketKind packet,
		MuiAreaContextMenuMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (packet == MuiAreaContextMenuPacketKind.Add)
		{
			if (!MuiAreaContextMenuAddMessageCodec.TryReadStructural(ref platform,
				message, out var add)) return false;
			if (field == MuiAreaContextMenuMessageField.MethodId)
				add.MethodId = value;
			else if (field == MuiAreaContextMenuMessageField.MenuStrip)
				add.MenuStrip = APTR.FromPointer(value);
			else if (field == MuiAreaContextMenuMessageField.MouseX)
				add.MouseX = unchecked((int)value);
			else if (field == MuiAreaContextMenuMessageField.MouseY)
				add.MouseY = unchecked((int)value);
			else if (field == MuiAreaContextMenuMessageField.MouseXPointer)
				add.MouseXPointer = APTR.FromPointer(value);
			else if (field == MuiAreaContextMenuMessageField.MouseYPointer)
				add.MouseYPointer = APTR.FromPointer(value);
			else
				return false;
			return MuiAreaContextMenuAddMessageCodec.WriteStructural(ref platform,
				message, add);
		}
		if (packet == MuiAreaContextMenuPacketKind.Build)
		{
			if (!MuiAreaContextMenuBuildMessageCodec.TryReadStructural(ref platform,
				message, out var build)) return false;
			if (field == MuiAreaContextMenuMessageField.MethodId)
				build.MethodId = value;
			else if (field == MuiAreaContextMenuMessageField.MouseX)
				build.MouseX = unchecked((int)value);
			else if (field == MuiAreaContextMenuMessageField.MouseY)
				build.MouseY = unchecked((int)value);
			else
				return false;
			return MuiAreaContextMenuBuildMessageCodec.WriteStructural(ref platform,
				message, build);
		}
		if (packet == MuiAreaContextMenuPacketKind.Choice)
		{
			if (!MuiAreaContextMenuChoiceMessageCodec.TryReadStructural(ref platform,
				message, out var choice)) return false;
			if (field == MuiAreaContextMenuMessageField.MethodId)
				choice.MethodId = value;
			else if (field == MuiAreaContextMenuMessageField.Item)
				choice.Item = APTR.FromPointer(value);
			else
				return false;
			return MuiAreaContextMenuChoiceMessageCodec.WriteStructural(ref platform,
				message, choice);
		}
		return false;
	}
}

internal static class MuiAreaContextMenuAddMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaContextMenuAddMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaContextMenuAddMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMenuStrip) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMouseX) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMouseY) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMouseXPointer) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMouseYPointer) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.MenuStrip = APTR.FromPointer(rawMenuStrip);
		value.MouseX = unchecked((int)rawMouseX);
		value.MouseY = unchecked((int)rawMouseY);
		value.MouseXPointer = APTR.FromPointer(rawMouseXPointer);
		value.MouseYPointer = APTR.FromPointer(rawMouseYPointer);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaContextMenuAddMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaContextMenuAddMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MenuStrip.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.MouseX)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.MouseY)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MouseXPointer.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MouseYPointer.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaContextMenuAddMessage value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryRead(ref platform, address, out value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaContextMenuAddMessage value)
		where TPlatform : struct, IMuiGuestMemory =>
		Write(ref platform, address, value);
}

internal static class MuiAreaContextMenuBuildMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaContextMenuBuildMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaContextMenuBuildMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMouseX) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMouseY) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.MouseX = unchecked((int)rawMouseX);
		value.MouseY = unchecked((int)rawMouseY);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaContextMenuBuildMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaContextMenuBuildMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.MouseX)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.MouseY))) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaContextMenuBuildMessage value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryRead(ref platform, address, out value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaContextMenuBuildMessage value)
		where TPlatform : struct, IMuiGuestMemory =>
		Write(ref platform, address, value);
}

internal static class MuiAreaContextMenuChoiceMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaContextMenuChoiceMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaContextMenuChoiceMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawItem) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Item = APTR.FromPointer(rawItem);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaContextMenuChoiceMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaContextMenuChoiceMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Item.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaContextMenuChoiceMessage value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryRead(ref platform, address, out value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaContextMenuChoiceMessage value)
		where TPlatform : struct, IMuiGuestMemory =>
		Write(ref platform, address, value);
}

internal static class MuiAreaContextMenuMessageCodec
{
	internal const uint Add = 0x8042DF9Eu;
	internal const uint Build = 0x80429D2Eu;
	internal const uint Choice = 0x80420F0Eu;
	internal const uint BuildDefault = 0xFFFFFFFFu;

	internal static bool IsMethod(uint method) => method == Add ||
		method == Build || method == Choice;

	internal static bool TryReadAdd<TPlatform>(ref TPlatform platform, APTR message,
		out MuiAreaContextMenuAddMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaContextMenuAddMessageCodec.TryRead(ref platform, message,
			out packet) || packet.MethodId != Add)
		{
			packet = default;
			return false;
		}
		return true;
	}

	internal static bool TryReadBuild<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaContextMenuBuildMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaContextMenuBuildMessageCodec.TryRead(ref platform, message,
			out packet) || packet.MethodId != Build)
		{
			packet = default;
			return false;
		}
		return true;
	}

	internal static bool TryReadChoice<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaContextMenuChoiceMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaContextMenuChoiceMessageCodec.TryRead(ref platform, message,
			out packet) || packet.MethodId != Choice)
		{
			packet = default;
			return false;
		}
		return true;
	}

	internal static bool WriteAdd<TPlatform>(ref TPlatform platform, APTR message,
		APTR menuStrip, int mouseX, int mouseY, APTR mouseXPointer,
		APTR mouseYPointer) where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiAreaContextMenuAddMessage);
		packet.MethodId = Add;
		packet.MenuStrip = menuStrip;
		packet.MouseX = mouseX;
		packet.MouseY = mouseY;
		packet.MouseXPointer = mouseXPointer;
		packet.MouseYPointer = mouseYPointer;
		return MuiAreaContextMenuAddMessageCodec.Write(ref platform, message,
			packet);
	}

	internal static bool WriteBuild<TPlatform>(ref TPlatform platform, APTR message,
		int mouseX, int mouseY) where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiAreaContextMenuBuildMessage);
		packet.MethodId = Build;
		packet.MouseX = mouseX;
		packet.MouseY = mouseY;
		return MuiAreaContextMenuBuildMessageCodec.Write(ref platform, message,
			packet);
	}

	internal static bool WriteChoice<TPlatform>(ref TPlatform platform, APTR message,
		APTR item) where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiAreaContextMenuChoiceMessage);
		packet.MethodId = Choice;
		packet.Item = item;
		return MuiAreaContextMenuChoiceMessageCodec.Write(ref platform, message,
			packet);
	}
}
