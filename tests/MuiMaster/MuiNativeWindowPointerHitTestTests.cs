using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeWindowPointerHitTestTests
{
	[Fact]
	public void PointerPositionCopiesNamedWindowMouseCoordinates()
	{
		var window = new Window
		{
			MouseX = -123,
			MouseY = 234,
		};

		var pointer = MuiNativePointerPosition.FromWindow(window);

		Assert.Equal(-123, pointer.X);
		Assert.Equal(234, pointer.Y);
	}

	[Fact]
	public void PointerPositionCopiesNamedIntuiMessageCoordinates()
	{
		var message = new MuiIntuiPointerMessage
		{
			MouseX = -45,
			MouseY = 67,
		};

		var pointer = MuiNativePointerPosition.FromMessage(message);

		Assert.Equal(-45, pointer.X);
		Assert.Equal(67, pointer.Y);
	}

	[Theory]
	[InlineData(0u, 1u, false)]
	[InlineData(1u, 0u, true)]
	[InlineData(3u, 3u, false)]
	public void PointerOverlapOrderingUsesCurrentGroupChildPosition(
		uint candidatePosition, uint incumbentPosition, bool expected)
	{
		Assert.Equal(expected, MuiNativeGuiMode.IsLaterGroupChildPosition(
			candidatePosition, incumbentPosition));
	}

	[Fact]
	public void PointerHitTestContainsSignedPointWithinNamedAreaRectangle()
	{
		var rectangle = new MuiNativeAreaRectangle
		{
			Left = -20,
			Top = -12,
			Width = 40,
			Height = 24,
		};

		Assert.True(MuiNativeGuiMode.ContainsPoint(rectangle, -1, -1));
		Assert.True(MuiNativeGuiMode.ContainsPoint(rectangle, -20, -12));
	}

	[Theory]
	[InlineData(20, 0)]
	[InlineData(0, 12)]
	[InlineData(-21, 0)]
	[InlineData(0, -13)]
	public void PointerHitTestUsesHalfOpenAreaBounds(int x, int y)
	{
		var rectangle = new MuiNativeAreaRectangle
		{
			Left = -20,
			Top = -12,
			Width = 40,
			Height = 24,
		};

		Assert.False(MuiNativeGuiMode.ContainsPoint(rectangle, x, y));
	}

	[Fact]
	public void PointerHitTestRejectsEmptyOrNegativeAreaSize()
	{
		Assert.False(MuiNativeGuiMode.ContainsPoint(new MuiNativeAreaRectangle
		{
			Width = 0,
			Height = 10,
		}, 0, 0));
		Assert.False(MuiNativeGuiMode.ContainsPoint(new MuiNativeAreaRectangle
		{
			Width = 10,
			Height = -1,
		}, 0, 0));
	}

	[Fact]
	public void PointerHitTestHandlesSignedCoordinateRangeWithoutWidenedArithmetic()
	{
		var rectangle = new MuiNativeAreaRectangle
		{
			Left = int.MinValue,
			Top = int.MinValue,
			Width = int.MaxValue,
			Height = int.MaxValue,
		};

		Assert.True(MuiNativeGuiMode.ContainsPoint(rectangle, -2, -2));
		Assert.False(MuiNativeGuiMode.ContainsPoint(rectangle, -1, -1));
	}

	[Fact]
	public void VirtualGroupClipIntersectionKeepsNamedSignedRectangle()
	{
		var first = new MuiNativeAreaRectangle
		{
			Left = -10,
			Top = -6,
			Width = 16,
			Height = 12,
		};
		var second = new MuiNativeAreaRectangle
		{
			Left = 0,
			Top = -2,
			Width = 12,
			Height = 8,
		};

		Assert.True(MuiNativeGuiMode.TryIntersectRectangles(first, second,
			out var intersection));
		Assert.Equal(0, intersection.Left);
		Assert.Equal(-2, intersection.Top);
		Assert.Equal(6, intersection.Width);
		Assert.Equal(8, intersection.Height);
	}

	[Fact]
	public void VirtualGroupClipIntersectionHandlesSignedExtremes()
	{
		var first = new MuiNativeAreaRectangle
		{
			Left = int.MinValue,
			Top = int.MinValue,
			Width = int.MaxValue,
			Height = int.MaxValue,
		};
		var second = new MuiNativeAreaRectangle
		{
			Left = -2,
			Top = -2,
			Width = 20,
			Height = 20,
		};

		Assert.True(MuiNativeGuiMode.TryIntersectRectangles(first, second,
			out var intersection));
		Assert.Equal(-2, intersection.Left);
		Assert.Equal(-2, intersection.Top);
		Assert.Equal(1, intersection.Width);
		Assert.Equal(1, intersection.Height);
	}

	[Fact]
	public void VirtualGroupClipIntersectionRejectsDisjointOrInvalidRectangles()
	{
		var first = new MuiNativeAreaRectangle
		{
			Left = 0,
			Top = 0,
			Width = 10,
			Height = 10,
		};
		var disjoint = new MuiNativeAreaRectangle
		{
			Left = 10,
			Top = 0,
			Width = 4,
			Height = 10,
		};
		var empty = new MuiNativeAreaRectangle
		{
			Left = 0,
			Top = 0,
			Width = 0,
			Height = 10,
		};

		Assert.False(MuiNativeGuiMode.TryIntersectRectangles(first, disjoint,
			out _));
		Assert.False(MuiNativeGuiMode.TryIntersectRectangles(first, empty,
			out _));
	}

	[Fact]
	public void VirtualGroupViewportUsesTheNamedInnerAreaInsets()
	{
		var outer = new MuiNativeAreaRectangle
		{
			Left = 30,
			Top = 40,
			Width = 100,
			Height = 80,
		};
		var insets = new MuiNativeAreaInsets
		{
			Left = 3,
			Top = 4,
			Right = 5,
			Bottom = 6,
		};

		Assert.True(MuiNativeGuiMode.TryInsetRectangle(outer, insets,
			out var inner));
		Assert.Equal(33, inner.Left);
		Assert.Equal(44, inner.Top);
		Assert.Equal(92, inner.Width);
		Assert.Equal(70, inner.Height);
	}

	[Fact]
	public void VirtualGroupViewportAcceptsAnEmptyContentAreaAndRejectsOverflow()
	{
		var outer = new MuiNativeAreaRectangle
		{
			Left = 10,
			Top = 20,
			Width = 10,
			Height = 8,
		};
		var exactInsets = new MuiNativeAreaInsets
		{
			Left = 6,
			Right = 4,
			Top = 3,
			Bottom = 5,
		};
		var overflowingInsets = new MuiNativeAreaInsets { Left = 8 };

		Assert.True(MuiNativeGuiMode.TryInsetRectangle(outer, exactInsets,
			out var empty));
		Assert.Equal(0, empty.Width);
		Assert.Equal(0, empty.Height);
		outer.Left = int.MaxValue - 1;
		Assert.False(MuiNativeGuiMode.TryInsetRectangle(outer,
			overflowingInsets, out _));
	}
}
