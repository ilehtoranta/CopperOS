using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiGadgetAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void GadgetRecordsRoundTripThroughNamedStructs()
	{
		var platform = CreatePlatform();
		var gadgetAddress = APTR.FromPointer(0x1500);
		var interactionAddress = APTR.FromPointer(0x1520);
		var gadget = new MuiGadgetGadgetStateRecord
		{
			Magic = MuiGadgetGadgetStateRecord.Cookie,
			Gadget = APTR.FromPointer(0x1800),
		};
		var interaction = new MuiGadgetInteractionStateRecord
		{
			Magic = MuiGadgetInteractionStateRecord.Cookie,
			InputMode = 1,
			Selected = 1,
			Pressed = 0,
			ShowSelState = 1,
		};
		Assert.True(MuiGadgetGadgetStateRecordCodec.Write(ref platform,
			gadgetAddress, gadget));
		Assert.True(MuiGadgetInteractionStateRecordCodec.Write(ref platform,
			interactionAddress, interaction));
		Assert.True(MuiGadgetGadgetStateRecordCodec.TryRead(ref platform,
			gadgetAddress, out var gadgetRead));
		Assert.True(MuiGadgetInteractionStateRecordCodec.TryRead(ref platform,
			interactionAddress, out var interactionRead));
		Assert.Equal(gadget.Gadget, gadgetRead.Gadget);
		Assert.Equal(interaction.InputMode, interactionRead.InputMode);
		Assert.Equal(interaction.Selected, interactionRead.Selected);
		Assert.Equal(interaction.Pressed, interactionRead.Pressed);
		Assert.Equal(interaction.ShowSelState, interactionRead.ShowSelState);
	}

	[Fact]
	public void GadgetGadgetRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1BC0);
		var value = new MuiGadgetGadgetStateRecord
		{
			Magic = MuiGadgetGadgetStateRecord.Cookie,
			Gadget = APTR.FromPointer(0x1800),
		};

		Assert.True(MuiGadgetGadgetStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiGadgetGadgetStateRecordCodec.TryReadStructural(
			ref platform, address, out var structural));
		Assert.Equal(value.Magic, structural.Magic);
		Assert.Equal(value.Gadget, structural.Gadget);
		Assert.True(MuiGadgetGadgetStateRecordCodec.TryRead(ref platform, address,
			out _));
		Assert.True(MuiGadgetGadgetStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiGadgetGadgetStateField.Gadget,
			out var gadgetField));
		Assert.Equal(address.Raw + MuiGadgetGadgetStateRecord.GadgetOffset,
			gadgetField.Raw);
		Assert.True(MuiGadgetGadgetStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiGadgetGadgetStateField.Gadget,
			out var gadgetRaw));
		Assert.Equal(value.Gadget.Raw, gadgetRaw);
		Assert.False(MuiGadgetGadgetStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiGadgetGadgetStateField)255, out _));
		Assert.False(MuiGadgetGadgetStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiGadgetGadgetStateField.Magic, out _));
		Assert.False(MuiGadgetGadgetStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void GadgetInteractionRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1BE0);
		var value = new MuiGadgetInteractionStateRecord
		{
			Magic = MuiGadgetInteractionStateRecord.Cookie,
			InputMode = 3,
			Selected = 1,
			Pressed = 0,
			ShowSelState = 1,
		};

		Assert.True(MuiGadgetInteractionStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiGadgetInteractionStateRecordCodec.TryReadStructural(
			ref platform, address, out var structural));
		Assert.Equal(value.Magic, structural.Magic);
		Assert.Equal(value.InputMode, structural.InputMode);
		Assert.Equal(value.Selected, structural.Selected);
		Assert.Equal(value.Pressed, structural.Pressed);
		Assert.Equal(value.ShowSelState, structural.ShowSelState);
		Assert.True(MuiGadgetInteractionStateRecordCodec.TryRead(ref platform,
			address, out _));
		Assert.True(MuiGadgetInteractionStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiGadgetInteractionStateField.Pressed, 1));
		Assert.True(MuiGadgetInteractionStateRecordCodec.TryReadStructural(
			ref platform, address, out var typedDecoded));
		Assert.Equal(value.Magic, typedDecoded.Magic);
		Assert.Equal(value.InputMode, typedDecoded.InputMode);
		Assert.Equal(1u, typedDecoded.Pressed);
		Assert.Equal(value.ShowSelState, typedDecoded.ShowSelState);
		Assert.True(MuiGadgetInteractionStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, 16u, out var showSelField));
		Assert.Equal(address.Raw + 16, showSelField.Raw);
		Assert.True(MuiGadgetInteractionStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4u, out var inputMode));
		Assert.Equal(value.InputMode, inputMode);
		Assert.False(MuiGadgetInteractionStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiGadgetInteractionStateRecord.Size, out _));
		Assert.False(MuiGadgetInteractionStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, 0u, out _));
		Assert.False(MuiGadgetInteractionStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void GadgetInteractionSequentialRecordPreservesFieldsAndRejectsTruncation()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			State);
		var address = APTR.FromPointer(0x3600);
		var value = new MuiGadgetInteractionStateRecord
		{
			Magic = MuiGadgetInteractionStateRecord.Cookie,
			InputMode = 3,
			Selected = 1,
			Pressed = 0,
			ShowSelState = 1,
		};
		Assert.True(MuiGadgetInteractionStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiGadgetInteractionStateRecordCodec.TryReadRecord(ref platform,
			address, out var actual));
		Assert.Equal(value.Magic, actual.Magic);
		Assert.Equal(value.InputMode, actual.InputMode);
		Assert.Equal(value.Selected, actual.Selected);
		Assert.Equal(value.Pressed, actual.Pressed);
		Assert.Equal(value.ShowSelState, actual.ShowSelState);
		Assert.False(MuiGadgetInteractionStateRecordCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x30FFC), out _));
	}

	[Fact]
	public void MalformedGadgetMagicRemainsStructuralButFailsClosed()
	{
		var platform = CreatePlatform();
		var gadgetAddress = APTR.FromPointer(0x1600);
		var interactionAddress = APTR.FromPointer(0x1620);
		Assert.True(MuiGadgetGadgetStateRecordCodec.Write(ref platform,
			gadgetAddress, new MuiGadgetGadgetStateRecord
			{
				Magic = MuiGadgetGadgetStateRecord.Cookie,
				Gadget = APTR.Null,
			}));
		Assert.True(MuiGadgetInteractionStateRecordCodec.Write(ref platform,
			interactionAddress, new MuiGadgetInteractionStateRecord
			{
				Magic = MuiGadgetInteractionStateRecord.Cookie,
				InputMode = 0,
				Selected = 0,
				Pressed = 0,
				ShowSelState = 0,
			}));
		Assert.True(MuiGadgetGadgetStateFieldCursorCodec.TryWriteUInt32(ref platform,
			gadgetAddress, MuiGadgetGadgetStateField.Magic, 0));
		Assert.True(MuiGadgetInteractionStateFieldCursorCodec.TryWriteUInt32(
			ref platform, interactionAddress, MuiGadgetInteractionStateField.Magic,
			0));
		Assert.True(MuiGadgetGadgetStateRecordCodec.TryReadStructural(ref platform,
			gadgetAddress, out var gadget));
		Assert.True(MuiGadgetInteractionStateRecordCodec.TryReadStructural(
			ref platform, interactionAddress, out var interaction));
		Assert.Equal(0u, gadget.Magic);
		Assert.Equal(0u, interaction.Magic);
		Assert.False(MuiGadgetGadgetStateRecordCodec.TryRead(ref platform,
			gadgetAddress, out _));
		Assert.False(MuiGadgetInteractionStateRecordCodec.TryRead(ref platform,
			interactionAddress, out _));
		Assert.False(MuiGadgetGadgetStateAdmission.Validate(ref platform, gadget));
		Assert.False(MuiGadgetInteractionStateAdmission.Validate(interaction));
	}

	private static MuiHeadlessTestPlatform CreatePlatform() =>
		new(0x1000, 0x20000, 0x4000, State);
}
