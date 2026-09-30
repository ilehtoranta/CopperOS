using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster.Tests;

// Host ordering model, not a real scheduler or native provider implementation.
public sealed partial class MuiNativeProviderLeaseTests
{
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	public void InvalidServiceOperationRejectsBeforeGateOrTokenMutation(int field)
	{
		var provider = NewPlatform();
		var gate = NewGate(ref provider);
		Assert.Equal(MuiNativeClassLeaseResult.Acquired, Enter(ref provider, out var operation));
		var original = operation;
		switch (field)
		{
			case 0: operation.Owner = APTR.Null; break;
			case 1: operation.Service = APTR.FromPointer(0x1234); break;
			case 2: operation.RegistryGeneration++; break;
			case 3: operation.Providers.UtilityBase = APTR.FromPointer(0x1234); break;
			case 4: operation.OwnerRoot = APTR.Null; break;
			case 5: operation.KeymapBase = APTR.FromPointer(0x2345); break;
		}
		var rejected = operation;
		Assert.False(MuiNativeServiceAccessCore.TryEnter(ref gate, ref operation, out var service));
		Assert.Equal(rejected, operation);
		Assert.Equal(default(MuiNativeServiceLease), service);
		Assert.Equal(0, gate.Obtains);
		Assert.Equal(0, gate.Releases);
		Assert.True(MuiNativeClassLeaseCore.Leave(ref provider, ref original));
	}

	[Fact]
	public void ServiceAdmissionTransfersPinAndReleasesGateBeforeProviderRetirement()
	{
		Assert.Equal(48, Unsafe.SizeOf<MuiNativeServiceLease>());
		Assert.Equal(48u, MuiNativeServiceLease.Size);
		var provider = NewPlatform();
		var gate = NewGate(ref provider);
		Assert.Equal(MuiNativeClassLeaseResult.Acquired, Enter(ref provider, out var operation));
		Assert.True(MuiNativeServiceAccessCore.TryEnter(ref gate, ref operation, out var service));
		Assert.Equal(default(MuiNativeClassLease), operation);
		Assert.Equal(1u, Owner(ref provider).ActiveOperations);
		Assert.Equal(gate.Caller, service.OwnerTask);
		Assert.True(MuiNativeServiceAccessCore.TryLeave(ref gate, ref service, out operation));
		Assert.Equal(default(MuiNativeServiceLease), service);
		Assert.Equal(1u, Owner(ref provider).ActiveOperations);
		Assert.Empty(provider.State.Closed);
		Assert.Equal(0, Owner(ref provider).ServiceGate.NestCount);
		Assert.True(MuiNativeClassLeaseCore.Leave(ref provider, ref operation));
		AssertReverseCloses(provider.State);
		Assert.False(MuiNativeServiceAccessCore.TryLeave(ref gate, ref service, out _));
	}

	[Fact]
	public void NestedServiceAdmissionRetainsOuterGateAndProviderPin()
	{
		var provider = NewPlatform();
		var gate = NewGate(ref provider);
		Assert.Equal(MuiNativeClassLeaseResult.Acquired, Enter(ref provider, out var outer));
		Assert.True(MuiNativeServiceAccessCore.TryEnter(ref gate, ref outer, out var outerService));
		Assert.Equal(MuiNativeClassLeaseResult.Acquired, Enter(ref provider, out var inner));
		Assert.True(MuiNativeServiceAccessCore.TryEnter(ref gate, ref inner, out var innerService));
		Assert.Equal(2, Owner(ref provider).ServiceGate.NestCount);
		Assert.True(MuiNativeServiceAccessCore.TryLeave(ref gate, ref innerService, out inner));
		Assert.True(MuiNativeClassLeaseCore.Leave(ref provider, ref inner));
		Assert.Equal(1, Owner(ref provider).ServiceGate.NestCount);
		Assert.Equal(1u, Owner(ref provider).ActiveOperations);
		Assert.Empty(provider.State.Closed);
		Assert.True(MuiNativeServiceAccessCore.TryLeave(ref gate, ref outerService, out outer));
		Assert.True(MuiNativeClassLeaseCore.Leave(ref provider, ref outer));
		AssertReverseCloses(provider.State);
	}

