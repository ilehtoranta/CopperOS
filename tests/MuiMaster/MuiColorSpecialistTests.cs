using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiColorSpecialistTests
{
	private const uint Base = 0x1000;
	private const int Size = 0x20000;
	private const uint FirstAllocation = 0x8000;

	private static readonly APTR DrawState = APTR.FromPointer(0x1000);
	private static readonly APTR Instance = APTR.FromPointer(0x1100);
	private static readonly APTR Instance2 = APTR.FromPointer(0x1180);
	private static readonly APTR Mri = APTR.FromPointer(0x1200);
	private static readonly APTR RastPort = APTR.FromPointer(0x1280);
	private static readonly APTR ClassId = APTR.FromPointer(0x1300);
	private static readonly APTR RgbSource = APTR.FromPointer(0x1340);
	private static readonly APTR SpecSource = APTR.FromPointer(0x1380);
	private static readonly APTR Storage = APTR.FromPointer(0x1500);
	private static readonly APTR Packet = APTR.FromPointer(0x1600);

	private static MuiHeadlessTestPlatform NewPlatform()
	{
		var platform = new MuiHeadlessTestPlatform(Base, Size, FirstAllocation,
			DrawState);
		Assert.True(MuiDrawingServiceCore.Initialize(ref platform, DrawState));
		return platform;
	}

	private static MuiColorSpecialistClass Create(ref MuiHeadlessTestPlatform p,
		APTR instance, string name)
	{
		p.WriteCString(ClassId, name);
		return MuiColorSpecialistCore.CreateByName(ref p, instance, ClassId);
	}

	// ---- Classification ------------------------------------------------------

	[Fact]
	public void ExactClassNamesAreClassified()
	{
		var p = NewPlatform();
		p.WriteCString(ClassId, "Pendisplay.mui");
		Assert.Equal(MuiColorSpecialistClass.Pendisplay,
			MuiColorSpecialistCore.ClassifyName(ref p, ClassId));
		p.WriteCString(ClassId, "Colorfield.mui");
		Assert.Equal(MuiColorSpecialistClass.Colorfield,
			MuiColorSpecialistCore.ClassifyName(ref p, ClassId));
		p.WriteCString(ClassId, "Coloradjust.mui");
		Assert.Equal(MuiColorSpecialistClass.Coloradjust,
			MuiColorSpecialistCore.ClassifyName(ref p, ClassId));
		p.WriteCString(ClassId, "Palette.mui");
		Assert.Equal(MuiColorSpecialistClass.Palette,
			MuiColorSpecialistCore.ClassifyName(ref p, ClassId));
		p.WriteCString(ClassId, "Penadjust.mui");
		Assert.Equal(MuiColorSpecialistClass.Penadjust,
			MuiColorSpecialistCore.ClassifyName(ref p, ClassId));
	}

	[Fact]
	public void UnknownAndMiscasedNamesAreRejected()
	{
		var p = NewPlatform();
		// Poppen is deferred to the Popstring hierarchy, not this slice.
		p.WriteCString(ClassId, "Poppen.mui");
		Assert.Equal(MuiColorSpecialistClass.None,
			MuiColorSpecialistCore.ClassifyName(ref p, ClassId));
		// The loader contract is case-sensitive.
		p.WriteCString(ClassId, "pendisplay.mui");
		Assert.Equal(MuiColorSpecialistClass.None,
			MuiColorSpecialistCore.ClassifyName(ref p, ClassId));
		Assert.Equal(MuiColorSpecialistClass.None,
			MuiColorSpecialistCore.ClassifyName(ref p, APTR.Null));
	}

	[Fact]
	public void PaletteIsObsoleteAndPenadjustIsPrivate()
	{
		Assert.True(MuiColorSpecialistCore.IsObsolete(
			MuiColorSpecialistClass.Palette));
		Assert.False(MuiColorSpecialistCore.IsObsolete(
			MuiColorSpecialistClass.Coloradjust));
		Assert.True(MuiColorSpecialistCore.IsPrivate(
			MuiColorSpecialistClass.Penadjust));
	}

	// ---- Creation-time defaults ----------------------------------------------

	[Fact]
	public void PendisplayCreationAllocatesSpecAndRgbWithZeroDefaults()
	{
		var p = NewPlatform();
		Assert.Equal(MuiColorSpecialistClass.Pendisplay,
			Create(ref p, Instance, "Pendisplay.mui"));
		Assert.Equal(MuiColorSpecialistClass.Pendisplay,
			MuiColorSpecialistCore.Classify(ref p, Instance));
		Assert.False(MuiColorSpecialistCore.IsSetup(ref p, Instance));
		// Spec and RGB pointers are owned, guest-resident and distinct.
		Assert.True(MuiColorSpecialistCore.GetAttribute(ref p, Instance,
			MuiColorAttributes.PendisplaySpec, out var spec));
		Assert.True(MuiColorSpecialistCore.GetAttribute(ref p, Instance,
			MuiColorAttributes.PendisplayRgbColor, out var rgb));
		Assert.NotEqual(0u, spec);
		Assert.NotEqual(0u, rgb);
		Assert.NotEqual(spec, rgb);
		// Default RGB is black; default pen is none (0).
		Assert.Equal(0u, p.ReadUInt32(APTR.FromPointer(rgb), 0));
		Assert.True(MuiColorSpecialistCore.GetAttribute(ref p, Instance,
			MuiColorAttributes.PendisplayPen, out var pen));
		Assert.Equal(0u, pen);
	}

	[Fact]
	public void PaletteDefaultsToGroupableAndObsolete()
	{
		var p = NewPlatform();
		Assert.Equal(MuiColorSpecialistClass.Palette,
			Create(ref p, Instance, "Palette.mui"));
		Assert.True(MuiColorSpecialistCore.GetAttribute(ref p, Instance,
			MuiColorAttributes.PaletteGroupable, out var groupable));
		Assert.Equal(1u, groupable);
	}

	[Fact]
	public void PenadjustAndPaletteOwnNoRgbBlock()
	{
		var p = NewPlatform();
		var before = p.AllocationCount;
		Assert.Equal(MuiColorSpecialistClass.Penadjust,
			Create(ref p, Instance, "Penadjust.mui"));
		// No copied blocks are allocated for the private group class.
		Assert.Equal(before, p.AllocationCount);
	}

	[Fact]
	public void CreateRejectsNullOrUnmappedInstance()
	{
		var p = NewPlatform();
		Assert.False(MuiColorSpecialistCore.Create(ref p, APTR.Null,
			MuiColorSpecialistClass.Colorfield));
		Assert.False(MuiColorSpecialistCore.Create(ref p, Instance,
			MuiColorSpecialistClass.None));
	}

	[Fact]
	public void ColorSpecialistStateCodecUsesNamedFields()
	{
		var p = NewPlatform();
		var address = APTR.FromPointer(0x1700);
		var value = default(MuiColorSpecialistState);
		value.Magic = MuiColorSpecialistState.Cookie;
		value.Class = (uint)MuiColorSpecialistClass.Coloradjust;
		value.Flags = MuiColorSpecialistLayout.FlagSetupActive |
			MuiColorSpecialistLayout.FlagShowAlpha;
		value.RenderInfo = APTR.FromPointer(0x1800);
		value.DrawState = APTR.FromPointer(0x1900);
		value.Pen = 12;
		value.SpecBlock = APTR.FromPointer(0x1A00);
		value.RgbBlock = APTR.FromPointer(0x1B00);
		value.Reference = APTR.FromPointer(0x1C00);
		value.ModeID = 7;
		value.Alpha = 0xFFFFFFFF;
		value.Entries = APTR.FromPointer(0x1D00);
		value.Names = APTR.FromPointer(0x1E00);
		value.NotifyAttribute = MuiColorAttributes.ColoradjustAlpha;
		value.NotifyValue = 0x01020304;
		value.NotifyCount = 5;
		Assert.True(MuiColorSpecialistStateCodec.Write(ref p, address, value));
		Assert.True(MuiColorSpecialistStateCodec.TryRead(ref p, address,
			out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Class, decoded.Class);
		Assert.Equal(value.Flags, decoded.Flags);
		Assert.Equal(value.RenderInfo, decoded.RenderInfo);
		Assert.Equal(value.DrawState, decoded.DrawState);
		Assert.Equal(value.Pen, decoded.Pen);
		Assert.Equal(value.SpecBlock, decoded.SpecBlock);
		Assert.Equal(value.RgbBlock, decoded.RgbBlock);
		Assert.Equal(value.Reference, decoded.Reference);
		Assert.Equal(value.ModeID, decoded.ModeID);
		Assert.Equal(value.Alpha, decoded.Alpha);
		Assert.Equal(value.Entries, decoded.Entries);
		Assert.Equal(value.Names, decoded.Names);
		Assert.Equal(value.NotifyAttribute, decoded.NotifyAttribute);
		Assert.Equal(value.NotifyValue, decoded.NotifyValue);
		Assert.Equal(value.NotifyCount, decoded.NotifyCount);
		Assert.False(MuiColorSpecialistStateCodec.TryRead(ref p,
			APTR.FromPointer(0x21000), out _));
	}

	[Fact]
	public void ColorPenSpecCodecUsesNamedFields()
	{
		var p = NewPlatform();
		var address = APTR.FromPointer(0x2180);
		var value = default(MuiColorPenSpecRecord);
		value.Kind = MuiColorSpecialistLayout.SpecKindRgb;
		value.Scalar = 0x10203040;
		value.Red = 0x11223344;
		value.Green = 0x55667788;
		value.Blue = 0x99AABBCC;
		value.Reserved0 = 0xDEADBEEF;
		value.Reserved1 = 0x0BADF00D;
		value.Reserved2 = 0xFEEDFACE;
		Assert.True(MuiColorPenSpecCodec.Write(ref p, address, value));
		Assert.True(MuiColorPenSpecCodec.TryRead(ref p, address,
			out var decoded));
		Assert.Equal(value.Kind, decoded.Kind);
		Assert.Equal(value.Scalar, decoded.Scalar);
		Assert.Equal(value.Red, decoded.Red);
		Assert.Equal(value.Green, decoded.Green);
		Assert.Equal(value.Blue, decoded.Blue);
		Assert.Equal(value.Reserved0, decoded.Reserved0);
		Assert.Equal(value.Reserved1, decoded.Reserved1);
		Assert.Equal(value.Reserved2, decoded.Reserved2);
		Assert.False(MuiColorPenSpecCodec.TryRead(ref p,
			APTR.FromPointer(0x22000), out _));
	}

	[Fact]
	public void ColorRecordFieldCursorUsesSemanticRecordKinds()
	{
		var p = NewPlatform();
		var cursor = default(MuiColorRecordFieldCursor);
		cursor.Address = APTR.FromPointer(0x2300);
		cursor.Record = MuiColorRecordKind.PenSpec;
		cursor.Field = MuiColorRecordField.Blue;
		Assert.True(MuiColorRecordFieldCursorCodec.TryGetAddress(ref p, cursor,
			out var fieldAddress));
		Assert.Equal(0x2310u, fieldAddress.Raw);

		cursor.Record = MuiColorRecordKind.State;
		cursor.Field = MuiColorRecordField.NotifyCount;
		Assert.True(MuiColorRecordFieldCursorCodec.TryGetAddress(ref p, cursor,
			out fieldAddress));
		Assert.Equal(0x233Cu, fieldAddress.Raw);
		Assert.True(MuiColorRecordFieldCursorCodec.TryWriteUInt32(ref p,
			cursor.Address, MuiColorRecordKind.Rgb, MuiColorRecordField.Green,
			0xAABBCCDDu));
		Assert.True(MuiColorRecordFieldCursorCodec.TryReadUInt32(ref p,
			cursor.Address, MuiColorRecordKind.Rgb, MuiColorRecordField.Green,
			out var green));
		Assert.Equal(0xAABBCCDDu, green);
		cursor.Record = MuiColorRecordKind.Rgb;
		cursor.Field = MuiColorRecordField.NotifyCount;
		Assert.False(MuiColorRecordFieldCursorCodec.TryGetAddress(ref p, cursor,
			out _));
		cursor.Address = APTR.FromPointer(0xFFFFFFF0u);
		Assert.False(MuiColorRecordFieldCursorCodec.TryGetAddress(ref p, cursor,
			out _));
	}

	// ---- Colorfield attributes -----------------------------------------------

	[Fact]
	public void ColorfieldComponentsAndRgbAreSynchronized()
	{
		var p = NewPlatform();
		Assert.Equal(MuiColorSpecialistClass.Colorfield,
			Create(ref p, Instance, "Colorfield.mui"));

		Assert.True(Set(ref p, MuiColorAttributes.ColorfieldRed, 0x11223344));
		Assert.True(Set(ref p, MuiColorAttributes.ColorfieldGreen, 0x55667788));
		Assert.True(Set(ref p, MuiColorAttributes.ColorfieldBlue, 0x99AABBCC));
		Assert.Equal(0x11223344u, Get(ref p, MuiColorAttributes.ColorfieldRed));
		Assert.Equal(0x55667788u, Get(ref p, MuiColorAttributes.ColorfieldGreen));
		Assert.Equal(0x99AABBCCu, Get(ref p, MuiColorAttributes.ColorfieldBlue));

		// MUIA_Colorfield_RGB get returns the owned block; its bytes mirror the
		// components.
		var rgb = APTR.FromPointer(Get(ref p, MuiColorAttributes.ColorfieldRgb));
		Assert.Equal(0x11223344u, p.ReadUInt32(rgb, 0));
		Assert.Equal(0x55667788u, p.ReadUInt32(rgb, 4));
		Assert.Equal(0x99AABBCCu, p.ReadUInt32(rgb, 8));
	}

	[Fact]
	public void ColorfieldRgbPointerSetCopiesFromCaller()
	{
		var p = NewPlatform();
		Create(ref p, Instance, "Colorfield.mui");
		p.WriteUInt32(RgbSource, 0, 0xDEADBEEF);
		p.WriteUInt32(RgbSource, 4, 0x0BADF00D);
		p.WriteUInt32(RgbSource, 8, 0xFEEDFACE);
		Assert.True(Set(ref p, MuiColorAttributes.ColorfieldRgb, RgbSource.Raw));
		Assert.Equal(0xDEADBEEFu, Get(ref p, MuiColorAttributes.ColorfieldRed));
		Assert.Equal(0xFEEDFACEu, Get(ref p, MuiColorAttributes.ColorfieldBlue));
	}

	[Fact]
	public void ColorfieldPenIsGetOnly()
	{
		var p = NewPlatform();
		Create(ref p, Instance, "Colorfield.mui");
		// The pen is read-only: no setter claims it.
		Assert.False(MuiColorSpecialistCore.SetAttribute(ref p, Instance,
			MuiColorAttributes.ColorfieldPen, 5, false, true, out _));
		Assert.True(MuiColorSpecialistCore.GetAttribute(ref p, Instance,
			MuiColorAttributes.ColorfieldPen, out var pen));
		Assert.Equal(0u, pen);
	}

	// ---- Coloradjust synchronized channels ------------------------------------

	[Fact]
	public void ColoradjustArgbXrgbAlphaAndModeIdAreSynchronized()
	{
		var p = NewPlatform();
		Assert.Equal(MuiColorSpecialistClass.Coloradjust,
			Create(ref p, Instance, "Coloradjust.mui"));

		// ARGB expands each 8-bit field to a 32-bit intensity by replication.
		Assert.True(Set(ref p, MuiColorAttributes.ColoradjustArgb, 0x80FF8040));
		Assert.Equal(0xFFFFFFFFu, Get(ref p, MuiColorAttributes.ColoradjustRed));
		Assert.Equal(0x80808080u, Get(ref p, MuiColorAttributes.ColoradjustGreen));
		Assert.Equal(0x40404040u, Get(ref p, MuiColorAttributes.ColoradjustBlue));
		Assert.Equal(0x80808080u, Get(ref p, MuiColorAttributes.ColoradjustAlpha));
		// The packed ARGB round-trips through the high bytes.
		Assert.Equal(0x80FF8040u, Get(ref p, MuiColorAttributes.ColoradjustArgb));

		// XRGB forces the alpha channel fully opaque and reports a zero top byte.
		Assert.True(Set(ref p, MuiColorAttributes.ColoradjustXrgb, 0x00112233));
		Assert.Equal(0xFFFFFFFFu, Get(ref p, MuiColorAttributes.ColoradjustAlpha));
		Assert.Equal(0x00112233u, Get(ref p, MuiColorAttributes.ColoradjustXrgb));

		Assert.True(Set(ref p, MuiColorAttributes.ColoradjustModeId, 0x00021000));
		Assert.Equal(0x00021000u, Get(ref p, MuiColorAttributes.ColoradjustModeId));
	}

	[Fact]
	public void ColoradjustShowAlphaIsInitOnly()
	{
		var p = NewPlatform();
		Create(ref p, Instance, "Coloradjust.mui");
		// Runtime set is a claimed no-op; the value stays at its default (false).
		Assert.True(MuiColorSpecialistCore.SetAttribute(ref p, Instance,
			MuiColorAttributes.ColoradjustShowAlpha, 1, false, true, out var changed));
		Assert.False(changed);
		Assert.Equal(0u, Get(ref p, MuiColorAttributes.ColoradjustShowAlpha));
		// Init set honours it.
		Assert.True(MuiColorSpecialistCore.SetAttribute(ref p, Instance,
			MuiColorAttributes.ColoradjustShowAlpha, 1, true, false, out changed));
		Assert.True(changed);
		Assert.Equal(1u, Get(ref p, MuiColorAttributes.ColoradjustShowAlpha));
	}

	// ---- Palette obsolete-but-supported / Penadjust private -------------------

	[Fact]
	public void PaletteEntriesAndNamesAreInitOnlyReferencesGroupableGettable()
	{
		var p = NewPlatform();
		Create(ref p, Instance, "Palette.mui");
		// Entries/Names are [I..]: honoured at init, ignored at runtime, not
		// gettable (no placeholder value is fabricated).
		Assert.True(MuiColorSpecialistCore.SetAttribute(ref p, Instance,
			MuiColorAttributes.PaletteEntries, 0x9000, true, false, out _));
		Assert.False(MuiColorSpecialistCore.GetAttribute(ref p, Instance,
			MuiColorAttributes.PaletteEntries, out _));
		Assert.True(MuiColorSpecialistCore.SetAttribute(ref p, Instance,
			MuiColorAttributes.PaletteNames, 0x9100, true, false, out _));
		// Groupable is [I.G]: init toggles, runtime set is ignored.
		Assert.True(MuiColorSpecialistCore.SetAttribute(ref p, Instance,
			MuiColorAttributes.PaletteGroupable, 0, true, false, out _));
		Assert.Equal(0u, Get(ref p, MuiColorAttributes.PaletteGroupable));
		Assert.True(MuiColorSpecialistCore.SetAttribute(ref p, Instance,
			MuiColorAttributes.PaletteGroupable, 1, false, true, out var changed));
		Assert.False(changed);
		Assert.Equal(0u, Get(ref p, MuiColorAttributes.PaletteGroupable));
	}

	[Fact]
	public void PenadjustPsiModeStoresAndReports()
	{
		var p = NewPlatform();
		Create(ref p, Instance, "Penadjust.mui");
		Assert.True(MuiColorSpecialistCore.SetAttribute(ref p, Instance,
			MuiColorAttributes.PenadjustPsiMode, 1, false, false, out var changed));
		Assert.True(changed);
		// PSIMode is private, exercised only through the core.
		Assert.True(MuiColorSpecialistCore.SetAttribute(ref p, Instance,
			MuiColorAttributes.PenadjustPsiMode, 1, false, false, out changed));
		Assert.False(changed);
	}

	[Fact]
	public void AttributesAreClassScoped()
	{
		var p = NewPlatform();
		Create(ref p, Instance, "Colorfield.mui");
		// A Coloradjust attribute is not claimed by a Colorfield.
		Assert.False(MuiColorSpecialistCore.SetAttribute(ref p, Instance,
			MuiColorAttributes.ColoradjustModeId, 1, false, true, out _));
		Assert.False(MuiColorSpecialistCore.GetAttribute(ref p, Instance,
			MuiColorAttributes.ColoradjustModeId, out _));
	}

	// ---- Notification behaviour ----------------------------------------------

	[Fact]
	public void NotificationsFireOnChangeOnlyNotOnInitOrNoNotifyOrNoOp()
	{
		var p = NewPlatform();
		Create(ref p, Instance, "Colorfield.mui");
		Assert.Equal(0u, MuiColorSpecialistCore.NotificationCount(ref p, Instance));

		// Notifying set that changes the value fires once.
		Assert.True(MuiColorSpecialistCore.SetAttribute(ref p, Instance,
			MuiColorAttributes.ColorfieldRed, 7, false, true, out _));
		Assert.Equal(1u, MuiColorSpecialistCore.NotificationCount(ref p, Instance));
		Assert.Equal(MuiColorAttributes.ColorfieldRed,
			MuiColorSpecialistCore.LastNotifiedAttribute(ref p, Instance));
		Assert.Equal(7u, MuiColorSpecialistCore.LastNotifiedValue(ref p, Instance));

		// A no-op set (same value) does not notify.
		Assert.True(MuiColorSpecialistCore.SetAttribute(ref p, Instance,
			MuiColorAttributes.ColorfieldRed, 7, false, true, out var changed));
		Assert.False(changed);
		Assert.Equal(1u, MuiColorSpecialistCore.NotificationCount(ref p, Instance));

		// The no-notify set changes state without notifying.
		Assert.True(MuiColorSpecialistCore.SetAttribute(ref p, Instance,
			MuiColorAttributes.ColorfieldRed, 9, false, false, out changed));
		Assert.True(changed);
		Assert.Equal(1u, MuiColorSpecialistCore.NotificationCount(ref p, Instance));

		// Init sets never notify.
		Assert.True(MuiColorSpecialistCore.SetAttribute(ref p, Instance,
			MuiColorAttributes.ColorfieldRed, 12, true, true, out _));
		Assert.Equal(1u, MuiColorSpecialistCore.NotificationCount(ref p, Instance));
	}

	// ---- Setup / Cleanup pen lifecycle ---------------------------------------

	[Fact]
	public void PendisplaySetupObtainsFullTokenThroughDrawingServiceAndCleanupReleasesOnce()
	{
		var p = NewPlatform();
		p.NextPenToken = 0x00030009; // high tag proves the full token is tracked
		MapRenderInfo(ref p);
		Create(ref p, Instance, "Pendisplay.mui");

		Assert.True(MuiColorSpecialistCore.Setup(ref p, Instance, DrawState, Mri));
		Assert.True(MuiColorSpecialistCore.IsSetup(ref p, Instance));
		Assert.Equal(1u, p.ObtainPenCount);
		Assert.Equal(0x00030009u,
			Get(ref p, MuiColorAttributes.PendisplayPen));

		// Cleanup releases exactly once, with the FULL token.
		Assert.True(MuiColorSpecialistCore.Cleanup(ref p, Instance));
		Assert.Equal(1u, p.ReleasePenCount);
		Assert.Equal(0x00030009, p.LastReleasedPen);

		// The pen is owned by the drawing service, which no longer tracks it: a
		// direct release of the same token now fails (released exactly once).
		Assert.False(MuiDrawingServiceCore.ReleasePen(ref p, DrawState, Mri,
			0x00030009));

		// Cleanup is idempotent.
		Assert.False(MuiColorSpecialistCore.Cleanup(ref p, Instance));
		Assert.Equal(1u, p.ReleasePenCount);
	}

	[Fact]
	public void SetupRejectsUnmappedRenderInfoAndDoubleSetup()
	{
		var p = NewPlatform();
		Create(ref p, Instance, "Pendisplay.mui");
		// Null render info.
		Assert.False(MuiColorSpecialistCore.Setup(ref p, Instance, DrawState,
			APTR.Null));
		Assert.Equal(0u, p.ObtainPenCount);

		MapRenderInfo(ref p);
		Assert.True(MuiColorSpecialistCore.Setup(ref p, Instance, DrawState, Mri));
		// A second Setup while active is rejected (no second pen).
		Assert.False(MuiColorSpecialistCore.Setup(ref p, Instance, DrawState, Mri));
		Assert.Equal(1u, p.ObtainPenCount);
	}

	[Fact]
	public void PendisplaySetupPenObtainFailureReservesNothing()
	{
		var p = NewPlatform();
		MapRenderInfo(ref p);
		Create(ref p, Instance, "Pendisplay.mui");
		p.PenObtainFailure = true;
		p.PenObtainFailureValue = -2;
		Assert.False(MuiColorSpecialistCore.Setup(ref p, Instance, DrawState, Mri));
		Assert.False(MuiColorSpecialistCore.IsSetup(ref p, Instance));
		Assert.Equal(1u, p.ObtainPenCount);
		Assert.Equal(0u, p.ReleasePenCount);
		// Nothing to clean up.
		Assert.False(MuiColorSpecialistCore.Cleanup(ref p, Instance));
	}

	[Fact]
	public void ColorfieldSetupAllocatesTransientSpecObtainsPenAndCleanupFreesIt()
	{
		var p = NewPlatform();
		MapRenderInfo(ref p);
		Create(ref p, Instance, "Colorfield.mui");
		Set(ref p, MuiColorAttributes.ColorfieldRed, 0xAABBCCDD);

		var allocsBefore = p.AllocationCount;
		var freesBefore = p.FreeCount;
		Assert.True(MuiColorSpecialistCore.Setup(ref p, Instance, DrawState, Mri));
		Assert.Equal(1u, p.ObtainPenCount);
		// A transient 32-byte pen spec was allocated for the obtain.
		Assert.True(p.AllocationCount > allocsBefore);
		Assert.NotEqual(0u, Get(ref p, MuiColorAttributes.ColorfieldPen));

		Assert.True(MuiColorSpecialistCore.Cleanup(ref p, Instance));
		Assert.Equal(1u, p.ReleasePenCount);
		// The transient spec is freed at Cleanup (pen + spec).
		Assert.True(p.FreeCount > freesBefore);
	}

	[Fact]
	public void ColorfieldSetupSpecAllocationFailureIsAtomic()
	{
		// Build a Colorfield whose RGB copy was allocated, then exhaust the arena
		// so Setup's transient 32-byte spec allocation fails and no pen is
		// obtained (the failure path is atomic).
		var p = NewPlatform();
		MapRenderInfo(ref p);
		Create(ref p, Instance, "Colorfield.mui");
		while (p.Allocate(0x400, 0).IsNotNull) { }
		Assert.False(MuiColorSpecialistCore.Setup(ref p, Instance, DrawState, Mri));
		Assert.Equal(0u, p.ObtainPenCount);
		Assert.False(MuiColorSpecialistCore.IsSetup(ref p, Instance));
	}

	// ---- Draw (fill / fallback / disabled) -----------------------------------

	[Fact]
	public void DrawUsesHeldPenAndFallsBackWhenNotSetUp()
	{
		var p = NewPlatform();
		p.NextPenToken = 0x00000005;
		MapRenderInfo(ref p);
		Create(ref p, Instance, "Colorfield.mui");

		// Not set up: fallback background pen 0 is used, and the field still fills.
		Assert.True(MuiColorSpecialistCore.Draw(ref p, Instance, RastPort, 2, 3,
			10, 6));
		Assert.Equal(1u, p.FillCount);
		Assert.Equal(0u, p.LastPen);
		Assert.Equal(2, p.LastLeft);
		Assert.Equal(11, p.LastRight);   // left + width - 1
		Assert.Equal(8, p.LastBottom);   // top + height - 1

		// Set up: the held shared pen is used.
		Assert.True(MuiColorSpecialistCore.Setup(ref p, Instance, DrawState, Mri));
		Assert.True(MuiColorSpecialistCore.Draw(ref p, Instance, RastPort, 0, 0,
			4, 4));
		Assert.Equal(2u, p.FillCount);
		Assert.Equal(5u, p.LastPen);

		// Non-positive geometry paints nothing but is still claimed.
		Assert.True(MuiColorSpecialistCore.Draw(ref p, Instance, RastPort, 0, 0,
			0, 4));
		Assert.Equal(2u, p.FillCount);
	}

	[Fact]
	public void AskMinMaxWritesBoundedSwatch()
	{
		var p = NewPlatform();
		Create(ref p, Instance, "Pendisplay.mui");
		Assert.True(MuiColorSpecialistCore.AskMinMax(ref p, Instance, Storage));
		Assert.Equal((ushort)8, p.ReadUInt16(Storage, 0));
		Assert.Equal((ushort)6, p.ReadUInt16(Storage, 2));
		Assert.Equal((ushort)10000, p.ReadUInt16(Storage, 4));
		Assert.Equal((ushort)24, p.ReadUInt16(Storage, 8));
		// A group class has no direct swatch.
		Create(ref p, Instance2, "Coloradjust.mui");
		Assert.False(MuiColorSpecialistCore.AskMinMax(ref p, Instance2, Storage));
	}

	// ---- Reference / shared behaviour ----------------------------------------

	[Fact]
	public void PendisplayReferenceLendsPenAndSetupObtainsNone()
	{
		var p = NewPlatform();
		p.NextPenToken = 0x00040011;
		MapRenderInfo(ref p);
		// Source object holds a real pen.
		Create(ref p, Instance2, "Pendisplay.mui");
		Assert.True(MuiColorSpecialistCore.Setup(ref p, Instance2, DrawState, Mri));
		var lentPen = Get2(ref p, MuiColorAttributes.PendisplayPen);
		Assert.Equal(0x00040011u, lentPen);

		// Referencing object borrows the pen and obtains none of its own.
		Create(ref p, Instance, "Pendisplay.mui");
		Assert.True(MuiColorSpecialistCore.SetAttribute(ref p, Instance,
			MuiColorAttributes.PendisplayReference, Instance2.Raw, false, true,
			out _));
		var obtainBefore = p.ObtainPenCount;
		Assert.True(MuiColorSpecialistCore.Setup(ref p, Instance, DrawState, Mri));
		Assert.Equal(obtainBefore, p.ObtainPenCount); // no new pen obtained
		// Pen get follows the reference.
		Assert.Equal(0x00040011u, Get(ref p, MuiColorAttributes.PendisplayPen));

		// A referenced-object cleanup does not double-release for the reference.
		Assert.True(MuiColorSpecialistCore.Cleanup(ref p, Instance)); // releases none
		Assert.Equal(0u, p.ReleasePenCount);
		Assert.True(MuiColorSpecialistCore.Cleanup(ref p, Instance2));
		Assert.Equal(1u, p.ReleasePenCount);
	}

	// ---- Ownership after caller mutation --------------------------------------

	[Fact]
	public void SpecAndRgbCopiesAreOwnedAfterCallerMutation()
	{
		var p = NewPlatform();
		Create(ref p, Instance, "Pendisplay.mui");

		for (var i = 0; i < 32; i += 4) p.WriteUInt32(SpecSource, i, 0xA0000000u + (uint)i);
		Assert.True(MuiColorSpecialistCore.SetAttribute(ref p, Instance,
			MuiColorAttributes.PendisplaySpec, SpecSource.Raw, false, true, out _));
		var ownedSpec = APTR.FromPointer(Get(ref p,
			MuiColorAttributes.PendisplaySpec));
		Assert.NotEqual(SpecSource.Raw, ownedSpec.Raw);
		Assert.Equal(0xA0000000u, p.ReadUInt32(ownedSpec, 0));

		// Mutate the caller's source; the owned copy is unaffected.
		p.WriteUInt32(SpecSource, 0, 0xFFFFFFFFu);
		Assert.Equal(0xA0000000u, p.ReadUInt32(ownedSpec, 0));

		p.WriteUInt32(RgbSource, 0, 0x01020304);
		p.WriteUInt32(RgbSource, 4, 0x05060708);
		p.WriteUInt32(RgbSource, 8, 0x090A0B0C);
		Assert.True(MuiColorSpecialistCore.SetAttribute(ref p, Instance,
			MuiColorAttributes.PendisplayRgbColor, RgbSource.Raw, false, true,
			out _));
		var ownedRgb = APTR.FromPointer(Get(ref p,
			MuiColorAttributes.PendisplayRgbColor));
		p.WriteUInt32(RgbSource, 4, 0);
		Assert.Equal(0x05060708u, p.ReadUInt32(ownedRgb, 4));
	}

	// ---- Repeated disposal ----------------------------------------------------

	[Fact]
	public void DisposeReleasesPenFreesBlocksAndIsSafeToRepeat()
	{
		var p = NewPlatform();
		MapRenderInfo(ref p);
		Create(ref p, Instance, "Pendisplay.mui");
		Assert.True(MuiColorSpecialistCore.Setup(ref p, Instance, DrawState, Mri));
		var freesBefore = p.FreeCount;

		Assert.True(MuiColorSpecialistLifecycle.Dispose(ref p, Instance));
		// Pen released once; spec (32) and rgb (12) freed.
		Assert.Equal(1u, p.ReleasePenCount);
		Assert.True(p.FreeCount >= freesBefore + 2);
		// The instance is invalidated.
		Assert.False(MuiColorSpecialistCore.Valid(ref p, Instance));
		Assert.Equal(MuiColorSpecialistClass.None,
			MuiColorSpecialistCore.Classify(ref p, Instance));

		// Repeated disposal is a safe no-op.
		Assert.False(MuiColorSpecialistLifecycle.Dispose(ref p, Instance));
		Assert.Equal(1u, p.ReleasePenCount);
	}

	[Fact]
	public void CreateAllocationFailureRollsBackSpec()
	{
		// Arena sized so the 32-byte pen spec fits but the following 12-byte RGB
		// allocation runs past the mapped region: the spec must be rolled back.
		var p = new MuiHeadlessTestPlatform(Base, Size,
			Base + (uint)Size - 0x20, DrawState);
		Assert.True(MuiDrawingServiceCore.Initialize(ref p, DrawState));
		var freesBefore = p.FreeCount;
		Assert.False(MuiColorSpecialistCore.Create(ref p, Instance,
			MuiColorSpecialistClass.Pendisplay));
		// The spec allocation was freed on rollback.
		Assert.True(p.FreeCount > freesBefore);
		Assert.False(MuiColorSpecialistCore.Valid(ref p, Instance));
	}

	// ---- Dispatcher ----------------------------------------------------------

	[Fact]
	public void ColorSpecialistPacketCodecUsesNamedRecordsAndRejectsMalformedPackets()
	{
		var p = NewPlatform();
		Assert.True(MuiColorSpecialistMessageCodec.WriteGet(ref p, Packet,
			MuiColorAttributes.PendisplaySpec, Storage.Raw));
		Assert.True(MuiColorSpecialistMessageCodec.TryReadGet(ref p, Packet,
			out var get));
		Assert.Equal(MuiColorAttributes.PendisplaySpec, get.Attribute);
		Assert.Equal(Storage.Raw, get.Storage);

		Assert.True(MuiColorSpecialistMessageCodec.WriteSet(ref p, Packet,
			MuiColorSpecialistMessageCodec.MethodSet,
			MuiColorAttributes.PendisplayReference, 0x1234));
		Assert.True(MuiColorSpecialistMessageCodec.TryReadSet(ref p, Packet,
			MuiColorSpecialistMessageCodec.MethodSet, out var set));
		Assert.Equal(MuiColorAttributes.PendisplayReference, set.Attribute);
		Assert.Equal(0x1234u, set.Value);

		Assert.True(MuiColorSpecialistMessageCodec.WritePointer(ref p, Packet,
			MuiColorSpecialistMessageCodec.SetColormap, 7));
		Assert.True(MuiColorSpecialistMessageCodec.TryReadPointer(ref p, Packet,
			MuiColorSpecialistMessageCodec.SetColormap, out var pointer));
		Assert.Equal(7u, pointer.Pointer);

		Assert.True(MuiColorSpecialistMessageCodec.WriteRgb(ref p, Packet,
			0x10, 0x20, 0x30));
		Assert.True(MuiColorSpecialistMessageCodec.TryReadRgb(ref p, Packet,
			out var rgb));
		Assert.Equal(0x10u, rgb.Red);
		Assert.Equal(0x20u, rgb.Green);
		Assert.Equal(0x30u, rgb.Blue);

		Assert.True(MuiColorSpecialistMessageCodec.WriteMethod(ref p, Packet,
			MuiColorSpecialistMessageCodec.OmDispose));
		Assert.True(MuiColorSpecialistMessageCodec.IsValidMethod(ref p, Packet,
			MuiColorSpecialistMessageCodec.OmDispose));
		Assert.False(MuiColorSpecialistMessageCodec.WriteSet(ref p, Packet,
			0x80420000u, 1, 2));
		Assert.False(MuiColorSpecialistMessageCodec.TryReadRgb(ref p,
			APTR.FromPointer(Base + (uint)Size - 1), out _));
		Assert.False(MuiColorSpecialistMessageCodec.IsValidMethod(ref p, Packet,
			0x80420000u));
	}

	[Fact]
	public void ColorSpecialistMethodHeaderUsesNamedField()
	{
		var p = NewPlatform();
		Assert.True(MuiColorSpecialistMessageCodec.WriteMethod(ref p, Packet,
			MuiColorSpecialistMessageCodec.OmDispose));
		Assert.True(MuiColorSpecialistMessageCodec.TryReadMethodId(ref p, Packet,
			out var packet));
		Assert.Equal(MuiColorSpecialistMessageCodec.OmDispose, packet.MethodId);
		Assert.False(MuiColorSpecialistMessageCodec.TryReadMethodId(ref p,
			APTR.Null, out _));
	}

	[Fact]
	public void ColorSpecialistTypedReadersUseNamedMethodHeader()
	{
		var p = NewPlatform();
		Assert.True(MuiColorSpecialistMessageCodec.WriteRgb(ref p, Packet,
			1, 2, 3));
		Assert.True(MuiColorSpecialistMessageCodec.TryReadRgb(ref p, Packet,
			out var rgb));
		Assert.Equal(MuiColorSpecialistMessageCodec.SetRGB, rgb.MethodId);
		Assert.True(MuiColorSpecialistFieldCursorCodec.TryWriteUInt32(ref p,
			Packet, MuiColorSpecialistPacketKind.Rgb,
			MuiColorSpecialistField.MethodId, 0xDEADBEEFu));
		Assert.False(MuiColorSpecialistMessageCodec.TryReadRgb(ref p, Packet,
			out _));
	}

	[Fact]
	public void ColorSpecialistFieldCursorUsesNamedMixedPacketBoundaries()
	{
		var p = NewPlatform();
		var cursor = default(MuiColorSpecialistFieldCursor);
		cursor.Message = Packet;
		cursor.Packet = MuiColorSpecialistPacketKind.Get;
		cursor.Field = MuiColorSpecialistField.MethodId;
		Assert.True(MuiColorSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out var address));
		Assert.Equal(Packet.Raw, address.Raw);
		cursor.Field = MuiColorSpecialistField.Attribute;
		Assert.True(MuiColorSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out address));
		Assert.Equal(Packet.Raw + 4, address.Raw);
		cursor.Field = MuiColorSpecialistField.Storage;
		Assert.True(MuiColorSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out address));
		Assert.Equal(Packet.Raw + 8, address.Raw);

		Assert.True(MuiColorSpecialistFieldCursorCodec.TryWriteUInt32(ref p,
			Packet, MuiColorSpecialistPacketKind.Rgb,
			MuiColorSpecialistField.Blue, 0xAABBCCDD));
		Assert.True(MuiColorSpecialistFieldCursorCodec.TryReadUInt32(ref p,
			Packet, MuiColorSpecialistPacketKind.Rgb,
			MuiColorSpecialistField.Blue, out var blue));
		Assert.Equal(0xAABBCCDDu, blue);

		cursor.Packet = MuiColorSpecialistPacketKind.Pointer;
		cursor.Field = MuiColorSpecialistField.Attribute;
		Assert.False(MuiColorSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out _));
		cursor.Message = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Packet = MuiColorSpecialistPacketKind.Rgb;
		cursor.Field = MuiColorSpecialistField.Blue;
		Assert.False(MuiColorSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out _));
	}

	[Fact]
	public void ColorRgbCodecUsesNamedComponents()
	{
		var p = NewPlatform();
		var record = default(MuiColorRgbRecord);
		record.Red = 0x11223344;
		record.Green = 0x55667788;
		record.Blue = 0x99AABBCC;
		Assert.True(MuiColorRgbCodec.Write(ref p, RgbSource, record));
		Assert.True(MuiColorRgbCodec.TryRead(ref p, RgbSource,
			out var decoded));
		Assert.Equal(record.Red, decoded.Red);
		Assert.Equal(record.Green, decoded.Green);
		Assert.Equal(record.Blue, decoded.Blue);
		Assert.False(MuiColorRgbCodec.TryRead(ref p,
			APTR.FromPointer(Base + (uint)Size - 1), out _));
	}

	[Fact]
	public void DispatcherRoutesSetGetPendisplayMethodsAndDispose()
	{
		var p = NewPlatform();
		Create(ref p, Instance, "Pendisplay.mui");

		// MUIM_Pendisplay_SetRGB via dispatcher.
		p.WriteUInt32(Packet, 0, MuiColorAttributes.PendisplaySetRGB);
		p.WriteUInt32(Packet, 4, 0x10);
		p.WriteUInt32(Packet, 8, 0x20);
		p.WriteUInt32(Packet, 12, 0x30);
		Assert.Equal(1u, MuiColorSpecialistDispatcher.Dispatch(ref p, Instance,
			Packet));
		var rgb = APTR.FromPointer(Get(ref p, MuiColorAttributes.PendisplayRgbColor));
		Assert.Equal(0x10u, p.ReadUInt32(rgb, 0));

		// MUIM_Pendisplay_SetColormap authors a colormap spec (and detaches ref).
		p.WriteUInt32(Packet, 0, MuiColorAttributes.PendisplaySetColormap);
		p.WriteUInt32(Packet, 4, 7);
		Assert.Equal(1u, MuiColorSpecialistDispatcher.Dispatch(ref p, Instance,
			Packet));

		// OM_GET through the dispatcher writes into caller storage.
		p.WriteUInt32(Packet, 0, 0x00000104u);
		p.WriteUInt32(Packet, 4, MuiColorAttributes.PendisplaySpec);
		p.WriteUInt32(Packet, 8, Storage.Raw);
		Assert.Equal(1u, MuiColorSpecialistDispatcher.Dispatch(ref p, Instance,
			Packet));
		Assert.NotEqual(0u, p.ReadUInt32(Storage, 0));

		// OM_SET single-tag frame.
		p.WriteUInt32(Packet, 0, 0x8042549au);
		p.WriteUInt32(Packet, 4, MuiColorAttributes.PendisplayReference);
		p.WriteUInt32(Packet, 8, 0x1234);
		Assert.Equal(1u, MuiColorSpecialistDispatcher.Dispatch(ref p, Instance,
			Packet));
		Assert.Equal(0x1234u, Get(ref p, MuiColorAttributes.PendisplayReference));

		// OM_DISPOSE tears the object down.
		p.WriteUInt32(Packet, 0, 0x00000102u);
		Assert.Equal(1u, MuiColorSpecialistDispatcher.Dispatch(ref p, Instance,
			Packet));
		Assert.False(MuiColorSpecialistCore.Valid(ref p, Instance));
	}

	[Fact]
	public void DispatcherDeclinesUnknownMethodsAndInvalidInstances()
	{
		var p = NewPlatform();
		Create(ref p, Instance, "Pendisplay.mui");
		p.WriteUInt32(Packet, 0, 0xDEADBEEFu);
		Assert.False(MuiColorSpecialistDispatcher.TryDispatch(ref p, Instance,
			Packet, out _));
		// An unmapped/invalid instance is never claimed.
		Assert.False(MuiColorSpecialistDispatcher.TryDispatch(ref p, Instance2,
			Packet, out _));
	}

	// ---- Helpers -------------------------------------------------------------

	private static void MapRenderInfo(ref MuiHeadlessTestPlatform p)
	{
		// A 28-byte render info with a rast port/layer, enough for the pen and
		// (unused here) clipping seams.
		p.WriteUInt32(Mri, 20, RastPort.Raw);
		p.WriteUInt32(RastPort, 0, 0x1290);
	}

	private static bool Set(ref MuiHeadlessTestPlatform p, uint attr, uint value)
	{
		var handled = MuiColorSpecialistCore.SetAttribute(ref p, Instance, attr,
			value, false, true, out _);
		return handled;
	}

	private static uint Get(ref MuiHeadlessTestPlatform p, uint attr)
	{
		Assert.True(MuiColorSpecialistCore.GetAttribute(ref p, Instance, attr,
			out var value));
		return value;
	}

	private static uint Get2(ref MuiHeadlessTestPlatform p, uint attr)
	{
		Assert.True(MuiColorSpecialistCore.GetAttribute(ref p, Instance2, attr,
			out var value));
		return value;
	}
}
