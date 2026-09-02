using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringPresentationAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint StringMaxLen = 0x80424984;
	private const uint StringSecret = 0x80428769;
	private const uint StringFormat = 0x80427484;
	private const uint Unicode = 0x8042E7D0;
	private const uint StateKey = 0x7F070011;

	[Fact]
	public void StringPresentationAdmissionRequiresCanonicalValuesAndOwner()
	{
		var platform = CreatePlatform(out var stringClass);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, 32, 1, 2, 1));
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiCommonControlCore.TryGetStringPresentationStateRecord(
			ref platform, State, stringObj, out var valid));
		Assert.True(MuiStringPresentationStateAdmission.Validate(valid));
		Assert.True(MuiStringPresentationStateAdmission.ValidateLive(ref platform,
			State, stringObj, valid));

		var malformed = valid;
		malformed.MaxLen = uint.MaxValue;
		Assert.False(MuiStringPresentationStateAdmission.Validate(malformed));
		malformed = valid;
		malformed.Secret = 2;
		Assert.False(MuiStringPresentationStateAdmission.Validate(malformed));
		malformed = valid;
		malformed.Format = 3;
		Assert.False(MuiStringPresentationStateAdmission.Validate(malformed));
		malformed = valid;
		malformed.Unicode = 2;
		Assert.False(MuiStringPresentationStateAdmission.Validate(malformed));
		malformed = valid;
		malformed.Magic = 0;
		Assert.False(MuiStringPresentationStateAdmission.Validate(malformed));
		Assert.False(MuiStringPresentationStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedStringPresentationFailsClosedBeforeRawRepairOrGet()
	{
		var platform = CreatePlatform(out var stringClass);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, 32, 1, 2, 1));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringFormat, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, stringObj,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiStringPresentationStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiStringPresentationStateField.Format, 3));
		Assert.True(MuiStringPresentationStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(3u, structural.Format);
		Assert.False(MuiStringPresentationStateAdmission.Validate(structural));
		Assert.False(MuiStringPresentationStateAdmission.ValidateLive(ref platform,
			State, stringObj, structural));

		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetStringPresentationStateRecord(
			ref platform, State, stringObj, out _));
		Assert.False(MuiCommonControlCore.TryReadStringPresentationState(ref platform,
			State, stringObj, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			StringFormat, out _, out _));
		var source = APTR.FromPointer(0x1A00);
		platform.WriteCString(source, "blocked");
		Assert.False(MuiCommonControlCore.SetPersistenceContents(ref platform, State,
			stringObj, MuiCommonControlCore.StringContents, source));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			stringObj, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringFormat, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiStringPresentationStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiStringPresentationStateField.Format,
			out var preserved));
		Assert.Equal(3u, preserved);
	}

	[Fact]
	public void StringPresentationRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var recordAddress = APTR.FromPointer(0x1D40);
		var value = new MuiStringPresentationStateRecord
		{
			Magic = MuiStringPresentationStateRecord.Cookie,
			MaxLen = 32,
			Secret = 1,
			Format = 2,
			Unicode = 1,
		};
		Assert.True(MuiStringPresentationStateRecordCodec.Write(ref platform,
			recordAddress, value));
		Assert.True(MuiStringPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress, MuiStringPresentationStateField.Format,
			out var typedFormatAddress));
		Assert.Equal(0x1D4Cu, typedFormatAddress.Raw);
		Assert.True(MuiStringPresentationStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, MuiStringPresentationStateField.MaxLen,
			out var typedMaxLen));
		Assert.Equal(32u, typedMaxLen);
		Assert.True(MuiStringPresentationStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, recordAddress, MuiStringPresentationStateField.Unicode, 0));
		Assert.True(MuiStringPresentationStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, MuiStringPresentationStateField.Magic,
			out var typedMagic));
		Assert.Equal(MuiStringPresentationStateRecord.Cookie, typedMagic);
		Assert.False(MuiStringPresentationStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, (MuiStringPresentationStateField)255, out _));
		Assert.True(MuiStringPresentationStateRecordCodec.TryReadStructural(
			ref platform, recordAddress, out var typedUpdated));
		Assert.Equal(MuiStringPresentationStateRecord.Cookie, typedUpdated.Magic);
		Assert.Equal(32u, typedUpdated.MaxLen);
		Assert.Equal(0u, typedUpdated.Unicode);
		Assert.True(MuiStringPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress, 12, out var formatAddress));
		Assert.Equal(0x1D4Cu, formatAddress.Raw);
		Assert.True(MuiStringPresentationStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, 4, out var maxLen));
		Assert.Equal(32u, maxLen);
		Assert.True(MuiStringPresentationStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, recordAddress, 16, 0));
		Assert.True(MuiStringPresentationStateRecordCodec.TryReadStructural(
			ref platform, recordAddress, out var decoded));
		Assert.Equal(0u, decoded.Unicode);
		Assert.False(MuiStringPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress, MuiStringPresentationStateRecord.Size,
			out _));
		Assert.False(MuiStringPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, (uint)0, out _));
		Assert.False(MuiStringPresentationStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR stringClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "String.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		stringClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR BuildTags(ref MuiHeadlessTestPlatform platform,
		uint address, uint maxLen, uint secret, uint format, uint unicode)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, StringMaxLen);
		platform.WriteUInt32(tags, 4, maxLen);
		platform.WriteUInt32(tags, 8, StringSecret);
		platform.WriteUInt32(tags, 12, secret);
		platform.WriteUInt32(tags, 16, StringFormat);
		platform.WriteUInt32(tags, 20, format);
		platform.WriteUInt32(tags, 24, Unicode);
		platform.WriteUInt32(tags, 28, unicode);
		platform.WriteUInt32(tags, 32, 0);
		platform.WriteUInt32(tags, 36, 0);
		return tags;
	}
}
