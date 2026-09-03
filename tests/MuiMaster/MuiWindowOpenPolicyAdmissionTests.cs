using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiWindowOpenPolicyAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void WindowOpenPolicyAdmissionRequiresCanonicalValuesAndOwner()
	{
		var platform = CreateWindow(out var window);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.AltHeight, out _));
		Assert.True(MuiApplicationWindowCore.TryGetWindowOpenPolicyState(
			ref platform, State, window, out var valid));
		Assert.True(MuiWindowOpenPolicyStateAdmission.Validate(valid));
		Assert.True(MuiWindowOpenPolicyStateAdmission.ValidateLive(ref platform,
			State, window, valid));
		valid.Borderless = 2;
		Assert.False(MuiWindowOpenPolicyStateAdmission.Validate(valid));
		Assert.False(MuiWindowOpenPolicyStateAdmission.ValidateLive(ref platform,
			State, window, valid));
		valid.Borderless = 0;
		Assert.False(MuiWindowOpenPolicyStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedWindowOpenPolicyMagicFailsClosedButRemainsStructural()
	{
		var platform = CreateWindow(out var window);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.AltHeight, out _));
		Assert.True(MuiApplicationWindowCore.TryGetWindowOpenPolicyState(
			ref platform, State, window, out _));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, window,
			MuiApplicationWindowCore.WindowOpenPolicyStateKey);
		Assert.True(MuiWindowOpenPolicyStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiWindowOpenPolicyStateField.Magic, 0));
		Assert.True(MuiWindowOpenPolicyStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiWindowOpenPolicyStateRecordCodec.TryRead(ref platform,
			block, out _));
		Assert.False(MuiApplicationWindowCore.TryGetWindowOpenPolicyState(
			ref platform, State, window, out _));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Borderless, out _));
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, window,
			MuiApplicationWindowCore.WindowOpenPolicyStateKey));
	}

	[Fact]
	public void WindowOpenPolicyRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var address = APTR.FromPointer(0x1740);
		var value = new MuiWindowOpenPolicyStateRecord
		{
			Magic = MuiWindowOpenPolicyStateRecord.Cookie,
			AlternateHeight = -2,
			AlternateWidth = 640,
			Height = 480,
			Width = 640,
			CloseGadget = 1,
			Borderless = 1,
		};
		Assert.True(MuiWindowOpenPolicyStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiWindowOpenPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiWindowOpenPolicyStateField.UseRightBorderScroller,
			out var rightScroller) && rightScroller.Raw ==
			0x1794u);
		Assert.True(MuiWindowOpenPolicyStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiWindowOpenPolicyStateField.AlternateHeight,
			out var alternateHeight) &&
			alternateHeight == unchecked((uint)-2));
		Assert.True(MuiWindowOpenPolicyStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiWindowOpenPolicyStateField.Height,
			unchecked((uint)-240)));
		Assert.True(MuiWindowOpenPolicyStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded) && decoded.Magic == value.Magic &&
			decoded.AlternateHeight == value.AlternateHeight &&
			decoded.CloseGadget == value.CloseGadget && decoded.Borderless == value.Borderless &&
			decoded.Height == -240);
		Assert.False(MuiWindowOpenPolicyStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiWindowOpenPolicyStateField)255, out _));
		Assert.False(MuiWindowOpenPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiWindowOpenPolicyStateRecord.Size, out _));
		Assert.False(MuiWindowOpenPolicyStateRecordMemoryCodec.TryGetAddress(
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
		var tags = APTR.FromPointer(0x1200);
		platform.WriteUInt32(tags, 0, MuiWindowPublicCore.AltHeight);
		platform.WriteUInt32(tags, 4, unchecked((uint)-1));
		platform.WriteUInt32(tags, 8, 0);
		window = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			windowClass, tags);
		return platform;
	}
}
