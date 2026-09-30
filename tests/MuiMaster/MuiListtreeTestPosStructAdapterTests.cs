using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListtreeTestPosStructAdapterTests
{
	[Fact]
	public void TestPosFieldsRoundTripThroughNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListtreeCore.MuiListtreeTestPosResult
		{
			TreeNode = APTR.FromPointer(0x36200),
			Flags = 0x12,
			ListEntry = -4,
			ListFlags = 0x34,
		};

		Assert.True(MuiListtreeCore.MuiListtreeTestPosResultCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiListtreeCore.MuiListtreeTestPosMemoryCodec.TryWriteUInt16(
			ref platform, address, MuiListtreeCore.MuiListtreeTestPosField.Flags, 0x22));
		Assert.True(MuiListtreeCore.MuiListtreeTestPosMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeTestPosField.ListEntry,
			unchecked((uint)-7)));
		Assert.True(MuiListtreeCore.MuiListtreeTestPosMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeTestPosField.ListEntry,
			out var listEntry));
		Assert.Equal(unchecked((uint)-7), listEntry);
		Assert.True(MuiListtreeCore.MuiListtreeTestPosResultCodec.TryRead(
			ref platform, address, out var decoded));
		Assert.Equal(value.TreeNode.Raw, decoded.TreeNode.Raw);
		Assert.Equal((ushort)0x22, decoded.Flags);
		Assert.Equal(-7, decoded.ListEntry);
		Assert.Equal(value.ListFlags, decoded.ListFlags);
		Assert.True(MuiListtreeCore.MuiListtreeTestPosMemoryCodec.TryGetAddress(
			ref platform, address, MuiListtreeCore.MuiListtreeTestPosField.ListFlags,
			out var listFlagsAddress, out var listFlagsSize));
		Assert.Equal(0x350Au, listFlagsAddress.Raw);
		Assert.Equal(2u, listFlagsSize);
	}

	[Fact]
	public void TestPosAdapterRejectsInvalidWidthsOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListtreeCore.MuiListtreeTestPosMemoryCodec.TryReadUInt16(
			ref platform, address, MuiListtreeCore.MuiListtreeTestPosField.TreeNode,
			out _));
		Assert.False(MuiListtreeCore.MuiListtreeTestPosMemoryCodec.TryWriteUInt16(
			ref platform, address, MuiListtreeCore.MuiListtreeTestPosField.ListEntry, 1));
		Assert.False(MuiListtreeCore.MuiListtreeTestPosMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreeTestPosField)0xFF,
			out _));
		Assert.False(MuiListtreeCore.MuiListtreeTestPosMemoryCodec.TryWriteUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreeTestPosField)0xFF,
			1));
		Assert.False(MuiListtreeCore.MuiListtreeTestPosMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x30FFC),
			MuiListtreeCore.MuiListtreeTestPosField.ListEntry, out _));
		Assert.False(MuiListtreeCore.MuiListtreeTestPosMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null, MuiListtreeCore.MuiListtreeTestPosField.TreeNode,
			1));
	}
}
