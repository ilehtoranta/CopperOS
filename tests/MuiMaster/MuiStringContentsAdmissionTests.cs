using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringContentsAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint StringContents = 0x80428FFD;
	private const uint StateKey = 0x7F07001A;

	[Fact]
	public void StringContentsAdmissionRequiresCanonicalMagicMappedContentsAndOwner()
	{
		var platform = CreatePlatform(out var stringClass, out var source);
		var stringObject = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, source));
		Assert.True(MuiCommonControlCore.TryGetStringContentsStateRecord(
			ref platform, State, stringObject, out var valid));
		Assert.True(MuiStringContentsStateAdmission.Validate(valid));
		Assert.True(MuiStringContentsStateAdmission.ValidateLive(ref platform,
			State, stringObject, valid));

		var malformed = valid;
		malformed.Contents = APTR.FromPointer(0x30000);
		Assert.True(MuiStringContentsStateAdmission.Validate(malformed));
		Assert.False(MuiStringContentsStateAdmission.ValidateLive(ref platform,
			State, stringObject, malformed));

		malformed = valid;
		malformed.Magic = 0;
		Assert.False(MuiStringContentsStateAdmission.Validate(malformed));
		Assert.False(MuiStringContentsStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedStringContentsFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var stringClass, out var source);
		var stringObject = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, source));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObject, StringContents, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, stringObject,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiStringContentsStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiStringContentsStateField.Contents, 0x30000));
		Assert.True(MuiStringContentsStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0x30000u, structural.Contents.Raw);
		Assert.True(MuiStringContentsStateAdmission.Validate(structural));
		Assert.False(MuiStringContentsStateAdmission.ValidateLive(ref platform,
			State, stringObject, structural));

		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetStringContentsStateRecord(
			ref platform, State, stringObject, out _));
		Assert.False(MuiCommonControlCore.TryReadStringContentsState(
			ref platform, State, stringObject, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State,
			stringObject, StringContents, out _, out _));
		var replacement = APTR.FromPointer(0x1C00);
		platform.WriteCString(replacement, "replacement");
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObject, StringContents, replacement.Raw));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			stringObject, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObject, StringContents, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiStringContentsStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiStringContentsStateField.Contents,
			out var preserved));
		Assert.Equal(0x30000u, preserved);
	}

	[Fact]
	public void StringContentsRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform(out _, out var source);
		var address = APTR.FromPointer(0x1D60);
		var record = new MuiStringContentsStateRecord
		{
			Magic = MuiStringContentsStateRecord.Cookie,
			Contents = source,
		};
		Assert.True(MuiStringContentsStateRecordCodec.Write(ref platform, address,
			record));
		Assert.True(MuiStringContentsStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiStringContentsStateField.Contents,
			out var typedContentsAddress));
		Assert.Equal(0x1D64u, typedContentsAddress.Raw);
		Assert.True(MuiStringContentsStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiStringContentsStateField.Contents,
			out var typedContents));
		Assert.Equal(source.Raw, typedContents);
		Assert.True(MuiStringContentsStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiStringContentsStateField.Contents, 0));
		Assert.True(MuiStringContentsStateRecordCodec.TryReadStructural(ref platform,
			address, out var typedUpdated));
		Assert.Equal(APTR.Null, typedUpdated.Contents);
		Assert.False(MuiStringContentsStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiStringContentsStateField)255, out _));
		Assert.True(MuiStringContentsStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiStringContentsStateField.Contents, source.Raw));
		Assert.True(MuiStringContentsStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, 4, out var contentsAddress));
		Assert.Equal(0x1D64u, contentsAddress.Raw);
		Assert.True(MuiStringContentsStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4, out var contents));
		Assert.Equal(source.Raw, contents);
		Assert.True(MuiStringContentsStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 4, 0));
		Assert.True(MuiStringContentsStateRecordCodec.TryReadStructural(ref platform,
			address, out var updated));
		Assert.Equal(APTR.Null, updated.Contents);
		Assert.False(MuiStringContentsStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiStringContentsStateRecord.Size, out _));
		Assert.False(MuiStringContentsStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, (uint)0, out _));
		Assert.False(MuiStringContentsStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR stringClass,
		out APTR source)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		source = APTR.FromPointer(0x1800);
		platform.WriteCString(name, "String.mui");
		platform.WriteCString(source, "initial contents");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		stringClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR BuildTags(ref MuiHeadlessTestPlatform platform, uint address,
		APTR source)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, StringContents);
		platform.WriteUInt32(tags, 4, source.Raw);
		platform.WriteUInt32(tags, 8, 0);
		platform.WriteUInt32(tags, 12, 0);
		return tags;
	}
}
