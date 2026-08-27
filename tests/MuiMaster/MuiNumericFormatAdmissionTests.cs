using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNumericFormatAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint NumericFormat = 0x804263E9;
	private const uint StateKey = 0x7F07001E;

	[Fact]
	public void NumericFormatAdmissionRequiresCanonicalMagicMappedStringAndOwner()
	{
		var platform = CreatePlatform(out var numericClass, out var source);
		var numeric = MuiCommonControlCore.CreateControl(ref platform, State,
			numericClass, BuildTags(ref platform, 0x1900, source));
		Assert.True(MuiCommonControlCore.TryGetNumericFormatStateRecord(
			ref platform, State, numeric, out var valid));
		Assert.True(MuiNumericFormatStateAdmission.Validate(valid));
		Assert.True(MuiNumericFormatStateAdmission.ValidateLive(ref platform, State,
			numeric, valid));

		var malformed = valid;
		malformed.Format = APTR.FromPointer(0x30000);
		Assert.True(MuiNumericFormatStateAdmission.Validate(malformed));
		Assert.False(MuiNumericFormatStateAdmission.ValidateLive(ref platform,
			State, numeric, malformed));
		malformed = valid;
		malformed.Magic = 0;
		Assert.False(MuiNumericFormatStateAdmission.Validate(malformed));
		Assert.False(MuiNumericFormatStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedNumericFormatFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var numericClass, out var source);
		var numeric = MuiCommonControlCore.CreateControl(ref platform, State,
			numericClass, BuildTags(ref platform, 0x1900, source));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			numeric, NumericFormat, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, numeric,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiNumericFormatStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiNumericFormatStateField.Format, 0x30000));
		Assert.True(MuiNumericFormatStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0x30000u, structural.Format.Raw);
		Assert.True(MuiNumericFormatStateAdmission.Validate(structural));
		Assert.False(MuiNumericFormatStateAdmission.ValidateLive(ref platform,
			State, numeric, structural));

		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetNumericFormatStateRecord(
			ref platform, State, numeric, out _));
		Assert.False(MuiCommonControlCore.TryReadNumericFormatState(ref platform,
			State, numeric, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, numeric,
			NumericFormat, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			numeric, NumericFormat, 0));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			numeric, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			numeric, NumericFormat, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiNumericFormatStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiNumericFormatStateField.Format,
			out var preserved));
		Assert.Equal(0x30000u, preserved);
	}

	[Fact]
	public void NumericFormatRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform(out _, out var source);
		var address = APTR.FromPointer(0x1A00);
		var record = new MuiNumericFormatStateRecord
		{
			Magic = MuiNumericFormatStateRecord.Cookie,
			Format = source,
		};
		Assert.True(MuiNumericFormatStateRecordCodec.Write(ref platform, address,
			record));
		Assert.True(MuiNumericFormatStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, 4, out var formatAddress));
		Assert.Equal(0x1A04u, formatAddress.Raw);
		Assert.True(MuiNumericFormatStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out var format));
		Assert.Equal(source.Raw, format);
		Assert.True(MuiNumericFormatStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 4, 0));
		Assert.True(MuiNumericFormatStateRecordCodec.TryReadStructural(ref platform,
			address, out var updated));
		Assert.True(updated.Format.IsNull);
		Assert.False(MuiNumericFormatStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiNumericFormatStateRecord.Size, out _));
		Assert.False(MuiNumericFormatStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, 0, out _));
		Assert.False(MuiNumericFormatStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR numericClass,
		out APTR source)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		source = APTR.FromPointer(0x1800);
		platform.WriteCString(name, "Numeric.mui");
		platform.WriteCString(source, "%ld");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		numericClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR BuildTags(ref MuiHeadlessTestPlatform platform,
		uint address, APTR source)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, NumericFormat);
		platform.WriteUInt32(tags, 4, source.Raw);
		platform.WriteUInt32(tags, 8, 0);
		platform.WriteUInt32(tags, 12, 0);
		return tags;
	}
}
