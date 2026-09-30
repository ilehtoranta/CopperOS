/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeApplicationLoopTests
{
	private static readonly APTR Base = APTR.FromPointer(0x1800);
	private static readonly APTR Application = APTR.FromPointer(0x1200);
	private const uint SignalBreakCtrlC = 0x00001000;

	[Fact]
	public void NativeLoopFrameIsACompleteNamedInputAndSignalRecord()
	{
		Assert.Equal((int)MuiNativeApplicationLoopFrameRecord.Size,
			Unsafe.SizeOf<MuiNativeApplicationLoopFrameRecord>());
		var memory = new MuiHeadlessTestPlatform(Base.Raw, 0x200, 0, Base);
		Assert.True(MuiNativeApplicationLoopFrameCodec.TryInitialize(ref memory,
			Base, out var inputAddress, out var signalAddress));
		Assert.True(MuiApplicationInputMessageCodec.TryRead(ref memory,
			inputAddress, out var input));
		Assert.Equal(MuiApplicationDispatcher.ApplicationNewInputMethod,
			input.MethodId);
		Assert.Equal(signalAddress, input.SignalStorage);
		Assert.True(MuiApplicationWindowSignalStorageCodec.TryRead(ref memory,
			signalAddress, out var signals));
		Assert.Equal(0u, signals.Signals);
	}

	[Fact]
	public void NativeExecuteLoopStopsOnQuitReturnIdAndReleasesItsFrame()
	{
		var platform = NewMemory();
		platform.DispatchResult = MuiNativeApplicationLoopCore.ReturnIdQuit;

		var result = MuiNativeApplicationLoopCore.Run(ref platform, Application);

		Assert.Equal(MuiNativeApplicationLoopCore.ReturnIdQuit, result);
		Assert.Equal(1u, platform.DispatchCount);
		Assert.Equal(MuiApplicationDispatcher.ApplicationNewInputMethod,
			platform.LastDispatchMethod);
		Assert.NotEqual(0u, platform.LastDispatchArgument);
		Assert.Equal(0u, platform.WaitMuiSignalsCount);
		Assert.Equal(1u, platform.AllocationCount);
		Assert.Equal(1u, platform.FreeCount);
	}

	[Fact]
	public void NativeExecuteLoopWaitsOnNewInputMaskAndHonorsCtrlC()
	{
		var platform = NewMemory();
		platform.DispatchResult = 0;
		platform.NativeApplicationLoopTestMode = true;
		platform.NativeApplicationLoopSignalMask = 0x00000420;
		platform.PendingSignals = SignalBreakCtrlC;

		var result = MuiNativeApplicationLoopCore.Run(ref platform, Application);

		Assert.Equal(MuiNativeApplicationLoopCore.ReturnIdQuit, result);
		Assert.Equal(1u, platform.DispatchCount);
		Assert.Equal(1u, platform.WaitMuiSignalsCount);
		Assert.Equal(0x00001420u, platform.LastWaitMuiSignalsMask);
		Assert.Equal(1u, platform.AllocationCount);
		Assert.Equal(1u, platform.FreeCount);
	}

	private static MuiHeadlessTestPlatform NewMemory() =>
		new(0x1000, 0x1000, 0x1400, Base);
}
