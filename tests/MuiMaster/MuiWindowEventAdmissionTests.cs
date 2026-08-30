using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiWindowEventAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void WindowEventAdmissionRequiresCanonicalPointersAndOwner()
	{
		var platform = CreateWindow(out var window);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.InputEvent, out _));
		Assert.True(MuiApplicationWindowCore.TryGetWindowEventState(ref platform,
			State, window, out var valid));
		Assert.True(MuiWindowEventStateAdmission.Validate(ref platform, valid));
		Assert.True(MuiWindowEventStateAdmission.ValidateLive(ref platform,
			State, window, valid));
		valid.CloseRequest = 2;
		Assert.False(MuiWindowEventStateAdmission.Validate(ref platform, valid));
		Assert.False(MuiWindowEventStateAdmission.ValidateLive(ref platform,
			State, window, valid));
		valid.CloseRequest = 0;
		valid.MouseObject = APTR.FromPointer(0x1200);
		Assert.True(MuiWindowEventStateAdmission.Validate(ref platform, valid));
		Assert.False(MuiWindowEventStateAdmission.ValidateLive(ref platform,
			State, window, valid));
	}

	[Fact]
	public void MalformedWindowEventMagicFailsClosedButRemainsStructural()
	{
		var platform = CreateWindow(out var window);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.InputEvent, out _));
		Assert.True(MuiApplicationWindowCore.TryGetWindowEventState(ref platform,
			State, window, out _));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, window,
			MuiApplicationWindowCore.WindowEventStateKey);
		Assert.True(MuiWindowEventStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiWindowEventStateField.Magic, 0));
		Assert.True(MuiWindowEventStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiWindowEventStateRecordCodec.TryRead(ref platform, block,
			out _));
		Assert.False(MuiApplicationWindowCore.TryGetWindowEventState(ref platform,
			State, window, out _));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.InputEvent, out _));
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, window,
			MuiApplicationWindowCore.WindowEventStateKey));
	}

	[Fact]
	public void WindowEventRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var address = APTR.FromPointer(0x15C0);
		var value = new MuiWindowEventStateRecord
		{
			Magic = MuiWindowEventStateRecord.Cookie,
			CloseRequest = 1,
			InputEvent = APTR.Null,
			MouseObject = APTR.Null,
		};
		Assert.True(MuiWindowEventStateRecordCodec.WriteRecord(ref platform, address,
			value));
		Assert.True(MuiWindowEventStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, 12, out var mouseObject) && mouseObject.Raw == 0x15CCu);
		Assert.True(MuiWindowEventStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out var closeRequest) && closeRequest == 1);
		Assert.True(MuiWindowEventStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 4, 0));
		Assert.True(MuiWindowEventStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded) && decoded.CloseRequest == 0);
		Assert.False(MuiWindowEventStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiWindowEventStateRecord.Size, out _));
		Assert.False(MuiWindowEventStateRecordMemoryCodec.TryGetAddress(ref platform,
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
