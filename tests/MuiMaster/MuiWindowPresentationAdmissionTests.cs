using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiWindowPresentationAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void WindowPresentationAdmissionRequiresMappedPointersAndOwner()
	{
		var platform = CreateWindow(out var window);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Title, out _));
		Assert.True(MuiWindowPublicCore.TryGetWindowPresentationState(
			ref platform, State, window, out var valid));
		Assert.True(MuiWindowPresentationStateAdmission.Validate(ref platform,
			valid));
		Assert.True(MuiWindowPresentationStateAdmission.ValidateLive(ref platform,
			State, window, valid));
		valid.Screen = APTR.FromPointer(0xF0000000);
		Assert.False(MuiWindowPresentationStateAdmission.Validate(ref platform,
			valid));
		Assert.False(MuiWindowPresentationStateAdmission.ValidateLive(ref platform,
			State, window, valid));
		valid.Screen = APTR.Null;
		Assert.False(MuiWindowPresentationStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedWindowPresentationMagicFailsClosedButRemainsStructural()
	{
		var platform = CreateWindow(out var window);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Title, out _));
		Assert.True(MuiWindowPublicCore.TryGetWindowPresentationState(
			ref platform, State, window, out _));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, window,
			MuiWindowPublicCore.WindowPresentationStateKey);
		Assert.True(MuiWindowPresentationStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiWindowPresentationStateField.Magic, 0));
		Assert.True(MuiWindowPresentationStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiWindowPresentationStateRecordCodec.TryRead(ref platform,
			block, out _));
		Assert.False(MuiWindowPublicCore.TryGetWindowPresentationState(
			ref platform, State, window, out _));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Title, out _));
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, window,
			MuiWindowPublicCore.WindowPresentationStateKey));
	}

	[Fact]
	public void WindowPresentationRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var address = APTR.FromPointer(0x1680);
		var value = new MuiWindowPresentationStateRecord
		{
			Magic = MuiWindowPresentationStateRecord.Cookie,
			Title = APTR.Null,
			Screen = APTR.Null,
			ScreenTitle = APTR.Null,
			PublicScreen = APTR.Null,
		};
		Assert.True(MuiWindowPresentationStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiWindowPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiWindowPresentationStateField.PublicScreen,
			out var publicScreen) && publicScreen.Raw ==
			0x1690u);
		Assert.True(MuiWindowPresentationStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiWindowPresentationStateField.Screen,
			out var screen) && screen == 0);
		Assert.True(MuiWindowPresentationStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiWindowPresentationStateField.Magic,
			MuiWindowPresentationStateRecord.Cookie));
		Assert.True(MuiWindowPresentationStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded) && decoded.Magic == value.Magic &&
			decoded.Title == value.Title && decoded.Screen == value.Screen &&
			decoded.ScreenTitle == value.ScreenTitle && decoded.PublicScreen.IsNull);
		Assert.False(MuiWindowPresentationStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiWindowPresentationStateField)255, out _));
		Assert.False(MuiWindowPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiWindowPresentationStateRecord.Size, out _));
		Assert.False(MuiWindowPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, (uint)0, out _));
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
