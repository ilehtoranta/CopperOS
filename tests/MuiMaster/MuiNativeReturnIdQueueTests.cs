/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeReturnIdQueueTests
{
	private static readonly APTR Sidecar = APTR.FromPointer(0x1800);
	private static readonly APTR First = APTR.FromPointer(0x1880);
	private static readonly APTR Second = APTR.FromPointer(0x18A0);

	[Fact]
	public void ReturnIdNodeIsACompleteNamedRecord()
	{
		Assert.Equal((int)MuiNativeReturnIdRecord.Size,
			Unsafe.SizeOf<MuiNativeReturnIdRecord>());
		var memory = new MuiHeadlessTestPlatform(First.Raw, 32, 0, First);
		var expected = new MuiNativeReturnIdRecord
		{
			Signature = MuiNativeReturnIdRecord.Magic,
			Revision = MuiNativeReturnIdRecord.Version,
			Next = Second,
			ReturnId = 0xDEADBEEF,
		};
		Assert.True(MuiNativeReturnIdRecordCodec.Write(ref memory, First,
			expected));
		Assert.True(MuiNativeReturnIdRecordCodec.TryRead(ref memory, First,
			out var actual));
		Assert.Equal(expected, actual);
	}

	[Fact]
	public void NativeReturnIdQueueDeliversFifoAndClearsItsHead()
	{
		var memory = new MuiHeadlessTestPlatform(Sidecar.Raw, 0x200, 0, Sidecar);
		var sidecar = NewLiveSidecar();
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, Sidecar, sidecar));

		Assert.True(MuiNativeReturnIdQueue.TryEnqueue(ref memory, Sidecar,
			First, 0x11223344));
		Assert.True(MuiNativeReturnIdQueue.TryEnqueue(ref memory, Sidecar,
			Second, 0x55667788));
		Assert.True(MuiNativeMuiObjectCodec.TryRead(ref memory, Sidecar,
			out var queuedSidecar));
		Assert.Equal(First, queuedSidecar.ReturnIdQueue);
		Assert.True(MuiNativeReturnIdQueue.Validate(ref memory, queuedSidecar));

		Assert.True(MuiNativeReturnIdQueue.TryDequeue(ref memory, Sidecar,
			out var hasValue, out var returnId, out var removed));
		Assert.True(hasValue);
		Assert.Equal(0x11223344u, returnId);
		Assert.Equal(First, removed);
		Assert.True(MuiNativeReturnIdQueue.TryDequeue(ref memory, Sidecar,
			out hasValue, out returnId, out removed));
		Assert.True(hasValue);
		Assert.Equal(0x55667788u, returnId);
		Assert.Equal(Second, removed);
		Assert.True(MuiNativeReturnIdQueue.TryDequeue(ref memory, Sidecar,
			out hasValue, out returnId, out removed));
		Assert.False(hasValue);
		Assert.Equal(0u, returnId);
		Assert.True(removed.IsNull);
		Assert.True(MuiNativeMuiObjectCodec.TryRead(ref memory, Sidecar,
			out queuedSidecar));
		Assert.True(queuedSidecar.ReturnIdQueue.IsNull);
	}

	[Fact]
	public void CorruptNativeReturnIdQueueIsRejectedWithoutMutation()
	{
		var memory = new MuiHeadlessTestPlatform(Sidecar.Raw, 0x200, 0, Sidecar);
		var sidecar = NewLiveSidecar();
		sidecar.ReturnIdQueue = First;
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, Sidecar, sidecar));
		var node = new MuiNativeReturnIdRecord
		{
			Signature = MuiNativeReturnIdRecord.Magic,
			Revision = MuiNativeReturnIdRecord.Version,
			Next = First,
			ReturnId = 7,
		};
		Assert.True(MuiNativeReturnIdRecordCodec.Write(ref memory, First, node));

		Assert.False(MuiNativeReturnIdQueue.TryDequeue(ref memory, Sidecar,
			out var hasValue, out var returnId, out var removed));
		Assert.False(hasValue);
		Assert.Equal(0u, returnId);
		Assert.True(removed.IsNull);
		Assert.True(MuiNativeMuiObjectCodec.TryRead(ref memory, Sidecar,
			out var unchanged));
		Assert.Equal(First, unchanged.ReturnIdQueue);
	}

	[Fact]
	public void ApplicationWaitTaskAndSignalMaskUseNamedSidecarFields()
	{
		var memory = new MuiHeadlessTestPlatform(Sidecar.Raw, 0x200, 0, Sidecar);
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, Sidecar,
			NewLiveSidecar()));
		var task = APTR.FromPointer(0x1B00);
		Assert.True(MuiNativeApplicationInputWaitState.TrySet(ref memory,
			Sidecar, task, 0x00000420));
		Assert.True(MuiNativeApplicationInputWaitState.TryGet(ref memory,
			Sidecar, out var storedTask, out var storedMask));
		Assert.Equal(task, storedTask);
		Assert.Equal(0x00000420u, storedMask);

		Assert.True(MuiNativeApplicationInputWaitState.TrySet(ref memory,
			Sidecar, APTR.Null, 0));
		Assert.True(MuiNativeApplicationInputWaitState.TryGet(ref memory,
			Sidecar, out storedTask, out storedMask));
		Assert.True(storedTask.IsNull);
		Assert.Equal(0u, storedMask);
	}

	private static MuiNativeMuiObjectRecord NewLiveSidecar() => new()
	{
		Signature = MuiNativeMuiObjectRecord.Magic,
		Revision = MuiNativeMuiObjectRecord.Version,
		Flags = MuiNativeMuiObjectRecord.ObjectInitialized,
		LifecycleState = MuiNativeMuiObjectRecord.StateLive,
	};
}
