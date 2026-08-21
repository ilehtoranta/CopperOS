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
	}

	[Fact]
	public void AreaDragStateUsesNamedRecordBoundaries()
	{
		var platform = CreatePlatform(out _);
		var storage = APTR.FromPointer(0x1700);
		var cursor = new MuiAreaDragStateFieldCursor
		{
			Record = storage,
			Field = MuiAreaDragStateField.LastY,
		};
		Assert.True(MuiAreaDragStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x1710), address);
		Assert.True(MuiAreaDragStateFieldCursorCodec.TryWriteUInt32(ref platform,
			storage, MuiAreaDragStateField.LastY, unchecked((uint)-12)));
		Assert.True(MuiAreaDragStateFieldCursorCodec.TryReadUInt32(ref platform,
			storage, MuiAreaDragStateField.LastY, out var lastY));
		Assert.Equal(-12, unchecked((int)lastY));
		Assert.False(MuiAreaDragStateFieldCursorCodec.TryReadUInt32(ref platform,
			storage, unchecked((MuiAreaDragStateField)255), out _));
		Assert.False(MuiAreaDragStateFieldCursorCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0xFFFFFFF0u), MuiAreaDragStateField.Flags, out _));
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
		Assert.True(MuiAreaDragMessageCodec.WriteReport(ref platform, packet,
			source.Raw, 1, 2, 1, 0));
		Assert.Equal(MuiAreaDragCore.ReportAbort,
			MuiLayoutDispatcher.Dispatch(ref platform, State, source, packet));
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
