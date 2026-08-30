using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiLevelmeterLabelAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint LevelmeterLabel = 0x80420DD5;
	private const uint StateKey = 0x7F070020;

	[Fact]
	public void LevelmeterLabelAdmissionRequiresCanonicalMagicMappedStringAndOwner()
	{
		var platform = CreatePlatform(out var levelmeterClass, out var source);
		var levelmeter = MuiCommonControlCore.CreateControl(ref platform, State,
			levelmeterClass, BuildTags(ref platform, 0x1900, source));
		Assert.True(MuiCommonControlCore.TryGetLevelmeterLabelStateRecord(
			ref platform, State, levelmeter, out var valid));
		Assert.True(MuiLevelmeterLabelStateAdmission.Validate(valid));
		Assert.True(MuiLevelmeterLabelStateAdmission.ValidateLive(ref platform, State,
			levelmeter, valid));

		var malformed = valid;
		malformed.Label = APTR.FromPointer(0x30000);
		Assert.True(MuiLevelmeterLabelStateAdmission.Validate(malformed));
		Assert.False(MuiLevelmeterLabelStateAdmission.ValidateLive(ref platform,
			State, levelmeter, malformed));
		malformed = valid;
		malformed.Magic = 0;
		Assert.False(MuiLevelmeterLabelStateAdmission.Validate(malformed));
		Assert.False(MuiLevelmeterLabelStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedLevelmeterLabelFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var levelmeterClass, out var source);
		var levelmeter = MuiCommonControlCore.CreateControl(ref platform, State,
			levelmeterClass, BuildTags(ref platform, 0x1900, source));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			levelmeter, LevelmeterLabel, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, levelmeter,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiLevelmeterLabelStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiLevelmeterLabelStateField.Label, 0x30000));
		Assert.True(MuiLevelmeterLabelStateRecordCodec.TryReadRecord(
			ref platform, block, out var structural));
		Assert.Equal(0x30000u, structural.Label.Raw);
		Assert.True(MuiLevelmeterLabelStateAdmission.Validate(structural));
		Assert.False(MuiLevelmeterLabelStateAdmission.ValidateLive(ref platform,
			State, levelmeter, structural));

		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetLevelmeterLabelStateRecord(
			ref platform, State, levelmeter, out _));
		Assert.False(MuiCommonControlCore.TryReadLevelmeterLabelState(ref platform,
			State, levelmeter, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, levelmeter,
			LevelmeterLabel, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			levelmeter, LevelmeterLabel, 0));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			levelmeter, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			levelmeter, LevelmeterLabel, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiLevelmeterLabelStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiLevelmeterLabelStateField.Label,
			out var preserved));
		Assert.Equal(0x30000u, preserved);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(
		out APTR levelmeterClass, out APTR source)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		source = APTR.FromPointer(0x1800);
		platform.WriteCString(name, "Levelmeter.mui");
		platform.WriteCString(source, "load");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		levelmeterClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR BuildTags(ref MuiHeadlessTestPlatform platform,
		uint address, APTR source)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, LevelmeterLabel);
		platform.WriteUInt32(tags, 4, source.Raw);
		platform.WriteUInt32(tags, 8, 0);
		platform.WriteUInt32(tags, 12, 0);
		return tags;
	}
}
