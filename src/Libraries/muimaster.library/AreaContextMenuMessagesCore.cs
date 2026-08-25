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
	internal uint MethodId;
	internal int MouseX;
	internal int MouseY;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaContextMenuChoiceMessage
{
	internal const uint Size = 8;
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

internal static class MuiAreaContextMenuMessageCodec
{
	internal const uint Add = 0x8042DF9Eu;
	internal const uint Build = 0x80429D2Eu;
	internal const uint Choice = 0x80420F0Eu;
	internal const uint BuildDefault = 0xFFFFFFFFu;

	private static uint PacketSize(MuiAreaContextMenuPacketKind packet) =>
		packet switch
		{
			MuiAreaContextMenuPacketKind.Add => MuiAreaContextMenuAddMessage.Size,
			MuiAreaContextMenuPacketKind.Build => MuiAreaContextMenuBuildMessage.Size,
			MuiAreaContextMenuPacketKind.Choice => MuiAreaContextMenuChoiceMessage.Size,
			_ => 0u,
		};

	private static bool TryReadWord<TPlatform>(ref TPlatform platform,
		APTR message, uint offset, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (message.IsNull || message.Raw > uint.MaxValue - offset) return false;
		var address = APTR.FromPointer(message.Raw + offset);
		if (!platform.IsMapped(address, 4)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	private static bool TryWriteWord<TPlatform>(ref TPlatform platform,
		APTR message, uint offset, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || message.Raw > uint.MaxValue - offset) return false;
		var address = APTR.FromPointer(message.Raw + offset);
		if (!platform.IsMapped(address, 4)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	private static bool TryReadHeader<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaContextMenuPacketKind packet, uint expected,
		out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		var size = PacketSize(packet);
		return size != 0 && message.IsNotNull && platform.IsMapped(message, size) &&
			TryReadWord(ref platform, message, 0, out methodId) &&
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
			!TryReadWord(ref platform, message, 4, out var menuStrip) ||
			!TryReadWord(ref platform, message, 8, out var mouseX) ||
			!TryReadWord(ref platform, message, 12, out var mouseY) ||
			!TryReadWord(ref platform, message, 16, out var mouseXPointer) ||
			!TryReadWord(ref platform, message, 20, out var mouseYPointer)) return false;
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
			!TryReadWord(ref platform, message, 4, out var mouseX) ||
			!TryReadWord(ref platform, message, 8, out var mouseY)) return false;
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
			!TryReadWord(ref platform, message, 4, out var item)) return false;
		packet.Item = APTR.FromPointer(item);
		return true;
	}

	internal static bool WriteAdd<TPlatform>(ref TPlatform platform, APTR message,
		APTR menuStrip, int mouseX, int mouseY, APTR mouseXPointer,
		APTR mouseYPointer) where TPlatform : struct, IMuiGuestMemory =>
		TryWriteWord(ref platform, message, 0, Add) &&
		TryWriteWord(ref platform, message, 4, menuStrip.Raw) &&
		TryWriteWord(ref platform, message, 8, unchecked((uint)mouseX)) &&
		TryWriteWord(ref platform, message, 12, unchecked((uint)mouseY)) &&
		TryWriteWord(ref platform, message, 16, mouseXPointer.Raw) &&
		TryWriteWord(ref platform, message, 20, mouseYPointer.Raw);

	internal static bool WriteBuild<TPlatform>(ref TPlatform platform, APTR message,
		int mouseX, int mouseY) where TPlatform : struct, IMuiGuestMemory =>
		TryWriteWord(ref platform, message, 0, Build) &&
		TryWriteWord(ref platform, message, 4, unchecked((uint)mouseX)) &&
		TryWriteWord(ref platform, message, 8, unchecked((uint)mouseY));

	internal static bool WriteChoice<TPlatform>(ref TPlatform platform, APTR message,
		APTR item) where TPlatform : struct, IMuiGuestMemory =>
		TryWriteWord(ref platform, message, 0, Choice) &&
		TryWriteWord(ref platform, message, 4, item.Raw);
}
