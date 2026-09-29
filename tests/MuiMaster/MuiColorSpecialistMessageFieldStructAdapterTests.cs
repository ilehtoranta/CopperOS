using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiColorSpecialistMessageFieldStructAdapterTests
{
	[Fact]
	public void FieldWritesPreservePacketSiblingsAcrossAllColorFrames()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));

		var getAddress = APTR.FromPointer(0x2400);
		var get = new MuiColorSpecialistGetMessage
		{
			MethodId = MuiColorSpecialistMessageCodec.OmGet,
			Attribute = 0x120,
			Storage = 0x3500,
		};
		Assert.True(MuiColorSpecialistMessageStructCodec.WriteGet(ref platform,
			getAddress, get));
		Assert.True(MuiColorSpecialistMessageMemoryCodec.TryWriteUInt32(
			ref platform, getAddress, MuiColorSpecialistPacketKind.Get,
			MuiColorSpecialistField.Storage, 0x3600));
		Assert.True(MuiColorSpecialistMessageStructCodec.TryReadGet(ref platform,
			getAddress, out var getDecoded));
		Assert.Equal(get.MethodId, getDecoded.MethodId);
		Assert.Equal(get.Attribute, getDecoded.Attribute);
		Assert.Equal(0x3600u, getDecoded.Storage);

		var setAddress = APTR.FromPointer(0x2420);
		var set = new MuiColorSpecialistSetMessage
		{
			MethodId = MuiColorSpecialistMessageCodec.MethodSet,
			Attribute = 0x220,
			Value = 0x456,
		};
		Assert.True(MuiColorSpecialistMessageStructCodec.WriteSet(ref platform,
			setAddress, set));
		Assert.True(MuiColorSpecialistMessageMemoryCodec.TryWriteUInt32(
			ref platform, setAddress, MuiColorSpecialistPacketKind.Set,
			MuiColorSpecialistField.Attribute, 0x221));
		Assert.True(MuiColorSpecialistMessageStructCodec.TryReadSet(ref platform,
			setAddress, out var setDecoded));
		Assert.Equal(set.MethodId, setDecoded.MethodId);
		Assert.Equal(0x221u, setDecoded.Attribute);
		Assert.Equal(set.Value, setDecoded.Value);

		var pointerAddress = APTR.FromPointer(0x2440);
		var pointer = new MuiColorSpecialistPointerMessage
		{
			MethodId = MuiColorSpecialistMessageCodec.SetColormap,
			Pointer = 0x3700,
		};
		Assert.True(MuiColorSpecialistMessageStructCodec.WritePointer(ref platform,
			pointerAddress, pointer));
		Assert.True(MuiColorSpecialistMessageMemoryCodec.TryWriteUInt32(
			ref platform, pointerAddress, MuiColorSpecialistPacketKind.Pointer,
			MuiColorSpecialistField.Pointer, 0x3800));
		Assert.True(MuiColorSpecialistMessageStructCodec.TryReadPointer(
			ref platform, pointerAddress, out var pointerDecoded));
		Assert.Equal(pointer.MethodId, pointerDecoded.MethodId);
		Assert.Equal(0x3800u, pointerDecoded.Pointer);

		var rgbAddress = APTR.FromPointer(0x2460);
		var rgb = new MuiColorSpecialistRgbMessage
		{
			MethodId = MuiColorSpecialistMessageCodec.SetRGB,
			Red = 1,
			Green = 2,
			Blue = 3,
		};
		Assert.True(MuiColorSpecialistMessageStructCodec.WriteRgb(ref platform,
			rgbAddress, rgb));
		Assert.True(MuiColorSpecialistMessageMemoryCodec.TryWriteUInt32(
			ref platform, rgbAddress, MuiColorSpecialistPacketKind.Rgb,
			MuiColorSpecialistField.Green, 0x22));
		Assert.True(MuiColorSpecialistMessageStructCodec.TryReadRgb(ref platform,
			rgbAddress, out var rgbDecoded));
		Assert.Equal(rgb.MethodId, rgbDecoded.MethodId);
		Assert.Equal(rgb.Red, rgbDecoded.Red);
		Assert.Equal(0x22u, rgbDecoded.Green);
		Assert.Equal(rgb.Blue, rgbDecoded.Blue);
	}
}
