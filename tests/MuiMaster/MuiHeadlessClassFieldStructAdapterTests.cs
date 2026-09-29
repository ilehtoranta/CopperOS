using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiHeadlessClassFieldStructAdapterTests
{
	[Fact]
	public void FieldWritesRoundTripThroughCompleteClassRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2A00);
		var initial = new MuiHeadlessClassRecord
		{
			Next = APTR.FromPointer(0x3000),
			Name = APTR.FromPointer(0x3040),
			Boopsi = APTR.FromPointer(0x3080),
			Super = APTR.FromPointer(0x30C0),
			InstanceSize = 96,
			Reserved = 0x55AA,
			Flags = 0x11223344,
			ObjectCount = 7,
		};
		Assert.True(MuiHeadlessClassCodec.WriteRecord(ref platform, address,
			initial));

		Assert.True(MuiHeadlessClassMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiHeadlessClassField.Flags, 0xAABBCCDD));
		Assert.True(MuiHeadlessClassMemoryCodec.TryWriteUInt16(ref platform,
			address, MuiHeadlessClassField.InstanceSize, 128));
		Assert.True(MuiHeadlessClassCodec.TryReadRecord(ref platform, address,
			out var decoded));

		Assert.Equal(initial.Next.Raw, decoded.Next.Raw);
		Assert.Equal(initial.Name.Raw, decoded.Name.Raw);
		Assert.Equal(initial.Boopsi.Raw, decoded.Boopsi.Raw);
		Assert.Equal(initial.Super.Raw, decoded.Super.Raw);
		Assert.Equal((ushort)128, decoded.InstanceSize);
		Assert.Equal(initial.Reserved, decoded.Reserved);
		Assert.Equal(0xAABBCCDDu, decoded.Flags);
		Assert.Equal(initial.ObjectCount, decoded.ObjectCount);
	}

	[Fact]
	public void UnsupportedWidthAndTruncatedRecordsFailClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2C00);
		Assert.False(MuiHeadlessClassMemoryCodec.TryReadUInt16(ref platform,
			address, MuiHeadlessClassField.Flags, out _));
		Assert.False(MuiHeadlessClassMemoryCodec.TryWriteUInt32(ref platform,
			APTR.FromPointer(0x20FF0), MuiHeadlessClassField.Flags, 1));
		Assert.False(MuiHeadlessClassMemoryCodec.TryReadUInt32(ref platform,
			address, (MuiHeadlessClassField)255, out _));
	}
}
