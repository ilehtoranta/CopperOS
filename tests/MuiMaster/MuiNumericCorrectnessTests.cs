using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNumericCorrectnessTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Theory]
	[InlineData(true, 32u, 32u)]
	[InlineData(false, 20u, 0u)]
	public void NumericValuePreservesNotificationWritesAndHonorsSuppression(
		bool notify, uint expectedValue, uint expectedCallbacks)
	{
		var platform = CreateNumeric(out var obj);
		var follow = APTR.FromPointer(0x1300);
		platform.WriteUInt32(follow, 0, 0x90000002);
		Assert.True(MuiNotifyCore.Add(ref platform, State, obj,
			MuiCommonControlCore.NumericValue, 1233727793, obj, 1, follow));
		platform.MutationMode = 2;
		platform.MutationSource = obj;
		platform.MutationDestination = obj;
		platform.MutationAttribute = MuiCommonControlCore.NumericValue;

		Assert.True(MuiCommonControlCore.SetNumericValue(ref platform, State,
			obj, 20, false, notify));

		Assert.Equal(expectedCallbacks, platform.RecursiveDispatches);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			MuiCommonControlCore.NumericValue, out var value));
		Assert.Equal(expectedValue, value);
		Assert.True(MuiCommonControlCore.TryReadNumericState(ref platform, State,
			obj, out var numeric));
		Assert.Equal(expectedValue, numeric.Value);
	}

	[Theory]
	[InlineData(0, 1000000, 0, 1000000, 1000000, false, 1000000)]
	[InlineData(0, 1000000, 0, 1000000, 500000, false, 500000)]
	[InlineData(int.MinValue, int.MaxValue, int.MinValue, int.MaxValue, 0, false, 0)]
	[InlineData(int.MinValue, int.MaxValue, 0, 100, 50, false, -1)]
	[InlineData(int.MinValue, int.MaxValue, 0, 100, 100, true, int.MinValue)]
	[InlineData(0, 1000000, -100, 100, -200, false, 0)]
	[InlineData(0, 1000000, -100, 100, 200, false, 1000000)]
	public void ScaleToValueHandlesLargeAndSignedRanges(int min, int max,
		int scaleMin, int scaleMax, int scale, bool reverse, int expected)
	{
		var platform = CreateNumeric(out var obj);
		SetRange(ref platform, obj, min, max, 0, reverse);
		Assert.Equal(expected, MuiCommonControlCore.ScaleToValue(ref platform,
			State, obj, scaleMin, scaleMax, scale));
	}

	[Theory]
	[InlineData(0, 1000000, 1000000, 0, 1000000, false, 1000000)]
	[InlineData(0, 1000000, 500000, 0, 1000000, false, 500000)]
	[InlineData(int.MinValue, int.MaxValue, 0, int.MinValue, int.MaxValue, false, 0)]
	[InlineData(int.MinValue, int.MaxValue, int.MaxValue, 0, 100, false, 100)]
	[InlineData(int.MinValue, int.MaxValue, int.MinValue, 0, 100, true, 100)]
	[InlineData(-100, 100, -200, int.MinValue, int.MaxValue, false, int.MinValue)]
	[InlineData(-100, 100, 200, int.MinValue, int.MaxValue, false, int.MaxValue)]
	public void ValueToScaleHandlesLargeAndSignedRanges(int min, int max,
		int value, int scaleMin, int scaleMax, bool reverse, int expected)
	{
		var platform = CreateNumeric(out var obj);
		SetRange(ref platform, obj, min, max, value, reverse);
		Assert.Equal(expected, MuiCommonControlCore.ValueToScale(ref platform,
			State, obj, scaleMin, scaleMax));
	}

	[Fact]
	public void ScalingMatchesWideIntegerReferenceAcrossUnsignedSpans()
	{
		var random = new Random(23026);
		for (var index = 0; index < 10000; index++)
		{
			var inputSpan = (uint)random.NextInt64(1, 0x100000000L);
			var outputSpan = (uint)random.NextInt64(0, 0x100000000L);
			var offset = (uint)random.NextInt64(0, (long)inputSpan + 1);
			var expected = (uint)((ulong)offset * outputSpan / inputSpan);
			Assert.Equal(expected,
				MuiNumericScalingCore.ScaleOffset(offset, inputSpan, outputSpan));
		}
	}

	private static void SetRange(ref MuiHeadlessTestPlatform platform, APTR obj,
		int min, int max, int value, bool reverse)
	{
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiCommonControlCore.NumericMin, unchecked((uint)min), false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiCommonControlCore.NumericMax, unchecked((uint)max), false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiCommonControlCore.NumericValue, unchecked((uint)value), false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiCommonControlCore.NumericReverse, reverse ? 1u : 0u, false));
	}

	private static MuiHeadlessTestPlatform CreateNumeric(out APTR obj)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000, State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Numeric.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var cls = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cls, APTR.Null);
		Assert.NotEqual(APTR.Null, obj);
		return platform;
	}
}
