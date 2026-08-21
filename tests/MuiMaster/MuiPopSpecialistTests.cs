using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

// Focused host tests for the MG09 Pop* specialist family. They exercise exact
// class-name/inheritance classification, failure-atomic child adoption and
// construction rollback, immediate OpenHook / deferred CloseHook with the exact
// CallHookPkt A0/A2/A1 register delivery, reentrancy, disabled-input policy,
// Popobject window/volatile/follow with rollback, Poplist array materialization
// and selection-to-string, Popasl scheduler-driven ASL integration with tag
// delivery and failure cleanup, Poppen cancel-on-Cleanup, Popcolor ShowAlpha,
// the private Popscreen classification, recursive class-owned disposal, and the
// standalone dispatcher.
public sealed class MuiPopSpecialistTests
{
	private const uint Base = 0x1000;
	private const int Size = 0x40000;
	private const uint FirstAllocation = 0x10000;

	private static readonly APTR DrawState = APTR.FromPointer(0x1000);
	private static readonly APTR Instance = APTR.FromPointer(0x2000);
	private static readonly APTR StringChild = APTR.FromPointer(0x2100);
	private static readonly APTR ButtonChild = APTR.FromPointer(0x2180);
	private static readonly APTR PopObject = APTR.FromPointer(0x2200);
	private static readonly APTR OpenHook = APTR.FromPointer(0x2300);
	private static readonly APTR CloseHook = APTR.FromPointer(0x2380);
	private static readonly APTR StrObjHook = APTR.FromPointer(0x2400);
	private static readonly APTR ObjStrHook = APTR.FromPointer(0x2480);
	private static readonly APTR WindowHook = APTR.FromPointer(0x2500);
	private static readonly APTR StartHook = APTR.FromPointer(0x2580);
	private static readonly APTR StopHook = APTR.FromPointer(0x2600);
	private static readonly APTR ClassId = APTR.FromPointer(0x2700);
	private static readonly APTR Arr = APTR.FromPointer(0x2800);
	private static readonly APTR EntryText = APTR.FromPointer(0x2900);
	private static readonly APTR Storage = APTR.FromPointer(0x2A00);
	private static readonly APTR Packet = APTR.FromPointer(0x2B00);
	private static readonly APTR Tags = APTR.FromPointer(0x2C00);

	private static MuiHeadlessTestPlatform NewPlatform() =>
		new MuiHeadlessTestPlatform(Base, Size, FirstAllocation, DrawState);

	[Fact]
	public void PoplistArrayCodecUsesNamedPointerSlot()
	{
		var p = NewPlatform();
		var address = APTR.FromPointer(0x2E00);
		var expected = default(MuiPoplistArrayEntry);
		expected.Value = APTR.FromPointer(0x2F00);
		Assert.True(MuiPoplistArrayEntryCodec.Write(ref p, address, expected));
		Assert.True(MuiPoplistArrayEntryCodec.TryRead(ref p, address,
			out var actual));
		Assert.Equal(expected.Value, actual.Value);
		Assert.False(MuiPoplistArrayEntryCodec.TryRead(ref p,
			APTR.FromPointer(0x50000), out _));
	}

	[Fact]
	public void PoplistArrayCursorUsesNamedEntryBoundary()
	{
		var p = NewPlatform();
		var cursor = default(MuiPoplistArrayCursor);
		cursor.Base = APTR.FromPointer(0x2400);
		cursor.Index = MuiPopSpecialistLayout.MaximumArray;

		Assert.True(MuiPoplistArrayCursorCodec.TryGetEntry(ref p, cursor,
			out var address));
		Assert.Equal(APTR.FromPointer(0x3400), address);
		cursor.Index = MuiPoplistArrayCursor.MaximumEntries;
		Assert.False(MuiPoplistArrayCursorCodec.TryGetEntry(ref p, cursor,
			out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFF0);
		cursor.Index = 1;
		Assert.False(MuiPoplistArrayCursorCodec.TryGetEntry(ref p, cursor,
			out _));
	}

	[Fact]
	public void PopSpecialistStateCodecUsesNamedFields()
	{
		var p = NewPlatform();
		var address = APTR.FromPointer(0x3000);
		var expected = default(MuiPopSpecialistState);
		expected.Magic = MuiPopSpecialistState.Cookie;
		expected.Class = (uint)MuiPopSpecialistClass.Popobject;
		expected.Flags = MuiPopSpecialistLayout.FlagVolatile;
		expected.StringChild = APTR.FromPointer(0x3100);
		expected.PopObject = APTR.FromPointer(0x3180);
		expected.OpenHook = APTR.FromPointer(0x3200);
		expected.ArrayCount = 7;
		expected.AslType = 3;
		expected.Selected = 0x3300;
		expected.NotifyAttribute = MuiPopAttributes.Popobject_Object;
		expected.NotifyValue = 0x3400;
		expected.NotifyCount = 2;
		Assert.True(MuiPopSpecialistStateCodec.Write(ref p, address, expected));
		Assert.True(MuiPopSpecialistStateCodec.TryRead(ref p, address,
			out var actual));
		Assert.Equal(expected.Class, actual.Class);
		Assert.Equal(expected.StringChild, actual.StringChild);
		Assert.Equal(expected.PopObject, actual.PopObject);
		Assert.Equal(expected.ArrayCount, actual.ArrayCount);
		Assert.Equal(expected.NotifyAttribute, actual.NotifyAttribute);
		Assert.Equal(expected.NotifyValue, actual.NotifyValue);
		Assert.Equal(expected.NotifyCount, actual.NotifyCount);
		Assert.False(MuiPopSpecialistStateCodec.TryRead(ref p,
			APTR.FromPointer(0x50000), out _));
	}

	[Fact]
	public void PopSpecialistStateFieldCursorUsesNamedRecordFields()
	{
		var p = NewPlatform();
		var cursor = default(MuiPopSpecialistRecordFieldCursor);
		cursor.Address = APTR.FromPointer(0x3000);
		cursor.Field = MuiPopSpecialistRecordField.Magic;
		Assert.True(MuiPopSpecialistRecordFieldCursorCodec.TryGetAddress(ref p,
			cursor, out var fieldAddress));
		Assert.Equal(0x3000u, fieldAddress.Raw);

		cursor.Field = MuiPopSpecialistRecordField.NotifyCount;
		Assert.True(MuiPopSpecialistRecordFieldCursorCodec.TryGetAddress(ref p,
			cursor, out fieldAddress));
		Assert.Equal(0x3068u, fieldAddress.Raw);
		Assert.True(MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref p,
			cursor.Address, MuiPopSpecialistRecordField.Flags, 0xA5A5u));
		Assert.True(MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref p,
			cursor.Address, MuiPopSpecialistRecordField.Flags, out var flags));
		Assert.Equal(0xA5A5u, flags);

