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
	public void ApplicationQueuePacketFieldAccessUsesCompleteNamedRecords()
	{
		var platform = CreatePlatform(out _);
		var pushAddress = APTR.FromPointer(0x1280);
		var push = new MuiApplicationPushMethodMessage
		{
			MethodId = 1,
			Destination = 2,
			Count = 3,
		};
		Assert.True(MuiApplicationPushMethodMessageCodec.WriteStructural(ref platform,
			pushAddress, push));
		Assert.True(MuiApplicationQueuePacketRecordMemoryCodec.TryWriteUInt32(
			ref platform, pushAddress, MuiApplicationQueuePacketKind.PushMethod,
			MuiApplicationQueuePacketField.Count, 4));
		Assert.True(MuiApplicationQueuePacketRecordMemoryCodec.TryReadUInt32(
			ref platform, pushAddress, MuiApplicationQueuePacketKind.PushMethod,
			MuiApplicationQueuePacketField.MethodId, out var methodId));
		Assert.Equal(push.MethodId, methodId);
		Assert.True(MuiApplicationPushMethodMessageCodec.TryReadStructural(ref platform,
			pushAddress, out var pushAfter));
		Assert.Equal(4u, pushAfter.Count);
		Assert.Equal(push.Destination, pushAfter.Destination);

		var unpushAddress = APTR.FromPointer(0x12C0);
		var unpush = new MuiApplicationUnpushMethodMessage
		{
			MethodId = 5,
			TargetObject = 6,
			MethodIdSelector = 7,
			Method = 8,
		};
		Assert.True(MuiApplicationUnpushMethodMessageCodec.WriteStructural(ref platform,
			unpushAddress, unpush));
		Assert.True(MuiApplicationQueuePacketRecordMemoryCodec.TryWriteUInt32(
			ref platform, unpushAddress, MuiApplicationQueuePacketKind.UnpushMethod,
			MuiApplicationQueuePacketField.Method, 9));
		Assert.True(MuiApplicationUnpushMethodMessageCodec.TryReadStructural(ref platform,
			unpushAddress, out var unpushAfter));
		Assert.Equal(unpush.TargetObject, unpushAfter.TargetObject);
		Assert.Equal(9u, unpushAfter.Method);
		Assert.False(MuiApplicationQueuePacketRecordMemoryCodec.TryReadUInt32(
			ref platform, pushAddress, MuiApplicationQueuePacketKind.PushMethod,
			MuiApplicationQueuePacketField.Method, out _));
		Assert.False(MuiApplicationQueuePacketRecordMemoryCodec.TryWriteUInt32(
			ref platform, APTR.FromPointer(0x20FFC),
			MuiApplicationQueuePacketKind.UnpushMethod,
			MuiApplicationQueuePacketField.Method, 1));
	}

	[Fact]
	public void ApplicationInputPacketFieldAccessUsesCompleteNamedRecords()
	{
		var platform = CreatePlatform(out _);
		var returnAddress = APTR.FromPointer(0x1400);
		var returnId = new MuiApplicationReturnIdMessage
		{
			MethodId = 1,
			ReturnId = 2,
		};
		Assert.True(MuiApplicationReturnIdMessageCodec.WriteStructural(ref platform,
			returnAddress, returnId));
		Assert.True(MuiApplicationInputPacketRecordMemoryCodec.TryWriteUInt32(
			ref platform, returnAddress, MuiApplicationInputPacketKind.ReturnId,
			MuiApplicationInputPacketField.ReturnId, 3));
		Assert.True(MuiApplicationInputPacketRecordMemoryCodec.TryReadUInt32(
			ref platform, returnAddress, MuiApplicationInputPacketKind.ReturnId,
			MuiApplicationInputPacketField.MethodId, out var returnMethod));
		Assert.Equal(returnId.MethodId, returnMethod);
		Assert.True(MuiApplicationReturnIdMessageCodec.TryReadStructural(ref platform,
			returnAddress, out var returnAfter));
		Assert.Equal(3u, returnAfter.ReturnId);

		var inputAddress = APTR.FromPointer(0x1440);
		var input = new MuiApplicationInputMessage { MethodId = 4, SignalStorage = 5 };
		Assert.True(MuiApplicationInputMessageCodec.WriteStructural(ref platform,
			inputAddress, input));
		Assert.True(MuiApplicationInputPacketRecordMemoryCodec.TryWriteUInt32(
			ref platform, inputAddress, MuiApplicationInputPacketKind.Input,
			MuiApplicationInputPacketField.SignalStorage, 6));
		Assert.True(MuiApplicationInputMessageCodec.TryReadStructural(ref platform,
			inputAddress, out var inputAfter));
		Assert.Equal(input.MethodId, inputAfter.MethodId);
		Assert.Equal(6u, inputAfter.SignalStorage);

		var bufferedAddress = APTR.FromPointer(0x1480);
		Assert.True(MuiApplicationInputBufferedMessageCodec.WriteStructural(ref platform,
			bufferedAddress, new MuiApplicationInputBufferedMessage { MethodId = 7 }));
		Assert.True(MuiApplicationInputPacketRecordMemoryCodec.TryWriteUInt32(
			ref platform, bufferedAddress, MuiApplicationInputPacketKind.InputBuffered,
			MuiApplicationInputPacketField.MethodId, 8));
		Assert.True(MuiApplicationInputBufferedMessageCodec.TryReadStructural(ref platform,
			bufferedAddress, out var bufferedAfter));
		Assert.Equal(8u, bufferedAfter.MethodId);

		var handlerAddress = APTR.FromPointer(0x14C0);
		var handler = new MuiApplicationInputHandlerMessage
		{
			MethodId = 9,
			Handler = 10,
		};
		Assert.True(MuiApplicationInputHandlerMessageCodec.WriteStructural(ref platform,
			handlerAddress, handler));
		Assert.True(MuiApplicationInputPacketRecordMemoryCodec.TryWriteUInt32(
			ref platform, handlerAddress, MuiApplicationInputPacketKind.InputHandler,
			MuiApplicationInputPacketField.Handler, 11));
		Assert.True(MuiApplicationInputHandlerMessageCodec.TryReadStructural(ref platform,
			handlerAddress, out var handlerAfter));
		Assert.Equal(handler.MethodId, handlerAfter.MethodId);
		Assert.Equal(11u, handlerAfter.Handler);
		Assert.False(MuiApplicationInputPacketRecordMemoryCodec.TryReadUInt32(
			ref platform, returnAddress, MuiApplicationInputPacketKind.ReturnId,
			MuiApplicationInputPacketField.Handler, out _));
		Assert.False(MuiApplicationInputPacketRecordMemoryCodec.TryWriteUInt32(
			ref platform, APTR.FromPointer(0x20FFC), MuiApplicationInputPacketKind.Input,
			MuiApplicationInputPacketField.MethodId, 1));
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
	public void ApplicationPushMethodParameterRangeUsesNamedCursor()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var message = APTR.FromPointer(0x1800);
		var cursor = default(MuiApplicationPushMethodParameterCursor);
		cursor.Message = message;
		cursor.Index = 0;
		Assert.True(MuiApplicationPushMethodParameterCursorCodec.TryGetEntry(
			ref platform, cursor, out var expected));
		Assert.True(MuiApplicationQueuePacketCodec.TryGetParameters(ref platform,
			message, 3, out var actual));
		Assert.Equal(expected, actual);
		Assert.True(MuiApplicationPushMethodParameterCodec.WriteValue(ref platform,
			actual, 0xF1234567u));
		Assert.True(MuiApplicationPushMethodParameterCodec.TryReadValue(ref platform,
			expected, out var value));
		Assert.Equal(0xF1234567u, value);
	}

	[Fact]
	public void ApplicationPushMethodParameterUsesSharedUlongRecordCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var address = APTR.FromPointer(0x1C00);

		Assert.True(MuiGuestUlongStorageCodec.WriteValue(ref platform, address,
			0xF0E1D2C3u));
		Assert.True(MuiApplicationPushMethodParameterCodec.TryReadValue(ref platform,
			address, out var value));
		Assert.Equal(0xF0E1D2C3u, value);

		Assert.True(MuiApplicationPushMethodParameterCodec.WriteValue(ref platform,
			address, 0x80706050u));
		Assert.True(MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
			out value));
		Assert.Equal(0x80706050u, value);
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
	public void ApplicationPushMethodParameterVectorCursorExchangesNamedRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var cursor = new MuiApplicationPushMethodParameterVectorCursor
		{
			Base = APTR.FromPointer(0x1900),
			Index = 2,
		};
		var expected = new MuiApplicationPushMethodParameter
		{
			Value = 0xFEDCBA98u,
		};
		Assert.True(MuiApplicationPushMethodParameterVectorCodec.TryWrite(
			ref platform, cursor, expected));
		Assert.True(MuiApplicationPushMethodParameterVectorCodec.TryRead(
			ref platform, cursor, out var decoded));
		Assert.Equal(expected.Value, decoded.Value);
		cursor.Index = MuiApplicationPushMethodParameterVectorCursor.MaximumEntries;
		Assert.False(MuiApplicationPushMethodParameterVectorCodec.TryRead(
			ref platform, cursor, out _));
		Assert.False(MuiApplicationPushMethodParameterVectorCodec.TryWrite(
			ref platform, cursor, expected));
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
