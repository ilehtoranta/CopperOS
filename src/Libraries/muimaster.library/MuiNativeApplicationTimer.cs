/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

internal static class MuiApplicationTimerInputHandlerFlags
{
	internal const uint Timer = 1u << 0;
	internal const uint Scale10 = 1u << 1;
	internal const uint Scale100 = 1u << 2;
	internal const uint KnownMask = Timer | Scale10 | Scale100;
}

// A typed conversion boundary between the public MUI timer union and the
// timer.device TimeVal. Scale flags choose decimal timer units; no floating
// point or host-runtime time service is involved.
internal static class MuiApplicationTimerInputHandlerCore
{
	internal static bool TryGetInterval(uint flags,
		MuiInputHandlerValueRecord value, out TimeVal interval)
	{
		interval = default;
		if ((flags & MuiApplicationTimerInputHandlerFlags.Timer) == 0 ||
			(flags & ~MuiApplicationTimerInputHandlerFlags.KnownMask) != 0)
			return false;

		var timerValue = value.Timer;
		if (timerValue.Millis == 0) return false;
		var ticks = (uint)timerValue.Millis;
		if ((flags & MuiApplicationTimerInputHandlerFlags.Scale10) != 0)
		{
			if ((flags & MuiApplicationTimerInputHandlerFlags.Scale100) != 0)
				interval.Seconds = ticks;
			else
			{
				interval.Seconds = ticks / 100u;
				interval.Microseconds = (ticks % 100u) * 10000u;
			}
		}
		else if ((flags & MuiApplicationTimerInputHandlerFlags.Scale100) != 0)
		{
			interval.Seconds = ticks / 10u;
			interval.Microseconds = (ticks % 10u) * 100000u;
		}
		else
		{
			interval.Seconds = ticks / 1000u;
			interval.Microseconds = (ticks % 1000u) * 1000u;
		}
		return interval.Seconds != 0 || interval.Microseconds != 0;
	}
}

internal static class MuiNativeApplicationTimer
{
	internal static bool TryCreatePort(out APTR port)
	{
		port = Exec.CreateMsgPort();
		if (port.IsNull) return false;
		var memory = default(MuiNativeClassMemory);
		if (!ExecMsgPortCodec.IsMapped(ref memory, port))
		{
			Exec.DeleteMsgPort(port);
			port = APTR.Null;
			return false;
		}
		var value = ExecMsgPortCodec.Read(ref memory, port);
		if (value.SignalTask.IsNull || value.SignalBit >= 32)
		{
			Exec.DeleteMsgPort(port);
			port = APTR.Null;
			return false;
		}
		return true;
	}

	internal static bool TryCreateRequest(APTR port, uint flags,
		MuiInputHandlerValueRecord value, out APTR request)
	{
		request = APTR.Null;
		if (port.IsNull || !MuiApplicationTimerInputHandlerCore.TryGetInterval(
			flags, value, out var interval)) return false;
		request = Exec.CreateIORequest(port, TimerRequest.Size);
		if (request.IsNull) return false;
		if (Exec.OpenDevice(TimerDevice.Name, (uint)TimerUnit.MicroHz,
			request, 0) != 0)
		{
			Exec.DeleteIORequest(request);
			request = APTR.Null;
			return false;
		}

		var memory = default(MuiNativeClassMemory);
		if (!TimerRequestCodec.IsMapped(ref memory, request))
		{
			Exec.CloseDevice(request);
			Exec.DeleteIORequest(request);
			request = APTR.Null;
			return false;
		}
		var timerRequest = TimerRequestCodec.Read(ref memory, request);
		if (timerRequest.Request.Device.IsNull ||
			timerRequest.Request.Message.ReplyPort != port)
		{
			Exec.CloseDevice(request);
			Exec.DeleteIORequest(request);
			request = APTR.Null;
			return false;
		}
		timerRequest.Request.Command = (DeviceCommand)TimerCommand.AddRequest;
		timerRequest.Time = interval;
		TimerRequestCodec.Write(ref memory, request, timerRequest);
		Exec.SendIO(request);
		return true;
	}

	internal static bool Schedule(APTR request, uint flags,
		MuiInputHandlerValueRecord value)
	{
		if (!MuiApplicationTimerInputHandlerCore.TryGetInterval(flags, value,
			out var interval)) return false;
		var memory = default(MuiNativeClassMemory);
		if (!TimerRequestCodec.IsMapped(ref memory, request)) return false;
		var timerRequest = TimerRequestCodec.Read(ref memory, request);
		if (timerRequest.Request.Device.IsNull ||
			timerRequest.Request.Message.ReplyPort.IsNull) return false;
		timerRequest.Request.Command = (DeviceCommand)TimerCommand.AddRequest;
		timerRequest.Time = interval;
		TimerRequestCodec.Write(ref memory, request, timerRequest);
		Exec.SendIO(request);
		return true;
	}

	// Every linked timer request is either pending or completed-but-uncollected.
	// CheckIO distinguishes those states; an active request is aborted before
	// WaitIO, and both states are retired before closing the device handle.
	internal static void ReleaseRequest(APTR request)
	{
		if (request.IsNull) return;
		if (Exec.CheckIO(request).IsNull) Exec.AbortIO(request);
		Exec.WaitIO(request);
		Exec.CloseDevice(request);
		Exec.DeleteIORequest(request);
	}

	internal static void ReleaseUnsentRequest(APTR request)
	{
		if (request.IsNull) return;
		Exec.CloseDevice(request);
		Exec.DeleteIORequest(request);
	}

	internal static void ReleasePort(APTR port)
	{
		if (port.IsNotNull) Exec.DeleteMsgPort(port);
	}
}
