/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// MUI uses a zero maximum as the unbounded sentinel. These small records keep
// that distinction while a Group combines child maxima; a plain short cannot
// tell an empty aggregate from a finite aggregate that contains an unbounded
// child.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupMaximumSumState
{
	private const int Maximum = 10000;
	private int sum;
	private byte hasValue;
	private byte unbounded;

	internal void Include(short maximum)
	{
		hasValue = 1;
		if (maximum == 0)
		{
			unbounded = 1;
			return;
		}
		if (unbounded != 0) return;
		sum = Add(sum, maximum);
	}

	internal void IncludeGap(int gap)
	{
		if (hasValue == 0 || unbounded != 0 || gap <= 0) return;
		sum = Add(sum, gap);
	}

	internal short Value => hasValue == 0 || unbounded != 0 ? (short)0 :
		unchecked((short)sum);

	private static int Add(int value, int addition)
	{
		var result = value + addition;
		return result > Maximum ? Maximum : result;
	}
}

// Cross-axis Group maxima are the largest child maximum, except that any
// unbounded child makes the aggregate unbounded as well.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupMaximumExtentState
{
	private short extent;
	private byte hasValue;
	private byte unbounded;

	internal void Include(short maximum)
	{
		hasValue = 1;
		if (maximum == 0)
		{
			unbounded = 1;
			return;
		}
		if (unbounded == 0 && maximum > extent) extent = maximum;
	}

	internal short Value => hasValue == 0 || unbounded != 0 ? (short)0 : extent;
}
