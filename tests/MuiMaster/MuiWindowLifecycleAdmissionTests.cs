using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiWindowLifecycleAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void WindowLifecycleAdmissionRequiresCanonicalTopologyAndOwner()
	{
		var platform = CreateWindow(out var window);
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.True(MuiApplicationWindowCore.TryGetWindowLifecycleState(
			ref platform, State, window, out var valid));
		Assert.True(MuiWindowLifecycleStateAdmission.Validate(valid));
		Assert.True(MuiWindowLifecycleStateAdmission.ValidateLive(ref platform,
			State, window, valid));
		valid.Open = 2;
		Assert.False(MuiWindowLifecycleStateAdmission.Validate(valid));
		Assert.False(MuiWindowLifecycleStateAdmission.ValidateLive(ref platform,
			State, window, valid));
		valid.Open = 0;
		valid.NativeWindow = APTR.Null;
		Assert.False(MuiWindowLifecycleStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedWindowLifecycleMagicFailsClosedButRemainsStructural()
	{
		var platform = CreateWindow(out var window);
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.True(MuiApplicationWindowCore.TryGetWindowLifecycleState(
			ref platform, State, window, out _));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, window,
			MuiApplicationWindowCore.WindowLifecycleStateKey);
		Assert.True(MuiWindowLifecycleStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiWindowLifecycleStateField.Magic, 0));
		Assert.True(MuiWindowLifecycleStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiWindowLifecycleStateRecordCodec.TryRead(ref platform,
			block, out _));
		Assert.False(MuiApplicationWindowCore.TryGetWindowLifecycleState(
			ref platform, State, window, out _));
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, window,
			MuiApplicationWindowCore.WindowLifecycleStateKey));
	}

	[Fact]
	public void WindowLifecycleRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var address = APTR.FromPointer(0x1580);
		var value = new MuiWindowLifecycleStateRecord
		{
			Magic = MuiWindowLifecycleStateRecord.Cookie,
			NativeWindow = APTR.Null,
			Open = 0,
			EventMask = 0x40,
			IconifiedOpen = 1,
		};
		Assert.True(MuiWindowLifecycleStateRecordCodec.WriteRecord(ref platform, address,
			value));
		Assert.True(MuiWindowLifecycleStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, 16, out var iconified) && iconified.Raw == 0x1590u);
		Assert.True(MuiWindowLifecycleStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 12, out var eventMask) && eventMask == 0x40u);
		Assert.True(MuiWindowLifecycleStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 16, 0));
		Assert.True(MuiWindowLifecycleStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded) && decoded.IconifiedOpen == 0);
		Assert.False(MuiWindowLifecycleStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiWindowLifecycleStateRecord.Size, out _));
		Assert.False(MuiWindowLifecycleStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, 0, out _));
	}

	private static MuiHeadlessTestPlatform CreateWindow(out APTR window)
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
		return platform;
	}
}
