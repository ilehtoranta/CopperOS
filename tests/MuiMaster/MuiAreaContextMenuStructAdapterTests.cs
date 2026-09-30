using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaContextMenuStructAdapterTests
{
	[Fact]
	public void AreaContextMenuStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiAreaContextMenuStateRecord
		{
			Magic = MuiAreaContextMenuStateRecord.Cookie,
			MenuStrip = APTR.FromPointer(0x1234),
			Trigger = APTR.FromPointer(0x5678),
			Generation = 7,
		};

		Assert.True(MuiAreaContextMenuStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaContextMenuStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaContextMenuStateField.Trigger,
			out var triggerAddress));
		Assert.Equal(0x3508u, triggerAddress.Raw);
		var cursor = new MuiAreaContextMenuStateFieldCursor
		{
			Record = address,
			Field = MuiAreaContextMenuStateField.Trigger,
		};
		Assert.True(MuiAreaContextMenuStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var typedTriggerAddress, out var typedTriggerSize));
		Assert.Equal(triggerAddress, typedTriggerAddress);
		Assert.Equal(MuiAreaContextMenuStateRecord.FieldSize, typedTriggerSize);
		Assert.True(MuiAreaContextMenuStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out var memoryTriggerAddress, out var memoryTriggerSize));
		Assert.Equal(typedTriggerAddress, memoryTriggerAddress);
		Assert.Equal(typedTriggerSize, memoryTriggerSize);
		Assert.True(MuiAreaContextMenuStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaContextMenuStateField.Trigger, 0xABCDEF01));
		Assert.True(MuiAreaContextMenuStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0xABCDEF01u, decoded.Trigger.Raw);
		Assert.False(MuiAreaContextMenuStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaContextMenuStateField)255, out _));
		Assert.False(MuiAreaContextMenuStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaContextMenuStateField.Magic, out _));
		cursor.Field = (MuiAreaContextMenuStateField)255;
		Assert.False(MuiAreaContextMenuStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
		cursor.Record = APTR.Null;
		cursor.Field = MuiAreaContextMenuStateField.Magic;
		Assert.False(MuiAreaContextMenuStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
		Assert.False(MuiAreaContextMenuStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void AreaContextMenuSequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x35C0);
		var value = new MuiAreaContextMenuStateRecord
		{
			Magic = MuiAreaContextMenuStateRecord.Cookie,
			MenuStrip = APTR.FromPointer(0xFEEDBEEF),
			Trigger = APTR.FromPointer(0xCAFEBABE),
			Generation = uint.MaxValue,
		};

		Assert.True(MuiAreaContextMenuStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaContextMenuStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.MenuStrip, decoded.MenuStrip);
		Assert.Equal(value.Trigger, decoded.Trigger);
		Assert.Equal(value.Generation, decoded.Generation);

		var crossingEnd = APTR.FromPointer(0x30FF1);
		Assert.False(MuiAreaContextMenuStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiAreaContextMenuStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}

	[Fact]
	public void AreaContextMenuFieldPathPreservesNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3640);
		var initial = new MuiAreaContextMenuStateRecord
		{
			Magic = 0x10203040u,
			MenuStrip = APTR.FromPointer(0x50607080u),
			Trigger = APTR.FromPointer(0x90A0B0C0u),
			Generation = 0xDDEEFF00u,
		};

		Assert.True(MuiAreaContextMenuStateRecordCodec.WriteRecord(ref platform,
			address, initial));
		Assert.True(MuiAreaContextMenuStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaContextMenuStateField.Trigger,
			0xF1020304u));
		Assert.True(MuiAreaContextMenuStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiAreaContextMenuStateField.MenuStrip,
			out var menuStrip));
		Assert.Equal(initial.MenuStrip.Raw, menuStrip);
		Assert.True(MuiAreaContextMenuStateRecordCodec.TryReadStructural(ref platform,
			address, out var updated));
		Assert.Equal(initial.Magic, updated.Magic);
		Assert.Equal(initial.MenuStrip, updated.MenuStrip);
		Assert.Equal(APTR.FromPointer(0xF1020304u), updated.Trigger);
		Assert.Equal(initial.Generation, updated.Generation);
		Assert.False(MuiAreaContextMenuStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address,
			unchecked((MuiAreaContextMenuStateField)255), 1));
	}
}
