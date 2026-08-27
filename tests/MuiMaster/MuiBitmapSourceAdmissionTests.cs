using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiBitmapSourceAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint BitmapBitmap = 0x804279BD;
	private const uint StateKey = 0x7F070023;

	[Fact]
	public void BitmapSourceAdmissionRequiresMappedSourceAndOwner()
	{
		var platform = CreatePlatform(out var bitmapClass);
		var bitmap = MuiCommonControlCore.CreateControl(ref platform, State,
			bitmapClass, APTR.Null);
		Assert.True(MuiCommonControlCore.TryGetBitmapSourceStateRecord(
			ref platform, State, bitmap, out var valid));
		Assert.True(MuiBitmapSourceStateAdmission.Validate(ref platform, valid));
		Assert.True(MuiBitmapSourceStateAdmission.ValidateLive(ref platform,
			State, bitmap, valid));
		valid.Source = APTR.FromPointer(0xF0000);
		Assert.False(MuiBitmapSourceStateAdmission.Validate(ref platform, valid));
		Assert.False(MuiBitmapSourceStateAdmission.ValidateLive(ref platform,
			State, bitmap, valid));
		valid.Source = APTR.Null;
		Assert.False(MuiBitmapSourceStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedBitmapSourceFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var bitmapClass);
		var bitmap = MuiCommonControlCore.CreateControl(ref platform, State,
			bitmapClass, APTR.Null);
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			bitmap, BitmapBitmap, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, bitmap,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiBitmapSourceStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiBitmapSourceStateField.Source, 0xF0000));
		Assert.True(MuiBitmapSourceStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0xF0000u, structural.Source.Raw);
		Assert.False(MuiBitmapSourceStateAdmission.Validate(ref platform,
			structural));
		Assert.False(MuiBitmapSourceStateAdmission.ValidateLive(ref platform,
			State, bitmap, structural));
		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetBitmapSourceStateRecord(
			ref platform, State, bitmap, out _));
		Assert.False(MuiCommonControlCore.TryReadBitmapSourceState(ref platform,
			State, bitmap, MuiControlClass.Bitmap, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, bitmap,
			BitmapBitmap, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapBitmap, 0x3000));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			bitmap, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			bitmap, BitmapBitmap, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiBitmapSourceStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiBitmapSourceStateField.Source,
			out var preserved));
		Assert.Equal(0xF0000u, preserved);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR bitmapClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x8000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Bitmap.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		bitmapClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 1, APTR.FromPointer(1), false);
		return platform;
	}
}
