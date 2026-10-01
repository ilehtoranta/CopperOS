/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// MUI V8+ documents these legacy vectors as direct DOS IoErr/SetIoErr aliases.
// The borrowed dos.library base exists only for the duration of one vector;
// opening and closing it is kept outside the short task-switch exclusion used
// to bind the SDK's static DOS base for the actual library call.
internal interface IMuiErrorDosPlatform
{
	APTR OpenDosLibrary();
	void CloseDosLibrary(APTR dosBase);
	int ReadIoErr(APTR dosBase);
	void WriteIoErr(APTR dosBase, int error);
}

internal static class MuiErrorVectorCore
{
	internal static int Error<TPlatform>(ref TPlatform platform)
		where TPlatform : struct, IMuiErrorDosPlatform
	{
		var dosBase = platform.OpenDosLibrary();
		if (dosBase.IsNull) return 0;
		var result = platform.ReadIoErr(dosBase);
		platform.CloseDosLibrary(dosBase);
		return result;
	}

	internal static void SetError<TPlatform>(ref TPlatform platform, int error)
		where TPlatform : struct, IMuiErrorDosPlatform
	{
		var dosBase = platform.OpenDosLibrary();
		if (dosBase.IsNull) return;
		platform.WriteIoErr(dosBase, error);
		platform.CloseDosLibrary(dosBase);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeErrorDosPlatform : IMuiErrorDosPlatform
{
	private uint _abiSlot;

	public APTR OpenDosLibrary() => Exec.OpenLibraryRaw(
		CString.FromLiteral("dos.library"), 0);

	public void CloseDosLibrary(APTR dosBase)
	{
		_ = _abiSlot;
		Exec.CloseLibrary(dosBase);
	}

	public int ReadIoErr(APTR dosBase)
	{
		_ = _abiSlot;
		Exec.Forbid();
		var previousBase = DOS.DOSLibraryBase;
		DOS.DOSLibraryBase = dosBase;
		var result = (int)DOS.IoErr();
		DOS.DOSLibraryBase = previousBase;
		Exec.Permit();
		return result;
	}

	public void WriteIoErr(APTR dosBase, int error)
	{
		_ = _abiSlot;
		Exec.Forbid();
		var previousBase = DOS.DOSLibraryBase;
		DOS.DOSLibraryBase = dosBase;
		DOS.SetIoErr((DOS.Error)error);
		DOS.DOSLibraryBase = previousBase;
		Exec.Permit();
	}
}

internal static class MuiNativeErrorGateway
{
	internal static int Error(APTR library)
	{
		_ = library;
		var platform = default(MuiNativeErrorDosPlatform);
		return MuiErrorVectorCore.Error(ref platform);
	}

	internal static void SetError(APTR library, int error)
	{
		_ = library;
		var platform = default(MuiNativeErrorDosPlatform);
		MuiErrorVectorCore.SetError(ref platform, error);
	}
}
