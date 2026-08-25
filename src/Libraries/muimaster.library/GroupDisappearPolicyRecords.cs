/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// The selection result is a bounded, stack-resident view used by Group layout.
// It deliberately contains no guest pointers or managed collections: the
// group can re-evaluate the signed Area priorities on every layout pass while
// retaining the MorphOS ordering rule (smaller positive levels disappear
// first).
internal enum MuiGroupDisappearAxis : byte
{
	Horizontal = 0,
	Vertical = 1,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupDisappearSelection
{
	internal MuiGroupDisappearAxis Axis;
	internal int Available;
	internal int Spacing;
	internal int HiddenPriority;
	internal int CandidateCount;
	internal int VisibleCount;
}

// Grid layout resolves the two Area disappearance axes independently.  The
// named pair keeps that decision state explicit while the grid continues to
// place children in their original cells (a hidden child receives a zero
// extent, but its row/column is not silently re-packed).
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupGridDisappearSelection
{
	internal MuiGroupDisappearSelection Horizontal;
	internal MuiGroupDisappearSelection Vertical;
}
