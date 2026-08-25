/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// One pass of a weighted one-dimensional Group allocation. The state keeps
// only bounded scalar values; no child pointer or managed collection crosses
// the layout seam.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupAxisAllocationState
{
	internal int Remaining;
	internal int RemainingMinimum;
	internal uint RemainingWeight;
	internal uint Weight;
	internal int Minimum;
	internal int Share;
	internal int Slot;
	internal int Placed;
}

internal static class MuiGroupAxisAllocationCore
{
	internal static MuiGroupAxisAllocationState Begin(int available,
		uint totalWeight, int totalMinimum)
	{
		var state = default(MuiGroupAxisAllocationState);
		state.Remaining = available < 0 ? 0 : available;
		state.RemainingMinimum = totalMinimum < 0 ? 0 : totalMinimum;
		state.RemainingWeight = totalWeight;
		return state;
	}

	internal static void Take(ref MuiGroupAxisAllocationState state,
		uint weight, short minimum, short maximum, bool last)
	{
		state.Weight = weight == 0 ? 1u : weight;
		state.Minimum = minimum > 0 ? minimum : 0;
		var futureMinimum = state.RemainingMinimum - state.Minimum;
		if (futureMinimum < 0) futureMinimum = 0;
		var maximumSlot = state.Remaining - futureMinimum;
		if (maximumSlot < 0) maximumSlot = 0;
		state.Share = last ? state.Remaining :
			state.RemainingWeight == 0 ? 0 :
			(int)((uint)state.Remaining * state.Weight /
				state.RemainingWeight);
		if (state.Share < state.Minimum) state.Share = state.Minimum;
		if (state.Share > maximumSlot) state.Share = maximumSlot;
		if (state.Share < 0) state.Share = 0;
		state.Placed = CapToMaximum(maximum, state.Share);
		if (state.Placed < state.Minimum && maximumSlot >= state.Minimum)
			state.Placed = state.Minimum;
		// A capped non-final child releases its unused share to the remaining
		// weights. The final child retains the complete remaining slot so center
		// alignment can still place a finite child inside excess parent space.
		state.Slot = last ? state.Remaining : state.Placed;
		if (state.Slot > maximumSlot) state.Slot = maximumSlot;
		if (state.Slot < 0) state.Slot = 0;
		state.Remaining -= state.Slot;
		if (state.Remaining < 0) state.Remaining = 0;
		state.RemainingMinimum = futureMinimum;
		state.RemainingWeight = state.RemainingWeight > state.Weight ?
			state.RemainingWeight - state.Weight : 0;
	}

	private static int CapToMaximum(short maximum, int available)
	{
		if (available <= 0) return 0;
		var limit = (int)maximum;
		return limit > 0 && limit < available ? limit : available;
	}
}
