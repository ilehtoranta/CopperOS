using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationLegacyMenuTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint ApplicationMenu = 0x80420E1Fu;
	private const uint ApplicationMenustrip = 0x804252D9u;

	[Fact]
	public void ObsoleteApplicationMenuDispatchUsesTypedMenustripState()
	{
		var platform = CreatePlatform(out var application, out var menustrip);
		var packet = APTR.FromPointer(0x1400);
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, ApplicationMenu, menustrip.Raw));

		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationMenustrip(
			ref platform, State, application, packet));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationObjectState(
			ref platform, State, application, out var objectState));
		Assert.Equal(menustrip, objectState.Menustrip);
		Assert.Equal(application, MuiHeadlessObjectCore.ParentObject(ref platform,
			State, menustrip));

		// Both the obsolete alias and its replacement project the same named
		// record; no second raw relationship is created for the alias.
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, ApplicationMenu, out var legacyValue));
		Assert.Equal(menustrip.Raw, legacyValue);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, ApplicationMenustrip, out var replacementValue));
		Assert.Equal(menustrip.Raw, replacementValue);
	}

	[Fact]
	public void ObsoleteApplicationMenuRemainsInitializerOnlyAfterApplicationInit()
	{
		var platform = CreatePlatform(out var application, out var menustrip);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		var packet = APTR.FromPointer(0x1480);
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, ApplicationMenu, menustrip.Raw));

		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationMenustrip(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, ApplicationMenu, out var value));
		Assert.Equal(0u, value);
		Assert.True(MuiHeadlessObjectCore.ParentObject(ref platform, State,
			menustrip).IsNull);
	}

	[Fact]
	public void ObsoleteApplicationMenuCreationTagBuildsTheSameFamilyEdge()
	{
		var platform = CreatePlatform(out _, out var menustrip);
		var applicationName = APTR.FromPointer(0x1100);
		var applicationClass = MuiHeadlessObjectCore.FindClassByName(ref platform,
			State, applicationName);
		var tags = APTR.FromPointer(0x1500);
		platform.WriteUInt32(tags, 0, ApplicationMenu);
		platform.WriteUInt32(tags, 4, menustrip.Raw);
		platform.WriteUInt32(tags, 8, 0);

		var application = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			applicationClass, tags);
		Assert.True(application.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, ApplicationMenu, out var legacyValue));
		Assert.Equal(menustrip.Raw, legacyValue);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, ApplicationMenustrip, out var replacementValue));
		Assert.Equal(menustrip.Raw, replacementValue);
		Assert.Equal(application, MuiHeadlessObjectCore.ParentObject(ref platform,
			State, menustrip));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR application,
		out APTR menustrip)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			State);
		var applicationName = APTR.FromPointer(0x1100);
		var menustripName = APTR.FromPointer(0x1140);
		platform.WriteCString(applicationName, "Application.mui");
		platform.WriteCString(menustripName, "Menustrip.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var applicationClass = MuiHeadlessObjectCore.RegisterBuiltinClass(
			ref platform, State, applicationName, APTR.Null, 0,
			APTR.FromPointer(1));
		var menustripClass = MuiHeadlessObjectCore.RegisterBuiltinClass(
			ref platform, State, menustripName, APTR.Null, 0,
			APTR.FromPointer(2));
		application = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			applicationClass, APTR.Null);
		menustrip = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			menustripClass, APTR.Null);
		Assert.True(application.IsNotNull);
		Assert.True(menustrip.IsNotNull);
		Assert.True(MuiMenuSpecialistCore.Attach(ref platform, State, menustrip,
			MuiMenuSpecialistClass.Menustrip).IsNotNull);
		return platform;
	}
}
