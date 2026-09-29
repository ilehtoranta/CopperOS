using Amiga;
using Amiga.MUI;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiRequesterServiceTests
{
	private struct RequesterWindowCapability : IMuiRequesterWindowCapability
	{
		internal APTR ExpectedMuiWindow;
		internal APTR NativeWindow;
		internal APTR LastMuiWindow;
		internal uint LookupCount;
		internal bool LookupSucceeds;

		public bool TryGetNativeWindow(APTR muiWindow,
			out APTR intuitionWindow)
		{
			LookupCount++;
			LastMuiWindow = muiWindow;
			intuitionWindow = APTR.Null;
			if (!LookupSucceeds || muiWindow != ExpectedMuiWindow) return false;
			intuitionWindow = NativeWindow;
			return true;
		}
	}

	private static readonly APTR State = APTR.FromPointer(0x1000);
	private static readonly APTR Application = APTR.FromPointer(0x1200);
	private static readonly APTR Window = APTR.FromPointer(0x1220);
	private static readonly APTR Title = APTR.FromPointer(0x1240);
	private static readonly APTR Gadgets = APTR.FromPointer(0x1260);
	private static readonly APTR Format = APTR.FromPointer(0x1280);
	private static readonly APTR Parameters = APTR.FromPointer(0x12A0);
	private static readonly APTR Object = APTR.FromPointer(0x1300);
	private static readonly APTR RequesterScratch = APTR.FromPointer(0x1600);

	[Fact]
	public void SystemFallbackRemovesActiveButtonMarkerAndUsesMuiResultIndex()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		WriteCString(ref platform, Gadgets, "_Save|*_Use|_Cancel");

		Assert.True(MuiRequesterSystemGadgetCore.TryPrepare(ref platform,
			Gadgets, out var plan));
		Assert.Equal(3u, plan.GadgetCount);
		Assert.Equal(1u, plan.HasActiveButton);
		Assert.Equal(2u, plan.ActiveButton);
		Assert.NotEqual(Gadgets, plan.GadgetFormat);
		Assert.Equal("_Save|_Use|_Cancel", ReadCString(ref platform,
			plan.GadgetFormat));
		MuiRequesterSystemGadgetCore.Release(ref platform, plan);

		WriteCString(ref platform, Gadgets, "One|Two|*Three");
		Assert.True(MuiRequesterSystemGadgetCore.TryPrepare(ref platform,
			Gadgets, out plan));
		Assert.Equal(0u, plan.ActiveButton);
		MuiRequesterSystemGadgetCore.Release(ref platform, plan);

		WriteCString(ref platform, Gadgets, "*One|Two|Three");
		Assert.True(MuiRequesterSystemGadgetCore.TryPrepare(ref platform,
			Gadgets, out plan));
		Assert.Equal(1u, plan.ActiveButton);
		MuiRequesterSystemGadgetCore.Release(ref platform, plan);
	}

	[Fact]
	public void SystemFallbackGadgetPlanBorrowsUnmarkedTextAndRejectsAmbiguousDefaults()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		WriteCString(ref platform, Gadgets, "_Yes|_No");
		Assert.True(MuiRequesterSystemGadgetCore.TryPrepare(ref platform,
			Gadgets, out var plan));
		Assert.Equal(Gadgets, plan.GadgetFormat);
		Assert.Equal(0u, plan.AllocationSize);
		Assert.Equal(0u, plan.HasActiveButton);

		WriteCString(ref platform, Gadgets, "*Yes|*No");
		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiRequesterSystemGadgetCore.TryPrepare(ref platform,
			Gadgets, out _));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
	}

	[Fact]
	public void MorphosExtendedEasyStructAndActiveButtonTagsUseNamedCodecs()
	{
		Assert.Equal(24, System.Runtime.CompilerServices.Unsafe.SizeOf<
			MuiNativeExtendedEasyStructRecord>());
		Assert.Equal(16, System.Runtime.CompilerServices.Unsafe.SizeOf<
			MuiRequesterActiveButtonTagListRecord>());
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var expectedEasy = new MuiNativeExtendedEasyStructRecord
		{
			StructureSize = MuiNativeExtendedEasyStructRecord.Size,
			Title = Title,
			TextFormat = Format,
			GadgetFormat = Gadgets,
			Tags = RequesterScratch,
		};
		Assert.True(MuiNativeExtendedEasyStructCodec.Write(ref platform,
			RequesterScratch, expectedEasy));
		Assert.True(MuiNativeExtendedEasyStructCodec.TryRead(ref platform,
			RequesterScratch, out var actualEasy));
		Assert.Equal(expectedEasy.StructureSize, actualEasy.StructureSize);
		Assert.Equal(expectedEasy.Flags, actualEasy.Flags);
		Assert.Equal(expectedEasy.Title, actualEasy.Title);
		Assert.Equal(expectedEasy.TextFormat, actualEasy.TextFormat);
		Assert.Equal(expectedEasy.GadgetFormat, actualEasy.GadgetFormat);
		Assert.Equal(expectedEasy.Tags, actualEasy.Tags);

		var expectedTags = new MuiRequesterActiveButtonTagListRecord
		{
			ActiveButton = TagItem.Create(
				MuiRequesterActiveButtonTagListCodec.ActiveButtonTag, 2),
			Terminator = TagItem.Done,
		};
		Assert.True(MuiRequesterActiveButtonTagListCodec.Write(ref platform,
			RequesterScratch, expectedTags));
		Assert.True(MuiRequesterActiveButtonTagListCodec.TryRead(ref platform,
			RequesterScratch, out var actualTags));
		Assert.Equal(expectedTags.ActiveButton.Tag, actualTags.ActiveButton.Tag);
		Assert.Equal(expectedTags.ActiveButton.Data, actualTags.ActiveButton.Data);
		Assert.Equal(ExecConstants.TagDone, actualTags.Terminator.Tag);
		Assert.Equal(0u, actualTags.Terminator.Data);
	}

	[Fact]
	public void NativeRequesterPreparationBuildsTypedCallAndReleasesScratch()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		WriteCString(ref platform, Gadgets, "_Save|*_Use|_Cancel");
		var allocationsBefore = platform.AllocationCount;
		var freesBefore = platform.FreeCount;

		Assert.True(MuiNativeRequesterPreparationCore.TryPrepare(ref platform,
			Title, Gadgets, Format, out var preparation));
		Assert.True(MuiNativeExtendedEasyStructCodec.TryRead(ref platform,
			preparation.EasyStruct, out var easy));
		Assert.Equal(MuiNativeExtendedEasyStructRecord.Size,
			easy.StructureSize);
		Assert.Equal(Title, easy.Title);
		Assert.Equal(Format, easy.TextFormat);
		Assert.Equal("_Save|_Use|_Cancel", ReadCString(ref platform,
			easy.GadgetFormat));
		Assert.True(MuiRequesterActiveButtonTagListCodec.TryRead(ref platform,
			easy.Tags, out var tags));
		Assert.Equal(MuiRequesterActiveButtonTagListCodec.ActiveButtonTag,
			tags.ActiveButton.Tag);
		Assert.Equal(2u, tags.ActiveButton.Data);
		Assert.Equal(ExecConstants.TagDone, tags.Terminator.Tag);

		var call = MuiNativeRequesterPreparationCore.CreateCall(preparation,
			Window);
		Assert.Equal(Window, call.Window);
		Assert.Equal(preparation.EasyStruct, call.EasyStruct);
		Assert.True(call.Idcmp.IsNull);
		Assert.True(call.Arguments.IsNull);

		MuiNativeRequesterPreparationCore.Release(ref platform, ref preparation);
		Assert.Equal(platform.AllocationCount - allocationsBefore,
			platform.FreeCount - freesBefore);
		Assert.True(preparation.EasyStruct.IsNull);
		Assert.True(preparation.ActiveButtonTags.IsNull);
		Assert.True(preparation.Gadgets.GadgetFormat.IsNull);
		var freesAfterRelease = platform.FreeCount;
		MuiNativeRequesterPreparationCore.Release(ref platform, ref preparation);
		Assert.Equal(freesAfterRelease, platform.FreeCount);
	}

	[Fact]
	public void NativeRequesterRouteRequiresZeroFlagsAndSeparatesApplicationRoute()
	{
		var request = new MuiRequesterCallRecord
		{
			Application = APTR.Null,
			Window = Window,
			Flags = 0,
			Title = Title,
			Gadgets = Gadgets,
			Format = Format,
			Parameters = Parameters,
		};
		Assert.True(MuiNativeRequesterRouteCore.TrySelect(request,
			out var route));
		Assert.Equal(MuiNativeRequesterRoute.IntuitionSystemFallback, route);
		Assert.Equal(Window, request.Window);
		Assert.Equal(Title, request.Title);
		Assert.Equal(Gadgets, request.Gadgets);
		Assert.Equal(Format, request.Format);
		Assert.Equal(Parameters, request.Parameters);

		request.Flags = MUIConstants.MUIREQ_XYPOS | (40u << 16) | 25u;
		Assert.False(MuiNativeRequesterRouteCore.TrySelect(request, out route));
		Assert.Equal(MuiNativeRequesterRoute.Unsupported, route);

		request.Flags = 0;
		request.Application = Application;
		Assert.True(MuiNativeRequesterRouteCore.TrySelect(request, out route));
		Assert.Equal(MuiNativeRequesterRoute.ApplicationMui, route);
	}

	[Fact]
	public void NativeRequesterResolvesMuiWindowObjectToIntuitionWindow()
	{
		var muiWindow = APTR.FromPointer(0x2400);
		var intuitionWindow = APTR.FromPointer(0x9000);
		var platform = new RequesterWindowCapability
		{
			ExpectedMuiWindow = muiWindow,
			NativeWindow = intuitionWindow,
			LookupSucceeds = true,
		};

		Assert.True(MuiNativeRequesterWindowCore.TryResolve(ref platform,
			muiWindow, out var resolution));
		Assert.Equal(muiWindow, resolution.MuiWindow);
		Assert.Equal(intuitionWindow, resolution.IntuitionWindow);
		Assert.Equal(1u, resolution.Resolved);
		Assert.Equal(1u, platform.LookupCount);
		Assert.Equal(muiWindow, platform.LastMuiWindow);

		Assert.True(MuiNativeRequesterWindowCore.TryResolve(ref platform,
			APTR.Null, out resolution));
		Assert.True(resolution.MuiWindow.IsNull);
		Assert.True(resolution.IntuitionWindow.IsNull);
		Assert.Equal(1u, resolution.Resolved);
		Assert.Equal(1u, platform.LookupCount);
	}

	[Fact]
	public void NativeRequesterAllowsClosedMuiWindowButNeverForwardsItsObjectPointer()
	{
		var muiWindow = APTR.FromPointer(0x2400);
		var platform = new RequesterWindowCapability
		{
			ExpectedMuiWindow = muiWindow,
			NativeWindow = APTR.Null,
			LookupSucceeds = true,
		};
		Assert.True(MuiNativeRequesterWindowCore.TryResolve(ref platform,
			muiWindow, out var resolution));
		Assert.True(resolution.IntuitionWindow.IsNull);

		platform.LookupSucceeds = false;
		Assert.False(MuiNativeRequesterWindowCore.TryResolve(ref platform,
			muiWindow, out resolution));
		Assert.True(resolution.IntuitionWindow.IsNull);
		Assert.Equal(2u, platform.LookupCount);
	}

	[Fact]
	public void RequestARequiresInitializationAndPreservesPresentationFlags()
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
		Assert.Equal(3, MuiRequesterServiceCore.Request(ref platform, State,
			Application, Window, 7, Title, Gadgets, Format, Parameters));
		Assert.Equal(1u, platform.RequestCallCount);
		Assert.Equal(7u, platform.LastRequestFlags);
		Assert.Equal(3, MuiRequesterServiceCore.Request(ref platform, State,
			Application, Window, 0, Title, Gadgets, Format, Parameters));
		Assert.Equal(2u, platform.RequestCallCount);
		Assert.Equal(0u, platform.LastRequestFlags);
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
	public void RequestObjectDoesNotReleaseWhenPresenterDidNotConsumeReference()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		WriteCString(ref platform, Title, "Object requester");
		WriteCString(ref platform, Gadgets, "Continue|Cancel");
		WriteCString(ref platform, Format, "Proceed?");
		platform.WriteUInt32(Object, 4, 1);
		platform.RequestObjectConsumesReference = false;
		Assert.True(MuiRequesterServiceCore.Initialize(ref platform, State));

		Assert.Equal(1, MuiRequesterServiceCore.RequestObject(ref platform, State,
			Application, Window, 0, Title, Gadgets, Object, Format, Parameters));
		Assert.Equal(1u, platform.RequestObjectCallCount);
		Assert.Equal(0u, platform.ObjectReleaseCount);
		Assert.Equal(1u, platform.ReadUInt32(Object, 4));
	}

	[Fact]
	public void RequestObjectPassesPresentationFlagsToCapability()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		WriteCString(ref platform, Title, "Object requester");
		WriteCString(ref platform, Gadgets, "Yes|No");
		WriteCString(ref platform, Format, "Proceed?");
		platform.WriteUInt32(Object, 4, 1);
		Assert.True(MuiRequesterServiceCore.Initialize(ref platform, State));

		Assert.Equal(1, MuiRequesterServiceCore.RequestObject(ref platform, State,
			Application, Window, 0x12345678, Title, Gadgets, Object, Format,
			Parameters));
		Assert.Equal(1u, platform.RequestObjectCallCount);
		Assert.Equal(0x12345678u, platform.LastRequestFlags);
		Assert.Equal(1u, platform.ObjectReleaseCount);
	}

	[Fact]
	public void NativeObjectReleaseUsesNamedMorphosMethodRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var message = APTR.FromPointer(0x1400);
		var value = default(MuiNativeObjectMethodMessage);
		value.MethodId = MuiNativeBoopsiMethodId.Release;

		Assert.True(MuiNativeObjectMethodMessageCodec.Write(ref platform, message,
			value));
		Assert.Equal(0x10Cu, platform.ReadUInt32(message, 0));
		Assert.Equal(0x10Bu, MuiNativeBoopsiMethodId.Retain);
		Assert.Equal(0x10Du, MuiNativeBoopsiMethodId.RetainCount);
		value.MethodId = MuiNativeBoopsiMethodId.RetainCount;
		Assert.True(MuiNativeObjectMethodMessageCodec.Write(ref platform, message,
			value));
		Assert.Equal(0x10Du, platform.ReadUInt32(message, 0));
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
		var generationCursor = new MuiRequesterServiceStateFieldCursor
		{
			Record = state,
			Field = MuiRequesterServiceStateField.Generation,
		};
		Assert.True(MuiRequesterServiceStateFieldCursorCodec.TryGetAddress(ref platform,
			generationCursor, out var cursorGeneration, out var cursorFieldSize));
		Assert.Equal(generation, cursorGeneration);
		Assert.Equal(MuiRequesterServiceStateRecord.FieldSize, cursorFieldSize);
		Assert.True(MuiRequesterServiceStateMemoryCodec.TryGetAddress(ref platform,
			generationCursor, out var memoryGeneration, out var memoryFieldSize));
		Assert.Equal(cursorGeneration, memoryGeneration);
		Assert.Equal(cursorFieldSize, memoryFieldSize);
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
		generationCursor.Field = (MuiRequesterServiceStateField)255;
		Assert.False(MuiRequesterServiceStateFieldCursorCodec.TryGetAddress(ref platform,
			generationCursor, out _, out _));
		generationCursor.Record = APTR.Null;
		generationCursor.Field = MuiRequesterServiceStateField.Generation;
		Assert.False(MuiRequesterServiceStateFieldCursorCodec.TryGetAddress(ref platform,
			generationCursor, out _, out _));
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
	public void RequestFormatPointerConversionUsesMorphosRawDoFmtPrefix()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		WriteCString(ref platform, Title, "Pointer requester");
		WriteCString(ref platform, Gadgets, "Ok");
		WriteCString(ref platform, Format, "p=%p n=%p w=%08p a=%#p");
		Assert.True(MuiRequesterParameterVectorCodec.TryWriteValue(ref platform,
			Parameters, 0, 0x1234));
		Assert.True(MuiRequesterParameterVectorCodec.TryWriteValue(ref platform,
			Parameters, 1, 0));
		Assert.True(MuiRequesterParameterVectorCodec.TryWriteValue(ref platform,
			Parameters, 2, 0x2A));
		Assert.True(MuiRequesterParameterVectorCodec.TryWriteValue(ref platform,
			Parameters, 3, 0xABCD));
		Assert.True(MuiRequesterPayloadCore.TryGetFormatParameterCount(ref platform,
			Format, out var parameterCount));
		Assert.Equal(4u, parameterCount);
		Assert.True(MuiRequesterPayloadCore.Validate(ref platform, Title, Gadgets,
			Format, Parameters));
		Assert.True(MuiRequesterFormatCore.TryMaterialize(ref platform, Format,
			Parameters, out var materialized, out var allocationSize));
		Assert.Equal("p=0x1234 n=0x0 w=0x00002a a=0xabcd",
			ReadCString(ref platform, materialized));
		if (allocationSize != 0) platform.Free(materialized, allocationSize);
		Assert.True(MuiRequesterServiceCore.Initialize(ref platform, State));

		Assert.Equal(1, MuiRequesterServiceCore.Request(ref platform, State,
			Application, Window, 0, Title, Gadgets, Format, Parameters));
		Assert.Equal("p=0x1234 n=0x0 w=0x00002a a=0xabcd",
			ReadCString(ref platform, platform.LastRequestFormat));
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
