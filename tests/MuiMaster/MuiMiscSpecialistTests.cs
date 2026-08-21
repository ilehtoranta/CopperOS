/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

// Focused host tests for the final MG09 misc specialist family: Keyadjust,
// Panel, Filepanel, Fontdisplay, the private Scrmodelist, Argstring, Aboutmui,
// Mccprefs, FSProtectionBits and Title. They exercise exact class-name and
// inheritance classification, per-attribute I-S-G policy and notification,
// owned-string failure-atomic copy/replace/free, Keyadjust allow/force input
// policy, Argstring formatting, Aboutmui application-ref/self-close lifetime,
// Panel_Run's honest validated boundary, Filepanel owned strings/init booleans/
// FilterFunc hook ABI/AddRow adoption and ASL failure cleanup, Mccprefs bounded
// registry with unregister id=0, FSProtectionBits flags, Title page topology,
// Fontdisplay minmax/draw, Scrmodelist bounded records, recursive class-owned
// disposal and the standalone dispatcher.
public sealed class MuiMiscSpecialistTests
{
	private const uint Base = 0x1000;
	private const int Size = 0x40000;
	private const uint FirstAllocation = 0x10000;

	private static readonly APTR DrawState = APTR.FromPointer(0x1000);
	private static readonly APTR Instance = APTR.FromPointer(0x2000);
	private static readonly APTR ClassId = APTR.FromPointer(0x2400);
	private static readonly APTR Text = APTR.FromPointer(0x2500);
	private static readonly APTR Text2 = APTR.FromPointer(0x2600);
	private static readonly APTR Hook = APTR.FromPointer(0x2700);
	private static readonly APTR Storage = APTR.FromPointer(0x2800);
	private static readonly APTR Packet = APTR.FromPointer(0x2900);
	private static readonly APTR Tags = APTR.FromPointer(0x2A00);
	private static readonly APTR App = APTR.FromPointer(0x2B00);
	private static readonly APTR Win = APTR.FromPointer(0x2B80);

	private static MuiHeadlessTestPlatform NewPlatform() =>
		new MuiHeadlessTestPlatform(Base, Size, FirstAllocation, DrawState);

	private static MuiMiscSpecialistClass CreateNamed(
		ref MuiHeadlessTestPlatform p, string name)
	{
		p.WriteCString(ClassId, name);
		return MuiMiscSpecialistCore.CreateByName(ref p, Instance, ClassId);
	}

	[Fact]
	public void MiscSpecialistHeaderCodecUsesNamedFields()
	{
		var p = NewPlatform();
		var address = APTR.FromPointer(0x2D00);
		var expected = default(MuiMiscSpecialistHeader);
		expected.Magic = MuiMiscSpecialistHeader.Cookie;
		expected.Class = (uint)MuiMiscSpecialistClass.Title;
		expected.Flags = MuiMiscSpecialistLayout.FlagTiNewable |
			MuiMiscSpecialistLayout.FlagSetupActive;
		expected.NotifyAttribute = 0x8042ABCDu;
		expected.NotifyValue = 0x12345678u;
		expected.NotifyCount = 4;
		Assert.True(MuiMiscSpecialistHeaderCodec.Write(ref p, address, expected));
		Assert.True(MuiMiscSpecialistHeaderCodec.TryRead(ref p, address,
			out var actual));
		Assert.Equal(expected.Class, actual.Class);
		Assert.Equal(expected.Flags, actual.Flags);
		Assert.Equal(expected.NotifyAttribute, actual.NotifyAttribute);
		Assert.Equal(expected.NotifyValue, actual.NotifyValue);
		Assert.Equal(expected.NotifyCount, actual.NotifyCount);
		Assert.False(MuiMiscSpecialistHeaderCodec.TryRead(ref p,
			APTR.FromPointer(0x50000), out _));
	}

	[Fact]
	public void MiscTitleStateCodecUsesNamedFields()
	{
		var p = NewPlatform();
		var address = APTR.FromPointer(0x2E00);
		var expected = default(MuiMiscTitleState);
		expected.Pages = APTR.FromPointer(0x2F00);
		expected.PageCount = 3;
		expected.ActivePage = 2;
		expected.PageSequence = 9;
		expected.Position = MuiMiscAttributes.Title_Position_Top;
		expected.EventPriority = 4;
		expected.OnLastClose = 1;
		Assert.True(MuiMiscTitleStateCodec.Write(ref p, address, expected));
		Assert.True(MuiMiscTitleStateCodec.TryRead(ref p, address,
			out var actual));
		Assert.Equal(expected.Pages, actual.Pages);
		Assert.Equal(expected.PageCount, actual.PageCount);
		Assert.Equal(expected.ActivePage, actual.ActivePage);
		Assert.Equal(expected.PageSequence, actual.PageSequence);
		Assert.Equal(expected.Position, actual.Position);
		Assert.Equal(expected.EventPriority, actual.EventPriority);
		Assert.Equal(expected.OnLastClose, actual.OnLastClose);
		Assert.False(MuiMiscTitleStateCodec.TryRead(ref p,
			APTR.FromPointer(0x50000), out _));
	}

