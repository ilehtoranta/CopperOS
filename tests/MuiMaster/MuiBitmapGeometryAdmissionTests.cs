using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiBitmapGeometryAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint BitmapWidth = 0x8042EB3A;
	private const uint BitmapHeight = 0x80421560;
	private const uint StateKey = 0x7F070028;

	[Fact]
	public void BitmapGeometryAdmissionRequiresCanonicalRecordAndOwner()
	{
		var platform = CreatePlatform(out var bitmapClass);
		var bitmap = MuiCommonControlCore.CreateControl(ref platform, State,
			bitmapClass, BuildTags(ref platform, 0x1900, 32, 12));
		Assert.NotEqual(APTR.Null, bitmap);
		Assert.True(MuiCommonControlCore.TryGetBitmapGeometryStateRecord(
			ref platform, State, bitmap, out var valid));
		Assert.True(MuiBitmapGeometryStateAdmission.Validate(valid));
		Assert.True(MuiBitmapGeometryStateAdmission.ValidateLive(ref platform,
			State, bitmap, valid));
		valid.Magic = 0;
		Assert.False(MuiBitmapGeometryStateAdmission.Validate(valid));
		Assert.False(MuiBitmapGeometryStateAdmission.ValidateLive(ref platform,
			State, bitmap, valid));
		valid.Magic = MuiBitmapGeometryStateRecord.Cookie;
		Assert.False(MuiBitmapGeometryStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void BitmapGeometryRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1B00);
		var value = new MuiBitmapGeometryStateRecord
		{
			Magic = MuiBitmapGeometryStateRecord.Cookie,
			Width = uint.MaxValue,
			Height = 12,
		};

		Assert.True(MuiBitmapGeometryStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiBitmapGeometryStateRecordCodec.TryReadStructural(ref platform,
			address, out var structural));
		Assert.Equal(value.Magic, structural.Magic);
		Assert.Equal(value.Width, structural.Width);
		Assert.Equal(value.Height, structural.Height);
		Assert.True(MuiBitmapGeometryStateRecordCodec.TryRead(ref platform, address,
			out _));
		Assert.True(MuiBitmapGeometryStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiBitmapGeometryStateField.Height,
			out var typedHeightAddress));
		Assert.Equal(address.Raw + MuiBitmapGeometryStateRecord.HeightOffset,
			typedHeightAddress.Raw);
		var heightCursor = new MuiBitmapGeometryStateFieldCursor
		{
			Record = address,
			Field = MuiBitmapGeometryStateField.Height,
		};
		Assert.True(MuiBitmapGeometryStateFieldCursorCodec.TryGetAddress(
			ref platform, heightCursor, out var cursorHeightAddress,
			out var fieldSize));
		Assert.Equal(typedHeightAddress, cursorHeightAddress);
		Assert.Equal(MuiBitmapGeometryStateRecord.FieldSize, fieldSize);
		Assert.True(MuiBitmapGeometryStateRecordMemoryCodec.TryGetAddress(
			ref platform, heightCursor, out var memoryCursorAddress,
			out var memoryFieldSize));
		Assert.Equal(cursorHeightAddress, memoryCursorAddress);
		Assert.Equal(fieldSize, memoryFieldSize);
		Assert.True(MuiBitmapGeometryStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiBitmapGeometryStateField.Width,
			0x12345678));
		Assert.True(MuiBitmapGeometryStateRecordCodec.TryReadStructural(
			ref platform, address, out var typedDecoded));
		Assert.Equal(value.Magic, typedDecoded.Magic);
		Assert.Equal(0x12345678u, typedDecoded.Width);
		Assert.Equal(value.Height, typedDecoded.Height);
		Assert.True(MuiBitmapGeometryStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, 8u, out var lastField));
		Assert.Equal(address.Raw + 8, lastField.Raw);
		Assert.True(MuiBitmapGeometryStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4u, out var width));
		Assert.Equal(0x12345678u, width);
		Assert.False(MuiBitmapGeometryStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiBitmapGeometryStateRecord.Size, out _));
		Assert.False(MuiBitmapGeometryStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, 0u, out _));
		heightCursor.Record = APTR.Null;
		Assert.False(MuiBitmapGeometryStateFieldCursorCodec.TryGetAddress(
			ref platform, heightCursor, out _, out _));
		Assert.False(MuiBitmapGeometryStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void BitmapGeometrySequentialRecordPreservesFullUlongRangeAndBounds()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1B40);
		var value = new MuiBitmapGeometryStateRecord
		{
			Magic = MuiBitmapGeometryStateRecord.Cookie,
			Width = uint.MaxValue,
			Height = 0xCAFEBABEu,
		};

		Assert.True(MuiBitmapGeometryStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiBitmapGeometryStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Width, decoded.Width);
		Assert.Equal(value.Height, decoded.Height);

		var crossingEnd = APTR.FromPointer(0x40FFF);
		Assert.False(MuiBitmapGeometryStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiBitmapGeometryStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}

	[Fact]
	public void BitmapGeometryOffsetBridgeUsesNamedUlongCodec()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1C40);
		var initial = new MuiBitmapGeometryStateRecord
		{
			Magic = MuiBitmapGeometryStateRecord.Cookie,
			Width = 320,
			Height = 200,
		};

		Assert.True(MuiBitmapGeometryStateRecordCodec.WriteRecord(ref platform,
			address, initial));
		Assert.True(MuiBitmapGeometryStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiBitmapGeometryStateRecord.WidthOffset,
			0xF1020304u));
		Assert.True(MuiBitmapGeometryStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiBitmapGeometryStateRecord.WidthOffset,
			out var width));
		Assert.Equal(0xF1020304u, width);
		Assert.True(MuiBitmapGeometryStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(initial.Magic, decoded.Magic);
		Assert.Equal(0xF1020304u, decoded.Width);
		Assert.Equal(initial.Height, decoded.Height);
		Assert.False(MuiBitmapGeometryStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiBitmapGeometryStateRecord.Size, out _));
		Assert.False(MuiBitmapGeometryStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			APTR.Null, MuiBitmapGeometryStateRecord.HeightOffset, 1));
	}

	[Fact]
	public void MalformedBitmapGeometryFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var bitmapClass);
		var bitmap = MuiCommonControlCore.CreateControl(ref platform, State,
			bitmapClass, BuildTags(ref platform, 0x1A00, 32, 12));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			bitmap, BitmapWidth, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, bitmap,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiBitmapGeometryStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiBitmapGeometryStateField.Magic, 0));
		Assert.True(MuiBitmapGeometryStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiBitmapGeometryStateAdmission.Validate(structural));
		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetBitmapGeometryStateRecord(
			ref platform, State, bitmap, out _));
		Assert.False(MuiCommonControlCore.TryReadBitmapGeometryState(ref platform,
			State, bitmap, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, bitmap,
			BitmapWidth, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapWidth, 64));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			bitmap, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			bitmap, BitmapWidth, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiBitmapGeometryStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiBitmapGeometryStateField.Magic,
			out var preserved));
		Assert.Equal(0u, preserved);
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

	private static APTR BuildTags(ref MuiHeadlessTestPlatform platform,
		uint address, uint width, uint height)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, BitmapWidth);
		platform.WriteUInt32(tags, 4, width);
		platform.WriteUInt32(tags, 8, BitmapHeight);
		platform.WriteUInt32(tags, 12, height);
		platform.WriteUInt32(tags, 16, 0);
		platform.WriteUInt32(tags, 20, 0);
		return tags;
	}
}
