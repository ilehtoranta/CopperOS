using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationObjectStructAdapterTests
{
	[Fact]
	public void ApplicationObjectStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiApplicationObjectStateRecord
		{
			Magic = MuiApplicationObjectStateRecord.Cookie,
			DiskObject = APTR.Null,
			DropObject = APTR.Null,
			Menustrip = APTR.Null,
		};

		Assert.True(MuiApplicationObjectStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationObjectStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationObjectStateField.Menustrip,
			out var menustripAddress));
		Assert.Equal(0x350Cu, menustripAddress.Raw);
		Assert.True(MuiApplicationObjectStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationObjectStateField.DropObject,
			0x12345678));
		Assert.True(MuiApplicationObjectStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(0x12345678u, decoded.DropObject.Raw);
		Assert.False(MuiApplicationObjectStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FF1),
			MuiApplicationObjectStateField.Magic, out _));
		Assert.False(MuiApplicationObjectStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiApplicationObjectStateField.Magic, out _));
	}

	[Fact]
	public void ApplicationObjectSequentialRecordPreservesPointersAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x35C0);
		var value = new MuiApplicationObjectStateRecord
		{
			Magic = MuiApplicationObjectStateRecord.Cookie,
			DiskObject = APTR.FromPointer(uint.MaxValue),
			DropObject = APTR.FromPointer(0x01020304u),
			Menustrip = APTR.FromPointer(0xAABBCCDDu),
		};

		Assert.True(MuiApplicationObjectStateRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiApplicationObjectStateRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.DiskObject, decoded.DiskObject);
		Assert.Equal(value.DropObject, decoded.DropObject);
		Assert.Equal(value.Menustrip, decoded.Menustrip);

		var crossingEnd = APTR.FromPointer(0x30FF1);
		Assert.False(MuiApplicationObjectStateRecordCodec.WriteRecord(
			ref platform, crossingEnd, value));
		Assert.False(MuiApplicationObjectStateRecordCodec.TryReadRecord(
			ref platform, crossingEnd, out _));
	}

	[Fact]
	public void ApplicationObjectFieldPathPreservesNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3640);
		var initial = new MuiApplicationObjectStateRecord
		{
			Magic = 0x10203040u,
			DiskObject = APTR.FromPointer(0x50607080u),
			DropObject = APTR.FromPointer(0x90A0B0C0u),
			Menustrip = APTR.FromPointer(0x01020304u),
		};

		Assert.True(MuiApplicationObjectStateRecordCodec.WriteRecord(ref platform,
			address, initial));
		Assert.True(MuiApplicationObjectStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationObjectStateField.DropObject,
			0xF1020304u));
		Assert.True(MuiApplicationObjectStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiApplicationObjectStateField.DiskObject,
			out var disk));
		Assert.Equal(initial.DiskObject.Raw, disk);
		Assert.True(MuiApplicationObjectStateRecordCodec.TryReadStructural(
			ref platform, address, out var updated));
		Assert.Equal(initial.Magic, updated.Magic);
		Assert.Equal(initial.DiskObject, updated.DiskObject);
		Assert.Equal(APTR.FromPointer(0xF1020304u), updated.DropObject);
		Assert.Equal(initial.Menustrip, updated.Menustrip);
		Assert.False(MuiApplicationObjectStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address,
			unchecked((MuiApplicationObjectStateField)255), 1));
	}
}
