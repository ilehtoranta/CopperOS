/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiRequesterApplicationTitleTests
{
	private static readonly APTR Application = APTR.FromPointer(0x1200);
	private static readonly APTR ExplicitTitle = APTR.FromPointer(0x1240);
	private static readonly APTR ApplicationTitle = APTR.FromPointer(0x1280);

	[Fact]
	public void ExplicitTitleTakesPrecedenceWithoutQueryingApplication()
	{
		var platform = new TitlePlatform
		{
			QuerySucceeds = true,
			Title = ApplicationTitle,
		};

		Assert.True(MuiRequesterApplicationTitleCore.TryResolve(ref platform,
			Application, ExplicitTitle, out var title));
		Assert.Equal(ExplicitTitle, title);
		Assert.Equal(0u, platform.QueryCount);
	}

	[Fact]
	public void MissingTitleUsesApplicationTitle()
	{
		var platform = new TitlePlatform
		{
			QuerySucceeds = true,
			Title = ApplicationTitle,
		};

		Assert.True(MuiRequesterApplicationTitleCore.TryResolve(ref platform,
			Application, APTR.Null, out var title));
		Assert.Equal(ApplicationTitle, title);
		Assert.Equal(1u, platform.QueryCount);
		Assert.Equal(Application, platform.LastApplication);
	}

	[Fact]
	public void MissingTitleAllowsNullApplicationTitleButRejectsFailedQuery()
	{
		var platform = new TitlePlatform { QuerySucceeds = true };
		Assert.True(MuiRequesterApplicationTitleCore.TryResolve(ref platform,
			Application, APTR.Null, out var title));
		Assert.True(title.IsNull);

		platform.QuerySucceeds = false;
		Assert.False(MuiRequesterApplicationTitleCore.TryResolve(ref platform,
			Application, APTR.Null, out title));
		Assert.True(title.IsNull);
	}

	private struct TitlePlatform : IMuiRequesterApplicationTitleCapability
	{
		internal bool QuerySucceeds;
		internal APTR Title;
		internal APTR LastApplication;
		internal uint QueryCount;

		public bool TryGetApplicationTitle(APTR application, out APTR title)
		{
			LastApplication = application;
			QueryCount++;
			title = QuerySucceeds ? Title : APTR.Null;
			return QuerySucceeds;
		}
	}
}
