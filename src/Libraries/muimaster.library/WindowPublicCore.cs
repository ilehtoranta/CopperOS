/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Public Window.mui attributes backed by the existing named lifecycle state.
// The native pointer is opaque to MUI; the platform owns its allocation and
// lifetime, while the guest-facing getter observes the current value.
public static class MuiWindowPublicCore
{
	// Creation-time alternate geometry is carried across the native boundary as
	// one named value record. Keeping the four LONGs together avoids positional
	// packet offsets while preserving the caller's signed 32-bit values.
	public struct MuiWindowAlternateGeometry
	{
		public int Height;
		public int Width;
		public int LeftEdge;
		public int TopEdge;
	}

	// Creation-time primary geometry is kept separate from the alternate
	// zoomed geometry because MorphOS exposes the two records independently.
	public struct MuiWindowGeometry
	{
		public int Height;
		public int Width;
		public int LeftEdge;
		public int TopEdge;
	}

	// Initializer-only window chrome policy. ULONG fields carry canonical
	// TRUE/FALSE values without relying on managed bool layout across the
	// freestanding platform boundary.
	public struct MuiWindowGadgetPolicy
	{
		public uint CloseGadget;
		public uint DepthGadget;
		public uint DragBar;
		public uint SizeGadget;
		public uint SizeRight;
	}

	// Initializer-only window mode policy. ULONG fields carry canonical
	// TRUE/FALSE values across the freestanding platform boundary.
	public struct MuiWindowModePolicy
	{
		public uint AppWindow;
		public uint Backdrop;
		public uint Borderless;
		public uint PanelWindow;
	}

	// Named lifecycle view kept separate from the opaque native pointer. This
	// prevents callers from depending on positional guest-handler offsets.
	public struct MuiWindowLifecycleState
	{
		public uint Open;
		public APTR NativeWindow;
	}

	// Mutable keyboard-control mask. MorphOS exposes this as one ULONG whose
	// bits are the MUIKEYF_* values; keeping it as a named field avoids making
	// event routing depend on a positional guest-state offset.
	public struct MuiWindowKeyboardState
	{
		public uint DisableKeys;
	}

	public const uint Window = 0x80426A42;
	public const uint Open = 0x80428AA0;
	public const uint Id = 0x804201BD;
	public const uint CloseRequest = 0x8042E86E;
	public const uint RootObject = 0x8042CBA5;
	public const uint NoMenus = 0x80429DF5;
	public const uint HasAlpha = 0x8042E632;
	public const uint Opacity = 0x80429617;
	public const uint Title = 0x8042AD3D;
	public const uint Screen = 0x8042DF4F;
	public const uint ScreenTitle = 0x804234B0;
	public const uint PublicScreen = 0x804278E4;
	public const uint InputEvent = 0x804247D8;
	public const uint DisableKeys = 0x80424C36;
	public const uint Sleep = 0x8042E7DB;
	// Obsolete MorphOS initializer alias for Menustrip. -1 means no menu.
	public const uint Menu = 0x8042DB94;
	public const uint RefWindow = 0x804201F4;
	public const uint VisibleOnMaximize = 0x8042ACFD;
	public const uint IsSubWindow = 0x8042B5AA;
	public const uint TabletMessages = 0x804217B7;
	public const uint UseBottomBorderScroller = 0x80424E79;
	public const uint UseLeftBorderScroller = 0x8042433E;
	public const uint UseRightBorderScroller = 0x8042C05E;
	public const uint AltHeight = 0x8042CCE3;
	public const uint AltLeftEdge = 0x80422D65;
	public const uint AltTopEdge = 0x8042E99B;
	public const uint AltWidth = 0x804260F4;
	public const uint Height = 0x80425846;
	public const uint LeftEdge = 0x80426C65;
	public const uint TopEdge = 0x80427C66;
	public const uint Width = 0x8042DCAE;
	public const uint Menustrip = 0x8042855E;
	public const uint FancyDrawing = 0x8042BD0E;
	public const uint MenuAction = 0x80427521;
	public const uint MouseObject = 0x8042BF9B;
	public const uint NeedsMouseObject = 0x8042372A;
	public const uint CloseGadget = 0x8042A110;
	public const uint DepthGadget = 0x80421923;
	public const uint DragBar = 0x8042045D;
	public const uint SizeGadget = 0x8042E33D;
	public const uint SizeRight = 0x80424780;
	public const uint AppWindow = 0x804280CF;
	public const uint Backdrop = 0x8042C0BB;
	public const uint Borderless = 0x80429B79;
	public const uint PanelWindow = 0x80429528;

