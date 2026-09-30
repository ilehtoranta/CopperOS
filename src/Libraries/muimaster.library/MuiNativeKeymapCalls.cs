/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.MuiMaster;

// The keymap base is an explicitly leased provider owned by the class platform;
// never mutate the SDK's global KeymapLibraryBase. This is an ABI-call
// boundary, not a state model.
internal static class MuiNativeKeymapCalls
{
	private const short AskKeyMapDefaultLvo = -36;
	private const short MapRawKeyLvo = -42;
	private const uint TranslationBufferSize = 4;

	internal static bool TryMapRawKey(APTR library, APTR inputEvent,
		out byte character)
	{
		character = 0;
		if (library.IsNull) return false;
		var memory = default(MuiNativeClassMemory);
		if (!MuiWindowInputEventRecordCodec.TryReadStructural(ref memory,
			inputEvent, out var eventRecord) ||
			eventRecord.Class != (byte)InputEventClass.RawKey) return false;

		var keyMap = AskKeyMapDefault(library);
		if (keyMap.IsNull) return false;

		var buffer = Exec.AllocMem(TranslationBufferSize,
			(Exec.MemoryFlags)MuiHeadlessLayout.AllocationFlags);
		if (buffer.IsNull)
		{
			return false;
		}

		var count = MapRawKey(library, inputEvent, buffer,
			unchecked((int)TranslationBufferSize), keyMap);
		var translated = count > 0 ? APTR.ReadUInt8(buffer, 0) : (byte)0;
		Exec.FreeMem(buffer, TranslationBufferSize);
		if (count <= 0) return false;
		character = translated;
		return true;
	}

	private static APTR AskKeyMapDefault(APTR library) =>
		APTR.FromPointer(AskKeyMapDefaultCall(Entry(library,
			AskKeyMapDefaultLvo), library));

	private static short MapRawKey(APTR library, APTR inputEvent,
		APTR buffer, int length, APTR keyMap) =>
		MapRawKeyCall(Entry(library, MapRawKeyLvo), library, inputEvent,
			buffer, length, keyMap);

	// Negative-vector arithmetic is confined to this library ABI boundary.
	private static APTR Entry(APTR library, short lvo) =>
		APTR.FromPointer(unchecked(library.Raw - (uint)-lvo));

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern uint AskKeyMapDefaultCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library);

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern short MapRawKeyCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.A0)] APTR inputEvent,
		[M68kRegister(M68kRegister.A1)] APTR buffer,
		[M68kRegister(M68kRegister.D1)] int length,
		[M68kRegister(M68kRegister.A2)] APTR keyMap);
}

internal interface IMuiNativeRawKeyMapCapability
{
	bool TryMapRawKey(APTR inputEvent, out byte character);
}

internal static class MuiNativeControlCharKeyPolicy
{
	private const ushort ShiftQualifiers =
		(ushort)InputEventQualifier.LeftShift |
		(ushort)InputEventQualifier.RightShift;

	internal static bool Matches(uint controlChar, byte translated,
		ushort qualifier)
	{
		if (controlChar < 32 || controlChar > byte.MaxValue || translated < 32)
			return false;
		var target = unchecked((byte)controlChar);
		if (LowerAscii(target) != LowerAscii(translated)) return false;
		return !RequiresShift(target) || (qualifier & ShiftQualifiers) != 0;
	}

	private static byte LowerAscii(byte value) =>
		value >= (byte)'A' && value <= (byte)'Z'
			? unchecked((byte)(value + ((byte)'a' - (byte)'A')))
			: value;

	private static bool RequiresShift(byte value) =>
		value >= (byte)'A' && value <= (byte)'Z';
}

internal static class MuiNativeAreaControlCharInput
{
	internal static bool TryPromote<TPlatform>(ref TPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR windowSidecar,
		APTR intuiMessage, APTR inputEvent, APTR handleEventPacket)
		where TPlatform : struct, IMuiGuestMemory,
			IMuiNativeRawKeyMapCapability
	{
		if (!MuiCommonHandleEventMessageCodec.TryRead(ref platform,
			handleEventPacket, out var packet) ||
			packet.MuiKey != (int)MuiNativeHandleEventKey.None ||
			!MuiIntuiMessageCodec.TryReadPointerRecord(ref platform,
				intuiMessage, out var source) ||
			source.Class != MuiIntuiMessageCodec.RawKeyClass ||
			MuiIntuiMessageCodec.IsRawKeyRelease(source.Code) ||
			!MuiWindowInputEventRecordCodec.TryReadStructural(ref platform,
				inputEvent, out var translatedInput) ||
			translatedInput.Class != (byte)InputEventClass.RawKey ||
			!MuiNativeObjectStateCore.TryGetAttribute(ref platform,
				windowSidecar, MuiWindowPublicCore.ActiveObject,
				out var activeObjectValue)) return false;

		var activeObject = APTR.FromPointer(activeObjectValue);
		if (activeObject.IsNull ||
			!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
				ownerRoot, activeObject, out var activeBinding) ||
			!MuiNativeObjectStateCore.TryGetAttribute(ref platform,
				activeBinding.Sidecar, MuiCommonControlCore.ControlChar,
				out var controlChar) ||
			!platform.TryMapRawKey(inputEvent, out var character) ||
			!MuiNativeControlCharKeyPolicy.Matches(controlChar, character,
				translatedInput.Qualifier)) return false;

		packet.MuiKey = (int)MuiNativeHandleEventKey.Press;
		return MuiCommonHandleEventMessageCodec.TryWrite(ref platform,
			handleEventPacket, packet);
	}
}
