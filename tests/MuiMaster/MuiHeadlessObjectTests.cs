using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiHeadlessObjectTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint AttributeA = 0x80420001;
	private const uint AttributeB = 0x80420002;
	private const uint FixWidth = 0x8042A3F1;
	private const uint FixHeight = 0x8042A92B;
	private const uint RectangleBarTitle = 0x80426689;
	private const uint RectangleHBar = 0x8042C943;
	private const uint RectangleVBar = 0x80422204;
	private const uint Frame = 0x8042AC64;
	private const uint InputMode = 0x8042FB04;
	private const uint Background = 0x8042545B;
	private const uint Selected = 0x8042654B;
	private const uint ImageSpec = 0x804233D5;
	private const uint ImageFreeHoriz = 0x8042DA84;
	private const uint TextContents = 0x8042F8DC;
	private const uint TextPreParse = 0x8042566D;
	private const uint TextHiChar = 0x804218FF;
	private const uint TextCopy = 0x80427727;
	private const uint ControlChar = 0x8042120B;
	private const uint CycleChain = 0x80421CE7;
	private const uint CycleEntries = 0x80420629;
	private const uint RadioEntries = 0x8042B6A1;
	private const uint NumericMin = 0x8042E404;
	private const uint NumericMax = 0x8042D78A;
	private const uint NumericValue = 0x8042AE3A;
	private const uint NumericFormat = 0x804263E9;
	private const uint StringMaxLen = 0x80424984;
	private const uint MenuTitle = 0x8042A0E3;
	private const uint MenuEnabled = 0x8042ED48;
	private const uint MenuitemTitle = 0x804218BE;
	private const uint MenuitemShortcut = 0x80422030;
	private const uint MenuitemCheckit = 0x80425ACE;
	private const uint MenuitemChecked = 0x8042562A;
	private const uint MenuitemToggle = 0x80424D5C;
	private const uint MenuitemEnabled = 0x8042AE0F;
	private const uint MenuitemExclude = 0x80420BC6;
	private const uint MenuitemCommandString = 0x8042B9CC;
	private const uint UserData = 0x80420313;

	[Fact]
	public void MakeObjectChoiceVectorRejectsUnmappedEntryThroughNamedCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var className = APTR.FromPointer(0x1100);
		var label = APTR.FromPointer(0x1140);
		var parameters = APTR.FromPointer(0x1180);
		platform.WriteCString(className, "Cycle.mui");
		platform.WriteCString(label, "Cycle");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
			className, APTR.Null, 0, APTR.FromPointer(1)).IsNotNull);
		platform.WriteUInt32(parameters, 0, label.Raw);
		platform.WriteUInt32(parameters, 4, 0x21000);
		Assert.Equal(APTR.Null, MuiMakeObjectServiceCore.MakeObjectA(ref platform,
			State, MuiMakeObjectServiceCore.MUIO_Cycle, parameters));
	}

	[Fact]
	public void NewMenuCursorUsesNamedEntryBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var cursor = default(MuiNewMenuCursor);
		cursor.Base = APTR.FromPointer(0x1200);
		cursor.Index = 255;

		Assert.True(MuiNewMenuCursorCodec.TryGetEntry(ref platform, cursor,
			out var address));
		Assert.Equal(APTR.FromPointer(0x25EC), address);
		cursor.Index = 256;
		Assert.False(MuiNewMenuCursorCodec.TryGetEntry(ref platform, cursor,
			out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFF0);
		cursor.Index = 0;
		Assert.False(MuiNewMenuCursorCodec.TryGetEntry(ref platform, cursor,
			out _));
	}

	[Fact]
	public void MakeObjectAndNewMenuFieldsUseNamedBoundaries()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var parameters = APTR.FromPointer(0x1300);
		platform.WriteUInt32(parameters, 0, 0x11111111u);
		platform.WriteUInt32(parameters, 4, 0x22222222u);
		var parameterCursor = new MuiMakeObjectParameterFieldCursor
		{
			Base = parameters,
			Field = MuiMakeObjectParameterField.Second,
		};
		Assert.True(MuiMakeObjectParameterFieldCursorCodec.TryGetAddress(ref platform,
			parameterCursor, out var fieldAddress));
		Assert.Equal(APTR.FromPointer(0x1304), fieldAddress);
		Assert.True(MuiMakeObjectParameterFieldCursorCodec.TryReadUInt32(ref platform,
			parameters, MuiMakeObjectParameterField.Second, out var second));
		Assert.Equal(0x22222222u, second);
		Assert.False(MuiMakeObjectParameterFieldCursorCodec.TryReadUInt32(ref platform,
			parameters, unchecked((MuiMakeObjectParameterField)255), out _));

		var menu = APTR.FromPointer(0x1400);
		platform.WriteUInt8(menu, 0, 2);
		platform.WriteUInt8(menu, 1, 0xA5);
		platform.WriteUInt32(menu, 2, 0x12345678u);
		platform.WriteUInt32(menu, 6, 0x87654321u);
		platform.WriteUInt16(menu, 10, 0x55AA);
		platform.WriteUInt32(menu, 12, 0x01020304u);
		platform.WriteUInt32(menu, 16, 0xAABBCCDDu);
		var menuCursor = new MuiNewMenuFieldCursor
		{
			Record = menu,
			Field = MuiNewMenuField.Flags,
		};
		Assert.True(MuiNewMenuFieldCursorCodec.TryGetAddress(ref platform,
			menuCursor, out fieldAddress, out var fieldSize));
		Assert.Equal(APTR.FromPointer(0x140A), fieldAddress);
		Assert.Equal(2u, fieldSize);
		Assert.True(MuiNewMenuFieldCursorCodec.TryReadUInt32(ref platform, menu,
			MuiNewMenuField.Label, out var label));
		Assert.Equal(0x12345678u, label);
		Assert.True(MuiNewMenuFieldCursorCodec.TryReadUInt16(ref platform, menu,
			MuiNewMenuField.Flags, out var flags));
		Assert.Equal((ushort)0x55AA, flags);
		Assert.True(MuiNewMenuFieldCursorCodec.TryReadUInt8(ref platform, menu,
			MuiNewMenuField.Padding, out var padding));
		Assert.Equal((byte)0xA5, padding);
		Assert.False(MuiNewMenuFieldCursorCodec.TryReadUInt32(ref platform, menu,
			MuiNewMenuField.Flags, out _));
		Assert.False(MuiNewMenuFieldCursorCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0xFFFFFFF0u), MuiNewMenuField.UserData, out _));
	}

	[Fact]
	public void SharedClassesTagsAttributesAndLifecycleAreDeterministic()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var rootName = APTR.FromPointer(0x1100);
		var rootNameCopy = APTR.FromPointer(0x1140);
		var customName = APTR.FromPointer(0x1180);
		platform.WriteCString(rootName, "Notify.mui");
		platform.WriteCString(rootNameCopy, "Notify.mui");
		platform.WriteCString(customName, "test.custom");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var root = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			rootName, APTR.Null, 8, APTR.FromPointer(0xD001), true);
		var custom = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			customName, MuiHeadlessObjectCore.ClassPointer(ref platform, root), 12,
			APTR.FromPointer(0xD002), false);
		Assert.True(root.IsNotNull);
		Assert.True(custom.IsNotNull);
		Assert.Equal(root, MuiHeadlessObjectCore.FindClassByName(ref platform,
			State, rootNameCopy));

		var tags = APTR.FromPointer(0x1200);
		var more = APTR.FromPointer(0x1280);
		platform.WriteUInt32(tags, 0, AttributeA);
		platform.WriteUInt32(tags, 4, 10);
		platform.WriteUInt32(tags, 8, 1);
		platform.WriteUInt32(tags, 12, 0);
		platform.WriteUInt32(tags, 16, 3);
		platform.WriteUInt32(tags, 20, 1);
		platform.WriteUInt32(tags, 24, AttributeA);
		platform.WriteUInt32(tags, 28, 99);
		platform.WriteUInt32(tags, 32, 2);
		platform.WriteUInt32(tags, 36, more.Raw);
		platform.WriteUInt32(more, 0, AttributeB);
		platform.WriteUInt32(more, 4, 20);
		platform.WriteUInt32(more, 8, 0);
		platform.WriteUInt32(more, 12, 0);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, custom,
			tags);
		Assert.True(obj.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			AttributeA, out var a));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			AttributeB, out var b));
		Assert.Equal(10u, a);
		Assert.Equal(20u, b);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			AttributeA, 42, false));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			AttributeA, out a));
		Assert.Equal(42u, a);
		Assert.False(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			custom));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, obj));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			custom));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State, root));
		Assert.True(platform.FreeCount > 0);
	}

	[Fact]
	public void MalformedTagChainsFailAtomically()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Notify.mui");
		MuiHeadlessObjectCore.Initialize(ref platform, State);
		var cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		var tags = APTR.FromPointer(0x1200);
		platform.WriteUInt32(tags, 0, 2);
		platform.WriteUInt32(tags, 4, 0xFFFF0000);
		Assert.Equal(APTR.Null, MuiHeadlessObjectCore.CreateObjectA(ref platform,
			State, cl, tags));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State, cl));
	}

	[Fact]
	public void MasterLifecycleOwnsBuiltinsButOnlyDetachesExternalClasses()
	{
		var privateRoot = APTR.FromPointer(0x1080);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var builtinName = APTR.FromPointer(0x1100);
		var externalName = APTR.FromPointer(0x1140);
		var foreignClass = APTR.FromPointer(0x1300);
		platform.WriteCString(builtinName, "Notify.mui");
		platform.WriteCString(externalName, "external.mcc");
		platform.WriteUInt32(foreignClass, 0, 0xC1A55EED);
		Assert.True(MuiMasterLifecycleCore.Create(ref platform, privateRoot, State));
		Assert.Equal(State.Raw, platform.ReadUInt32(privateRoot, 0));

		var builtin = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, builtinName, APTR.Null, 8, APTR.FromPointer(0xD001));
		var external = MuiHeadlessObjectCore.RegisterExternalClass(ref platform,
			State, externalName, foreignClass, APTR.Null);
		Assert.True(builtin.IsNotNull);
		Assert.True(external.IsNotNull);
		Assert.Equal(external, MuiHeadlessObjectCore.FindClassByName(ref platform,
			State, externalName));
		Assert.True(MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			builtin, APTR.Null).IsNotNull);

		Assert.True(MuiMasterLifecycleCore.Dispose(ref platform, privateRoot));
		Assert.Equal(0u, platform.ReadUInt32(privateRoot, 0));
		Assert.Equal(0u, platform.ReadUInt32(State, 0));
		Assert.Equal(0xC1A55EEDu, platform.ReadUInt32(foreignClass, 0));
	}

	[Fact]
	public void PublicNewObjectAResolvesCaseSensitiveClassAndAppliesTags()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Text.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1)).IsNotNull);
		var tags = APTR.FromPointer(0x1200);
		platform.WriteUInt32(tags, 0, AttributeA);
		platform.WriteUInt32(tags, 4, 77);
		platform.WriteUInt32(tags, 8, MuiAslTagListCore.TagDone);
		platform.WriteUInt32(tags, 12, 0);
		var objectName = APTR.FromPointer(0x1140);
		platform.WriteCString(objectName, "text.mui");
		var obj = MuiObjectFactoryServiceCore.NewObjectA(ref platform, State,
			name, tags);
		Assert.True(obj.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			AttributeA, out var value));
		Assert.Equal(77u, value);
		Assert.Equal(APTR.Null,
			MuiObjectFactoryServiceCore.NewObjectA(ref platform, State,
				objectName, tags));
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			State, obj));
		Assert.False(MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			State, obj));
	}

	[Fact]
	public void PublicNewObjectARejectsMalformedTagsAndUnknownClasses()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Text.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1)).IsNotNull);
		var malformed = APTR.FromPointer(0x1200);
		platform.WriteUInt32(malformed, 0, MuiAslTagListCore.TagMore);
		platform.WriteUInt32(malformed, 4, malformed.Raw);
		Assert.Equal(APTR.Null,
			MuiObjectFactoryServiceCore.NewObjectA(ref platform, State, name,
				malformed));
		var unknown = APTR.FromPointer(0x1140);
		platform.WriteCString(unknown, "Unknown.mui");
		Assert.Equal(APTR.Null,
			MuiObjectFactoryServiceCore.NewObjectA(ref platform, State, unknown,
				APTR.Null));
	}

	[Fact]
	public void HeadlessCreationTagWalkerUsesNamedTagItemCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		var tags = APTR.FromPointer(0x1200);
		platform.WriteCString(name, "Text.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, name, APTR.Null, 0, APTR.FromPointer(1));
		Assert.True(classRecord.IsNotNull);

		var attribute = new MuiAslTagItemRecord
		{
			Tag = AttributeA,
			Data = 77
		};
		Assert.True(MuiAslTagItemCodec.Write(ref platform, tags, attribute));
		Assert.True(MuiAslTagItemCodec.Write(ref platform,
			APTR.FromPointer(tags.Raw + MuiAslTagItemRecord.Size),
			new MuiAslTagItemRecord { Tag = MuiAslTagListCore.TagDone }));
		Assert.True(MuiAslTagItemCodec.TryRead(ref platform, tags,
			out var decoded));
		Assert.Equal(attribute.Tag, decoded.Tag);
		Assert.Equal(attribute.Data, decoded.Data);

		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			classRecord, tags);
		Assert.True(obj.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			AttributeA, out var value));
		Assert.Equal(attribute.Data, value);
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, obj));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			classRecord));
	}

	[Fact]
	public void MakeObjectGeneratedTagsUseSharedTagItemCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		var label = APTR.FromPointer(0x1140);
		var parameters = APTR.FromPointer(0x1200);
		platform.WriteCString(name, "Text.mui");
		platform.WriteCString(label, "Button");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, name, APTR.Null, 0, APTR.FromPointer(1)).IsNotNull);
		platform.WriteUInt32(parameters, 0, label.Raw);

		var button = MuiMakeObjectServiceCore.MakeObjectA(ref platform, State,
			MuiMakeObjectServiceCore.MUIO_Button, parameters);
		Assert.True(button.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			button, Frame, out var frame));
		Assert.Equal(1u, frame);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			button, TextContents, out var contents));
		Assert.True(CStringCodec.TryEquals(ref platform,
			APTR.FromPointer(contents), label, 64, out var equal));
		Assert.True(equal);
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			button));
	}

	[Fact]
	public void PublicMakeObjectAConstructsBoundedMorphosObjectFamilies()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var textName = APTR.FromPointer(0x1100);
		var rectangleName = APTR.FromPointer(0x1140);
		var imageName = APTR.FromPointer(0x1180);
		var cycleName = APTR.FromPointer(0x12A0);
		var radioName = APTR.FromPointer(0x12C0);
		var sliderName = APTR.FromPointer(0x12E0);
		var stringName = APTR.FromPointer(0x1300);
		var numericButtonName = APTR.FromPointer(0x13B0);
		var menustripName = APTR.FromPointer(0x1440);
		var menuName = APTR.FromPointer(0x1460);
		var menuitemName = APTR.FromPointer(0x1480);
		var label = APTR.FromPointer(0x11C0);
		var hSpaceParameters = APTR.FromPointer(0x1200);
		var buttonParameters = APTR.FromPointer(0x1210);
		var labelParameters = APTR.FromPointer(0x1220);
		var checkmarkParameters = APTR.FromPointer(0x1230);
		var cycleParameters = APTR.FromPointer(0x1370);
		var radioParameters = APTR.FromPointer(0x1380);
		var sliderParameters = APTR.FromPointer(0x1390);
		var stringParameters = APTR.FromPointer(0x13A0);
		var popButtonParameters = APTR.FromPointer(0x13F0);
		var numericButtonParameters = APTR.FromPointer(0x1400);
		var numericFormat = APTR.FromPointer(0x1420);
		var menuitemParameters = APTR.FromPointer(0x14A0);
		var newMenus = APTR.FromPointer(0x1600);
		var projectTitle = APTR.FromPointer(0x1700);
		var openTitle = APTR.FromPointer(0x1710);
		var openShortcut = APTR.FromPointer(0x1720);
		var modesTitle = APTR.FromPointer(0x1730);
		var standardTitle = APTR.FromPointer(0x1740);
		var editTitle = APTR.FromPointer(0x1750);
		var quitTitle = APTR.FromPointer(0x1760);
		var quitShortcut = APTR.FromPointer(0x1770);
		var entries = APTR.FromPointer(0x1310);
		var firstEntry = APTR.FromPointer(0x1340);
		var secondEntry = APTR.FromPointer(0x1350);
		var thirdEntry = APTR.FromPointer(0x1360);
		platform.WriteCString(textName, "Text.mui");
		platform.WriteCString(rectangleName, "Rectangle.mui");
		platform.WriteCString(imageName, "Image.mui");
		platform.WriteCString(cycleName, "Cycle.mui");
		platform.WriteCString(radioName, "Radio.mui");
		platform.WriteCString(sliderName, "Slider.mui");
		platform.WriteCString(stringName, "String.mui");
		platform.WriteCString(numericButtonName, "Numericbutton.mui");
		platform.WriteCString(numericFormat, "%ld");
		platform.WriteCString(menustripName, "Menustrip.mui");
		platform.WriteCString(menuName, "Menu.mui");
		platform.WriteCString(menuitemName, "Menuitem.mui");
		platform.WriteCString(projectTitle, "Project");
		platform.WriteCString(openTitle, "Open");
		platform.WriteCString(openShortcut, "O");
		platform.WriteCString(modesTitle, "Modes");
		platform.WriteCString(standardTitle, "Standard");
		platform.WriteCString(editTitle, "Edit");
		platform.WriteCString(quitTitle, "Quit");
		platform.WriteCString(quitShortcut, "Q");
		platform.WriteCString(label, "MUI label");
		platform.WriteCString(firstEntry, "First");
		platform.WriteCString(secondEntry, "Second");
		platform.WriteCString(thirdEntry, "Third");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
			textName, APTR.Null, 0, APTR.FromPointer(1)).IsNotNull);
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
			rectangleName, APTR.Null, 0, APTR.FromPointer(2)).IsNotNull);
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
			imageName, APTR.Null, 0, APTR.FromPointer(3)).IsNotNull);
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
			cycleName, APTR.Null, 0, APTR.FromPointer(4)).IsNotNull);
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
			radioName, APTR.Null, 0, APTR.FromPointer(5)).IsNotNull);
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
			sliderName, APTR.Null, 0, APTR.FromPointer(6)).IsNotNull);
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
			stringName, APTR.Null, 0, APTR.FromPointer(7)).IsNotNull);
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
			numericButtonName, APTR.Null, 0, APTR.FromPointer(8)).IsNotNull);
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
			menustripName, APTR.Null, 0, APTR.FromPointer(9)).IsNotNull);
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
			menuName, APTR.Null, 0, APTR.FromPointer(10)).IsNotNull);
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
			menuitemName, APTR.Null, 0, APTR.FromPointer(11)).IsNotNull);
		platform.WriteUInt32(entries, 0, firstEntry.Raw);
		platform.WriteUInt32(entries, 4, secondEntry.Raw);
		platform.WriteUInt32(entries, 8, thirdEntry.Raw);
		platform.WriteUInt32(entries, 12, 0);

		platform.WriteUInt32(hSpaceParameters, 0, 17);
		var hSpace = MuiMakeObjectServiceCore.MakeObjectA(ref platform, State,
			MuiMakeObjectServiceCore.MUIO_HSpace, hSpaceParameters);
		Assert.True(hSpace.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, hSpace,
			FixWidth, out var value));
		Assert.Equal(17u, value);
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref platform, State,
			hSpace));
		platform.WriteUInt32(APTR.FromPointer(0x1240), 0, 13);
		var vSpace = MuiMakeObjectServiceCore.MakeObjectA(ref platform, State,
			MuiMakeObjectServiceCore.MUIO_VSpace, APTR.FromPointer(0x1240));
		Assert.True(vSpace.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, vSpace,
			FixHeight, out value));
		Assert.Equal(13u, value);
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref platform, State,
			vSpace));

		var hBar = MuiMakeObjectServiceCore.MakeObjectA(ref platform, State,
			MuiMakeObjectServiceCore.MUIO_HBar, hSpaceParameters);
		Assert.True(hBar.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, hBar,
			RectangleHBar, out value));
		Assert.Equal(1u, value);
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref platform, State,
			hBar));

		var vBar = MuiMakeObjectServiceCore.MakeObjectA(ref platform, State,
			MuiMakeObjectServiceCore.MUIO_VBar, APTR.FromPointer(0x1240));
		Assert.True(vBar.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, vBar,
			RectangleVBar, out value));
		Assert.Equal(1u, value);
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref platform, State,
			vBar));

		platform.WriteUInt32(APTR.FromPointer(0x1250), 0, label.Raw);
		var barTitle = MuiMakeObjectServiceCore.MakeObjectA(ref platform, State,
			MuiMakeObjectServiceCore.MUIO_BarTitle, APTR.FromPointer(0x1250));
		Assert.True(barTitle.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, barTitle,
			RectangleBarTitle, out value));
		Assert.Equal(label.Raw, value);
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref platform, State,
			barTitle));

		platform.WriteUInt32(buttonParameters, 0, label.Raw);
		var button = MuiMakeObjectServiceCore.MakeObjectA(ref platform, State,
			MuiMakeObjectServiceCore.MUIO_Button, buttonParameters);
		Assert.True(button.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, button,
			Frame, out value));
		Assert.Equal(1u, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, button,
			InputMode, out value));
		Assert.Equal(1u, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, button,
			TextContents, out value));
		Assert.True(CStringCodec.TryEquals(ref platform, APTR.FromPointer(value),
			label, 64, out var equal));
		Assert.True(equal);
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref platform, State,
			button));

		platform.WriteUInt32(labelParameters, 0, label.Raw);
		platform.WriteUInt32(labelParameters, 4, 0x00000400u | 0x00000100u | 0x41u);
		var muiLabel = MuiMakeObjectServiceCore.MakeObjectA(ref platform, State,
			MuiMakeObjectServiceCore.MUIO_Label, labelParameters);
		Assert.True(muiLabel.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, muiLabel,
			Frame, out value));
		Assert.Equal(3u, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, muiLabel,
			TextHiChar, out value));
		Assert.Equal(0x41u, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, muiLabel,
			TextCopy, out value));
		Assert.Equal(1u, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, muiLabel,
			TextPreParse, out value));
		Assert.True(CStringCodec.TryReadLength(ref platform, APTR.FromPointer(value),
			8, out var preParseLength));
		Assert.Equal(2u, preParseLength);
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref platform, State,
			muiLabel));

		platform.WriteUInt32(checkmarkParameters, 0, 1);
		var checkmark = MuiMakeObjectServiceCore.MakeObjectA(ref platform, State,
			MuiMakeObjectServiceCore.MUIO_Checkmark, checkmarkParameters);
		Assert.True(checkmark.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, checkmark,
			Selected, out value));
		Assert.Equal(1u, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, checkmark,
			ImageSpec, out value));
		Assert.Equal(15u, value);
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref platform, State,
			checkmark));

		platform.WriteUInt32(cycleParameters, 0, label.Raw);
		platform.WriteUInt32(cycleParameters, 4, entries.Raw);
		var cycle = MuiMakeObjectServiceCore.MakeObjectA(ref platform, State,
			MuiMakeObjectServiceCore.MUIO_Cycle, cycleParameters);
		Assert.True(cycle.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, cycle,
			CycleEntries, out value));
		Assert.Equal(entries.Raw, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, cycle,
			CycleChain, out value));
		Assert.Equal(1u, value);
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref platform, State,
			cycle));

		platform.WriteUInt32(radioParameters, 0, label.Raw);
		platform.WriteUInt32(radioParameters, 4, entries.Raw);
		var radio = MuiMakeObjectServiceCore.MakeObjectA(ref platform, State,
			MuiMakeObjectServiceCore.MUIO_Radio, radioParameters);
		Assert.True(radio.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, radio,
			RadioEntries, out value));
		Assert.Equal(entries.Raw, value);
		var radioChild = MuiFamilyCore.GetChild(ref platform, State, radio, 0,
			APTR.Null);
		Assert.True(radioChild.IsNotNull);
		Assert.Equal(MuiControlClass.Text,
			MuiCommonControlCore.Classify(ref platform, State, radioChild));
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref platform, State,
			radio));

		platform.WriteUInt32(sliderParameters, 0, label.Raw);
		platform.WriteUInt32(sliderParameters, 4, unchecked((uint)-10));
		platform.WriteUInt32(sliderParameters, 8, 90);
		var slider = MuiMakeObjectServiceCore.MakeObjectA(ref platform, State,
			MuiMakeObjectServiceCore.MUIO_Slider, sliderParameters);
		Assert.True(slider.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, slider,
			NumericMin, out value));
		Assert.Equal(unchecked((uint)-10), value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, slider,
			NumericMax, out value));
		Assert.Equal(90u, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, slider,
			NumericValue, out value));
		Assert.Equal(unchecked((uint)-10), value);
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref platform, State,
			slider));

		platform.WriteUInt32(stringParameters, 0, label.Raw);
		platform.WriteUInt32(stringParameters, 4, 24);
		var stringObject = MuiMakeObjectServiceCore.MakeObjectA(ref platform, State,
			MuiMakeObjectServiceCore.MUIO_String, stringParameters);
		Assert.True(stringObject.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			stringObject, StringMaxLen, out value));
		Assert.Equal(24u, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			stringObject, Frame, out value));
		Assert.Equal(4u, value);
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref platform, State,
			stringObject));

		platform.WriteUInt32(popButtonParameters, 0, 15);
		var popButton = MuiMakeObjectServiceCore.MakeObjectA(ref platform, State,
			MuiMakeObjectServiceCore.MUIO_PopButton, popButtonParameters);
		Assert.True(popButton.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			popButton, Frame, out value));
		Assert.Equal(2u, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			popButton, Background, out value));
		Assert.Equal(2u, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			popButton, ImageSpec, out value));
		Assert.Equal(15u, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			popButton, InputMode, out value));
		Assert.Equal(1u, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			popButton, ImageFreeHoriz, out value));
		Assert.Equal(0u, value);
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref platform, State,
			popButton));

		platform.WriteUInt32(numericButtonParameters, 0, label.Raw);
		platform.WriteUInt32(numericButtonParameters, 4, unchecked((uint)-5));
		platform.WriteUInt32(numericButtonParameters, 8, 95);
		platform.WriteUInt32(numericButtonParameters, 12, numericFormat.Raw);
		var numericButton = MuiMakeObjectServiceCore.MakeObjectA(ref platform,
			State, MuiMakeObjectServiceCore.MUIO_NumericButton,
			numericButtonParameters);
		Assert.True(numericButton.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			numericButton, NumericMin, out value));
		Assert.Equal(unchecked((uint)-5), value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			numericButton, NumericMax, out value));
		Assert.Equal(95u, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			numericButton, NumericValue, out value));
		Assert.Equal(unchecked((uint)-5), value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			numericButton, NumericFormat, out value));
		Assert.True(CStringCodec.TryEquals(ref platform, APTR.FromPointer(value),
			numericFormat, 64, out equal));
		Assert.True(equal);
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref platform, State,
			numericButton));

		platform.WriteUInt32(menuitemParameters, 0, label.Raw);
		platform.WriteUInt32(menuitemParameters, 4, openShortcut.Raw);
		platform.WriteUInt32(menuitemParameters, 8, 0x00000129);
		platform.WriteUInt32(menuitemParameters, 12, 0xCAFE);
		var directMenuitem = MuiMakeObjectServiceCore.MakeObjectA(ref platform,
			State, MuiMakeObjectServiceCore.MUIO_Menuitem, menuitemParameters);
		Assert.True(directMenuitem.IsNotNull);
		Assert.Equal(MuiMenuSpecialistClass.Menuitem,
			MuiMenuSpecialistCore.Classify(ref platform, State, directMenuitem));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			directMenuitem, MenuitemCheckit, out value));
		Assert.Equal(1u, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			directMenuitem, MenuitemChecked, out value));
		Assert.Equal(1u, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			directMenuitem, MenuitemToggle, out value));
		Assert.Equal(1u, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			directMenuitem, MenuitemCommandString, out value));
		Assert.Equal(1u, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			directMenuitem, UserData, out value));
		Assert.Equal(0xCAFEu, value);
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref platform, State,
			directMenuitem));
		var copyLabel = APTR.FromPointer(0x1780);
		var copyShortcut = APTR.FromPointer(0x1790);
		var expectedLabel = APTR.FromPointer(0x17A0);
		var expectedShortcut = APTR.FromPointer(0x17B0);
		platform.WriteCString(copyLabel, "Copied label");
		platform.WriteCString(copyShortcut, "C");
		platform.WriteCString(expectedLabel, "Copied label");
		platform.WriteCString(expectedShortcut, "C");
		platform.WriteUInt32(menuitemParameters, 0, copyLabel.Raw);
		platform.WriteUInt32(menuitemParameters, 4, copyShortcut.Raw);
		platform.WriteUInt32(menuitemParameters, 8, 0x40000129);
		var copiedMenuitem = MuiMakeObjectServiceCore.MakeObjectA(ref platform,
			State, MuiMakeObjectServiceCore.MUIO_Menuitem, menuitemParameters);
		Assert.True(copiedMenuitem.IsNotNull);
		Assert.True(MuiMenuSpecialistCore.CopyStringsFlag(ref platform, State,
			copiedMenuitem));
		platform.WriteCString(copyLabel, "caller changed");
		platform.WriteCString(copyShortcut, "X");
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			copiedMenuitem, MenuitemTitle, out value));
		Assert.True(CStringCodec.TryEquals(ref platform, APTR.FromPointer(value),
			expectedLabel, 64, out equal) && equal);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			copiedMenuitem, MenuitemShortcut, out value));
		Assert.True(CStringCodec.TryEquals(ref platform, APTR.FromPointer(value),
			expectedShortcut, 64, out equal) && equal);
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref platform, State,
			copiedMenuitem));
		platform.WriteUInt32(menuitemParameters, 8, 0x00000129);

		WriteNewMenu(ref platform, newMenus, 1, projectTitle, APTR.Null, 0, 0,
			0x111);
		WriteNewMenu(ref platform, APTR.FromPointer(newMenus.Raw + 20), 2,
			openTitle, openShortcut, 0, 0, 0x222);
		WriteNewMenu(ref platform, APTR.FromPointer(newMenus.Raw + 40), 2,
			modesTitle, APTR.Null, 0x00000129, 0xFFFFFFFE, 0x333);
		WriteNewMenu(ref platform, APTR.FromPointer(newMenus.Raw + 60), 3,
			standardTitle, APTR.Null, 0x00000001, 0xFFFFFFFD, 0x444);
		WriteNewMenu(ref platform, APTR.FromPointer(newMenus.Raw + 80), 2,
			APTR.FromPointer(0xFFFFFFFF), APTR.Null, 0, 0, 0x555);
		WriteNewMenu(ref platform, APTR.FromPointer(newMenus.Raw + 100), 1,
			editTitle, APTR.Null, 0, 0, 0x666);
		WriteNewMenu(ref platform, APTR.FromPointer(newMenus.Raw + 120), 2,
			quitTitle, quitShortcut, 0x0010, 0, 0x777);
		WriteNewMenu(ref platform, APTR.FromPointer(newMenus.Raw + 140), 0,
			APTR.Null, APTR.Null, 0, 0, 0);
		var menuParameters = APTR.FromPointer(0x14C0);
		platform.WriteUInt32(menuParameters, 0, newMenus.Raw);
		platform.WriteUInt32(menuParameters, 4, 0);
		var menuStrip = MuiMakeObjectServiceCore.MakeObjectA(ref platform, State,
			MuiMakeObjectServiceCore.MUIO_MenustripNM, menuParameters);
		Assert.True(menuStrip.IsNotNull);
		// MUI_MakeObjectA materializes the menu family with its specialist
		// sidecars attached; callers can dispatch menu methods immediately.
		Assert.Equal(MuiMenuSpecialistClass.Menustrip,
			MuiMenuSpecialistCore.Classify(ref platform, State, menuStrip));
		var projectMenu = MuiFamilyCore.GetChild(ref platform, State, menuStrip, 0,
			APTR.Null);
		var editMenu = MuiFamilyCore.GetChild(ref platform, State, menuStrip, 1,
			APTR.Null);
		Assert.True(projectMenu.IsNotNull);
		Assert.True(editMenu.IsNotNull);
		Assert.Equal(MuiMenuSpecialistClass.Menu,
			MuiMenuSpecialistCore.Classify(ref platform, State, projectMenu));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			projectMenu, MenuTitle, out value));
		Assert.True(CStringCodec.TryEquals(ref platform, APTR.FromPointer(value),
			projectTitle, 64, out equal));
		Assert.True(equal);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			projectMenu, MenuEnabled, out value));
		Assert.Equal(1u, value);
		var modesMenuitem = MuiFamilyCore.GetChild(ref platform, State, projectMenu,
			1, APTR.Null);
		Assert.True(modesMenuitem.IsNotNull);
		Assert.Equal(MuiMenuSpecialistClass.Menuitem,
			MuiMenuSpecialistCore.Classify(ref platform, State, modesMenuitem));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			modesMenuitem, MenuitemChecked, out value));
		Assert.Equal(1u, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			modesMenuitem, MenuitemExclude, out value));
		Assert.Equal(0xFFFFFFFEu, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			modesMenuitem, UserData, out value));
		Assert.Equal(0x333u, value);
		var subMenuitem = MuiFamilyCore.GetChild(ref platform, State,
			modesMenuitem, 0, APTR.Null);
		Assert.True(subMenuitem.IsNotNull);
		var separator = MuiFamilyCore.GetChild(ref platform, State, projectMenu, 2,
			APTR.Null);
		Assert.True(separator.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			separator, MenuitemTitle, out value));
		Assert.Equal(0xFFFFFFFFu, value);
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref platform, State,
			menuStrip));
		Assert.False(MuiMenuSpecialistCore.Valid(ref platform, State, menuStrip));
		Assert.False(MuiMenuSpecialistCore.Valid(ref platform, State, projectMenu));

		Assert.Equal(APTR.Null, MuiMakeObjectServiceCore.MakeObjectA(ref platform,
			State, 13, APTR.Null));
		Assert.Equal(APTR.Null, MuiMakeObjectServiceCore.MakeObjectA(ref platform,
			State, MuiMakeObjectServiceCore.MUIO_Button, APTR.FromPointer(0x20FFE)));
	}

	private static void WriteNewMenu(ref MuiHeadlessTestPlatform platform,
		APTR address, byte type, APTR label, APTR shortcut, ushort flags,
		uint mutualExclude, uint userData)
	{
		platform.WriteUInt8(address, 0, type);
		platform.WriteUInt8(address, 1, 0);
		platform.WriteUInt32(address, 2, label.Raw);
		platform.WriteUInt32(address, 6, shortcut.Raw);
		platform.WriteUInt16(address, 10, flags);
		platform.WriteUInt32(address, 12, mutualExclude);
		platform.WriteUInt32(address, 16, userData);
	}

	[Fact]
	public void ClassServiceObjectFactoryKeepsExternalLeaseUntilDisposal()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var serviceState = APTR.FromPointer(0x1080);
		var classId = APTR.FromPointer(0x1300);
		var libraryName = APTR.FromPointer(0x1340);
		var tags = APTR.FromPointer(0x1380);
		platform.WriteCString(classId, "Foo.mcc");
		platform.WriteCString(libraryName, "mui/Foo.mcc");
		platform.LoadableLibraryName = libraryName;
		platform.LoadableLibraryBase = APTR.FromPointer(0x2000);
		platform.LoadablePublicClassId = classId;
		platform.LoadablePublicClass = APTR.FromPointer(0x2100);
		platform.WriteUInt32(tags, 0, AttributeA);
		platform.WriteUInt32(tags, 4, 88);
		platform.WriteUInt32(tags, 8, MuiAslTagListCore.TagDone);
		platform.WriteUInt32(tags, 12, 0);
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		Assert.True(MuiClassServiceCore.Initialize(ref platform, serviceState,
			State));

		var obj = MuiObjectFactoryServiceCore.NewObjectAWithClassService(
			ref platform, serviceState, State, classId, tags);
		Assert.True(obj.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			AttributeA, out var value));
		Assert.Equal(88u, value);
		Assert.Equal(1u, MuiClassServiceCore.ReferenceCount(ref platform,
			serviceState, platform.LoadablePublicClass));
		Assert.Equal(1u, MuiClassServiceCore.ObjectLeaseCount(ref platform,
			serviceState, platform.LoadablePublicClass));
		Assert.False(MuiClassServiceCore.FreeClass(ref platform, serviceState,
			platform.LoadablePublicClass));
		Assert.Equal(0u, platform.CloseLibraryCount);

		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			serviceState, State, obj));
		Assert.Equal(0u, MuiClassServiceCore.ReferenceCount(ref platform,
			serviceState, platform.LoadablePublicClass));
		Assert.Equal(0u, MuiClassServiceCore.ObjectLeaseCount(ref platform,
			serviceState, platform.LoadablePublicClass));
		Assert.Equal(1u, platform.CloseLibraryCount);
		Assert.False(MuiHeadlessObjectCore.FindClassByName(ref platform, State,
			classId).IsNotNull);
	}

	[Fact]
	public void ClassServiceObjectFactoryBalancesMultipleExternalObjects()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var serviceState = APTR.FromPointer(0x1080);
		var classId = APTR.FromPointer(0x1300);
		var libraryName = APTR.FromPointer(0x1340);
		platform.WriteCString(classId, "Foo.mcc");
		platform.WriteCString(libraryName, "mui/Foo.mcc");
		platform.LoadableLibraryName = libraryName;
		platform.LoadableLibraryBase = APTR.FromPointer(0x2000);
		platform.LoadablePublicClassId = classId;
		platform.LoadablePublicClass = APTR.FromPointer(0x2100);
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		Assert.True(MuiClassServiceCore.Initialize(ref platform, serviceState,
			State));
		var first = MuiObjectFactoryServiceCore.NewObjectAWithClassService(
			ref platform, serviceState, State, classId, APTR.Null);
		var second = MuiObjectFactoryServiceCore.NewObjectAWithClassService(
			ref platform, serviceState, State, classId, APTR.Null);
		Assert.True(first.IsNotNull);
		Assert.True(second.IsNotNull);
		var classPointer = platform.LoadablePublicClass;
		Assert.Equal(2u, MuiClassServiceCore.ReferenceCount(ref platform,
			serviceState, classPointer));
		Assert.Equal(2u, MuiClassServiceCore.ObjectLeaseCount(ref platform,
			serviceState, classPointer));
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			serviceState, State, first));
		Assert.Equal(1u, MuiClassServiceCore.ReferenceCount(ref platform,
			serviceState, classPointer));
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			serviceState, State, second));
		Assert.Equal(1u, platform.CloseLibraryCount);
		Assert.Equal(0u, MuiClassServiceCore.ReferenceCount(ref platform,
			serviceState, classPointer));
	}
}
