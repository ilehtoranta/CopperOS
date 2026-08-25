/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// One named decision record describes a SameWidth/SameHeight pass on either
// the stacking or cross axis. The common bounds are derived from the visible
// children; no child pointer or managed collection is retained in the record.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupEqualExtentSelection
{
	internal int Available;
	internal int Spacing;
	internal int VisibleCount;
	internal int GapCount;
	internal int MinimumExtent;
	internal int MaximumExtent;
	internal int DefaultExtent;
	internal int EqualExtent;
	internal int TailExtent;
	internal uint HasFiniteMaximum;
}

internal static class MuiGroupEqualExtentCore
{
	// Keep the preferred equal extent inside the same valid finite bounds as
	// the minimum and maximum.  MorphOS dimensions normally satisfy Min <= Max;
	// if malformed child data reverses that relationship, preserve the minimum
	// rather than manufacturing a second contradictory bound.
	internal static void NormalizeDefault(
		ref MuiGroupEqualExtentSelection selection)
	{
		if (selection.HasFiniteMaximum != 0 &&
			selection.MaximumExtent >= selection.MinimumExtent &&
			selection.DefaultExtent > selection.MaximumExtent)
			selection.DefaultExtent = selection.MaximumExtent;
		if (selection.DefaultExtent < selection.MinimumExtent)
			selection.DefaultExtent = selection.MinimumExtent;
	}
}
