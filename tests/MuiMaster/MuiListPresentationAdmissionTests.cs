using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListPresentationAdmissionTests
{
	[Fact]
	public void ListPresentationPolicyRecordRoundTripsThroughNamedStruct()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListPresentationPolicyState
		{
			Magic = MuiListCore.MuiListPresentationPolicyState.Cookie,
			Editable = 1,
			Quiet = 1,
			AdjustHeight = 1,
			AdjustWidth = 0,
			Stripes = 1,
			ShowDropMarks = 1,
			DragSortable = 1,
			DragType = 1,
			AutoVisible = 1,
			AutoLineHeight = 1,
			MinLineHeight = 8,
		};
		Assert.True(MuiListCore.MuiListPresentationPolicyStateCodec.Write(
			ref platform, address, value));
		Assert.True(MuiListCore.MuiListPresentationPolicyStateCodec.TryRead(
			ref platform, address, out var read));
		Assert.Equal(value.Editable, read.Editable);
		Assert.Equal(value.Stripes, read.Stripes);
		Assert.Equal(value.DragType, read.DragType);
		Assert.Equal(value.MinLineHeight, read.MinLineHeight);
	}

	[Fact]
	public void MalformedListPresentationMagicRemainsStructuralButFailsClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3540);
		Assert.True(MuiListCore.MuiListPresentationPolicyStateCodec.Write(
			ref platform, address, new MuiListCore.MuiListPresentationPolicyState
			{
				Magic = MuiListCore.MuiListPresentationPolicyState.Cookie,
				Editable = 1,
				Stripes = 1,
				DragType = 1,
				MinLineHeight = 8,
			}));
		Assert.True(MuiListCore.MuiListPresentationPolicyStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListPresentationPolicyStateField.Magic, 0));
		Assert.True(MuiListCore.MuiListPresentationPolicyStateCodec
			.TryReadStructural(ref platform, address, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.Equal(1u, structural.Stripes);
		Assert.Equal(1u, structural.DragType);
		Assert.False(MuiListCore.MuiListPresentationPolicyStateCodec.TryRead(
			ref platform, address, out _));
		Assert.True(MuiListCore.MuiListPresentationPolicyStateCodec.TryReadStorage(
			ref platform, address, out _));
	}

	[Fact]
	public void ListPresentationSequentialRecordPreservesFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3580);
		var value = new MuiListCore.MuiListPresentationPolicyState
		{
			Magic = MuiListCore.MuiListPresentationPolicyState.Cookie,
			Editable = 0x01020304u,
			Quiet = 0x11223344u,
			AdjustHeight = 0x55667788u,
			AdjustWidth = 0x99AABBCCu,
			Stripes = 0xDDEEFF00u,
			ShowDropMarks = 0x13579BDFu,
			DragSortable = 0x2468ACE0u,
			DragType = 0x80000001u,
			AutoVisible = 0xFFFFFFFFu,
			AutoLineHeight = 0x7FFFFFFFu,
			MinLineHeight = 0x40000000u,
		};

		Assert.True(MuiListCore.MuiListPresentationPolicyStateCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiListCore.MuiListPresentationPolicyStateCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Editable, decoded.Editable);
		Assert.Equal(value.Quiet, decoded.Quiet);
		Assert.Equal(value.AdjustHeight, decoded.AdjustHeight);
		Assert.Equal(value.AdjustWidth, decoded.AdjustWidth);
		Assert.Equal(value.Stripes, decoded.Stripes);
		Assert.Equal(value.ShowDropMarks, decoded.ShowDropMarks);
		Assert.Equal(value.DragSortable, decoded.DragSortable);
		Assert.Equal(value.DragType, decoded.DragType);
		Assert.Equal(value.AutoVisible, decoded.AutoVisible);
		Assert.Equal(value.AutoLineHeight, decoded.AutoLineHeight);
		Assert.Equal(value.MinLineHeight, decoded.MinLineHeight);

		Assert.False(MuiListCore.MuiListPresentationPolicyStateCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x30FD1), out _));
	}
}
