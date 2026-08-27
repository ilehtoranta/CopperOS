using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiControlFontAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint Font = 0x8042BE50;
	private const uint StateKey = 0x7F070025;

	[Fact]
	public void ControlFontAdmissionRequiresCanonicalPresenceAndLiveOwner()
	{
		var platform = CreatePlatform(out var textClass);
		var text = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, APTR.Null);
		var valid = new MuiControlFontStateRecord
		{
			Magic = MuiControlFontStateRecord.Cookie,
			Present = 1,
			Font = APTR.FromPointer(0x1400),
		};
		Assert.True(MuiControlFontStateAdmission.Validate(valid));
		Assert.True(MuiControlFontStateAdmission.ValidateLive(ref platform, State,
			text, valid));
		var malformed = valid;
		malformed.Present = 2;
		Assert.False(MuiControlFontStateAdmission.Validate(malformed));
		Assert.False(MuiControlFontStateAdmission.ValidateLive(ref platform, State,
			text, malformed));
		malformed = valid;
		malformed.Present = 0;
		Assert.True(MuiControlFontStateAdmission.Validate(malformed));
		Assert.False(MuiControlFontStateAdmission.ValidateLive(ref platform, State,
			APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void ControlFontRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1BC0);
		var value = new MuiControlFontStateRecord
		{
			Magic = MuiControlFontStateRecord.Cookie,
			Present = 1,
			Font = APTR.FromPointer(0x1D00),
		};

		Assert.True(MuiControlFontStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiControlFontStateRecordCodec.TryReadStructural(
			ref platform, address, out var structural));
		Assert.Equal(value.Magic, structural.Magic);
		Assert.Equal(value.Present, structural.Present);
		Assert.Equal(value.Font, structural.Font);
		Assert.True(MuiControlFontStateRecordCodec.TryRead(ref platform, address,
			out _));
		Assert.True(MuiControlFontStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, 8, out var fontField));
		Assert.Equal(address.Raw + 8, fontField.Raw);
		Assert.True(MuiControlFontStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 8, out var fontRaw));
		Assert.Equal(value.Font.Raw, fontRaw);
		Assert.False(MuiControlFontStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiControlFontStateRecord.Size, out _));
		Assert.False(MuiControlFontStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, 0, out _));
		Assert.False(MuiControlFontStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void MalformedControlFontFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var textClass);
		var font = APTR.FromPointer(0x1800);
		var tags = APTR.FromPointer(0x1840);
		platform.WriteUInt32(tags, 0, Font);
		platform.WriteUInt32(tags, 4, font.Raw);
		platform.WriteUInt32(tags, 8, 0);
		platform.WriteUInt32(tags, 12, 0);
		var text = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, tags);
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State, text,
			Font, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, text, StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiControlFontStateFieldCursorCodec.TryWriteUInt32(ref platform,
			block, MuiControlFontStateField.Present, 2));
		Assert.True(MuiControlFontStateRecordCodec.TryReadStructural(ref platform,
			block, out var structural));
		Assert.Equal(2u, structural.Present);
		Assert.False(MuiControlFontStateAdmission.Validate(structural));
		Assert.False(MuiControlFontStateRecordCodec.TryRead(ref platform, block,
			out _));

		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetControlFontStateRecord(
			ref platform, State, text, out _));
		Assert.False(MuiCommonControlCore.TryReadControlFontState(ref platform,
			State, text, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, text, Font,
			out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			text, Font, APTR.FromPointer(0x1C00).Raw, false));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, text,
			StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State, text,
			Font, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiControlFontStateFieldCursorCodec.TryReadUInt32(ref platform,
			block, MuiControlFontStateField.Present, out var preserved));
		Assert.Equal(2u, preserved);
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
