/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeClassDispatchContextTests
{
	private static readonly APTR OwnerAddress = APTR.FromPointer(0x1800);
	private static readonly APTR FrameOne = APTR.FromPointer(0x2200);
	private static readonly APTR FrameTwo = APTR.FromPointer(0x2240);
	private static readonly APTR TaskOne = APTR.FromPointer(0x3000);
	private static readonly APTR TaskTwo = APTR.FromPointer(0x3100);
	private static readonly APTR ClassOne = APTR.FromPointer(0x4000);
	private static readonly APTR ClassTwo = APTR.FromPointer(0x4100);
	private static readonly APTR ClassThree = APTR.FromPointer(0x4200);
	private static readonly APTR ClassFour = APTR.FromPointer(0x4300);
	private static readonly APTR ObjectOne = APTR.FromPointer(0x5000);
	private static readonly APTR MessageOne = APTR.FromPointer(0x5100);
	private const uint Method = 0x80420001;

	[Fact]
	public void DispatchFrameCodecUsesTheNamedPackedRecord()
	{
		Assert.Equal(28, Marshal.SizeOf<MuiNativeClassDispatchFrameRecord>());

		var memory = NewMemory();
		var expected = new MuiNativeClassDispatchFrameRecord
		{
			Signature = MuiNativeClassDispatchFrameRecord.SignatureValue,
			Next = FrameTwo,
			OwnerTask = TaskOne,
			Class = ClassOne,
			Object = ObjectOne,
			Message = MessageOne,
			Method = Method,
		};
		Assert.True(MuiNativeClassDispatchFrameCodec.Write(ref memory, FrameOne,
			expected));
		Assert.True(MuiNativeClassDispatchFrameCodec.TryRead(ref memory, FrameOne,
			out var actual));
		Assert.Equal(expected, actual);
	}

	[Fact]
	public void ActiveDispatcherFramesAuthorizeOnlyTheirTaskAndUnlinkByIdentity()
	{
		var memory = NewMemory();
		InitializeOwner(ref memory);
		Assert.False(MuiNativeClassDispatchContextCore.HasActiveFrameForTask(
			ref memory, OwnerAddress, TaskOne));
		Assert.True(MuiNativeClassDispatchContextCore.TryPush(ref memory,
			OwnerAddress, FrameOne, TaskOne, ClassOne, ObjectOne, MessageOne,
			Method));
		Assert.True(MuiNativeClassDispatchContextCore.TryPush(ref memory,
			OwnerAddress, FrameTwo, TaskTwo, ClassOne, ObjectOne, MessageOne,
			Method + 1));
		Assert.True(MuiNativeClassDispatchContextCore.HasActiveFrameForTask(
			ref memory, OwnerAddress, TaskOne));
		Assert.True(MuiNativeClassDispatchContextCore.HasActiveFrameForTask(
			ref memory, OwnerAddress, TaskTwo));
		Assert.False(MuiNativeClassDispatchContextCore.HasActiveFrameForTask(
			ref memory, OwnerAddress, APTR.FromPointer(0x3200)));

		// Removing a non-head frame must preserve the nested task's authority.
		Assert.True(MuiNativeClassDispatchContextCore.TryPop(ref memory,
			OwnerAddress, FrameOne));
		Assert.False(MuiNativeClassDispatchContextCore.HasActiveFrameForTask(
			ref memory, OwnerAddress, TaskOne));
		Assert.True(MuiNativeClassDispatchContextCore.HasActiveFrameForTask(
			ref memory, OwnerAddress, TaskTwo));
		Assert.True(MuiNativeClassDispatchContextCore.TryPop(ref memory,
			OwnerAddress, FrameTwo));
		Assert.False(MuiNativeClassDispatchContextCore.HasActiveFrameForTask(
			ref memory, OwnerAddress, TaskTwo));
		Assert.True(MuiNativeClassOwnerCodec.TryRead(ref memory, OwnerAddress,
			out var owner));
		Assert.True(owner.ActiveDispatchFrames.IsNull);
		Assert.Equal(0u, owner.DispatchFramesPoisoned);
	}

	[Fact]
	public void CorruptOrPoisonedFrameChainFailsClosedAndBlocksFurtherPushes()
	{
		var memory = NewMemory();
		InitializeOwner(ref memory);
		Assert.True(MuiNativeClassDispatchContextCore.TryPush(ref memory,
			OwnerAddress, FrameOne, TaskOne, ClassOne, ObjectOne, MessageOne,
			Method));
		Assert.True(MuiNativeClassDispatchFrameCodec.TryRead(ref memory, FrameOne,
			out var frame));
		frame.Signature++;
		Assert.True(MuiNativeClassDispatchFrameCodec.Write(ref memory, FrameOne,
			frame));
		Assert.False(MuiNativeClassDispatchContextCore.HasActiveFrameForTask(
			ref memory, OwnerAddress, TaskOne));
		Assert.False(MuiNativeClassDispatchContextCore.TryPush(ref memory,
			OwnerAddress, FrameTwo, TaskTwo, ClassOne, ObjectOne, MessageOne,
			Method));

		InitializeOwner(ref memory);
		Assert.True(MuiNativeClassOwnerCodec.TryWriteDispatchContext(ref memory,
			OwnerAddress, APTR.Null, 1));
		Assert.False(MuiNativeClassDispatchContextCore.HasActiveFrameForTask(
			ref memory, OwnerAddress, TaskOne));
		Assert.False(MuiNativeClassDispatchContextCore.TryPush(ref memory,
			OwnerAddress, FrameTwo, TaskTwo, ClassOne, ObjectOne, MessageOne,
			Method));
	}

	[Fact]
	public void SuperDispatchFrameMatchesOnlyItsTaskObjectMessageMethodAndClassAncestry()
	{
		var memory = NewMemory();
		InitializeOwner(ref memory);
		BOOPSIGuestCodec.WriteClass(ref memory, ClassOne,
			new IClass { cl_Super = ClassTwo });
		BOOPSIGuestCodec.WriteClass(ref memory, ClassTwo,
			new IClass { cl_Super = ClassThree });
		BOOPSIGuestCodec.WriteClass(ref memory, ClassThree, new IClass());
		BOOPSIGuestCodec.WriteClass(ref memory, ClassFour, new IClass());
		Assert.True(MuiNativeClassDispatchContextCore.TryPush(ref memory,
			OwnerAddress, FrameOne, TaskOne, ClassOne, ObjectOne, MessageOne,
			Method));

		Assert.True(MuiNativeClassDispatchContextCore.TryHasSuperDispatchFrame(
			ref memory, OwnerAddress, TaskOne, ClassTwo, ObjectOne, MessageOne,
			Method, out var isSuperDispatch));
		Assert.True(isSuperDispatch);
		Assert.True(MuiNativeClassDispatchContextCore.TryHasSuperDispatchFrame(
			ref memory, OwnerAddress, TaskOne, ClassThree, ObjectOne, MessageOne,
			Method, out isSuperDispatch));
		Assert.True(isSuperDispatch);

		Assert.True(MuiNativeClassDispatchContextCore.TryHasSuperDispatchFrame(
			ref memory, OwnerAddress, TaskOne, ClassOne, ObjectOne, MessageOne,
			Method, out isSuperDispatch));
		Assert.False(isSuperDispatch);
		Assert.True(MuiNativeClassDispatchContextCore.TryHasSuperDispatchFrame(
			ref memory, OwnerAddress, TaskTwo, ClassTwo, ObjectOne, MessageOne,
			Method, out isSuperDispatch));
		Assert.False(isSuperDispatch);
		Assert.True(MuiNativeClassDispatchContextCore.TryHasSuperDispatchFrame(
			ref memory, OwnerAddress, TaskOne, ClassTwo,
			APTR.FromPointer(0x5010), MessageOne, Method,
			out isSuperDispatch));
		Assert.False(isSuperDispatch);
		Assert.True(MuiNativeClassDispatchContextCore.TryHasSuperDispatchFrame(
			ref memory, OwnerAddress, TaskOne, ClassTwo, ObjectOne,
			APTR.FromPointer(0x5110), Method, out isSuperDispatch));
		Assert.False(isSuperDispatch);
		Assert.True(MuiNativeClassDispatchContextCore.TryHasSuperDispatchFrame(
			ref memory, OwnerAddress, TaskOne, ClassTwo, ObjectOne, MessageOne,
			Method + 1, out isSuperDispatch));
		Assert.False(isSuperDispatch);
		Assert.True(MuiNativeClassDispatchContextCore.TryHasSuperDispatchFrame(
			ref memory, OwnerAddress, TaskOne, ClassFour, ObjectOne, MessageOne,
			Method, out isSuperDispatch));
		Assert.False(isSuperDispatch);

		Assert.True(MuiNativeClassDispatchContextCore.TryPop(ref memory,
			OwnerAddress, FrameOne));
	}

	private static MuiHeadlessTestPlatform NewMemory() =>
		new(0x1000, 0x7000, 0, APTR.FromPointer(0x1000));

	private static void InitializeOwner(ref MuiHeadlessTestPlatform memory)
	{
		var owner = default(MuiNativeClassOwnerRecord);
		owner.Context.OwnerRoot = APTR.FromPointer(0x1200);
		owner.Magic = MuiNativeClassOwnerCore.MagicValue;
		owner.Version = MuiNativeClassOwnerCore.Version;
		owner.RegistryGeneration = 1;
		owner.Registry.Magic = MuiHeadlessLayout.Magic;
		owner.Registry.Version = MuiHeadlessLayout.Version;
		owner.Registry.NextSequence = 1;
		owner.PublicObjects.Signature =
			MuiNativePublicObjectRegistryRecord.Magic;
		owner.PublicObjects.Revision =
			MuiNativePublicObjectRegistryRecord.Version;
		Assert.True(MuiNativeClassOwnerCodec.Write(ref memory, OwnerAddress,
			owner));
	}
}