	private const uint NativeWindow = 0x7FFE0011;
	private const uint MenuNoMenu = uint.MaxValue;
	private const uint WindowPresentationStateKey = 0x7F0A0004u;
	private const uint WindowVisualStateKey = 0x7F0A0005u;
	private const uint WindowRelationshipStateKey = 0x7F0A0018u;
	private const uint WindowControlStateKey = 0x7F0A0019u;

	private static bool IsPresentationAttribute(uint attribute) =>
		attribute == Title || attribute == Screen || attribute == ScreenTitle ||
		attribute == PublicScreen;

	private static bool IsVisualAttribute(uint attribute) =>
		attribute == NoMenus || attribute == HasAlpha || attribute == Opacity ||
		attribute == FancyDrawing || attribute == MenuAction;

	private static bool IsOpenPolicyAttribute(uint attribute) =>
		attribute == AltHeight || attribute == AltWidth ||
		attribute == AltLeftEdge || attribute == AltTopEdge ||
		attribute == Height || attribute == Width || attribute == LeftEdge ||
		attribute == TopEdge || attribute == CloseGadget ||
		attribute == DepthGadget || attribute == DragBar ||
		attribute == SizeGadget || attribute == SizeRight ||
		attribute == AppWindow || attribute == Backdrop ||
		attribute == Borderless || attribute == PanelWindow ||
		attribute == TabletMessages ||
		attribute == UseBottomBorderScroller ||
		attribute == UseLeftBorderScroller ||
		attribute == UseRightBorderScroller;

	private static uint OpenPolicyValue(MuiWindowOpenPolicyStateRecord value,
		uint attribute) => attribute == AltHeight ? unchecked((uint)value.AlternateHeight) :
		attribute == AltWidth ? unchecked((uint)value.AlternateWidth) :
		attribute == AltLeftEdge ? unchecked((uint)value.AlternateLeftEdge) :
		attribute == AltTopEdge ? unchecked((uint)value.AlternateTopEdge) :
		attribute == Height ? unchecked((uint)value.Height) :
		attribute == Width ? unchecked((uint)value.Width) :
		attribute == LeftEdge ? unchecked((uint)value.LeftEdge) :
		attribute == TopEdge ? unchecked((uint)value.TopEdge) :
		attribute == CloseGadget ? value.CloseGadget :
		attribute == DepthGadget ? value.DepthGadget :
		attribute == DragBar ? value.DragBar :
		attribute == SizeGadget ? value.SizeGadget :
		attribute == SizeRight ? value.SizeRight :
		attribute == AppWindow ? value.AppWindow :
		attribute == Backdrop ? value.Backdrop :
		attribute == Borderless ? value.Borderless :
		attribute == PanelWindow ? value.PanelWindow :
		attribute == TabletMessages ? value.TabletMessages :
		attribute == UseBottomBorderScroller ? value.UseBottomBorderScroller :
		attribute == UseLeftBorderScroller ? value.UseLeftBorderScroller :
		value.UseRightBorderScroller;

