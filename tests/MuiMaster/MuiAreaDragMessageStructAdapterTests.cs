using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaDragMessageStructAdapterTests
{
	[Fact]
	public void DragMessageRecordsUseNamedFieldsAndCompleteBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var doDragAddress = APTR.FromPointer(0x3000);
		Assert.True(MuiAreaDragMessageCodec.WriteDoDrag(ref platform,
			doDragAddress, -5, 7, 2));
		Assert.True(MuiAreaDragMessageCodec.TryReadDoDrag(ref platform,
			doDragAddress, out var doDrag));
		Assert.Equal(-5, doDrag.TouchX);
		Assert.Equal(7, doDrag.TouchY);
		Assert.Equal(2u, doDrag.Flags);
		Assert.True(MuiAreaDragMessageMemoryCodec.TryGetAddress(ref platform,
			doDragAddress, MuiAreaDragPacketKind.DoDrag,
			MuiAreaDragField.TouchY, out var touchYAddress));
		Assert.Equal(APTR.FromPointer(0x3008), touchYAddress);

		var dropAddress = APTR.FromPointer(0x3040);
		Assert.True(MuiAreaDragMessageCodec.WriteDrop(ref platform, dropAddress,
			0x3500, -12, 24, 0x40));
		Assert.True(MuiAreaDragMessageCodec.TryReadDrop(ref platform, dropAddress,
			out var drop));
		Assert.Equal(APTR.FromPointer(0x3500).Raw, drop.Object);
		Assert.Equal(-12, drop.X);
		Assert.Equal(24, drop.Y);
		Assert.Equal(0x40u, drop.Qualifier);

		var eventAddress = APTR.FromPointer(0x3080);
		Assert.True(MuiAreaDragMessageCodec.WriteEvent(ref platform, eventAddress,
			0x3600, 0x3610, 0x3620, 0x3630, -1, 3, 4));
		Assert.True(MuiAreaDragMessageCodec.TryReadEvent(ref platform,
			eventAddress, out var dragEvent));
		Assert.Equal(0x3600u, dragEvent.Window);
		Assert.Equal(-1, dragEvent.MuiKey);
		Assert.Equal(4u, dragEvent.Flags);
		Assert.True(MuiAreaDragMessageMemoryCodec.TryGetAddress(ref platform,
			eventAddress, MuiAreaDragPacketKind.Event, MuiAreaDragField.Flags,
			out var flagsAddress));
		Assert.Equal(APTR.FromPointer(0x309Cu), flagsAddress);

		var finishAddress = APTR.FromPointer(0x30C0);
		Assert.True(MuiAreaDragMessageMemoryCodec.TryWriteUInt32(ref platform,
			finishAddress, MuiAreaDragPacketKind.Finish,
			MuiAreaDragField.MethodId, MuiAreaDragMessageCodec.DragFinish));
		Assert.True(MuiAreaDragMessageMemoryCodec.TryWriteUInt32(ref platform,
			finishAddress, MuiAreaDragPacketKind.Finish,
			MuiAreaDragField.Object, 0x3700));
		Assert.True(MuiAreaDragMessageMemoryCodec.TryWriteUInt32(ref platform,
			finishAddress, MuiAreaDragPacketKind.Finish,
			MuiAreaDragField.DropFollows, unchecked((uint)-1)));
		Assert.True(MuiAreaDragMessageCodec.TryReadFinish(ref platform,
			finishAddress, out var finish));
		Assert.Equal(-1, finish.DropFollows);

		var queryAddress = APTR.FromPointer(0x30E0);
		Assert.True(MuiAreaDragMessageCodec.WriteQuery(ref platform, queryAddress,
			0x3710));
		Assert.True(MuiAreaDragMessageCodec.TryReadQuery(ref platform,
			queryAddress, out var query));
		Assert.Equal(0x3710u, query.Object);

		var reportAddress = APTR.FromPointer(0x3100);
		Assert.True(MuiAreaDragMessageCodec.WriteReport(ref platform,
			reportAddress, 0x3720, -2, 5, -3, 0x80));
		Assert.True(MuiAreaDragMessageCodec.TryReadReport(ref platform,
			reportAddress, out var report));
		Assert.Equal(-2, report.X);
		Assert.Equal(-3, report.Update);

		var imageAddress = APTR.FromPointer(0x3120);
		Assert.True(MuiAreaDragMessageCodec.WriteCreateDragImage(ref platform,
			imageAddress, 11, -9, 6));
		Assert.True(MuiAreaDragMessageCodec.TryReadCreateDragImage(ref platform,
			imageAddress, out var image));
		Assert.Equal(-9, image.TouchY);
		var deleteAddress = APTR.FromPointer(0x3140);
		Assert.True(MuiAreaDragMessageCodec.WriteDeleteDragImage(ref platform,
			deleteAddress, 0x3730));
		Assert.True(MuiAreaDragMessageCodec.TryReadDeleteDragImage(ref platform,
			deleteAddress, out var deleted));
		Assert.Equal(0x3730u, deleted.DragImage);

		Assert.False(MuiAreaDragMessageMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x20FF0), MuiAreaDragPacketKind.Event,
			MuiAreaDragField.Flags, out _));
		Assert.False(MuiAreaDragMessageMemoryCodec.TryGetAddress(ref platform,
			doDragAddress, MuiAreaDragPacketKind.Method,
			MuiAreaDragField.Object, out _));
		Assert.False(MuiAreaDragMessageMemoryCodec.TryGetAddress(ref platform,
			doDragAddress, MuiAreaDragPacketKind.DoDrag,
			(MuiAreaDragField)255, out _));
		Assert.False(MuiAreaDragMessageMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiAreaDragPacketKind.Begin, MuiAreaDragField.Object,
			out _));
	}
}
