using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiProcessSpecialistTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint Base = 0x1000;
	private const int Size = 0x40000;
	private const uint FirstAllocation = 0x10000;

	// Fixed class-id buffers.
	private static readonly APTR ProcessName = APTR.FromPointer(0x1100);
	private static readonly APTR SlaveName = APTR.FromPointer(0x1120);
	private static readonly APTR AppName = APTR.FromPointer(0x1140);
	// Fixed string / scratch buffers.
	private static readonly APTR NameA = APTR.FromPointer(0x1200);
	private static readonly APTR NameB = APTR.FromPointer(0x1240);
	private static readonly APTR Storage = APTR.FromPointer(0x1300);
	private static readonly APTR Packet = APTR.FromPointer(0x1400);
	private static readonly APTR Message = APTR.FromPointer(0x1500);

	private static MuiHeadlessTestPlatform NewPlatform()
	{
		var p = new MuiHeadlessTestPlatform(Base, Size, FirstAllocation, State);
		Assert.True(MuiHeadlessObjectCore.Initialize(ref p, State));
		p.WriteCString(ProcessName, "Process.mui");
		p.WriteCString(SlaveName, "Slave.mui");
		p.WriteCString(AppName, "Application.mui");
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, State,
			ProcessName, APTR.Null, 0, APTR.FromPointer(20)).IsNotNull);
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, State,
			SlaveName, APTR.Null, 0, APTR.FromPointer(21)).IsNotNull);
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, State,
			AppName, APTR.Null, 0, APTR.FromPointer(22)).IsNotNull);
		return p;
	}

	private static APTR Create(ref MuiHeadlessTestPlatform p, APTR className,
		MuiProcessSpecialistClass expected)
	{
		var classRecord = MuiHeadlessObjectCore.FindClassByName(ref p, State,
			className);
		Assert.True(classRecord.IsNotNull);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref p, State, classRecord,
			APTR.Null);
		Assert.True(obj.IsNotNull);
		Assert.True(MuiProcessSpecialistCore.Attach(ref p, State, obj, expected)
			.IsNotNull);
		return obj;
	}

	private static APTR RawObject(ref MuiHeadlessTestPlatform p, APTR className)
	{
		var classRecord = MuiHeadlessObjectCore.FindClassByName(ref p, State,
			className);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref p, State, classRecord,
			APTR.Null);
		Assert.True(obj.IsNotNull);
		return obj;
	}

	private static APTR Process(ref MuiHeadlessTestPlatform p) =>
		Create(ref p, ProcessName, MuiProcessSpecialistClass.Process);
	private static APTR Slave(ref MuiHeadlessTestPlatform p) =>
		Create(ref p, SlaveName, MuiProcessSpecialistClass.Slave);

	// ---- Classification and inheritance --------------------------------------

	[Fact]
	public void ExactClassNamesAreClassified()
	{
		var p = NewPlatform();
		Assert.Equal(MuiProcessSpecialistClass.Process,
			MuiProcessSpecialistCore.ClassifyName(ref p, ProcessName));
		Assert.Equal(MuiProcessSpecialistClass.Slave,
			MuiProcessSpecialistCore.ClassifyName(ref p, SlaveName));
	}

	[Fact]
	public void MiscasedAndUnknownNamesAreRejected()
	{
		var p = NewPlatform();
		p.WriteCString(Storage, "process.mui");
		Assert.Equal(MuiProcessSpecialistClass.None,
			MuiProcessSpecialistCore.ClassifyName(ref p, Storage));
		p.WriteCString(Storage, "Processor.mui");
		Assert.Equal(MuiProcessSpecialistClass.None,
			MuiProcessSpecialistCore.ClassifyName(ref p, Storage));
		p.WriteCString(Storage, "Slaved.mui");
		Assert.Equal(MuiProcessSpecialistClass.None,
			MuiProcessSpecialistCore.ClassifyName(ref p, Storage));
		Assert.Equal(MuiProcessSpecialistClass.None,
			MuiProcessSpecialistCore.ClassifyName(ref p, APTR.Null));
	}

	[Fact]
	public void BothClassesDescendFromSemaphore()
	{
		Assert.Equal(MuiProcessSpecialistClass.None,
			MuiProcessSpecialistCore.Superclass(MuiProcessSpecialistClass.Process));
		Assert.Equal(MuiProcessSpecialistClass.None,
			MuiProcessSpecialistCore.Superclass(MuiProcessSpecialistClass.Slave));
	}

	[Fact]
	public void AttachByObjectClassifiesRegisteredName()
	{
		var p = NewPlatform();
		var obj = RawObject(ref p, ProcessName);
		Assert.True(MuiProcessSpecialistCore.AttachByObject(ref p, State, obj)
			.IsNotNull);
		Assert.Equal(MuiProcessSpecialistClass.Process,
			MuiProcessSpecialistCore.Classify(ref p, State, obj));
	}

	[Fact]
	public void DuplicateAttachIsRejected()
	{
		var p = NewPlatform();
		var proc = Process(ref p);
		Assert.True(MuiProcessSpecialistCore.Attach(ref p, State, proc,
			MuiProcessSpecialistClass.Process).IsNull);
	}

	[Fact]
	public void MalformedProcessSidecarFailsClosedBeforeSchedulerCalls()
	{
		var p = NewPlatform();
		var proc = Process(ref p);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref p, State, proc,
			MuiProcessSpecialistLayout.SidecarAttribute, out var raw));
		var sidecar = APTR.FromPointer(raw);
		Assert.True(MuiProcessSpecialistCodec.TryReadStructural(ref p, sidecar,
			out var record));
		// The structural codec can represent the guest record, but the live
		// admission boundary rejects flags that are not owned by Process.mui.
		record.Flags = 0x80000000u;
		Assert.True(MuiProcessSpecialistCodec.Write(ref p, sidecar, record));
		Assert.False(MuiProcessSpecialistCore.Valid(ref p, State, proc));
		Assert.False(MuiProcessSpecialistCore.Launch(ref p, State, proc));
		Assert.Equal(0u, p.ProcessLaunchCount);

		var slave = Slave(ref p);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref p, State, slave,
			MuiProcessSpecialistLayout.SidecarAttribute, out raw));
		sidecar = APTR.FromPointer(raw);
		Assert.True(MuiProcessSpecialistCodec.TryReadStructural(ref p, sidecar,
			out record));
		record.State = (uint)MuiProcessState.Running;
		Assert.True(MuiProcessSpecialistCodec.Write(ref p, sidecar, record));
		Assert.False(MuiProcessSpecialistCore.Valid(ref p, State, slave));
		Assert.False(MuiProcessSpecialistCore.Setup(ref p, State, slave));
	}

	// ---- Creation defaults ---------------------------------------------------

	[Fact]
	public void ProcessStartsPendingWithDefaultStack()
	{
		var p = NewPlatform();
		var proc = Process(ref p);
		Assert.Equal(MuiProcessState.Pending,
			MuiProcessSpecialistCore.ProcessStateOf(ref p, State, proc));
		Assert.True(MuiProcessSpecialistCore.GetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_StackSize, out var stack) &&
			stack == 8192);
		Assert.True(MuiProcessSpecialistCore.GetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_Priority, out var prio) && prio == 0);
	}

	// ---- Attribute policy ----------------------------------------------------

	[Fact]
	public void NameIsKeptAsOwnedCopyAndSurvivesCallerMutation()
	{
		var p = NewPlatform();
		var proc = Process(ref p);
		p.WriteCString(NameA, "Worker");
		Assert.True(MuiProcessSpecialistCore.SetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_Name, NameA.Raw, true, false, out _));
		Assert.True(MuiProcessSpecialistCore.GetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_Name, out var owned));
		Assert.NotEqual(NameA.Raw, owned);          // must be a class-owned copy
		Assert.Equal((byte)'W', p.ReadUInt8(APTR.FromPointer(owned), 0));
		p.WriteUInt8(NameA, 0, (byte)'X');           // mutate caller buffer
		Assert.Equal((byte)'W', p.ReadUInt8(APTR.FromPointer(owned), 0));
	}

	[Fact]
	public void NameIsInitOnly()
	{
		var p = NewPlatform();
		var proc = Process(ref p);
		p.WriteCString(NameA, "Worker");
		Assert.False(MuiProcessSpecialistCore.SetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_Name, NameA.Raw, false, false, out _));
	}

	[Fact]
	public void PriorityIsBoundedAndRuntimeSettableWithNotification()
	{
		var p = NewPlatform();
		var proc = Process(ref p);
		// Out of range rejected (both directions of the signed byte range).
		Assert.False(MuiProcessSpecialistCore.SetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_Priority, 200, true, false, out _));
		Assert.False(MuiProcessSpecialistCore.SetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_Priority, unchecked((uint)-200), true,
			false, out _));
		// Legal boundaries.
		Assert.True(MuiProcessSpecialistCore.SetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_Priority, 127, true, false, out _));
		Assert.True(MuiProcessSpecialistCore.SetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_Priority, unchecked((uint)-128), true,
			false, out _));
		// Runtime change notifies.
		var before = MuiProcessSpecialistCore.NotificationCount(ref p, State, proc);
		Assert.True(MuiProcessSpecialistCore.SetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_Priority, 5, false, true, out var changed));
		Assert.True(changed);
		Assert.Equal(before + 1,
			MuiProcessSpecialistCore.NotificationCount(ref p, State, proc));
		Assert.Equal(MuiProcessAttributes.Process_Priority,
			MuiProcessSpecialistCore.LastNotifiedAttribute(ref p, State, proc));
	}

	[Fact]
	public void StackSizeIsBoundedAndInitOnly()
	{
		var p = NewPlatform();
		var proc = Process(ref p);
		Assert.False(MuiProcessSpecialistCore.SetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_StackSize, 512, true, false, out _));
		Assert.False(MuiProcessSpecialistCore.SetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_StackSize, 0x00200000, true, false, out _));
		Assert.True(MuiProcessSpecialistCore.SetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_StackSize, 16384, true, false, out _));
		Assert.False(MuiProcessSpecialistCore.SetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_StackSize, 16384, false, false, out _));
	}

	[Fact]
	public void TaskAttributeIsNotCallerSettable()
	{
		var p = NewPlatform();
		var proc = Process(ref p);
		Assert.False(MuiProcessSpecialistCore.SetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_Task, 0x999, true, false, out _));
	}

	// ---- Legal state machine -------------------------------------------------

	[Fact]
	public void LaunchMovesPendingToRunningAndPublishesTask()
	{
		var p = NewPlatform();
		var proc = Process(ref p);
		Assert.True(MuiProcessSpecialistCore.Launch(ref p, State, proc));
		Assert.Equal(MuiProcessState.Running,
			MuiProcessSpecialistCore.ProcessStateOf(ref p, State, proc));
		var token = MuiProcessSpecialistCore.TaskToken(ref p, State, proc);
		Assert.NotEqual(0u, token);
		Assert.True(MuiProcessSpecialistCore.GetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_Task, out var task) && task == token);
		Assert.Equal(1u, p.ProcessLaunchCount);
	}

	[Fact]
	public void LaunchForwardsNameOwnedCopyPriorityAndStackToScheduler()
	{
		var p = NewPlatform();
		var proc = Process(ref p);
		p.WriteCString(NameA, "Job");
		MuiProcessSpecialistCore.SetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_Name, NameA.Raw, true, false, out _);
		MuiProcessSpecialistCore.SetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_Priority, 7, true, false, out _);
		MuiProcessSpecialistCore.SetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_SourceClass, 0xAAAA, true, false, out _);
		MuiProcessSpecialistCore.SetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_SourceObject, 0xBBBB, true, false, out _);
		Assert.True(MuiProcessSpecialistCore.Launch(ref p, State, proc));
		Assert.NotEqual(NameA.Raw, p.LastLaunchName.Raw);   // owned copy, not caller
		Assert.Equal(7, p.LastLaunchPriority);
		Assert.Equal(8192u, p.LastLaunchStackSize);
		Assert.Equal(0xAAAAu, p.LastLaunchSourceClass.Raw);
		Assert.Equal(0xBBBBu, p.LastLaunchSourceObject.Raw);
	}

	[Fact]
	public void DuplicateLaunchIsRejected()
	{
		var p = NewPlatform();
		var proc = Process(ref p);
		Assert.True(MuiProcessSpecialistCore.Launch(ref p, State, proc));
		Assert.False(MuiProcessSpecialistCore.Launch(ref p, State, proc));
		Assert.Equal(1u, p.ProcessLaunchCount);   // scheduler entered exactly once
	}

	[Fact]
	public void LaunchFailureIsAtomicAndEntersFailed()
	{
		var p = NewPlatform();
		p.ProcessLaunchFailure = true;
		var proc = Process(ref p);
		Assert.False(MuiProcessSpecialistCore.Launch(ref p, State, proc));
		Assert.Equal(MuiProcessState.Failed,
			MuiProcessSpecialistCore.ProcessStateOf(ref p, State, proc));
		Assert.Equal(0u, MuiProcessSpecialistCore.TaskToken(ref p, State, proc));
		Assert.True(MuiProcessSpecialistCore.GetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_Task, out var task) && task == 0);
		// A failed launch is terminal, not a retryable Pending.
		Assert.False(MuiProcessSpecialistCore.Launch(ref p, State, proc));
	}

	[Fact]
	public void KillMovesRunningToKilled()
	{
		var p = NewPlatform();
		var proc = Process(ref p);
		Assert.True(MuiProcessSpecialistCore.Launch(ref p, State, proc));
		var token = MuiProcessSpecialistCore.TaskToken(ref p, State, proc);
		Assert.True(MuiProcessSpecialistCore.Kill(ref p, State, proc));
		Assert.Equal(MuiProcessState.Killed,
			MuiProcessSpecialistCore.ProcessStateOf(ref p, State, proc));
		Assert.Equal(token, p.LastKilledToken);
		Assert.Equal(0u, MuiProcessSpecialistCore.TaskToken(ref p, State, proc));
	}

	[Fact]
	public void KillOfNonRunningProcessIsRejected()
	{
		var p = NewPlatform();
		var proc = Process(ref p);
		Assert.False(MuiProcessSpecialistCore.Kill(ref p, State, proc)); // Pending
		Assert.Equal(0u, p.ProcessKillCount);
	}

	[Fact]
	public void ProcessPollAdvancesToCompleted()
	{
		var p = NewPlatform();
		var proc = Process(ref p);
		MuiProcessSpecialistCore.Launch(ref p, State, proc);
		p.ProcessPollStatus = MuiProcessSchedulerStatus.Completed;
		Assert.Equal((uint)MuiProcessState.Completed,
			MuiProcessSpecialistCore.Process(ref p, State, proc));
		Assert.Equal(MuiProcessState.Completed,
			MuiProcessSpecialistCore.ProcessStateOf(ref p, State, proc));
	}

	[Fact]
	public void ProcessPollAdvancesToFailed()
	{
		var p = NewPlatform();
		var proc = Process(ref p);
		MuiProcessSpecialistCore.Launch(ref p, State, proc);
		p.ProcessPollStatus = MuiProcessSchedulerStatus.Failed;
		Assert.Equal((uint)MuiProcessState.Failed,
			MuiProcessSpecialistCore.Process(ref p, State, proc));
	}

	[Fact]
	public void ProcessPollWithoutExitStaysRunningAndInventsNoSuccess()
	{
		var p = NewPlatform();
		var proc = Process(ref p);
		MuiProcessSpecialistCore.Launch(ref p, State, proc);
		p.ProcessPollStatus = MuiProcessSchedulerStatus.Running;
		Assert.Equal((uint)MuiProcessState.Running,
			MuiProcessSpecialistCore.Process(ref p, State, proc));
		Assert.Equal(MuiProcessState.Running,
			MuiProcessSpecialistCore.ProcessStateOf(ref p, State, proc));
	}

	[Fact]
	public void SignalRequiresRunningState()
	{
		var p = NewPlatform();
		var proc = Process(ref p);
		Assert.False(MuiProcessSpecialistCore.Signal(ref p, State, proc, 0x10));
		MuiProcessSpecialistCore.Launch(ref p, State, proc);
		Assert.True(MuiProcessSpecialistCore.Signal(ref p, State, proc, 0x10));
		Assert.Equal(0x10u, p.LastSignaledProcessMask);
	}

	[Fact]
	public void AutoLaunchLatchDrivesLaunch()
	{
		var p = NewPlatform();
		var proc = Process(ref p);
		Assert.True(MuiProcessSpecialistCore.SetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_AutoLaunch, 1, true, false, out _));
		Assert.True(MuiProcessSpecialistCore.AutoLaunchFlag(ref p, State, proc));
		Assert.True(MuiProcessSpecialistCore.AutoLaunchIfRequested(ref p, State,
			proc));
		Assert.Equal(MuiProcessState.Running,
			MuiProcessSpecialistCore.ProcessStateOf(ref p, State, proc));
	}

	[Fact]
	public void DisposeKillsRunningProcessAndIsIdempotent()
	{
		var p = NewPlatform();
		var proc = Process(ref p);
		p.WriteCString(NameA, "Worker");
		MuiProcessSpecialistCore.SetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_Name, NameA.Raw, true, false, out _);
		MuiProcessSpecialistCore.Launch(ref p, State, proc);
		var token = MuiProcessSpecialistCore.TaskToken(ref p, State, proc);
		Assert.True(MuiProcessSpecialistLifecycle.Dispose(ref p, State, proc));
		Assert.Equal(token, p.LastKilledToken);         // running task not orphaned
		Assert.False(MuiProcessSpecialistCore.Valid(ref p, State, proc));
		Assert.False(MuiProcessSpecialistLifecycle.Dispose(ref p, State, proc));
	}

	// ---- Slave: ownership, setup/cleanup -------------------------------------

	private static (APTR slave, APTR app, APTR target) NewSlave(
		ref MuiHeadlessTestPlatform p)
	{
		var slave = Slave(ref p);
		var app = RawObject(ref p, AppName);
		var target = RawObject(ref p, AppName);
		MuiProcessSpecialistCore.SetAttribute(ref p, State, slave,
			MuiProcessAttributes.Slave_Application, app.Raw, true, false, out _);
		MuiProcessSpecialistCore.SetAttribute(ref p, State, slave,
			MuiProcessAttributes.Slave_Object, target.Raw, true, false, out _);
		MuiProcessSpecialistCore.SetAttribute(ref p, State, slave,
			MuiProcessAttributes.Slave_Class, 0xCCCC, true, false, out _);
		return (slave, app, target);
	}

	[Fact]
	public void SlaveOwnershipAttributesAreInitGettable()
	{
		var p = NewPlatform();
		var (slave, app, target) = NewSlave(ref p);
		Assert.True(MuiProcessSpecialistCore.GetAttribute(ref p, State, slave,
			MuiProcessAttributes.Slave_Application, out var a) && a == app.Raw);
		Assert.True(MuiProcessSpecialistCore.GetAttribute(ref p, State, slave,
			MuiProcessAttributes.Slave_Object, out var o) && o == target.Raw);
		Assert.True(MuiProcessSpecialistCore.GetAttribute(ref p, State, slave,
			MuiProcessAttributes.Slave_Class, out var c) && c == 0xCCCC);
		// [I.G]: runtime set is rejected.
		Assert.False(MuiProcessSpecialistCore.SetAttribute(ref p, State, slave,
			MuiProcessAttributes.Slave_Application, app.Raw, false, false, out _));
	}

	[Fact]
	public void SetupRequiresLiveApplicationAndBalances()
	{
		var p = NewPlatform();
		var (slave, app, _) = NewSlave(ref p);
		Assert.True(MuiProcessSpecialistCore.Setup(ref p, State, slave));
		Assert.True(MuiProcessSpecialistCore.SlaveIsSetup(ref p, State, slave));
		// Double setup rejected (balance).
		Assert.False(MuiProcessSpecialistCore.Setup(ref p, State, slave));
		// Cleanup balances; a second cleanup underflows and is rejected.
		Assert.True(MuiProcessSpecialistCore.Cleanup(ref p, State, slave));
		Assert.False(MuiProcessSpecialistCore.Cleanup(ref p, State, slave));
	}

	[Fact]
	public void SetupFailsWhenApplicationNotAlive()
	{
		var p = NewPlatform();
		var (slave, app, _) = NewSlave(ref p);
		// Kill the application object: the slave must refuse setup.
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref p, State, app));
		Assert.False(MuiProcessSpecialistCore.Setup(ref p, State, slave));
	}

	// ---- Slave: dispatch semantics -------------------------------------------

	private static void WritePacket(ref MuiHeadlessTestPlatform p, uint argCount,
		uint methodId, params uint[] args)
	{
		p.WriteUInt32(Packet, 0, argCount);
		p.WriteUInt32(Packet, 4, methodId);
		for (var i = 0; i < args.Length; i++)
			p.WriteUInt32(Packet, 8 + i * 4, args[i]);
	}

	[Fact]
	public void DispatchDeliversExactDoMethodUnderLockAndBalances()
	{
		var p = NewPlatform();
		var (slave, _, target) = NewSlave(ref p);
		Assert.True(MuiProcessSpecialistCore.Setup(ref p, State, slave));
		WritePacket(ref p, 2, 0x8042AAAA, 0x1111, 0x2222);
		var before = p.DispatchCount;
		Assert.True(MuiProcessSpecialistCore.Dispatch(ref p, State, slave, Packet,
			out _));
		Assert.Equal(before + 1, p.DispatchCount);
		Assert.Equal(target.Raw, p.LastDispatchObject.Raw);
		Assert.Equal(0x8042AAAAu, p.LastDispatchMethod);
		Assert.Equal(0x1111u, p.LastDispatchArgument);
		// Semaphore balance: the target lock was released, so it can be obtained.
		Assert.True(MuiSemaphoreCore.Obtain(ref p, State, target));
	}

	[Fact]
	public void DispatchAcceptsSixteenArgsAndRejectsSeventeen()
	{
		var p = NewPlatform();
		var (slave, _, _) = NewSlave(ref p);
		MuiProcessSpecialistCore.Setup(ref p, State, slave);
		var sixteen = new uint[16];
		for (var i = 0u; i < 16; i++) sixteen[i] = i + 1;
		WritePacket(ref p, 16, 0x8042BBBB, sixteen);
		Assert.True(MuiProcessSpecialistCore.Dispatch(ref p, State, slave, Packet,
			out _));
		WritePacket(ref p, 17, 0x8042BBBB, sixteen);   // 17th arg unread
		Assert.False(MuiProcessSpecialistCore.Dispatch(ref p, State, slave, Packet,
			out _));
	}

	[Fact]
	public void DispatchRejectsMalformedPackets()
	{
		var p = NewPlatform();
		var (slave, _, _) = NewSlave(ref p);
		MuiProcessSpecialistCore.Setup(ref p, State, slave);
		// Null packet.
		Assert.False(MuiProcessSpecialistCore.Dispatch(ref p, State, slave,
			APTR.Null, out _));
		// Zero method id.
		WritePacket(ref p, 0, 0);
		Assert.False(MuiProcessSpecialistCore.Dispatch(ref p, State, slave, Packet,
			out _));
	}

	[Fact]
	public void ProcessDispatchPacketUsesNamedHeaderAndArgumentCodec()
	{
		var p = NewPlatform();
		WritePacket(ref p, 2, 0x8042AAAA, 0x1111, 0x2222);
		Assert.True(MuiProcessDispatchPacketCodec.TryReadHeader(ref p, Packet,
			out var header));
		Assert.Equal(2u, header.ArgumentCount);
		Assert.Equal(0x8042AAAAu, header.MethodId);
		Assert.True(MuiProcessDispatchPacketCodec.TryReadMethodIdValue(ref p,
			Packet, out var methodId));
		Assert.Equal(header.MethodId, methodId);
		Assert.True(MuiProcessDispatchPacketCodec.TryReadArgument(ref p, Packet,
			header, 0, out var first));
		Assert.True(MuiProcessDispatchPacketCodec.TryReadArgument(ref p, Packet,
			header, 1, out var second));
		Assert.Equal(0x1111u, first);
		Assert.Equal(0x2222u, second);
		Assert.False(MuiProcessDispatchPacketCodec.TryReadArgument(ref p, Packet,
			header, 2, out _));

		p.WriteUInt32(Packet, 0,
			MuiProcessSpecialistLayout.MaximumDispatchArgs + 1);
		Assert.False(MuiProcessDispatchPacketCodec.TryReadHeader(ref p, Packet,
			out _));
	}

	[Fact]
	public void ProcessMethodMessageHeaderCodecUsesNamedRecord()
	{
		var p = NewPlatform();
		var header = default(MuiProcessMethodMessageHeader);
		header.MethodId = 0x8042BBBBu;
		Assert.True(MuiProcessMethodMessageHeaderCodec.Write(ref p, Message,
			header));
		Assert.True(MuiProcessMethodMessageHeaderCodec.TryRead(ref p, Message,
			out var roundTrip));
		Assert.Equal(header.MethodId, roundTrip.MethodId);
		Assert.False(MuiProcessMethodMessageHeaderCodec.TryRead(ref p,
			APTR.FromPointer(0x40FFEu), out _));
	}

	[Fact]
	public void ProcessMethodMessageHeaderUsesSharedUlongStorageBoundary()
	{
		var p = NewPlatform();
		var address = APTR.FromPointer(0x2FFFC);
		const uint methodId = 0xC2468ACEu;

		Assert.True(MuiProcessMethodMessageHeaderStructCodec.TryWriteMethodIdValue(
			ref p, address, methodId));
		Assert.True(MuiProcessMethodMessageHeaderStructCodec.TryReadMethodIdValue(
			ref p, address, out var decoded));
		Assert.Equal(methodId, decoded);
		Assert.True(MuiProcessMethodMessageHeaderCodec.TryRead(ref p, address,
			out var header));
		Assert.Equal(methodId, header.MethodId);

		Assert.True(MuiProcessMethodMessageHeaderStructCodec.TryWriteMethodIdValue(
			ref p, address, 0xF1020304u));
		Assert.True(MuiProcessMethodMessageHeaderCodec.TryRead(ref p, address,
			out header));
		Assert.Equal(0xF1020304u, header.MethodId);

		var nearEnd = APTR.FromPointer(0x40FFC);
		Assert.True(MuiProcessMethodMessageHeaderStructCodec.TryWriteMethodIdValue(
			ref p, nearEnd, methodId));
		Assert.True(MuiProcessMethodMessageHeaderStructCodec.TryReadMethodIdValue(
			ref p, nearEnd, out decoded));
		Assert.Equal(methodId, decoded);
		var truncated = APTR.FromPointer(0x40FFD);
		Assert.False(MuiProcessMethodMessageHeaderStructCodec.TryReadMethodIdValue(
			ref p, truncated, out _));
		Assert.False(MuiProcessMethodMessageHeaderStructCodec.TryWriteMethodIdValue(
			ref p, truncated, methodId));
		Assert.False(MuiProcessMethodMessageHeaderStructCodec.TryReadMethodIdValue(
			ref p, APTR.Null, out _));
	}

	[Fact]
	public void ProcessDispatchArgumentSlotCodecUsesNamedValue()
	{
		var p = NewPlatform();
		WritePacket(ref p, 2, 0x8042AAAA, 0x1111, 0x2222);
		var firstSlot = APTR.FromPointer(Packet.Raw +
			MuiProcessDispatchPacketHeader.Size);
		Assert.True(MuiProcessDispatchArgumentSlotCodec.TryRead(ref p, firstSlot,
			out var first));
		Assert.Equal(0x1111u, first.Value);

		var secondSlot = APTR.FromPointer(firstSlot.Raw +
			MuiProcessDispatchArgumentSlot.Size);
		Assert.True(MuiProcessDispatchArgumentSlotCodec.Write(ref p, secondSlot,
			new MuiProcessDispatchArgumentSlot { Value = 0xABCD }));
		Assert.True(MuiProcessDispatchArgumentSlotCodec.TryRead(ref p, secondSlot,
			out var second));
		Assert.Equal(0xABCDu, second.Value);
	}

	[Fact]
	public void ProcessDispatchArgumentSlotSequentialCodecPreservesHighBitAndBounds()
	{
		var p = NewPlatform();
		var address = APTR.FromPointer(0x1700);
		p.WriteUInt16(address, 0, 0x8042);
		p.WriteUInt16(address, 2, 0xAAAA);

		Assert.True(MuiProcessDispatchArgumentSlotCodec.TryReadValue(ref p,
			address, out var value));
		Assert.Equal(0x8042AAAAu, value);
		Assert.False(MuiProcessDispatchArgumentSlotCodec.TryReadValue(ref p,
			APTR.FromPointer(0x40FFEu), out _));
		Assert.True(MuiProcessDispatchArgumentSlotCodec.WriteValue(ref p, address,
			0xC001D00Du));
		Assert.True(MuiProcessDispatchArgumentSlotCodec.TryReadValue(ref p,
			address, out value));
		Assert.Equal(0xC001D00Du, value);
	}

	[Fact]
	public void ProcessGeneratedHeaderAndArgumentSlotUseDirectNamedAdapters()
	{
		var p = NewPlatform();
		var header = new MuiProcessMethodMessageHeader { MethodId = 0x8042BBBBu };
		Assert.True(MuiProcessMethodMessageHeaderMemoryCodec.Write(ref p, Message,
			header));
		Assert.True(MuiProcessMethodMessageHeaderMemoryCodec.TryRead(ref p,
			Message, out var roundTrip));
		Assert.Equal(header.MethodId, roundTrip.MethodId);
		Assert.False(MuiProcessMethodMessageHeaderMemoryCodec.TryGetAddress(ref p,
			APTR.FromPointer(0xFFFFFFF0u),
			MuiProcessMethodMessageHeaderField.MethodId, out _));

		var slotAddress = APTR.FromPointer(Packet.Raw +
			MuiProcessDispatchPacketHeader.Size);
		Assert.True(MuiProcessDispatchArgumentSlotFieldMemoryCodec.TryWriteUInt32(
			ref p, slotAddress, MuiProcessDispatchArgumentSlotField.Value,
			0xABCDu));
		Assert.True(MuiProcessDispatchArgumentSlotFieldMemoryCodec.TryReadUInt32(
			ref p, slotAddress, MuiProcessDispatchArgumentSlotField.Value,
			out var slotValue));
		Assert.Equal(0xABCDu, slotValue);
		Assert.False(MuiProcessDispatchArgumentSlotFieldMemoryCodec.TryGetAddress(
			ref p, slotAddress,
			(MuiProcessDispatchArgumentSlotField)0xFF, out _));
	}

	[Fact]
	public void ProcessArgumentCursorUsesNamedPacketBoundaries()
	{
		var p = NewPlatform();
		var cursor = default(MuiProcessArgumentCursor);
		cursor.Message = Packet;
		cursor.Index = 1;
		cursor.Count = 2;
		cursor.Kind = MuiProcessArgumentVectorKind.DispatchPacket;
		Assert.True(MuiProcessArgumentCursorCodec.TryGetEntry(ref p, cursor,
			out var address));
		Assert.Equal(APTR.FromPointer(Packet.Raw + 12), address);
		cursor.Message = APTR.FromPointer(0x1800);
		cursor.Kind = MuiProcessArgumentVectorKind.MethodMessage;
		Assert.True(MuiProcessArgumentCursorCodec.TryGetEntry(ref p, cursor,
			out address));
		Assert.Equal(APTR.FromPointer(0x1808), address);
		cursor.Count = MuiProcessSpecialistLayout.MaximumDispatchArgs + 1;
		Assert.False(MuiProcessArgumentCursorCodec.TryGetEntry(ref p, cursor,
			out _));
	}

	[Fact]
	public void ProcessArgumentVectorMemoryAdapterOwnsEntryBounds()
	{
		var p = NewPlatform();
		Assert.True(MuiProcessArgumentVectorMemoryCodec.TryGetEntry(ref p,
			Packet, MuiProcessArgumentVectorKind.DispatchPacket, 1, 2,
			out var dispatchSlot));
		Assert.Equal(Packet.Raw + 12, dispatchSlot.Raw);
		Assert.True(MuiProcessArgumentVectorMemoryCodec.TryGetEntry(ref p,
			Message, MuiProcessArgumentVectorKind.MethodMessage, 0, 2,
			out var methodSlot));
		Assert.Equal(Message.Raw + 4, methodSlot.Raw);
		Assert.False(MuiProcessArgumentVectorMemoryCodec.TryGetEntry(ref p,
			APTR.Null, MuiProcessArgumentVectorKind.MethodMessage, 0, 1,
			out _));
		Assert.False(MuiProcessArgumentVectorMemoryCodec.TryGetEntry(ref p,
			Packet, MuiProcessArgumentVectorKind.DispatchPacket, 2, 2,
			out _));
		Assert.False(MuiProcessArgumentVectorMemoryCodec.TryGetEntry(ref p,
			Packet, MuiProcessArgumentVectorKind.DispatchPacket, 0,
			MuiProcessSpecialistLayout.MaximumDispatchArgs + 1, out _));
		Assert.False(MuiProcessArgumentVectorMemoryCodec.TryGetEntry(ref p,
			APTR.FromPointer(0x40FFEu), MuiProcessArgumentVectorKind.MethodMessage,
			0, 1, out _));
	}

	[Fact]
	public void ProcessArgumentVectorBridgeUsesNamedDispatchSlots()
	{
		var p = NewPlatform();
		var expected = new MuiProcessDispatchArgumentSlot
		{
			Value = 0xFEDCBA98u,
		};

		Assert.True(MuiProcessArgumentVectorCodec.TryWrite(ref p, Packet,
			MuiProcessArgumentVectorKind.DispatchPacket, 1, 2, expected));
		Assert.True(MuiProcessArgumentVectorCodec.TryRead(ref p, Packet,
			MuiProcessArgumentVectorKind.DispatchPacket, 1, 2,
			out var decoded));
		Assert.Equal(expected.Value, decoded.Value);
		Assert.True(MuiProcessArgumentVectorCodec.TryReadValue(ref p, Packet,
			MuiProcessArgumentVectorKind.DispatchPacket, 1, 2,
			out var rawValue));
		Assert.Equal(expected.Value, rawValue);
		Assert.False(MuiProcessArgumentVectorCodec.TryReadValue(ref p, Packet,
			MuiProcessArgumentVectorKind.DispatchPacket,
			MuiProcessSpecialistLayout.MaximumDispatchArgs, 2, out _));
		Assert.False(MuiProcessArgumentVectorCodec.TryWriteValue(ref p,
			APTR.FromPointer(0x40FFEu), MuiProcessArgumentVectorKind.MethodMessage,
			0, 1, expected.Value));
	}

	[Fact]
	public void ProcessArgumentCursorExchangesCompleteNamedSlots()
	{
		var p = NewPlatform();
		var cursor = default(MuiProcessArgumentCursor);
		cursor.Message = Packet;
		cursor.Kind = MuiProcessArgumentVectorKind.DispatchPacket;
		cursor.Index = 1;
		cursor.Count = 2;
		const uint expected = 0xFEDCBA98u;

		Assert.True(MuiProcessArgumentCursorCodec.TryWriteValue(ref p, cursor,
			expected));
		Assert.True(MuiProcessArgumentCursorCodec.TryReadValue(ref p, cursor,
			out var actual));
		Assert.Equal(expected, actual);

		var slot = default(MuiProcessDispatchArgumentSlot);
		slot.Value = 0x10203040u;
		Assert.True(MuiProcessArgumentCursorCodec.TryWrite(ref p, cursor, slot));
		Assert.True(MuiProcessArgumentCursorCodec.TryRead(ref p, cursor,
			out var decoded));
		Assert.Equal(slot.Value, decoded.Value);

		cursor.Index = cursor.Count;
		Assert.False(MuiProcessArgumentCursorCodec.TryReadValue(ref p, cursor,
			out _));
		cursor.Message = APTR.FromPointer(0x40FFEu);
		cursor.Index = 0;
		cursor.Count = 1;
		cursor.Kind = MuiProcessArgumentVectorKind.MethodMessage;
		Assert.False(MuiProcessArgumentCursorCodec.TryWriteValue(ref p, cursor,
			0x55AAu));
	}

	[Fact]
	public void ProcessRecordFieldCursorUsesSemanticHeaderAndStateKinds()
	{
		var p = NewPlatform();
		var cursor = default(MuiProcessRecordFieldCursor);
		cursor.Address = Packet;
		cursor.Record = MuiProcessRecordKind.DispatchHeader;
		cursor.Field = MuiProcessRecordField.MethodId;
		Assert.True(MuiProcessRecordFieldCursorCodec.TryGetAddress(ref p,
			cursor, out var fieldAddress));
		Assert.Equal(Packet.Raw + 4, fieldAddress.Raw);
		cursor.Address = APTR.FromPointer(0x2C00);
		cursor.Record = MuiProcessRecordKind.Specialist;
		cursor.Field = MuiProcessRecordField.NotifyAttribute;
		Assert.True(MuiProcessRecordFieldCursorCodec.TryGetAddress(ref p,
			cursor, out fieldAddress));
		Assert.Equal(0x2C30u, fieldAddress.Raw);
		Assert.True(MuiProcessRecordFieldCursorCodec.TryWriteUInt32(ref p,
			cursor.Address, MuiProcessRecordKind.Specialist,
			MuiProcessRecordField.NameOwned, 0xDEADBEEFu));
		Assert.True(MuiProcessRecordFieldCursorCodec.TryReadUInt32(ref p,
			cursor.Address, MuiProcessRecordKind.Specialist,
			MuiProcessRecordField.NameOwned, out var nameOwned));
		Assert.Equal(0xDEADBEEFu, nameOwned);
		cursor.Record = MuiProcessRecordKind.DispatchHeader;
		cursor.Field = MuiProcessRecordField.NotifyCount;
		Assert.False(MuiProcessRecordFieldCursorCodec.TryGetAddress(ref p,
			cursor, out _));
		cursor.Address = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Field = MuiProcessRecordField.MethodId;
		Assert.False(MuiProcessRecordFieldCursorCodec.TryGetAddress(ref p,
			cursor, out _));
	}

	[Fact]
	public void ProcessSpecialistMessageMemoryAdapterUsesNamedPacketRecords()
	{
		var p = NewPlatform();
		Assert.True(MuiProcessSpecialistMessageMemoryCodec.TryWriteUInt32(ref p,
			Message, MuiProcessSpecialistPacketKind.Set,
			MuiProcessSpecialistField.MethodId,
			MuiProcessSpecialistMessageCodec.MethodSet));
		Assert.True(MuiProcessSpecialistMessageMemoryCodec.TryWriteUInt32(ref p,
			Message, MuiProcessSpecialistPacketKind.Set,
			MuiProcessSpecialistField.Attribute, 0x8042F001u));
		Assert.True(MuiProcessSpecialistMessageMemoryCodec.TryReadUInt32(ref p,
			Message, MuiProcessSpecialistPacketKind.Set,
			MuiProcessSpecialistField.Attribute, out var attribute));
		Assert.Equal(0x8042F001u, attribute);

		Assert.True(MuiProcessSpecialistMessageMemoryCodec.TryGetAddress(ref p,
			Message, MuiProcessSpecialistPacketKind.Get,
			MuiProcessSpecialistField.Storage, out var storageAddress));
		Assert.Equal(Message.Raw +
			MuiProcessSpecialistGetMessage.StorageOffset, storageAddress.Raw);
		Assert.False(MuiProcessSpecialistMessageMemoryCodec.TryGetAddress(ref p,
			Message, MuiProcessSpecialistPacketKind.Method,
			MuiProcessSpecialistField.Attribute, out _));
		Assert.False(MuiProcessSpecialistMessageMemoryCodec.TryGetAddress(ref p,
			APTR.FromPointer(0xFFFFFFF0u), MuiProcessSpecialistPacketKind.Set,
			MuiProcessSpecialistField.Value, out _));
	}

	[Fact]
	public void ProcessRecordFieldMemoryAdapterUsesNamedRecordBoundaries()
	{
		var p = NewPlatform();
		Assert.True(MuiProcessRecordFieldMemoryCodec.TryWriteUInt32(ref p,
			Packet, MuiProcessRecordKind.DispatchHeader,
			MuiProcessRecordField.ArgumentCount, 2));
		Assert.True(MuiProcessRecordFieldMemoryCodec.TryReadUInt32(ref p,
			Packet, MuiProcessRecordKind.DispatchHeader,
			MuiProcessRecordField.ArgumentCount, out var argumentCount));
		Assert.Equal(2u, argumentCount);

		var recordAddress = APTR.FromPointer(0x2C00u);
		Assert.True(MuiProcessRecordFieldMemoryCodec.TryWriteUInt32(ref p,
			recordAddress, MuiProcessRecordKind.Specialist,
			MuiProcessRecordField.NotifyAttribute, 0x8042F002u));
		Assert.True(MuiProcessRecordFieldMemoryCodec.TryReadUInt32(ref p,
			recordAddress, MuiProcessRecordKind.Specialist,
			MuiProcessRecordField.NotifyAttribute, out var attribute));
		Assert.Equal(0x8042F002u, attribute);

		Assert.False(MuiProcessRecordFieldMemoryCodec.TryGetAddress(ref p,
			Packet, MuiProcessRecordKind.DispatchHeader,
			MuiProcessRecordField.NotifyAttribute, out _));
		Assert.False(MuiProcessRecordFieldMemoryCodec.TryGetAddress(ref p,
			APTR.FromPointer(0xFFFFFFF0u), MuiProcessRecordKind.Specialist,
			MuiProcessRecordField.NotifyAttribute, out _));
	}

	[Fact]
	public void DispatchRequiresSetupAndLiveApplication()
	{
		var p = NewPlatform();
		var (slave, app, _) = NewSlave(ref p);
		WritePacket(ref p, 0, 0x8042CCCC);
		// Not set up yet.
		Assert.False(MuiProcessSpecialistCore.Dispatch(ref p, State, slave, Packet,
			out _));
		MuiProcessSpecialistCore.Setup(ref p, State, slave);
		Assert.True(MuiProcessSpecialistCore.Dispatch(ref p, State, slave, Packet,
			out _));
		// Application dies: dispatch refuses.
		MuiHeadlessObjectCore.DisposeObject(ref p, State, app);
		Assert.False(MuiProcessSpecialistCore.Dispatch(ref p, State, slave, Packet,
			out _));
	}

	[Fact]
	public void DispatchFailsWhenTargetLockedByAnotherTask()
	{
		var p = NewPlatform();
		var (slave, _, target) = NewSlave(ref p);
		MuiProcessSpecialistCore.Setup(ref p, State, slave);
		// Another task exclusively owns the target instance.
		p.CurrentTask = 99;
		Assert.True(MuiSemaphoreCore.Obtain(ref p, State, target));
		p.CurrentTask = 1;
		WritePacket(ref p, 0, 0x8042DDDD);
		Assert.False(MuiProcessSpecialistCore.Dispatch(ref p, State, slave, Packet,
			out _));
	}

	// ---- Slave: error and signals --------------------------------------------

	[Fact]
	public void ErrorStoresAndReportsCode()
	{
		var p = NewPlatform();
		var (slave, _, _) = NewSlave(ref p);
		Assert.True(MuiProcessSpecialistCore.Error(ref p, State, slave, 205,
			out var stored) && stored == 205);
		Assert.Equal(205u, MuiProcessSpecialistCore.LastError(ref p, State, slave));
	}

	[Fact]
	public void SignalsReceivedReservesBreakMaskAndAccumulates()
	{
		var p = NewPlatform();
		var (slave, _, _) = NewSlave(ref p);
		// A break signal arrives even though the caller only asked for a custom
		// bit: the reserved SIGBREAKF_CTRL mask is always coordinated.
		p.ProcessPendingSignals = MuiProcessAttributes.SIGBREAKF_CTRL_C | 0x40;
		var received = MuiProcessSpecialistCore.SignalsReceived(ref p, State, slave,
			0x40);
		Assert.True((received & MuiProcessAttributes.SIGBREAKF_CTRL_C) != 0);
		Assert.True((received & 0x40) != 0);
		// The poll widened the mask to include the reserved break bits.
		Assert.True((p.LastSignalsReceivedMask &
			MuiProcessAttributes.SIGBREAKF_CTRL) ==
			MuiProcessAttributes.SIGBREAKF_CTRL);
		Assert.Equal(received,
			MuiProcessSpecialistCore.AccumulatedSignals(ref p, State, slave));
	}

	// ---- Dispatcher routing --------------------------------------------------

	[Fact]
	public void ProcessSpecialistPacketCodecUsesNamedRecordsAndRejectsMalformedPackets()
	{
		var p = NewPlatform();
		Assert.True(MuiProcessSpecialistMessageCodec.WriteGet(ref p, Message,
			MuiProcessAttributes.Process_StackSize, Storage.Raw));
		Assert.True(MuiProcessSpecialistMessageCodec.TryReadGet(ref p, Message,
			out var get));
		Assert.Equal(MuiProcessAttributes.Process_StackSize, get.Attribute);
		Assert.Equal(Storage.Raw, get.Storage);

		Assert.True(MuiProcessSpecialistMessageCodec.WriteSet(ref p, Message,
			MuiProcessSpecialistMessageCodec.MethodSet,
			MuiProcessAttributes.Process_Priority, 5));
		Assert.True(MuiProcessSpecialistMessageCodec.TryReadSet(ref p, Message,
			MuiProcessSpecialistMessageCodec.MethodSet, out var set));
		Assert.Equal(MuiProcessAttributes.Process_Priority, set.Attribute);
		Assert.Equal(5u, set.Value);

		Assert.True(MuiProcessSpecialistMessageCodec.WriteSignal(ref p, Message,
			MuiProcessAttributes.Process_Signal, 0x40));
		Assert.True(MuiProcessSpecialistMessageCodec.TryReadSignal(ref p, Message,
			MuiProcessAttributes.Process_Signal, out var signal));
		Assert.Equal(0x40u, signal.Signals);

		Assert.True(MuiProcessSpecialistMessageCodec.WriteError(ref p, Message,
			205));
		Assert.True(MuiProcessSpecialistMessageCodec.TryReadError(ref p, Message,
			out var error));
		Assert.Equal(205u, error.ErrorCode);

		Assert.True(MuiProcessSpecialistMessageCodec.WriteDispatch(ref p, Message,
			Packet.Raw));
		Assert.True(MuiProcessSpecialistMessageCodec.TryReadDispatch(ref p, Message,
			out var dispatch));
		Assert.Equal(Packet.Raw, dispatch.Packet);

		Assert.True(MuiProcessSpecialistMessageCodec.WriteMethod(ref p, Message,
			MuiProcessAttributes.Process_Launch));
		Assert.True(MuiProcessSpecialistMessageCodec.IsValidMethod(ref p, Message,
			MuiProcessAttributes.Process_Launch));
		Assert.False(MuiProcessSpecialistMessageCodec.WriteSignal(ref p, Message,
			0x80420000u, 1));
		Assert.False(MuiProcessSpecialistMessageCodec.TryReadSignal(ref p,
			APTR.FromPointer(Base + (uint)Size - 1),
			MuiProcessAttributes.Process_Signal, out _));
		Assert.False(MuiProcessSpecialistMessageCodec.TryReadError(ref p,
			APTR.FromPointer(Base + (uint)Size - 1), out _));
		Assert.False(MuiProcessSpecialistMessageCodec.IsValidMethod(ref p, Message,
			0x80420000u));
	}

	[Fact]
	public void ProcessSpecialistMessageAdapterOwnsStructBounds()
	{
		var p = NewPlatform();
		Assert.True(MuiProcessSpecialistMessageMemoryCodec.TryWriteUInt32(ref p,
			Message, MuiProcessSpecialistPacketKind.Get,
			MuiProcessSpecialistField.Storage, Storage.Raw));
		Assert.True(MuiProcessSpecialistMessageMemoryCodec.TryReadUInt32(ref p,
			Message, MuiProcessSpecialistPacketKind.Get,
			MuiProcessSpecialistField.Storage, out var storage));
		Assert.Equal(Storage.Raw, storage);
		Assert.True(MuiProcessSpecialistMessageMemoryCodec.TryWriteUInt32(ref p,
			Message, MuiProcessSpecialistPacketKind.Set,
			MuiProcessSpecialistField.Value, 0x456));
		Assert.True(MuiProcessSpecialistMessageMemoryCodec.TryReadUInt32(ref p,
			Message, MuiProcessSpecialistPacketKind.Set,
			MuiProcessSpecialistField.Value, out var value));
		Assert.Equal(0x456u, value);
		Assert.True(MuiProcessSpecialistMessageMemoryCodec.TryWriteUInt32(ref p,
			Message, MuiProcessSpecialistPacketKind.Signal,
			MuiProcessSpecialistField.Signals, 0x40));
		Assert.True(MuiProcessSpecialistMessageMemoryCodec.TryReadUInt32(ref p,
			Message, MuiProcessSpecialistPacketKind.Signal,
			MuiProcessSpecialistField.Signals, out var signals));
		Assert.Equal(0x40u, signals);
		Assert.True(MuiProcessSpecialistMessageMemoryCodec.TryWriteUInt32(ref p,
			Message, MuiProcessSpecialistPacketKind.Error,
			MuiProcessSpecialistField.ErrorCode, 205));
		Assert.True(MuiProcessSpecialistMessageMemoryCodec.TryReadUInt32(ref p,
			Message, MuiProcessSpecialistPacketKind.Error,
			MuiProcessSpecialistField.ErrorCode, out var error));
		Assert.Equal(205u, error);
		Assert.True(MuiProcessSpecialistMessageMemoryCodec.TryWriteUInt32(ref p,
			Message, MuiProcessSpecialistPacketKind.Dispatch,
			MuiProcessSpecialistField.Packet, Packet.Raw));
		Assert.True(MuiProcessSpecialistMessageMemoryCodec.TryReadUInt32(ref p,
			Message, MuiProcessSpecialistPacketKind.Dispatch,
			MuiProcessSpecialistField.Packet, out var dispatch));
		Assert.Equal(Packet.Raw, dispatch);
		Assert.False(MuiProcessSpecialistMessageMemoryCodec.TryReadUInt32(ref p,
			APTR.FromPointer(0x40FFD), MuiProcessSpecialistPacketKind.Dispatch,
			MuiProcessSpecialistField.Packet, out _));
		Assert.False(MuiProcessSpecialistMessageMemoryCodec.TryReadUInt32(ref p,
			Message, MuiProcessSpecialistPacketKind.Method,
			MuiProcessSpecialistField.Attribute, out _));
		Assert.False(MuiProcessSpecialistMessageMemoryCodec.TryReadUInt32(ref p,
			APTR.Null, MuiProcessSpecialistPacketKind.Set,
			MuiProcessSpecialistField.Value, out _));
	}

	[Fact]
	public void ProcessSpecialistMethodHeaderUsesNamedField()
	{
		var p = NewPlatform();
		Assert.True(MuiProcessSpecialistMessageCodec.WriteMethod(ref p, Message,
			MuiProcessAttributes.Process_Launch));
		Assert.True(MuiProcessSpecialistMessageCodec.TryReadMethodId(ref p, Message,
			out var packet));
		Assert.Equal(MuiProcessAttributes.Process_Launch, packet.MethodId);
		Assert.True(MuiProcessSpecialistMessageCodec.TryReadMethodIdValue(ref p,
			Message, out var methodId));
		Assert.Equal(MuiProcessAttributes.Process_Launch, methodId);
		Assert.False(MuiProcessSpecialistMessageCodec.TryReadMethodId(ref p,
			APTR.Null, out _));
	}

	[Fact]
	public void ProcessSpecialistMethodHeaderUsesSharedUlongStorageBoundary()
	{
		var p = NewPlatform();
		const uint methodId = 0x8246ACEFu;
		Assert.True(MuiProcessSpecialistMethodHeaderCodec.WriteValue(ref p,
			Message, methodId));
		Assert.True(MuiProcessSpecialistMethodHeaderCodec.TryReadValue(ref p,
			Message, out var readMethodId));
		Assert.Equal(methodId, readMethodId);
		Assert.False(MuiProcessSpecialistMethodHeaderCodec.TryReadValue(ref p,
			APTR.FromPointer(0x40FFFu), out _));
		Assert.False(MuiProcessSpecialistMethodHeaderCodec.WriteValue(ref p,
			APTR.Null, methodId));
	}

	[Fact]
	public void ProcessSpecialistTypedReadersUseNamedMethodHeader()
	{
		var p = NewPlatform();
		Assert.True(MuiProcessSpecialistMessageCodec.WriteError(ref p, Message,
			205));
		Assert.True(MuiProcessSpecialistMessageCodec.TryReadError(ref p, Message,
			out var error));
		Assert.Equal(MuiProcessAttributes.Slave_Error, error.MethodId);
		Assert.True(MuiProcessSpecialistFieldCursorCodec.TryWriteUInt32(ref p,
			Message, MuiProcessSpecialistPacketKind.Error,
			MuiProcessSpecialistField.MethodId, 0xDEADBEEFu));
		Assert.False(MuiProcessSpecialistMessageCodec.TryReadError(ref p, Message,
			out _));
	}

	[Fact]
	public void ProcessSpecialistFieldCursorUsesNamedMixedPacketBoundaries()
	{
		var p = NewPlatform();
		var cursor = default(MuiProcessSpecialistFieldCursor);
		cursor.Message = Message;
		cursor.Packet = MuiProcessSpecialistPacketKind.Get;
		cursor.Field = MuiProcessSpecialistField.MethodId;
		Assert.True(MuiProcessSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out var address));
		Assert.Equal(Message.Raw, address.Raw);
		cursor.Field = MuiProcessSpecialistField.Attribute;
		Assert.True(MuiProcessSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out address));
		Assert.Equal(Message.Raw + 4, address.Raw);
		cursor.Field = MuiProcessSpecialistField.Storage;
		Assert.True(MuiProcessSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out address));
		Assert.Equal(Message.Raw + 8, address.Raw);

		Assert.True(MuiProcessSpecialistFieldCursorCodec.TryWriteUInt32(ref p,
			Message, MuiProcessSpecialistPacketKind.Signal,
			MuiProcessSpecialistField.Signals, 0x40));
		Assert.True(MuiProcessSpecialistFieldCursorCodec.TryReadUInt32(ref p,
			Message, MuiProcessSpecialistPacketKind.Signal,
			MuiProcessSpecialistField.Signals, out var signals));
		Assert.Equal(0x40u, signals);
		cursor.Packet = MuiProcessSpecialistPacketKind.Error;
		cursor.Field = MuiProcessSpecialistField.Attribute;
		Assert.False(MuiProcessSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out _));
		cursor.Message = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Packet = MuiProcessSpecialistPacketKind.Dispatch;
		cursor.Field = MuiProcessSpecialistField.Packet;
		Assert.False(MuiProcessSpecialistFieldCursorCodec.TryGetAddress(ref p,
			cursor, out _));
	}

	[Fact]
	public void DispatcherRoutesGetLaunchAndDispose()
	{
		var p = NewPlatform();
		var proc = Process(ref p);
		// OM_GET of the stack size through the dispatcher.
		p.WriteUInt32(Message, 0, 0x00000104u);              // OM_GET
		p.WriteUInt32(Message, 4, MuiProcessAttributes.Process_StackSize);
		p.WriteUInt32(Message, 8, Storage.Raw);
		Assert.True(MuiProcessSpecialistDispatcher.TryDispatch(ref p, State, proc,
			Message, out var got) && got == 1);
		Assert.Equal(8192u, p.ReadUInt32(Storage, 0));
		// MUIM_Process_Launch through the dispatcher.
		p.WriteUInt32(Message, 0, MuiProcessAttributes.Process_Launch);
		Assert.True(MuiProcessSpecialistDispatcher.TryDispatch(ref p, State, proc,
			Message, out var launched) && launched == 1);
		Assert.Equal(MuiProcessState.Running,
			MuiProcessSpecialistCore.ProcessStateOf(ref p, State, proc));
		// OM_DISPOSE through the dispatcher.
		p.WriteUInt32(Message, 0, 0x00000102u);
		Assert.True(MuiProcessSpecialistDispatcher.TryDispatch(ref p, State, proc,
			Message, out var disposed) && disposed == 1);
		Assert.False(MuiProcessSpecialistCore.Valid(ref p, State, proc));
	}

	[Fact]
	public void DispatcherRoutesSlaveMethodsAndSemaphoreSuperclass()
	{
		var p = NewPlatform();
		var (slave, _, target) = NewSlave(ref p);
		// MUIM_Slave_Setup.
		p.WriteUInt32(Message, 0, MuiProcessAttributes.Slave_Setup);
		Assert.True(MuiProcessSpecialistDispatcher.TryDispatch(ref p, State, slave,
			Message, out var setup) && setup == 1);
		// MUIM_Slave_Dispatch with a bounded packet.
		WritePacket(ref p, 1, 0x8042EEEE, 0x55);
		p.WriteUInt32(Message, 0, MuiProcessAttributes.Slave_Dispatch);
		p.WriteUInt32(Message, 4, Packet.Raw);
		Assert.True(MuiProcessSpecialistDispatcher.TryDispatch(ref p, State, slave,
			Message, out _));
		Assert.Equal(0x8042EEEEu, p.LastDispatchMethod);
		// Inherited Semaphore.mui Obtain/Release route to the frozen core.
		p.WriteUInt32(Message, 0, MuiProcessAttributes.Semaphore_Obtain);
		Assert.True(MuiProcessSpecialistDispatcher.TryDispatch(ref p, State, slave,
			Message, out var obtained) && obtained == 1);
		p.WriteUInt32(Message, 0, MuiProcessAttributes.Semaphore_Release);
		Assert.True(MuiProcessSpecialistDispatcher.TryDispatch(ref p, State, slave,
			Message, out var released) && released == 1);
	}

	[Fact]
	public void DispatcherIgnoresForeignInstances()
	{
		var p = NewPlatform();
		var raw = RawObject(ref p, AppName);   // no process/slave sidecar
		p.WriteUInt32(Message, 0, MuiProcessAttributes.Process_Launch);
		Assert.False(MuiProcessSpecialistDispatcher.TryDispatch(ref p, State, raw,
			Message, out _));
	}

	[Fact]
	public void FactoryImportsInitialProcessAttributesBeforeSidecarAttach()
	{
		var p = NewPlatform();
		p.WriteCString(NameA, "Worker");
		// TagItem vector: Name, Priority, StackSize, AutoLaunch, TAG_DONE.
		p.WriteUInt32(Storage, 0, MuiProcessAttributes.Process_Name);
		p.WriteUInt32(Storage, 4, NameA.Raw);
		p.WriteUInt32(Storage, 8, MuiProcessAttributes.Process_Priority);
		p.WriteUInt32(Storage, 12, unchecked((uint)-3));
		p.WriteUInt32(Storage, 16, MuiProcessAttributes.Process_StackSize);
		p.WriteUInt32(Storage, 20, 16384);
		p.WriteUInt32(Storage, 24, MuiProcessAttributes.Process_AutoLaunch);
		p.WriteUInt32(Storage, 28, 1);
		p.WriteUInt32(Storage, 32, 0);
		p.WriteUInt32(Storage, 36, 0);

		var proc = MuiObjectFactoryServiceCore.NewObjectA(ref p, State,
			ProcessName, Storage);
		Assert.True(proc.IsNotNull);
		Assert.True(MuiProcessSpecialistCore.GetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_Priority, out var priority));
		Assert.Equal(unchecked((uint)-3), priority);
		Assert.True(MuiProcessSpecialistCore.GetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_StackSize, out var stack));
		Assert.Equal(16384u, stack);
		Assert.True(MuiProcessSpecialistCore.AutoLaunchFlag(ref p, State, proc));
		Assert.True(MuiProcessSpecialistCore.GetAttribute(ref p, State, proc,
			MuiProcessAttributes.Process_Name, out var name));
		Assert.NotEqual(NameA.Raw, name);
		Assert.Equal((byte)'W', p.ReadUInt8(APTR.FromPointer(name), 0));

		Assert.True(MuiProcessSpecialistCore.AutoLaunchIfRequested(ref p, State,
			proc));
		Assert.Equal(MuiProcessState.Running,
			MuiProcessSpecialistCore.ProcessStateOf(ref p, State, proc));
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref p, State, proc));
	}

	[Fact]
	public void ServiceHeadlessDispatcherRoutesFactoryCreatedProcess()
	{
		var p = NewPlatform();
		var proc = MuiObjectFactoryServiceCore.NewObjectA(ref p, State,
			ProcessName, APTR.Null);
		Assert.True(proc.IsNotNull);
		Assert.Equal(MuiProcessSpecialistClass.Process,
			MuiProcessSpecialistCore.Classify(ref p, State, proc));

		p.WriteUInt32(Message, 0, 0x00000104u); // OM_GET
		p.WriteUInt32(Message, 4, MuiProcessAttributes.Process_StackSize);
		p.WriteUInt32(Message, 8, Storage.Raw);
		Assert.Equal(1u, MuiProcessSpecialistDispatcher.Dispatch(ref p, State,
			proc, Message));
		Assert.Equal(8192u, p.ReadUInt32(Storage, 0));

		p.WriteUInt32(Message, 0, MuiProcessAttributes.Process_Launch);
		Assert.Equal(1u, MuiProcessSpecialistDispatcher.Dispatch(ref p, State,
			proc, Message));
		Assert.Equal(MuiProcessState.Running,
			MuiProcessSpecialistCore.ProcessStateOf(ref p, State, proc));

		p.WriteUInt32(Message, 0, 0x00000102u); // OM_DISPOSE
		Assert.Equal(1u, MuiProcessSpecialistDispatcher.Dispatch(ref p, State,
			proc, Message));
		Assert.False(MuiProcessSpecialistCore.Valid(ref p, State, proc));
	}
}