	internal static bool TryGetWindowPresentationState<TPlatform>(
		ref TPlatform platform, APTR state, APTR window,
		out MuiWindowPresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, window,
			WindowPresentationStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, window,
			WindowPresentationStateKey) !=
			unchecked((int)MuiWindowPresentationStateRecord.Size)) return false;
		return MuiWindowPresentationStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static MuiWindowPresentationStateRecord ReadWindowPresentation<TPlatform>(
		ref TPlatform platform, APTR state, APTR window)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishWindowPresentation(ref platform, state, window, out var value))
			return value;
		value = default;
		value.Magic = MuiWindowPresentationStateRecord.Cookie;
		FillWindowPresentation(ref platform, state, window, ref value);
		return value;
	}

	private static bool PublishWindowPresentation<TPlatform>(
		ref TPlatform platform, APTR state, APTR window,
		out MuiWindowPresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, window,
			WindowPresentationStateKey);
		if (TryGetWindowPresentationState(ref platform, state, window, out value))
		{
			FillWindowPresentation(ref platform, state, window, ref value);
			return MuiWindowPresentationStateRecordCodec.Write(ref platform, block,
				value);
		}

		value = default;
		value.Magic = MuiWindowPresentationStateRecord.Cookie;
		FillWindowPresentation(ref platform, state, window, ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiWindowPresentationStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiWindowPresentationStateRecord.Size);
		var written = MuiWindowPresentationStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, window,
			WindowPresentationStateKey, scratch,
			unchecked((int)MuiWindowPresentationStateRecord.Size));
		platform.Clear(scratch, MuiWindowPresentationStateRecord.Size);
		platform.Free(scratch, MuiWindowPresentationStateRecord.Size);
		return added;
	}

	private static void FillWindowPresentation<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, ref MuiWindowPresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			Title, out var title)) title = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			Screen, out var screen)) screen = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			ScreenTitle, out var screenTitle)) screenTitle = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			PublicScreen, out var publicScreen)) publicScreen = 0;
		value.Title = APTR.FromPointer(title);
		value.Screen = APTR.FromPointer(screen);
		value.ScreenTitle = APTR.FromPointer(screenTitle);
		value.PublicScreen = APTR.FromPointer(publicScreen);
	}

	private static uint PresentationValue(
		MuiWindowPresentationStateRecord value, uint attribute) =>
		attribute == Title ? value.Title.Raw : attribute == Screen ?
		value.Screen.Raw : attribute == ScreenTitle ? value.ScreenTitle.Raw :
		value.PublicScreen.Raw;

	internal static bool TryGetWindowVisualState<TPlatform>(
		ref TPlatform platform, APTR state, APTR window,
		out MuiWindowVisualStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, window,
			WindowVisualStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, window,
			WindowVisualStateKey) !=
			unchecked((int)MuiWindowVisualStateRecord.Size)) return false;
		return MuiWindowVisualStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static MuiWindowVisualStateRecord ReadWindowVisual<TPlatform>(
		ref TPlatform platform, APTR state, APTR window)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishWindowVisual(ref platform, state, window, out var value))
			return value;
		value = default;
		value.Magic = MuiWindowVisualStateRecord.Cookie;
		FillWindowVisual(ref platform, state, window, ref value);
		return value;
	}

	private static bool PublishWindowVisual<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, out MuiWindowVisualStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, window,
			WindowVisualStateKey);
		if (TryGetWindowVisualState(ref platform, state, window, out value))
		{
			FillWindowVisual(ref platform, state, window, ref value);
			return MuiWindowVisualStateRecordCodec.Write(ref platform, block,
				value);
		}

		value = default;
		value.Magic = MuiWindowVisualStateRecord.Cookie;
		FillWindowVisual(ref platform, state, window, ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiWindowVisualStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiWindowVisualStateRecord.Size);
		var written = MuiWindowVisualStateRecordCodec.Write(ref platform, scratch,
			value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, window,
			WindowVisualStateKey, scratch,
			unchecked((int)MuiWindowVisualStateRecord.Size));
		platform.Clear(scratch, MuiWindowVisualStateRecord.Size);
		platform.Free(scratch, MuiWindowVisualStateRecord.Size);
		return added;
	}

	private static void FillWindowVisual<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, ref MuiWindowVisualStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			NoMenus, out value.NoMenus)) value.NoMenus = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			HasAlpha, out value.HasAlpha)) value.HasAlpha = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			Opacity, out value.Opacity)) value.Opacity = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			FancyDrawing, out value.FancyDrawing)) value.FancyDrawing = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			MenuAction, out value.MenuAction)) value.MenuAction = 0;
	}

	private static uint VisualValue(MuiWindowVisualStateRecord value,
		uint attribute) => attribute == NoMenus ? value.NoMenus :
		attribute == HasAlpha ? value.HasAlpha : attribute == Opacity ?
		value.Opacity : attribute == FancyDrawing ? value.FancyDrawing :
		value.MenuAction;

	internal static bool TryGetWindowRelationshipState<TPlatform>(
		ref TPlatform platform, APTR state, APTR window,
		out MuiWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, window,
			WindowRelationshipStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, window,
			WindowRelationshipStateKey) != unchecked((int)
			MuiWindowRelationshipStateRecord.Size)) return false;
		return MuiWindowRelationshipStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static MuiWindowRelationshipStateRecord ReadWindowRelationship<TPlatform>(
		ref TPlatform platform, APTR state, APTR window)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishWindowRelationship(ref platform, state, window, out var value))
			return value;
		value = default;
		value.Magic = MuiWindowRelationshipStateRecord.Cookie;
		FillWindowRelationship(ref platform, state, window, ref value);
		return value;
	}

	private static bool PublishWindowRelationship<TPlatform>(
		ref TPlatform platform, APTR state, APTR window,
		out MuiWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, window,
			WindowRelationshipStateKey);
		if (TryGetWindowRelationshipState(ref platform, state, window, out value))
		{
			FillWindowRelationship(ref platform, state, window, ref value);
			return MuiWindowRelationshipStateRecordCodec.Write(ref platform, block,
				value);
		}

		value = default;
		value.Magic = MuiWindowRelationshipStateRecord.Cookie;
		FillWindowRelationship(ref platform, state, window, ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiWindowRelationshipStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiWindowRelationshipStateRecord.Size);
		var written = MuiWindowRelationshipStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, window,
			WindowRelationshipStateKey, scratch,
			unchecked((int)MuiWindowRelationshipStateRecord.Size));
		platform.Clear(scratch, MuiWindowRelationshipStateRecord.Size);
		platform.Free(scratch, MuiWindowRelationshipStateRecord.Size);
		return added;
	}

	private static void FillWindowRelationship<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, ref MuiWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			RootObject, out var rootObject)) rootObject = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			Menustrip, out var menustrip)) menustrip = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			RefWindow, out var refWindow)) refWindow = 0;
		value.RootObject = APTR.FromPointer(rootObject);
		value.Menustrip = APTR.FromPointer(menustrip);
		value.RefWindow = APTR.FromPointer(refWindow);
	}

	private static uint RelationshipValue(MuiWindowRelationshipStateRecord value,
		uint attribute) => attribute == RootObject ? value.RootObject.Raw :
		attribute == Menustrip || attribute == Menu ? value.Menustrip.Raw :
		value.RefWindow.Raw;

	private static bool IsControlAttribute(uint attribute) => attribute == Id ||
		attribute == DisableKeys || attribute == VisibleOnMaximize ||
		attribute == IsSubWindow || attribute == NeedsMouseObject;

	internal static bool TryGetWindowControlState<TPlatform>(
		ref TPlatform platform, APTR state, APTR window,
		out MuiWindowControlStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, window,
			WindowControlStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, window,
			WindowControlStateKey) != unchecked((int)
			MuiWindowControlStateRecord.Size)) return false;
		return MuiWindowControlStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static MuiWindowControlStateRecord ReadWindowControl<TPlatform>(
		ref TPlatform platform, APTR state, APTR window)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishWindowControl(ref platform, state, window, out var value))
			return value;
		value = default;
		value.Magic = MuiWindowControlStateRecord.Cookie;
		FillWindowControl(ref platform, state, window, ref value);
		return value;
	}

	private static bool PublishWindowControl<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, out MuiWindowControlStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, window,
			WindowControlStateKey);
		if (TryGetWindowControlState(ref platform, state, window, out value))
		{
			FillWindowControl(ref platform, state, window, ref value);
			return MuiWindowControlStateRecordCodec.Write(ref platform, block,
				value);
		}

		value = default;
		value.Magic = MuiWindowControlStateRecord.Cookie;
		FillWindowControl(ref platform, state, window, ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiWindowControlStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiWindowControlStateRecord.Size);
		var written = MuiWindowControlStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, window,
			WindowControlStateKey, scratch,
			unchecked((int)MuiWindowControlStateRecord.Size));
		platform.Clear(scratch, MuiWindowControlStateRecord.Size);
		platform.Free(scratch, MuiWindowControlStateRecord.Size);
		return added;
	}

	private static void FillWindowControl<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, ref MuiWindowControlStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			Id, out value.Id)) value.Id = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			DisableKeys, out value.DisableKeys)) value.DisableKeys = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			VisibleOnMaximize, out var visible)) visible = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			IsSubWindow, out var subWindow)) subWindow = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			NeedsMouseObject, out var needsMouseObject)) needsMouseObject = 0;
		value.VisibleOnMaximize = visible == 0 ? 0u : 1u;
		value.IsSubWindow = subWindow == 0 ? 0u : 1u;
		value.NeedsMouseObject = needsMouseObject == 0 ? 0u : 1u;
	}

	private static uint ControlValue(MuiWindowControlStateRecord value,
		uint attribute) => attribute == Id ? value.Id : attribute == DisableKeys ?
		value.DisableKeys : attribute == VisibleOnMaximize ?
		value.VisibleOnMaximize : attribute == IsSubWindow ? value.IsSubWindow :
		value.NeedsMouseObject;

	private static bool SetWindowControlValue<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var ownerValue)) return false;
		var owner = ownerValue.Boopsi;
		if (owner.IsNull) return false;
		var previous = 0u;
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, owner,
			attribute, out previous);
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
			record, attribute, value, notify)) return false;
		if (PublishWindowControl(ref platform, state, owner, out _)) return true;
		MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
			attribute, previous, notify);
		return false;
}

	internal static bool TrySet<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, uint attribute, uint value, bool notify, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		handled = attribute == Window || attribute == Open || attribute == Id ||
			attribute == CloseRequest || attribute == RootObject ||
			attribute == DisableKeys ||
			attribute == Sleep ||
			attribute == NoMenus || attribute == HasAlpha || attribute == Opacity ||
			attribute == Title || attribute == Screen || attribute == ScreenTitle ||
			attribute == PublicScreen || attribute == Menu || attribute == RefWindow ||
			attribute == Menustrip ||
			attribute == FancyDrawing ||
			attribute == MenuAction ||
			attribute == NeedsMouseObject ||
			attribute == VisibleOnMaximize || attribute == IsSubWindow ||
			attribute == TabletMessages ||
			attribute == UseBottomBorderScroller ||
			attribute == UseLeftBorderScroller ||
			attribute == UseRightBorderScroller ||
			attribute == AltHeight || attribute == AltLeftEdge ||
			attribute == AltTopEdge || attribute == AltWidth ||
			attribute == Height || attribute == LeftEdge ||
			attribute == TopEdge || attribute == Width ||
			attribute == CloseGadget || attribute == DepthGadget ||
			attribute == DragBar || attribute == SizeGadget ||
			attribute == SizeRight || attribute == AppWindow ||
			attribute == Backdrop || attribute == Borderless ||
			attribute == PanelWindow;
		if (!handled) return false;
		if (attribute == Window)
		{
			// MUIA_Window_Window is getter-only; opening and closing are
			// controlled by the typed Window_Open lifecycle methods.
			return false;
		}
		if (attribute == IsSubWindow &&
			MuiHeadlessObjectCore.IsObjectInitialized(ref platform, record))
			return false;
		if (attribute == TabletMessages &&
			MuiHeadlessObjectCore.IsObjectInitialized(ref platform, record))
			return false;
		if ((attribute == AltHeight || attribute == AltLeftEdge ||
			attribute == AltTopEdge || attribute == AltWidth) &&
			MuiHeadlessObjectCore.IsObjectInitialized(ref platform, record))
			return false;
		if ((attribute == Height || attribute == LeftEdge ||
			attribute == TopEdge || attribute == Width) &&
			MuiHeadlessObjectCore.IsObjectInitialized(ref platform, record))
			return false;
		if ((attribute == CloseGadget || attribute == DepthGadget ||
			attribute == DragBar || attribute == SizeGadget ||
			attribute == SizeRight) &&
			MuiHeadlessObjectCore.IsObjectInitialized(ref platform, record))
			return false;
		if ((attribute == AppWindow || attribute == Backdrop ||
			attribute == Borderless || attribute == PanelWindow) &&
			MuiHeadlessObjectCore.IsObjectInitialized(ref platform, record))
			return false;
		if (attribute == NeedsMouseObject &&
			MuiHeadlessObjectCore.IsObjectInitialized(ref platform, record))
			return false;
		if (attribute == Menu &&
			MuiHeadlessObjectCore.IsObjectInitialized(ref platform, record))
			return false;
		if (attribute == Menustrip)
			return SetMenustrip(ref platform, state, record, value, notify);
		if (attribute == Menu)
			return SetMenustrip(ref platform, state, record,
				value == MenuNoMenu ? 0u : value, notify);
		if (attribute == RootObject)
			return SetRootObject(ref platform, state, record, value, notify);
		if (attribute == RefWindow)
			return SetRefWindow(ref platform, state, record, value, notify);
		if (attribute == Screen)
		{
			var screen = APTR.FromPointer(value);
			if (screen.IsNotNull && !platform.IsMapped(screen, 1)) return false;
		}
		if (attribute == Opacity && value > 255) return false;
		if (attribute == Title || attribute == ScreenTitle ||
			attribute == PublicScreen)
		{
			var title = APTR.FromPointer(value);
			if (title.IsNotNull && !CStringCodec.TryReadLength(ref platform, title,
				65536, out _)) return false;
		}
		var storedValue = (attribute == CloseRequest || attribute == NoMenus ||
			attribute == HasAlpha || attribute == VisibleOnMaximize ||
			attribute == IsSubWindow || attribute == TabletMessages ||
			attribute == UseBottomBorderScroller ||
			attribute == UseLeftBorderScroller ||
			attribute == UseRightBorderScroller ||
			attribute == CloseGadget || attribute == DepthGadget ||
			attribute == DragBar || attribute == SizeGadget ||
			attribute == SizeRight || attribute == AppWindow ||
			attribute == Backdrop || attribute == Borderless ||
			attribute == PanelWindow || attribute == FancyDrawing) &&
			value != 0 ? 1u : value;
		if (attribute == NeedsMouseObject)
			storedValue = value != 0 ? 1u : 0u;
		if (IsControlAttribute(attribute))
			return SetWindowControlValue(ref platform, state, record, attribute,
				storedValue, notify);
		var result = MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform,
			state, record, attribute, storedValue, notify);
		if (result && IsPresentationAttribute(attribute))
		{
			if (MuiHeadlessObjectCodec.TryRead(ref platform, record,
				out var objectValue))
				PublishWindowPresentation(ref platform, state, objectValue.Boopsi,
					out _);
		}
		if (result && IsVisualAttribute(attribute) &&
			MuiHeadlessObjectCodec.TryRead(ref platform, record,
				out var visualObjectValue))
			PublishWindowVisual(ref platform, state, visualObjectValue.Boopsi,
				out _);
		return result;
	}

	// Window.mui getters are projected from the named control, policy,
	// relationship, lifecycle, visual, and event records used below. Keep the
	// admission predicate beside the typed getter so common-control OM_GET can
	// handle Window.mui (which is outside the common-control classifier).
	internal static bool IsPublicGetterAttribute(uint attribute) =>
		attribute == Window || attribute == Open || attribute == Id ||
		attribute == CloseRequest || attribute == RootObject ||
		attribute == DisableKeys || attribute == Sleep || attribute == Menu ||
		attribute == NoMenus || attribute == HasAlpha || attribute == Opacity ||
		attribute == Title || attribute == Screen || attribute == ScreenTitle ||
		attribute == PublicScreen || attribute == RefWindow ||
		attribute == InputEvent || attribute == Menustrip ||
		attribute == FancyDrawing || attribute == MenuAction ||
		attribute == MouseObject || attribute == NeedsMouseObject ||
		attribute == VisibleOnMaximize || attribute == IsSubWindow ||
		attribute == TabletMessages ||
		attribute == UseBottomBorderScroller ||
		attribute == UseLeftBorderScroller ||
		attribute == UseRightBorderScroller ||
		attribute == AltHeight || attribute == AltLeftEdge ||
		attribute == AltTopEdge || attribute == AltWidth ||
		attribute == Height || attribute == LeftEdge || attribute == TopEdge ||
		attribute == Width || attribute == CloseGadget ||
		attribute == DepthGadget || attribute == DragBar ||
		attribute == SizeGadget || attribute == SizeRight ||
		attribute == AppWindow || attribute == Backdrop ||
		attribute == Borderless || attribute == PanelWindow;

	internal static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, out uint value, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		handled = IsPublicGetterAttribute(attribute);
		if (!handled) return false;
		if (IsControlAttribute(attribute))
		{
			var control = ReadWindowControl(ref platform, state, obj);
			value = ControlValue(control, attribute);
			return true;
		}
		if (IsOpenPolicyAttribute(attribute))
		{
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!MuiApplicationWindowCore.TryGetWindowOpenPolicyState(ref platform,
					state, obj, out _))
			{
				handled = false;
				return false;
			}
			if (!MuiApplicationWindowCore.PublishWindowOpenPolicy(ref platform,
				state, obj, out var policy)) return false;
			value = OpenPolicyValue(policy, attribute);
			return true;
		}
		if (attribute == RootObject)
		{
			var relationship = ReadWindowRelationship(ref platform, state, obj);
			value = RelationshipValue(relationship, attribute);
			return true;
		}
		if (attribute == Sleep)
		{
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				Sleep, out _) &&
				!MuiApplicationWindowCore.TryGetWindowSleepState(ref platform, state,
					obj, out _))
			{
				handled = false;
				return false;
			}
			if (!MuiApplicationWindowCore.PublishWindowSleepState(ref platform,
				state, obj) ||
				!MuiApplicationWindowCore.TryGetWindowSleepState(ref platform,
					state, obj, out var sleepState)) return false;
			value = sleepState.Request;
			return true;
		}
		if (attribute == Menu || attribute == Menustrip || attribute == RefWindow)
		{
			var relationship = ReadWindowRelationship(ref platform, state, obj);
			value = RelationshipValue(relationship, attribute);
			return true;
		}
		// MorphOS reports NULL for MUIA_Window_Screen while the window is
		// closed.  The requested Screen pointer remains in named state so an
		// OpenWindow transition can expose it without introducing a positional
		// lifecycle field or a managed mirror.  When no explicit screen was
		// supplied, the platform's preference resolution remains opaque and the
		// getter consequently stays NULL until that capability is implemented.
		if (attribute == Screen &&
			(!MuiApplicationWindowCore.TryGetWindowLifecycleState(ref platform,
				state, obj, out var lifecycle) || lifecycle.NativeWindow.IsNull))
		{
			value = 0;
			return true;
		}
		if (IsPresentationAttribute(attribute))
		{
			var presentation = ReadWindowPresentation(ref platform, state, obj);
			value = PresentationValue(presentation, attribute);
			return true;
		}
		if (IsVisualAttribute(attribute))
		{
			var visual = ReadWindowVisual(ref platform, state, obj);
			value = VisualValue(visual, attribute);
			return true;
		}
		if (attribute == CloseRequest || attribute == InputEvent ||
			attribute == MouseObject)
		{
			if (!MuiApplicationWindowCore.TryGetWindowEventState(ref platform,
				state, obj, out var eventState))
			{
				value = 0;
				return true;
			}
			value = attribute == CloseRequest ? eventState.CloseRequest :
			attribute == InputEvent ? eventState.InputEvent.Raw :
				eventState.MouseObject.Raw;
			return true;
		}
		if (attribute == Window || attribute == Open)
		{
			if (!MuiApplicationWindowCore.TryGetWindowLifecycleState(ref platform,
				state, obj, out var windowLifecycle))
			{
				value = 0;
				return true;
			}
			value = attribute == Window ? windowLifecycle.NativeWindow.Raw :
				windowLifecycle.Open;
			return true;
		}
		var storage = attribute == Window ? NativeWindow :
			attribute == Open ? Open : attribute == Id ? Id : attribute == CloseRequest ? CloseRequest :
			attribute == DisableKeys ? DisableKeys :
			attribute == NoMenus ? NoMenus : attribute == HasAlpha ? HasAlpha :
			attribute == Opacity ? Opacity : attribute == Title ? Title :
			attribute == Screen ? Screen :
			attribute == ScreenTitle ? ScreenTitle :
			attribute == PublicScreen ? PublicScreen :
			attribute == InputEvent ? InputEvent :
			attribute == Sleep ? Sleep :
			attribute == RefWindow ? RefWindow : attribute == VisibleOnMaximize ?
			VisibleOnMaximize : attribute == IsSubWindow ? IsSubWindow :
			attribute == TabletMessages ? TabletMessages :
			attribute == UseBottomBorderScroller ? UseBottomBorderScroller :
			attribute == UseLeftBorderScroller ? UseLeftBorderScroller :
			attribute == UseRightBorderScroller ? UseRightBorderScroller :
			attribute == Menustrip ? Menustrip :
			attribute == FancyDrawing ? FancyDrawing :
			attribute == MenuAction ? MenuAction :
			attribute == MouseObject ? MouseObject :
			attribute == NeedsMouseObject ? NeedsMouseObject :
			attribute == AltHeight ? AltHeight :
			attribute == AltLeftEdge ? AltLeftEdge :
			attribute == AltTopEdge ? AltTopEdge :
			attribute == AltWidth ? AltWidth :
			attribute == Height ? Height :
			attribute == LeftEdge ? LeftEdge :
			attribute == TopEdge ? TopEdge :
			attribute == Width ? Width :
			attribute == CloseGadget ? CloseGadget :
			attribute == DepthGadget ? DepthGadget :
			attribute == DragBar ? DragBar :
			attribute == SizeGadget ? SizeGadget :
			attribute == SizeRight ? SizeRight :
			attribute == AppWindow ? AppWindow :
			attribute == Backdrop ? Backdrop :
			attribute == Borderless ? Borderless :
			PanelWindow;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			storage, out value)) value = 0;
		return true;
	}

	private static bool SetRootObject<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var ownerValue)) return false;
		var owner = ownerValue.Boopsi;
		if (owner.IsNull) return false;
		var current = APTR.Null;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, owner,
			RootObject, out var currentValue)) current = APTR.FromPointer(currentValue);
		var target = APTR.FromPointer(value);
		if (target == current) return true;
		if (target.IsNotNull && (target == owner ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, target).IsNull ||
			MuiHeadlessObjectCore.ParentObject(ref platform, state, target).IsNotNull))
			return false;
		if (current.IsNotNull && !MuiFamilyCore.Remove(ref platform, state,
			owner, current)) return false;
		if (target.IsNotNull && !MuiFamilyCore.AddTail(ref platform, state,
			owner, target))
		{
			if (current.IsNotNull) MuiFamilyCore.AddTail(ref platform, state,
				owner, current);
			return false;
		}
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
			record, RootObject, target.Raw, notify))
		{
			if (target.IsNotNull) MuiFamilyCore.Remove(ref platform, state,
				owner, target);
			if (current.IsNotNull) MuiFamilyCore.AddTail(ref platform, state,
				owner, current);
			return false;
		}
		if (PublishWindowRelationship(ref platform, state, owner, out _))
			return true;
		MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
			RootObject, current.Raw, notify);
		if (target.IsNotNull) MuiFamilyCore.Remove(ref platform, state, owner,
			target);
		if (current.IsNotNull) MuiFamilyCore.AddTail(ref platform, state, owner,
			current);
		return false;
	}

	// MUIA_Window_Menustrip is an owned [ISG] relationship. The target must be
	// a live Menustrip.mui object with no existing parent; the window family
	// owns it until replacement, clearing, or disposal. Relationship changes
	// and the named attribute record are updated failure-atomically.
	private static bool SetMenustrip<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var ownerValue)) return false;
		var owner = ownerValue.Boopsi;
		if (owner.IsNull) return false;
		var current = APTR.Null;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, owner,
			Menustrip, out var currentValue)) current = APTR.FromPointer(currentValue);
		var target = APTR.FromPointer(value);
		if (target == current) return true;
		if (target.IsNotNull && (target == owner ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, target).IsNull ||
			MuiMenuSpecialistCore.Classify(ref platform, state, target) !=
				MuiMenuSpecialistClass.Menustrip ||
			MuiHeadlessObjectCore.ParentObject(ref platform, state, target).IsNotNull))
			return false;
		if (current.IsNotNull && !MuiFamilyCore.Remove(ref platform, state,
			owner, current)) return false;
		if (target.IsNotNull && !MuiFamilyCore.AddTail(ref platform, state,
			owner, target))
		{
			if (current.IsNotNull) MuiFamilyCore.AddTail(ref platform, state,
				owner, current);
			return false;
		}
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
			record, Menustrip, target.Raw, notify))
		{
			if (target.IsNotNull) MuiFamilyCore.Remove(ref platform, state,
				owner, target);
			if (current.IsNotNull) MuiFamilyCore.AddTail(ref platform, state,
				owner, current);
			return false;
		}
		if (PublishWindowRelationship(ref platform, state, owner, out _))
			return true;
		MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
			Menustrip, current.Raw, notify);
		if (target.IsNotNull) MuiFamilyCore.Remove(ref platform, state, owner,
			target);
		if (current.IsNotNull) MuiFamilyCore.AddTail(ref platform, state, owner,
			current);
		return false;
	}

	// MUIA_Window_RefWindow is a caller-owned MUI Window relationship used for
	// relative placement. Keep the pointer in the named attribute record, but
	// validate that a non-NULL target is a live guest object and is not the
	// window being configured. The platform owns the eventual coordinate
	// calculation; no managed object graph or positional packet offset is used.
	private static bool SetRefWindow<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var ownerValue)) return false;
		var owner = ownerValue.Boopsi;
		if (owner.IsNull) return false;
		var current = APTR.Null;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, owner,
			RefWindow, out var currentValue)) current = APTR.FromPointer(currentValue);
		var target = APTR.FromPointer(value);
		if (target == current) return true;
		if (target.IsNotNull && (target == owner ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, target).IsNull))
			return false;
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
			record, RefWindow, value, notify)) return false;
		if (PublishWindowRelationship(ref platform, state, owner, out _))
			return true;
		MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
			RefWindow, current.Raw, notify);
		return false;
	}
}
