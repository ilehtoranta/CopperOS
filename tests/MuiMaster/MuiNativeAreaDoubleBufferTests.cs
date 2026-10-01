using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeAreaDoubleBufferTests
{
	private static readonly APTR Sidecar = APTR.FromPointer(0x1800);
	private static readonly APTR Object = APTR.FromPointer(0x1900);
	private static readonly APTR OwnerRoot = APTR.FromPointer(0x1A00);
	private static readonly APTR SourceRenderInfo = APTR.FromPointer(0x2100);
	private static readonly APTR SourceRastPort = APTR.FromPointer(0x2200);
	private static readonly APTR TargetRenderInfo = APTR.FromPointer(0x2300);
	private static readonly APTR TargetRastPort = APTR.FromPointer(0x2400);
	private static readonly APTR OtherRastPort = APTR.FromPointer(0x2500);

	private static MuiHeadlessTestPlatform CreatePlatform()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x8000, 0x5000,
			Sidecar);
		var sidecar = new MuiNativeMuiObjectRecord
		{
			Signature = MuiNativeMuiObjectRecord.Magic,
			Revision = MuiNativeMuiObjectRecord.Version,
			Object = Object,
			OwnerRoot = OwnerRoot,
			Flags = MuiNativeMuiObjectRecord.ObjectInitialized,
			LifecycleState = MuiNativeMuiObjectRecord.StateLive,
		};
		Assert.True(MuiNativeMuiObjectCodec.Write(ref platform, Sidecar, sidecar));
		WriteRenderInfo(ref platform, SourceRenderInfo, SourceRastPort);
		WriteRenderInfo(ref platform, TargetRenderInfo, TargetRastPort);
		Assert.True(MuiNativeObjectStateCore.SetBorrowedAttributeNoNotify(
			ref platform, Sidecar, MuiCommonControlCore.DoubleBuffer, 1));
		Assert.True(MuiNativeObjectStateCore.SetBorrowedAttributeNoNotify(
			ref platform, Sidecar,
			MuiAreaWindowRelationshipCore.RenderInfoAttribute,
			SourceRenderInfo.Raw));
		platform.DoubleBufferCapabilityAvailable = true;
		platform.DoubleBufferTargetRenderInfo = TargetRenderInfo;
		platform.DoubleBufferTargetRastPort = TargetRastPort;
		return platform;
	}

	[Fact]
	public void NativeDrawLeasePublishesTemporaryRenderInfoAndGeometryThenRestores()
	{
		var platform = CreatePlatform();
		var geometry = new MuiNativeAreaRectangle
		{
			Left = 22,
			Top = 31,
			Width = 44,
			Height = 28,
		};
		Assert.True(MuiNativeAreaDoubleBufferCore.TryBegin(ref platform,
			Sidecar, OwnerRoot, Object, geometry, 7, out var lease));
		Assert.Equal(1u, lease.Active);
		Assert.Equal(SourceRastPort, platform.LastDoubleBufferRequest.SourceRastPort);
		Assert.Equal(22, platform.LastDoubleBufferRequest.Left);
		Assert.Equal(31, platform.LastDoubleBufferRequest.Top);
		Assert.Equal(0, platform.LastDoubleBufferRequest.TargetLeft);
		Assert.Equal(0, platform.LastDoubleBufferRequest.TargetTop);
		Assert.True(MuiNativeObjectStateCore.TryGetAttribute(ref platform,
			Sidecar, MuiAreaWindowRelationshipCore.RenderInfoAttribute,
			out var renderInfoRaw));
		Assert.Equal(TargetRenderInfo.Raw, renderInfoRaw);
		Assert.True(MuiNativeAreaDoubleBufferCore.TryGetTemporaryGeometry(
			ref platform, Sidecar, OwnerRoot, Object,
			MuiNativeGuiMode.AreaLeftEdge, out var left));
		Assert.Equal(0u, left);
		Assert.True(MuiNativeAreaDoubleBufferCore.TryGetTemporaryGeometry(
			ref platform, Sidecar, OwnerRoot, Object,
			MuiNativeGuiMode.AreaTopEdge, out var top));
		Assert.Equal(0u, top);
		Assert.True(MuiNativeAreaDoubleBufferCore.TryGetTemporaryGeometry(
			ref platform, Sidecar, OwnerRoot, Object,
			MuiNativeGuiMode.AreaWidth, out var width));
		Assert.Equal(44u, width);
		Assert.True(MuiNativeAreaDoubleBufferCore.TryGetTemporaryGeometry(
			ref platform, Sidecar, OwnerRoot, Object,
			MuiNativeGuiMode.AreaHeight, out var height));
		Assert.Equal(28u, height);

		Assert.True(MuiNativeAreaDoubleBufferCore.End(ref platform, ref lease,
			true));
		Assert.Equal(0u, lease.Active);
		Assert.True(platform.LastDoubleBufferEndCompleted);
		Assert.True(MuiNativeObjectStateCore.TryGetAttribute(ref platform,
			Sidecar, MuiAreaWindowRelationshipCore.RenderInfoAttribute,
			out renderInfoRaw));
		Assert.Equal(SourceRenderInfo.Raw, renderInfoRaw);
		Assert.False(MuiNativeAreaDoubleBufferCore.TryGetTemporaryGeometry(
			ref platform, Sidecar, OwnerRoot, Object,
			MuiNativeGuiMode.AreaLeftEdge, out _));
	}

	[Fact]
	public void NestedNativeDrawLeaseRestoresTheParentRenderFrame()
	{
		var platform = CreatePlatform();
		var geometry = new MuiNativeAreaRectangle
		{
			Left = 10,
			Top = 15,
			Width = 32,
			Height = 20,
		};
		platform.DoubleBufferOverrideTargetGeometry = true;
		platform.DoubleBufferTargetLeft = 2;
		platform.DoubleBufferTargetTop = 3;
		platform.DoubleBufferTargetWidth = 32;
		platform.DoubleBufferTargetHeight = 20;
		var outerResult = MuiNativeAreaDoubleBufferCore.TryBegin(ref platform,
			Sidecar, OwnerRoot, Object, geometry, 0, out var outer);
		Assert.True(outerResult);
		Assert.True(MuiNativeAreaDoubleBufferCore.TryGetTemporaryGeometry(
			ref platform, Sidecar, OwnerRoot, Object,
			MuiNativeGuiMode.AreaLeftEdge, out var outerLeft));
		Assert.Equal(2u, outerLeft);

		platform.DoubleBufferTargetLeft = 6;
		platform.DoubleBufferTargetTop = 7;
		var nestedGeometry = new MuiNativeAreaRectangle
		{
			Left = 2,
			Top = 3,
			Width = 32,
			Height = 20,
		};
		Assert.True(MuiNativeAreaDoubleBufferCore.TryBegin(ref platform,
			Sidecar, OwnerRoot, Object, nestedGeometry, 0, out var nested));
		Assert.Equal(TargetRastPort, platform.LastDoubleBufferRequest.SourceRastPort);
		Assert.True(MuiNativeAreaDoubleBufferCore.TryGetTemporaryGeometry(
			ref platform, Sidecar, OwnerRoot, Object,
			MuiNativeGuiMode.AreaLeftEdge, out var nestedLeft));
		Assert.Equal(6u, nestedLeft);

		Assert.True(MuiNativeAreaDoubleBufferCore.End(ref platform, ref nested,
			true));
		Assert.True(MuiNativeAreaDoubleBufferCore.TryGetTemporaryGeometry(
			ref platform, Sidecar, OwnerRoot, Object,
			MuiNativeGuiMode.AreaLeftEdge, out var restoredOuterLeft));
		Assert.Equal(2u, restoredOuterLeft);
		Assert.True(MuiNativeAreaDoubleBufferCore.End(ref platform, ref outer,
			true));
		Assert.False(MuiNativeAreaDoubleBufferCore.TryGetTemporaryGeometry(
			ref platform, Sidecar, OwnerRoot, Object,
			MuiNativeGuiMode.AreaLeftEdge, out _));
	}

	[Fact]
	public void MismatchedTargetRenderInfoIsDiscardedWithoutPublication()
	{
		var platform = CreatePlatform();
		WriteRenderInfo(ref platform, TargetRenderInfo, OtherRastPort);
		var allocations = platform.AllocationCount;
		var geometry = new MuiNativeAreaRectangle
		{
			Left = 1,
			Top = 2,
			Width = 12,
			Height = 9,
		};

		Assert.False(MuiNativeAreaDoubleBufferCore.TryBegin(ref platform,
			Sidecar, OwnerRoot, Object, geometry, 0, out var lease));
		Assert.Equal(0u, lease.Active);
		Assert.Equal(allocations, platform.AllocationCount);
		Assert.Equal(1u, platform.DoubleBufferEndCount);
		Assert.False(platform.LastDoubleBufferEndCompleted);
		Assert.True(MuiNativeObjectStateCore.TryGetAttribute(ref platform,
			Sidecar, MuiAreaWindowRelationshipCore.RenderInfoAttribute,
			out var renderInfoRaw));
		Assert.Equal(SourceRenderInfo.Raw, renderInfoRaw);
	}

	[Fact]
	public void IncompleteNativeDrawDiscardsSurfaceAndRestoresFrame()
	{
		var platform = CreatePlatform();
		var geometry = new MuiNativeAreaRectangle
		{
			Left = 4,
			Top = 6,
			Width = 18,
			Height = 11,
		};
		Assert.True(MuiNativeAreaDoubleBufferCore.TryBegin(ref platform,
			Sidecar, OwnerRoot, Object, geometry, 0, out var lease));

		Assert.True(MuiNativeAreaDoubleBufferCore.End(ref platform, ref lease,
			false));
		Assert.False(platform.LastDoubleBufferEndCompleted);
		Assert.Equal(0u, lease.Active);
		Assert.False(MuiNativeAreaDoubleBufferCore.TryGetTemporaryGeometry(
			ref platform, Sidecar, OwnerRoot, Object,
			MuiNativeGuiMode.AreaWidth, out _));
		Assert.True(MuiNativeObjectStateCore.TryGetAttribute(ref platform,
			Sidecar, MuiAreaWindowRelationshipCore.RenderInfoAttribute,
			out var renderInfoRaw));
		Assert.Equal(SourceRenderInfo.Raw, renderInfoRaw);
	}

	private static void WriteRenderInfo(ref MuiHeadlessTestPlatform platform,
		APTR address, APTR rastPort)
	{
		var record = default(MuiDrawingRenderInfoRecord);
		record.RastPort = rastPort;
		Assert.True(MuiDrawingRenderInfoCodec.Write(ref platform, address, record));
	}
}