	[Fact]
	public void MiscPersistentRecordFieldCursorUsesNamedRecordKinds()
	{
		var p = NewPlatform();
		var header = APTR.FromPointer(0x2D80);
		Assert.True(MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref p, header,
			MuiMiscRecordKind.Header, MuiMiscRecordField.Magic,
			MuiMiscSpecialistHeader.Cookie));
		Assert.True(MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref p, header,
			MuiMiscRecordKind.Header, MuiMiscRecordField.Class,
			(uint)MuiMiscSpecialistClass.Title));
		Assert.True(MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref p, header,
			MuiMiscRecordKind.Header, MuiMiscRecordField.NotifyCount, 4u));
		Assert.True(MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref p, header,
			MuiMiscRecordKind.Header, MuiMiscRecordField.NotifyCount,
			out var notifyCount));
		Assert.Equal(4u, notifyCount);

		var title = APTR.FromPointer(0x2E80);
		Assert.True(MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref p, title,
			MuiMiscRecordKind.Title, MuiMiscRecordField.Pages, 0x2F00u));
		Assert.True(MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref p, title,
			MuiMiscRecordKind.Title, MuiMiscRecordField.Position, 2u));
		Assert.True(MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref p, title,
			MuiMiscRecordKind.Title, MuiMiscRecordField.OnLastClose, 1u));
		Assert.True(MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref p, title,
			MuiMiscRecordKind.Title, MuiMiscRecordField.Position, out var position));
		Assert.Equal(2u, position);

		var filepanel = APTR.FromPointer(0x2F80);
		Assert.True(MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref p, filepanel,
			MuiMiscRecordKind.FilepanelService, MuiMiscRecordField.Rows,
			0x3100u));
		Assert.True(MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref p, filepanel,
			MuiMiscRecordKind.FilepanelService, MuiMiscRecordField.Rows,
			out var rows));
		Assert.Equal(0x3100u, rows);

		var stringSlot = APTR.FromPointer(0x3080);
		Assert.True(MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref p,
			stringSlot, MuiMiscRecordKind.OwnedStringSlot,
			MuiMiscRecordField.AllocationSize, 17u));
		Assert.True(MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref p,
			stringSlot, MuiMiscRecordKind.OwnedStringSlot,
			MuiMiscRecordField.AllocationSize, out var allocationSize));
		Assert.Equal(17u, allocationSize);

		var mccprefs = APTR.FromPointer(0x3180);
		Assert.True(MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref p, mccprefs,
			MuiMiscRecordKind.Mccprefs, MuiMiscRecordField.RegistryCount, 6u));
		Assert.True(MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref p, mccprefs,
			MuiMiscRecordKind.Mccprefs, MuiMiscRecordField.RegistryCount,
			out var registryCount));
		Assert.Equal(6u, registryCount);

		var scrmodelist = APTR.FromPointer(0x3200);
		Assert.True(MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref p,
			scrmodelist, MuiMiscRecordKind.Scrmodelist,
			MuiMiscRecordField.ActiveMode, 2u));
		Assert.True(MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref p,
			scrmodelist, MuiMiscRecordKind.Scrmodelist,
			MuiMiscRecordField.ActiveMode, out var activeMode));
		Assert.Equal(2u, activeMode);

		var windowPanel = APTR.FromPointer(0x3280);
		Assert.True(MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref p,
			windowPanel, MuiMiscRecordKind.WindowPanel,
			MuiMiscRecordField.PanelWindow, 0x2B80u));
		Assert.True(MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref p,
			windowPanel, MuiMiscRecordKind.WindowPanel,
			MuiMiscRecordField.PanelWindow, out var panelWindow));
		Assert.Equal(0x2B80u, panelWindow);

		var fontdisplay = APTR.FromPointer(0x3300);
		Assert.True(MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref p,
			fontdisplay, MuiMiscRecordKind.Fontdisplay,
			MuiMiscRecordField.Height, 24u));
		Assert.True(MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref p,
			fontdisplay, MuiMiscRecordKind.Fontdisplay,
			MuiMiscRecordField.Height, out var height));
		Assert.Equal(24u, height);

		var page = APTR.FromPointer(0x3380);
		Assert.True(MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref p, page,
			MuiMiscRecordKind.TitlePage, MuiMiscRecordField.Handle, 0x44u));
		Assert.True(MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref p, page,
			MuiMiscRecordKind.TitlePage, MuiMiscRecordField.Handle, out var handle));
		Assert.Equal(0x44u, handle);

		var registry = APTR.FromPointer(0x3400);
		Assert.True(MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref p, registry,
			MuiMiscRecordKind.MccprefsRegistry, MuiMiscRecordField.Attr,
			0x80420000u));
		Assert.True(MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref p, registry,
			MuiMiscRecordKind.MccprefsRegistry, MuiMiscRecordField.Attr,
			out var attr));
		Assert.Equal(0x80420000u, attr);

		var row = APTR.FromPointer(0x3500);
		Assert.True(MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref p, row,
			MuiMiscRecordKind.FilepanelRow, MuiMiscRecordField.Contents,
			0x3600u));
		Assert.True(MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref p, row,
			MuiMiscRecordKind.FilepanelRow, MuiMiscRecordField.Contents,
			out var contents));
		Assert.Equal(0x3600u, contents);
		Assert.False(MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref p, title,
			MuiMiscRecordKind.Header, MuiMiscRecordField.Position, out _));
		Assert.False(MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref p,
			APTR.FromPointer(0xFFFFFFF0u), MuiMiscRecordKind.Title,
			MuiMiscRecordField.OnLastClose, out _));
	}

	[Fact]
	public void MiscFilepanelServiceStateCodecUsesNamedFields()
	{
		var p = NewPlatform();
		var address = APTR.FromPointer(0x2F00);
		var expected = default(MuiMiscFilepanelServiceState);
		expected.FilterFunc = APTR.FromPointer(0x3000);
		expected.AslState = APTR.FromPointer(0x3080);
		expected.Rows = APTR.FromPointer(0x3100);
		expected.RowCount = 5;
		expected.HookMsg = APTR.FromPointer(0x3180);
		Assert.True(MuiMiscFilepanelServiceStateCodec.Write(ref p, address,
			expected));
		Assert.True(MuiMiscFilepanelServiceStateCodec.TryRead(ref p, address,
			out var actual));
		Assert.Equal(expected.FilterFunc, actual.FilterFunc);
		Assert.Equal(expected.AslState, actual.AslState);
		Assert.Equal(expected.Rows, actual.Rows);
		Assert.Equal(expected.RowCount, actual.RowCount);
		Assert.Equal(expected.HookMsg, actual.HookMsg);
		Assert.False(MuiMiscFilepanelServiceStateCodec.TryRead(ref p,
			APTR.FromPointer(0x50000), out _));
	}

	[Fact]
	public void MiscOwnedStringSlotCodecUsesNamedFields()
	{
		var p = NewPlatform();
		var address = APTR.FromPointer(0x3200);
		var expected = default(MuiMiscOwnedStringSlot);
		expected.Value = APTR.FromPointer(0x3280);
		expected.AllocationSize = 17;
		Assert.True(MuiMiscOwnedStringSlotCodec.Write(ref p, address, expected));
		Assert.True(MuiMiscOwnedStringSlotCodec.TryRead(ref p, address,
			out var actual));
		Assert.Equal(expected.Value, actual.Value);
		Assert.Equal(expected.AllocationSize, actual.AllocationSize);
		Assert.False(MuiMiscOwnedStringSlotCodec.TryRead(ref p,
			APTR.FromPointer(0x50000), out _));
	}

	[Fact]
	public void MiscMccprefsStateCodecUsesNamedFields()
	{
		var p = NewPlatform();
		var address = APTR.FromPointer(0x3300);
		var expected = default(MuiMiscMccprefsState);
		expected.Registry = APTR.FromPointer(0x3380);
		expected.RegistryCount = 6;
		expected.RegistryConfig = APTR.FromPointer(0x3400);
		expected.RegistryOriginator = APTR.FromPointer(0x3480);
		Assert.True(MuiMiscMccprefsStateCodec.Write(ref p, address, expected));
		Assert.True(MuiMiscMccprefsStateCodec.TryRead(ref p, address,
			out var actual));
		Assert.Equal(expected.Registry, actual.Registry);
		Assert.Equal(expected.RegistryCount, actual.RegistryCount);
		Assert.Equal(expected.RegistryConfig, actual.RegistryConfig);
		Assert.Equal(expected.RegistryOriginator, actual.RegistryOriginator);
		Assert.False(MuiMiscMccprefsStateCodec.TryRead(ref p,
			APTR.FromPointer(0x50000), out _));
	}

	[Fact]
	public void MiscScrmodelistStateCodecUsesNamedFields()
	{
		var p = NewPlatform();
		var address = APTR.FromPointer(0x3500);
		var expected = default(MuiMiscScrmodelistState);
		expected.Modes = APTR.FromPointer(0x3580);
		expected.ModeCount = 4;
		expected.ActiveMode = 2;
		Assert.True(MuiMiscScrmodelistStateCodec.Write(ref p, address, expected));
		Assert.True(MuiMiscScrmodelistStateCodec.TryRead(ref p, address,
			out var actual));
		Assert.Equal(expected.Modes, actual.Modes);
		Assert.Equal(expected.ModeCount, actual.ModeCount);
		Assert.Equal(expected.ActiveMode, actual.ActiveMode);
		Assert.False(MuiMiscScrmodelistStateCodec.TryRead(ref p,
			APTR.FromPointer(0x50000), out _));
	}

	[Fact]
	public void MiscWindowPanelStateCodecUsesNamedFields()
	{
		var p = NewPlatform();
		var address = APTR.FromPointer(0x3680);
		var expected = default(MuiMiscWindowPanelState);
		expected.Application = APTR.FromPointer(0x3700);
		expected.PanelWindow = APTR.FromPointer(0x3780);
		Assert.True(MuiMiscWindowPanelStateCodec.Write(ref p, address, expected));
		Assert.True(MuiMiscWindowPanelStateCodec.TryRead(ref p, address,
			out var actual));
		Assert.Equal(expected.Application, actual.Application);
		Assert.Equal(expected.PanelWindow, actual.PanelWindow);
		Assert.False(MuiMiscWindowPanelStateCodec.TryRead(ref p,
			APTR.FromPointer(0x50000), out _));
	}

	[Fact]
	public void MiscProtectionStateCodecUsesNamedFields()
	{
		var p = NewPlatform();
		var address = APTR.FromPointer(0x3800);
		var expected = default(MuiMiscProtectionState);
		expected.Flags = 0xA5A55A5Au;
		Assert.True(MuiMiscProtectionStateCodec.Write(ref p, address, expected));
		Assert.True(MuiMiscProtectionStateCodec.TryRead(ref p, address,
			out var actual));
		Assert.Equal(expected.Flags, actual.Flags);
		Assert.False(MuiMiscProtectionStateCodec.TryRead(ref p,
			APTR.FromPointer(0x50000), out _));
	}

	[Fact]
	public void MiscFontdisplaySizeCodecUsesNamedFields()
	{
		var p = NewPlatform();
		var address = APTR.FromPointer(0x3880);
		var expected = default(MuiMiscFontdisplaySize);
		expected.Width = 640;
		expected.Height = 480;
		Assert.True(MuiMiscFontdisplaySizeCodec.Write(ref p, address, expected));
		Assert.True(MuiMiscFontdisplaySizeCodec.TryRead(ref p, address,
			out var actual));
		Assert.Equal(expected.Width, actual.Width);
		Assert.Equal(expected.Height, actual.Height);
		Assert.False(MuiMiscFontdisplaySizeCodec.TryRead(ref p,
			APTR.FromPointer(0x50000), out _));
	}

	[Fact]
	public void MiscStateCursorUsesNamedRegionBoundary()
	{
		var cursor = default(MuiMiscStateCursor);
		cursor.Instance = APTR.FromPointer(0x2000);
		cursor.Region = MuiMiscStateRegion.Title;
		Assert.True(MuiMiscStateCursorCodec.TryGetAddress(cursor,
			out var address));
		Assert.Equal(APTR.FromPointer(0x204C), address);
		cursor.Instance = APTR.FromPointer(0xFFFFFFF0);
		Assert.False(MuiMiscStateCursorCodec.TryGetAddress(cursor, out _));
	}

	[Fact]
	public void MiscOwnedStringCursorUsesNamedFieldBoundary()
	{
		var cursor = default(MuiMiscOwnedStringCursor);
		cursor.Instance = APTR.FromPointer(0x2000);
		cursor.Field = MuiMiscOwnedStringField.FilepanelRejectPattern;
		Assert.True(MuiMiscOwnedStringCursorCodec.TryGetAddress(cursor,
			out var address));
		Assert.Equal(APTR.FromPointer(0x2088), address);
		cursor.Instance = APTR.FromPointer(0xFFFFFFF0);
		Assert.False(MuiMiscOwnedStringCursorCodec.TryGetAddress(cursor, out _));
	}

	// ---- Classification & inheritance ----------------------------------------

	[Fact]
	public void ExactClassNamesAreClassified()
	{
		var p = NewPlatform();
		Assert.Equal(MuiMiscSpecialistClass.Keyadjust, Classify(ref p, "Keyadjust.mui"));
		Assert.Equal(MuiMiscSpecialistClass.Panel, Classify(ref p, "Panel.mui"));
		Assert.Equal(MuiMiscSpecialistClass.Filepanel, Classify(ref p, "Filepanel.mui"));
		Assert.Equal(MuiMiscSpecialistClass.Fontdisplay, Classify(ref p, "Fontdisplay.mui"));
		Assert.Equal(MuiMiscSpecialistClass.Scrmodelist, Classify(ref p, "Scrmodelist.mui"));
		Assert.Equal(MuiMiscSpecialistClass.Argstring, Classify(ref p, "Argstring.mui"));
		Assert.Equal(MuiMiscSpecialistClass.Aboutmui, Classify(ref p, "Aboutmui.mui"));
		Assert.Equal(MuiMiscSpecialistClass.Mccprefs, Classify(ref p, "Mccprefs.mui"));
		Assert.Equal(MuiMiscSpecialistClass.FSProtectionBits, Classify(ref p, "FSProtectionBits.mui"));
		Assert.Equal(MuiMiscSpecialistClass.Title, Classify(ref p, "Title.mui"));
	}

	private static MuiMiscSpecialistClass Classify(ref MuiHeadlessTestPlatform p,
		string name)
	{
		p.WriteCString(ClassId, name);
		return MuiMiscSpecialistCore.ClassifyName(ref p, ClassId);
	}

	[Fact]
	public void UnknownTruncatedAndMiscasedNamesAreRejected()
	{
		var p = NewPlatform();
		Assert.Equal(MuiMiscSpecialistClass.None, Classify(ref p, "Title"));      // no suffix
		Assert.Equal(MuiMiscSpecialistClass.None, Classify(ref p, "title.mui"));  // case
		Assert.Equal(MuiMiscSpecialistClass.None, Classify(ref p, "Panell.mui")); // extra char
		Assert.Equal(MuiMiscSpecialistClass.None, Classify(ref p, "Group.mui"));
		Assert.Equal(MuiMiscSpecialistClass.None,
			MuiMiscSpecialistCore.ClassifyName(ref p, APTR.Null));
	}

	[Fact]
	public void InheritanceAndPrivacyMatchDocumentedHierarchy()
	{
		Assert.Equal(MuiMiscSpecialistClass.Panel,
			MuiMiscSpecialistCore.Superclass(MuiMiscSpecialistClass.Filepanel));
		Assert.Equal(MuiMiscSpecialistClass.None,
			MuiMiscSpecialistCore.Superclass(MuiMiscSpecialistClass.Panel));
		Assert.True(MuiMiscSpecialistCore.InheritsFrom(
			MuiMiscSpecialistClass.Filepanel, MuiMiscSpecialistClass.Panel));
		Assert.True(MuiMiscSpecialistCore.IsPrivate(
			MuiMiscSpecialistClass.Scrmodelist));
		Assert.False(MuiMiscSpecialistCore.IsPrivate(
			MuiMiscSpecialistClass.Title));
	}

	// ---- Keyadjust -----------------------------------------------------------

	[Fact]
	public void KeyadjustKeyIsOwnedCopiedAndFreedOnReplace()
	{
		var p = NewPlatform();
		Assert.Equal(MuiMiscSpecialistClass.Keyadjust, CreateNamed(ref p, "Keyadjust.mui"));
		p.WriteCString(Text, "shift a");
		Assert.True(MuiMiscSpecialistCore.SetAttribute(ref p, Instance,
			MuiMiscAttributes.Keyadjust_Key, Text.Raw, false, true, out _));
		MuiMiscSpecialistCore.GetAttribute(ref p, Instance,
			MuiMiscAttributes.Keyadjust_Key, out var stored);
		Assert.NotEqual(0u, stored);
		Assert.NotEqual(Text.Raw, stored);   // owned copy, not the caller pointer
		Assert.Equal("shift a", ReadCString(ref p, APTR.FromPointer(stored)));
		var before = p.FreeCount;
		p.WriteCString(Text2, "control b");
		MuiMiscSpecialistCore.SetAttribute(ref p, Instance,
			MuiMiscAttributes.Keyadjust_Key, Text2.Raw, false, true, out _);
		Assert.True(p.FreeCount > before);   // previous copy freed
	}

	[Fact]
	public void KeyadjustAllowAndForcePoliciesAreIsg()
	{
		var p = NewPlatform();
		CreateNamed(ref p, "Keyadjust.mui");
		AssertBoolIsg(ref p, MuiMiscAttributes.Keyadjust_AllowMultipleKeys);
		AssertBoolIsg(ref p, MuiMiscAttributes.Keyadjust_AllowDoubleClick);
		AssertBoolIsg(ref p, MuiMiscAttributes.Keyadjust_AllowTripleClick);
		AssertBoolIsg(ref p, MuiMiscAttributes.Keyadjust_AllowMouseEvents);
		AssertBoolIsg(ref p, MuiMiscAttributes.Keyadjust_ForceKeyCode);
	}

	[Fact]
	public void KeyadjustInputHonorsPolicies()
	{
		var p = NewPlatform();
		CreateNamed(ref p, "Keyadjust.mui");
		p.WriteCString(Text, "a");
		// Mouse event rejected until AllowMouseEvents.
		Assert.False(MuiMiscSpecialistCore.RecordInput(ref p, Instance, Text, true, 1, false));
		MuiMiscSpecialistCore.SetAttribute(ref p, Instance,
			MuiMiscAttributes.Keyadjust_AllowMouseEvents, 1, true, false, out _);
		Assert.True(MuiMiscSpecialistCore.RecordInput(ref p, Instance, Text, true, 1, false));
		// Double click rejected until AllowDoubleClick.
		Assert.False(MuiMiscSpecialistCore.RecordInput(ref p, Instance, Text, true, 2, false));
		MuiMiscSpecialistCore.SetAttribute(ref p, Instance,
			MuiMiscAttributes.Keyadjust_AllowDoubleClick, 1, true, false, out _);
		Assert.True(MuiMiscSpecialistCore.RecordInput(ref p, Instance, Text, true, 2, false));
		// Multi-key chord rejected until AllowMultipleKeys.
		Assert.False(MuiMiscSpecialistCore.RecordInput(ref p, Instance, Text, false, 1, true));
		MuiMiscSpecialistCore.SetAttribute(ref p, Instance,
			MuiMiscAttributes.Keyadjust_AllowMultipleKeys, 1, true, false, out _);
		Assert.True(MuiMiscSpecialistCore.RecordInput(ref p, Instance, Text, false, 1, true));
		// Disabled ignores all input.
		MuiMiscSpecialistCore.SetAttribute(ref p, Instance,
			MuiMiscAttributes.Disabled, 1, false, false, out _);
		Assert.False(MuiMiscSpecialistCore.RecordInput(ref p, Instance, Text, false, 1, false));
	}

	// ---- Argstring -----------------------------------------------------------

	[Fact]
	public void ArgstringTemplateAndContentsAreOwnedAndFormatted()
	{
		var p = NewPlatform();
		Assert.Equal(MuiMiscSpecialistClass.Argstring, CreateNamed(ref p, "Argstring.mui"));
		p.WriteCString(Text, "FROM/A,TO/A");
		MuiMiscSpecialistCore.SetAttribute(ref p, Instance,
			MuiMiscAttributes.Argstring_Template, Text.Raw, false, true, out _);
		MuiMiscSpecialistCore.GetAttribute(ref p, Instance,
			MuiMiscAttributes.Argstring_Template, out var tmpl);
		Assert.NotEqual(Text.Raw, tmpl);
		Assert.Equal("FROM/A,TO/A", ReadCString(ref p, APTR.FromPointer(tmpl)));
		var notifyBefore = MuiMiscSpecialistCore.NotificationCount(ref p, Instance);
		Assert.True(MuiMiscSpecialistCore.FormatContents(ref p, Instance));
		MuiMiscSpecialistCore.GetAttribute(ref p, Instance,
			MuiMiscAttributes.Argstring_Contents, out var contents);
		Assert.Equal("FROM/A,TO/A", ReadCString(ref p, APTR.FromPointer(contents)));
		Assert.True(MuiMiscSpecialistCore.NotificationCount(ref p, Instance) > notifyBefore);
	}

	// ---- Aboutmui ------------------------------------------------------------

	[Fact]
	public void AboutmuiRequiresApplicationAndSelfCloses()
	{
		var p = NewPlatform();
		Assert.Equal(MuiMiscSpecialistClass.Aboutmui, CreateNamed(ref p, "Aboutmui.mui"));
		// No Application bound -> cannot open.
		Assert.False(MuiMiscSpecialistCore.AboutmuiOpen(ref p, Instance));
		MuiMiscSpecialistCore.SetAttribute(ref p, Instance,
			MuiMiscAttributes.Aboutmui_Application, App.Raw, true, false, out _);
		MuiMiscSpecialistCore.GetAttribute(ref p, Instance,
			MuiMiscAttributes.Aboutmui_Application, out var app);
		Assert.Equal(App.Raw, app);
		Assert.True(MuiMiscSpecialistCore.AboutmuiOpen(ref p, Instance));
		Assert.False(MuiMiscSpecialistCore.AboutmuiOpen(ref p, Instance)); // no double open
		Assert.True(MuiMiscSpecialistCore.AboutmuiIsOpen(ref p, Instance));
		Assert.True(MuiMiscSpecialistCore.AboutmuiClose(ref p, Instance));
		Assert.False(MuiMiscSpecialistCore.AboutmuiIsOpen(ref p, Instance));
		Assert.False(MuiMiscSpecialistCore.AboutmuiClose(ref p, Instance)); // already closed
		// Window-derived: no MUIA_Disabled state.
		Assert.False(MuiMiscSpecialistCore.GetAttribute(ref p, Instance,
			MuiMiscAttributes.Disabled, out _));
	}

	// ---- Panel ---------------------------------------------------------------

	[Fact]
	public void PanelRunEnforcesHonestBoundary()
	{
		var p = NewPlatform();
		Assert.Equal(MuiMiscSpecialistClass.Panel, CreateNamed(ref p, "Panel.mui"));
		Assert.False(MuiMiscSpecialistCore.PanelHasRun(ref p, Instance));
		Assert.False(MuiMiscSpecialistCore.PanelRun(ref p, Instance, APTR.Null, Win));
		Assert.False(MuiMiscSpecialistCore.PanelRun(ref p, Instance, App, APTR.Null));
		Assert.False(MuiMiscSpecialistCore.PanelHasRun(ref p, Instance));
		Assert.True(MuiMiscSpecialistCore.PanelRun(ref p, Instance, App, Win));
		Assert.True(MuiMiscSpecialistCore.PanelHasRun(ref p, Instance));
	}

	// ---- FSProtectionBits ----------------------------------------------------

	[Fact]
	public void FSProtectionBitsFlagsAreIsgWithNotification()
	{
		var p = NewPlatform();
		Assert.Equal(MuiMiscSpecialistClass.FSProtectionBits,
			CreateNamed(ref p, "FSProtectionBits.mui"));
		var before = MuiMiscSpecialistCore.NotificationCount(ref p, Instance);
		Assert.True(MuiMiscSpecialistCore.SetAttribute(ref p, Instance,
			MuiMiscAttributes.FSProtectionBits_Flags, 0x000000FF, false, true,
			out var changed));
		Assert.True(changed);
		MuiMiscSpecialistCore.GetAttribute(ref p, Instance,
			MuiMiscAttributes.FSProtectionBits_Flags, out var flags);
		Assert.Equal(0x000000FFu, flags);
		Assert.Equal(before + 1, MuiMiscSpecialistCore.NotificationCount(ref p, Instance));
		Assert.Equal(MuiMiscAttributes.FSProtectionBits_Flags,
			MuiMiscSpecialistCore.LastNotifiedAttribute(ref p, Instance));
	}

	// ---- Title ---------------------------------------------------------------

	[Fact]
	public void TitleDefaultsAndAttributeValidation()
	{
		var p = NewPlatform();
		Assert.Equal(MuiMiscSpecialistClass.Title, CreateNamed(ref p, "Title.mui"));
		MuiMiscSpecialistCore.GetAttribute(ref p, Instance,
			MuiMiscAttributes.Title_Newable, out var newable);
		Assert.Equal(1u, newable);
		MuiMiscSpecialistCore.GetAttribute(ref p, Instance,
			MuiMiscAttributes.Title_Sortable, out var sortable);
		Assert.Equal(1u, sortable);
		MuiMiscSpecialistCore.GetAttribute(ref p, Instance,
			MuiMiscAttributes.Title_Position, out var position);
		Assert.Equal(MuiMiscAttributes.Title_Position_Top, position);
		// Position accepts 0..3, rejects 4.
		Assert.True(MuiMiscSpecialistCore.SetAttribute(ref p, Instance,
			MuiMiscAttributes.Title_Position, MuiMiscAttributes.Title_Position_Right,
			false, true, out _));
		Assert.False(MuiMiscSpecialistCore.SetAttribute(ref p, Instance,
			MuiMiscAttributes.Title_Position, 4, false, true, out _));
		// OnLastClose accepts 0..1, rejects 2.
		Assert.True(MuiMiscSpecialistCore.SetAttribute(ref p, Instance,
			MuiMiscAttributes.Title_OnLastClose, 1, false, true, out _));
		Assert.False(MuiMiscSpecialistCore.SetAttribute(ref p, Instance,
			MuiMiscAttributes.Title_OnLastClose, 2, false, true, out _));
	}

	[Fact]
	public void TitlePageTopologyNewFindClose()
	{
		var p = NewPlatform();
		CreateNamed(ref p, "Title.mui");
		// Newable is TRUE by default.
		var h1 = MuiMiscSpecialistCore.TitleNew(ref p, Instance);
		var h2 = MuiMiscSpecialistCore.TitleNew(ref p, Instance);
		var h3 = MuiMiscSpecialistCore.TitleNew(ref p, Instance);
		Assert.NotEqual(0u, h1);
		Assert.NotEqual(h1, h2);
		Assert.NotEqual(h2, h3);
		Assert.Equal(3u, MuiMiscSpecialistCore.TitlePageCount(ref p, Instance));
		Assert.Equal(0u, MuiMiscSpecialistCore.TitleFindPage(ref p, Instance, h1));
		Assert.Equal(1u, MuiMiscSpecialistCore.TitleFindPage(ref p, Instance, h2));
		Assert.Equal(0xFFFFFFFFu, MuiMiscSpecialistCore.TitleFindPage(ref p, Instance, 0xDEAD));
		// Close requires Closable.
		Assert.False(MuiMiscSpecialistCore.TitleClose(ref p, Instance, h2));
		MuiMiscSpecialistCore.SetAttribute(ref p, Instance,
			MuiMiscAttributes.Title_Closable, 1, true, false, out _);
		Assert.True(MuiMiscSpecialistCore.TitleClose(ref p, Instance, h2));
		Assert.Equal(2u, MuiMiscSpecialistCore.TitlePageCount(ref p, Instance));
		Assert.Equal(0xFFFFFFFFu, MuiMiscSpecialistCore.TitleFindPage(ref p, Instance, h2));
		// h3 shifted down into h2's slot.
		Assert.Equal(1u, MuiMiscSpecialistCore.TitleFindPage(ref p, Instance, h3));
	}

	[Fact]
	public void TitlePageCodecUsesNamedHandleAndFlags()
	{
		var p = NewPlatform();
		var address = APTR.FromPointer(0x1800);
		var record = default(MuiTitlePageRecord);
		record.Handle = 0x1234;
		record.Flags = 0xA5A5;
		Assert.True(MuiTitlePageCodec.Write(ref p, address, record));
		Assert.True(MuiTitlePageCodec.TryRead(ref p, address,
			out var decoded));
		Assert.Equal(record.Handle, decoded.Handle);
		Assert.Equal(record.Flags, decoded.Flags);
		Assert.False(MuiTitlePageCodec.TryRead(ref p,
			APTR.FromPointer(Base + (uint)Size - 1), out _));
	}

	[Fact]
	public void TitlePageCursorUsesNamedEntryBoundary()
	{
		var p = NewPlatform();
		var cursor = default(MuiTitlePageCursor);
		cursor.Base = APTR.FromPointer(0x1800);
		cursor.Index = MuiTitlePageCursor.MaximumEntries - 1;

		Assert.True(MuiTitlePageCursorCodec.TryGetEntry(ref p, cursor,
			out var address));
		Assert.Equal(APTR.FromPointer(0x19F8), address);
		cursor.Index = MuiTitlePageCursor.MaximumEntries;
		Assert.False(MuiTitlePageCursorCodec.TryGetEntry(ref p, cursor,
			out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFF0);
		cursor.Index = 1;
		Assert.False(MuiTitlePageCursorCodec.TryGetEntry(ref p, cursor,
			out _));
	}

	[Fact]
	public void TitleNewRejectedWhenNotNewable()
	{
		var p = NewPlatform();
		CreateNamed(ref p, "Title.mui");
		MuiMiscSpecialistCore.SetAttribute(ref p, Instance,
			MuiMiscAttributes.Title_Newable, 0, true, false, out _);
		Assert.Equal(0u, MuiMiscSpecialistCore.TitleNew(ref p, Instance));
	}

	// ---- Mccprefs ------------------------------------------------------------

	[Fact]
	public void MccprefsRegistryAndConfigBoundaries()
	{
		var p = NewPlatform();
		Assert.Equal(MuiMiscSpecialistClass.Mccprefs, CreateNamed(ref p, "Mccprefs.mui"));
		// Empty registry -> Config methods report the honest empty boundary.
		Assert.False(MuiMiscSpecialistCore.MccprefsConfigToGadgets(ref p, Instance, App));
		var g1 = APTR.FromPointer(0x3000);
		var g2 = APTR.FromPointer(0x3100);
		Assert.True(MuiMiscSpecialistCore.MccprefsRegisterGadget(ref p, Instance,
			g1, 10, 0, Text, 0, APTR.Null));
		Assert.True(MuiMiscSpecialistCore.MccprefsRegisterGadget(ref p, Instance,
			g2, 11, 0, Text, 0, APTR.Null));
		Assert.Equal(2u, MuiMiscSpecialistCore.MccprefsRegistryCount(ref p, Instance));
		// Re-register same gadget updates in place (no growth).
		Assert.True(MuiMiscSpecialistCore.MccprefsRegisterGadget(ref p, Instance,
			g1, 99, 0, Text, 0, APTR.Null));
		Assert.Equal(2u, MuiMiscSpecialistCore.MccprefsRegistryCount(ref p, Instance));
		// Register with null gadget fails; unregister unknown fails.
		Assert.False(MuiMiscSpecialistCore.MccprefsRegisterGadget(ref p, Instance,
			APTR.Null, 5, 0, Text, 0, APTR.Null));
		Assert.False(MuiMiscSpecialistCore.MccprefsRegisterGadget(ref p, Instance,
			APTR.FromPointer(0x9999), 0, 0, APTR.Null, 0, APTR.Null));
		// Now the config methods have gadgets to distribute/collect.
		Assert.True(MuiMiscSpecialistCore.MccprefsConfigToGadgets(ref p, Instance, App));
		Assert.True(MuiMiscSpecialistCore.MccprefsGadgetsToConfig(ref p, Instance, App, Win));
		// Unregister id=0 removes the matching gadget.
		Assert.True(MuiMiscSpecialistCore.MccprefsRegisterGadget(ref p, Instance,
			g1, 0, 0, APTR.Null, 0, APTR.Null));
		Assert.Equal(1u, MuiMiscSpecialistCore.MccprefsRegistryCount(ref p, Instance));
	}

	[Fact]
	public void MccprefsRegistryCodecUsesNamedFields()
	{
		var p = NewPlatform();
		var address = APTR.FromPointer(0x1800);
		var record = default(MuiMccprefsRegistryRecord);
		record.Gadget = APTR.FromPointer(0x1900);
		record.Id = 7;
		record.Params = 8;
		record.Title = APTR.FromPointer(0x1A00);
		record.Attr = 9;
		record.Label = APTR.FromPointer(0x1B00);
		Assert.True(MuiMccprefsRegistryCodec.Write(ref p, address, record));
		Assert.True(MuiMccprefsRegistryCodec.TryRead(ref p, address,
			out var decoded));
		Assert.Equal(record.Gadget, decoded.Gadget);
		Assert.Equal(record.Id, decoded.Id);
		Assert.Equal(record.Params, decoded.Params);
		Assert.Equal(record.Title, decoded.Title);
		Assert.Equal(record.Attr, decoded.Attr);
		Assert.Equal(record.Label, decoded.Label);
		Assert.False(MuiMccprefsRegistryCodec.TryRead(ref p,
			APTR.FromPointer(Base + (uint)Size - 1), out _));
	}

	[Fact]
	public void MccprefsRegistryCursorUsesNamedEntryBoundary()
	{
		var p = NewPlatform();
		var cursor = default(MuiMccprefsRegistryCursor);
		cursor.Base = APTR.FromPointer(0x1800);
		cursor.Index = MuiMccprefsRegistryCursor.MaximumEntries - 1;

		Assert.True(MuiMccprefsRegistryCursorCodec.TryGetEntry(ref p, cursor,
			out var address));
		Assert.Equal(APTR.FromPointer(0x1DE8), address);
		cursor.Index = MuiMccprefsRegistryCursor.MaximumEntries;
		Assert.False(MuiMccprefsRegistryCursorCodec.TryGetEntry(ref p, cursor,
			out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFF0);
		cursor.Index = 1;
		Assert.False(MuiMccprefsRegistryCursorCodec.TryGetEntry(ref p, cursor,
			out _));
	}

	// ---- Filepanel -----------------------------------------------------------

	[Fact]
	public void FilepanelOwnedStringsAndInitBooleans()
	{
		var p = NewPlatform();
		Assert.Equal(MuiMiscSpecialistClass.Filepanel, CreateNamed(ref p, "Filepanel.mui"));
		p.WriteCString(Text, "RAM:");
		MuiMiscSpecialistCore.SetAttribute(ref p, Instance,
			MuiMiscAttributes.Filepanel_Drawer, Text.Raw, false, true, out _);
		MuiMiscSpecialistCore.GetAttribute(ref p, Instance,
			MuiMiscAttributes.Filepanel_Drawer, out var drawer);
		Assert.NotEqual(Text.Raw, drawer);
		Assert.Equal("RAM:", ReadCString(ref p, APTR.FromPointer(drawer)));
		// Init-only boolean: settable at init, latched at runtime.
		Assert.True(MuiMiscSpecialistCore.SetAttribute(ref p, Instance,
			MuiMiscAttributes.Filepanel_DoSaveMode, 1, true, false, out _));
		MuiMiscSpecialistCore.GetAttribute(ref p, Instance,
			MuiMiscAttributes.Filepanel_DoSaveMode, out var save);
		Assert.Equal(1u, save);
		Assert.False(MuiMiscSpecialistCore.SetAttribute(ref p, Instance,
			MuiMiscAttributes.Filepanel_DoSaveMode, 0, false, true, out _));
	}

	[Fact]
	public void FilepanelFilterFuncUsesHookAbi()
	{
		var p = NewPlatform();
		CreateNamed(ref p, "Filepanel.mui");
		// No hook -> every entry kept.
		Assert.Equal(1u, MuiMiscSpecialistCore.FilepanelFilter(ref p, Instance, Text));
		p.WriteUInt32(Hook, 8, 0x00DD0001u);   // h_Entry (non-sentinel)
		p.WriteUInt32(Hook, 16, Hook.Raw + 32);
		MuiMiscSpecialistCore.SetAttribute(ref p, Instance,
			MuiMiscAttributes.Filepanel_FilterFunc, Hook.Raw, true, false, out _);
		var before = p.HookInvokeCount;
		var kept = MuiMiscSpecialistCore.FilepanelFilter(ref p, Instance, Text);
		Assert.Equal(before + 1, p.HookInvokeCount);
		Assert.Equal(Instance.Raw, p.LastHookA2.Raw);   // A2 = object
		Assert.NotEqual(0u, kept);
	}

	[Fact]
	public void FilepanelAddRowAdoptsAndDisposes()
	{
		var p = NewPlatform();
		CreateNamed(ref p, "Filepanel.mui");
		var label = p.NewObject(APTR.FromPointer(0x9000), APTR.Null);
		var contents = p.NewObject(APTR.FromPointer(0x9000), APTR.Null);
		// Null child rejected atomically.
		Assert.False(MuiMiscSpecialistCore.FilepanelAddRow(ref p, Instance, APTR.Null, contents));
		Assert.Equal(0u, MuiMiscSpecialistCore.FilepanelRowCount(ref p, Instance));
		Assert.True(MuiMiscSpecialistCore.FilepanelAddRow(ref p, Instance, label, contents));
		Assert.Equal(1u, MuiMiscSpecialistCore.FilepanelRowCount(ref p, Instance));
		var freeBefore = p.FreeCount;
		Assert.True(MuiMiscSpecialistLifecycle.Dispose(ref p, Instance));
		Assert.True(p.FreeCount > freeBefore);   // adopted children + blocks freed
		Assert.False(MuiMiscSpecialistCore.Valid(ref p, Instance));
	}

	[Fact]
	public void FilepanelRowCodecUsesNamedPointers()
	{
		var p = NewPlatform();
		var address = APTR.FromPointer(0x1800);
		var record = default(MuiFilepanelRowRecord);
		record.Label = APTR.FromPointer(0x1900);
		record.Contents = APTR.FromPointer(0x1A00);
		Assert.True(MuiFilepanelRowCodec.Write(ref p, address, record));
		Assert.True(MuiFilepanelRowCodec.TryRead(ref p, address,
			out var decoded));
		Assert.Equal(record.Label, decoded.Label);
		Assert.Equal(record.Contents, decoded.Contents);
		Assert.False(MuiFilepanelRowCodec.TryRead(ref p,
			APTR.FromPointer(Base + (uint)Size - 1), out _));
	}

	[Fact]
	public void FilepanelRowCursorUsesNamedEntryBoundary()
	{
		var p = NewPlatform();
		var cursor = default(MuiFilepanelRowCursor);
		cursor.Base = APTR.FromPointer(0x1800);
		cursor.Index = MuiFilepanelRowCursor.MaximumEntries - 1;

		Assert.True(MuiFilepanelRowCursorCodec.TryGetEntry(ref p, cursor,
			out var address));
		Assert.Equal(APTR.FromPointer(0x19F8), address);
		cursor.Index = MuiFilepanelRowCursor.MaximumEntries;
		Assert.False(MuiFilepanelRowCursorCodec.TryGetEntry(ref p, cursor,
			out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFF0);
		cursor.Index = 1;
		Assert.False(MuiFilepanelRowCursorCodec.TryGetEntry(ref p, cursor,
			out _));
	}

	[Fact]
	public void FilepanelBrowseIsFailureAtomic()
	{
		var p = NewPlatform();
		CreateNamed(ref p, "Filepanel.mui");
		p.WriteUInt32(Tags, 0, 0);   // TAG_DONE
		var allocBefore = p.AslAllocateCount;
		var freeBefore = p.AslFreeCount;
		Assert.True(MuiMiscSpecialistCore.FilepanelBrowse(ref p, Instance, 0, Tags));
		Assert.Equal(allocBefore + 1, p.AslAllocateCount);
		Assert.Equal(freeBefore + 1, p.AslFreeCount);   // requester released, no leak
	}

	// ---- Fontdisplay ---------------------------------------------------------

	[Fact]
	public void FontdisplayMinMaxAndDrawOnly()
	{
		var p = NewPlatform();
		Assert.Equal(MuiMiscSpecialistClass.Fontdisplay, CreateNamed(ref p, "Fontdisplay.mui"));
		Assert.True(MuiMiscSpecialistCore.FontdisplayAskMinMax(ref p, Instance, Storage));
		Assert.Equal(40, p.ReadUInt16(Storage, 0));
		Assert.Equal(16, p.ReadUInt16(Storage, 2));
		Assert.True(MuiMiscSpecialistCore.FontdisplayDraw(ref p, Instance, 123, 45));
		// No invented public attributes: only Disabled (Area) is answered.
		Assert.True(MuiMiscSpecialistCore.GetAttribute(ref p, Instance,
			MuiMiscAttributes.Disabled, out _));
		Assert.False(MuiMiscSpecialistCore.GetAttribute(ref p, Instance,
			MuiMiscAttributes.Keyadjust_Key, out _));
	}

	// ---- Scrmodelist (private) -----------------------------------------------

	[Fact]
	public void ScrmodelistBoundedRecords()
	{
		var p = NewPlatform();
		Assert.Equal(MuiMiscSpecialistClass.Scrmodelist, CreateNamed(ref p, "Scrmodelist.mui"));
		Assert.True(MuiMiscSpecialistCore.ScrmodelistAddMode(ref p, Instance, 0x00021000));
		Assert.True(MuiMiscSpecialistCore.ScrmodelistAddMode(ref p, Instance, 0x00029000));
		Assert.Equal(2u, MuiMiscSpecialistCore.ScrmodelistModeCount(ref p, Instance));
		Assert.Equal(0x00021000u, MuiMiscSpecialistCore.ScrmodelistModeAt(ref p, Instance, 0));
		Assert.Equal(0x00029000u, MuiMiscSpecialistCore.ScrmodelistModeAt(ref p, Instance, 1));
		Assert.Equal(0u, MuiMiscSpecialistCore.ScrmodelistModeAt(ref p, Instance, 2));
		// A public class cannot add screenmode records.
		var q = NewPlatform();
		CreateNamed(ref q, "Title.mui");
		Assert.False(MuiMiscSpecialistCore.ScrmodelistAddMode(ref q, Instance, 1));
	}

	[Fact]
	public void ScrmodelistModeCodecUsesNamedModeId()
	{
		var p = NewPlatform();
		var address = APTR.FromPointer(0x1800);
		var record = default(MuiScrmodelistModeRecord);
		record.ModeId = 0x00021000;
		Assert.True(MuiScrmodelistModeCodec.Write(ref p, address, record));
		Assert.True(MuiScrmodelistModeCodec.TryRead(ref p, address,
			out var decoded));
		Assert.Equal(record.ModeId, decoded.ModeId);
		Assert.False(MuiScrmodelistModeCodec.TryRead(ref p,
			APTR.FromPointer(Base + (uint)Size - 1), out _));
	}

	[Fact]
	public void ScrmodelistModeCursorUsesNamedEntryBoundary()
	{
		var p = NewPlatform();
		var cursor = default(MuiScrmodelistModeCursor);
		cursor.Base = APTR.FromPointer(0x1800);
		cursor.Index = MuiScrmodelistModeCursor.MaximumEntries - 1;

		Assert.True(MuiScrmodelistModeCursorCodec.TryGetEntry(ref p, cursor,
			out var address));
		Assert.Equal(APTR.FromPointer(0x1BFC), address);
		cursor.Index = MuiScrmodelistModeCursor.MaximumEntries;
		Assert.False(MuiScrmodelistModeCursorCodec.TryGetEntry(ref p, cursor,
			out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFF0);
		cursor.Index = 1;
		Assert.False(MuiScrmodelistModeCursorCodec.TryGetEntry(ref p, cursor,
			out _));
	}

	// ---- Disposal ------------------------------------------------------------

	[Fact]
	public void DisposalIsIdempotent()
	{
		var p = NewPlatform();
		CreateNamed(ref p, "Argstring.mui");
		p.WriteCString(Text, "template");
		MuiMiscSpecialistCore.SetAttribute(ref p, Instance,
			MuiMiscAttributes.Argstring_Template, Text.Raw, false, false, out _);
		Assert.True(MuiMiscSpecialistLifecycle.Dispose(ref p, Instance));
		Assert.False(MuiMiscSpecialistCore.Valid(ref p, Instance));
		Assert.False(MuiMiscSpecialistLifecycle.Dispose(ref p, Instance)); // no-op
	}

	[Fact]
	public void FactoryAdoptsMiscSidecarAndDisposesItWithObject()
	{
		var p = NewPlatform();
		p.WriteCString(ClassId, "Keyadjust.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref p, DrawState));
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref p,
			DrawState, ClassId, APTR.Null, 0, APTR.FromPointer(1));
		Assert.True(classRecord.IsNotNull);

		var obj = MuiObjectFactoryServiceCore.NewObjectA(ref p, DrawState,
			ClassId, APTR.Null);
		Assert.True(obj.IsNotNull);
		Assert.True(MuiMiscSpecialistCore.ValidObject(ref p, DrawState, obj));
		Assert.Equal(MuiMiscSpecialistClass.Keyadjust,
			MuiMiscSpecialistCore.ClassifyObjectInstance(ref p, DrawState, obj));
		var instance = MuiMiscSpecialistCore.ObjectInstance(ref p, DrawState, obj);
		Assert.True(instance.IsNotNull);
		Assert.True(MuiMiscSpecialistCore.SetAttribute(ref p, instance,
			MuiMiscAttributes.Keyadjust_AllowMouseEvents, 1, false, true, out _));
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref p, DrawState,
			obj));
		Assert.False(MuiMiscSpecialistCore.ValidObject(ref p, DrawState, obj));
		Assert.False(MuiMiscSpecialistCore.Valid(ref p, instance));
	}

	[Fact]
	public void ObjectDispatcherRoutesFactoryMiscSetGetAndDispose()
	{
		var p = NewPlatform();
		p.WriteCString(ClassId, "Keyadjust.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref p, DrawState));
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, DrawState,
			ClassId, APTR.Null, 0, APTR.FromPointer(1)).IsNotNull);
		var obj = MuiObjectFactoryServiceCore.NewObjectA(ref p, DrawState,
			ClassId, APTR.Null);
		Assert.True(obj.IsNotNull);
		var instance = MuiMiscSpecialistCore.ObjectInstance(ref p, DrawState, obj);
		p.WriteUInt32(Packet, 0, MuiMiscAttributes.Setup);
		Assert.Equal(1u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
		Assert.True(MuiMiscSpecialistCore.IsSetupActive(ref p, instance));

		p.WriteUInt32(Packet, 0, 0x00000104u); // OM_GET
		p.WriteUInt32(Packet, 4, MuiMiscAttributes.Keyadjust_AllowMouseEvents);
		p.WriteUInt32(Packet, 8, Storage.Raw);
		Assert.Equal(1u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
		Assert.Equal(0u, p.ReadUInt32(Storage, 0));

		p.WriteUInt32(Packet, 0, 0x8042549Au); // MUIM_Set
		p.WriteUInt32(Packet, 4, MuiMiscAttributes.Keyadjust_AllowMouseEvents);
		p.WriteUInt32(Packet, 8, 1);
		Assert.Equal(1u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
		p.WriteUInt32(Packet, 0, MuiMiscAttributes.Cleanup);
		Assert.Equal(1u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
		Assert.False(MuiMiscSpecialistCore.IsSetupActive(ref p, instance));
		p.WriteUInt32(Packet, 0, 0x00000102u); // OM_DISPOSE
		Assert.Equal(1u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
		Assert.False(MuiMiscSpecialistCore.ValidObject(ref p, DrawState, obj));
		Assert.Equal(0u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
	}

	[Fact]
	public void ObjectDispatcherRoutesFactoryTitlePageMethods()
	{
		var p = NewPlatform();
		p.WriteCString(ClassId, "Title.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref p, DrawState));
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, DrawState,
			ClassId, APTR.Null, 0, APTR.FromPointer(1)).IsNotNull);
		var obj = MuiObjectFactoryServiceCore.NewObjectA(ref p, DrawState,
			ClassId, APTR.Null);
		Assert.True(obj.IsNotNull);

		// Title defaults Newable/Sortable; enable Closable through the same
		// object packet seam before creating the page.
		p.WriteUInt32(Packet, 0, 0x8042549Au);
		p.WriteUInt32(Packet, 4, MuiMiscAttributes.Title_Closable);
		p.WriteUInt32(Packet, 8, 1);
		Assert.Equal(1u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
		p.WriteUInt32(Packet, 0, MuiMiscAttributes.Title_New);
		var handle = MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet);
		Assert.NotEqual(0u, handle);
		p.WriteUInt32(Packet, 0, MuiMiscAttributes.Title_FindPage);
		p.WriteUInt32(Packet, 4, handle);
		Assert.Equal(0u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
		p.WriteUInt32(Packet, 0, MuiMiscAttributes.Title_Close);
		p.WriteUInt32(Packet, 4, handle);
		Assert.Equal(1u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
		p.WriteUInt32(Packet, 0, 0x00000102u);
		Assert.Equal(1u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
	}

	[Fact]
	public void ObjectDispatcherRoutesPanelRunBoundary()
	{
		var p = NewPlatform();
		p.WriteCString(ClassId, "Panel.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref p, DrawState));
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, DrawState,
			ClassId, APTR.Null, 0, APTR.FromPointer(1)).IsNotNull);
		var obj = MuiObjectFactoryServiceCore.NewObjectA(ref p, DrawState,
			ClassId, APTR.Null);
		Assert.True(obj.IsNotNull);
		p.WriteUInt32(Packet, 0, MuiMiscAttributes.Panel_Run);
		p.WriteUInt32(Packet, 4, 0);
		p.WriteUInt32(Packet, 8, Win.Raw);
		Assert.Equal(0u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
		p.WriteUInt32(Packet, 4, App.Raw);
		p.WriteUInt32(Packet, 8, 0);
		Assert.Equal(0u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
		p.WriteUInt32(Packet, 4, App.Raw);
		p.WriteUInt32(Packet, 8, Win.Raw);
		Assert.Equal(1u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
		var instance = MuiMiscSpecialistCore.ObjectInstance(ref p, DrawState, obj);
		Assert.True(MuiMiscSpecialistCore.PanelHasRun(ref p, instance));
		p.WriteUInt32(Packet, 0, 0x00000102u);
		Assert.Equal(1u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
	}

	[Fact]
	public void ObjectDispatcherRoutesFilepanelAddRow()
	{
		var p = NewPlatform();
		p.WriteCString(ClassId, "Filepanel.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref p, DrawState));
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, DrawState,
			ClassId, APTR.Null, 0, APTR.FromPointer(1)).IsNotNull);
		var obj = MuiObjectFactoryServiceCore.NewObjectA(ref p, DrawState,
			ClassId, APTR.Null);
		Assert.True(obj.IsNotNull);
		var label = p.NewObject(APTR.FromPointer(0x9000), APTR.Null);
		var contents = p.NewObject(APTR.FromPointer(0x9000), APTR.Null);
		p.WriteUInt32(Packet, 0, MuiMiscAttributes.Filepanel_AddRow);
		p.WriteUInt32(Packet, 4, 0);
		p.WriteUInt32(Packet, 8, contents.Raw);
		Assert.Equal(0u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
		p.WriteUInt32(Packet, 4, label.Raw);
		Assert.Equal(1u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
		var instance = MuiMiscSpecialistCore.ObjectInstance(ref p, DrawState, obj);
		Assert.Equal(1u, MuiMiscSpecialistCore.FilepanelRowCount(ref p, instance));
		p.WriteUInt32(Packet, 0, 0x00000102u);
		Assert.Equal(1u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
	}

	[Fact]
	public void ObjectDispatcherRoutesMccprefsRegisterGadget()
	{
		var p = NewPlatform();
		p.WriteCString(ClassId, "Mccprefs.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref p, DrawState));
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, DrawState,
			ClassId, APTR.Null, 0, APTR.FromPointer(1)).IsNotNull);
		var obj = MuiObjectFactoryServiceCore.NewObjectA(ref p, DrawState,
			ClassId, APTR.Null);
		Assert.True(obj.IsNotNull);
		var instance = MuiMiscSpecialistCore.ObjectInstance(ref p, DrawState, obj);
		p.WriteUInt32(Packet, 0, MuiMiscAttributes.Mccprefs_RegisterGadget);
		p.WriteUInt32(Packet, 4, 0);
		p.WriteUInt32(Packet, 8, 10);
		p.WriteUInt32(Packet, 12, 0);
		p.WriteUInt32(Packet, 16, Text.Raw);
		p.WriteUInt32(Packet, 20, 0);
		p.WriteUInt32(Packet, 24, 0);
		Assert.Equal(0u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
		p.WriteUInt32(Packet, 4, 0x9000);
		Assert.Equal(1u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
		Assert.Equal(1u, MuiMiscSpecialistCore.MccprefsRegistryCount(ref p,
			instance));
		p.WriteUInt32(Packet, 12, 99);
		Assert.Equal(1u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
		Assert.Equal(1u, MuiMiscSpecialistCore.MccprefsRegistryCount(ref p,
			instance));
		p.WriteUInt32(Packet, 8, 0);
		Assert.Equal(1u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
		Assert.Equal(0u, MuiMiscSpecialistCore.MccprefsRegistryCount(ref p,
			instance));
		p.WriteUInt32(Packet, 0, 0x00000102u);
		Assert.Equal(1u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
	}

	[Fact]
	public void ObjectDispatcherRoutesMccprefsConfigToGadgets()
	{
		var p = NewPlatform();
		p.WriteCString(ClassId, "Mccprefs.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref p, DrawState));
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, DrawState,
			ClassId, APTR.Null, 0, APTR.FromPointer(1)).IsNotNull);
		var obj = MuiObjectFactoryServiceCore.NewObjectA(ref p, DrawState,
			ClassId, APTR.Null);
		Assert.True(obj.IsNotNull);
		p.WriteUInt32(Packet, 0, MuiMiscAttributes.Mccprefs_ConfigToGadgets);
		p.WriteUInt32(Packet, 4, App.Raw);
		Assert.Equal(0u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
		var instance = MuiMiscSpecialistCore.ObjectInstance(ref p, DrawState, obj);
		Assert.True(MuiMiscSpecialistCore.MccprefsRegisterGadget(ref p, instance,
			APTR.FromPointer(0x9000), 10, 0, Text, 0, APTR.Null));
		p.WriteUInt32(Packet, 4, Win.Raw);
		Assert.Equal(1u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
		p.WriteUInt32(Packet, 0, 0x00000102u);
		Assert.Equal(1u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
	}

	[Fact]
	public void ObjectDispatcherRoutesMccprefsGadgetsToConfig()
	{
		var p = NewPlatform();
		p.WriteCString(ClassId, "Mccprefs.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref p, DrawState));
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, DrawState,
			ClassId, APTR.Null, 0, APTR.FromPointer(1)).IsNotNull);
		var obj = MuiObjectFactoryServiceCore.NewObjectA(ref p, DrawState,
			ClassId, APTR.Null);
		Assert.True(obj.IsNotNull);
		var instance = MuiMiscSpecialistCore.ObjectInstance(ref p, DrawState, obj);
		p.WriteUInt32(Packet, 0, MuiMiscAttributes.Mccprefs_GadgetsToConfig);
		p.WriteUInt32(Packet, 4, App.Raw);
		p.WriteUInt32(Packet, 8, Win.Raw);
		Assert.Equal(0u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
		Assert.True(MuiMiscSpecialistCore.MccprefsRegisterGadget(ref p, instance,
			APTR.FromPointer(0x9000), 10, 0, Text, 0, APTR.Null));
		Assert.Equal(1u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
		p.WriteUInt32(Packet, 0, 0x00000102u);
		Assert.Equal(1u, MuiMiscObjectDispatcher.Dispatch(ref p, DrawState, obj,
			Packet));
	}

	[Fact]
	public void MiscSpecialistPacketCodecUsesNamedRecordsAndRejectsMalformed()
	{
		var p = NewPlatform();
		Assert.True(MuiMiscSpecialistMessageCodec.WriteLifecycle(ref p, Packet,
			MuiMiscAttributes.Setup));
		Assert.True(MuiMiscSpecialistMessageCodec.TryReadMethodId(ref p, Packet,
			out var header));
		Assert.Equal(MuiMiscAttributes.Setup, header.MethodId);
		Assert.True(MuiMiscSpecialistMessageCodec.TryReadLifecycle(ref p, Packet,
			MuiMiscAttributes.Setup, out var lifecycle));
		Assert.Equal(MuiMiscAttributes.Setup, lifecycle.MethodId);

		Assert.True(MuiMiscSpecialistMessageCodec.WriteGet(ref p, Packet,
			MuiMiscAttributes.FSProtectionBits_Flags, Storage.Raw));
		Assert.True(MuiMiscSpecialistMessageCodec.TryReadGet(ref p, Packet,
			out var get));
		Assert.Equal(MuiMiscAttributes.FSProtectionBits_Flags, get.Attribute);
		Assert.Equal(Storage.Raw, get.Storage);

		Assert.True(MuiMiscSpecialistMessageCodec.WriteSet(ref p, Packet,
			MuiMiscSpecialistMessageCodec.MethodSet,
			MuiMiscAttributes.FSProtectionBits_Flags, 0x55));
		Assert.True(MuiMiscSpecialistMessageCodec.TryReadSet(ref p, Packet,
			MuiMiscSpecialistMessageCodec.MethodSet, out var set));
		Assert.Equal(0x55u, set.Value);

		Assert.True(MuiMiscSpecialistMessageCodec.WritePointer(ref p, Packet,
			MuiMiscAttributes.Title_Close, 0x3300));
		Assert.True(MuiMiscSpecialistMessageCodec.TryReadPointer(ref p, Packet,
			MuiMiscAttributes.Title_Close, out var pointer));
		Assert.Equal(0x3300u, pointer.Pointer);

		Assert.True(MuiMiscSpecialistMessageCodec.WritePair(ref p, Packet,
			MuiMiscAttributes.Panel_Run, App.Raw, Win.Raw));
		Assert.True(MuiMiscSpecialistMessageCodec.TryReadPair(ref p, Packet,
			MuiMiscAttributes.Panel_Run, out var pair));
		Assert.Equal(App.Raw, pair.First);
		Assert.Equal(Win.Raw, pair.Second);

		Assert.True(MuiMiscSpecialistMessageCodec.WriteRegisterGadget(ref p,
			Packet, 0x3300, 7, 8, Text.Raw, 9, 10));
		Assert.True(MuiMiscSpecialistMessageCodec.TryReadRegisterGadget(ref p,
			Packet, out var gadget));
		Assert.Equal(7u, gadget.Id);
		Assert.Equal(Text.Raw, gadget.Title);
		Assert.Equal(10u, gadget.Label);

		Assert.False(MuiMiscSpecialistMessageCodec.TryReadGet(ref p,
			APTR.FromPointer(0x40FFF), out _));
		Assert.False(MuiMiscSpecialistMessageCodec.TryReadSet(ref p, Packet,
			0xDEADBEEFu, out _));
		Assert.False(MuiMiscSpecialistMessageCodec.TryReadLifecycle(ref p, Packet,
			0xDEADBEEFu, out _));
	}

	[Fact]
	public void MiscSpecialistTypedReadersUseNamedMethodHeader()
	{
		var p = NewPlatform();
		Assert.True(MuiMiscSpecialistMessageCodec.WriteGet(ref p, Packet,
			MuiMiscAttributes.FSProtectionBits_Flags, Storage.Raw));
		Assert.True(MuiMiscSpecialistMessageCodec.TryReadGet(ref p, Packet,
			out var packet));
		Assert.Equal(MuiMiscSpecialistMessageCodec.OmGet, packet.MethodId);
		Assert.True(MuiMiscSpecialistFieldCursorCodec.TryWriteUInt32(ref p,
			Packet, MuiMiscSpecialistPacketKind.Get,
			MuiMiscSpecialistField.MethodId, 0xDEADBEEFu));
		Assert.False(MuiMiscSpecialistMessageCodec.TryReadGet(ref p, Packet,
			out _));
	}

	[Fact]
	public void MiscSpecialistFieldCursorUsesNamedMixedPacketBoundaries()
	{
		var p = NewPlatform();
		var cursor = default(MuiMiscSpecialistFieldCursor);
		cursor.Message = Packet;
		cursor.Packet = MuiMiscSpecialistPacketKind.Get;
		cursor.Field = MuiMiscSpecialistField.MethodId;
		Assert.True(MuiMiscSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out var address));
		Assert.Equal(Packet.Raw, address.Raw);
		cursor.Field = MuiMiscSpecialistField.Attribute;
		Assert.True(MuiMiscSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out address));
		Assert.Equal(Packet.Raw + 4, address.Raw);
		cursor.Field = MuiMiscSpecialistField.Storage;
		Assert.True(MuiMiscSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out address));
		Assert.Equal(Packet.Raw + 8, address.Raw);

		Assert.True(MuiMiscSpecialistFieldCursorCodec.TryReadUInt32(ref p,
			Packet, MuiMiscSpecialistPacketKind.RegisterGadget,
			MuiMiscSpecialistField.Label, out var label));
		Assert.Equal(0u, label);
		Assert.True(MuiMiscSpecialistFieldCursorCodec.TryWriteUInt32(ref p,
			Packet, MuiMiscSpecialistPacketKind.RegisterGadget,
			MuiMiscSpecialistField.Label, 0xAABBCCDD));
		Assert.True(MuiMiscSpecialistFieldCursorCodec.TryReadUInt32(ref p,
			Packet, MuiMiscSpecialistPacketKind.RegisterGadget,
			MuiMiscSpecialistField.Label, out label));
		Assert.Equal(0xAABBCCDDu, label);

		cursor.Packet = MuiMiscSpecialistPacketKind.Pointer;
		cursor.Field = MuiMiscSpecialistField.Second;
		Assert.False(MuiMiscSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out _));
		cursor.Message = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Packet = MuiMiscSpecialistPacketKind.RegisterGadget;
		cursor.Field = MuiMiscSpecialistField.Label;
		Assert.False(MuiMiscSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out _));
	}

	// ---- Standalone dispatcher -----------------------------------------------

	[Fact]
	public void StandaloneDispatcherRoutesSetGetDisposeAndMethods()
	{
		var p = NewPlatform();
		CreateNamed(ref p, "FSProtectionBits.mui");
		// OM_SET single-tag frame.
		p.WriteUInt32(Packet, 0, 0x8042549au);
		p.WriteUInt32(Packet, 4, MuiMiscAttributes.FSProtectionBits_Flags);
		p.WriteUInt32(Packet, 8, 0x55);
		Assert.Equal(1u, MuiMiscSpecialistDispatcher.Dispatch(ref p, Instance, Packet));
		// OM_GET storage frame.
		p.WriteUInt32(Packet, 0, 0x00000104u);
		p.WriteUInt32(Packet, 4, MuiMiscAttributes.FSProtectionBits_Flags);
		p.WriteUInt32(Packet, 8, Storage.Raw);
		Assert.Equal(1u, MuiMiscSpecialistDispatcher.Dispatch(ref p, Instance, Packet));
		Assert.Equal(0x55u, p.ReadUInt32(Storage, 0));

		// Title methods route through the dispatcher.
		var q = NewPlatform();
		CreateNamed(ref q, "Title.mui");
		p = q;
		p.WriteUInt32(Packet, 0, MuiMiscAttributes.Title_New);
		var handle = MuiMiscSpecialistDispatcher.Dispatch(ref p, Instance, Packet);
		Assert.NotEqual(0u, handle);
		p.WriteUInt32(Packet, 0, MuiMiscAttributes.Title_FindPage);
		p.WriteUInt32(Packet, 4, handle);
		Assert.Equal(0u, MuiMiscSpecialistDispatcher.Dispatch(ref p, Instance, Packet));

		// OM_DISPOSE routes to the family lifecycle.
		p.WriteUInt32(Packet, 0, 0x00000102u);
		Assert.Equal(1u, MuiMiscSpecialistDispatcher.Dispatch(ref p, Instance, Packet));
		Assert.False(MuiMiscSpecialistCore.Valid(ref p, Instance));
	}

	[Fact]
	public void StandaloneDispatcherRoutesStructFirstSetupAndCleanup()
	{
		var p = NewPlatform();
		Assert.Equal(MuiMiscSpecialistClass.Keyadjust,
			CreateNamed(ref p, "Keyadjust.mui"));
		p.WriteUInt32(Packet, 0, MuiMiscAttributes.Setup);
		Assert.Equal(1u, MuiMiscSpecialistDispatcher.Dispatch(ref p,
			Instance, Packet));
		Assert.True(MuiMiscSpecialistCore.IsSetupActive(ref p, Instance));

		// Repeated setup is deliberately harmless, matching the lifecycle's
		// state-oriented contract rather than counting nested calls.
		Assert.Equal(1u, MuiMiscSpecialistDispatcher.Dispatch(ref p,
			Instance, Packet));
		Assert.True(MuiMiscSpecialistCore.IsSetupActive(ref p, Instance));

		p.WriteUInt32(Packet, 0, MuiMiscAttributes.Cleanup);
		Assert.Equal(1u, MuiMiscSpecialistDispatcher.Dispatch(ref p,
			Instance, Packet));
		Assert.False(MuiMiscSpecialistCore.IsSetupActive(ref p, Instance));

		// Unknown methods remain unclaimed so an outer dispatcher can continue.
		p.WriteUInt32(Packet, 0, 0xDEADBEEFu);
		Assert.Equal(0u, MuiMiscSpecialistDispatcher.Dispatch(ref p,
			Instance, Packet));
		Assert.True(MuiMiscSpecialistLifecycle.Dispose(ref p, Instance));
		Assert.False(MuiMiscSpecialistDispatcher.TryDispatch(ref p, Instance,
			Packet, out _));
	}

	// ---- helpers -------------------------------------------------------------

	private static void AssertBoolIsg(ref MuiHeadlessTestPlatform p, uint attribute)
	{
		Assert.True(MuiMiscSpecialistCore.SetAttribute(ref p, Instance, attribute,
			1, false, true, out var changed));
		Assert.True(changed);
		MuiMiscSpecialistCore.GetAttribute(ref p, Instance, attribute, out var value);
		Assert.Equal(1u, value);
		Assert.True(MuiMiscSpecialistCore.SetAttribute(ref p, Instance, attribute,
			0, false, true, out _));
		MuiMiscSpecialistCore.GetAttribute(ref p, Instance, attribute, out var cleared);
		Assert.Equal(0u, cleared);
	}

	private static string ReadCString(ref MuiHeadlessTestPlatform p, APTR address)
	{
		var chars = new System.Text.StringBuilder();
		for (var i = 0; i < 256; i++)
		{
			var b = p.ReadUInt8(address, i);
			if (b == 0) break;
			chars.Append((char)b);
		}
		return chars.ToString();
	}
}
