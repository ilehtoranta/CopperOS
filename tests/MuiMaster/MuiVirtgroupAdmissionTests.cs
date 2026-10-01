using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiVirtgroupAdmissionTests
{
	[Fact]
	public void VirtgroupLayoutInputAndPolicyRecordsRoundTripThroughNamedStructs()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var layoutAddress = APTR.FromPointer(0x1500);
		var scrollAddress = APTR.FromPointer(0x1540);
		var displayAddress = APTR.FromPointer(0x1580);
		var pointerAddress = APTR.FromPointer(0x15A0);
		var policyAddress = APTR.FromPointer(0x15E0);
		var layout = new MuiVirtgroupLayoutStateRecord
		{
			Magic = MuiVirtgroupLayoutStateRecord.Cookie,
			Width = 320,
			Height = 180,
			Left = 12,
			Top = 7,
			TryFit = 1,
		};
		var scroll = new MuiScrollgroupLayoutStateRecord
		{
			Magic = MuiScrollgroupLayoutStateRecord.Cookie,
			Contents = APTR.FromPointer(0x1600),
			FreeHorizontal = 1,
			FreeVertical = 1,
			HorizontalBar = APTR.FromPointer(0x1640),
			VerticalBar = APTR.FromPointer(0x1680),
			NoHorizontalBar = 0,
			NoVerticalBar = 1,
		};
		var display = new MuiVirtgroupDisplayStateRecord
		{
			Magic = MuiVirtgroupDisplayStateRecord.Cookie,
			Left = 10,
			Top = 15,
			Width = 100,
			Height = 80,
		};
		var pointer = new MuiVirtgroupPointerStateRecord
		{
			Magic = MuiVirtgroupPointerStateRecord.Cookie,
			Flags = MuiVirtgroupPointerStateRecord.ActiveFlag |
				MuiVirtgroupPointerStateRecord.CapturedFlag,
			StartX = 50,
			StartY = 40,
			StartLeft = 30,
			StartTop = 20,
			LastX = 42,
			LastY = 31,
		};
		var policy = new MuiVirtgroupPolicyStateRecord
		{
			Magic = MuiVirtgroupPolicyStateRecord.Cookie,
			Input = 1,
			Width = 300,
			Height = 180,
			Left = -15,
			Top = 20,
			TryFit = 0,
		};

		Assert.True(MuiVirtgroupLayoutStateRecordCodec.Write(ref platform,
			layoutAddress, layout));
		Assert.True(MuiScrollgroupLayoutStateRecordCodec.Write(ref platform,
			scrollAddress, scroll));
		Assert.True(MuiVirtgroupDisplayStateRecordCodec.Write(ref platform,
			displayAddress, display));
		Assert.True(MuiVirtgroupPointerStateRecordCodec.Write(ref platform,
			pointerAddress, pointer));
		Assert.True(MuiVirtgroupPolicyStateRecordCodec.Write(ref platform,
			policyAddress, policy));

		Assert.True(MuiVirtgroupLayoutStateRecordCodec.TryRead(ref platform,
			layoutAddress, out var readLayout));
		Assert.Equal(layout.Width, readLayout.Width);
		Assert.Equal(layout.Left, readLayout.Left);
		Assert.True(MuiScrollgroupLayoutStateRecordCodec.TryRead(ref platform,
			scrollAddress, out var readScroll));
		Assert.Equal(scroll.Contents, readScroll.Contents);
		Assert.Equal(scroll.NoVerticalBar, readScroll.NoVerticalBar);
		Assert.True(MuiVirtgroupDisplayStateRecordCodec.TryRead(ref platform,
			displayAddress, out var readDisplay));
		Assert.Equal(display.Width, readDisplay.Width);
		Assert.True(MuiVirtgroupPointerStateRecordCodec.TryRead(ref platform,
			pointerAddress, out var readPointer));
		Assert.Equal(pointer.StartLeft, readPointer.StartLeft);
		Assert.Equal(pointer.Flags, readPointer.Flags);
		Assert.True(MuiVirtgroupPolicyStateRecordCodec.TryRead(ref platform,
			policyAddress, out var readPolicy));
		Assert.Equal(policy.Left, readPolicy.Left);
		Assert.Equal(policy.Input, readPolicy.Input);
	}

	[Fact]
	public void MalformedVirtgroupRecordsRemainStructuralButFailClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var layoutAddress = APTR.FromPointer(0x1700);
		var scrollAddress = APTR.FromPointer(0x1740);
		var displayAddress = APTR.FromPointer(0x1780);
		var pointerAddress = APTR.FromPointer(0x17A0);
		var policyAddress = APTR.FromPointer(0x17E0);
		Assert.True(MuiVirtgroupLayoutStateRecordCodec.Write(ref platform,
			layoutAddress, new MuiVirtgroupLayoutStateRecord
			{
				Magic = MuiVirtgroupLayoutStateRecord.Cookie,
				Width = 1, Height = 2, Left = 3, Top = 4, TryFit = 0,
			}));
		Assert.True(MuiScrollgroupLayoutStateRecordCodec.Write(ref platform,
			scrollAddress, new MuiScrollgroupLayoutStateRecord
			{
				Magic = MuiScrollgroupLayoutStateRecord.Cookie,
				Contents = APTR.FromPointer(0x1800),
				HorizontalBar = APTR.FromPointer(0x1840),
				VerticalBar = APTR.FromPointer(0x1880),
			}));
		Assert.True(MuiVirtgroupDisplayStateRecordCodec.Write(ref platform,
			displayAddress, new MuiVirtgroupDisplayStateRecord
			{
				Magic = MuiVirtgroupDisplayStateRecord.Cookie,
				Width = 1, Height = 2,
			}));
		Assert.True(MuiVirtgroupPointerStateRecordCodec.Write(ref platform,
			pointerAddress, new MuiVirtgroupPointerStateRecord
			{
				Magic = MuiVirtgroupPointerStateRecord.Cookie,
				Flags = MuiVirtgroupPointerStateRecord.ActiveFlag,
			}));
		Assert.True(MuiVirtgroupPolicyStateRecordCodec.Write(ref platform,
			policyAddress, new MuiVirtgroupPolicyStateRecord
			{
				Magic = MuiVirtgroupPolicyStateRecord.Cookie,
				Input = 1,
			}));

		Assert.True(MuiVirtgroupLayoutFieldCursorCodec.TryWriteUInt32(ref platform,
			layoutAddress, MuiVirtgroupLayoutField.Magic, 0));
		Assert.True(MuiScrollgroupLayoutFieldCursorCodec.TryWriteUInt32(ref platform,
			scrollAddress, MuiScrollgroupLayoutField.Magic, 0));
		Assert.True(MuiVirtgroupInputFieldCursorCodec.TryWriteUInt32(ref platform,
			displayAddress, MuiVirtgroupInputRecordKind.Display,
			MuiVirtgroupInputField.Magic, 0));
		Assert.True(MuiVirtgroupInputFieldCursorCodec.TryWriteUInt32(ref platform,
			pointerAddress, MuiVirtgroupInputRecordKind.Pointer,
			MuiVirtgroupInputField.Magic, 0));
		Assert.True(MuiVirtgroupPolicyStateFieldCursorCodec.TryWriteUInt32(
			ref platform, policyAddress, MuiVirtgroupPolicyStateField.Magic, 0));

		Assert.True(MuiVirtgroupLayoutStateRecordCodec.TryReadStructural(ref platform,
			layoutAddress, out var structuralLayout));
		Assert.Equal(0u, structuralLayout.Magic);
		Assert.False(MuiVirtgroupLayoutStateRecordCodec.TryRead(ref platform,
			layoutAddress, out _));
		Assert.True(MuiScrollgroupLayoutStateRecordCodec.TryReadStructural(ref platform,
			scrollAddress, out var structuralScroll));
		Assert.Equal(0u, structuralScroll.Magic);
		Assert.False(MuiScrollgroupLayoutStateRecordCodec.TryRead(ref platform,
			scrollAddress, out _));
		Assert.True(MuiVirtgroupDisplayStateRecordCodec.TryReadStructural(ref platform,
			displayAddress, out var structuralDisplay));
		Assert.Equal(0u, structuralDisplay.Magic);
		Assert.False(MuiVirtgroupDisplayStateRecordCodec.TryRead(ref platform,
			displayAddress, out _));
		Assert.True(MuiVirtgroupPointerStateRecordCodec.TryReadStructural(ref platform,
			pointerAddress, out var structuralPointer));
		Assert.Equal(0u, structuralPointer.Magic);
		Assert.False(MuiVirtgroupPointerStateRecordCodec.TryRead(ref platform,
			pointerAddress, out _));
		Assert.True(MuiVirtgroupPolicyStateRecordCodec.TryReadStructural(ref platform,
			policyAddress, out var structuralPolicy));
		Assert.Equal(0u, structuralPolicy.Magic);
		Assert.False(MuiVirtgroupPolicyStateRecordCodec.TryRead(ref platform,
			policyAddress, out _));
	}

	[Fact]
	public void VirtgroupInputAndPolicyRecordsUseDedicatedStructMemoryAdapters()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var displayAddress = APTR.FromPointer(0x1A00);
		var pointerAddress = APTR.FromPointer(0x1A30);
		var policyAddress = APTR.FromPointer(0x1A60);
		Assert.True(MuiVirtgroupDisplayStateRecordCodec.Write(ref platform,
			displayAddress, new MuiVirtgroupDisplayStateRecord
			{
				Magic = MuiVirtgroupDisplayStateRecord.Cookie,
				Left = -12, Top = 8, Width = 100, Height = 60,
			}));
		Assert.True(MuiVirtgroupPointerStateRecordCodec.Write(ref platform,
			pointerAddress, new MuiVirtgroupPointerStateRecord
			{
				Magic = MuiVirtgroupPointerStateRecord.Cookie,
				Flags = MuiVirtgroupPointerStateRecord.ActiveFlag,
				StartX = 20, StartY = 10, StartLeft = -12, StartTop = 8,
				LastX = 24, LastY = 14,
			}));
		Assert.True(MuiVirtgroupPolicyStateRecordCodec.Write(ref platform,
			policyAddress, new MuiVirtgroupPolicyStateRecord
			{
				Magic = MuiVirtgroupPolicyStateRecord.Cookie,
				Input = 1, Width = 100, Height = 60, Left = -12, Top = 8,
				TryFit = 0,
			}));
		Assert.True(MuiVirtgroupInputRecordMemoryCodec.TryGetAddress(ref platform,
			displayAddress, MuiVirtgroupInputRecordKind.Display,
			MuiVirtgroupInputField.Height,
			out var heightAddress, out var inputFieldSize));
		Assert.Equal(0x1A10u, heightAddress.Raw);
		Assert.Equal(4u, inputFieldSize);
		Assert.True(MuiVirtgroupInputRecordMemoryCodec.TryGetAddress(ref platform,
			pointerAddress, MuiVirtgroupInputRecordKind.Pointer,
			MuiVirtgroupInputField.LastY,
			out var lastYAddress));
		Assert.Equal(0x1A4Cu, lastYAddress.Raw);
		Assert.True(MuiVirtgroupPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, policyAddress, MuiVirtgroupPolicyStateField.TryFit,
			out var tryFitAddress));
		Assert.Equal(0x1A78u, tryFitAddress.Raw);
		var policyCursor = new MuiVirtgroupPolicyStateFieldCursor
		{
			Record = policyAddress,
			Field = MuiVirtgroupPolicyStateField.TryFit,
		};
		Assert.True(MuiVirtgroupPolicyStateFieldCursorCodec.TryGetAddress(
			ref platform, policyCursor, out var cursorTryFitAddress,
			out var fieldSize));
		Assert.Equal(tryFitAddress, cursorTryFitAddress);
		Assert.Equal(MuiVirtgroupPolicyStateRecord.FieldSize, fieldSize);
		Assert.True(MuiVirtgroupPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, policyCursor, out var memoryCursorAddress,
			out var memoryFieldSize));
		Assert.Equal(tryFitAddress, memoryCursorAddress);
		Assert.Equal(MuiVirtgroupPolicyStateRecord.FieldSize, memoryFieldSize);
		Assert.True(MuiVirtgroupInputRecordMemoryCodec.TryReadUInt32(ref platform,
			displayAddress, MuiVirtgroupInputRecordKind.Display,
			MuiVirtgroupInputField.Left,
			out var left));
		Assert.Equal(unchecked((uint)-12), left);
		Assert.True(MuiVirtgroupInputRecordMemoryCodec.TryWriteUInt32(ref platform,
			pointerAddress, MuiVirtgroupInputRecordKind.Pointer,
			MuiVirtgroupInputField.Flags, 0));
		Assert.True(MuiVirtgroupPolicyStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, policyAddress, MuiVirtgroupPolicyStateField.TryFit, 1));
		Assert.True(MuiVirtgroupPointerStateRecordCodec.TryReadStructural(ref platform,
			pointerAddress, out var pointer));
		Assert.Equal(0u, pointer.Flags);
		Assert.True(MuiVirtgroupPolicyStateRecordCodec.TryReadStructural(ref platform,
			policyAddress, out var policy));
		Assert.Equal(1u, policy.TryFit);
		Assert.False(MuiVirtgroupInputRecordMemoryCodec.TryGetAddress(ref platform,
			displayAddress, MuiVirtgroupInputRecordKind.Display,
			(MuiVirtgroupInputField)255, out _));
		Assert.False(MuiVirtgroupInputRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiVirtgroupInputRecordKind.Pointer,
			MuiVirtgroupInputField.Magic, out _));
		Assert.False(MuiVirtgroupPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, policyAddress, (MuiVirtgroupPolicyStateField)255, out _));
		policyCursor.Record = APTR.Null;
		policyCursor.Field = MuiVirtgroupPolicyStateField.Magic;
		Assert.False(MuiVirtgroupPolicyStateFieldCursorCodec.TryGetAddress(
			ref platform, policyCursor, out _, out _));
		Assert.False(MuiVirtgroupPolicyStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}
}
