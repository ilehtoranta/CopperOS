/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
namespace CopperOS.Locale;
public static class LocaleResidentMetadata
{
    public const string DevelopmentNameText = "locale.library";
    public const ushort DevelopmentVersion = LocaleNativeLibrary.Version, DevelopmentRevision = LocaleNativeLibrary.Revision;
    public const int VectorStride = 6;
}
