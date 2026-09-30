using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiHeadlessStateFieldStructAdapterTests
{
	[Fact]
	public void FieldWritesPreserveTheCompleteStateHeader()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2E00);
		var initial = new MuiHeadlessStateRecord
		{
			Magic = MuiHeadlessLayout.Magic,
			Version = MuiHeadlessLayout.Version,
			Classes = APTR.FromPointer(0x3000),
			Objects = APTR.FromPointer(0x3040),
			NextSequence = 11,
			NotifyDepth = 2,
			Mutation = 7,
			Reserved = 9,
		};
		Assert.True(MuiHeadlessStateCodec.WriteRecord(ref platform, address,
			initial));

		Assert.True(MuiHeadlessStateMemoryCodec.TryWrite(ref platform, address,
			MuiHeadlessStateField.Objects, 0x30C0));
		Assert.True(MuiHeadlessStateMemoryCodec.TryRead(ref platform, address,
			MuiHeadlessStateField.Objects, out var objects));
		Assert.Equal(0x30C0u, objects);
		Assert.True(MuiHeadlessStateCodec.TryReadStructural(ref platform, address,
			out var decoded));
		Assert.Equal(initial.Magic, decoded.Magic);
		Assert.Equal(initial.Version, decoded.Version);
		Assert.Equal(initial.Classes.Raw, decoded.Classes.Raw);
		Assert.Equal(initial.NextSequence, decoded.NextSequence);
		Assert.Equal(initial.NotifyDepth, decoded.NotifyDepth);
		Assert.Equal(initial.Mutation, decoded.Mutation);
		Assert.Equal(initial.Reserved, decoded.Reserved);
	}
}
