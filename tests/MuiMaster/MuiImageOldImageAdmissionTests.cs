using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiImageOldImageAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint ImageOldImage = 0x80424F3D;
	private const uint StateKey = 0x7F070021;

	[Fact]
	public void ImageOldImageAdmissionRequiresMappedImageAndOwner()
	{
		var platform = CreatePlatform(out var imageClass);
		var image = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, BuildTags(ref platform, 0x1900, 0x3000));
		Assert.NotEqual(APTR.Null, image);
		Assert.True(MuiCommonControlCore.TryGetImageOldImageStateRecord(
			ref platform, State, image, out var valid));
		Assert.True(MuiImageOldImageStateAdmission.Validate(ref platform, valid));
		Assert.True(MuiImageOldImageStateAdmission.ValidateLive(ref platform,
			State, image, valid));
		valid.Image = APTR.FromPointer(0xF0000);
		Assert.False(MuiImageOldImageStateAdmission.Validate(ref platform, valid));
		Assert.False(MuiImageOldImageStateAdmission.ValidateLive(ref platform,
			State, image, valid));
		valid.Image = APTR.FromPointer(0x3000);
		Assert.False(MuiImageOldImageStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void ImageOldImageRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1BC0);
		var value = new MuiImageOldImageStateRecord
		{
			Magic = MuiImageOldImageStateRecord.Cookie,
			Image = APTR.FromPointer(0x3000),
		};

		Assert.True(MuiImageOldImageStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiImageOldImageStateRecordCodec.TryReadStructural(
			ref platform, address, out var structural));
		Assert.Equal(value.Magic, structural.Magic);
		Assert.Equal(value.Image, structural.Image);
		Assert.True(MuiImageOldImageStateRecordCodec.TryRead(ref platform, address,
			out _));
		Assert.True(MuiImageOldImageStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, 4, out var imageField));
		Assert.Equal(address.Raw + 4, imageField.Raw);
		Assert.True(MuiImageOldImageStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4, out var imageRaw));
		Assert.Equal(value.Image.Raw, imageRaw);
		Assert.True(MuiImageOldImageStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiImageOldImageStateField.Image,
			out var typedImage));
		Assert.Equal(address.Raw + MuiImageOldImageStateRecord.ImageOffset,
			typedImage.Raw);
		var imageCursor = new MuiImageOldImageStateFieldCursor
		{
			Record = address,
			Field = MuiImageOldImageStateField.Image,
		};
		Assert.True(MuiImageOldImageStateFieldCursorCodec.TryGetAddress(
			ref platform, imageCursor, out var cursorImage, out var cursorFieldSize));
		Assert.Equal(typedImage, cursorImage);
		Assert.Equal(MuiImageOldImageStateRecord.FieldSize, cursorFieldSize);
		Assert.True(MuiImageOldImageStateRecordMemoryCodec.TryGetAddress(
			ref platform, imageCursor, out var memoryImage, out var memoryFieldSize));
		Assert.Equal(cursorImage, memoryImage);
		Assert.Equal(cursorFieldSize, memoryFieldSize);
		Assert.True(MuiImageOldImageStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiImageOldImageStateField.Image, 0x2B00));
		Assert.True(MuiImageOldImageStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiImageOldImageStateField.Magic,
			out var typedMagic));
		Assert.Equal(value.Magic, typedMagic);
		Assert.True(MuiImageOldImageStateRecordCodec.TryReadStructural(ref platform,
			address, out var typedStructural));
		Assert.Equal(0x2B00u, typedStructural.Image.Raw);
		Assert.False(MuiImageOldImageStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiImageOldImageStateField)0xFF, out _));
		Assert.False(MuiImageOldImageStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiImageOldImageStateRecord.Size, out _));
		Assert.False(MuiImageOldImageStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, (uint)0, out _));
		imageCursor.Field = (MuiImageOldImageStateField)255;
		Assert.False(MuiImageOldImageStateFieldCursorCodec.TryGetAddress(
			ref platform, imageCursor, out _, out _));
		imageCursor.Record = APTR.Null;
		imageCursor.Field = MuiImageOldImageStateField.Image;
		Assert.False(MuiImageOldImageStateFieldCursorCodec.TryGetAddress(
			ref platform, imageCursor, out _, out _));
		Assert.False(MuiImageOldImageStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void ImageOldImageSequentialRecordPreservesPointerAndRejectsTruncation()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			State);
		var address = APTR.FromPointer(0x3600);
		var value = new MuiImageOldImageStateRecord
		{
			Magic = MuiImageOldImageStateRecord.Cookie,
			Image = APTR.FromPointer(0x2A00),
		};
		Assert.True(MuiImageOldImageStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiImageOldImageStateRecordCodec.TryReadRecord(ref platform,
			address, out var actual));
		Assert.Equal(value.Magic, actual.Magic);
		Assert.Equal(value.Image, actual.Image);
		Assert.False(MuiImageOldImageStateRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x30FFC), out _));
	}

	[Fact]
	public void MalformedImageOldImageFailsClosedBeforeRawRepairOrGet()
	{
		var platform = CreatePlatform(out var imageClass);
		var image = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, BuildTags(ref platform, 0x1A00, 0x3000));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			image, ImageOldImage, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, image,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiImageOldImageStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiImageOldImageStateField.Image, 0xF0000));
		Assert.True(MuiImageOldImageStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0xF0000u, structural.Image.Raw);
		Assert.False(MuiImageOldImageStateAdmission.Validate(ref platform,
			structural));
		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetImageOldImageStateRecord(
			ref platform, State, image, out _));
		Assert.False(MuiCommonControlCore.TryReadImageOldImageState(
			ref platform, State, image, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, image,
			ImageOldImage, out _, out _));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			image, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			image, ImageOldImage, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiImageOldImageStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiImageOldImageStateField.Image,
			out var preserved));
		Assert.Equal(0xF0000u, preserved);
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
		uint address, uint image)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, ImageOldImage);
		platform.WriteUInt32(tags, 4, image);
		platform.WriteUInt32(tags, 8, 0);
		platform.WriteUInt32(tags, 12, 0);
		return tags;
	}
}
