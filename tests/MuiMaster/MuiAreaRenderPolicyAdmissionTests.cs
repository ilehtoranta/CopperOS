using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaRenderPolicyAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint FrameVisible = 0x80426498;
	private const uint RenderPolicyStateKey = 0x7F070037;

	[Fact]
	public void RenderPolicyAdmissionRequiresCanonicalBooleansLiveOwnerAndTitle()
	{
		var platform = CreatePlatform(out var areaClass);
		var title = APTR.FromPointer(0x1400);
		platform.WriteCString(title, "Frame");
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var valid = new MuiAreaRenderPolicyStateRecord
		{
			Magic = MuiAreaRenderPolicyStateRecord.Cookie,
			FillArea = 1,
			Background = 7,
			Frame = 2,
			Font = 0x2200,
			FrameVisible = 1,
			FramePhantomHoriz = 0,
			FrameTitle = title,
			FrameDynamic = 1,
		};

		Assert.True(MuiAreaRenderPolicyStateAdmission.Validate(valid));
		Assert.True(MuiAreaRenderPolicyStateAdmission.ValidateLive(ref platform,
			State, obj, valid));

		var malformed = valid;
		malformed.FillArea = 2;
		Assert.False(MuiAreaRenderPolicyStateAdmission.Validate(malformed));
		Assert.False(MuiAreaRenderPolicyStateAdmission.ValidateLive(ref platform,
			State, obj, malformed));
		malformed = valid;
		malformed.FrameTitle = APTR.FromPointer(0x30000);
		Assert.False(MuiAreaRenderPolicyStateAdmission.ValidateLive(ref platform,
			State, obj, malformed));
		Assert.False(MuiAreaRenderPolicyStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedRenderPolicyFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiAreaLayoutCore.TryReadRenderPolicyState(ref platform, State,
			obj, out _));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, obj,
			RenderPolicyStateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiAreaRenderPolicyStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiAreaRenderPolicyStateField.FrameVisible, 2));
		Assert.True(MuiAreaRenderPolicyStateRecordCodec.TryReadStructural(ref platform,
			block, out var structural));
		Assert.Equal(2u, structural.FrameVisible);
		Assert.False(MuiAreaRenderPolicyStateAdmission.Validate(structural));
		Assert.False(MuiAreaRenderPolicyStateRecordCodec.TryRead(ref platform, block,
			out _));

		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiAreaLayoutCore.TryGetRenderPolicyState(ref platform, State,
			obj, out _));
		Assert.False(MuiAreaLayoutCore.TryReadRenderPolicyState(ref platform, State,
			obj, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, obj,
			FrameVisible, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			obj, FrameVisible, 1, false));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, obj,
			RenderPolicyStateKey));
		Assert.True(MuiAreaRenderPolicyStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiAreaRenderPolicyStateField.FrameVisible,
			out var preserved));
		Assert.Equal(2u, preserved);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR areaClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Rectangle.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		areaClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
