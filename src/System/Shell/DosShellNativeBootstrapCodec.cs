using Amiga;

namespace CopperOS.Shell.Dos;

/// <summary>Guest-memory boundary for the native Shell task's Exec bootstrap.</summary>
public static class DosShellNativeBootstrapCodec
{
	/// <summary>Canonical public ExecBase pointer slot in Amiga low memory.</summary>
	public const uint ExecBasePointerAddress = 4;

	/// <summary>
	/// Resolves ExecBase without relying on inherited task registers. Mapping
	/// checks use the supplied memory provider; native raw memory can validate
	/// address arithmetic but cannot discover an absent physical memory region.
	/// </summary>
	public static bool TryReadExecBase<TMemory>(ref TMemory memory,
		out APTR execBase) where TMemory : struct, IAmigaGuestMemory
	{
		execBase = APTR.Null;
		var pointerSlot = APTR.FromPointer(ExecBasePointerAddress);
		if (!memory.IsMapped(pointerSlot, sizeof(uint))) return false;
		var candidate = APTR.FromPointer(memory.ReadUInt32(pointerSlot));
		if (candidate.IsNull || (candidate.Raw & 1u) != 0 ||
			candidate.Raw > uint.MaxValue - global::Amiga.ExecBase.Size ||
			!memory.IsMapped(candidate, global::Amiga.ExecBase.Size))
			return false;
		execBase = candidate;
		return true;
	}
}
