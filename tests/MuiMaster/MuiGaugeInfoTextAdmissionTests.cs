using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiGaugeInfoTextAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint GaugeInfoText = 0x8042BF15;
	private const uint StateKey = 0x7F07001F;

	[Fact]
	public void GaugeInfoTextAdmissionRequiresCanonicalMagicMappedStringAndOwner()
	{
		var platform = CreatePlatform(out var gaugeClass, out var source);
		var gauge = MuiCommonControlCore.CreateControl(ref platform, State,
			gaugeClass, BuildTags(ref platform, 0x1900, source));
		Assert.True(MuiCommonControlCore.TryGetGaugeInfoTextStateRecord(
			ref platform, State, gauge, out var valid));
		Assert.True(MuiGaugeInfoTextStateAdmission.Validate(valid));
		Assert.True(MuiGaugeInfoTextStateAdmission.ValidateLive(ref platform, State,
			gauge, valid));

		var malformed = valid;
		malformed.InfoText = APTR.FromPointer(0x30000);
		Assert.True(MuiGaugeInfoTextStateAdmission.Validate(malformed));
		Assert.False(MuiGaugeInfoTextStateAdmission.ValidateLive(ref platform,
			State, gauge, malformed));
		malformed = valid;
		malformed.Magic = 0;
		Assert.False(MuiGaugeInfoTextStateAdmission.Validate(malformed));
		Assert.False(MuiGaugeInfoTextStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void GaugeInfoTextRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform(out _, out _);
		var address = APTR.FromPointer(0x1BC0);
		var infoText = APTR.FromPointer(0x1D20);
		platform.WriteCString(infoText, "%ld%%");
		var value = new MuiGaugeInfoTextStateRecord
		{
			Magic = MuiGaugeInfoTextStateRecord.Cookie,
			InfoText = infoText,
		};

		Assert.True(MuiGaugeInfoTextStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiGaugeInfoTextStateRecordCodec.TryReadStructural(
			ref platform, address, out var structural));
		Assert.Equal(value.Magic, structural.Magic);
		Assert.Equal(value.InfoText, structural.InfoText);
		Assert.True(MuiGaugeInfoTextStateRecordCodec.TryRead(ref platform, address,
			out _));
		Assert.True(MuiGaugeInfoTextStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiGaugeInfoTextStateField.InfoText,
			out var infoTextField));
		Assert.Equal(address.Raw + 4, infoTextField.Raw);
		Assert.True(MuiGaugeInfoTextStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiGaugeInfoTextStateField.InfoText,
			out var infoTextRaw));
		Assert.Equal(value.InfoText.Raw, infoTextRaw);
		Assert.True(MuiGaugeInfoTextStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiGaugeInfoTextStateField.InfoText,
			infoText.Raw));
		Assert.True(MuiGaugeInfoTextStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiGaugeInfoTextStateField.Magic,
			out var preservedMagic));
		Assert.Equal(value.Magic, preservedMagic);
		Assert.True(MuiGaugeInfoTextStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiGaugeInfoTextStateField.InfoText,
			out var typedInfoTextRaw));
		Assert.Equal(value.InfoText.Raw, typedInfoTextRaw);
		Assert.False(MuiGaugeInfoTextStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiGaugeInfoTextStateField)0xFF, out _));
		// The raw-offset overload remains bounded for legacy diagnostics.
		Assert.True(MuiGaugeInfoTextStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiGaugeInfoTextStateRecord.InfoTextOffset,
			out var legacyInfoTextRaw));
		Assert.Equal(value.InfoText.Raw, legacyInfoTextRaw);
		Assert.False(MuiGaugeInfoTextStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiGaugeInfoTextStateRecord.Size, out _));
		Assert.False(MuiGaugeInfoTextStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, (uint)0, out _));
		Assert.False(MuiGaugeInfoTextStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void GaugeInfoTextSequentialRecordPreservesPointerAndRejectsTruncation()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x8000,
			State);
		var address = APTR.FromPointer(0x1C00);
		var value = new MuiGaugeInfoTextStateRecord
		{
			Magic = MuiGaugeInfoTextStateRecord.Cookie,
			InfoText = APTR.FromPointer(0x2A00),
		};
		Assert.True(MuiGaugeInfoTextStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiGaugeInfoTextStateRecordCodec.TryReadRecord(ref platform,
			address, out var actual));
		Assert.Equal(value.Magic, actual.Magic);
		Assert.Equal(value.InfoText, actual.InfoText);
		Assert.False(MuiGaugeInfoTextStateRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x30FFC), out _));
	}

	[Fact]
	public void MalformedGaugeInfoTextFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var gaugeClass, out var source);
		var gauge = MuiCommonControlCore.CreateControl(ref platform, State,
			gaugeClass, BuildTags(ref platform, 0x1900, source));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			gauge, GaugeInfoText, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, gauge,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiGaugeInfoTextStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiGaugeInfoTextStateField.InfoText, 0x30000));
		Assert.True(MuiGaugeInfoTextStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0x30000u, structural.InfoText.Raw);
		Assert.True(MuiGaugeInfoTextStateAdmission.Validate(structural));
		Assert.False(MuiGaugeInfoTextStateAdmission.ValidateLive(ref platform,
			State, gauge, structural));

		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetGaugeInfoTextStateRecord(
			ref platform, State, gauge, out _));
		Assert.False(MuiCommonControlCore.TryReadGaugeInfoTextState(ref platform,
			State, gauge, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, gauge,
			GaugeInfoText, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			gauge, GaugeInfoText, 0));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			gauge, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			gauge, GaugeInfoText, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiGaugeInfoTextStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiGaugeInfoTextStateField.InfoText,
			out var preserved));
		Assert.Equal(0x30000u, preserved);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR gaugeClass,
		out APTR source)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		source = APTR.FromPointer(0x1800);
		platform.WriteCString(name, "Gauge.mui");
		platform.WriteCString(source, "%ld%%");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		gaugeClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR BuildTags(ref MuiHeadlessTestPlatform platform,
		uint address, APTR source)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, GaugeInfoText);
		platform.WriteUInt32(tags, 4, source.Raw);
		platform.WriteUInt32(tags, 8, 0);
		platform.WriteUInt32(tags, 12, 0);
		return tags;
	}
}
