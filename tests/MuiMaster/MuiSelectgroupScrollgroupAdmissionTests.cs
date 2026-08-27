using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiSelectgroupScrollgroupAdmissionTests
{
	[Fact]
	public void SelectgroupAndScrollgroupRecordsRoundTripThroughNamedStructs()
	{
		var platform = CreatePlatform();
		var selectgroupAddress = APTR.FromPointer(0x1500);
		var policyAddress = APTR.FromPointer(0x1520);
		var viewportAddress = APTR.FromPointer(0x1560);
		var selectgroup = new MuiSelectgroupActiveStateRecord
		{
			Magic = MuiSelectgroupActiveStateRecord.Cookie,
			Active = 3,
		};
		var policy = new MuiScrollgroupPolicyStateRecord
		{
			Magic = MuiScrollgroupPolicyStateRecord.Cookie,
			Contents = APTR.FromPointer(0x1800),
			FreeHorizontal = 1,
			FreeVertical = 0,
			HorizontalBar = APTR.FromPointer(0x1820),
			VerticalBar = APTR.Null,
			NoHorizontalBar = 0,
			NoVerticalBar = 1,
			AutoBars = 1,
			UseWindowBorder = 0,
		};
		var viewport = new MuiScrollgroupViewportStateRecord
		{
			Magic = MuiScrollgroupViewportStateRecord.Cookie,
			ViewportWidth = 640,
			ViewportHeight = 480,
			ContentWidth = 1280,
			ContentHeight = 960,
			MaximumScrollX = 640,
			MaximumScrollY = 480,
			ScrollLeft = 100,
			ScrollTop = 200,
			HorizontalBarVisible = 1,
			VerticalBarVisible = 0,
		};
		Assert.True(MuiSelectgroupActiveStateRecordCodec.Write(ref platform,
			selectgroupAddress, selectgroup));
		Assert.True(MuiScrollgroupPolicyStateRecordCodec.Write(ref platform,
			policyAddress, policy));
		Assert.True(MuiScrollgroupViewportStateRecordCodec.Write(ref platform,
			viewportAddress, viewport));
		Assert.True(MuiSelectgroupActiveStateRecordCodec.TryRead(ref platform,
			selectgroupAddress, out var selectgroupRead));
		Assert.True(MuiScrollgroupPolicyStateRecordCodec.TryRead(ref platform,
			policyAddress, out var policyRead));
		Assert.True(MuiScrollgroupViewportStateRecordCodec.TryRead(ref platform,
			viewportAddress, out var viewportRead));
		Assert.Equal(selectgroup.Active, selectgroupRead.Active);
		Assert.Equal(policy.Contents, policyRead.Contents);
		Assert.Equal(policy.HorizontalBar, policyRead.HorizontalBar);
		Assert.Equal(policy.NoVerticalBar, policyRead.NoVerticalBar);
		Assert.Equal(viewport.ContentWidth, viewportRead.ContentWidth);
		Assert.Equal(viewport.MaximumScrollY, viewportRead.MaximumScrollY);
		Assert.Equal(viewport.ScrollTop, viewportRead.ScrollTop);
		Assert.Equal(viewport.HorizontalBarVisible, viewportRead.HorizontalBarVisible);
	}

	[Fact]
	public void MalformedSelectgroupAndScrollgroupMagicRemainStructuralButFailClosed()
	{
		var platform = CreatePlatform();
		var selectgroupAddress = APTR.FromPointer(0x1600);
		var policyAddress = APTR.FromPointer(0x1620);
		var viewportAddress = APTR.FromPointer(0x1660);
		Assert.True(MuiSelectgroupActiveStateRecordCodec.Write(ref platform,
			selectgroupAddress, new MuiSelectgroupActiveStateRecord
			{
				Magic = MuiSelectgroupActiveStateRecord.Cookie,
				Active = 1,
			}));
		Assert.True(MuiScrollgroupPolicyStateRecordCodec.Write(ref platform,
			policyAddress, new MuiScrollgroupPolicyStateRecord
			{
				Magic = MuiScrollgroupPolicyStateRecord.Cookie,
				Contents = APTR.Null,
				FreeHorizontal = 0,
				FreeVertical = 0,
				HorizontalBar = APTR.Null,
				VerticalBar = APTR.Null,
				NoHorizontalBar = 0,
				NoVerticalBar = 0,
				AutoBars = 0,
				UseWindowBorder = 0,
			}));
		Assert.True(MuiScrollgroupViewportStateRecordCodec.Write(ref platform,
			viewportAddress, new MuiScrollgroupViewportStateRecord
			{
				Magic = MuiScrollgroupViewportStateRecord.Cookie,
				ViewportWidth = 100,
				ViewportHeight = 100,
				ContentWidth = 100,
				ContentHeight = 100,
				MaximumScrollX = 0,
				MaximumScrollY = 0,
				ScrollLeft = 0,
				ScrollTop = 0,
				HorizontalBarVisible = 0,
				VerticalBarVisible = 0,
			}));
		Assert.True(MuiSelectgroupActiveStateFieldCursorCodec.TryWriteUInt32(
			ref platform, selectgroupAddress,
			MuiSelectgroupActiveStateField.Magic, 0));
		Assert.True(MuiScrollgroupPolicyStateFieldCursorCodec.TryWriteUInt32(
			ref platform, policyAddress, MuiScrollgroupPolicyStateField.Magic, 0));
		Assert.True(MuiScrollgroupViewportFieldCursorCodec.TryWriteUInt32(
			ref platform, viewportAddress, MuiScrollgroupViewportField.Magic, 0));
		Assert.True(MuiSelectgroupActiveStateRecordCodec.TryReadStructural(
			ref platform, selectgroupAddress, out var selectgroup));
		Assert.True(MuiScrollgroupPolicyStateRecordCodec.TryReadStructural(
			ref platform, policyAddress, out var policy));
		Assert.True(MuiScrollgroupViewportStateRecordCodec.TryReadStructural(
			ref platform, viewportAddress, out var viewport));
		Assert.Equal(0u, selectgroup.Magic);
		Assert.Equal(0u, policy.Magic);
		Assert.Equal(0u, viewport.Magic);
		Assert.False(MuiSelectgroupActiveStateRecordCodec.TryRead(ref platform,
			selectgroupAddress, out _));
		Assert.False(MuiScrollgroupPolicyStateRecordCodec.TryRead(ref platform,
			policyAddress, out _));
		Assert.False(MuiScrollgroupViewportStateRecordCodec.TryRead(ref platform,
			viewportAddress, out _));
		Assert.False(MuiSelectgroupActiveStateAdmission.Validate(selectgroup));
		Assert.False(MuiScrollgroupPolicyStateAdmission.Validate(ref platform,
			policy));
		Assert.False(MuiScrollgroupViewportStateAdmission.Validate(viewport));
	}

	[Fact]
	public void SelectgroupActiveRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1700);
		var record = new MuiSelectgroupActiveStateRecord
		{
			Magic = MuiSelectgroupActiveStateRecord.Cookie,
			Active = 7,
		};
		Assert.True(MuiSelectgroupActiveStateRecordCodec.Write(ref platform,
			address, record));
		Assert.True(MuiSelectgroupActiveStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiSelectgroupActiveStateField.Active,
			out var activeAddress));
		Assert.Equal(0x1704u, activeAddress.Raw);
		Assert.True(MuiSelectgroupActiveStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiSelectgroupActiveStateField.Active,
			out var active));
		Assert.Equal(7u, active);
		Assert.True(MuiSelectgroupActiveStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiSelectgroupActiveStateField.Active, 2));
		Assert.True(MuiSelectgroupActiveStateRecordCodec.TryReadStructural(
			ref platform, address, out var updated));
		Assert.Equal(2u, updated.Active);
		Assert.False(MuiSelectgroupActiveStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiSelectgroupActiveStateField)255, out _));
		Assert.False(MuiSelectgroupActiveStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiSelectgroupActiveStateField.Magic, out _));
		Assert.False(MuiSelectgroupActiveStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void ScrollgroupRecordsUseDedicatedStructMemoryAdapters()
	{
		var platform = CreatePlatform();
		var borderAddress = APTR.FromPointer(0x1A00);
		var policyAddress = APTR.FromPointer(0x1A40);
		var viewportAddress = APTR.FromPointer(0x1A80);
		var border = new MuiScrollgroupBorderScrollerStateRecord
		{
			Magic = MuiScrollgroupBorderScrollerStateRecord.Cookie,
			Window = APTR.FromPointer(0x1800),
			UseWindowBorder = 1,
			HorizontalRequested = 1,
			VerticalRequested = 0,
			Applied = 1,
			Reserved = 0,
		};
		var policy = new MuiScrollgroupPolicyStateRecord
		{
			Magic = MuiScrollgroupPolicyStateRecord.Cookie,
			Contents = APTR.FromPointer(0x1840),
			FreeHorizontal = 1,
			FreeVertical = 0,
			HorizontalBar = APTR.FromPointer(0x1880),
			VerticalBar = APTR.Null,
			NoHorizontalBar = 0,
			NoVerticalBar = 1,
			AutoBars = 1,
			UseWindowBorder = 0,
		};
		var viewport = new MuiScrollgroupViewportStateRecord
		{
			Magic = MuiScrollgroupViewportStateRecord.Cookie,
			ViewportWidth = 100,
			ViewportHeight = 80,
			ContentWidth = 200,
			ContentHeight = 120,
			MaximumScrollX = 100,
			MaximumScrollY = 40,
			ScrollLeft = 25,
			ScrollTop = 10,
			HorizontalBarVisible = 1,
			VerticalBarVisible = 0,
		};
		Assert.True(MuiScrollgroupBorderScrollerStateRecordCodec.Write(ref platform,
			borderAddress, border));
		Assert.True(MuiScrollgroupPolicyStateRecordCodec.Write(ref platform,
			policyAddress, policy));
		Assert.True(MuiScrollgroupViewportStateRecordCodec.Write(ref platform,
			viewportAddress, viewport));
		Assert.True(MuiScrollgroupBorderScrollerStateRecordMemoryCodec.TryGetAddress(
			ref platform, borderAddress,
			MuiScrollgroupBorderScrollerStateField.Applied, out var appliedAddress));
		Assert.Equal(0x1A14u, appliedAddress.Raw);
		Assert.True(MuiScrollgroupPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, policyAddress, MuiScrollgroupPolicyStateField.VerticalBar,
			out var verticalBarAddress));
		Assert.Equal(0x1A54u, verticalBarAddress.Raw);
		Assert.True(MuiScrollgroupViewportStateRecordMemoryCodec.TryGetAddress(
			ref platform, viewportAddress, MuiScrollgroupViewportField.ScrollTop,
			out var scrollTopAddress));
		Assert.Equal(0x1AA0u, scrollTopAddress.Raw);
		Assert.True(MuiScrollgroupPolicyStateRecordMemoryCodec.TryReadUInt32(
			ref platform, policyAddress,
			MuiScrollgroupPolicyStateField.FreeHorizontal, out var freeHorizontal));
		Assert.Equal(1u, freeHorizontal);
		Assert.True(MuiScrollgroupViewportStateRecordMemoryCodec.TryReadUInt32(
			ref platform, viewportAddress, MuiScrollgroupViewportField.ScrollLeft,
			out var scrollLeft));
		Assert.Equal(25u, scrollLeft);
		Assert.True(MuiScrollgroupBorderScrollerStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, borderAddress,
			MuiScrollgroupBorderScrollerStateField.Applied, 0));
		Assert.True(MuiScrollgroupViewportStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, viewportAddress,
			MuiScrollgroupViewportField.VerticalBarVisible, 1));
		Assert.True(MuiScrollgroupBorderScrollerStateRecordCodec.TryReadStructural(
			ref platform, borderAddress, out var borderUpdated));
		Assert.Equal(0u, borderUpdated.Applied);
		Assert.True(MuiScrollgroupViewportStateRecordCodec.TryReadStructural(
			ref platform, viewportAddress, out var viewportUpdated));
		Assert.Equal(1u, viewportUpdated.VerticalBarVisible);
		Assert.False(MuiScrollgroupBorderScrollerStateRecordMemoryCodec.TryGetAddress(
			ref platform, borderAddress,
			(MuiScrollgroupBorderScrollerStateField)255, out _));
		Assert.False(MuiScrollgroupPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiScrollgroupPolicyStateField.Magic, out _));
		Assert.False(MuiScrollgroupViewportStateRecordMemoryCodec.TryGetAddress(
			ref platform, viewportAddress,
			(MuiScrollgroupViewportField)255, out _));
		Assert.False(MuiScrollgroupViewportStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform() =>
		new(0x1000, 0x20000, 0x4000, APTR.FromPointer(0x1000));
}
