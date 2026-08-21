using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationInputTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationInputReadersUseNamedMethodHeader()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.ApplicationReturnIdMethod);
		platform.WriteUInt32(packet, 4, 77);
		Assert.True(MuiApplicationInputPacketCodec.TryReadReturnId(ref platform,
			packet, MuiApplicationDispatcher.ApplicationReturnIdMethod,
			out var returnPacket));
		Assert.Equal(77u, returnPacket.ReturnId);
		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.ApplicationInputBufferedMethod);
		Assert.True(MuiApplicationInputPacketCodec.TryReadInputBuffered(
			ref platform, packet,
			MuiApplicationDispatcher.ApplicationInputBufferedMethod,
			out var buffered));
		Assert.Equal(MuiApplicationDispatcher.ApplicationInputBufferedMethod,
			buffered.MethodId);
		platform.WriteUInt32(packet, 0, 0xDEADBEEFu);
		Assert.False(MuiApplicationInputPacketCodec.TryReadInputBuffered(
			ref platform, packet,
			MuiApplicationDispatcher.ApplicationInputBufferedMethod, out _));
	}

	[Fact]
	public void ApplicationInputPacketFieldCursorUsesNamedMixedBoundary()
	{
		var platform = CreatePlatform(out _);
		var returnId = APTR.FromPointer(0x1200);
		var input = APTR.FromPointer(0x1240);
		var inputHandler = APTR.FromPointer(0x1280);

		Assert.True(MuiApplicationInputPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, returnId,
			MuiApplicationInputPacketKind.ReturnId,
			MuiApplicationInputPacketField.ReturnId, 0x12345678u));
		Assert.True(MuiApplicationInputPacketFieldCursorCodec.TryReadUInt32(
			ref platform, returnId,
			MuiApplicationInputPacketKind.ReturnId,
			MuiApplicationInputPacketField.ReturnId, out var returnValue));
		Assert.Equal(0x12345678u, returnValue);

		Assert.True(MuiApplicationInputPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, input, MuiApplicationInputPacketKind.Input,
			MuiApplicationInputPacketField.SignalStorage, 0x2000u));
		Assert.True(MuiApplicationInputPacketFieldCursorCodec.TryReadUInt32(
			ref platform, input, MuiApplicationInputPacketKind.Input,
			MuiApplicationInputPacketField.SignalStorage, out var signalValue));
		Assert.Equal(0x2000u, signalValue);

		Assert.True(MuiApplicationInputPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, inputHandler,
			MuiApplicationInputPacketKind.InputHandler,
			MuiApplicationInputPacketField.Handler, 0x3000u));
		Assert.True(MuiApplicationInputPacketFieldCursorCodec.TryReadUInt32(
			ref platform, inputHandler,
			MuiApplicationInputPacketKind.InputHandler,
			MuiApplicationInputPacketField.Handler, out var handlerValue));
		Assert.Equal(0x3000u, handlerValue);

		Assert.False(MuiApplicationInputPacketFieldCursorCodec.TryReadUInt32(
			ref platform, input, MuiApplicationInputPacketKind.Input,
			MuiApplicationInputPacketField.ReturnId, out _));
		Assert.False(MuiApplicationInputPacketFieldCursorCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0xFFFFFFFEu),
			MuiApplicationInputPacketKind.ReturnId,
			MuiApplicationInputPacketField.ReturnId, out _));
	}

	[Fact]
	public void InputAndNewInputConsumeReturnIdsAndPublishSignals()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0x20));
		var returnPacket = APTR.FromPointer(0x1200);
		var inputPacket = APTR.FromPointer(0x1240);
		var signals = APTR.FromPointer(0x1280);
		platform.WriteUInt32(returnPacket, 0,
			MuiApplicationDispatcher.ApplicationReturnIdMethod);
		platform.WriteUInt32(returnPacket, 4, 77);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationReturnId(
			ref platform, State, application, returnPacket));
		platform.WriteUInt32(inputPacket, 0,
			MuiApplicationDispatcher.ApplicationNewInputMethod);
		platform.WriteUInt32(inputPacket, 4, signals.Raw);
		Assert.Equal(77u, MuiApplicationDispatcher.DispatchApplicationInput(
			ref platform, State, application, inputPacket));
		Assert.Equal(0u, platform.ReadUInt32(signals, 0));

		platform.PendingSignals = 0x20;
		platform.WriteUInt32(inputPacket, 0,
			MuiApplicationDispatcher.ApplicationInputMethod);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationInput(
			ref platform, State, application, inputPacket));
		Assert.Equal(0x20u, platform.ReadUInt32(signals, 0));
	}

	[Fact]
	public void InputAcceptsNullStorageAndRejectsUnknownTruncatedOrDeadCalls()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.ApplicationNewInputMethod);
		platform.WriteUInt32(packet, 4, 0);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationInput(
			ref platform, State, application, packet));

		platform.WriteUInt32(packet, 0, 0xDEADBEEFu);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationInput(
			ref platform, State, application, packet));
		var truncated = APTR.FromPointer(0x20FFC);
		platform.WriteUInt32(truncated, 0,
			MuiApplicationDispatcher.ApplicationInputMethod);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationInput(
			ref platform, State, application, truncated));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			application));
		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.ApplicationInputMethod);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationInput(
			ref platform, State, application, packet));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR cl)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Application.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR Object(ref MuiHeadlessTestPlatform platform, APTR cl) =>
		MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl, APTR.Null);
}
