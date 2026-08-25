using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaResizeTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ResizePacketsRoundTripAsNamedRecords()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiAreaResizeMessageCodec.WriteInit(ref platform, packet,
			0xA5A5));
		Assert.True(MuiAreaResizeMessageCodec.TryReadInit(ref platform, packet,
			out var init));
		Assert.Equal(MuiAreaResizeMessageCodec.InitResize, init.MethodId);
		Assert.Equal(0xA5A5u, init.Flags);

		Assert.True(MuiAreaResizeMessageCodec.WriteExit(ref platform, packet));
		Assert.True(MuiAreaResizeMessageCodec.TryReadExit(ref platform, packet,
			out var exit));
		Assert.Equal(MuiAreaResizeMessageCodec.ExitResize, exit.MethodId);
		Assert.False(MuiAreaResizeMessageCodec.TryReadInit(ref platform,
			APTR.FromPointer(0x20FFCu), out _));
	}

	[Fact]
	public void ResizeStateRecordUsesNamedFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1500);
		var expected = default(MuiAreaResizeStateRecord);
		expected.Magic = MuiAreaResizeStateRecord.Cookie;
		expected.Active = 1;
		expected.Flags = 0x01020304;
		expected.Generation = 9;

		Assert.True(MuiAreaResizeStateRecordCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiAreaResizeStateRecordCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(expected.Magic, decoded.Magic);
		Assert.Equal(expected.Active, decoded.Active);
		Assert.Equal(expected.Flags, decoded.Flags);
		Assert.Equal(expected.Generation, decoded.Generation);

		var cursor = new MuiAreaResizeStateFieldCursor
		{
			Record = address,
			Field = MuiAreaResizeStateField.Generation,
		};
		Assert.True(MuiAreaResizeStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 12, fieldAddress.Raw);
		Assert.True(MuiAreaResizeStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaResizeStateField.Flags, 0x55667788));
		Assert.True(MuiAreaResizeStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiAreaResizeStateField.Flags, out var flags));
		Assert.Equal(0x55667788u, flags);
		Assert.False(MuiAreaResizeStateRecordCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
	}

	[Fact]
	public void DispatcherTracksResizeLifecycleAndGeneration()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var packet = APTR.FromPointer(0x1300);

		Assert.True(MuiAreaResizeMessageCodec.WriteInit(ref platform, packet,
			0x00000011));
		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiAreaResizePacketCore.TryGet(ref platform, State, obj,
			out var first));
		Assert.Equal(1u, first.Active);
		Assert.Equal(0x11u, first.Flags);
		Assert.Equal(1u, first.Generation);

		Assert.True(MuiAreaResizeMessageCodec.WriteInit(ref platform, packet,
			0x00000022));
		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiAreaResizePacketCore.TryGet(ref platform, State, obj,
			out var second));
		Assert.Equal(1u, second.Active);
		Assert.Equal(0x22u, second.Flags);
		Assert.Equal(2u, second.Generation);

		Assert.True(MuiAreaResizeMessageCodec.WriteExit(ref platform, packet));
		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiAreaResizePacketCore.TryGet(ref platform, State, obj,
			out var exited));
		Assert.Equal(0u, exited.Active);
		Assert.Equal(0x22u, exited.Flags);
		Assert.Equal(2u, exited.Generation);
		Assert.Equal(0u, MuiLayoutDispatcher.Dispatch(ref platform, State, obj,
			packet));

		Assert.True(MuiAreaResizePacketCore.Init(ref platform, State, obj, 7));
		Assert.True(MuiAreaResizeCore.Cleanup(ref platform, State, obj));
		Assert.False(MuiAreaResizePacketCore.TryGet(ref platform, State, obj,
			out _));
	}

	[Fact]
	public void ResizeTransitionHelpersRejectInactiveExit()
	{
		Assert.True(MuiAreaResizeCore.BuildInitState(7, uint.MaxValue,
			out var initialized));
		Assert.Equal(1u, initialized.Active);
		Assert.Equal(7u, initialized.Flags);
		Assert.Equal(1u, initialized.Generation);
		Assert.True(MuiAreaResizeCore.BuildExitState(initialized,
			out var exited));
		Assert.Equal(0u, exited.Active);
		Assert.Equal(7u, exited.Flags);
		Assert.False(MuiAreaResizeCore.BuildExitState(exited, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR areaClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Area.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		areaClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
