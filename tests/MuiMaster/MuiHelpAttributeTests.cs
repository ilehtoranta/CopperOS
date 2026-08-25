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
		Assert.False(MuiHelpStateRecordCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
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
