using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiGaugeInfoRateTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint GaugeInfoRate = 0x804253C8u;
	private const uint GaugeInfoRateStateKey = 0x7F070075u;

	[Fact]
	public void GaugeInfoRatePreservesSignedLongThroughConstructionAndSetGet()
	{
		var platform = CreatePlatform(out var gaugeClass);
		var gauge = MuiCommonControlCore.CreateControl(ref platform, State,
			gaugeClass, BuildTags(ref platform, 0x1400, unchecked((uint)-42)));
		Assert.True(gauge.IsNotNull);

		Assert.True(MuiCommonControlCore.TryGetGaugeInfoRateStateRecord(
			ref platform, State, gauge, out var initial));
		Assert.Equal(MuiGaugeInfoRateStateRecord.Cookie, initial.Magic);
		Assert.Equal(-42, initial.InfoRate);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, gauge,
			GaugeInfoRate, out var initialRaw));
		Assert.Equal(unchecked((uint)-42), initialRaw);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			gauge, GaugeInfoRate, unchecked((uint)-7)));
		Assert.True(MuiCommonControlCore.TryReadGaugeInfoRateState(ref platform,
			State, gauge, out var changed));
		Assert.Equal(-7, changed.InfoRate);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, gauge,
			GaugeInfoRate, out var projected, out var handled));
		Assert.True(handled);
		Assert.Equal(unchecked((uint)-7), projected);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, gauge,
			GaugeInfoRate, out var getter));
		Assert.Equal(unchecked((uint)-7), getter);
	}

	[Fact]
	public void GaugeInfoRateMalformedRecordFailsClosed()
	{
		var platform = CreatePlatform(out var gaugeClass);
		var gauge = MuiCommonControlCore.CreateControl(ref platform, State,
			gaugeClass, BuildTags(ref platform, 0x1500, 12));
		Assert.True(gauge.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			gauge, GaugeInfoRate, out var rawBefore));

		var block = MuiStoreCore.DataspaceFind(ref platform, State, gauge,
			GaugeInfoRateStateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiGaugeInfoRateStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiGaugeInfoRateStateField.Magic, 0));
		Assert.True(MuiGaugeInfoRateStateRecordCodec.TryReadStructural(ref platform,
			block, out var malformed));
		Assert.Equal(0u, malformed.Magic);
		Assert.False(MuiGaugeInfoRateStateRecordCodec.TryRead(ref platform, block,
			out _));
		Assert.False(MuiCommonControlCore.TryReadGaugeInfoRateState(ref platform,
			State, gauge, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, gauge,
			GaugeInfoRate, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			gauge, GaugeInfoRate, 100));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			gauge, GaugeInfoRate, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
	}

	[Fact]
	public void GaugeInfoRateCodecUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x8000,
			State);
		var address = APTR.FromPointer(0x1800);
		var record = new MuiGaugeInfoRateStateRecord
		{
			Magic = MuiGaugeInfoRateStateRecord.Cookie,
			InfoRate = -123,
		};
		Assert.True(MuiGaugeInfoRateStateRecordCodec.Write(ref platform, address,
			record));
		Assert.True(MuiGaugeInfoRateStateFieldCursorCodec.TryGetAddress(
			ref platform, new MuiGaugeInfoRateStateFieldCursor
			{
				Record = address,
				Field = MuiGaugeInfoRateStateField.InfoRate,
			}, out var infoRateAddress));
		Assert.Equal(0x1804u, infoRateAddress.Raw);
		Assert.True(MuiGaugeInfoRateStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiGaugeInfoRateStateField.InfoRate,
			unchecked((uint)-7)));
		Assert.True(MuiGaugeInfoRateStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiGaugeInfoRateStateField.Magic,
			out var preservedMagic));
		Assert.Equal(record.Magic, preservedMagic);
		Assert.True(MuiGaugeInfoRateStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiGaugeInfoRateStateField.InfoRate,
			out var infoRateRaw));
		Assert.Equal(unchecked((uint)-7), infoRateRaw);
		Assert.True(MuiGaugeInfoRateStateRecordCodec.TryReadStructural(ref platform,
			address, out var read));
		Assert.Equal(-7, read.InfoRate);
		Assert.False(MuiGaugeInfoRateStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
		Assert.False(MuiGaugeInfoRateStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiGaugeInfoRateStateField)0xFF, out _));
		Assert.False(MuiGaugeInfoRateStateFieldCursorCodec.TryGetAddress(
			ref platform, new MuiGaugeInfoRateStateFieldCursor
			{
				Record = address,
				Field = (MuiGaugeInfoRateStateField)0xFF,
			}, out _));
	}

	[Fact]
	public void GaugeInfoRateSequentialRecordPreservesSignedLongAndRejectsTruncation()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x8000,
			State);
		var address = APTR.FromPointer(0x1A00);
		var expected = new MuiGaugeInfoRateStateRecord
		{
			Magic = MuiGaugeInfoRateStateRecord.Cookie,
			InfoRate = int.MinValue,
		};
		Assert.True(MuiGaugeInfoRateStateRecordCodec.WriteRecord(ref platform,
			address, expected));
		Assert.True(MuiGaugeInfoRateStateRecordCodec.TryReadRecord(ref platform,
			address, out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.InfoRate, actual.InfoRate);
		Assert.False(MuiGaugeInfoRateStateRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x30FFC), out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR gaugeClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x8000,
			State);
		var className = APTR.FromPointer(0x1100);
		platform.WriteCString(className, "Gauge.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		gaugeClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			className, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR BuildTags(ref MuiHeadlessTestPlatform platform,
		uint address, uint infoRate)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, GaugeInfoRate);
		platform.WriteUInt32(tags, 4, infoRate);
		platform.WriteUInt32(tags, 8, MuiAslTagListCore.TagDone);
		platform.WriteUInt32(tags, 12, 0);
		return tags;
	}
}
