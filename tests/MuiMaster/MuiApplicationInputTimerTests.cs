/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationInputTimerTests
{
	[Fact]
	public void TimerQueueEntryUsesTypedTwentyByteGuestRecord()
	{
		Assert.Equal((int)MuiApplicationWindowNodeRecord.Size,
			Unsafe.SizeOf<MuiNativeInputHandlerEntryRecord>());
	}

	[Theory]
	[InlineData(MuiApplicationTimerInputHandlerFlags.Timer, 500, 0, 500000)]
	[InlineData(MuiApplicationTimerInputHandlerFlags.Timer |
		MuiApplicationTimerInputHandlerFlags.Scale10, 150, 1, 500000)]
	[InlineData(MuiApplicationTimerInputHandlerFlags.Timer |
		MuiApplicationTimerInputHandlerFlags.Scale100, 25, 2, 500000)]
	[InlineData(MuiApplicationTimerInputHandlerFlags.Timer |
		MuiApplicationTimerInputHandlerFlags.Scale10 |
		MuiApplicationTimerInputHandlerFlags.Scale100, 3, 3, 0)]
	public void TimerScaleFlagsConvertNamedUnionValuesToTimeVal(uint flags,
		ushort ticks, uint seconds, uint microseconds)
	{
		var value = new MuiInputHandlerValueRecord
		{
			Timer = new MuiInputHandlerTimerValueRecord { Millis = ticks },
		};

		Assert.True(MuiApplicationTimerInputHandlerCore.TryGetInterval(flags,
			value, out var interval));
		Assert.Equal(new TimeVal
		{
			Seconds = seconds,
			Microseconds = microseconds,
		}, interval);
	}

	[Theory]
	[InlineData(0u, 10u)]
	[InlineData(MuiApplicationTimerInputHandlerFlags.Scale10, 10u)]
	[InlineData(MuiApplicationTimerInputHandlerFlags.Timer, 0u)]
	[InlineData(MuiApplicationTimerInputHandlerFlags.Timer | 8u, 10u)]
	public void TimerScaleRejectsMissingTimerZeroAndUnknownValues(uint flags,
		uint ticks)
	{
		var value = new MuiInputHandlerValueRecord
		{
			Timer = new MuiInputHandlerTimerValueRecord
			{
				Millis = unchecked((ushort)ticks),
			},
		};

		Assert.False(MuiApplicationTimerInputHandlerCore.TryGetInterval(flags,
			value, out _));
	}
}
