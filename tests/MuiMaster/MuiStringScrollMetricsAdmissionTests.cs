using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringScrollMetricsAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint StringContents = 0x80428FFD;
	private const uint StringScrollLeft = 0x8042BD0D;
	private const uint StateKey = MuiStringScrollAttributeCore.MetricsStateKey;

	[Fact]
	public void StringScrollMetricsAdmissionRequiresCanonicalRecordAndOwner()
	{
		var platform = CreatePlatform(out var stringClass, out var stringObj);
		Assert.True(MuiStringScrollAttributeCore.TryGetMetricsStateRecord(
			ref platform, State, stringObj, out var valid));
		Assert.True(MuiStringScrollMetricsStateAdmission.Validate(valid));
		Assert.True(MuiStringScrollMetricsStateAdmission.ValidateLive(ref platform,
			State, stringObj, valid));
		var malformed = valid;
		malformed.Magic = 0;
		Assert.False(MuiStringScrollMetricsStateAdmission.Validate(malformed));
		Assert.False(MuiStringScrollMetricsStateAdmission.ValidateLive(ref platform,
			State, stringObj, malformed));
		Assert.False(MuiStringScrollMetricsStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedStringScrollMetricsFailsClosedBeforeRawClampOrSet()
	{
		var platform = CreatePlatform(out _, out var stringObj);
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringScrollLeft, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, stringObj,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiStringScrollMetricsStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiStringScrollMetricsStateField.Magic, 0));
		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiStringScrollAttributeCore.Get(ref platform, State, stringObj,
			StringScrollLeft, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringScrollLeft, 999));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			stringObj, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringScrollLeft, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiStringScrollMetricsStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiStringScrollMetricsStateField.Magic,
			out var preserved));
		Assert.Equal(0u, preserved);
	}

	[Fact]
	public void StringScrollMetricsRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x8000,
			State);
		var recordAddress = APTR.FromPointer(0x1D80);
		var value = new MuiStringScrollMetricsStateRecord
		{
			Magic = MuiStringScrollMetricsStateRecord.Cookie,
			Width = 640,
			Height = 240,
			VisibleWidth = 320,
			VisibleHeight = 120,
			Left = 24,
			Top = 8,
		};
		Assert.True(MuiStringScrollMetricsStateRecordCodec.Write(ref platform,
			recordAddress, value));
		Assert.True(MuiStringScrollMetricsStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress, MuiStringScrollMetricsStateField.Left,
			out var typedLeftAddress));
		Assert.Equal(0x1D94u, typedLeftAddress.Raw);
		var leftCursor = new MuiStringScrollMetricsStateFieldCursor
		{
			Record = recordAddress,
			Field = MuiStringScrollMetricsStateField.Left,
		};
		Assert.True(MuiStringScrollMetricsStateFieldCursorCodec.TryGetAddress(ref platform,
			leftCursor, out var cursorLeftAddress, out var cursorFieldSize));
		Assert.Equal(typedLeftAddress, cursorLeftAddress);
		Assert.Equal(MuiStringScrollMetricsStateRecord.FieldSize, cursorFieldSize);
		Assert.True(MuiStringScrollMetricsStateRecordMemoryCodec.TryGetAddress(ref platform,
			leftCursor, out var memoryLeftAddress, out var memoryFieldSize));
		Assert.Equal(cursorLeftAddress, memoryLeftAddress);
		Assert.Equal(cursorFieldSize, memoryFieldSize);
		Assert.True(MuiStringScrollMetricsStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, MuiStringScrollMetricsStateField.Height,
			out var typedHeight));
		Assert.Equal(240u, typedHeight);
		Assert.True(MuiStringScrollMetricsStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, recordAddress, MuiStringScrollMetricsStateField.Top, 0));
		Assert.True(MuiStringScrollMetricsStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, MuiStringScrollMetricsStateField.Magic,
			out var typedMagic));
		Assert.Equal(MuiStringScrollMetricsStateRecord.Cookie, typedMagic);
		Assert.False(MuiStringScrollMetricsStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, (MuiStringScrollMetricsStateField)255,
			out _));
		Assert.True(MuiStringScrollMetricsStateRecordCodec.TryReadStructural(
			ref platform, recordAddress, out var typedUpdated));
		Assert.Equal(value.Width, typedUpdated.Width);
		Assert.Equal(value.Left, typedUpdated.Left);
		Assert.Equal(0u, typedUpdated.Top);
		Assert.True(MuiStringScrollMetricsStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress, 20, out var leftAddress));
		Assert.Equal(0x1D94u, leftAddress.Raw);
		Assert.True(MuiStringScrollMetricsStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, 8, out var height));
		Assert.Equal(240u, height);
		Assert.True(MuiStringScrollMetricsStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, recordAddress, 24, 0));
		Assert.True(MuiStringScrollMetricsStateRecordCodec.TryReadStructural(
			ref platform, recordAddress, out var decoded));
		Assert.Equal(0u, decoded.Top);
		Assert.False(MuiStringScrollMetricsStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress, MuiStringScrollMetricsStateRecord.Size,
			out _));
		Assert.False(MuiStringScrollMetricsStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, (uint)0, out _));
		Assert.False(MuiStringScrollMetricsStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
		leftCursor.Field = (MuiStringScrollMetricsStateField)255;
		Assert.False(MuiStringScrollMetricsStateFieldCursorCodec.TryGetAddress(ref platform,
			leftCursor, out _, out _));
		leftCursor.Record = APTR.Null;
		leftCursor.Field = MuiStringScrollMetricsStateField.Left;
		Assert.False(MuiStringScrollMetricsStateFieldCursorCodec.TryGetAddress(ref platform,
			leftCursor, out _, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR stringClass,
		out APTR stringObj)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x8000,
			State);
		var name = APTR.FromPointer(0x1100);
		var contents = APTR.FromPointer(0x1800);
		platform.WriteCString(name, "String.mui");
		platform.WriteCString(contents, "scroll content");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		stringClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 1, APTR.FromPointer(1), false);
		var tags = APTR.FromPointer(0x1900);
		platform.WriteUInt32(tags, 0, StringContents);
		platform.WriteUInt32(tags, 4, contents.Raw);
		platform.WriteUInt32(tags, 8, 0);
		platform.WriteUInt32(tags, 12, 0);
		stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, tags);
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, stringObj, 0, 0,
			40, 10));
		return platform;
	}
}
