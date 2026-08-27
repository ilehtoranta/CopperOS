using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiWindowVisualAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void WindowVisualAdmissionRequiresCanonicalValuesAndOwner()
	{
		var platform = CreateWindow(out var window);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.NoMenus, out _));
		Assert.True(MuiWindowPublicCore.TryGetWindowVisualState(ref platform,
			State, window, out var valid));
		Assert.True(MuiWindowVisualStateAdmission.Validate(valid));
		Assert.True(MuiWindowVisualStateAdmission.ValidateLive(ref platform,
			State, window, valid));
		valid.Opacity = 256;
		Assert.False(MuiWindowVisualStateAdmission.Validate(valid));
		Assert.False(MuiWindowVisualStateAdmission.ValidateLive(ref platform,
			State, window, valid));
		valid.Opacity = 0;
		Assert.False(MuiWindowVisualStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedWindowVisualMagicFailsClosedButRemainsStructural()
	{
		var platform = CreateWindow(out var window);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.NoMenus, out _));
		Assert.True(MuiWindowPublicCore.TryGetWindowVisualState(ref platform,
			State, window, out _));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, window,
			MuiWindowPublicCore.WindowVisualStateKey);
		Assert.True(MuiWindowVisualStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiWindowVisualStateField.Magic, 0));
		Assert.True(MuiWindowVisualStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiWindowVisualStateRecordCodec.TryRead(ref platform, block,
			out _));
		Assert.False(MuiWindowPublicCore.TryGetWindowVisualState(ref platform,
			State, window, out _));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.NoMenus, out _));
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, window,
			MuiWindowPublicCore.WindowVisualStateKey));
	}

	[Fact]
	public void WindowVisualRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var address = APTR.FromPointer(0x1700);
		var value = new MuiWindowVisualStateRecord
		{
			Magic = MuiWindowVisualStateRecord.Cookie,
			NoMenus = 1,
			HasAlpha = 1,
			Opacity = 128,
			FancyDrawing = 1,
			MenuAction = 7,
		};
		Assert.True(MuiWindowVisualStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiWindowVisualStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, 12, out var opacity) && opacity.Raw == 0x170Cu);
		Assert.True(MuiWindowVisualStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 20, out var action) && action == 7);
		Assert.True(MuiWindowVisualStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 12, 64));
		Assert.True(MuiWindowVisualStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded) && decoded.Opacity == 64);
		Assert.False(MuiWindowVisualStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiWindowVisualStateRecord.Size, out _));
		Assert.False(MuiWindowVisualStateRecordMemoryCodec.TryGetAddress(ref platform,
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
