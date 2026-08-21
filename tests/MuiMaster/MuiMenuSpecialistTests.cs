using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiMenuSpecialistTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint Base = 0x1000;
	private const int Size = 0x40000;
	private const uint FirstAllocation = 0x10000;

	// Fixed class-id buffers.
	private static readonly APTR MenustripName = APTR.FromPointer(0x1100);
	private static readonly APTR MenuName = APTR.FromPointer(0x1120);
	private static readonly APTR MenuitemName = APTR.FromPointer(0x1140);
	private static readonly APTR ApplicationName = APTR.FromPointer(0x1160);
	// Fixed string buffers.
	private static readonly APTR TitleA = APTR.FromPointer(0x1200);
	private static readonly APTR TitleB = APTR.FromPointer(0x1240);
	private static readonly APTR Shortcut = APTR.FromPointer(0x1280);
	private static readonly APTR Storage = APTR.FromPointer(0x1300);
	private static readonly APTR Packet = APTR.FromPointer(0x1340);

	private static MuiHeadlessTestPlatform NewPlatform()
	{
		var p = new MuiHeadlessTestPlatform(Base, Size, FirstAllocation, State);
		Assert.True(MuiHeadlessObjectCore.Initialize(ref p, State));
		p.WriteCString(MenustripName, "Menustrip.mui");
		p.WriteCString(MenuName, "Menu.mui");
		p.WriteCString(MenuitemName, "Menuitem.mui");
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, State,
			MenustripName, APTR.Null, 0, APTR.FromPointer(9)).IsNotNull);
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, State,
			MenuName, APTR.Null, 0, APTR.FromPointer(10)).IsNotNull);
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, State,
			MenuitemName, APTR.Null, 0, APTR.FromPointer(11)).IsNotNull);
		return p;
	}

	private static APTR Create(ref MuiHeadlessTestPlatform p, APTR className,
		MuiMenuSpecialistClass expected)
	{
		var classRecord = MuiHeadlessObjectCore.FindClassByName(ref p, State,
			className);
		Assert.True(classRecord.IsNotNull);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref p, State, classRecord,
			APTR.Null);
		Assert.True(obj.IsNotNull);
		Assert.True(MuiMenuSpecialistCore.Attach(ref p, State, obj, expected)
			.IsNotNull);
		return obj;
	}

	[Fact]
	public void MenuRecordFieldCursorUsesNamedSidecarFields()
	{
		var p = NewPlatform();
		var cursor = default(MuiMenuRecordFieldCursor);
		cursor.Address = APTR.FromPointer(0x1400);
		cursor.Field = MuiMenuRecordField.NotifyCount;
		Assert.True(MuiMenuRecordFieldCursorCodec.TryGetAddress(ref p, cursor,
			out var fieldAddress));
		Assert.Equal(0x142Cu, fieldAddress.Raw);
		cursor.Field = MuiMenuRecordField.TitleOwned;
		Assert.True(MuiMenuRecordFieldCursorCodec.TryGetAddress(ref p, cursor,
			out fieldAddress));
		Assert.Equal(0x140Cu, fieldAddress.Raw);
		Assert.True(MuiMenuRecordFieldCursorCodec.TryWriteUInt32(ref p,
			cursor.Address, MuiMenuRecordField.Flags, 0xA5A5u));
		Assert.True(MuiMenuRecordFieldCursorCodec.TryReadUInt32(ref p,
			cursor.Address, MuiMenuRecordField.Flags, out var flags));
		Assert.Equal(0xA5A5u, flags);
		cursor.Field = (MuiMenuRecordField)255;
		Assert.False(MuiMenuRecordFieldCursorCodec.TryGetAddress(ref p, cursor,
			out _));
		cursor.Address = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Field = MuiMenuRecordField.NotifyValue;
		Assert.False(MuiMenuRecordFieldCursorCodec.TryGetAddress(ref p, cursor,
			out _));
	}

	private static APTR Strip(ref MuiHeadlessTestPlatform p) =>
		Create(ref p, MenustripName, MuiMenuSpecialistClass.Menustrip);
	private static APTR Menu(ref MuiHeadlessTestPlatform p) =>
		Create(ref p, MenuName, MuiMenuSpecialistClass.Menu);
	private static APTR Item(ref MuiHeadlessTestPlatform p) =>
		Create(ref p, MenuitemName, MuiMenuSpecialistClass.Menuitem);

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

	// ---- Classification and inheritance --------------------------------------

	[Fact]
	public void ExactClassNamesAreClassified()
	{
		var p = NewPlatform();
		Assert.Equal(MuiMenuSpecialistClass.Menustrip,
			MuiMenuSpecialistCore.ClassifyName(ref p, MenustripName));
		Assert.Equal(MuiMenuSpecialistClass.Menu,
			MuiMenuSpecialistCore.ClassifyName(ref p, MenuName));
		Assert.Equal(MuiMenuSpecialistClass.Menuitem,
			MuiMenuSpecialistCore.ClassifyName(ref p, MenuitemName));
	}

	[Fact]
	public void MiscasedAndUnknownNamesAreRejected()
	{
		var p = NewPlatform();
		p.WriteCString(Storage, "menu.mui");
		Assert.Equal(MuiMenuSpecialistClass.None,
			MuiMenuSpecialistCore.ClassifyName(ref p, Storage));
		p.WriteCString(Storage, "Menubar.mui");
		Assert.Equal(MuiMenuSpecialistClass.None,
			MuiMenuSpecialistCore.ClassifyName(ref p, Storage));
		Assert.Equal(MuiMenuSpecialistClass.None,
			MuiMenuSpecialistCore.ClassifyName(ref p, APTR.Null));
	}

	[Fact]
	public void EveryMenuClassDescendsFromFamily()
	{
		Assert.Equal(MuiMenuSpecialistClass.None,
			MuiMenuSpecialistCore.Superclass(MuiMenuSpecialistClass.Menustrip));
		Assert.Equal(MuiMenuSpecialistClass.None,
			MuiMenuSpecialistCore.Superclass(MuiMenuSpecialistClass.Menu));
		Assert.Equal(MuiMenuSpecialistClass.None,
			MuiMenuSpecialistCore.Superclass(MuiMenuSpecialistClass.Menuitem));
	}

	// ---- Creation defaults ---------------------------------------------------

	[Fact]
	public void EverythingIsEnabledByDefault()
	{
		var p = NewPlatform();
		var strip = Strip(ref p);
		var menu = Menu(ref p);
		var item = Item(ref p);
		Assert.True(MuiMenuSpecialistCore.GetAttribute(ref p, State, strip,
			MuiMenuAttributes.Menustrip_Enabled, out var e1) && e1 == 1);
		Assert.True(MuiMenuSpecialistCore.GetAttribute(ref p, State, menu,
			MuiMenuAttributes.Menu_Enabled, out var e2) && e2 == 1);
		Assert.True(MuiMenuSpecialistCore.GetAttribute(ref p, State, item,
			MuiMenuAttributes.Menuitem_Enabled, out var e3) && e3 == 1);
	}

	[Fact]
	public void AttachIsClassifiedAndValidated()
	{
		var p = NewPlatform();
		var strip = Strip(ref p);
		Assert.True(MuiMenuSpecialistCore.Valid(ref p, State, strip));
		Assert.Equal(MuiMenuSpecialistClass.Menustrip,
			MuiMenuSpecialistCore.Classify(ref p, State, strip));
		// A second attach on the same object fails.
		Assert.True(MuiMenuSpecialistCore.Attach(ref p, State, strip,
			MuiMenuSpecialistClass.Menustrip).IsNull);
	}

	[Fact]
	public void MenuSidecarCodecUsesNamedStateFields()
	{
		var p = NewPlatform();
		var address = APTR.FromPointer(0x1500);
		var value = default(MuiMenuSpecialistState);
		value.Magic = MuiMenuSpecialistState.Cookie;
		value.Class = (uint)MuiMenuSpecialistClass.Menuitem;
		value.ChangeDepth = 3;
		value.TitleOwned = APTR.FromPointer(0x1800);
		value.TitleOwnedSize = 7;
		value.ShortcutOwned = APTR.FromPointer(0x1900);
		value.ShortcutOwnedSize = 4;
		value.Flags = MuiMenuSpecialistLayout.FlagCopyStrings;
		value.Trigger = 0x1A00;
		value.NotifyAttribute = MuiMenuAttributes.Menuitem_Title;
		value.NotifyValue = 0x1B00;
		value.NotifyCount = 9;
		Assert.True(MuiMenuSpecialistStateCodec.Write(ref p, address, value));
		Assert.True(MuiMenuSpecialistStateCodec.TryRead(ref p, address,
			out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Class, decoded.Class);
		Assert.Equal(value.ChangeDepth, decoded.ChangeDepth);
		Assert.Equal(value.TitleOwned, decoded.TitleOwned);
		Assert.Equal(value.TitleOwnedSize, decoded.TitleOwnedSize);
		Assert.Equal(value.ShortcutOwned, decoded.ShortcutOwned);
		Assert.Equal(value.ShortcutOwnedSize, decoded.ShortcutOwnedSize);
		Assert.Equal(value.Flags, decoded.Flags);
		Assert.Equal(value.Trigger, decoded.Trigger);
		Assert.Equal(value.NotifyAttribute, decoded.NotifyAttribute);
		Assert.Equal(value.NotifyValue, decoded.NotifyValue);
		Assert.Equal(value.NotifyCount, decoded.NotifyCount);
		Assert.False(MuiMenuSpecialistStateCodec.TryRead(ref p,
			APTR.FromPointer(0x41000), out _));
	}

	// ---- Owned hierarchy -----------------------------------------------------

	[Fact]
	public void WellFormedHierarchyIsBuilt()
	{
		var p = NewPlatform();
		var strip = Strip(ref p);
		var menu = Menu(ref p);
		var item = Item(ref p);
		var sub = Item(ref p);
		Assert.True(MuiMenuSpecialistCore.AddChild(ref p, State, strip, menu));
		Assert.True(MuiMenuSpecialistCore.AddChild(ref p, State, menu, item));
		Assert.True(MuiMenuSpecialistCore.AddChild(ref p, State, item, sub));
		Assert.Equal(1u, MuiMenuSpecialistCore.ChildCount(ref p, State, strip));
		Assert.Equal(1u, MuiMenuSpecialistCore.ChildCount(ref p, State, menu));
		Assert.Equal(1u, MuiMenuSpecialistCore.ChildCount(ref p, State, item));
		Assert.True(MuiMenuSpecialistCore.GetAttribute(ref p, State, strip,
			MuiMenuAttributes.Family_ChildCount, out var count) && count == 1);
	}

	[Fact]
	public void MalformedNestingIsRejected()
	{
		var p = NewPlatform();
		var strip = Strip(ref p);
		var menu = Menu(ref p);
		var menu2 = Menu(ref p);
		var item = Item(ref p);
		var sub = Item(ref p);
		var sub2 = Item(ref p);
		// Menustrip only accepts Menu.
		Assert.False(MuiMenuSpecialistCore.AddChild(ref p, State, strip, item));
		// Menu only accepts Menuitem.
		Assert.False(MuiMenuSpecialistCore.AddChild(ref p, State, menu, menu2));
		// Build strip -> menu -> item -> sub.
		Assert.True(MuiMenuSpecialistCore.AddChild(ref p, State, strip, menu));
		Assert.True(MuiMenuSpecialistCore.AddChild(ref p, State, menu, item));
		Assert.True(MuiMenuSpecialistCore.AddChild(ref p, State, item, sub));
		// One-level nesting: a sub-item may not gain its own sub-items.
		Assert.False(MuiMenuSpecialistCore.AddChild(ref p, State, sub, sub2));
	}

	[Fact]
	public void ReparentingIsRejectedByFamily()
	{
		var p = NewPlatform();
		var strip = Strip(ref p);
		var menu = Menu(ref p);
		var strip2 = Strip(ref p);
		Assert.True(MuiMenuSpecialistCore.AddChild(ref p, State, strip, menu));
		Assert.False(MuiMenuSpecialistCore.AddChild(ref p, State, strip2, menu));
		Assert.True(MuiMenuSpecialistCore.RemoveChild(ref p, State, strip, menu));
		Assert.True(MuiMenuSpecialistCore.AddChild(ref p, State, strip2, menu));
	}

	// ---- String ownership / CopyStrings --------------------------------------

	[Fact]
	public void CopyStringsDuplicatesTitleAndSurvivesCallerMutation()
	{
		var p = NewPlatform();
		var menu = Menu(ref p);
		p.WriteCString(TitleA, "Project");
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, menu,
			MuiMenuAttributes.Menu_CopyStrings, 1, true, false, out _));
		Assert.True(MuiMenuSpecialistCore.CopyStringsFlag(ref p, State, menu));
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, menu,
			MuiMenuAttributes.Menu_Title, TitleA.Raw, true, false, out _));
		Assert.True(MuiMenuSpecialistCore.GetAttribute(ref p, State, menu,
			MuiMenuAttributes.Menu_Title, out var stored));
		// The stored pointer is a class-owned copy, not the caller buffer.
		Assert.NotEqual(TitleA.Raw, stored);
		Assert.Equal("Project", ReadCString(ref p, APTR.FromPointer(stored)));
		// Mutating the caller buffer must not affect the owned copy.
		p.WriteCString(TitleA, "XXXXXXX");
		Assert.Equal("Project", ReadCString(ref p, APTR.FromPointer(stored)));
	}

	[Fact]
	public void NoCopyReferencesCallerStringAndReflectsMutation()
	{
		var p = NewPlatform();
		var menu = Menu(ref p);
		p.WriteCString(TitleA, "Project");
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, menu,
			MuiMenuAttributes.Menu_Title, TitleA.Raw, true, false, out _));
		Assert.True(MuiMenuSpecialistCore.GetAttribute(ref p, State, menu,
			MuiMenuAttributes.Menu_Title, out var stored));
		// Without CopyStrings the caller pointer is referenced directly.
		Assert.Equal(TitleA.Raw, stored);
		p.WriteCString(TitleA, "Edited");
		Assert.Equal("Edited", ReadCString(ref p, APTR.FromPointer(stored)));
		Assert.False(MuiMenuSpecialistCore.CopyStringsFlag(ref p, State, menu));
	}

	[Fact]
	public void MenuitemTitleAndShortcutAreCopiedWhenRequested()
	{
		var p = NewPlatform();
		var item = Item(ref p);
		p.WriteCString(TitleA, "Open");
		p.WriteCString(Shortcut, "O");
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, item,
			MuiMenuAttributes.Menuitem_CopyStrings, 1, true, false, out _));
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, item,
			MuiMenuAttributes.Menuitem_Title, TitleA.Raw, true, false, out _));
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, item,
			MuiMenuAttributes.Menuitem_Shortcut, Shortcut.Raw, true, false, out _));
		Assert.True(MuiMenuSpecialistCore.GetAttribute(ref p, State, item,
			MuiMenuAttributes.Menuitem_Title, out var t));
		Assert.True(MuiMenuSpecialistCore.GetAttribute(ref p, State, item,
			MuiMenuAttributes.Menuitem_Shortcut, out var s));
		Assert.NotEqual(TitleA.Raw, t);
		Assert.NotEqual(Shortcut.Raw, s);
		Assert.Equal("Open", ReadCString(ref p, APTR.FromPointer(t)));
		Assert.Equal("O", ReadCString(ref p, APTR.FromPointer(s)));
	}

	// ---- Change brackets -----------------------------------------------------

	[Fact]
	public void ChangeBracketsNestAndAreUnderflowProtected()
	{
		var p = NewPlatform();
		var strip = Strip(ref p);
		Assert.Equal(0u, MuiMenuSpecialistCore.ChangeDepth(ref p, State, strip));
		// ExitChange without a matching InitChange is protected.
		Assert.False(MuiMenuSpecialistCore.ExitChange(ref p, State, strip));
		Assert.Equal(0u, MuiMenuSpecialistCore.ChangeDepth(ref p, State, strip));
		// Nested brackets.
		Assert.True(MuiMenuSpecialistCore.InitChange(ref p, State, strip));
		Assert.True(MuiMenuSpecialistCore.InitChange(ref p, State, strip));
		Assert.Equal(2u, MuiMenuSpecialistCore.ChangeDepth(ref p, State, strip));
		Assert.True(MuiMenuSpecialistCore.ExitChange(ref p, State, strip));
		Assert.True(MuiMenuSpecialistCore.ExitChange(ref p, State, strip));
		Assert.Equal(0u, MuiMenuSpecialistCore.ChangeDepth(ref p, State, strip));
		Assert.False(MuiMenuSpecialistCore.ExitChange(ref p, State, strip));
	}

	[Fact]
	public void ChangeBracketsAreMenustripOnly()
	{
		var p = NewPlatform();
		var menu = Menu(ref p);
		Assert.False(MuiMenuSpecialistCore.InitChange(ref p, State, menu));
		Assert.False(MuiMenuSpecialistCore.ExitChange(ref p, State, menu));
	}

	// ---- WillOpen / Popup ----------------------------------------------------

	[Fact]
	public void WillOpenRequiresEnabledSettledStrip()
	{
		var p = NewPlatform();
		var strip = Strip(ref p);
		Assert.True(MuiMenuSpecialistCore.WillOpen(ref p, State, strip));
		Assert.True(MuiMenuSpecialistCore.IsWillOpen(ref p, State, strip));
		// A mid-change strip must not open.
		Assert.True(MuiMenuSpecialistCore.InitChange(ref p, State, strip));
		Assert.False(MuiMenuSpecialistCore.WillOpen(ref p, State, strip));
		Assert.True(MuiMenuSpecialistCore.ExitChange(ref p, State, strip));
		// A disabled strip must not open.
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, strip,
			MuiMenuAttributes.Menustrip_Enabled, 0, false, true, out _));
		Assert.False(MuiMenuSpecialistCore.WillOpen(ref p, State, strip));
		Assert.False(MuiMenuSpecialistCore.Popup(ref p, State, strip));
	}

	// ---- Checkit / Toggle / Exclude ------------------------------------------

	[Fact]
	public void ToggleFlipsCheckedAndPublishesTrigger()
	{
		var p = NewPlatform();
		var item = Item(ref p);
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, item,
			MuiMenuAttributes.Menuitem_Checkit, 1, true, false, out _));
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, item,
			MuiMenuAttributes.Menuitem_Toggle, 1, true, false, out _));
		Assert.True(MuiMenuSpecialistCore.TriggerItem(ref p, State, item));
		Assert.True(MuiMenuSpecialistCore.GetAttribute(ref p, State, item,
			MuiMenuAttributes.Menuitem_Checked, out var c1) && c1 == 1);
		Assert.Equal(item.Raw, MuiMenuSpecialistCore.Trigger(ref p, State, item));
		Assert.True(MuiMenuSpecialistCore.GetAttribute(ref p, State, item,
			MuiMenuAttributes.Menuitem_Trigger, out var trig) && trig == item.Raw);
		// A second trigger toggles it back off.
		Assert.True(MuiMenuSpecialistCore.TriggerItem(ref p, State, item));
		Assert.True(MuiMenuSpecialistCore.GetAttribute(ref p, State, item,
			MuiMenuAttributes.Menuitem_Checked, out var c2) && c2 == 0);
	}

	[Fact]
	public void TriggerPublishesUserDataToOwningApplicationMenuAttributes()
	{
		var p = NewPlatform();
		p.WriteCString(ApplicationName, "Application.mui");
		var appClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, State,
			ApplicationName, APTR.Null, 0, APTR.FromPointer(12));
		var application = MuiHeadlessObjectCore.CreateObjectA(ref p, State,
			appClass, APTR.Null);
		Assert.True(application.IsNotNull);

		var strip = Strip(ref p);
		var menu = Menu(ref p);
		var item = Item(ref p);
		Assert.True(MuiMenuSpecialistCore.AddChild(ref p, State, strip, menu));
		Assert.True(MuiMenuSpecialistCore.AddChild(ref p, State, menu, item));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref p, State, item,
			0x80420313, 0x4242, false));
		Assert.True(MuiApplicationWindowCore.SetApplicationMenustripValue(ref p,
			State, application, strip.Raw));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationObjectState(
			ref p, State, application, out var objectState));
		Assert.Equal(strip, objectState.Menustrip);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref p, State,
			application, 0));

		Assert.True(MuiMenuSpecialistCore.TriggerItem(ref p, State, item));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref p, State, application,
			0x80428961, out var action));
		Assert.Equal(0x4242u, action);

		Assert.True(MuiMenuSpecialistCore.TriggerItem(ref p, State, item, true));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref p, State, application,
			0x8042540B, out var help));
		Assert.Equal(0x4242u, help);
	}

	[Fact]
	public void CheckingWithExcludeUnchecksSiblings()
	{
		var p = NewPlatform();
		var menu = Menu(ref p);
		var a = Item(ref p);
		var b = Item(ref p);
		var c = Item(ref p);
		Assert.True(MuiMenuSpecialistCore.AddChild(ref p, State, menu, a));
		Assert.True(MuiMenuSpecialistCore.AddChild(ref p, State, menu, b));
		Assert.True(MuiMenuSpecialistCore.AddChild(ref p, State, menu, c));
		// Radio group: each item excludes the other two positions (bits 0/1/2).
		foreach (var (item, mask) in new[] { (a, 0b110u), (b, 0b101u),
			(c, 0b011u) })
			Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, item,
				MuiMenuAttributes.Menuitem_Exclude, mask, true, false, out _));
		// Check a, then check c: a must be unchecked by the exclusion sweep.
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, a,
			MuiMenuAttributes.Menuitem_Checked, 1, false, true, out _));
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, c,
			MuiMenuAttributes.Menuitem_Checked, 1, false, true, out _));
		Assert.True(MuiMenuSpecialistCore.GetAttribute(ref p, State, a,
			MuiMenuAttributes.Menuitem_Checked, out var ca) && ca == 0);
		Assert.True(MuiMenuSpecialistCore.GetAttribute(ref p, State, c,
			MuiMenuAttributes.Menuitem_Checked, out var cc) && cc == 1);
	}

	// ---- Disabled ------------------------------------------------------------

	[Fact]
	public void DisabledItemIgnoresTrigger()
	{
		var p = NewPlatform();
		var item = Item(ref p);
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, item,
			MuiMenuAttributes.Menuitem_Checkit, 1, true, false, out _));
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, item,
			MuiMenuAttributes.Menuitem_Toggle, 1, true, false, out _));
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, item,
			MuiMenuAttributes.Menuitem_Enabled, 0, false, true, out _));
		Assert.False(MuiMenuSpecialistCore.TriggerItem(ref p, State, item));
		Assert.True(MuiMenuSpecialistCore.GetAttribute(ref p, State, item,
			MuiMenuAttributes.Menuitem_Checked, out var c) && c == 0);
	}

	// ---- Notifications on actual runtime change ------------------------------

	[Fact]
	public void NotificationsOnlyFireOnActualRuntimeChange()
	{
		var p = NewPlatform();
		var item = Item(ref p);
		Assert.Equal(0u, MuiMenuSpecialistCore.NotificationCount(ref p, State,
			item));
		// Init-time set does not notify.
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, item,
			MuiMenuAttributes.Menuitem_Enabled, 0, true, true, out _));
		Assert.Equal(0u, MuiMenuSpecialistCore.NotificationCount(ref p, State,
			item));
		// Runtime change fires once.
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, item,
			MuiMenuAttributes.Menuitem_Enabled, 1, false, true, out var changed1));
		Assert.True(changed1);
		Assert.Equal(1u, MuiMenuSpecialistCore.NotificationCount(ref p, State,
			item));
		Assert.Equal(MuiMenuAttributes.Menuitem_Enabled,
			MuiMenuSpecialistCore.LastNotifiedAttribute(ref p, State, item));
		// Setting the same value again does not notify.
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, item,
			MuiMenuAttributes.Menuitem_Enabled, 1, false, true, out var changed2));
		Assert.False(changed2);
		Assert.Equal(1u, MuiMenuSpecialistCore.NotificationCount(ref p, State,
			item));
	}

	// ---- Init-only policy ----------------------------------------------------

	[Fact]
	public void InitOnlyAttributesAreNotGettableAndNotRuntimeSettable()
	{
		var p = NewPlatform();
		var strip = Strip(ref p);
		// CaseSensitive [I..]: settable at init, not exposed through Get.
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, strip,
			MuiMenuAttributes.Menustrip_CaseSensitive, 1, true, false, out _));
		Assert.True(MuiMenuSpecialistCore.CaseSensitiveFlag(ref p, State, strip));
		Assert.False(MuiMenuSpecialistCore.GetAttribute(ref p, State, strip,
			MuiMenuAttributes.Menustrip_CaseSensitive, out _));
		// A runtime set of an [I..] latch is ignored (no change).
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, strip,
			MuiMenuAttributes.Menustrip_CaseSensitive, 0, false, true,
			out var changed));
		Assert.False(changed);
		Assert.True(MuiMenuSpecialistCore.CaseSensitiveFlag(ref p, State, strip));
	}

	[Fact]
	public void AttributesAreRejectedOnTheWrongClass()
	{
		var p = NewPlatform();
		var strip = Strip(ref p);
		// Menuitem attributes are not valid on a Menustrip.
		Assert.False(MuiMenuSpecialistCore.SetAttribute(ref p, State, strip,
			MuiMenuAttributes.Menuitem_Checked, 1, false, true, out _));
		Assert.False(MuiMenuSpecialistCore.GetAttribute(ref p, State, strip,
			MuiMenuAttributes.Menuitem_Title, out _));
	}

	// ---- Dispatcher ----------------------------------------------------------

	[Fact]
	public void MenuSpecialistPacketCodecUsesNamedRecordsAndRejectsMalformedPackets()
	{
		var p = NewPlatform();
		Assert.True(MuiMenuSpecialistMessageCodec.WriteGet(ref p, Packet,
			MuiMenuAttributes.Menu_Title, Storage.Raw));
		Assert.True(MuiMenuSpecialistMessageCodec.TryReadGet(ref p, Packet,
			out var get));
		Assert.Equal(MuiMenuAttributes.Menu_Title, get.Attribute);
		Assert.Equal(Storage.Raw, get.Storage);

		Assert.True(MuiMenuSpecialistMessageCodec.WriteSet(ref p, Packet,
			MuiMenuSpecialistMessageCodec.MethodSet,
			MuiMenuAttributes.Menu_CopyStrings, 1));
		Assert.True(MuiMenuSpecialistMessageCodec.TryReadSet(ref p, Packet,
			MuiMenuSpecialistMessageCodec.MethodSet, out var set));
		Assert.Equal(MuiMenuAttributes.Menu_CopyStrings, set.Attribute);
		Assert.Equal(1u, set.Value);

		Assert.True(MuiMenuSpecialistMessageCodec.WritePointer(ref p, Packet,
			MuiMenuAttributes.Family_AddTail, 0x2200));
		Assert.True(MuiMenuSpecialistMessageCodec.TryReadPointer(ref p, Packet,
			MuiMenuAttributes.Family_AddTail, out var pointer));
		Assert.Equal(0x2200u, pointer.ObjectPointer);

		Assert.True(MuiMenuSpecialistMessageCodec.WritePair(ref p, Packet,
			MuiMenuAttributes.Family_Insert, 0x2200, 0x2300));
		Assert.True(MuiMenuSpecialistMessageCodec.TryReadPair(ref p, Packet,
			MuiMenuAttributes.Family_Insert, out var pair));
		Assert.Equal(0x2200u, pair.First);
		Assert.Equal(0x2300u, pair.Second);

		Assert.True(MuiMenuSpecialistMessageCodec.WritePopup(ref p, Packet,
			0x2400, 10, 20));
		Assert.True(MuiMenuSpecialistMessageCodec.TryReadPopup(ref p, Packet,
			out var popup));
		Assert.Equal(0x2400u, popup.Window);
		Assert.Equal(10u, popup.X);
		Assert.Equal(20u, popup.Y);

		Assert.True(MuiMenuSpecialistMessageCodec.WriteMethod(ref p, Packet,
			MuiMenuAttributes.Menustrip_InitChange));
		Assert.True(MuiMenuSpecialistMessageCodec.IsValidMethod(ref p, Packet,
			MuiMenuAttributes.Menustrip_InitChange));
		Assert.False(MuiMenuSpecialistMessageCodec.WritePointer(ref p, Packet,
			0x80420000u, 1));
		Assert.False(MuiMenuSpecialistMessageCodec.TryReadPopup(ref p,
			APTR.FromPointer(Base + (uint)Size - 1), out _));
		Assert.False(MuiMenuSpecialistMessageCodec.IsValidMethod(ref p, Packet,
			0x80420000u));
	}

	[Fact]
	public void MenuSpecialistMethodHeaderUsesNamedField()
	{
		var p = NewPlatform();
		Assert.True(MuiMenuSpecialistMessageCodec.WriteMethod(ref p, Packet,
			MuiMenuAttributes.Menustrip_InitChange));
		Assert.True(MuiMenuSpecialistMessageCodec.TryReadMethodId(ref p, Packet,
			out var packet));
		Assert.Equal(MuiMenuAttributes.Menustrip_InitChange, packet.MethodId);
		Assert.False(MuiMenuSpecialistMessageCodec.TryReadMethodId(ref p,
			APTR.Null, out _));
	}

	[Fact]
	public void MenuSpecialistFieldCursorUsesNamedMixedPacketBoundaries()
	{
		var p = NewPlatform();
		var cursor = default(MuiMenuSpecialistFieldCursor);
		cursor.Message = Packet;
		cursor.Packet = MuiMenuSpecialistPacketKind.Get;
		cursor.Field = MuiMenuSpecialistField.MethodId;
		Assert.True(MuiMenuSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out var address));
		Assert.Equal(Packet.Raw, address.Raw);
		cursor.Field = MuiMenuSpecialistField.Attribute;
		Assert.True(MuiMenuSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out address));
		Assert.Equal(Packet.Raw + 4, address.Raw);
		cursor.Field = MuiMenuSpecialistField.Storage;
		Assert.True(MuiMenuSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out address));
		Assert.Equal(Packet.Raw + 8, address.Raw);

		Assert.True(MuiMenuSpecialistFieldCursorCodec.TryWriteUInt32(ref p,
			Packet, MuiMenuSpecialistPacketKind.Popup,
			MuiMenuSpecialistField.Y, 0xAABBCCDD));
		Assert.True(MuiMenuSpecialistFieldCursorCodec.TryReadUInt32(ref p,
			Packet, MuiMenuSpecialistPacketKind.Popup,
			MuiMenuSpecialistField.Y, out var y));
		Assert.Equal(0xAABBCCDDu, y);

		cursor.Packet = MuiMenuSpecialistPacketKind.Pointer;
		cursor.Field = MuiMenuSpecialistField.Second;
		Assert.False(MuiMenuSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out _));
		cursor.Message = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Packet = MuiMenuSpecialistPacketKind.Popup;
		cursor.Field = MuiMenuSpecialistField.Y;
		Assert.False(MuiMenuSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out _));
	}

	[Fact]
	public void MenuSpecialistTypedReadersUseNamedMethodHeader()
	{
		var p = NewPlatform();
		Assert.True(MuiMenuSpecialistMessageCodec.WriteSet(ref p, Packet,
			MuiMenuSpecialistMessageCodec.MethodSet, 9, 11));
		Assert.True(MuiMenuSpecialistMessageCodec.TryReadSet(ref p, Packet,
			MuiMenuSpecialistMessageCodec.MethodSet, out var set));
		Assert.Equal(MuiMenuSpecialistMessageCodec.MethodSet, set.MethodId);
		Assert.False(MuiMenuSpecialistMessageCodec.TryReadSet(ref p, Packet,
			MuiMenuSpecialistMessageCodec.MethodNoNotifySet, out _));

		Assert.True(MuiMenuSpecialistMessageCodec.WriteMethod(ref p, Packet,
			MuiMenuAttributes.Menustrip_WillOpen));
		Assert.True(MuiMenuSpecialistMessageCodec.TryReadMethod(ref p, Packet,
			MuiMenuAttributes.Menustrip_WillOpen, out var method));
		Assert.Equal(MuiMenuAttributes.Menustrip_WillOpen, method.MethodId);
		Assert.False(MuiMenuSpecialistMessageCodec.TryReadMethod(ref p, Packet,
			MuiMenuAttributes.Menustrip_ExitChange, out _));
	}

	[Fact]
	public void DispatcherRoutesSetGetAndFamilyVerbs()
	{
		var p = NewPlatform();
		var strip = Strip(ref p);
		var menu = Menu(ref p);
		// Family_AddTail through the dispatcher.
		p.WriteUInt32(Packet, 0, MuiMenuAttributes.Family_AddTail);
		p.WriteUInt32(Packet, 4, menu.Raw);
		Assert.Equal(1u, MuiMenuSpecialistDispatcher.Dispatch(ref p, State, strip,
			Packet));
		Assert.Equal(1u, MuiMenuSpecialistCore.ChildCount(ref p, State, strip));
		// Set via dispatcher (MUIM_Set single-tag frame).
		p.WriteUInt32(Packet, 0, 0x8042549au);
		p.WriteUInt32(Packet, 4, MuiMenuAttributes.Menu_Enabled);
		p.WriteUInt32(Packet, 8, 0);
		Assert.Equal(1u, MuiMenuSpecialistDispatcher.Dispatch(ref p, State, menu,
			Packet));
		// Get via dispatcher (OM_GET storage frame).
		p.WriteUInt32(Packet, 0, 0x00000104u);
		p.WriteUInt32(Packet, 4, MuiMenuAttributes.Menu_Enabled);
		p.WriteUInt32(Packet, 8, Storage.Raw);
		Assert.Equal(1u, MuiMenuSpecialistDispatcher.Dispatch(ref p, State, menu,
			Packet));
		Assert.Equal(0u, p.ReadUInt32(Storage, 0));
	}

	[Fact]
	public void DispatcherRoutesMenustripMethods()
	{
		var p = NewPlatform();
		var strip = Strip(ref p);
		p.WriteUInt32(Packet, 0, MuiMenuAttributes.Menustrip_InitChange);
		Assert.Equal(1u, MuiMenuSpecialistDispatcher.Dispatch(ref p, State, strip,
			Packet));
		Assert.Equal(1u, MuiMenuSpecialistCore.ChangeDepth(ref p, State, strip));
		p.WriteUInt32(Packet, 0, MuiMenuAttributes.Menustrip_ExitChange);
		Assert.Equal(1u, MuiMenuSpecialistDispatcher.Dispatch(ref p, State, strip,
			Packet));
		// Underflow-protected ExitChange returns 0 through the dispatcher too.
		Assert.Equal(0u, MuiMenuSpecialistDispatcher.Dispatch(ref p, State, strip,
			Packet));
	}

	[Fact]
	public void ServiceDispatcherRoutesFactoryCreatedMenuObjects()
	{
		var p = NewPlatform();
		var menu = MuiObjectFactoryServiceCore.NewObjectA(ref p, State,
			MenuName, APTR.Null);
		Assert.True(menu.IsNotNull);

		// The additive service route gives the menu specialist first refusal while
		// preserving the frozen generic dispatcher as a one-way fallback.
		p.WriteUInt32(Packet, 0, 0x8042549au); // MUIM_Set
		p.WriteUInt32(Packet, 4, MuiMenuAttributes.Menu_Enabled);
		p.WriteUInt32(Packet, 8, 0);
		Assert.Equal(1u, MuiMenuSpecialistDispatcher.Dispatch(ref p, State, menu,
			Packet));
		p.WriteUInt32(Packet, 0, 0x00000104u); // OM_GET
		p.WriteUInt32(Packet, 4, MuiMenuAttributes.Menu_Enabled);
		p.WriteUInt32(Packet, 8, Storage.Raw);
		Assert.Equal(1u, MuiMenuSpecialistDispatcher.Dispatch(ref p, State, menu,
			Packet));
		Assert.Equal(0u, p.ReadUInt32(Storage, 0));

		// Disposal is also specialist-owned and must remove the sidecar before
		// the ordinary object record disappears.
		p.WriteUInt32(Packet, 0, 0x00000102u); // OM_DISPOSE
		Assert.Equal(1u, MuiMenuSpecialistDispatcher.Dispatch(ref p, State, menu,
			Packet));
		Assert.False(MuiMenuSpecialistCore.Valid(ref p, State, menu));
	}

	[Fact]
	public void DispatcherDoesNotClaimNonMenuObjects()
	{
		var p = NewPlatform();
		var classRecord = MuiHeadlessObjectCore.FindClassByName(ref p, State,
			MenuName);
		var plain = MuiHeadlessObjectCore.CreateObjectA(ref p, State, classRecord,
			APTR.Null);
		// No sidecar attached: the dispatcher must not claim it.
		p.WriteUInt32(Packet, 0, MuiMenuAttributes.Menustrip_InitChange);
		Assert.False(MuiMenuSpecialistDispatcher.TryDispatch(ref p, State, plain,
			Packet, out _));
	}

	// ---- Rollback / disposal -------------------------------------------------

	[Fact]
	public void RecursiveDisposalFreesSubtreeAndIsIdempotent()
	{
		var p = NewPlatform();
		var strip = Strip(ref p);
		var menu = Menu(ref p);
		var item = Item(ref p);
		p.WriteCString(TitleA, "Project");
		p.WriteCString(TitleB, "Open");
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, menu,
			MuiMenuAttributes.Menu_CopyStrings, 1, true, false, out _));
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, menu,
			MuiMenuAttributes.Menu_Title, TitleA.Raw, true, false, out _));
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, item,
			MuiMenuAttributes.Menuitem_CopyStrings, 1, true, false, out _));
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, item,
			MuiMenuAttributes.Menuitem_Title, TitleB.Raw, true, false, out _));
		Assert.True(MuiMenuSpecialistCore.AddChild(ref p, State, strip, menu));
		Assert.True(MuiMenuSpecialistCore.AddChild(ref p, State, menu, item));

		var freesBefore = p.FreeCount;
		Assert.True(MuiMenuSpecialistLifecycle.Dispose(ref p, State, strip));
		// The two copied strings plus three sidecars plus the object records are
		// all freed; at minimum the copied title blocks were released.
		Assert.True(p.FreeCount > freesBefore);
		// The objects are gone from the registry, so a repeated disposal no-ops.
		Assert.False(MuiMenuSpecialistCore.Valid(ref p, State, strip));
		Assert.False(MuiMenuSpecialistLifecycle.Dispose(ref p, State, strip));
	}

	[Fact]
	public void FailedStringCopyLeavesPreviousValueIntact()
	{
		// A tiny arena forces the copy allocation to fail; the previous value and
		// owned block must be left untouched (failure-atomic).
		var p = new MuiHeadlessTestPlatform(Base, 0x11000, 0x10000, State);
		Assert.True(MuiHeadlessObjectCore.Initialize(ref p, State));
		p.WriteCString(MenuName, "Menu.mui");
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, State,
			MenuName, APTR.Null, 0, APTR.FromPointer(10)).IsNotNull);
		var classRecord = MuiHeadlessObjectCore.FindClassByName(ref p, State,
			MenuName);
		var menu = MuiHeadlessObjectCore.CreateObjectA(ref p, State, classRecord,
			APTR.Null);
		Assert.True(MuiMenuSpecialistCore.Attach(ref p, State, menu,
			MuiMenuSpecialistClass.Menu).IsNotNull);
		p.WriteCString(TitleA, "Project");
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, menu,
			MuiMenuAttributes.Menu_CopyStrings, 1, true, false, out _));
		Assert.True(MuiMenuSpecialistCore.SetAttribute(ref p, State, menu,
			MuiMenuAttributes.Menu_Title, TitleA.Raw, true, false, out _));
		Assert.True(MuiMenuSpecialistCore.GetAttribute(ref p, State, menu,
			MuiMenuAttributes.Menu_Title, out var beforePtr));
		Assert.Equal("Project", ReadCString(ref p, APTR.FromPointer(beforePtr)));
		// Exhaust the arena so the next copy allocation cannot succeed.
		while (true)
		{
			var block = p.Allocate(0x400, 0x10001);
			if (block.IsNull) break;
		}
		p.WriteCString(TitleB, "Replacement");
		Assert.False(MuiMenuSpecialistCore.SetAttribute(ref p, State, menu,
			MuiMenuAttributes.Menu_Title, TitleB.Raw, false, true, out var changed));
		Assert.False(changed);
		// The previous owned copy is intact and unchanged.
		Assert.True(MuiMenuSpecialistCore.GetAttribute(ref p, State, menu,
			MuiMenuAttributes.Menu_Title, out var afterPtr));
		Assert.Equal(beforePtr, afterPtr);
		Assert.Equal("Project", ReadCString(ref p, APTR.FromPointer(afterPtr)));
	}

	// ---- MakeObject interop --------------------------------------------------

	[Fact]
	public void MakeObjectStripIsAdoptedByTheSpecialist()
	{
		var p = NewPlatform();
		var projectTitle = APTR.FromPointer(0x1400);
		var openTitle = APTR.FromPointer(0x1420);
		var openShortcut = APTR.FromPointer(0x1440);
		var newMenus = APTR.FromPointer(0x1500);
		var parameters = APTR.FromPointer(0x15C0);
		p.WriteCString(projectTitle, "Project");
		p.WriteCString(openTitle, "Open");
		p.WriteCString(openShortcut, "O");
		// NewMenu records (20 bytes each): title, item, end.
		p.WriteUInt8(newMenus, 0, 1);                 // NM_TITLE
		p.WriteUInt32(newMenus, 2, projectTitle.Raw);
		p.WriteUInt8(APTR.FromPointer(newMenus.Raw + 20), 0, 2);   // NM_ITEM
		p.WriteUInt32(APTR.FromPointer(newMenus.Raw + 20), 2, openTitle.Raw);
		p.WriteUInt32(APTR.FromPointer(newMenus.Raw + 20), 6, openShortcut.Raw);
		p.WriteUInt8(APTR.FromPointer(newMenus.Raw + 40), 0, 0);   // NM_END
		p.WriteUInt32(parameters, 0, newMenus.Raw);
		p.WriteUInt32(parameters, 4, 0);

		var strip = MuiMakeObjectServiceCore.MakeObjectA(ref p, State,
			MuiMakeObjectServiceCore.MUIO_MenustripNM, parameters);
		Assert.True(strip.IsNotNull);
		// MUI_MakeObjectA classifies and adopts the complete tree during
		// construction, so its specialist methods are immediately available.
		Assert.Equal(MuiMenuSpecialistClass.Menustrip,
			MuiMenuSpecialistCore.ClassifyObject(ref p, State, strip));
		Assert.True(MuiMenuSpecialistCore.Valid(ref p, State, strip));
		var menu = MuiFamilyCore.GetChild(ref p, State, strip, 0, APTR.Null);
		Assert.True(menu.IsNotNull);
		Assert.Equal(MuiMenuSpecialistClass.Menu,
			MuiMenuSpecialistCore.ClassifyObject(ref p, State, menu));
		Assert.True(MuiMenuSpecialistCore.Valid(ref p, State, menu));
		var item = MuiFamilyCore.GetChild(ref p, State, menu, 0, APTR.Null);
		Assert.True(item.IsNotNull);
		Assert.True(MuiMenuSpecialistCore.Valid(ref p, State, item));
		// MakeObject references the caller title; the specialist reads it.
		Assert.True(MuiMenuSpecialistCore.GetAttribute(ref p, State, item,
			MuiMenuAttributes.Menuitem_Title, out var title));
		Assert.Equal(openTitle.Raw, title);
		Assert.Equal(1u, MuiMenuSpecialistCore.ChildCount(ref p, State, strip));
	}

	[Fact]
	public void PublicNewObjectAAttachesMenuSpecialistImmediately()
	{
		var p = NewPlatform();
		var strip = MuiObjectFactoryServiceCore.NewObjectA(ref p, State,
			MenustripName, APTR.Null);
		Assert.True(strip.IsNotNull);
		Assert.True(MuiMenuSpecialistCore.Valid(ref p, State, strip));
		Assert.Equal(MuiMenuSpecialistClass.Menustrip,
			MuiMenuSpecialistCore.Classify(ref p, State, strip));
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref p, State,
			strip));
		Assert.False(MuiMenuSpecialistCore.Valid(ref p, State, strip));
	}

	[Fact]
	public void ClassServiceNewObjectAAttachesAndReleasesMenuSpecialist()
	{
		var p = NewPlatform();
		var serviceState = APTR.FromPointer(0x1800);
		Assert.True(MuiClassServiceCore.Initialize(ref p, serviceState, State));
		var menu = MuiObjectFactoryServiceCore.NewObjectAWithClassService(ref p,
			serviceState, State, MenuName, APTR.Null);
		Assert.True(menu.IsNotNull);
		Assert.True(MuiMenuSpecialistCore.Valid(ref p, State, menu));
		var menuClassRecord = MuiHeadlessObjectCore.FindClassByName(ref p, State,
			MenuName);
		var menuClassPointer = MuiHeadlessObjectCore.ClassPointer(ref p,
			menuClassRecord);
		Assert.Equal(1u, MuiClassServiceCore.ReferenceCount(ref p, serviceState,
			menuClassPointer));
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref p,
			serviceState, State, menu));
		Assert.False(MuiMenuSpecialistCore.Valid(ref p, State, menu));
		Assert.Equal(0u, MuiClassServiceCore.ReferenceCount(ref p, serviceState,
			menuClassPointer));
	}
}
