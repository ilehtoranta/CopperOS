/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiExternalWrapperMessageStructCodecTests
{
	[Fact]
	public void FixedWrapperPacketsRoundTripThroughSequentialRecords()
	{
		Assert.Equal(16, Unsafe.SizeOf<MuiExternalUpdateMessage>());
		Assert.Equal(12, Unsafe.SizeOf<MuiExternalGetMessage>());
		Assert.Equal(12, Unsafe.SizeOf<MuiExternalSetMessage>());
		Assert.Equal(4, Unsafe.SizeOf<MuiExternalMethodMessage>());
		Assert.Equal(8, Unsafe.SizeOf<MuiExternalRenderInfoMessage>());
		Assert.Equal(8, Unsafe.SizeOf<MuiExternalAskMinMaxMessage>());
		Assert.Equal(20, Unsafe.SizeOf<MuiExternalLayoutMessage>());

		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x10000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2600);
		Assert.True(MuiExternalWrapperMessageStructCodec.TryWriteUpdate(
			ref platform, address, 0x2700, 0x2800, 3));
		Assert.True(MuiExternalWrapperMessageStructCodec.TryReadUpdate(
			ref platform, address, out var update));
		Assert.Equal(0x2700u, update.AttributeList);
		Assert.Equal(0x2800u, update.GadgetInfo);
		Assert.Equal(3u, update.Flags);

		Assert.True(MuiExternalWrapperMessageStructCodec.TryWriteGet(ref platform,
			address, 7, 0x2860));
		Assert.True(MuiExternalWrapperMessageStructCodec.TryReadGet(ref platform,
			address, out var get));
		Assert.Equal(7u, get.Attribute);
		Assert.Equal(0x2860u, get.Storage);

		Assert.True(MuiExternalWrapperMessageStructCodec.TryWriteSet(ref platform,
			address, MuiExternalWrapperMessageCodec.MethodSet, 9, 11));
		Assert.True(MuiExternalWrapperMessageStructCodec.TryReadSet(ref platform,
			address, out var set));
		Assert.Equal(9u, set.Attribute);
		Assert.Equal(11u, set.Value);

		Assert.True(MuiExternalWrapperMessageStructCodec.TryWriteMethod(
			ref platform, address, MuiExternalWrapperMessageCodec.Draw));
		Assert.True(MuiExternalWrapperMessageStructCodec.TryReadMethodIdValue(
			ref platform, address, out var method));
		Assert.Equal(MuiExternalWrapperMessageCodec.Draw, method);

		Assert.True(MuiExternalWrapperMessageStructCodec.TryWriteRenderInfo(
			ref platform, address, MuiExternalWrapperMessageCodec.Setup, 0x2400));
		Assert.True(MuiExternalWrapperMessageStructCodec.TryReadRenderInfo(
			ref platform, address, out var renderInfo));
		Assert.Equal(0x2400u, renderInfo.RenderInfo);

		Assert.True(MuiExternalWrapperMessageStructCodec.TryWriteAskMinMax(
			ref platform, address, 0x2440));
		Assert.True(MuiExternalWrapperMessageStructCodec.TryReadAskMinMax(
			ref platform, address, out var askMinMax));
		Assert.Equal(0x2440u, askMinMax.Storage);

		Assert.True(MuiExternalWrapperMessageStructCodec.TryWriteLayout(
			ref platform, address, 1, 2, 80, 40));
		Assert.True(MuiExternalWrapperMessageStructCodec.TryReadLayout(
			ref platform, address, out var layout));
		Assert.Equal(1u, layout.Left);
		Assert.Equal(2u, layout.Top);
		Assert.Equal(80u, layout.Width);
		Assert.Equal(40u, layout.Height);
	}

	[Fact]
	public void FixedWrapperPacketsRejectIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x10000,
			APTR.FromPointer(0x1000));
		var crossingEnd = APTR.FromPointer(0x40FF5);
		Assert.False(MuiExternalWrapperMessageStructCodec.TryWriteLayout(
			ref platform, crossingEnd, 1, 2, 3, 4));
		Assert.False(MuiExternalWrapperMessageStructCodec.TryReadUpdate(
			ref platform, crossingEnd, out _));
		Assert.False(MuiExternalWrapperMessageStructCodec.TryReadMethodIdValue(
			ref platform, APTR.Null, out _));
	}
}
