using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaBuiltinFontTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void BuiltinFontSelectorProjectsAndWinsEffectiveFontResolution()
	{
		var platform = CreatePlatform(out var areaClass);
		var tags = APTR.FromPointer(0x1200);
		platform.WriteUInt32(tags, 0, MuiCommonControlCore.BuiltinFont);
		platform.WriteUInt32(tags, 4, unchecked((uint)-7)); // MUIV_BuiltinFont_Button
		platform.WriteUInt32(tags, 8, 0);
		var obj = MuiCommonControlCore.CreateControl(ref platform, State,
			areaClass, tags);

		Assert.True(MuiAreaBuiltinFontPacketCore.TryGet(ref platform, State, obj,
			out var initial));
		Assert.Equal(unchecked((uint)-7), initial.Selector);
		Assert.Equal(1u, initial.Present);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			MuiCommonControlCore.BuiltinFont, out var selector));
		Assert.Equal(unchecked((uint)-7), selector);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			MuiCommonControlCore.Font, out var effective));
		Assert.Equal(unchecked((uint)-7), effective);

		Assert.True(MuiAreaBuiltinFontPacketCore.Set(ref platform, State, obj,
			unchecked((uint)-13), false)); // MUIV_BuiltinFont_Huge
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			MuiCommonControlCore.Font, out effective));
		Assert.Equal(unchecked((uint)-13), effective);
	}

	[Fact]
	public void BuiltinFontInheritSelectorFallsThroughToParent()
	{
		var platform = CreatePlatform(out var areaClass);
		var parent = MuiCommonControlCore.CreateControl(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, parent,
			MuiCommonControlCore.BuiltinFont, unchecked((uint)-5), false));
		var child = MuiCommonControlCore.CreateControl(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, child,
			MuiCommonControlCore.BuiltinFont, 0, false));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, parent, child));

		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, child,
			MuiCommonControlCore.Font, out var effective));
		Assert.Equal(unchecked((uint)-5), effective);
	}

	[Fact]
	public void BuiltinFontRecordUsesNamedFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1500);
		var expected = new MuiAreaBuiltinFontStateRecord
		{
			Magic = MuiAreaBuiltinFontStateRecord.Cookie,
			Selector = unchecked((uint)-9),
			Present = 1,
			Generation = 3,
		};
		Assert.True(MuiAreaBuiltinFontStateRecordCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiAreaBuiltinFontStateRecordCodec.TryRead(ref platform,
			address, out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Selector, actual.Selector);
		Assert.Equal(expected.Present, actual.Present);
		Assert.Equal(expected.Generation, actual.Generation);
		var cursor = new MuiAreaBuiltinFontStateFieldCursor
		{
			Record = address,
			Field = MuiAreaBuiltinFontStateField.Generation,
		};
		Assert.True(MuiAreaBuiltinFontStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 12, fieldAddress.Raw);
	}

	[Fact]
	public void BuiltinFontRecordUsesDedicatedStructCodec()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1540);
		var value = default(MuiAreaBuiltinFontStateRecord);
		value.Magic = MuiAreaBuiltinFontStateRecord.Cookie;
		value.Selector = unchecked((uint)int.MinValue);
		value.Present = 1;
		value.Generation = 7;

		Assert.True(MuiAreaBuiltinFontStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiAreaBuiltinFontStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Selector, decoded.Selector);
		Assert.Equal(value.Present, decoded.Present);
		Assert.Equal(value.Generation, decoded.Generation);
		Assert.True(MuiAreaBuiltinFontStateRecordCodec.TryRead(ref platform,
			address, out decoded));
		Assert.False(MuiAreaBuiltinFontStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void BuiltinFontAdmissionRequiresCanonicalPresenceAndLiveOwner()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiCommonControlCore.CreateControl(ref platform, State,
			areaClass, APTR.Null);
		var valid = new MuiAreaBuiltinFontStateRecord
		{
			Magic = MuiAreaBuiltinFontStateRecord.Cookie,
			Selector = unchecked((uint)-7),
			Present = 1,
			Generation = 1,
		};
		Assert.True(MuiAreaBuiltinFontStateAdmission.Validate(valid));
		Assert.True(MuiAreaBuiltinFontStateAdmission.ValidateLive(ref platform,
			State, obj, valid));
		var malformed = valid;
		malformed.Present = 2;
		Assert.False(MuiAreaBuiltinFontStateAdmission.Validate(malformed));
		Assert.False(MuiAreaBuiltinFontStateAdmission.ValidateLive(ref platform,
			State, obj, malformed));
		malformed = valid;
		malformed.Generation = 0;
		Assert.False(MuiAreaBuiltinFontStateAdmission.Validate(malformed));
		Assert.False(MuiAreaBuiltinFontStateAdmission.ValidateLive(ref platform,
			State, obj, malformed));
		Assert.False(MuiAreaBuiltinFontStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedBuiltinFontFailsClosedBeforeRawRepair()
	{
		var platform = CreatePlatform(out var areaClass);
		var tags = APTR.FromPointer(0x1200);
		platform.WriteUInt32(tags, 0, MuiCommonControlCore.BuiltinFont);
		platform.WriteUInt32(tags, 4, unchecked((uint)-7));
		platform.WriteUInt32(tags, 8, 0);
		var obj = MuiCommonControlCore.CreateControl(ref platform, State,
			areaClass, tags);
		var block = MuiStoreCore.DataspaceFind(ref platform, State, obj,
			MuiAreaBuiltinFontCore.StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiAreaBuiltinFontStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiAreaBuiltinFontStateField.Present, 2));
		Assert.True(MuiAreaBuiltinFontStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(2u, structural.Present);
		Assert.False(MuiAreaBuiltinFontStateAdmission.Validate(structural));
		Assert.False(MuiAreaBuiltinFontStateRecordCodec.TryRead(ref platform,
			block, out _));
		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiAreaBuiltinFontPacketCore.TryGet(ref platform, State, obj,
			out _));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, obj,
			MuiAreaBuiltinFontCore.StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State, obj,
			MuiCommonControlCore.BuiltinFont, out var raw));
		Assert.Equal(unchecked((uint)-7), raw);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR areaClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Text.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		areaClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
