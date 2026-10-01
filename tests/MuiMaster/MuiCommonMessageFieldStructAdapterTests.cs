using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiCommonMessageFieldStructAdapterTests
{
	[Fact]
	public void FieldWritesPreserveCommonPacketSiblings()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));

		var signedAddress = APTR.FromPointer(0x2400);
		var signed = new MuiCommonSignedValueMessage
		{
			MethodId = 0x804243A7,
			Value = -7,
		};
		Assert.True(MuiCommonMessageStructCodec.WriteSigned(ref platform,
			signedAddress, signed));
		Assert.True(MuiCommonMessageMemoryCodec.TryWriteUInt32(ref platform,
			signedAddress, MuiCommonPacketKind.Signed, MuiCommonField.Value, 99));
		Assert.True(MuiCommonMessageStructCodec.TryReadSigned(ref platform,
			signedAddress, out var signedDecoded));
		Assert.Equal(signed.MethodId, signedDecoded.MethodId);
		Assert.Equal(99, signedDecoded.Value);

		var methodAddress = APTR.FromPointer(0x2410);
		Assert.True(MuiCommonMethodMessageHeaderCodec.WriteValue(ref platform,
			methodAddress, 0x80420001));
		Assert.True(MuiCommonMessageMemoryCodec.TryWriteUInt32(ref platform,
			methodAddress, MuiCommonPacketKind.Method, MuiCommonField.MethodId,
			0x80420002));
		Assert.True(MuiCommonMessageMemoryCodec.TryReadUInt32(ref platform,
			methodAddress, MuiCommonPacketKind.Method, MuiCommonField.MethodId,
			out var methodDecoded));
		Assert.Equal(0x80420002u, methodDecoded);

		var scaleAddress = APTR.FromPointer(0x2420);
		var scale = new MuiCommonScaleToValueMessage
		{
			MethodId = 0x8042032C,
			Min = -100,
			Max = 100,
			Value = 3,
		};
		Assert.True(MuiCommonMessageStructCodec.WriteScaleToValue(ref platform,
			scaleAddress, scale));
		Assert.True(MuiCommonMessageMemoryCodec.TryWriteUInt32(ref platform,
			scaleAddress, MuiCommonPacketKind.ScaleToValue, MuiCommonField.Max,
			200));
		Assert.True(MuiCommonMessageStructCodec.TryReadScaleToValue(ref platform,
			scaleAddress, out var scaleDecoded));
		Assert.Equal(scale.Min, scaleDecoded.Min);
		Assert.Equal(200, scaleDecoded.Max);
		Assert.Equal(scale.Value, scaleDecoded.Value);

		var valueToScaleAddress = APTR.FromPointer(0x2430);
		var valueToScale = new MuiCommonValueToScaleMessage
		{
			MethodId = 0x8042032D,
			Min = -10,
			Max = 10,
		};
		Assert.True(MuiCommonMessageStructCodec.WriteValueToScale(ref platform,
			valueToScaleAddress, valueToScale));
		Assert.True(MuiCommonMessageMemoryCodec.TryWriteUInt32(ref platform,
			valueToScaleAddress, MuiCommonPacketKind.ValueToScale,
			MuiCommonField.Min, unchecked((uint)-20)));
		Assert.True(MuiCommonMessageStructCodec.TryReadValueToScale(ref platform,
			valueToScaleAddress, out var valueToScaleDecoded));
		Assert.Equal(-20, valueToScaleDecoded.Min);
		Assert.Equal(valueToScale.Max, valueToScaleDecoded.Max);

		var stringifyAddress = APTR.FromPointer(0x2440);
		var stringify = new MuiCommonStringifyMessage
		{
			MethodId = 0x8042032E,
			Value = -12,
		};
		Assert.True(MuiCommonMessageStructCodec.WriteStringify(ref platform,
			stringifyAddress, stringify));
		Assert.True(MuiCommonMessageMemoryCodec.TryWriteUInt32(ref platform,
			stringifyAddress, MuiCommonPacketKind.Stringify, MuiCommonField.Value,
			33));
		Assert.True(MuiCommonMessageStructCodec.TryReadStringify(ref platform,
			stringifyAddress, out var stringifyDecoded));
		Assert.Equal(33, stringifyDecoded.Value);

		var getAddress = APTR.FromPointer(0x2450);
		var get = new MuiCommonGetMessage
		{
			MethodId = 0x8042032F,
			Attribute = 0x80420010,
			Storage = 0x3600,
		};
		Assert.True(MuiCommonMessageStructCodec.WriteGet(ref platform, getAddress,
			get));
		Assert.True(MuiCommonMessageMemoryCodec.TryWriteUInt32(ref platform,
			getAddress, MuiCommonPacketKind.Get, MuiCommonField.Storage, 0x3700));
		Assert.True(MuiCommonMessageStructCodec.TryReadGet(ref platform, getAddress,
			out var getDecoded));
		Assert.Equal(get.MethodId, getDecoded.MethodId);
		Assert.Equal(get.Attribute, getDecoded.Attribute);
		Assert.Equal(0x3700u, getDecoded.Storage);

		var attributeAddress = APTR.FromPointer(0x2460);
		var attribute = new MuiCommonAttributeMessage
		{
			MethodId = 0x80420330,
			Attribute = 0x80420011,
			Value = 1,
		};
		Assert.True(MuiCommonMessageStructCodec.WriteAttribute(ref platform,
			attributeAddress, attribute));
		Assert.True(MuiCommonMessageMemoryCodec.TryWriteUInt32(ref platform,
			attributeAddress, MuiCommonPacketKind.Attribute, MuiCommonField.Value,
			2));
		Assert.True(MuiCommonMessageStructCodec.TryReadAttribute(ref platform,
			attributeAddress, out var attributeDecoded));
		Assert.Equal(attribute.MethodId, attributeDecoded.MethodId);
		Assert.Equal(attribute.Attribute, attributeDecoded.Attribute);
		Assert.Equal(2u, attributeDecoded.Value);

		var askMinMaxAddress = APTR.FromPointer(0x2470);
		var askMinMax = new MuiCommonAskMinMaxMessage
		{
			MethodId = 0x80420331,
			Storage = 0x3800,
		};
		Assert.True(MuiCommonMessageStructCodec.WriteAskMinMax(ref platform,
			askMinMaxAddress, askMinMax));
		Assert.True(MuiCommonMessageMemoryCodec.TryWriteUInt32(ref platform,
			askMinMaxAddress, MuiCommonPacketKind.AskMinMax,
			MuiCommonField.Storage, 0x3900));
		Assert.True(MuiCommonMessageStructCodec.TryReadAskMinMax(ref platform,
			askMinMaxAddress, out var askMinMaxDecoded));
		Assert.Equal(askMinMax.MethodId, askMinMaxDecoded.MethodId);
		Assert.Equal(0x3900u, askMinMaxDecoded.Storage);

		var eventAddress = APTR.FromPointer(0x2480);
		var handleEvent = new MuiCommonHandleEventMessage
		{
			MethodId = 0x80426D66,
			InputMessage = 0x3500,
			MuiKey = -4,
			EventHandlerNode = 0x3600,
		};
		Assert.True(MuiCommonMessageStructCodec.WriteHandleEvent(ref platform,
			eventAddress, handleEvent));
		Assert.True(MuiCommonMessageMemoryCodec.TryWriteUInt32(ref platform,
			eventAddress, MuiCommonPacketKind.HandleEvent, MuiCommonField.MuiKey,
			unchecked((uint)8)));
		Assert.True(MuiCommonMessageStructCodec.TryReadHandleEvent(ref platform,
			eventAddress, out var eventDecoded));
		Assert.Equal(handleEvent.InputMessage, eventDecoded.InputMessage);
		Assert.Equal(8, eventDecoded.MuiKey);
		Assert.Equal(handleEvent.EventHandlerNode, eventDecoded.EventHandlerNode);

		var layoutAddress = APTR.FromPointer(0x24C0);
		var layout = new MuiLayoutMessage
		{
			MethodId = MuiLayoutPacketCore.Layout,
			Left = 1,
			Top = 2,
			Width = 30,
			Height = 40,
			Flags = 5,
		};
		Assert.True(MuiLayoutMessageStructCodec.WriteLayout(ref platform,
			layoutAddress, layout));
		Assert.True(MuiCommonMessageMemoryCodec.TryWriteUInt32(ref platform,
			layoutAddress, MuiCommonPacketKind.Layout, MuiCommonField.Width, 300));
		Assert.True(MuiLayoutMessageStructCodec.TryReadLayout(ref platform,
			layoutAddress, out var layoutDecoded));
		Assert.Equal(layout.Left, layoutDecoded.Left);
		Assert.Equal(300u, layoutDecoded.Width);
		Assert.Equal(layout.Height, layoutDecoded.Height);
		Assert.Equal(layout.Flags, layoutDecoded.Flags);

		var flagsAddress = APTR.FromPointer(0x2500);
		var flags = new MuiLayoutFlagsMessage
		{
			MethodId = MuiLayoutPacketCore.Draw,
			Flags = 0x11,
		};
		Assert.True(MuiLayoutMessageStructCodec.WriteFlags(ref platform,
			flagsAddress, flags));
		Assert.True(MuiCommonMessageMemoryCodec.TryWriteUInt32(ref platform,
			flagsAddress, MuiCommonPacketKind.Flags, MuiCommonField.Flags, 0x22));
		Assert.True(MuiLayoutMessageStructCodec.TryReadFlags(ref platform,
			flagsAddress, out var flagsDecoded));
		Assert.Equal(flags.MethodId, flagsDecoded.MethodId);
		Assert.Equal(0x22u, flagsDecoded.Flags);

		var renderInfoAddress = APTR.FromPointer(0x2510);
		var renderInfo = new MuiLayoutRenderInfoMessage
		{
			MethodId = MuiLayoutPacketCore.Setup,
			RenderInfo = 0x4000,
		};
		Assert.True(MuiLayoutMessageStructCodec.WriteRenderInfo(ref platform,
			renderInfoAddress, renderInfo));
		Assert.True(MuiCommonMessageMemoryCodec.TryWriteUInt32(ref platform,
			renderInfoAddress, MuiCommonPacketKind.RenderInfo,
			MuiCommonField.RenderInfo, 0x4100));
		Assert.True(MuiLayoutMessageStructCodec.TryReadRenderInfo(ref platform,
			renderInfoAddress, out var renderInfoDecoded));
		Assert.Equal(renderInfo.MethodId, renderInfoDecoded.MethodId);
		Assert.Equal(0x4100u, renderInfoDecoded.RenderInfo);

		Assert.False(MuiCommonMessageMemoryCodec.TryWriteUInt32(ref platform,
			layoutAddress, MuiCommonPacketKind.Layout, MuiCommonField.Storage, 1));
		Assert.False(MuiCommonMessageMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x21000), MuiCommonPacketKind.Get,
			MuiCommonField.Storage, out _));
	}
}
