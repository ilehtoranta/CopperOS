/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeClassBindingTests
{
	private static readonly APTR Address = APTR.FromPointer(0x1000);

	private static MuiNativeClassBinding Sample() => new()
	{
		Next = APTR.FromPointer(0x11223344),
		Class = APTR.FromPointer(0x22334455),
		Dispatcher = APTR.FromPointer(0x33445566),
		LibraryBase = APTR.FromPointer(0x44556677),
		OwnerRoot = APTR.FromPointer(0x55667788),
		SidecarClass = APTR.FromPointer(0x66778899),
		ObjectHead = APTR.FromPointer(0x778899AA),
		ActiveCalls = 0x89ABCDEF,
		ActiveObjects = 0x9ABCDEF0,
		LifecycleFlags = 1,
	};

	private static MuiNativeClassObjectBinding SampleObject() => new()
	{
		Signature = MuiNativeClassObjectBinding.Magic,
		Next = APTR.FromPointer(0x12345678),
		Object = APTR.FromPointer(0x23456789),
		Class = APTR.FromPointer(0x3456789A),
		OwnerRoot = APTR.FromPointer(0x456789AB),
		Sidecar = APTR.FromPointer(0x56789ABC),
		ActiveCalls = 2,
		LifecycleFlags = MuiNativeClassObjectBinding.NativeDisposed,
	};

	private static MuiHeadlessTestPlatform CreateNativeObjectMemory(
		APTR bindingAddress, APTR objectBindingAddress, APTR sidecarAddress,
		APTR ownerRoot, APTR classAddress, APTR objectAddress)
	{
		var memory = new MuiHeadlessTestPlatform(Address.Raw, 0x1000, 0, Address);
		var classBinding = new MuiNativeClassBinding
		{
			Class = classAddress,
			Dispatcher = APTR.FromPointer(0x1600),
			OwnerRoot = ownerRoot,
			ObjectHead = objectBindingAddress,
		};
		var objectBinding = new MuiNativeClassObjectBinding
		{
			Signature = MuiNativeClassObjectBinding.Magic,
			Object = objectAddress,
			Class = classAddress,
			OwnerRoot = ownerRoot,
			Sidecar = sidecarAddress,
		};
		var sidecar = new MuiNativeMuiObjectRecord
		{
			Signature = MuiNativeMuiObjectRecord.Magic,
			Revision = MuiNativeMuiObjectRecord.Version,
			Object = objectAddress,
			Class = classAddress,
			OwnerRoot = ownerRoot,
			Flags = MuiNativeMuiObjectRecord.ObjectInitialized,
			LifecycleState = MuiNativeMuiObjectRecord.StateLive,
		};
		Assert.True(MuiNativeClassBindingCodec.Write(ref memory, bindingAddress,
			classBinding));
		Assert.True(MuiNativeClassObjectBindingCodec.Write(ref memory,
			objectBindingAddress, objectBinding));
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress,
			sidecar));
		return memory;
	}

	[Fact]
	public void PackedBindingRoundTripsNamedFieldsWithoutTouchingAdjacentBytes()
	{
		Assert.Equal((int)MuiNativeClassBinding.Size, Unsafe.SizeOf<MuiNativeClassBinding>());
		var memory = new MuiHeadlessTestPlatform(Address.Raw,
			(int)MuiNativeClassBinding.Size + 4, 0, Address);
		memory.WriteUInt32(Address, (int)MuiNativeClassBinding.Size, 0xA5A55A5A);
		var expected = Sample();
		Assert.True(MuiNativeClassBindingCodec.Write(ref memory, Address, expected));
		Assert.True(MuiNativeClassBindingCodec.TryRead(ref memory, Address, out var actual));
		Assert.Equal(expected, actual);
		Assert.Equal(0xA5A55A5Au, memory.ReadUInt32(Address, (int)MuiNativeClassBinding.Size));
	}

	[Fact]
	public void ClearingOptionalCallbackBaseAndChangingDepthPreservesClassOwnership()
	{
		var memory = new MuiHeadlessTestPlatform(Address.Raw, (int)MuiNativeClassBinding.Size, 0, Address);
		var expected = Sample();
		Assert.True(MuiNativeClassBindingCodec.Write(ref memory, Address, expected));
		Assert.True(MuiNativeClassBindingCodec.TryRead(ref memory, Address, out var actual));
		actual.LibraryBase = APTR.Null;
		actual.ActiveCalls = 1;
		Assert.True(MuiNativeClassBindingCodec.Write(ref memory, Address, actual));
		Assert.True(MuiNativeClassBindingCodec.TryRead(ref memory, Address, out var updated));
		expected.LibraryBase = APTR.Null;
		expected.ActiveCalls = 1;
		Assert.Equal(expected, updated);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(4)]
	[InlineData(8)]
	[InlineData(12)]
	[InlineData(16)]
	[InlineData(20)]
	[InlineData(23)]
	[InlineData(27)]
	[InlineData(31)]
	[InlineData(35)]
	[InlineData(39)]
	public void TruncatedBindingRejectsBeforeReadingOrWritingFields(int mappedBytes)
	{
		var memory = new MuiHeadlessTestPlatform(Address.Raw, mappedBytes, 0, Address);
		for (var index = 0; index < mappedBytes; index++) memory.WriteUInt8(Address, index, 0xA5);
		Assert.False(MuiNativeClassBindingCodec.TryRead(ref memory, Address, out var actual));
		Assert.Equal(default(MuiNativeClassBinding), actual);
		Assert.False(MuiNativeClassBindingCodec.Write(ref memory, Address, Sample()));
		for (var index = 0; index < mappedBytes; index++) Assert.Equal(0xA5, memory.ReadUInt8(Address, index));
	}

	[Fact]
	public void PackedObjectBindingRoundTripsNamedFieldsWithoutTouchingAdjacentBytes()
	{
		Assert.Equal((int)MuiNativeClassObjectBinding.Size,
			Unsafe.SizeOf<MuiNativeClassObjectBinding>());
		var memory = new MuiHeadlessTestPlatform(Address.Raw,
			(int)MuiNativeClassObjectBinding.Size + 4, 0, Address);
		memory.WriteUInt32(Address, (int)MuiNativeClassObjectBinding.Size,
			0x5AA5A55A);
		var expected = SampleObject();
		Assert.True(MuiNativeClassObjectBindingCodec.Write(ref memory, Address,
			expected));
		Assert.True(MuiNativeClassObjectBindingCodec.TryRead(ref memory, Address,
			out var actual));
		Assert.Equal(expected, actual);
		Assert.Equal(0x5AA5A55Au, memory.ReadUInt32(Address,
			(int)MuiNativeClassObjectBinding.Size));
	}

	[Fact]
	public void NativeObjectSidecarResolutionUsesTypedClassAndObjectRecords()
	{
		var bindingAddress = APTR.FromPointer(0x1000);
		var objectBindingAddress = APTR.FromPointer(0x1100);
		var sidecarAddress = APTR.FromPointer(0x1200);
		var ownerRoot = APTR.FromPointer(0x1300);
		var classAddress = APTR.FromPointer(0x1400);
		var objectAddress = APTR.FromPointer(0x1500);
		var memory = CreateNativeObjectMemory(bindingAddress,
			objectBindingAddress, sidecarAddress, ownerRoot, classAddress,
			objectAddress);

		Assert.True(MuiNativeClassDispatcher.TryFindNativeObjectSidecar(ref memory,
			bindingAddress, classAddress, ownerRoot, objectAddress,
			out var actualSidecar, out var actualObjectBinding));
		Assert.Equal(sidecarAddress, actualSidecar);
		Assert.Equal(objectBindingAddress, actualObjectBinding);
	}

	[Fact]
	public void NativeObjectSidecarResolutionRejectsWrongClassRootAndObject()
	{
		var bindingAddress = APTR.FromPointer(0x1000);
		var objectBindingAddress = APTR.FromPointer(0x1100);
		var sidecarAddress = APTR.FromPointer(0x1200);
		var ownerRoot = APTR.FromPointer(0x1300);
		var classAddress = APTR.FromPointer(0x1400);
		var objectAddress = APTR.FromPointer(0x1500);
		var memory = CreateNativeObjectMemory(bindingAddress,
			objectBindingAddress, sidecarAddress, ownerRoot, classAddress,
			objectAddress);

		Assert.False(MuiNativeClassDispatcher.TryFindNativeObjectSidecar(ref memory,
			bindingAddress, APTR.FromPointer(0x1404), ownerRoot, objectAddress,
			out _, out _));
		Assert.False(MuiNativeClassDispatcher.TryFindNativeObjectSidecar(ref memory,
			bindingAddress, classAddress, APTR.FromPointer(0x1304), objectAddress,
			out _, out _));
		Assert.False(MuiNativeClassDispatcher.TryFindNativeObjectSidecar(ref memory,
			bindingAddress, classAddress, ownerRoot, APTR.FromPointer(0x1504),
			out _, out _));
	}

	[Fact]
	public void PublicBorrowMarkerPreservesTheNativeClassOwnedSidecar()
	{
		var bindingAddress = APTR.FromPointer(0x1000);
		var objectBindingAddress = APTR.FromPointer(0x1100);
		var sidecarAddress = APTR.FromPointer(0x1200);
		var ownerRoot = APTR.FromPointer(0x1300);
		var classAddress = APTR.FromPointer(0x1400);
		var objectAddress = APTR.FromPointer(0x1500);
		var memory = CreateNativeObjectMemory(bindingAddress,
			objectBindingAddress, sidecarAddress, ownerRoot, classAddress,
			objectAddress);

		Assert.True(MuiNativeClassDispatcher.TrySetPublicSidecarBorrowed(ref memory,
			objectBindingAddress, classAddress, ownerRoot, objectAddress,
			sidecarAddress));
		Assert.True(MuiNativeClassObjectBindingCodec.TryRead(ref memory,
			objectBindingAddress, out var objectBinding));
		Assert.Equal(MuiNativeClassObjectBinding.PublicSidecarBorrowed,
			objectBinding.LifecycleFlags);
		Assert.False(MuiNativeClassDispatcher.TrySetPublicSidecarBorrowed(ref memory,
			objectBindingAddress, classAddress, ownerRoot, objectAddress,
			sidecarAddress));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(4)]
	[InlineData(23)]
	[InlineData(27)]
	[InlineData(31)]
	public void TruncatedObjectBindingRejectsBeforeReadingOrWritingFields(int mappedBytes)
	{
		var memory = new MuiHeadlessTestPlatform(Address.Raw, mappedBytes, 0,
			Address);
		for (var index = 0; index < mappedBytes; index++)
			memory.WriteUInt8(Address, index, 0xA5);
		Assert.False(MuiNativeClassObjectBindingCodec.TryRead(ref memory, Address,
			out var actual));
		Assert.Equal(default(MuiNativeClassObjectBinding), actual);
		Assert.False(MuiNativeClassObjectBindingCodec.Write(ref memory, Address,
			SampleObject()));
		for (var index = 0; index < mappedBytes; index++)
			Assert.Equal(0xA5, memory.ReadUInt8(Address, index));
	}

	[Fact]
	public void NativeMemoryChecksPointerArithmeticWithoutClaimingAnAddressProbe()
	{
		Assert.Equal(4, Unsafe.SizeOf<MuiNativeClassMemory>());
		Assert.Equal(4, Unsafe.SizeOf<MuiNativeClassPlatform>());
		var memory = default(MuiNativeClassMemory);
		Assert.True(memory.IsMapped(Address, MuiNativeClassBinding.Size));
		Assert.True(memory.IsMapped(APTR.FromPointer(Address.Raw + 1), 1));
		Assert.False(memory.IsMapped(APTR.Null, 1));
		Assert.False(memory.IsMapped(Address, 0));
		Assert.False(memory.IsMapped(APTR.FromPointer(uint.MaxValue - 3), 4));
		Assert.False(memory.IsMapped(APTR.FromPointer(uint.MaxValue), 1));
	}

	[Fact]
	public void PackedContextKeepsBothPointersBehindAScalarProviderHandle()
	{
		Assert.Equal((int)MuiNativeClassContext.Size, Unsafe.SizeOf<MuiNativeClassContext>());
		var memory = new MuiHeadlessTestPlatform(Address.Raw, 12, 0, Address);
		memory.WriteUInt32(Address, (int)MuiNativeClassContext.Size, 0xAABBCCDD);
		var expected = new MuiNativeClassContext
		{
			IntuitionBase = APTR.FromPointer(0x11223344),
			OwnerRoot = APTR.FromPointer(0x55667788),
		};
		Assert.True(MuiNativeClassContextCodec.Write(ref memory, Address, expected));
		Assert.True(MuiNativeClassContextCodec.TryRead(ref memory, Address, out var actual));
		Assert.Equal(expected, actual);
		Assert.Equal(0xAABBCCDDu, memory.ReadUInt32(Address, (int)MuiNativeClassContext.Size));
	}

	[Fact]
	public void MissingContextCannotAcquireOrFreeAnOwnedCustomClass()
	{
		var platform = default(MuiNativeClassPlatform);
		Assert.Equal(APTR.Null, platform.IntuitionBase);
		Assert.Equal(APTR.Null, platform.OwnerRoot);
		Assert.Equal(APTR.Null, platform.MakeCustomClass(Address, 4, Address, APTR.Null));
		Assert.False(platform.FreeCustomClass(Address));
	}

	[Fact]
	public void NativeHookCapabilityFailsClosedWithoutNamedOwnerUtilityBase()
	{
		var platform = default(MuiNativeClassPlatform);
		Assert.Equal(0u, platform.InvokeHook(Address, Address,
			Address));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(7)]
	public void TruncatedContextRejectsBeforeReadingOrWritingPointers(int mappedBytes)
	{
		var memory = new MuiHeadlessTestPlatform(Address.Raw, mappedBytes, 0, Address);
		for (var index = 0; index < mappedBytes; index++) memory.WriteUInt8(Address, index, 0xA5);
		Assert.False(MuiNativeClassContextCodec.TryRead(ref memory, Address, out var actual));
		Assert.Equal(default(MuiNativeClassContext), actual);
		Assert.False(MuiNativeClassContextCodec.Write(ref memory, Address, default));
		for (var index = 0; index < mappedBytes; index++) Assert.Equal(0xA5, memory.ReadUInt8(Address, index));
	}
}
