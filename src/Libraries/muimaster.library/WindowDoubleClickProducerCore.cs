/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// One producer-owned double-click decision. The input subsystem resolves the
// target and click timing; the freestanding core receives only named values.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiWindowDoubleClickInput
{
	public APTR Object;
	public int Value;
	public uint Available;
}

// Producer-independent admission facts. The input subsystem owns hit-testing
// and the object core owns liveness/parent traversal; this record is the
// explicit value boundary between those policies and Area publication.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiWindowDoubleClickValidation
{
	public APTR Window;
	public APTR Target;
	public uint WindowLive;
	public uint TargetLive;
	public uint Descendant;
}

// Struct-first Window-to-Area publication boundary. Keeping this in a small
// standalone core makes the native ABI closure independent of Window
// lifecycle and event-queue implementation details.
public static class MuiWindowDoubleClickProducerCore
{
	public static bool Accept(MuiWindowDoubleClickInput input,
		MuiWindowDoubleClickValidation validation)
	{
		if (input.Available == 0 || input.Object.IsNull ||
			validation.Window.IsNull || validation.Target.IsNull ||
			input.Object != validation.Target) return false;
		return validation.WindowLive != 0 && validation.TargetLive != 0 &&
			validation.Descendant != 0;
	}

	public static bool Publish<TPlatform>(ref TPlatform platform, APTR state,
		APTR window, MuiWindowDoubleClickInput input)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var target = input.Object;
		var validation = default(MuiWindowDoubleClickValidation);
		validation.Window = window;
		validation.Target = target;
		validation.WindowLive = MuiHeadlessObjectCore.FindObject(ref platform,
			state, window).IsNotNull ? 1u : 0u;
		validation.TargetLive = MuiHeadlessObjectCore.FindObject(ref platform,
			state, target).IsNotNull ? 1u : 0u;
		validation.Descendant = IsObjectInWindow(ref platform, state, target,
			window) ? 1u : 0u;
		if (!Accept(input, validation)) return false;
		return MuiAreaDoubleClickPacketCore.Publish(ref platform, state, target,
			input.Value, true);
	}

	private static bool IsObjectInWindow<TPlatform>(ref TPlatform platform,
		APTR state, APTR target, APTR window)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var current = target;
		for (var index = 0u; index < MuiHeadlessLayout.MaximumTraversal;
			index++)
		{
			if (current == window) return true;
			if (current.IsNull) return false;
			current = MuiHeadlessObjectCore.ParentObject(ref platform, state,
				current);
		}
		return false;
	}
}
