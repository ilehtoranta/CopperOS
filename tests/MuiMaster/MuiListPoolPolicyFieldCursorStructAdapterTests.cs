using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListPoolPolicyFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedPoolPolicyRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListPoolPolicyState
		{
			Magic = MuiListCore.MuiListPoolPolicyState.Cookie,
			Pool = APTR.FromPointer(0x1234),
			PuddleSize = 4096,
			ThresholdSize = 8192,
			UsesExternalPool = 1,
		};
		Assert.True(MuiListCore.MuiListPoolPolicyStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListCore.MuiListPoolPolicyFieldCursor
		{
			Address = address,
			Field = MuiListCore.MuiListPoolPolicyField.UsesExternalPool,
		};
		Assert.True(MuiListCore.MuiListPoolPolicyFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3510u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListPoolPolicyFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListPoolPolicyField.PuddleSize, 16384));
		Assert.True(MuiListCore.MuiListPoolPolicyFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListPoolPolicyField.Pool, out var pool));
		Assert.Equal(value.Pool.Raw, pool);
		Assert.True(MuiListCore.MuiListPoolPolicyStateCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Pool.Raw, decoded.Pool.Raw);
		Assert.Equal(16384u, decoded.PuddleSize);
		Assert.Equal(value.ThresholdSize, decoded.ThresholdSize);
		Assert.Equal(value.UsesExternalPool, decoded.UsesExternalPool);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListPoolPolicyFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListPoolPolicyFieldCursor
				{
					Address = address,
					Field = (MuiListCore.MuiListPoolPolicyField)255,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListPoolPolicyFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListPoolPolicyFieldCursor
				{
					Address = APTR.FromPointer(0x30FF9),
					Field = MuiListCore.MuiListPoolPolicyField.Magic,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListPoolPolicyMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListCore.MuiListPoolPolicyField.Magic,
			out _, out _));
	}
}
