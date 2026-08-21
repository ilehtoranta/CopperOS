using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

// MorphOS 3.20 String.mui scroll metrics are separate from the older
// Stringscroll.mui composite.  These tests cover bounded pixel metrics,
// clamping, and the public OM_GET/OM_SET packet seam.
public sealed class MuiStringScrollAttributeTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint StringContents = 0x80428FFDu;
	private const uint StringScrollHeight = 0x8042BE8Bu;
	private const uint StringScrollLeft = 0x8042BD0Du;
	private const uint StringScrollTop = 0x8042F4E5u;
	private const uint StringScrollVisibleHeight = 0x8042791Eu;
	private const uint StringScrollVisibleWidth = 0x8042D280u;
	private const uint StringScrollWidth = 0x80420FB5u;
	private const uint MethodSet = 0x8042549Au;
	private const uint OmGet = 0x00000104u;

	[Fact]
	public void MetricsArePixelBasedAndOffsetsClampToTheLaidOutViewport()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var contents = APTR.FromPointer(0x1800);
		platform.WriteCString(contents, "abcdefghij\nxy");
		var obj = Create(ref platform, stringClass, contents);

		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, obj, 4, 6,
			40, 20));
		Assert.Equal(80u, Get(ref platform, obj, StringScrollWidth));
		Assert.Equal(20u, Get(ref platform, obj, StringScrollHeight));
		Assert.Equal(40u, Get(ref platform, obj, StringScrollVisibleWidth));
		Assert.Equal(20u, Get(ref platform, obj, StringScrollVisibleHeight));
		Assert.Equal(0u, Get(ref platform, obj, StringScrollLeft));
		Assert.Equal(0u, Get(ref platform, obj, StringScrollTop));

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			obj, StringScrollLeft, 999));
		Assert.Equal(40u, Get(ref platform, obj, StringScrollLeft));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			obj, StringScrollTop, 999));
		Assert.Equal(0u, Get(ref platform, obj, StringScrollTop));

		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			obj, StringScrollWidth, 1));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, obj));
	}

	[Fact]
	public void Utf8MetricsCountCodepointsAndPreserveLineHeight()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var contents = APTR.FromPointer(0x1850);
		var utf8 = new byte[] { 0xC3, 0x85, 0xCE, 0xB2, 0xF0, 0x9F,
			0x99, 0x82, 0x0A, 0 };
		for (var index = 0; index < utf8.Length; index++)
			platform.WriteUInt8(contents, index, utf8[index]);
		var obj = Create(ref platform, stringClass, contents);

		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, obj, 0, 0,
			40, 30));
		Assert.Equal(24u, Get(ref platform, obj, StringScrollWidth));
		Assert.Equal(20u, Get(ref platform, obj, StringScrollHeight));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, obj));
	}

	[Fact]
	public void ScrollMetricsReconcileAreaGeometryProjection()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var contents = APTR.FromPointer(0x1870);
		platform.WriteCString(contents, "content");
		var obj = Create(ref platform, stringClass, contents);
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, obj, 0, 0,
			40, 20));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			0x8042B59Cu, 24, false));

		Assert.True(MuiStringScrollAttributeCore.Get(ref platform, State, obj,
			StringScrollVisibleWidth, out var visibleWidth));
		Assert.Equal(24u, visibleWidth);
		Assert.True(MuiAreaLayoutCore.TryGetGeometryStateRecord(ref platform,
			State, obj, out var geometry));
		Assert.Equal(24, geometry.Width);
		Assert.Equal(20, geometry.Height);
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, obj));
	}

	[Fact]
	public void ContentChangesRecomputeExtentAndClampExistingOffsets()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var contents = APTR.FromPointer(0x1800);
		platform.WriteCString(contents, "0123456789");
		var obj = Create(ref platform, stringClass, contents);
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, obj, 0, 0,
			32, 10));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			obj, StringScrollLeft, 48));
		Assert.Equal(48u, Get(ref platform, obj, StringScrollLeft));

		var replacement = APTR.FromPointer(0x1900);
		platform.WriteCString(replacement, "abc");
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			obj, StringContents, replacement.Raw));
		Assert.Equal(24u, Get(ref platform, obj, StringScrollWidth));
		Assert.Equal(0u, Get(ref platform, obj, StringScrollLeft));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, obj));
	}

	[Fact]
	public void OmGetAndOmSetExposeTheScrollAttributes()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var contents = APTR.FromPointer(0x1800);
		platform.WriteCString(contents, "abcdefgh");
		var obj = Create(ref platform, stringClass, contents);
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, obj, 0, 0,
			24, 10));

		var storage = APTR.FromPointer(0x1A00);
		var getPacket = APTR.FromPointer(0x1A20);
		platform.WriteUInt32(getPacket, 0, OmGet);
		platform.WriteUInt32(getPacket, 4, StringScrollWidth);
		platform.WriteUInt32(getPacket, 8, storage.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, getPacket));
		Assert.Equal(64u, platform.ReadUInt32(storage, 0));

		var setPacket = APTR.FromPointer(0x1A40);
		platform.WriteUInt32(setPacket, 0, MethodSet);
		platform.WriteUInt32(setPacket, 4, StringScrollLeft);
		platform.WriteUInt32(setPacket, 8, 40);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, setPacket));
		Assert.Equal(40u, Get(ref platform, obj, StringScrollLeft));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, obj));
	}

	[Fact]
	public void ScrollMetricsUseNamedRecordForGenericGetAndRawSynchronization()
	{
		var platform = NewPlatform();
		var stringClass = Register(ref platform, 0x1100, "String.mui");
		var contents = APTR.FromPointer(0x1D00);
		platform.WriteCString(contents, "abcdefghij");
		var obj = Create(ref platform, stringClass, contents);
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, obj, 0, 0,
			40, 10));

		Assert.True(MuiStringScrollAttributeCore.TryReadMetricsState(ref platform,
			State, obj, out var state));
		Assert.Equal(80u, state.Width);
		Assert.Equal(40u, state.VisibleWidth);
		Assert.Equal(0u, state.Left);
		Assert.True(MuiStringScrollAttributeCore.TryGetMetricsStateRecord(
			ref platform, State, obj, out var record));
		Assert.Equal(MuiStringScrollMetricsStateRecord.Cookie, record.Magic);
		Assert.Equal(80u, record.Width);
		Assert.Equal(40u, record.VisibleWidth);

		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, obj,
			StringScrollWidth, out var projected, out var handled));
		Assert.True(handled);
		Assert.Equal(80u, projected);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			StringScrollVisibleWidth, out var genericVisible));
		Assert.Equal(40u, genericVisible);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			StringScrollLeft, 999, false));
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, obj,
			StringScrollLeft, out var projectedLeft, out handled));
		Assert.True(handled);
		Assert.Equal(40u, projectedLeft);
		Assert.True(MuiStringScrollAttributeCore.TryGetMetricsStateRecord(
			ref platform, State, obj, out record));
		Assert.Equal(40u, record.Left);
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, obj));
	}

	private static APTR Create(ref MuiHeadlessTestPlatform platform, APTR classRecord,
		APTR contents)
	{
		var tags = APTR.FromPointer(0x1C00);
		platform.WriteUInt32(tags, 0, StringContents);
		platform.WriteUInt32(tags, 4, contents.Raw);
		platform.WriteUInt32(tags, 8, 0);
		platform.WriteUInt32(tags, 12, 0);
		return MuiCommonControlCore.CreateControl(ref platform, State, classRecord,
			tags);
	}

	private static MuiHeadlessTestPlatform NewPlatform()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x8000, State);
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		return platform;
	}

	private static APTR Register(ref MuiHeadlessTestPlatform platform, uint address,
		string name)
	{
		platform.WriteCString(APTR.FromPointer(address), name);
		return MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			APTR.FromPointer(address), APTR.Null, 1, APTR.FromPointer(1), false);
	}

	private static uint Get(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		Assert.True(MuiStringScrollAttributeCore.Get(ref platform, State, obj,
			attribute, out var value));
		return value;
	}
}
