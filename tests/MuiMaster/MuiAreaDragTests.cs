using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaDragTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void AreaDragPacketsRoundTripAsNamedRecords()
	{
		var platform = CreatePlatform(out var areaClass);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiAreaDragMessageCodec.WriteBegin(ref platform, packet,
			0x3400));
		Assert.True(MuiAreaDragMessageCodec.TryReadBegin(ref platform, packet,
			out var begin));
		Assert.Equal(0x3400u, begin.Object);

		Assert.True(MuiAreaDragMessageCodec.WriteDoDrag(ref platform, packet,
			-17, 23, 1));
		Assert.True(MuiAreaDragMessageCodec.TryReadDoDrag(ref platform, packet,
			out var doDrag));
		Assert.Equal(-17, doDrag.TouchX);
		Assert.Equal(23, doDrag.TouchY);
		Assert.Equal(1u, doDrag.Flags);

		Assert.True(MuiAreaDragMessageCodec.WriteDrop(ref platform, packet,
			0x3400, -4, 12, 3));
		Assert.True(MuiAreaDragMessageCodec.TryReadDrop(ref platform, packet,
			out var drop));
		Assert.Equal(-4, drop.X);
		Assert.Equal(12, drop.Y);
		Assert.Equal(3u, drop.Qualifier);

		Assert.True(MuiAreaDragMessageCodec.WriteEvent(ref platform, packet,
			0x3500, 0x3400, 0x3600, 0x3700, -2, 4, 5));
		Assert.True(MuiAreaDragMessageCodec.TryReadEvent(ref platform, packet,
			out var dragEvent));
		Assert.Equal(-2, dragEvent.MuiKey);
		Assert.Equal(5u, dragEvent.Flags);

		Assert.True(MuiAreaDragMessageCodec.WriteFinish(ref platform, packet,
			0x3400, 1));
		Assert.True(MuiAreaDragMessageCodec.TryReadFinish(ref platform, packet,
			out var finish));
		Assert.Equal(1, finish.DropFollows);

		Assert.True(MuiAreaDragMessageCodec.WriteQuery(ref platform, packet,
			0x3400));
		Assert.True(MuiAreaDragMessageCodec.TryReadQuery(ref platform, packet,
			out var query));
		Assert.Equal(0x3400u, query.Object);

		Assert.True(MuiAreaDragMessageCodec.WriteReport(ref platform, packet,
			0x3400, 8, -9, 2, 7));
		Assert.True(MuiAreaDragMessageCodec.TryReadReport(ref platform, packet,
			out var report));
		Assert.Equal(8, report.X);
		Assert.Equal(-9, report.Y);
		Assert.Equal(2, report.Update);
		Assert.Equal(7u, report.Qualifier);

		Assert.False(MuiAreaDragMessageCodec.TryReadReport(ref platform,
			APTR.FromPointer(0x12F0), out _));
		_ = areaClass;
	}

	[Fact]
	public void AreaDragMethodHeaderUsesNamedField()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiAreaDragMessageCodec.WriteBegin(ref platform, packet,
			0x3400));
		Assert.True(MuiAreaDragMessageCodec.TryReadMethodId(ref platform, packet,
			out var header));
		Assert.Equal(MuiAreaDragMessageCodec.DragBegin, header.MethodId);
		Assert.True(MuiAreaDragMessageCodec.TryReadMethodIdValue(ref platform,
			packet, out var methodId));
		Assert.Equal(MuiAreaDragMessageCodec.DragBegin, methodId);
		Assert.True(MuiAreaDragMessageCodec.WriteMethodIdValue(ref platform,
			packet, 0xF1234567u));
		Assert.True(MuiAreaDragMessageCodec.TryReadMethodIdValue(ref platform,
			packet, out methodId));
		Assert.Equal(0xF1234567u, methodId);
		Assert.False(MuiAreaDragMessageCodec.WriteMethodIdValue(ref platform,
			APTR.Null, 1));
		Assert.False(MuiAreaDragMessageCodec.TryReadMethodIdValue(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
		Assert.False(MuiAreaDragMessageCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void AreaDragFieldCursorUsesNamedMixedPacketBoundaries()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		var cursor = default(MuiAreaDragFieldCursor);
		cursor.Message = packet;
		cursor.Packet = MuiAreaDragPacketKind.Event;
		cursor.Field = MuiAreaDragField.MethodId;
		Assert.True(MuiAreaDragFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var address));
		Assert.Equal(0x1200u, address.Raw);
		cursor.Field = MuiAreaDragField.Window;
		Assert.True(MuiAreaDragFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(0x1204u, address.Raw);
		cursor.Field = MuiAreaDragField.Flags;
		Assert.True(MuiAreaDragFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(0x121Cu, address.Raw);
		Assert.True(MuiAreaDragFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var typedAddress, out var typedSize));
		Assert.Equal(address, typedAddress);
		Assert.Equal(MuiAreaDragMethodMessage.FieldSize, typedSize);
		Assert.True(MuiAreaDragMessageMemoryCodec.TryGetAddress(ref platform,
			cursor, out var memoryAddress, out var memorySize));
		Assert.Equal(typedAddress, memoryAddress);
		Assert.Equal(typedSize, memorySize);

		Assert.True(MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform,
			packet, MuiAreaDragPacketKind.Drop, MuiAreaDragField.X,
			unchecked((uint)-4)));
		Assert.True(MuiAreaDragFieldCursorCodec.TryReadUInt32(ref platform,
			packet, MuiAreaDragPacketKind.Drop, MuiAreaDragField.X,
			out var rawX));
		Assert.Equal(-4, unchecked((int)rawX));
		Assert.False(MuiAreaDragFieldCursorCodec.TryGetAddress(ref platform,
			new MuiAreaDragFieldCursor
			{
				Message = packet,
				Packet = MuiAreaDragPacketKind.Drop,
				Field = MuiAreaDragField.Window,
			}, out _));
		Assert.False(MuiAreaDragFieldCursorCodec.TryGetAddress(ref platform,
			new MuiAreaDragFieldCursor
			{
				Message = APTR.FromPointer(0xFFFFFFF0u),
				Packet = MuiAreaDragPacketKind.Report,
				Field = MuiAreaDragField.Qualifier,
			}, out _));
		Assert.False(MuiAreaDragFieldCursorCodec.TryGetAddress(ref platform,
			new MuiAreaDragFieldCursor
			{
				Message = APTR.Null,
				Packet = MuiAreaDragPacketKind.Event,
				Field = MuiAreaDragField.Flags,
			}, out _, out _));
	}

	[Fact]
	public void AreaDragStateUsesNamedRecordBoundaries()
	{
		var platform = CreatePlatform(out _);
		var storage = APTR.FromPointer(0x1700);
		Assert.True(MuiAreaDragStateMemoryCodec.TryGetAddress(ref platform,
			storage, MuiAreaDragStateField.LastY, out var address));
		Assert.Equal(APTR.FromPointer(0x1710), address);
		Assert.True(MuiAreaDragStateMemoryCodec.TryWriteUInt32(ref platform,
			storage, MuiAreaDragStateField.LastY, unchecked((uint)-12)));
		Assert.True(MuiAreaDragStateMemoryCodec.TryReadUInt32(ref platform,
			storage, MuiAreaDragStateField.LastY, out var lastY));
		Assert.Equal(-12, unchecked((int)lastY));

		var value = new MuiAreaDragState
		{
			Magic = MuiAreaDragStateCodec.Cookie,
			Source = 0x3400,
			Target = 0x3500,
			LastX = -4,
			LastY = -12,
			Qualifier = 3,
			EventFlags = 5,
			Flags = MuiAreaDragState.ActiveFlag,
		};
		MuiAreaDragStateCodec.Write(ref platform, storage, value);
		Assert.True(MuiAreaDragStateCodec.TryRead(ref platform, storage,
			out var decoded));
		Assert.Equal(value.Source, decoded.Source);
		Assert.Equal(value.LastX, decoded.LastX);
		Assert.Equal(value.Flags, decoded.Flags);

		Assert.False(MuiAreaDragStateMemoryCodec.TryReadUInt32(ref platform,
			storage, unchecked((MuiAreaDragStateField)255), out _));
		Assert.False(MuiAreaDragStateMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0xFFFFFFF0u), MuiAreaDragStateField.Flags, out _));

		var cursor = new MuiAreaDragStateFieldCursor
		{
			Record = storage,
			Field = MuiAreaDragStateField.LastY,
		};
		Assert.True(MuiAreaDragStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(APTR.FromPointer(0x1710), address);
		Assert.True(MuiAreaDragStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var typedAddress, out var typedSize));
		Assert.Equal(address, typedAddress);
		Assert.Equal(MuiAreaDragState.FieldSize, typedSize);
		Assert.True(MuiAreaDragStateMemoryCodec.TryGetAddress(ref platform, cursor,
			out var memoryAddress, out var memorySize));
		Assert.Equal(typedAddress, memoryAddress);
		Assert.Equal(typedSize, memorySize);
		cursor.Field = (MuiAreaDragStateField)255;
		Assert.False(MuiAreaDragStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
		cursor.Record = APTR.Null;
		cursor.Field = MuiAreaDragStateField.LastY;
		Assert.False(MuiAreaDragStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
	}

	[Fact]
	public void AreaDragStateFieldAccessUsesCompleteNamedRecord()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1780);
		var expected = new MuiAreaDragState
		{
			Magic = MuiAreaDragStateCodec.Cookie,
			Source = 0x3400,
			Target = 0x3500,
			LastX = -4,
			LastY = -12,
			Qualifier = 3,
			EventFlags = 5,
			Flags = MuiAreaDragState.ActiveFlag | MuiAreaDragState.CapturedFlag,
		};
		MuiAreaDragStateCodec.Write(ref platform, address, expected);
		Assert.True(MuiAreaDragStateMemoryCodec.TryReadUInt32(ref platform,
			address, MuiAreaDragStateField.LastX, out var lastX));
		Assert.Equal(-4, unchecked((int)lastX));
		Assert.True(MuiAreaDragStateMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiAreaDragStateField.LastY, unchecked((uint)-20)));
		Assert.True(MuiAreaDragStateMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiAreaDragStateField.Target, 0x3600));
		Assert.True(MuiAreaDragStateCodec.TryReadStructural(ref platform, address,
			out var updated));
		Assert.Equal(expected.Magic, updated.Magic);
		Assert.Equal(expected.Source, updated.Source);
		Assert.Equal(0x3600u, updated.Target);
		Assert.Equal(expected.LastX, updated.LastX);
		Assert.Equal(-20, updated.LastY);
		Assert.Equal(expected.Qualifier, updated.Qualifier);
		Assert.Equal(expected.EventFlags, updated.EventFlags);
		Assert.Equal(expected.Flags, updated.Flags);

		Assert.True(MuiAreaDragStateMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiAreaDragStateField.Magic, 0xDEADBEEFu));
		Assert.True(MuiAreaDragStateMemoryCodec.TryReadUInt32(ref platform,
			address, MuiAreaDragStateField.Magic, out var magic));
		Assert.Equal(0xDEADBEEFu, magic);
		Assert.False(MuiAreaDragStateCodec.TryRead(ref platform, address, out _));
		Assert.False(MuiAreaDragStateMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x20FF0u), MuiAreaDragStateField.Flags, out _));
		Assert.False(MuiAreaDragStateMemoryCodec.TryWriteUInt32(ref platform,
			APTR.Null, MuiAreaDragStateField.Flags, 1));
		Assert.False(MuiAreaDragStateMemoryCodec.TryReadUInt32(ref platform,
			address, (MuiAreaDragStateField)255, out _));
	}

	[Fact]
	public void AreaDragTypedReadersUseNamedMethodHeader()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiAreaDragMessageCodec.WriteQuery(ref platform, packet,
			0x3400));
		Assert.True(MuiAreaDragMessageCodec.TryReadQuery(ref platform, packet,
			out var query));
		Assert.Equal(MuiAreaDragMessageCodec.DragQuery, query.MethodId);
		Assert.True(MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform,
			packet, MuiAreaDragPacketKind.Query, MuiAreaDragField.MethodId,
			0xDEADBEEFu));
		Assert.False(MuiAreaDragMessageCodec.TryReadQuery(ref platform, packet,
			out _));
	}

	[Fact]
	public void AreaDragDispatcherTracksAcceptedDropAndReleasesState()
	{
		var platform = CreatePlatform(out var areaClass);
		var source = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var target = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, source,
			MuiAreaDragCore.Draggable, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, target,
			MuiAreaDragCore.Dropable, 1, false));
		platform.PointerCaptureSampleAvailable = true;

		var packet = APTR.FromPointer(0x1400);
		Assert.True(MuiAreaDragMessageCodec.WriteBegin(ref platform, packet,
			source.Raw));
		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State, source,
			packet));
		Assert.True(MuiAreaDragMessageCodec.WriteQuery(ref platform, packet,
			source.Raw));
		Assert.Equal(MuiAreaDragCore.QueryAccept,
			MuiLayoutDispatcher.Dispatch(ref platform, State, target, packet));

		Assert.True(MuiAreaDragMessageCodec.WriteReport(ref platform, packet,
			source.Raw, 10, 11, 3, 5));
		Assert.Equal(MuiAreaDragCore.ReportContinue,
			MuiLayoutDispatcher.Dispatch(ref platform, State, target, packet));
		Assert.Equal(1u, platform.PointerCaptureCount);
		Assert.Equal(source, platform.LastPointerCaptureObject);
		Assert.Equal(MuiPointerCaptureKind.AreaDrag,
			platform.LastPointerCaptureKind);
		Assert.Equal(10, platform.LastPointerCaptureStartX);
		Assert.Equal(11, platform.LastPointerCaptureStartY);
		Assert.True(MuiAreaDragMessageCodec.WriteDrop(ref platform, packet,
			source.Raw, -4, 12, 7));
		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State, target,
			packet));
		Assert.True(MuiAreaDragMessageCodec.WriteEvent(ref platform, packet,
			0x3500, source.Raw, 0x3600, 0x3700, -1, 2, 9));
		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State, source,
			packet));

		Assert.True(MuiAreaDragMessageCodec.WriteFinish(ref platform, packet,
			source.Raw, 1));
		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State, source,
			packet));
		Assert.Equal(1u, platform.PointerReleaseCount);
		Assert.Equal(source, platform.LastPointerReleaseObject);
		Assert.Equal(MuiPointerCaptureKind.AreaDrag,
			platform.LastPointerReleaseKind);
		Assert.True(MuiAreaDragMessageCodec.WriteReport(ref platform, packet,
			source.Raw, 1, 2, 1, 0));
		Assert.Equal(MuiAreaDragCore.ReportAbort,
			MuiLayoutDispatcher.Dispatch(ref platform, State, source, packet));
	}

	[Fact]
	public void AreaDoDragStartsFromReceiverAndPreservesTouchInput()
	{
		var platform = CreatePlatform(out var areaClass);
		var source = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, source,
			MuiAreaDragCore.Draggable, 1, false));
		platform.DragRouteSampleAvailable = true;
		var packet = APTR.FromPointer(0x1B00);
		Assert.True(MuiAreaDragMessageCodec.WriteDoDrag(ref platform, packet,
			-17, 23, 1));

		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State, source,
			packet));
		Assert.Equal(MuiDragRoutePhase.Begin, platform.LastDragRouteSample.Phase);
		Assert.Equal(source, platform.LastDragRouteSample.Source);
		Assert.Equal(-17, platform.LastDragRouteSample.X);
		Assert.Equal(23, platform.LastDragRouteSample.Y);
		Assert.Equal(1u, platform.LastDragRouteSample.Flags);

		var stateStorage = APTR.Null;
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, source,
			MuiAreaDragCore.StateKey, out var rawState));
		stateStorage = APTR.FromPointer(rawState);
		Assert.True(MuiAreaDragStateCodec.TryRead(ref platform, stateStorage,
			out var value));
		Assert.Equal(source.Raw, value.Source);
		Assert.True((value.Flags & MuiAreaDragState.ActiveFlag) != 0);

		var nonDraggable = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.Equal(0u, MuiLayoutDispatcher.Dispatch(ref platform, State,
			nonDraggable, packet));
	}

	[Fact]
	public void AreaDragRoutesNamedStructSamplesToNativeCapability()
	{
		var platform = CreatePlatform(out var areaClass);
		var source = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var target = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, source,
			MuiAreaDragCore.Draggable, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, target,
			MuiAreaDragCore.Dropable, 1, false));
		platform.DragRouteSampleAvailable = true;
		// A report result of Lock also remains a non-zero success result for the
		// other MUIM_Drag* phases in this deterministic provider.
		platform.DragRouteResult = MuiAreaDragCore.ReportLock;
		var packet = APTR.FromPointer(0x1C00);

		Assert.True(MuiAreaDragMessageCodec.WriteBegin(ref platform, packet,
			source.Raw));
		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State, source,
			packet));
		Assert.Equal(MuiDragRoutePhase.Begin, platform.LastDragRouteSample.Phase);
		Assert.Equal(source, platform.LastDragRouteSample.Source);

		Assert.True(MuiAreaDragMessageCodec.WriteQuery(ref platform, packet,
			source.Raw));
		Assert.Equal(MuiAreaDragCore.QueryAccept,
			MuiLayoutDispatcher.Dispatch(ref platform, State, target, packet));
		Assert.Equal(MuiDragRoutePhase.Query, platform.LastDragRouteSample.Phase);
		Assert.Equal(target, platform.LastDragRouteSample.Target);

		Assert.True(MuiAreaDragMessageCodec.WriteReport(ref platform, packet,
			source.Raw, -3, 8, 2, 9));
		Assert.Equal(MuiAreaDragCore.ReportLock,
			MuiLayoutDispatcher.Dispatch(ref platform, State, source, packet));
		Assert.Equal(MuiDragRoutePhase.Report, platform.LastDragRouteSample.Phase);
		Assert.Equal(-3, platform.LastDragRouteSample.X);
		Assert.Equal(8, platform.LastDragRouteSample.Y);

		Assert.True(MuiAreaDragMessageCodec.WriteDrop(ref platform, packet,
			source.Raw, 4, -5, 7));
		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State, target,
			packet));
		Assert.True(MuiAreaDragMessageCodec.WriteEvent(ref platform, packet,
			0x3500, source.Raw, 0x3600, 0x3700, -1, 2, 9));
		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State, source,
			packet));
		Assert.True(MuiAreaDragMessageCodec.WriteFinish(ref platform, packet,
			source.Raw, 1));
		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State, source,
			packet));
		Assert.Equal(MuiDragRoutePhase.Finish, platform.LastDragRouteSample.Phase);
		Assert.Equal(1, platform.LastDragRouteSample.DropFollows);
		Assert.Equal(7u, platform.DragRouteCount);
	}

	[Fact]
	public void AreaDragDefaultsRefuseWithoutDraggableAndDropableAttributes()
	{
		var platform = CreatePlatform(out var areaClass);
		var source = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var target = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var packet = APTR.FromPointer(0x1500);
		Assert.True(MuiAreaDragMessageCodec.WriteBegin(ref platform, packet,
			source.Raw));
		Assert.Equal(0u, MuiLayoutDispatcher.Dispatch(ref platform, State, source,
			packet));
		Assert.True(MuiAreaDragMessageCodec.WriteQuery(ref platform, packet,
			source.Raw));
		Assert.Equal(MuiAreaDragCore.QueryRefuse,
			MuiLayoutDispatcher.Dispatch(ref platform, State, target, packet));
		Assert.True(MuiAreaDragMessageCodec.WriteDrop(ref platform, packet,
			source.Raw, 0, 0, 0));
		Assert.Equal(0u, MuiLayoutDispatcher.Dispatch(ref platform, State, target,
			packet));
	}

	[Fact]
	public void AreaDragImagePacketsUseNamedCapabilityBoundary()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var packet = APTR.FromPointer(0x1800);
		Assert.True(MuiAreaDragMessageCodec.WriteCreateDragImage(ref platform,
			packet, -7, 13, 5));
		Assert.True(MuiAreaDragMessageCodec.TryReadCreateDragImage(ref platform,
			packet, out var create));
		Assert.Equal(-7, create.TouchX);
		Assert.Equal(13, create.TouchY);
		Assert.Equal(5u, create.Flags);

		var result = APTR.FromPointer(0x1A00);
		platform.DragImageCreateSampleAvailable = true;
		platform.DragImageCreateResult = result;
		Assert.Equal(result.Raw, MuiLayoutDispatcher.Dispatch(ref platform,
			State, obj, packet));
		Assert.Equal(obj, platform.LastDragImageCreateObject);
		Assert.Equal(-7, platform.LastDragImageCreateTouchX);
		Assert.Equal(13, platform.LastDragImageCreateTouchY);
		Assert.Equal(5u, platform.LastDragImageCreateFlags);

		platform.DragImageDeleteSampleAvailable = true;
		Assert.True(MuiAreaDragMessageCodec.WriteDeleteDragImage(ref platform,
			packet, result.Raw));
		Assert.True(MuiAreaDragMessageCodec.TryReadDeleteDragImage(ref platform,
			packet, out var delete));
		Assert.Equal(result.Raw, delete.DragImage);
		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.Equal(result, platform.LastDragImageDeleted);
	}

	[Fact]
	public void AreaDragMalformedStateIsReplacedAndFreed()
	{
		var platform = CreatePlatform(out var areaClass);
		var source = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, source,
			MuiAreaDragCore.Draggable, 1, false));
		var malformed = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaDragState.Size);
		Assert.True(malformed.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, source,
			MuiAreaDragCore.StateKey, malformed.Raw, false));
		var freesBefore = platform.FreeCount;
		var packet = APTR.FromPointer(0x1600);
		Assert.True(MuiAreaDragMessageCodec.WriteBegin(ref platform, packet,
			source.Raw));
		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State, source,
			packet));
		Assert.Equal(freesBefore + 1, platform.FreeCount);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, source,
			MuiAreaDragCore.StateKey, out var replacement));
		Assert.NotEqual(malformed.Raw, replacement);
	}

	[Fact]
	public void AreaDragStateIsFreedWhenObjectIsDisposed()
	{
		var platform = CreatePlatform(out var areaClass);
		var source = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, source,
			MuiAreaDragCore.Draggable, 1, false));
		var packet = APTR.FromPointer(0x1E00);
		Assert.True(MuiAreaDragMessageCodec.WriteBegin(ref platform, packet,
			source.Raw));
		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State, source,
			packet));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			source, MuiAreaDragCore.StateKey, out var rawStorage));
		var storage = APTR.FromPointer(rawStorage);
		Assert.True(platform.IsMapped(storage, MuiAreaDragState.Size));

		var freesBeforeDispose = platform.FreeCount;
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			source));
		Assert.True(platform.FreeCount > freesBeforeDispose);
		// The deterministic platform retains mapped guest pages after Free; the
		// cleared typed record proves that the Area state itself was released,
		// rather than merely losing its attribute node.
		Assert.Equal(0u, platform.ReadUInt32(storage, 0));
		Assert.False(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			source, MuiAreaDragCore.StateKey, out _));
		Assert.True(MuiHeadlessObjectCore.FindObject(ref platform, State, source)
			.IsNull);
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
