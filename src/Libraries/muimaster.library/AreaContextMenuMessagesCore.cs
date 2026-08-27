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
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaContextMenuPacketKind packet,
		MuiAreaContextMenuMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaContextMenuMessageCodec
{
	internal const uint Add = 0x8042DF9Eu;
	internal const uint Build = 0x80429D2Eu;
	internal const uint Choice = 0x80420F0Eu;
	internal const uint BuildDefault = 0xFFFFFFFFu;

	private static bool TryReadHeader<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaContextMenuPacketKind packet, uint expected,
		out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		return MuiAreaContextMenuMessageMemoryCodec.TryReadUInt32(ref platform,
			message, packet, MuiAreaContextMenuMessageField.MethodId,
			out methodId) &&
			methodId == expected;
	}

	internal static bool IsMethod(uint method) => method == Add ||
		method == Build || method == Choice;

	internal static bool TryReadAdd<TPlatform>(ref TPlatform platform, APTR message,
		out MuiAreaContextMenuAddMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryReadHeader(ref platform, message,
			MuiAreaContextMenuPacketKind.Add, Add, out packet.MethodId) ||
			!MuiAreaContextMenuMessageMemoryCodec.TryReadUInt32(ref platform,
				message, MuiAreaContextMenuPacketKind.Add,
				MuiAreaContextMenuMessageField.MenuStrip, out var menuStrip) ||
			!MuiAreaContextMenuMessageMemoryCodec.TryReadUInt32(ref platform,
				message, MuiAreaContextMenuPacketKind.Add,
				MuiAreaContextMenuMessageField.MouseX, out var mouseX) ||
			!MuiAreaContextMenuMessageMemoryCodec.TryReadUInt32(ref platform,
				message, MuiAreaContextMenuPacketKind.Add,
				MuiAreaContextMenuMessageField.MouseY, out var mouseY) ||
			!MuiAreaContextMenuMessageMemoryCodec.TryReadUInt32(ref platform,
				message, MuiAreaContextMenuPacketKind.Add,
				MuiAreaContextMenuMessageField.MouseXPointer,
				out var mouseXPointer) ||
			!MuiAreaContextMenuMessageMemoryCodec.TryReadUInt32(ref platform,
				message, MuiAreaContextMenuPacketKind.Add,
				MuiAreaContextMenuMessageField.MouseYPointer,
				out var mouseYPointer)) return false;
		packet.MenuStrip = APTR.FromPointer(menuStrip);
		packet.MouseX = unchecked((int)mouseX);
		packet.MouseY = unchecked((int)mouseY);
		packet.MouseXPointer = APTR.FromPointer(mouseXPointer);
		packet.MouseYPointer = APTR.FromPointer(mouseYPointer);
		return true;
	}

	internal static bool TryReadBuild<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaContextMenuBuildMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryReadHeader(ref platform, message,
			MuiAreaContextMenuPacketKind.Build, Build, out packet.MethodId) ||
			!MuiAreaContextMenuMessageMemoryCodec.TryReadUInt32(ref platform,
				message, MuiAreaContextMenuPacketKind.Build,
				MuiAreaContextMenuMessageField.MouseX, out var mouseX) ||
			!MuiAreaContextMenuMessageMemoryCodec.TryReadUInt32(ref platform,
				message, MuiAreaContextMenuPacketKind.Build,
				MuiAreaContextMenuMessageField.MouseY, out var mouseY)) return false;
		packet.MouseX = unchecked((int)mouseX);
		packet.MouseY = unchecked((int)mouseY);
		return true;
	}

	internal static bool TryReadChoice<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaContextMenuChoiceMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryReadHeader(ref platform, message,
			MuiAreaContextMenuPacketKind.Choice, Choice, out packet.MethodId) ||
			!MuiAreaContextMenuMessageMemoryCodec.TryReadUInt32(ref platform,
				message, MuiAreaContextMenuPacketKind.Choice,
				MuiAreaContextMenuMessageField.Item, out var item)) return false;
		packet.Item = APTR.FromPointer(item);
		return true;
	}

	internal static bool WriteAdd<TPlatform>(ref TPlatform platform, APTR message,
		APTR menuStrip, int mouseX, int mouseY, APTR mouseXPointer,
		APTR mouseYPointer) where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaContextMenuMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiAreaContextMenuPacketKind.Add,
			MuiAreaContextMenuMessageField.MethodId, Add) &&
		MuiAreaContextMenuMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiAreaContextMenuPacketKind.Add,
			MuiAreaContextMenuMessageField.MenuStrip, menuStrip.Raw) &&
		MuiAreaContextMenuMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiAreaContextMenuPacketKind.Add,
			MuiAreaContextMenuMessageField.MouseX, unchecked((uint)mouseX)) &&
		MuiAreaContextMenuMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiAreaContextMenuPacketKind.Add,
			MuiAreaContextMenuMessageField.MouseY, unchecked((uint)mouseY)) &&
		MuiAreaContextMenuMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiAreaContextMenuPacketKind.Add,
			MuiAreaContextMenuMessageField.MouseXPointer, mouseXPointer.Raw) &&
		MuiAreaContextMenuMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiAreaContextMenuPacketKind.Add,
			MuiAreaContextMenuMessageField.MouseYPointer, mouseYPointer.Raw);

	internal static bool WriteBuild<TPlatform>(ref TPlatform platform, APTR message,
		int mouseX, int mouseY) where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaContextMenuMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiAreaContextMenuPacketKind.Build,
			MuiAreaContextMenuMessageField.MethodId, Build) &&
		MuiAreaContextMenuMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiAreaContextMenuPacketKind.Build,
			MuiAreaContextMenuMessageField.MouseX, unchecked((uint)mouseX)) &&
		MuiAreaContextMenuMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiAreaContextMenuPacketKind.Build,
			MuiAreaContextMenuMessageField.MouseY, unchecked((uint)mouseY));

	internal static bool WriteChoice<TPlatform>(ref TPlatform platform, APTR message,
		APTR item) where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaContextMenuMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiAreaContextMenuPacketKind.Choice,
			MuiAreaContextMenuMessageField.MethodId, Choice) &&
		MuiAreaContextMenuMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiAreaContextMenuPacketKind.Choice,
			MuiAreaContextMenuMessageField.Item, item.Raw);
}
