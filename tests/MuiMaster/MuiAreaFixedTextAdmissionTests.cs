using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaFixedTextAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint FixWidthTxt = 0x8042D044;
	private const uint FixHeightTxt = 0x804276F2;

	[Fact]
	public void FixedTextStringByteCursorUsesBoundedNamedReads()
	{
		var platform = CreatePlatform(out _);
		var text = APTR.FromPointer(0x1380);
		platform.WriteCString(text, "wide");
		var cursor = default(MuiAreaFixedTextStringByteCursor);
		cursor.Text = text;
		cursor.Index = 1;
		Assert.True(MuiAreaFixedTextStringByteCursorCodec.TryReadByte(ref platform,
			cursor, out var value));
		Assert.Equal((byte)'i', value);
		Assert.True(MuiAreaFixedTextStringByteCursorCodec.TryReadAt(ref platform,
			text, 4, out value));
		Assert.Equal(0, value);
		Assert.False(MuiAreaFixedTextStringByteCursorCodec.TryReadAt(ref platform,
			text, -1, out _));
		Assert.False(MuiAreaFixedTextStringByteCursorCodec.TryReadAt(ref platform,
			text, (int)MuiAreaFixedTextStringByteCursor.MaximumLength, out _));
		Assert.False(MuiAreaFixedTextStringByteCursorCodec.TryReadAt(ref platform,
			APTR.FromPointer(0xFFFFFFFEu), 2, out _));
		Assert.False(MuiAreaFixedTextStringByteCursorCodec.TryReadAt(ref platform,
			APTR.FromPointer(0x30000), 0, out _));
	}

	[Fact]
	public void FixedTextAdmissionRequiresGenerationLiveOwnerAndMappedSamples()
	{
		var platform = CreatePlatform(out var textClass);
		var width = APTR.FromPointer(0x1400);
		var height = APTR.FromPointer(0x1440);
		platform.WriteCString(width, "wide");
		platform.WriteCString(height, "high");
		var obj = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, APTR.Null);
		var valid = new MuiAreaFixedTextStateRecord
		{
			Magic = MuiAreaFixedTextStateRecord.Cookie,
			WidthText = width,
			HeightText = height,
			Generation = 1,
		};

		Assert.True(MuiAreaFixedTextStateAdmission.Validate(valid));
		Assert.True(MuiAreaFixedTextStateAdmission.ValidateLive(ref platform,
			State, obj, valid));

		var malformed = valid;
		malformed.Generation = 0;
		Assert.False(MuiAreaFixedTextStateAdmission.Validate(malformed));
		Assert.False(MuiAreaFixedTextStateAdmission.ValidateLive(ref platform,
			State, obj, malformed));
		malformed = valid;
		malformed.WidthText = APTR.FromPointer(0x30000);
		Assert.False(MuiAreaFixedTextStateAdmission.ValidateLive(ref platform,
			State, obj, malformed));
		Assert.False(MuiAreaFixedTextStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void FixedTextRecordUsesDedicatedStructCodec()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1540);
		var value = default(MuiAreaFixedTextStateRecord);
		value.Magic = MuiAreaFixedTextStateRecord.Cookie;
		value.WidthText = APTR.FromPointer(0x1A00);
		value.HeightText = APTR.FromPointer(0x1B00);
		value.Generation = 7;

		Assert.True(MuiAreaFixedTextStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaFixedTextStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.WidthText, decoded.WidthText);
		Assert.Equal(value.HeightText, decoded.HeightText);
		Assert.Equal(value.Generation, decoded.Generation);
		Assert.True(MuiAreaFixedTextStateRecordCodec.TryRead(ref platform, address,
			out decoded));
		Assert.False(MuiAreaFixedTextStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void MalformedFixedTextFailsClosedBeforeCopyOrMeasurement()
	{
		var platform = CreatePlatform(out var textClass);
		var widthSource = APTR.FromPointer(0x1800);
		var heightSource = APTR.FromPointer(0x1840);
		platform.WriteCString(widthSource, "1234");
		platform.WriteCString(heightSource, "5678");
		var tags = APTR.FromPointer(0x1880);
		platform.WriteUInt32(tags, 0, FixWidthTxt);
		platform.WriteUInt32(tags, 4, widthSource.Raw);
		platform.WriteUInt32(tags, 8, FixHeightTxt);
		platform.WriteUInt32(tags, 12, heightSource.Raw);
		platform.WriteUInt32(tags, 16, 0);
		platform.WriteUInt32(tags, 20, 0);
		var obj = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, tags);
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			obj, FixWidthTxt, out var widthBefore));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			obj, FixHeightTxt, out var heightBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, obj,
			MuiAreaFixedTextCore.StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiAreaFixedTextStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiAreaFixedTextStateField.Generation, 0));
		Assert.True(MuiAreaFixedTextStateRecordCodec.TryReadStructural(ref platform,
			block, out var structural));
		Assert.Equal(0u, structural.Generation);
		Assert.False(MuiAreaFixedTextStateAdmission.Validate(structural));
		Assert.False(MuiAreaFixedTextStateRecordCodec.TryRead(ref platform, block,
			out _));

		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiAreaFixedTextCore.Initialize(ref platform, State, obj));
		Assert.False(MuiAreaFixedTextCore.TryReadState(ref platform, State, obj,
			out _));
		Assert.False(MuiAreaFixedTextCore.TryMeasure(ref platform, State, obj,
			true, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, obj,
			FixWidthTxt, out _, out _));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, obj,
			MuiAreaFixedTextCore.StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			obj, FixWidthTxt, out var widthAfter));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			obj, FixHeightTxt, out var heightAfter));
		Assert.Equal(widthBefore, widthAfter);
		Assert.Equal(heightBefore, heightAfter);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR textClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Text.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		textClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
