using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaBubbleTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void BubblePacketsUseNamedCreateAndDeleteFields()
	{
		var platform = CreatePlatform(out _);
		var text = APTR.FromPointer(0x1800);
		platform.WriteCString(text, "Bubble");
		var create = APTR.FromPointer(0x1900);
		Assert.True(MuiAreaBubbleMessageCodec.WriteCreate(ref platform, create,
			-11, 23, text, 7));
		Assert.True(MuiAreaBubbleMessageCodec.TryReadCreate(ref platform, create,
			out var createPacket));
		Assert.Equal(-11, createPacket.X);
		Assert.Equal(23, createPacket.Y);
		Assert.Equal(text, createPacket.Text);
		Assert.Equal(7u, createPacket.Flags);

		var delete = APTR.FromPointer(0x1A00);
		Assert.True(MuiAreaBubbleMessageCodec.WriteDelete(ref platform, delete,
			APTR.FromPointer(0x1B00)));
		Assert.True(MuiAreaBubbleMessageCodec.TryReadDelete(ref platform, delete,
			out var deletePacket));
		Assert.Equal(APTR.FromPointer(0x1B00), deletePacket.Bubble);
		Assert.False(MuiAreaBubbleMessageCodec.TryReadCreate(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
	}

	[Fact]
	public void DispatcherCreatesAndDeletesProviderOwnedBubble()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var text = APTR.FromPointer(0x1C00);
		platform.WriteCString(text, "Provider bubble");
		var bubble = APTR.FromPointer(0x1D00);
		platform.BubbleCreateSampleAvailable = true;
		platform.BubbleCreateResult = bubble;

		var create = APTR.FromPointer(0x1E00);
		Assert.True(MuiAreaBubbleMessageCodec.WriteCreate(ref platform, create,
			-4, 19, text, 3));
		Assert.Equal(bubble.Raw, MuiCommonControlDispatcher.Dispatch(ref platform,
			State, obj, create));
		Assert.Equal(obj, platform.LastBubbleCreateObject);
		Assert.Equal(-4, platform.LastBubbleCreateX);
		Assert.Equal(19, platform.LastBubbleCreateY);
		Assert.Equal(text, platform.LastBubbleCreateText);
		Assert.Equal(3u, platform.LastBubbleCreateFlags);

		platform.BubbleDeleteSampleAvailable = true;
		var delete = APTR.FromPointer(0x1F00);
		Assert.True(MuiAreaBubbleMessageCodec.WriteDelete(ref platform, delete,
			bubble));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, delete));
		Assert.Equal(bubble, platform.LastBubbleDeleted);
	}

	[Fact]
	public void BubbleCreationDeclinesUnmappedTextBeforeProviderCall()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		platform.BubbleCreateSampleAvailable = true;
		platform.BubbleCreateResult = APTR.FromPointer(0x1D00);
		var result = MuiAreaBubblePacketCore.Create(ref platform, State, obj, 1, 2,
			APTR.FromPointer(0x21000u), 0);
		Assert.Equal(0u, result.Raw);
		Assert.Equal(APTR.Null, platform.LastBubbleCreateObject);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR areaClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Text.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		areaClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
