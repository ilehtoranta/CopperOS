using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeLayoutDispatchBaseTests
{
	private static readonly APTR MuiLibrary = APTR.FromPointer(0x1800);
	private static readonly APTR Class = APTR.FromPointer(0x1900);
	private static readonly APTR LeaseAddress = APTR.FromPointer(0x1400);
	private static readonly APTR ProviderLibrary = APTR.FromPointer(0x1A00);

	[Fact]
	public void CustomClassWithoutAServiceLeaseUsesMuiLibraryBase()
	{
		var memory = NewMemory();

		Assert.True(MuiNativeLayoutServiceCore.TryResolveCallbackBase(ref memory,
			APTR.Null, Class, MuiLibrary, out var callbackBase));
		Assert.Equal(MuiLibrary, callbackBase);
	}

	[Fact]
	public void BuiltinClassUsesMuiBaseWhenItsNamedLeaseHasNoProviderBase()
	{
		var memory = NewMemory();
		WriteLease(ref memory, MuiClassServiceLayout.FlagBuiltin, Class,
			APTR.Null);

		Assert.True(MuiNativeLayoutServiceCore.TryResolveCallbackBase(ref memory,
			LeaseAddress, Class, MuiLibrary, out var callbackBase));
		Assert.Equal(MuiLibrary, callbackBase);

		WriteLease(ref memory, MuiClassServiceLayout.FlagBuiltin, Class,
			ProviderLibrary);
		Assert.True(MuiNativeLayoutServiceCore.TryResolveCallbackBase(ref memory,
			LeaseAddress, Class, MuiLibrary, out callbackBase));
		Assert.Equal(ProviderLibrary, callbackBase);
	}

	[Fact]
	public void ExternalClassUsesTheProviderBaseFromItsNamedLease()
	{
		var memory = NewMemory();
		WriteLease(ref memory, MuiClassServiceLayout.FlagExternal |
			MuiClassServiceLayout.FlagOwnsClassId, Class, ProviderLibrary);

		Assert.True(MuiNativeLayoutServiceCore.TryResolveCallbackBase(ref memory,
			LeaseAddress, Class, MuiLibrary, out var callbackBase));
		Assert.Equal(ProviderLibrary, callbackBase);
	}

	[Fact]
	public void InvalidOrMismatchedLeaseFailsInsteadOfFallingBackToMuiBase()
	{
		var memory = NewMemory();
		WriteLease(ref memory, MuiClassServiceLayout.FlagExternal, Class,
			APTR.Null);

		Assert.False(MuiNativeLayoutServiceCore.TryResolveCallbackBase(ref memory,
			LeaseAddress, Class, MuiLibrary, out _));
		Assert.False(MuiNativeLayoutServiceCore.TryResolveCallbackBase(ref memory,
			LeaseAddress, APTR.FromPointer(0x1940), MuiLibrary, out _));
		Assert.False(MuiNativeLayoutServiceCore.TryResolveCallbackBase(ref memory,
			APTR.FromPointer(0x20FFC), Class, MuiLibrary, out _));
	}

	[Fact]
	public void CustomOrAmbiguousLeaseKindsAreRejected()
	{
		var memory = NewMemory();
		WriteLease(ref memory, MuiClassServiceLayout.FlagCustom, Class,
			ProviderLibrary);
		Assert.False(MuiNativeLayoutServiceCore.TryResolveCallbackBase(ref memory,
			LeaseAddress, Class, MuiLibrary, out _));

		WriteLease(ref memory, MuiClassServiceLayout.FlagBuiltin |
			MuiClassServiceLayout.FlagExternal, Class, ProviderLibrary);
		Assert.False(MuiNativeLayoutServiceCore.TryResolveCallbackBase(ref memory,
			LeaseAddress, Class, MuiLibrary, out _));
	}

	private static MuiHeadlessTestPlatform NewMemory() =>
		new(0x1000, 0x20000, 0x4000, APTR.FromPointer(0x1800));

	private static void WriteLease(ref MuiHeadlessTestPlatform memory,
		uint flags, APTR classPointer, APTR libraryBase)
	{
		var lease = default(MuiClassServiceLeaseRecord);
		lease.Flags = flags;
		lease.Boopsi = classPointer;
		lease.LibraryBase = libraryBase;
		Assert.True(MuiClassServiceLeaseCodec.Write(ref memory, LeaseAddress,
			lease));
	}
}
