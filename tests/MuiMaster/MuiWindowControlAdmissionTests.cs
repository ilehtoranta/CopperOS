using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiWindowControlAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void WindowControlAdmissionRequiresCanonicalValuesAndOwner()
	{
		var platform = CreateWindow(out var window);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.VisibleOnMaximize, out _));
		Assert.True(MuiWindowPublicCore.TryGetWindowControlState(ref platform,
			State, window, out var valid));
		Assert.True(MuiWindowControlStateAdmission.Validate(valid));
		Assert.True(MuiWindowControlStateAdmission.ValidateLive(ref platform,
			State, window, valid));
		valid.VisibleOnMaximize = 2;
		Assert.False(MuiWindowControlStateAdmission.Validate(valid));
		Assert.False(MuiWindowControlStateAdmission.ValidateLive(ref platform,
			State, window, valid));
		valid.VisibleOnMaximize = 1;
		Assert.False(MuiWindowControlStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedWindowControlMagicFailsClosedButRemainsStructural()
	{
		var platform = CreateWindow(out var window);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.VisibleOnMaximize, out _));
		Assert.True(MuiWindowPublicCore.TryGetWindowControlState(ref platform,
			State, window, out _));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, window,
			MuiWindowPublicCore.WindowControlStateKey);
		Assert.True(MuiWindowControlStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiWindowControlStateField.Magic, 0));
		Assert.True(MuiWindowControlStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiWindowControlStateRecordCodec.TryRead(ref platform, block,
			out _));
		Assert.False(MuiWindowPublicCore.TryGetWindowControlState(ref platform,
			State, window, out _));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.VisibleOnMaximize, out _));
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, window,
			MuiWindowPublicCore.WindowControlStateKey));
	}

	[Fact]
	public void WindowControlRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var address = APTR.FromPointer(0x1500);
		var value = new MuiWindowControlStateRecord
		{
			Magic = MuiWindowControlStateRecord.Cookie,
			Id = 7,
			DisableKeys = 1,
			VisibleOnMaximize = 1,
			IsSubWindow = 0,
			NeedsMouseObject = 1,
		};
		Assert.True(MuiWindowControlStateRecordCodec.WriteRecord(ref platform, address,
			value));
		Assert.True(MuiWindowControlStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiWindowControlStateField.NeedsMouseObject,
			out var needsMouse) && needsMouse.Raw == 0x1514u);
		Assert.True(MuiWindowControlStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiWindowControlStateField.VisibleOnMaximize,
			out var visible) && visible == 1);
		Assert.True(MuiWindowControlStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiWindowControlStateField.VisibleOnMaximize, 0));
		Assert.True(MuiWindowControlStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded) && decoded.Magic == value.Magic &&
			decoded.Id == value.Id && decoded.NeedsMouseObject == value.NeedsMouseObject &&
			decoded.VisibleOnMaximize == 0);
		Assert.True(MuiWindowControlStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiWindowControlStateField.Magic,
			out var magic) && magic == value.Magic);
		Assert.False(MuiWindowControlStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiWindowControlStateField)255, out _));
		Assert.False(MuiWindowControlStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiWindowControlStateRecord.Size, out _));
		Assert.False(MuiWindowControlStateRecordMemoryCodec.TryGetAddress(ref platform,
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
