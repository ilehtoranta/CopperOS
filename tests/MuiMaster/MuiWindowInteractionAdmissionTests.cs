using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiWindowInteractionAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void WindowInteractionAdmissionRequiresCanonicalChainAndOwner()
	{
		var platform = CreateWindow(out var window, out var member);
		var vector = APTR.FromPointer(0x1200);
		platform.WriteUInt32(vector, 0, member.Raw);
		platform.WriteUInt32(vector, 4, 0);
		Assert.True(MuiApplicationWindowCore.SetCycleChain(ref platform, State,
			window, vector));
		Assert.True(MuiApplicationWindowCore.TryGetWindowInteractionState(
			ref platform, State, window, out var valid));
		Assert.True(MuiWindowInteractionStateAdmission.Validate(ref platform,
			valid));
		Assert.True(MuiWindowInteractionStateAdmission.ValidateLive(ref platform,
			State, window, valid));
		valid.SnapshotFlags = 2;
		Assert.False(MuiWindowInteractionStateAdmission.Validate(ref platform,
			valid));
		Assert.False(MuiWindowInteractionStateAdmission.ValidateLive(ref platform,
			State, window, valid));
		valid.SnapshotFlags = 0;
		Assert.False(MuiWindowInteractionStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedWindowInteractionMagicFailsClosedButRemainsStructural()
	{
		var platform = CreateWindow(out var window, out var member);
		var vector = APTR.FromPointer(0x1200);
		platform.WriteUInt32(vector, 0, member.Raw);
		platform.WriteUInt32(vector, 4, 0);
		Assert.True(MuiApplicationWindowCore.SetCycleChain(ref platform, State,
			window, vector));
		Assert.True(MuiApplicationWindowCore.TryGetWindowInteractionState(
			ref platform, State, window, out _));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, window,
			MuiApplicationWindowCore.WindowInteractionStateKey);
		Assert.True(MuiWindowInteractionStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiWindowInteractionStateField.Magic, 0));
		Assert.True(MuiWindowInteractionStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiWindowInteractionStateRecordCodec.TryRead(ref platform,
			block, out _));
		Assert.False(MuiApplicationWindowCore.TryGetWindowInteractionState(
			ref platform, State, window, out _));
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, window,
			MuiApplicationWindowCore.WindowInteractionStateKey));
	}

	[Fact]
	public void WindowInteractionRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var address = APTR.FromPointer(0x1640);
		var value = new MuiWindowInteractionStateRecord
		{
			Magic = MuiWindowInteractionStateRecord.Cookie,
			SnapshotFlags = 1,
			SnapshotRequests = 2,
			CycleChainHead = APTR.Null,
			CycleChainCount = 0,
			CycleChainRequests = 3,
		};
		Assert.True(MuiWindowInteractionStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiWindowInteractionStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, 20, out var requests) && requests.Raw == 0x1654u);
		Assert.True(MuiWindowInteractionStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4, out var flags) && flags == 1);
		Assert.True(MuiWindowInteractionStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 4, 0));
		Assert.True(MuiWindowInteractionStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded) && decoded.SnapshotFlags == 0);
		Assert.False(MuiWindowInteractionStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiWindowInteractionStateRecord.Size, out _));
		Assert.False(MuiWindowInteractionStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, 0, out _));
	}

	private static MuiHeadlessTestPlatform CreateWindow(out APTR window,
		out APTR member)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Window.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var windowClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		window = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			windowClass, APTR.Null);
		member = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			windowClass, APTR.Null);
		return platform;
	}
}
