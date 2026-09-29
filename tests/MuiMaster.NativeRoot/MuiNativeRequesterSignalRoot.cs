using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.NativeRoot;

public static class MuiNativeRequesterSignalRoot
{
	public static uint CtrlCInterruptsApplicationRequesterPump()
	{
		var breakSignal = MuiNativeApplicationLoopCore.SignalBreakCtrlC;
		if (MuiNativeRequesterSignalPolicyCore.CreateWaitMask(0) != breakSignal)
			return 1;
		const uint windowSignal = 0x20;
		if (MuiNativeRequesterSignalPolicyCore.CreateWaitMask(windowSignal) !=
			(windowSignal | breakSignal)) return 2;
		if (!MuiNativeRequesterSignalPolicyCore.HasBreakSignal(breakSignal |
			windowSignal) ||
			MuiNativeRequesterSignalPolicyCore.HasBreakSignal(windowSignal)) return 3;
		var requestObject = new MuiNativeRequesterApplicationPresenterRecord
		{
			RequestObject = Amiga.APTR.FromPointer(0x1000),
			PresentationCompleted = 1,
			RequestObjectAttached = 1,
		};
		if (MuiRequesterApplicationCompletionCore
			.ShouldConsumeReference(requestObject)) return 4;
		requestObject.RequestObjectAttached = 0;
		if (!MuiRequesterApplicationCompletionCore
			.ShouldConsumeReference(requestObject)) return 5;
		requestObject.PresentationCompleted = 0;
		if (MuiRequesterApplicationCompletionCore
			.ShouldConsumeReference(requestObject)) return 6;
		return 42;
	}
}
