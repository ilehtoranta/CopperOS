using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster.NativeRoot;

// Guest-resident fixture control for an allocator callback that attaches the
// same native object while the outer operation allocates its sidecar.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeAllocationReentryRecord
{
	internal const uint Address = 0x00036148;
	internal const uint Size = 28;
	internal const uint Cookie = 0x414C5231;
	internal const uint RunAndSucceed = 1;
	internal const uint RunAndRefuseOuter = 2;
	internal uint Magic;
	internal uint Mode;
	internal APTR State;
	internal APTR Class;
	internal APTR NativeObject;
	internal APTR NestedObject;
	internal uint NestedOwnership;
}

// Native-fixture-only in-flight control. The callback keeps this value on the
// platform receiver while it is inside Allocate; the guest diagnostic record
// is synchronized only after the re-entrant call has returned.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeAllocationReentryControl
{
	internal const uint Address = 0x00036180;
	internal const uint Size = 24;
	internal uint Mode;
	internal APTR State;
	internal APTR Class;
	internal APTR NativeObject;
	internal APTR NestedObject;
	internal uint NestedOwnership;
}

// This fixture control is intentionally a direct native wire codec. It is
// separate from the guest diagnostic record so the recursive allocator does
// not re-enter a generic platform/ref cursor while its receiver is active.
internal static class MuiNativeAllocationReentryControlCodec
{
	internal static MuiNativeAllocationReentryControl Read()
	{
		var address = APTR.FromPointer(MuiNativeAllocationReentryControl.Address);
		var value = default(MuiNativeAllocationReentryControl);
		value.Mode = APTR.ReadUInt32(address, 0);
		value.State = APTR.FromPointer(APTR.ReadUInt32(address, 4));
		value.Class = APTR.FromPointer(APTR.ReadUInt32(address, 8));
		value.NativeObject = APTR.FromPointer(APTR.ReadUInt32(address, 12));
		value.NestedObject = APTR.FromPointer(APTR.ReadUInt32(address, 16));
		value.NestedOwnership = APTR.ReadUInt32(address, 20);
		return value;
	}

	internal static void Write(MuiNativeAllocationReentryControl value)
	{
		var address = APTR.FromPointer(MuiNativeAllocationReentryControl.Address);
		APTR.WriteUInt32(address, 0, value.Mode);
		APTR.WriteUInt32(address, 4, value.State.Raw);
		APTR.WriteUInt32(address, 8, value.Class.Raw);
		APTR.WriteUInt32(address, 12, value.NativeObject.Raw);
		APTR.WriteUInt32(address, 16, value.NestedObject.Raw);
		APTR.WriteUInt32(address, 20, value.NestedOwnership);
	}
}

internal static class MuiNativeAllocationReentryProjection
{
	internal static MuiNativeAllocationReentryRecord ToRecord(
		MuiNativeAllocationReentryControl control)
	{
		var value = default(MuiNativeAllocationReentryRecord);
		value.Magic = MuiNativeAllocationReentryRecord.Cookie;
		value.Mode = control.Mode;
		value.State = control.State;
		value.Class = control.Class;
		value.NativeObject = control.NativeObject;
		value.NestedObject = control.NestedObject;
		value.NestedOwnership = control.NestedOwnership;
		return value;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeAllocationReentryStatusRecord
{
	internal const uint Address = 0x000361A0;
	internal uint Result;
}

internal static class MuiNativeAllocationReentryStatusCodec
{
	internal static uint Read() => APTR.ReadUInt32(
		APTR.FromPointer(MuiNativeAllocationReentryStatusRecord.Address), 0);

	internal static void Write(uint result) => APTR.WriteUInt32(
		APTR.FromPointer(MuiNativeAllocationReentryStatusRecord.Address), 0,
		result);
}

internal static class MuiNativeAllocationReentryCodec
{
	internal static bool TryRead(ref MuiNativeHeadlessPlatform platform,
		out MuiNativeAllocationReentryRecord value)
	{
		value = default;
		var address = APTR.FromPointer(MuiNativeAllocationReentryRecord.Address);
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiNativeAllocationReentryRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor, out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor, out value.Mode) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor, out var state) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor, out var cls) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor, out var native) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor, out var nested) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor, out value.NestedOwnership)) return false;
		value.State = APTR.FromPointer(state);
		value.Class = APTR.FromPointer(cls);
		value.NativeObject = APTR.FromPointer(native);
		value.NestedObject = APTR.FromPointer(nested);
		return MuiGuestStructCursor.IsComplete(cursor) &&
			value.Magic == MuiNativeAllocationReentryRecord.Cookie;
	}

	internal static bool Write(ref MuiNativeHeadlessPlatform platform,
		MuiNativeAllocationReentryRecord value)
	{
		var address = APTR.FromPointer(MuiNativeAllocationReentryRecord.Address);
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiNativeAllocationReentryRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, value.Mode) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, value.State.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, value.Class.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, value.NativeObject.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, value.NestedObject.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, value.NestedOwnership) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}
}
