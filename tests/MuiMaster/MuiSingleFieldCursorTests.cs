using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiSingleFieldCursorTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void RemainingPointerAndValueSlotsUseNamedFieldCursors()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);

		var usedClasses = APTR.FromPointer(0x1200);
		Assert.True(MuiApplicationUsedClassesVectorEntryFieldCursorCodec
			.TryWriteUInt32(ref platform, usedClasses,
				MuiApplicationUsedClassesVectorEntryField.Name, 0x1300u));
		Assert.True(MuiApplicationUsedClassesVectorEntryFieldCursorCodec
			.TryReadUInt32(ref platform, usedClasses,
				MuiApplicationUsedClassesVectorEntryField.Name, out var className));
		Assert.Equal(0x1300u, className);

		var poplist = APTR.FromPointer(0x1220);
		Assert.True(MuiPoplistArrayEntryFieldCursorCodec.TryWriteUInt32(
			ref platform, poplist, MuiPoplistArrayEntryField.Value, 0x1400u));
		Assert.True(MuiPoplistArrayEntryFieldCursorCodec.TryReadUInt32(
			ref platform, poplist, MuiPoplistArrayEntryField.Value,
			out var popValue));
		Assert.Equal(0x1400u, popValue);

		var requester = APTR.FromPointer(0x1240);
		Assert.True(MuiRequesterParameterSlotFieldCursorCodec.TryWriteUInt32(
			ref platform, requester, MuiRequesterParameterSlotField.Value, 7u));
		Assert.True(MuiRequesterParameterSlotFieldCursorCodec.TryReadUInt32(
			ref platform, requester, MuiRequesterParameterSlotField.Value,
			out var parameter));
		Assert.Equal(7u, parameter);

		var process = APTR.FromPointer(0x1260);
		Assert.True(MuiProcessDispatchArgumentSlotFieldCursorCodec.TryWriteUInt32(
			ref platform, process, MuiProcessDispatchArgumentSlotField.Value,
			0xABCDu));
		Assert.True(MuiProcessDispatchArgumentSlotFieldCursorCodec.TryReadUInt32(
			ref platform, process, MuiProcessDispatchArgumentSlotField.Value,
			out var processValue));
		Assert.Equal(0xABCDu, processValue);

		var updateObject = APTR.FromPointer(0x1280);
		Assert.True(MuiUpdateConfigObjectSlotFieldCursorCodec.TryWriteUInt32(
			ref platform, updateObject, MuiUpdateConfigObjectSlotField.Object,
			0x1500u));
		Assert.True(MuiUpdateConfigObjectSlotFieldCursorCodec.TryReadUInt32(
			ref platform, updateObject, MuiUpdateConfigObjectSlotField.Object,
			out var objectValue));
		Assert.Equal(0x1500u, objectValue);

		var updateFlag = APTR.FromPointer(0x12A0);
		Assert.True(MuiUpdateConfigFlagSlotFieldCursorCodec.TryWriteUInt8(
			ref platform, updateFlag, MuiUpdateConfigFlagSlotField.Value, 1));
		Assert.True(MuiUpdateConfigFlagSlotFieldCursorCodec.TryReadUInt8(
			ref platform, updateFlag, MuiUpdateConfigFlagSlotField.Value,
			out var flag));
		Assert.Equal((byte)1, flag);

		Assert.False(MuiPoplistArrayEntryFieldCursorCodec.TryReadUInt32(
			ref platform, poplist, unchecked((MuiPoplistArrayEntryField)255),
			out _));
	}
}
