using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

// Comprehensive host coverage for the additive MG09 external-resource wrapper
// family: the official Boopsi.mui and Dtpic.mui classes and their standalone
// core / dispatcher / lifecycle. Exercises classification, every attribute,
// malformed names/tags, allocation / open / create / acquire failures and
// their failure-atomic rollback, geometry / min-max / draw, the colorwheel -1
// workaround, the IDCMP_UPDATE -> notification mapping, transparent
// attribute pass-through, remember/regenerate, the owned Dtpic name copy with
// caller mutation, and idempotent cleanup / dispose.
public sealed class MuiExternalWrapperTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint Base = 0x1000;
	private const int Size = 0x40000;
	private const uint FirstAllocation = 0x10000;

	private static readonly APTR Instance = APTR.FromPointer(0x2000);
	private static readonly APTR ClassId = APTR.FromPointer(0x2200);
	private static readonly APTR CreationTags = APTR.FromPointer(0x2300);
	private static readonly APTR RenderInfo = APTR.FromPointer(0x2400);
	private static readonly APTR MinMax = APTR.FromPointer(0x2440);
	private static readonly APTR NameA = APTR.FromPointer(0x2500);
	private static readonly APTR NameB = APTR.FromPointer(0x2540);
	private static readonly APTR Packet = APTR.FromPointer(0x2600);
	private static readonly APTR AttrList = APTR.FromPointer(0x2700);
	private static readonly APTR PrivateClass = APTR.FromPointer(0x2800);
	private static readonly APTR Storage = APTR.FromPointer(0x2860);
	private static readonly APTR Window = APTR.FromPointer(0x2900);
	private static readonly APTR Screen = APTR.FromPointer(0x2920);
	private static readonly APTR DrawInfo = APTR.FromPointer(0x2940);
	private static readonly APTR RastPort = APTR.FromPointer(0x2960);

	// Internal layout offsets poked by a few white-box assertions.
	private const int WorkBufferOffset = 80;

	private const uint OmSet = 0x00000103u;
	private const uint OmGet = 0x00000104u;
	private const uint OmUpdate = 0x00000108u;
	private const uint OmDispose = 0x00000102u;
	private const uint MuimSet = 0x8042549au;
	private const uint MethodSetup = 0x80428354u;
	private const uint MethodCleanup = 0x8042d985u;
	private const uint MethodShow = 0x8042cc84u;
	private const uint MethodDraw = 0x80426f3fu;
	private const uint MethodAskMinMax = 0x80423874u;
	private const uint MethodLayout = 0x8042845bu;

	private static MuiHeadlessTestPlatform NewPlatform() =>
		new(Base, Size, FirstAllocation, State);

	private static MuiHeadlessTestPlatform ExhaustedPlatform(uint headroom) =>
		new(Base, Size, (uint)(Base + Size) - headroom, State);

	private static void BuildRenderInfo(ref MuiHeadlessTestPlatform p)
	{
		p.WriteUInt32(RenderInfo, 0, Screen.Raw);
		p.WriteUInt32(RenderInfo, 4, Window.Raw);
		p.WriteUInt32(RenderInfo, 8, DrawInfo.Raw);
		p.WriteUInt32(RenderInfo, 12, RastPort.Raw);
	}

	private static MuiExternalWrapperClass CreateBoopsi(
		ref MuiHeadlessTestPlatform p)
	{
		p.WriteCString(ClassId, "Boopsi.mui");
		return MuiExternalWrapperCore.CreateByName(ref p, Instance, ClassId);
	}

	private static MuiExternalWrapperClass CreateDtpic(
		ref MuiHeadlessTestPlatform p)
	{
		p.WriteCString(ClassId, "Dtpic.mui");
		return MuiExternalWrapperCore.CreateByName(ref p, Instance, ClassId);
	}

	[Fact]
	public void ExternalWrapperPacketCodecUsesNamedRecordsAndRejectsMalformedPackets()
	{
		var p = NewPlatform();

		Assert.True(MuiExternalWrapperMessageCodec.WriteUpdate(ref p, Packet,
			AttrList.Raw, 0x2800, 3));
		Assert.True(MuiExternalWrapperMessageCodec.TryReadUpdate(ref p, Packet,
			out var update));
		Assert.Equal(AttrList.Raw, update.AttributeList);
		Assert.Equal(0x2800u, update.GadgetInfo);
		Assert.Equal(3u, update.Flags);

		Assert.True(MuiExternalWrapperMessageCodec.WriteGet(ref p, Packet, 7,
			Storage.Raw));
		Assert.True(MuiExternalWrapperMessageCodec.TryReadGet(ref p, Packet,
			out var get));
		Assert.Equal(7u, get.Attribute);
		Assert.Equal(Storage.Raw, get.Storage);

		Assert.True(MuiExternalWrapperMessageCodec.WriteSet(ref p, Packet,
			MuiExternalWrapperMessageCodec.MethodSet, 9, 11));
		Assert.True(MuiExternalWrapperMessageCodec.TryReadSet(ref p, Packet,
			MuiExternalWrapperMessageCodec.MethodSet, out var set));
		Assert.Equal(9u, set.Attribute);
		Assert.Equal(11u, set.Value);

		Assert.True(MuiExternalWrapperMessageCodec.WriteRenderInfo(ref p, Packet,
			MuiExternalWrapperMessageCodec.Setup, RenderInfo.Raw));
		Assert.True(MuiExternalWrapperMessageCodec.TryReadRenderInfo(ref p, Packet,
			MuiExternalWrapperMessageCodec.Setup, out var setup));
		Assert.Equal(RenderInfo.Raw, setup.RenderInfo);

		Assert.True(MuiExternalWrapperMessageCodec.WriteAskMinMax(ref p, Packet,
			MinMax.Raw));
		Assert.True(MuiExternalWrapperMessageCodec.TryReadAskMinMax(ref p, Packet,
			out var askMinMax));
		Assert.Equal(MinMax.Raw, askMinMax.Storage);

		Assert.True(MuiExternalWrapperMessageCodec.WriteLayout(ref p, Packet,
			1, 2, 80, 40));
		Assert.True(MuiExternalWrapperMessageCodec.TryReadLayout(ref p, Packet,
			out var layout));
		Assert.Equal(80u, layout.Width);
		Assert.Equal(40u, layout.Height);

		Assert.True(MuiExternalWrapperMessageCodec.WriteMethod(ref p, Packet,
			MuiExternalWrapperMessageCodec.Draw));
		Assert.True(MuiExternalWrapperMessageCodec.IsValidMethod(ref p, Packet,
			MuiExternalWrapperMessageCodec.Draw));
		Assert.False(MuiExternalWrapperMessageCodec.WriteSet(ref p, Packet,
			0x80420000u, 1, 2));
		Assert.False(MuiExternalWrapperMessageCodec.TryReadLayout(ref p,
			APTR.FromPointer(Base + (uint)Size - 1), out _));
		Assert.False(MuiExternalWrapperMessageCodec.IsValidMethod(ref p, Packet,
			0x80420000u));
	}

	[Fact]
	public void ExternalWrapperFieldCursorUsesNamedMixedPacketBoundaries()
	{
		var p = NewPlatform();
		var cursor = default(MuiExternalWrapperFieldCursor);
		cursor.Message = Packet;
		cursor.Packet = MuiExternalWrapperPacketKind.Update;
		cursor.Field = MuiExternalWrapperField.MethodId;
		Assert.True(MuiExternalWrapperFieldCursorCodec.TryGetAddress(ref p,
			cursor, out var address));
		Assert.Equal(Packet.Raw, address.Raw);
		cursor.Field = MuiExternalWrapperField.GadgetInfo;
		Assert.True(MuiExternalWrapperFieldCursorCodec.TryGetAddress(ref p,
			cursor, out address));
		Assert.Equal(Packet.Raw + 8, address.Raw);
		cursor.Field = MuiExternalWrapperField.Flags;
		Assert.True(MuiExternalWrapperFieldCursorCodec.TryGetAddress(ref p,
			cursor, out address));
		Assert.Equal(Packet.Raw + 12, address.Raw);

		Assert.True(MuiExternalWrapperFieldCursorCodec.TryWriteUInt32(ref p,
			Packet, MuiExternalWrapperPacketKind.Layout,
			MuiExternalWrapperField.Height, 0xAABBCCDD));
		Assert.True(MuiExternalWrapperFieldCursorCodec.TryReadUInt32(ref p,
			Packet, MuiExternalWrapperPacketKind.Layout,
			MuiExternalWrapperField.Height, out var height));
		Assert.Equal(0xAABBCCDDu, height);

		cursor.Packet = MuiExternalWrapperPacketKind.Method;
		cursor.Field = MuiExternalWrapperField.Value;
		Assert.False(MuiExternalWrapperFieldCursorCodec.TryGetAddress(ref p,
			cursor, out _));
		cursor.Message = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Packet = MuiExternalWrapperPacketKind.Layout;
		cursor.Field = MuiExternalWrapperField.Height;
		Assert.False(MuiExternalWrapperFieldCursorCodec.TryGetAddress(ref p,
			cursor, out _));
	}

	[Fact]
	public void ExternalWrapperHeaderCodecUsesNamedFields()
	{
		var p = NewPlatform();
		var address = APTR.FromPointer(0x3880);
		var expected = default(MuiExternalWrapperHeader);
		expected.Magic = MuiExternalWrapperHeader.Cookie;
		expected.Class = MuiExternalWrapperClass.Dtpic;
		expected.Flags = MuiExternalWrapperLayout.FlagSetup |
			MuiExternalWrapperLayout.FlagPicture;
		Assert.True(MuiExternalWrapperHeaderCodec.Write(ref p, address,
			expected));
		Assert.True(MuiExternalWrapperHeaderCodec.TryRead(ref p, address,
			out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Class, actual.Class);
		Assert.Equal(expected.Flags, actual.Flags);

		var invalid = default(MuiExternalWrapperHeader);
		invalid.Magic = MuiExternalWrapperHeader.Cookie;
		Assert.False(MuiExternalWrapperHeaderCodec.Write(ref p,
			APTR.FromPointer(0x50000), invalid));
		Assert.False(MuiExternalWrapperHeaderCodec.TryRead(ref p,
			APTR.FromPointer(0x50000), out _));
	}

	[Fact]
	public void ExternalHeaderFieldCursorUsesNamedBoundary()
	{
		var p = NewPlatform();
		var cursor = default(MuiExternalWrapperHeaderFieldCursor);
		cursor.Header = Instance;
		cursor.Field = MuiExternalWrapperHeaderField.Magic;
		Assert.True(MuiExternalWrapperHeaderFieldCursorCodec.TryGetAddress(
			ref p, cursor, out var address));
		Assert.Equal(Instance.Raw, address.Raw);
		cursor.Field = MuiExternalWrapperHeaderField.Class;
		Assert.True(MuiExternalWrapperHeaderFieldCursorCodec.TryGetAddress(
			ref p, cursor, out address));
		Assert.Equal(Instance.Raw + 4, address.Raw);
		cursor.Field = MuiExternalWrapperHeaderField.Flags;
		Assert.True(MuiExternalWrapperHeaderFieldCursorCodec.TryGetAddress(
			ref p, cursor, out address));
		Assert.Equal(Instance.Raw + 8, address.Raw);
		cursor.Header = APTR.FromPointer(0xfffffff0);
		Assert.False(MuiExternalWrapperHeaderFieldCursorCodec.TryGetAddress(
			ref p, cursor, out _));
	}

	[Fact]
	public void ExternalStateCursorUsesNamedRegionBoundary()
	{
		var cursor = default(MuiExternalStateCursor);
		cursor.Instance = Instance;
		cursor.Region = MuiExternalStateRegion.Dtpic;
		Assert.True(MuiExternalStateCursorCodec.TryGetAddress(cursor,
			out var address));
		Assert.Equal(APTR.FromPointer(Instance.Raw + 84), address);

		cursor.Region = MuiExternalStateRegion.RastPort;
		Assert.True(MuiExternalStateCursorCodec.TryGetAddress(cursor,
			out address));
		Assert.Equal(APTR.FromPointer(Instance.Raw + 132), address);

		cursor.Instance = APTR.FromPointer(0xfffffff0);
		Assert.False(MuiExternalStateCursorCodec.TryGetAddress(cursor,
			out _));
	}

	[Fact]
	public void BoopsiGeometryCodecUsesNamedFields()
	{
		var p = NewPlatform();
		var instance = APTR.FromPointer(0x2000);
		var expected = default(MuiExternalBoopsiGeometryState);
		expected.MinWidth = 11;
		expected.MinHeight = 12;
		expected.MaxWidth = 640;
		expected.MaxHeight = 480;
		expected.TagWindow = 0x80030001u;
		expected.TagScreen = 0x80030002u;
		expected.TagDrawInfo = 0x80030003u;
		Assert.True(MuiExternalBoopsiGeometryCodec.Write(ref p, instance,
			expected));
		Assert.True(MuiExternalBoopsiGeometryCodec.TryRead(ref p, instance,
			out var actual));
		Assert.Equal(expected.MinWidth, actual.MinWidth);
		Assert.Equal(expected.MinHeight, actual.MinHeight);
		Assert.Equal(expected.MaxWidth, actual.MaxWidth);
		Assert.Equal(expected.MaxHeight, actual.MaxHeight);
		Assert.Equal(expected.TagWindow, actual.TagWindow);
		Assert.Equal(expected.TagScreen, actual.TagScreen);
		Assert.Equal(expected.TagDrawInfo, actual.TagDrawInfo);
		Assert.False(MuiExternalBoopsiGeometryCodec.TryRead(ref p,
			APTR.FromPointer(0x50000), out _));
	}

	[Fact]
	public void BoopsiGeometryFieldCursorUsesNamedBoundary()
	{
		var p = NewPlatform();
		var cursor = default(MuiExternalBoopsiGeometryFieldCursor);
		cursor.Instance = Instance;
		cursor.Field = MuiExternalBoopsiGeometryField.MinWidth;
		Assert.True(MuiExternalBoopsiGeometryFieldCursorCodec.TryGetAddress(
			ref p, cursor, out var address));
		Assert.Equal(Instance.Raw + 32, address.Raw);
		cursor.Field = MuiExternalBoopsiGeometryField.TagDrawInfo;
		Assert.True(MuiExternalBoopsiGeometryFieldCursorCodec.TryGetAddress(
			ref p, cursor, out address));
		Assert.Equal(Instance.Raw + 56, address.Raw);
		cursor.Instance = APTR.FromPointer(0xfffffff0);
		Assert.False(MuiExternalBoopsiGeometryFieldCursorCodec.TryGetAddress(
			ref p, cursor, out _));
	}

	[Fact]
	public void ExternalDisplayStateCodecUsesNamedPointers()
	{
		var p = NewPlatform();
		var expected = default(MuiExternalDisplayState);
		expected.Window = Window;
		expected.Screen = Screen;
		expected.DrawInfo = DrawInfo;
		expected.RastPort = RastPort;
		Assert.True(MuiExternalDisplayStateCodec.Write(ref p, Instance,
			expected));
		Assert.True(MuiExternalDisplayStateCodec.TryRead(ref p, Instance,
			out var actual));
		Assert.Equal(expected.Window.Raw, actual.Window.Raw);
		Assert.Equal(expected.Screen.Raw, actual.Screen.Raw);
		Assert.Equal(expected.DrawInfo.Raw, actual.DrawInfo.Raw);
		Assert.Equal(expected.RastPort.Raw, actual.RastPort.Raw);
		Assert.False(MuiExternalDisplayStateCodec.TryRead(ref p,
			APTR.FromPointer(0x50000), out _));
	}

	[Fact]
	public void ExternalDisplayRecordsUseNamedFields()
	{
		var p = NewPlatform();
		var environment = APTR.FromPointer(0x3100);
		var rastPort = APTR.FromPointer(0x3120);
		var environmentValue = default(MuiExternalDisplayEnvironmentRecord);
		environmentValue.Window = Window;
		environmentValue.Screen = Screen;
		environmentValue.DrawInfo = DrawInfo;
		Assert.True(MuiExternalDisplayEnvironmentCodec.Write(ref p, environment,
			environmentValue));
		var rastPortValue = default(MuiExternalRastPortSlot);
		rastPortValue.RastPort = RastPort;
		Assert.True(MuiExternalRastPortSlotCodec.Write(ref p, rastPort,
			rastPortValue));
		Assert.True(MuiExternalDisplayEnvironmentCodec.TryRead(ref p, environment,
			out var environmentActual));
		Assert.Equal(Window.Raw, environmentActual.Window.Raw);
		Assert.Equal(Screen.Raw, environmentActual.Screen.Raw);
		Assert.Equal(DrawInfo.Raw, environmentActual.DrawInfo.Raw);
		Assert.True(MuiExternalRastPortSlotCodec.TryRead(ref p, rastPort,
			out var rastPortActual));
		Assert.Equal(RastPort.Raw, rastPortActual.RastPort.Raw);
		Assert.False(MuiExternalDisplayEnvironmentCodec.TryRead(ref p,
			APTR.FromPointer(0x50000), out _));
	}

	[Fact]
	public void ExternalDisplayEnvironmentFieldCursorUsesNamedBoundary()
	{
		var p = NewPlatform();
		var environment = APTR.FromPointer(0x3100);
		var cursor = default(MuiExternalDisplayEnvironmentFieldCursor);
		cursor.Environment = environment;
		cursor.Field = MuiExternalDisplayEnvironmentField.Window;
		Assert.True(MuiExternalDisplayEnvironmentFieldCursorCodec.TryGetAddress(
			ref p, cursor, out var address));
		Assert.Equal(environment.Raw, address.Raw);
		cursor.Field = MuiExternalDisplayEnvironmentField.Screen;
		Assert.True(MuiExternalDisplayEnvironmentFieldCursorCodec.TryGetAddress(
			ref p, cursor, out address));
		Assert.Equal(environment.Raw + 4, address.Raw);
		cursor.Field = MuiExternalDisplayEnvironmentField.DrawInfo;
		Assert.True(MuiExternalDisplayEnvironmentFieldCursorCodec.TryGetAddress(
			ref p, cursor, out address));
		Assert.Equal(environment.Raw + 8, address.Raw);
		Assert.True(MuiExternalDisplayEnvironmentFieldCursorCodec.TryWrite(ref p,
			environment, MuiExternalDisplayEnvironmentField.Window, Window));
		Assert.True(MuiExternalDisplayEnvironmentFieldCursorCodec.TryWrite(ref p,
			environment, MuiExternalDisplayEnvironmentField.Screen, Screen));
		Assert.True(MuiExternalDisplayEnvironmentFieldCursorCodec.TryWrite(ref p,
			environment, MuiExternalDisplayEnvironmentField.DrawInfo, DrawInfo));
		Assert.True(MuiExternalDisplayEnvironmentFieldCursorCodec.TryRead(ref p,
			environment, MuiExternalDisplayEnvironmentField.Window, out var window));
		Assert.Equal(Window.Raw, window.Raw);
		Assert.True(MuiExternalDisplayEnvironmentFieldCursorCodec.TryRead(ref p,
			environment, MuiExternalDisplayEnvironmentField.DrawInfo,
			out var drawInfo));
		Assert.Equal(DrawInfo.Raw, drawInfo.Raw);
		cursor.Environment = APTR.FromPointer(0xfffffff8u);
		Assert.False(MuiExternalDisplayEnvironmentFieldCursorCodec.TryGetAddress(
			ref p, cursor, out _));
	}

	[Fact]
	public void ExternalRastPortSlotFieldCursorUsesNamedBoundary()
	{
		var p = NewPlatform();
		var slot = APTR.FromPointer(0x3120);
		var cursor = default(MuiExternalRastPortSlotFieldCursor);
		cursor.Slot = slot;
		cursor.Field = MuiExternalRastPortSlotField.RastPort;
		Assert.True(MuiExternalRastPortSlotFieldCursorCodec.TryGetAddress(ref p,
			cursor, out var address));
		Assert.Equal(slot.Raw, address.Raw);
		Assert.True(MuiExternalRastPortSlotFieldCursorCodec.TryWrite(ref p, slot,
			MuiExternalRastPortSlotField.RastPort, RastPort));
		Assert.True(MuiExternalRastPortSlotFieldCursorCodec.TryRead(ref p, slot,
			MuiExternalRastPortSlotField.RastPort, out var value));
		Assert.Equal(RastPort.Raw, value.Raw);
		cursor.Slot = APTR.FromPointer(0x50000);
		Assert.False(MuiExternalRastPortSlotFieldCursorCodec.TryGetAddress(ref p,
			cursor, out _));
	}

	[Fact]
	public void ExternalRenderInfoCodecUsesNamedFields()
	{
		var p = NewPlatform();
		BuildRenderInfo(ref p);
		Assert.True(MuiExternalRenderInfoCodec.TryRead(ref p, RenderInfo,
			out var value));
		Assert.Equal(Screen.Raw, value.Screen.Raw);
		Assert.Equal(Window.Raw, value.Window.Raw);
		Assert.Equal(DrawInfo.Raw, value.DrawInfo.Raw);
		Assert.Equal(RastPort.Raw, value.RastPort.Raw);
		Assert.False(MuiExternalRenderInfoCodec.TryRead(ref p,
			APTR.FromPointer(0x50000), out _));
	}

	[Fact]
	public void ExternalRenderInfoFieldCursorUsesNamedBoundary()
	{
		var p = NewPlatform();
		var cursor = default(MuiExternalRenderInfoFieldCursor);
		cursor.RenderInfo = RenderInfo;
		cursor.Field = MuiExternalRenderInfoField.Screen;
		Assert.True(MuiExternalRenderInfoFieldCursorCodec.TryGetAddress(ref p,
			cursor, out var address));
		Assert.Equal(RenderInfo.Raw, address.Raw);
		cursor.Field = MuiExternalRenderInfoField.Window;
		Assert.True(MuiExternalRenderInfoFieldCursorCodec.TryGetAddress(ref p,
			cursor, out address));
		Assert.Equal(RenderInfo.Raw + 4, address.Raw);
		cursor.Field = MuiExternalRenderInfoField.DrawInfo;
		Assert.True(MuiExternalRenderInfoFieldCursorCodec.TryGetAddress(ref p,
			cursor, out address));
		Assert.Equal(RenderInfo.Raw + 8, address.Raw);
		cursor.Field = MuiExternalRenderInfoField.RastPort;
		Assert.True(MuiExternalRenderInfoFieldCursorCodec.TryGetAddress(ref p,
			cursor, out address));
		Assert.Equal(RenderInfo.Raw + 12, address.Raw);
		BuildRenderInfo(ref p);
		Assert.True(MuiExternalRenderInfoFieldCursorCodec.TryRead(ref p,
			RenderInfo, MuiExternalRenderInfoField.Screen, out var screen));
		Assert.Equal(Screen.Raw, screen.Raw);
		Assert.True(MuiExternalRenderInfoFieldCursorCodec.TryRead(ref p,
			RenderInfo, MuiExternalRenderInfoField.RastPort, out var rastPort));
		Assert.Equal(RastPort.Raw, rastPort.Raw);
		cursor.RenderInfo = APTR.FromPointer(0xfffffffcu);
		Assert.False(MuiExternalRenderInfoFieldCursorCodec.TryGetAddress(ref p,
			cursor, out _));
	}

	[Fact]
	public void BoopsiResourceCodecUsesNamedPointers()
	{
		var p = NewPlatform();
		var expected = default(MuiExternalBoopsiResourceState);
		expected.PrivateClass = PrivateClass;
		expected.ClassId = ClassId;
		expected.OpenedClass = APTR.FromPointer(0x2A00);
		expected.BoopsiObject = APTR.FromPointer(0x2A20);
		expected.CreationTags = CreationTags;
		Assert.True(MuiExternalBoopsiResourceCodec.Write(ref p, Instance,
			expected));
		Assert.True(MuiExternalBoopsiResourceCodec.TryRead(ref p, Instance,
			out var actual));
		Assert.Equal(expected.PrivateClass.Raw, actual.PrivateClass.Raw);
		Assert.Equal(expected.ClassId.Raw, actual.ClassId.Raw);
		Assert.Equal(expected.OpenedClass.Raw, actual.OpenedClass.Raw);
		Assert.Equal(expected.BoopsiObject.Raw, actual.BoopsiObject.Raw);
		Assert.Equal(expected.CreationTags.Raw, actual.CreationTags.Raw);
		Assert.False(MuiExternalBoopsiResourceCodec.TryRead(ref p,
			APTR.FromPointer(0x50000), out _));
	}

	[Fact]
	public void BoopsiResourceFieldCursorUsesNamedBoundary()
	{
		var p = NewPlatform();
		var cursor = default(MuiExternalBoopsiResourceFieldCursor);
		cursor.Instance = Instance;
		cursor.Field = MuiExternalBoopsiResourceField.PrivateClass;
		Assert.True(MuiExternalBoopsiResourceFieldCursorCodec.TryGetAddress(
			ref p, cursor, out var address));
		Assert.Equal(Instance.Raw + 12, address.Raw);
		cursor.Field = MuiExternalBoopsiResourceField.CreationTags;
		Assert.True(MuiExternalBoopsiResourceFieldCursorCodec.TryGetAddress(
			ref p, cursor, out address));
		Assert.Equal(Instance.Raw + 28, address.Raw);
		cursor.Instance = APTR.FromPointer(0xfffffff0);
		Assert.False(MuiExternalBoopsiResourceFieldCursorCodec.TryGetAddress(
			ref p, cursor, out _));
	}

	[Fact]
	public void ExternalScratchStateCodecUsesNamedFields()
	{
		var p = NewPlatform();
		var expected = default(MuiExternalScratchState);
		expected.RememberBuffer = APTR.FromPointer(0x2B00);
		expected.RememberCount = 3;
		expected.WorkBuffer = APTR.FromPointer(0x2B40);
		Assert.True(MuiExternalScratchStateCodec.Write(ref p, Instance,
			expected));
		Assert.True(MuiExternalScratchStateCodec.TryRead(ref p, Instance,
			out var actual));
		Assert.Equal(expected.RememberBuffer.Raw, actual.RememberBuffer.Raw);
		Assert.Equal(expected.RememberCount, actual.RememberCount);
		Assert.Equal(expected.WorkBuffer.Raw, actual.WorkBuffer.Raw);
		Assert.False(MuiExternalScratchStateCodec.TryRead(ref p,
			APTR.FromPointer(0x50000), out _));
	}

	[Fact]
	public void ScratchFieldCursorUsesNamedBoundary()
	{
		var p = NewPlatform();
		var cursor = default(MuiExternalScratchFieldCursor);
		cursor.Instance = Instance;
		cursor.Field = MuiExternalScratchField.RememberBuffer;
		Assert.True(MuiExternalScratchFieldCursorCodec.TryGetAddress(ref p,
			cursor, out var address));
		Assert.Equal(Instance.Raw + 72, address.Raw);
		cursor.Field = MuiExternalScratchField.WorkBuffer;
		Assert.True(MuiExternalScratchFieldCursorCodec.TryGetAddress(ref p,
			cursor, out address));
		Assert.Equal(Instance.Raw + 80, address.Raw);
		cursor.Instance = APTR.FromPointer(0xfffffff0);
		Assert.False(MuiExternalScratchFieldCursorCodec.TryGetAddress(ref p,
			cursor, out _));
	}

	[Fact]
	public void DtpicStateCodecUsesNamedFields()
	{
		var p = NewPlatform();
		var expected = default(MuiExternalDtpicState);
		expected.CallerName = NameA;
		expected.OwnedName = APTR.FromPointer(0x2C00);
		expected.OwnedNameSize = 17;
		expected.PictureObject = APTR.FromPointer(0x2C40);
		expected.Alpha = 255;
		expected.MinWidth = 32;
		expected.MinHeight = 24;
		expected.PicWidth = 640;
		expected.PicHeight = 480;
		Assert.True(MuiExternalDtpicStateCodec.Write(ref p, Instance,
			expected));
		Assert.True(MuiExternalDtpicStateCodec.TryRead(ref p, Instance,
			out var actual));
		Assert.Equal(expected.CallerName.Raw, actual.CallerName.Raw);
		Assert.Equal(expected.OwnedName.Raw, actual.OwnedName.Raw);
		Assert.Equal(expected.OwnedNameSize, actual.OwnedNameSize);
		Assert.Equal(expected.PictureObject.Raw, actual.PictureObject.Raw);
		Assert.Equal(expected.Alpha, actual.Alpha);
		Assert.Equal(expected.MinWidth, actual.MinWidth);
		Assert.Equal(expected.MinHeight, actual.MinHeight);
		Assert.Equal(expected.PicWidth, actual.PicWidth);
		Assert.Equal(expected.PicHeight, actual.PicHeight);
		Assert.False(MuiExternalDtpicStateCodec.TryRead(ref p,
			APTR.FromPointer(0x50000), out _));
	}

	[Fact]
	public void DtpicFieldCursorUsesNamedBoundary()
	{
		var p = NewPlatform();
		var cursor = default(MuiExternalDtpicFieldCursor);
		cursor.Instance = Instance;
		cursor.Field = MuiExternalDtpicField.Alpha;
		Assert.True(MuiExternalDtpicFieldCursorCodec.TryGetAddress(ref p,
			cursor, out var address));
		Assert.Equal(Instance.Raw + 100, address.Raw);
		cursor.Field = MuiExternalDtpicField.PicHeight;
		Assert.True(MuiExternalDtpicFieldCursorCodec.TryGetAddress(ref p,
			cursor, out address));
		Assert.Equal(Instance.Raw + 116, address.Raw);
		cursor.Instance = APTR.FromPointer(0xfffffff0);
		Assert.False(MuiExternalDtpicFieldCursorCodec.TryGetAddress(ref p,
			cursor, out _));
	}

	[Fact]
	public void DtpicLayoutResultCodecUsesNamedFields()
	{
		var p = NewPlatform();
		p.WriteUInt32(Packet, 0, 640);
		p.WriteUInt32(Packet, 4, 480);
		Assert.True(MuiExternalDtpicLayoutResultCodec.TryRead(ref p, Packet,
			out var value));
		Assert.Equal(640u, value.Width);
		Assert.Equal(480u, value.Height);
		Assert.False(MuiExternalDtpicLayoutResultCodec.TryRead(ref p,
			APTR.FromPointer(0x50000), out _));
	}

	[Fact]
	public void DtpicLayoutFieldCursorUsesNamedBoundary()
	{
		var p = NewPlatform();
		var cursor = default(MuiExternalDtpicLayoutFieldCursor);
		cursor.Result = Packet;
		cursor.Field = MuiExternalDtpicLayoutField.Width;
		Assert.True(MuiExternalDtpicLayoutFieldCursorCodec.TryGetAddress(ref p,
			cursor, out var address));
		Assert.Equal(Packet.Raw, address.Raw);
		cursor.Field = MuiExternalDtpicLayoutField.Height;
		Assert.True(MuiExternalDtpicLayoutFieldCursorCodec.TryGetAddress(ref p,
			cursor, out address));
		Assert.Equal(Packet.Raw + 4, address.Raw);
		p.WriteUInt32(Packet, 0, 640);
		p.WriteUInt32(Packet, 4, 480);
		Assert.True(MuiExternalDtpicLayoutFieldCursorCodec.TryRead(ref p, Packet,
			MuiExternalDtpicLayoutField.Width, out var width));
		Assert.Equal(640u, width);
		Assert.True(MuiExternalDtpicLayoutFieldCursorCodec.TryRead(ref p, Packet,
			MuiExternalDtpicLayoutField.Height, out var height));
		Assert.Equal(480u, height);
		cursor.Result = APTR.FromPointer(0xfffffffcu);
		Assert.False(MuiExternalDtpicLayoutFieldCursorCodec.TryGetAddress(ref p,
			cursor, out _));
	}

	[Fact]
	public void ExternalNotificationStateCodecUsesNamedFields()
	{
		var p = NewPlatform();
		var expected = default(MuiExternalNotificationState);
		expected.Attribute = 0x8042BFA3u;
		expected.Value = 37;
		expected.Count = 9;
		Assert.True(MuiExternalNotificationStateCodec.Write(ref p, Instance,
			expected));
		Assert.True(MuiExternalNotificationStateCodec.TryRead(ref p, Instance,
			out var actual));
		Assert.Equal(expected.Attribute, actual.Attribute);
		Assert.Equal(expected.Value, actual.Value);
		Assert.Equal(expected.Count, actual.Count);
		Assert.False(MuiExternalNotificationStateCodec.TryRead(ref p,
			APTR.FromPointer(0x50000), out _));
	}

	[Fact]
	public void NotificationFieldCursorUsesNamedBoundary()
	{
		var p = NewPlatform();
		var cursor = default(MuiExternalNotificationFieldCursor);
		cursor.Instance = Instance;
		cursor.Field = MuiExternalNotificationField.Attribute;
		Assert.True(MuiExternalNotificationFieldCursorCodec.TryGetAddress(
			ref p, cursor, out var address));
		Assert.Equal(Instance.Raw + 120, address.Raw);
		cursor.Field = MuiExternalNotificationField.Count;
		Assert.True(MuiExternalNotificationFieldCursorCodec.TryGetAddress(
			ref p, cursor, out address));
		Assert.Equal(Instance.Raw + 128, address.Raw);
		cursor.Instance = APTR.FromPointer(0xfffffff0);
		Assert.False(MuiExternalNotificationFieldCursorCodec.TryGetAddress(
			ref p, cursor, out _));
	}

	[Fact]
	public void BoopsiWorkPacketsUseNamedRecordsAndRejectUnmappedScratch()
	{
		var p = NewPlatform();
		var work = APTR.FromPointer(0x3000);
		Assert.True(MuiExternalBoopsiPacketCodec.TryGetInlineTagList(ref p,
			work, out var list));

		var set = default(MuiExternalBoopsiOpSetMessage);
		set.MethodId = MuiExternalBoopsiPacketCodec.OmSet;
		set.AttributeList = list;
		Assert.True(MuiExternalBoopsiPacketCodec.WriteOpSet(ref p, work, set));
		var tag = new MuiExternalBoopsiTagItem { Tag = 0x80030001u, Data = 7 };
		Assert.True(MuiExternalBoopsiPacketCodec.WriteTag(ref p, list, tag));
		Assert.Equal(MuiExternalBoopsiPacketCodec.OmSet, p.ReadUInt32(work, 0));
		Assert.Equal(tag.Tag, p.ReadUInt32(list, 0));
		Assert.Equal(tag.Data, p.ReadUInt32(list, 4));

		var storage = APTR.FromPointer(0x3040);
		var get = new MuiExternalBoopsiOpGetMessage
		{
			MethodId = MuiExternalBoopsiPacketCodec.OmGet,
			Attribute = 0x80030005u,
			Storage = storage
		};
		Assert.True(MuiExternalBoopsiPacketCodec.WriteOpGet(ref p, work, get));
		Assert.True(MuiExternalBoopsiPacketCodec.WriteResult(ref p, storage,
			new MuiExternalBoopsiResultWord { Value = 99 }));
		Assert.True(MuiExternalBoopsiPacketCodec.TryReadResult(ref p, storage,
			out var result));
		Assert.Equal(99u, result.Value);

		var render = new MuiExternalBoopsiRenderMessage
		{
			MethodId = MuiExternalBoopsiPacketCodec.GmRender,
			RastPort = RastPort
		};
		Assert.True(MuiExternalBoopsiPacketCodec.WriteRender(ref p, work,
			render));
		Assert.False(MuiExternalBoopsiPacketCodec.TryGetInlineTagList(ref p,
			APTR.FromPointer(Base + (uint)Size - 1), out _));
	}

	[Fact]
	public void BoopsiPacketFieldCursorUsesNamedBoundary()
	{
		var p = NewPlatform();
		var cursor = default(MuiExternalBoopsiPacketFieldCursor);
		cursor.Packet = Packet;
		cursor.Kind = MuiExternalBoopsiPacketKind.OpSet;
		cursor.Field = MuiExternalBoopsiPacketField.AttributeList;
		Assert.True(MuiExternalBoopsiPacketFieldCursorCodec.TryGetAddress(
			ref p, cursor, out var address));
		Assert.Equal(Packet.Raw + 4, address.Raw);
		cursor.Kind = MuiExternalBoopsiPacketKind.Render;
		cursor.Field = MuiExternalBoopsiPacketField.RastPort;
		Assert.True(MuiExternalBoopsiPacketFieldCursorCodec.TryGetAddress(
			ref p, cursor, out address));
		Assert.Equal(Packet.Raw + 8, address.Raw);
		cursor.Kind = MuiExternalBoopsiPacketKind.Tag;
		cursor.Field = MuiExternalBoopsiPacketField.Data;
		Assert.True(MuiExternalBoopsiPacketFieldCursorCodec.TryGetAddress(
			ref p, cursor, out address));
		Assert.Equal(Packet.Raw + 4, address.Raw);
		cursor.Kind = MuiExternalBoopsiPacketKind.Result;
		cursor.Field = MuiExternalBoopsiPacketField.Attribute;
		Assert.False(MuiExternalBoopsiPacketFieldCursorCodec.TryGetAddress(
			ref p, cursor, out _));
		cursor.Packet = APTR.FromPointer(0xfffffff0);
		cursor.Field = MuiExternalBoopsiPacketField.Value;
		Assert.False(MuiExternalBoopsiPacketFieldCursorCodec.TryGetAddress(
			ref p, cursor, out _));
	}

	[Fact]
	public void ExternalWorkRegionCursorUsesNamedInlineBoundary()
	{
		var p = NewPlatform();
		var cursor = default(MuiExternalWorkRegionCursor);
		cursor.Work = APTR.FromPointer(0x3000);
		cursor.Region = MuiExternalWorkRegion.InlineTagList;
		Assert.True(MuiExternalWorkRegionCursorCodec.TryGetAddress(ref p,
			cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x3010), address);

		cursor.Region = MuiExternalWorkRegion.InlineResult;
		Assert.True(MuiExternalWorkRegionCursorCodec.TryGetAddress(ref p,
			cursor, out address));
		Assert.Equal(APTR.FromPointer(0x3010), address);

		cursor.Work = APTR.FromPointer(0xFFFFFFF0);
		Assert.False(MuiExternalWorkRegionCursorCodec.TryGetAddress(ref p,
			cursor, out _));
	}

	[Fact]
	public void ExternalTagListCursorUsesNamedEntryBoundary()
	{
		var p = NewPlatform();
		var cursor = default(MuiExternalTagListCursor);
		cursor.Base = APTR.FromPointer(0x3000);
		cursor.Index = 1;

		Assert.True(MuiExternalTagListCursorCodec.TryGetEntry(ref p, cursor,
			out var address));
		Assert.Equal(APTR.FromPointer(0x3008), address);
		cursor.Base = APTR.FromPointer(0x40FF8);
		cursor.Index = 0;
		Assert.True(MuiExternalTagListCursorCodec.TryGetEntry(ref p, cursor,
			out address));
		Assert.Equal(APTR.FromPointer(0x40FF8), address);
		cursor.Index = 1;
		Assert.False(MuiExternalTagListCursorCodec.TryGetEntry(ref p, cursor,
			out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFF0);
		cursor.Index = 0;
		Assert.False(MuiExternalTagListCursorCodec.TryGetEntry(ref p, cursor,
			out _));
	}

	[Fact]
	public void ExternalWrapperMethodHeaderUsesNamedField()
	{
		var p = NewPlatform();
		Assert.True(MuiExternalWrapperMessageCodec.WriteMethod(ref p, Packet,
			MuiExternalWrapperMessageCodec.Draw));
		Assert.True(MuiExternalWrapperMessageCodec.TryReadMethodId(ref p, Packet,
			out var packet));
		Assert.Equal(MuiExternalWrapperMessageCodec.Draw, packet.MethodId);
		Assert.False(MuiExternalWrapperMessageCodec.TryReadMethodId(ref p,
			APTR.Null, out _));
	}

	[Fact]
	public void ExternalWrapperTypedReadersUseNamedMethodHeader()
	{
		var p = NewPlatform();
		Assert.True(MuiExternalWrapperMessageCodec.WriteSet(ref p, Packet,
			MuiExternalWrapperMessageCodec.MethodSet, 9, 11));
		Assert.True(MuiExternalWrapperMessageCodec.TryReadSet(ref p, Packet,
			MuiExternalWrapperMessageCodec.MethodSet, out var set));
		Assert.Equal(MuiExternalWrapperMessageCodec.MethodSet, set.MethodId);
		Assert.False(MuiExternalWrapperMessageCodec.TryReadSet(ref p, Packet,
			MuiExternalWrapperMessageCodec.MethodNoNotifySet, out _));

		Assert.True(MuiExternalWrapperMessageCodec.WriteMethod(ref p, Packet,
			MuiExternalWrapperMessageCodec.Draw));
		Assert.True(MuiExternalWrapperMessageCodec.TryReadMethod(ref p, Packet,
			MuiExternalWrapperMessageCodec.Draw, out var method));
		Assert.Equal(MuiExternalWrapperMessageCodec.Draw, method.MethodId);
		Assert.False(MuiExternalWrapperMessageCodec.TryReadMethod(ref p, Packet,
			MuiExternalWrapperMessageCodec.Show, out _));
	}

	// ---- Classification ------------------------------------------------------

	[Fact]
	public void ClassifiesOfficialClassIds()
	{
		var p = NewPlatform();
		p.WriteCString(ClassId, "Boopsi.mui");
		Assert.Equal(MuiExternalWrapperClass.Boopsi,
			MuiExternalWrapperCore.ClassifyName(ref p, ClassId));
		p.WriteCString(ClassId, "Dtpic.mui");
		Assert.Equal(MuiExternalWrapperClass.Dtpic,
			MuiExternalWrapperCore.ClassifyName(ref p, ClassId));
	}

	[Theory]
	[InlineData("boopsi.mui")]   // wrong case
	[InlineData("Boopsi.mu")]    // truncated suffix
	[InlineData("Boopsi.muix")]  // trailing garbage
	[InlineData("Boops.mui")]    // wrong name
	[InlineData("Dtpic.gui")]    // wrong suffix
	[InlineData("")]             // empty
	[InlineData("Area.mui")]     // different class
	public void RejectsMalformedClassIds(string name)
	{
		var p = NewPlatform();
		p.WriteCString(ClassId, name);
		Assert.Equal(MuiExternalWrapperClass.None,
			MuiExternalWrapperCore.ClassifyName(ref p, ClassId));
	}

	[Fact]
	public void RejectsNullClassId()
	{
		var p = NewPlatform();
		Assert.Equal(MuiExternalWrapperClass.None,
			MuiExternalWrapperCore.ClassifyName(ref p, APTR.Null));
	}

	// ---- Creation & defaults -------------------------------------------------

	[Fact]
	public void BoopsiCreatesWithDocumentedDefaults()
	{
		var p = NewPlatform();
		Assert.Equal(MuiExternalWrapperClass.Boopsi, CreateBoopsi(ref p));
		Assert.True(MuiExternalWrapperCore.Valid(ref p, Instance));
		Assert.Equal(MuiExternalWrapperClass.Boopsi,
			MuiExternalWrapperCore.Classify(ref p, Instance));
		Get(ref p, MuiExternalWrapperAttributes.Boopsi_MinWidth, out var minW);
		Get(ref p, MuiExternalWrapperAttributes.Boopsi_MinHeight, out var minH);
		Get(ref p, MuiExternalWrapperAttributes.Boopsi_MaxWidth, out var maxW);
		Get(ref p, MuiExternalWrapperAttributes.Boopsi_MaxHeight, out var maxH);
		Assert.Equal(1u, minW);
		Assert.Equal(1u, minH);
		Assert.Equal(10000u, maxW);
		Assert.Equal(10000u, maxH);
	}

	[Fact]
	public void DtpicCreatesOpaqueByDefault()
	{
		var p = NewPlatform();
		Assert.Equal(MuiExternalWrapperClass.Dtpic, CreateDtpic(ref p));
		Get(ref p, MuiExternalWrapperAttributes.Dtpic_Alpha, out var alpha);
		Assert.Equal(255u, alpha);
	}

	[Fact]
	public void CreateFailsWhenWorkAllocationFails()
	{
		var p = ExhaustedPlatform(16);   // no room for the 64-byte work block
		p.WriteCString(ClassId, "Boopsi.mui");
		Assert.Equal(MuiExternalWrapperClass.None,
			MuiExternalWrapperCore.CreateByName(ref p, Instance, ClassId));
		Assert.False(MuiExternalWrapperCore.Valid(ref p, Instance));
	}

	[Fact]
	public void BoopsiCreateRollsBackWorkWhenRememberAllocationFails()
	{
		// Enough headroom for the 64-byte work block but not the 40-byte
		// remember block: the work block must be freed and creation must fail.
		var p = ExhaustedPlatform(80);
		p.WriteCString(ClassId, "Boopsi.mui");
		Assert.Equal(MuiExternalWrapperClass.None,
			MuiExternalWrapperCore.CreateByName(ref p, Instance, ClassId));
		Assert.False(MuiExternalWrapperCore.Valid(ref p, Instance));
		Assert.Equal(1u, p.FreeCount);   // the work block was rolled back
	}

	// ---- Boopsi attributes ---------------------------------------------------

	[Fact]
	public void BoopsiAttributesRoundTrip()
	{
		var p = NewPlatform();
		CreateBoopsi(ref p);
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_MinWidth, 30);
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_MinHeight, 40);
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_MaxWidth, 300);
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_MaxHeight, 400);
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_TagWindow, 0x1111);
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_TagScreen, 0x2222);
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_TagDrawInfo, 0x3333);
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_Class, PrivateClass.Raw);
		Get(ref p, MuiExternalWrapperAttributes.Boopsi_MinWidth, out var minW);
		Get(ref p, MuiExternalWrapperAttributes.Boopsi_MaxHeight, out var maxH);
		Get(ref p, MuiExternalWrapperAttributes.Boopsi_TagWindow, out var tw);
		Get(ref p, MuiExternalWrapperAttributes.Boopsi_TagScreen, out var ts);
		Get(ref p, MuiExternalWrapperAttributes.Boopsi_TagDrawInfo, out var td);
		Get(ref p, MuiExternalWrapperAttributes.Boopsi_Class, out var cl);
		Assert.Equal(30u, minW);
		Assert.Equal(400u, maxH);
		Assert.Equal(0x1111u, tw);
		Assert.Equal(0x2222u, ts);
		Assert.Equal(0x3333u, td);
		Assert.Equal(PrivateClass.Raw, cl);
	}

	[Fact]
	public void BoopsiObjectGetIsNullUntilSetupAndValidAfter()
	{
		var p = NewPlatform();
		CreateBoopsi(ref p);
		p.WriteCString(ClassId, "gadget.class");
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_ClassID, ClassId.Raw);
		// Before setup MUIA_Boopsi_Object reads back Null.
		Get(ref p, MuiExternalWrapperAttributes.Boopsi_Object, out var before);
		Assert.Equal(0u, before);
		BuildRenderInfo(ref p);
		WriteTagDone(ref p, CreationTags);
		MuiExternalWrapperCore.SetCreationTags(ref p, Instance, CreationTags);
		Assert.True(MuiExternalWrapperCore.Setup(ref p, Instance, RenderInfo));
		Get(ref p, MuiExternalWrapperAttributes.Boopsi_Object, out var after);
		Assert.NotEqual(0u, after);
		Assert.True(MuiExternalWrapperCore.IsObjectCreated(ref p, Instance));
	}

	// ---- Boopsi setup: open + create, failures, exactly-once close ----------

	[Fact]
	public void BoopsiSetupOpensClassAndCreatesObject()
	{
		var p = NewPlatform();
		CreateBoopsi(ref p);
		p.WriteCString(ClassId, "gadget.class");
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_ClassID, ClassId.Raw);
		WriteTagDone(ref p, CreationTags);
		MuiExternalWrapperCore.SetCreationTags(ref p, Instance, CreationTags);
		BuildRenderInfo(ref p);
		Assert.True(MuiExternalWrapperCore.Setup(ref p, Instance, RenderInfo));
		Assert.Equal(1u, p.OpenExternalClassCount);
		Assert.True(MuiExternalWrapperCore.IsObjectCreated(ref p, Instance));
	}

	[Fact]
	public void BoopsiSetupWithPrivateClassSkipsLoader()
	{
		var p = NewPlatform();
		CreateBoopsi(ref p);
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_Class, PrivateClass.Raw);
		WriteTagDone(ref p, CreationTags);
		MuiExternalWrapperCore.SetCreationTags(ref p, Instance, CreationTags);
		BuildRenderInfo(ref p);
		Assert.True(MuiExternalWrapperCore.Setup(ref p, Instance, RenderInfo));
		Assert.Equal(0u, p.OpenExternalClassCount);   // used the private class
		Assert.True(MuiExternalWrapperCore.IsObjectCreated(ref p, Instance));
		// Cleanup disposes the object but never closes a class it did not open.
		Assert.True(MuiExternalWrapperCore.Cleanup(ref p, Instance));
		Assert.Equal(0u, p.CloseExternalClassCount);
	}

	[Fact]
	public void BoopsiSetupFailsWhenClassOpenFails()
	{
		var p = NewPlatform();
		p.ExternalClassOpenFailure = true;
		CreateBoopsi(ref p);
		p.WriteCString(ClassId, "gadget.class");
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_ClassID, ClassId.Raw);
		WriteTagDone(ref p, CreationTags);
		MuiExternalWrapperCore.SetCreationTags(ref p, Instance, CreationTags);
		BuildRenderInfo(ref p);
		Assert.False(MuiExternalWrapperCore.Setup(ref p, Instance, RenderInfo));
		Assert.False(MuiExternalWrapperCore.IsObjectCreated(ref p, Instance));
		Assert.Equal(0u, p.CloseExternalClassCount);   // nothing was opened
	}

	[Fact]
	public void BoopsiSetupClosesClassWhenObjectCreationFails()
	{
		var p = NewPlatform();
		p.NewObjectFailure = true;
		CreateBoopsi(ref p);
		p.WriteCString(ClassId, "gadget.class");
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_ClassID, ClassId.Raw);
		WriteTagDone(ref p, CreationTags);
		MuiExternalWrapperCore.SetCreationTags(ref p, Instance, CreationTags);
		BuildRenderInfo(ref p);
		Assert.False(MuiExternalWrapperCore.Setup(ref p, Instance, RenderInfo));
		Assert.False(MuiExternalWrapperCore.IsObjectCreated(ref p, Instance));
		// The failure-atomic path opened the class then closed it exactly once.
		Assert.Equal(1u, p.OpenExternalClassCount);
		Assert.Equal(1u, p.CloseExternalClassCount);
	}

	// ---- Boopsi tag filling --------------------------------------------------

	[Fact]
	public void BoopsiFillsWindowScreenDrawInfoTagsAtCreation()
	{
		var p = NewPlatform();
		CreateBoopsi(ref p);
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_Class, PrivateClass.Raw);
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_TagWindow, 0xA001);
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_TagScreen, 0xA002);
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_TagDrawInfo, 0xA003);
		// Caller creation tag list: three placeholders (data 0) + TAG_DONE.
		p.WriteUInt32(CreationTags, 0, 0xA001); p.WriteUInt32(CreationTags, 4, 0);
		p.WriteUInt32(CreationTags, 8, 0xA002); p.WriteUInt32(CreationTags, 12, 0);
		p.WriteUInt32(CreationTags, 16, 0xA003); p.WriteUInt32(CreationTags, 20, 0);
		p.WriteUInt32(CreationTags, 24, 0);   // TAG_DONE
		MuiExternalWrapperCore.SetCreationTags(ref p, Instance, CreationTags);
		BuildRenderInfo(ref p);
		Assert.True(MuiExternalWrapperCore.Setup(ref p, Instance, RenderInfo));
		Assert.Equal(Window.Raw, p.ReadUInt32(CreationTags, 4));
		Assert.Equal(Screen.Raw, p.ReadUInt32(CreationTags, 12));
		Assert.Equal(DrawInfo.Raw, p.ReadUInt32(CreationTags, 20));
	}

	[Fact]
	public void BoopsiToleratesMalformedCreationTags()
	{
		var p = NewPlatform();
		CreateBoopsi(ref p);
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_Class, PrivateClass.Raw);
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_TagWindow, 0xB001);
		// Point the creation tags at the very end of mapped memory so the walk
		// must stop on the bound / mapping check rather than crash.
		var edge = APTR.FromPointer(Base + (uint)Size - 4);
		MuiExternalWrapperCore.SetCreationTags(ref p, Instance, edge);
		BuildRenderInfo(ref p);
		Assert.True(MuiExternalWrapperCore.Setup(ref p, Instance, RenderInfo));
	}

	// ---- Boopsi geometry + colorwheel -1 workaround --------------------------

	[Fact]
	public void BoopsiAppliesGeometryToObject()
	{
		var p = NewPlatform();
		SetupPrivateBoopsi(ref p);
		Assert.True(MuiExternalWrapperCore.ApplyGeometry(ref p, Instance, 5, 7,
			100, 50));
		var work = APTR.FromPointer(p.ReadUInt32(Instance, WorkBufferOffset));
		Assert.Equal(OmSet, p.ReadUInt32(work, 0));
		// Official interleaved gadgetclass tag IDs (GA_Rel* variants sit between
		// the absolute geometry tags): GA_Left=+1, GA_Top=+3, GA_Width=+5,
		// GA_Height=+7. The tag list starts at work+16.
		Assert.Equal(0x80030001u, p.ReadUInt32(work, 16));  // GA_Left tag
		Assert.Equal(5u, p.ReadUInt32(work, 20));           // GA_Left value
		Assert.Equal(0x80030003u, p.ReadUInt32(work, 24));  // GA_Top tag
		Assert.Equal(7u, p.ReadUInt32(work, 28));           // GA_Top value
		Assert.Equal(0x80030005u, p.ReadUInt32(work, 32));  // GA_Width tag
		Assert.Equal(100u, p.ReadUInt32(work, 36));         // GA_Width value
		Assert.Equal(0x80030007u, p.ReadUInt32(work, 40));  // GA_Height tag
		Assert.Equal(50u, p.ReadUInt32(work, 44));          // GA_Height value
	}

	[Fact]
	public void ColorwheelGeometrySubtractsOnePixel()
	{
		var p = NewPlatform();
		CreateBoopsi(ref p);
		p.WriteCString(ClassId, "colorwheel.gadget");
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_ClassID, ClassId.Raw);
		WriteTagDone(ref p, CreationTags);
		MuiExternalWrapperCore.SetCreationTags(ref p, Instance, CreationTags);
		BuildRenderInfo(ref p);
		Assert.True(MuiExternalWrapperCore.Setup(ref p, Instance, RenderInfo));
		Assert.True(MuiExternalWrapperCore.ApplyGeometry(ref p, Instance, 0, 0,
			100, 50));
		var work = APTR.FromPointer(p.ReadUInt32(Instance, WorkBufferOffset));
		Assert.Equal(99u, p.ReadUInt32(work, 36));   // width - 1
		Assert.Equal(49u, p.ReadUInt32(work, 44));   // height - 1
	}

	// ---- Boopsi min/max ------------------------------------------------------

	[Fact]
	public void BoopsiAskMinMaxPublishesClampedValues()
	{
		var p = NewPlatform();
		CreateBoopsi(ref p);
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_MinWidth, 30);
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_MinHeight, 20);
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_MaxWidth, 200);
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_MaxHeight, 100);
		Assert.True(MuiExternalWrapperCore.AskMinMax(ref p, Instance, MinMax));
		Assert.Equal(30, p.ReadUInt16(MinMax, 0));
		Assert.Equal(20, p.ReadUInt16(MinMax, 2));
		Assert.Equal(200, p.ReadUInt16(MinMax, 4));
		Assert.Equal(100, p.ReadUInt16(MinMax, 6));
	}

	// ---- Boopsi draw ---------------------------------------------------------

	[Fact]
	public void BoopsiDrawRendersOnlyWhenShown()
	{
		var p = NewPlatform();
		SetupPrivateBoopsi(ref p);
		Assert.False(MuiExternalWrapperCore.Draw(ref p, Instance)); // not shown
		Assert.True(MuiExternalWrapperCore.Show(ref p, Instance));
		Assert.True(MuiExternalWrapperCore.Draw(ref p, Instance));
		Assert.Equal(0x00000001u, p.LastDispatchMethod);   // GM_RENDER forwarded
	}

	// ---- Boopsi remember / regenerate ---------------------------------------

	[Fact]
	public void BoopsiRegenerateDisposesAndRecreatesRememberingTags()
	{
		var p = NewPlatform();
		CreateBoopsi(ref p);
		p.WriteCString(ClassId, "gadget.class");
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_ClassID, ClassId.Raw);
		// Two remembered tag ids (init only).
		MuiExternalWrapperCore.SetAttribute(ref p, Instance,
			MuiExternalWrapperAttributes.Boopsi_Remember, 0xC001, true, false,
			out _, out _);
		MuiExternalWrapperCore.SetAttribute(ref p, Instance,
			MuiExternalWrapperAttributes.Boopsi_Remember, 0xC002, true, false,
			out _, out _);
		WriteTagDone(ref p, CreationTags);
		MuiExternalWrapperCore.SetCreationTags(ref p, Instance, CreationTags);
		BuildRenderInfo(ref p);
		Assert.True(MuiExternalWrapperCore.Setup(ref p, Instance, RenderInfo));
		Assert.Equal(1u, p.OpenExternalClassCount);

		Assert.True(MuiExternalWrapperCore.Regenerate(ref p, Instance));
		// The class stayed open across the regenerate (never re-opened/closed).
		Assert.Equal(1u, p.OpenExternalClassCount);
		Assert.Equal(0u, p.CloseExternalClassCount);
		Assert.True(MuiExternalWrapperCore.IsObjectCreated(ref p, Instance));
	}

	[Fact]
	public void BoopsiSmartObjectIsNotRegenerated()
	{
		var p = NewPlatform();
		CreateBoopsi(ref p);
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_Class, PrivateClass.Raw);
		MuiExternalWrapperCore.SetAttribute(ref p, Instance,
			MuiExternalWrapperAttributes.Boopsi_Smart, 1, true, false, out _, out _);
		WriteTagDone(ref p, CreationTags);
		MuiExternalWrapperCore.SetCreationTags(ref p, Instance, CreationTags);
		BuildRenderInfo(ref p);
		Assert.True(MuiExternalWrapperCore.Setup(ref p, Instance, RenderInfo));
		var before = p.DispatchCount;
		Assert.True(MuiExternalWrapperCore.Regenerate(ref p, Instance));
		// A smart gadget is left untouched: no dispose/recreate work happened.
		Assert.Equal(before, p.DispatchCount);
		Assert.True(MuiExternalWrapperCore.IsObjectCreated(ref p, Instance));
	}

	[Fact]
	public void BoopsiRemembersAtMostFiveTags()
	{
		var p = NewPlatform();
		CreateBoopsi(ref p);
		for (var i = 0u; i < 5; i++)
			Assert.True(SetInit(ref p, MuiExternalWrapperAttributes.Boopsi_Remember,
				0xD000 + i));
		Assert.False(SetInit(ref p, MuiExternalWrapperAttributes.Boopsi_Remember,
			0xD005));   // sixth ignored
	}

	[Fact]
	public void ExternalWrapperRememberBufferUsesNamedTagItemCodec()
	{
		var p = NewPlatform();
		CreateBoopsi(ref p);
		Assert.True(SetInit(ref p, MuiExternalWrapperAttributes.Boopsi_Remember,
			0xD101));
		Assert.True(MuiExternalScratchStateCodec.TryRead(ref p, Instance,
			out var scratch));
		var buffer = scratch.RememberBuffer;
		Assert.True(MuiAslTagItemCodec.TryRead(ref p, buffer, out var item));
		Assert.Equal(0xD101u, item.Tag);
		Assert.Equal(0u, item.Data);
	}

	[Fact]
	public void ExternalRememberCursorUsesNamedEntryBoundary()
	{
		var p = NewPlatform();
		var cursor = default(MuiExternalRememberCursor);
		cursor.Base = APTR.FromPointer(0x2B00);
		cursor.Index = 4;

		Assert.True(MuiExternalRememberCursorCodec.TryGetEntry(ref p, cursor,
			out var address));
		Assert.Equal(APTR.FromPointer(0x2B20), address);
		cursor.Index = 5;
		Assert.False(MuiExternalRememberCursorCodec.TryGetEntry(ref p, cursor,
			out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFF0);
		cursor.Index = 0;
		Assert.False(MuiExternalRememberCursorCodec.TryGetEntry(ref p, cursor,
			out _));
	}

	[Fact]
	public void ExternalBoopsiTagCursorUsesNamedEntryBoundary()
	{
		var p = NewPlatform();
		var cursor = default(MuiExternalBoopsiTagCursor);
		cursor.Base = APTR.FromPointer(0x3010);
		cursor.Index = 4;

		Assert.True(MuiExternalBoopsiTagCursorCodec.TryGetEntry(ref p, cursor,
			out var address));
		Assert.Equal(APTR.FromPointer(0x3030), address);
		cursor.Index = 5;
		Assert.False(MuiExternalBoopsiTagCursorCodec.TryGetEntry(ref p, cursor,
			out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFF0);
		cursor.Index = 0;
		Assert.False(MuiExternalBoopsiTagCursorCodec.TryGetEntry(ref p, cursor,
			out _));
	}

	// ---- IDCMP_UPDATE -> notification ---------------------------------------

	[Fact]
	public void OmUpdateMapsToMuiNotification()
	{
		var p = NewPlatform();
		SetupPrivateBoopsi(ref p);
		// opUpdate attr list: two changed pairs + TAG_DONE.
		p.WriteUInt32(AttrList, 0, 0x80421234); p.WriteUInt32(AttrList, 4, 77);
		p.WriteUInt32(AttrList, 8, 0x80425678); p.WriteUInt32(AttrList, 12, 88);
		p.WriteUInt32(AttrList, 16, 0);
		p.WriteUInt32(Packet, 0, OmUpdate);
		p.WriteUInt32(Packet, 4, AttrList.Raw);
		p.WriteUInt32(Packet, 8, 0);
		p.WriteUInt32(Packet, 12, 0);
		Assert.Equal(2u, MuiExternalWrapperDispatcher.Dispatch(ref p, Instance,
			Packet));
		Assert.Equal(2u, MuiExternalWrapperCore.NotificationCount(ref p, Instance));
		Assert.Equal(0x80425678u,
			MuiExternalWrapperCore.LastNotifiedAttribute(ref p, Instance));
		Assert.Equal(88u,
			MuiExternalWrapperCore.LastNotifiedValue(ref p, Instance));
	}

	// ---- Transparent attribute pass-through ---------------------------------

	[Fact]
	public void UnknownBoopsiSetIsPassedThroughToObject()
	{
		var p = NewPlatform();
		SetupPrivateBoopsi(ref p);
		var obj = ObjectPointer(ref p);
		p.WriteUInt32(Packet, 0, MuimSet);
		p.WriteUInt32(Packet, 4, 0x80080001);   // unknown attribute
		p.WriteUInt32(Packet, 8, 0x1234);
		Assert.Equal(1u, MuiExternalWrapperDispatcher.Dispatch(ref p, Instance,
			Packet));
		Assert.Equal(obj.Raw, p.LastDispatchObject.Raw);
		Assert.Equal(OmSet, p.LastDispatchMethod);   // forwarded as OM_SET
	}

	[Fact]
	public void UnknownBoopsiGetIsPassedThroughToObject()
	{
		var p = NewPlatform();
		SetupPrivateBoopsi(ref p);
		var obj = ObjectPointer(ref p);
		p.WriteUInt32(Packet, 0, OmGet);
		p.WriteUInt32(Packet, 4, 0x80080002);   // unknown attribute
		p.WriteUInt32(Packet, 8, Storage.Raw);
		Assert.Equal(1u, MuiExternalWrapperDispatcher.Dispatch(ref p, Instance,
			Packet));
		Assert.Equal(obj.Raw, p.LastDispatchObject.Raw);
		Assert.Equal(OmGet, p.LastDispatchMethod);   // forwarded as OM_GET
	}

	// ---- Dispatcher standard methods ----------------------------------------

	[Fact]
	public void DispatcherRoutesLifecycleMethods()
	{
		var p = NewPlatform();
		CreateBoopsi(ref p);
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_Class, PrivateClass.Raw);
		WriteTagDone(ref p, CreationTags);
		MuiExternalWrapperCore.SetCreationTags(ref p, Instance, CreationTags);
		BuildRenderInfo(ref p);

		p.WriteUInt32(Packet, 0, MethodSetup);
		p.WriteUInt32(Packet, 4, RenderInfo.Raw);
		Assert.Equal(1u, MuiExternalWrapperDispatcher.Dispatch(ref p, Instance,
			Packet));
		Assert.True(MuiExternalWrapperCore.IsObjectCreated(ref p, Instance));

		p.WriteUInt32(Packet, 0, MethodShow);
		Assert.Equal(1u, MuiExternalWrapperDispatcher.Dispatch(ref p, Instance,
			Packet));

		p.WriteUInt32(Packet, 0, MethodAskMinMax);
		p.WriteUInt32(Packet, 4, MinMax.Raw);
		Assert.Equal(1u, MuiExternalWrapperDispatcher.Dispatch(ref p, Instance,
			Packet));

		p.WriteUInt32(Packet, 0, MethodLayout);
		p.WriteUInt32(Packet, 4, 0);
		p.WriteUInt32(Packet, 8, 0);
		p.WriteUInt32(Packet, 12, 64);
		p.WriteUInt32(Packet, 16, 32);
		Assert.Equal(1u, MuiExternalWrapperDispatcher.Dispatch(ref p, Instance,
			Packet));

		p.WriteUInt32(Packet, 0, MethodDraw);
		Assert.Equal(1u, MuiExternalWrapperDispatcher.Dispatch(ref p, Instance,
			Packet));

		p.WriteUInt32(Packet, 0, MethodCleanup);
		Assert.Equal(1u, MuiExternalWrapperDispatcher.Dispatch(ref p, Instance,
			Packet));
		Assert.False(MuiExternalWrapperCore.IsObjectCreated(ref p, Instance));
	}

	[Fact]
	public void DispatcherRejectsForeignInstances()
	{
		var p = NewPlatform();
		p.WriteUInt32(Packet, 0, MethodShow);
		Assert.False(MuiExternalWrapperDispatcher.TryDispatch(ref p, Instance,
			Packet, out _));   // instance is not a valid wrapper
	}

	// ---- Dtpic: owned name copy + caller mutation ---------------------------

	[Fact]
	public void DtpicOwnsNameCopyImmuneToCallerMutation()
	{
		var p = NewPlatform();
		CreateDtpic(ref p);
		p.WriteCString(NameA, "picture.png");
		Assert.True(MuiExternalWrapperCore.SetName(ref p, Instance, NameA));
		Get(ref p, MuiExternalWrapperAttributes.Dtpic_Name, out var owned);
		Assert.NotEqual(NameA.Raw, owned);              // it is a copy
		Assert.Equal((byte)'p', p.ReadUInt8(APTR.FromPointer(owned), 0));
		// Mutating (and clobbering) the caller buffer must not affect the copy.
		p.WriteCString(NameA, "ZZZZZZZZZZZ");
		Assert.Equal((byte)'p', p.ReadUInt8(APTR.FromPointer(owned), 0));
		Assert.Equal((byte)'c', p.ReadUInt8(APTR.FromPointer(owned), 2));
	}

	[Fact]
	public void DtpicNameRejectsForeignClass()
	{
		var p = NewPlatform();
		CreateBoopsi(ref p);   // SetName is Dtpic-only
		p.WriteCString(NameA, "x.png");
		Assert.False(MuiExternalWrapperCore.SetName(ref p, Instance, NameA));
	}

	// ---- Dtpic attributes ----------------------------------------------------

	[Fact]
	public void DtpicAttributesRoundTrip()
	{
		var p = NewPlatform();
		CreateDtpic(ref p);
		Set(ref p, MuiExternalWrapperAttributes.Dtpic_Alpha, 128);
		SetInit(ref p, MuiExternalWrapperAttributes.Dtpic_FreeHoriz, 1);
		SetInit(ref p, MuiExternalWrapperAttributes.Dtpic_FreeVert, 1);
		SetInit(ref p, MuiExternalWrapperAttributes.Dtpic_LightenOnMouse, 1);
		SetInit(ref p, MuiExternalWrapperAttributes.Dtpic_DarkenSelState, 1);
		SetInit(ref p, MuiExternalWrapperAttributes.Dtpic_MinWidth, 12);
		SetInit(ref p, MuiExternalWrapperAttributes.Dtpic_MinHeight, 8);
		Get(ref p, MuiExternalWrapperAttributes.Dtpic_Alpha, out var alpha);
		Get(ref p, MuiExternalWrapperAttributes.Dtpic_FreeHoriz, out var fh);
		Get(ref p, MuiExternalWrapperAttributes.Dtpic_FreeVert, out var fv);
		Get(ref p, MuiExternalWrapperAttributes.Dtpic_LightenOnMouse, out var lm);
		Get(ref p, MuiExternalWrapperAttributes.Dtpic_DarkenSelState, out var ds);
		Get(ref p, MuiExternalWrapperAttributes.Dtpic_MinWidth, out var mw);
		Get(ref p, MuiExternalWrapperAttributes.Dtpic_MinHeight, out var mh);
		Assert.Equal(128u, alpha);
		Assert.Equal(1u, fh);
		Assert.Equal(1u, fv);
		Assert.Equal(1u, lm);
		Assert.Equal(1u, ds);
		Assert.Equal(12u, mw);
		Assert.Equal(8u, mh);
	}

	// ---- Dtpic setup / acquire / layout / draw ------------------------------

	[Fact]
	public void DtpicSetupAcquiresAndLaysOutPicture()
	{
		var p = NewPlatform();
		p.PictureWidth = 40;
		p.PictureHeight = 30;
		CreateDtpic(ref p);
		p.WriteCString(NameA, "logo.iff");
		Set(ref p, MuiExternalWrapperAttributes.Dtpic_Name, NameA.Raw);
		BuildRenderInfo(ref p);
		Assert.True(MuiExternalWrapperCore.Setup(ref p, Instance, RenderInfo));
		Assert.True(MuiExternalWrapperCore.IsPictureAcquired(ref p, Instance));
		Assert.Equal(1u, p.AcquirePictureCount);
		Assert.Equal(1u, p.LayoutPictureCount);
		Assert.Equal(Screen.Raw, p.LastAcquiredPictureScreen.Raw);
		Assert.True(MuiExternalWrapperCore.AskMinMax(ref p, Instance, MinMax));
		Assert.Equal(40, p.ReadUInt16(MinMax, 0));   // laid-out width
		Assert.Equal(30, p.ReadUInt16(MinMax, 2));   // laid-out height
	}

	[Fact]
	public void DtpicSetupWithoutNameIsValidButEmpty()
	{
		var p = NewPlatform();
		CreateDtpic(ref p);
		BuildRenderInfo(ref p);
		Assert.True(MuiExternalWrapperCore.Setup(ref p, Instance, RenderInfo));
		Assert.False(MuiExternalWrapperCore.IsPictureAcquired(ref p, Instance));
		Assert.Equal(0u, p.AcquirePictureCount);
	}

	[Fact]
	public void DtpicAcquireFailureLeavesObjectEmptyAtomically()
	{
		var p = NewPlatform();
		p.AcquirePictureFailure = true;
		CreateDtpic(ref p);
		p.WriteCString(NameA, "missing.iff");
		Set(ref p, MuiExternalWrapperAttributes.Dtpic_Name, NameA.Raw);
		BuildRenderInfo(ref p);
		Assert.True(MuiExternalWrapperCore.Setup(ref p, Instance, RenderInfo));
		Assert.False(MuiExternalWrapperCore.IsPictureAcquired(ref p, Instance));
		Assert.Equal(1u, p.AcquirePictureCount);
		Assert.Equal(0u, p.ReleasePictureCount);   // nothing to release
	}

	[Fact]
	public void DtpicDrawBlitsPictureWhenShownAndEnabled()
	{
		var p = NewPlatform();
		p.PictureWidth = 40;
		p.PictureHeight = 30;
		CreateDtpic(ref p);
		p.WriteCString(NameA, "logo.iff");
		Set(ref p, MuiExternalWrapperAttributes.Dtpic_Name, NameA.Raw);
		BuildRenderInfo(ref p);
		MuiExternalWrapperCore.Setup(ref p, Instance, RenderInfo);
		MuiExternalWrapperCore.Show(ref p, Instance);
		Assert.True(MuiExternalWrapperCore.Draw(ref p, Instance));
		Assert.Equal(1u, p.DrawPictureCount);
		Assert.Equal(40, p.LastDrawnPictureWidth);
		Assert.Equal(30, p.LastDrawnPictureHeight);
	}

	[Fact]
	public void DtpicDisabledDrawsNothing()
	{
		var p = NewPlatform();
		p.PictureWidth = 40;
		p.PictureHeight = 30;
		CreateDtpic(ref p);
		p.WriteCString(NameA, "logo.iff");
		Set(ref p, MuiExternalWrapperAttributes.Dtpic_Name, NameA.Raw);
		BuildRenderInfo(ref p);
		MuiExternalWrapperCore.Setup(ref p, Instance, RenderInfo);
		MuiExternalWrapperCore.Show(ref p, Instance);
		Set(ref p, MuiExternalWrapperAttributes.Disabled, 1);
		Assert.False(MuiExternalWrapperCore.Draw(ref p, Instance));
		Assert.Equal(0u, p.DrawPictureCount);
	}

	[Fact]
	public void DtpicRuntimeNameChangeReloadsPicture()
	{
		var p = NewPlatform();
		p.PictureWidth = 40;
		p.PictureHeight = 30;
		CreateDtpic(ref p);
		p.WriteCString(NameA, "first.iff");
		Set(ref p, MuiExternalWrapperAttributes.Dtpic_Name, NameA.Raw);
		BuildRenderInfo(ref p);
		MuiExternalWrapperCore.Setup(ref p, Instance, RenderInfo);
		Assert.Equal(1u, p.AcquirePictureCount);
		p.WriteCString(NameB, "second.iff");
		Set(ref p, MuiExternalWrapperAttributes.Dtpic_Name, NameB.Raw);
		Assert.Equal(1u, p.ReleasePictureCount);   // old released
		Assert.Equal(2u, p.AcquirePictureCount);   // new acquired
		Assert.True(MuiExternalWrapperCore.RedrawPending(ref p, Instance));
	}

	// ---- Cleanup / dispose idempotence --------------------------------------

	[Fact]
	public void BoopsiCleanupAndDisposeCloseClassExactlyOnce()
	{
		var p = NewPlatform();
		CreateBoopsi(ref p);
		p.WriteCString(ClassId, "gadget.class");
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_ClassID, ClassId.Raw);
		WriteTagDone(ref p, CreationTags);
		MuiExternalWrapperCore.SetCreationTags(ref p, Instance, CreationTags);
		BuildRenderInfo(ref p);
		MuiExternalWrapperCore.Setup(ref p, Instance, RenderInfo);

		Assert.True(MuiExternalWrapperCore.Cleanup(ref p, Instance));
		Assert.Equal(1u, p.CloseExternalClassCount);
		Assert.True(MuiExternalWrapperCore.Cleanup(ref p, Instance));  // idempotent
		Assert.Equal(1u, p.CloseExternalClassCount);

		Assert.True(MuiExternalWrapperLifecycle.Dispose(ref p, Instance));
		Assert.Equal(1u, p.CloseExternalClassCount);   // still exactly once
		Assert.False(MuiExternalWrapperCore.Valid(ref p, Instance));
		Assert.False(MuiExternalWrapperLifecycle.Dispose(ref p, Instance));
	}

	[Fact]
	public void BoopsiDisposeWithoutCleanupClosesClassOnce()
	{
		var p = NewPlatform();
		CreateBoopsi(ref p);
		p.WriteCString(ClassId, "gadget.class");
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_ClassID, ClassId.Raw);
		WriteTagDone(ref p, CreationTags);
		MuiExternalWrapperCore.SetCreationTags(ref p, Instance, CreationTags);
		BuildRenderInfo(ref p);
		MuiExternalWrapperCore.Setup(ref p, Instance, RenderInfo);
		Assert.True(MuiExternalWrapperLifecycle.Dispose(ref p, Instance));
		Assert.Equal(1u, p.CloseExternalClassCount);
		Assert.False(MuiExternalWrapperCore.Valid(ref p, Instance));
	}

	[Fact]
	public void DtpicCleanupAndDisposeReleasePictureExactlyOnce()
	{
		var p = NewPlatform();
		p.PictureWidth = 8;
		p.PictureHeight = 8;
		CreateDtpic(ref p);
		p.WriteCString(NameA, "logo.iff");
		Set(ref p, MuiExternalWrapperAttributes.Dtpic_Name, NameA.Raw);
		BuildRenderInfo(ref p);
		MuiExternalWrapperCore.Setup(ref p, Instance, RenderInfo);
		Assert.True(MuiExternalWrapperCore.IsPictureAcquired(ref p, Instance));

		Assert.True(MuiExternalWrapperCore.Cleanup(ref p, Instance));
		Assert.Equal(1u, p.ReleasePictureCount);
		Assert.True(MuiExternalWrapperCore.Cleanup(ref p, Instance));  // idempotent
		Assert.Equal(1u, p.ReleasePictureCount);

		Assert.True(MuiExternalWrapperLifecycle.Dispose(ref p, Instance));
		Assert.Equal(1u, p.ReleasePictureCount);
		Assert.False(MuiExternalWrapperCore.Valid(ref p, Instance));
		Assert.False(MuiExternalWrapperLifecycle.Dispose(ref p, Instance));
	}

	[Fact]
	public void DtpicDisposeFreesOwnedName()
	{
		var p = NewPlatform();
		CreateDtpic(ref p);
		p.WriteCString(NameA, "logo.iff");
		Set(ref p, MuiExternalWrapperAttributes.Dtpic_Name, NameA.Raw);
		var before = p.FreeCount;
		Assert.True(MuiExternalWrapperLifecycle.Dispose(ref p, Instance));
		// Owned name + work block are freed on dispose.
		Assert.True(p.FreeCount > before);
	}

	// ---- Disabled state notifies + redraw -----------------------------------

	[Fact]
	public void DisabledSetNotifiesAndRequestsRedraw()
	{
		var p = NewPlatform();
		CreateBoopsi(ref p);
		MuiExternalWrapperCore.SetAttribute(ref p, Instance,
			MuiExternalWrapperAttributes.Disabled, 1, false, true, out var changed,
			out var handled);
		Assert.True(changed);
		Assert.True(handled);
		Assert.Equal(1u, MuiExternalWrapperCore.NotificationCount(ref p, Instance));
		Assert.True(MuiExternalWrapperCore.RedrawPending(ref p, Instance));
		Get(ref p, MuiExternalWrapperAttributes.Disabled, out var disabled);
		Assert.Equal(1u, disabled);
	}

	// ---- Helpers -------------------------------------------------------------

	private static void SetupPrivateBoopsi(ref MuiHeadlessTestPlatform p)
	{
		CreateBoopsi(ref p);
		Set(ref p, MuiExternalWrapperAttributes.Boopsi_Class, PrivateClass.Raw);
		WriteTagDone(ref p, CreationTags);
		MuiExternalWrapperCore.SetCreationTags(ref p, Instance, CreationTags);
		BuildRenderInfo(ref p);
		Assert.True(MuiExternalWrapperCore.Setup(ref p, Instance, RenderInfo));
	}

	private static APTR ObjectPointer(ref MuiHeadlessTestPlatform p)
	{
		MuiExternalWrapperCore.GetAttribute(ref p, Instance,
			MuiExternalWrapperAttributes.Boopsi_Object, out var raw);
		return APTR.FromPointer(raw);
	}

	private static void WriteTagDone(ref MuiHeadlessTestPlatform p, APTR list) =>
		p.WriteUInt32(list, 0, 0);

	private static void Set(ref MuiHeadlessTestPlatform p, uint attribute,
		uint value) =>
		MuiExternalWrapperCore.SetAttribute(ref p, Instance, attribute, value,
			false, false, out _, out _);

	private static bool SetInit(ref MuiHeadlessTestPlatform p, uint attribute,
		uint value)
	{
		MuiExternalWrapperCore.SetAttribute(ref p, Instance, attribute, value,
			true, false, out var changed, out _);
		return changed;
	}

	private static void Get(ref MuiHeadlessTestPlatform p, uint attribute,
		out uint value) =>
		Assert.True(MuiExternalWrapperCore.GetAttribute(ref p, Instance, attribute,
			out value));
}
