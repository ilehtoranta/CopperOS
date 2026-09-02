using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiImageSpecAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint ImageSpec = 0x804233D5;
	private const uint ImageBuiltinSpec = 0x8042B907;
	private const uint StateKey = 0x7F070022;

	[Fact]
	public void ImageSpecAdmissionRequiresCanonicalValueAndOwner()
	{
		var platform = CreatePlatform(out var imageClass);
		var image = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, BuildTags(ref platform, 0x1900, 3, 7));
		Assert.NotEqual(APTR.Null, image);
		Assert.True(MuiCommonControlCore.TryGetImageSpecStateRecord(
			ref platform, State, image, out var valid));
		Assert.True(MuiImageSpecStateAdmission.Validate(ref platform, valid));
		Assert.True(MuiImageSpecStateAdmission.ValidateLive(ref platform, State,
			image, valid));
		valid.Raw = APTR.FromPointer(0x3000).Raw;
		Assert.True(MuiImageSpecStateAdmission.Validate(ref platform, valid));
		valid.Raw = 0xF0000;
		Assert.False(MuiImageSpecStateAdmission.Validate(ref platform, valid));
		Assert.False(MuiImageSpecStateAdmission.ValidateLive(ref platform, State,
			image, valid));
		valid.Raw = 3;
		Assert.False(MuiImageSpecStateAdmission.ValidateLive(ref platform, State,
			APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void ImageSpecRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1BC0);
		var value = new MuiImageSpecStateRecord
		{
			Magic = MuiImageSpecStateRecord.Cookie,
			Present = 1,
			Raw = 3,
			BuiltinPresent = 1,
			Builtin = 7,
		};

		Assert.True(MuiImageSpecStateRecordCodec.Write(ref platform, address, value));
		Assert.True(MuiImageSpecStateRecordCodec.TryReadStructural(ref platform,
			address, out var structural));
		Assert.Equal(value.Magic, structural.Magic);
		Assert.Equal(value.Present, structural.Present);
		Assert.Equal(value.Raw, structural.Raw);
		Assert.Equal(value.BuiltinPresent, structural.BuiltinPresent);
		Assert.Equal(value.Builtin, structural.Builtin);
		Assert.True(MuiImageSpecStateRecordCodec.TryRead(ref platform, address,
			out _));
		Assert.True(MuiImageSpecStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, 16, out var builtinField));
		Assert.Equal(address.Raw + 16, builtinField.Raw);
		Assert.True(MuiImageSpecStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 8, out var raw));
		Assert.Equal(value.Raw, raw);
		Assert.True(MuiImageSpecStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiImageSpecStateField.Builtin, out var typedBuiltin));
		Assert.Equal(address.Raw + MuiImageSpecStateRecord.BuiltinOffset,
			typedBuiltin.Raw);
		Assert.True(MuiImageSpecStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiImageSpecStateField.Raw, 0x2B00));
		Assert.True(MuiImageSpecStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiImageSpecStateField.Magic, out var typedMagic));
		Assert.Equal(value.Magic, typedMagic);
		Assert.True(MuiImageSpecStateRecordCodec.TryReadStructural(ref platform,
			address, out var typedStructural));
		Assert.Equal(0x2B00u, typedStructural.Raw);
		Assert.Equal(value.Builtin, typedStructural.Builtin);
		Assert.False(MuiImageSpecStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, (MuiImageSpecStateField)0xFF, out _));
		Assert.False(MuiImageSpecStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiImageSpecStateRecord.Size, out _));
		Assert.False(MuiImageSpecStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, (uint)0, out _));
		Assert.False(MuiImageSpecStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void ImageSpecSequentialRecordPreservesUnionStateAndRejectsTruncation()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x8000,
			State);
		var address = APTR.FromPointer(0x3600);
		var value = new MuiImageSpecStateRecord
		{
			Magic = MuiImageSpecStateRecord.Cookie,
			Present = 1,
			Raw = 0x2A00,
			BuiltinPresent = 1,
			Builtin = uint.MaxValue,
		};
		Assert.True(MuiImageSpecStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiImageSpecStateRecordCodec.TryReadRecord(ref platform,
			address, out var actual));
		Assert.Equal(value.Magic, actual.Magic);
		Assert.Equal(value.Present, actual.Present);
		Assert.Equal(value.Raw, actual.Raw);
		Assert.Equal(value.BuiltinPresent, actual.BuiltinPresent);
		Assert.Equal(value.Builtin, actual.Builtin);
		Assert.False(MuiImageSpecStateRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x40FFC), out _));
	}

	[Fact]
	public void MalformedImageSpecFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var imageClass);
		var image = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, BuildTags(ref platform, 0x1A00, 3, 7));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			image, ImageSpec, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, image,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiImageSpecStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiImageSpecStateField.Raw, 0xF0000));
		Assert.True(MuiImageSpecStateRecordCodec.TryReadStructural(ref platform,
			block, out var structural));
		Assert.Equal(0xF0000u, structural.Raw);
		Assert.False(MuiImageSpecStateAdmission.Validate(ref platform, structural));
		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetImageSpecStateRecord(
			ref platform, State, image, out _));
		Assert.False(MuiCommonControlCore.TryReadImageSpecState(ref platform,
			State, image, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, image,
			ImageSpec, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			image, ImageSpec, 1));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			image, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			image, ImageSpec, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiImageSpecStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiImageSpecStateField.Raw, out var preserved));
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
		uint address, uint spec, uint builtin)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, ImageSpec);
		platform.WriteUInt32(tags, 4, spec);
		platform.WriteUInt32(tags, 8, ImageBuiltinSpec);
		platform.WriteUInt32(tags, 12, builtin);
		platform.WriteUInt32(tags, 16, 0);
		platform.WriteUInt32(tags, 20, 0);
		return tags;
	}
}
