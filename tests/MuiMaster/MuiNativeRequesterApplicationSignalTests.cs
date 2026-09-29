using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeRequesterApplicationSignalTests
{
	[Fact]
	public void WaitMaskIncludesCtrlCWithEmptyOrWindowSignals()
	{
		var breakSignal = MuiNativeApplicationLoopCore.SignalBreakCtrlC;

		Assert.Equal(breakSignal,
			MuiNativeRequesterSignalPolicyCore.CreateWaitMask(0));
		Assert.Equal(breakSignal | 0x20u,
			MuiNativeRequesterSignalPolicyCore.CreateWaitMask(0x20u));
	}

	[Fact]
	public void BreakSignalInterruptsRequesterWithoutMatchingWindowSignal()
	{
		var breakSignal = MuiNativeApplicationLoopCore.SignalBreakCtrlC;

		Assert.True(MuiNativeRequesterSignalPolicyCore.HasBreakSignal(
			breakSignal));
		Assert.True(MuiNativeRequesterSignalPolicyCore.HasBreakSignal(
			breakSignal | 0x20u));
		Assert.False(MuiNativeRequesterSignalPolicyCore.HasBreakSignal(0x20u));
		Assert.False(MuiNativeRequesterSignalPolicyCore.HasBreakSignal(0));
	}

	[Fact]
	public void ObjectReferenceWaitsForCompletionAndDetachment()
	{
		var state = new MuiNativeRequesterApplicationPresenterRecord
		{
			RequestObject = Amiga.APTR.FromPointer(0x1000),
			PresentationCompleted = 1,
			RequestObjectAttached = 1,
		};
		Assert.False(MuiRequesterApplicationCompletionCore
			.ShouldConsumeReference(state));

		state.RequestObjectAttached = 0;
		Assert.True(MuiRequesterApplicationCompletionCore
			.ShouldConsumeReference(state));

		state.PresentationCompleted = 0;
		Assert.False(MuiRequesterApplicationCompletionCore
			.ShouldConsumeReference(state));
	}
}
