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
