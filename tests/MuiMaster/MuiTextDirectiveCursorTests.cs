/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiTextDirectiveCursorTests
{
	[Fact]
	public void LeadingStyleDirectiveUsesBoundedScanCursor()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var text = APTR.FromPointer(0x1300);
		platform.WriteCString(text, "\u001bs\u001bo");
		Assert.True(MuiCommonControlCore.TryReadLeadingTextStyle(ref platform,
			text, out var flags));
		Assert.Equal(MuiCustomFontSpecFlags.Shadow |
			MuiCustomFontSpecFlags.Outline, flags);
		platform.WriteCString(text, "\u001bn");
		Assert.True(MuiCommonControlCore.TryReadLeadingTextStyle(ref platform,
			text, out flags));
		Assert.Equal(0u, flags);
		Assert.False(MuiCommonControlCore.TryReadLeadingTextStyle(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void InlineColourAndImageDirectivesUseBoundedScanCursor()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var text = APTR.FromPointer(0x1380);
		platform.WriteCString(text, "\u001bP[AABBCCDD]");
		Assert.True(MuiCommonControlCore.TryReadLeadingTextInlineColor(
			ref platform, text, out var color, out var alpha, out var flags));
		Assert.Equal(0x00BBCCDDu, color);
		Assert.Equal(0xAAu, alpha);
		Assert.Equal(MuiTextInlineColorFlags.HasColor |
			MuiTextInlineColorFlags.HasAlpha, flags);

		platform.WriteCString(text, "\u001bP[AA------]");
		Assert.True(MuiCommonControlCore.TryReadLeadingTextInlineColor(
			ref platform, text, out color, out alpha, out flags));
		Assert.Equal(0u, color);
		Assert.Equal(0xAAu, alpha);
		Assert.Equal(MuiTextInlineColorFlags.HasAlpha, flags);

		platform.WriteCString(text, "\u001bI[2:112233]");
		Assert.True(MuiCommonControlCore.TryReadLeadingTextInlineImage(
			ref platform, text, out var spec));
		Assert.Equal(MuiImageSpecKind.Color, spec.Kind);
		Assert.Equal(0x00112233u, spec.Value);
		Assert.Equal(0x11u, spec.Red);
		Assert.Equal(0x22u, spec.Green);
		Assert.Equal(0x33u, spec.Blue);
	}

	[Fact]
	public void HexHelpersRejectUnmappedAndOutOfBoundRanges()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var text = APTR.FromPointer(0x1400);
		platform.WriteCString(text, "AABBCCDD");
		Assert.True(MuiCommonControlCore.TryParseHex(ref platform, text, 0, 8,
			out var value));
		Assert.Equal(0xAABBCCDDu, value);
		Assert.False(MuiCommonControlCore.TryReadHexAlphaOnly(ref platform, text,
			0, out _));
		Assert.False(MuiCommonControlCore.TryParseHex(ref platform, text, -1, 2,
			out _));
		Assert.False(MuiCommonControlCore.TryParseHex(ref platform, text, 4093,
			4, out _));
		Assert.False(MuiCommonControlCore.TryParseHex(ref platform,
			APTR.FromPointer(0x30000), 0, 2, out _));
		Assert.True(MuiCommonControlCore.TryReadInlineHexByte(ref platform, text,
			0, out value));
		Assert.Equal(0xAAu, value);
		Assert.False(MuiCommonControlCore.TryReadInlineHexByte(ref platform, text,
			4095, out _));
	}
}
