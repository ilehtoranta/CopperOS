/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiFoundationTests
{
	[Fact]
	public void GuestResidentStateUsesExactFixedLayouts()
	{
		Assert.Equal(16, Unsafe.SizeOf<MuiMasterLibraryState>());
		Assert.Equal(48, Unsafe.SizeOf<MuiMasterPrivateRoot>());
		Assert.Equal(16, Unsafe.SizeOf<MuiErrorState>());
		Assert.Equal(24, Unsafe.SizeOf<MuiClassRegistryState>());
		Assert.Equal(24, Unsafe.SizeOf<MuiAllocationPolicy>());
		Assert.Equal((nint)8, Marshal.OffsetOf<MuiMasterPrivateRoot>(nameof(MuiMasterPrivateRoot.ErrorState)));
		Assert.Equal((nint)28, Marshal.OffsetOf<MuiMasterPrivateRoot>(nameof(MuiMasterPrivateRoot.RegistryGeneration)));
		Assert.Equal((nint)20, Marshal.OffsetOf<MuiClassRegistryState>(nameof(MuiClassRegistryState.Generation)));
	}

	[Fact]
	public void MasterPrivateRootCodecUsesNamedFieldsAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);
		var value = new MuiMasterPrivateRoot
		{
			ClassRegistry = 0x11111111,
			AllocationPolicy = 0x22222222,
			ErrorState = 0x33333333,
			ApplicationHead = 0x44444444,
			ExternalClassHead = 0x55555555,
			CallbackState = 0x66666666,
			LoaderState = 0x77777777,
			RegistryGeneration = 8,
			ActiveDispatchDepth = 9,
			ActiveCallbackDepth = 10,
			Flags = 11,
			Reserved = 12,
		};

		Assert.True(MuiMasterPrivateRootCodec.Write(ref platform, address, value));
		Assert.True(MuiMasterPrivateRootCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(value.ClassRegistry, decoded.ClassRegistry);
		Assert.Equal(value.RegistryGeneration, decoded.RegistryGeneration);
		Assert.Equal(value.Reserved, decoded.Reserved);

		var cursor = default(MuiMasterPrivateRootFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiMasterPrivateRootField.Flags;
		Assert.True(MuiMasterPrivateRootRecordMemoryCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + MuiMasterPrivateRoot.FlagsOffset,
			fieldAddress.Raw);
		Assert.True(MuiMasterPrivateRootFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiMasterPrivateRootField.Flags, 0xCAFEBABE));
		Assert.True(MuiMasterPrivateRootFieldCursorCodec.TryReadUInt32(
			ref platform, address, MuiMasterPrivateRootField.Flags, out var flags));
		Assert.Equal(0xCAFEBABEu, flags);

		cursor.Field = (MuiMasterPrivateRootField)255;
		Assert.False(MuiMasterPrivateRootRecordMemoryCodec.TryGetAddress(
			ref platform, cursor, out _));
		Assert.False(MuiMasterPrivateRootCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FF0), out _));
	}

	[Fact]
	public void GuestUlongStorageCodecUsesNamedValue()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2100);
		var expected = 0xDEADBEEFu;

		Assert.Equal(4, Unsafe.SizeOf<MuiGuestUlongStorage>());
		Assert.True(MuiGuestUlongStorageCodec.WriteValue(ref platform, address,
			expected));
		Assert.True(MuiGuestUlongStorageCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected, actual.Value);
		Assert.False(MuiGuestUlongStorageCodec.TryRead(ref platform, APTR.Null,
			out _));
		Assert.False(MuiGuestUlongStorageCodec.WriteValue(ref platform,
			APTR.FromPointer(0x30000), expected));
	}

	[Fact]
	public void GuestUlongStorageFieldCursorUsesNamedBoundary()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var storage = APTR.FromPointer(0x2200);
		var cursor = default(MuiGuestUlongStorageFieldCursor);
		cursor.Storage = storage;
		cursor.Field = MuiGuestUlongStorageField.Value;
		Assert.True(MuiGuestUlongStorageFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var address));
		Assert.Equal(storage.Raw, address.Raw);
		Assert.True(MuiGuestUlongStorageFieldCursorCodec.TryWrite(ref platform,
			storage, MuiGuestUlongStorageField.Value, 0x12345678));
		Assert.True(MuiGuestUlongStorageFieldCursorCodec.TryRead(ref platform,
			storage, MuiGuestUlongStorageField.Value, out var value));
		Assert.Equal(0x12345678u, value);
		cursor.Field = (MuiGuestUlongStorageField)255;
		Assert.False(MuiGuestUlongStorageFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
		cursor.Storage = APTR.FromPointer(0x30000);
		cursor.Field = MuiGuestUlongStorageField.Value;
		Assert.False(MuiGuestUlongStorageFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void GuestUlongStorageMemoryAdapterOwnsStructBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var storage = APTR.FromPointer(0x2300);

		Assert.True(MuiGuestUlongStorageMemoryCodec.TryGetAddress(ref platform,
			storage, MuiGuestUlongStorageField.Value, out var valueAddress));
		Assert.Equal(storage.Raw + MuiGuestUlongStorage.ValueOffset,
			valueAddress.Raw);
		Assert.True(MuiGuestUlongStorageMemoryCodec.TryWrite(ref platform,
			storage, MuiGuestUlongStorageField.Value, 0xCAFEBABEu));
		Assert.True(MuiGuestUlongStorageMemoryCodec.TryRead(ref platform,
			storage, MuiGuestUlongStorageField.Value, out var value));
		Assert.Equal(0xCAFEBABEu, value);
		Assert.False(MuiGuestUlongStorageMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x30FFF), MuiGuestUlongStorageField.Value, out _));
		Assert.False(MuiGuestUlongStorageMemoryCodec.TryGetAddress(ref platform,
			storage, (MuiGuestUlongStorageField)255, out _));
	}

	[Fact]
	public void EmptyRootAndErrorMutationAreDeterministic()
	{
		var root = MuiMasterState.CreateEmptyRoot();
		Assert.Equal(1u, root.RegistryGeneration);
		Assert.Equal(0u, root.ActiveDispatchDepth);
		var error = MuiMasterState.SetError(default, 7, 205, -42);
		Assert.Equal(7, error.MuiError);
		Assert.Equal(205, error.IoError);
		Assert.Equal(-42, error.FailingLvo);
		Assert.Equal(1u, error.Sequence);
	}

	[Fact]
	public void ResidentIdentityDoesNotAdvertiseMorphOsCompatibility()
	{
		Assert.Equal((ushort)0, MuiResidentMetadata.DevelopmentVersion);
		Assert.Equal((ushort)1, MuiResidentMetadata.DevelopmentRevision);
		Assert.Equal(-30, MuiResidentMetadata.FirstLvo);
		Assert.Equal(-756, MuiResidentMetadata.LastLvo);
		Assert.Equal(typeof(Amiga.CString), typeof(MuiResidentMetadata)
			.GetProperty(nameof(MuiResidentMetadata.DevelopmentName))!.PropertyType);
	}

	[Fact]
	public void RouterCoversExactlyThe27PublicMorphOs320Vectors()
	{
		var expected = new (int Lvo, MuiVectorId Id)[]
		{
			(-30, MuiVectorId.NewObjectA), (-36, MuiVectorId.DisposeObject),
			(-42, MuiVectorId.RequestA), (-48, MuiVectorId.AllocAslRequest),
			(-54, MuiVectorId.AslRequest), (-60, MuiVectorId.FreeAslRequest),
			(-66, MuiVectorId.Error), (-72, MuiVectorId.SetError),
			(-78, MuiVectorId.GetClass), (-84, MuiVectorId.FreeClass),
			(-90, MuiVectorId.RequestIDCMP), (-96, MuiVectorId.RejectIDCMP),
			(-102, MuiVectorId.Redraw), (-108, MuiVectorId.CreateCustomClass),
			(-114, MuiVectorId.DeleteCustomClass), (-120, MuiVectorId.MakeObjectA),
			(-126, MuiVectorId.Layout), (-156, MuiVectorId.ObtainPen),
			(-162, MuiVectorId.ReleasePen), (-168, MuiVectorId.AddClipping),
			(-174, MuiVectorId.RemoveClipping), (-180, MuiVectorId.AddClipRegion),
			(-186, MuiVectorId.RemoveClipRegion), (-192, MuiVectorId.BeginRefresh),
			(-198, MuiVectorId.EndRefresh), (-690, MuiVectorId.GetRGBColor),
			(-756, MuiVectorId.RequestObjectA),
		};
		Assert.Equal(27, expected.Length);
		foreach (var item in expected)
		{
			Assert.True(MuiVectorRouter.TryResolve(item.Lvo, out var actual));
			Assert.Equal(item.Id, actual);
		}
		foreach (var gap in new[] { -24, -132, -150, -204, -684, -696, -750, -762 })
			Assert.False(MuiVectorRouter.TryResolve(gap, out _));
	}
}
