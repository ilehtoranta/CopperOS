/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Explicit row/column inputs are a caller contract in MorphOS: the child
// count is expected to be divisible by the selected axis.  Keep the bounded
// compatibility normalization and its qualification result in one named
// value record instead of scattering arithmetic through layout code.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupGridDimensionPolicy
{
	internal int Count;
	internal int Columns;
	internal int Rows;
	internal int Remainder;
	internal uint ExplicitColumns;
	internal uint ExplicitRows;
	internal uint Divisible;
}