		cursor.Address = APTR.FromPointer(0xFFFFFFF0u);
		Assert.False(MuiPopSpecialistRecordFieldCursorCodec.TryGetAddress(ref p,
			cursor, out _));
	}

	// Two BOOPSI children so recursive disposal can be observed through the
	// platform allocation counters.
	private static bool CreateNamed(ref MuiHeadlessTestPlatform p, string name,
		out MuiPopSpecialistClass cls)
	{
		p.WriteCString(ClassId, name);
		var sChild = p.NewObject(APTR.FromPointer(0x9000), APTR.Null);
		var bChild = p.NewObject(APTR.FromPointer(0x9000), APTR.Null);
		cls = MuiPopSpecialistCore.CreateByName(ref p, Instance, ClassId, sChild,
			bChild);
		return cls != MuiPopSpecialistClass.None;
	}

	private static void MakeHook(ref MuiHeadlessTestPlatform p, APTR hook,
		uint entry)
	{
		p.WriteUInt32(hook, 8, entry);          // h_Entry
		p.WriteUInt32(hook, 16, hook.Raw + 32); // h_Data
	}

	// ---- Classification & inheritance ----------------------------------------

	[Fact]
	public void ExactClassNamesAreClassified()
	{
		var p = NewPlatform();
		p.WriteCString(ClassId, "Popstring.mui");
		Assert.Equal(MuiPopSpecialistClass.Popstring,
			MuiPopSpecialistCore.ClassifyName(ref p, ClassId));
		p.WriteCString(ClassId, "Popobject.mui");
		Assert.Equal(MuiPopSpecialistClass.Popobject,
			MuiPopSpecialistCore.ClassifyName(ref p, ClassId));
		p.WriteCString(ClassId, "Poplist.mui");
		Assert.Equal(MuiPopSpecialistClass.Poplist,
			MuiPopSpecialistCore.ClassifyName(ref p, ClassId));
		p.WriteCString(ClassId, "Popasl.mui");
		Assert.Equal(MuiPopSpecialistClass.Popasl,
			MuiPopSpecialistCore.ClassifyName(ref p, ClassId));
		p.WriteCString(ClassId, "Popscreen.mui");
		Assert.Equal(MuiPopSpecialistClass.Popscreen,
			MuiPopSpecialistCore.ClassifyName(ref p, ClassId));
		p.WriteCString(ClassId, "Popcolor.mui");
		Assert.Equal(MuiPopSpecialistClass.Popcolor,
			MuiPopSpecialistCore.ClassifyName(ref p, ClassId));
		p.WriteCString(ClassId, "Poppen.mui");
		Assert.Equal(MuiPopSpecialistClass.Poppen,
			MuiPopSpecialistCore.ClassifyName(ref p, ClassId));
	}

	[Fact]
	public void UnknownAndTruncatedNamesAreRejected()
	{
		var p = NewPlatform();
		p.WriteCString(ClassId, "Poplist");           // no ".mui" suffix
		Assert.Equal(MuiPopSpecialistClass.None,
			MuiPopSpecialistCore.ClassifyName(ref p, ClassId));
		p.WriteCString(ClassId, "popstring.mui");     // case-sensitive
		Assert.Equal(MuiPopSpecialistClass.None,
			MuiPopSpecialistCore.ClassifyName(ref p, ClassId));
		p.WriteCString(ClassId, "Group.mui");
		Assert.Equal(MuiPopSpecialistClass.None,
			MuiPopSpecialistCore.ClassifyName(ref p, ClassId));
		Assert.Equal(MuiPopSpecialistClass.None,
			MuiPopSpecialistCore.ClassifyName(ref p, APTR.Null));
	}

	[Fact]
	public void InheritanceMatchesDocumentedHierarchy()
	{
		// Popstring roots at Group; every Pop class descends from Popstring.
		Assert.Equal(MuiPopSpecialistClass.None,
			MuiPopSpecialistCore.Superclass(MuiPopSpecialistClass.Popstring));
		Assert.Equal(MuiPopSpecialistClass.Popstring,
			MuiPopSpecialistCore.Superclass(MuiPopSpecialistClass.Popobject));
		Assert.Equal(MuiPopSpecialistClass.Popobject,
			MuiPopSpecialistCore.Superclass(MuiPopSpecialistClass.Poplist));
		Assert.Equal(MuiPopSpecialistClass.Popobject,
			MuiPopSpecialistCore.Superclass(MuiPopSpecialistClass.Popcolor));
		Assert.Equal(MuiPopSpecialistClass.Popobject,
			MuiPopSpecialistCore.Superclass(MuiPopSpecialistClass.Poppen));
		Assert.Equal(MuiPopSpecialistClass.Popstring,
			MuiPopSpecialistCore.Superclass(MuiPopSpecialistClass.Popasl));
		Assert.Equal(MuiPopSpecialistClass.Popasl,
			MuiPopSpecialistCore.Superclass(MuiPopSpecialistClass.Popscreen));

		Assert.True(MuiPopSpecialistCore.InheritsFrom(MuiPopSpecialistClass.Poplist,
			MuiPopSpecialistClass.Popstring));
		Assert.True(MuiPopSpecialistCore.IsObjectDerived(
			MuiPopSpecialistClass.Poppen));
		Assert.False(MuiPopSpecialistCore.IsObjectDerived(
			MuiPopSpecialistClass.Popasl));
		Assert.True(MuiPopSpecialistCore.IsAslDerived(
			MuiPopSpecialistClass.Popscreen));
		Assert.True(MuiPopSpecialistCore.IsPrivate(
			MuiPopSpecialistClass.Popscreen));
		Assert.False(MuiPopSpecialistCore.IsPrivate(
			MuiPopSpecialistClass.Popstring));
	}

	// ---- Construction rollback -----------------------------------------------

	[Fact]
	public void CreationRequiresBothChildrenAtomically()
	{
		var p = NewPlatform();
		p.WriteCString(ClassId, "Popstring.mui");
		var child = p.NewObject(APTR.FromPointer(0x9000), APTR.Null);
		var before = p.AllocationCount;
		// A missing button child fails and allocates nothing.
		Assert.Equal(MuiPopSpecialistClass.None,
			MuiPopSpecialistCore.CreateByName(ref p, Instance, ClassId, child,
				APTR.Null));
		Assert.Equal(MuiPopSpecialistClass.None,
			MuiPopSpecialistCore.CreateByName(ref p, Instance, ClassId, APTR.Null,
				child));
		Assert.Equal(before, p.AllocationCount);
		Assert.False(MuiPopSpecialistCore.Valid(ref p, Instance));
	}

	[Fact]
	public void PartialAllocationFailureRollsBack()
	{
		// Size the arena so the first owned block (hook scratch, 16 bytes)
		// allocates but the second (Popasl service state) cannot: Create must
		// free the first and report failure atomically. The children are fixed
		// mapped pointers so they never consume the arena.
		var p = new MuiHeadlessTestPlatform(Base, Size, Base + (uint)Size - 16,
			DrawState);
		var s = APTR.FromPointer(0x1500);
		var b = APTR.FromPointer(0x1580);
		var freesBefore = p.FreeCount;
		p.WriteCString(ClassId, "Popasl.mui");
		Assert.Equal(MuiPopSpecialistClass.None,
			MuiPopSpecialistCore.CreateByName(ref p, Instance, ClassId, s, b));
		Assert.False(MuiPopSpecialistCore.Valid(ref p, Instance));
		// The hook scratch that did allocate was freed on the rollback path.
		Assert.True(p.FreeCount > freesBefore);
	}

	// ---- Popstring open / deferred close / hook ABI --------------------------

	[Fact]
	public void OpenInvokesOpenHookImmediatelyWithExactAbi()
	{
		var p = NewPlatform();
		Assert.True(CreateNamed(ref p, "Popstring.mui", out _));
		MakeHook(ref p, OpenHook, 0x00AB0001u);
		Assert.True(MuiPopSpecialistCore.SetAttribute(ref p, Instance,
			MuiPopAttributes.Popstring_OpenHook, OpenHook.Raw, true, false, out _));

		var before = p.HookInvokeCount;
		Assert.True(MuiPopSpecialistCore.Open(ref p, Instance));
		Assert.Equal(before + 1, p.HookInvokeCount);
		// Exact CallHookPkt register delivery: A0 = hook, A2 = object.
		Assert.Equal(OpenHook.Raw, p.LastHookBase.Raw);
		Assert.Equal(Instance.Raw, p.LastHookA2.Raw);
		Assert.True(p.LastHookA1.IsNotNull);       // A1 = message scratch
		Assert.True(MuiPopSpecialistCore.IsOpen(ref p, Instance));
	}

	[Fact]
	public void CloseHookIsDeferredUntilHandleInput()
	{
		var p = NewPlatform();
		Assert.True(CreateNamed(ref p, "Popstring.mui", out _));
		MakeHook(ref p, OpenHook, 0x00AB0001u);
		MakeHook(ref p, CloseHook, 0x00AB0002u);
		MuiPopSpecialistCore.SetAttribute(ref p, Instance,
			MuiPopAttributes.Popstring_OpenHook, OpenHook.Raw, true, false, out _);
		MuiPopSpecialistCore.SetAttribute(ref p, Instance,
			MuiPopAttributes.Popstring_CloseHook, CloseHook.Raw, true, false,
			out _);

		Assert.True(MuiPopSpecialistCore.Open(ref p, Instance));
		var afterOpen = p.HookInvokeCount;

		// Close schedules but does not yet invoke the CloseHook.
		Assert.True(MuiPopSpecialistCore.Close(ref p, Instance, 1));
		Assert.Equal(afterOpen, p.HookInvokeCount);
		Assert.True(MuiPopSpecialistCore.IsCloseDeferred(ref p, Instance));
		Assert.True(MuiPopSpecialistCore.IsOpen(ref p, Instance));

		// The next explicit HandleInput tick invokes the deferred CloseHook.
		Assert.True(MuiPopSpecialistCore.HandleInput(ref p, Instance));
		Assert.Equal(afterOpen + 1, p.HookInvokeCount);
		Assert.Equal(CloseHook.Raw, p.LastHookBase.Raw);
		Assert.False(MuiPopSpecialistCore.IsOpen(ref p, Instance));
		Assert.False(MuiPopSpecialistCore.IsCloseDeferred(ref p, Instance));

		// A second HandleInput has nothing to do.
		Assert.False(MuiPopSpecialistCore.HandleInput(ref p, Instance));
	}

	[Fact]
	public void ReentrantOpenAndCloseAreRejected()
	{
		var p = NewPlatform();
		Assert.True(CreateNamed(ref p, "Popstring.mui", out _));
		Assert.False(MuiPopSpecialistCore.Close(ref p, Instance, 0)); // not open
		Assert.True(MuiPopSpecialistCore.Open(ref p, Instance));
		Assert.False(MuiPopSpecialistCore.Open(ref p, Instance));     // already open
	}

	[Fact]
	public void DisabledStateBlocksOpenAndInput()
	{
		var p = NewPlatform();
		Assert.True(CreateNamed(ref p, "Popstring.mui", out _));
		Assert.True(MuiPopSpecialistCore.SetAttribute(ref p, Instance,
			MuiPopAttributes.Disabled, 1, false, true, out var ch));
		Assert.True(ch);
		Assert.False(MuiPopSpecialistCore.Open(ref p, Instance));
		Assert.False(MuiPopSpecialistCore.IsOpen(ref p, Instance));

		// Re-enable, open, defer a close, then disable: input is ignored while
		// disabled so the deferred CloseHook does not fire.
		MuiPopSpecialistCore.SetAttribute(ref p, Instance, MuiPopAttributes.Disabled,
			0, false, true, out _);
		MakeHook(ref p, CloseHook, 0x00AB0002u);
		MuiPopSpecialistCore.SetAttribute(ref p, Instance,
			MuiPopAttributes.Popstring_CloseHook, CloseHook.Raw, true, false,
			out _);
		Assert.True(MuiPopSpecialistCore.Open(ref p, Instance));
		Assert.True(MuiPopSpecialistCore.Close(ref p, Instance, 1));
		MuiPopSpecialistCore.SetAttribute(ref p, Instance, MuiPopAttributes.Disabled,
			1, false, true, out _);
		var count = p.HookInvokeCount;
		Assert.False(MuiPopSpecialistCore.HandleInput(ref p, Instance));
		Assert.Equal(count, p.HookInvokeCount);
	}

	[Fact]
	public void ToggleOpensAndClosesThePopup()
	{
		var p = NewPlatform();
		Assert.True(CreateNamed(ref p, "Popstring.mui", out _));
		Assert.True(MuiPopSpecialistCore.Toggle(ref p, Instance));
		Assert.True(MuiPopSpecialistCore.IsOpen(ref p, Instance));
		Assert.True(MuiPopSpecialistCore.Toggle(ref p, Instance)); // schedules close
		Assert.True(MuiPopSpecialistCore.IsCloseDeferred(ref p, Instance));
	}

	// ---- Popobject window / volatile / follow / rollback ---------------------

	[Fact]
	public void PopobjectRunsConversionHooksAndVolatileWindow()
	{
		var p = NewPlatform();
		Assert.True(CreateNamed(ref p, "Popobject.mui", out _));
		var obj = p.NewObject(APTR.FromPointer(0x9000), APTR.Null);
		Assert.True(MuiPopSpecialistCore.SetAttribute(ref p, Instance,
			MuiPopAttributes.Popobject_Object, obj.Raw, true, false, out _));
		MakeHook(ref p, StrObjHook, 0x00AC0001u);
		MakeHook(ref p, ObjStrHook, 0x00AC0002u);
		MakeHook(ref p, WindowHook, 0x00AC0003u);
		MuiPopSpecialistCore.SetAttribute(ref p, Instance,
			MuiPopAttributes.Popobject_StrObjHook, StrObjHook.Raw, true, false,
			out _);
		MuiPopSpecialistCore.SetAttribute(ref p, Instance,
			MuiPopAttributes.Popobject_ObjStrHook, ObjStrHook.Raw, true, false,
			out _);
		MuiPopSpecialistCore.SetAttribute(ref p, Instance,
			MuiPopAttributes.Popobject_WindowHook, WindowHook.Raw, true, false,
			out _);

		// Volatile defaults TRUE for Popobject-derived classes.
		Assert.True(MuiPopSpecialistCore.GetAttribute(ref p, Instance,
			MuiPopAttributes.Popobject_Volatile, out var vol) && vol == 1);

		var allocBefore = p.AllocationCount;
		Assert.True(MuiPopSpecialistCore.Open(ref p, Instance));
		// StrObjHook ran on the object, a volatile window was allocated, WindowHook
		// ran on that window.
		Assert.Equal(WindowHook.Raw, p.LastHookBase.Raw);
		Assert.True(p.AllocationCount > allocBefore);

		Assert.True(MuiPopSpecialistCore.Close(ref p, Instance, 1));
		// ObjStrHook (object -> string) ran immediately, targeting the string.
		Assert.Equal(ObjStrHook.Raw, p.LastHookBase.Raw);
		Assert.Equal(StringChildOf(ref p), p.LastHookA2.Raw);

		var freeBefore = p.FreeCount;
		Assert.True(MuiPopSpecialistCore.HandleInput(ref p, Instance));
		// Volatile: the popup window is freed on close.
		Assert.True(p.FreeCount > freeBefore);
		Assert.False(MuiPopSpecialistCore.IsOpen(ref p, Instance));
	}

	private static uint StringChildOf(ref MuiHeadlessTestPlatform p) =>
		p.ReadUInt32(Instance, 12); // MuiPopSpecialistLayout.StringChild

	[Fact]
	public void NonVolatileWindowIsRetainedAcrossClose()
	{
		var p = NewPlatform();
		Assert.True(CreateNamed(ref p, "Popobject.mui", out _));
		MuiPopSpecialistCore.SetAttribute(ref p, Instance,
			MuiPopAttributes.Popobject_Volatile, 0, false, true, out _);
		Assert.True(MuiPopSpecialistCore.Open(ref p, Instance));
		Assert.True(MuiPopSpecialistCore.Close(ref p, Instance, 0));
		var freeBefore = p.FreeCount;
		Assert.True(MuiPopSpecialistCore.HandleInput(ref p, Instance));
		// Non-volatile window kept alive: nothing freed at close.
		Assert.Equal(freeBefore, p.FreeCount);
	}

	[Fact]
	public void FollowAndLightAreStoredAndNotify()
	{
		var p = NewPlatform();
		Assert.True(CreateNamed(ref p, "Popobject.mui", out _));
		Assert.True(MuiPopSpecialistCore.SetAttribute(ref p, Instance,
			MuiPopAttributes.Popobject_Follow, 1, false, true, out var c1));
		Assert.True(c1);
		Assert.True(MuiPopSpecialistCore.GetAttribute(ref p, Instance,
			MuiPopAttributes.Popobject_Follow, out var f) && f == 1);
		Assert.Equal(MuiPopAttributes.Popobject_Follow,
			MuiPopSpecialistCore.LastNotifiedAttribute(ref p, Instance));
		Assert.True(MuiPopSpecialistCore.SetAttribute(ref p, Instance,
			MuiPopAttributes.Popobject_Light, 1, false, true, out _));
		Assert.True(MuiPopSpecialistCore.GetAttribute(ref p, Instance,
			MuiPopAttributes.Popobject_Light, out var l) && l == 1);
	}

	[Fact]
	public void PopobjectAttributesRejectedOnPlainPopstring()
	{
		var p = NewPlatform();
		Assert.True(CreateNamed(ref p, "Popstring.mui", out _));
		Assert.False(MuiPopSpecialistCore.GetAttribute(ref p, Instance,
			MuiPopAttributes.Popobject_Volatile, out _));
		Assert.False(MuiPopSpecialistCore.SetAttribute(ref p, Instance,
			MuiPopAttributes.Popobject_Follow, 1, false, true, out _));
	}

	// ---- Poplist array materialization & selection ---------------------------

	[Fact]
	public void PoplistMaterializesArrayAndSelectsToString()
	{
		var p = NewPlatform();
		Assert.True(CreateNamed(ref p, "Poplist.mui", out _));
		// A NULL-terminated array of three entry pointers.
		p.WriteUInt32(Arr, 0, EntryText.Raw);
		p.WriteUInt32(Arr, 4, EntryText.Raw + 0x40);
		p.WriteUInt32(Arr, 8, EntryText.Raw + 0x80);
		p.WriteUInt32(Arr, 12, 0);
		p.WriteCString(EntryText, "one");
		p.WriteCString(APTR.FromPointer(EntryText.Raw + 0x40), "two");
		p.WriteCString(APTR.FromPointer(EntryText.Raw + 0x80), "three");

		Assert.True(MuiPopSpecialistCore.SetAttribute(ref p, Instance,
			MuiPopAttributes.Poplist_Array, Arr.Raw, true, false, out _));
		Assert.Equal(3u, MuiPopSpecialistCore.ArrayCount(ref p, Instance));

		MakeHook(ref p, ObjStrHook, 0x00AD0001u);
		MuiPopSpecialistCore.SetAttribute(ref p, Instance,
			MuiPopAttributes.Popobject_ObjStrHook, ObjStrHook.Raw, true, false,
			out _);
		var notify = MuiPopSpecialistCore.NotificationCount(ref p, Instance);
		Assert.True(MuiPopSpecialistCore.SelectEntry(ref p, Instance, 1));
		Assert.Equal(EntryText.Raw + 0x40,
			MuiPopSpecialistCore.SelectedEntry(ref p, Instance));
		// Selection routes through the ObjStrHook and notifies String_Contents.
		Assert.Equal(ObjStrHook.Raw, p.LastHookBase.Raw);
		Assert.Equal(notify + 1,
			MuiPopSpecialistCore.NotificationCount(ref p, Instance));
		Assert.Equal(MuiPopAttributes.String_Contents,
			MuiPopSpecialistCore.LastNotifiedAttribute(ref p, Instance));

		Assert.False(MuiPopSpecialistCore.SelectEntry(ref p, Instance, 3)); // OOB
	}

	// ---- Popasl scheduler-driven integration ---------------------------------

	[Fact]
	public void PopaslDrivesRequesterThroughSchedulerTicks()
	{
		var p = NewPlatform();
		Assert.True(CreateNamed(ref p, "Popasl.mui", out _));
		MuiPopSpecialistCore.SetAttribute(ref p, Instance, MuiPopAttributes.Popasl_Type,
			0, true, false, out _);
		MakeHook(ref p, StartHook, 0x00AE0001u);
		MakeHook(ref p, StopHook, 0x00AE0002u);
		MuiPopSpecialistCore.SetAttribute(ref p, Instance,
			MuiPopAttributes.Popasl_StartHook, StartHook.Raw, true, false, out _);
		MuiPopSpecialistCore.SetAttribute(ref p, Instance,
			MuiPopAttributes.Popasl_StopHook, StopHook.Raw, true, false, out _);
		// A valid, TAG_DONE-terminated caller ASL tag list.
		p.WriteUInt32(Tags, 0, 0); // TAG_DONE
		Assert.True(MuiPopSpecialistCore.SetAslTags(ref p, Instance, Tags));

		// Open: StartHook fires, the requester is allocated, Active is set and a
		// scheduler tick is armed. No host task/thread is created.
		Assert.True(MuiPopSpecialistCore.Open(ref p, Instance));
		Assert.Equal(1u, p.AslAllocateCount);
		Assert.Equal(StartHook.Raw, p.LastHookBase.Raw);
		Assert.True(MuiPopSpecialistCore.GetAttribute(ref p, Instance,
			MuiPopAttributes.Popasl_Active, out var active) && active == 1);
		Assert.Equal(0u, p.AslRequestCount);

		// HandleInput runs the requester, invokes StopHook, frees it and clears
		// Active; the caller ASL tags are delivered to the capability.
		Assert.True(MuiPopSpecialistCore.HandleInput(ref p, Instance));
		Assert.Equal(1u, p.AslRequestCount);
		Assert.Equal(1u, p.AslFreeCount);
		Assert.Equal(Tags.Raw, p.LastAslTags.Raw);
		Assert.True(MuiPopSpecialistCore.GetAttribute(ref p, Instance,
			MuiPopAttributes.Popasl_Active, out var active2) && active2 == 0);
	}

	[Fact]
	public void PopaslFailsCleanlyOnMalformedTags()
	{
		var p = NewPlatform();
		Assert.True(CreateNamed(ref p, "Popasl.mui", out _));
		// An odd-aligned tag pointer is malformed and the ASL service rejects it.
		Assert.True(MuiPopSpecialistCore.SetAslTags(ref p, Instance,
			APTR.FromPointer(Tags.Raw + 1)));
		Assert.False(MuiPopSpecialistCore.Open(ref p, Instance));
		Assert.Equal(0u, p.AslAllocateCount);
		Assert.True(MuiPopSpecialistCore.GetAttribute(ref p, Instance,
			MuiPopAttributes.Popasl_Active, out var active) && active == 0);
		Assert.False(MuiPopSpecialistCore.IsOpen(ref p, Instance));
	}

	// ---- Poppen cancel-on-Cleanup --------------------------------------------

	[Fact]
	public void PoppenCancelsPopupOnCleanup()
	{
		var p = NewPlatform();
		Assert.True(CreateNamed(ref p, "Poppen.mui", out _));
		Assert.True(MuiPopSpecialistCore.Open(ref p, Instance));
		Assert.True(MuiPopSpecialistCore.IsOpen(ref p, Instance));
		var freeBefore = p.FreeCount;
		// MUIM_Cleanup cancels the live popup and frees the volatile window.
		Assert.True(MuiPopSpecialistCore.Cleanup(ref p, Instance));
		Assert.False(MuiPopSpecialistCore.IsOpen(ref p, Instance));
		Assert.True(p.FreeCount > freeBefore);
	}

	[Fact]
	public void PopaslCleanupReleasesActiveRequester()
	{
		var p = NewPlatform();
		Assert.True(CreateNamed(ref p, "Popasl.mui", out _));
		p.WriteUInt32(Tags, 0, 0);
		MuiPopSpecialistCore.SetAslTags(ref p, Instance, Tags);
		Assert.True(MuiPopSpecialistCore.Open(ref p, Instance));
		Assert.Equal(0u, p.AslFreeCount);
		Assert.True(MuiPopSpecialistCore.Cleanup(ref p, Instance));
		Assert.Equal(1u, p.AslFreeCount); // active requester released on cleanup
		Assert.True(MuiPopSpecialistCore.GetAttribute(ref p, Instance,
			MuiPopAttributes.Popasl_Active, out var active) && active == 0);
	}

	// ---- Popcolor / Popscreen ------------------------------------------------

	[Fact]
	public void PopcolorShowAlphaInitState()
	{
		var p = NewPlatform();
		Assert.True(CreateNamed(ref p, "Popcolor.mui", out _));
		Assert.True(MuiPopSpecialistCore.SetAttribute(ref p, Instance,
			MuiPopAttributes.Popcolor_ShowAlpha, 1, true, false, out var ch));
		Assert.True(ch);
		Assert.True(MuiPopSpecialistCore.GetAttribute(ref p, Instance,
			MuiPopAttributes.Popcolor_ShowAlpha, out var v) && v == 1);
		// Popcolor is Popobject-derived, so it honours the object contract too.
		Assert.True(MuiPopSpecialistCore.GetAttribute(ref p, Instance,
			MuiPopAttributes.Popobject_Volatile, out var vol) && vol == 1);
	}

	[Fact]
	public void PopscreenIsPrivateAslDerived()
	{
		var p = NewPlatform();
		Assert.True(CreateNamed(ref p, "Popscreen.mui", out var cls));
		Assert.Equal(MuiPopSpecialistClass.Popscreen, cls);
		Assert.True(MuiPopSpecialistCore.IsPrivate(cls));
		// It drives an ASL requester like its Popasl superclass.
		p.WriteUInt32(Tags, 0, 0);
		MuiPopSpecialistCore.SetAslTags(ref p, Instance, Tags);
		Assert.True(MuiPopSpecialistCore.Open(ref p, Instance));
		Assert.Equal(1u, p.AslAllocateCount);
	}

	// ---- Recursive class-owned disposal --------------------------------------

	[Fact]
	public void DisposeRecursivelyReleasesChildrenPopupAndState()
	{
		var p = NewPlatform();
		p.WriteCString(ClassId, "Popobject.mui");
		var sChild = p.NewObject(APTR.FromPointer(0x9000), APTR.Null);
		var bChild = p.NewObject(APTR.FromPointer(0x9000), APTR.Null);
		var obj = p.NewObject(APTR.FromPointer(0x9000), APTR.Null);
		Assert.Equal(MuiPopSpecialistClass.Popobject,
			MuiPopSpecialistCore.CreateByName(ref p, Instance, ClassId, sChild,
				bChild));
		MuiPopSpecialistCore.SetAttribute(ref p, Instance,
			MuiPopAttributes.Popobject_Object, obj.Raw, true, false, out _);

		var alloc = p.AllocationCount;
		var free = p.FreeCount;
		Assert.True(MuiPopSpecialistLifecycle.Dispose(ref p, Instance));
		Assert.False(MuiPopSpecialistCore.Valid(ref p, Instance));
		// The string child, button child, retained popup object and the owned
		// hook scratch are all released (>= 4 frees).
		Assert.True(p.FreeCount - free >= 4);
		Assert.Equal(alloc, p.AllocationCount); // no new allocation during dispose
		// Repeated disposal is a safe no-op.
		Assert.False(MuiPopSpecialistLifecycle.Dispose(ref p, Instance));
	}

	[Fact]
	public void PoplistDisposeFreesMaterializedArray()
	{
		var p = NewPlatform();
		Assert.True(CreateNamed(ref p, "Poplist.mui", out _));
		p.WriteUInt32(Arr, 0, EntryText.Raw);
		p.WriteUInt32(Arr, 4, 0);
		p.WriteCString(EntryText, "x");
		MuiPopSpecialistCore.SetAttribute(ref p, Instance,
			MuiPopAttributes.Poplist_Array, Arr.Raw, true, false, out _);
		var free = p.FreeCount;
		Assert.True(MuiPopSpecialistLifecycle.Dispose(ref p, Instance));
		Assert.True(p.FreeCount > free);
	}

	// ---- Standalone dispatcher -----------------------------------------------

	[Fact]
	public void PopSpecialistPacketCodecUsesNamedRecordsAndRejectsMalformedPackets()
	{
		var p = NewPlatform();
		Assert.True(MuiPopSpecialistMessageCodec.WriteGet(ref p, Packet,
			MuiPopAttributes.Disabled, Storage.Raw));
		Assert.True(MuiPopSpecialistMessageCodec.TryReadGet(ref p, Packet,
			out var get));
		Assert.Equal(MuiPopAttributes.Disabled, get.Attribute);
		Assert.Equal(Storage.Raw, get.Storage);

		Assert.True(MuiPopSpecialistMessageCodec.WriteSet(ref p, Packet,
			MuiPopSpecialistMessageCodec.MethodSet, MuiPopAttributes.Disabled, 1));
		Assert.True(MuiPopSpecialistMessageCodec.TryReadSet(ref p, Packet,
			MuiPopSpecialistMessageCodec.MethodSet, out var set));
		Assert.Equal(MuiPopAttributes.Disabled, set.Attribute);
		Assert.Equal(1u, set.Value);

		Assert.True(MuiPopSpecialistMessageCodec.WriteClose(ref p, Packet, 1));
		Assert.True(MuiPopSpecialistMessageCodec.TryReadClose(ref p, Packet,
			out var close));
		Assert.Equal(1u, close.Result);

		Assert.True(MuiPopSpecialistMessageCodec.WriteMethod(ref p, Packet,
			MuiPopAttributes.Popstring_Open));
		Assert.True(MuiPopSpecialistMessageCodec.IsValidMethod(ref p, Packet,
			MuiPopAttributes.Popstring_Open));
		Assert.False(MuiPopSpecialistMessageCodec.WriteSet(ref p, Packet,
			0x80420000u, 1, 2));
		Assert.False(MuiPopSpecialistMessageCodec.TryReadGet(ref p,
			APTR.FromPointer(Base + (uint)Size - 1), out _));
		Assert.False(MuiPopSpecialistMessageCodec.TryReadClose(ref p,
			APTR.FromPointer(Base + (uint)Size - 1), out _));
		Assert.False(MuiPopSpecialistMessageCodec.IsValidMethod(ref p, Packet,
			0x80420000u));
	}

	[Fact]
	public void PopSpecialistMethodHeaderUsesNamedField()
	{
		var p = NewPlatform();
		Assert.True(MuiPopSpecialistMessageCodec.WriteMethod(ref p, Packet,
			MuiPopSpecialistMessageCodec.OmDispose));
		Assert.True(MuiPopSpecialistMessageCodec.TryReadMethodId(ref p, Packet,
			out var packet));
		Assert.Equal(MuiPopSpecialistMessageCodec.OmDispose, packet.MethodId);
		Assert.False(MuiPopSpecialistMessageCodec.TryReadMethodId(ref p,
			APTR.Null, out _));
	}

	[Fact]
	public void PopSpecialistTypedReadersUseNamedMethodHeader()
	{
		var p = NewPlatform();
		Assert.True(MuiPopSpecialistMessageCodec.WriteClose(ref p, Packet, 1));
		Assert.True(MuiPopSpecialistMessageCodec.TryReadClose(ref p, Packet,
			out var close));
		Assert.Equal(MuiPopAttributes.Popstring_Close, close.MethodId);
		Assert.True(MuiPopSpecialistFieldCursorCodec.TryWriteUInt32(ref p,
			Packet, MuiPopSpecialistPacketKind.Close,
			MuiPopSpecialistField.MethodId, 0xDEADBEEFu));
		Assert.False(MuiPopSpecialistMessageCodec.TryReadClose(ref p, Packet,
			out _));
	}

	[Fact]
	public void PopSpecialistFieldCursorUsesNamedMixedPacketBoundaries()
	{
		var p = NewPlatform();
		var cursor = default(MuiPopSpecialistFieldCursor);
		cursor.Message = Packet;
		cursor.Packet = MuiPopSpecialistPacketKind.Get;
		cursor.Field = MuiPopSpecialistField.MethodId;
		Assert.True(MuiPopSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out var address));
		Assert.Equal(Packet.Raw, address.Raw);
		cursor.Field = MuiPopSpecialistField.Attribute;
		Assert.True(MuiPopSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out address));
		Assert.Equal(Packet.Raw + 4, address.Raw);
		cursor.Field = MuiPopSpecialistField.Storage;
		Assert.True(MuiPopSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out address));
		Assert.Equal(Packet.Raw + 8, address.Raw);

		Assert.True(MuiPopSpecialistFieldCursorCodec.TryWriteUInt32(ref p,
			Packet, MuiPopSpecialistPacketKind.Close,
			MuiPopSpecialistField.Result, 0xAABBCCDD));
		Assert.True(MuiPopSpecialistFieldCursorCodec.TryReadUInt32(ref p,
			Packet, MuiPopSpecialistPacketKind.Close,
			MuiPopSpecialistField.Result, out var result));
		Assert.Equal(0xAABBCCDDu, result);

		cursor.Packet = MuiPopSpecialistPacketKind.Close;
		cursor.Field = MuiPopSpecialistField.Value;
		Assert.False(MuiPopSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out _));
		cursor.Message = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Field = MuiPopSpecialistField.Result;
		Assert.False(MuiPopSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out _));
	}

	[Fact]
	public void DispatcherRoutesSetGetAndMethods()
	{
		var p = NewPlatform();
		Assert.True(CreateNamed(ref p, "Popstring.mui", out _));

		// MUIM_Set { method, attr, value } (Disabled = 1).
		p.WriteUInt32(Packet, 0, 0x8042549au);
		p.WriteUInt32(Packet, 4, MuiPopAttributes.Disabled);
		p.WriteUInt32(Packet, 8, 1);
		Assert.True(MuiPopSpecialistDispatcher.TryDispatch(ref p, Instance, Packet,
			out var setResult) && setResult == 1);

		// OM_GET { method, attr, *storage }.
		p.WriteUInt32(Packet, 0, 0x00000104u);
		p.WriteUInt32(Packet, 4, MuiPopAttributes.Disabled);
		p.WriteUInt32(Packet, 8, Storage.Raw);
		Assert.True(MuiPopSpecialistDispatcher.TryDispatch(ref p, Instance, Packet,
			out var getResult) && getResult == 1);
		Assert.Equal(1u, p.ReadUInt32(Storage, 0));

		// Re-enable so Open works, then MUIM_Popstring_Open / _Close /
		// MUIM_HandleInput through the dispatcher.
		p.WriteUInt32(Packet, 0, 0x8042549au);
		p.WriteUInt32(Packet, 4, MuiPopAttributes.Disabled);
		p.WriteUInt32(Packet, 8, 0);
		MuiPopSpecialistDispatcher.TryDispatch(ref p, Instance, Packet, out _);

		p.WriteUInt32(Packet, 0, MuiPopAttributes.Popstring_Open);
		Assert.True(MuiPopSpecialistDispatcher.TryDispatch(ref p, Instance, Packet,
			out var opened) && opened == 1);
		Assert.True(MuiPopSpecialistCore.IsOpen(ref p, Instance));

		p.WriteUInt32(Packet, 0, MuiPopAttributes.Popstring_Close);
		p.WriteUInt32(Packet, 4, 1);
		Assert.True(MuiPopSpecialistDispatcher.TryDispatch(ref p, Instance, Packet,
			out _));
		Assert.True(MuiPopSpecialistCore.IsCloseDeferred(ref p, Instance));

		p.WriteUInt32(Packet, 0, MuiPopAttributes.HandleInput);
		Assert.True(MuiPopSpecialistDispatcher.TryDispatch(ref p, Instance, Packet,
			out _));
		Assert.False(MuiPopSpecialistCore.IsOpen(ref p, Instance));

		// OM_DISPOSE routes to the recursive lifecycle.
		p.WriteUInt32(Packet, 0, 0x00000102u);
		Assert.True(MuiPopSpecialistDispatcher.TryDispatch(ref p, Instance, Packet,
			out var disposed) && disposed == 1);
		Assert.False(MuiPopSpecialistCore.Valid(ref p, Instance));
	}

	[Fact]
	public void AskMinMaxPublishesBoundedGeometry()
	{
		var p = NewPlatform();
		Assert.True(CreateNamed(ref p, "Popstring.mui", out _));
		Assert.True(MuiPopSpecialistCore.AskMinMax(ref p, Instance, Storage));
		Assert.Equal(40, p.ReadUInt16(Storage, 0));   // MinWidth
		Assert.True(p.ReadUInt16(Storage, 4) >= p.ReadUInt16(Storage, 0));
	}
}
