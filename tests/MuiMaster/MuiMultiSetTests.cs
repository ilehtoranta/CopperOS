using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiMultiSetTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint Attribute = 0x80420030;

	[Fact]
	public void MultiSetTargetCodecUsesNamedPointerField()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1180);
		var expected = default(MuiMultiSetTargetEntry);
		expected.Target = APTR.FromPointer(0x1300);
		Assert.True(MuiMultiSetTargetEntryCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiMultiSetTargetEntryCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.Target, actual.Target);
		Assert.False(MuiMultiSetTargetEntryCodec.TryRead(ref platform,
			APTR.FromPointer(0x30000), out _));
	}

	[Fact]
	public void MultiSetTargetVectorUsesNamedCursorBoundary()
	{
		var platform = CreatePlatform(out _);
		var cursor = new MuiMultiSetTargetVectorCursor
		{
			Base = APTR.FromPointer(0x1800),
			Index = 2,
		};

		Assert.True(MuiMultiSetTargetVectorCodec.TryGetEntry(ref platform,
			cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x1808), address);
		cursor.Base = APTR.FromPointer(0x20FFE);
		cursor.Index = 0;
		Assert.False(MuiMultiSetTargetVectorCodec.TryGetEntry(ref platform,
			cursor, out _));
		cursor.Base = APTR.FromPointer(0x1800);
		cursor.Index = MuiMultiSetTargetVectorCursor.MaximumEntries;
		Assert.False(MuiMultiSetTargetVectorCodec.TryGetEntry(ref platform,
			cursor, out _));
	}

	[Fact]
	public void MultiSetUpdatesTheListedObjectsButNotItsExecutor()
	{
		var platform = CreatePlatform(out var cl);
		var executor = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var third = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, MuiNotifyCore.MultiSetMethod);
		platform.WriteUInt32(packet, 4, Attribute);
		platform.WriteUInt32(packet, 8, 0xCAFE);
		platform.WriteUInt32(packet, 12, first.Raw);
		platform.WriteUInt32(packet, 16, second.Raw);
		platform.WriteUInt32(packet, 20, third.Raw);
		platform.WriteUInt32(packet, 24, 0);
		Assert.Equal(1u, MuiHeadlessDispatcher.DispatchNotify(ref platform, State,
			executor, packet));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			executor, Attribute, out _));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, first,
			Attribute, out var firstValue));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, second,
			Attribute, out var secondValue));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, third,
			Attribute, out var thirdValue));
		Assert.Equal(0xCAFEu, firstValue);
		Assert.Equal(0xCAFEu, secondValue);
		Assert.Equal(0xCAFEu, thirdValue);
	}

	[Fact]
	public void MultiSetSkipsExecutorEvenWhenItAppearsInTheTargetVector()
	{
		var platform = CreatePlatform(out var cl);
		var executor = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var target = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, MuiNotifyCore.MultiSetMethod);
		platform.WriteUInt32(packet, 4, Attribute);
		platform.WriteUInt32(packet, 8, 7);
		platform.WriteUInt32(packet, 12, executor.Raw);
		platform.WriteUInt32(packet, 16, target.Raw);
		platform.WriteUInt32(packet, 20, 0);
		Assert.Equal(1u, MuiHeadlessDispatcher.DispatchNotify(ref platform, State,
			executor, packet));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			executor, Attribute, out _));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, target,
			Attribute, out var value));
		Assert.Equal(7u, value);
	}

	[Fact]
	public void MultiSetRejectsDeadTargetsAndTruncatedVectorsBeforeMutation()
	{
		var platform = CreatePlatform(out var cl);
		var executor = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var target = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, MuiNotifyCore.MultiSetMethod);
		platform.WriteUInt32(packet, 4, Attribute);
		platform.WriteUInt32(packet, 8, 9);
		platform.WriteUInt32(packet, 12, target.Raw);
		platform.WriteUInt32(packet, 16, 0x1F000);
		Assert.Equal(0u, MuiHeadlessDispatcher.DispatchNotify(ref platform, State,
			executor, packet));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State, target,
			Attribute, out _));

		packet = APTR.FromPointer(0x20FF0);
		platform.WriteUInt32(packet, 0, MuiNotifyCore.MultiSetMethod);
		platform.WriteUInt32(packet, 4, Attribute);
		platform.WriteUInt32(packet, 8, 10);
		platform.WriteUInt32(packet, 12, target.Raw);
		Assert.Equal(0u, MuiHeadlessDispatcher.DispatchNotify(ref platform, State,
			executor, packet));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State, target,
			Attribute, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR cl)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Notify.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
