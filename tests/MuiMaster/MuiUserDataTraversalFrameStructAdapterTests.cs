/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiUserDataTraversalFrameStructAdapterTests
{
	[Fact]
	public void UserDataTraversalFrameFieldsUseNamedRecordAndPreserveSiblings()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var frame = APTR.FromPointer(0x2400);
		Assert.True(MuiUDataTraversalFrameRecordCodec.Write(ref platform, frame,
			new MuiUDataTraversalFrame
			{
				Object = APTR.FromPointer(0x3000),
				NextChild = 3,
			}));
		Assert.True(MuiUDataTraversalFrameMemoryCodec.TryWriteUInt32(ref platform,
			frame, MuiUDataTraversalField.Object, 0x3100));
		Assert.True(MuiUDataTraversalFrameMemoryCodec.TryWriteUInt32(ref platform,
			frame, MuiUDataTraversalField.NextChild, 7));
		Assert.True(MuiUDataTraversalFrameMemoryCodec.TryReadUInt32(ref platform,
			frame, MuiUDataTraversalField.Object, out var objectAddress));
		Assert.Equal(0x3100u, objectAddress);
		Assert.True(MuiUDataTraversalFrameMemoryCodec.TryReadUInt32(ref platform,
			frame, MuiUDataTraversalField.NextChild, out var nextChild));
		Assert.Equal(7u, nextChild);
		var cursor = new MuiUDataTraversalFieldCursor
		{
			Frame = frame,
			Field = MuiUDataTraversalField.Object,
		};
		Assert.True(MuiUDataTraversalFrameMemoryCodec.TryGetAddress(ref platform,
			cursor, out var objectFieldAddress, out var objectFieldSize));
		Assert.Equal(frame.Raw, objectFieldAddress.Raw);
		Assert.Equal(4u, objectFieldSize);
		cursor.Field = MuiUDataTraversalField.NextChild;
		Assert.True(MuiUDataTraversalFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var nextChildAddress, out var nextChildSize));
		Assert.Equal(frame.Raw + 4, nextChildAddress.Raw);
		Assert.Equal(4u, nextChildSize);
	}

	[Fact]
	public void UserDataTraversalFrameFieldAdapterRejectsBadRangesAndFields()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var frame = APTR.FromPointer(0x2400);
		Assert.True(MuiUDataTraversalFrameRecordCodec.Write(ref platform, frame,
			new MuiUDataTraversalFrame
			{
				Object = APTR.FromPointer(0x3000),
				NextChild = 3,
			}));
		Assert.False(MuiUDataTraversalFrameMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x20FFC), MuiUDataTraversalField.Object, out _));
		Assert.False(MuiUDataTraversalFrameMemoryCodec.TryWriteUInt32(ref platform,
			APTR.Null, MuiUDataTraversalField.NextChild, 1));
		Assert.False(MuiUDataTraversalFrameMemoryCodec.TryReadUInt32(ref platform,
			frame, (MuiUDataTraversalField)255, out _));
		Assert.False(MuiUDataTraversalFrameMemoryCodec.TryWriteUInt32(ref platform,
			frame, (MuiUDataTraversalField)255, 1));
	}
}
