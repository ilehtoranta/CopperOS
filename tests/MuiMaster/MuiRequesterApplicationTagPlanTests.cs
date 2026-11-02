/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiRequesterApplicationTagPlanTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void RequesterTagsSatisfyNativeWindowConstructorAndTransferRootOwnership()
	{
		var p = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x5000, State);
		Assert.True(MuiHeadlessObjectCore.Initialize(ref p, State));
		p.WriteCString(APTR.FromPointer(0x1100), "Group.mui");
		p.WriteCString(APTR.FromPointer(0x1140), "Window.mui");
		p.WriteCString(APTR.FromPointer(0x1180), "Result");
		var groupClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, State,
			APTR.FromPointer(0x1100), APTR.Null, 0, APTR.FromPointer(42));
		var windowClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, State,
			APTR.FromPointer(0x1140), APTR.Null, 0, APTR.FromPointer(43));
		var missing = MuiHeadlessObjectCore.CreateObjectA(ref p, State,
			windowClass, APTR.Null);
		Assert.False(MuiNativeBuiltinWindow.ValidateConstructorRoot(ref p, State, missing));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref p, State, missing));
		var root = MuiHeadlessObjectCore.CreateObjectA(ref p, State, groupClass, APTR.Null);
		Assert.True(MuiRequesterApplicationWindowTagCore.TryCreate(ref p,
			default, root, APTR.FromPointer(0x1180), APTR.Null, out var tags));
		var window = MuiHeadlessObjectCore.CreateObjectA(ref p, State,
			windowClass, tags.Items);
		MuiRequesterApplicationTagPlanCore.Release(ref p, ref tags);
		Assert.NotEqual(APTR.Null, window);
		Assert.True(MuiNativeBuiltinWindow.ValidateConstructorRoot(ref p, State, window));
		Assert.Equal(window, MuiHeadlessObjectCore.ParentObject(ref p, State, root));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref p, State, window));
		Assert.Equal(APTR.Null, MuiHeadlessObjectCore.FindObject(ref p, State, root));
	}

	[Fact]
	public void TypedTagPlanWritesNamedItemsAndOneTerminator()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		Assert.True(MuiRequesterApplicationTagPlanCore.TryCreate(ref platform,
			3, out var plan));
		Assert.True(MuiRequesterApplicationTagPlanCore.TryAdd(ref platform,
			ref plan, MuiWindowPublicCore.Open, 0));
		Assert.True(MuiRequesterApplicationTagPlanCore.TryAdd(ref platform,
			ref plan, MuiWindowPublicCore.Title, 0x1200));
		Assert.True(MuiRequesterApplicationTagPlanCore.TryFinish(ref platform,
			ref plan));

		Assert.True(MuiAslTagItemVectorCodec.TryRead(ref platform, plan.Items,
			0, out var open));
		Assert.Equal(MuiWindowPublicCore.Open, open.Tag);
		Assert.Equal(0u, open.Data);
		Assert.True(MuiAslTagItemVectorCodec.TryRead(ref platform, plan.Items,
			1, out var title));
		Assert.Equal(MuiWindowPublicCore.Title, title.Tag);
		Assert.Equal(0x1200u, title.Data);
		Assert.True(MuiAslTagItemVectorCodec.TryRead(ref platform, plan.Items,
			2, out var done));
		Assert.Equal(MuiAslTagListCore.TagDone, done.Tag);
		Assert.Equal(3u, plan.Written);

		MuiRequesterApplicationTagPlanCore.Release(ref platform, ref plan);
		Assert.Equal(APTR.Null, plan.Items);
	}

	[Fact]
	public void TypedTagPlanRejectsCapacityOverflowAndIncompleteLists()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		Assert.True(MuiRequesterApplicationTagPlanCore.TryCreate(ref platform,
			3, out var plan));
		Assert.True(MuiRequesterApplicationTagPlanCore.TryAdd(ref platform,
			ref plan, MuiWindowPublicCore.Open, 0));
		Assert.False(MuiRequesterApplicationTagPlanCore.TryFinish(ref platform,
			ref plan));
		MuiRequesterApplicationTagPlanCore.Release(ref platform, ref plan);

		Assert.True(MuiRequesterApplicationTagPlanCore.TryCreate(ref platform,
			1, out plan));
		Assert.True(MuiRequesterApplicationTagPlanCore.TryAdd(ref platform,
			ref plan, MuiWindowPublicCore.Open, 0));
		Assert.False(MuiRequesterApplicationTagPlanCore.TryFinish(ref platform,
			ref plan));
		MuiRequesterApplicationTagPlanCore.Release(ref platform, ref plan);
	}
}
