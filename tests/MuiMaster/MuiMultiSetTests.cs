using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiMultiSetTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint Attribute = 0x80420030;
	private const uint PropEntries = 0x8042FBDB;
	private const uint PropFirst = 0x8042D4B2;
	private const uint PropVisible = 0x8042FEA6;

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
	public void MultiSetTargetVectorBridgeUsesNamedPointerRecords()
	{
		var platform = CreatePlatform(out _);
		var vector = APTR.FromPointer(0x1800);
		var expected = new MuiMultiSetTargetEntry
		{
			Target = APTR.FromPointer(0xFEDCBA98u),
		};

		Assert.True(MuiMultiSetTargetVectorCodec.TryWrite(ref platform, vector, 2,
			expected));
		Assert.True(MuiMultiSetTargetVectorCodec.TryRead(ref platform, vector, 2,
			out var decoded));
		Assert.Equal(expected.Target.Raw, decoded.Target.Raw);
		Assert.True(MuiMultiSetTargetVectorCodec.TryReadValue(ref platform, vector,
			2, out var rawTarget));
		Assert.Equal(expected.Target.Raw, rawTarget);
		Assert.True(MuiMultiSetTargetVectorCodec.TryWriteValue(ref platform, vector,
			3, 0x80000001u));
		Assert.True(MuiMultiSetTargetVectorCodec.TryReadValue(ref platform, vector,
			3, out rawTarget));
		Assert.Equal(0x80000001u, rawTarget);
		Assert.False(MuiMultiSetTargetVectorCodec.TryReadValue(ref platform, vector,
			MuiMultiSetTargetVectorCursor.MaximumEntries, out _));
		Assert.False(MuiMultiSetTargetVectorCodec.TryWriteValue(ref platform,
			APTR.FromPointer(0xFFFFFFF0), 0, expected.Target.Raw));
	}

	[Fact]
	public void MultiSetTargetCursorExchangesCompleteRecords()
	{
		var platform = CreatePlatform(out _);
		var cursor = new MuiMultiSetTargetVectorCursor
		{
			Base = APTR.FromPointer(0x1800),
			Index = 0,
		};
		var expected = new MuiMultiSetTargetEntry
		{
			Target = APTR.FromPointer(0xFEDCBA98u),
		};

		Assert.True(MuiMultiSetTargetVectorCodec.TryWrite(ref platform, cursor,
			expected));
		Assert.True(MuiMultiSetTargetVectorCodec.TryRead(ref platform, cursor,
			out var actual));
		Assert.Equal(expected.Target.Raw, actual.Target.Raw);
		Assert.True(MuiMultiSetTargetVectorCodec.TryWrite(ref platform, cursor,
			ref expected));
		actual = default;
		Assert.True(MuiMultiSetTargetVectorCodec.TryReadInto(ref platform, cursor,
			ref actual));
		Assert.Equal(expected.Target.Raw, actual.Target.Raw);
		Assert.True(MuiMultiSetTargetVectorCodec.TryAdvance(ref cursor, 1));
		Assert.Equal(1u, cursor.Index);
		Assert.False(MuiMultiSetTargetVectorCodec.TryAdvance(ref cursor,
			MuiMultiSetTargetVectorCursor.MaximumEntries));
		cursor.Index = MuiMultiSetTargetVectorCursor.MaximumEntries;
		Assert.False(MuiMultiSetTargetVectorCodec.TryRead(ref platform, cursor,
			out _));
	}

	[Fact]
	public void MultiSetPacketCodecUsesCompleteNamedRecord()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		var expected = new MuiMultiSetMessage
		{
			MethodId = MuiNotifyCore.MultiSetMethod,
			Attribute = Attribute,
			Value = 0xCAFE,
			FirstObject = 0x1300,
		};

		Assert.True(MuiMultiSetMessageCodec.Write(ref platform, packet, expected));
		Assert.True(MuiMultiSetMessageCodec.TryRead(ref platform, packet,
			out var actual));
		Assert.Equal(expected.MethodId, actual.MethodId);
		Assert.Equal(expected.Attribute, actual.Attribute);
		Assert.Equal(expected.Value, actual.Value);
		Assert.Equal(expected.FirstObject, actual.FirstObject);
		Assert.True(MuiNotifyCore.TryReadMultiSet(ref platform, packet,
			MuiNotifyCore.MultiSetMethod, out var admitted));
		Assert.Equal(expected.FirstObject, admitted.FirstObject);

		Assert.False(MuiMultiSetMessageCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
		Assert.False(MuiMultiSetMessageCodec.Write(ref platform,
			APTR.FromPointer(0x20FFFu), expected));
		platform.WriteUInt32(packet, 0, 0xDEADBEEFu);
		Assert.False(MuiNotifyCore.TryReadMultiSet(ref platform, packet,
			MuiNotifyCore.MultiSetMethod, out _));
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
		Assert.True(MuiMultiSetMessageCodec.Write(ref platform, packet,
			new MuiMultiSetMessage
			{
				MethodId = MuiNotifyCore.MultiSetMethod,
				Attribute = Attribute,
				Value = 0xCAFE,
				FirstObject = first.Raw,
			}));
		var vector = MuiNotifyCore.MultiSetVector(ref platform, packet);
		Assert.True(MuiMultiSetTargetEntryCodec.Write(ref platform, vector,
			new MuiMultiSetTargetEntry { Target = second }));
		Assert.True(MuiMultiSetTargetEntryCodec.Write(ref platform,
			APTR.FromPointer(vector.Raw + MuiMultiSetTargetEntry.Size),
			new MuiMultiSetTargetEntry { Target = third }));
		Assert.True(MuiMultiSetTargetEntryCodec.Write(ref platform,
			APTR.FromPointer(vector.Raw + 2 * MuiMultiSetTargetEntry.Size),
			default));
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
		Assert.True(MuiMultiSetMessageCodec.Write(ref platform, packet,
			new MuiMultiSetMessage
			{
				MethodId = MuiNotifyCore.MultiSetMethod,
				Attribute = Attribute,
				Value = 7,
				FirstObject = executor.Raw,
			}));
		var vector = MuiNotifyCore.MultiSetVector(ref platform, packet);
		Assert.True(MuiMultiSetTargetEntryCodec.Write(ref platform, vector,
			new MuiMultiSetTargetEntry { Target = target }));
		Assert.True(MuiMultiSetTargetEntryCodec.Write(ref platform,
			APTR.FromPointer(vector.Raw + MuiMultiSetTargetEntry.Size),
			default));
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
		Assert.True(MuiMultiSetMessageCodec.Write(ref platform, packet,
			new MuiMultiSetMessage
			{
				MethodId = MuiNotifyCore.MultiSetMethod,
				Attribute = Attribute,
				Value = 9,
				FirstObject = target.Raw,
			}));
		var vector = MuiNotifyCore.MultiSetVector(ref platform, packet);
		Assert.True(MuiMultiSetTargetEntryCodec.Write(ref platform, vector,
			new MuiMultiSetTargetEntry { Target = APTR.FromPointer(0x1F000) }));
		Assert.Equal(0u, MuiHeadlessDispatcher.DispatchNotify(ref platform, State,
			executor, packet));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State, target,
			Attribute, out _));

		packet = APTR.FromPointer(0x20FF0);
		Assert.True(MuiMultiSetMessageCodec.Write(ref platform, packet,
			new MuiMultiSetMessage
			{
				MethodId = MuiNotifyCore.MultiSetMethod,
				Attribute = Attribute,
				Value = 10,
				FirstObject = target.Raw,
			}));
		Assert.Equal(0u, MuiHeadlessDispatcher.DispatchNotify(ref platform, State,
			executor, packet));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State, target,
			Attribute, out _));
	}

	[Fact]
	public void MultiSetProjectsPropRangeThroughNamedRecordsAndClampsFirst()
	{
		var platform = CreatePlatform(out var executorClass);
		var propName = APTR.FromPointer(0x1140);
		platform.WriteCString(propName, "Prop.mui");
		var propClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			propName, APTR.Null, 0, APTR.FromPointer(1), false);
		var executor = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			executorClass, APTR.Null);
		var first = MuiCommonControlCore.CreateControl(ref platform, State,
			propClass, APTR.Null);
		var second = MuiCommonControlCore.CreateControl(ref platform, State,
			propClass, APTR.Null);
		Assert.NotEqual(APTR.Null, first);
		Assert.NotEqual(APTR.Null, second);
		foreach (var prop in new[] { first, second })
		{
			Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
				prop, PropEntries, 100, false));
			Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
				prop, PropVisible, 10, false));
			Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
				prop, PropFirst, 80, false));
		}

		var vector = APTR.FromPointer(0x1300);
		platform.WriteUInt32(vector, 0, second.Raw);
		platform.WriteUInt32(vector, 4, 0);
		Assert.True(MuiNotifyCore.MultiSet(ref platform, State, executor,
			PropVisible, 30, first, vector));

		Assert.True(MuiCommonControlCore.TryGetPropRangeStateRecord(ref platform,
			State, first, out var firstRecord));
		Assert.True(MuiCommonControlCore.TryGetPropRangeStateRecord(ref platform,
			State, second, out var secondRecord));
		Assert.Equal(30u, firstRecord.Visible);
		Assert.Equal(70u, firstRecord.First);
		Assert.Equal(30u, secondRecord.Visible);
		Assert.Equal(70u, secondRecord.First);
		Assert.Equal(70u, Get(ref platform, first, PropFirst));
		Assert.Equal(70u, Get(ref platform, second, PropFirst));
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

	private static uint Get(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, obj, attribute,
			out var value, out var handled));
		Assert.True(handled);
		return value;
	}
}
