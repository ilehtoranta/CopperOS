using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiSleepStructAdapterTests
{
	[Fact]
	public void SleepStateFieldAccessUsesCompleteNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3600);
		var original = new MuiSleepStateRecord
		{
			Magic = MuiSleepStateRecord.Cookie,
			Depth = 2,
			SavedDisabled = 1,
			Request = 2,
		};

		Assert.True(MuiSleepStateRecordCodec.WriteRecord(ref platform, address,
			original));
		Assert.True(MuiSleepStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiSleepStateField.Depth, out var depth));
		Assert.Equal(original.Depth, depth);
		Assert.True(MuiSleepStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiSleepStateField.Request, 7));
		Assert.True(MuiSleepStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiSleepStateField.Depth, 7));
		Assert.True(MuiSleepStateRecordCodec.TryReadStructural(ref platform,
			address, out var updated));
		Assert.Equal(original.Magic, updated.Magic);
		Assert.Equal(7u, updated.Depth);
		Assert.Equal(original.SavedDisabled, updated.SavedDisabled);
		Assert.Equal(7u, updated.Request);

		// Field access remains structural so diagnostics can inspect malformed
		// storage; the semantic codec still rejects it for live consumers.
		Assert.True(MuiSleepStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiSleepStateField.SavedDisabled, 2));
		Assert.True(MuiSleepStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiSleepStateField.SavedDisabled, out var malformed));
		Assert.Equal(2u, malformed);
		Assert.False(MuiSleepStateRecordCodec.TryRead(ref platform, address,
			out _));
		Assert.False(MuiSleepStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, (MuiSleepStateField)255, out _));
		Assert.False(MuiSleepStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			APTR.FromPointer(0x30FF1), MuiSleepStateField.Depth, 1));
	}

	[Fact]
	public void SleepStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiSleepStateRecord
		{
			Magic = MuiSleepStateRecord.Cookie,
			Depth = 3,
			SavedDisabled = 1,
			Request = 3,
		};

		Assert.True(MuiSleepStateRecordCodec.Write(ref platform, address, value));
		Assert.True(MuiSleepStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiSleepStateField.SavedDisabled, out var disabledAddress));
		Assert.Equal(0x3508u, disabledAddress.Raw);
		Assert.True(MuiSleepStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiSleepStateField.Request, out var request));
		Assert.Equal(3u, request);
		Assert.True(MuiSleepStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiSleepStateField.Depth, 4));
		Assert.True(MuiSleepStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiSleepStateField.Request, 4));
		Assert.True(MuiSleepStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(4u, decoded.Depth);
		Assert.Equal(4u, decoded.Request);
		Assert.False(MuiSleepStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x30FF1), MuiSleepStateField.Magic, out _));
		Assert.False(MuiSleepStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiSleepStateField.Magic, out _));
	}

	[Fact]
	public void SleepSequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x35C0);
		var value = new MuiSleepStateRecord
		{
			Magic = MuiSleepStateRecord.Cookie,
			Depth = uint.MaxValue,
			SavedDisabled = 0x01020304u,
			Request = 0xAABBCCDDu,
		};

		Assert.True(MuiSleepStateRecordCodec.WriteRecord(ref platform, address,
			value));
		Assert.True(MuiSleepStateRecordCodec.TryReadRecord(ref platform, address,
			out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Depth, decoded.Depth);
		Assert.Equal(value.SavedDisabled, decoded.SavedDisabled);
		Assert.Equal(value.Request, decoded.Request);

		var crossingEnd = APTR.FromPointer(0x30FF1);
		Assert.False(MuiSleepStateRecordCodec.WriteRecord(ref platform, crossingEnd,
			value));
		Assert.False(MuiSleepStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}
}
