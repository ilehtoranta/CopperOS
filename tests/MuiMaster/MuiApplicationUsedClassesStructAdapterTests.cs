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

	[Fact]
	public void ApplicationUsedClassesSequentialRecordPreservesPointerAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x35C0);
		var value = new MuiApplicationUsedClassesStateRecord
		{
			Magic = MuiApplicationUsedClassesStateRecord.Cookie,
			Vector = APTR.FromPointer(uint.MaxValue),
		};

		Assert.True(MuiApplicationUsedClassesStateRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiApplicationUsedClassesStateRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Vector, decoded.Vector);

		var crossingEnd = APTR.FromPointer(0x30FFC);
		Assert.False(MuiApplicationUsedClassesStateRecordCodec.WriteRecord(
			ref platform, crossingEnd, value));
		Assert.False(MuiApplicationUsedClassesStateRecordCodec.TryReadRecord(
			ref platform, crossingEnd, out _));
	}

	[Fact]
	public void ApplicationUsedClassesVectorEntryUsesNamedRecordBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiApplicationUsedClassesVectorEntry
		{
			Name = APTR.FromPointer(0x3600),
		};

		Assert.True(MuiApplicationUsedClassesVectorEntryCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationUsedClassesVectorEntryMemoryCodec.TryGetAddress(
			ref platform, address,
			MuiApplicationUsedClassesVectorEntryField.Name, out var nameField));
		Assert.Equal(address, nameField);
		Assert.True(MuiApplicationUsedClassesVectorEntryMemoryCodec.TryReadUInt32(
			ref platform, address,
			MuiApplicationUsedClassesVectorEntryField.Name, out var rawName));
		Assert.Equal(0x3600u, rawName);
		Assert.False(MuiApplicationUsedClassesVectorEntryMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FFF),
			MuiApplicationUsedClassesVectorEntryField.Name, out _));
		Assert.False(MuiApplicationUsedClassesVectorEntryMemoryCodec.TryGetAddress(
			ref platform, APTR.Null,
			(MuiApplicationUsedClassesVectorEntryField)255, out _));
	}

	[Fact]
	public void ApplicationUsedClassesVectorMemoryAdapterOwnsEntryBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var vector = APTR.FromPointer(0x1900);
		var className = APTR.FromPointer(0x1A00);
		platform.WriteCString(className, "Listtree.mcc");
		platform.WriteUInt32(vector, 0, className.Raw);
		platform.WriteUInt32(vector, 4, 0);
		Assert.True(MuiApplicationUsedClassesVectorMemoryCodec.TryValidate(
			ref platform, vector));
		platform.WriteUInt32(vector, 4, 0x31000);
		Assert.False(MuiApplicationUsedClassesVectorMemoryCodec.TryValidate(
			ref platform, vector));
		platform.WriteUInt32(vector, 4, 0);
		Assert.True(MuiApplicationUsedClassesVectorMemoryCodec.TryGetEntry(
			ref platform, vector, 2, out var address));
		Assert.Equal(APTR.FromPointer(0x1908), address);
		Assert.False(MuiApplicationUsedClassesVectorMemoryCodec.TryGetEntry(
			ref platform, APTR.FromPointer(0x30FFE), 0, out _));
		Assert.False(MuiApplicationUsedClassesVectorMemoryCodec.TryGetEntry(
			ref platform, APTR.FromPointer(0xFFFFFFF0), 4, out _));
		Assert.False(MuiApplicationUsedClassesVectorMemoryCodec.TryGetEntry(
			ref platform, APTR.Null, 0, out _));
	}

	[Fact]
	public void ApplicationUsedClassesVectorCursorExchangesNamedEntries()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var cursor = new MuiApplicationUsedClassesVectorCursor
		{
			Base = APTR.FromPointer(0x1900),
			Index = 1,
		};
		var expected = new MuiApplicationUsedClassesVectorEntry
		{
			Name = APTR.FromPointer(0xFEDCBA98u),
		};

		Assert.True(MuiApplicationUsedClassesVectorCodec.TryWrite(ref platform,
			cursor, expected));
		Assert.True(MuiApplicationUsedClassesVectorCodec.TryRead(ref platform,
			cursor, out var actual));
		Assert.Equal(expected.Name.Raw, actual.Name.Raw);
		Assert.True(MuiApplicationUsedClassesVectorCodec.TryAdvance(ref cursor, 1));
		Assert.Equal(2u, cursor.Index);
		Assert.False(MuiApplicationUsedClassesVectorCodec.TryAdvance(ref cursor,
			MuiHeadlessLayout.MaximumTraversal));
		cursor.Index = MuiHeadlessLayout.MaximumTraversal;
		Assert.False(MuiApplicationUsedClassesVectorCodec.TryRead(ref platform,
			cursor, out _));
	}
}
