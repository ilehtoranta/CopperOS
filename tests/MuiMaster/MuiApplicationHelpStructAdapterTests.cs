using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationHelpStructAdapterTests
{
	[Fact]
	public void ApplicationHelpStateUsesDedicatedNamedStructAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiApplicationHelpStateRecord
		{
			Magic = MuiApplicationHelpStateRecord.Cookie,
			AboutReferenceWindow = APTR.Null,
			AboutRequests = 2,
			HelpWindow = APTR.Null,
			HelpName = APTR.Null,
			HelpNode = APTR.Null,
			HelpLine = unchecked((uint)-3),
			HelpRequests = 4,
		};

		Assert.True(MuiApplicationHelpStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiApplicationHelpStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationHelpStateField.AboutRequests,
			out var aboutRequestsField));
		Assert.Equal(APTR.FromPointer(0x3508), aboutRequestsField);
		Assert.True(MuiApplicationHelpStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationHelpStateField.HelpRequests,
			out var helpRequestsField));
		Assert.Equal(APTR.FromPointer(0x351C), helpRequestsField);
		Assert.True(MuiApplicationHelpStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationHelpStateField.HelpLine,
			unchecked((uint)-4)));
		Assert.True(MuiApplicationHelpStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.AboutRequests, decoded.AboutRequests);
		Assert.Equal(-4, unchecked((int)decoded.HelpLine));
		Assert.Equal(value.HelpRequests, decoded.HelpRequests);
		Assert.False(MuiApplicationHelpStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FE4),
			MuiApplicationHelpStateField.Magic, out _));
		Assert.False(MuiApplicationHelpStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null,
			MuiApplicationHelpStateField.HelpNode, out _));
	}

	[Fact]
	public void ApplicationHelpStateSequentialRecordPreservesPointersAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3580);
		var value = new MuiApplicationHelpStateRecord
		{
			Magic = MuiApplicationHelpStateRecord.Cookie,
			AboutReferenceWindow = APTR.FromPointer(uint.MaxValue),
			AboutRequests = 0x01020304u,
			HelpWindow = APTR.FromPointer(0x11223344u),
			HelpName = APTR.FromPointer(0x55667788u),
			HelpNode = APTR.FromPointer(0x99AABBCCu),
			HelpLine = unchecked((uint)int.MinValue + 5u),
			HelpRequests = 0xDDEEFF00u,
		};

		Assert.True(MuiApplicationHelpStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiApplicationHelpStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.AboutReferenceWindow, decoded.AboutReferenceWindow);
		Assert.Equal(value.AboutRequests, decoded.AboutRequests);
		Assert.Equal(value.HelpWindow, decoded.HelpWindow);
		Assert.Equal(value.HelpName, decoded.HelpName);
		Assert.Equal(value.HelpNode, decoded.HelpNode);
		Assert.Equal(value.HelpLine, decoded.HelpLine);
		Assert.Equal(value.HelpRequests, decoded.HelpRequests);

		var crossingEnd = APTR.FromPointer(0x30FE1);
		Assert.False(MuiApplicationHelpStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiApplicationHelpStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}
}
