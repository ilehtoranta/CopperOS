using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiHelpAttributeTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void HelpStateRecordUsesNamedFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1500);
		var expected = default(MuiHelpStateRecord);
		expected.Magic = MuiHelpStateRecord.Cookie;
		expected.Node = APTR.FromPointer(0x1800);
		expected.Line = unchecked((uint)-7);
		expected.Generation = 3;

		Assert.True(MuiHelpStateRecordCodec.Write(ref platform, address, expected));
		Assert.True(MuiHelpStateRecordCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Node, actual.Node);
		Assert.Equal(expected.Line, actual.Line);
		Assert.Equal(expected.Generation, actual.Generation);

		var cursor = default(MuiHelpStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiHelpStateField.Line;
		Assert.True(MuiHelpStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var lineAddress));
		Assert.Equal(address.Raw + 8, lineAddress.Raw);
		Assert.True(MuiHelpStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var sizedLineAddress, out var fieldSize));
		Assert.Equal(lineAddress, sizedLineAddress);
		Assert.Equal(MuiHelpStateRecord.FieldSize, fieldSize);
		Assert.True(MuiHelpStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out var memoryLineAddress, out var memoryFieldSize));
		Assert.Equal(lineAddress, memoryLineAddress);
		Assert.Equal(MuiHelpStateRecord.FieldSize, memoryFieldSize);
		cursor.Record = APTR.Null;
		cursor.Field = MuiHelpStateField.Magic;
		Assert.False(MuiHelpStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
		Assert.False(MuiHelpStateRecordCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
	}

	[Fact]
	public void HelpStateRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1D00);
		var record = new MuiHelpStateRecord
		{
			Magic = MuiHelpStateRecord.Cookie,
			Node = APTR.FromPointer(0x1D40),
			Line = unchecked((uint)-7),
			Generation = 3,
		};
		Assert.True(MuiHelpStateRecordCodec.Write(ref platform, address, record));
		Assert.True(MuiHelpStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiHelpStateField.Node, out var typedNodeAddress));
		Assert.Equal(0x1D04u, typedNodeAddress.Raw);
		Assert.True(MuiHelpStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiHelpStateField.Line, out var typedLine));
		Assert.Equal(unchecked((uint)-7), typedLine);
		Assert.True(MuiHelpStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiHelpStateField.Generation, 4));
		Assert.True(MuiHelpStateRecordCodec.TryReadStructural(ref platform, address,
			out var typedUpdated));
		Assert.Equal(4u, typedUpdated.Generation);
		Assert.Equal(record.Node, typedUpdated.Node);
		Assert.Equal(record.Line, typedUpdated.Line);
		Assert.False(MuiHelpStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, (MuiHelpStateField)255, out _));
		Assert.True(MuiHelpStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiHelpStateField.Generation, record.Generation));
		Assert.True(MuiHelpStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, 8, out var lineAddress));
		Assert.Equal(0x1D08u, lineAddress.Raw);
		Assert.True(MuiHelpStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out var node));
		Assert.Equal(0x1D40u, node);
		Assert.True(MuiHelpStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 12, 4));
		Assert.True(MuiHelpStateRecordCodec.TryReadStructural(ref platform,
			address, out var updated));
		Assert.Equal(4u, updated.Generation);
		Assert.False(MuiHelpStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiHelpStateRecord.Size, out _));
		Assert.False(MuiHelpStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, (uint)0, out _));
		Assert.False(MuiHelpStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void HelpStateSequentialRecordPreservesPointerSignedLineAndBounds()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1D40);
		var value = new MuiHelpStateRecord
		{
			Magic = MuiHelpStateRecord.Cookie,
			Node = APTR.FromPointer(0xFEEDBEEF),
			Line = unchecked((uint)-7),
			Generation = uint.MaxValue,
		};

		Assert.True(MuiHelpStateRecordCodec.WriteRecord(ref platform, address,
			value));
		Assert.True(MuiHelpStateRecordCodec.TryReadRecord(ref platform, address,
			out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Node, decoded.Node);
		Assert.Equal(value.Line, decoded.Line);
		Assert.Equal(value.Generation, decoded.Generation);

		var crossingEnd = APTR.FromPointer(0x40FFF);
		Assert.False(MuiHelpStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiHelpStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}

	[Fact]
	public void GenericAndDispatcherAccessUseNamedHelpStateForExternalObjects()
	{
		var platform = CreatePlatform(out var classRecord);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			classRecord, APTR.Null);
		var node = APTR.FromPointer(0x1900);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiHelpStateCore.HelpNode, node.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiHelpStateCore.HelpLine, unchecked((uint)-12), false));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			MuiHelpStateCore.HelpNode, out var rawNode));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			MuiHelpStateCore.HelpLine, out var rawLine));
		Assert.Equal(node.Raw, rawNode);
		Assert.Equal(unchecked((uint)-12), rawLine);

		var set = APTR.FromPointer(0x1300);
		platform.WriteUInt32(set, 0, MuiCommonControlPacketCore.Set);
		platform.WriteUInt32(set, 4, MuiHelpStateCore.HelpLine);
		platform.WriteUInt32(set, 8, unchecked((uint)34));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, set));

		var storage = APTR.FromPointer(0x1400);
		platform.WriteUInt32(set, 0, MuiCommonControlPacketCore.NoNotifySet);
		platform.WriteUInt32(set, 4, MuiHelpStateCore.HelpLine);
		platform.WriteUInt32(set, 8, unchecked((uint)35));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, set));

		platform.WriteUInt32(set, 0, MuiCommonControlPacketCore.OmGet);
		platform.WriteUInt32(set, 4, MuiHelpStateCore.HelpLine);
		platform.WriteUInt32(set, 8, storage.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, set));
		Assert.Equal(35u, platform.ReadUInt32(storage, 0));

		Assert.True(MuiHelpStateCore.TryReadState(ref platform, State, obj,
			out var state));
		Assert.Equal(node, state.Node);
		Assert.Equal(35, state.Line);
		Assert.Equal((int)MuiHelpStateRecord.Size,
			MuiStoreCore.DataspaceLength(ref platform, State, obj,
				MuiHelpStateCore.StateKey));
	}

	[Fact]
	public void RawBootstrapIsReconciledWithoutReplacingTheNamedRecord()
	{
		var platform = CreatePlatform(out var classRecord);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			classRecord, APTR.Null);
		var record = MuiHeadlessObjectCore.FindObject(ref platform, State, obj);
		var node = APTR.FromPointer(0x1A00);
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			record, MuiHelpStateCore.HelpNode, node.Raw, false));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			MuiHelpStateCore.HelpNode, out var raw));
		Assert.Equal(node.Raw, raw);
		Assert.True(MuiHelpStateCore.TryReadState(ref platform, State, obj,
			out var state));
		Assert.Equal(node, state.Node);
	}

	[Fact]
	public void HelpStateAdmissionRequiresCookieGenerationAndLiveOwner()
	{
		var platform = CreatePlatform(out var classRecord);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			classRecord, APTR.Null);
		var valid = default(MuiHelpStateRecord);
		valid.Magic = MuiHelpStateRecord.Cookie;
		valid.Node = APTR.FromPointer(0x1B00);
		valid.Line = unchecked((uint)-7);
		valid.Generation = 1;
		Assert.True(MuiHelpStateAdmission.Validate(valid));
		Assert.True(MuiHelpStateAdmission.ValidateLive(ref platform, State, obj,
			valid));
		var malformed = valid;
		malformed.Generation = 0;
		Assert.False(MuiHelpStateAdmission.Validate(malformed));
		Assert.False(MuiHelpStateAdmission.ValidateLive(ref platform, State, obj,
			malformed));
		malformed = valid;
		malformed.Magic = 0;
		Assert.False(MuiHelpStateAdmission.Validate(malformed));
		Assert.False(MuiHelpStateAdmission.ValidateLive(ref platform, State, obj,
			malformed));
		Assert.False(MuiHelpStateAdmission.ValidateLive(ref platform, State,
			APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedHelpStateFailsClosedBeforeResolutionAndMutation()
	{
		var platform = CreatePlatform(out var classRecord);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			classRecord, APTR.Null);
		var node = APTR.FromPointer(0x1C00);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiHelpStateCore.HelpNode, node.Raw, false));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, obj,
			MuiHelpStateCore.StateKey);
		Assert.True(MuiHelpStateRecordCodec.TryReadStructural(ref platform, block,
			out var before));
		Assert.Equal(MuiHelpStateRecord.Cookie, before.Magic);
		var cursor = default(MuiHelpStateFieldCursor);
		cursor.Record = block;
		cursor.Field = MuiHelpStateField.Generation;
		Assert.True(MuiHelpStateFieldCursorCodec.TryWriteUInt32(ref platform,
			block, cursor.Field, 0));
		Assert.True(MuiHelpStateRecordCodec.TryReadStructural(ref platform, block,
			out var malformed));
		Assert.Equal(0u, malformed.Generation);
		Assert.False(MuiHelpStateAdmission.Validate(malformed));
		Assert.False(MuiHelpStateCore.TryReadState(ref platform, State, obj,
			out _));
		Assert.False(MuiHelpStateCore.TryResolve(ref platform, State, obj,
			out _));
		Assert.False(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiHelpStateCore.HelpLine, unchecked((uint)9), false));
		Assert.True(MuiHelpStateRecordCodec.TryReadStructural(ref platform, block,
			out var after));
		Assert.Equal(0u, after.Generation);
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State, obj,
			MuiHelpStateCore.HelpNode, out var rawNode));
		Assert.Equal(node.Raw, rawNode);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR classRecord)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "ExternalHelp.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		classRecord = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
