using System.Runtime.CompilerServices;
using Amiga;
using CopperSharp.Compiler;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos.NativeRoot;

/// <summary>Compile-only reachability root for the typed DOS pipe-buffer core.</summary>
public static class DosQueueBufferNativeRoots
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	[M68kExport("copperstart.dos.queue-buffer")]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint QueueBufferRoot()
	{
		var memory = default(CopperSharpRomDosPlatform);
		if (!DosQueueBufferCore.TryInitialize(ref memory,
			APTR.FromPointer(0x0003_0000), 4096, 1024, out var queue)) return 1;
		var write = DosQueueBufferCore.Write(ref memory, ref queue,
			APTR.FromPointer(0x0003_2000), 1536);
		var read = DosQueueBufferCore.Read(ref memory, ref queue,
			APTR.FromPointer(0x0003_4000), 1024);
		var closed = DosQueueBufferCore.CloseWriter(ref memory, ref queue);
		var tail = DosQueueBufferCore.Read(ref memory, ref queue,
			APTR.FromPointer(0x0003_4000), 1024);
		var readerClosed = DosQueueBufferCore.CloseReader(ref memory,
			ref queue, out var abortWriter);
		return write.Status == DosQueueBufferStatus.Ready &&
			read.Status == DosQueueBufferStatus.Ready &&
			closed && tail.Status == DosQueueBufferStatus.Ready &&
			readerClosed && abortWriter ? 0u : 2u;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[M68kExport("copperstart.dos.queue-channel")]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint QueueChannelRoot()
	{
		var memory = default(CopperSharpRomDosPlatform);
		var channel = APTR.FromPointer(0x0004_0000);
		if (!DosQueueChannelCore.TryInitialize(ref memory, channel,
			DosQueueChannelRecord.Size + 13u,
			APTR.FromPointer(0x0004_1000), 12, APTR.Null,
			APTR.FromPointer(0x0004_2000), 4096, 1024) ||
			!DosQueueChannelCore.OpenReader(ref memory, channel) ||
			!DosQueueChannelCore.OpenWriter(ref memory, channel)) return 1;
		var write = DosQueueChannelCore.Write(ref memory, channel,
			APTR.FromPointer(0x0004_3000), 1536);
		var read = DosQueueChannelCore.Read(ref memory, channel,
			APTR.FromPointer(0x0004_4000), 1024);
		var writerClosed = DosQueueChannelCore.CloseWriter(ref memory, channel);
		return write.Status == DosQueueBufferStatus.Ready &&
			read.Status == DosQueueBufferStatus.Ready && writerClosed ? 0u : 2u;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[M68kExport("copperstart.dos.queue-registry")]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint QueueRegistryRoot()
	{
		var memory = default(CopperSharpRomDosPlatform);
		var registry = APTR.FromPointer(0x0005_0000);
		var channel = APTR.FromPointer(0x0005_0100);
		var name = APTR.FromPointer(0x0005_1000);
		var lookup = APTR.FromPointer(0x0005_1100);
		WriteChannelName(ref memory, name, false);
		WriteChannelName(ref memory, lookup, true);
		if (!DosQueueHandlerRegistryCore.TryInitialize(ref memory, registry,
			16, 4, 4) ||
			!DosQueueChannelCore.TryInitialize(ref memory, channel,
				DosQueueChannelRecord.Size + 9, name, 8, APTR.Null,
				APTR.FromPointer(0x0005_2000), 16, 4) ||
			!DosQueueHandlerRegistryCore.TryAdd(ref memory, registry, channel) ||
			DosQueueHandlerRegistryCore.Find(ref memory, registry, lookup, 8,
				out var foundAddress, out _) != DosQueueHandlerLookupStatus.Found ||
			foundAddress != channel ||
			!DosQueueChannelCore.OpenReader(ref memory, channel) ||
			!DosQueueChannelCore.OpenWriter(ref memory, channel)) return 1;

		var source = APTR.FromPointer(0x0005_3000);
		var destination = APTR.FromPointer(0x0005_4000);
		WritePayload(ref memory, source);
		var write = DosQueueChannelCore.Write(ref memory, channel, source, 9);
		var firstRead = DosQueueChannelCore.Read(ref memory, channel,
			destination, 9);
		if (write.Status != DosQueueBufferStatus.Ready ||
			firstRead.Status != DosQueueBufferStatus.Ready ||
			firstRead.BytesTransferred != 8 ||
			!DosQueueChannelCore.CloseWriter(ref memory, channel)) return 2;
		var tailRead = DosQueueChannelCore.Read(ref memory, channel,
			destination, 1);
		if (tailRead.Status != DosQueueBufferStatus.Ready ||
			tailRead.BytesTransferred != 1 ||
			!DosQueueChannelCore.CloseReader(ref memory, channel,
				out var abortWriter) || abortWriter ||
			!DosQueueHandlerRegistryCore.TryRemove(ref memory, registry,
				channel) ||
			!DosQueueHandlerRegistryCore.TryRead(ref memory, registry,
				out var state)) return 3;
		return state.ChannelCount == 0 && state.ChannelHead.IsNull ? 0u : 4u;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[M68kExport("copperstart.dos.queue-filehandle")]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint QueueFileHandleRoot()
	{
		var memory = default(CopperSharpRomDosPlatform);
		var channel = APTR.FromPointer(0x0006_0000);
		var name = APTR.FromPointer(0x0006_1000);
		var storage = APTR.FromPointer(0x0006_2000);
		var handlerPort = APTR.FromPointer(0x0006_3000);
		var readerRecord = APTR.FromPointer(0x0006_4000);
		var writerRecord = APTR.FromPointer(0x0006_4040);
		var readerHandle = APTR.FromPointer(0x0006_5000);
		var writerHandle = APTR.FromPointer(0x0006_5080);
		WriteChannelName(ref memory, name, false);
		if (!DosQueueChannelCore.TryInitialize(ref memory, channel,
			DosQueueChannelRecord.Size + 9, name, 8, APTR.Null, storage, 16, 4))
			return 1;
		var initialHandle = new FileHandle
		{
			Type = handlerPort,
			Position = -1,
			End = -1,
			Function2 = -1,
		};
		DosFileHandleCodec.Write(ref memory, readerHandle, in initialHandle);
		DosFileHandleCodec.Write(ref memory, writerHandle, in initialHandle);
		if (!DosQueueHandlerFileCore.TryOpen(ref memory, readerHandle,
			handlerPort, readerRecord, channel,
			DosQueueChannelEndpoints.Reader) ||
			!DosQueueHandlerFileCore.TryOpen(ref memory, writerHandle,
				handlerPort, writerRecord, channel,
				DosQueueChannelEndpoints.Writer)) return 2;

		var source = APTR.FromPointer(0x0006_6000);
		var destination = APTR.FromPointer(0x0006_7000);
		WritePayload(ref memory, source);
		if (!DosQueueHandlerFileCore.TryWrite(ref memory, writerRecord, source,
			9, out var write) || write.Status != DosQueueBufferStatus.Ready ||
			!DosQueueHandlerFileCore.TryRead(ref memory, readerRecord,
				destination, 9, out var firstRead) ||
			firstRead.BytesTransferred != 8 ||
			!DosQueueHandlerFileCore.TryClose(ref memory, writerRecord,
				out var writerAbort) || writerAbort) return 3;
		if (!DosQueueHandlerFileCore.TryRead(ref memory, readerRecord,
			destination, 1, out var tailRead) ||
			tailRead.BytesTransferred != 1 ||
			!DosQueueHandlerFileCore.TryClose(ref memory, readerRecord,
				out var readerAbort) || readerAbort) return 4;
		return 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[M68kExport("copperstart.dos.queue-io-packet")]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint QueueIoPacketRoot()
	{
		var memory = default(CopperSharpRomDosPlatform);
		var channel = APTR.FromPointer(0x0007_0000);
		var name = APTR.FromPointer(0x0007_1000);
		var handlerPort = APTR.FromPointer(0x0007_2000);
		var readerRecord = APTR.FromPointer(0x0007_3000);
		var writerRecord = APTR.FromPointer(0x0007_3040);
		var readerHandle = APTR.FromPointer(0x0007_4000);
		var writerHandle = APTR.FromPointer(0x0007_4080);
		var storage = APTR.FromPointer(0x0007_5000);
		WriteChannelName(ref memory, name, false);
		if (!DosQueueChannelCore.TryInitialize(ref memory, channel,
			DosQueueChannelRecord.Size + 9, name, 8, APTR.Null, storage, 16, 4))
			return 1;
		var initialHandle = new FileHandle
		{
			Type = handlerPort,
			Position = -1,
			End = -1,
			Function2 = -1,
		};
		DosFileHandleCodec.Write(ref memory, readerHandle, in initialHandle);
		DosFileHandleCodec.Write(ref memory, writerHandle, in initialHandle);
		if (!DosQueueHandlerFileCore.TryOpen(ref memory, readerHandle,
			handlerPort, readerRecord, channel,
			DosQueueChannelEndpoints.Reader) ||
			!DosQueueHandlerFileCore.TryOpen(ref memory, writerHandle,
				handlerPort, writerRecord, channel,
				DosQueueChannelEndpoints.Writer)) return 2;

		var source = APTR.FromPointer(0x0007_6000);
		var destination = APTR.FromPointer(0x0007_7000);
		var standardPacket = APTR.FromPointer(0x0007_8000);
		var packetAddress = DosStandardPacketCodec.PacketAddress(standardPacket);
		var replyPort = APTR.FromPointer(0x0007_9000);
		ExecMessageCodec.Write(ref memory, standardPacket, new Message
		{
			Node = new Node { Name = STRPTR.FromPointer(packetAddress.Raw) },
			ReplyPort = replyPort,
			Length = unchecked((ushort)StandardPacket.Size),
		});
		DosPacketCodec.Write(ref memory, packetAddress, new DosPacket
		{
			Link = standardPacket,
			Port = replyPort,
			Type = (int)DosPacketAction.Read,
			Argument1 = unchecked((int)readerRecord.Raw),
			Argument2 = unchecked((int)destination.Raw),
			Argument3 = 4,
		});
		if (!DosQueueHandlerIoCore.TryCreateRequest(ref memory, standardPacket,
			out var request) || request.Action != DosPacketAction.Read ||
			request.FileRecord != readerRecord ||
			DosQueueHandlerIoCore.Service(ref memory, ref request) !=
				DosQueueHandlerIoDisposition.Pending) return 3;
		var pendingRecord = APTR.FromPointer(0x0007_A000);
		if (!DosQueueHandlerIoCore.TryAttachPending(ref memory, pendingRecord,
			in request)) return 4;
		WritePayload(ref memory, source);
		if (!DosQueueHandlerFileCore.TryWrite(ref memory, writerRecord, source,
			4, out var write) || write.Status != DosQueueBufferStatus.Ready)
			return 5;
		if (DosQueueHandlerIoCore.ServicePending(ref memory, pendingRecord,
			out request) != DosQueueHandlerIoDisposition.ReadyToReply ||
			request.BytesTransferred != 4 ||
			!DosQueueHandlerIoCore.TryDetachPending(ref memory, pendingRecord,
				out var detached) || detached.BytesTransferred != 4) return 6;
		var result = DosPacketCodec.Read(ref memory, packetAddress);
		return result.Result1 == 4 && result.Result2 == (int)DOS.Error.None
			? 0u : 7u;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[M68kExport("copperstart.dos.queue-packet-dispatch")]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint QueuePacketDispatchRoot()
	{
		var memory = default(CopperSharpRomDosPlatform);
		var standardPacket = APTR.FromPointer(0x0008_0000);
		var packetAddress = DosStandardPacketCodec.PacketAddress(standardPacket);
		var replyPort = APTR.FromPointer(0x0008_1000);
		ExecMessageCodec.Write(ref memory, standardPacket, new Message
		{
			Node = new Node { Name = STRPTR.FromPointer(packetAddress.Raw) },
			ReplyPort = replyPort,
			Length = unchecked((ushort)StandardPacket.Size),
		});
		var packet = DosPacketCodec.Read(ref memory, packetAddress);
		packet.Link = standardPacket;
		packet.Port = replyPort;
		packet.Argument1 = 0x0008_2000;
		packet.Argument2 = 0x0008_3000;
		packet.Argument3 = 16;
		DosPacketCodec.Write(ref memory, packetAddress, packet);
		var result = DosQueueHandlerPacketCore.Process(ref memory,
			APTR.FromPointer(0x0008_4000), APTR.FromPointer(0x0008_5000),
			standardPacket);
		return (uint)result;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[M68kExport("copperstart.dos.queue-port-wait")]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint QueuePortWaitRoot()
	{
		var platform = new CopperSharpNativeDosPlatform(0);
		return platform.WaitDosMessagePort(APTR.FromPointer(0x0008_5000))
			? 1u : 0u;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[M68kExport("copperstart.dos.queue-task-loop")]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint QueueTaskLoopRoot()
	{
		var platform = new CopperSharpNativeDosPlatform(0);
		return DosQueueHandlerTaskCore.Run(ref platform,
			APTR.FromPointer(0x0008_4000));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[M68kExport("copperstart.dos.queue-task-stop")]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint QueueTaskStopRoot()
	{
		var platform = new CopperSharpNativeDosPlatform(0);
		return DosQueueHandlerTaskCore.RequestStop(ref platform,
			APTR.FromPointer(0x0008_4000)) ? 1u : 0u;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[M68kExport("copperstart.dos.queue-process-start")]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint QueueProcessStartRoot()
	{
		var platform = new CopperSharpNativeDosPlatform(0);
		var taskRecord = DosQueueHandlerProcessCore.Prepare(ref platform,
			APTR.FromPointer(0x0008_2000), 8, 4096, 4);
		if (taskRecord.IsNull ||
			!DosQueueHandlerProcessCore.TryGetRegistry(ref platform,
				taskRecord, out _)) return 0;
		return DosQueueHandlerProcessCore.Start(ref platform, taskRecord,
			DosAbiProfile.Unified,
			APTR.ExportAddress("copperos.shell.queue-handler-task"),
			8192, 0) ? 1u : 0u;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[M68kExport("copperstart.dos.queue-process-retire")]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint QueueProcessRetireRoot()
	{
		var platform = new CopperSharpNativeDosPlatform(0);
		return DosQueueHandlerProcessCore.TryRetire(ref platform,
			APTR.FromPointer(0x0008_4000)) ? 1u : 0u;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[M68kExport("copperstart.dos.queue-channel-lifecycle")]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint QueueChannelLifecycleRoot()
	{
		var platform = new CopperSharpNativeDosPlatform(0);
		var taskRecord = DosQueueHandlerProcessCore.Prepare(ref platform,
			APTR.FromPointer(0x0008_2000), 4, 16, 4);
		if (taskRecord.IsNull ||
			!DosQueueHandlerProcessCore.TryGetRegistry(ref platform, taskRecord,
				out var registry) ||
			!DosQueueHandlerChannelOwnerCore.TryCreate(ref platform,
				taskRecord, APTR.FromPointer(0x0008_3000), 8,
				out var channel) ||
			!DosQueueChannelCore.OpenReader(ref platform, channel) ||
			!DosQueueChannelCore.OpenWriter(ref platform, channel) ||
			!DosQueueChannelCore.CloseReader(ref platform, channel, out _) ||
			!DosQueueChannelCore.CloseWriter(ref platform, channel)) return 0;
		return DosQueueHandlerPacketCore.PumpPendingOnce(ref platform,
			registry) ? 1u : 0u;
	}

	private static void WriteChannelName(ref CopperSharpRomDosPlatform memory,
		APTR address, bool uppercase)
	{
		memory.WriteUInt8(address, 0, (byte)'P');
		memory.WriteUInt8(address, 1, uppercase ? (byte)'I' : (byte)'i');
		memory.WriteUInt8(address, 2, uppercase ? (byte)'P' : (byte)'p');
		memory.WriteUInt8(address, 3, uppercase ? (byte)'E' : (byte)'e');
		memory.WriteUInt8(address, 4, (byte)'-');
		memory.WriteUInt8(address, 5, uppercase ? (byte)'O' : (byte)'O');
		memory.WriteUInt8(address, 6, uppercase ? (byte)'N' : (byte)'n');
		memory.WriteUInt8(address, 7, uppercase ? (byte)'E' : (byte)'e');
	}

	private static void WritePayload(ref CopperSharpRomDosPlatform memory,
		APTR address)
	{
		memory.WriteUInt8(address, 0, (byte)'p');
		memory.WriteUInt8(address, 1, (byte)'i');
		memory.WriteUInt8(address, 2, (byte)'p');
		memory.WriteUInt8(address, 3, (byte)'e');
		memory.WriteUInt8(address, 4, (byte)'-');
		memory.WriteUInt8(address, 5, (byte)'d');
		memory.WriteUInt8(address, 6, (byte)'a');
		memory.WriteUInt8(address, 7, (byte)'t');
		memory.WriteUInt8(address, 8, (byte)'a');
	}
}
