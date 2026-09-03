using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiWindowFocusAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void WindowFocusAdmissionRequiresLiveFocusObjectsAndOwner()
	{
		var platform = CreateWindow(out var window);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, 0x80427925, out _));
		Assert.True(MuiApplicationWindowCore.TryGetWindowFocusState(
			ref platform, State, window, out var valid));
		Assert.True(MuiWindowFocusStateAdmission.Validate(ref platform, valid));
		Assert.True(MuiWindowFocusStateAdmission.ValidateLive(ref platform,
			State, window, valid));
		valid.ActiveObject = APTR.FromPointer(0x1200);
		Assert.True(MuiWindowFocusStateAdmission.Validate(ref platform, valid));
		Assert.False(MuiWindowFocusStateAdmission.ValidateLive(ref platform,
			State, window, valid));
		Assert.False(MuiWindowFocusStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedWindowFocusMagicFailsClosedButRemainsStructural()
	{
		var platform = CreateWindow(out var window);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, 0x80427925, out _));
		Assert.True(MuiApplicationWindowCore.TryGetWindowFocusState(
			ref platform, State, window, out _));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, window,
			MuiApplicationWindowCore.WindowFocusStateKey);
		Assert.True(MuiWindowFocusStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiWindowFocusStateField.Magic, 0));
		Assert.True(MuiWindowFocusStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiWindowFocusStateRecordCodec.TryRead(ref platform, block,
			out _));
		Assert.False(MuiApplicationWindowCore.TryGetWindowFocusState(
			ref platform, State, window, out _));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x80427925, out _));
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, window,
			MuiApplicationWindowCore.WindowFocusStateKey));
	}

	[Fact]
	public void WindowFocusRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var address = APTR.FromPointer(0x1540);
		var value = new MuiWindowFocusStateRecord
		{
			Magic = MuiWindowFocusStateRecord.Cookie,
			ActiveObject = APTR.Null,
			DefaultObject = APTR.Null,
		};
		Assert.True(MuiWindowFocusStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiWindowFocusStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiWindowFocusStateField.DefaultObject,
			out var defaultObject) && defaultObject.Raw == 0x1548u);
		Assert.True(MuiWindowFocusStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiWindowFocusStateField.ActiveObject,
			out var active) && active == 0);
		Assert.True(MuiWindowFocusStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiWindowFocusStateField.Magic,
			MuiWindowFocusStateRecord.Cookie));
		Assert.True(MuiWindowFocusStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded) && decoded.Magic == value.Magic &&
			decoded.ActiveObject == value.ActiveObject && decoded.DefaultObject.IsNull);
		Assert.False(MuiWindowFocusStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, (MuiWindowFocusStateField)255, out _));
		Assert.False(MuiWindowFocusStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiWindowFocusStateRecord.Size, out _));
		Assert.False(MuiWindowFocusStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, (uint)0, out _));
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
