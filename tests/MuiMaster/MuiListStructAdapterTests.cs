using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListStructAdapterTests
{
	[Fact]
	public void ListTestPosUsesDedicatedMixedWidthStructAdapter()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1800);
		var value = new MuiListTestPosResult
		{
			Entry = -3,
			Column = 2,
			Flags = MuiListTestPosResult.FlagAbove | MuiListTestPosResult.FlagRight,
			XOffset = -4,
			YOffset = 5,
		};
		Assert.True(MuiListTestPosResultCodec.Write(ref platform, address, value));
		Assert.True(MuiListTestPosResultMemoryCodec.TryGetAddress(ref platform,
			address, MuiListTestPosResultField.XOffset, out var xOffset) &&
			xOffset.Raw == 0x1808u);
		Assert.True(MuiListTestPosResultMemoryCodec.TryReadUInt32(ref platform,
			address, MuiListTestPosResultField.Entry, out var entry) &&
			entry == unchecked((uint)-3));
		Assert.True(MuiListTestPosResultMemoryCodec.TryWriteUInt16(ref platform,
			address, MuiListTestPosResultField.YOffset, unchecked((ushort)-6)));
		Assert.True(MuiListTestPosResultCodec.TryRead(ref platform, address,
			out var decoded) && decoded.YOffset == -6 && decoded.Flags == value.Flags);
		Assert.False(MuiListTestPosResultMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x51000), MuiListTestPosResultField.XOffset,
			out _));
		Assert.False(MuiListTestPosResultMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiListTestPosResultField.Entry, out _));
	}

	[Fact]
	public void ListScalarAndDisplayRowsUseDedicatedStructAdapters()
	{
		var platform = CreatePlatform();
		var scalarAddress = APTR.FromPointer(0x1820);
		var rowAddress = APTR.FromPointer(0x1830);
		Assert.True(MuiListScalarStorageCodec.Write(ref platform, scalarAddress,
			new MuiListScalarStorageRecord { Value = 0xFFFFFFFEu }));
		Assert.True(MuiListDisplayRowRecordCodec.Write(ref platform, rowAddress,
			new MuiListDisplayRowRecord { Row = -4 }));
		Assert.True(MuiListScalarStorageRecordMemoryCodec.TryGetAddress(ref platform,
			scalarAddress, out var scalarField) && scalarField.Raw == 0x1820u);
		Assert.True(MuiListDisplayRowRecordMemoryCodec.TryGetAddress(ref platform,
			rowAddress, out var rowField) && rowField.Raw == 0x1830u);
		Assert.True(MuiListScalarStorageRecordMemoryCodec.TryReadUInt32(ref platform,
			scalarAddress, out var scalar) && scalar == 0xFFFFFFFEu);
		Assert.True(MuiListDisplayRowRecordMemoryCodec.TryWriteUInt32(ref platform,
			rowAddress, 9));
		Assert.True(MuiListDisplayRowRecordCodec.TryRead(ref platform, rowAddress,
			out var row) && row.Row == 9);
		Assert.False(MuiListScalarStorageRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform() =>
		new(0x1000, 0x20000, 0x4000, APTR.FromPointer(0x1000));
}
