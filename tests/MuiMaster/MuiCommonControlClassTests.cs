using System.Text;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

// Collective and per-family qualification of every active MG07 common control:
// construction attributes, set/get, methods, notifications, layout, input,
// redraw, disabled state, and disposal. No MorphOS compatibility is advertised.
public sealed class MuiCommonControlClassTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	// Attribute identifiers (private semantic values matching the SDK surface).
	private const uint Disabled = 0x80423661;
	private const uint Selected = 0x8042654B;
	private const uint InputMode = 0x8042FB04;
	private const uint Pressed = 0x80423535;
	private const uint ShowMe = 0x80429BA8;
	private const uint Background = 0x8042545B;
	private const uint Frame = 0x8042AC64;
	private const uint Font = 0x8042BE50;
	private const uint NumericMin = 0x8042E404;
	private const uint NumericMax = 0x8042D78A;
	private const uint NumericValue = 0x8042AE3A;
	private const uint NumericDefault = 0x804263E8;
	private const uint NumericFormat = 0x804263E9;
	private const uint NumericReverse = 0x8042F2A0;
	private const uint PropEntries = 0x8042FBDB;
	private const uint PropVisible = 0x8042FEA6;
	private const uint PropFirst = 0x8042D4B2;
	private const uint GaugeMax = 0x8042BCDB;
	private const uint GaugeCurrent = 0x8042F0DD;
	private const uint GaugeHoriz = 0x804232DD;
	private const uint GaugeDivide = 0x8042D8DF;
	private const uint GaugeInfoText = 0x8042BF15;
	private const uint LevelmeterLabel = 0x80420DD5;
	private const uint SliderHoriz = 0x8042FAD1;
	private const uint SliderQuiet = 0x80420B26;
	private const uint PropHoriz = 0x8042F4F3;
	private const uint PropDeltaFactor = 0x80427C5E;
	private const uint PropSlider = 0x80429C3A;
	private const uint PropUseWinBorder = 0x8042DEEE;
	private const uint NumericCheckAllSizes = 0x80421594;
	private const uint GroupHoriz = 0x8042536B;
	private const uint ScrollbarType = 0x8042FB6B;
	private const uint ScrollbarTypeBottom = 1;
	private const uint ScrollbarTypeTop = 2;
	private const uint ScrollbarTypeSym = 3;
	private const uint ScrollbarTypeNone = 4;
	private const uint FixWidth = 0x8042A3F1;
	private const uint FixHeight = 0x8042A92B;
	private const uint LeftEdge = 0x8042BEC6;
	private const uint Width = 0x8042B59C;
	private const uint TopEdge = 0x8042509B;
	private const uint RightEdge = 0x8042BA82;
	private const uint BottomEdge = 0x8042E552;
	private const uint Weight = 0x80421D1F;
	private const uint HorizWeight = 0x80426DB9;
	private const uint VertWeight = 0x804298D0;
	private const uint MaxWidth = 0x8042F112;
	private const uint MaxHeight = 0x804293E4;
	private const uint InnerLeft = 0x804228F8;
	private const uint InnerRight = 0x804297FF;
	private const uint InnerTop = 0x80421EB6;
	private const uint InnerBottom = 0x8042F2C0;
	private const uint FillArea = 0x804294A3;
	private const uint ScaleHoriz = 0x8042919A;
	private const uint CycleActive = 0x80421788;
	private const uint CycleEntries = 0x80420629;
	private const uint RadioActive = 0x80429B41;
	private const uint RadioEntries = 0x8042B6A1;
	private const uint ControlHeight = 0x80423237;
	private const uint StringContents = 0x80428FFD;
	private const uint StringAttachedList = 0x80420FD2;
	private const uint StringMaxLen = 0x80424984;
	private const uint StringAcknowledge = 0x8042026C;
	private const uint StringBufferPos = 0x80428B6C;
	private const uint StringDisplayPos = 0x8042CCBF;
	private const uint Unicode = 0x8042E7D0;
	private const uint StringEditable = 0x8042C94B;
	private const uint StringAdvanceOnCR = 0x804226DE;
	private const uint StringMultiline = 0x8042D18B;
	private const uint StringAccept = 0x8042E3E1;
	private const uint StringReject = 0x8042179C;
	private const uint StringSecret = 0x80428769;
	private const uint StringInteger = 0x80426E8A;
	private const uint StringInteger64 = 0x80424820;
	private const uint StringSpellChecking = 0x804266C6;
	private const uint StringFormat = 0x80427484;
	private const uint StringPlaceholder = 0x8042AE65;
	private const uint StringFormatCenter = 1;
	private const uint StringFormatRight = 2;
	private const uint TextContents = 0x8042F8DC;
	private const uint TextCopy = 0x80427727;
	private const uint TextSetMin = 0x80424E10;
	private const uint TextSetMax = 0x80424D0A;
	private const uint TextSetVMax = 0x80420D8B;
	private const uint TextShortened = 0x80425A86;
	private const uint TextPreParse = 0x8042566D;
	private const uint TextControlChar = 0x8042E6D0;
	private const uint TextHiChar = 0x804218FF;
	private const uint TextMarking = 0x8042F780;
	private const uint TextShorten = 0x80428BBD;
	private const uint TextShortenNothing = 0;
	private const uint TextShortenCutoff = 1;
	private const uint TextShortenHide = 2;
	private const uint ImageSpec = 0x804233D5;
	private const uint ImageBuiltinSpec = 0x8042B907;
	private const uint ImageFontMatch = 0x8042815D;
	private const uint ImageFontMatchHeight = 0x80429F26;
	private const uint ImageFontMatchString = 0x804263C1;
	private const uint ImageFontMatchWidth = 0x804239BF;
	private const uint ImageFreeHoriz = 0x8042DA84;
	private const uint ImageFreeVert = 0x8042EA28;
	private const uint ImageOldImage = 0x80424F3D;
	private const uint ImageState = 0x8042A3AD;
	private const uint ImageResolvedKindKey = 0x7F070005;
	private const uint ImageResolvedValueKey = 0x7F070006;
	private const uint RectangleBarTitle = 0x80426689;
	private const uint RectangleHBar = 0x8042C943;
	private const uint RectangleVBar = 0x80422204;
	private const uint BitmapBitmap = 0x804279BD;
	private const uint BitmapAlpha = 0x80423E71;
	private const uint BitmapWidth = 0x8042EB3A;
	private const uint BitmapHeight = 0x80421560;
	private const uint BitmapMappingTable = 0x8042E23D;
	private const uint BitmapPrecision = 0x80420C74;
	private const uint BitmapRemapped = 0x80423A47;
	private const uint BitmapSourceColors = 0x80425360;
	private const uint BitmapTransparent = 0x80422805;
	private const uint BitmapUseFriend = 0x804239D8;
	private const uint BodychunkBody = 0x8042CA67;
	private const uint BodychunkCompression = 0x8042DE5F;
	private const uint BodychunkDepth = 0x8042C392;
	private const uint BodychunkMasking = 0x80423B0E;
	private const uint GadgetGadget = 0x8042EC1A;
	private const uint ListviewList = 0x8042BCCE;
	private const uint ListActive = 0x8042391C;

	// Method identifiers.
	private const uint MethodSet = 0x8042549A;
	private const uint MethodNoNotifySet = 0x8042216F;
	private const uint NumericIncrease = 0x80426ECD;
	private const uint NumericDecrease = 0x804243A7;
	private const uint NumericSetDefault = 0x8042AB0A;
	private const uint NumericScaleToValue = 0x8042032C;
	private const uint NumericValueToScale = 0x80423E4F;
	private const uint NumericStringify = 0x80424891;
	private const uint PropIncrease = 0x8042CAC0;
	private const uint PropDecrease = 0x80420DD1;
	private const uint HandleEvent = 0x80426D66;
	private const uint AskMinMax = 0x80423874;
	private const uint Layout = 0x8042845B;
	private const uint Draw = 0x80426F3F;
	private const uint Setup = 0x80428354;
	private const uint Cleanup = 0x8042D985;

	private const int KeyUp = 2;
	private const int KeyDown = 3;
	private const int KeyPageUp = 4;
	private const int KeyPageDown = 5;
	private const int KeyPress = 0;
	private const int KeyToggle = 1;
	private const int KeyBackspace = 29;
	private const int KeyDelete = 28;
	private const int KeyLeft = 8;
	private const int KeyRight = 9;
	private const int KeyHome = 12;
	private const int KeyEnd = 13;
	private const int ListInsertBottom = -3;
	private const uint InputModeNone = 0;
	private const uint InputModeRelVerify = 1;
	private const uint InputModeImmediate = 2;
	private const uint InputModeToggle = 3;

	private static readonly (string Name, MuiControlClass Class)[] Classes =
	{
		("Text.mui", MuiControlClass.Text),
		("Rectangle.mui", MuiControlClass.Rectangle),
		("Image.mui", MuiControlClass.Image),
		("Bitmap.mui", MuiControlClass.Bitmap),
		("Bodychunk.mui", MuiControlClass.Bodychunk),
		("Gauge.mui", MuiControlClass.Gauge),
		("Levelmeter.mui", MuiControlClass.Levelmeter),
		("Numeric.mui", MuiControlClass.Numeric),
		("Slider.mui", MuiControlClass.Slider),
		("Knob.mui", MuiControlClass.Knob),
		("Numericbutton.mui", MuiControlClass.Numericbutton),
		("String.mui", MuiControlClass.String),
		("Cycle.mui", MuiControlClass.Cycle),
		("Radio.mui", MuiControlClass.Radio),
		("Prop.mui", MuiControlClass.Prop),
		("Scrollbar.mui", MuiControlClass.Scrollbar),
		("Scale.mui", MuiControlClass.Scale),
		("Gadget.mui", MuiControlClass.Gadget),
	};

	[Fact]
	public void ImageGeometryCodecUsesNamedFields()
	{
		var platform = NewPlatform();
		var address = APTR.FromPointer(0x3400);
		var expected = default(MuiImageGeometryState);
		expected.LeftEdge = -3;
		expected.TopEdge = 4;
		expected.Width = 24;
		expected.Height = 20;
		Assert.True(MuiImageGeometryCodec.Write(ref platform, address, expected));
		Assert.True(MuiImageGeometryCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.LeftEdge, actual.LeftEdge);
		Assert.Equal(expected.TopEdge, actual.TopEdge);
		Assert.Equal(expected.Width, actual.Width);
		Assert.Equal(expected.Height, actual.Height);
		Assert.False(MuiImageGeometryCodec.TryRead(ref platform, APTR.Null,
			out _));
	}

	[Fact]
	public void ImageGeometryFieldCursorUsesNamedMixedWidthBoundaries()
	{
		var platform = NewPlatform();
		var record = APTR.FromPointer(0x3480);
		var cursor = new MuiImageGeometryFieldCursor
		{
			Record = record,
			Field = MuiImageGeometryField.Height,
		};
		Assert.True(MuiImageGeometryFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x3486), address);
		Assert.True(MuiImageGeometryFieldCursorCodec.TryWriteUInt16(ref platform,
			record, MuiImageGeometryField.LeftEdge, unchecked((ushort)-7)));
		Assert.True(MuiImageGeometryFieldCursorCodec.TryReadUInt16(ref platform,
			record, MuiImageGeometryField.LeftEdge, out var leftEdge));
		Assert.Equal(-7, unchecked((short)leftEdge));
		Assert.False(MuiImageGeometryFieldCursorCodec.TryReadUInt16(ref platform,
			record, unchecked((MuiImageGeometryField)255), out _));
		Assert.False(MuiImageGeometryFieldCursorCodec.TryReadUInt16(ref platform,
			APTR.FromPointer(0xFFFFFFF0u), MuiImageGeometryField.Height, out _));
	}

	[Fact]
	public void ControlFontUsesNamedGuestRecordAndTracksProjection()
	{
		var platform = NewPlatform();
		var textClass = Register(ref platform, 0x1100, "Text.mui");
		var font = APTR.FromPointer(0x3600);
		var text = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, BuildTags(ref platform, 0x1900, new[] {
				(Font, font.Raw) }));
		Assert.True(MuiCommonControlCore.TryGetControlFontStateRecord(
			ref platform, State, text, out var record));
		Assert.Equal(MuiControlFontStateRecord.Cookie, record.Magic);
		Assert.Equal(1u, record.Present);
		Assert.Equal(font.Raw, record.Font.Raw);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, text, Font,
			out var projected, out var handled));
		Assert.True(handled);
		Assert.Equal(font.Raw, projected);
		Assert.Equal(font.Raw, Get(ref platform, text, Font));
		var getMessage = APTR.FromPointer(0x36C0);
		var getStorage = APTR.FromPointer(0x3700);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute, Font));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			text, getMessage));
		Assert.Equal(font.Raw, platform.ReadUInt32(getStorage, 0));

		var replacement = APTR.FromPointer(0x3640);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			text, Font, replacement.Raw));
		Assert.True(MuiCommonControlCore.TryReadControlFontState(ref platform,
			State, text, out var state));
		Assert.True(state.Present);
		Assert.Equal(replacement.Raw, state.Font.Raw);
		Assert.True(MuiCommonControlCore.TryGetControlFontStateRecord(
			ref platform, State, text, out record));
		Assert.Equal(replacement.Raw, record.Font.Raw);

		var noFont = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, APTR.Null);
		Assert.True(MuiCommonControlCore.TryGetControlFontStateRecord(
			ref platform, State, noFont, out record));
		Assert.Equal(0u, record.Present);
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			noFont));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, text));
	}

	[Fact]
	public void CommonControlMethodHeaderUsesNamedField()
	{
		var platform = NewPlatform();
		var address = APTR.FromPointer(0x3440);
		Assert.True(MuiCommonControlPacketCore.WriteMethod(ref platform, address,
			MuiCommonControlPacketCore.Draw));
		Assert.True(MuiCommonControlPacketCore.TryReadMethodId(ref platform,
			address, out var packet));
		Assert.Equal(MuiCommonControlPacketCore.Draw, packet.MethodId);
		Assert.False(MuiCommonControlPacketCore.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void CommonControlTypedReadersUseNamedMethodHeader()
	{
		var platform = NewPlatform();
		var address = APTR.FromPointer(0x3480);
		Assert.True(MuiCommonControlPacketCore.WriteMethod(ref platform, address,
			MuiCommonControlPacketCore.NumericIncrease));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiCommonPacketKind.Signed, MuiCommonField.Value,
			unchecked((uint)-7)));
		Assert.True(MuiCommonControlPacketCore.TryReadSigned(ref platform,
			address, MuiCommonControlPacketCore.NumericIncrease, out var packet));
		Assert.Equal(MuiCommonControlPacketCore.NumericIncrease, packet.MethodId);
		Assert.Equal(-7, packet.Value);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiCommonPacketKind.Signed, MuiCommonField.MethodId, 0xDEADBEEFu));
		Assert.False(MuiCommonControlPacketCore.TryReadSigned(ref platform,
			address, MuiCommonControlPacketCore.NumericIncrease, out _));
	}

	[Fact]
	public void CommonControlFieldCursorUsesNamedMixedPacketBoundaries()
	{
		var platform = NewPlatform();
		var address = APTR.FromPointer(0x34C0);
		var cursor = default(MuiCommonFieldCursor);
		cursor.Message = address;
		cursor.Packet = MuiCommonPacketKind.Layout;
		cursor.Field = MuiCommonField.MethodId;
		Assert.True(MuiCommonFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var fieldAddress));
		Assert.Equal(address.Raw, fieldAddress.Raw);
		cursor.Field = MuiCommonField.Left;
		Assert.True(MuiCommonFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out fieldAddress));
		Assert.Equal(address.Raw + 4, fieldAddress.Raw);
		cursor.Field = MuiCommonField.Flags;
		Assert.True(MuiCommonFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out fieldAddress));
		Assert.Equal(address.Raw + 20, fieldAddress.Raw);

		Assert.True(MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiCommonPacketKind.ScaleToValue, MuiCommonField.Value,
			out var value));
		Assert.Equal(0u, value);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiCommonPacketKind.HandleEvent, MuiCommonField.MuiKey,
			unchecked((uint)-9)));
		Assert.True(MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiCommonPacketKind.HandleEvent, MuiCommonField.MuiKey,
			out value));
		Assert.Equal(unchecked((uint)-9), value);

		cursor.Packet = MuiCommonPacketKind.Method;
		cursor.Field = MuiCommonField.Value;
		Assert.False(MuiCommonFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
		cursor.Message = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Packet = MuiCommonPacketKind.HandleEvent;
		cursor.Field = MuiCommonField.EventHandlerNode;
		Assert.False(MuiCommonFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
	}

	[Fact]
	public void EveryListedClassConstructsClassifiesAndDisposes()
	{
		var platform = NewPlatform();
		uint nameAddr = 0x1100;
		var objects = new (APTR obj, MuiControlClass cls)[Classes.Length];
		for (var index = 0; index < Classes.Length; index++)
		{
			var cl = Register(ref platform, nameAddr, Classes[index].Name);
			nameAddr += 0x40;
			Assert.True(cl.IsNotNull, Classes[index].Name);
			var obj = MuiCommonControlCore.CreateControl(ref platform, State, cl,
				APTR.Null);
			Assert.True(obj.IsNotNull, Classes[index].Name);
			Assert.Equal(Classes[index].Class,
				MuiCommonControlCore.Classify(ref platform, State, obj));
			objects[index] = (obj, Classes[index].Class);
		}

		// Family-specific normalized construction defaults.
		foreach (var entry in objects)
		{
			switch (entry.cls)
			{
				case MuiControlClass.Text:
				Assert.Equal(1u, Get(ref platform, entry.obj, TextCopy));
				Assert.Equal(1u, Get(ref platform, entry.obj, TextSetMin));
				Assert.Equal(0u, Get(ref platform, entry.obj, TextSetMax));
				Assert.Equal(1u, Get(ref platform, entry.obj, TextSetVMax));
				break;
			case MuiControlClass.String:
				Assert.Equal(80u, Get(ref platform, entry.obj, StringMaxLen));
				break;
			case MuiControlClass.Numeric:
				case MuiControlClass.Slider:
				case MuiControlClass.Knob:
			case MuiControlClass.Numericbutton:
					Assert.Equal(0u, Get(ref platform, entry.obj, NumericMin));
					Assert.Equal(100u, Get(ref platform, entry.obj, NumericMax));
					Assert.Equal(0u, Get(ref platform, entry.obj, NumericValue));
					Assert.Equal(0u, Get(ref platform, entry.obj, NumericDefault));
					Assert.Equal(0u, Get(ref platform, entry.obj, NumericReverse));
					break;
				case MuiControlClass.Gauge:
					Assert.Equal(100u, Get(ref platform, entry.obj, GaugeMax));
					Assert.Equal(0u, Get(ref platform, entry.obj, GaugeCurrent));
					break;
				case MuiControlClass.Levelmeter:
					Assert.Equal(100u, Get(ref platform, entry.obj, NumericMax));
					Assert.Equal(0u, Get(ref platform, entry.obj, NumericValue));
					break;
				case MuiControlClass.Gadget:
					Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform,
						State, entry.obj, GadgetGadget, 0x3100));
					break;
				case MuiControlClass.Prop:
				case MuiControlClass.Scrollbar:
					Assert.Equal(0u, Get(ref platform, entry.obj, PropFirst));
					break;
				case MuiControlClass.Cycle:
					Assert.Equal(0u, Get(ref platform, entry.obj, CycleActive));
					break;
				case MuiControlClass.Radio:
					Assert.Equal(0u, Get(ref platform, entry.obj, RadioActive));
					break;
			}
		}

		var freedBefore = platform.FreeCount;
		foreach (var entry in objects)
			Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
				entry.obj), entry.cls.ToString());
		Assert.True(platform.FreeCount > freedBefore);
	}

	[Fact]
	public void StringAndTextContentsAreOwnedOrReferencedThroughStores()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var textClass = Register(ref platform, 0x1140, "Text.mui");

		var source = APTR.FromPointer(0x1200);
		platform.WriteCString(source, "HelloWorld");

		// String contents are always copied and honor MaxLen (buffer size 5 => 4).
		var stringTags = BuildTags(ref platform, 0x1300,
			new[] { (StringMaxLen, 5u), (StringContents, source.Raw) });
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, stringTags);
		var stringPtr = APTR.FromPointer(Get(ref platform, stringObj, StringContents));
		Assert.NotEqual(source.Raw, stringPtr.Raw);
		Assert.Equal("Hell", ReadCString(ref platform, stringPtr));

		// Text with Copy true owns its own copy.
		var copyTags = BuildTags(ref platform, 0x1380,
			new[] { (TextCopy, 1u), (TextContents, source.Raw) });
		var textCopied = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, copyTags);
		var textPtr = APTR.FromPointer(Get(ref platform, textCopied, TextContents));
		Assert.NotEqual(source.Raw, textPtr.Raw);
		Assert.Equal("HelloWorld", ReadCString(ref platform, textPtr));

		// Text with Copy false references the caller-owned pointer untouched.
		var refTags = BuildTags(ref platform, 0x1400,
			new[] { (TextCopy, 0u), (TextContents, source.Raw) });
		var textReferenced = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, refTags);
		Assert.Equal(source.Raw,
			Get(ref platform, textReferenced, TextContents));

		// Disposal retires the owned copies without freeing caller memory.
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			stringObj));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			textCopied));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			textReferenced));
		Assert.Equal("HelloWorld", ReadCString(ref platform, source));
	}

	[Fact]
	public void TextContentsUsesNamedGuestRecordAndTracksCopyPolicy()
	{
		var platform = NewPlatform();
		var textClass = Register(ref platform, 0x1100, "Text.mui");
		var source = APTR.FromPointer(0x2180);
		platform.WriteCString(source, "hello");
		var copied = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, BuildTags(ref platform, 0x21C0, new[] {
				(TextCopy, 1u), (TextContents, source.Raw) }));
		Assert.NotEqual(APTR.Null, copied);
		Assert.True(MuiCommonControlCore.TryGetTextContentsStateRecord(
			ref platform, State, copied, out var record));
		Assert.Equal(MuiTextContentsStateRecord.Cookie, record.Magic);
		Assert.NotEqual(source.Raw, record.Contents.Raw);
		Assert.Equal("hello", ReadCString(ref platform, record.Contents));

		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, copied,
			TextContents, out var projectedContents, out var contentsHandled));
		Assert.True(contentsHandled);
		Assert.Equal(record.Contents.Raw, projectedContents);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, copied,
			TextContents, out projectedContents));
		Assert.Equal(record.Contents.Raw, projectedContents);
		var contentsGetMessage = APTR.FromPointer(0x2300);
		var contentsGetStorage = APTR.FromPointer(0x2340);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			contentsGetMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			contentsGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			TextContents));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			contentsGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			contentsGetStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			copied, contentsGetMessage));
		Assert.Equal(record.Contents.Raw, platform.ReadUInt32(contentsGetStorage, 0));

		var replacement = APTR.FromPointer(0x2200);
		platform.WriteCString(replacement, "world");
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			copied, TextContents, replacement.Raw));
		Assert.True(MuiCommonControlCore.TryGetTextContentsStateRecord(
			ref platform, State, copied, out record));
		Assert.NotEqual(replacement.Raw, record.Contents.Raw);
		Assert.Equal("world", ReadCString(ref platform, record.Contents));

		var referenced = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, BuildTags(ref platform, 0x2240, new[] {
				(TextCopy, 0u), (TextContents, source.Raw) }));
		Assert.NotEqual(APTR.Null, referenced);
		Assert.True(MuiCommonControlCore.TryGetTextContentsStateRecord(
			ref platform, State, referenced, out record));
		Assert.Equal(source.Raw, record.Contents.Raw);

		// Persistence/bootstrap may write the public scalar directly; the typed
		// reader absorbs the guest pointer without taking ownership.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			referenced, TextContents, replacement.Raw, false));
		Assert.True(MuiCommonControlCore.TryReadTextContentsState(ref platform,
			State, referenced, out var state));
		Assert.Equal(replacement.Raw, state.Contents.Raw);
		Assert.True(MuiCommonControlCore.TryGetTextContentsStateRecord(
			ref platform, State, referenced, out record));
		Assert.Equal(replacement.Raw, record.Contents.Raw);

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, copied));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			referenced));
	}

	[Fact]
	public void TextPreParseUsesNamedGuestRecordAndOwnedReplacement()
	{
		var platform = NewPlatform();
		var textClass = Register(ref platform, 0x1100, "Text.mui");
		var preParse = APTR.FromPointer(0x2280);
		var contents = APTR.FromPointer(0x22C0);
		platform.WriteCString(preParse, "\u001bc");
		platform.WriteCString(contents, "hello");
		var text = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, BuildTags(ref platform, 0x2300, new[] {
				(TextPreParse, preParse.Raw), (TextContents, contents.Raw) }));
		Assert.NotEqual(APTR.Null, text);
		Assert.True(MuiCommonControlCore.TryGetTextPreParseStateRecord(
			ref platform, State, text, out var record));
		Assert.Equal(MuiTextPreParseStateRecord.Cookie, record.Magic);
		Assert.NotEqual(preParse.Raw, record.PreParse.Raw);
		Assert.Equal("\u001bc", ReadCString(ref platform, record.PreParse));

		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, text,
			TextPreParse, out var projectedPreParse, out var preParseHandled));
		Assert.True(preParseHandled);
		Assert.Equal(record.PreParse.Raw, projectedPreParse);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, text,
			TextPreParse, out projectedPreParse));
		Assert.Equal(record.PreParse.Raw, projectedPreParse);
		var preParseGetMessage = APTR.FromPointer(0x2380);
		var preParseGetStorage = APTR.FromPointer(0x23C0);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			preParseGetMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			preParseGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			TextPreParse));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			preParseGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			preParseGetStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			text, preParseGetMessage));
		Assert.Equal(record.PreParse.Raw, platform.ReadUInt32(preParseGetStorage, 0));

		var replacement = APTR.FromPointer(0x2340);
		platform.WriteCString(replacement, "\u001br");
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			text, TextPreParse, replacement.Raw));
		Assert.True(MuiCommonControlCore.TryGetTextPreParseStateRecord(
			ref platform, State, text, out record));
		Assert.NotEqual(replacement.Raw, record.PreParse.Raw);
		Assert.Equal("\u001br", ReadCString(ref platform, record.PreParse));
		Assert.True(MuiCommonControlCore.TryReadTextPreParseState(ref platform,
			State, text, out var state));
		Assert.Equal(record.PreParse.Raw, state.PreParse.Raw);

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, text));
		Assert.Equal("\u001bc", ReadCString(ref platform, preParse));
		Assert.Equal("\u001br", ReadCString(ref platform, replacement));
	}

	[Fact]
	public void InitOnlyGetOnlyAndSettableEnforcementHolds()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var numericClass = Register(ref platform, 0x1140, "Numeric.mui");

		var tags = BuildTags(ref platform, 0x1300,
			new[] { (StringMaxLen, 20u) });
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, tags);

		// Init-only MaxLen cannot be changed after construction.
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringMaxLen, 40));
		Assert.Equal(20u, Get(ref platform, stringObj, StringMaxLen));

		// Get-only Acknowledge rejects post-construction sets.
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringAcknowledge, 1));

		// Settable numeric value clamps to the active bounds.
		var numeric = MuiCommonControlCore.CreateControl(ref platform, State,
			numericClass, APTR.Null);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			numeric, NumericValue, 500));
		Assert.Equal(100u, Get(ref platform, numeric, NumericValue));
	}

	[Fact]
	public void NumericMethodsStringifyScaleReverseAndNotifyOnlyOnChange()
	{
		var platform = NewPlatform();
		var numericClass = Register(ref platform, 0x1100, "Numeric.mui");
		var numeric = MuiCommonControlCore.CreateControl(ref platform, State,
			numericClass, APTR.Null);
		Set(ref platform, numeric, NumericMin, 0);
		Set(ref platform, numeric, NumericMax, 100);
		Set(ref platform, numeric, NumericValue, 25);

		// Reverse-aware scaling.
		Set(ref platform, numeric, NumericReverse, 1);
		Assert.Equal(75, MuiCommonControlCore.ValueToScale(ref platform, State,
			numeric, 0, 100));
		Assert.Equal(25, MuiCommonControlCore.ScaleToValue(ref platform, State,
			numeric, 0, 100, 75));
		Set(ref platform, numeric, NumericReverse, 0);

		// Stringify through the exact packet.
		var packet = APTR.FromPointer(0x1400);
		platform.WriteUInt32(packet, 0, NumericStringify);
		platform.WriteUInt32(packet, 4, unchecked((uint)-1234));
		var stringified = APTR.FromPointer(MuiCommonControlDispatcher.Dispatch(
			ref platform, State, numeric, packet));
		Assert.Equal("-1234", ReadCString(ref platform, stringified));

		// Increase/decrease and default via exact packets.
		platform.WriteUInt32(packet, 0, NumericIncrease);
		platform.WriteUInt32(packet, 4, 10);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			numeric, packet));
		Assert.Equal(35u, Get(ref platform, numeric, NumericValue));
		Set(ref platform, numeric, NumericDefault, 40);
		platform.WriteUInt32(packet, 0, NumericSetDefault);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			numeric, packet));
		Assert.Equal(40u, Get(ref platform, numeric, NumericValue));

		// Notifications fire only for an actual state change.
		var destination = MuiCommonControlCore.CreateControl(ref platform, State,
			numericClass, APTR.Null);
		var follow = APTR.FromPointer(0x1480);
		platform.WriteUInt32(follow, 0, 0x90000001);
		Assert.True(MuiNotifyCore.Add(ref platform, State, numeric, NumericValue, 55,
			destination, 1, follow));
		var dispatchesBefore = platform.DispatchCount;
		platform.WriteUInt32(packet, 0, MethodNoNotifySet);
		platform.WriteUInt32(packet, 4, NumericValue);
		platform.WriteUInt32(packet, 8, 50);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			numeric, packet));
		Assert.Equal(50u, Get(ref platform, numeric, NumericValue));
		Assert.Equal(dispatchesBefore, platform.DispatchCount);
		platform.WriteUInt32(packet, 0, MethodSet);
		platform.WriteUInt32(packet, 8, 55);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			numeric, packet));
		Assert.Equal(dispatchesBefore + 1, platform.DispatchCount);
		Assert.True(MuiCommonControlCore.SetNumericValue(ref platform, State,
			numeric, 55, false));
		Assert.Equal(dispatchesBefore + 1, platform.DispatchCount);
		Assert.True(MuiCommonControlCore.SetNumericValue(ref platform, State,
			numeric, 40, false));
		Assert.Equal(dispatchesBefore + 1, platform.DispatchCount);
	}

	[Fact]
	public void NumericStateUsesNamedGuestRecordAcrossRangeAndValuePaths()
	{
		var platform = NewPlatform();
		var numericClass = Register(ref platform, 0x1100, "Numeric.mui");
		var numeric = MuiCommonControlCore.CreateControl(ref platform, State,
			numericClass, BuildTags(ref platform, 0x1500, new[] {
				(NumericMin, 10u), (NumericMax, 90u), (NumericValue, 50u),
				(NumericDefault, 25u), (NumericReverse, 1u) }));

		Assert.True(MuiCommonControlCore.TryGetNumericStateRecord(
			ref platform, State, numeric, out var initial));
		Assert.Equal(MuiNumericStateRecord.Cookie, initial.Magic);
		Assert.Equal(10u, initial.Minimum);
		Assert.Equal(90u, initial.Maximum);
		Assert.Equal(50u, initial.Value);
		Assert.Equal(25u, initial.Default);
		Assert.Equal(1u, initial.Reverse);
		var numericGetters = new (uint Attribute, uint Expected)[] {
			(NumericMin, 10u), (NumericMax, 90u), (NumericValue, 50u),
			(NumericDefault, 25u), (NumericReverse, 1u) };
		var getMessage = APTR.FromPointer(0x1700);
		var getStorage = APTR.FromPointer(0x1740);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		foreach (var getter in numericGetters)
		{
			Assert.True(MuiCommonControlCore.TryGet(ref platform, State, numeric,
				getter.Attribute, out var projected, out var handled));
			Assert.True(handled);
			Assert.Equal(getter.Expected, projected);
			Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
				numeric, getter.Attribute, out projected));
			Assert.Equal(getter.Expected, projected);
			Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
				getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
				getter.Attribute));
			Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform,
				State, numeric, getMessage));
			Assert.Equal(getter.Expected, platform.ReadUInt32(getStorage, 0));
		}

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			numeric, NumericValue, 100));
		Assert.Equal(90u, Get(ref platform, numeric, NumericValue));
		Assert.True(MuiCommonControlCore.TryReadNumericState(ref platform, State,
			numeric, out var state));
		Assert.Equal(90u, state.Value);
		Assert.Equal(10u, state.Minimum);
		Assert.Equal(90u, state.Maximum);

		Set(ref platform, numeric, NumericReverse, 0);
		Assert.Equal(100, MuiCommonControlCore.ValueToScale(ref platform, State,
			numeric, 0, 100));
		Assert.True(MuiCommonControlCore.TryGetNumericStateRecord(
			ref platform, State, numeric, out var changed));
		Assert.Equal(0u, changed.Reverse);
	}

	[Fact]
	public void NumericFormatIsCopiedFormattedAndDisposedWithoutManagedRuntime()
	{
		var platform = NewPlatform();
		var numericClass = Register(ref platform, 0x1100, "Numeric.mui");
		var formatSource = APTR.FromPointer(0x1500);
		platform.WriteCString(formatSource, "Value=%+04ld");
		var tags = BuildTags(ref platform, 0x1600,
			new[] { (NumericFormat, formatSource.Raw), (NumericValue, 35u) });
		var numeric = MuiCommonControlCore.CreateControl(ref platform, State,
			numericClass, tags);
		var copiedFormat = APTR.FromPointer(Get(ref platform, numeric,
			NumericFormat));
		Assert.NotEqual(formatSource.Raw, copiedFormat.Raw);
		Assert.Equal("Value=%+04ld", ReadCString(ref platform, copiedFormat));

		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, numeric,
			NumericFormat, out var projectedFormat, out var formatHandled));
		Assert.True(formatHandled);
		Assert.Equal(copiedFormat.Raw, projectedFormat);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, numeric,
			NumericFormat, out projectedFormat));
		Assert.Equal(copiedFormat.Raw, projectedFormat);
		var formatGetMessage = APTR.FromPointer(0x1A00);
		var formatGetStorage = APTR.FromPointer(0x1A40);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			formatGetMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			formatGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			NumericFormat));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			formatGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			formatGetStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			numeric, formatGetMessage));
		Assert.Equal(copiedFormat.Raw, platform.ReadUInt32(formatGetStorage, 0));

		var packet = APTR.FromPointer(0x1700);
		platform.WriteUInt32(packet, 0, NumericStringify);
		platform.WriteUInt32(packet, 4, 35);
		var rendered = APTR.FromPointer(MuiCommonControlDispatcher.Dispatch(
			ref platform, State, numeric, packet));
		Assert.Equal("Value=+035", ReadCString(ref platform, rendered));

		var replacement = APTR.FromPointer(0x1800);
		platform.WriteCString(replacement, "0x%08lx");
		var redrawBefore = platform.RedrawCount;
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			numeric, NumericFormat, replacement.Raw));
		Assert.True(platform.RedrawCount > redrawBefore);
		Assert.Equal("0x%08lx", ReadCString(ref platform,
			APTR.FromPointer(Get(ref platform, numeric, NumericFormat))));
		platform.WriteUInt32(packet, 4, unchecked((uint)-1));
		rendered = APTR.FromPointer(MuiCommonControlDispatcher.Dispatch(ref platform,
			State, numeric, packet));
		Assert.Equal("0xffffffff", ReadCString(ref platform, rendered));

		var invalid = APTR.FromPointer(0x1900);
		platform.WriteCString(invalid, "%q");
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			numeric, NumericFormat, invalid.Raw));
		platform.WriteUInt32(packet, 4, 7);
		rendered = APTR.FromPointer(MuiCommonControlDispatcher.Dispatch(ref platform,
			State, numeric, packet));
		Assert.Equal("7", ReadCString(ref platform, rendered));

		var freedBefore = platform.FreeCount;
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			numeric));
		Assert.True(platform.FreeCount > freedBefore);
		Assert.Equal("Value=%+04ld", ReadCString(ref platform, formatSource));
		Assert.Equal("0x%08lx", ReadCString(ref platform, replacement));
	}

	[Fact]
	public void NumericFormatUsesNamedGuestRecordAndTracksReplacement()
	{
		var platform = NewPlatform();
		var numericClass = Register(ref platform, 0x1100, "Numeric.mui");
		var source = APTR.FromPointer(0x1A80);
		platform.WriteCString(source, "Value=%ld");
		var numeric = MuiCommonControlCore.CreateControl(ref platform, State,
			numericClass, BuildTags(ref platform, 0x1AC0, new[] {
				(NumericFormat, source.Raw) }));
		Assert.NotEqual(APTR.Null, numeric);
		Assert.True(MuiCommonControlCore.TryGetNumericFormatStateRecord(
			ref platform, State, numeric, out var record));
		Assert.Equal(MuiNumericFormatStateRecord.Cookie, record.Magic);
		Assert.NotEqual(source.Raw, record.Format.Raw);
		Assert.Equal("Value=%ld", ReadCString(ref platform, record.Format));

		var replacement = APTR.FromPointer(0x1B00);
		platform.WriteCString(replacement, "0x%lx");
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			numeric, NumericFormat, replacement.Raw));
		Assert.True(MuiCommonControlCore.TryGetNumericFormatStateRecord(
			ref platform, State, numeric, out record));
		Assert.NotEqual(replacement.Raw, record.Format.Raw);
		Assert.Equal("0x%lx", ReadCString(ref platform, record.Format));

		// Persistence/bootstrap may stage the public scalar directly; typed
		// consumers fold that valid guest pointer into the named record.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, numeric,
			NumericFormat, replacement.Raw, false));
		Assert.True(MuiCommonControlCore.TryReadNumericFormatState(ref platform,
			State, numeric, out var state));
		Assert.Equal(replacement.Raw, state.Format.Raw);
		Assert.True(MuiCommonControlCore.TryGetNumericFormatStateRecord(
			ref platform, State, numeric, out record));
		Assert.Equal(replacement.Raw, record.Format.Raw);

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			numeric));
	}

	[Fact]
	public void PropGaugeChoiceAndDisabledKeyboardInteraction()
	{
		var platform = NewPlatform();
		var propClass = Register(ref platform, 0x1100, "Prop.mui");
		var gaugeClass = Register(ref platform, 0x1140, "Gauge.mui");
		var cycleClass = Register(ref platform, 0x1180, "Cycle.mui");

		var prop = MuiCommonControlCore.CreateControl(ref platform, State,
			propClass, APTR.Null);
		Set(ref platform, prop, PropEntries, 100);
		Set(ref platform, prop, PropVisible, 10);
		Set(ref platform, prop, PropFirst, 5);
		var packet = APTR.FromPointer(0x1400);
		platform.WriteUInt32(packet, 0, PropIncrease);
		platform.WriteUInt32(packet, 4, 200);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			prop, packet));
		Assert.Equal(90u, Get(ref platform, prop, PropFirst));
		platform.WriteUInt32(packet, 0, PropDecrease);
		platform.WriteUInt32(packet, 4, 100);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			prop, packet));
		Assert.Equal(0u, Get(ref platform, prop, PropFirst));

		var gauge = MuiCommonControlCore.CreateControl(ref platform, State,
			gaugeClass, APTR.Null);
		Set(ref platform, gauge, GaugeMax, 50);
		Assert.True(MuiCommonControlCore.SetGauge(ref platform, State, gauge, 70));
		Assert.Equal(50u, Get(ref platform, gauge, GaugeCurrent));

		var cycle = MuiCommonControlCore.CreateControl(ref platform, State,
			cycleClass, APTR.Null);
		var entries = APTR.FromPointer(0x1500);
		platform.WriteUInt32(entries, 0, 0x1600);
		platform.WriteUInt32(entries, 4, 0x1610);
		platform.WriteUInt32(entries, 8, 0x1620);
		platform.WriteUInt32(entries, 12, 0);
		Set(ref platform, cycle, CycleEntries, entries.Raw);

		// Disabled controls ignore keyboard input.
		Set(ref platform, cycle, Disabled, 1);
		platform.WriteUInt32(packet, 0, HandleEvent);
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyDown));
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			cycle, packet));
		Assert.Equal(0u, Get(ref platform, cycle, CycleActive));

		// Enabled: cursor keys advance and wrap the active choice.
		Set(ref platform, cycle, Disabled, 0);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			cycle, packet));
		Assert.Equal(1u, Get(ref platform, cycle, CycleActive));
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyUp));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			cycle, packet));
		Assert.Equal(0u, Get(ref platform, cycle, CycleActive));
	}

	[Fact]
	public void ChoiceActiveSettersWrapValidateAndNotify()
	{
		var platform = NewPlatform();
		var cycleClass = Register(ref platform, 0x1100, "Cycle.mui");
		var radioClass = Register(ref platform, 0x1140, "Radio.mui");
		var entries = APTR.FromPointer(0x1500);
		platform.WriteCString(APTR.FromPointer(0x1600), "First");
		platform.WriteCString(APTR.FromPointer(0x1610), "Second");
		platform.WriteCString(APTR.FromPointer(0x1620), "Third");
		platform.WriteUInt32(entries, 0, 0x1600);
		platform.WriteUInt32(entries, 4, 0x1610);
		platform.WriteUInt32(entries, 8, 0x1620);
		platform.WriteUInt32(entries, 12, 0);
		var cycleTags = BuildTags(ref platform, 0x1700,
			new[] { (CycleEntries, entries.Raw) });
		var cycle = MuiCommonControlCore.CreateControl(ref platform, State,
			cycleClass, cycleTags);
		var destination = MuiCommonControlCore.CreateControl(ref platform, State,
			cycleClass, APTR.Null);
		var follow = APTR.FromPointer(0x1800);
		platform.WriteUInt32(follow, 0, 0x90000001);
		Assert.True(MuiNotifyCore.Add(ref platform, State, cycle, CycleActive, 1,
			destination, 1, follow));
		var dispatchesBefore = platform.DispatchCount;
		var redrawBefore = platform.RedrawCount;
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			cycle, CycleActive, 1));
		Assert.Equal(1u, Get(ref platform, cycle, CycleActive));
		Assert.Equal(dispatchesBefore + 1, platform.DispatchCount);
		Assert.True(platform.RedrawCount > redrawBefore);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			cycle, CycleActive, unchecked((uint)-1)));
		Assert.Equal(2u, Get(ref platform, cycle, CycleActive));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			cycle, CycleActive, unchecked((uint)-1)));
		Assert.Equal(0u, Get(ref platform, cycle, CycleActive));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			cycle, CycleActive, unchecked((uint)-2)));
		Assert.Equal(2u, Get(ref platform, cycle, CycleActive));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			cycle, CycleActive, 3));
		Assert.Equal(2u, Get(ref platform, cycle, CycleActive));

		var shortEntries = APTR.FromPointer(0x1900);
		platform.WriteUInt32(shortEntries, 0, 0x1910);
		platform.WriteUInt32(shortEntries, 4, 0);
		platform.WriteCString(APTR.FromPointer(0x1910), "Only");
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			cycle, CycleEntries, shortEntries.Raw));
		Assert.Equal(0u, Get(ref platform, cycle, CycleActive));

		var radioTags = BuildTags(ref platform, 0x1A00,
			new[] { (RadioEntries, entries.Raw) });
		var radio = MuiCommonControlCore.CreateControl(ref platform, State,
			radioClass, radioTags);
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			radio, RadioActive, unchecked((uint)-1)));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			radio, RadioActive, 1));
		Assert.Equal(1u, Get(ref platform, radio, RadioActive));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			radio, RadioActive, 3));
		var packet = APTR.FromPointer(0x1B00);
		platform.WriteUInt32(packet, 0, HandleEvent);
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyDown));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			radio, packet));
		Assert.Equal(2u, Get(ref platform, radio, RadioActive));
		Set(ref platform, radio, Disabled, 1);
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			radio, packet));
		Assert.Equal(2u, Get(ref platform, radio, RadioActive));
		Set(ref platform, radio, Disabled, 0);
		var renderInfo = APTR.FromPointer(0x1C00);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, radio,
			renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, radio, 0, 0,
			80, 16));
		platform.WriteUInt32(packet, 0, Draw);
		var textBefore = platform.TextCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			radio, packet));
		Assert.Equal(textBefore + 1, platform.TextCount);
	}

	[Fact]
	public void ChoiceEntriesUsesNamedGuestRecordAndSharedConsumers()
	{
		var platform = NewPlatform();
		var cycleClass = Register(ref platform, 0x1100, "Cycle.mui");
		var radioClass = Register(ref platform, 0x1140, "Radio.mui");
		var entries = APTR.FromPointer(0x1F00);
		var first = APTR.FromPointer(0x1F40);
		var second = APTR.FromPointer(0x1F50);
		platform.WriteCString(first, "First");
		platform.WriteCString(second, "Second");
		platform.WriteUInt32(entries, 0, first.Raw);
		platform.WriteUInt32(entries, 4, second.Raw);
		platform.WriteUInt32(entries, 8, 0);

		var cycle = MuiCommonControlCore.CreateControl(ref platform, State,
			cycleClass, BuildTags(ref platform, 0x1F80, new[] {
				(CycleEntries, entries.Raw) }));
		Assert.NotEqual(APTR.Null, cycle);
		Assert.True(MuiCommonControlCore.TryGetChoiceEntriesStateRecord(
			ref platform, State, cycle, out var record));
		Assert.Equal(MuiChoiceEntriesStateRecord.Cookie, record.Magic);
		Assert.Equal(entries.Raw, record.Entries.Raw);
		Assert.True(MuiCommonControlCore.TryReadChoiceEntriesState(ref platform,
			State, cycle, CycleEntries, out var state));
		Assert.Equal(entries.Raw, state.Entries.Raw);

		var replacement = APTR.FromPointer(0x1FC0);
		var replacementText = APTR.FromPointer(0x1FE0);
		platform.WriteCString(replacementText, "Replacement");
		platform.WriteUInt32(replacement, 0, replacementText.Raw);
		platform.WriteUInt32(replacement, 4, 0);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			cycle, CycleEntries, replacement.Raw));
		Assert.True(MuiCommonControlCore.TryGetChoiceEntriesStateRecord(
			ref platform, State, cycle, out record));
		Assert.Equal(replacement.Raw, record.Entries.Raw);
		Assert.Equal(0u, Get(ref platform, cycle, CycleActive));

		var radio = MuiCommonControlCore.CreateControl(ref platform, State,
			radioClass, BuildTags(ref platform, 0x2000, new[] {
				(RadioEntries, entries.Raw) }));
		Assert.NotEqual(APTR.Null, radio);
		Assert.True(MuiCommonControlCore.TryGetChoiceEntriesStateRecord(
			ref platform, State, radio, out record));
		Assert.Equal(entries.Raw, record.Entries.Raw);

		// Persistence/bootstrap may write the public scalar directly; the typed
		// reader folds that guest change back into the named record.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, radio,
			RadioEntries, replacement.Raw, false));
		Assert.True(MuiCommonControlCore.TryReadChoiceEntriesState(ref platform,
			State, radio, RadioEntries, out state));
		Assert.Equal(replacement.Raw, state.Entries.Raw);
		Assert.True(MuiCommonControlCore.TryGetChoiceEntriesStateRecord(
			ref platform, State, radio, out record));
		Assert.Equal(replacement.Raw, record.Entries.Raw);

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, cycle));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, radio));
	}

	[Fact]
	public void ChoiceStateUsesNamedRecordsForGetAndOmGet()
	{
		var platform = NewPlatform();
		var cycleClass = Register(ref platform, 0x2100, "Cycle.mui");
		var radioClass = Register(ref platform, 0x2140, "Radio.mui");
		var entries = APTR.FromPointer(0x2200);
		var first = APTR.FromPointer(0x2240);
		var second = APTR.FromPointer(0x2250);
		platform.WriteCString(first, "First");
		platform.WriteCString(second, "Second");
		platform.WriteUInt32(entries, 0, first.Raw);
		platform.WriteUInt32(entries, 4, second.Raw);
		platform.WriteUInt32(entries, 8, 0);

		var cycle = MuiCommonControlCore.CreateControl(ref platform, State,
			cycleClass, BuildTags(ref platform, 0x2280, new[] {
				(CycleEntries, entries.Raw), (CycleActive, 1u) }));
		Assert.True(MuiCommonControlCore.TryGetChoiceEntriesStateRecord(
			ref platform, State, cycle, out var entriesRecord));
		Assert.Equal(MuiChoiceEntriesStateRecord.Cookie, entriesRecord.Magic);
		Assert.Equal(entries.Raw, entriesRecord.Entries.Raw);
		Assert.True(MuiCommonControlCore.TryGetChoiceActiveStateRecord(
			ref platform, State, cycle, out var activeRecord));
		Assert.Equal(MuiChoiceActiveStateRecord.Cookie, activeRecord.Magic);
		Assert.Equal(1u, activeRecord.Active);
		Assert.True(MuiCommonControlCore.TryReadChoiceActiveState(ref platform,
			State, cycle, CycleActive, out var activeState));
		Assert.Equal(1u, activeState.Active);

		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, cycle,
			CycleEntries, out var projectedEntries, out var entriesHandled));
		Assert.True(entriesHandled);
		Assert.Equal(entries.Raw, projectedEntries);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, cycle,
			CycleActive, out var projectedActive, out var activeHandled));
		Assert.True(activeHandled);
		Assert.Equal(1u, projectedActive);
		Assert.Equal(entries.Raw, Get(ref platform, cycle, CycleEntries));
		Assert.Equal(1u, Get(ref platform, cycle, CycleActive));

		// A raw persistence/bootstrap write is folded back into the active
		// record by the named reader used by both Get and OM_GET.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, cycle,
			CycleActive, 0, false));
		Assert.Equal(0u, Get(ref platform, cycle, CycleActive));
		Assert.True(MuiCommonControlCore.TryGetChoiceActiveStateRecord(
			ref platform, State, cycle, out activeRecord));
		Assert.Equal(0u, activeRecord.Active);

		var radio = MuiCommonControlCore.CreateControl(ref platform, State,
			radioClass, BuildTags(ref platform, 0x2300, new[] {
				(RadioEntries, entries.Raw), (RadioActive, 1u) }));
		Assert.Equal(entries.Raw, Get(ref platform, radio, RadioEntries));
		Assert.Equal(1u, Get(ref platform, radio, RadioActive));

		var getMessage = APTR.FromPointer(0x2380);
		var getStorage = APTR.FromPointer(0x23C0);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			RadioActive));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			radio, getMessage));
		Assert.Equal(1u, platform.ReadUInt32(getStorage, 0));

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, cycle));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, radio));
	}

	[Fact]
	public void AskMinMaxAndDrawAreClassSpecific()
	{
		var platform = NewPlatform();
		var numericClass = Register(ref platform, 0x1100, "Numeric.mui");
		var gaugeClass = Register(ref platform, 0x1140, "Gauge.mui");
		var textClass = Register(ref platform, 0x1180, "Text.mui");
		var imageClass = Register(ref platform, 0x11C0, "Image.mui");

		var storage = APTR.FromPointer(0x1400);
		var numeric = MuiCommonControlCore.CreateControl(ref platform, State,
			numericClass, APTR.Null);
		var packet = APTR.FromPointer(0x1440);
		platform.WriteUInt32(packet, 0, AskMinMax);
		platform.WriteUInt32(packet, 4, storage.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			numeric, packet));
		Assert.Equal(48, platform.ReadUInt16(storage, 0));
		Assert.Equal(14, platform.ReadUInt16(storage, 2));

		// Neutral Draw fills the background and renders class-specific content.
		var renderInfo = APTR.FromPointer(0x1480);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		var text = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, APTR.Null);
		var contents = APTR.FromPointer(0x1500);
		platform.WriteCString(contents, "Hi");
		Set(ref platform, text, TextContents, contents.Raw);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, text, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, text, 0, 0, 40,
			12));
		var textBefore = platform.TextCount;
		platform.WriteUInt32(packet, 0, Draw);
		platform.WriteUInt32(packet, 4, 0);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			text, packet));
		Assert.Equal(textBefore + 1, platform.TextCount);

		var gauge = MuiCommonControlCore.CreateControl(ref platform, State,
			gaugeClass, APTR.Null);
		Set(ref platform, gauge, GaugeMax, 100);
		Set(ref platform, gauge, GaugeCurrent, 50);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, gauge, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, gauge, 0, 0, 100,
			16));
		var fillBefore = platform.FillCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			gauge, packet));
		Assert.Equal(fillBefore + 2, platform.FillCount);

		var image = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, APTR.Null);
		Set(ref platform, image, ImageSpec, 0x3000);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, image, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, image, 0, 0, 16,
			16));
		var imageBefore = platform.ImageCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			image, packet));
		Assert.Equal(imageBefore + 1, platform.ImageCount);

		var builtinImage = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, APTR.Null);
		Set(ref platform, builtinImage, ImageBuiltinSpec, 0x0000000B);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, builtinImage,
			renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, builtinImage, 0,
			0, 16, 16));
		var builtinImageBefore = platform.FillCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			builtinImage, packet));
		Assert.Equal(builtinImageBefore + 1, platform.FillCount);

		var levelmeterClass = Register(ref platform, 0x1200, "Levelmeter.mui");
		var levelmeter = MuiCommonControlCore.CreateControl(ref platform, State,
			levelmeterClass, APTR.Null);
		var label = APTR.FromPointer(0x30C0);
		platform.WriteCString(label, "Load");
		Set(ref platform, levelmeter, NumericValue, 50);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			levelmeter, LevelmeterLabel, label.Raw));
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, levelmeter,
			renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, levelmeter, 0,
			0, 80, 16));
		var levelmeterTextBefore = platform.TextCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			levelmeter, packet));
		Assert.Equal(levelmeterTextBefore + 1, platform.TextCount);

		// Cycle and Radio render the currently active bounded entry.
		var entries = APTR.FromPointer(0x3100);
		var first = APTR.FromPointer(0x3180);
		var second = APTR.FromPointer(0x3190);
		platform.WriteCString(first, "First");
		platform.WriteCString(second, "Second");
		platform.WriteUInt32(entries, 0, first.Raw);
		platform.WriteUInt32(entries, 4, second.Raw);
		platform.WriteUInt32(entries, 8, 0);
		var cycleClass = Register(ref platform, 0x11C0, "Cycle.mui");
		var cycle = MuiCommonControlCore.CreateControl(ref platform, State,
			cycleClass, APTR.Null);
		Set(ref platform, cycle, CycleEntries, entries.Raw);
		Set(ref platform, cycle, CycleActive, 1);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, cycle, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, cycle, 0, 0,
			80, 16));
		var cycleTextBefore = platform.TextCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			cycle, packet));
		Assert.Equal(cycleTextBefore + 1, platform.TextCount);

		var rectangleClass = Register(ref platform, 0x1240, "Rectangle.mui");
		var rectangle = MuiCommonControlCore.CreateControl(ref platform, State,
			rectangleClass, APTR.Null);
		var title = APTR.FromPointer(0x31C0);
		platform.WriteCString(title, "Bar");
		Set(ref platform, rectangle, RectangleHBar, 1);
		Set(ref platform, rectangle, RectangleVBar, 1);
		Set(ref platform, rectangle, RectangleBarTitle, title.Raw);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, rectangle,
			renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, rectangle, 0, 0,
			80, 16));
		var linesBefore = platform.LineCount;
		var textBeforeRectangle = platform.TextCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			rectangle, packet));
		Assert.Equal(linesBefore + 2, platform.LineCount);
		Assert.Equal(textBeforeRectangle + 1, platform.TextCount);
	}

	[Fact]
	public void RectangleBarTitleUsesNamedGuestRecordAndTracksProjection()
	{
		var platform = NewPlatform();
		var rectangleClass = Register(ref platform, 0x1100, "Rectangle.mui");
		var title = APTR.FromPointer(0x3200);
		platform.WriteCString(title, "First");
		var rectangle = MuiCommonControlCore.CreateControl(ref platform, State,
			rectangleClass, BuildTags(ref platform, 0x1900, new[] {
				(RectangleBarTitle, title.Raw) }));
		Assert.True(MuiCommonControlCore.TryGetRectangleBarTitleStateRecord(
			ref platform, State, rectangle, out var record));
		Assert.Equal(MuiRectangleBarTitleStateRecord.Cookie, record.Magic);
		Assert.Equal(1u, record.Present);
		Assert.Equal(title.Raw, record.Title.Raw);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, rectangle,
			RectangleBarTitle, out var projected, out var handled));
		Assert.True(handled);
		Assert.Equal(title.Raw, projected);
		Assert.Equal(title.Raw, Get(ref platform, rectangle, RectangleBarTitle));
		var getMessage = APTR.FromPointer(0x3280);
		var getStorage = APTR.FromPointer(0x32C0);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			RectangleBarTitle));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			rectangle, getMessage));
		Assert.Equal(title.Raw, platform.ReadUInt32(getStorage, 0));

		var replacement = APTR.FromPointer(0x3240);
		platform.WriteCString(replacement, "Second");
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			rectangle, RectangleBarTitle, replacement.Raw, false));
		Assert.True(MuiCommonControlCore.TryReadRectangleBarTitleState(
			ref platform, State, rectangle, out var state));
		Assert.True(state.Present);
		Assert.Equal(replacement.Raw, state.Title.Raw);
		Assert.True(MuiCommonControlCore.TryGetRectangleBarTitleStateRecord(
			ref platform, State, rectangle, out record));
		Assert.Equal(replacement.Raw, record.Title.Raw);

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			rectangle));
	}

	[Fact]
	public void ImageSpecStringsParseIntoKindsColoursAndIds()
	{
		var platform = NewPlatform();

		// "2:rrggbb" solid colour.
		var color6 = APTR.FromPointer(0x3200);
		platform.WriteCString(color6, "2:ff8040");
		Assert.True(MuiCommonControlCore.TryParseImageSpec(ref platform, color6,
			out var parsedColor));
		Assert.Equal(MuiImageSpecKind.Color, parsedColor.Kind);
		Assert.Equal(0xFFu, parsedColor.Red);
		Assert.Equal(0x80u, parsedColor.Green);
		Assert.Equal(0x40u, parsedColor.Blue);
		Assert.Equal(0xFF8040u, parsedColor.Value);

		// Eight hex digits per channel collapse to the high byte of each channel.
		var color24 = APTR.FromPointer(0x3240);
		platform.WriteCString(color24, "2:ffffffff8080808040404040");
		Assert.True(MuiCommonControlCore.TryParseImageSpec(ref platform, color24,
			out var parsedWide));
		Assert.Equal(MuiImageSpecKind.Color, parsedWide.Kind);
		Assert.Equal(0xFF8040u, parsedWide.Value);

		// "0:x" builtin background pattern.
		var pattern = APTR.FromPointer(0x3280);
		platform.WriteCString(pattern, "0:3");
		Assert.True(MuiCommonControlCore.TryParseImageSpec(ref platform, pattern,
			out var parsedPattern));
		Assert.Equal(MuiImageSpecKind.BackgroundPattern, parsedPattern.Kind);
		Assert.Equal(3u, parsedPattern.Value);

		// "1:x" builtin image and "6:x" preconfigured image / background.
		var builtin = APTR.FromPointer(0x32C0);
		platform.WriteCString(builtin, "1:5");
		Assert.True(MuiCommonControlCore.TryParseImageSpec(ref platform, builtin,
			out var parsedBuiltin));
		Assert.Equal(MuiImageSpecKind.BuiltinImage, parsedBuiltin.Kind);
		Assert.Equal(5u, parsedBuiltin.Value);

		var preconfigured = APTR.FromPointer(0x3300);
		platform.WriteCString(preconfigured, "6:12");
		Assert.True(MuiCommonControlCore.TryParseImageSpec(ref platform,
			preconfigured, out var parsedPre));
		Assert.Equal(MuiImageSpecKind.Preconfigured, parsedPre.Kind);
		Assert.Equal(12u, parsedPre.Value);

		// Out-of-range kind, malformed colour, and non-spec pointers are rejected.
		var badKind = APTR.FromPointer(0x3340);
		platform.WriteCString(badKind, "9:1");
		Assert.False(MuiCommonControlCore.TryParseImageSpec(ref platform, badKind,
			out var badKindResult));
		Assert.Equal(MuiImageSpecKind.Invalid, badKindResult.Kind);

		var badColor = APTR.FromPointer(0x3380);
		platform.WriteCString(badColor, "2:zz");
		Assert.False(MuiCommonControlCore.TryParseImageSpec(ref platform, badColor,
			out _));

		var text = APTR.FromPointer(0x33C0);
		platform.WriteCString(text, "hello");
		Assert.False(MuiCommonControlCore.TryParseImageSpec(ref platform, text,
			out _));

		// A zeroed pointer (the conventional raw-drawable case) is not a spec.
		Assert.False(MuiCommonControlCore.TryParseImageSpec(ref platform,
			APTR.FromPointer(0x3000), out _));
	}

	[Fact]
	public void ImageSpecStringsRenderAsResolvedFillsNotRawDrawables()
	{
		var platform = NewPlatform();
		var imageClass = Register(ref platform, 0x11C0, "Image.mui");
		var renderInfo = APTR.FromPointer(0x1480);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		var packet = APTR.FromPointer(0x1440);
		platform.WriteUInt32(packet, 0, Draw);
		platform.WriteUInt32(packet, 4, 0);

		// A "2:rrggbb" colour spec resolves through the object store and renders
		// as a colour fill, never as a raw drawable image.
		var colorSpec = APTR.FromPointer(0x3200);
		platform.WriteCString(colorSpec, "2:ff8040");
		var colorImage = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, APTR.Null);
		Set(ref platform, colorImage, ImageSpec, colorSpec.Raw);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, colorImage,
			renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, colorImage, 0, 0,
			16, 16));
		var fillBefore = platform.FillCount;
		var imageBefore = platform.ImageCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			colorImage, packet));
		Assert.Equal((uint)MuiImageSpecKind.Color, Get(ref platform, colorImage,
			ImageResolvedKindKey));
		Assert.Equal(0xFF8040u, Get(ref platform, colorImage,
			ImageResolvedValueKey));
		Assert.Equal(0xFF8040u, platform.LastPen);
		Assert.True(platform.FillCount > fillBefore);
		Assert.Equal(imageBefore, platform.ImageCount);

		// A "0:x" pattern spec also resolves to a fill.
		var patternSpec = APTR.FromPointer(0x3280);
		platform.WriteCString(patternSpec, "0:3");
		var patternImage = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, APTR.Null);
		Set(ref platform, patternImage, ImageSpec, patternSpec.Raw);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, patternImage,
			renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, patternImage, 0,
			0, 16, 16));
		var patternFillBefore = platform.FillCount;
		var patternImageBefore = platform.ImageCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			patternImage, packet));
		Assert.Equal((uint)MuiImageSpecKind.BackgroundPattern, Get(ref platform,
			patternImage, ImageResolvedKindKey));
		Assert.Equal(3u, Get(ref platform, patternImage, ImageResolvedValueKey));
		Assert.True(platform.FillCount > patternFillBefore);
		Assert.Equal(patternImageBefore, platform.ImageCount);

		// A non-spec pointer is still treated as a raw drawable.
		var rawImage = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, APTR.Null);
		Set(ref platform, rawImage, ImageSpec, 0x3000);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, rawImage,
			renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, rawImage, 0, 0,
			16, 16));
		var rawImageBefore = platform.ImageCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			rawImage, packet));
		Assert.Equal(rawImageBefore + 1, platform.ImageCount);
	}

	[Fact]
	public void ImageSpecUsesNamedGuestRecordAndPreservesAbsentUnion()
	{
		var platform = NewPlatform();
		var imageClass = Register(ref platform, 0x1100, "Image.mui");
		var image = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, APTR.Null);
		Assert.NotEqual(APTR.Null, image);
		Assert.True(MuiCommonControlCore.TryGetImageSpecStateRecord(
			ref platform, State, image, out var record));
		Assert.Equal(MuiImageSpecStateRecord.Cookie, record.Magic);
		Assert.Equal(0u, record.Present);
		Assert.False(MuiCommonControlCore.TryReadImageSpecState(ref platform, State,
			image, out _));

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			image, ImageSpec, 0x0Bu));
		Assert.True(MuiCommonControlCore.TryReadImageSpecState(ref platform, State,
			image, out var state));
		Assert.True(state.Present);
		Assert.Equal(0x0Bu, state.Raw);
		Assert.True(MuiCommonControlCore.TryGetImageSpecStateRecord(
			ref platform, State, image, out record));
		Assert.Equal(1u, record.Present);
		Assert.Equal(0x0Bu, record.Raw);

		var pointerSpec = APTR.FromPointer(0x3200);
		platform.WriteCString(pointerSpec, "2:ff8040");
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			image, ImageSpec, pointerSpec.Raw));
		Assert.True(MuiCommonControlCore.TryReadImageSpecState(ref platform, State,
			image, out state));
		Assert.Equal(pointerSpec.Raw, state.Raw);
		Assert.True(MuiCommonControlCore.TryGetImageSpecStateRecord(
			ref platform, State, image, out record));
		Assert.Equal(pointerSpec.Raw, record.Raw);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, image,
			ImageSpec, out var projectedSpec, out var specHandled));
		Assert.True(specHandled);
		Assert.Equal(pointerSpec.Raw, projectedSpec);
		Assert.Equal(pointerSpec.Raw, Get(ref platform, image, ImageSpec));
		var specGetMessage = APTR.FromPointer(0x3380);
		var specGetStorage = APTR.FromPointer(0x33C0);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			specGetMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			specGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			ImageSpec));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			specGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			specGetStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			image, specGetMessage));
		Assert.Equal(pointerSpec.Raw, platform.ReadUInt32(specGetStorage, 0));

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, image));
	}

	[Fact]
	public void ImageBuiltinSpecUsesNamedUnionStateAndRendersFallback()
	{
		var platform = NewPlatform();
		var imageClass = Register(ref platform, 0x1100, "Image.mui");
		var image = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, APTR.Null);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			image, ImageBuiltinSpec, 0x0B));
		Assert.True(MuiCommonControlCore.TryReadImageSpecState(ref platform, State,
			image, out var semantic));
		Assert.False(semantic.Present);
		Assert.True(semantic.BuiltinPresent);
		Assert.Equal(0x0Bu, semantic.Builtin);
		Assert.True(MuiCommonControlCore.TryGetImageSpecStateRecord(
			ref platform, State, image, out var record));
		Assert.Equal(0u, record.Present);
		Assert.Equal(1u, record.BuiltinPresent);
		Assert.Equal(0x0Bu, record.Builtin);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, image,
			ImageBuiltinSpec, out var projectedBuiltin, out var builtinHandled));
		Assert.True(builtinHandled);
		Assert.Equal(0x0Bu, projectedBuiltin);
		Assert.Equal(0x0Bu, Get(ref platform, image, ImageBuiltinSpec));
		var builtinGetMessage = APTR.FromPointer(0x32C0);
		var builtinGetStorage = APTR.FromPointer(0x3300);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			builtinGetMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			builtinGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			ImageBuiltinSpec));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			builtinGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			builtinGetStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			image, builtinGetMessage));
		Assert.Equal(0x0Bu, platform.ReadUInt32(builtinGetStorage, 0));

		var renderInfo = APTR.FromPointer(0x1480);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, image,
			renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, image, 0, 0,
			16, 16));
		var packet = APTR.FromPointer(0x3280);
		platform.WriteUInt32(packet, 0, Draw);
		var lines = platform.LineCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			image, packet));
		Assert.True(platform.LineCount >= lines + 3);
	}

	[Fact]
	public void ImageOldImageIsPrimarySizedToStructAndInitOnly()
	{
		var platform = NewPlatform();
		var imageClass = Register(ref platform, 0x11C0, "Image.mui");

		// struct Image: Width at offset 4, Height at offset 6 (WORD each).
		var structImage = APTR.FromPointer(0x3400);
		platform.WriteUInt16(structImage, 4, 24);
		platform.WriteUInt16(structImage, 6, 20);
		var oldTags = BuildTags(ref platform, 0x1900, new[] {
			(ImageOldImage, structImage.Raw) });
		var image = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, oldTags);

		// OldImage is readable (init-only) and rejects a post-init set.
		Assert.Equal(structImage.Raw, Get(ref platform, image, ImageOldImage));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			image, ImageOldImage, 0x9999));

		// AskMinMax sizes the image to the struct dimensions and is not resizable.
		var storage = APTR.FromPointer(0x1500);
		var packet = APTR.FromPointer(0x1440);
		platform.WriteUInt32(packet, 0, AskMinMax);
		platform.WriteUInt32(packet, 4, storage.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			image, packet));
		Assert.Equal(24, platform.ReadUInt16(storage, 0)); // MinWidth
		Assert.Equal(20, platform.ReadUInt16(storage, 2)); // MinHeight
		Assert.Equal(24, platform.ReadUInt16(storage, 4)); // MaxWidth
		Assert.Equal(20, platform.ReadUInt16(storage, 6)); // MaxHeight

		// OldImage is drawn unconditionally as the primary image (state == 0).
		var renderInfo = APTR.FromPointer(0x1480);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		Assert.Equal(0u, Get(ref platform, image, ImageState));
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, image, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, image, 0, 0, 24,
			20));
		platform.WriteUInt32(packet, 0, Draw);
		platform.WriteUInt32(packet, 4, 0);
		var imageBefore = platform.ImageCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			image, packet));
		Assert.Equal(imageBefore + 1, platform.ImageCount);
	}

	[Fact]
	public void ImageOldImageUsesNamedGuestRecordAndTracksScalarProjection()
	{
		var platform = NewPlatform();
		var imageClass = Register(ref platform, 0x1100, "Image.mui");
		var oldImage = APTR.FromPointer(0x3600);
		var image = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, BuildTags(ref platform, 0x1900, new[] {
				(ImageOldImage, oldImage.Raw) }));
		Assert.NotEqual(APTR.Null, image);
		Assert.True(MuiCommonControlCore.TryGetImageOldImageStateRecord(
			ref platform, State, image, out var record));
		Assert.Equal(MuiImageOldImageStateRecord.Cookie, record.Magic);
		Assert.Equal(oldImage.Raw, record.Image.Raw);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, image,
			ImageOldImage, out var projected, out var handled));
		Assert.True(handled);
		Assert.Equal(oldImage.Raw, projected);
		Assert.Equal(oldImage.Raw, Get(ref platform, image, ImageOldImage));
		var getMessage = APTR.FromPointer(0x36C0);
		var getStorage = APTR.FromPointer(0x3700);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			ImageOldImage));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			image, getMessage));
		Assert.Equal(oldImage.Raw, platform.ReadUInt32(getStorage, 0));

		var replacement = APTR.FromPointer(0x3680);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, image,
			ImageOldImage, replacement.Raw, false));
		Assert.True(MuiCommonControlCore.TryReadImageOldImageState(ref platform,
			State, image, out var state));
		Assert.Equal(replacement.Raw, state.Image.Raw);
		Assert.True(MuiCommonControlCore.TryGetImageOldImageStateRecord(
			ref platform, State, image, out record));
		Assert.Equal(replacement.Raw, record.Image.Raw);

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, image));
	}

	[Fact]
	public void ImageFreeHorizVertWidenAskMinMax()
	{
		var platform = NewPlatform();
		var imageClass = Register(ref platform, 0x11C0, "Image.mui");
		var storage = APTR.FromPointer(0x1500);
		var packet = APTR.FromPointer(0x1440);
		platform.WriteUInt32(packet, 0, AskMinMax);
		platform.WriteUInt32(packet, 4, storage.Raw);

		var horizTags = BuildTags(ref platform, 0x1900, new[] {
			(ImageFreeHoriz, 1u) });
		var horiz = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, horizTags);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			horiz, packet));
		Assert.Equal(16, platform.ReadUInt16(storage, 0));    // MinWidth
		Assert.Equal(16, platform.ReadUInt16(storage, 2));    // MinHeight
		Assert.Equal(10000, platform.ReadUInt16(storage, 4)); // MaxWidth widened
		Assert.Equal(16, platform.ReadUInt16(storage, 6));    // MaxHeight fixed

		var vertTags = BuildTags(ref platform, 0x1A00, new[] {
			(ImageFreeVert, 1u) });
		var vert = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, vertTags);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			vert, packet));
		Assert.Equal(16, platform.ReadUInt16(storage, 4));    // MaxWidth fixed
		Assert.Equal(10000, platform.ReadUInt16(storage, 6)); // MaxHeight widened

		var bothTags = BuildTags(ref platform, 0x1B00, new[] {
			(ImageFreeHoriz, 1u), (ImageFreeVert, 1u) });
		var both = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, bothTags);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			both, packet));
		Assert.Equal(10000, platform.ReadUInt16(storage, 4));
		Assert.Equal(10000, platform.ReadUInt16(storage, 6));
	}

	[Fact]
	public void ImageRenderStateUsesNamedGuestRecordForGetAndOmGet()
	{
		var platform = NewPlatform();
		var imageClass = Register(ref platform, 0x11C0, "Image.mui");
		var image = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, BuildTags(ref platform, 0x1C00, new[] {
				(ImageFreeHoriz, 1u), (ImageFreeVert, 0u), (Selected, 1u) }));
		Assert.True(MuiCommonControlCore.TryGetImageRenderStateRecord(
			ref platform, State, image, out var record));
		Assert.Equal(MuiImageRenderStateRecord.Cookie, record.Magic);
		Assert.Equal(0u, record.ImageState);
		Assert.Equal(1u, record.Selected);
		Assert.Equal(1u, record.FreeHoriz);
		Assert.Equal(0u, record.FreeVert);

		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, image,
			ImageState, out var projected, out var handled));
		Assert.True(handled);
		Assert.Equal(0u, projected);
		Assert.Equal(1u, Get(ref platform, image, Selected));
		Assert.Equal(1u, Get(ref platform, image, ImageFreeHoriz));
		Assert.Equal(0u, Get(ref platform, image, ImageFreeVert));

		var getMessage = APTR.FromPointer(0x1C80);
		var getStorage = APTR.FromPointer(0x1CC0);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			ImageFreeHoriz));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			image, getMessage));
		Assert.Equal(1u, platform.ReadUInt32(getStorage, 0));
	}

	[Fact]
	public void NonInteractiveControlsDoNotConsumeNumericInputPackets()
	{
		var platform = NewPlatform();
		var textClass = Register(ref platform, 0x1100, "Text.mui");
		var text = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, APTR.Null);
		var packet = APTR.FromPointer(0x1400);
		platform.WriteUInt32(packet, 0, NumericIncrease);
		platform.WriteUInt32(packet, 4, 5);
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			text, packet));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State, text,
			NumericValue, out _));

		platform.WriteUInt32(packet, 0, HandleEvent);
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyDown));
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			text, packet));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State, text,
			NumericValue, out _));
	}

	[Fact]
	public void ImageSelectionTracksStateAndGadgetExposesWrappedPointer()
	{
		var platform = NewPlatform();
		var imageClass = Register(ref platform, 0x1100, "Image.mui");
		var gadgetClass = Register(ref platform, 0x1140, "Gadget.mui");
		var imageTags = BuildTags(ref platform, 0x1800,
			new[] { (InputMode, 1u) });
		var image = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, imageTags);
		Assert.Equal(0u, Get(ref platform, image, ImageState));
		var packet = APTR.FromPointer(0x1900);
		platform.WriteUInt32(packet, 0, HandleEvent);
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyPress));
		platform.WriteUInt32(packet, 12, 0);
		var redraws = platform.RedrawCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			image, packet));
		Assert.Equal(1u, Get(ref platform, image, Selected));
		Assert.Equal(1u, Get(ref platform, image, ImageState));
		Assert.True(platform.RedrawCount > redraws);
		Set(ref platform, image, Disabled, 1);
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			image, packet));
		Assert.Equal(1u, Get(ref platform, image, ImageState));

		var wrapped = APTR.FromPointer(0x3000);
		var gadgetTags = BuildTags(ref platform, 0x1840,
			new[] { (GadgetGadget, wrapped.Raw) });
		var gadget = MuiCommonControlCore.CreateControl(ref platform, State,
			gadgetClass, gadgetTags);
		Assert.Equal(wrapped.Raw, Get(ref platform, gadget, GadgetGadget));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			gadget, GadgetGadget, 0x3010));
		Assert.Equal(wrapped.Raw, Get(ref platform, gadget, GadgetGadget));
	}

	[Fact]
	public void GadgetGadgetUsesNamedRecordForGetOmGetAndRawSync()
	{
		var platform = NewPlatform();
		var gadgetClass = Register(ref platform, 0x3A00, "Gadget.mui");
		var wrapped = APTR.FromPointer(0x3A40);
		var gadget = MuiCommonControlCore.CreateControl(ref platform, State,
			gadgetClass, BuildTags(ref platform, 0x3A80, new[] {
				(GadgetGadget, wrapped.Raw) }));

		Assert.True(MuiCommonControlCore.TryGetGadgetGadgetStateRecord(
			ref platform, State, gadget, out var record));
		Assert.Equal(MuiGadgetGadgetStateRecord.Cookie, record.Magic);
		Assert.Equal(wrapped.Raw, record.Gadget.Raw);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, gadget,
			GadgetGadget, out var projected, out var handled));
		Assert.True(handled);
		Assert.Equal(wrapped.Raw, projected);

		var getMessage = APTR.FromPointer(0x3AC0);
		var getStorage = APTR.FromPointer(0x3B00);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			GadgetGadget));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			gadget, getMessage));
		Assert.Equal(wrapped.Raw, platform.ReadUInt32(getStorage, 0));

		var replacement = APTR.FromPointer(0x3B40);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, gadget,
			GadgetGadget, replacement.Raw, false));
		Assert.Equal(replacement.Raw, Get(ref platform, gadget, GadgetGadget));
		Assert.True(MuiCommonControlCore.TryGetGadgetGadgetStateRecord(
			ref platform, State, gadget, out record));
		Assert.Equal(replacement.Raw, record.Gadget.Raw);
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			gadget, GadgetGadget, wrapped.Raw));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			gadget));
	}

	[Fact]
	public void ImageSpecsParseWithoutTreatingSpecStringsAsBitmaps()
	{
		var platform = NewPlatform();
		var imageClass = Register(ref platform, 0x1100, "Image.mui");
		var image = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, APTR.Null);
		Assert.Equal(0u, Get(ref platform, image, ImageFontMatch));
		Assert.Equal(0u, Get(ref platform, image, ImageFontMatchHeight));
		Assert.Equal(0u, Get(ref platform, image, ImageFontMatchWidth));
		Assert.Equal(0u, Get(ref platform, image, ImageFreeHoriz));
		Assert.Equal(0u, Get(ref platform, image, ImageFreeVert));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			image, ImageFreeHoriz, 1));
		var spec = APTR.FromPointer(0x3200);
		platform.WriteCString(spec, "0:128");
		Assert.True(MuiCommonControlCore.TryParseImageSpec(ref platform, spec,
			out var parsed));
		Assert.Equal(MuiImageSpecKind.BackgroundPattern, parsed.Kind);
		Assert.Equal(128u, parsed.Value);

		var colour = APTR.FromPointer(0x3220);
		platform.WriteCString(colour, "2:11223344aabbccddeeff0011");
		Assert.True(MuiCommonControlCore.TryParseImageSpec(ref platform, colour,
			out parsed));
		Assert.Equal(MuiImageSpecKind.Color, parsed.Kind);
		Assert.Equal(0x11u, parsed.Red);
		Assert.Equal(0xAAu, parsed.Green);
		Assert.Equal(0xEEu, parsed.Blue);

		var invalid = APTR.FromPointer(0x3260);
		platform.WriteCString(invalid, "7:1");
		Assert.False(MuiCommonControlCore.TryParseImageSpec(ref platform, invalid,
			out _));

		var renderInfo = APTR.FromPointer(0x1480);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, image,
			renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, image, 0, 0,
			16, 16));
		var packet = APTR.FromPointer(0x3280);
		platform.WriteUInt32(packet, 0, Draw);
		Set(ref platform, image, ImageSpec, spec.Raw);
		var fills = platform.FillCount;
		var images = platform.ImageCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			image, packet));
		Assert.True(platform.FillCount >= fills + 2);
		Assert.Equal(images, platform.ImageCount);

		Set(ref platform, image, ImageSpec, 0x0000000B);
		var lines = platform.LineCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			image, packet));
		Assert.True(platform.LineCount >= lines + 3);

		platform.WriteCString(spec, "3:ExternalImage");
		Set(ref platform, image, ImageSpec, spec.Raw);
		images = platform.ImageCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			image, packet));
		Assert.Equal(images, platform.ImageCount);

		var oldImage = APTR.FromPointer(0x3300);
		platform.WriteUInt16(oldImage, 4, 12);
		platform.WriteUInt16(oldImage, 6, 10);
		var oldTags = BuildTags(ref platform, 0x3340,
			new[] { (ImageOldImage, oldImage.Raw) });
		var legacy = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, oldTags);
		var legacyStorage = APTR.FromPointer(0x3380);
		Assert.True(MuiCommonControlCore.AskMinMax(ref platform, State, legacy,
			legacyStorage));
		Assert.Equal(12, platform.ReadUInt16(legacyStorage, 0));
		Assert.Equal(10, platform.ReadUInt16(legacyStorage, 2));
	}


	[Fact]
	public void OrientationAttributesControlProgressAndTrackGeometry()
	{
		var platform = NewPlatform();
		var renderInfo = APTR.FromPointer(0x1480);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		var packet = APTR.FromPointer(0x1440);
		platform.WriteUInt32(packet, 0, Draw);
		platform.WriteUInt32(packet, 4, 0);

		var gaugeClass = Register(ref platform, 0x1100, "Gauge.mui");
		var gauge = MuiCommonControlCore.CreateControl(ref platform, State,
			gaugeClass, APTR.Null);
		Set(ref platform, gauge, GaugeMax, 100);
		Set(ref platform, gauge, GaugeCurrent, 25);
		Set(ref platform, gauge, GaugeHoriz, 0);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, gauge, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, gauge, 10, 20,
			40, 80));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			gauge, packet));
		Assert.Equal(10, platform.LastLeft);
		Assert.Equal(80, platform.LastTop);
		Assert.Equal(49, platform.LastRight);
		Assert.Equal(99, platform.LastBottom);

		var sliderClass = Register(ref platform, 0x1140, "Slider.mui");
		var slider = MuiCommonControlCore.CreateControl(ref platform, State,
			sliderClass, APTR.Null);
		Set(ref platform, slider, NumericMin, 0);
		Set(ref platform, slider, NumericMax, 100);
		Set(ref platform, slider, NumericValue, 50);
		Set(ref platform, slider, SliderHoriz, 0);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, slider, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, slider, 0, 0,
			20, 40));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			slider, packet));
		Assert.Equal(0, platform.LastLeft);
		Assert.Equal(20, platform.LastTop);
		Assert.Equal(19, platform.LastRight);
		Assert.Equal(21, platform.LastBottom);

		var propClass = Register(ref platform, 0x1180, "Prop.mui");
		var prop = MuiCommonControlCore.CreateControl(ref platform, State,
			propClass, APTR.Null);
		Set(ref platform, prop, PropEntries, 100);
		Set(ref platform, prop, PropVisible, 10);
		Set(ref platform, prop, PropFirst, 20);
		Set(ref platform, prop, PropHoriz, 0);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, prop, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, prop, 0, 0,
			20, 40));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			prop, packet));
		Assert.Equal(0, platform.LastLeft);
		Assert.Equal(28, platform.LastTop);
		Assert.Equal(19, platform.LastRight);
		Assert.Equal(31, platform.LastBottom);

		var scaleClass = Register(ref platform, 0x11C0, "Scale.mui");
		var scale = MuiCommonControlCore.CreateControl(ref platform, State,
			scaleClass, APTR.Null);
		Set(ref platform, scale, ScaleHoriz, 0);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, scale, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, scale, 0, 0,
			20, 40));
		var linesBefore = platform.LineCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			scale, packet));
		// Height 40 selects two divisions: one axis line plus three ticks.
		Assert.Equal(linesBefore + 4, platform.LineCount);
		Assert.Equal(0, platform.LastLineX1);
		Assert.Equal(39, platform.LastLineY1);
		Assert.Equal(19, platform.LastLineX2);
		Assert.Equal(39, platform.LastLineY2);
	}

	[Fact]
	public void TextSetLimitsAndShortenedStateFollowLayout()
	{
		var platform = NewPlatform();
		var textClass = Register(ref platform, 0x1100, "Text.mui");
		var source = APTR.FromPointer(0x1800);
		platform.WriteCString(source, "LongText");
		var tags = BuildTags(ref platform, 0x1900, new[] {
			(TextContents, source.Raw), (TextSetMin, 0u),
			(TextSetMax, 1u), (TextSetVMax, 0u) });
		var text = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, tags);
		var renderInfo = APTR.FromPointer(0x1480);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		var packet = APTR.FromPointer(0x1440);
		var storage = APTR.FromPointer(0x1400);
		platform.WriteUInt32(packet, 0, AskMinMax);
		platform.WriteUInt32(packet, 4, storage.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			text, packet));
		Assert.Equal(0, platform.ReadUInt16(storage, 0));
		Assert.Equal(0, platform.ReadUInt16(storage, 2));
		Assert.Equal(64, platform.ReadUInt16(storage, 4));
		Assert.Equal(10000, platform.ReadUInt16(storage, 6));
		Assert.Equal(64, platform.ReadUInt16(storage, 8));

		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, text, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, text, 0, 0,
			16, 10));
		platform.WriteUInt32(packet, 0, Draw);
		platform.WriteUInt32(packet, 4, 0);
	Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
		text, packet));
		Assert.Equal(1u, Get(ref platform, text, TextShortened));
		Assert.True(MuiCommonControlCore.TryGetTextShortenedStateRecord(
			ref platform, State, text, out var record));
		Assert.Equal(MuiTextShortenedStateRecord.Cookie, record.Magic);
		Assert.Equal(1u, record.Shortened);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, text,
			TextShortened, out var projected, out var handled));
		Assert.True(handled);
		Assert.Equal(1u, projected);
		var getMessage = APTR.FromPointer(0x1C00);
		var getStorage = APTR.FromPointer(0x1C40);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			TextShortened));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			text, getMessage));
		Assert.Equal(1u, platform.ReadUInt32(getStorage, 0));
	}

	[Fact]
	public void BitmapFamilySetupExposesRemappedStateWithoutFreeing()
	{
		var platform = NewPlatform();
		var bitmapClass = Register(ref platform, 0x1100, "Bitmap.mui");
		var bitmap = MuiCommonControlCore.CreateControl(ref platform, State,
			bitmapClass, APTR.Null);
		Assert.Equal(0u, Get(ref platform, bitmap, BitmapRemapped));
		Assert.Equal(0u, Get(ref platform, bitmap, BitmapAlpha));
		Assert.Equal(0u, Get(ref platform, bitmap, BitmapMappingTable));
		Assert.Equal(0u, Get(ref platform, bitmap, BitmapPrecision));
		Assert.Equal(0u, Get(ref platform, bitmap, BitmapSourceColors));
		Assert.Equal(0u, Get(ref platform, bitmap, BitmapTransparent));
		Assert.Equal(0u, Get(ref platform, bitmap, BitmapUseFriend));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapWidth, 24));
		Assert.Equal(24u, Get(ref platform, bitmap, BitmapWidth));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapRemapped, 0x3100));

		var sourceBitmap = APTR.FromPointer(0x3000);
		platform.WriteUInt32(sourceBitmap, 0, 0xC0FFEE00);
		Set(ref platform, bitmap, BitmapBitmap, sourceBitmap.Raw);
		Set(ref platform, bitmap, BitmapWidth, 24);
		Set(ref platform, bitmap, BitmapHeight, 12);

		var renderInfo = APTR.FromPointer(0x1480);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		var packet = APTR.FromPointer(0x1440);
		platform.WriteUInt32(packet, 0, Setup);
		platform.WriteUInt32(packet, 4, renderInfo.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			bitmap, packet));
		Assert.Equal(sourceBitmap.Raw, Get(ref platform, bitmap, BitmapRemapped));

		// AskMinMax reflects the caller bitmap dimensions.
		var storage = APTR.FromPointer(0x1400);
		platform.WriteUInt32(packet, 0, AskMinMax);
		platform.WriteUInt32(packet, 4, storage.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			bitmap, packet));
		Assert.Equal(24, platform.ReadUInt16(storage, 0));
		Assert.Equal(12, platform.ReadUInt16(storage, 2));

		platform.WriteUInt32(packet, 0, Cleanup);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			bitmap, packet));
		Assert.Equal(0u, Get(ref platform, bitmap, BitmapRemapped));

		// The caller-owned bitmap is never freed.
		var freedBefore = platform.FreeCount;
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			bitmap));
		Assert.Equal(0xC0FFEE00u, platform.ReadUInt32(sourceBitmap, 0));
		Assert.True(platform.FreeCount > freedBefore);
	}

	[Fact]
	public void BitmapRemappedUsesNamedRecordForRebuildGetAndCleanup()
	{
		var platform = NewPlatform();
		var bitmapClass = Register(ref platform, 0x3600, "Bitmap.mui");
		var bitmap = MuiCommonControlCore.CreateControl(ref platform, State,
			bitmapClass, APTR.Null);
		Assert.True(MuiCommonControlCore.TryGetBitmapRemappedStateRecord(
			ref platform, State, bitmap, out var initial));
		Assert.Equal(MuiBitmapRemappedStateRecord.Cookie, initial.Magic);
		Assert.Equal(0u, initial.Remapped.Raw);

		var source = APTR.FromPointer(0x3700);
		platform.WriteUInt32(source, 0, 0xC0FFEE00);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapBitmap, source.Raw));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapWidth, 16));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapHeight, 8));
		var renderInfo = APTR.FromPointer(0x3740);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		var packet = APTR.FromPointer(0x3780);
		platform.WriteUInt32(packet, 0, Setup);
		platform.WriteUInt32(packet, 4, renderInfo.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			bitmap, packet));

		Assert.True(MuiCommonControlCore.TryReadBitmapRemappedState(ref platform,
			State, bitmap, out var remappedState));
		Assert.Equal(source.Raw, remappedState.Remapped.Raw);
		Assert.True(MuiCommonControlCore.TryGetBitmapRemappedStateRecord(
			ref platform, State, bitmap, out var remappedRecord));
		Assert.Equal(source.Raw, remappedRecord.Remapped.Raw);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, bitmap,
			BitmapRemapped, out var projected, out var handled));
		Assert.True(handled);
		Assert.Equal(source.Raw, projected);

		var getMessage = APTR.FromPointer(0x37C0);
		var getStorage = APTR.FromPointer(0x3800);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			BitmapRemapped));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			bitmap, getMessage));
		Assert.Equal(source.Raw, platform.ReadUInt32(getStorage, 0));

		Assert.True(MuiCommonControlCore.CleanupBitmap(ref platform, State, bitmap));
		Assert.True(MuiCommonControlCore.TryGetBitmapRemappedStateRecord(
			ref platform, State, bitmap, out remappedRecord));
		Assert.Equal(0u, remappedRecord.Remapped.Raw);
		Assert.Equal(0u, Get(ref platform, bitmap, BitmapRemapped));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			bitmap));
	}

	[Fact]
	public void BitmapFamilySourceUsesNamedGuestRecordForBothSourceAttributes()
	{
		var platform = NewPlatform();
		var bitmapClass = Register(ref platform, 0x1100, "Bitmap.mui");
		var bitmap = MuiCommonControlCore.CreateControl(ref platform, State,
			bitmapClass, APTR.Null);
		Assert.True(MuiCommonControlCore.TryGetBitmapSourceStateRecord(
			ref platform, State, bitmap, out var record));
		Assert.Equal(MuiBitmapSourceStateRecord.Cookie, record.Magic);
		Assert.Equal(0u, record.Source.Raw);

		var bitmapSource = APTR.FromPointer(0x3000);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapBitmap, bitmapSource.Raw));
		Assert.True(MuiCommonControlCore.TryReadBitmapSourceState(ref platform,
			State, bitmap, MuiControlClass.Bitmap, out var sourceState));
		Assert.Equal(bitmapSource.Raw, sourceState.Source.Raw);
		Assert.True(MuiCommonControlCore.TryGetBitmapSourceStateRecord(
			ref platform, State, bitmap, out record));
		Assert.Equal(bitmapSource.Raw, record.Source.Raw);

		var bodyClass = Register(ref platform, 0x1140, "Bodychunk.mui");
		var body = MuiCommonControlCore.CreateControl(ref platform, State,
			bodyClass, APTR.Null);
		var bodySource = APTR.FromPointer(0x3040);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			body, BodychunkBody, bodySource.Raw));
		Assert.True(MuiCommonControlCore.TryReadBitmapSourceState(ref platform,
			State, body, MuiControlClass.Bodychunk, out sourceState));
		Assert.Equal(bodySource.Raw, sourceState.Source.Raw);
		Assert.True(MuiCommonControlCore.TryGetBitmapSourceStateRecord(
			ref platform, State, body, out record));
		Assert.Equal(bodySource.Raw, record.Source.Raw);

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, body));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			bitmap));
	}

	[Fact]
	public void BitmapFamilyNamedRecordsProjectGetAndOmGet()
	{
		var platform = NewPlatform();
		var bitmapClass = Register(ref platform, 0x1100, "Bitmap.mui");
		var bitmap = MuiCommonControlCore.CreateControl(ref platform, State,
			bitmapClass, APTR.Null);
		var source = APTR.FromPointer(0x3100);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapBitmap, source.Raw));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapWidth, 32));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapHeight, 8));
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, bitmap,
			BitmapBitmap, out var projectedSource, out var sourceHandled));
		Assert.True(sourceHandled);
		Assert.Equal(source.Raw, projectedSource);
		Assert.Equal(32u, Get(ref platform, bitmap, BitmapWidth));
		Assert.Equal(8u, Get(ref platform, bitmap, BitmapHeight));

		var bitmapGetMessage = APTR.FromPointer(0x3140);
		var bitmapGetStorage = APTR.FromPointer(0x3180);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			bitmapGetMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			bitmapGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			BitmapHeight));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			bitmapGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			bitmapGetStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			bitmap, bitmapGetMessage));
		Assert.Equal(8u, platform.ReadUInt32(bitmapGetStorage, 0));

		var bodyClass = Register(ref platform, 0x1140, "Bodychunk.mui");
		var body = MuiCommonControlCore.CreateControl(ref platform, State,
			bodyClass, APTR.Null);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			body, BodychunkCompression, 1));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			body, BodychunkDepth, 2));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			body, BodychunkMasking, 1));
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, body,
			BodychunkDepth, out var projectedDepth, out var depthHandled));
		Assert.True(depthHandled);
		Assert.Equal(2u, projectedDepth);
		Assert.Equal(1u, Get(ref platform, body, BodychunkCompression));
		Assert.Equal(1u, Get(ref platform, body, BodychunkMasking));

		var bodyGetMessage = APTR.FromPointer(0x31C0);
		var bodyGetStorage = APTR.FromPointer(0x3200);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			bodyGetMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			bodyGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			BodychunkDepth));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			bodyGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			bodyGetStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			body, bodyGetMessage));
		Assert.Equal(2u, platform.ReadUInt32(bodyGetStorage, 0));
	}

	[Fact]
	public void BitmapPolicyUsesNamedRecordForGetOmGetAndRuntimeSetters()
	{
		var platform = NewPlatform();
		var bitmapClass = Register(ref platform, 0x3400, "Bitmap.mui");
		var mappingTable = APTR.FromPointer(0x3500);
		var sourceColors = APTR.FromPointer(0x3520);
		var bitmap = MuiCommonControlCore.CreateControl(ref platform, State,
			bitmapClass, BuildTags(ref platform, 0x3440, new[] {
				(BitmapAlpha, 1u), (BitmapMappingTable, mappingTable.Raw),
				(BitmapPrecision, 2u), (BitmapSourceColors, sourceColors.Raw),
				(BitmapTransparent, 7u), (BitmapUseFriend, 0x3540u) }));

		Assert.True(MuiCommonControlCore.TryGetBitmapPolicyStateRecord(
			ref platform, State, bitmap, out var record));
		Assert.Equal(MuiBitmapPolicyStateRecord.Cookie, record.Magic);
		Assert.Equal(1u, record.Alpha);
		Assert.Equal(mappingTable.Raw, record.MappingTable);
		Assert.Equal(2u, record.Precision);
		Assert.Equal(sourceColors.Raw, record.SourceColors);
		Assert.Equal(7u, record.Transparent);
		Assert.Equal(0x3540u, record.UseFriend);

		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, bitmap,
			BitmapMappingTable, out var projected, out var handled));
		Assert.True(handled);
		Assert.Equal(mappingTable.Raw, projected);
		Assert.Equal(1u, Get(ref platform, bitmap, BitmapAlpha));
		Assert.Equal(2u, Get(ref platform, bitmap, BitmapPrecision));
		Assert.Equal(sourceColors.Raw, Get(ref platform, bitmap,
			BitmapSourceColors));
		Assert.Equal(7u, Get(ref platform, bitmap, BitmapTransparent));
		Assert.Equal(0x3540u, Get(ref platform, bitmap, BitmapUseFriend));

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapAlpha, 3));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapPrecision, 4));
		Assert.True(MuiCommonControlCore.TryReadBitmapPolicyState(ref platform,
			State, bitmap, out var state));
		Assert.Equal(3u, state.Alpha);
		Assert.Equal(4u, state.Precision);

		// A persistence/bootstrap scalar write is folded into the named record.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, bitmap,
			BitmapTransparent, 9, false));
		Assert.True(MuiCommonControlCore.TryReadBitmapPolicyState(ref platform,
			State, bitmap, out state));
		Assert.Equal(9u, state.Transparent);
		Assert.True(MuiCommonControlCore.TryGetBitmapPolicyStateRecord(
			ref platform, State, bitmap, out record));
		Assert.Equal(9u, record.Transparent);

		var getMessage = APTR.FromPointer(0x3580);
		var getStorage = APTR.FromPointer(0x35C0);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			BitmapSourceColors));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			bitmap, getMessage));
		Assert.Equal(sourceColors.Raw, platform.ReadUInt32(getStorage, 0));

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			bitmap));
	}

	[Fact]
	public void RadioConstructsOwnsLaysOutAndDisposesOneChildPerEntry()
	{
		var platform = NewPlatform();
		var radioClass = Register(ref platform, 0x1100, "Radio.mui");
		var entries = APTR.FromPointer(0x1800);
		platform.WriteUInt32(entries, 0, 0x1900);
		platform.WriteUInt32(entries, 4, 0x1910);
		platform.WriteUInt32(entries, 8, 0);
		platform.WriteCString(APTR.FromPointer(0x1900), "First");
		platform.WriteCString(APTR.FromPointer(0x1910), "Second");
		var tags = BuildTags(ref platform, 0x1A00,
			new[] { (RadioEntries, entries.Raw) });
		var radio = MuiCommonControlCore.CreateControl(ref platform, State,
			radioClass, tags);
		Assert.True(radio.IsNotNull);
		var first = MuiFamilyCore.GetChild(ref platform, State, radio, 0, APTR.Null);
		var second = MuiFamilyCore.GetChild(ref platform, State, radio, 1, APTR.Null);
		Assert.True(first.IsNotNull);
		Assert.True(second.IsNotNull);
		Assert.True(MuiFamilyCore.GetChild(ref platform, State, radio, 2,
			APTR.Null).IsNull);

		var packet = APTR.FromPointer(0x1B00);
		platform.WriteUInt32(packet, 0, Layout);
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, 0);
		platform.WriteUInt32(packet, 12, 80);
		platform.WriteUInt32(packet, 16, 40);
		platform.WriteUInt32(packet, 20, 0);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			radio, packet));
		Assert.Equal(20u, Get(ref platform, first, ControlHeight));
		Assert.Equal(20u, Get(ref platform, second, ControlHeight));

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, radio));
		Assert.True(MuiHeadlessObjectCore.FindObject(ref platform, State, first).IsNull);
		Assert.True(MuiHeadlessObjectCore.FindObject(ref platform, State, second).IsNull);
	}


	[Fact]
	public void BodychunkByteRun1UsesDepthMaskingAndOwnedSetupStorage()
	{
		var platform = NewPlatform();
		var bodyClass = Register(ref platform, 0x1100, "Bodychunk.mui");
		var body = MuiCommonControlCore.CreateControl(ref platform, State,
			bodyClass, APTR.Null);
		Assert.Equal(1u, Get(ref platform, body, BodychunkDepth));
		Assert.Equal(0u, Get(ref platform, body, BodychunkCompression));
		Assert.Equal(0u, Get(ref platform, body, BodychunkMasking));
		Assert.Equal(0u, Get(ref platform, body, BitmapRemapped));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			body, BodychunkDepth, 2));
		Assert.Equal(2u, Get(ref platform, body, BodychunkDepth));
		var compressed = APTR.FromPointer(0x3000);
		platform.WriteUInt8(compressed, 0, 253); // Repeat the next byte four times.
		platform.WriteUInt8(compressed, 1, 0x5A);
		Set(ref platform, body, BodychunkBody, compressed.Raw);
		Set(ref platform, body, BitmapWidth, 16);
		Set(ref platform, body, BitmapHeight, 2);
		Set(ref platform, body, BodychunkDepth, 1);
		Set(ref platform, body, BodychunkMasking, 0);
		Set(ref platform, body, BodychunkCompression, 1);
		var renderInfo = APTR.FromPointer(0x1480);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		var packet = APTR.FromPointer(0x1440);
		platform.WriteUInt32(packet, 0, Setup);
		platform.WriteUInt32(packet, 4, renderInfo.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			body, packet));
		var decoded = APTR.FromPointer(Get(ref platform, body, BitmapRemapped));
		Assert.NotEqual(compressed.Raw, decoded.Raw);
		for (var index = 0; index < 4; index++)
			Assert.Equal(0x5A, platform.ReadUInt8(decoded, index));

		platform.WriteUInt32(packet, 0, Cleanup);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			body, packet));
		Assert.Equal(0u, Get(ref platform, body, BitmapRemapped));
		Assert.Equal(253, platform.ReadUInt8(compressed, 0));

		var invalid = MuiCommonControlCore.CreateControl(ref platform, State,
			bodyClass, APTR.Null);
		Set(ref platform, invalid, BodychunkBody, compressed.Raw);
		Set(ref platform, invalid, BitmapWidth, 16);
		Set(ref platform, invalid, BitmapHeight, 1);
		Set(ref platform, invalid, BodychunkDepth, 1);
		Set(ref platform, invalid, BodychunkCompression, 2);
		platform.WriteUInt32(packet, 0, Setup);
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			invalid, packet));
	}

	[Fact]
	public void GadgetInputModesTrackSelectionPressedStateAndRedraw()
	{
		var platform = NewPlatform();
		var gadgetClass = Register(ref platform, 0x1100, "Gadget.mui");
		var toggleTags = BuildTags(ref platform, 0x1900, new[] {
			(InputMode, InputModeToggle) });
		var toggle = MuiCommonControlCore.CreateControl(ref platform, State,
			gadgetClass, toggleTags);
		Assert.Equal(0u, Get(ref platform, toggle, Selected));
		Assert.Equal(0u, Get(ref platform, toggle, Pressed));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			toggle, InputMode, InputModeImmediate));

		var packet = APTR.FromPointer(0x1A00);
		platform.WriteUInt32(packet, 0, HandleEvent);
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyToggle));
		var redrawBefore = platform.RedrawCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			toggle, packet));
		Assert.Equal(1u, Get(ref platform, toggle, Selected));
		Assert.Equal(0u, Get(ref platform, toggle, Pressed));
		Assert.True(platform.RedrawCount > redrawBefore);

		var renderInfo = APTR.FromPointer(0x1480);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, toggle,
			renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, toggle, 4, 6,
			20, 12));
		platform.WriteUInt32(packet, 0, Draw);
		var linesBefore = platform.LineCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			toggle, packet));
		Assert.Equal(linesBefore + 4, platform.LineCount);
		Assert.Equal(3u, platform.LastPen);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			toggle, Disabled, 1));
		var disabledRedraw = platform.RedrawCount;
		platform.WriteUInt32(packet, 0, HandleEvent);
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyToggle));
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			toggle, packet));
		Assert.Equal(1u, Get(ref platform, toggle, Selected));
		Assert.Equal(disabledRedraw, platform.RedrawCount);
		platform.WriteUInt32(packet, 0, Draw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			toggle, packet));
		Assert.Equal(1u, platform.LastPen);

		var relativeTags = BuildTags(ref platform, 0x1B00, new[] {
			(InputMode, InputModeRelVerify) });
		var relative = MuiCommonControlCore.CreateControl(ref platform, State,
			gadgetClass, relativeTags);
		Assert.Equal(MuiControlClass.Gadget, MuiCommonControlCore.Classify(ref platform,
			State, relative));
		Assert.Equal(InputModeRelVerify, Get(ref platform, relative, InputMode));
		platform.WriteUInt32(packet, 0, HandleEvent);
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyPress));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			relative, packet));
		Assert.Equal(1u, Get(ref platform, relative, Selected));
		Assert.Equal(1u, Get(ref platform, relative, Pressed));
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyUp));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			relative, packet));
		Assert.Equal(1u, Get(ref platform, relative, Selected));
		Assert.Equal(0u, Get(ref platform, relative, Pressed));

		var immediateTags = BuildTags(ref platform, 0x1C00, new[] {
			(InputMode, InputModeImmediate) });
		var immediate = MuiCommonControlCore.CreateControl(ref platform, State,
			gadgetClass, immediateTags);
		platform.WriteUInt32(packet, 0, HandleEvent);
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyPress));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			immediate, packet));
		Assert.Equal(1u, Get(ref platform, immediate, Selected));
		Assert.Equal(1u, Get(ref platform, immediate, Pressed));
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyUp));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			immediate, packet));
		Assert.Equal(0u, Get(ref platform, immediate, Selected));
		Assert.Equal(0u, Get(ref platform, immediate, Pressed));
	}

	[Fact]
	public void GadgetInteractionUsesNamedGuestRecordForStateTransitions()
	{
		var platform = NewPlatform();
		var gadgetClass = Register(ref platform, 0x1E00, "Gadget.mui");
		var gadget = MuiCommonControlCore.CreateControl(ref platform, State,
			gadgetClass, BuildTags(ref platform, 0x1E40, new[] {
				(InputMode, InputModeToggle), (Selected, 1u) }));

		Assert.True(MuiCommonControlCore.TryGetGadgetInteractionStateRecord(
			ref platform, State, gadget, out var initial));
		Assert.Equal(MuiGadgetInteractionStateRecord.Cookie, initial.Magic);
		Assert.Equal(InputModeToggle, initial.InputMode);
		Assert.Equal(1u, initial.Selected);
		Assert.Equal(0u, initial.Pressed);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			gadget, Selected, 0));
		Assert.True(MuiCommonControlCore.TryReadGadgetInteractionState(
			ref platform, State, gadget, out var changed));
		Assert.Equal(0u, changed.Selected);

		// Compatibility paths may update the scalar directly; reading the typed
		// state reconciles that value before event handling or drawing consumes it.
		Set(ref platform, gadget, Pressed, 1);
		Assert.True(MuiCommonControlCore.TryReadGadgetInteractionState(
			ref platform, State, gadget, out var synchronized));
		Assert.Equal(1u, synchronized.Pressed);
		Assert.True(MuiCommonControlCore.TryGetGadgetInteractionStateRecord(
			ref platform, State, gadget, out var record));
		Assert.Equal(1u, record.Pressed);
	}

	[Fact]
	public void StringHandleEventEditsOwnedContentsAndHonorsLimits()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var source = APTR.FromPointer(0x1800);
		platform.WriteCString(source, "abc");
		var tags = BuildTags(ref platform, 0x1900, new[] {
			(StringMaxLen, 8u), (StringContents, source.Raw) });
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, tags);
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.Equal(3u, Get(ref platform, stringObj, StringBufferPos));

		var packet = APTR.FromPointer(0x1A00);
		var intuiMessage = APTR.FromPointer(0x1B80);
		platform.WriteUInt32(intuiMessage, 20, 0x00000400);
		platform.WriteUInt32(packet, 0, HandleEvent);
		platform.WriteUInt32(packet, 4, intuiMessage.Raw);
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyLeft));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));
		Assert.Equal(2u, Get(ref platform, stringObj, StringBufferPos));

		// Printable input inserts at the cursor and grows the owned dataspace.
		platform.WriteUInt16(intuiMessage, 24, (ushort)'X');
		platform.WriteUInt32(packet, 8, unchecked((uint)-1));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));
		Assert.Equal("abXc", ReadCString(ref platform, APTR.FromPointer(
			Get(ref platform, stringObj, StringContents))));
		Assert.Equal(3u, Get(ref platform, stringObj, StringBufferPos));

		platform.WriteUInt32(packet, 8, unchecked((uint)KeyBackspace));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));
		Assert.Equal("abc", ReadCString(ref platform, APTR.FromPointer(
			Get(ref platform, stringObj, StringContents))));
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyDelete));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));
		Assert.Equal("ab", ReadCString(ref platform, APTR.FromPointer(
			Get(ref platform, stringObj, StringContents))));

		platform.WriteUInt32(packet, 8, unchecked((uint)KeyHome));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyRight));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyEnd));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));
		Assert.Equal(2u, Get(ref platform, stringObj, StringBufferPos));

		platform.WriteUInt32(packet, 8, unchecked((uint)KeyPress));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));
		Assert.Equal(Get(ref platform, stringObj, StringContents),
			Get(ref platform, stringObj, StringAcknowledge));

		// Accept is a character-set pointer, and disabled/editable state blocks input.
		var accept = APTR.FromPointer(0x1B00);
		platform.WriteCString(accept, "Z");
		Set(ref platform, stringObj, StringAccept, accept.Raw);
		platform.WriteUInt16(intuiMessage, 24, (ushort)'A');
		platform.WriteUInt32(packet, 8, unchecked((uint)-1));
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));
		Set(ref platform, stringObj, StringEditable, 0);
		platform.WriteUInt16(intuiMessage, 24, (ushort)'Z');
		platform.WriteUInt32(packet, 8, unchecked((uint)-1));
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));
	}

	// Gap 12/14: keyboard stepping and disabled gating across the whole Numeric
	// family (shared behavior — asserted once per registered numeric class).
	[Fact]
	public void NumericFamilyKeyboardSteppingIsClassWideReverseAndDisabledAware()
	{
		var platform = NewPlatform();
		var numericClasses = new[]
		{
			("Numeric.mui", MuiControlClass.Numeric),
			("Levelmeter.mui", MuiControlClass.Levelmeter),
			("Slider.mui", MuiControlClass.Slider),
			("Knob.mui", MuiControlClass.Knob),
			("Numericbutton.mui", MuiControlClass.Numericbutton),
		};
		uint nameAddr = 0x1100;
		var packet = APTR.FromPointer(0x1400);
		platform.WriteUInt32(packet, 0, HandleEvent);
		platform.WriteUInt32(packet, 4, 0);
		foreach (var (name, cls) in numericClasses)
		{
			var cl = Register(ref platform, nameAddr, name);
			nameAddr += 0x40;
			var obj = MuiCommonControlCore.CreateControl(ref platform, State, cl,
				APTR.Null);
			Assert.Equal(cls, MuiCommonControlCore.Classify(ref platform, State, obj));
			Set(ref platform, obj, NumericMin, 0);
			Set(ref platform, obj, NumericMax, 100);
			Set(ref platform, obj, NumericValue, 50);

			// KeyUp steps forward by one, KeyDown steps back by one.
			platform.WriteUInt32(packet, 8, unchecked((uint)KeyUp));
			Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
				obj, packet));
			Assert.Equal(51u, Get(ref platform, obj, NumericValue));
			platform.WriteUInt32(packet, 8, unchecked((uint)KeyDown));
			Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
				obj, packet));
			Assert.Equal(50u, Get(ref platform, obj, NumericValue));

			// Reverse inverts the stepping direction for the same key.
			Set(ref platform, obj, NumericReverse, 1);
			platform.WriteUInt32(packet, 8, unchecked((uint)KeyUp));
			Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
				obj, packet));
			Assert.Equal(49u, Get(ref platform, obj, NumericValue));
			Set(ref platform, obj, NumericReverse, 0);
			Set(ref platform, obj, NumericValue, 50);

			// A disabled control consumes nothing and leaves the value untouched.
			Set(ref platform, obj, Disabled, 1);
			platform.WriteUInt32(packet, 8, unchecked((uint)KeyUp));
			Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
				obj, packet));
			Assert.Equal(50u, Get(ref platform, obj, NumericValue));
		}
	}

	[Fact]
	public void StringUnicodeModeUsesLogicalCursorsAndCompleteUtf8DrawSpans()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1200, "String.mui");
		var source = APTR.FromPointer(0x1D00);
		var utf8 = Encoding.UTF8.GetBytes("Åβ🙂x");
		for (var index = 0; index < utf8.Length; index++)
			platform.WriteUInt8(source, index, utf8[index]);
		platform.WriteUInt8(source, utf8.Length, 0);
		var tags = BuildTags(ref platform, 0x1E00, new[] {
			(StringContents, source.Raw), (Unicode, 1u) });
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, tags);
		Assert.NotEqual(APTR.Null, stringObj);
		// Å, β, 🙂 and x are four logical characters despite nine UTF-8 bytes.
		Assert.Equal(4u, Get(ref platform, stringObj, StringBufferPos));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringBufferPos, 99));
		Assert.Equal(4u, Get(ref platform, stringObj, StringBufferPos));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringDisplayPos, 99));
		Assert.Equal(4u, Get(ref platform, stringObj, StringDisplayPos));

		var packet = APTR.FromPointer(0x1F00);
		platform.WriteUInt32(packet, 0, HandleEvent);
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyLeft));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));
		Assert.Equal(3u, Get(ref platform, stringObj, StringBufferPos));

		// Draw from the second logical character in a 16-pixel box: β and 🙂
		// fit, and the reported span is exactly their six UTF-8 bytes.
		var owned = APTR.FromPointer(Get(ref platform, stringObj, StringContents));
		var renderInfo = APTR.FromPointer(0x2100);
		platform.WriteUInt32(renderInfo, 20, 0x2200);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, stringObj, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, stringObj, 0, 0,
			16, 14));
		Set(ref platform, stringObj, StringDisplayPos, 0);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringBufferPos, 4));
		Assert.Equal(2u, Get(ref platform, stringObj, StringDisplayPos));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringBufferPos, 1));
		Assert.Equal(1u, Get(ref platform, stringObj, StringDisplayPos));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringBufferPos, 3));
		Set(ref platform, stringObj, StringDisplayPos, 1);
		var drawPacket = APTR.FromPointer(0x2300);
		platform.WriteUInt32(drawPacket, 0, Draw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, drawPacket));
		Assert.Equal(6, platform.LastTextLength);
		Assert.Equal(owned.Raw + 2, platform.LastText.Raw);
		Assert.Equal("β🙂", Encoding.UTF8.GetString(ReadBytes(ref platform,
			platform.LastText, platform.LastTextLength)));

		// Backspace removes the complete four-byte emoji, not one continuation byte.
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyBackspace));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));
		Assert.Equal("Åβx", Encoding.UTF8.GetString(ReadBytes(ref platform, owned)));
		Assert.Equal(2u, Get(ref platform, stringObj, StringBufferPos));

		// Unicode printable input is encoded as a complete multi-byte character
		// before it is inserted into the owned guest buffer.
		var intuiMessage = APTR.FromPointer(0x2400);
		platform.WriteUInt32(packet, 4, intuiMessage.Raw);
		platform.WriteUInt32(intuiMessage, 20, 0x00000400);
		platform.WriteUInt16(intuiMessage, 24, 0x03B2);
		platform.WriteUInt32(packet, 8, unchecked((uint)-1));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));
		Assert.Equal("Åββx", Encoding.UTF8.GetString(ReadBytes(ref platform, owned)));
		Assert.Equal(3u, Get(ref platform, stringObj, StringBufferPos));

		Assert.True(MuiCommonControlCore.TryEncodeUtf8Input(0x1F642, true,
			out var emoji));
		Assert.Equal((byte)4, emoji.Length);
		Assert.Equal((byte)0xF0, emoji.First);
		Assert.Equal((byte)0x9F, emoji.Second);
		Assert.Equal((byte)0x99, emoji.Third);
		Assert.Equal((byte)0x82, emoji.Fourth);
		Assert.False(MuiCommonControlCore.TryEncodeUtf8Input(0xD800, true,
			out _));

		// MaxLen truncation also stops at a logical character boundary rather than
		// leaving an incomplete UTF-8 prefix in the owned contents.
		var boundedSource = APTR.FromPointer(0x2500);
		for (var index = 0; index < utf8.Length; index++)
			platform.WriteUInt8(boundedSource, index, utf8[index]);
		platform.WriteUInt8(boundedSource, utf8.Length, 0);
		var boundedTags = BuildTags(ref platform, 0x2600, new[] {
			(StringContents, boundedSource.Raw), (StringMaxLen, 4u), (Unicode, 1u) });
		var bounded = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, boundedTags);
		var boundedContents = APTR.FromPointer(Get(ref platform, bounded,
			StringContents));
		Assert.Equal("Åβ🙂", Encoding.UTF8.GetString(ReadBytes(ref platform,
			boundedContents)));
		Assert.Equal(3u, Get(ref platform, bounded, StringBufferPos));

		// In Unicode mode Accept/Reject strings are UTF-8 character sets rather
		// than byte bags, so continuation bytes cannot accidentally match.
		var filteredSource = APTR.FromPointer(0x2700);
		platform.WriteCString(filteredSource, "x");
		var filter = APTR.FromPointer(0x2800);
		platform.WriteUInt8(filter, 0, 0xCE);
		platform.WriteUInt8(filter, 1, 0xB2);
		platform.WriteUInt8(filter, 2, 0);
		var filteredTags = BuildTags(ref platform, 0x2900, new[] {
			(StringContents, filteredSource.Raw), (Unicode, 1u) });
		var filtered = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, filteredTags);
		Set(ref platform, filtered, StringAccept, filter.Raw);
		platform.WriteUInt16(intuiMessage, 24, 0x03B2);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			filtered, packet));
		var filteredContents = APTR.FromPointer(Get(ref platform, filtered,
			StringContents));
		Assert.Equal("xβ", Encoding.UTF8.GetString(ReadBytes(ref platform,
			filteredContents)));
		platform.WriteUInt16(intuiMessage, 24, 0x00C5);
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			filtered, packet));
		Assert.Equal("xβ", Encoding.UTF8.GetString(ReadBytes(ref platform,
			filteredContents)));
		Set(ref platform, filtered, StringAccept, 0);
		Set(ref platform, filtered, StringReject, filter.Raw);
		platform.WriteUInt16(intuiMessage, 24, 0x03B2);
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			filtered, packet));
	}

	[Fact]
	public void LevelmeterUsesNumericStateAndOwnsItsLabel()
	{
		var platform = NewPlatform();
		var levelmeterClass = Register(ref platform, 0x1100, "Levelmeter.mui");
		var label = APTR.FromPointer(0x1800);
		platform.WriteCString(label, "Levelmeter");
		var tags = BuildTags(ref platform, 0x1900, new[] {
			(NumericMin, 10u), (NumericMax, 90u), (NumericValue, 50u),
			(LevelmeterLabel, label.Raw) });
		var levelmeter = MuiCommonControlCore.CreateControl(ref platform, State,
			levelmeterClass, tags);
		Assert.Equal(MuiControlClass.Levelmeter,
			MuiCommonControlCore.Classify(ref platform, State, levelmeter));
		Assert.Equal(10u, Get(ref platform, levelmeter, NumericMin));
		Assert.Equal(90u, Get(ref platform, levelmeter, NumericMax));
		Assert.Equal(50u, Get(ref platform, levelmeter, NumericValue));
		var ownedLabel = APTR.FromPointer(Get(ref platform, levelmeter,
			LevelmeterLabel));
		Assert.NotEqual(label.Raw, ownedLabel.Raw);
		Assert.Equal("Levelm", ReadCString(ref platform, ownedLabel));

		// Numeric methods and SetAttrs operate on Numeric_Value, not the old
		// Gauge_Current attribute.
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			levelmeter, NumericValue, 100));
		Assert.Equal(90u, Get(ref platform, levelmeter, NumericValue));
		var packet = APTR.FromPointer(0x1440);
		platform.WriteUInt32(packet, 0, NumericDecrease);
		platform.WriteUInt32(packet, 4, 5);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			levelmeter, packet));
		Assert.Equal(85u, Get(ref platform, levelmeter, NumericValue));

		var renderInfo = APTR.FromPointer(0x1A80);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, levelmeter,
			renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, levelmeter, 0,
			0, 48, 14));
		platform.WriteUInt32(packet, 0, Draw);
		var fillsBefore = platform.FillCount;
		var textBefore = platform.TextCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			levelmeter, packet));
		Assert.True(platform.FillCount > fillsBefore);
		Assert.Equal(textBefore + 1, platform.TextCount);
		Assert.Equal(6, platform.LastTextLength);

		Set(ref platform, levelmeter, Disabled, 1);
		platform.WriteUInt32(packet, 0, HandleEvent);
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyUp));
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			levelmeter, packet));
		Assert.Equal(85u, Get(ref platform, levelmeter, NumericValue));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			levelmeter));
		Assert.Equal("Levelmeter", ReadCString(ref platform, label));
	}

	[Fact]
	public void LevelmeterPresentationUsesNamedGuestRecordForOrientation()
	{
		var platform = NewPlatform();
		var levelmeterClass = Register(ref platform, 0x1E80, "Levelmeter.mui");
		var levelmeter = MuiCommonControlCore.CreateControl(ref platform, State,
			levelmeterClass, BuildTags(ref platform, 0x1EC0, new[] {
				(GaugeHoriz, 0u), (NumericMin, 0u), (NumericMax, 100u),
				(NumericValue, 50u) }));

		Assert.True(MuiCommonControlCore.TryGetLevelmeterPresentationStateRecord(
			ref platform, State, levelmeter, out var initial));
		Assert.Equal(MuiLevelmeterPresentationStateRecord.Cookie, initial.Magic);
		Assert.Equal(0u, initial.Horizontal);
		Assert.True(MuiCommonControlCore.TryReadLevelmeterPresentationState(
			ref platform, State, levelmeter, out var state));
		Assert.Equal(0u, state.Horizontal);

		var renderInfo = APTR.FromPointer(0x1F80);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, levelmeter,
			renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, levelmeter, 0,
			0, 14, 48));
		var packet = APTR.FromPointer(0x1FC0);
		platform.WriteUInt32(packet, 0, Draw);
		platform.WriteUInt32(packet, 4, 0);
		var fillsBefore = platform.FillCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			levelmeter, packet));
		Assert.True(platform.FillCount > fillsBefore);
	}

	[Fact]
	public void LevelmeterLabelUsesNamedGuestRecordAndTracksReplacement()
	{
		var platform = NewPlatform();
		var levelmeterClass = Register(ref platform, 0x1100, "Levelmeter.mui");
		var source = APTR.FromPointer(0x1D80);
		platform.WriteCString(source, "Label");
		var levelmeter = MuiCommonControlCore.CreateControl(ref platform, State,
			levelmeterClass, BuildTags(ref platform, 0x1DC0, new[] {
				(LevelmeterLabel, source.Raw) }));
		Assert.NotEqual(APTR.Null, levelmeter);
		Assert.True(MuiCommonControlCore.TryGetLevelmeterLabelStateRecord(
			ref platform, State, levelmeter, out var record));
		Assert.Equal(MuiLevelmeterLabelStateRecord.Cookie, record.Magic);
		Assert.NotEqual(source.Raw, record.Label.Raw);
		Assert.Equal("Label", ReadCString(ref platform, record.Label));

		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, levelmeter,
			LevelmeterLabel, out var projectedLabel, out var labelHandled));
		Assert.True(labelHandled);
		Assert.Equal(record.Label.Raw, projectedLabel);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			levelmeter, LevelmeterLabel, out projectedLabel));
		Assert.Equal(record.Label.Raw, projectedLabel);
		var labelGetMessage = APTR.FromPointer(0x1E80);
		var labelGetStorage = APTR.FromPointer(0x1EC0);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			labelGetMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			labelGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			LevelmeterLabel));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			labelGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			labelGetStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			levelmeter, labelGetMessage));
		Assert.Equal(record.Label.Raw, platform.ReadUInt32(labelGetStorage, 0));

		var replacement = APTR.FromPointer(0x1E00);
		platform.WriteCString(replacement, "New");
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			levelmeter, LevelmeterLabel, replacement.Raw));
		Assert.True(MuiCommonControlCore.TryGetLevelmeterLabelStateRecord(
			ref platform, State, levelmeter, out record));
		Assert.NotEqual(replacement.Raw, record.Label.Raw);
		Assert.Equal("New", ReadCString(ref platform, record.Label));
		Assert.True(MuiCommonControlCore.TryReadLevelmeterLabelState(ref platform,
			State, levelmeter, out var state));
		Assert.Equal(record.Label.Raw, state.Label.Raw);

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			levelmeter));
		Assert.Equal("Label", ReadCString(ref platform, source));
		Assert.Equal("New", ReadCString(ref platform, replacement));
	}

	// Gap 12/14: Prop and Scrollbar share single-step and page-step keyboard
	// movement and the same disabled gate.
	[Fact]
	public void PropAndScrollbarKeyboardSteppingPagesAndHonorsDisabled()
	{
		var platform = NewPlatform();
		var propLike = new[]
		{
			("Prop.mui", MuiControlClass.Prop),
			("Scrollbar.mui", MuiControlClass.Scrollbar),
		};
		uint nameAddr = 0x1100;
		var packet = APTR.FromPointer(0x1400);
		platform.WriteUInt32(packet, 0, HandleEvent);
		platform.WriteUInt32(packet, 4, 0);
		foreach (var (name, cls) in propLike)
		{
			var cl = Register(ref platform, nameAddr, name);
			nameAddr += 0x40;
			var obj = MuiCommonControlCore.CreateControl(ref platform, State, cl,
				APTR.Null);
			Assert.Equal(cls, MuiCommonControlCore.Classify(ref platform, State, obj));
			Set(ref platform, obj, PropEntries, 100);
			Set(ref platform, obj, PropVisible, 10);
			Set(ref platform, obj, PropFirst, 50);

			platform.WriteUInt32(packet, 8, unchecked((uint)KeyDown));
			Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
				obj, packet));
			Assert.Equal(51u, Get(ref platform, obj, PropFirst));
			platform.WriteUInt32(packet, 8, unchecked((uint)KeyUp));
			Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
				obj, packet));
			Assert.Equal(50u, Get(ref platform, obj, PropFirst));
			platform.WriteUInt32(packet, 8, unchecked((uint)KeyPageDown));
			Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
				obj, packet));
			Assert.Equal(58u, Get(ref platform, obj, PropFirst));
			platform.WriteUInt32(packet, 8, unchecked((uint)KeyPageUp));
			Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
				obj, packet));
			Assert.Equal(50u, Get(ref platform, obj, PropFirst));

			Set(ref platform, obj, Disabled, 1);
			platform.WriteUInt32(packet, 8, unchecked((uint)KeyDown));
			Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
				obj, packet));
			Assert.Equal(50u, Get(ref platform, obj, PropFirst));
		}
	}

	[Fact]
	public void PropRangeUsesNamedGuestRecordForMovementAndDrawingInputs()
	{
		var platform = NewPlatform();
		var propClass = Register(ref platform, 0x1100, "Prop.mui");
		var prop = MuiCommonControlCore.CreateControl(ref platform, State,
			propClass, BuildTags(ref platform, 0x1500, new[] {
				(PropEntries, 100u), (PropVisible, 10u), (PropFirst, 20u) }));

		Assert.True(MuiCommonControlCore.TryGetPropRangeStateRecord(
			ref platform, State, prop, out var initial));
		Assert.Equal(MuiPropRangeStateRecord.Cookie, initial.Magic);
		Assert.Equal(100u, initial.Entries);
		Assert.Equal(10u, initial.Visible);
		Assert.Equal(20u, initial.First);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			prop, PropFirst, 50));
		Assert.True(MuiCommonControlCore.TryReadPropRangeState(ref platform, State,
			prop, out var state));
		Assert.Equal(50u, state.First);

		Set(ref platform, prop, PropVisible, 25);
		Assert.True(MuiCommonControlCore.ChangeProp(ref platform, State, prop, 1));
		Assert.True(MuiCommonControlCore.TryGetPropRangeStateRecord(
			ref platform, State, prop, out var changed));
		Assert.Equal(25u, changed.Visible);
		Assert.Equal(51u, changed.First);
	}

	[Fact]
	public void GaugeStateUsesNamedGuestRecordForDivideClampAndDrawing()
	{
		var platform = NewPlatform();
		var gaugeClass = Register(ref platform, 0x1E00, "Gauge.mui");
		var gauge = MuiCommonControlCore.CreateControl(ref platform, State,
			gaugeClass, BuildTags(ref platform, 0x1E40, new[] {
				(GaugeMax, 100u), (GaugeDivide, 4u), (GaugeCurrent, 240u),
				(GaugeHoriz, 1u) }));

		Assert.True(MuiCommonControlCore.TryGetGaugeStateRecord(
			ref platform, State, gauge, out var initial));
		Assert.Equal(MuiGaugeStateRecord.Cookie, initial.Magic);
		Assert.Equal(100u, initial.Maximum);
		Assert.Equal(60u, initial.Current);
		Assert.Equal(4u, initial.Divide);
		Assert.Equal(1u, initial.Horizontal);
		var gaugeGetters = new (uint Attribute, uint Expected)[] {
			(GaugeMax, 100u), (GaugeCurrent, 60u), (GaugeDivide, 4u),
			(GaugeHoriz, 1u) };
		var getMessage = APTR.FromPointer(0x1E80);
		var getStorage = APTR.FromPointer(0x1EC0);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		foreach (var getter in gaugeGetters)
		{
			Assert.True(MuiCommonControlCore.TryGet(ref platform, State, gauge,
				getter.Attribute, out var projected, out var handled));
			Assert.True(handled);
			Assert.Equal(getter.Expected, projected);
			Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
				getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
				getter.Attribute));
			Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform,
				State, gauge, getMessage));
			Assert.Equal(getter.Expected, platform.ReadUInt32(getStorage, 0));
		}

		// Current is divided before it is clamped, and the named record follows
		// the resulting value rather than retaining the source scalar.
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			gauge, GaugeCurrent, 360));
		Assert.True(MuiCommonControlCore.TryReadGaugeState(ref platform, State,
			gauge, out var scaled));
		Assert.Equal(90u, scaled.Current);

		// Lowering Max clamps Current through the same typed state.
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			gauge, GaugeMax, 80));
		Assert.True(MuiCommonControlCore.TryGetGaugeStateRecord(
			ref platform, State, gauge, out var clamped));
		Assert.Equal(80u, clamped.Maximum);
		Assert.Equal(80u, clamped.Current);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			gauge, GaugeDivide, 2));
		Assert.True(MuiCommonControlCore.SetGauge(ref platform, State, gauge,
			100));
		Assert.True(MuiCommonControlCore.TryReadGaugeState(ref platform, State,
			gauge, out var changed));
		Assert.Equal(50u, changed.Current);
		Assert.Equal(2u, changed.Divide);

		// A direct guest scalar write is reconciled before drawing, so the
		// renderer consumes the named record's synchronized value.
		Set(ref platform, gauge, GaugeCurrent, 25);
		var renderInfo = APTR.FromPointer(0x1F80);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, gauge, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, gauge, 0, 0,
			80, 10));
		var drawPacket = APTR.FromPointer(0x1FC0);
		platform.WriteUInt32(drawPacket, 0, Draw);
		platform.WriteUInt32(drawPacket, 4, 0);
		var fillsBefore = platform.FillCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			gauge, drawPacket));
		Assert.Equal(fillsBefore + 2, platform.FillCount);
		Assert.True(MuiCommonControlCore.TryGetGaugeStateRecord(
			ref platform, State, gauge, out var rendered));
		Assert.Equal(25u, rendered.Current);
	}

	// Gap 13: Draw produces class-specific content for the numeric readout
	// classes (stringified value text) and the thumb classes (a thumb fill in
	// addition to the background fill).
	[Fact]
	public void NumericAndPropFamilyDrawRenderValuesAndThumbs()
	{
		var platform = NewPlatform();
		var renderInfo = APTR.FromPointer(0x1480);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		var packet = APTR.FromPointer(0x1440);
		platform.WriteUInt32(packet, 0, Draw);
		platform.WriteUInt32(packet, 4, 0);
		uint nameAddr = 0x1100;

		// Numeric and Numericbutton draw their stringified value as text.
		foreach (var (name, _) in new[]
		{
			("Numeric.mui", MuiControlClass.Numeric),
			("Numericbutton.mui", MuiControlClass.Numericbutton),
		})
		{
			var cl = Register(ref platform, nameAddr, name);
			nameAddr += 0x40;
			var obj = MuiCommonControlCore.CreateControl(ref platform, State, cl,
				APTR.Null);
			Set(ref platform, obj, NumericValue, 42);
			Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, obj, renderInfo));
			Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, obj, 0, 0, 48,
				14));
			var textBefore = platform.TextCount;
			Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
				obj, packet));
			Assert.Equal(textBefore + 1, platform.TextCount);
		}

		// Slider and Knob draw a thumb fill on top of the background fill.
		foreach (var (name, _) in new[]
		{
			("Slider.mui", MuiControlClass.Slider),
			("Knob.mui", MuiControlClass.Knob),
		})
		{
			var cl = Register(ref platform, nameAddr, name);
			nameAddr += 0x40;
			var obj = MuiCommonControlCore.CreateControl(ref platform, State, cl,
				APTR.Null);
			Set(ref platform, obj, NumericValue, 50);
			Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, obj, renderInfo));
			Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, obj, 0, 0, 48,
				14));
			var fillBefore = platform.FillCount;
			Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
				obj, packet));
			Assert.Equal(fillBefore + 2, platform.FillCount);
		}

		// Scrollbar shares Prop's thumb geometry: background fill plus thumb fill.
		var scrollClass = Register(ref platform, nameAddr, "Scrollbar.mui");
		var scroll = MuiCommonControlCore.CreateControl(ref platform, State,
			scrollClass, APTR.Null);
		Set(ref platform, scroll, PropEntries, 100);
		Set(ref platform, scroll, PropVisible, 10);
		Set(ref platform, scroll, PropFirst, 20);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, scroll, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, scroll, 0, 0, 16,
			100));
		var scrollFillBefore = platform.FillCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			scroll, packet));
		Assert.Equal(scrollFillBefore + 2, platform.FillCount);
	}

	// Gap 13: Draw for String (owned text) and the Bitmap family (blitted image
	// through the remapped-state pointer established by Setup).
	[Fact]
	public void StringAndBitmapFamilyDrawRenderTextAndImages()
	{
		var platform = NewPlatform();
		var renderInfo = APTR.FromPointer(0x1480);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		var drawPacket = APTR.FromPointer(0x1440);
		platform.WriteUInt32(drawPacket, 0, Draw);
		platform.WriteUInt32(drawPacket, 4, 0);

		// String draws its (owned) contents as text.
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var source = APTR.FromPointer(0x1800);
		platform.WriteCString(source, "Hello");
		var stringTags = BuildTags(ref platform, 0x1900,
			new[] { (StringContents, source.Raw) });
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, stringTags);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, stringObj,
			renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, stringObj, 0, 0,
			64, 14));
		var textBefore = platform.TextCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, drawPacket));
		Assert.Equal(textBefore + 1, platform.TextCount);

		// Bitmap draws the caller bitmap through the remapped pointer after Setup.
		var bitmapClass = Register(ref platform, 0x1140, "Bitmap.mui");
		var bitmap = MuiCommonControlCore.CreateControl(ref platform, State,
			bitmapClass, APTR.Null);
		var sourceBitmap = APTR.FromPointer(0x3000);
		platform.WriteUInt32(sourceBitmap, 0, 0xC0FFEE00);
		Set(ref platform, bitmap, BitmapBitmap, sourceBitmap.Raw);
		Set(ref platform, bitmap, BitmapWidth, 24);
		Set(ref platform, bitmap, BitmapHeight, 12);
		var setupPacket = APTR.FromPointer(0x1980);
		platform.WriteUInt32(setupPacket, 0, Setup);
		platform.WriteUInt32(setupPacket, 4, renderInfo.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			bitmap, setupPacket));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, bitmap, 0, 0, 24,
			12));
		var imageBefore = platform.ImageCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			bitmap, drawPacket));
		Assert.Equal(imageBefore + 1, platform.ImageCount);

		// Bodychunk decodes an uncompressed body and draws the decoded image.
		var bodyClass = Register(ref platform, 0x1180, "Bodychunk.mui");
		var body = MuiCommonControlCore.CreateControl(ref platform, State,
			bodyClass, APTR.Null);
		var chunk = APTR.FromPointer(0x3080);
		for (var index = 0; index < 4; index++)
			platform.WriteUInt8(chunk, index, unchecked((byte)(0x10 + index)));
		Set(ref platform, body, BodychunkBody, chunk.Raw);
		Set(ref platform, body, BitmapWidth, 16);
		Set(ref platform, body, BitmapHeight, 2);
		Set(ref platform, body, BodychunkDepth, 1);
		Set(ref platform, body, BodychunkCompression, 0);
		platform.WriteUInt32(setupPacket, 0, Setup);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			body, setupPacket));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, body, 0, 0, 16, 2));
		var bodyImageBefore = platform.ImageCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			body, drawPacket));
		Assert.Equal(bodyImageBefore + 1, platform.ImageCount);
	}

	// Gap 15: the neutral class-specific AskMinMax geometry is asserted exactly
	// for every class whose bounds were previously exercised without checking
	// their values.
	[Fact]
	public void ClassSpecificAskMinMaxGeometryMatchesTheNeutralModel()
	{
		var platform = NewPlatform();
		// (name, class, minW, minH, maxW, maxH, defW, defH)
		var cases = new (string Name, int MinW, int MinH, int MaxW, int MaxH,
			int DefW, int DefH)[]
		{
			("Rectangle.mui", 1, 1, 10000, 10000, 1, 1),
			("Gauge.mui", 64, 16, 10000, 16, 64, 16),
			("Levelmeter.mui", 48, 14, 10000, 14, 48, 14),
			("Prop.mui", 16, 16, 10000, 10000, 16, 16),
			("Scrollbar.mui", 16, 16, 10000, 10000, 16, 16),
			("Scale.mui", 64, 8, 10000, 8, 64, 8),
			("Gadget.mui", 8, 8, 10000, 10000, 8, 8),
			("String.mui", 64, 14, 10000, 14, 64, 14),
		};
		uint nameAddr = 0x1100;
		var storage = APTR.FromPointer(0x1400);
		var packet = APTR.FromPointer(0x1440);
		platform.WriteUInt32(packet, 0, AskMinMax);
		platform.WriteUInt32(packet, 4, storage.Raw);
		foreach (var item in cases)
		{
			var cl = Register(ref platform, nameAddr, item.Name);
			nameAddr += 0x40;
			var obj = MuiCommonControlCore.CreateControl(ref platform, State, cl,
				APTR.Null);
			Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
				obj, packet));
			Assert.Equal(item.MinW, platform.ReadUInt16(storage, 0));
			Assert.Equal(item.MinH, platform.ReadUInt16(storage, 2));
			Assert.Equal(item.MaxW, platform.ReadUInt16(storage, 4));
			Assert.Equal(item.MaxH, platform.ReadUInt16(storage, 6));
			Assert.Equal(item.DefW, platform.ReadUInt16(storage, 8));
			Assert.Equal(item.DefH, platform.ReadUInt16(storage, 10));
		}
	}

	// Gap 16: change-only notification firing extends beyond Numeric and Cycle
	// to the Gauge, Prop, Slider, and Radio state attributes.
	[Fact]
	public void NotificationsFireOnceOnChangeAcrossControlFamilies()
	{
		var platform = NewPlatform();
		var gaugeClass = Register(ref platform, 0x1100, "Gauge.mui");
		var propClass = Register(ref platform, 0x1140, "Prop.mui");
		var sliderClass = Register(ref platform, 0x1180, "Slider.mui");
		var radioClass = Register(ref platform, 0x11C0, "Radio.mui");
		var follow = APTR.FromPointer(0x1800);
		platform.WriteUInt32(follow, 0, 0x90000001);
		var destination = MuiCommonControlCore.CreateControl(ref platform, State,
			gaugeClass, APTR.Null);

		// Gauge current level.
		var gauge = MuiCommonControlCore.CreateControl(ref platform, State,
			gaugeClass, APTR.Null);
		Set(ref platform, gauge, GaugeMax, 100);
		Assert.True(MuiNotifyCore.Add(ref platform, State, gauge, GaugeCurrent, 30,
			destination, 1, follow));
		var before = platform.DispatchCount;
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			gauge, GaugeCurrent, 30));
		Assert.Equal(before + 1, platform.DispatchCount);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			gauge, GaugeCurrent, 30));
		Assert.Equal(before + 1, platform.DispatchCount);

		// Prop first-visible index.
		var prop = MuiCommonControlCore.CreateControl(ref platform, State,
			propClass, APTR.Null);
		Set(ref platform, prop, PropEntries, 100);
		Set(ref platform, prop, PropVisible, 10);
		Assert.True(MuiNotifyCore.Add(ref platform, State, prop, PropFirst, 20,
			destination, 1, follow));
		before = platform.DispatchCount;
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			prop, PropFirst, 20));
		Assert.Equal(before + 1, platform.DispatchCount);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			prop, PropFirst, 20));
		Assert.Equal(before + 1, platform.DispatchCount);

		// Slider (Numeric family) value.
		var slider = MuiCommonControlCore.CreateControl(ref platform, State,
			sliderClass, APTR.Null);
		Set(ref platform, slider, NumericMin, 0);
		Set(ref platform, slider, NumericMax, 100);
		Assert.True(MuiNotifyCore.Add(ref platform, State, slider, NumericValue, 60,
			destination, 1, follow));
		before = platform.DispatchCount;
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			slider, NumericValue, 60));
		Assert.Equal(before + 1, platform.DispatchCount);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			slider, NumericValue, 60));
		Assert.Equal(before + 1, platform.DispatchCount);

		// Radio active choice.
		var entries = APTR.FromPointer(0x1900);
		platform.WriteCString(APTR.FromPointer(0x1A00), "One");
		platform.WriteCString(APTR.FromPointer(0x1A10), "Two");
		platform.WriteUInt32(entries, 0, 0x1A00);
		platform.WriteUInt32(entries, 4, 0x1A10);
		platform.WriteUInt32(entries, 8, 0);
		var radioTags = BuildTags(ref platform, 0x1B00,
			new[] { (RadioEntries, entries.Raw) });
		var radio = MuiCommonControlCore.CreateControl(ref platform, State,
			radioClass, radioTags);
		Assert.True(MuiNotifyCore.Add(ref platform, State, radio, RadioActive, 1,
			destination, 1, follow));
		before = platform.DispatchCount;
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			radio, RadioActive, 1));
		Assert.Equal(before + 1, platform.DispatchCount);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			radio, RadioActive, 1));
		Assert.Equal(before + 1, platform.DispatchCount);
	}

	// Gap 17: Rectangle's decorative bar attributes are init-only and reject a
	// post-construction set while ordinary settable attributes still apply.
	[Fact]
	public void RectangleBarAttributesAreInitOnlyButBackgroundRemainsSettable()
	{
		var platform = NewPlatform();
		var rectangleClass = Register(ref platform, 0x1100, "Rectangle.mui");
		var title = APTR.FromPointer(0x1800);
		platform.WriteCString(title, "Section");
		var tags = BuildTags(ref platform, 0x1900, new[]
		{
			(RectangleHBar, 1u), (RectangleVBar, 1u),
			(RectangleBarTitle, title.Raw),
		});
		var rectangle = MuiCommonControlCore.CreateControl(ref platform, State,
			rectangleClass, tags);

		// Init-only bar attributes are readable and reject post-init sets.
		Assert.Equal(1u, Get(ref platform, rectangle, RectangleHBar));
		Assert.Equal(1u, Get(ref platform, rectangle, RectangleVBar));
		Assert.Equal(title.Raw, Get(ref platform, rectangle, RectangleBarTitle));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, RectangleHBar, 0));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, RectangleVBar, 0));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, RectangleBarTitle, 0));
		Assert.Equal(1u, Get(ref platform, rectangle, RectangleHBar));
		Assert.Equal(1u, Get(ref platform, rectangle, RectangleVBar));
		Assert.Equal(title.Raw, Get(ref platform, rectangle, RectangleBarTitle));

		// An ordinary settable attribute (background pen) is still honored.
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, Background, 5));
		Assert.Equal(5u, Get(ref platform, rectangle, Background));
	}

	[Fact]
	public void RectanglePresentationUsesNamedGuestRecordForBarFlags()
	{
		var platform = NewPlatform();
		var rectangleClass = Register(ref platform, 0x1240, "Rectangle.mui");
		var rectangle = MuiCommonControlCore.CreateControl(ref platform, State,
			rectangleClass, BuildTags(ref platform, 0x1900, new[]
			{
				(RectangleHBar, 1u), (RectangleVBar, 1u),
			}));

		Assert.True(MuiCommonControlCore.TryGetRectanglePresentationStateRecord(
			ref platform, State, rectangle, out var record));
		Assert.Equal(MuiRectanglePresentationStateRecord.Cookie, record.Magic);
		Assert.Equal(1u, record.HorizontalBar);
		Assert.Equal(1u, record.VerticalBar);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, rectangle,
			RectangleHBar, out var projected, out var handled));
		Assert.True(handled);
		Assert.Equal(1u, projected);
		Assert.Equal(1u, Get(ref platform, rectangle, RectangleVBar));
		var getMessage = APTR.FromPointer(0x1A80);
		var getStorage = APTR.FromPointer(0x1AC0);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			RectangleHBar));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			rectangle, getMessage));
		Assert.Equal(1u, platform.ReadUInt32(getStorage, 0));

		Assert.True(MuiCommonControlCore.TryReadRectanglePresentationState(
			ref platform, State, rectangle, out var state));
		Assert.Equal(1u, state.HorizontalBar);
		Assert.Equal(1u, state.VerticalBar);
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, RectangleHBar, 0));
		Assert.True(MuiCommonControlCore.TryReadRectanglePresentationState(
			ref platform, State, rectangle, out state));
		Assert.Equal(1u, state.HorizontalBar);

		var renderInfo = APTR.FromPointer(0x1A00);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, rectangle,
			renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, rectangle, 0, 0,
			40, 20));
		var linesBefore = platform.LineCount;
		var packet = APTR.FromPointer(0x1B00);
		platform.WriteUInt32(packet, 0, Draw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			rectangle, packet));
		Assert.Equal(linesBefore + 2, platform.LineCount);
	}

	[Fact]
	public void AreaPresentationUsesNamedGuestRecordForVisibilityAndDrawingPolicy()
	{
		var platform = NewPlatform();
		var rectangleClass = Register(ref platform, 0x1240, "Rectangle.mui");
		var rectangle = MuiCommonControlCore.CreateControl(ref platform, State,
			rectangleClass, BuildTags(ref platform, 0x1900, new[]
			{
				(Disabled, 1u), (ShowMe, 1u), (Background, 5u), (Frame, 1u),
			}));

		Assert.True(MuiCommonControlCore.TryGetAreaPresentationStateRecord(
			ref platform, State, rectangle, out var record));
		Assert.Equal(MuiAreaPresentationStateRecord.Cookie, record.Magic);
		Assert.Equal(1u, record.Disabled);
		Assert.Equal(1u, record.ShowMe);
		Assert.Equal(5u, record.Background);
		Assert.Equal(1u, record.Frame);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, rectangle,
			Background, out var projected, out var handled));
		Assert.True(handled);
		Assert.Equal(5u, projected);
		Assert.Equal(1u, Get(ref platform, rectangle, Disabled));
		Assert.Equal(1u, Get(ref platform, rectangle, ShowMe));
		Assert.Equal(1u, Get(ref platform, rectangle, Frame));
		var getMessage = APTR.FromPointer(0x1AC0);
		var getStorage = APTR.FromPointer(0x1B00);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			Background));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			rectangle, getMessage));
		Assert.Equal(5u, platform.ReadUInt32(getStorage, 0));

		var renderInfo = APTR.FromPointer(0x1A00);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, rectangle,
			renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, rectangle, 0, 0,
			40, 20));
		var fillsBefore = platform.FillCount;
		var linesBefore = platform.LineCount;
		Assert.True(MuiCommonControlCore.DrawControl(ref platform, State, rectangle,
			0));
		Assert.Equal(fillsBefore + 1, platform.FillCount);
		Assert.Equal(linesBefore + 4, platform.LineCount);
		Assert.Equal(4u, platform.LastPen);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, Background, 7));
		Assert.True(MuiCommonControlCore.TryReadAreaPresentationState(ref platform,
			State, rectangle, out var state));
		Assert.Equal(7u, state.Background);
		Assert.True(MuiCommonControlCore.TryGetAreaPresentationStateRecord(
			ref platform, State, rectangle, out record));
		Assert.Equal(7u, record.Background);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, ShowMe, 0));
		Assert.True(MuiCommonControlCore.TryReadAreaPresentationState(ref platform,
			State, rectangle, out state));
		Assert.Equal(0u, state.ShowMe);
		Assert.True(MuiCommonControlCore.TryComputeMinMax(ref platform, State,
			rectangle, out var minMax));
		Assert.Equal((short)0, minMax.MinWidth);
		Assert.Equal((short)0, minMax.MinHeight);
		Assert.Equal((short)0, minMax.DefWidth);
		Assert.Equal((short)0, minMax.DefHeight);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			rectangle, ShowMe, 1, false));
		Assert.True(MuiCommonControlCore.TryReadAreaPresentationState(ref platform,
			State, rectangle, out state));
		Assert.Equal(1u, state.ShowMe);
		Assert.True(MuiCommonControlCore.TryGetAreaPresentationStateRecord(
			ref platform, State, rectangle, out record));
		Assert.Equal(1u, record.ShowMe);
	}

	[Fact]
	public void AreaGeometryUsesNamedRecordForGetAndOmGet()
	{
		var platform = NewPlatform();
		var textClass = Register(ref platform, 0x1280, "Text.mui");
		var text = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, APTR.Null);
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, text, -3, 4,
			40, 12));
		Assert.True(MuiAreaLayoutCore.TryGetGeometryStateRecord(
			ref platform, State, text, out var record));
		Assert.Equal(MuiAreaGeometryStateRecord.Cookie, record.Magic);
		Assert.Equal(-3, record.Left);
		Assert.Equal(4, record.Top);
		Assert.Equal(40, record.Width);
		Assert.Equal(12, record.Height);
		Assert.Equal(36, record.Right);
		Assert.Equal(15, record.Bottom);

		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, text,
			LeftEdge, out var projected, out var handled));
		Assert.True(handled);
		Assert.Equal(unchecked((uint)-3), projected);
		Assert.Equal(unchecked((uint)4), Get(ref platform, text, TopEdge));
		Assert.Equal(40u, Get(ref platform, text, Width));
		Assert.Equal(12u, Get(ref platform, text, ControlHeight));
		Assert.Equal(36u, Get(ref platform, text, RightEdge));
		Assert.Equal(15u, Get(ref platform, text, BottomEdge));

		var getMessage = APTR.FromPointer(0x1D00);
		var getStorage = APTR.FromPointer(0x1D40);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute, Width));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			text, getMessage));
		Assert.Equal(40u, platform.ReadUInt32(getStorage, 0));
	}

	[Fact]
	public void AreaLayoutPolicyUsesNamedRecordForGetAndOmGet()
	{
		var platform = NewPlatform();
		var textClass = Register(ref platform, 0x12C0, "Text.mui");
		var text = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, APTR.Null);
		Set(ref platform, text, FixWidth, 20);
		Set(ref platform, text, FixHeight, 10);
		Set(ref platform, text, MaxWidth, 100);
		Set(ref platform, text, MaxHeight, 80);
		Set(ref platform, text, InnerLeft, 2);
		Set(ref platform, text, InnerRight, 3);
		Set(ref platform, text, InnerTop, 1);
		Set(ref platform, text, InnerBottom, 1);
		Set(ref platform, text, HorizWeight, 3);
		Set(ref platform, text, VertWeight, 5);

		Assert.True(MuiAreaLayoutCore.AskMinMax(ref platform, State, text,
			APTR.FromPointer(0x1D80)));
		Assert.True(MuiAreaLayoutCore.TryGetLayoutPolicyState(ref platform, State,
			text, out var policy));
		Assert.Equal(MuiAreaLayoutPolicyStateRecord.Cookie, policy.Magic);
		Assert.Equal(20u, policy.FixWidth);
		Assert.Equal(10u, policy.FixHeight);
		Assert.Equal(100u, policy.MaxWidth);
		Assert.Equal(80u, policy.MaxHeight);
		Assert.Equal(2u, policy.InnerLeft);
		Assert.Equal(3u, policy.InnerRight);
		Assert.Equal(1u, policy.InnerTop);
		Assert.Equal(1u, policy.InnerBottom);
		Assert.Equal(3u, policy.HorizontalWeight);
		Assert.Equal(5u, policy.VerticalWeight);

		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, text,
			FixWidth, out var projected, out var handled));
		Assert.True(handled);
		Assert.Equal(20u, projected);
		Assert.Equal(3u, Get(ref platform, text, HorizWeight));
		Assert.Equal(5u, Get(ref platform, text, VertWeight));
		Assert.Equal(2u, Get(ref platform, text, InnerLeft));
		Assert.Equal(80u, Get(ref platform, text, MaxHeight));
		Assert.Equal(100u, Get(ref platform, text, Weight));

		var getMessage = APTR.FromPointer(0x1DC0);
		var getStorage = APTR.FromPointer(0x1E00);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			VertWeight));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			text, getMessage));
		Assert.Equal(5u, platform.ReadUInt32(getStorage, 0));
	}

	[Fact]
	public void AreaWeightUsesNamedRecordForGetOmGetAndRuntimeSetter()
	{
		var platform = NewPlatform();
		var textClass = Register(ref platform, 0x12A0, "Text.mui");
		var text = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, APTR.Null);

		Assert.True(MuiCommonControlCore.TryGetAreaWeightStateRecord(
			ref platform, State, text, out var initial));
		Assert.Equal(MuiAreaWeightStateRecord.Cookie, initial.Magic);
		Assert.Equal(100u, initial.Weight);
		Assert.Equal(100u, Get(ref platform, text, Weight));

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			text, Weight, 37, false));
		Assert.True(MuiCommonControlCore.TryReadAreaWeightState(ref platform, State,
			text, out var changed));
		Assert.Equal(37u, changed.Weight);
		Assert.Equal(37u, Get(ref platform, text, Weight));

		// A raw compatibility write is reconciled by the named getter record.
		Set(ref platform, text, Weight, 42);
		Assert.Equal(42u, Get(ref platform, text, Weight));
		Assert.True(MuiCommonControlCore.TryGetAreaWeightStateRecord(
			ref platform, State, text, out initial));
		Assert.Equal(42u, initial.Weight);

		var getMessage = APTR.FromPointer(0x12E0);
		var getStorage = APTR.FromPointer(0x1320);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute, Weight));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			text, getMessage));
		Assert.Equal(42u, platform.ReadUInt32(getStorage, 0));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, text));
	}

	[Fact]
	public void AreaFillUsesNamedRenderPolicyForGetAndOmGet()
	{
		var platform = NewPlatform();
		var textClass = Register(ref platform, 0x12E0, "Text.mui");
		var text = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, APTR.Null);

		Assert.Equal(1u, Get(ref platform, text, FillArea));
		Assert.True(MuiAreaLayoutCore.TryGetRenderPolicyState(ref platform, State,
			text, out var initial));
		Assert.Equal(MuiAreaRenderPolicyStateRecord.Cookie, initial.Magic);
		Assert.Equal(1u, initial.FillArea);

		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, text,
			FillArea, out var projected, out var handled));
		Assert.True(handled);
		Assert.Equal(1u, projected);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			text, FillArea, 0, false));
		Assert.Equal(0u, Get(ref platform, text, FillArea));
		Assert.True(MuiAreaLayoutCore.TryGetRenderPolicyState(ref platform, State,
			text, out var changed));
		Assert.Equal(0u, changed.FillArea);

		var getMessage = APTR.FromPointer(0x1E40);
		var getStorage = APTR.FromPointer(0x1E80);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			FillArea));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			text, getMessage));
		Assert.Equal(0u, platform.ReadUInt32(getStorage, 0));
	}

	[Fact]
	public void SliderScaleAndLevelmeterPresentationUseNamedRecordsForGetAndOmGet()
	{
		var platform = NewPlatform();
		var sliderClass = Register(ref platform, 0x1300, "Slider.mui");
		var scaleClass = Register(ref platform, 0x1380, "Scale.mui");
		var levelmeterClass = Register(ref platform, 0x1400, "Levelmeter.mui");
		var sliderTags = BuildTags(ref platform, 0x1F00,
			[(SliderHoriz, 0u), (SliderQuiet, 1u)]);
		var scaleTags = BuildTags(ref platform, 0x1F40,
			[(ScaleHoriz, 0u)]);
		var levelmeterTags = BuildTags(ref platform, 0x1F80,
			[(GaugeHoriz, 0u)]);
		var slider = MuiCommonControlCore.CreateControl(ref platform, State,
			sliderClass, sliderTags);
		var scale = MuiCommonControlCore.CreateControl(ref platform, State,
			scaleClass, scaleTags);
		var levelmeter = MuiCommonControlCore.CreateControl(ref platform, State,
			levelmeterClass, levelmeterTags);

		Assert.True(MuiCommonControlCore.TryGetSliderPresentationStateRecord(
			ref platform, State, slider, out var sliderRecord));
		Assert.Equal(MuiSliderPresentationStateRecord.Cookie, sliderRecord.Magic);
		Assert.Equal(0u, sliderRecord.Horizontal);
		Assert.Equal(1u, sliderRecord.Quiet);
		Assert.Equal(0u, Get(ref platform, slider, SliderHoriz));
		Assert.Equal(1u, Get(ref platform, slider, SliderQuiet));

		Assert.True(MuiCommonControlCore.TryGetScalePresentationStateRecord(
			ref platform, State, scale, out var scaleRecord));
		Assert.Equal(MuiScalePresentationStateRecord.Cookie, scaleRecord.Magic);
		Assert.Equal(0u, Get(ref platform, scale, ScaleHoriz));

		Assert.True(MuiCommonControlCore.TryGetLevelmeterPresentationStateRecord(
			ref platform, State, levelmeter, out var levelmeterRecord));
		Assert.Equal(MuiLevelmeterPresentationStateRecord.Cookie,
			levelmeterRecord.Magic);
		Assert.Equal(0u, Get(ref platform, levelmeter, GaugeHoriz));

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			slider, SliderHoriz, 1, false));
		Assert.Equal(1u, Get(ref platform, slider, SliderHoriz));

		var getMessage = APTR.FromPointer(0x2100);
		var getStorage = APTR.FromPointer(0x2140);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			ScaleHoriz));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			scale, getMessage));
		Assert.Equal(0u, platform.ReadUInt32(getStorage, 0));
	}

	[Fact]
	public void GadgetInteractionUsesNamedRecordForGetAndOmGet()
	{
		var platform = NewPlatform();
		var gadgetClass = Register(ref platform, 0x1500, "Gadget.mui");
		var gadget = MuiCommonControlCore.CreateControl(ref platform, State,
			gadgetClass, BuildTags(ref platform, 0x2200,
			[(InputMode, InputModeToggle), (Selected, 1u)]));

		Assert.True(MuiCommonControlCore.TryGetGadgetInteractionStateRecord(
			ref platform, State, gadget, out var record));
		Assert.Equal(MuiGadgetInteractionStateRecord.Cookie, record.Magic);
		Assert.Equal(InputModeToggle, record.InputMode);
		Assert.Equal(1u, record.Selected);
		Assert.Equal(0u, record.Pressed);

		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, gadget,
			InputMode, out var projected, out var handled));
		Assert.True(handled);
		Assert.Equal(InputModeToggle, projected);
		Assert.Equal(1u, Get(ref platform, gadget, Selected));
		Assert.Equal(0u, Get(ref platform, gadget, Pressed));

		// Compatibility callers may update the raw Pressed slot directly; the
		// typed getter reconciles that value before exposing it.
		Set(ref platform, gadget, Pressed, 1);
		Assert.Equal(1u, Get(ref platform, gadget, Pressed));
		Assert.True(MuiCommonControlCore.TryGetGadgetInteractionStateRecord(
			ref platform, State, gadget, out record));
		Assert.Equal(1u, record.Pressed);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			gadget, Selected, 0, false));
		Assert.Equal(0u, Get(ref platform, gadget, Selected));

		var getMessage = APTR.FromPointer(0x2280);
		var getStorage = APTR.FromPointer(0x22C0);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			Pressed));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			gadget, getMessage));
		Assert.Equal(1u, platform.ReadUInt32(getStorage, 0));
	}

	[Fact]
	public void PropAndScrollbarStateUseNamedRecordsForGetAndOmGet()
	{
		var platform = NewPlatform();
		var propClass = Register(ref platform, 0x1580, "Prop.mui");
		_ = Register(ref platform, 0x15C0, "Gadget.mui");
		var scrollbarClass = Register(ref platform, 0x1600, "Scrollbar.mui");
		var prop = MuiCommonControlCore.CreateControl(ref platform, State,
			propClass, BuildTags(ref platform, 0x2300,
			[(PropEntries, 100u), (PropVisible, 10u), (PropFirst, 20u)]));
		var scrollbar = MuiCommonControlCore.CreateControl(ref platform, State,
			scrollbarClass, BuildTags(ref platform, 0x2340,
			[(GroupHoriz, 1u), (ScrollbarType, ScrollbarTypeSym),
				(PropEntries, 80u), (PropVisible, 8u), (PropFirst, 12u)]));

		Assert.True(MuiCommonControlCore.TryGetPropRangeStateRecord(
			ref platform, State, prop, out var propRecord));
		Assert.Equal(MuiPropRangeStateRecord.Cookie, propRecord.Magic);
		Assert.Equal(100u, propRecord.Entries);
		Assert.Equal(10u, propRecord.Visible);
		Assert.Equal(20u, propRecord.First);
		Assert.Equal(100u, Get(ref platform, prop, PropEntries));
		Assert.Equal(10u, Get(ref platform, prop, PropVisible));
		Assert.Equal(20u, Get(ref platform, prop, PropFirst));

		Assert.True(MuiCommonControlCore.TryGetScrollbarLayoutStateRecord(
			ref platform, State, scrollbar, out var scrollbarRecord));
		Assert.Equal(MuiScrollbarLayoutStateRecord.Cookie, scrollbarRecord.Magic);
		Assert.Equal(1u, scrollbarRecord.Horizontal);
		Assert.Equal(ScrollbarTypeSym, scrollbarRecord.Type);
		Assert.Equal(1u, Get(ref platform, scrollbar, GroupHoriz));
		Assert.Equal(ScrollbarTypeSym, Get(ref platform, scrollbar, ScrollbarType));
		Assert.Equal(80u, Get(ref platform, scrollbar, PropEntries));
		Assert.Equal(8u, Get(ref platform, scrollbar, PropVisible));
		Assert.Equal(12u, Get(ref platform, scrollbar, PropFirst));

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			prop, PropFirst, 50, false));
		Assert.Equal(50u, Get(ref platform, prop, PropFirst));

		var getMessage = APTR.FromPointer(0x2480);
		var getStorage = APTR.FromPointer(0x24C0);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			ScrollbarType));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			scrollbar, getMessage));
		Assert.Equal(ScrollbarTypeSym, platform.ReadUInt32(getStorage, 0));
	}

	[Fact]
	public void PropPolicyUsesNamedRecordForGetOmGetAndRuntimeSetters()
	{
		var platform = NewPlatform();
		var propClass = Register(ref platform, 0x2680, "Prop.mui");
		var scrollbarClass = Register(ref platform, 0x26C0, "Scrollbar.mui");
		var prop = MuiCommonControlCore.CreateControl(ref platform, State,
			propClass, BuildTags(ref platform, 0x2700, new[] {
				(PropHoriz, 0u), (PropDeltaFactor, 3u), (PropSlider, 1u),
				(PropUseWinBorder, 2u) }));

		Assert.True(MuiCommonControlCore.TryGetPropPolicyStateRecord(
			ref platform, State, prop, out var propRecord));
		Assert.Equal(MuiPropPolicyStateRecord.Cookie, propRecord.Magic);
		Assert.Equal(0u, propRecord.Horizontal);
		Assert.Equal(3u, propRecord.DeltaFactor);
		Assert.Equal(1u, propRecord.Slider);
		Assert.Equal(2u, propRecord.UseWinBorder);
		Assert.Equal(3u, Get(ref platform, prop, PropDeltaFactor));
		Assert.Equal(1u, Get(ref platform, prop, PropSlider));
		Assert.Equal(2u, Get(ref platform, prop, PropUseWinBorder));

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			prop, PropDeltaFactor, 5, false));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			prop, PropSlider, 0, false));
		Assert.True(MuiCommonControlCore.TryReadPropPolicyState(ref platform, State,
			prop, out var changed));
		Assert.Equal(5u, changed.DeltaFactor);
		Assert.Equal(0u, changed.Slider);

		// A compatibility caller may still write the raw slot directly; the
		// named record reconciles that value at the getter boundary.
		Set(ref platform, prop, PropUseWinBorder, 3);
		Assert.Equal(3u, Get(ref platform, prop, PropUseWinBorder));
		Assert.True(MuiCommonControlCore.TryGetPropPolicyStateRecord(
			ref platform, State, prop, out propRecord));
		Assert.Equal(3u, propRecord.UseWinBorder);

		var getMessage = APTR.FromPointer(0x2780);
		var getStorage = APTR.FromPointer(0x27C0);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			PropDeltaFactor));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			prop, getMessage));
		Assert.Equal(5u, platform.ReadUInt32(getStorage, 0));

		var scrollbar = MuiCommonControlCore.CreateControl(ref platform, State,
			scrollbarClass, BuildTags(ref platform, 0x2800, new[] {
				(GroupHoriz, 1u), (PropDeltaFactor, 2u), (PropSlider, 1u),
				(PropUseWinBorder, 1u) }));
		Assert.True(MuiCommonControlCore.TryGetPropPolicyStateRecord(
			ref platform, State, scrollbar, out var scrollbarRecord));
		Assert.Equal(1u, scrollbarRecord.Horizontal);
		Assert.Equal(2u, scrollbarRecord.DeltaFactor);
		Assert.Equal(1u, scrollbarRecord.Slider);
		Assert.Equal(1u, scrollbarRecord.UseWinBorder);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			scrollbar, PropSlider, 0, false));
		Assert.True(MuiCommonControlCore.TryReadPropPolicyState(ref platform, State,
			scrollbar, out var scrollbarPolicy));
		Assert.Equal(0u, scrollbarPolicy.Slider);

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, prop));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			scrollbar));
	}

	// Gap 8: Bitmap source/geometry attributes the autodocs mark [ISG]
	// (Bitmap/Width/Height/Alpha/MappingTable/Precision/SourceColors/Transparent)
	// are settable at runtime and, while the object is set up, a change
	// invalidates and rebuilds the remapped pointer, notifies once, and redraws.
	// RemappedBitmap stays [..G] and UseFriend stays [I..].
	[Fact]
	public void BitmapSourceAndGeometryAreSettableAndRebuildRemappedState()
	{
		var platform = NewPlatform();
		var bitmapClass = Register(ref platform, 0x1100, "Bitmap.mui");
		var bitmap = MuiCommonControlCore.CreateControl(ref platform, State,
			bitmapClass, APTR.Null);

		var first = APTR.FromPointer(0x3000);
		platform.WriteUInt32(first, 0, 0xAA11BB22);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapBitmap, first.Raw));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapWidth, 24));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapHeight, 12));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapAlpha, 0xffffffff));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapMappingTable, 0x3500));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapPrecision, 1));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapSourceColors, 0x3600));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapTransparent, 0));
		Assert.Equal(first.Raw, Get(ref platform, bitmap, BitmapBitmap));
		Assert.Equal(24u, Get(ref platform, bitmap, BitmapWidth));
		Assert.Equal(0xffffffffu, Get(ref platform, bitmap, BitmapAlpha));

		// RemappedBitmap is get-only; UseFriend is init-only.
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapRemapped, 0x9999));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapUseFriend, 1));

		var renderInfo = APTR.FromPointer(0x1480);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		Assert.True(MuiCommonControlCore.SetupBitmap(ref platform, State, bitmap,
			renderInfo));
		Assert.Equal(first.Raw, Get(ref platform, bitmap, BitmapRemapped));

		// Replacing the source while live rebuilds the remapped pointer, fires the
		// change notification exactly once, and schedules a redraw.
		var second = APTR.FromPointer(0x3040);
		platform.WriteUInt32(second, 0, 0xCC33DD44);
		var follow = APTR.FromPointer(0x1800);
		platform.WriteUInt32(follow, 0, 0x90000001);
		var destination = MuiCommonControlCore.CreateControl(ref platform, State,
			bitmapClass, APTR.Null);
		Assert.True(MuiNotifyCore.Add(ref platform, State, bitmap, BitmapBitmap,
			second.Raw, destination, 1, follow));
		var dispatchesBefore = platform.DispatchCount;
		var redrawBefore = platform.RedrawCount;
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapBitmap, second.Raw));
		Assert.Equal(second.Raw, Get(ref platform, bitmap, BitmapBitmap));
		Assert.Equal(second.Raw, Get(ref platform, bitmap, BitmapRemapped));
		Assert.Equal(dispatchesBefore + 1, platform.DispatchCount);
		Assert.True(platform.RedrawCount > redrawBefore);

		// A no-op set (same value) neither notifies nor redraws.
		var dispatchesSteady = platform.DispatchCount;
		var redrawSteady = platform.RedrawCount;
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapBitmap, second.Raw));
		Assert.Equal(dispatchesSteady, platform.DispatchCount);
		Assert.Equal(redrawSteady, platform.RedrawCount);

		// Cleanup clears the remapped state; the caller bitmaps are never freed.
		Assert.True(MuiCommonControlCore.CleanupBitmap(ref platform, State, bitmap));
		Assert.Equal(0u, Get(ref platform, bitmap, BitmapRemapped));
		Assert.Equal(0xAA11BB22u, platform.ReadUInt32(first, 0));
		Assert.Equal(0xCC33DD44u, platform.ReadUInt32(second, 0));
	}

	// Gap 9: Bodychunk Body/Compression/Depth/Masking are [ISG]; changing the
	// BODY while set up re-decodes into fresh owned storage and redraws.
	[Fact]
	public void BodychunkSourceAttributesAreSettableAndRebuildDecodedState()
	{
		var platform = NewPlatform();
		var bodyClass = Register(ref platform, 0x1100, "Bodychunk.mui");
		var body = MuiCommonControlCore.CreateControl(ref platform, State,
			bodyClass, APTR.Null);

		var firstBody = APTR.FromPointer(0x3000);
		platform.WriteUInt8(firstBody, 0, 253); // repeat the next byte four times
		platform.WriteUInt8(firstBody, 1, 0x11);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			body, BodychunkBody, firstBody.Raw));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			body, BitmapWidth, 16));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			body, BitmapHeight, 2));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			body, BodychunkDepth, 1));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			body, BodychunkMasking, 0));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			body, BodychunkCompression, 1));
		Assert.Equal(1u, Get(ref platform, body, BodychunkCompression));

		var renderInfo = APTR.FromPointer(0x1480);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		Assert.True(MuiCommonControlCore.SetupBitmap(ref platform, State, body,
			renderInfo));
		var firstDecoded = APTR.FromPointer(Get(ref platform, body, BitmapRemapped));
		Assert.NotEqual(0u, firstDecoded.Raw);
		Assert.NotEqual(firstBody.Raw, firstDecoded.Raw);
		for (var index = 0; index < 4; index++)
			Assert.Equal(0x11, platform.ReadUInt8(firstDecoded, index));

		// Replacing the BODY while live re-decodes into fresh owned storage.
		var secondBody = APTR.FromPointer(0x3040);
		platform.WriteUInt8(secondBody, 0, 253);
		platform.WriteUInt8(secondBody, 1, 0x77);
		var redrawBefore = platform.RedrawCount;
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			body, BodychunkBody, secondBody.Raw));
		var secondDecoded = APTR.FromPointer(Get(ref platform, body,
			BitmapRemapped));
		Assert.NotEqual(0u, secondDecoded.Raw);
		for (var index = 0; index < 4; index++)
			Assert.Equal(0x77, platform.ReadUInt8(secondDecoded, index));
		Assert.True(platform.RedrawCount > redrawBefore);

		// Cleanup retires the decoded storage; caller body data is untouched.
		Assert.True(MuiCommonControlCore.CleanupBitmap(ref platform, State, body));
		Assert.Equal(0u, Get(ref platform, body, BitmapRemapped));
		Assert.Equal(253, platform.ReadUInt8(secondBody, 0));
		Assert.Equal(0x77, platform.ReadUInt8(secondBody, 1));
	}

	[Fact]
	public void BodychunkDecodeFormatUsesNamedGuestRecordAndTracksRuntimeChanges()
	{
		var platform = NewPlatform();
		var bodyClass = Register(ref platform, 0x1100, "Bodychunk.mui");
		var body = MuiCommonControlCore.CreateControl(ref platform, State,
			bodyClass, APTR.Null);

		Assert.True(MuiCommonControlCore.TryGetBodychunkFormatStateRecord(
			ref platform, State, body, out var initial));
		Assert.Equal(MuiBodychunkFormatStateRecord.Cookie, initial.Magic);
		Assert.Equal(0u, initial.Compression);
		Assert.Equal(1u, initial.Depth);
		Assert.Equal(0u, initial.Masking);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			body, BodychunkCompression, 1));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			body, BodychunkDepth, 4));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			body, BodychunkMasking, 1));
		Assert.True(MuiCommonControlCore.TryReadBodychunkFormatState(ref platform,
			State, body, out var format));
		Assert.Equal(1u, format.Compression);
		Assert.Equal(4u, format.Depth);
		Assert.Equal(1u, format.Masking);
		Assert.True(MuiCommonControlCore.TryGetBodychunkFormatStateRecord(
			ref platform, State, body, out var changed));
		Assert.Equal(1u, changed.Compression);
		Assert.Equal(4u, changed.Depth);
		Assert.Equal(1u, changed.Masking);
	}

	[Fact]
	public void BitmapGeometryUsesNamedGuestRecordForLayoutAndDecodeInputs()
	{
		var platform = NewPlatform();
		var bitmapClass = Register(ref platform, 0x1100, "Bitmap.mui");
		var bitmap = MuiCommonControlCore.CreateControl(ref platform, State,
			bitmapClass, APTR.Null);

		Assert.True(MuiCommonControlCore.TryGetBitmapGeometryStateRecord(
			ref platform, State, bitmap, out var initial));
		Assert.Equal(MuiBitmapGeometryStateRecord.Cookie, initial.Magic);
		Assert.Equal(0u, initial.Width);
		Assert.Equal(0u, initial.Height);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapWidth, 32));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			bitmap, BitmapHeight, 12));
		Assert.True(MuiCommonControlCore.TryReadBitmapGeometryState(ref platform,
			State, bitmap, out var geometry));
		Assert.Equal(32u, geometry.Width);
		Assert.Equal(12u, geometry.Height);
		Assert.True(MuiCommonControlCore.TryGetBitmapGeometryStateRecord(
			ref platform, State, bitmap, out var changed));
		Assert.Equal(32u, changed.Width);
		Assert.Equal(12u, changed.Height);
	}

	// Gap 10: Slider_Horiz [ISG] and Scale_Horiz [ISG] are settable, and
	// Image_FontMatchString [IS.] is settable, while the [I..] FontMatch flags
	// stay init-only.
	[Fact]
	public void SliderScaleHorizAndImageFontMatchStringAreSettableAtRuntime()
	{
		var platform = NewPlatform();
		var sliderClass = Register(ref platform, 0x1100, "Slider.mui");
		var scaleClass = Register(ref platform, 0x1140, "Scale.mui");
		var imageClass = Register(ref platform, 0x1180, "Image.mui");

		var slider = MuiCommonControlCore.CreateControl(ref platform, State,
			sliderClass, APTR.Null);
		var redrawBefore = platform.RedrawCount;
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			slider, SliderHoriz, 0));
		Assert.Equal(0u, Get(ref platform, slider, SliderHoriz));
		Assert.True(platform.RedrawCount > redrawBefore);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			slider, SliderHoriz, 1));
		Assert.Equal(1u, Get(ref platform, slider, SliderHoriz));

		var scale = MuiCommonControlCore.CreateControl(ref platform, State,
			scaleClass, APTR.Null);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			scale, ScaleHoriz, 0));
		Assert.Equal(0u, Get(ref platform, scale, ScaleHoriz));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			scale, ScaleHoriz, 1));
		Assert.Equal(1u, Get(ref platform, scale, ScaleHoriz));

		var image = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, APTR.Null);
		var matchString = APTR.FromPointer(0x3000);
		platform.WriteCString(matchString, "topaz/8");
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			image, ImageFontMatchString, matchString.Raw));
		Assert.Equal(matchString.Raw, Get(ref platform, image,
			ImageFontMatchString));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			image, ImageFontMatch, 1));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			image, ImageFontMatchWidth, 1));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			image, ImageFontMatchHeight, 1));
	}

	[Fact]
	public void SliderPresentationUsesNamedGuestRecordForOrientationAndQuietMode()
	{
		var platform = NewPlatform();
		var sliderClass = Register(ref platform, 0x1E00, "Slider.mui");
		var slider = MuiCommonControlCore.CreateControl(ref platform, State,
			sliderClass, BuildTags(ref platform, 0x1E40, new[] {
				(GroupHoriz, 0u), (SliderQuiet, 1u) }));

		Assert.True(MuiCommonControlCore.TryGetSliderPresentationStateRecord(
			ref platform, State, slider, out var initial));
		Assert.Equal(MuiSliderPresentationStateRecord.Cookie, initial.Magic);
		Assert.Equal(0u, initial.Horizontal);
		Assert.Equal(1u, initial.Quiet);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			slider, SliderHoriz, 1));
		Assert.True(MuiCommonControlCore.TryReadSliderPresentationState(
			ref platform, State, slider, out var changed));
		Assert.Equal(1u, changed.Horizontal);
		Assert.Equal(1u, changed.Quiet);

		// A compatibility/persistence scalar write is reconciled when the typed
		// presentation state is read by layout or drawing.
		Set(ref platform, slider, SliderQuiet, 0);
		Assert.True(MuiCommonControlCore.TryReadSliderPresentationState(
			ref platform, State, slider, out var synchronized));
		Assert.Equal(0u, synchronized.Quiet);
		Assert.True(MuiCommonControlCore.TryGetSliderPresentationStateRecord(
			ref platform, State, slider, out var record));
		Assert.Equal(0u, record.Quiet);
	}

	[Fact]
	public void ScalePresentationUsesNamedGuestRecordForOrientationAndDrawing()
	{
		var platform = NewPlatform();
		var scaleClass = Register(ref platform, 0x1E80, "Scale.mui");
		var scale = MuiCommonControlCore.CreateControl(ref platform, State,
			scaleClass, APTR.Null);

		Assert.True(MuiCommonControlCore.TryGetScalePresentationStateRecord(
			ref platform, State, scale, out var initial));
		Assert.Equal(MuiScalePresentationStateRecord.Cookie, initial.Magic);
		Assert.Equal(1u, initial.Horizontal);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			scale, ScaleHoriz, 0));
		Assert.True(MuiCommonControlCore.TryReadScalePresentationState(
			ref platform, State, scale, out var changed));
		Assert.Equal(0u, changed.Horizontal);

		var renderInfo = APTR.FromPointer(0x1F00);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, scale, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, scale, 0, 0,
			20, 80));
		var packet = APTR.FromPointer(0x1F40);
		platform.WriteUInt32(packet, 0, Draw);
		platform.WriteUInt32(packet, 4, 0);
		var linesBefore = platform.LineCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			scale, packet));
		Assert.True(platform.LineCount > linesBefore);
	}

	[Fact]
	public void ImageFontMatchStringUsesNamedGuestRecordAndValidatesReplacement()
	{
		var platform = NewPlatform();
		var imageClass = Register(ref platform, 0x1100, "Image.mui");
		var matchString = APTR.FromPointer(0x3000);
		platform.WriteCString(matchString, "topaz/8");
		var image = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, BuildTags(ref platform, 0x1900, new[] {
				(ImageFontMatchString, matchString.Raw) }));
		Assert.True(MuiCommonControlCore.TryGetImageFontMatchStringStateRecord(
			ref platform, State, image, out var record));
		Assert.Equal(MuiImageFontMatchStringStateRecord.Cookie, record.Magic);
		Assert.Equal(1u, record.Present);
		Assert.Equal(matchString.Raw, record.MatchString.Raw);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, image,
			ImageFontMatchString, out var projected, out var handled));
		Assert.True(handled);
		Assert.Equal(matchString.Raw, projected);
		Assert.Equal(matchString.Raw, Get(ref platform, image,
			ImageFontMatchString));
		var getMessage = APTR.FromPointer(0x30C0);
		var getStorage = APTR.FromPointer(0x3100);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			ImageFontMatchString));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			image, getMessage));
		Assert.Equal(matchString.Raw, platform.ReadUInt32(getStorage, 0));

		var replacement = APTR.FromPointer(0x3040);
		platform.WriteCString(replacement, "courier/9");
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			image, ImageFontMatchString, replacement.Raw));
		Assert.True(MuiCommonControlCore.TryReadImageFontMatchStringState(
			ref platform, State, image, out var state));
		Assert.True(state.Present);
		Assert.Equal(replacement.Raw, state.MatchString.Raw);
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			image, ImageFontMatchString, 0xFFFFFF00u));
		Assert.True(MuiCommonControlCore.TryGetImageFontMatchStringStateRecord(
			ref platform, State, image, out record));
		Assert.Equal(replacement.Raw, record.MatchString.Raw);

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, image));
	}

	[Fact]
	public void ImageFontMatchScalarsUseNamedRecordForGetAndOmGet()
	{
		var platform = NewPlatform();
		var imageClass = Register(ref platform, 0x3200, "Image.mui");
		var image = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, BuildTags(ref platform, 0x3240, new[] {
				(ImageFontMatch, 1u), (ImageFontMatchHeight, 12u),
				(ImageFontMatchWidth, 24u) }));

		Assert.True(MuiCommonControlCore.TryGetImageFontMatchStateRecord(
			ref platform, State, image, out var record));
		Assert.Equal(MuiImageFontMatchStateRecord.Cookie, record.Magic);
		Assert.Equal(1u, record.Match);
		Assert.Equal(12u, record.Height);
		Assert.Equal(24u, record.Width);
		Assert.True(MuiCommonControlCore.TryReadImageFontMatchState(
			ref platform, State, image, out var state));
		Assert.Equal(1u, state.Match);
		Assert.Equal(12u, state.Height);
		Assert.Equal(24u, state.Width);

		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, image,
			ImageFontMatch, out var projected, out var handled));
		Assert.True(handled);
		Assert.Equal(1u, projected);
		Assert.Equal(12u, Get(ref platform, image, ImageFontMatchHeight));
		Assert.Equal(24u, Get(ref platform, image, ImageFontMatchWidth));

		// Persistence/bootstrap writes are reconciled by the raw-only reader;
		// the public SetControlAttribute path remains init-only.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, image,
			ImageFontMatchHeight, 18u, false));
		Assert.True(MuiCommonControlCore.TryReadImageFontMatchState(
			ref platform, State, image, out state));
		Assert.Equal(18u, state.Height);
		Assert.True(MuiCommonControlCore.TryGetImageFontMatchStateRecord(
			ref platform, State, image, out record));
		Assert.Equal(18u, record.Height);

		var getMessage = APTR.FromPointer(0x3300);
		var getStorage = APTR.FromPointer(0x3340);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			ImageFontMatchWidth));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			image, getMessage));
		Assert.Equal(24u, platform.ReadUInt32(getStorage, 0));

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, image));
	}

	[Fact]
	public void ImageRenderStateUsesNamedGuestRecordForSelectionAndFreeAxes()
	{
		var platform = NewPlatform();
		var imageClass = Register(ref platform, 0x1100, "Image.mui");
		var image = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, BuildTags(ref platform, 0x1900, new[] {
				(ImageFreeHoriz, 1u), (ImageFreeVert, 1u) }));

		Assert.True(MuiCommonControlCore.TryGetImageRenderStateRecord(
			ref platform, State, image, out var initial));
		Assert.Equal(MuiImageRenderStateRecord.Cookie, initial.Magic);
		Assert.Equal(0u, initial.ImageState);
		Assert.Equal(0u, initial.Selected);
		Assert.Equal(1u, initial.FreeHoriz);
		Assert.Equal(1u, initial.FreeVert);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			image, Selected, 1));
		Assert.True(MuiCommonControlCore.TryReadImageRenderState(ref platform,
			State, image, out var state));
		Assert.Equal(1u, state.ImageState);
		Assert.Equal(1u, state.Selected);
		Assert.Equal(1u, state.FreeHoriz);
		Assert.Equal(1u, state.FreeVert);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			image, ImageState, 0));
		Assert.True(MuiCommonControlCore.TryGetImageRenderStateRecord(
			ref platform, State, image, out var changed));
		Assert.Equal(0u, changed.ImageState);
		Assert.Equal(1u, changed.Selected);
	}

	// Gap 11: Cycle uses a class-specific AskMinMax geometry derived from the
	// widest entry text plus the fixed cycle image and inner spacing, with a
	// fixed row height and horizontal growth (no longer the generic path).
	[Fact]
	public void CycleAskMinMaxUsesClassSpecificGeometry()
	{
		var platform = NewPlatform();
		var cycleClass = Register(ref platform, 0x1100, "Cycle.mui");
		var entries = APTR.FromPointer(0x3000);
		platform.WriteCString(APTR.FromPointer(0x3100), "One");     // 3 chars
		platform.WriteCString(APTR.FromPointer(0x3110), "Longest"); // 7 chars
		platform.WriteUInt32(entries, 0, 0x3100);
		platform.WriteUInt32(entries, 4, 0x3110);
		platform.WriteUInt32(entries, 8, 0);
		var cycle = MuiCommonControlCore.CreateControl(ref platform, State,
			cycleClass, APTR.Null);
		Set(ref platform, cycle, CycleEntries, entries.Raw);

		var storage = APTR.FromPointer(0x1400);
		var packet = APTR.FromPointer(0x1440);
		platform.WriteUInt32(packet, 0, AskMinMax);
		platform.WriteUInt32(packet, 4, storage.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			cycle, packet));
		// 16 (image) + 4 (spacing) + 7*8 (widest text) = 76 wide; height fixed 14.
		Assert.Equal(76, platform.ReadUInt16(storage, 0));    // MinWidth
		Assert.Equal(14, platform.ReadUInt16(storage, 2));    // MinHeight
		Assert.Equal(10000, platform.ReadUInt16(storage, 4)); // MaxWidth grows
		Assert.Equal(14, platform.ReadUInt16(storage, 6));    // MaxHeight fixed
		Assert.Equal(76, platform.ReadUInt16(storage, 8));    // DefWidth
		Assert.Equal(14, platform.ReadUInt16(storage, 10));   // DefHeight

		// With no entries a single-character text allowance is used (16+4+8=28).
		var empty = MuiCommonControlCore.CreateControl(ref platform, State,
			cycleClass, APTR.Null);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			empty, packet));
		Assert.Equal(28, platform.ReadUInt16(storage, 0));
		Assert.Equal(14, platform.ReadUInt16(storage, 2));
		Assert.Equal(28, platform.ReadUInt16(storage, 8));
	}

	// Gap 2: a Gauge shows progress text (MUIA_Gauge_InfoText) with %ld replaced
	// by the current value and %% collapsed to a literal percent, centered inside
	// the gauge; MUIA_Gauge_Divide scales every Current before further processing.
	[Fact]
	public void GaugeInfoTextRendersProgressAndDivideScalesCurrent()
	{
		var platform = NewPlatform();
		var renderInfo = APTR.FromPointer(0x1480);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		var drawPacket = APTR.FromPointer(0x1440);
		platform.WriteUInt32(drawPacket, 0, Draw);
		platform.WriteUInt32(drawPacket, 4, 0);

		var gaugeClass = Register(ref platform, 0x1100, "Gauge.mui");
		var info = APTR.FromPointer(0x1800);
		platform.WriteCString(info, "%ld %%");
		// Divide of 4 means a construction Current of 200 stores as 50.
		var tags = BuildTags(ref platform, 0x1900, new[] {
			(GaugeMax, 100u), (GaugeDivide, 4u), (GaugeCurrent, 200u),
			(GaugeInfoText, info.Raw) });
		var gauge = MuiCommonControlCore.CreateControl(ref platform, State,
			gaugeClass, tags);
		Assert.Equal(50u, Get(ref platform, gauge, GaugeCurrent));

		// The InfoText format string is copied into owned storage.
		var owned = Get(ref platform, gauge, GaugeInfoText);
		Assert.NotEqual(info.Raw, owned);
		Assert.Equal("%ld %%", ReadCString(ref platform, APTR.FromPointer(owned)));

		// A later Current set is divided too: 360 / 4 = 90.
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			gauge, GaugeCurrent, 360));
		Assert.Equal(90u, Get(ref platform, gauge, GaugeCurrent));

		// Drawing renders "90 %" centered within an 80px-wide gauge:
		// text width = 4 chars * 8 = 32, left = (80 - 32) / 2 = 24.
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, gauge, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, gauge, 0, 0,
			80, 16));
		var textBefore = platform.TextCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			gauge, drawPacket));
		Assert.Equal(textBefore + 1, platform.TextCount);
		Assert.Equal("90 %", ReadCString(ref platform, platform.LastText));
		Assert.Equal(24, platform.LastTextLeft);

		// Changing the InfoText at runtime re-copies and is reflected on redraw.
		var info2 = APTR.FromPointer(0x1880);
		platform.WriteCString(info2, "val=%ld");
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			gauge, GaugeInfoText, info2.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			gauge, drawPacket));
		Assert.Equal("val=90", ReadCString(ref platform, platform.LastText));

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, gauge));
	}

	[Fact]
	public void GaugeInfoTextUsesNamedGuestRecordAndTracksReplacement()
	{
		var platform = NewPlatform();
		var gaugeClass = Register(ref platform, 0x1100, "Gauge.mui");
		var source = APTR.FromPointer(0x1C80);
		platform.WriteCString(source, "value=%ld");
		var gauge = MuiCommonControlCore.CreateControl(ref platform, State,
			gaugeClass, BuildTags(ref platform, 0x1CC0, new[] {
				(GaugeInfoText, source.Raw) }));
		Assert.NotEqual(APTR.Null, gauge);
		Assert.True(MuiCommonControlCore.TryGetGaugeInfoTextStateRecord(
			ref platform, State, gauge, out var record));
		Assert.Equal(MuiGaugeInfoTextStateRecord.Cookie, record.Magic);
		Assert.NotEqual(source.Raw, record.InfoText.Raw);
		Assert.Equal("value=%ld", ReadCString(ref platform, record.InfoText));

		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, gauge,
			GaugeInfoText, out var projectedInfoText, out var infoTextHandled));
		Assert.True(infoTextHandled);
		Assert.Equal(record.InfoText.Raw, projectedInfoText);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, gauge,
			GaugeInfoText, out projectedInfoText));
		Assert.Equal(record.InfoText.Raw, projectedInfoText);
		var infoTextGetMessage = APTR.FromPointer(0x1E00);
		var infoTextGetStorage = APTR.FromPointer(0x1E40);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			infoTextGetMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			infoTextGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			GaugeInfoText));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			infoTextGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			infoTextGetStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			gauge, infoTextGetMessage));
		Assert.Equal(record.InfoText.Raw, platform.ReadUInt32(infoTextGetStorage, 0));

		var replacement = APTR.FromPointer(0x1D00);
		platform.WriteCString(replacement, "now=%ld");
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			gauge, GaugeInfoText, replacement.Raw));
		Assert.True(MuiCommonControlCore.TryGetGaugeInfoTextStateRecord(
			ref platform, State, gauge, out record));
		Assert.NotEqual(replacement.Raw, record.InfoText.Raw);
		Assert.Equal("now=%ld", ReadCString(ref platform, record.InfoText));
		Assert.True(MuiCommonControlCore.TryReadGaugeInfoTextState(ref platform,
			State, gauge, out var state));
		Assert.Equal(record.InfoText.Raw, state.InfoText.Raw);

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, gauge));
		Assert.Equal("value=%ld", ReadCString(ref platform, source));
		Assert.Equal("now=%ld", ReadCString(ref platform, replacement));
	}

	// Gap 4: String Secret masking, Integer set/get + accept/reject filtering,
	// Format alignment, and Placeholder — grounded in the String.mui autodoc.
	[Fact]
	public void StringSecretIntegerFormatAndPlaceholderSemanticsHold()
	{
		var platform = NewPlatform();
		var renderInfo = APTR.FromPointer(0x1480);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		var drawPacket = APTR.FromPointer(0x1440);
		platform.WriteUInt32(drawPacket, 0, Draw);
		platform.WriteUInt32(drawPacket, 4, 0);

		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var placeholder = APTR.FromPointer(0x1800);
		platform.WriteCString(placeholder, "type here");
		// Secret + right alignment + integer seed of 42.
		var tags = BuildTags(ref platform, 0x1900, new[] {
			(StringMaxLen, 16u), (StringSecret, 1u),
			(StringFormat, StringFormatRight),
			(StringPlaceholder, placeholder.Raw), (StringInteger, 42u) });
		var s = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, tags);
		Assert.True(MuiCommonControlCore.TryReadStringPresentationState(
			ref platform, State, s, out var presentation));
		Assert.Equal(16u, presentation.MaxLen);
		Assert.Equal(1u, presentation.Secret);
		Assert.Equal(StringFormatRight, presentation.Format);
		Assert.Equal(0u, presentation.Unicode);

		// The integer seed is materialised into the contents and is get-parseable.
		Assert.Equal("42", ReadCString(ref platform, APTR.FromPointer(
			Get(ref platform, s, StringContents))));
		Assert.Equal(42u, Get(ref platform, s, StringInteger));

		// Placeholder is copied into owned storage (independent of the caller).
		var ownedPh = Get(ref platform, s, StringPlaceholder);
		Assert.NotEqual(placeholder.Raw, ownedPh);
		Assert.Equal("type here",
			ReadCString(ref platform, APTR.FromPointer(ownedPh)));

		// Secret and Format are [I.G] init-only and reject post-init sets.
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			s, StringSecret, 0));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			s, StringFormat, 0));

		// Drawing masks "42" to dots and right-aligns within 64px:
		// masked width = 2 * 8 = 16, left = 64 - 16 = 48.
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, s, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, s, 0, 0, 64, 14));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			s, drawPacket));
		Assert.Equal("..", ReadCString(ref platform, platform.LastText));
		Assert.Equal(48, platform.LastTextLeft);

		// Setting Integer at runtime rewrites contents and keeps get in sync.
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			s, StringInteger, 7));
		Assert.Equal("7", ReadCString(ref platform, APTR.FromPointer(
			Get(ref platform, s, StringContents))));
		Assert.Equal(7u, Get(ref platform, s, StringInteger));

		// A second, empty gadget shows its placeholder text on redraw.
		var emptyTags = BuildTags(ref platform, 0x1A00,
			new[] { (StringPlaceholder, placeholder.Raw) });
		var empty = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, emptyTags);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, empty, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, empty, 0, 0,
			128, 14));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			empty, drawPacket));
		Assert.Equal("type here",
			ReadCString(ref platform, platform.LastText));

		// Integer filtering piggybacks on Accept/Reject: a non-accepted or a
		// rejected keystroke is refused; an accepted digit edits and re-syncs.
		var accept = APTR.FromPointer(0x1B00);
		platform.WriteCString(accept, "0123456789");
		var reject = APTR.FromPointer(0x1B40);
		platform.WriteCString(reject, "5");
		Set(ref platform, s, StringAccept, accept.Raw);
		Set(ref platform, s, StringReject, reject.Raw);
		var intuiMessage = APTR.FromPointer(0x1B80);
		platform.WriteUInt32(intuiMessage, 20, 0x00000400);
		var eventPacket = APTR.FromPointer(0x1BC0);
		platform.WriteUInt32(eventPacket, 0, HandleEvent);
		platform.WriteUInt32(eventPacket, 4, intuiMessage.Raw);
		platform.WriteUInt32(eventPacket, 8, unchecked((uint)-1));
		platform.WriteUInt16(intuiMessage, 24, (ushort)'A');
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			s, eventPacket));
		platform.WriteUInt16(intuiMessage, 24, (ushort)'5');
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			s, eventPacket));
		platform.WriteUInt16(intuiMessage, 24, (ushort)'3');
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			s, eventPacket));
		// Editing keeps the integer view synchronised: "7" + "3" => 73.
		Assert.Equal("73", ReadCString(ref platform, APTR.FromPointer(
			Get(ref platform, s, StringContents))));
		Assert.Equal(73u, Get(ref platform, s, StringInteger));

		// Unknown alignment selectors are normalized to the documented left
		// selector at construction and never leak into the renderer.
		var invalidTags = BuildTags(ref platform, 0x1C00,
			new[] { (StringFormat, 99u) });
		var invalid = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, invalidTags);
		Assert.True(MuiCommonControlCore.TryReadStringPresentationState(
			ref platform, State, invalid, out presentation));
		Assert.Equal(0u, presentation.Format);

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, s));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, empty));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, invalid));
	}

	[Fact]
	public void StringPlaceholderUsesNamedGuestRecordAndOwnedReplacement()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var source = APTR.FromPointer(0x1D00);
		platform.WriteCString(source, "hint");
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1D80, new[] {
				(StringPlaceholder, source.Raw) }));
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiCommonControlCore.TryGetStringPlaceholderStateRecord(
			ref platform, State, stringObj, out var record));
		Assert.Equal(MuiStringPlaceholderStateRecord.Cookie, record.Magic);
		Assert.NotEqual(source.Raw, record.Contents.Raw);
		Assert.Equal(record.Contents.Raw, Get(ref platform, stringObj,
			StringPlaceholder));
		Assert.Equal("hint", ReadCString(ref platform, record.Contents));

		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			StringPlaceholder, out var projectedPlaceholder,
			out var placeholderHandled));
		Assert.True(placeholderHandled);
		Assert.Equal(record.Contents.Raw, projectedPlaceholder);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			stringObj, StringPlaceholder, out projectedPlaceholder));
		Assert.Equal(record.Contents.Raw, projectedPlaceholder);
		var placeholderGetMessage = APTR.FromPointer(0x1E00);
		var placeholderGetStorage = APTR.FromPointer(0x1E40);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			placeholderGetMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			placeholderGetMessage, MuiCommonPacketKind.Get,
			MuiCommonField.Attribute, StringPlaceholder));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			placeholderGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			placeholderGetStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, placeholderGetMessage));
		Assert.Equal(record.Contents.Raw,
			platform.ReadUInt32(placeholderGetStorage, 0));

		var replacement = APTR.FromPointer(0x1DC0);
		platform.WriteCString(replacement, "new hint");
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringPlaceholder, replacement.Raw));
		Assert.True(MuiCommonControlCore.TryGetStringPlaceholderStateRecord(
			ref platform, State, stringObj, out record));
		Assert.NotEqual(replacement.Raw, record.Contents.Raw);
		Assert.Equal("new hint", ReadCString(ref platform, record.Contents));

		var unmapped = APTR.FromPointer(0x7F0000);
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringPlaceholder, unmapped.Raw));
		Assert.Equal("new hint", ReadCString(ref platform, record.Contents));

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			stringObj));
	}

	[Fact]
	public void StringContentsUsesNamedGuestRecordAndTracksOwnedEdits()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var source = APTR.FromPointer(0x1E00);
		platform.WriteCString(source, "hello");
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1E80, new[] {
				(StringContents, source.Raw) }));
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiCommonControlCore.TryGetStringContentsStateRecord(
			ref platform, State, stringObj, out var record));
		Assert.Equal(MuiStringContentsStateRecord.Cookie, record.Magic);
		Assert.NotEqual(source.Raw, record.Contents.Raw);
		Assert.Equal(record.Contents.Raw, Get(ref platform, stringObj,
			StringContents));
		Assert.Equal("hello", ReadCString(ref platform, record.Contents));

		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			StringContents, out var projectedContents, out var contentsHandled));
		Assert.True(contentsHandled);
		Assert.Equal(record.Contents.Raw, projectedContents);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			stringObj, StringContents, out projectedContents));
		Assert.Equal(record.Contents.Raw, projectedContents);
		var contentsGetMessage = APTR.FromPointer(0x1F00);
		var contentsGetStorage = APTR.FromPointer(0x1F40);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			contentsGetMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			contentsGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			StringContents));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			contentsGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			contentsGetStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, contentsGetMessage));
		Assert.Equal(record.Contents.Raw, platform.ReadUInt32(contentsGetStorage, 0));

		var replacement = APTR.FromPointer(0x1E40);
		platform.WriteCString(replacement, "world");
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringContents, replacement.Raw));
		Assert.True(MuiCommonControlCore.TryGetStringContentsStateRecord(
			ref platform, State, stringObj, out record));
		Assert.NotEqual(replacement.Raw, record.Contents.Raw);
		Assert.Equal("world", ReadCString(ref platform, record.Contents));
		Assert.Equal(record.Contents.Raw, Get(ref platform, stringObj,
			StringContents));

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			stringObj));
	}

	[Fact]
	public void StringPresentationUsesNamedGuestRecord()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var tags = BuildTags(ref platform, 0x1D80, new[] {
			(StringMaxLen, 24u),
			(StringSecret, 9u),
			(StringFormat, StringFormatRight),
			(Unicode, 7u) });
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, tags);
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiCommonControlCore.TryGetStringPresentationStateRecord(
			ref platform, State, stringObj, out var record));
		Assert.Equal(MuiStringPresentationStateRecord.Cookie, record.Magic);
		Assert.Equal(24u, record.MaxLen);
		Assert.Equal(1u, record.Secret);
		Assert.Equal(StringFormatRight, record.Format);
		Assert.Equal(1u, record.Unicode);
		Assert.Equal(24u, Get(ref platform, stringObj, StringMaxLen));
		Assert.Equal(1u, Get(ref platform, stringObj, StringSecret));
		Assert.Equal(StringFormatRight, Get(ref platform, stringObj, StringFormat));
		Assert.Equal(1u, Get(ref platform, stringObj, Unicode));
		foreach (var pair in new[] {
			(StringMaxLen, 24u), (StringSecret, 1u),
			(StringFormat, StringFormatRight), (Unicode, 1u) })
		{
			Assert.True(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
				pair.Item1, out var projected, out var handled));
			Assert.True(handled);
			Assert.Equal(pair.Item2, projected);
		}
		var getMessage = APTR.FromPointer(0x3A00);
		var getStorage = APTR.FromPointer(0x3A40);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		foreach (var pair in new[] {
			(StringMaxLen, 24u), (StringSecret, 1u),
			(StringFormat, StringFormatRight), (Unicode, 1u) })
		{
			Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
				getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
				pair.Item1));
			Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform,
				State, stringObj, getMessage));
			Assert.Equal(pair.Item2, platform.ReadUInt32(getStorage, 0));
		}
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			stringObj));
	}

	[Fact]
	public void StringFilterStateUsesNamedPointersAndRejectsMalformedGuestStrings()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var accept = APTR.FromPointer(0x1800);
		var reject = APTR.FromPointer(0x1840);
		platform.WriteCString(accept, "abc");
		platform.WriteCString(reject, "x");
		var tags = BuildTags(ref platform, 0x1900, new[] {
			(MuiCommonControlCore.StringAccept, accept.Raw),
			(MuiCommonControlCore.StringReject, reject.Raw) });
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, tags);
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiCommonControlCore.TryReadStringFilterState(ref platform,
			State, stringObj, out MuiStringFilterState filters));
		Assert.Equal(accept.Raw, filters.Accept.Raw);
		Assert.Equal(reject.Raw, filters.Reject.Raw);

		// A malformed caller pointer is rejected before it can replace the live
		// filter, preserving the previous named state.
		var unmapped = APTR.FromPointer(0x7F0000);
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, MuiCommonControlCore.StringAccept, unmapped.Raw));
		Assert.Equal(accept.Raw, Get(ref platform, stringObj,
			MuiCommonControlCore.StringAccept));

		// The same validation applies to initial tags; construction fails without
		// publishing an object whose filter pointers cannot be safely read.
		var badTags = BuildTags(ref platform, 0x1A00, new[] {
			(MuiCommonControlCore.StringReject, unmapped.Raw) });
		var rejected = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, badTags);
		Assert.Equal(APTR.Null, rejected);

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			stringObj));
	}

	[Fact]
	public void StringFilterUsesNamedGuestRecord()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var accept = APTR.FromPointer(0x1D00);
		var reject = APTR.FromPointer(0x1D40);
		platform.WriteCString(accept, "abc");
		platform.WriteCString(reject, "x");
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1D80, new[] {
				(StringAccept, accept.Raw), (StringReject, reject.Raw) }));
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiCommonControlCore.TryGetStringFilterStateRecord(
			ref platform, State, stringObj, out var record));
		Assert.Equal(MuiStringFilterStateRecord.Cookie, record.Magic);
		Assert.Equal(accept.Raw, record.Accept.Raw);
		Assert.Equal(reject.Raw, record.Reject.Raw);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			MuiCommonControlCore.StringAccept, out var projectedAccept,
			out var filterHandled));
		Assert.True(filterHandled);
		Assert.Equal(accept.Raw, projectedAccept);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			MuiCommonControlCore.StringReject, out var projectedReject,
			out filterHandled));
		Assert.True(filterHandled);
		Assert.Equal(reject.Raw, projectedReject);
		var filterGetMessage = APTR.FromPointer(0x3500);
		var filterGetStorage = APTR.FromPointer(0x3540);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			filterGetMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			filterGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			MuiCommonControlCore.StringAccept));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			filterGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			filterGetStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, filterGetMessage));
		Assert.Equal(accept.Raw, platform.ReadUInt32(filterGetStorage, 0));

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringReject, 0));
		Assert.True(MuiCommonControlCore.TryGetStringFilterStateRecord(
			ref platform, State, stringObj, out record));
		Assert.True(record.Reject.IsNull);
		Assert.Equal(accept.Raw, record.Accept.Raw);

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			stringObj));
	}

	[Fact]
	public void StringInteractionStateCanonicalizesBooleansAndLeavesAdvanceOnCrToCycleRouting()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var tags = BuildTags(ref platform, 0x1800, new[] {
			(MuiCommonControlCore.StringEditable, 0xFFFFFFFFu),
			(MuiCommonControlCore.StringAdvanceOnCR, 2u),
			(MuiCommonControlCore.StringMultiline, 7u) });
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, tags);
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.Equal(1u, Get(ref platform, stringObj,
			MuiCommonControlCore.StringEditable));
		Assert.Equal(1u, Get(ref platform, stringObj,
			MuiCommonControlCore.StringAdvanceOnCR));
		Assert.Equal(1u, Get(ref platform, stringObj,
			MuiCommonControlCore.StringMultiline));

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, MuiCommonControlCore.StringEditable, 0xFFFFFFFFu));
		Assert.Equal(1u, Get(ref platform, stringObj,
			MuiCommonControlCore.StringEditable));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, MuiCommonControlCore.StringAdvanceOnCR, 0));
		Assert.Equal(0u, Get(ref platform, stringObj,
			MuiCommonControlCore.StringAdvanceOnCR));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, MuiCommonControlCore.StringMultiline, 0));
		Assert.Equal(1u, Get(ref platform, stringObj,
			MuiCommonControlCore.StringMultiline));

		// Re-enable AdvanceOnCR and verify Return is deliberately left for the
		// containing cycle-chain/input platform instead of acknowledging text.
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, MuiCommonControlCore.StringAdvanceOnCR, 1));
		var packet = APTR.FromPointer(0x1C00);
		platform.WriteUInt32(packet, 0, HandleEvent);
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyPress));
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			stringObj));
	}

	[Fact]
	public void StringInteractionUsesNamedGuestRecord()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var tags = BuildTags(ref platform, 0x1D00, new[] {
			(MuiCommonControlCore.StringEditable, 0u),
			(MuiCommonControlCore.StringAdvanceOnCR, 7u),
			(MuiCommonControlCore.StringMultiline, 9u) });
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, tags);
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiCommonControlCore.TryGetStringInteractionStateRecord(
			ref platform, State, stringObj, out var record));
		Assert.Equal(MuiStringInteractionStateRecord.Cookie, record.Magic);
		Assert.Equal(0u, record.Editable);
		Assert.Equal(1u, record.AdvanceOnCR);
		Assert.Equal(1u, record.Multiline);

		// Runtime setters update both the public attribute and the named record.
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, MuiCommonControlCore.StringEditable, 1));
		Assert.True(MuiCommonControlCore.TryGetStringInteractionStateRecord(
			ref platform, State, stringObj, out record));
		Assert.Equal(1u, record.Editable);
		Assert.Equal(1u, Get(ref platform, stringObj,
			MuiCommonControlCore.StringEditable));

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			stringObj));
	}

	[Fact]
	public void StringEditHookUsesNamedSgWorkAndLonelyHookSuppressesPrivateEditing()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var source = APTR.FromPointer(0x1800);
		var replacement = APTR.FromPointer(0x2D00);
		platform.WriteCString(source, "abc");
		platform.WriteCString(replacement, "hooked");
		var hook = APTR.FromPointer(0x2C00);
		platform.WriteUInt32(hook, 8, MuiHeadlessTestPlatform.HookEntryStringEdit);
		platform.WriteUInt32(hook, 16, 0);
		var tags = BuildTags(ref platform, 0x1900, new[] {
			(StringContents, source.Raw),
			(MuiCommonControlCore.StringEditHook, hook.Raw) });
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, tags);
		Assert.NotEqual(APTR.Null, stringObj);

		platform.StringEditHookResult = 1;
		platform.StringEditHookActions = MuiStringEditWorkCodec.ActionUse;
		platform.StringEditHookBuffer = replacement;
		var packet = APTR.FromPointer(0x1C00);
		platform.WriteUInt32(packet, 0, HandleEvent);
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyPress));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));
		Assert.Equal("hooked", ReadCString(ref platform, APTR.FromPointer(
			Get(ref platform, stringObj, StringContents))));
		Assert.Equal(1u, platform.HookInvokeCount);
		Assert.NotEqual(APTR.Null, platform.LastHookA2);
		Assert.NotEqual(APTR.Null, platform.LastHookA1);

		// A zero result normally falls back to MUI's private editor.
		platform.StringEditHookResult = 0;
		platform.StringEditHookActions = MuiStringEditWorkCodec.ActionUse;
		platform.StringEditHookBuffer = APTR.Null;
		var intuiMessage = APTR.FromPointer(0x1D00);
		platform.WriteUInt32(intuiMessage, 20, 0x00000400);
		platform.WriteUInt16(intuiMessage, 24, (ushort)'Q');
		platform.WriteUInt32(packet, 4, intuiMessage.Raw);
		platform.WriteUInt32(packet, 8, unchecked((uint)-1));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));
		Assert.Equal("hooQked", ReadCString(ref platform, APTR.FromPointer(
			Get(ref platform, stringObj, StringContents))));

		// With LonelyEditHook enabled the same zero result suppresses private
		// editing and leaves contents unchanged.
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, MuiCommonControlCore.StringLonelyEditHook, 1));
		platform.WriteUInt16(intuiMessage, 24, (ushort)'R');
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));
		Assert.Equal("hooQked", ReadCString(ref platform, APTR.FromPointer(
			Get(ref platform, stringObj, StringContents))));

		var unmapped = APTR.FromPointer(0x7F0000);
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, MuiCommonControlCore.StringEditHook, unmapped.Raw));
		var badTags = BuildTags(ref platform, 0x1E00, new[] {
			(MuiCommonControlCore.StringEditHook, unmapped.Raw) });
		Assert.Equal(APTR.Null, MuiCommonControlCore.CreateControl(ref platform,
			State, stringClass, badTags));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			stringObj));
	}

	[Fact]
	public void StringEditHookUsesNamedGuestRecord()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var hook = APTR.FromPointer(0x2E80);
		platform.WriteUInt32(hook, 8, MuiHeadlessTestPlatform.HookEntryStringEdit);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x2F00, new[] {
				(MuiCommonControlCore.StringEditHook, hook.Raw),
				(MuiCommonControlCore.StringLonelyEditHook, 7u) }));
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiCommonControlCore.TryGetStringEditHookStateRecord(
			ref platform, State, stringObj, out var record));
		Assert.Equal(MuiStringEditHookStateRecord.Cookie, record.Magic);
		Assert.Equal(hook.Raw, record.EditHook.Raw);
		Assert.Equal(1u, record.LonelyEditHook);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			MuiCommonControlCore.StringEditHook, out var projectedEditHook,
			out var editHookHandled));
		Assert.True(editHookHandled);
		Assert.Equal(hook.Raw, projectedEditHook);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			MuiCommonControlCore.StringLonelyEditHook,
			out var projectedLonelyEditHook, out editHookHandled));
		Assert.True(editHookHandled);
		Assert.Equal(1u, projectedLonelyEditHook);
		var editHookGetMessage = APTR.FromPointer(0x3400);
		var editHookGetStorage = APTR.FromPointer(0x3440);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			editHookGetMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			editHookGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			MuiCommonControlCore.StringEditHook));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			editHookGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			editHookGetStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, editHookGetMessage));
		Assert.Equal(hook.Raw, platform.ReadUInt32(editHookGetStorage, 0));

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, MuiCommonControlCore.StringLonelyEditHook, 0));
		Assert.True(MuiCommonControlCore.TryGetStringEditHookStateRecord(
			ref platform, State, stringObj, out record));
		Assert.Equal(0u, record.LonelyEditHook);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, MuiCommonControlCore.StringEditHook, 0));
		Assert.True(MuiCommonControlCore.TryGetStringEditHookStateRecord(
			ref platform, State, stringObj, out record));
		Assert.True(record.EditHook.IsNull);

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			stringObj));
	}

	[Fact]
	public void StringAttachedListUsesNamedPointerAndForwardsListviewNavigation()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var listClass = Register(ref platform, 0x1200, "List.mui");
		var listviewClass = Register(ref platform, 0x1240, "Listview.mui");
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		var first = APTR.FromPointer(0x2300);
		var second = APTR.FromPointer(0x2340);
		var third = APTR.FromPointer(0x2380);
		platform.WriteCString(first, "first");
		platform.WriteCString(second, "second");
		platform.WriteCString(third, "third");
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, first,
			ListInsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, second,
			ListInsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, third,
			ListInsertBottom));
		var listviewTags = BuildTags(ref platform, 0x2400, new[] {
			(ListviewList, list.Raw) });
		var listview = MuiListviewCore.CreateListview(ref platform, State,
			listviewClass, listviewTags);
		Assert.NotEqual(APTR.Null, listview);

		var source = APTR.FromPointer(0x2480);
		platform.WriteCString(source, "search");
		var stringTags = BuildTags(ref platform, 0x2500, new[] {
			(StringContents, source.Raw),
			(StringAttachedList, listview.Raw) });
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, stringTags);
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiCommonControlCore.TryReadStringAttachedListState(ref platform,
			State, stringObj, out var attached));
		Assert.Equal(listview.Raw, attached.Listview.Raw);

		var packet = APTR.FromPointer(0x2600);
		platform.WriteUInt32(packet, 0, HandleEvent);
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyDown));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));
		Assert.Equal(0u, Get(ref platform, list, ListActive));
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyDown));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));
		Assert.Equal(1u, Get(ref platform, list, ListActive));

		var unmapped = APTR.FromPointer(0x7F0000);
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringAttachedList, unmapped.Raw));
		Assert.Equal(listview.Raw, Get(ref platform, stringObj,
			StringAttachedList));
		var badTags = BuildTags(ref platform, 0x2700, new[] {
			(StringAttachedList, unmapped.Raw) });
		Assert.Equal(APTR.Null, MuiCommonControlCore.CreateControl(ref platform,
			State, stringClass, badTags));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringAttachedList, 0));
		Assert.Equal(0u, Get(ref platform, stringObj, StringAttachedList));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			stringObj));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			listview));
	}

	[Fact]
	public void StringAttachedListUsesNamedGuestRecord()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var listClass = Register(ref platform, 0x1200, "List.mui");
		var listviewClass = Register(ref platform, 0x1240, "Listview.mui");
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		var listview = MuiListviewCore.CreateListview(ref platform, State,
			listviewClass, BuildTags(ref platform, 0x2F00, new[] {
				(ListviewList, list.Raw) }));
		Assert.NotEqual(APTR.Null, listview);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x2F80, new[] {
				(StringAttachedList, listview.Raw) }));
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiCommonControlCore.TryGetStringAttachedListStateRecord(
			ref platform, State, stringObj, out var record));
		Assert.Equal(MuiStringAttachedListStateRecord.Cookie, record.Magic);
		Assert.Equal(listview.Raw, record.Listview.Raw);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			StringAttachedList, out var projectedListview, out var listviewHandled));
		Assert.True(listviewHandled);
		Assert.Equal(record.Listview.Raw, projectedListview);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			stringObj, StringAttachedList, out projectedListview));
		Assert.Equal(record.Listview.Raw, projectedListview);
		var attachedGetMessage = APTR.FromPointer(0x3100);
		var attachedGetStorage = APTR.FromPointer(0x3140);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			attachedGetMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			attachedGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			StringAttachedList));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			attachedGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			attachedGetStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, attachedGetMessage));
		Assert.Equal(record.Listview.Raw,
			platform.ReadUInt32(attachedGetStorage, 0));

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringAttachedList, 0));
		Assert.True(MuiCommonControlCore.TryGetStringAttachedListStateRecord(
			ref platform, State, stringObj, out record));
		Assert.True(record.Listview.IsNull);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringAttachedList, listview.Raw));
		Assert.True(MuiCommonControlCore.TryGetStringAttachedListStateRecord(
			ref platform, State, stringObj, out record));
		Assert.Equal(listview.Raw, record.Listview.Raw);

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			stringObj));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			listview));
	}

	[Fact]
	public void StringInteger64UsesNamedQuadStorageAndBoundedDecimalConversion()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var negative = APTR.FromPointer(0x2800);
		// MorphOS QUAD -42: high ULONG followed by low ULONG.
		platform.WriteUInt32(negative, 0, 0xFFFFFFFFu);
		platform.WriteUInt32(negative, 4, 0xFFFFFFD6u);
		var tags = BuildTags(ref platform, 0x2900, new[] {
			(StringMaxLen, 32u), (StringInteger64, negative.Raw) });
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, tags);
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.Equal("-42", ReadCString(ref platform, APTR.FromPointer(
			Get(ref platform, stringObj, StringContents))));
		var ownedPointer = Get(ref platform, stringObj, StringInteger64);
		Assert.NotEqual(negative.Raw, ownedPointer);
		Assert.True(MuiCommonControlCore.TryReadStringInteger64State(ref platform,
			State, stringObj, out var state, out var stored));
		Assert.Equal(Get(ref platform, stringObj, StringInteger64), state.Value.Raw);
		Assert.Equal(0xFFFFFFFFu, stored.High);
		Assert.Equal(0xFFFFFFD6u, stored.Low);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			StringInteger64, out var projectedInteger64, out var integer64Handled));
		Assert.True(integer64Handled);
		Assert.Equal(ownedPointer, projectedInteger64);
		var getMessage = APTR.FromPointer(0x2B00);
		var getStorage = APTR.FromPointer(0x2B40);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			StringInteger64));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, getMessage));
		Assert.Equal(ownedPointer, platform.ReadUInt32(getStorage, 0));

		// A value outside the 32-bit Integer range is copied and rendered without
		// using managed 64-bit conversion helpers.
		var wide = APTR.FromPointer(0x2840);
		platform.WriteUInt32(wide, 0, 1);
		platform.WriteUInt32(wide, 4, 0);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringInteger64, wide.Raw));
		Assert.Equal(ownedPointer, Get(ref platform, stringObj, StringInteger64));
		Assert.Equal("4294967296", ReadCString(ref platform, APTR.FromPointer(
			Get(ref platform, stringObj, StringContents))));
		Assert.True(MuiCommonControlCore.TryReadStringInteger64State(ref platform,
			State, stringObj, out _, out stored));
		Assert.Equal(1u, stored.High);
		Assert.Equal(0u, stored.Low);

		// Contents edits parse back into the live QUAD, including the signed
		// minimum, while the caller-owned text remains just an input STRPTR.
		var minimumText = APTR.FromPointer(0x2880);
		platform.WriteCString(minimumText, "-9223372036854775808");
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringContents, minimumText.Raw));
		Assert.True(MuiCommonControlCore.TryReadStringInteger64State(ref platform,
			State, stringObj, out _, out stored));
		Assert.Equal(0x80000000u, stored.High);
		Assert.Equal(0u, stored.Low);

		var unmapped = APTR.FromPointer(0x7F0000);
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringInteger64, unmapped.Raw));
		var badTags = BuildTags(ref platform, 0x2A00, new[] {
			(StringInteger64, unmapped.Raw) });
		Assert.Equal(APTR.Null, MuiCommonControlCore.CreateControl(ref platform,
			State, stringClass, badTags));

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			stringObj));
	}

	[Fact]
	public void StringIntegerUsesNamedGuestRecordAndTracksContents()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var source = APTR.FromPointer(0x2C80);
		platform.WriteCString(source, "-17");
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x2D00, new[] {
				(StringContents, source.Raw) }));
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiCommonControlCore.TryGetStringIntegerStateRecord(
			ref platform, State, stringObj, out var record));
		Assert.Equal(MuiStringIntegerStateRecord.Cookie, record.Magic);
		Assert.Equal(-17, record.Value);
		Assert.Equal(unchecked((uint)-17), Get(ref platform, stringObj,
			StringInteger));
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			StringInteger, out var projectedInteger, out var integerHandled));
		Assert.True(integerHandled);
		Assert.Equal(unchecked((uint)-17), projectedInteger);
		var integerGetMessage = APTR.FromPointer(0x3200);
		var integerGetStorage = APTR.FromPointer(0x3240);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			integerGetMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			integerGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			StringInteger));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			integerGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			integerGetStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, integerGetMessage));
		Assert.Equal(unchecked((uint)-17),
			platform.ReadUInt32(integerGetStorage, 0));

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringInteger, 123));
		Assert.True(MuiCommonControlCore.TryGetStringIntegerStateRecord(
			ref platform, State, stringObj, out record));
		Assert.Equal(123, record.Value);
		Assert.Equal("123", ReadCString(ref platform, APTR.FromPointer(
			Get(ref platform, stringObj, StringContents))));

		var replacement = APTR.FromPointer(0x2D80);
		platform.WriteCString(replacement, "-9");
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringContents, replacement.Raw));
		Assert.True(MuiCommonControlCore.TryGetStringIntegerStateRecord(
			ref platform, State, stringObj, out record));
		Assert.Equal(-9, record.Value);

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			stringObj));
	}

	[Fact]
	public void StringSpellCheckingUsesNamedBooleanStateAndCanonicalSetGet()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var tags = BuildTags(ref platform, 0x2B00, new[] {
			(StringSpellChecking, 0xFFFFFFFFu) });
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, tags);
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiCommonControlCore.TryReadStringSpellCheckingState(
			ref platform, State, stringObj, out var spellChecking));
		Assert.Equal(1u, spellChecking.Enabled);
		Assert.Equal(1u, Get(ref platform, stringObj, StringSpellChecking));
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			StringSpellChecking, out var projectedSpellChecking,
			out var spellCheckingHandled));
		Assert.True(spellCheckingHandled);
		Assert.Equal(1u, projectedSpellChecking);
		var spellCheckingGetMessage = APTR.FromPointer(0x3300);
		var spellCheckingGetStorage = APTR.FromPointer(0x3340);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			spellCheckingGetMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			spellCheckingGetMessage, MuiCommonPacketKind.Get,
			MuiCommonField.Attribute, StringSpellChecking));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			spellCheckingGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			spellCheckingGetStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, spellCheckingGetMessage));
		Assert.Equal(1u, platform.ReadUInt32(spellCheckingGetStorage, 0));

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringSpellChecking, 0));
		Assert.Equal(0u, Get(ref platform, stringObj, StringSpellChecking));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringSpellChecking, 7));
		Assert.Equal(1u, Get(ref platform, stringObj, StringSpellChecking));
		Assert.True(MuiCommonControlCore.TryReadStringSpellCheckingState(
			ref platform, State, stringObj, out spellChecking));
		Assert.Equal(1u, spellChecking.Enabled);

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			stringObj));
	}

	[Fact]
	public void StringSpellCheckingUsesNamedGuestRecord()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var tags = BuildTags(ref platform, 0x2C80, new[] {
			(StringSpellChecking, 9u) });
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, tags);
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiCommonControlCore.TryGetStringSpellCheckingStateRecord(
			ref platform, State, stringObj, out var record));
		Assert.Equal(MuiStringSpellCheckingStateRecord.Cookie, record.Magic);
		Assert.Equal(1u, record.Enabled);
		Assert.Equal(1u, Get(ref platform, stringObj, StringSpellChecking));

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringSpellChecking, 0));
		Assert.True(MuiCommonControlCore.TryGetStringSpellCheckingStateRecord(
			ref platform, State, stringObj, out record));
		Assert.Equal(0u, record.Enabled);
		Assert.Equal(0u, Get(ref platform, stringObj, StringSpellChecking));

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			stringObj));
	}

	[Fact]
	public void StringAcknowledgeUsesNamedContentsPointerOnReturn()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var source = APTR.FromPointer(0x2C00);
		platform.WriteCString(source, "ack");
		var tags = BuildTags(ref platform, 0x2D00, new[] {
			(StringContents, source.Raw) });
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, tags);
		Assert.NotEqual(APTR.Null, stringObj);

		var packet = APTR.FromPointer(0x2E00);
		platform.WriteUInt32(packet, 0, HandleEvent);
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyPress));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));
		Assert.True(MuiCommonControlCore.TryReadStringAcknowledgeState(
			ref platform, State, stringObj, out var acknowledge));
		Assert.Equal(Get(ref platform, stringObj, StringContents),
			acknowledge.Contents.Raw);
		Assert.Equal("ack", ReadCString(ref platform, acknowledge.Contents));

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			stringObj));
	}

	[Fact]
	public void StringAcknowledgeUsesNamedGuestRecord()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var source = APTR.FromPointer(0x2F00);
		platform.WriteCString(source, "ack");
		var tags = BuildTags(ref platform, 0x2F80, new[] {
			(StringContents, source.Raw) });
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, tags);
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiCommonControlCore.TryGetStringAcknowledgeStateRecord(
			ref platform, State, stringObj, out var record));
		Assert.Equal(MuiStringAcknowledgeStateRecord.Cookie, record.Magic);
		Assert.True(record.Contents.IsNull);

		var packet = APTR.FromPointer(0x3000);
		platform.WriteUInt32(packet, 0, HandleEvent);
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, unchecked((uint)KeyPress));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));
		Assert.True(MuiCommonControlCore.TryGetStringAcknowledgeStateRecord(
			ref platform, State, stringObj, out record));
		Assert.Equal(Get(ref platform, stringObj, StringContents),
			record.Contents.Raw);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			StringAcknowledge, out var projectedAcknowledge,
			out var acknowledgeHandled));
		Assert.True(acknowledgeHandled);
		Assert.Equal(record.Contents.Raw, projectedAcknowledge);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			stringObj, StringAcknowledge, out projectedAcknowledge));
		Assert.Equal(record.Contents.Raw, projectedAcknowledge);
		var acknowledgeGetMessage = APTR.FromPointer(0x3080);
		var acknowledgeGetStorage = APTR.FromPointer(0x30C0);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			acknowledgeGetMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			acknowledgeGetMessage, MuiCommonPacketKind.Get,
			MuiCommonField.Attribute, StringAcknowledge));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			acknowledgeGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			acknowledgeGetStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, acknowledgeGetMessage));
		Assert.Equal(record.Contents.Raw,
			platform.ReadUInt32(acknowledgeGetStorage, 0));
		Assert.Equal(record.Contents.Raw, Get(ref platform, stringObj,
			StringAcknowledge));

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			stringObj));
	}

	[Fact]
	public void StringCursorStateUsesNamedPositionsAndClampsWithoutPrivateOffsets()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var source = APTR.FromPointer(0x3000);
		platform.WriteCString(source, "abcd");
		var tags = BuildTags(ref platform, 0x3100, new[] {
			(StringContents, source.Raw),
			(StringBufferPos, 99u),
			(StringDisplayPos, 99u) });
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, tags);
		Assert.NotEqual(APTR.Null, stringObj);

		Assert.True(MuiCommonControlCore.TryReadStringCursorState(
			ref platform, State, stringObj, out var cursor));
		Assert.Equal(4, cursor.BufferPos);
		Assert.Equal(4, cursor.DisplayPos);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringBufferPos, 2));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringDisplayPos, 1));
		Assert.True(MuiCommonControlCore.TryReadStringCursorState(
			ref platform, State, stringObj, out cursor));
		Assert.Equal(2, cursor.BufferPos);
		Assert.Equal(1, cursor.DisplayPos);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringBufferPos, uint.MaxValue));
		Assert.True(MuiCommonControlCore.TryReadStringCursorState(
			ref platform, State, stringObj, out cursor));
		Assert.Equal(4, cursor.BufferPos);
		Assert.Equal(1, cursor.DisplayPos);

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			stringObj));
	}

	[Fact]
	public void StringCursorUsesNamedGuestRecord()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var source = APTR.FromPointer(0x3200);
		platform.WriteCString(source, "cursor");
		var tags = BuildTags(ref platform, 0x3300, new[] {
			(StringContents, source.Raw),
			(StringBufferPos, 3u),
			(StringDisplayPos, 1u) });
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, tags);
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiCommonControlCore.TryGetStringCursorStateRecord(
			ref platform, State, stringObj, out var record));
		Assert.Equal(MuiStringCursorStateRecord.Cookie, record.Magic);
		Assert.Equal(3, record.BufferPos);
		Assert.Equal(1, record.DisplayPos);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringBufferPos, 5));
		Assert.True(MuiCommonControlCore.TryGetStringCursorStateRecord(
			ref platform, State, stringObj, out record));
		Assert.Equal(5, record.BufferPos);
		Assert.Equal(1, record.DisplayPos);
		Assert.Equal(5u, Get(ref platform, stringObj, StringBufferPos));
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			StringBufferPos, out var projectedBuffer, out var bufferHandled));
		Assert.True(bufferHandled);
		Assert.Equal(5u, projectedBuffer);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			StringDisplayPos, out var projectedDisplay, out var displayHandled));
		Assert.True(displayHandled);
		Assert.Equal(1u, projectedDisplay);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			stringObj, StringDisplayPos, out projectedDisplay));
		Assert.Equal(1u, projectedDisplay);

		var getMessage = APTR.FromPointer(0x3400);
		var getStorage = APTR.FromPointer(0x3440);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			StringBufferPos));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, getMessage));
		Assert.Equal(5u, platform.ReadUInt32(getStorage, 0));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			StringDisplayPos));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, getMessage));
		Assert.Equal(1u, platform.ReadUInt32(getStorage, 0));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			stringObj));
	}

	[Fact]
	public void StringInteractionUsesNamedGuestRecordAndProjectsGetters()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var tags = BuildTags(ref platform, 0x3500, new[] {
			(StringEditable, 0u),
			(StringAdvanceOnCR, 1u),
			(StringMultiline, 1u) });
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, tags);
		Assert.NotEqual(APTR.Null, stringObj);

		Assert.True(MuiCommonControlCore.TryReadStringInteractionState(
			ref platform, State, stringObj, out var interaction));
		Assert.Equal(0u, interaction.Editable);
		Assert.Equal(1u, interaction.AdvanceOnCR);
		Assert.Equal(1u, interaction.Multiline);
		Assert.True(MuiCommonControlCore.TryGetStringInteractionStateRecord(
			ref platform, State, stringObj, out var record));
		Assert.Equal(MuiStringInteractionStateRecord.Cookie, record.Magic);
		Assert.Equal(0u, record.Editable);
		Assert.Equal(1u, record.AdvanceOnCR);
		Assert.Equal(1u, record.Multiline);

		AssertInteractionGetter(ref platform, stringObj, StringEditable, 0u);
		AssertInteractionGetter(ref platform, stringObj, StringAdvanceOnCR, 1u);
		AssertInteractionGetter(ref platform, stringObj, StringMultiline, 1u);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringEditable, 9u));
		AssertInteractionGetter(ref platform, stringObj, StringEditable, 1u);
		Assert.True(MuiCommonControlCore.TryGetStringInteractionStateRecord(
			ref platform, State, stringObj, out record));
		Assert.Equal(1u, record.Editable);

		var getMessage = APTR.FromPointer(0x3600);
		var getStorage = APTR.FromPointer(0x3640);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		foreach (var pair in new[] {
			(StringEditable, 1u), (StringAdvanceOnCR, 1u), (StringMultiline, 1u) })
		{
			Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
				getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
				pair.Item1));
			Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform,
				State, stringObj, getMessage));
			Assert.Equal(pair.Item2, platform.ReadUInt32(getStorage, 0));
		}

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			stringObj));
	}

	// Gap 7: Scale renders a graduated 0%..100% scale whose detail (division
	// count) adapts to the available width or height.
	[Fact]
	public void ScaleRendersGraduatedZeroToHundredPercent()
	{
		var platform = NewPlatform();
		var renderInfo = APTR.FromPointer(0x1480);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		var drawPacket = APTR.FromPointer(0x1440);
		platform.WriteUInt32(drawPacket, 0, Draw);
		platform.WriteUInt32(drawPacket, 4, 0);

		var scaleClass = Register(ref platform, 0x1100, "Scale.mui");

		// A wide (>=110px) horizontal scale renders 10 divisions:
		// one axis line + 11 tick lines = 12 lines.
		var wide = MuiCommonControlCore.CreateControl(ref platform, State,
			scaleClass, APTR.Null);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, wide, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, wide, 0, 0,
			110, 8));
		var wideBefore = platform.LineCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			wide, drawPacket));
		Assert.Equal(wideBefore + 12, platform.LineCount);
		// The final tick sits at the 100% end and is a full-height major.
		Assert.Equal(109, platform.LastLineX1);
		Assert.Equal(109, platform.LastLineX2);
		Assert.Equal(0, platform.LastLineY1);
		Assert.Equal(7, platform.LastLineY2);

		// A narrow (20px) scale is less detailed: 2 divisions => axis + 3 ticks.
		var narrow = MuiCommonControlCore.CreateControl(ref platform, State,
			scaleClass, APTR.Null);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, narrow, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, narrow, 0, 0,
			20, 8));
		var narrowBefore = platform.LineCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			narrow, drawPacket));
		Assert.Equal(narrowBefore + 4, platform.LineCount);

		// Vertical Scale behavior is covered separately so this fact keeps the
		// historical horizontal and narrow-width evidence focused.
	}

	[Fact]
	public void VerticalScaleRendersGraduatedZeroToHundredPercent()
	{
		var platform = NewPlatform();
		var renderInfo = APTR.FromPointer(0x1480);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		var drawPacket = APTR.FromPointer(0x1440);
		platform.WriteUInt32(drawPacket, 0, Draw);
		platform.WriteUInt32(drawPacket, 4, 0);

		var scaleClass = Register(ref platform, 0x1100, "Scale.mui");
		var vertical = MuiCommonControlCore.CreateControl(ref platform, State,
			scaleClass, APTR.Null);
		Set(ref platform, vertical, ScaleHoriz, 0);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, vertical,
			renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, vertical, 0, 0,
			20, 40));
		var verticalBefore = platform.LineCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			vertical, drawPacket));
		// Height 40 selects two divisions: one axis line plus three ticks.
		Assert.Equal(verticalBefore + 4, platform.LineCount);
		// The final major tick spans the complete width at the 100% end.
		Assert.Equal(0, platform.LastLineX1);
		Assert.Equal(39, platform.LastLineY1);
		Assert.Equal(19, platform.LastLineX2);
		Assert.Equal(39, platform.LastLineY2);
	}

	[Fact]
	public void TextPreParseControlCharMarkingAndShortenAttributesFollowContract()
	{
		var platform = NewPlatform();
		var textClass = Register(ref platform, 0x1100, "Text.mui");
		var preParse = APTR.FromPointer(0x1800);
		platform.WriteCString(preParse, "\u001bc\u001bi");
		var contents = APTR.FromPointer(0x1820);
		platform.WriteCString(contents, "foobar");
		var tags = BuildTags(ref platform, 0x1900, new[] {
			(TextPreParse, preParse.Raw), (TextContents, contents.Raw),
			(TextControlChar, (uint)'f'), (TextShorten, TextShortenCutoff) });
		var text = MuiCommonControlCore.CreateControl(ref platform, State, textClass,
			tags);
		Assert.True(text.IsNotNull);

		// PreParse is copied into a private buffer; the caller pointer is released.
		var preParsePtr = APTR.FromPointer(Get(ref platform, text, TextPreParse));
		Assert.NotEqual(preParse.Raw, preParsePtr.Raw);
		Assert.Equal("\u001bc\u001bi", ReadCString(ref platform, preParsePtr));

		// ControlChar is [ISG]: the constructed value is retained and re-settable.
		Assert.Equal((uint)'f', Get(ref platform, text, TextControlChar));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			text, TextControlChar, (uint)'b'));
		Assert.Equal((uint)'b', Get(ref platform, text, TextControlChar));

		// Shorten is [ISG]: honored from construction and re-settable at runtime.
		Assert.Equal(TextShortenCutoff, Get(ref platform, text, TextShorten));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			text, TextShorten, TextShortenHide));
		Assert.Equal(TextShortenHide, Get(ref platform, text, TextShorten));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			text, TextShorten, 3));
		Assert.Equal(TextShortenHide, Get(ref platform, text, TextShorten));

		// Marking defaults FALSE and is [I.G] (init + get, not settable at runtime).
		Assert.Equal(0u, Get(ref platform, text, TextMarking));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			text, TextMarking, 1));
		Assert.Equal(0u, Get(ref platform, text, TextMarking));

		// Shortened is get-only and rejects direct sets.
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			text, TextShortened, 1));

		// PreParse is re-settable and re-copied into the private buffer.
		var newPreParse = APTR.FromPointer(0x1860);
		platform.WriteCString(newPreParse, "\u001br");
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			text, TextPreParse, newPreParse.Raw));
		var updated = APTR.FromPointer(Get(ref platform, text, TextPreParse));
		Assert.NotEqual(newPreParse.Raw, updated.Raw);
		Assert.Equal("\u001br", ReadCString(ref platform, updated));

		// Disposal retires the owned buffers without touching caller memory.
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, text));
		Assert.Equal("\u001bc\u001bi", ReadCString(ref platform, preParse));
		Assert.Equal("foobar", ReadCString(ref platform, contents));
	}

	[Fact]
	public void TextPresentationUsesNamedGuestRecordForSizingInputAndDrawingPolicy()
	{
		var platform = NewPlatform();
		var textClass = Register(ref platform, 0x1E80, "Text.mui");
		var contents = APTR.FromPointer(0x1EC0);
		platform.WriteCString(contents, "LongText");
		var text = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, BuildTags(ref platform, 0x1F00, new[] {
				(TextContents, contents.Raw), (TextSetMin, 1u),
				(TextSetMax, 1u), (TextSetVMax, 0u),
				(TextControlChar, (uint)'x'), (TextMarking, 1u),
				(TextShorten, TextShortenCutoff), (TextHiChar, (uint)'^') }));

		Assert.True(MuiCommonControlCore.TryGetTextPresentationStateRecord(
			ref platform, State, text, out var initial));
		Assert.Equal(MuiTextPresentationStateRecord.Cookie, initial.Magic);
		Assert.Equal(1u, initial.SetMin);
		Assert.Equal(1u, initial.SetMax);
		Assert.Equal(0u, initial.SetVMax);
		Assert.Equal((uint)'x', initial.ControlChar);
		Assert.Equal(1u, initial.Marking);
		Assert.Equal(TextShortenCutoff, initial.Shorten);
		Assert.Equal((uint)'^', initial.HiChar);
		Assert.Equal(1u, initial.HiCharPresent);
		foreach (var pair in new[] {
			(TextSetMin, 1u), (TextSetMax, 1u), (TextSetVMax, 0u),
			(TextControlChar, (uint)'x'), (TextMarking, 1u),
			(TextShorten, TextShortenCutoff), (TextHiChar, (uint)'^') })
		{
			Assert.True(MuiCommonControlCore.TryGet(ref platform, State, text,
				pair.Item1, out var projected, out var handled));
			Assert.True(handled);
			Assert.Equal(pair.Item2, projected);
			Assert.Equal(pair.Item2, Get(ref platform, text, pair.Item1));
		}
		var getMessage = APTR.FromPointer(0x2100);
		var getStorage = APTR.FromPointer(0x2140);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		foreach (var pair in new[] {
			(TextSetMin, 1u), (TextSetMax, 1u), (TextSetVMax, 0u),
			(TextControlChar, (uint)'x'), (TextMarking, 1u),
			(TextShorten, TextShortenCutoff), (TextHiChar, (uint)'^') })
		{
			Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
				getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
				pair.Item1));
			Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform,
				State, text, getMessage));
			Assert.Equal(pair.Item2, platform.ReadUInt32(getStorage, 0));
		}

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			text, TextControlChar, (uint)'y'));
		Assert.True(MuiCommonControlCore.TryReadTextPresentationState(
			ref platform, State, text, out var changed));
		Assert.Equal((uint)'y', changed.ControlChar);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			text, TextShorten, TextShortenHide));
		Assert.True(MuiCommonControlCore.TryGetTextPresentationStateRecord(
			ref platform, State, text, out var updated));
		Assert.Equal(TextShortenHide, updated.Shorten);
	}

	[Fact]
	public void TextMeasurementCountsVisibleGlyphsAcrossPreParseEscapesAndNewlines()
	{
		var platform = NewPlatform();
		var textClass = Register(ref platform, 0x1100, "Text.mui");
		var preParse = APTR.FromPointer(0x1800);
		platform.WriteCString(preParse, "\u001bc");        // alignment only, 0 glyphs
		var contents = APTR.FromPointer(0x1820);
		// Line 1 "MUI" (bold/normal escapes), line 2 "is magic".
		platform.WriteCString(contents, "\u001bbMUI\u001bn\nis magic");
		var tags = BuildTags(ref platform, 0x1900, new[] {
			(TextPreParse, preParse.Raw), (TextContents, contents.Raw),
			(TextSetMin, 1u), (TextSetMax, 1u), (TextSetVMax, 1u) });
		var text = MuiCommonControlCore.CreateControl(ref platform, State, textClass,
			tags);

		var storage = APTR.FromPointer(0x1400);
		var packet = APTR.FromPointer(0x1440);
		platform.WriteUInt32(packet, 0, AskMinMax);
		platform.WriteUInt32(packet, 4, storage.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			text, packet));

		// Widest visible line is "is magic" (8 glyphs -> 64px); escape style codes
		// and the centered PreParse add no width. Two lines -> 20px tall.
		Assert.Equal(64, platform.ReadUInt16(storage, 0));  // MinWidth (SetMin)
		Assert.Equal(20, platform.ReadUInt16(storage, 2));  // MinHeight
		Assert.Equal(64, platform.ReadUInt16(storage, 4));  // MaxWidth (SetMax)
		Assert.Equal(20, platform.ReadUInt16(storage, 6));  // MaxHeight (SetVMax)
		Assert.Equal(64, platform.ReadUInt16(storage, 8));  // DefWidth
		Assert.Equal(20, platform.ReadUInt16(storage, 10)); // DefHeight

		// The same visible glyphs supplied without escapes measure identically,
		// proving escape bytes are excluded from the width.
		var plain = APTR.FromPointer(0x1A00);
		platform.WriteCString(plain, "MUI\nis magic");
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			text, TextContents, plain.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			text, packet));
		Assert.Equal(64, platform.ReadUInt16(storage, 0));
		Assert.Equal(20, platform.ReadUInt16(storage, 2));
	}

	[Fact]
	public void TextShortenModesGovernDrawnGlyphsAndShortenedFlag()
	{
		var platform = NewPlatform();
		var textClass = Register(ref platform, 0x1100, "Text.mui");
		var renderInfo = APTR.FromPointer(0x1480);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		var packet = APTR.FromPointer(0x1440);
		platform.WriteUInt32(packet, 0, Draw);
		platform.WriteUInt32(packet, 4, 0);

		// MUIV_Text_Shorten_Nothing: the full "LongText" (8 glyphs) is drawn even
		// though only 40px (5 glyphs) is allocated; Shortened is still reported.
		var nothing = MakeShortenText(ref platform, textClass, TextShortenNothing,
			0x1800);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, nothing, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, nothing, 0, 0, 40,
			10));
		platform.TextCount = 0;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			nothing, packet));
		Assert.Equal(1u, Get(ref platform, nothing, TextShortened));
		Assert.True(platform.TextCount > 0);
		Assert.Equal(8, platform.LastTextLength);

		// MUIV_Text_Shorten_Cutoff: the over-wide line is trimmed to the visible
		// width with a trailing "..." ellipsis (5 glyphs -> "Lo...").
		var cutoff = MakeShortenText(ref platform, textClass, TextShortenCutoff,
			0x1900);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, cutoff, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, cutoff, 0, 0, 40,
			10));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			cutoff, packet));
		Assert.Equal(1u, Get(ref platform, cutoff, TextShortened));
		Assert.Equal(5, platform.LastTextLength);
		Assert.Equal((byte)'L', platform.ReadUInt8(platform.LastText, 0));
		Assert.Equal((byte)'o', platform.ReadUInt8(platform.LastText, 1));
		Assert.Equal((byte)'.', platform.ReadUInt8(platform.LastText, 2));
		Assert.Equal((byte)'.', platform.ReadUInt8(platform.LastText, 3));
		Assert.Equal((byte)'.', platform.ReadUInt8(platform.LastText, 4));

		// MUIV_Text_Shorten_Hide: nothing is drawn, yet Shortened is reported.
		var hide = MakeShortenText(ref platform, textClass, TextShortenHide, 0x1A00);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, hide, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, hide, 0, 0, 40, 10));
		platform.TextCount = 0;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			hide, packet));
		Assert.Equal(1u, Get(ref platform, hide, TextShortened));
		Assert.Equal(0u, platform.TextCount);
	}

	[Fact]
	public void TextAlignmentAndMarkingPenDriveDrawPlacement()
	{
		var platform = NewPlatform();
		var textClass = Register(ref platform, 0x1100, "Text.mui");
		var contents = APTR.FromPointer(0x1800);
		platform.WriteCString(contents, "Hi");             // 2 glyphs -> 16px
		var preParse = APTR.FromPointer(0x1820);
		platform.WriteCString(preParse, "\u001br");        // right justify
		var tags = BuildTags(ref platform, 0x1900, new[] {
			(TextContents, contents.Raw), (TextPreParse, preParse.Raw),
			(TextMarking, 1u), (TextSetMin, 0u), (TextSetMax, 0u) });
		var text = MuiCommonControlCore.CreateControl(ref platform, State, textClass,
			tags);

		var renderInfo = APTR.FromPointer(0x1480);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, text, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, text, 0, 0, 80, 10));
		var packet = APTR.FromPointer(0x1440);
		platform.WriteUInt32(packet, 0, Draw);
		platform.WriteUInt32(packet, 4, 0);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			text, packet));

		// "Hi" (16px) right-justified in an 80px field starts at x = 64.
		Assert.Equal(64, platform.LastTextLeft);
		Assert.Equal(2, platform.LastTextLength);
		// Marking selects the DrawInfo marking pen for the glyphs.
		Assert.Equal(6u, platform.LastPen);
		// 16px fits in 80px, so the text is not shortened.
		Assert.Equal(0u, Get(ref platform, text, TextShortened));
	}

	[Fact]
	public void ScrollbarBuildsMorphosGroupChildrenAndForwardsPropState()
	{
		var platform = NewPlatform();
		var propClass = Register(ref platform, 0x1100, "Prop.mui");
		var gadgetClass = Register(ref platform, 0x1140, "Gadget.mui");
		var scrollbarClass = Register(ref platform, 0x1180, "Scrollbar.mui");
		var tags = BuildTags(ref platform, 0x1900, new[] {
			(GroupHoriz, 1u), (ScrollbarType, ScrollbarTypeSym),
			(PropEntries, 100u), (PropVisible, 10u), (PropFirst, 20u) });
		var scrollbar = MuiCommonControlCore.CreateControl(ref platform, State,
			scrollbarClass, tags);
		Assert.True(scrollbar.IsNotNull);
		Assert.Equal(MuiControlClass.Scrollbar,
			MuiCommonControlCore.Classify(ref platform, State, scrollbar));

		var first = MuiFamilyCore.GetChild(ref platform, State, scrollbar, 0,
			APTR.Null);
		var prop = MuiFamilyCore.GetChild(ref platform, State, scrollbar, 1,
			APTR.Null);
		var second = MuiFamilyCore.GetChild(ref platform, State, scrollbar, 2,
			APTR.Null);
		Assert.Equal(MuiControlClass.Gadget,
			MuiCommonControlCore.Classify(ref platform, State, first));
		Assert.Equal(MuiControlClass.Prop,
			MuiCommonControlCore.Classify(ref platform, State, prop));
		Assert.Equal(MuiControlClass.Gadget,
			MuiCommonControlCore.Classify(ref platform, State, second));
		Assert.Equal(100u, Get(ref platform, prop, PropEntries));
		Assert.Equal(10u, Get(ref platform, prop, PropVisible));
		Assert.Equal(20u, Get(ref platform, prop, PropFirst));

		var storage = APTR.FromPointer(0x1A00);
		var packet = APTR.FromPointer(0x1A40);
		platform.WriteUInt32(packet, 0, AskMinMax);
		platform.WriteUInt32(packet, 4, storage.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			scrollbar, packet));
		Assert.Equal(32, platform.ReadUInt16(storage, 0));
		Assert.Equal(16, platform.ReadUInt16(storage, 2));
		Assert.Equal(10000, platform.ReadUInt16(storage, 4));
		Assert.Equal(10000, platform.ReadUInt16(storage, 6));
		Assert.Equal(32, platform.ReadUInt16(storage, 8));
		Assert.Equal(16, platform.ReadUInt16(storage, 10));

		var renderInfo = APTR.FromPointer(0x1B00);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, scrollbar,
			renderInfo));
		var layoutPacket = APTR.FromPointer(0x1B20);
		platform.WriteUInt32(layoutPacket, 0, Layout);
		platform.WriteUInt32(layoutPacket, 4, 0);
		platform.WriteUInt32(layoutPacket, 8, 0);
		platform.WriteUInt32(layoutPacket, 12, 80);
		platform.WriteUInt32(layoutPacket, 16, 16);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			scrollbar, layoutPacket));
		Assert.Equal(0u, Get(ref platform, first, LeftEdge));
		Assert.Equal(16u, Get(ref platform, first, Width));
		Assert.Equal(16u, Get(ref platform, prop, LeftEdge));
		Assert.Equal(48u, Get(ref platform, prop, Width));
		Assert.Equal(64u, Get(ref platform, second, LeftEdge));
		Assert.Equal(16u, Get(ref platform, second, Width));
		Assert.Equal(16u, Get(ref platform, prop, FixHeight));

		var drawPacket = APTR.FromPointer(0x1B40);
		platform.WriteUInt32(drawPacket, 0, Draw);
		platform.WriteUInt32(drawPacket, 4, 0);
		var linesBefore = platform.LineCount;
		var fillsBefore = platform.FillCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			scrollbar, drawPacket));
		Assert.True(platform.LineCount >= linesBefore + 12);
		Assert.True(platform.FillCount > fillsBefore);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			scrollbar, PropFirst, 40));
		Assert.Equal(40u, Get(ref platform, scrollbar, PropFirst));
		Assert.Equal(40u, Get(ref platform, prop, PropFirst));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			scrollbar, ScrollbarType, ScrollbarTypeNone));

		// The selector changes the family order while None keeps the children
		// present but hidden, so later state changes do not rebuild ownership.
		var topTags = BuildTags(ref platform, 0x1C00, new[] {
			(ScrollbarType, ScrollbarTypeTop) });
		var top = MuiCommonControlCore.CreateControl(ref platform, State,
			scrollbarClass, topTags);
		Assert.Equal(MuiControlClass.Prop, MuiCommonControlCore.Classify(ref platform,
			State, MuiFamilyCore.GetChild(ref platform, State, top, 2, APTR.Null)));
		var bottomTags = BuildTags(ref platform, 0x1D00, new[] {
			(ScrollbarType, ScrollbarTypeBottom) });
		var bottom = MuiCommonControlCore.CreateControl(ref platform, State,
			scrollbarClass, bottomTags);
		Assert.Equal(MuiControlClass.Prop, MuiCommonControlCore.Classify(ref platform,
			State, MuiFamilyCore.GetChild(ref platform, State, bottom, 0, APTR.Null)));
		var noneTags = BuildTags(ref platform, 0x1E00, new[] {
			(ScrollbarType, ScrollbarTypeNone) });
		var none = MuiCommonControlCore.CreateControl(ref platform, State,
			scrollbarClass, noneTags);
		Assert.Equal(0u, Get(ref platform,
			MuiFamilyCore.GetChild(ref platform, State, none, 0, APTR.Null), ShowMe));

		var eventPacket = APTR.FromPointer(0x1B80);
		platform.WriteUInt32(eventPacket, 0, HandleEvent);
		platform.WriteUInt32(eventPacket, 8, unchecked((uint)KeyDown));
		Set(ref platform, scrollbar, Disabled, 1);
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			scrollbar, eventPacket));
		Assert.Equal(40u, Get(ref platform, prop, PropFirst));

		var freedBefore = platform.FreeCount;
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			scrollbar));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, top));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, bottom));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, none));
		Assert.True(platform.FreeCount > freedBefore);
	}

	[Fact]
	public void ScrollbarLayoutUsesNamedGuestRecordForOrientationAndType()
	{
		var platform = NewPlatform();
		var propClass = Register(ref platform, 0x2000, "Prop.mui");
		var gadgetClass = Register(ref platform, 0x2040, "Gadget.mui");
		var scrollbarClass = Register(ref platform, 0x2080, "Scrollbar.mui");
		var scrollbar = MuiCommonControlCore.CreateControl(ref platform, State,
			scrollbarClass, BuildTags(ref platform, 0x20C0, new[] {
				(GroupHoriz, 1u), (ScrollbarType, ScrollbarTypeTop) }));

		Assert.True(MuiCommonControlCore.TryGetScrollbarLayoutStateRecord(
			ref platform, State, scrollbar, out var initial));
		Assert.Equal(MuiScrollbarLayoutStateRecord.Cookie, initial.Magic);
		Assert.Equal(1u, initial.Horizontal);
		Assert.Equal(ScrollbarTypeTop, initial.Type);

		// The record remains the authority for layout consumers, but a direct
		// guest scalar update is reconciled at the boundary for persistence and
		// compatibility paths.
		Set(ref platform, scrollbar, GroupHoriz, 0);
		Set(ref platform, scrollbar, ScrollbarType, ScrollbarTypeNone);
		Assert.True(MuiCommonControlCore.TryReadScrollbarLayoutState(
			ref platform, State, scrollbar, out var changed));
		Assert.Equal(0u, changed.Horizontal);
		Assert.Equal(ScrollbarTypeNone, changed.Type);
		Assert.True(MuiCommonControlCore.TryGetScrollbarLayoutStateRecord(
			ref platform, State, scrollbar, out var synchronized));
		Assert.Equal(0u, synchronized.Horizontal);
		Assert.Equal(ScrollbarTypeNone, synchronized.Type);
	}

	[Fact]
	public void PropDeltaFactorAndSliderQuietFollowAuthorityAttributes()
	{
		var platform = NewPlatform();
		var propClass = Register(ref platform, 0x1100, "Prop.mui");
		var sliderClass = Register(ref platform, 0x1140, "Slider.mui");
		var propTags = BuildTags(ref platform, 0x1900, new[] {
			(PropEntries, 100u), (PropVisible, 10u), (PropFirst, 20u),
			(PropDeltaFactor, 3u), (PropSlider, 1u),
			(PropUseWinBorder, 2u) });
		var prop = MuiCommonControlCore.CreateControl(ref platform, State,
			propClass, propTags);
		Assert.Equal(3u, Get(ref platform, prop, PropDeltaFactor));
		Assert.Equal(1u, Get(ref platform, prop, PropSlider));
		Assert.Equal(2u, Get(ref platform, prop, PropUseWinBorder));

		var methodPacket = APTR.FromPointer(0x1400);
		platform.WriteUInt32(methodPacket, 0, PropIncrease);
		platform.WriteUInt32(methodPacket, 4, 2);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			prop, methodPacket));
		Assert.Equal(26u, Get(ref platform, prop, PropFirst));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			prop, PropDeltaFactor, 2));
		platform.WriteUInt32(methodPacket, 0, PropDecrease);
		platform.WriteUInt32(methodPacket, 4, 1);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			prop, methodPacket));
		Assert.Equal(24u, Get(ref platform, prop, PropFirst));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			prop, PropSlider, 2));
		Assert.Equal(1u, Get(ref platform, prop, PropSlider));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			prop, PropUseWinBorder, 0));

		var storage = APTR.FromPointer(0x1500);
		platform.WriteUInt32(methodPacket, 0, AskMinMax);
		platform.WriteUInt32(methodPacket, 4, storage.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			prop, methodPacket));
		Assert.Equal(0, platform.ReadUInt16(storage, 0));
		Assert.Equal(0, platform.ReadUInt16(storage, 2));
		Assert.Equal(0, platform.ReadUInt16(storage, 4));
		Assert.Equal(0, platform.ReadUInt16(storage, 6));
		var renderInfo = APTR.FromPointer(0x2200);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, prop, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, prop, 0, 0, 40,
			16));
		var drawPacket = APTR.FromPointer(0x1580);
		platform.WriteUInt32(drawPacket, 0, Draw);
		platform.WriteUInt32(drawPacket, 4, 0);
		var fillsBefore = platform.FillCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			prop, drawPacket));
		Assert.Equal(fillsBefore + 1, platform.FillCount);

		var sliderTags = BuildTags(ref platform, 0x1A00, new[] {
			(GroupHoriz, 0u), (SliderQuiet, 1u),
			(NumericMin, unchecked((uint)-20)), (NumericMax, 20u),
			(NumericValue, 0u) });
		var slider = MuiCommonControlCore.CreateControl(ref platform, State,
			sliderClass, sliderTags);
		Assert.Equal(0u, Get(ref platform, slider, SliderHoriz));
		Assert.Equal(1u, Get(ref platform, slider, SliderQuiet));
		Assert.Equal(0u, Get(ref platform, slider, NumericCheckAllSizes));
		platform.WriteUInt32(methodPacket, 0, AskMinMax);
		platform.WriteUInt32(methodPacket, 4, storage.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			slider, methodPacket));
		Assert.Equal(14, platform.ReadUInt16(storage, 0));
		Assert.Equal(48, platform.ReadUInt16(storage, 2));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			slider, SliderHoriz, 1));
		Assert.Equal(1u, Get(ref platform, slider, SliderHoriz));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			slider, NumericCheckAllSizes, 1));
		Assert.Equal(1u, Get(ref platform, slider, NumericCheckAllSizes));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			slider, SliderQuiet, 0));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			slider, GroupHoriz, 1));
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, slider, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, slider, 0, 0, 80,
			14));
		var textBefore = platform.TextCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			slider, drawPacket));
		Assert.Equal(textBefore, platform.TextCount);

		var loudTags = BuildTags(ref platform, 0x1B00, new[] {
			(NumericValue, 7u) });
		var loud = MuiCommonControlCore.CreateControl(ref platform, State,
			sliderClass, loudTags);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, loud, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, loud, 0, 0, 80,
			14));
		textBefore = platform.TextCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			loud, drawPacket));
		Assert.Equal(textBefore + 1, platform.TextCount);

		var freedBefore = platform.FreeCount;
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, prop));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, slider));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, loud));
		Assert.True(platform.FreeCount > freedBefore);
	}

	[Fact]
	public void TextHiCharUnderlinesAndControlCharActivatesOnInput()
	{
		var platform = NewPlatform();
		var textClass = Register(ref platform, 0x1100, "Text.mui");
		var contents = APTR.FromPointer(0x1800);
		platform.WriteCString(contents, "Alpha");
		var tags = BuildTags(ref platform, 0x1900, new[] {
			(TextContents, contents.Raw), (TextHiChar, (uint)'a'),
			(TextControlChar, (uint)'x'), (InputMode, InputModeRelVerify) });
		var text = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, tags);

		var renderInfo = APTR.FromPointer(0x1A80);
		platform.WriteUInt32(renderInfo, 20, 0x2000);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, text, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, text, 0, 0,
			80, 10));
		var packet = APTR.FromPointer(0x1B00);
		platform.WriteUInt32(packet, 0, Draw);
		platform.WriteUInt32(packet, 4, 0);
		var linesBefore = platform.LineCount;
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			text, packet));
		Assert.Equal(linesBefore + 1, platform.LineCount);
		Assert.Equal(0, platform.LastLineX1);
		Assert.Equal(7, platform.LastLineX2);
		Assert.Equal(9, platform.LastLineY1);
		Assert.Equal(9, platform.LastLineY2);
		Assert.Equal(3u, platform.LastPen);

		// ControlChar is case-insensitive and accepts the translated IntuiMessage
		// character when MUIM_HandleEvent receives the raw key marker (-1).
		var intuiMessage = APTR.FromPointer(0x1B80);
		platform.WriteUInt32(intuiMessage, 20, 0x00000400);
		platform.WriteUInt16(intuiMessage, 24, (ushort)'X');
		platform.WriteUInt32(packet, 0, HandleEvent);
		platform.WriteUInt32(packet, 4, intuiMessage.Raw);
		platform.WriteUInt32(packet, 8, unchecked((uint)-1));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			text, packet));
		Assert.Equal(0u, Get(ref platform, text, Selected));
		Assert.Equal(1u, Get(ref platform, text, Pressed));

		// A different printable key does not activate the control.
		platform.WriteUInt16(intuiMessage, 24, (ushort)'Y');
		platform.WriteUInt32(packet, 8, unchecked((uint)-1));
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			text, packet));
		Assert.Equal(1u, Get(ref platform, text, Pressed));
	}

	private static APTR MakeShortenText(ref MuiHeadlessTestPlatform platform,
		APTR textClass, uint shorten, uint baseAddr)
	{
		var src = APTR.FromPointer(baseAddr);
		platform.WriteCString(src, "LongText");
		var tags = BuildTags(ref platform, baseAddr + 0x40, new[] {
			(TextContents, src.Raw), (TextShorten, shorten),
			(TextSetMin, 0u), (TextSetMax, 0u) });
		return MuiCommonControlCore.CreateControl(ref platform, State, textClass,
			tags);
	}

	private static MuiHeadlessTestPlatform NewPlatform()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x8000, State);
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		return platform;
	}

	private static APTR Register(ref MuiHeadlessTestPlatform platform, uint nameAddr,
		string name)
	{
		platform.WriteCString(APTR.FromPointer(nameAddr), name);
		return MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			APTR.FromPointer(nameAddr), APTR.Null, 1, APTR.FromPointer(1), false);
	}

	private static APTR BuildTags(ref MuiHeadlessTestPlatform platform, uint addr,
		(uint tag, uint data)[] pairs)
	{
		var offset = 0;
		foreach (var pair in pairs)
		{
			platform.WriteUInt32(APTR.FromPointer(addr), offset, pair.tag);
			platform.WriteUInt32(APTR.FromPointer(addr), offset + 4, pair.data);
			offset += 8;
		}
		platform.WriteUInt32(APTR.FromPointer(addr), offset, 0);
		platform.WriteUInt32(APTR.FromPointer(addr), offset + 4, 0);
		return APTR.FromPointer(addr);
	}

	private static void Set(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute, uint value) => Assert.True(
		MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj, attribute,
			value, false));

	private static uint Get(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			attribute, out var value));
		return value;
	}

	private static void AssertInteractionGetter(ref MuiHeadlessTestPlatform platform,
		APTR obj, uint attribute, uint expected)
	{
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, obj,
			attribute, out var projected, out var handled));
		Assert.True(handled);
		Assert.Equal(expected, projected);
		Assert.Equal(expected, Get(ref platform, obj, attribute));
	}

	private static string ReadCString(ref MuiHeadlessTestPlatform platform,
		APTR address)
	{
		var builder = new StringBuilder();
		for (var index = 0; index < 256; index++)
		{
			var ch = platform.ReadUInt8(address, index);
			if (ch == 0) break;
			builder.Append((char)ch);
		}
		return builder.ToString();
	}

	private static byte[] ReadBytes(ref MuiHeadlessTestPlatform platform, APTR address,
		int? length = null)
	{
		var bytes = new List<byte>();
		for (var index = 0; !length.HasValue || index < length.Value; index++)
		{
			var value = platform.ReadUInt8(address, index);
			if (!length.HasValue && value == 0) break;
			bytes.Add(value);
		}
		return bytes.ToArray();
	}
}

