using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiDrawingServiceTests
{
	private const uint Base = 0x1000;
	private const int Size = 0x20000;
	private const uint FirstAllocation = 0x4000;

	private static readonly APTR State = APTR.FromPointer(0x1000);
	private static readonly APTR Mri = APTR.FromPointer(0x1100);
	private static readonly APTR RastPort = APTR.FromPointer(0x1200);
	private static readonly APTR Layer = APTR.FromPointer(0x1280);
	private static readonly APTR PenSpec = APTR.FromPointer(0x1300);
	private static readonly APTR Region = APTR.FromPointer(0x1400);
	private static readonly APTR Rgb = APTR.FromPointer(0x1440);

	private const int RiFlags = 24;
	private const uint RefreshMode = 1u << 3;

	private static MuiHeadlessTestPlatform NewInitializedPlatform(
		bool mapRastPort = true)
	{
		var platform = new MuiHeadlessTestPlatform(Base, Size, FirstAllocation,
			State);
		Assert.True(MuiDrawingServiceCore.Initialize(ref platform, State));
		platform.WriteUInt32(Mri, 20, mapRastPort ? RastPort.Raw : 0);
		platform.WriteUInt32(RastPort, 0, Layer.Raw);
		return platform;
	}

	[Fact]
	public void DrawingRecordFieldCursorUsesSemanticRecordKinds()
	{
		var platform = NewInitializedPlatform();
		var cursor = default(MuiDrawingRecordFieldCursor);
		cursor.Address = State;
		cursor.Record = MuiDrawingRecordKind.State;
		cursor.Field = MuiDrawingRecordField.Generation;
		Assert.True(MuiDrawingRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var fieldAddress));
		Assert.Equal(State.Raw + 16, fieldAddress.Raw);
		cursor.Address = Mri;
		cursor.Record = MuiDrawingRecordKind.RenderInfo;
		cursor.Field = MuiDrawingRecordField.Flags;
		Assert.True(MuiDrawingRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out fieldAddress));
		Assert.Equal(Mri.Raw + 24, fieldAddress.Raw);
		Assert.True(MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			Mri, MuiDrawingRecordKind.RenderInfo, MuiDrawingRecordField.Flags,
			0xA5A5u));
		Assert.True(MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform,
			Mri, MuiDrawingRecordKind.RenderInfo, MuiDrawingRecordField.Flags,
			out var flags));
		Assert.Equal(0xA5A5u, flags);
		cursor.Record = MuiDrawingRecordKind.RasterPort;
		cursor.Field = MuiDrawingRecordField.Flags;
		Assert.False(MuiDrawingRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
		cursor.Address = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Field = MuiDrawingRecordField.Layer;
		Assert.False(MuiDrawingRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
	}

	[Fact]
	public void OperationsRequireInitialization()
	{
		var platform = new MuiHeadlessTestPlatform(Base, Size, FirstAllocation,
			State);
		platform.WriteUInt32(Mri, 20, RastPort.Raw);
		platform.WriteUInt32(RastPort, 0, Layer.Raw);
		Assert.Equal(APTR.Null, MuiDrawingServiceCore.AddClipping(ref platform,
			State, Mri, 0, 0, 8, 8));
		Assert.Equal(APTR.Null, MuiDrawingServiceCore.AddClipRegion(ref platform,
			State, Mri, Region));
		Assert.False(MuiDrawingServiceCore.BeginRefresh(ref platform, State, Mri,
			0));
		Assert.Equal(-1, MuiDrawingServiceCore.ObtainPen(ref platform, State, Mri,
			PenSpec, 0));
		Assert.False(MuiDrawingServiceCore.GetRGBColor(ref platform, State, Mri,
			PenSpec, Rgb));

		Assert.True(MuiDrawingServiceCore.Initialize(ref platform, State));
		Assert.True(MuiDrawingServiceCore.AddClipping(ref platform, State, Mri, 0,
			0, 8, 8).IsNotNull);
	}

	[Fact]
	public void ReinitializationIsIdempotentAndPreservesOutstandingClip()
	{
		var platform = NewInitializedPlatform();
		var clip = MuiDrawingServiceCore.AddClipping(ref platform, State, Mri, 1,
			2, 3, 4);
		Assert.True(clip.IsNotNull);
		// A second Initialize must not drop the outstanding clip stack.
		Assert.True(MuiDrawingServiceCore.Initialize(ref platform, State));
		Assert.True(MuiDrawingServiceCore.RemoveClipping(ref platform, State, Mri,
			clip));
	}

	[Fact]
	public void MalformedRenderInfoIsRejected()
	{
		var platform = NewInitializedPlatform(mapRastPort: false);
		// Null rast port -> no layer.
		Assert.Equal(APTR.Null, MuiDrawingServiceCore.AddClipping(ref platform,
			State, Mri, 0, 0, 8, 8));
		Assert.Equal(APTR.Null, MuiDrawingServiceCore.AddClipRegion(ref platform,
			State, Mri, Region));
		Assert.False(MuiDrawingServiceCore.BeginRefresh(ref platform, State, Mri,
			0));
		// Null render info pointer.
		Assert.Equal(-1, MuiDrawingServiceCore.ObtainPen(ref platform, State,
			APTR.Null, PenSpec, 0));
		Assert.False(MuiDrawingServiceCore.GetRGBColor(ref platform, State,
			APTR.Null, PenSpec, Rgb));
		Assert.Equal(0u, platform.ClipPushCount);
		Assert.Equal(0u, platform.ObtainPenCount);
	}

	[Fact]
	public void AddClippingRoutesPushClipAndReturnsOpaqueHandle()
	{
		var platform = NewInitializedPlatform();
		var clip = MuiDrawingServiceCore.AddClipping(ref platform, State, Mri, 5,
			7, 24, 12);
		Assert.True(clip.IsNotNull);
		Assert.Equal(1u, platform.ClipPushCount);
		Assert.Equal(Layer.Raw, platform.LastPushClipLayer.Raw);
		Assert.Equal(5, platform.LastClipLeft);
		Assert.Equal(7, platform.LastClipTop);
		Assert.Equal(24, platform.LastClipWidth);
		Assert.Equal(12, platform.LastClipHeight);
		// The handle is guest-resident (allocated), not the caller's inputs.
		Assert.NotEqual(Mri.Raw, clip.Raw);
		Assert.True(MuiDrawingServiceCore.RemoveClipping(ref platform, State, Mri,
			clip));
		Assert.Equal(1u, platform.ClipPopCount);
		Assert.Equal(Layer.Raw, platform.LastPopClipLayer.Raw);
	}

	[Fact]
	public void ClipRegionRequiresNonNullRegionAndRoutesInstallRestore()
	{
		var platform = NewInitializedPlatform();
		Assert.Equal(APTR.Null, MuiDrawingServiceCore.AddClipRegion(ref platform,
			State, Mri, APTR.Null));
		Assert.Equal(0u, platform.InstallRegionCount);

		var handle = MuiDrawingServiceCore.AddClipRegion(ref platform, State, Mri,
			Region);
		Assert.True(handle.IsNotNull);
		Assert.Equal(1u, platform.InstallRegionCount);
		Assert.Equal(Region.Raw, platform.LastInstalledRegion.Raw);
		var installedPrevious = platform.LastRegionLayer; // layer recorded
		Assert.Equal(Layer.Raw, installedPrevious.Raw);

		Assert.True(MuiDrawingServiceCore.RemoveClipRegion(ref platform, State,
			Mri, handle));
		Assert.Equal(1u, platform.RestoreRegionCount);
		// The previous region returned by install is what restore receives.
		Assert.Equal(0x2A000u, platform.LastRestoredRegion.Raw);
	}

	[Fact]
	public void NestedClipsEnforceStrictLifoAndKind()
	{
		var platform = NewInitializedPlatform();
		var clip = MuiDrawingServiceCore.AddClipping(ref platform, State, Mri, 0,
			0, 40, 40);
		var region = MuiDrawingServiceCore.AddClipRegion(ref platform, State, Mri,
			Region);
		Assert.True(clip.IsNotNull);
		Assert.True(region.IsNotNull);

		// Out-of-order removal of the older clip is rejected.
		Assert.False(MuiDrawingServiceCore.RemoveClipping(ref platform, State, Mri,
			clip));
		// Wrong kind at the top is rejected (region top removed via RemoveClipping).
		Assert.False(MuiDrawingServiceCore.RemoveClipping(ref platform, State, Mri,
			region));
		// Correct LIFO order: region first, then clip.
		Assert.True(MuiDrawingServiceCore.RemoveClipRegion(ref platform, State,
			Mri, region));
		Assert.True(MuiDrawingServiceCore.RemoveClipping(ref platform, State, Mri,
			clip));
		// Both retired; double removal fails.
		Assert.False(MuiDrawingServiceCore.RemoveClipping(ref platform, State, Mri,
			clip));
		Assert.False(MuiDrawingServiceCore.RemoveClipRegion(ref platform, State,
			Mri, region));
	}

	[Fact]
	public void DeeplyNestedClipsUnwindInReverseOrder()
	{
		var platform = NewInitializedPlatform();
		var a = MuiDrawingServiceCore.AddClipping(ref platform, State, Mri, 0, 0,
			10, 10);
		var b = MuiDrawingServiceCore.AddClipping(ref platform, State, Mri, 1, 1,
			8, 8);
		var c = MuiDrawingServiceCore.AddClipping(ref platform, State, Mri, 2, 2,
			6, 6);
		Assert.True(a.IsNotNull && b.IsNotNull && c.IsNotNull);
		Assert.Equal(3u, platform.ClipPushCount);
		// Removing anything but the top fails.
		Assert.False(MuiDrawingServiceCore.RemoveClipping(ref platform, State, Mri,
			a));
		Assert.False(MuiDrawingServiceCore.RemoveClipping(ref platform, State, Mri,
			b));
		Assert.True(MuiDrawingServiceCore.RemoveClipping(ref platform, State, Mri,
			c));
		Assert.True(MuiDrawingServiceCore.RemoveClipping(ref platform, State, Mri,
			b));
		Assert.True(MuiDrawingServiceCore.RemoveClipping(ref platform, State, Mri,
			a));
		Assert.Equal(3u, platform.ClipPopCount);
	}

	[Fact]
	public void BeginRefreshValidatesFlagsAndBracketsUpdate()
	{
		var platform = NewInitializedPlatform();
		platform.WriteUInt32(Mri, RiFlags, 0x40); // pre-existing unrelated flags

		// Reserved flags must be 0.
		Assert.False(MuiDrawingServiceCore.BeginRefresh(ref platform, State, Mri,
			1));
		Assert.Equal(0x40u, platform.ReadUInt32(Mri, RiFlags));
		Assert.Equal(0u, platform.BeginUpdateCount);

		Assert.True(MuiDrawingServiceCore.BeginRefresh(ref platform, State, Mri,
			0));
		Assert.Equal(1u, platform.BeginUpdateCount);
		Assert.Equal(0x40u | RefreshMode, platform.ReadUInt32(Mri, RiFlags));

		// EndRefresh also validates flags == 0.
		Assert.False(MuiDrawingServiceCore.EndRefresh(ref platform, State, Mri,
			2));
		Assert.Equal(0u, platform.EndUpdateCount);

		Assert.True(MuiDrawingServiceCore.EndRefresh(ref platform, State, Mri, 0));
		Assert.Equal(1u, platform.EndUpdateCount);
		Assert.True(platform.LastEndUpdateCompleted);
		// The refresh flag is restored to its pre-refresh value.
		Assert.Equal(0x40u, platform.ReadUInt32(Mri, RiFlags));
	}

	[Fact]
	public void BeginRefreshFailureIsAtomic()
	{
		var platform = NewInitializedPlatform();
		platform.WriteUInt32(Mri, RiFlags, 0x11);
		platform.BeginUpdateFails = true;
		Assert.False(MuiDrawingServiceCore.BeginRefresh(ref platform, State, Mri,
			0));
		// Flag restored, no update left open, and nothing to end.
		Assert.Equal(0x11u, platform.ReadUInt32(Mri, RiFlags));
		Assert.Equal(0u, platform.BeginUpdateCount);
		Assert.False(MuiDrawingServiceCore.EndRefresh(ref platform, State, Mri,
			0));
		Assert.Equal(0u, platform.EndUpdateCount);
	}

	[Fact]
	public void NestedRefreshBalancesInLifoOrder()
	{
		var platform = NewInitializedPlatform();
		var otherMri = APTR.FromPointer(0x1500);
		var otherRast = APTR.FromPointer(0x1560);
		platform.WriteUInt32(otherMri, 20, otherRast.Raw);
		platform.WriteUInt32(otherRast, 0, Layer.Raw);

		Assert.True(MuiDrawingServiceCore.BeginRefresh(ref platform, State, Mri,
			0));
		Assert.True(MuiDrawingServiceCore.BeginRefresh(ref platform, State,
			otherMri, 0));
		// EndRefresh for the older (non-top) render info is rejected: strict LIFO.
		Assert.False(MuiDrawingServiceCore.EndRefresh(ref platform, State, Mri,
			0));
		Assert.True(MuiDrawingServiceCore.EndRefresh(ref platform, State, otherMri,
			0));
		Assert.True(MuiDrawingServiceCore.EndRefresh(ref platform, State, Mri, 0));
		Assert.Equal(2u, platform.BeginUpdateCount);
		Assert.Equal(2u, platform.EndUpdateCount);
	}

	[Fact]
	public void ObtainPenTracksFullTokenAndBalancesRelease()
	{
		var platform = NewInitializedPlatform();
		platform.NextPenToken = 0x00010005; // low bits 5, high tag 1

		var pen = MuiDrawingServiceCore.ObtainPen(ref platform, State, Mri,
			PenSpec, 0x80);
		Assert.Equal(0x00010005, pen);
		Assert.Equal(1u, platform.ObtainPenCount);
		Assert.Equal(0x80u, platform.LastObtainPenFlags);
		Assert.Equal(PenSpec.Raw, platform.LastPenSpec.Raw);

		// Releasing a MUIPEN-masked value must not match the tracked full token.
		Assert.False(MuiDrawingServiceCore.ReleasePen(ref platform, State, Mri,
			pen & 0xffff));
		Assert.Equal(0u, platform.ReleasePenCount);

		// Releasing the full token succeeds and calls the capability with it.
		Assert.True(MuiDrawingServiceCore.ReleasePen(ref platform, State, Mri,
			pen));
		Assert.Equal(1u, platform.ReleasePenCount);
		Assert.Equal(0x00010005, platform.LastReleasedPen);

		// Duplicate release fails.
		Assert.False(MuiDrawingServiceCore.ReleasePen(ref platform, State, Mri,
			pen));
		Assert.Equal(1u, platform.ReleasePenCount);
	}

	[Fact]
	public void ObtainPenFailureReservesNothing()
	{
		var platform = NewInitializedPlatform();
		platform.PenObtainFailure = true;
		platform.PenObtainFailureValue = -3;
		var allocationsBefore = platform.AllocationCount;
		Assert.Equal(-3, MuiDrawingServiceCore.ObtainPen(ref platform, State, Mri,
			PenSpec, 0));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		// Nothing was tracked, so a release fails.
		Assert.False(MuiDrawingServiceCore.ReleasePen(ref platform, State, Mri,
			-3));
	}

	[Fact]
	public void MultiplePensAreIndependentlyBalanced()
	{
		var platform = NewInitializedPlatform();
		platform.NextPenToken = 0x00020010;
		var first = MuiDrawingServiceCore.ObtainPen(ref platform, State, Mri,
			PenSpec, 0);
		var second = MuiDrawingServiceCore.ObtainPen(ref platform, State, Mri,
			PenSpec, 0);
		Assert.NotEqual(first, second);
		// Release out of acquisition order: pens are keyed by token, not LIFO.
		Assert.True(MuiDrawingServiceCore.ReleasePen(ref platform, State, Mri,
			first));
		Assert.True(MuiDrawingServiceCore.ReleasePen(ref platform, State, Mri,
			second));
		Assert.False(MuiDrawingServiceCore.ReleasePen(ref platform, State, Mri,
			first));
		Assert.Equal(2u, platform.ReleasePenCount);
	}

	[Fact]
	public void ObtainPenRejectsUnmappedSpec()
	{
		var platform = NewInitializedPlatform();
		// A spec that runs past the mapped arena is rejected before the capability.
		var badSpec = APTR.FromPointer(Base + (uint)Size - 4);
		Assert.Equal(-1, MuiDrawingServiceCore.ObtainPen(ref platform, State, Mri,
			badSpec, 0));
		Assert.Equal(0u, platform.ObtainPenCount);
	}

	[Fact]
	public void GetRGBColorValidatesMappingAndWritesComponents()
	{
		var platform = NewInitializedPlatform();
		platform.RgbRed = 0x11223344;
		platform.RgbGreen = 0x55667788;
		platform.RgbBlue = 0x99AABBCC;

		// Null / unmapped RGB output is rejected before the capability runs.
		Assert.False(MuiDrawingServiceCore.GetRGBColor(ref platform, State, Mri,
			PenSpec, APTR.Null));
		Assert.Equal(0u, platform.GetRGBColorCount);

		Assert.True(MuiDrawingServiceCore.GetRGBColor(ref platform, State, Mri,
			PenSpec, Rgb));
		Assert.Equal(1u, platform.GetRGBColorCount);
		Assert.Equal(0x11223344u, platform.ReadUInt32(Rgb, 0));
		Assert.Equal(0x55667788u, platform.ReadUInt32(Rgb, 4));
		Assert.Equal(0x99AABBCCu, platform.ReadUInt32(Rgb, 8));
	}

	[Fact]
	public void ClipAndPenAllocationFailuresAreAtomic()
	{
		// A tiny arena that cannot satisfy a record allocation forces the
		// rollback paths: the clip/pen must be undone in the capability.
		var platform = new MuiHeadlessTestPlatform(Base, Size, Base + (uint)Size,
			State);
		Assert.True(MuiDrawingServiceCore.Initialize(ref platform, State));
		platform.WriteUInt32(Mri, 20, RastPort.Raw);
		platform.WriteUInt32(RastPort, 0, Layer.Raw);

		Assert.Equal(APTR.Null, MuiDrawingServiceCore.AddClipping(ref platform,
			State, Mri, 0, 0, 8, 8));
		Assert.Equal(1u, platform.ClipPushCount);
		Assert.Equal(1u, platform.ClipPopCount); // rolled back

		Assert.Equal(APTR.Null, MuiDrawingServiceCore.AddClipRegion(ref platform,
			State, Mri, Region));
		Assert.Equal(1u, platform.InstallRegionCount);
		Assert.Equal(1u, platform.RestoreRegionCount); // rolled back

		Assert.Equal(-1, MuiDrawingServiceCore.ObtainPen(ref platform, State, Mri,
			PenSpec, 0));
		Assert.Equal(1u, platform.ObtainPenCount);
		Assert.Equal(1u, platform.ReleasePenCount); // rolled back
	}
}
