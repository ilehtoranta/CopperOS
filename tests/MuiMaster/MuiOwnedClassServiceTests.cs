using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed partial class MuiNativeProviderLeaseTests
{
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	public void OwnedConsumersRejectInvalidAuthorityWithoutConsumingCleanup(int invalid)
	{
		var provider = NewPlatform();
		var gate = NewGate(ref provider);
		Assert.Equal(MuiNativeClassLeaseResult.Acquired, Enter(ref provider, out var operation));
		Assert.True(MuiNativeServiceAccessCore.TryEnter(ref gate, ref operation, out var service));
		var valid = service;
		var caller = gate.Caller;
		if (invalid == 0) caller = APTR.Null;
		if (invalid == 1) service.Gate = APTR.Null;
		if (invalid == 2) service.Operation.RegistryGeneration++;
		if (invalid == 3) service.Operation.Providers.UtilityBase = APTR.Null;
		var rejected = service;
		var allocations = provider.State.Memory.AllocationCount;
		var pointer = APTR.FromPointer(0x2200);
		Assert.True(MuiOwnedClassServiceCore.GetClass(ref provider.State.Memory, ref service, caller, pointer).IsNull);
		Assert.False(MuiOwnedClassServiceCore.FreeClass(ref provider.State.Memory, ref service, caller, pointer));
		Assert.True(MuiOwnedClassServiceCore.CreateCustomClass(ref provider.State.Memory, ref service,
			caller, pointer, pointer, APTR.Null, 8, pointer).IsNull);
		Assert.False(MuiOwnedClassServiceCore.DeleteCustomClass(ref provider.State.Memory, ref service, caller, pointer));
		Assert.Equal(rejected, service);
		Assert.Equal(allocations, provider.State.Memory.AllocationCount);
		Assert.Empty(provider.State.Closed);
		service = valid;
		Assert.True(MuiNativeServiceAccessCore.TryLeave(ref gate, ref service, out operation));
		Assert.True(MuiNativeClassLeaseCore.Leave(ref provider, ref operation));
		AssertReverseCloses(provider.State);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void OwnedClassConsumersUseAdmittedServiceAndProviderBases(bool separateScope)
	{
		var provider = NewPlatform();
		var gate = NewGate(ref provider);
		Assert.Equal(MuiNativeClassLeaseResult.Acquired, Enter(ref provider, out var operation));
		Assert.True(MuiNativeServiceAccessCore.TryEnter(ref gate, ref operation, out var service));
		var original = service;
		var name = APTR.FromPointer(0x2200);
		var dispatcher = APTR.FromPointer(0xD000);
		provider.State.Memory.WriteCString(name, "Notify.mui");
		var registry = Owner(ref provider).Service.Headless;
		var builtin = MuiHeadlessObjectCore.RegisterBuiltinClass(ref provider.State.Memory,
			registry, name, APTR.Null, 8, dispatcher);
		Assert.True(builtin.IsNotNull);
		var cls = MuiOwnedClassServiceCore.GetClass(ref provider.State.Memory, ref service, gate.Caller, name);
		Assert.True(cls.IsNotNull);
		Assert.True(MuiOwnedClassServiceCore.FreeClass(ref provider.State.Memory, ref service, gate.Caller, cls));
		var custom = MuiOwnedClassServiceCore.CreateCustomClass(ref provider.State.Memory, ref service,
			gate.Caller, APTR.FromPointer(0xD100), name, APTR.Null, 12, dispatcher);
		Assert.True(custom.IsNotNull);
		Assert.True(MuiCustomClassStructCodec.TryRead(ref provider.State.Memory, custom, out var record));
		Assert.Equal(service.Operation.Providers.UtilityBase, record.UtilityBase);
		Assert.Equal(service.Operation.Providers.DosBase, record.DosBase);
		Assert.Equal(service.Operation.Providers.GraphicsBase, record.GfxBase);
		Assert.Equal(service.Operation.Providers.IntuitionBase, record.IntuitionBase);
		if (separateScope)
		{
			Assert.True(MuiNativeServiceAccessCore.TryLeave(ref gate, ref service, out operation));
			Assert.True(MuiNativeClassLeaseCore.Leave(ref provider, ref operation));
			Assert.Equal(0u, Owner(ref provider).ActiveOperations);
			Assert.Empty(provider.State.Closed);
			Assert.True(MuiCustomClassStructCodec.TryRead(ref provider.State.Memory, custom, out var retained));
			Assert.Equal(record, retained);
			Assert.Equal(MuiNativeClassLeaseResult.Acquired, Enter(ref provider, out operation));
			Assert.Equal(5, provider.State.Opened.Count);
			Assert.True(MuiNativeServiceAccessCore.TryEnter(ref gate, ref operation, out service));
		}
		Assert.True(MuiOwnedClassServiceCore.DeleteCustomClass(ref provider.State.Memory, ref service, gate.Caller, custom));
		Assert.Equal(original, service);
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref provider.State.Memory, registry, builtin));
		Assert.True(MuiNativeServiceAccessCore.TryLeave(ref gate, ref service, out operation));
		Assert.True(MuiNativeClassLeaseCore.Leave(ref provider, ref operation));
		Assert.Equal(0u, Owner(ref provider).ActiveOperations);
		AssertReverseCloses(provider.State);
	}
}
