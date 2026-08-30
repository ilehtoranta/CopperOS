using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiHeadlessStateAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void HeadlessStateAdmissionRequiresCanonicalHeaderAndDepth()
	{
		var valid = new MuiHeadlessStateRecord
		{
			Magic = MuiHeadlessLayout.Magic,
			Version = MuiHeadlessLayout.Version,
			NextSequence = 1,
			NotifyDepth = MuiHeadlessLayout.MaximumNotificationDepth,
		};
		Assert.True(MuiHeadlessStateAdmission.Validate(valid));
		valid.Magic = 0;
		Assert.False(MuiHeadlessStateAdmission.Validate(valid));
		valid.Magic = MuiHeadlessLayout.Magic;
		valid.NotifyDepth = MuiHeadlessLayout.MaximumNotificationDepth + 1;
		Assert.False(MuiHeadlessStateAdmission.Validate(valid));
		valid.NotifyDepth = 0;
		valid.NextSequence = 0;
		Assert.True(MuiHeadlessStateAdmission.Validate(valid));
	}

	[Fact]
	public void MalformedHeadlessStateFailsClosedBeforeConsumers()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		Assert.True(MuiHeadlessMemory.Initialize(ref platform, State));
		Assert.True(MuiHeadlessStateFieldCursorCodec.TryWrite(ref platform, State,
			MuiHeadlessStateField.NotifyDepth,
			MuiHeadlessLayout.MaximumNotificationDepth + 1));
		Assert.True(MuiHeadlessStateCodec.TryReadStructural(ref platform, State,
			out var structural));
		Assert.False(MuiHeadlessStateAdmission.Validate(structural));
		Assert.False(MuiHeadlessStateCodec.TryRead(ref platform, State, out _));
		Assert.Equal(0u, MuiHeadlessMemory.NextSequence(ref platform, State));
		MuiHeadlessMemory.Mutated(ref platform, State);
		Assert.True(MuiHeadlessStateFieldCursorCodec.TryRead(ref platform, State,
			MuiHeadlessStateField.NotifyDepth, out var preserved));
		Assert.Equal(MuiHeadlessLayout.MaximumNotificationDepth + 1, preserved);
	}

	[Fact]
	public void HeadlessStateMemoryAdapterOwnsNamedStructBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var state = APTR.FromPointer(0x2300);
		Assert.True(MuiHeadlessStateMemoryCodec.TryGetAddress(ref platform, state,
			MuiHeadlessStateField.Objects, out var objects));
		Assert.Equal(state.Raw + MuiHeadlessStateRecord.ObjectsOffset,
			objects.Raw);
		Assert.True(MuiHeadlessStateMemoryCodec.TryWrite(ref platform, state,
			MuiHeadlessStateField.NotifyDepth, 7));
		Assert.True(MuiHeadlessStateMemoryCodec.TryRead(ref platform, state,
			MuiHeadlessStateField.NotifyDepth, out var depth));
		Assert.Equal(7u, depth);
		Assert.False(MuiHeadlessStateMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x30FE1), MuiHeadlessStateField.Reserved, out _));
		Assert.False(MuiHeadlessStateMemoryCodec.TryGetAddress(ref platform, state,
			(MuiHeadlessStateField)255, out _));
	}

	[Fact]
	public void HeadlessStateStructCodecRoundTripsDeclarationOrder()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var state = APTR.FromPointer(0x2800);
		var value = new MuiHeadlessStateRecord
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
		Assert.True(MuiHeadlessStateCodec.WriteRecord(ref platform, state, value));
		Assert.True(MuiHeadlessStateCodec.TryReadRecord(ref platform, state,
			out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Version, decoded.Version);
		Assert.Equal(value.Classes.Raw, decoded.Classes.Raw);
		Assert.Equal(value.Objects.Raw, decoded.Objects.Raw);
		Assert.Equal(value.NextSequence, decoded.NextSequence);
		Assert.Equal(value.NotifyDepth, decoded.NotifyDepth);
		Assert.Equal(value.Mutation, decoded.Mutation);
		Assert.Equal(value.Reserved, decoded.Reserved);
		Assert.False(MuiHeadlessStateCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x30FF0), out _));
	}
}
