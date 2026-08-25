using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationHelpResolutionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ResolutionWalksNamedParentChainForNodeAndLineIndependently()
	{
		var platform = CreatePlatform(out var classRecord);
		var application = Object(ref platform, classRecord);
		var window = Object(ref platform, classRecord);
		var group = Object(ref platform, classRecord);
		var child = Object(ref platform, classRecord);
		var helpFile = APTR.FromPointer(0x1800);
		var groupNode = APTR.FromPointer(0x1900);
		platform.WriteCString(helpFile, "SYS:Help.guide");
		platform.WriteCString(groupNode, "group-node");

		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, window));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, window,
			MuiWindowPublicCore.RootObject, group.Raw, false));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, child));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			MuiHelpStateCore.HelpNode, groupNode.Raw, false));
		// An explicit NULL does not mask the first non-NULL node in the parent
		// chain; MorphOS continues until a usable help node is found.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, child,
			MuiHelpStateCore.HelpNode, 0, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, child,
			MuiHelpStateCore.HelpLine, unchecked((uint)-7), false));

		Assert.True(MuiHelpStateCore.TryResolve(ref platform, State, child,
			out var resolved));
		Assert.Equal(groupNode, resolved.Node);
		Assert.Equal(-7, resolved.Line);
		Assert.Equal(group, resolved.NodeObject);
		Assert.Equal(child, resolved.LineObject);

		Assert.True(MuiApplicationWindowCore.ShowHelpFromObject(ref platform,
			State, application, window, child, helpFile));
		Assert.Equal(application, platform.LastShowHelpApplication);
		Assert.Equal(window, platform.LastShowHelpWindow);
		Assert.Equal(helpFile, platform.LastShowHelpName);
		Assert.Equal(groupNode, platform.LastShowHelpNode);
		Assert.Equal(-7, platform.LastShowHelpLine);
	}

	[Fact]
	public void ResolutionAllowsObjectsWithoutExplicitHelpAttributes()
	{
		var platform = CreatePlatform(out var classRecord);
		var objectAddress = Object(ref platform, classRecord);

		Assert.True(MuiHelpStateCore.TryResolve(ref platform, State,
			objectAddress, out var resolved));
		Assert.Equal(APTR.Null, resolved.Node);
		Assert.Equal(0, resolved.Line);
		Assert.Equal(APTR.Null, resolved.NodeObject);
		Assert.Equal(APTR.Null, resolved.LineObject);
	}

	[Fact]
	public void HelpKeyUsesWindowMouseObjectAndApplicationHelpFile()
	{
		var platform = CreatePlatform(out var classRecord);
		var application = Object(ref platform, classRecord);
		var window = Object(ref platform, classRecord);
		var group = Object(ref platform, classRecord);
		var child = Object(ref platform, classRecord);
		var helpFile = APTR.FromPointer(0x1800);
		var childNode = APTR.FromPointer(0x1900);
		var eventMessage = APTR.FromPointer(0x1A00);
		platform.WriteCString(helpFile, "SYS:Help.guide");
		platform.WriteCString(childNode, "child-node");

		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, window));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, window,
			MuiWindowPublicCore.RootObject, group.Raw, false));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, child));
		Assert.True(MuiApplicationWindowCore.SetApplicationHelpFileValue(
			ref platform, State, application, helpFile.Raw));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, child,
			MuiHelpStateCore.HelpNode, childNode.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, child,
			MuiHelpStateCore.HelpLine, unchecked((uint)-3), false));
		Assert.True(MuiApplicationWindowCore.PublishWindowMouseObjectValue(
			ref platform, State, window, child));
		Assert.True(MuiCommonControlPacketCore.WriteHandleEvent(ref platform,
			eventMessage, 0x1B00, MuiHelpTriggerInput.HelpKey, 0));

		Assert.True(MuiApplicationWindowCore.HandleHelpKey(ref platform, State,
			window, eventMessage));
		Assert.Equal(application, platform.LastShowHelpApplication);
		Assert.Equal(window, platform.LastShowHelpWindow);
		Assert.Equal(helpFile, platform.LastShowHelpName);
		Assert.Equal(childNode, platform.LastShowHelpNode);
		Assert.Equal(-3, platform.LastShowHelpLine);
	}

	[Fact]
	public void DispatchWindowEventConsumesSuccessfulAutomaticHelpTrigger()
	{
		var platform = CreatePlatform(out var classRecord);
		var application = Object(ref platform, classRecord);
		var window = Object(ref platform, classRecord);
		var current = Object(ref platform, classRecord);
		var helpFile = APTR.FromPointer(0x1800);
		var node = APTR.FromPointer(0x1900);
		var eventMessage = APTR.FromPointer(0x1A00);
		platform.WriteCString(helpFile, "SYS:Help.guide");
		platform.WriteCString(node, "dispatch-node");

		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, window));
		Assert.True(MuiApplicationWindowCore.SetApplicationHelpFileValue(
			ref platform, State, application, helpFile.Raw));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, current,
			MuiHelpStateCore.HelpNode, node.Raw, false));
		Assert.True(MuiApplicationWindowCore.PublishWindowMouseObjectValue(
			ref platform, State, window, current));
		Assert.True(MuiCommonControlPacketCore.WriteHandleEvent(ref platform,
			eventMessage, 0x1B00, MuiHelpTriggerInput.HelpKey, 0));

		Assert.Equal(1u, MuiApplicationWindowCore.DispatchWindowEvent(ref platform,
			State, window, eventMessage, 0x00000004));
		Assert.Equal(1u, platform.ShowHelpRequestCount);
		Assert.Equal(node, platform.LastShowHelpNode);
	}

	[Fact]
	public void PollWindowEventsUsesTypedPreprocessedHandleEventMessage()
	{
		var platform = CreatePlatform(out var classRecord);
		var application = Object(ref platform, classRecord);
		var window = Object(ref platform, classRecord);
		var current = Object(ref platform, classRecord);
		var helpFile = APTR.FromPointer(0x1800);
		var node = APTR.FromPointer(0x1900);
		var eventStorage = APTR.FromPointer(0x1B00);
		var eventMessage = APTR.FromPointer(0x1C00);
		platform.WriteCString(helpFile, "SYS:Help.guide");
		platform.WriteCString(node, "poll-node");

		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, window));
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0x200));
		Assert.True(MuiApplicationWindowCore.SetApplicationHelpFileValue(
			ref platform, State, application, helpFile.Raw));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, current,
			MuiHelpStateCore.HelpNode, node.Raw, false));
		Assert.True(MuiApplicationWindowCore.PublishWindowMouseObjectValue(
			ref platform, State, window, current));

		platform.PendingWindowEvent = 0x00000004;
		platform.PreprocessedWindowEventAvailable = true;
		platform.PreprocessedInputMessage = APTR.FromPointer(0x1D00);
		platform.PreprocessedMuiKey = MuiHelpTriggerInput.HelpKey;
		var pollInput = default(MuiWindowEventPollInput);
		pollInput.InputEvent = eventStorage;
		pollInput.EventMessage = eventMessage;
		Assert.Equal(1u, MuiApplicationWindowCore.PollWindowEvents(ref platform,
			State, application, pollInput));
		Assert.Equal(1u, platform.PreprocessedWindowEventCount);
		Assert.Equal(1u, platform.ShowHelpRequestCount);
		Assert.Equal(node, platform.LastShowHelpNode);
	}

	[Fact]
	public void PollWindowEventsDispatchesPreprocessedHandleEventToRegisteredNode()
	{
		var platform = CreatePlatform(out var classRecord);
		var application = Object(ref platform, classRecord);
		var window = Object(ref platform, classRecord);
		var target = Object(ref platform, classRecord);
		var handler = APTR.FromPointer(0x1E00);
		var eventStorage = APTR.FromPointer(0x1F00);
		var eventMessage = APTR.FromPointer(0x2000);
		var classPointer = MuiHeadlessObjectCore.ClassPointer(ref platform,
			classRecord);

		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0x20));
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, window));
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0x200));
		Assert.True(MuiApplicationWindowRecordPacketCore.WriteEventHandler(
			ref platform, handler, new MuiEventHandlerNodeInput
			{
				Flags = MuiEventHandlerNodeInput.MUI_EHF_GUIMODE,
				Object = target,
				Class = classPointer,
				Events = 4,
			}));
		Assert.True(MuiApplicationWindowCore.AddEventHandler(ref platform, State,
			window, handler));

		platform.PendingWindowEvent = 4;
		platform.PreprocessedWindowEventAvailable = true;
		platform.PreprocessedInputMessage = APTR.FromPointer(0x90000077);
		platform.PreprocessedMuiKey = -1;
		platform.PreprocessedEventHandlerNode = APTR.Null;
		platform.DispatchResult = 0x77;
		var pollInput = default(MuiWindowEventPollInput);
		pollInput.InputEvent = eventStorage;
		pollInput.EventMessage = eventMessage;

		Assert.Equal(0u, MuiApplicationWindowCore.PollWindowEvents(ref platform,
			State, application, pollInput));
		Assert.Equal(1u, platform.PreprocessedWindowEventCount);
		Assert.Equal(target, platform.LastDispatchObject);
		Assert.Equal(MuiCommonControlPacketCore.HandleEvent,
			platform.LastDispatchMethod);
		Assert.Equal(classPointer.Raw, platform.LastDispatchArgument);
	}

	[Fact]
	public void HelpKeyWithoutApplicationHelpFileDoesNotPresent()
	{
		var platform = CreatePlatform(out var classRecord);
		var application = Object(ref platform, classRecord);
		var window = Object(ref platform, classRecord);
		var current = Object(ref platform, classRecord);
		var node = APTR.FromPointer(0x1900);
		var eventMessage = APTR.FromPointer(0x1A00);
		platform.WriteCString(node, "no-file-node");

		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, window));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, current,
			MuiHelpStateCore.HelpNode, node.Raw, false));
		Assert.True(MuiApplicationWindowCore.PublishWindowMouseObjectValue(
			ref platform, State, window, current));
		Assert.True(MuiCommonControlPacketCore.WriteHandleEvent(ref platform,
			eventMessage, 0x1B00, MuiHelpTriggerInput.HelpKey, 0));

		Assert.False(MuiApplicationWindowCore.HandleHelpKey(ref platform, State,
			window, eventMessage));
		Assert.Equal(0u, platform.ShowHelpRequestCount);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR classRecord)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Application.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		classRecord = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR Object(ref MuiHeadlessTestPlatform platform, APTR classRecord) =>
		MuiHeadlessObjectCore.CreateObjectA(ref platform, State, classRecord,
			APTR.Null);
}
