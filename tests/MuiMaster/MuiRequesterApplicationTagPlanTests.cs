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
