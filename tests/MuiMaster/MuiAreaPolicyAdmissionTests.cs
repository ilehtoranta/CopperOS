using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaPolicyAdmissionTests
{
	[Fact]
	public void AreaPolicyRecordsRoundTripThroughNamedStructs()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var controlAddress = APTR.FromPointer(0x1500);
		var cycleAddress = APTR.FromPointer(0x1520);
		var contextAddress = APTR.FromPointer(0x1540);
		var control = new MuiAreaControlCharStateRecord
		{
			Magic = MuiAreaControlCharStateRecord.Cookie,
			Character = 0x41,
			Generation = 3,
		};
		var cycle = new MuiAreaCycleChainStateRecord
		{
			Magic = MuiAreaCycleChainStateRecord.Cookie,
			Value = -2,
			Generation = 5,
		};
		var context = new MuiAreaContextMenuStateRecord
		{
			Magic = MuiAreaContextMenuStateRecord.Cookie,
			MenuStrip = APTR.FromPointer(0x1900),
			Trigger = APTR.FromPointer(0x1A00),
			Generation = 7,
		};

		Assert.True(MuiAreaControlCharStateRecordCodec.Write(ref platform,
			controlAddress, control));
		Assert.True(MuiAreaCycleChainStateRecordCodec.Write(ref platform,
			cycleAddress, cycle));
		Assert.True(MuiAreaContextMenuStateRecordCodec.Write(ref platform,
			contextAddress, context));
		Assert.True(MuiAreaControlCharStateRecordCodec.TryRead(ref platform,
			controlAddress, out var controlRead));
		Assert.True(MuiAreaCycleChainStateRecordCodec.TryRead(ref platform,
			cycleAddress, out var cycleRead));
		Assert.True(MuiAreaContextMenuStateRecordCodec.TryRead(ref platform,
			contextAddress, out var contextRead));
		Assert.Equal(control.Character, controlRead.Character);
		Assert.Equal(cycle.Value, cycleRead.Value);
		Assert.Equal(context.MenuStrip, contextRead.MenuStrip);
		Assert.Equal(context.Trigger, contextRead.Trigger);
	}

	[Fact]
	public void MalformedAreaPolicyMagicRemainsStructuralButFailsClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var controlAddress = APTR.FromPointer(0x1500);
		var cycleAddress = APTR.FromPointer(0x1520);
		var contextAddress = APTR.FromPointer(0x1540);
		Assert.True(MuiAreaControlCharStateRecordCodec.Write(ref platform,
			controlAddress, new MuiAreaControlCharStateRecord
			{
				Magic = MuiAreaControlCharStateRecord.Cookie,
				Character = 0x41,
				Generation = 1,
			}));
		Assert.True(MuiAreaCycleChainStateRecordCodec.Write(ref platform,
			cycleAddress, new MuiAreaCycleChainStateRecord
			{
				Magic = MuiAreaCycleChainStateRecord.Cookie,
				Value = -1,
				Generation = 1,
			}));
		Assert.True(MuiAreaContextMenuStateRecordCodec.Write(ref platform,
			contextAddress, new MuiAreaContextMenuStateRecord
			{
				Magic = MuiAreaContextMenuStateRecord.Cookie,
				Generation = 1,
			}));

		Assert.True(MuiAreaControlCharStateFieldCursorCodec.TryWriteUInt32(
			ref platform, controlAddress, MuiAreaControlCharStateField.Magic, 0));
		Assert.True(MuiAreaCycleChainStateFieldCursorCodec.TryWriteUInt32(
			ref platform, cycleAddress, MuiAreaCycleChainStateField.Magic, 0));
		Assert.True(MuiAreaContextMenuStateFieldCursorCodec.TryWriteUInt32(
			ref platform, contextAddress, MuiAreaContextMenuStateField.Magic, 0));

		Assert.True(MuiAreaControlCharStateRecordCodec.TryReadStructural(
			ref platform, controlAddress, out var control));
		Assert.True(MuiAreaCycleChainStateRecordCodec.TryReadStructural(ref platform,
			cycleAddress, out var cycle));
		Assert.True(MuiAreaContextMenuStateRecordCodec.TryReadStructural(
			ref platform, contextAddress, out var context));
		Assert.Equal(0u, control.Magic);
		Assert.Equal(0u, cycle.Magic);
		Assert.Equal(0u, context.Magic);
		Assert.False(MuiAreaControlCharStateRecordCodec.TryRead(ref platform,
			controlAddress, out _));
		Assert.False(MuiAreaCycleChainStateRecordCodec.TryRead(ref platform,
			cycleAddress, out _));
		Assert.False(MuiAreaContextMenuStateRecordCodec.TryRead(ref platform,
			contextAddress, out _));
	}
}
