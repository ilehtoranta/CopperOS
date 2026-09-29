using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStoreFieldCursorStructAdapterTests
{
	[Fact]
	public void StorePoolAndIterationFieldsUseNamedCursorRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var poolAddress = APTR.FromPointer(0x2400);
		var pool = new MuiStorePoolStateRecord
		{
			Pool = APTR.FromPointer(0x2800),
			Policy = 1,
			OwnsPool = 1,
			Magic = MuiStorePoolStateRecord.MagicValue,
		};
		Assert.True(MuiStorePoolStateCodec.WriteRecord(ref platform, poolAddress,
			pool));
		var poolCursor = new MuiStorePoolStateFieldCursor
		{
			Record = poolAddress,
			Field = MuiStorePoolStateField.Magic,
		};
		Assert.True(MuiStorePoolStateCodec.TryGetAddress(ref platform, poolCursor,
			out var poolField, out var poolFieldSize));
		Assert.Equal(0x240Cu, poolField.Raw);
		Assert.Equal(MuiStorePoolStateRecord.FieldSize, poolFieldSize);
		Assert.True(MuiStorePoolStateCodec.TryWriteUInt32(ref platform, poolAddress,
			MuiStorePoolStateField.Policy, 2));
		Assert.True(MuiStorePoolStateCodec.TryReadUInt32(ref platform, poolAddress,
			MuiStorePoolStateField.Pool, out var poolRaw));
		Assert.Equal(pool.Pool.Raw, poolRaw);

		var iterationAddress = APTR.FromPointer(0x2440);
		var iteration = new MuiStoreIterationStateRecord
		{
			Next = APTR.FromPointer(0x2900),
			Counter = APTR.FromPointer(0x2940),
			Current = APTR.FromPointer(0x2980),
			NextRecord = APTR.FromPointer(0x29C0),
			Kind = (uint)MuiStorePolicyKind.Objectmap,
			Magic = MuiStoreIterationStateRecord.MagicValue,
		};
		Assert.True(MuiStoreIterationStateCodec.WriteRecord(ref platform,
			iterationAddress, iteration));
		var iterationCursor = new MuiStoreIterationStateFieldCursor
		{
			Record = iterationAddress,
			Field = MuiStoreIterationStateField.NextRecord,
		};
		Assert.True(MuiStoreIterationStateCodec.TryGetAddress(ref platform,
			iterationCursor, out var iterationField, out var iterationFieldSize));
		Assert.Equal(0x244Cu, iterationField.Raw);
		Assert.Equal(MuiStoreIterationStateRecord.FieldSize, iterationFieldSize);
		Assert.True(MuiStoreIterationStateCodec.TryReadUInt32(ref platform,
			iterationAddress, MuiStoreIterationStateField.Kind, out var kind));
		Assert.Equal(iteration.Kind, kind);
	}

	[Fact]
	public void StoreFieldCursorsRejectUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		Assert.False(MuiStorePoolStateCodec.TryGetAddress(ref platform,
			new MuiStorePoolStateFieldCursor
			{
				Record = APTR.FromPointer(0x30FFC),
				Field = MuiStorePoolStateField.Magic,
			}, out _, out _));
		Assert.False(MuiStorePoolStateCodec.TryGetAddress(ref platform,
			new MuiStorePoolStateFieldCursor
			{
				Record = APTR.FromPointer(0x2400),
				Field = (MuiStorePoolStateField)255,
			}, out _, out _));
		Assert.False(MuiStoreIterationStateCodec.TryGetAddress(ref platform,
			new MuiStoreIterationStateFieldCursor
			{
				Record = APTR.Null,
				Field = MuiStoreIterationStateField.Magic,
			}, out _, out _));
		Assert.False(MuiStoreIterationStateCodec.TryGetAddress(ref platform,
			new MuiStoreIterationStateFieldCursor
			{
				Record = APTR.FromPointer(0x2400),
				Field = (MuiStoreIterationStateField)255,
			}, out _, out _));
	}
}
