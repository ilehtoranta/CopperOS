using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiGadgetGadgetStructAdapterTests
{
	[Fact]
	public void GadgetGadgetStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiGadgetGadgetStateRecord
		{
			Magic = MuiGadgetGadgetStateRecord.Cookie,
			Gadget = APTR.Null,
		};

		Assert.True(MuiGadgetGadgetStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiGadgetGadgetStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiGadgetGadgetStateField.Gadget,
			out var gadgetAddress));
		Assert.Equal(0x3504u, gadgetAddress.Raw);
		var gadgetCursor = new MuiGadgetGadgetStateFieldCursor
		{
			Record = address,
			Field = MuiGadgetGadgetStateField.Gadget,
		};
		Assert.True(MuiGadgetGadgetStateFieldCursorCodec.TryGetAddress(
			ref platform, gadgetCursor, out var cursorGadgetAddress,
			out var fieldSize));
		Assert.Equal(gadgetAddress, cursorGadgetAddress);
		Assert.Equal(MuiGadgetGadgetStateRecord.FieldSize, fieldSize);
		Assert.True(MuiGadgetGadgetStateRecordMemoryCodec.TryGetAddress(
			ref platform, gadgetCursor, out var memoryCursorAddress,
			out var memoryFieldSize));
		Assert.Equal(cursorGadgetAddress, memoryCursorAddress);
		Assert.Equal(fieldSize, memoryFieldSize);
		Assert.True(MuiGadgetGadgetStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiGadgetGadgetStateField.Gadget,
			0x12345678));
		Assert.True(MuiGadgetGadgetStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(0x12345678u, decoded.Gadget.Raw);
		Assert.False(MuiGadgetGadgetStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FF9),
			MuiGadgetGadgetStateField.Magic, out _));
		Assert.False(MuiGadgetGadgetStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiGadgetGadgetStateField.Magic, out _));
		gadgetCursor.Record = APTR.Null;
		Assert.False(MuiGadgetGadgetStateFieldCursorCodec.TryGetAddress(
			ref platform, gadgetCursor, out _, out _));
	}

	[Fact]
	public void GadgetGadgetSequentialRecordPreservesPointerAndRejectsTruncation()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3600);
		var value = new MuiGadgetGadgetStateRecord
		{
			Magic = MuiGadgetGadgetStateRecord.Cookie,
			Gadget = APTR.FromPointer(0x1F00),
		};
		Assert.True(MuiGadgetGadgetStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiGadgetGadgetStateRecordCodec.TryReadRecord(ref platform,
			address, out var actual));
		Assert.Equal(value.Magic, actual.Magic);
		Assert.Equal(value.Gadget, actual.Gadget);
		Assert.False(MuiGadgetGadgetStateRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x30FFC), out _));
	}
}