	[Fact]
	public void AdmissionRevalidatesAfterWaitAndReturnsOriginalPinOnFailure()
	{
		var provider = NewPlatform();
		var gate = NewGate(ref provider);
		Assert.Equal(MuiNativeClassLeaseResult.Acquired, Enter(ref provider, out var operation));
		var original = operation;
		gate.RejectAfterObtain = true;
		Assert.False(MuiNativeServiceAccessCore.TryEnter(ref gate, ref operation, out var service));
		Assert.Equal(original, operation);
		Assert.Equal(default(MuiNativeServiceLease), service);
		Assert.Equal(1, gate.Obtains);
		Assert.Equal(1, gate.Releases);
		Assert.Equal(0, Owner(ref provider).ServiceGate.NestCount);
		Assert.Equal(1u, Owner(ref provider).ActiveOperations);
		Assert.True(MuiNativeClassLeaseCore.Leave(ref provider, ref operation));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void RejectedServiceLeaveRetainsAuthorityForRetry(bool differentTask)
	{
		var provider = NewPlatform();
		var gate = NewGate(ref provider);
		Assert.Equal(MuiNativeClassLeaseResult.Acquired, Enter(ref provider, out var operation));
		Assert.True(MuiNativeServiceAccessCore.TryEnter(ref gate, ref operation, out var service));
		var original = service;
		if (differentTask) gate.Caller = APTR.FromPointer(0x6000);
		else gate.RejectOwnerRead = true;
		Assert.False(MuiNativeServiceAccessCore.TryLeave(ref gate, ref service, out operation));
		Assert.Equal(original, service);
		Assert.Equal(default(MuiNativeClassLease), operation);
		Assert.Equal(0, gate.Releases);
		gate.Caller = original.OwnerTask;
		Assert.True(MuiNativeServiceAccessCore.TryLeave(ref gate, ref service, out operation));
		Assert.True(MuiNativeClassLeaseCore.Leave(ref provider, ref operation));
	}

	[Fact]
	public void RecursiveGateOverflowRejectsBeforeObtainAndPreservesPin()
	{
		var provider = NewPlatform();
		var gate = NewGate(ref provider);
		Assert.Equal(MuiNativeClassLeaseResult.Acquired, Enter(ref provider, out var operation));
		var value = Owner(ref provider);
		value.ServiceGate.Owner = gate.Caller;
		value.ServiceGate.NestCount = short.MaxValue;
		WriteOwner(ref provider, value);
		Assert.False(MuiNativeServiceAccessCore.TryEnter(ref gate, ref operation, out _));
		Assert.Equal(0, gate.Obtains);
		Assert.True(operation.Owner.IsNotNull);
		value.ServiceGate.Owner = APTR.Null;
		value.ServiceGate.NestCount = 0;
		WriteOwner(ref provider, value);
		Assert.True(MuiNativeClassLeaseCore.Leave(ref provider, ref operation));
	}

	private static ServiceGatePlatform NewGate(ref LeasePlatform provider)
	{
		Assert.True(MuiNativeClassOwnerCodec.TryGetGateAddress(ref provider, provider.State.Owner, out var address));
		var queue = address.Raw + (uint)Marshal.OffsetOf<SignalSemaphore>(nameof(SignalSemaphore.WaitQueue)).ToInt32();
		var value = default(SignalSemaphore);
		value.WaitQueue.Head = APTR.FromPointer(queue + (uint)Marshal.OffsetOf<MinList>(nameof(MinList.Tail)).ToInt32());
		value.WaitQueue.TailPred = APTR.FromPointer(queue);
		Assert.True(MuiSignalSemaphoreCodec.Write(ref provider, address, value));
		return new ServiceGatePlatform { State = provider.State, Caller = APTR.FromPointer(0x5000) };
	}

	private struct ServiceGatePlatform : IMuiServiceGatePlatform
	{
		internal LeaseState State;
		internal APTR Caller;
		internal int Obtains;
		internal int Releases;
		internal bool RejectAfterObtain;
		internal bool RejectOwnerRead;
		public APTR CurrentTask() => Caller;
		public void ObtainGate(APTR gate)
		{
			Assert.Equal(0, State.CriticalDepth);
			Assert.True(MuiNativeClassOwnerCodec.TryRead(ref State.Memory, State.Owner, out var owner));
			Assert.True(owner.ActiveOperations > 0); // retained before possible suspension
			Assert.True(MuiSignalSemaphoreCodec.TryRead(ref State.Memory, gate, out var value));
			Assert.True(value.Owner.IsNull || value.Owner == Caller);
			value.Owner = Caller;
			value.NestCount++;
			Assert.True(MuiSignalSemaphoreCodec.Write(ref State.Memory, gate, value));
			Obtains++;
			RejectOwnerRead = RejectAfterObtain;
		}
		public void ReleaseGate(APTR gate)
		{
			Assert.Equal(0, State.CriticalDepth);
			Assert.True(MuiSignalSemaphoreCodec.TryRead(ref State.Memory, gate, out var value));
			Assert.Equal(Caller, value.Owner);
			Assert.True(value.NestCount > 0);
			value.NestCount--;
			if (value.NestCount == 0) value.Owner = APTR.Null;
			Assert.True(MuiSignalSemaphoreCodec.Write(ref State.Memory, gate, value));
			Releases++;
		}
		public bool IsMapped(APTR address, uint size)
		{
			if (RejectOwnerRead && address == State.Owner && size == MuiNativeClassOwnerRecord.Size)
			{
				RejectOwnerRead = false;
				return false;
			}
			return State.Memory.IsMapped(address, size);
		}
		public byte ReadUInt8(APTR address, int offset) => State.Memory.ReadUInt8(address, offset);
		public ushort ReadUInt16(APTR address, int offset) => State.Memory.ReadUInt16(address, offset);
		public uint ReadUInt32(APTR address, int offset) => State.Memory.ReadUInt32(address, offset);
		public void WriteUInt8(APTR address, int offset, byte value) => State.Memory.WriteUInt8(address, offset, value);
		public void WriteUInt16(APTR address, int offset, ushort value) => State.Memory.WriteUInt16(address, offset, value);
		public void WriteUInt32(APTR address, int offset, uint value) => State.Memory.WriteUInt32(address, offset, value);
		public void Clear(APTR address, uint size) => State.Memory.Clear(address, size);
		public void Copy(APTR source, APTR destination, uint size) => State.Memory.Copy(source, destination, size);
	}
}
