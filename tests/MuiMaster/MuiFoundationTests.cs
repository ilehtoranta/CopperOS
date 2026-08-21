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
