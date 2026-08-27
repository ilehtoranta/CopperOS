using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiWindowEventReuseStructAdapterTests
{
	[Fact]
	public void WindowEventReuseStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiWindowEventReuseStateRecord
		{
			Magic = MuiWindowEventReuseStateRecord.Cookie,
			ContextActive = 0,
			Pending = 0,
			EventMessage = APTR.Null,
			InputEvent = APTR.Null,
			EventClass = 0,
			MuiKey = -7,
		};

		Assert.True(MuiWindowEventReuseStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiWindowEventReuseStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiWindowEventReuseStateField.MuiKey,
			out var keyAddress));
		Assert.Equal(0x3518u, keyAddress.Raw);
		Assert.True(MuiWindowEventReuseStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiWindowEventReuseStateField.MuiKey,
			unchecked((uint)-8)));
		Assert.True(MuiWindowEventReuseStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(-8, decoded.MuiKey);
		Assert.False(MuiWindowEventReuseStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FE5),
			MuiWindowEventReuseStateField.Magic, out _));
		Assert.False(MuiWindowEventReuseStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiWindowEventReuseStateField.Magic, out _));
	}
}
