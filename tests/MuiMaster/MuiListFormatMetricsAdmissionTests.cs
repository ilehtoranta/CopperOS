using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListFormatMetricsAdmissionTests
{
	[Fact]
	public void ListFormatAndColumnMetricsRecordsRoundTripThroughStructs()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x4000,
			APTR.FromPointer(0x1000));
		var formatAddress = APTR.FromPointer(0x3100);
		var formatValues = APTR.FromPointer(0x3300);
		var metricsAddress = APTR.FromPointer(0x3200);
		var metricsValues = APTR.FromPointer(0x3400);
		var format = new MuiListCore.MuiListFormatDescriptorState
		{
			Magic = MuiListCore.MuiListFormatDescriptorState.Cookie,
			Columns = 2,
			Values = formatValues,
		};
		var descriptor = new MuiListCore.MuiListFormatDescriptor
		{
			Delta = 1,
			Weight = 2,
			MinWidth = 16,
			MaxWidth = 480,
			Column = 0,
			Flags = 3,
			Preparse = APTR.FromPointer(0x3500),
			PreparseLength = 4,
		};
		var formatCursor = default(MuiListCore.MuiListFormatDescriptorCursor);
		formatCursor.Base = formatValues;
		formatCursor.Index = 0;
		Assert.True(MuiListCore.MuiListFormatDescriptorCursorCodec.TryGetEntry(
			ref platform, formatCursor, out var descriptorAddress));
		MuiListCore.WriteFormatDescriptor(ref platform, descriptorAddress,
			ref descriptor);
		formatCursor.Index = 1;
		Assert.True(MuiListCore.MuiListFormatDescriptorCursorCodec.TryGetEntry(
			ref platform, formatCursor, out descriptorAddress));
		MuiListCore.WriteFormatDescriptor(ref platform, descriptorAddress,
			ref descriptor);
		var metrics = new MuiListCore.MuiListColumnMetricsState
		{
			Magic = 0x434D4554u,
			Width = 640,
			Columns = 2,
			Values = metricsValues,
		};
		var metricCursor = default(MuiListCore.MuiListColumnMetricCursor);
		metricCursor.Base = metricsValues;
		metricCursor.Index = 0;
		Assert.True(MuiListCore.MuiListColumnMetricCursorCodec.TryGetEntry(
			ref platform, metricCursor, out var metricAddress));
		Assert.True(MuiListCore.MuiListColumnMetricCodec.Write(ref platform,
			metricAddress, new MuiListCore.MuiListColumnMetricValue { Value = 240 }));
		metricCursor.Index = 1;
		Assert.True(MuiListCore.MuiListColumnMetricCursorCodec.TryGetEntry(
			ref platform, metricCursor, out metricAddress));
		Assert.True(MuiListCore.MuiListColumnMetricCodec.Write(ref platform,
			metricAddress, new MuiListCore.MuiListColumnMetricValue { Value = 400 }));
		Assert.True(MuiListCore.MuiListFormatDescriptorStateCodec.Write(ref platform,
			formatAddress, format));
		Assert.True(MuiListCore.MuiListColumnMetricsStateCodec.Write(ref platform,
			metricsAddress, metrics));
		Assert.True(MuiListCore.MuiListFormatDescriptorStateCodec.TryRead(
			ref platform, formatAddress, out var readFormat));
		Assert.Equal(format.Columns, readFormat.Columns);
		Assert.Equal(format.Values, readFormat.Values);
		Assert.True(MuiListCore.MuiListColumnMetricsStateCodec.TryRead(ref platform,
			metricsAddress, out var readMetrics));
		Assert.Equal(metrics.Width, readMetrics.Width);
		Assert.Equal(metrics.Values, readMetrics.Values);
		metricCursor.Index = 1;
		Assert.True(MuiListCore.MuiListColumnMetricCursorCodec.TryGetEntry(
			ref platform, metricCursor, out metricAddress));
		Assert.True(MuiListCore.MuiListColumnMetricCodec.TryRead(ref platform,
			metricAddress, out var readMetric));
		Assert.Equal(400u, readMetric.Value);
	}

	[Fact]
	public void ListFormatDescriptorStateSequentialRecordPreservesFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3A00);
		var value = new MuiListCore.MuiListFormatDescriptorState
		{
			Magic = MuiListCore.MuiListFormatDescriptorState.Cookie,
			Columns = 0xFFFFFFFEu,
			Values = APTR.FromPointer(0xFFFFFFF0u),
		};
		Assert.True(MuiListCore.MuiListFormatDescriptorStateCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiListCore.MuiListFormatDescriptorStateCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Columns, decoded.Columns);
		Assert.Equal(value.Values, decoded.Values);
		Assert.False(MuiListCore.MuiListFormatDescriptorStateCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x40FF5), out _));
	}

	[Fact]
	public void ListColumnMetricsStateSequentialRecordPreservesFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3B00);
		var value = new MuiListCore.MuiListColumnMetricsState
		{
			Magic = MuiListCore.MuiListColumnMetricsState.Cookie,
			Width = 0xFFFFFFFEu,
			Columns = 0xFFFFFFFDu,
			Values = APTR.FromPointer(0xFFFFFFF0u),
		};
		Assert.True(MuiListCore.MuiListColumnMetricsStateCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiListCore.MuiListColumnMetricsStateCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Width, decoded.Width);
		Assert.Equal(value.Columns, decoded.Columns);
		Assert.Equal(value.Values, decoded.Values);
		Assert.False(MuiListCore.MuiListColumnMetricsStateCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x40FF5), out _));
	}

	[Fact]
	public void MalformedListFormatAndMetricsMagicRemainStructuralButFailClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x4000,
			APTR.FromPointer(0x1000));
		var formatAddress = APTR.FromPointer(0x3600);
		var formatValues = APTR.FromPointer(0x3700);
		var metricsAddress = APTR.FromPointer(0x3640);
		var metricsValues = APTR.FromPointer(0x3800);
		Assert.True(MuiListCore.MuiListFormatDescriptorStateCodec.Write(ref platform,
			formatAddress, new MuiListCore.MuiListFormatDescriptorState
			{
				Magic = MuiListCore.MuiListFormatDescriptorState.Cookie,
				Columns = 1,
				Values = formatValues,
			}));
		Assert.True(MuiListCore.MuiListColumnMetricsStateCodec.Write(ref platform,
			metricsAddress, new MuiListCore.MuiListColumnMetricsState
			{
				Magic = 0x434D4554u,
				Width = 320,
				Columns = 1,
				Values = metricsValues,
			}));
		Assert.True(MuiListCore.MuiListFormatDescriptorStateFieldCursorCodec
			.TryWriteUInt32(ref platform, formatAddress,
				MuiListCore.MuiListFormatDescriptorStateField.Magic, 0));
		Assert.True(MuiListCore.MuiListColumnMetricsFieldCursorCodec.TryWriteUInt32(
			ref platform, metricsAddress, MuiListCore.MuiListColumnMetricsField.Magic,
			0));
		Assert.True(MuiListCore.MuiListFormatDescriptorStateCodec.TryReadStructural(
			ref platform, formatAddress, out var format));
		Assert.Equal(0u, format.Magic);
		Assert.Equal(formatValues, format.Values);
		Assert.False(MuiListCore.MuiListFormatDescriptorStateCodec.TryRead(
			ref platform, formatAddress, out _));
		Assert.True(MuiListCore.MuiListFormatDescriptorStateCodec.TryReadStorage(
			ref platform, formatAddress, out var formatStorage));
		Assert.Equal(0u, formatStorage.Magic);
		Assert.True(MuiListCore.MuiListColumnMetricsStateCodec.TryReadStructural(
			ref platform, metricsAddress, out var metrics));
		Assert.Equal(0u, metrics.Magic);
		Assert.Equal(metricsValues, metrics.Values);
		Assert.False(MuiListCore.MuiListColumnMetricsStateCodec.TryRead(
			ref platform, metricsAddress, out _));
		Assert.True(MuiListCore.MuiListColumnMetricsStateCodec.TryReadStorage(
			ref platform, metricsAddress, out var metricsStorage));
		Assert.Equal(0u, metricsStorage.Magic);
	}
}
