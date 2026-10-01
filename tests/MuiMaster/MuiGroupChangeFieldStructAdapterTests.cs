using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiGroupChangeFieldStructAdapterTests
{
	private static MuiHeadlessTestPlatform NewPlatform()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000,
			0x4000, state);
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, state));
		return platform;
	}

	[Fact]
	public void GroupChangePacketFieldsUseCompleteNamedRecords()
	{
		var platform = NewPlatform();
		var packet = APTR.FromPointer(0x1200);
		var message = new MuiGroupChangeMessage { MethodId = 0x12345678 };
		Assert.True(MuiGroupChangeMessageCodec.WriteRecord(ref platform, packet,
			message));
		Assert.True(MuiGroupChangeRecordMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiGroupChangeRecordKind.Message,
			MuiGroupChangeRecordField.MethodId, 0xF00DCAFE));
		Assert.True(MuiGroupChangeRecordMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiGroupChangeRecordKind.Message,
			MuiGroupChangeRecordField.MethodId, out var method));
		Assert.Equal(0xF00DCAFEu, method);

		Assert.True(MuiGroupExitChange2MessageCodec.WriteRecord(ref platform,
			packet, new MuiGroupExitChange2Message
			{
				MethodId = 0x11111111,
				Flags = 0x22222222,
			}));
		Assert.True(MuiGroupChangeRecordMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiGroupChangeRecordKind.ExitChange2,
			MuiGroupChangeRecordField.Flags, 0xA5A5A5A5));
		Assert.True(MuiGroupExitChange2MessageCodec.TryReadRecord(ref platform,
			packet, out var exit2));
		Assert.Equal(0x11111111u, exit2.MethodId);
		Assert.Equal(0xA5A5A5A5u, exit2.Flags);
	}

	[Fact]
	public void GroupChangeStateFieldsPreserveSiblingsAndRejectBadRanges()
	{
		var platform = NewPlatform();
		var stateAddress = APTR.FromPointer(0x1300);
		var state = new MuiGroupChangeState
		{
			Cookie = MuiGroupChangeState.Magic,
			Depth = 2,
			ExitFlags = 0xA5,
			ExitRequests = 1,
		};
		Assert.True(MuiGroupChangeStateCodec.WriteRecord(ref platform,
			stateAddress, state));
		Assert.True(MuiGroupChangeRecordMemoryCodec.TryWriteUInt32(ref platform,
			stateAddress, MuiGroupChangeRecordKind.State,
			MuiGroupChangeRecordField.Depth, 7));
		Assert.True(MuiGroupChangeStateCodec.TryReadRecord(ref platform,
			stateAddress, out var updated));
		Assert.Equal(MuiGroupChangeState.Magic, updated.Cookie);
		Assert.Equal(7u, updated.Depth);
		Assert.Equal(state.ExitFlags, updated.ExitFlags);
		Assert.Equal(state.ExitRequests, updated.ExitRequests);

		Assert.False(MuiGroupChangeRecordMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x20FFE), MuiGroupChangeRecordKind.Message,
			MuiGroupChangeRecordField.MethodId, out _));
		Assert.False(MuiGroupChangeRecordMemoryCodec.TryWriteUInt32(ref platform,
			stateAddress, MuiGroupChangeRecordKind.State,
			MuiGroupChangeRecordField.MethodId, 1));
		Assert.False(MuiGroupChangeRecordMemoryCodec.TryReadUInt32(ref platform,
			APTR.Null, MuiGroupChangeRecordKind.State,
			MuiGroupChangeRecordField.Depth, out _));
	}
}
