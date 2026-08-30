using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiImageRenderAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint ImageState = 0x8042A3AD;
	private const uint Selected = 0x8042654B;
	private const uint ImageFreeHoriz = 0x8042DA84;
	private const uint ImageFreeVert = 0x8042EA28;
	private const uint ShowSelState = 0x80429BA8;
	private const uint StateKey = 0x7F070029;

	[Fact]
	public void ImageRenderAdmissionRequiresCanonicalRecordAndOwner()
	{
		var platform = CreatePlatform(out var imageClass);
		var image = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, BuildTags(ref platform, 0x1900));
		Assert.NotEqual(APTR.Null, image);
		Assert.True(MuiCommonControlCore.TryGetImageRenderStateRecord(
			ref platform, State, image, out var valid));
		Assert.True(MuiImageRenderStateAdmission.Validate(valid));
		Assert.True(MuiImageRenderStateAdmission.ValidateLive(ref platform,
			State, image, valid));
		valid.Magic = 0;
		Assert.False(MuiImageRenderStateAdmission.Validate(valid));
		Assert.False(MuiImageRenderStateAdmission.ValidateLive(ref platform,
			State, image, valid));
		valid.Magic = MuiImageRenderStateRecord.Cookie;
		Assert.False(MuiImageRenderStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void ImageRenderRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1BC0);
		var value = new MuiImageRenderStateRecord
		{
			Magic = MuiImageRenderStateRecord.Cookie,
			ImageState = uint.MaxValue,
			Selected = 1,
			FreeHoriz = 0x80000000u,
			FreeVert = uint.MaxValue,
			ShowSelState = 0,
		};

		Assert.True(MuiImageRenderStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiImageRenderStateRecordCodec.TryReadStructural(
			ref platform, address, out var structural));
		Assert.Equal(value.Magic, structural.Magic);
		Assert.Equal(value.ImageState, structural.ImageState);
		Assert.Equal(value.Selected, structural.Selected);
		Assert.Equal(value.FreeHoriz, structural.FreeHoriz);
		Assert.Equal(value.FreeVert, structural.FreeVert);
		Assert.Equal(value.ShowSelState, structural.ShowSelState);
		Assert.True(MuiImageRenderStateRecordCodec.TryRead(ref platform, address,
			out _));
		Assert.True(MuiImageRenderStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, 20, out var showSelField));
		Assert.Equal(address.Raw + 20, showSelField.Raw);
		Assert.True(MuiImageRenderStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 12, out var freeHoriz));
		Assert.Equal(value.FreeHoriz, freeHoriz);
		Assert.False(MuiImageRenderStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiImageRenderStateRecord.Size, out _));
		Assert.False(MuiImageRenderStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, 0, out _));
		Assert.False(MuiImageRenderStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void ImageRenderSequentialRecordPreservesPoliciesAndRejectsTruncation()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			State);
		var address = APTR.FromPointer(0x3600);
		var value = new MuiImageRenderStateRecord
		{
			Magic = MuiImageRenderStateRecord.Cookie,
			ImageState = uint.MaxValue,
			Selected = 1,
			FreeHoriz = 0x80000000u,
			FreeVert = uint.MaxValue,
			ShowSelState = 1,
		};
		Assert.True(MuiImageRenderStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiImageRenderStateRecordCodec.TryReadRecord(ref platform,
			address, out var actual));
		Assert.Equal(value.Magic, actual.Magic);
		Assert.Equal(value.ImageState, actual.ImageState);
		Assert.Equal(value.Selected, actual.Selected);
		Assert.Equal(value.FreeHoriz, actual.FreeHoriz);
		Assert.Equal(value.FreeVert, actual.FreeVert);
		Assert.Equal(value.ShowSelState, actual.ShowSelState);
		Assert.False(MuiImageRenderStateRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x30FFC), out _));
	}

	[Fact]
	public void MalformedImageRenderFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var imageClass);
		var image = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, BuildTags(ref platform, 0x1A00));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			image, ImageState, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, image,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiImageRenderStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiImageRenderStateField.Magic, 0));
		Assert.True(MuiImageRenderStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiImageRenderStateAdmission.Validate(structural));
		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetImageRenderStateRecord(
			ref platform, State, image, out _));
		Assert.False(MuiCommonControlCore.TryReadImageRenderState(ref platform,
			State, image, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, image,
			ImageState, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			image, ImageState, 1));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			image, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			image, ImageState, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiImageRenderStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiImageRenderStateField.Magic,
			out var preserved));
		Assert.Equal(0u, preserved);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR imageClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x8000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Image.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		imageClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 1, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR BuildTags(ref MuiHeadlessTestPlatform platform,
		uint address)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, ImageState);
		platform.WriteUInt32(tags, 4, 0);
		platform.WriteUInt32(tags, 8, Selected);
		platform.WriteUInt32(tags, 12, 1);
		platform.WriteUInt32(tags, 16, ImageFreeHoriz);
		platform.WriteUInt32(tags, 20, 1);
		platform.WriteUInt32(tags, 24, ImageFreeVert);
		platform.WriteUInt32(tags, 28, 0);
		platform.WriteUInt32(tags, 32, ShowSelState);
		platform.WriteUInt32(tags, 36, 1);
		platform.WriteUInt32(tags, 40, 0);
		platform.WriteUInt32(tags, 44, 0);
		return tags;
	}
}
