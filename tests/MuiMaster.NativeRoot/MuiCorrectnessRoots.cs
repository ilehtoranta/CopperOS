using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.NativeRoot;

public static class MuiCorrectnessRoots
{
	public static uint FullRangeNumericScaling()
	{
		if (!CheckScale(1000000, 1000000, 1000000, 1000000)) return 1;
		if (!CheckScale(500000, 1000000, 1000000, 500000)) return 2;
		if (!CheckScale(0x80000000, uint.MaxValue, uint.MaxValue, 0x80000000)) return 3;
		if (!CheckScale(2147483647, uint.MaxValue, 100, 49)) return 4;
		if (!CheckScale(uint.MaxValue - 1, uint.MaxValue, uint.MaxValue, uint.MaxValue - 1)) return 5;
		return 42;
	}

	private static bool CheckScale(uint offset, uint input, uint output, uint expected)
	{
		var memory = default(MuiNativeApplicationLoopTestPlatform);
		var address = APTR.FromPointer(0x0004F300);
		var record = new MuiNumericStateRecord
		{
			Magic = MuiNumericStateRecord.Cookie,
			Value = offset,
			Maximum = input,
			Default = output,
		};
		if (!MuiNumericStateRecordCodec.WriteRecord(ref memory, address, record) ||
			!MuiNumericStateRecordCodec.TryReadRecord(ref memory, address, out record)) return false;
		return MuiNumericScalingCore.ScaleOffset(record.Value, record.Maximum,
			record.Default) == expected;
	}

	public static uint RectangleClipUsesRelativeBounds()
	{
		var memory = default(MuiNativeApplicationLoopTestPlatform);
		var region = APTR.FromPointer(0x0004F300);
		var node = APTR.FromPointer(0x0004F320);
		if (!MuiNativeRectangleClipRegion.TryInitialize(ref memory, region, node,
			10, 20, 30, 40)) return 1;
		var bounds = LayersRegionCodec.ReadBounds(ref memory, region);
		var relative = LayersRegionRectangleCodec.ReadBounds(ref memory, node);
		if (bounds.MinX + relative.MinX != 10 || bounds.MinY + relative.MinY != 20 ||
			bounds.MinX + relative.MaxX != 39 || bounds.MinY + relative.MaxY != 59 ||
			LayersRegionCodec.ReadFirst(ref memory, region) != node ||
			LayersRegionRectangleCodec.ReadNext(ref memory, node).IsNotNull ||
			LayersRegionRectangleCodec.ReadPrevious(ref memory, node) !=
				LayersRegionCodec.HeadAnchor(region)) return 2;
		if (MuiNativeRectangleClipRegion.TryInitialize(ref memory, region, node,
			short.MaxValue, 0, 2, 1)) return 3;
		bounds = LayersRegionCodec.ReadBounds(ref memory, region);
		if (bounds.MinX != 10 || bounds.MinY != 20) return 4;
		return 42;
	}

	public static uint QueuedWorkPreventsWaiting()
	{
		var memory = default(MuiNativeApplicationLoopTestPlatform);
		var address = APTR.FromPointer(0x0004F400);
		var task = APTR.FromPointer(0x1234);
		var sidecar = new MuiNativeMuiObjectRecord
		{
			Signature = MuiNativeMuiObjectRecord.Magic,
			Revision = MuiNativeMuiObjectRecord.Version,
			Flags = MuiNativeMuiObjectRecord.ObjectInitialized,
			LifecycleState = MuiNativeMuiObjectRecord.StateLive,
		};
		if (!MuiNativeMuiObjectCodec.Write(ref memory, address, sidecar) ||
			!MuiNativeReturnIdQueue.TryEnqueue(ref memory, address,
				APTR.FromPointer(0x0004F500), 7) ||
			!MuiNativeReturnIdQueue.TryEnqueue(ref memory, address,
				APTR.FromPointer(0x0004F520), 9)) return 1;
		if (!MuiNativeReturnIdQueue.TryDequeue(ref memory, address,
			out var hasValue, out var id, out _) || !hasValue || id != 7 ||
			!MuiNativeApplicationInputWaitState.TryPrepare(ref memory, address,
				task, 0x420, out var mask) || mask != 0) return 2;
		if (!MuiNativeApplicationInputWaitState.TryGet(ref memory, address,
			out var wakeTask, out var wakeMask) || wakeTask != task || wakeMask != 0x420) return 3;
		if (!MuiNativeReturnIdQueue.TryDequeue(ref memory, address,
			out hasValue, out id, out _) || !hasValue || id != 9 ||
			!MuiNativeApplicationInputWaitState.TryPrepare(ref memory, address,
				task, 0x420, out mask) || mask != 0x420) return 4;
		return 42;
	}
}
