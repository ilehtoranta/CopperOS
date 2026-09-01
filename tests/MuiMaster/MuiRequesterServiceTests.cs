using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiRequesterServiceTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private static readonly APTR Application = APTR.FromPointer(0x1200);
	private static readonly APTR Window = APTR.FromPointer(0x1220);
	private static readonly APTR Title = APTR.FromPointer(0x1240);
	private static readonly APTR Gadgets = APTR.FromPointer(0x1260);
	private static readonly APTR Format = APTR.FromPointer(0x1280);
	private static readonly APTR Parameters = APTR.FromPointer(0x12A0);
	private static readonly APTR Object = APTR.FromPointer(0x1300);

	[Fact]
	public void RequestARequiresInitializationZeroFlagsAndPreservesGuestArguments()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		WriteCString(ref platform, Title, "MorphOS requester");
		WriteCString(ref platform, Gadgets, "_Ok|*_Cancel");
		WriteCString(ref platform, Format, "Body");
		Assert.Equal(0, MuiRequesterServiceCore.Request(ref platform, State,
			Application, Window, 0, Title, Gadgets, Format, Parameters));
		Assert.True(MuiRequesterServiceCore.Initialize(ref platform, State));

		platform.RequestResult = 3;
		Assert.Equal(0, MuiRequesterServiceCore.Request(ref platform, State,
			Application, Window, 7, Title, Gadgets, Format, Parameters));
		Assert.Equal(0u, platform.RequestCallCount);
		Assert.Equal(3, MuiRequesterServiceCore.Request(ref platform, State,
			Application, Window, 0, Title, Gadgets, Format, Parameters));
		Assert.Equal(1u, platform.RequestCallCount);
		Assert.Equal(Application, platform.LastRequestApplication);
		Assert.Equal(Window, platform.LastRequestWindow);
		Assert.Equal(Title, platform.LastRequestTitle);
		Assert.Equal(Gadgets, platform.LastRequestGadgets);
		Assert.Equal(Format, platform.LastRequestFormat);
		Assert.Equal(Parameters, platform.LastRequestParameters);
	}

	[Fact]
	public void RequestObjectConsumesOneReferenceSoCallerMustRetainToReuseObject()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		WriteCString(ref platform, Title, "Object requester");
		WriteCString(ref platform, Gadgets, "Yes|No");
		WriteCString(ref platform, Format, "Proceed?");
		// Two references model the caller's documented OM_RETAIN before entry.
		platform.WriteUInt32(Object, 4, 2);
		Assert.True(MuiRequesterServiceCore.Initialize(ref platform, State));
		platform.RequestObjectResult = 5;

		Assert.Equal(0, MuiRequesterServiceCore.RequestObject(ref platform, State,
			Application, Window, 9, Title, Gadgets, Object, Format, Parameters));
		Assert.Equal(0u, platform.RequestObjectCallCount);
		Assert.Equal(5, MuiRequesterServiceCore.RequestObject(ref platform, State,
			Application, Window, 0, Title, Gadgets, Object, Format, Parameters));
		Assert.Equal(1u, platform.RequestObjectCallCount);
		Assert.Equal(Object, platform.LastRequestObject);
		Assert.Equal(0u, platform.ObjectRetainCount);
		Assert.Equal(1u, platform.ObjectReleaseCount);
		Assert.Equal(Object, platform.LastReleasedObject);
		Assert.Equal(1u, platform.ReadUInt32(Object, 4));
		Assert.Equal(0, MuiRequesterServiceCore.RequestObject(ref platform, State,
			Application, Window, 0, Title, Gadgets, APTR.Null, Format, Parameters));
		Assert.Equal(1u, platform.RequestObjectCallCount);
	}

	[Fact]
	public void RequestPayloadCountsGadgetsAndKeepsParametersOpaque()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		WriteCString(ref platform, Title, "Title");
		WriteCString(ref platform, Gadgets, "One|Two|Three");
		WriteCString(ref platform, Format, "Text");
		uint count;
		Assert.True(MuiRequesterPayloadCore.TryGetGadgetCount(ref platform,
			Gadgets, out count));
		Assert.Equal(3u, count);
		Assert.True(MuiRequesterServiceCore.Initialize(ref platform, State));
		Assert.Equal(1, MuiRequesterServiceCore.Request(ref platform, State,
			Application, Window, 0, Title, Gadgets, Format, Parameters));
		Assert.Equal(Parameters, platform.LastRequestParameters);
	}

	[Fact]
	public void RequesterParameterSlotCodecUsesNamedValue()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		platform.WriteUInt32(Parameters, 0, 7);
		Assert.True(MuiRequesterParameterSlotMemoryCodec.TryGetAddress(ref platform,
			Parameters, MuiRequesterParameterSlotField.Value, out var valueAddress));
		Assert.Equal(Parameters, valueAddress);
		Assert.True(MuiRequesterParameterSlotMemoryCodec.TryReadUInt32(ref platform,
			Parameters, MuiRequesterParameterSlotField.Value, out var directValue));
		Assert.Equal(7u, directValue);
		Assert.True(MuiRequesterParameterSlotCodec.TryRead(ref platform,
			Parameters, out var slot));
		Assert.Equal(7u, slot.Value);

		slot.Value = 0xABCD;
		Assert.True(MuiRequesterParameterSlotCodec.Write(ref platform,
			Parameters, slot));
		Assert.True(MuiRequesterParameterSlotCodec.TryRead(ref platform,
			Parameters, out var updated));
		Assert.Equal(0xABCDu, updated.Value);
	}

	[Fact]
	public void RequesterServiceStateMemoryAdapterOwnsStructBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var state = APTR.FromPointer(0x1A00);
		Assert.True(MuiRequesterServiceStateMemoryCodec.TryGetAddress(ref platform,
			state, MuiRequesterServiceStateField.Generation, out var generation));
		Assert.Equal(APTR.FromPointer(0x1A04), generation);
		Assert.True(MuiRequesterServiceStateMemoryCodec.TryWriteUInt32(ref platform,
			state, MuiRequesterServiceStateField.Generation, 3));
		Assert.True(MuiRequesterServiceStateMemoryCodec.TryReadUInt32(ref platform,
			state, MuiRequesterServiceStateField.Generation, out var version));
		Assert.Equal(3u, version);
		Assert.False(MuiRequesterServiceStateMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x20FFC), MuiRequesterServiceStateField.Magic,
			out _));
		Assert.False(MuiRequesterServiceStateMemoryCodec.TryGetAddress(ref platform,
			state, (MuiRequesterServiceStateField)255, out _));
	}

	[Fact]
	public void RequesterParameterCursorUsesNamedEntryBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var cursor = default(MuiRequesterParameterCursor);
		cursor.Base = Parameters;
		cursor.Index = 2;
		Assert.True(MuiRequesterParameterCursorCodec.TryGetEntry(ref platform,
			cursor, out var address));
		Assert.Equal(APTR.FromPointer(Parameters.Raw + 8), address);
		cursor.Index = MuiRequesterParameterCursor.MaximumEntries;
		Assert.False(MuiRequesterParameterCursorCodec.TryGetEntry(ref platform,
			cursor, out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFF0);
		cursor.Index = 0;
		Assert.False(MuiRequesterParameterCursorCodec.TryGetEntry(ref platform,
			cursor, out _));
	}

	[Fact]
	public void RequesterParameterVectorMemoryAdapterOwnsEntryBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var vector = Parameters;
		Assert.True(MuiRequesterParameterVectorMemoryCodec.TryGetEntry(
			ref platform, vector, 2, out var address));
		Assert.Equal(APTR.FromPointer(vector.Raw + 8), address);
		Assert.False(MuiRequesterParameterVectorMemoryCodec.TryGetEntry(
			ref platform, vector,
			MuiRequesterParameterCursor.MaximumEntries, out _));
		Assert.False(MuiRequesterParameterVectorMemoryCodec.TryGetEntry(
			ref platform, APTR.FromPointer(0x20FFE), 0, out _));
		Assert.False(MuiRequesterParameterVectorMemoryCodec.TryGetEntry(
			ref platform, APTR.FromPointer(0xFFFFFFF0), 4, out _));
	}

	[Fact]
	public void RequesterParameterVectorBridgeUsesNamedSlots()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var expected = new MuiRequesterParameterSlot
		{
			Value = 0xFEDCBA98u,
		};

		Assert.True(MuiRequesterParameterVectorCodec.TryWrite(ref platform,
			Parameters, 2, expected));
		Assert.True(MuiRequesterParameterVectorCodec.TryRead(ref platform,
			Parameters, 2, out var decoded));
		Assert.Equal(expected.Value, decoded.Value);
		Assert.True(MuiRequesterParameterVectorCodec.TryReadValue(ref platform,
			Parameters, 2, out var rawValue));
		Assert.Equal(expected.Value, rawValue);
		Assert.True(MuiRequesterParameterVectorCodec.TryWriteValue(ref platform,
			Parameters, 3, 0x80000001u));
		Assert.True(MuiRequesterParameterVectorCodec.TryReadValue(ref platform,
			Parameters, 3, out rawValue));
		Assert.Equal(0x80000001u, rawValue);
		Assert.False(MuiRequesterParameterVectorCodec.TryReadValue(ref platform,
			Parameters, MuiRequesterParameterCursor.MaximumEntries, out _));
		Assert.False(MuiRequesterParameterVectorCodec.TryWriteValue(ref platform,
			APTR.FromPointer(0xFFFFFFF0), 4, expected.Value));
	}

	[Fact]
	public void RequesterParameterCursorExchangesCompleteNamedSlots()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var cursor = default(MuiRequesterParameterCursor);
		cursor.Base = Parameters;
		cursor.Index = 2;
		const uint expected = 0xFEDCBA98u;

		Assert.True(MuiRequesterParameterCursorCodec.TryWriteValue(ref platform,
			cursor, expected));
		Assert.True(MuiRequesterParameterCursorCodec.TryReadValue(ref platform,
			cursor, out var actual));
		Assert.Equal(expected, actual);

		var slot = default(MuiRequesterParameterSlot);
		slot.Value = 0x10203040u;
		Assert.True(MuiRequesterParameterCursorCodec.TryWrite(ref platform, cursor,
			slot));
		Assert.True(MuiRequesterParameterCursorCodec.TryRead(ref platform, cursor,
			out var decoded));
		Assert.Equal(slot.Value, decoded.Value);

		cursor.Index = MuiRequesterParameterCursor.MaximumEntries;
		Assert.False(MuiRequesterParameterCursorCodec.TryReadValue(ref platform,
			cursor, out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Index = 1;
		Assert.False(MuiRequesterParameterCursorCodec.TryWriteValue(ref platform,
			cursor, expected));
	}

	[Fact]
	public void RequestFormatCountsConversionsAndStarArguments()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		WriteCString(ref platform, Title, "Title");
		WriteCString(ref platform, Gadgets, "Ok|Cancel");
		WriteCString(ref platform, Format, "Value %*.*ld %s %% %c");
		var text = APTR.FromPointer(0x1400);
		WriteCString(ref platform, text, "hello");
		platform.WriteUInt32(Parameters, 0, 4);       // width
		platform.WriteUInt32(Parameters, 4, 2);       // precision
		platform.WriteUInt32(Parameters, 8, 7);       // value
		platform.WriteUInt32(Parameters, 12, text.Raw); // string
		platform.WriteUInt32(Parameters, 16, (uint)'Z'); // character
		uint count;
		Assert.True(MuiRequesterPayloadCore.TryGetFormatParameterCount(ref platform,
			Format, out count));
		Assert.Equal(5u, count);
		Assert.True(MuiRequesterServiceCore.Initialize(ref platform, State));
		Assert.Equal(1, MuiRequesterServiceCore.Request(ref platform, State,
			Application, Window, 0, Title, Gadgets, Format, Parameters));
		Assert.Equal(1u, platform.RequestCallCount);
		Assert.Equal(APTR.Null, platform.LastRequestParameters);
		Assert.Equal("Value   07 hello % Z", ReadCString(ref platform,
			platform.LastRequestFormat));
	}

	[Fact]
	public void RequestFormatSupportsIntegerStringCharacterAndLiteralPercent()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		WriteCString(ref platform, Format, "n=%+06ld p=%08x s=%.3s c=%c %%");
		var text = APTR.FromPointer(0x1400);
		WriteCString(ref platform, text, "hello");
		platform.WriteUInt32(Parameters, 0, unchecked((uint)-12));
		platform.WriteUInt32(Parameters, 4, 0x2Au);
		platform.WriteUInt32(Parameters, 8, text.Raw);
		platform.WriteUInt32(Parameters, 12, (uint)'Q');
		Assert.True(MuiRequesterFormatCore.TryMaterialize(ref platform, Format,
			Parameters, out var result, out var allocation));
		Assert.NotEqual(Format, result);
		Assert.True(allocation != 0);
		Assert.Equal("n=-00012 p=0000002a s=hel c=Q %", ReadCString(ref platform,
			result));
		platform.Free(result, allocation);
	}

	[Fact]
	public void RequestFormatRejectsUnsupportedConversionBeforeCapability()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		WriteCString(ref platform, Title, "Title");
		WriteCString(ref platform, Gadgets, "Ok");
		WriteCString(ref platform, Format, "Value %f");
		platform.WriteUInt32(Parameters, 0, 1);
		Assert.True(MuiRequesterServiceCore.Initialize(ref platform, State));
		Assert.Equal(0, MuiRequesterServiceCore.Request(ref platform, State,
			Application, Window, 0, Title, Gadgets, Format, Parameters));
		Assert.Equal(0u, platform.RequestCallCount);
	}

	[Fact]
	public void RequestFormatAllocationFailureIsAtomic()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x2000, 0x2F00,
			State);
		WriteCString(ref platform, Title, "Title");
		WriteCString(ref platform, Gadgets, "Ok");
		WriteCString(ref platform, Format, "Value %ld");
		platform.WriteUInt32(Parameters, 0, 7);
		Assert.True(MuiRequesterServiceCore.Initialize(ref platform, State));
		Assert.Equal(0, MuiRequesterServiceCore.Request(ref platform, State,
			Application, Window, 0, Title, Gadgets, Format, Parameters));
		Assert.Equal(0u, platform.RequestCallCount);
	}

	[Fact]
	public void RequestPayloadRejectsUnterminatedOrUnmappedTextBeforeCapability()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		WriteCString(ref platform, Title, "Title");
		WriteCString(ref platform, Gadgets, "Ok|Cancel");
		WriteCString(ref platform, Format, "Text");
		Assert.True(MuiRequesterServiceCore.Initialize(ref platform, State));

		var badFormat = APTR.FromPointer(0x21000);
		Assert.Equal(0, MuiRequesterServiceCore.Request(ref platform, State,
			Application, Window, 0, Title, Gadgets, badFormat, Parameters));
		Assert.Equal(0u, platform.RequestCallCount);
		platform.WriteUInt32(Object, 4, 1);
		Assert.Equal(0, MuiRequesterServiceCore.RequestObject(ref platform, State,
			Application, Window, 0, Title, Gadgets, Object, badFormat,
			Parameters));
		Assert.Equal(0u, platform.RequestObjectCallCount);
		Assert.Equal(0u, platform.ObjectRetainCount);

		var unterminated = APTR.FromPointer(0x1400);
		for (var index = 0; index < (int)MuiRequesterPayloadCore.MaximumStringLength;
			index++) platform.WriteUInt8(unterminated, index, (byte)'x');
		Assert.Equal(0, MuiRequesterServiceCore.Request(ref platform, State,
			Application, Window, 0, Title, Gadgets, unterminated, Parameters));
		Assert.Equal(0u, platform.RequestCallCount);
	}

	[Fact]
	public void RequestFormatRejectsMissingParametersAndMalformedPercent()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		WriteCString(ref platform, Title, "Title");
		WriteCString(ref platform, Gadgets, "Ok");
		Assert.True(MuiRequesterServiceCore.Initialize(ref platform, State));

		WriteCString(ref platform, Format, "Value %s");
		Assert.Equal(0, MuiRequesterServiceCore.Request(ref platform, State,
			Application, Window, 0, Title, Gadgets, Format, APTR.Null));
		Assert.Equal(0u, platform.RequestCallCount);

		WriteCString(ref platform, Format, "Broken %");
		Assert.Equal(0, MuiRequesterServiceCore.Request(ref platform, State,
			Application, Window, 0, Title, Gadgets, Format, Parameters));
		Assert.Equal(0u, platform.RequestCallCount);
	}

	private static void WriteCString(ref MuiHeadlessTestPlatform platform,
		APTR address, string value)
	{
		for (var index = 0; index < value.Length; index++)
			platform.WriteUInt8(address, index, (byte)value[index]);
		platform.WriteUInt8(address, value.Length, 0);
	}

	private static string ReadCString(ref MuiHeadlessTestPlatform platform,
		APTR address)
	{
		var result = new System.Text.StringBuilder();
		for (var index = 0; index < 4096; index++)
		{
			var value = platform.ReadUInt8(address, index);
			if (value == 0) break;
			result.Append((char)value);
		}
		return result.ToString();
	}
}
