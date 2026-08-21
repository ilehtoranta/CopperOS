/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// MorphOS exposes MUIA_Version and MUIA_Revision as class-owned getter
// values. Keep the consumer-facing shape named and fixed-width; the class
// registry owns the compact guest representation used to publish it.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiClassVersionMetadata
{
	internal const uint MaximumValue = 0x0FFF;
	internal uint Version;
	internal uint Revision;
}

