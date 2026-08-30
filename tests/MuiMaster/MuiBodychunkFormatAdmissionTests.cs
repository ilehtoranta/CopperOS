using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiBodychunkFormatAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint BodychunkCompression = 0x8042DE5F;
	private const uint BodychunkDepth = 0x8042C392;
	private const uint BodychunkMasking = 0x80423B0E;
	private const uint StateKey = 0x7F070027;

	[Fact]
	public void BodychunkFormatAdmissionRequiresCanonicalRecordAndOwner()
	{
		var platform = CreatePlatform(out var bodyClass);
		var body = MuiCommonControlCore.CreateControl(ref platform, State,
			bodyClass, BuildTags(ref platform, 0x1900, 1, 2, 0));
		Assert.NotEqual(APTR.Null, body);
		Assert.True(MuiCommonControlCore.TryGetBodychunkFormatStateRecord(
			ref platform, State, body, out var valid));
		Assert.True(MuiBodychunkFormatStateAdmission.Validate(valid));
		Assert.True(MuiBodychunkFormatStateAdmission.ValidateLive(ref platform,
			State, body, valid));
		valid.Magic = 0;
		Assert.False(MuiBodychunkFormatStateAdmission.Validate(valid));
		Assert.False(MuiBodychunkFormatStateAdmission.ValidateLive(ref platform,
			State, body, valid));
		valid.Magic = MuiBodychunkFormatStateRecord.Cookie;
		Assert.False(MuiBodychunkFormatStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void BodychunkFormatRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1B80);
		var value = new MuiBodychunkFormatStateRecord
		{
			Magic = MuiBodychunkFormatStateRecord.Cookie,
			Compression = uint.MaxValue,
			Depth = 2,
			Masking = 0,
		};

		Assert.True(MuiBodychunkFormatStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiBodychunkFormatStateRecordCodec.TryReadStructural(
			ref platform, address, out var structural));
		Assert.Equal(value.Magic, structural.Magic);
		Assert.Equal(value.Compression, structural.Compression);
		Assert.Equal(value.Depth, structural.Depth);
		Assert.Equal(value.Masking, structural.Masking);
		Assert.True(MuiBodychunkFormatStateRecordCodec.TryRead(ref platform, address,
			out _));
		Assert.True(MuiBodychunkFormatStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, 12, out var lastField));
		Assert.Equal(address.Raw + 12, lastField.Raw);
		Assert.True(MuiBodychunkFormatStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4, out var compression));
		Assert.Equal(value.Compression, compression);
		Assert.False(MuiBodychunkFormatStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiBodychunkFormatStateRecord.Size, out _));
		Assert.False(MuiBodychunkFormatStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, 0, out _));
		Assert.False(MuiBodychunkFormatStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void BodychunkFormatSequentialRecordPreservesValuesAndBounds()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1C20);
		var value = new MuiBodychunkFormatStateRecord
		{
			Magic = MuiBodychunkFormatStateRecord.Cookie,
			Compression = uint.MaxValue,
			Depth = 0xCAFEBABEu,
			Masking = 0x12345678u,
		};

		Assert.True(MuiBodychunkFormatStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiBodychunkFormatStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Compression, decoded.Compression);
		Assert.Equal(value.Depth, decoded.Depth);
		Assert.Equal(value.Masking, decoded.Masking);

		var crossingEnd = APTR.FromPointer(0x40FFF);
		Assert.False(MuiBodychunkFormatStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiBodychunkFormatStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}

	[Fact]
	public void MalformedBodychunkFormatFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var bodyClass);
		var body = MuiCommonControlCore.CreateControl(ref platform, State,
			bodyClass, BuildTags(ref platform, 0x1A00, 1, 2, 0));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			body, BodychunkDepth, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, body,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiBodychunkFormatStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiBodychunkFormatStateField.Magic, 0));
		Assert.True(MuiBodychunkFormatStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiBodychunkFormatStateAdmission.Validate(structural));
		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetBodychunkFormatStateRecord(
			ref platform, State, body, out _));
		Assert.False(MuiCommonControlCore.TryReadBodychunkFormatState(
			ref platform, State, body, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, body,
			BodychunkDepth, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			body, BodychunkCompression, 2));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			body, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			body, BodychunkDepth, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiBodychunkFormatStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiBodychunkFormatStateField.Magic,
			out var preserved));
		Assert.Equal(0u, preserved);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR bodyClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x8000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Bodychunk.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		bodyClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 1, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR BuildTags(ref MuiHeadlessTestPlatform platform,
		uint address, uint compression, uint depth, uint masking)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, BodychunkCompression);
		platform.WriteUInt32(tags, 4, compression);
		platform.WriteUInt32(tags, 8, BodychunkDepth);
		platform.WriteUInt32(tags, 12, depth);
		platform.WriteUInt32(tags, 16, BodychunkMasking);
		platform.WriteUInt32(tags, 20, masking);
		platform.WriteUInt32(tags, 24, 0);
		platform.WriteUInt32(tags, 28, 0);
		return tags;
	}
}
