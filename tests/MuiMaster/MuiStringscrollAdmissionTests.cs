using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringscrollAdmissionTests
{
	[Fact]
	public void StringscrollContentPolicyScrollbarAndCompositionRoundTrip()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var stateAddress = APTR.FromPointer(0x1500);
		var policyAddress = APTR.FromPointer(0x1540);
		var scrollbarAddress = APTR.FromPointer(0x1580);
		var compositionAddress = APTR.FromPointer(0x15A0);
		var state = new MuiStringscrollStateRecord
		{
			Magic = MuiStringscrollStateRecord.Cookie,
			String = APTR.FromPointer(0x1800),
			ContentWidth = 640,
			ContentHeight = 24,
			ScrollX = 17,
			ScrollY = 3,
		};
		var policy = new MuiStringscrollPolicyRecord
		{
			Magic = MuiStringscrollPolicyRecord.Cookie,
			HorizBar = 1,
			NoInput = 0,
			SetMin = 1,
			SetVMin = 0,
			UseWinBorder = 1,
			VertBar = 1,
			VertScrollerOnly = 0,
		};
		var scrollbar = new MuiStringscrollScrollbarRecord
		{
			Magic = MuiStringscrollScrollbarRecord.Cookie,
			HorizBar = APTR.FromPointer(0x1840),
			VertBar = APTR.FromPointer(0x1880),
		};
		var composition = new MuiStringscrollCompositionRecord
		{
			Magic = MuiStringscrollCompositionRecord.Cookie,
			Horizontal = scrollbar.HorizBar,
			Vertical = scrollbar.VertBar,
			OwnedMask = MuiStringscrollCompositionState.HorizontalOwned |
				MuiStringscrollCompositionState.VerticalOwned,
			LastHorizontalFirst = 5,
			LastVerticalFirst = 2,
		};

		Assert.True(MuiStringscrollStateRecordCodec.Write(ref platform,
			stateAddress, state));
		Assert.True(MuiStringscrollPolicyRecordCodec.Write(ref platform,
			policyAddress, policy));
		Assert.True(MuiStringscrollScrollbarRecordCodec.Write(ref platform,
			scrollbarAddress, scrollbar));
		Assert.True(MuiStringscrollCompositionRecordCodec.Write(ref platform,
			compositionAddress, composition));
		Assert.True(MuiStringscrollStateRecordCodec.TryRead(ref platform,
			stateAddress, out var readState));
		Assert.Equal(state.String, readState.String);
		Assert.Equal(state.ScrollX, readState.ScrollX);
		Assert.True(MuiStringscrollPolicyRecordCodec.TryRead(ref platform,
			policyAddress, out var readPolicy));
		Assert.Equal(policy.SetMin, readPolicy.SetMin);
		Assert.Equal(policy.VertBar, readPolicy.VertBar);
		Assert.True(MuiStringscrollScrollbarRecordCodec.TryRead(ref platform,
			scrollbarAddress, out var readScrollbar));
		Assert.Equal(scrollbar.VertBar, readScrollbar.VertBar);
		Assert.True(MuiStringscrollCompositionRecordCodec.TryRead(ref platform,
			compositionAddress, out var readComposition));
		Assert.Equal(composition.OwnedMask, readComposition.OwnedMask);
		Assert.Equal(composition.LastVerticalFirst, readComposition.LastVerticalFirst);
	}

	[Fact]
	public void StringscrollStateUsesNamedStructMemoryCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1A00);
		var state = new MuiStringscrollStateRecord
		{
			Magic = MuiStringscrollStateRecord.Cookie,
			String = APTR.FromPointer(0x1C00),
			ContentWidth = 800,
			ContentHeight = 32,
			ScrollX = 11,
			ScrollY = 7,
		};

		Assert.True(MuiStringscrollStateRecordCodec.Write(ref platform, address,
			state));
		var fieldCursor = new MuiStringscrollStateFieldCursor
		{
			Record = address,
			Field = MuiStringscrollStateField.String,
		};
		Assert.True(MuiStringscrollStateFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var cursorStringAddress));
		Assert.Equal(address.Raw + MuiStringscrollStateRecord.StringOffset,
			cursorStringAddress.Raw);
		Assert.True(MuiStringscrollStateFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var typedCursorStringAddress,
			out var typedCursorFieldSize));
		Assert.Equal(cursorStringAddress, typedCursorStringAddress);
		Assert.Equal(MuiStringscrollStateRecord.FieldSize, typedCursorFieldSize);
		Assert.True(MuiStringscrollStateMemoryCodec.TryGetAddress(ref platform,
			fieldCursor, out var memoryCursorStringAddress, out var memoryCursorFieldSize));
		Assert.Equal(typedCursorStringAddress, memoryCursorStringAddress);
		Assert.Equal(typedCursorFieldSize, memoryCursorFieldSize);
		fieldCursor.Field = MuiStringscrollStateField.ScrollY;
		Assert.True(MuiStringscrollStateFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var cursorScrollYAddress));
		Assert.Equal(address.Raw + MuiStringscrollStateRecord.ScrollYOffset,
			cursorScrollYAddress.Raw);
		Assert.True(MuiStringscrollStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiStringscrollStateField.ScrollX, 0));
		Assert.True(MuiStringscrollStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringscrollStateField.ContentHeight, out var cursorHeight));
		Assert.Equal(state.ContentHeight, cursorHeight);
		Assert.False(MuiStringscrollStateFieldCursorCodec.TryGetAddress(ref platform,
			new MuiStringscrollStateFieldCursor
			{
				Record = address,
				Field = (MuiStringscrollStateField)0xFF,
			}, out _));
		Assert.True(MuiStringscrollStateMemoryCodec.TryGetAddress(ref platform,
			address, MuiStringscrollStateField.ScrollY, out var scrollYAddress));
		Assert.Equal(address.Raw + MuiStringscrollStateRecord.ScrollYOffset,
			scrollYAddress.Raw);
		Assert.True(MuiStringscrollStateMemoryCodec.TryReadUInt32(ref platform,
			address, MuiStringscrollStateField.ContentWidth, out var width));
		Assert.Equal(state.ContentWidth, width);
		Assert.True(MuiStringscrollStateMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiStringscrollStateField.ScrollX, 19));
		Assert.True(MuiStringscrollStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(19u, decoded.ScrollX);
		Assert.False(MuiStringscrollStateMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiStringscrollStateField.Magic, out _));
		Assert.False(MuiStringscrollStateMemoryCodec.TryGetAddress(ref platform,
			address, (MuiStringscrollStateField)0xFF, out _));
		fieldCursor.Field = (MuiStringscrollStateField)0xFF;
		Assert.False(MuiStringscrollStateFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out _, out _));
		fieldCursor.Record = APTR.Null;
		fieldCursor.Field = MuiStringscrollStateField.String;
		Assert.False(MuiStringscrollStateFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out _, out _));
	}

	[Fact]
	public void StringscrollPolicyUsesNamedStructMemoryCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1B00);
		var policy = new MuiStringscrollPolicyRecord
		{
			Magic = MuiStringscrollPolicyRecord.Cookie,
			HorizBar = 1,
			NoInput = 0,
			SetMin = 1,
			SetVMin = 0,
			UseWinBorder = 1,
			VertBar = 1,
			VertScrollerOnly = 1,
		};

		Assert.True(MuiStringscrollPolicyRecordCodec.Write(ref platform, address,
			policy));
		var fieldCursor = new MuiStringscrollPolicyFieldCursor
		{
			Record = address,
			Field = MuiStringscrollPolicyField.UseWinBorder,
		};
		Assert.True(MuiStringscrollPolicyFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var cursorBorderAddress));
		Assert.Equal(address.Raw + MuiStringscrollPolicyRecord.UseWinBorderOffset,
			cursorBorderAddress.Raw);
		Assert.True(MuiStringscrollPolicyFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out var typedCursorBorderAddress, out var typedCursorFieldSize));
		Assert.Equal(cursorBorderAddress, typedCursorBorderAddress);
		Assert.Equal(MuiStringscrollPolicyRecord.FieldSize, typedCursorFieldSize);
		Assert.True(MuiStringscrollPolicyMemoryCodec.TryGetAddress(ref platform,
			fieldCursor, out var memoryCursorBorderAddress, out var memoryCursorFieldSize));
		Assert.Equal(typedCursorBorderAddress, memoryCursorBorderAddress);
		Assert.Equal(typedCursorFieldSize, memoryCursorFieldSize);
		fieldCursor.Field = MuiStringscrollPolicyField.VertScrollerOnly;
		Assert.True(MuiStringscrollPolicyFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var cursorScrollerAddress));
		Assert.Equal(address.Raw + MuiStringscrollPolicyRecord.VertScrollerOnlyOffset,
			cursorScrollerAddress.Raw);
		Assert.True(MuiStringscrollPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiStringscrollPolicyField.NoInput, 1));
		Assert.True(MuiStringscrollPolicyFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringscrollPolicyField.SetMin, out var cursorSetMin));
		Assert.Equal(policy.SetMin, cursorSetMin);
		Assert.False(MuiStringscrollPolicyFieldCursorCodec.TryGetAddress(
			ref platform, new MuiStringscrollPolicyFieldCursor
			{
				Record = address,
				Field = (MuiStringscrollPolicyField)0xFF,
			}, out _));
		Assert.True(MuiStringscrollPolicyMemoryCodec.TryGetAddress(ref platform,
			address, MuiStringscrollPolicyField.VertScrollerOnly,
			out var scrollerOnlyAddress));
		Assert.Equal(address.Raw + MuiStringscrollPolicyRecord.VertScrollerOnlyOffset,
			scrollerOnlyAddress.Raw);
		Assert.True(MuiStringscrollPolicyMemoryCodec.TryReadUInt32(ref platform,
			address, MuiStringscrollPolicyField.SetMin, out var setMin));
		Assert.Equal(policy.SetMin, setMin);
		Assert.True(MuiStringscrollPolicyMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiStringscrollPolicyField.NoInput, 1));
		Assert.True(MuiStringscrollPolicyRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(1u, decoded.NoInput);
		Assert.False(MuiStringscrollPolicyMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiStringscrollPolicyField.Magic, out _));
		Assert.False(MuiStringscrollPolicyMemoryCodec.TryGetAddress(ref platform,
			address, (MuiStringscrollPolicyField)0xFF, out _));
		fieldCursor.Field = (MuiStringscrollPolicyField)0xFF;
		Assert.False(MuiStringscrollPolicyFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out _, out _));
		fieldCursor.Record = APTR.Null;
		fieldCursor.Field = MuiStringscrollPolicyField.UseWinBorder;
		Assert.False(MuiStringscrollPolicyFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out _, out _));
	}

	[Fact]
	public void StringscrollScrollbarUsesNamedStructMemoryCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1C40);
		var scrollbar = new MuiStringscrollScrollbarRecord
		{
			Magic = MuiStringscrollScrollbarRecord.Cookie,
			HorizBar = APTR.FromPointer(0x1D00),
			VertBar = APTR.FromPointer(0x1D40),
		};

		Assert.True(MuiStringscrollScrollbarRecordCodec.Write(ref platform,
			address, scrollbar));
		var fieldCursor = new MuiStringscrollScrollbarFieldCursor
		{
			Record = address,
			Field = MuiStringscrollScrollbarField.HorizBar,
		};
		Assert.True(MuiStringscrollScrollbarFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var cursorHorizBarAddress));
		Assert.Equal(address.Raw + MuiStringscrollScrollbarRecord.HorizBarOffset,
			cursorHorizBarAddress.Raw);
		Assert.True(MuiStringscrollScrollbarFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out var typedCursorHorizBarAddress, out var typedCursorFieldSize));
		Assert.Equal(cursorHorizBarAddress, typedCursorHorizBarAddress);
		Assert.Equal(MuiStringscrollScrollbarRecord.FieldSize, typedCursorFieldSize);
		Assert.True(MuiStringscrollScrollbarMemoryCodec.TryGetAddress(ref platform,
			fieldCursor, out var memoryCursorHorizBarAddress, out var memoryCursorFieldSize));
		Assert.Equal(typedCursorHorizBarAddress, memoryCursorHorizBarAddress);
		Assert.Equal(typedCursorFieldSize, memoryCursorFieldSize);
		fieldCursor.Field = MuiStringscrollScrollbarField.VertBar;
		Assert.True(MuiStringscrollScrollbarFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var cursorVertBarAddress));
		Assert.Equal(address.Raw + MuiStringscrollScrollbarRecord.VertBarOffset,
			cursorVertBarAddress.Raw);
		Assert.True(MuiStringscrollScrollbarFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiStringscrollScrollbarField.HorizBar, 0));
		Assert.True(MuiStringscrollScrollbarFieldCursorCodec.TryReadUInt32(
				ref platform, address, MuiStringscrollScrollbarField.VertBar,
			out var cursorVertBar));
		Assert.Equal(scrollbar.VertBar.Raw, cursorVertBar);
		Assert.True(MuiStringscrollScrollbarFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiStringscrollScrollbarField.HorizBar,
			scrollbar.HorizBar.Raw));
		Assert.False(MuiStringscrollScrollbarFieldCursorCodec.TryGetAddress(
			ref platform, new MuiStringscrollScrollbarFieldCursor
			{
				Record = address,
				Field = (MuiStringscrollScrollbarField)0xFF,
			}, out _));
		Assert.True(MuiStringscrollScrollbarMemoryCodec.TryGetAddress(
			ref platform, address, MuiStringscrollScrollbarField.VertBar,
			out var vertBarAddress));
		Assert.Equal(address.Raw + MuiStringscrollScrollbarRecord.VertBarOffset,
			vertBarAddress.Raw);
		Assert.True(MuiStringscrollScrollbarMemoryCodec.TryReadUInt32(
			ref platform, address, MuiStringscrollScrollbarField.HorizBar,
			out var horizBar));
		Assert.Equal(scrollbar.HorizBar.Raw, horizBar);
		Assert.True(MuiStringscrollScrollbarMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiStringscrollScrollbarField.VertBar,
			0x1D80));
		Assert.True(MuiStringscrollScrollbarRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0x1D80u, decoded.VertBar.Raw);
		Assert.False(MuiStringscrollScrollbarMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiStringscrollScrollbarField.Magic, out _));
		Assert.False(MuiStringscrollScrollbarMemoryCodec.TryGetAddress(
			ref platform, address, (MuiStringscrollScrollbarField)0xFF, out _));
		fieldCursor.Field = (MuiStringscrollScrollbarField)0xFF;
		Assert.False(MuiStringscrollScrollbarFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out _, out _));
		fieldCursor.Record = APTR.Null;
		fieldCursor.Field = MuiStringscrollScrollbarField.VertBar;
		Assert.False(MuiStringscrollScrollbarFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out _, out _));
	}

	[Fact]
	public void StringscrollCompositionUsesNamedStructMemoryCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1D80);
		var composition = new MuiStringscrollCompositionRecord
		{
			Magic = MuiStringscrollCompositionRecord.Cookie,
			Horizontal = APTR.FromPointer(0x1E00),
			Vertical = APTR.FromPointer(0x1E40),
			OwnedMask = MuiStringscrollCompositionState.HorizontalOwned,
			LastHorizontalFirst = 4,
			LastVerticalFirst = 9,
		};

		Assert.True(MuiStringscrollCompositionRecordCodec.Write(ref platform,
			address, composition));
		var fieldCursor = new MuiStringscrollCompositionFieldCursor
		{
			Record = address,
			Field = MuiStringscrollCompositionField.Horizontal,
		};
		Assert.True(MuiStringscrollCompositionFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var cursorHorizontalAddress));
		Assert.Equal(address.Raw + MuiStringscrollCompositionRecord.HorizontalOffset,
			cursorHorizontalAddress.Raw);
		Assert.True(MuiStringscrollCompositionFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out var typedCursorHorizontalAddress, out var typedCursorFieldSize));
		Assert.Equal(cursorHorizontalAddress, typedCursorHorizontalAddress);
		Assert.Equal(MuiStringscrollCompositionRecord.FieldSize, typedCursorFieldSize);
		Assert.True(MuiStringscrollCompositionMemoryCodec.TryGetAddress(ref platform,
			fieldCursor, out var memoryCursorHorizontalAddress, out var memoryCursorFieldSize));
		Assert.Equal(typedCursorHorizontalAddress, memoryCursorHorizontalAddress);
		Assert.Equal(typedCursorFieldSize, memoryCursorFieldSize);
		fieldCursor.Field = MuiStringscrollCompositionField.LastVerticalFirst;
		Assert.True(MuiStringscrollCompositionFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var cursorLastAddress));
		Assert.Equal(address.Raw +
			MuiStringscrollCompositionRecord.LastVerticalFirstOffset,
			cursorLastAddress.Raw);
		Assert.True(MuiStringscrollCompositionFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiStringscrollCompositionField.OwnedMask, 0));
		Assert.True(MuiStringscrollCompositionFieldCursorCodec.TryReadUInt32(
			ref platform, address,
			MuiStringscrollCompositionField.LastHorizontalFirst, out var cursorFirst));
		Assert.Equal(composition.LastHorizontalFirst, cursorFirst);
		Assert.True(MuiStringscrollCompositionFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiStringscrollCompositionField.OwnedMask,
			composition.OwnedMask));
		Assert.False(MuiStringscrollCompositionFieldCursorCodec.TryGetAddress(
			ref platform, new MuiStringscrollCompositionFieldCursor
			{
				Record = address,
				Field = (MuiStringscrollCompositionField)0xFF,
			}, out _));
		Assert.True(MuiStringscrollCompositionMemoryCodec.TryGetAddress(
			ref platform, address, MuiStringscrollCompositionField.OwnedMask,
			out var ownedMaskAddress));
		Assert.Equal(address.Raw + MuiStringscrollCompositionRecord.OwnedMaskOffset,
			ownedMaskAddress.Raw);
		Assert.True(MuiStringscrollCompositionMemoryCodec.TryReadUInt32(
			ref platform, address, MuiStringscrollCompositionField.Vertical,
			out var vertical));
		Assert.Equal(composition.Vertical.Raw, vertical);
		Assert.True(MuiStringscrollCompositionMemoryCodec.TryWriteUInt32(
			ref platform, address,
			MuiStringscrollCompositionField.LastVerticalFirst, 12));
		Assert.True(MuiStringscrollCompositionRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(12u, decoded.LastVerticalFirst);
		Assert.False(MuiStringscrollCompositionMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiStringscrollCompositionField.Magic, out _));
		Assert.False(MuiStringscrollCompositionMemoryCodec.TryGetAddress(
			ref platform, address, (MuiStringscrollCompositionField)0xFF, out _));
		fieldCursor.Field = (MuiStringscrollCompositionField)0xFF;
		Assert.False(MuiStringscrollCompositionFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out _, out _));
		fieldCursor.Record = APTR.Null;
		fieldCursor.Field = MuiStringscrollCompositionField.Horizontal;
		Assert.False(MuiStringscrollCompositionFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out _, out _));
	}

	[Fact]
	public void StringscrollLayoutUsesNamedStructMemoryCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1E80);
		var layout = new MuiStringscrollLayoutStateRecord
		{
			Magic = MuiStringscrollLayoutStateRecord.Cookie,
			Left = -7,
			Top = 12,
			Width = 640,
			Height = 240,
		};

		Assert.True(MuiStringscrollLayoutStateRecordCodec.Write(ref platform,
			address, layout));
		var fieldCursor = new MuiStringscrollLayoutStateFieldCursor
		{
			Record = address,
			Field = MuiStringscrollLayoutStateField.Left,
		};
		Assert.True(MuiStringscrollLayoutStateFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var cursorLeftAddress));
		Assert.Equal(address.Raw + MuiStringscrollLayoutStateRecord.LeftOffset,
			cursorLeftAddress.Raw);
		Assert.True(MuiStringscrollLayoutStateFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out var typedCursorLeftAddress, out var typedCursorFieldSize));
		Assert.Equal(cursorLeftAddress, typedCursorLeftAddress);
		Assert.Equal(MuiStringscrollLayoutStateRecord.FieldSize, typedCursorFieldSize);
		Assert.True(MuiStringscrollLayoutStateMemoryCodec.TryGetAddress(ref platform,
			fieldCursor, out var memoryCursorLeftAddress, out var memoryCursorFieldSize));
		Assert.Equal(typedCursorLeftAddress, memoryCursorLeftAddress);
		Assert.Equal(typedCursorFieldSize, memoryCursorFieldSize);
		fieldCursor.Field = MuiStringscrollLayoutStateField.Height;
		Assert.True(MuiStringscrollLayoutStateFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var cursorHeightAddress));
		Assert.Equal(address.Raw + MuiStringscrollLayoutStateRecord.HeightOffset,
			cursorHeightAddress.Raw);
		Assert.True(MuiStringscrollLayoutStateFieldCursorCodec.TryWriteInt32(
			ref platform, address, MuiStringscrollLayoutStateField.Top, -19));
		Assert.True(MuiStringscrollLayoutStateFieldCursorCodec.TryReadInt32(
			ref platform, address, MuiStringscrollLayoutStateField.Width,
			out var cursorWidth));
		Assert.Equal(layout.Width, cursorWidth);
		Assert.True(MuiStringscrollLayoutStateFieldCursorCodec.TryWriteInt32(
			ref platform, address, MuiStringscrollLayoutStateField.Top, layout.Top));
		Assert.False(MuiStringscrollLayoutStateFieldCursorCodec.TryGetAddress(
			ref platform, new MuiStringscrollLayoutStateFieldCursor
			{
				Record = address,
				Field = (MuiStringscrollLayoutStateField)0xFF,
			}, out _));
		Assert.True(MuiStringscrollLayoutStateMemoryCodec.TryGetAddress(
			ref platform, address, MuiStringscrollLayoutStateField.Left,
			out var leftAddress));
		Assert.Equal(address.Raw + MuiStringscrollLayoutStateRecord.LeftOffset,
			leftAddress.Raw);
		Assert.True(MuiStringscrollLayoutStateMemoryCodec.TryReadInt32(
			ref platform, address, MuiStringscrollLayoutStateField.Left,
			out var left));
		Assert.Equal(layout.Left, left);
		Assert.True(MuiStringscrollLayoutStateMemoryCodec.TryWriteInt32(
			ref platform, address, MuiStringscrollLayoutStateField.Top, -19));
		Assert.True(MuiStringscrollLayoutStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(-19, decoded.Top);
		Assert.False(MuiStringscrollLayoutStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiStringscrollLayoutStateField.Magic, out _));
		Assert.False(MuiStringscrollLayoutStateMemoryCodec.TryGetAddress(
			ref platform, address, (MuiStringscrollLayoutStateField)0xFF, out _));
		fieldCursor.Field = (MuiStringscrollLayoutStateField)0xFF;
		Assert.False(MuiStringscrollLayoutStateFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out _, out _));
		fieldCursor.Record = APTR.Null;
		fieldCursor.Field = MuiStringscrollLayoutStateField.Left;
		Assert.False(MuiStringscrollLayoutStateFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out _, out _));
	}

	[Fact]
	public void StringscrollRenderUsesNamedStructMemoryCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1F40);
		var render = new MuiStringscrollRenderStateRecord
		{
			Magic = MuiStringscrollRenderStateRecord.Cookie,
			RenderInfo = APTR.FromPointer(0x2000),
			RastPort = APTR.FromPointer(0x2040),
			Font = APTR.FromPointer(0x2080),
		};

		Assert.True(MuiStringscrollRenderStateRecordCodec.Write(ref platform,
			address, render));
		var fieldCursor = new MuiStringscrollRenderStateFieldCursor
		{
			Record = address,
			Field = MuiStringscrollRenderStateField.RenderInfo,
		};
		Assert.True(MuiStringscrollRenderStateFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var cursorRenderInfoAddress));
		Assert.Equal(address.Raw + MuiStringscrollRenderStateRecord.RenderInfoOffset,
			cursorRenderInfoAddress.Raw);
		Assert.True(MuiStringscrollRenderStateFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out var typedCursorRenderInfoAddress, out var typedCursorFieldSize));
		Assert.Equal(cursorRenderInfoAddress, typedCursorRenderInfoAddress);
		Assert.Equal(MuiStringscrollRenderStateRecord.FieldSize, typedCursorFieldSize);
		Assert.True(MuiStringscrollRenderStateMemoryCodec.TryGetAddress(ref platform,
			fieldCursor, out var memoryCursorRenderInfoAddress, out var memoryCursorFieldSize));
		Assert.Equal(typedCursorRenderInfoAddress, memoryCursorRenderInfoAddress);
		Assert.Equal(typedCursorFieldSize, memoryCursorFieldSize);
		fieldCursor.Field = MuiStringscrollRenderStateField.Font;
		Assert.True(MuiStringscrollRenderStateFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var cursorFontAddress));
		Assert.Equal(address.Raw + MuiStringscrollRenderStateRecord.FontOffset,
			cursorFontAddress.Raw);
		Assert.True(MuiStringscrollRenderStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiStringscrollRenderStateField.Font, 0));
		Assert.True(MuiStringscrollRenderStateFieldCursorCodec.TryReadUInt32(
			ref platform, address, MuiStringscrollRenderStateField.RastPort,
			out var cursorRastPort));
		Assert.Equal(render.RastPort.Raw, cursorRastPort);
		Assert.True(MuiStringscrollRenderStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiStringscrollRenderStateField.Font,
			render.Font.Raw));
		Assert.False(MuiStringscrollRenderStateFieldCursorCodec.TryGetAddress(
			ref platform, new MuiStringscrollRenderStateFieldCursor
			{
				Record = address,
				Field = (MuiStringscrollRenderStateField)0xFF,
			}, out _));
		Assert.True(MuiStringscrollRenderStateMemoryCodec.TryGetAddress(
			ref platform, address, MuiStringscrollRenderStateField.Font,
			out var fontAddress));
		Assert.Equal(address.Raw + MuiStringscrollRenderStateRecord.FontOffset,
			fontAddress.Raw);
		Assert.True(MuiStringscrollRenderStateMemoryCodec.TryReadUInt32(
			ref platform, address, MuiStringscrollRenderStateField.RenderInfo,
			out var renderInfo));
		Assert.Equal(render.RenderInfo.Raw, renderInfo);
		Assert.True(MuiStringscrollRenderStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiStringscrollRenderStateField.RastPort,
			0x20C0));
		Assert.True(MuiStringscrollRenderStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(0x20C0u, decoded.RastPort.Raw);
		Assert.False(MuiStringscrollRenderStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiStringscrollRenderStateField.Magic, out _));
		Assert.False(MuiStringscrollRenderStateMemoryCodec.TryGetAddress(
			ref platform, address, (MuiStringscrollRenderStateField)0xFF, out _));
		fieldCursor.Field = (MuiStringscrollRenderStateField)0xFF;
		Assert.False(MuiStringscrollRenderStateFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out _, out _));
		fieldCursor.Record = APTR.Null;
		fieldCursor.Field = MuiStringscrollRenderStateField.Font;
		Assert.False(MuiStringscrollRenderStateFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out _, out _));
	}

	[Fact]
	public void StringscrollViewportUsesNamedStructMemoryCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x20C0);
		var viewport = new MuiStringscrollViewportStateRecord
		{
			Magic = MuiStringscrollViewportStateRecord.Cookie,
			ViewportWidth = -3,
			ViewportHeight = 80,
			HorizontalVisible = 1,
			VerticalVisible = 0,
			MaxScrollX = 220,
			MaxScrollY = 100,
		};

		Assert.True(MuiStringscrollViewportStateRecordCodec.Write(ref platform,
			address, viewport));
		var fieldCursor = new MuiStringscrollViewportStateFieldCursor
		{
			Record = address,
			Field = MuiStringscrollViewportStateField.Magic,
		};
		Assert.True(MuiStringscrollViewportStateFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var cursorMagicAddress));
		Assert.Equal(address.Raw, cursorMagicAddress.Raw);
		Assert.True(MuiStringscrollViewportStateFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out var typedCursorMagicAddress, out var typedCursorFieldSize));
		Assert.Equal(cursorMagicAddress, typedCursorMagicAddress);
		Assert.Equal(MuiStringscrollViewportStateRecord.FieldSize, typedCursorFieldSize);
		Assert.True(MuiStringscrollViewportStateMemoryCodec.TryGetAddress(ref platform,
			fieldCursor, out var memoryCursorMagicAddress, out var memoryCursorFieldSize));
		Assert.Equal(typedCursorMagicAddress, memoryCursorMagicAddress);
		Assert.Equal(typedCursorFieldSize, memoryCursorFieldSize);
		fieldCursor.Field = MuiStringscrollViewportStateField.MaxScrollY;
		Assert.True(MuiStringscrollViewportStateFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var cursorMaxScrollYAddress));
		Assert.Equal(address.Raw + MuiStringscrollViewportStateRecord.MaxScrollYOffset,
			cursorMaxScrollYAddress.Raw);
		Assert.True(MuiStringscrollViewportStateMemoryCodec.TryGetAddress(
			ref platform, address, MuiStringscrollViewportStateField.MaxScrollY,
			out var maxScrollYAddress));
		Assert.Equal(address.Raw + MuiStringscrollViewportStateRecord.MaxScrollYOffset,
			maxScrollYAddress.Raw);
		Assert.True(MuiStringscrollViewportStateMemoryCodec.TryReadInt32(
			ref platform, address, MuiStringscrollViewportStateField.ViewportWidth,
			out var width));
		Assert.Equal(viewport.ViewportWidth, width);
		Assert.True(MuiStringscrollViewportStateMemoryCodec.TryWriteUInt32(
			ref platform, address,
			MuiStringscrollViewportStateField.MaxScrollX, 300));
		Assert.True(MuiStringscrollViewportStateFieldCursorCodec.TryReadUInt32(
			ref platform, address, MuiStringscrollViewportStateField.MaxScrollX,
			out var cursorMaxScrollX));
		Assert.Equal(300u, cursorMaxScrollX);
		Assert.True(MuiStringscrollViewportStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(300u, decoded.MaxScrollX);
		Assert.False(MuiStringscrollViewportStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiStringscrollViewportStateField.Magic,
			out _));
		Assert.False(MuiStringscrollViewportStateMemoryCodec.TryGetAddress(
			ref platform, address, (MuiStringscrollViewportStateField)0xFF,
			out _));
		fieldCursor.Field = (MuiStringscrollViewportStateField)0xFF;
		Assert.False(MuiStringscrollViewportStateFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out _, out _));
		fieldCursor.Record = APTR.Null;
		fieldCursor.Field = MuiStringscrollViewportStateField.MaxScrollY;
		Assert.False(MuiStringscrollViewportStateFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out _, out _));
	}

	[Fact]
	public void MalformedStringscrollCookiesRemainStructuralButFailClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var stateAddress = APTR.FromPointer(0x1700);
		var policyAddress = APTR.FromPointer(0x1740);
		var scrollbarAddress = APTR.FromPointer(0x1780);
		var compositionAddress = APTR.FromPointer(0x17A0);
		Assert.True(MuiStringscrollStateRecordCodec.Write(ref platform,
			stateAddress, new MuiStringscrollStateRecord
			{
				Magic = MuiStringscrollStateRecord.Cookie,
				ContentWidth = 1,
				ContentHeight = 2,
			}));
		Assert.True(MuiStringscrollPolicyRecordCodec.Write(ref platform,
			policyAddress, new MuiStringscrollPolicyRecord
			{
				Magic = MuiStringscrollPolicyRecord.Cookie,
			}));
		Assert.True(MuiStringscrollScrollbarRecordCodec.Write(ref platform,
			scrollbarAddress, new MuiStringscrollScrollbarRecord
			{
				Magic = MuiStringscrollScrollbarRecord.Cookie,
			}));
		Assert.True(MuiStringscrollCompositionRecordCodec.Write(ref platform,
			compositionAddress, new MuiStringscrollCompositionRecord
			{
				Magic = MuiStringscrollCompositionRecord.Cookie,
			}));
		Assert.True(MuiStringscrollStateFieldCursorCodec.TryWriteUInt32(ref platform,
			stateAddress, MuiStringscrollStateField.Magic, 0));
		Assert.True(MuiStringscrollPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
			policyAddress, MuiStringscrollPolicyField.Magic, 0));
		Assert.True(MuiStringscrollScrollbarFieldCursorCodec.TryWriteUInt32(ref platform,
			scrollbarAddress, MuiStringscrollScrollbarField.Magic, 0));
		Assert.True(MuiStringscrollCompositionFieldCursorCodec.TryWriteUInt32(
			ref platform, compositionAddress, MuiStringscrollCompositionField.Magic, 0));

		Assert.True(MuiStringscrollStateRecordCodec.TryReadStructural(ref platform,
			stateAddress, out var state));
		Assert.Equal(0u, state.Magic);
		Assert.False(MuiStringscrollStateRecordCodec.TryRead(ref platform,
			stateAddress, out _));
		Assert.True(MuiStringscrollPolicyRecordCodec.TryReadStructural(ref platform,
			policyAddress, out var policy));
		Assert.Equal(0u, policy.Magic);
		Assert.False(MuiStringscrollPolicyRecordCodec.TryRead(ref platform,
			policyAddress, out _));
		Assert.True(MuiStringscrollScrollbarRecordCodec.TryReadStructural(ref platform,
			scrollbarAddress, out var scrollbar));
		Assert.Equal(0u, scrollbar.Magic);
		Assert.False(MuiStringscrollScrollbarRecordCodec.TryRead(ref platform,
			scrollbarAddress, out _));
		Assert.True(MuiStringscrollCompositionRecordCodec.TryReadStructural(ref platform,
			compositionAddress, out var composition));
		Assert.Equal(0u, composition.Magic);
		Assert.False(MuiStringscrollCompositionRecordCodec.TryRead(ref platform,
			compositionAddress, out _));
	}

	[Fact]
	public void StringscrollPointerRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var recordAddress = APTR.FromPointer(0x1D20);
		var value = new MuiStringscrollPointerState
		{
			Magic = MuiStringscrollPointerState.Cookie,
			Axis = MuiStringscrollPointerState.HorizontalAxis,
			GrabOffset = -3,
			StartScroll = 17,
			StartX = 10,
			StartY = 20,
			LastPointer = 30,
			Flags = MuiStringscrollPointerState.ActiveFlag |
				MuiStringscrollPointerState.CapturedFlag,
		};
		Assert.True(MuiStringscrollPointerStateCodec.Write(ref platform,
			recordAddress, value));
		var fieldCursor = new MuiStringscrollPointerStateFieldCursor
		{
			Address = recordAddress,
			Field = MuiStringscrollPointerStateField.LastPointer,
		};
		Assert.True(MuiStringscrollPointerStateFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var cursorPointerAddress));
		Assert.Equal(0x1D38u, cursorPointerAddress.Raw);
		Assert.True(MuiStringscrollPointerStateFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var typedCursorPointerAddress,
			out var typedCursorFieldSize));
		Assert.Equal(cursorPointerAddress, typedCursorPointerAddress);
		Assert.Equal(MuiStringscrollPointerState.FieldSize, typedCursorFieldSize);
		Assert.True(MuiStringscrollPointerStateRecordMemoryCodec.TryGetAddress(ref platform,
			fieldCursor, out var memoryCursorPointerAddress, out var memoryCursorFieldSize));
		Assert.Equal(typedCursorPointerAddress, memoryCursorPointerAddress);
		Assert.Equal(typedCursorFieldSize, memoryCursorFieldSize);
		fieldCursor.Field = MuiStringscrollPointerStateField.GrabOffset;
		Assert.True(MuiStringscrollPointerStateFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var cursorGrabAddress));
		Assert.Equal(0x1D28u, cursorGrabAddress.Raw);
		Assert.True(MuiStringscrollPointerStateFieldCursorCodec.TryWrite(ref platform,
			recordAddress, MuiStringscrollPointerStateField.LastPointer, 0));
		Assert.True(MuiStringscrollPointerStateFieldCursorCodec.TryRead(ref platform,
			recordAddress, MuiStringscrollPointerStateField.GrabOffset,
			out var cursorGrabRaw));
		Assert.Equal(unchecked((uint)-3), cursorGrabRaw);
		Assert.False(MuiStringscrollPointerStateFieldCursorCodec.TryGetAddress(
			ref platform, new MuiStringscrollPointerStateFieldCursor
			{
				Address = recordAddress,
				Field = (MuiStringscrollPointerStateField)255,
			}, out _));
		Assert.True(MuiStringscrollPointerStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress, MuiStringscrollPointerStateField.LastPointer,
			out var typedPointerAddress));
		Assert.Equal(0x1D38u, typedPointerAddress.Raw);
		Assert.True(MuiStringscrollPointerStateRecordMemoryCodec.TryReadInt32(
			ref platform, recordAddress, MuiStringscrollPointerStateField.GrabOffset,
			out var typedGrabOffset));
		Assert.Equal(-3, typedGrabOffset);
		Assert.True(MuiStringscrollPointerStateRecordMemoryCodec.TryWriteInt32(
			ref platform, recordAddress, MuiStringscrollPointerStateField.LastPointer, 0));
		Assert.True(MuiStringscrollPointerStateCodec.TryReadStructural(ref platform,
			recordAddress, out var typedUpdated));
		Assert.Equal(0, typedUpdated.LastPointer);
		Assert.Equal(value.StartX, typedUpdated.StartX);
		Assert.Equal(value.Flags, typedUpdated.Flags);
		Assert.True(MuiStringscrollPointerStateRecordMemoryCodec.TryWriteInt32(
			ref platform, recordAddress, MuiStringscrollPointerStateField.LastPointer,
			value.LastPointer));
		Assert.False(MuiStringscrollPointerStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, (MuiStringscrollPointerStateField)255,
			out _));
		Assert.True(MuiStringscrollPointerStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress, 24, out var pointerAddress));
		Assert.Equal(0x1D38u, pointerAddress.Raw);
		Assert.True(MuiStringscrollPointerStateRecordMemoryCodec.TryReadInt32(
			ref platform, recordAddress, 8, out var grabOffset));
		Assert.Equal(-3, grabOffset);
		Assert.True(MuiStringscrollPointerStateRecordMemoryCodec.TryWriteInt32(
			ref platform, recordAddress, 24, 0));
		Assert.True(MuiStringscrollPointerStateCodec.TryReadStructural(ref platform,
			recordAddress, out var decoded));
		Assert.Equal(0, decoded.LastPointer);
		Assert.False(MuiStringscrollPointerStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress, MuiStringscrollPointerState.Size, out _));
		Assert.False(MuiStringscrollPointerStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, (uint)0, out _));
		Assert.False(MuiStringscrollPointerStateCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}
}
