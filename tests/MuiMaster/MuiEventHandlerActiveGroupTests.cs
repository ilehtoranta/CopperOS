using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiEventHandlerActiveGroupTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void IsActiveGroupTracksNamedParentTopology()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			State);
		var windowName = APTR.FromPointer(0x1100);
		var groupName = APTR.FromPointer(0x1140);
		platform.WriteCString(windowName, "Window.mui");
		platform.WriteCString(groupName, "Group.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var windowClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, windowName, APTR.Null, 0, APTR.FromPointer(1));
		var groupClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, groupName, APTR.Null, 0, APTR.FromPointer(1));
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			windowClass, APTR.Null);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		var activeChild = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			windowClass, APTR.Null);
		var outside = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			windowClass, APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, activeChild));
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));

		var handler = APTR.FromPointer(0x1800);
		Assert.True(MuiApplicationWindowRecordPacketCore.WriteEventHandler(
			ref platform, handler, new MuiEventHandlerNodeInput
			{
				Object = group,
				Flags = MuiEventHandlerNodeInput.MUI_EHF_ISACTIVEGRP,
				Events = 4,
			}));
		Assert.True(MuiApplicationWindowCore.AddEventHandler(ref platform, State,
			window, handler));
		Assert.True(MuiApplicationWindowCore.Activate(ref platform, State, window,
			activeChild));
		Assert.True(MuiEventHandlerNodeCodec.TryRead(ref platform, handler,
			out var activeRecord));
		Assert.NotEqual(0, activeRecord.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISACTIVE);

		Assert.True(MuiApplicationWindowCore.Activate(ref platform, State, window,
			outside));
		Assert.True(MuiEventHandlerNodeCodec.TryRead(ref platform, handler,
			out var inactiveRecord));
		Assert.Equal(0, inactiveRecord.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISACTIVE);

		Assert.True(MuiApplicationWindowCore.RemoveEventHandler(ref platform,
			State, window, handler));
		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
	}
}
