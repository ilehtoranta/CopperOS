/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeMuiObjectTests
{
	private static readonly APTR Address = APTR.FromPointer(0x1800);

	private static MuiNativeMuiObjectRecord Sample() => new()
	{
		Signature = MuiNativeMuiObjectRecord.Magic,
		Revision = MuiNativeMuiObjectRecord.Version,
		Object = APTR.FromPointer(0x11223344),
		Class = APTR.FromPointer(0x22334455),
		OwnerRoot = APTR.FromPointer(0x33445566),
		Attributes = APTR.FromPointer(0x44556677),
		Notifications = APTR.FromPointer(0x55667788),
		Parent = APTR.FromPointer(0x66778899),
		Flags = MuiNativeMuiObjectRecord.ObjectInitialized |
			MuiNativeMuiObjectRecord.ObjectNativeDisposed,
		Generation = 0x778899AA,
		ObjectId = 0x89ABCDEF,
		UserData = 0x9ABCDEF0,
		ActiveCalls = 3,
		LifecycleState = MuiNativeMuiObjectRecord.StateNativeDisposed,
		NotifyDepth = 5,
		NotifySuppressionMethod = 0x80420A74,
		RequestedIDCMP = 0x00000418,
		RejectedIDCMP = 0x00000202,
		ReturnIdQueue = APTR.FromPointer(0x7788A000),
		InputSignalTask = APTR.FromPointer(0x7788B000),
		InputSignalMask = 0x00001800,
		NativeWindow = APTR.FromPointer(0x7788C000),
		InputHandlers = APTR.FromPointer(0x7788D000),
		InputHandlerGeneration = 0x8899AABB,
		InputTimerPort = APTR.FromPointer(0x7788E000),
		WindowEventHandlers = APTR.FromPointer(0x7788F000),
		WindowEventHandlerGeneration = 0x99AABBCC,
		PendingReleaseCount = 0xAABBCCDD,
		PendingReleaseDepth = 0xCCDDEEFF,
		PendingReleaseParent = APTR.FromPointer(0x7788F100),
		ApplicationPushQueue = APTR.FromPointer(0x7788F200),
		ApplicationPushGeneration = 0xDDEEFF00,
	};

	[Fact]
	public void PackedNativeMuiObjectRecordRoundTripsNamedFields()
	{
		Assert.Equal((int)MuiNativeMuiObjectRecord.Size,
			Unsafe.SizeOf<MuiNativeMuiObjectRecord>());
		var memory = new MuiHeadlessTestPlatform(Address.Raw,
			(int)MuiNativeMuiObjectRecord.Size + 4, 0, Address);
		memory.WriteUInt32(Address, (int)MuiNativeMuiObjectRecord.Size,
			0xA5A55A5A);
		var expected = Sample();
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, Address, expected));
		Assert.True(MuiNativeMuiObjectCodec.TryRead(ref memory, Address,
			out var actual));
		Assert.Equal(expected, actual);
		Assert.Equal(0xA5A55A5Au, memory.ReadUInt32(Address,
			(int)MuiNativeMuiObjectRecord.Size));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(4)]
	[InlineData(8)]
	[InlineData(31)]
	[InlineData(35)]
	[InlineData(39)]
	[InlineData(43)]
	[InlineData(47)]
	[InlineData(51)]
	[InlineData(55)]
	[InlineData(59)]
	[InlineData(63)]
	[InlineData(67)]
	[InlineData(71)]
	[InlineData(75)]
	[InlineData(79)]
	[InlineData(83)]
	[InlineData(84)]
	[InlineData(85)]
	[InlineData(86)]
	[InlineData(87)]
	[InlineData(88)]
	[InlineData(89)]
	[InlineData(90)]
	[InlineData(91)]
	[InlineData(92)]
	[InlineData(93)]
	[InlineData(94)]
	[InlineData(95)]
	[InlineData(96)]
	[InlineData(97)]
	[InlineData(98)]
	[InlineData(99)]
	[InlineData(100)]
	[InlineData(101)]
	[InlineData(102)]
	[InlineData(103)]
	[InlineData(104)]
	[InlineData(105)]
	[InlineData(106)]
	[InlineData(107)]
	[InlineData(108)]
	[InlineData(109)]
	[InlineData(110)]
	[InlineData(111)]
	[InlineData(112)]
	[InlineData(113)]
	[InlineData(114)]
	[InlineData(115)]
	[InlineData(116)]
	[InlineData(117)]
	[InlineData(118)]
	[InlineData(119)]
	[InlineData(120)]
	[InlineData(121)]
	[InlineData(122)]
	[InlineData(123)]
	[InlineData(124)]
	[InlineData(125)]
	[InlineData(126)]
	[InlineData(127)]
	public void TruncatedNativeMuiObjectRecordRejectsWithoutPartialWrites(
		int mappedBytes)
	{
		var memory = new MuiHeadlessTestPlatform(Address.Raw, mappedBytes, 0,
			Address);
		for (var index = 0; index < mappedBytes; index++)
			memory.WriteUInt8(Address, index, 0xA5);
		Assert.False(MuiNativeMuiObjectCodec.TryRead(ref memory, Address,
			out var actual));
		Assert.Equal(default(MuiNativeMuiObjectRecord), actual);
		Assert.False(MuiNativeMuiObjectCodec.Write(ref memory, Address, Sample()));
		for (var index = 0; index < mappedBytes; index++)
			Assert.Equal(0xA5, memory.ReadUInt8(Address, index));
	}

	[Fact]
	public void InvalidSignatureAndRevisionRemainStructurallyReadableButFailAdmission()
	{
		var memory = new MuiHeadlessTestPlatform(Address.Raw,
			(int)MuiNativeMuiObjectRecord.Size, 0, Address);
		var value = Sample();
		value.Signature = 0;
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, Address, value));
		Assert.False(MuiNativeMuiObjectCodec.TryRead(ref memory, Address,
			out var decoded));
		Assert.Equal(0u, decoded.Signature);
		Assert.Equal(MuiNativeMuiObjectRecord.Version, decoded.Revision);
	}

	[Fact]
	public void NativeUserDataTraversalFrameRoundTripsNamedFields()
	{
		var address = APTR.FromPointer(0x1A00);
		Assert.Equal((int)MuiNativeUDataTraversalFrame.Size,
			Unsafe.SizeOf<MuiNativeUDataTraversalFrame>());
		var memory = new MuiHeadlessTestPlatform(address.Raw,
			(int)MuiNativeUDataTraversalFrame.Size, 0, address);
		var expected = new MuiNativeUDataTraversalFrame
		{
			Object = APTR.FromPointer(0x11223344),
			NextBinding = APTR.FromPointer(0x22334455),
			Expanded = 1,
		};
		Assert.True(MuiNativeUDataTraversalFrameCodec.Write(ref memory, address,
			expected));
		Assert.True(MuiNativeUDataTraversalFrameCodec.TryRead(ref memory, address,
			out var actual));
		Assert.Equal(expected, actual);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(4)]
	[InlineData(8)]
	[InlineData(11)]
	public void TruncatedNativeUserDataTraversalFrameRejects(int mappedBytes)
	{
		var address = APTR.FromPointer(0x1A00);
		var memory = new MuiHeadlessTestPlatform(address.Raw, mappedBytes, 0,
			address);
		Assert.False(MuiNativeUDataTraversalFrameCodec.TryRead(ref memory,
			address, out _));
		Assert.False(MuiNativeUDataTraversalFrameCodec.Write(ref memory, address,
			new MuiNativeUDataTraversalFrame { Expanded = 1 }));
	}
}
