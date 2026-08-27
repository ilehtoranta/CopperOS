using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationUsedClassesStructAdapterTests
{
	[Fact]
	public void ApplicationUsedClassesStateUsesDedicatedNamedStructAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiApplicationUsedClassesStateRecord
		{
			Magic = MuiApplicationUsedClassesStateRecord.Cookie,
			Vector = APTR.Null,
		};

		Assert.True(MuiApplicationUsedClassesStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationUsedClassesStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationUsedClassesStateField.Vector,
			out var vectorField));
		Assert.Equal(APTR.FromPointer(0x3504), vectorField);
		Assert.True(MuiApplicationUsedClassesStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationUsedClassesStateField.Vector,
			0x3600));
		Assert.True(MuiApplicationUsedClassesStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(APTR.FromPointer(0x3600), decoded.Vector);
		Assert.False(MuiApplicationUsedClassesStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FFC),
			MuiApplicationUsedClassesStateField.Magic, out _));
		Assert.False(MuiApplicationUsedClassesStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null,
			MuiApplicationUsedClassesStateField.Vector, out _));
	}
}
