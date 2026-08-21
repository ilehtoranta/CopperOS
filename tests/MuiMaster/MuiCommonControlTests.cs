using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiCommonControlTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ChoiceEntryCodecUsesNamedTextPointer()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1180);
		var expected = default(MuiChoiceEntry);
		expected.Text = APTR.FromPointer(0x1300);
		Assert.True(MuiChoiceEntryCodec.Write(ref platform, address, expected));
		Assert.True(MuiChoiceEntryCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.Text, actual.Text);
		Assert.False(MuiChoiceEntryCodec.TryRead(ref platform,
			APTR.FromPointer(0x30000), out _));
	}

	[Fact]
	public void ChoiceEntryFieldCursorUsesNamedTextBoundary()
	{
		var platform = CreatePlatform(out _);
		var record = APTR.FromPointer(0x11A0);
		var cursor = new MuiChoiceEntryFieldCursor
		{
			Record = record,
			Field = MuiChoiceEntryField.Text,
		};
		Assert.True(MuiChoiceEntryFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var address));
		Assert.Equal(record, address);
		Assert.True(MuiChoiceEntryFieldCursorCodec.TryWriteUInt32(ref platform,
			record, MuiChoiceEntryField.Text, 0x1300u));
		Assert.True(MuiChoiceEntryFieldCursorCodec.TryReadUInt32(ref platform,
			record, MuiChoiceEntryField.Text, out var text));
		Assert.Equal(0x1300u, text);
		Assert.False(MuiChoiceEntryFieldCursorCodec.TryReadUInt32(ref platform,
			record, unchecked((MuiChoiceEntryField)255), out _));
	}

	[Fact]
	public void ChoiceEntryCursorUsesNamedEntryBoundary()
	{
		var platform = CreatePlatform(out _);
		var cursor = default(MuiChoiceEntryCursor);
		cursor.Base = APTR.FromPointer(0x1200);
		cursor.Index = 4095;

		Assert.True(MuiChoiceEntryCursorCodec.TryGetEntry(ref platform, cursor,
			out var address));
		Assert.Equal(APTR.FromPointer(0x51FC), address);
		cursor.Index = 4096;
		Assert.False(MuiChoiceEntryCursorCodec.TryGetEntry(ref platform, cursor,
			out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFF0);
		cursor.Index = 0;
		Assert.False(MuiChoiceEntryCursorCodec.TryGetEntry(ref platform, cursor,
			out _));
	}

	[Fact]
	public void NumericPropGaugeAndChoicesClampNotifyAndRedraw()
	{
		var platform = CreatePlatform(out var cl);
		var control = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Set(ref platform, control, 0x8042E404, 10);
		Set(ref platform, control, 0x8042D78A, 20);
		Set(ref platform, control, 0x804263E8, 15);
		Assert.True(MuiCommonControlCore.SetNumericValue(ref platform, State,
			control, 100, true));
		Assert.Equal(20u, Get(ref platform, control, 0x8042AE3A));
		Assert.True(MuiCommonControlCore.SetNumericDefault(ref platform, State,
			control));
		Assert.Equal(15u, Get(ref platform, control, 0x8042AE3A));
		Set(ref platform, control, 0x80423661, 1);
		Assert.False(MuiCommonControlCore.ChangeNumeric(ref platform, State,
			control, 1));
		Set(ref platform, control, 0x80423661, 0);

		Set(ref platform, control, 0x8042FBDB, 100);
		Set(ref platform, control, 0x8042FEA6, 10);
		Set(ref platform, control, 0x8042D4B2, 5);
		Assert.True(MuiCommonControlCore.ChangeProp(ref platform, State, control,
			200));
		Assert.Equal(90u, Get(ref platform, control, 0x8042D4B2));
		Set(ref platform, control, 0x8042BCDB, 50);
		Assert.True(MuiCommonControlCore.SetGauge(ref platform, State, control,
			70));
		Assert.Equal(50u, Get(ref platform, control, 0x8042F0DD));

		var entries = APTR.FromPointer(0x1200);
		platform.WriteUInt32(entries, 0, 0x1300);
		platform.WriteUInt32(entries, 4, 0x1310);
		platform.WriteUInt32(entries, 8, 0);
		Assert.True(MuiCommonControlCore.SetChoice(ref platform, State, control,
			0x80421788, entries, -1));
		Assert.Equal(1u, Get(ref platform, control, 0x80421788));
		Assert.True(platform.RedrawCount >= 4);
	}

	[Fact]
	public void ExactNumericPacketsScaleAndChangeValues()
	{
		var platform = CreatePlatform(out var cl);
		var control = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Set(ref platform, control, 0x8042E404, 0);
		Set(ref platform, control, 0x8042D78A, 100);
		Set(ref platform, control, 0x8042AE3A, 50);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0x80426ECD);
		platform.WriteUInt32(packet, 4, 10);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			control, packet));
		Assert.Equal(60u, Get(ref platform, control, 0x8042AE3A));
		platform.WriteUInt32(packet, 0, 0x80423E4F);
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, 1000);
		Assert.Equal(600u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			control, packet));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR cl)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Numeric.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static void Set(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute, uint value) => Assert.True(
		MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj, attribute,
			value, false));

	private static uint Get(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			attribute, out var value));
		return value;
	}
}
