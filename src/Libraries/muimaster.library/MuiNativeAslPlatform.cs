/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Narrow native capability for the three public ASL requester vectors. The
// requester lease itself remains a named MuiAslRequestLeaseRecord owned by the
// library; this platform only borrows asl.library for the actual OS call.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeAslPlatform : IMuiAslServicePlatform
{
	internal APTR Owner;

	public bool IsMapped(APTR address, uint size) =>
		default(MuiNativeClassMemory).IsMapped(address, size);
	public byte ReadUInt8(APTR address, int offset = 0) =>
		APTR.ReadUInt8(address, offset);
	public ushort ReadUInt16(APTR address, int offset = 0) =>
		APTR.ReadUInt16(address, offset);
	public uint ReadUInt32(APTR address, int offset = 0) =>
		APTR.ReadUInt32(address, offset);
	public void WriteUInt8(APTR address, int offset, byte value) =>
		APTR.WriteUInt8(address, offset, value);
	public void WriteUInt16(APTR address, int offset, ushort value) =>
		APTR.WriteUInt16(address, offset, value);
	public void WriteUInt32(APTR address, int offset, uint value) =>
		APTR.WriteUInt32(address, offset, value);
	public void Clear(APTR address, uint size) =>
		default(MuiNativeClassMemory).Clear(address, size);
	public void Copy(APTR source, APTR destination, uint size) =>
		default(MuiNativeClassMemory).Copy(source, destination, size);
	public APTR Allocate(uint size, uint flags) =>
		Exec.AllocMem(size, (Exec.MemoryFlags)flags);
	public void Free(APTR address, uint size) => Exec.FreeMem(address, size);

	public APTR AllocateRequest(uint requestType, APTR tags)
	{
		var aslBase = OpenAsl();
		if (aslBase.IsNull) return APTR.Null;
		var requester = ASL.AllocAslRequest(requestType, tags);
		CloseAsl(aslBase);
		return requester;
	}

	public int Request(APTR requester, APTR tags)
	{
		var aslBase = OpenAsl();
		if (aslBase.IsNull) return 0;
		var result = ASL.AslRequest(requester, tags);
		CloseAsl(aslBase);
		return result;
	}

	public void FreeRequest(APTR requester)
	{
		var aslBase = OpenAsl();
		if (aslBase.IsNull) return;
		ASL.FreeAslRequest(requester);
		CloseAsl(aslBase);
	}

	private static APTR OpenAsl()
	{
		var aslBase = Exec.OpenLibraryRaw(
			CString.FromLiteral("asl.library"), 0);
		if (aslBase.IsNotNull) ASL.ASLLibraryBase = aslBase;
		return aslBase;
	}

	private static void CloseAsl(APTR aslBase)
	{
		ASL.ASLLibraryBase = APTR.Null;
		Exec.CloseLibrary(aslBase);
	}
}
