using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiBoopsiLifecycleMessageTests
{
	[Fact]
	public void NamedMethodAndOpSetRecordsRoundTrip()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x10000, 0x4000,
			APTR.FromPointer(0x2000));
		var method = APTR.FromPointer(0x2200);
		var opSet = APTR.FromPointer(0x2240);
		Assert.True(MuiBoopsiMethodMessageCodec.Write(ref platform, method,
			new MuiBoopsiMethodMessage { MethodId = BOOPSI.OM_DISPOSE }));
		Assert.True(MuiBoopsiMethodMessageCodec.TryRead(ref platform, method,
			out var readMethod));
		Assert.Equal(BOOPSI.OM_DISPOSE, readMethod.MethodId);

		var expected = new MuiBoopsiOpSetMessage
		{
			MethodId = BOOPSI.OM_NEW,
			Attributes = APTR.FromPointer(0x2300),
			GadgetInfo = APTR.FromPointer(0x2400),
		};
		Assert.True(MuiBoopsiOpSetMessageCodec.Write(ref platform, opSet,
			expected));
		Assert.True(MuiBoopsiOpSetMessageCodec.TryRead(ref platform, opSet,
			out var actual));
		Assert.Equal(expected.MethodId, actual.MethodId);
		Assert.Equal(expected.Attributes, actual.Attributes);
		Assert.Equal(expected.GadgetInfo, actual.GadgetInfo);
	}

	[Fact]
	public void TruncatedLifecyclePacketsFailClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x10000, 0x4000,
			APTR.FromPointer(0x2000));
		var end = APTR.FromPointer(0x10FFF);
		Assert.False(MuiBoopsiMethodMessageCodec.TryRead(ref platform, end,
			out _));
		Assert.False(MuiBoopsiOpSetMessageCodec.TryRead(ref platform, end,
			out _));
		Assert.False(MuiBoopsiMethodMessageCodec.Write(ref platform, end,
			new MuiBoopsiMethodMessage { MethodId = BOOPSI.OM_NEW }));
	}

	[Fact]
	public void LifecycleDispatcherUsesNamedRecordsForConstructionAndDisposal()
	{
		const uint stateAddress = 0x2000;
		const uint messageAddress = 0x2300;
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x5000,
			APTR.FromPointer(stateAddress));
		var name = APTR.FromPointer(0x2100);
		platform.WriteCString(name, "Lifecycle.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform,
			APTR.FromPointer(stateAddress)));
		var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(stateAddress), name, APTR.Null, 8,
			APTR.FromPointer(0xD000));
		Assert.NotEqual(APTR.Null, cls);
		var tags = APTR.FromPointer(0x2500);
		Assert.True(MuiAslTagItemCodec.Write(ref platform, tags,
			new MuiAslTagItemRecord { Tag = MuiAslTagListCore.TagDone }));
		Assert.True(MuiBoopsiOpSetMessageCodec.Write(ref platform,
			APTR.FromPointer(messageAddress), new MuiBoopsiOpSetMessage
			{
				MethodId = BOOPSI.OM_NEW,
				Attributes = tags,
				GadgetInfo = APTR.Null,
			}));
		var native = platform.NewObject(MuiHeadlessObjectCore.ClassPointer(ref platform,
			cls), APTR.Null);
		platform.DispatchResult = native.Raw;
		var constructed = MuiHeadlessDispatcher.DispatchWithLifecycle(ref platform,
			APTR.FromPointer(stateAddress), cls, cls,
			APTR.FromPointer(0x2800), APTR.FromPointer(messageAddress));
		Assert.Equal(native.Raw, constructed);
		Assert.True(MuiBoopsiMethodMessageCodec.Write(ref platform,
			APTR.FromPointer(messageAddress), new MuiBoopsiMethodMessage
			{
				MethodId = BOOPSI.OM_DISPOSE,
			}));
		platform.SuperDisposeNative = true;
		platform.DispatchResult = 0;
		Assert.Equal(0u, MuiHeadlessDispatcher.DispatchWithLifecycle(ref platform,
			APTR.FromPointer(stateAddress), cls, cls, native,
			APTR.FromPointer(messageAddress)));
		Assert.Equal(2u, platform.SuperMethodCalls);
		Assert.True(MuiHeadlessObjectCore.FindObject(ref platform,
			APTR.FromPointer(stateAddress), native).IsNull);
	}
}
