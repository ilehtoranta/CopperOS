using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationInputBufferedTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationQueueReadersUseNamedMethodHeader()
	{
		const uint pushMethod = 0x80429EF8u;
		const uint unpushMethod = 0x804211DDu;
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiApplicationPushMethodMessageCodec.Write(ref platform,
			packet, new MuiApplicationPushMethodMessage
			{
				MethodId = pushMethod,
				Destination = 0x1300,
				Count = 2,
			}));
		Assert.True(MuiApplicationQueuePacketRecordMemoryCodec.TryGetAddress(
			ref platform, packet, MuiApplicationQueuePacketKind.PushMethod,
			MuiApplicationQueuePacketField.Count, out var countAddress));
		Assert.Equal(packet.Raw + MuiApplicationPushMethodMessage.CountOffset,
			countAddress.Raw);
		Assert.True(MuiApplicationQueuePacketRecordMemoryCodec.TryReadUInt32(
			ref platform, packet, MuiApplicationQueuePacketKind.PushMethod,
			MuiApplicationQueuePacketField.Count, out var directCount));
		Assert.Equal(2u, directCount);
		var pushRequest = new MuiApplicationQueuePacketCodec.QueuePacketAddress
		{
			Address = packet,
			Method = pushMethod,
		};
		Assert.True(MuiApplicationQueuePacketCodec.TryReadPush(ref platform,
			ref pushRequest, out var push));
		Assert.Equal(pushMethod, push.MethodId);
		Assert.Equal(0x1300u, push.Destination);
		Assert.True(MuiApplicationQueuePacketCodec.TryReadMethodIdValue(
			ref platform, packet, MuiApplicationQueuePacketKind.PushMethod,
			out var pushMethodId));
		Assert.Equal(pushMethod, pushMethodId);
		Assert.True(MuiApplicationUnpushMethodMessageCodec.Write(ref platform,
			packet, new MuiApplicationUnpushMethodMessage
			{
				MethodId = unpushMethod,
				TargetObject = 0x1400,
				MethodIdSelector = 0x90000001,
				Method = 77,
			}));
		var unpushRequest = new MuiApplicationQueuePacketCodec.QueuePacketAddress
		{
			Address = packet,
			Method = unpushMethod,
		};
		Assert.True(MuiApplicationQueuePacketCodec.TryReadUnpush(ref platform,
			ref unpushRequest, out var unpush));
		Assert.Equal(unpushMethod, unpush.MethodId);
		Assert.Equal(77u, unpush.Method);
		platform.WriteUInt32(packet, 0, 0xDEADBEEFu);
		Assert.False(MuiApplicationQueuePacketCodec.TryReadUnpush(ref platform,
			ref unpushRequest, out _));
		Assert.False(MuiApplicationPushMethodMessageCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFC), out _));
		Assert.False(MuiApplicationUnpushMethodMessageCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFC), out _));
	}

	[Fact]
	public void ApplicationQueuePacketFieldCursorUsesNamedMixedBoundaries()
	{
		var platform = CreatePlatform(out _);
		var push = APTR.FromPointer(0x1200);
		var unpush = APTR.FromPointer(0x1240);

		Assert.True(MuiApplicationQueuePacketFieldCursorCodec.TryWriteUInt32(
			ref platform, push, MuiApplicationQueuePacketKind.PushMethod,
			MuiApplicationQueuePacketField.MethodId, 0x80429EF8u));
		Assert.True(MuiApplicationQueuePacketFieldCursorCodec.TryWriteUInt32(
			ref platform, push, MuiApplicationQueuePacketKind.PushMethod,
			MuiApplicationQueuePacketField.Destination, 0x1300u));
		Assert.True(MuiApplicationQueuePacketFieldCursorCodec.TryWriteUInt32(
			ref platform, push, MuiApplicationQueuePacketKind.PushMethod,
			MuiApplicationQueuePacketField.Count, 2u));
		Assert.True(MuiApplicationQueuePacketFieldCursorCodec.TryReadUInt32(
			ref platform, push, MuiApplicationQueuePacketKind.PushMethod,
			MuiApplicationQueuePacketField.Destination, out var destination));
		Assert.True(MuiApplicationQueuePacketFieldCursorCodec.TryReadUInt32(
			ref platform, push, MuiApplicationQueuePacketKind.PushMethod,
			MuiApplicationQueuePacketField.Count, out var count));
		Assert.Equal(0x1300u, destination);
		Assert.Equal(2u, count);

		Assert.True(MuiApplicationQueuePacketFieldCursorCodec.TryWriteUInt32(
			ref platform, unpush, MuiApplicationQueuePacketKind.UnpushMethod,
			MuiApplicationQueuePacketField.MethodId, 0x804211DDu));
		Assert.True(MuiApplicationQueuePacketFieldCursorCodec.TryWriteUInt32(
			ref platform, unpush, MuiApplicationQueuePacketKind.UnpushMethod,
			MuiApplicationQueuePacketField.TargetObject, 0x1400u));
		Assert.True(MuiApplicationQueuePacketFieldCursorCodec.TryWriteUInt32(
			ref platform, unpush, MuiApplicationQueuePacketKind.UnpushMethod,
			MuiApplicationQueuePacketField.MethodIdSelector, 0x90000001u));
		Assert.True(MuiApplicationQueuePacketFieldCursorCodec.TryWriteUInt32(
			ref platform, unpush, MuiApplicationQueuePacketKind.UnpushMethod,
			MuiApplicationQueuePacketField.Method, 77u));
		Assert.True(MuiApplicationQueuePacketFieldCursorCodec.TryReadUInt32(
			ref platform, unpush, MuiApplicationQueuePacketKind.UnpushMethod,
			MuiApplicationQueuePacketField.Method, out var method));
		Assert.Equal(77u, method);

		Assert.False(MuiApplicationQueuePacketFieldCursorCodec.TryReadUInt32(
			ref platform, push, MuiApplicationQueuePacketKind.PushMethod,
			MuiApplicationQueuePacketField.Method, out _));
		Assert.False(MuiApplicationQueuePacketFieldCursorCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0xFFFFFFF0u),
			MuiApplicationQueuePacketKind.UnpushMethod,
			MuiApplicationQueuePacketField.Method, out _));
	}

	[Fact]
	public void ApplicationPushMethodParametersUseNamedTailBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var message = APTR.FromPointer(0x1800);

		Assert.True(MuiApplicationQueuePacketCodec.TryGetParameters(ref platform,
			message, 2, out var parameters));
		Assert.Equal(APTR.FromPointer(0x180C), parameters);
		Assert.False(MuiApplicationQueuePacketCodec.TryGetParameters(ref platform,
			APTR.FromPointer(0x20FF0), 2, out _));
		Assert.False(MuiApplicationQueuePacketCodec.TryGetParameters(ref platform,
			APTR.FromPointer(0xFFFFFFF0), 1, out _));
		Assert.False(MuiApplicationQueuePacketCodec.TryGetParameters(ref platform,
			message, 0, out _));
		Assert.False(MuiApplicationQueuePacketCodec.TryGetParameters(ref platform,
			message, MuiApplicationPushMethodMessage.MaximumParameterCount + 1,
			out _));
	}

	[Fact]
	public void ApplicationPushMethodParameterCursorUsesNamedEntryBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var cursor = default(MuiApplicationPushMethodParameterCursor);
		cursor.Message = APTR.FromPointer(0x1800);
		cursor.Index = 2;
		Assert.True(MuiApplicationPushMethodParameterCursorCodec.TryGetEntry(
			ref platform, cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x1814), address);
		cursor.Index = MuiApplicationPushMethodParameterCursor.MaximumEntries;
		Assert.False(MuiApplicationPushMethodParameterCursorCodec.TryGetEntry(
			ref platform, cursor, out _));
		cursor.Message = APTR.FromPointer(0xFFFFFFF0);
		cursor.Index = 0;
		Assert.False(MuiApplicationPushMethodParameterCursorCodec.TryGetEntry(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ApplicationPushMethodParameterMemoryAdapterOwnsEntryBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var message = APTR.FromPointer(0x1800);

		Assert.True(MuiApplicationPushMethodParameterMemoryCodec.TryGetEntry(
			ref platform, message, 2, out var address));
		Assert.Equal(APTR.FromPointer(0x1814), address);
		Assert.False(MuiApplicationPushMethodParameterMemoryCodec.TryGetEntry(
			ref platform, message,
			MuiApplicationPushMethodParameterCursor.MaximumEntries, out _));
		Assert.False(MuiApplicationPushMethodParameterMemoryCodec.TryGetEntry(
			ref platform, APTR.FromPointer(0x20FF4), 0, out _));
		Assert.False(MuiApplicationPushMethodParameterMemoryCodec.TryGetEntry(
			ref platform, APTR.FromPointer(0xFFFFFFF0), 0, out _));
		var source = APTR.FromPointer(0x1900);
		var destination = APTR.FromPointer(0x1A00);
		Assert.True(MuiApplicationPushMethodParameterMemoryCodec.TryGetStandaloneEntry(
			ref platform, source, 1, out var sourceSecond));
		Assert.True(MuiApplicationPushMethodParameterMemoryCodec.TryGetStandaloneEntry(
			ref platform, destination, 1, out var destinationSecond));
		Assert.True(MuiApplicationPushMethodParameterCodec.Write(ref platform,
			source, new MuiApplicationPushMethodParameter { Value = 0x12345678 }));
		Assert.True(MuiApplicationPushMethodParameterCodec.Write(ref platform,
			sourceSecond,
			new MuiApplicationPushMethodParameter { Value = 0xABCDEF01 }));
		Assert.True(MuiApplicationPushMethodParameterMemoryCodec.TryCopy(ref platform,
			source, destination, 2));
		Assert.True(MuiApplicationPushMethodParameterCodec.TryRead(ref platform,
			destination, out var first));
		Assert.Equal(0x12345678u, first.Value);
		Assert.True(MuiApplicationPushMethodParameterCodec.TryRead(ref platform,
			destinationSecond, out var second));
		Assert.Equal(0xABCDEF01u, second.Value);
		Assert.False(MuiApplicationPushMethodParameterMemoryCodec.TryCopy(
			ref platform, source, destination, 0));
		Assert.False(MuiApplicationPushMethodParameterCodec.TryRead(ref platform,
			APTR.FromPointer(0x21000), out _));
		Assert.False(MuiApplicationPushMethodParameterCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
	}

	[Fact]
	public void ApplicationPushMethodParameterVectorBridgeUsesNamedRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var vector = APTR.FromPointer(0x1900);
		var expected = new MuiApplicationPushMethodParameter
		{
			Value = 0xFEDCBA98u,
		};

		Assert.True(MuiApplicationPushMethodParameterVectorCodec.TryWrite(ref platform,
			vector, 2, expected));
		Assert.True(MuiApplicationPushMethodParameterVectorCodec.TryRead(ref platform,
			vector, 2, out var decoded));
		Assert.Equal(expected.Value, decoded.Value);
		Assert.True(MuiApplicationPushMethodParameterVectorCodec.TryReadValue(
			ref platform, vector, 2, out var rawValue));
		Assert.Equal(expected.Value, rawValue);
		Assert.True(MuiApplicationPushMethodParameterVectorCodec.TryWriteValue(
			ref platform, vector, 3, 0x80000001u));
		Assert.True(MuiApplicationPushMethodParameterVectorCodec.TryReadValue(
			ref platform, vector, 3, out rawValue));
		Assert.Equal(0x80000001u, rawValue);
		Assert.False(MuiApplicationPushMethodParameterVectorCodec.TryReadValue(
			ref platform, vector,
			MuiApplicationPushMethodParameterCursor.MaximumEntries, out _));
		Assert.False(MuiApplicationPushMethodParameterVectorCodec.TryWriteValue(
			ref platform, APTR.FromPointer(0xFFFFFFF0), 0, expected.Value));
	}

	[Fact]
	public void InputBufferedDispatchesOneQueuedPushMethod()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var target = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		var parameters = APTR.FromPointer(0x1200);
		platform.WriteUInt32(parameters, 0, 0x90000001);
		platform.WriteUInt32(parameters, 4, 77);
		Assert.NotEqual(0u, MuiApplicationWindowCore.PushMethod(ref platform, State,
			application, target, 2, parameters));

		var packet = APTR.FromPointer(0x1240);
		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.ApplicationInputBufferedMethod);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationInputBuffered(
			ref platform, State, application, packet));
		Assert.Equal(target, platform.LastDispatchObject);
		Assert.Equal(0x90000001u, platform.LastDispatchMethod);
		Assert.Equal(77u, platform.LastDispatchArgument);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationInputBuffered(
			ref platform, State, application, packet));
	}

	[Fact]
	public void InputBufferedRejectsUnknownUnmappedAndDeadCalls()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		var packet = APTR.FromPointer(0x1240);
		platform.WriteUInt32(packet, 0, 0xDEADBEEFu);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationInputBuffered(
			ref platform, State, application, packet));
		var unmapped = APTR.FromPointer(0x21000);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationInputBuffered(
			ref platform, State, application, unmapped));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			application));
		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.ApplicationInputBufferedMethod);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationInputBuffered(
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
