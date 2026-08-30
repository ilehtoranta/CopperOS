using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaDragPolicyStructAdapterTests
{
	[Fact]
	public void AreaDragPolicyStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiAreaDragPolicyStateRecord
		{
			Magic = MuiAreaDragPolicyStateRecord.Cookie,
			Draggable = 1,
			Dropable = 0,
		};

		Assert.True(MuiAreaDragPolicyStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaDragPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaDragPolicyStateField.Dropable,
			out var dropableAddress));
		Assert.Equal(0x3508u, dropableAddress.Raw);
		Assert.True(MuiAreaDragPolicyStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaDragPolicyStateField.Draggable, 0));
		Assert.True(MuiAreaDragPolicyStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0u, decoded.Draggable);
		Assert.False(MuiAreaDragPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaDragPolicyStateField)255, out _));
		Assert.False(MuiAreaDragPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaDragPolicyStateField.Magic, out _));
		Assert.False(MuiAreaDragPolicyStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void AreaDragPolicySequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x35C0);
		var value = new MuiAreaDragPolicyStateRecord
		{
			Magic = MuiAreaDragPolicyStateRecord.Cookie,
			Draggable = uint.MaxValue,
			Dropable = 0x01020304u,
		};

		Assert.True(MuiAreaDragPolicyStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaDragPolicyStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Draggable, decoded.Draggable);
		Assert.Equal(value.Dropable, decoded.Dropable);

		var crossingEnd = APTR.FromPointer(0x30FF5);
		Assert.False(MuiAreaDragPolicyStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiAreaDragPolicyStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}
}
