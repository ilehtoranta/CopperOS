using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiRectangleBarTitleAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint RectangleBarTitle = 0x80426689;
	private const uint StateKey = 0x7F070024;

	[Fact]
	public void RectangleBarTitleAdmissionRequiresCanonicalPresenceMappedTitleAndOwner()
	{
		var platform = CreatePlatform(out var rectangleClass, out var title);
		var rectangle = MuiCommonControlCore.CreateControl(ref platform, State,
			rectangleClass, BuildTags(ref platform, 0x1900, title));
		var valid = new MuiRectangleBarTitleStateRecord
		{
			Magic = MuiRectangleBarTitleStateRecord.Cookie,
			Present = 1,
			Title = title,
		};
		Assert.True(MuiRectangleBarTitleStateAdmission.Validate(valid));
		Assert.True(MuiRectangleBarTitleStateAdmission.ValidateLive(ref platform,
			State, rectangle, valid));

		var malformed = valid;
		malformed.Present = 2;
		Assert.False(MuiRectangleBarTitleStateAdmission.Validate(malformed));
		Assert.False(MuiRectangleBarTitleStateAdmission.ValidateLive(ref platform,
			State, rectangle, malformed));

		malformed = valid;
		malformed.Present = 0;
		malformed.Title = APTR.FromPointer(0x30000);
		Assert.True(MuiRectangleBarTitleStateAdmission.Validate(malformed));
		Assert.False(MuiRectangleBarTitleStateAdmission.ValidateLive(ref platform,
			State, rectangle, malformed));
		Assert.False(MuiRectangleBarTitleStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedRectangleBarTitleFailsClosedBeforeRawRepairOrGet()
	{
		var platform = CreatePlatform(out var rectangleClass, out var title);
		var rectangle = MuiCommonControlCore.CreateControl(ref platform, State,
			rectangleClass, BuildTags(ref platform, 0x1900, title));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			rectangle, RectangleBarTitle, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, rectangle,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiRectangleBarTitleStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiRectangleBarTitleStateField.Present, 2));
		Assert.True(MuiRectangleBarTitleStateRecordCodec.TryReadRecord(
			ref platform, block, out var structural));
		Assert.Equal(2u, structural.Present);
		Assert.False(MuiRectangleBarTitleStateAdmission.Validate(structural));
		Assert.False(MuiRectangleBarTitleStateRecordCodec.TryRead(ref platform,
			block, out _));

		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetRectangleBarTitleStateRecord(
			ref platform, State, rectangle, out _));
		Assert.False(MuiCommonControlCore.TryReadRectangleBarTitleState(
			ref platform, State, rectangle, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, rectangle,
			RectangleBarTitle, out _, out _));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			rectangle, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			rectangle, RectangleBarTitle, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiRectangleBarTitleStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiRectangleBarTitleStateField.Present,
			out var preserved));
		Assert.Equal(2u, preserved);
	}

	[Fact]
	public void RectangleBarTitleRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform(out _, out var title);
		var address = APTR.FromPointer(0x1A20);
		var record = new MuiRectangleBarTitleStateRecord
		{
			Magic = MuiRectangleBarTitleStateRecord.Cookie,
			Present = 1,
			Title = title,
		};
		Assert.True(MuiRectangleBarTitleStateRecordCodec.WriteRecord(ref platform, address,
			record));
		Assert.True(MuiRectangleBarTitleStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiRectangleBarTitleStateField.Title,
			out var typedTitleAddress));
		Assert.Equal(0x1A28u, typedTitleAddress.Raw);
		Assert.True(MuiRectangleBarTitleStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiRectangleBarTitleStateField.Present,
			out var typedPresent));
		Assert.Equal(1u, typedPresent);
		Assert.True(MuiRectangleBarTitleStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiRectangleBarTitleStateField.Present, 0));
		Assert.True(MuiRectangleBarTitleStateRecordCodec.TryReadStructural(ref platform,
			address, out var typedUpdated));
		Assert.Equal(0u, typedUpdated.Present);
		Assert.Equal(record.Title, typedUpdated.Title);
		Assert.True(MuiRectangleBarTitleStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiRectangleBarTitleStateField.Present, 1));
		Assert.False(MuiRectangleBarTitleStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiRectangleBarTitleStateField)0xFF, out _));
		Assert.True(MuiRectangleBarTitleStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, 8, out var titleAddress));
		Assert.Equal(0x1A28u, titleAddress.Raw);
		Assert.True(MuiRectangleBarTitleStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4, out var present));
		Assert.Equal(1u, present);
		Assert.True(MuiRectangleBarTitleStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 4, 0));
		Assert.True(MuiRectangleBarTitleStateRecordCodec.TryReadRecord(ref platform,
			address, out var updated));
		Assert.Equal(0u, updated.Present);
		Assert.False(MuiRectangleBarTitleStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiRectangleBarTitleStateRecord.Size, out _));
		Assert.False(MuiRectangleBarTitleStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, (uint)0, out _));
		Assert.False(MuiRectangleBarTitleStateRecordCodec.TryReadRecord(ref platform,
			APTR.Null, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR rectangleClass,
		out APTR title)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		title = APTR.FromPointer(0x1800);
		platform.WriteCString(name, "Rectangle.mui");
		platform.WriteCString(title, "Bar title");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		rectangleClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR BuildTags(ref MuiHeadlessTestPlatform platform, uint address,
		APTR title)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, RectangleBarTitle);
		platform.WriteUInt32(tags, 4, title.Raw);
		platform.WriteUInt32(tags, 8, 0);
		platform.WriteUInt32(tags, 12, 0);
		return tags;
	}
}
