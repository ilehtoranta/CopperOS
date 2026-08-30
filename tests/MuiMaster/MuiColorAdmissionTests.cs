using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiColorAdmissionTests
{
	[Fact]
	public void ColorSpecialistStateRoundTripsThroughNamedStruct()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1500);
		var value = new MuiColorSpecialistState
		{
			Magic = MuiColorSpecialistState.Cookie,
			Class = (uint)MuiColorSpecialistClass.Pendisplay,
			Flags = 0,
			RenderInfo = APTR.Null,
			DrawState = APTR.Null,
			Pen = 0,
			SpecBlock = APTR.FromPointer(0x1600),
			RgbBlock = APTR.FromPointer(0x1640),
			Reference = APTR.Null,
			ModeID = 7,
			Alpha = uint.MaxValue,
			Entries = APTR.Null,
			Names = APTR.Null,
			NotifyAttribute = 1,
			NotifyValue = 2,
			NotifyCount = 3,
		};
		Assert.True(MuiColorSpecialistStateCodec.WriteRecord(ref platform, address,
			value));
		Assert.True(MuiColorSpecialistStateCodec.TryReadRecord(ref platform, address,
			out var read));
		Assert.Equal(value.Magic, read.Magic);
		Assert.Equal(value.Class, read.Class);
		Assert.Equal(value.Flags, read.Flags);
		Assert.Equal(value.SpecBlock, read.SpecBlock);
		Assert.Equal(value.RgbBlock, read.RgbBlock);
		Assert.Equal(value.ModeID, read.ModeID);
		Assert.Equal(value.NotifyCount, read.NotifyCount);
	}

	[Fact]
	public void MalformedColorMagicRemainsStructuralButFailsClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1700);
		var value = new MuiColorSpecialistState
		{
			Magic = MuiColorSpecialistState.Cookie,
			Class = (uint)MuiColorSpecialistClass.Pendisplay,
			SpecBlock = APTR.FromPointer(0x1800),
			RgbBlock = APTR.FromPointer(0x1840),
		};
		Assert.True(MuiColorSpecialistStateCodec.WriteRecord(ref platform, address,
			value));
		Assert.True(MuiColorRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiColorRecordKind.State, MuiColorRecordField.Magic, 0));
		Assert.True(MuiColorSpecialistStateCodec.TryReadRecord(ref platform,
			address, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiColorSpecialistStateCodec.TryRead(ref platform, address,
			out _));
		Assert.False(MuiColorSpecialistStateAdmission.Validate(ref platform,
			structural));
	}
}
