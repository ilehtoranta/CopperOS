/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Struct-first MorphOS MUIA_Timer projection. The input layer decides when a
// relverify timer starts and advances; this core only stores and exposes the
// signed event counter through a typed publication boundary.
internal static class MuiAreaTimerCore
{
	internal const uint StateKey = 0x7F070065u;
	internal const uint EventStateKey = 0x7F070066u;

	internal static bool TryReadState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaTimerStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var hasRaw = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			MuiCommonControlCore.Timer, out var raw);
		var current = hasRaw ? unchecked((int)raw) : 0;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj, StateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			StateKey);
		MuiAreaTimerStateRecord record;
		if (block.IsNotNull || length != 0)
		{
			// A present block is authoritative typed state.  Do not repair a
			// malformed signed counter record from its raw compatibility slot.
			if (length != unchecked((int)MuiAreaTimerStateRecord.Size) ||
				!MuiAreaTimerStateRecordCodec.TryReadStructural(ref platform, block,
					out record) || !MuiAreaTimerStateAdmission.ValidateLive(ref platform,
					state, obj, record)) return false;
			if (hasRaw && record.Value != current)
			{
				record.Value = current;
				record.Generation = record.Generation == uint.MaxValue ? 1u :
					record.Generation + 1u;
				if (!MuiAreaTimerStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			value.Value = hasRaw ? current : record.Value;
			return ReadEventState(ref platform, state, obj, ref value);
		}
		if (!WriteState(ref platform, state, obj, current, 1)) return false;
		value.Value = current;
		return ReadEventState(ref platform, state, obj, ref value);
	}

	internal static bool WriteState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int value, uint generation)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaTimerStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaTimerStateRecord.Size);
		var record = default(MuiAreaTimerStateRecord);
		record.Magic = MuiAreaTimerStateRecord.Cookie;
		record.Value = value;
		record.Generation = generation == 0 ? 1u : generation;
		var written = MuiAreaTimerStateAdmission.ValidateLive(ref platform, state,
			obj, record) && MuiAreaTimerStateRecordCodec.Write(ref platform, scratch,
			record);
		var stored = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			StateKey, scratch, unchecked((int)MuiAreaTimerStateRecord.Size));
		platform.Clear(scratch, MuiAreaTimerStateRecord.Size);
		platform.Free(scratch, MuiAreaTimerStateRecord.Size);
		return stored;
	}

	internal static bool Publish<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull ||
			MuiCommonControlCore.Classify(ref platform, state, obj) ==
			MuiControlClass.Unknown) return false;
		// Validate the existing typed state before publishing the producer's
		// counter so malformed present state cannot be bypassed by raw mutation.
		if (!TryReadState(ref platform, state, obj, out _)) return false;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiCommonControlCore.Timer, unchecked((uint)value), notify))
			return false;
		return TryReadState(ref platform, state, obj, out _);
	}

	// Process one event supplied by the native input/event producer. The
	// producer decides when the MorphOS "little delay" has elapsed and supplies
	// a monotonically changing IntuiTick identity. This keeps scheduling and
	// clock ownership outside the freestanding core while making the state
	// transitions deterministic and testable.
	internal static bool ProcessEvent<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaTimerEventInput input, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull ||
			!EnsureEventState(ref platform, state, obj) ||
			!TryReadState(ref platform, state, obj, out var timer) ||
			!TryReadEventState(ref platform, state, obj, out var eventState))
			return false;
		var next = eventState;
		var valueChanged = false;
		switch (input.Kind)
		{
			case MuiAreaTimerEventKind.RelVerifyPress:
				next.Armed = 1;
				next.MouseOver = 1;
				next.DelayElapsed = input.DelayElapsed == 0 ? 0u : 1u;
				next.LastTick = input.Tick;
				break;
			case MuiAreaTimerEventKind.IntuiTick:
				if (next.Armed != 0)
				{
					next.MouseOver = input.PointerOver == 0 ? 0u : 1u;
					if (next.MouseOver != 0 && input.DelayElapsed != 0)
						next.DelayElapsed = 1;
					if (next.MouseOver != 0 && next.DelayElapsed != 0 &&
						input.Tick != next.LastTick)
					{
						timer.Value = unchecked(timer.Value + 1);
						valueChanged = true;
					}
					next.LastTick = input.Tick;
				}
				break;
			case MuiAreaTimerEventKind.PointerEnter:
				next.MouseOver = 1;
				break;
			case MuiAreaTimerEventKind.PointerLeave:
				next.MouseOver = 0;
				next.DelayElapsed = 0;
				break;
			case MuiAreaTimerEventKind.RelVerifyRelease:
				next.Armed = 0;
				next.MouseOver = 0;
				next.DelayElapsed = 0;
				break;
			default:
				return false;
		}
		if (valueChanged && !MuiHeadlessObjectCore.SetAttribute(ref platform,
			state, obj, MuiCommonControlCore.Timer,
			unchecked((uint)timer.Value), notify)) return false;
		next.Generation = next.Generation == uint.MaxValue ? 1u :
			next.Generation + 1u;
		if (!WriteEventState(ref platform, state, obj, next)) return false;
		return true;
	}

	// Pointer hit-testing is owned by the window/input producer. This helper
	// only forwards a change between two already-live objects to timer records
	// that were armed by relverify; unarmed objects do not acquire timer state
	// merely because the pointer crossed them.
	internal static bool ProcessPointerTransition<TPlatform>(
		ref TPlatform platform, APTR state, APTR previous, APTR current,
		uint tick, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (previous == current) return true;
		if (previous.IsNotNull && TryReadArmed(ref platform, state, previous))
		{
			var leave = default(MuiAreaTimerEventInput);
			leave.Kind = MuiAreaTimerEventKind.PointerLeave;
			leave.Tick = tick;
			leave.PointerOver = 0;
			if (!ProcessEvent(ref platform, state, previous, leave, notify))
				return false;
		}
		if (current.IsNotNull && TryReadArmed(ref platform, state, current))
		{
			var enter = default(MuiAreaTimerEventInput);
			enter.Kind = MuiAreaTimerEventKind.PointerEnter;
			enter.Tick = tick;
			enter.PointerOver = 1;
			if (!ProcessEvent(ref platform, state, current, enter, notify))
				return false;
		}
		return true;
	}

	internal static bool TryReadArmed<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (obj.IsNull || MuiHeadlessObjectCore.FindObject(ref platform, state,
			obj).IsNull || !TryReadEventState(ref platform, state, obj,
			out var eventState)) return false;
		return eventState.Armed != 0;
	}

	internal static bool TryReadEventState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaTimerEventStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			EventStateKey);
		return MuiStoreCore.DataspaceLength(ref platform, state, obj,
			EventStateKey) == unchecked((int)MuiAreaTimerEventStateRecord.Size) &&
			MuiAreaTimerEventStateCodec.TryReadStructural(ref platform, block,
				out value) && MuiAreaTimerEventStateAdmission.ValidateLive(ref platform,
				state, obj, value);
	}

	private static bool ReadEventState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, ref MuiAreaTimerStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadEventState(ref platform, state, obj, out var eventState))
		{
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				EventStateKey);
			var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
				EventStateKey);
			if (block.IsNotNull || length != 0) return false;
			value.Armed = 0;
			value.MouseOver = 0;
			value.DelayElapsed = 0;
			value.LastTick = 0;
			return true;
		}
		value.Armed = eventState.Armed;
		value.MouseOver = eventState.MouseOver;
		value.DelayElapsed = eventState.DelayElapsed;
		value.LastTick = eventState.LastTick;
		return true;
	}

	private static bool WriteEventState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaTimerEventStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value.Magic = MuiAreaTimerEventStateRecord.Cookie;
		if (!MuiAreaTimerEventStateAdmission.ValidateLive(ref platform, state,
			obj, value)) return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaTimerEventStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaTimerEventStateRecord.Size);
		var written = MuiAreaTimerEventStateCodec.Write(ref platform, scratch,
			value);
		var stored = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			EventStateKey, scratch,
			unchecked((int)MuiAreaTimerEventStateRecord.Size));
		platform.Clear(scratch, MuiAreaTimerEventStateRecord.Size);
		platform.Free(scratch, MuiAreaTimerEventStateRecord.Size);
		return stored;
	}

	private static bool EnsureEventState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			EventStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			EventStateKey);
		if (block.IsNotNull || length != 0)
		{
			// A present event record is authoritative.  Never replace malformed
			// input state merely because strict decoding rejected it.
			return TryReadEventState(ref platform, state, obj, out _);
		}
		var next = default(MuiAreaTimerEventStateRecord);
		next.Magic = MuiAreaTimerEventStateRecord.Cookie;
		next.Generation = 1;
		return WriteEventState(ref platform, state, obj, next);
	}
}

// Public typed Area timer getter/publication seam. Publication is explicit so
// timer cadence remains with the future input/event producer, not this state
// record implementation.
public static class MuiAreaTimerPacketCore
{
	public static bool Publish<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int value, bool notify = true)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaTimerCore.Publish(ref platform, state, obj, value, notify);

	public static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out MuiAreaTimerStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaTimerCore.TryReadState(ref platform, state, obj, out value);

	public static bool ProcessEvent<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaTimerEventInput input, bool notify = true)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaTimerCore.ProcessEvent(ref platform, state, obj, input, notify);

	public static bool ProcessPointerTransition<TPlatform>(
		ref TPlatform platform, APTR state, APTR previous, APTR current,
		uint tick, bool notify = true)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaTimerCore.ProcessPointerTransition(ref platform, state, previous,
			current, tick, notify);
}
