/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationPersistenceFrameStructCodecTests
{
	[Fact]
	public void PersistenceFrameRoundTripsThroughSequentialRecord()
	{
		Assert.Equal(12, Unsafe.SizeOf<MuiApplicationPersistenceFrameState>());
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2400);
		var expected = default(MuiApplicationPersistenceFrameState);
		expected.Object = APTR.FromPointer(0x3000);
		expected.NextChild = 17;
		expected.VisitMarker = 29;

		Assert.True(MuiApplicationPersistenceFrameStateStructCodec.Write(
			ref platform, address, expected));
		Assert.True(MuiApplicationPersistenceFrameStateStructCodec.TryRead(
			ref platform, address, out var actual));
		Assert.Equal(expected.Object, actual.Object);
		Assert.Equal(expected.NextChild, actual.NextChild);
		Assert.Equal(expected.VisitMarker, actual.VisitMarker);
	}

	[Fact]
	public void PersistenceFrameFieldAccessUsesCompleteNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2600);
		var expected = default(MuiApplicationPersistenceFrameState);
		expected.Object = APTR.FromPointer(0x3000);
		expected.NextChild = 17;
		expected.VisitMarker = 29;
		Assert.True(MuiApplicationPersistenceFrameStateCodec.WriteStructural(
			ref platform, address, expected));
		Assert.True(MuiApplicationPersistenceFrameMemoryCodec.TryWrite(ref platform,
			address, MuiApplicationPersistenceFrameField.NextChild, 23));
		Assert.True(MuiApplicationPersistenceFrameMemoryCodec.TryRead(ref platform,
			address, MuiApplicationPersistenceFrameField.Object, out var objectValue));
		Assert.Equal(expected.Object.Raw, objectValue);
		Assert.True(MuiApplicationPersistenceFrameStateCodec.TryReadStructural(
			ref platform, address, out var actual));
		Assert.Equal(23u, actual.NextChild);
		Assert.Equal(expected.VisitMarker, actual.VisitMarker);
		Assert.False(MuiApplicationPersistenceFrameMemoryCodec.TryRead(ref platform,
			address, (MuiApplicationPersistenceFrameField)255, out _));
		Assert.False(MuiApplicationPersistenceFrameMemoryCodec.TryWrite(ref platform,
			APTR.FromPointer(0x30FF5), MuiApplicationPersistenceFrameField.Object, 1));
	}

	[Fact]
	public void PersistenceFrameCodecRejectsIncompleteGuestRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var crossingEnd = APTR.FromPointer(0x20FF5);
		var value = default(MuiApplicationPersistenceFrameState);
		Assert.False(MuiApplicationPersistenceFrameStateStructCodec.Write(
			ref platform, crossingEnd, value));
		Assert.False(MuiApplicationPersistenceFrameStateStructCodec.TryRead(
			ref platform, crossingEnd, out _));
		Assert.False(MuiApplicationPersistenceFrameStateStructCodec.TryRead(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void PersistenceFrameVectorBridgeUsesCompleteNamedFrames()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var stack = APTR.FromPointer(0x2800);
		var expected = new MuiApplicationPersistenceFrameState
		{
			Object = APTR.FromPointer(0xFEDCBA98u),
			NextChild = 0x81234567u,
			VisitMarker = 0xF1234567u,
		};

		Assert.True(MuiApplicationPersistenceFrameVectorCodec.TryWrite(ref platform,
			stack, 3, expected));
		Assert.True(MuiApplicationPersistenceFrameVectorCodec.TryRead(ref platform,
			stack, 3, out var actual));
		Assert.Equal(expected.Object, actual.Object);
		Assert.Equal(expected.NextChild, actual.NextChild);
		Assert.Equal(expected.VisitMarker, actual.VisitMarker);

		Assert.False(MuiApplicationPersistenceFrameVectorCodec.TryRead(ref platform,
			stack, MuiApplicationPersistenceFrameCursor.MaximumEntries, out _));
		Assert.False(MuiApplicationPersistenceFrameVectorCodec.TryRead(ref platform,
			APTR.FromPointer(0x30FF8u), 0, out _));
		Assert.False(MuiApplicationPersistenceFrameVectorCodec.TryWrite(ref platform,
			APTR.Null, 0, expected));
	}

	[Fact]
	public void PersistenceFrameCursorExchangesCompleteNamedFrames()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var cursor = new MuiApplicationPersistenceFrameCursor
		{
			Base = APTR.FromPointer(0x2800),
			Index = 3,
		};
		var expected = new MuiApplicationPersistenceFrameState
		{
			Object = APTR.FromPointer(0xFEDCBA98u),
			NextChild = 0x81234567u,
			VisitMarker = 0xF1234567u,
		};

		Assert.True(MuiApplicationPersistenceFrameCursorCodec.TryWrite(ref platform,
			cursor, expected));
		Assert.True(MuiApplicationPersistenceFrameCursorCodec.TryRead(ref platform,
			cursor, out var actual));
		Assert.Equal(expected.Object.Raw, actual.Object.Raw);
		Assert.Equal(expected.NextChild, actual.NextChild);
		Assert.Equal(expected.VisitMarker, actual.VisitMarker);
		Assert.False(MuiApplicationPersistenceFrameCursorCodec.TryRead(ref platform,
			new MuiApplicationPersistenceFrameCursor
			{
				Base = APTR.FromPointer(0x30FF8u),
				Index = 0,
			}, out _));
	}
}
