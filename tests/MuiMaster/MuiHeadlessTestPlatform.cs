using System.Buffers.Binary;
using System.Text;
using Amiga;
using Amiga.MUI;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

internal struct MuiHeadlessTestPlatform : IMuiApplicationPlatform,
	IMuiServicePlatform, IMuiIffCapability
{
	private readonly uint _baseAddress;
	private readonly byte[] _memory;
	private uint _next;
	public APTR State;
	public uint CurrentTask;
	public uint AllocationCount;
	public uint FreeCount;
	public uint DispatchCount;
	public APTR LastDispatchObject;
	public uint LastDispatchMethod;
	public uint LastDispatchArgument;
	public uint DispatchResult;
	public APTR ObservedHandler;
	public bool CallingFlagObserved;
	public APTR MutationSource;
	public APTR MutationDestination;
	public uint MutationAttribute;
	public byte MutationMode;
	public uint RecursiveDispatches;
	public uint FillCount;
	public uint LineCount;
	public uint TextCount;
	public uint ImageCount;
	public APTR LastText;
	public APTR FirstText;
	public APTR SecondText;
	public APTR ThirdText;
	public int FirstTextLeft;
	public int SecondTextLeft;
	public int ThirdTextLeft;
	public int LastTextLength;
	public int LastTextLeft;
	public int LastTextBaseline;
	public uint LastPen;
	public int LastLeft;
	public int LastTop;
	public int LastRight;
	public int LastBottom;
	public int LastLineX1;
	public int LastLineY1;
	public int LastLineX2;
	public int LastLineY2;
	public uint LayerDepth;
	public uint RedrawCount;
	public uint PendingSignals;
	public uint SignaledMask;
	public uint WaitMuiSignalsCount;
	public uint WindowOpenCount;
	public uint WindowCloseCount;
	public uint WindowEventMask;
	public uint WindowActivationCount;
	public uint WindowBusyOperationCount;
	public bool WindowBusy;
	public uint WindowTabletMessagesOperationCount;
	public bool WindowTabletMessages;
	public uint WindowBorderScrollerOperationCount;
	public bool WindowUseBottomBorderScroller;
	public bool WindowUseLeftBorderScroller;
	public bool WindowUseRightBorderScroller;
	public uint WindowAlternateGeometryOperationCount;
	public MuiWindowPublicCore.MuiWindowAlternateGeometry WindowAlternateGeometry;
	public uint WindowGeometryOperationCount;
	public MuiWindowPublicCore.MuiWindowGeometry WindowGeometry;
	public uint WindowGadgetPolicyOperationCount;
	public MuiWindowPublicCore.MuiWindowGadgetPolicy WindowGadgetPolicy;
	public uint WindowModePolicyOperationCount;
	public MuiWindowPublicCore.MuiWindowModePolicy WindowModePolicy;
	public uint ScreenDepthOperationCount;
	public bool LastScreenDepthToFront;
	public uint MenuOperationCount;
	public uint RequesterOperationCount;
	public bool Iconified;
	public uint Ticks;
	public uint PendingWindowEvent;
	public uint AboutMUIRequestCount;
	public APTR LastAboutMUIApplication;
	public APTR LastAboutMUIReference;
	public uint ShowHelpRequestCount;
	public APTR LastShowHelpApplication;
	public APTR LastShowHelpWindow;
	public APTR LastShowHelpName;
	public APTR LastShowHelpNode;
	public int LastShowHelpLine;
	public uint DefaultConfigRequestCount;
	public APTR LastDefaultConfigApplication;
	public uint LastDefaultConfigId;
	public uint DefaultConfigItemValue;
	public uint ConfigItemRequestCount;
	public APTR LastConfigItemObject;
	public uint LastConfigItemId;
	public uint PublicScreenConfigValue;
	public bool ConfigItemOperationResult;
	public uint BuildSettingsPanelRequestCount;
	public APTR LastBuildSettingsPanelApplication;
	public uint LastBuildSettingsPanelNumber;
	public APTR SettingsPanelResult;
	public uint OpenConfigWindowRequestCount;
	public APTR LastOpenConfigWindowApplication;
	public uint LastOpenConfigWindowFlags;
	public APTR LastOpenConfigWindowClassId;
	public uint SettingsSaveRequestCount;
	public uint SettingsLoadRequestCount;
	public APTR LastSettingsApplication;
	public APTR LastSettingsName;
	public bool SettingsOperationResult;
	// Opt-in guest-backed DOS file used by the application-settings transport
	// tests. The default keeps legacy capability tests as a deterministic stub.
	public bool UseSettingsFile;
	public APTR SettingsFileBuffer;
	public uint SettingsFileLength;
	public uint SettingsFilePosition;
	public uint DosOpenCount;
	public uint DosCloseCount;
	public int LastDosError;
	// Deterministic guest-backed IFF chunk used by Dataspace ReadIFF/WriteIFF
	// tests. Handle 2 is reserved for this capability; transfers can be capped
	// to prove the core retries short ReadChunkBytes/WriteChunkBytes results.
	public APTR IffBuffer;
	public uint IffLength;
	public uint IffPosition;
	public uint IffCapacity;
	public uint IffReadChunkLimit;
	public uint IffWriteChunkLimit;
	public uint IffPushCount;
	public uint IffPopCount;
	public uint IffLastType;
	public uint IffLastId;
	public int IffReadError;
	public int IffWriteError;
	public uint ObjectExportRequestCount;
	public uint ObjectImportRequestCount;
	public APTR LastPersistenceObject;
	public APTR PreviousPersistenceObject;
	public APTR LastPersistenceDataspace;
	public uint LastPersistenceObjectId;
	public bool PersistenceOperationResult;
	public APTR PersistenceImportFailureDataspace;
	public APTR PersistenceImportFailureObject;
	public uint RefreshMuiWindowCount;
	public APTR LastRefreshedMuiWindow;
	public uint WindowSnapshotCount;
	public APTR LastSnapshotMuiWindow;
	public uint LastSnapshotFlags;

	// ---- Callback (struct Hook) telemetry ------------------------------------
	public uint HookInvokeCount;
	public uint HookDestructCount;
	public APTR LastHookBase;   // A0 delivered to the callback
	public APTR LastHookA2;     // A2 delivered to the callback
	public APTR LastHookA1;     // A1 delivered to the callback
	public APTR LastHookData;   // h_Data reached through A0 (hook+16)
	public uint StringEditHookResult;
	public uint StringEditHookActions;
	public APTR StringEditHookBuffer;
	public uint LayoutHookMinMaxCount;
	public uint LayoutHookLayoutCount;
	public APTR LastLayoutHookChildren;
	public APTR LastLayoutHookFirstChild;

	// ---- Directory/volume capability (deterministic host model) --------------
	// When set, the next DirectoryScan reports the directory as missing.
	public bool DirectoryMissing;
	// When >= 0, DirectoryEntry at this index fails (mid-scan failure).
	public int DirectoryFailIndex;
	// IoErr() code reported for scan failures.
	public int DirectoryErrorCode;
	// Result codes returned by the filesystem mutators (0 == success).
	public int RenameResult;
	public int SetCommentResult;
	public int SetProtectionResult;
	// Number of mounted volumes reported by VolumeScan.
	public int VolumeCount;
	// Most recent IoErr()-style code produced by the seam.
	public int LastDirectoryError;
	// Telemetry counters for tests.
	public uint DirectoryScanCount;
	public uint FilterInvocations;
	public int DirectoryCount;

	// ---- MG09 class-service capability (deterministic host model) ------------
	// The guest string ("mui/<id>") that OpenLibrary accepts (case-sensitive).
	public APTR LoadableLibraryName;
	// The library base OpenLibrary returns for that name.
	public APTR LoadableLibraryBase;
	// The classid ResolvePublicClass accepts (case-sensitive).
	public APTR LoadablePublicClassId;
	// The struct IClass* ResolvePublicClass returns for that classid.
	public APTR LoadablePublicClass;
	// Next synthetic custom-class pointer handed out by MakeCustomClass.
	public uint NextCustomClass;
	public uint OpenLibraryCount;
	public uint CloseLibraryCount;
	public uint MakeCustomClassCount;
	public uint FreeCustomClassCount;
	public uint ResolvePublicClassCount;
	public APTR LastOpenLibraryName;
	public APTR LastCustomLibraryBase;

	// ---- MG09 ASL/requester capability (deterministic host model) ------------
	public uint NextAslRequest;
	public uint AslAllocateCount;
	public uint AslRequestCount;
	public uint AslFreeCount;
	public uint AslRequestResult;
	public uint LastAslRequestType;
	public APTR LastAslTags;
	public APTR LastAslRequester;

	// ---- MG09 synchronous requester capability ------------------------------
	public uint RequestCallCount;
	public uint RequestObjectCallCount;
	public int RequestResult;
	public int RequestObjectResult;
	public APTR LastRequestApplication;
	public APTR LastRequestWindow;
	public APTR LastRequestTitle;
	public APTR LastRequestGadgets;
	public APTR LastRequestFormat;
	public APTR LastRequestParameters;
	public APTR LastRequestObject;
	public uint ObjectRetainCount;
	public uint ObjectReleaseCount;
	public APTR LastRetainedObject;
	public APTR LastReleasedObject;

	// ---- MG09 drawing-service region capability (deterministic host model) ---
	public uint InstallRegionCount;
	public uint RestoreRegionCount;
	public APTR LastRegionLayer;
	public APTR LastInstalledRegion;
	public APTR LastRestoredRegion;
	public uint NextRegionToken;

	// ---- MG09 drawing-service pen capability (deterministic host model) ------
	// Full pen token handed out by the next ObtainPen (low MUIPEN_MASK bits are
	// the physical pen; the high bits prove that the service releases the full
	// token, never a masked value).
	public uint NextPenToken;
	// When true, ObtainPen fails with PenObtainFailureValue and reserves no pen.
	public bool PenObtainFailure;
	public int PenObtainFailureValue;
	public uint ObtainPenCount;
	public uint ReleasePenCount;
	public uint GetRGBColorCount;
	public int LastObtainedPen;
	public int LastReleasedPen;
	public uint LastObtainPenFlags;
	public APTR LastPenRenderInfo;
	public APTR LastPenSpec;
	// RGB components GetRGBColor writes into the caller's MUI_RGBColor block.
	public uint RgbRed;
	public uint RgbGreen;
	public uint RgbBlue;
	public bool GetRGBColorResult;

	// ---- MG09 layers-seam telemetry / failure injection ---------------------
	public uint ClipPushCount;
	public uint ClipPopCount;
	public APTR LastPushClipLayer;
	public int LastClipLeft;
	public int LastClipTop;
	public int LastClipWidth;
	public int LastClipHeight;
	public APTR LastPopClipLayer;
	public APTR LastPoppedClip;
	public uint BeginUpdateCount;
	public uint EndUpdateCount;
	public bool BeginUpdateFails;
	public APTR LastBeginUpdateLayer;
	public APTR LastEndUpdateLayer;
	public bool LastEndUpdateCompleted;

	// ---- MG09 Process/Slave scheduler capability (deterministic host model) --
	// Next opaque task token handed out by ProcessLaunch (non-zero == success).
	public uint NextProcessToken;
	// When true, ProcessLaunch injects a launch failure (returns 0) and reserves
	// no token, exercising the failure-atomic Launch path.
	public bool ProcessLaunchFailure;
	// Result returned by ProcessKill.
	public bool ProcessKillResult;
	// Scheduler status returned by ProcessPoll.
	public uint ProcessPollStatus;
	// Signals available to the current task for ProcessSignalsReceived to
	// consume.
	public uint ProcessPendingSignals;
	public uint ProcessLaunchCount;
	public uint ProcessKillCount;
	public uint ProcessPollCount;
	public uint ProcessSignalCount;
	public uint ProcessSignalsReceivedCount;
	public APTR LastLaunchName;
	public int LastLaunchPriority;
	public uint LastLaunchStackSize;
	public APTR LastLaunchSourceClass;
	public APTR LastLaunchSourceObject;
	public uint LastKilledToken;
	public uint LastSignaledProcessToken;
	public uint LastSignaledProcessMask;
	public uint LastSignalsReceivedMask;

	// ---- MG09 external BOOPSI loader capability (deterministic host model) ---
	// The guest class-id string OpenExternalClass accepts (case-sensitive). When
	// Null any non-null id is accepted. LoadableExternalClass is the struct
	// IClass* returned; ExternalClassOpenFailure forces the open to fail.
	public APTR LoadableExternalClassId;
	public APTR LoadableExternalClass;
	public bool ExternalClassOpenFailure;
	public bool NewObjectFailure;
	public uint OpenExternalClassCount;
	public uint CloseExternalClassCount;
	public APTR LastOpenedExternalClassId;
	public APTR LastClosedExternalClass;

	// ---- MG09 datatypes picture capability (deterministic host model) --------
	// The guest name AcquirePicture accepts (case-sensitive). When Null any
	// non-null name is accepted. AcquirePictureFailure forces the acquire to
	// fail; LayoutPictureResult/PictureWidth/PictureHeight drive layout.
	public APTR AcquirablePictureName;
	public uint NextPictureObject;
	public bool AcquirePictureFailure;
	public bool LayoutPictureResult;
	public int PictureWidth;
	public int PictureHeight;
	public bool DrawPictureResult;
	public uint AcquirePictureCount;
	public uint ReleasePictureCount;
	public uint LayoutPictureCount;
	public uint DrawPictureCount;
	public APTR LastAcquiredPictureName;
	public APTR LastAcquiredPictureScreen;
	public APTR LastReleasedPicture;
	public APTR LastDrawnPicture;
	public int LastDrawnPictureWidth;
	public int LastDrawnPictureHeight;

	public MuiHeadlessTestPlatform(uint baseAddress, int size, uint firstAllocation,
		APTR state)
	{
		_baseAddress = baseAddress;
		_memory = new byte[size];
		_next = firstAllocation;
		State = state;
		CurrentTask = 1;
		AllocationCount = 0;
		FreeCount = 0;
		DispatchCount = 0;
		LastDispatchObject = APTR.Null;
		LastDispatchMethod = 0;
		LastDispatchArgument = 0;
		DispatchResult = 1;
		MutationSource = APTR.Null;
		MutationDestination = APTR.Null;
		MutationAttribute = 0;
		MutationMode = 0;
		RecursiveDispatches = 0;
		FillCount = 0;
		LineCount = 0;
		TextCount = 0;
		ImageCount = 0;
		LastText = APTR.Null;
		LastTextLength = 0;
		LastTextLeft = 0;
		LastTextBaseline = 0;
		LastPen = 0;
		LastLeft = 0;
		LastTop = 0;
		LastRight = 0;
		LastBottom = 0;
		LastLineX1 = 0;
		LastLineY1 = 0;
		LastLineX2 = 0;
		LastLineY2 = 0;
		LayerDepth = 0;
		RedrawCount = 0;
		PendingSignals = 0;
		SignaledMask = 0;
		WaitMuiSignalsCount = 0;
		WindowOpenCount = 0;
		WindowCloseCount = 0;
		WindowEventMask = 0;
		WindowActivationCount = 0;
		WindowBusyOperationCount = 0;
		WindowBusy = false;
		ScreenDepthOperationCount = 0;
		LastScreenDepthToFront = false;
		MenuOperationCount = 0;
		RequesterOperationCount = 0;
		Iconified = false;
		Ticks = 0;
		PendingWindowEvent = 0;
		AboutMUIRequestCount = 0;
		LastAboutMUIApplication = APTR.Null;
		LastAboutMUIReference = APTR.Null;
		ShowHelpRequestCount = 0;
		LastShowHelpApplication = APTR.Null;
		LastShowHelpWindow = APTR.Null;
		LastShowHelpName = APTR.Null;
		LastShowHelpNode = APTR.Null;
		LastShowHelpLine = 0;
		DefaultConfigRequestCount = 0;
		LastDefaultConfigApplication = APTR.Null;
		LastDefaultConfigId = 0;
		DefaultConfigItemValue = 0;
		ConfigItemRequestCount = 0;
		LastConfigItemObject = APTR.Null;
		LastConfigItemId = 0;
		PublicScreenConfigValue = 0;
		ConfigItemOperationResult = true;
		BuildSettingsPanelRequestCount = 0;
		LastBuildSettingsPanelApplication = APTR.Null;
		LastBuildSettingsPanelNumber = 0;
		SettingsPanelResult = APTR.Null;
		OpenConfigWindowRequestCount = 0;
		LastOpenConfigWindowApplication = APTR.Null;
		LastOpenConfigWindowFlags = 0;
		LastOpenConfigWindowClassId = APTR.Null;
		SettingsSaveRequestCount = 0;
		SettingsLoadRequestCount = 0;
		LastSettingsApplication = APTR.Null;
		LastSettingsName = APTR.Null;
		SettingsOperationResult = true;
		UseSettingsFile = false;
		SettingsFileBuffer = APTR.Null;
		SettingsFileLength = 0;
		SettingsFilePosition = 0;
		DosOpenCount = 0;
		DosCloseCount = 0;
		LastDosError = 0;
		IffBuffer = APTR.FromPointer(0x1F000);
		IffLength = 0;
		IffPosition = 0;
		IffCapacity = 4096;
		IffReadChunkLimit = 0;
		IffWriteChunkLimit = 0;
		IffPushCount = 0;
		IffPopCount = 0;
		IffLastType = 0;
		IffLastId = 0;
		IffReadError = 0;
		IffWriteError = 0;
		ObjectExportRequestCount = 0;
		ObjectImportRequestCount = 0;
		LastPersistenceObject = APTR.Null;
		LastPersistenceDataspace = APTR.Null;
		LastPersistenceObjectId = 0;
		PersistenceOperationResult = true;
		PersistenceImportFailureDataspace = APTR.Null;
		PersistenceImportFailureObject = APTR.Null;
		RefreshMuiWindowCount = 0;
		LastRefreshedMuiWindow = APTR.Null;
		WindowSnapshotCount = 0;
		LastSnapshotMuiWindow = APTR.Null;
		LastSnapshotFlags = 0;
		HookInvokeCount = 0;
		HookDestructCount = 0;
		LastHookBase = APTR.Null;
		LastHookA2 = APTR.Null;
		LastHookA1 = APTR.Null;
		LastHookData = APTR.Null;
		StringEditHookResult = 1;
		StringEditHookActions = MuiStringEditWorkCodec.ActionUse;
		StringEditHookBuffer = APTR.Null;
		DirectoryCount = 0;
		DirectoryMissing = false;
		DirectoryFailIndex = -1;
		DirectoryErrorCode = 0;
		RenameResult = 0;
		SetCommentResult = 0;
		SetProtectionResult = 0;
		VolumeCount = 0;
		LastDirectoryError = 0;
		DirectoryScanCount = 0;
		FilterInvocations = 0;
		LoadableLibraryName = APTR.Null;
		LoadableLibraryBase = APTR.Null;
		LoadablePublicClassId = APTR.Null;
		LoadablePublicClass = APTR.Null;
		NextCustomClass = 0;
		OpenLibraryCount = 0;
		CloseLibraryCount = 0;
		MakeCustomClassCount = 0;
		FreeCustomClassCount = 0;
		ResolvePublicClassCount = 0;
		LastOpenLibraryName = APTR.Null;
		LastCustomLibraryBase = APTR.Null;
		NextAslRequest = 0x1E000;
		AslAllocateCount = 0;
		AslRequestCount = 0;
		AslFreeCount = 0;
		AslRequestResult = 1;
		LastAslRequestType = 0;
		LastAslTags = APTR.Null;
		LastAslRequester = APTR.Null;
		RequestCallCount = 0;
		RequestObjectCallCount = 0;
		RequestResult = 1;
		RequestObjectResult = 1;
		LastRequestApplication = APTR.Null;
		LastRequestWindow = APTR.Null;
		LastRequestTitle = APTR.Null;
		LastRequestGadgets = APTR.Null;
		LastRequestFormat = APTR.Null;
		LastRequestParameters = APTR.Null;
		LastRequestObject = APTR.Null;
		ObjectRetainCount = 0;
		ObjectReleaseCount = 0;
		LastRetainedObject = APTR.Null;
		LastReleasedObject = APTR.Null;
		InstallRegionCount = 0;
		RestoreRegionCount = 0;
		LastRegionLayer = APTR.Null;
		LastInstalledRegion = APTR.Null;
		LastRestoredRegion = APTR.Null;
		NextRegionToken = 0x2A000;
		NextPenToken = 0x00010005;
		PenObtainFailure = false;
		PenObtainFailureValue = -1;
		ObtainPenCount = 0;
		ReleasePenCount = 0;
		GetRGBColorCount = 0;
		LastObtainedPen = 0;
		LastReleasedPen = 0;
		LastObtainPenFlags = 0;
		LastPenRenderInfo = APTR.Null;
		LastPenSpec = APTR.Null;
		RgbRed = 0;
		RgbGreen = 0;
		RgbBlue = 0;
		GetRGBColorResult = true;
		ClipPushCount = 0;
		ClipPopCount = 0;
		LastPushClipLayer = APTR.Null;
		LastClipLeft = 0;
		LastClipTop = 0;
		LastClipWidth = 0;
		LastClipHeight = 0;
		LastPopClipLayer = APTR.Null;
		LastPoppedClip = APTR.Null;
		BeginUpdateCount = 0;
		EndUpdateCount = 0;
		BeginUpdateFails = false;
		LastBeginUpdateLayer = APTR.Null;
		LastEndUpdateLayer = APTR.Null;
		LastEndUpdateCompleted = false;
		NextProcessToken = 0x00C0DE01;
		ProcessLaunchFailure = false;
		ProcessKillResult = true;
		ProcessPollStatus = MuiProcessSchedulerStatus.Running;
		ProcessPendingSignals = 0;
		ProcessLaunchCount = 0;
		ProcessKillCount = 0;
		ProcessPollCount = 0;
		ProcessSignalCount = 0;
		ProcessSignalsReceivedCount = 0;
		LastLaunchName = APTR.Null;
		LastLaunchPriority = 0;
		LastLaunchStackSize = 0;
		LastLaunchSourceClass = APTR.Null;
		LastLaunchSourceObject = APTR.Null;
		LastKilledToken = 0;
		LastSignaledProcessToken = 0;
		LastSignaledProcessMask = 0;
		LastSignalsReceivedMask = 0;
		LoadableExternalClassId = APTR.Null;
		LoadableExternalClass = APTR.Null;
		ExternalClassOpenFailure = false;
		NewObjectFailure = false;
		OpenExternalClassCount = 0;
		CloseExternalClassCount = 0;
		LastOpenedExternalClassId = APTR.Null;
		LastClosedExternalClass = APTR.Null;
		AcquirablePictureName = APTR.Null;
		NextPictureObject = 0x00D70000;
		AcquirePictureFailure = false;
		LayoutPictureResult = true;
		PictureWidth = 0;
		PictureHeight = 0;
		DrawPictureResult = true;
		AcquirePictureCount = 0;
		ReleasePictureCount = 0;
		LayoutPictureCount = 0;
		DrawPictureCount = 0;
		LastAcquiredPictureName = APTR.Null;
		LastAcquiredPictureScreen = APTR.Null;
		LastReleasedPicture = APTR.Null;
		LastDrawnPicture = APTR.Null;
		LastDrawnPictureWidth = 0;
		LastDrawnPictureHeight = 0;
	}
	public APTR Allocate(uint byteSize, uint flags)
	{
		if (byteSize == 0) return APTR.Null;
		var result = APTR.FromPointer(_next);
		_next = checked((_next + byteSize + 3) & ~3u);
		if (!IsMapped(result, byteSize)) return APTR.Null;
		AllocationCount++;
		Clear(result, byteSize);
		return result;
	}

	public void Free(APTR address, uint byteSize)
	{
		Assert.True(IsMapped(address, byteSize));
		FreeCount++;
	}

	public APTR MakeClass(APTR classId, APTR superClass, ushort instanceSize,
		APTR dispatcher)
	{
		var result = Allocate(24, 0);
		if (result.IsNull) return result;
		WriteUInt32(result, 0, classId.Raw);
		WriteUInt32(result, 4, superClass.Raw);
		WriteUInt16(result, 8, instanceSize);
		WriteUInt32(result, 12, dispatcher.Raw);
		return result;
	}

	public bool AddClass(APTR classPointer) => classPointer.IsNotNull;
	public bool RemoveClass(APTR classPointer) => classPointer.IsNotNull;
	public bool FreeClass(APTR classPointer)
	{
		if (classPointer.IsNull) return false;
		Free(classPointer, 24);
		return true;
	}

	public APTR NewObject(APTR classPointer, APTR tagList)
	{
		if (classPointer.IsNull || NewObjectFailure) return APTR.Null;
		var result = Allocate(16, 0);
		if (result.IsNull) return result;
		WriteUInt32(result, 0, classPointer.Raw);
		WriteUInt32(result, 4, 1);
		return result;
	}

	public uint DoMethod(APTR obj, APTR message)
	{
		ObserveCallingFlag();
		DispatchCount++;
		LastDispatchObject = obj;
		LastDispatchMethod = ReadUInt32(message, 0);
		LastDispatchArgument = IsMapped(message, 8) ? ReadUInt32(message, 4) : 0;
		if (MutationMode == 1 && obj.Raw == MutationDestination.Raw)
		{
			MuiNotifyCore.Remove(ref this, State, MutationSource,
				MutationAttribute, APTR.Null, false);
		}
		else if (MutationMode == 2 && obj.Raw == MutationDestination.Raw &&
			RecursiveDispatches < 40)
		{
			RecursiveDispatches++;
			MuiHeadlessObjectCore.SetAttribute(ref this, State, MutationSource,
				MutationAttribute, RecursiveDispatches, true);
		}
		return DispatchResult;
	}

	public uint CoerceMethod(APTR classPointer, APTR obj, APTR message)
	{
		ObserveCallingFlag();
		// Keep the same observable dispatch registers as DoMethod while storing
		// the explicitly selected class in LastDispatchArgument. This makes the
		// class-coercion route distinguishable without adding managed state.
		LastDispatchObject = obj;
		LastDispatchMethod = ReadUInt32(message, 0);
		LastDispatchArgument = classPointer.Raw;
		return DispatchResult;
	}

	private void ObserveCallingFlag()
	{
		if (ObservedHandler.IsNull) return;
		var snapshot = this;
		if (MuiApplicationWindowRecordPacketCore.TryReadEventHandler(
			ref snapshot, ObservedHandler, out var handler) &&
			(handler.Flags & MuiEventHandlerNodeInput.MUI_EHF_ISCALLING) != 0)
			CallingFlagObserved = true;
	}


	public uint DoSuperMethod(APTR classPointer, APTR obj, APTR message) =>
		DoMethod(obj, message);

	public APTR InstanceData(APTR classPointer, APTR obj) => obj;

	public bool RetainObject(APTR obj)
	{
		if (obj.IsNull || !IsMapped(obj, 8)) return false;
		var count = ReadUInt32(obj, 4);
		if (count == uint.MaxValue) return false;
		WriteUInt32(obj, 4, count + 1);
		ObjectRetainCount++;
		LastRetainedObject = obj;
		return true;
	}

	public bool ReleaseObject(APTR obj)
	{
		if (obj.IsNull || !IsMapped(obj, 8)) return false;
		var count = ReadUInt32(obj, 4);
		if (count == 0) return false;
		WriteUInt32(obj, 4, count - 1);
		ObjectReleaseCount++;
		LastReleasedObject = obj;
		return count == 1;
	}

	public void DisposeObject(APTR obj)
	{
		if (obj.IsNotNull && IsMapped(obj, 16))
		{
			Clear(obj, 16);
			Free(obj, 16);
		}
	}

	// ---- Callback (struct Hook) seam ----------------------------------------
	// Models exec CallHookPkt: A0 = hook base, A2 = object, A1 = message, result
	// in D0. The hook base is delivered (not a pre-extracted h_Entry), so a
	// callback can reach its own h_Data at hook+16 exactly as on real hardware.
	// A small set of h_Entry sentinels lets tests drive arbitrary List hooks and
	// assert register delivery plus h_Data reachability; every other h_Entry
	// falls back to the legacy DoMethod stub.
	internal const uint HookEntryConstruct = 0x00CA0001u;
	internal const uint HookEntryDestruct = 0x00CA0002u;
	internal const uint HookEntryCompare = 0x00CA0003u;
	internal const uint HookEntryMultiTest = 0x00CA0004u;
	internal const uint HookEntryGroupLayout = 0x00CA0005u;
	internal const uint HookEntryStringEdit = 0x00CA0006u;
	private const uint GroupLayoutMinMax = 1;
	private const uint GroupLayout = 2;
	internal const uint HookDataCookie = 0x00C0FFEEu;

	public uint InvokeHook(APTR hook, APTR objectAddress, APTR messageAddress)
	{
		if (hook.IsNull) return 0;
		HookInvokeCount++;
		LastHookBase = hook;
		LastHookA2 = objectAddress;
		LastHookA1 = messageAddress;
		// struct Hook: h_Entry at +8, h_Data at +16.
		var mapped = IsMapped(hook, 20);
		var entry = mapped ? ReadUInt32(hook, 8) : 0u;
		LastHookData = mapped ? APTR.FromPointer(ReadUInt32(hook, 16)) : APTR.Null;
		switch (entry)
		{
			case HookEntryConstruct:
				// A0 delivered the hook, so h_Data is reachable. Publish the three
				// delivered registers into the h_Data scratch and return it as the
				// newly constructed entry.
				if (LastHookData.IsNotNull && IsMapped(LastHookData, 16))
				{
					WriteUInt32(LastHookData, 0, hook.Raw);
					WriteUInt32(LastHookData, 4, objectAddress.Raw);
					WriteUInt32(LastHookData, 8, messageAddress.Raw);
					WriteUInt32(LastHookData, 12, HookDataCookie);
				}
				return LastHookData.Raw;
			case HookEntryDestruct:
				HookDestructCount++;
				return 0;
			case HookEntryCompare:
				// Gate on the h_Data cookie to prove A0 reachability, then compare
				// the first bytes of A2 (entry1) and A1 (entry2).
				if (LastHookData.IsNull || !IsMapped(LastHookData, 4) ||
					ReadUInt32(LastHookData, 0) != HookDataCookie) return 0;
				return unchecked((uint)(ReadUInt8(objectAddress, 0) -
					ReadUInt8(messageAddress, 0)));
			case HookEntryMultiTest:
				// Permit multiselection unless the entry pointer (A1) matches the
				// denied pointer recorded in h_Data[0]; the compare needs A0.
				if (LastHookData.IsNull || !IsMapped(LastHookData, 4)) return 1;
				return ReadUInt32(LastHookData, 0) == messageAddress.Raw ? 0u : 1u;
			case HookEntryGroupLayout:
				if (!MUI_LayoutMsgCodec.TryRead(ref this, messageAddress,
					out var layoutMessage)) return 0;
				LastLayoutHookChildren = layoutMessage.lm_Children;
				var cursor = layoutMessage.lm_Children.IsNull ||
					!IsMapped(layoutMessage.lm_Children, Amiga.List.Size)
					? 0u : ReadUInt32(layoutMessage.lm_Children,
						ExecLayout.List.Head);
				LastLayoutHookFirstChild = MuiGroupChildrenCore.NextObject(ref this,
					layoutMessage.lm_Children, ref cursor);
				if (layoutMessage.lm_Type == GroupLayoutMinMax)
				{
					LayoutHookMinMaxCount++;
					layoutMessage.lm_MinMax = new MUI_MinMax
					{
						MinWidth = 13, MinHeight = 17, MaxWidth = 101,
						MaxHeight = 107, DefWidth = 31, DefHeight = 37,
					};
					MUI_LayoutMsgCodec.Write(ref this, messageAddress,
						layoutMessage);
					return 0;
				}
				if (layoutMessage.lm_Type == GroupLayout)
				{
					LayoutHookLayoutCount++;
					MUI_LayoutMsgCodec.Write(ref this, messageAddress,
						layoutMessage);
					return 1;
				}
				return 0;
			case HookEntryStringEdit:
				if (!MuiStringEditWorkCodec.TryRead(ref this, objectAddress,
					out var stringEdit)) return 0;
				stringEdit.Actions = StringEditHookActions;
				if (StringEditHookBuffer.IsNotNull)
					stringEdit.WorkBuffer = StringEditHookBuffer;
				MuiStringEditWorkCodec.Write(ref this, objectAddress, stringEdit);
				return StringEditHookResult;
			default:
				return DoMethod(objectAddress, messageAddress);
		}
	}

	public uint CurrentTaskToken() => CurrentTask;

	public bool LockLayer(APTR layer)
	{
		if (layer.IsNull) return false;
		LayerDepth++;
		return true;
	}
	public void UnlockLayer(APTR layer)
	{
		if (LayerDepth != 0) LayerDepth--;
	}
	public bool BeginUpdate(APTR layer)
	{
		LastBeginUpdateLayer = layer;
		if (layer.IsNull || BeginUpdateFails) return false;
		BeginUpdateCount++;
		return true;
	}
	public void EndUpdate(APTR layer, bool completed)
	{
		EndUpdateCount++;
		LastEndUpdateLayer = layer;
		LastEndUpdateCompleted = completed;
	}

	public int TranslateTextInput(APTR intuiMessage)
	{
		if (intuiMessage.IsNull || !IsMapped(intuiMessage, 28) ||
			ReadUInt32(intuiMessage, 20) != 0x00000400u) return -1;
		return ReadUInt16(intuiMessage, 24);
	}

	public APTR PushClip(APTR layer, int left, int top, int width, int height)
	{
		ClipPushCount++;
		LastPushClipLayer = layer;
		LastClipLeft = left;
		LastClipTop = top;
		LastClipWidth = width;
		LastClipHeight = height;
		return APTR.FromPointer(1);
	}
	public void PopClip(APTR layer, APTR previousClip)
	{
		ClipPopCount++;
		LastPopClipLayer = layer;
		LastPoppedClip = previousClip;
	}
	public int TextWidth(APTR rastPort, APTR font, APTR text, int length) =>
		length < 0 ? 0 : length * 8;
	public int TextHeight(APTR rastPort, APTR font) => 8;
	public void SetPen(APTR rastPort, uint pen) => LastPen = pen;
	public void FillRectangle(APTR rastPort, int left, int top, int right,
		int bottom)
	{
		FillCount++;
		LastLeft = left;
		LastTop = top;
		LastRight = right;
		LastBottom = bottom;
	}
	public void DrawLine(APTR rastPort, int x1, int y1, int x2, int y2)
	{
		LineCount++;
		LastLineX1 = x1;
		LastLineY1 = y1;
		LastLineX2 = x2;
		LastLineY2 = y2;
	}
	public void DrawText(APTR rastPort, APTR font, int left, int baseline,
		APTR text, int length)
	{
		if (TextCount == 0)
		{
			FirstText = text;
			FirstTextLeft = left;
		}
		else if (TextCount == 1)
		{
			SecondText = text;
			SecondTextLeft = left;
		}
		else if (TextCount == 2)
		{
			ThirdText = text;
			ThirdTextLeft = left;
		}
		TextCount++;
		LastText = text;
		LastTextLength = length;
		LastTextLeft = left;
		LastTextBaseline = baseline;
	}
	public void DrawImage(APTR rastPort, APTR image, int left, int top, int width,
		int height) => ImageCount++;
	public bool ScheduleRedraw(APTR obj, uint flags)
	{
		if (obj.IsNull) return false;
		RedrawCount++;
		return true;
	}
	public APTR OpenMuiWindow(APTR windowObject)
	{
		if (windowObject.IsNull) return APTR.Null;
		WindowOpenCount++;
		return APTR.FromPointer(0x1800 + WindowOpenCount * 0x20);
	}
	public bool ShowMuiAbout(APTR application, APTR refWindow)
	{
		if (application.IsNull) return false;
		AboutMUIRequestCount++;
		LastAboutMUIApplication = application;
		LastAboutMUIReference = refWindow;
		return true;
	}
	public bool ShowMuiHelp(APTR application, APTR window, APTR name,
		APTR node, int line)
	{
		if (application.IsNull) return false;
		ShowHelpRequestCount++;
		LastShowHelpApplication = application;
		LastShowHelpWindow = window;
		LastShowHelpName = name;
		LastShowHelpNode = node;
		LastShowHelpLine = line;
		return true;
	}
	public bool GetApplicationDefaultConfigItem(APTR application, uint configId,
		out uint value)
	{
		value = DefaultConfigItemValue;
		if (application.IsNull) return false;
		DefaultConfigRequestCount++;
		LastDefaultConfigApplication = application;
		LastDefaultConfigId = configId;
		return true;
	}
	public bool GetMuiConfigItem(APTR objectAddress, uint configId,
		out uint value)
	{
		value = PublicScreenConfigValue;
		if (!ConfigItemOperationResult || objectAddress.IsNull || configId != 0x24)
			return false;
		ConfigItemRequestCount++;
		LastConfigItemObject = objectAddress;
		LastConfigItemId = configId;
		return true;
	}
	public APTR BuildMuiSettingsPanel(APTR application, uint number)
	{
		if (application.IsNull) return APTR.Null;
		BuildSettingsPanelRequestCount++;
		LastBuildSettingsPanelApplication = application;
		LastBuildSettingsPanelNumber = number;
		return SettingsPanelResult;
	}
	public bool OpenMuiConfigWindow(APTR application, uint flags, APTR classId)
	{
		if (application.IsNull) return false;
		OpenConfigWindowRequestCount++;
		LastOpenConfigWindowApplication = application;
		LastOpenConfigWindowFlags = flags;
		LastOpenConfigWindowClassId = classId;
		return true;
	}
	public bool SaveMuiApplicationSettings(APTR state, APTR application, APTR name)
	{
		if (application.IsNull || !SettingsOperationResult) return false;
		if (UseSettingsFile && !MuiApplicationSettingsFileCore.Save(ref this,
			state, application, name)) return false;
		SettingsSaveRequestCount++;
		LastSettingsApplication = application;
		LastSettingsName = name;
		return true;
	}
	public bool LoadMuiApplicationSettings(APTR state, APTR application, APTR name)
	{
		if (application.IsNull || !SettingsOperationResult) return false;
		if (UseSettingsFile && !MuiApplicationSettingsFileCore.Load(ref this,
			state, application, name)) return false;
		SettingsLoadRequestCount++;
		LastSettingsApplication = application;
		LastSettingsName = name;
		return true;
	}
	public bool ExportMuiObject(APTR obj, APTR dataspace, uint objectId)
	{
		if (obj.IsNull || dataspace.IsNull || objectId == 0 ||
			!PersistenceOperationResult) return false;
		ObjectExportRequestCount++;
		PreviousPersistenceObject = LastPersistenceObject;
		LastPersistenceObject = obj;
		LastPersistenceDataspace = dataspace;
		LastPersistenceObjectId = objectId;
		return true;
	}
	public bool ImportMuiObject(APTR obj, APTR dataspace, uint objectId)
	{
		if (obj.IsNull || dataspace.IsNull || objectId == 0 ||
			!PersistenceOperationResult ||
			(PersistenceImportFailureDataspace.IsNotNull &&
			 dataspace.Raw == PersistenceImportFailureDataspace.Raw &&
			 (PersistenceImportFailureObject.IsNull ||
			  obj.Raw == PersistenceImportFailureObject.Raw))) return false;
		ObjectImportRequestCount++;
		PreviousPersistenceObject = LastPersistenceObject;
		LastPersistenceObject = obj;
		LastPersistenceDataspace = dataspace;
		LastPersistenceObjectId = objectId;
		return true;
	}
	public bool RefreshMuiWindow(APTR windowObject)
	{
		if (windowObject.IsNull) return false;
		RefreshMuiWindowCount++;
		LastRefreshedMuiWindow = windowObject;
		return true;
	}
	public void CloseMuiWindow(APTR nativeWindow) => WindowCloseCount++;
	public bool ConfigureWindowEvents(APTR nativeWindow, uint eventMask)
	{
		WindowEventMask = eventMask;
		return nativeWindow.IsNotNull;
	}
	public uint ReadWindowEvent(APTR nativeWindow, APTR eventStorage)
	{
		var result = PendingWindowEvent;
		PendingWindowEvent = 0;
		if (result != 0 && eventStorage.IsNotNull)
			WriteUInt32(eventStorage, 0, result);
		return result;
	}
	public bool ActivateMuiWindow(APTR nativeWindow)
	{
		if (nativeWindow.IsNull) return false;
		WindowActivationCount++;
		return true;
	}
	public bool SetMuiWindowBusy(APTR nativeWindow, bool busy)
	{
		if (nativeWindow.IsNull) return false;
		WindowBusyOperationCount++;
		WindowBusy = busy;
		return true;
	}
	public bool SetMuiWindowTabletMessages(APTR nativeWindow, bool enabled)
	{
		if (nativeWindow.IsNull) return false;
		WindowTabletMessagesOperationCount++;
		WindowTabletMessages = enabled;
		return true;
	}
	public bool SetMuiWindowBorderScrollers(APTR nativeWindow, bool useBottom,
		bool useLeft, bool useRight)
	{
		if (nativeWindow.IsNull) return false;
		WindowBorderScrollerOperationCount++;
		WindowUseBottomBorderScroller = useBottom;
		WindowUseLeftBorderScroller = useLeft;
		WindowUseRightBorderScroller = useRight;
		return true;
	}
	public bool ConfigureMuiWindowAlternateGeometry(APTR nativeWindow,
		MuiWindowPublicCore.MuiWindowAlternateGeometry geometry)
	{
		if (nativeWindow.IsNull) return false;
		WindowAlternateGeometryOperationCount++;
		WindowAlternateGeometry = geometry;
		return true;
	}
	public bool ConfigureMuiWindowGeometry(APTR nativeWindow,
		MuiWindowPublicCore.MuiWindowGeometry geometry)
	{
		if (nativeWindow.IsNull) return false;
		WindowGeometryOperationCount++;
		WindowGeometry = geometry;
		return true;
	}
	public bool ConfigureMuiWindowGadgets(APTR nativeWindow,
		MuiWindowPublicCore.MuiWindowGadgetPolicy policy)
	{
		if (nativeWindow.IsNull) return false;
		WindowGadgetPolicyOperationCount++;
		WindowGadgetPolicy = policy;
		return true;
	}
	public bool ConfigureMuiWindowMode(APTR nativeWindow,
		MuiWindowPublicCore.MuiWindowModePolicy policy)
	{
		if (nativeWindow.IsNull) return false;
		WindowModePolicyOperationCount++;
		WindowModePolicy = policy;
		return true;
	}
	public bool MoveMuiWindow(APTR nativeWindow, bool toFront) =>
		nativeWindow.IsNotNull;
	public bool MoveMuiScreen(APTR nativeWindow, bool toFront)
	{
		if (nativeWindow.IsNull) return false;
		ScreenDepthOperationCount++;
		LastScreenDepthToFront = toFront;
		return true;
	}
	public bool SnapshotMuiWindow(APTR nativeWindow, uint flags)
	{
		if (nativeWindow.IsNull || flags > 1) return false;
		WindowSnapshotCount++;
		LastSnapshotMuiWindow = nativeWindow;
		LastSnapshotFlags = flags;
		return true;
	}
	public bool SetMuiMenuState(APTR nativeWindow, uint menuId, bool enabled,
		bool check, bool checkedState)
	{
		if (nativeWindow.IsNull) return false;
		MenuOperationCount++;
		return true;
	}
	public bool GetMuiMenuState(APTR nativeWindow, uint menuId, bool check,
		out bool state)
	{
		state = true;
		return nativeWindow.IsNotNull;
	}
	public bool SetApplicationIconified(APTR application, bool iconified)
	{
		Iconified = iconified;
		return application.IsNotNull;
	}
	public bool CoordinateRequester(APTR application, APTR window, APTR requester,
		bool open)
	{
		RequesterOperationCount++;
		return application.IsNotNull && requester.IsNotNull;
	}
	public uint ReadTicks() => Ticks;
	public uint ReadSignals(uint signalMask)
	{
		var delivered = PendingSignals & signalMask;
		PendingSignals &= ~delivered;
		return delivered;
	}
	public uint WaitMuiSignals(uint signalMask)
	{
		WaitMuiSignalsCount++;
		return PendingSignals & signalMask;
	}
	public void SignalTask(uint taskToken, uint signalMask)
	{
		SignaledMask |= signalMask;
		PendingSignals |= signalMask;
	}

	public byte ReadUInt8(APTR address, int offset) =>
		_memory[Index(address, offset, 1)];
	public ushort ReadUInt16(APTR address, int offset) =>
		BinaryPrimitives.ReadUInt16BigEndian(
			_memory.AsSpan(Index(address, offset, 2), 2));
	public uint ReadUInt32(APTR address, int offset) =>
		BinaryPrimitives.ReadUInt32BigEndian(
			_memory.AsSpan(Index(address, offset, 4), 4));
	public void WriteUInt8(APTR address, int offset, byte value) =>
		_memory[Index(address, offset, 1)] = value;
	public void WriteUInt16(APTR address, int offset, ushort value) =>
		BinaryPrimitives.WriteUInt16BigEndian(
			_memory.AsSpan(Index(address, offset, 2), 2), value);
	public void WriteUInt32(APTR address, int offset, uint value) =>
		BinaryPrimitives.WriteUInt32BigEndian(
			_memory.AsSpan(Index(address, offset, 4), 4), value);
	public void Clear(APTR address, uint byteSize) =>
		_memory.AsSpan(Index(address, 0, checked((int)byteSize)),
			checked((int)byteSize)).Clear();
	public void Copy(APTR source, APTR destination, uint byteSize) =>
		_memory.AsSpan(Index(source, 0, checked((int)byteSize)),
			checked((int)byteSize)).CopyTo(
				_memory.AsSpan(Index(destination, 0, checked((int)byteSize)),
					checked((int)byteSize)));
	public bool IsMapped(APTR address, uint byteSize) =>
		address.Raw >= _baseAddress && address.Raw - _baseAddress <=
			(uint)_memory.Length && byteSize <= (uint)_memory.Length -
			(address.Raw - _baseAddress);

	public int DirectoryScan(APTR path)
	{
		DirectoryScanCount++;
		if (DirectoryMissing)
		{
			LastDirectoryError = DirectoryErrorCode == 0 ? 205 :
				DirectoryErrorCode;
			return -1;
		}
		LastDirectoryError = 0;
		return DirectoryCount < 0 ? -1 : DirectoryCount;
	}
	public bool DirectoryEntry(APTR path, int index, APTR storage)
	{
		if (index < 0 || index >= DirectoryCount || index == DirectoryFailIndex ||
			!IsMapped(storage, 224))
		{
			LastDirectoryError = DirectoryErrorCode == 0 ? 103 :
				DirectoryErrorCode;
			return false;
		}
		Clear(storage, 224);
		// Deterministic synthetic directory: two drawers and three files, one of
		// which is an icon (*.info). Sizes let counter/NumBytes assertions be
		// exact; names give sort/pattern tests distinct, ordered targets.
		var type = index == 0 || index == 4 ? 2u : unchecked((uint)-3);
		WriteUInt32(storage, 0, type);
		WriteUInt32(storage, 4, index switch
		{
			1 => 300u,
			2 => 200u,
			3 => 100u,
			_ => 0u,
		});
		var name = APTR.FromPointer(storage.Raw + 28);
		switch (index)
		{
			case 0: WriteCString(name, "drawerB"); break;
			case 1: WriteCString(name, "alpha.txt"); break;
			case 2: WriteCString(name, "beta.info"); break;
			case 3: WriteCString(name, "charlie.txt"); break;
			case 4: WriteCString(name, "drawerA"); break;
			default: WriteCString(name, "entry"); break;
		}
		WriteCString(APTR.FromPointer(storage.Raw + 136), "");
		return true;
	}
	public int VolumeScan()
	{
		LastDirectoryError = 0;
		return VolumeCount < 0 ? -1 : VolumeCount;
	}
	public bool VolumeEntry(int index, APTR storage)
	{
		if (index < 0 || index >= VolumeCount || !IsMapped(storage, 224))
		{
			LastDirectoryError = DirectoryErrorCode == 0 ? 103 :
				DirectoryErrorCode;
			return false;
		}
		WriteUInt32(storage, 0, 2);
		WriteUInt32(storage, 4, 0);
		WriteUInt32(storage, 8, 0);
		WriteUInt32(storage, 12, 0);
		WriteUInt32(storage, 16, 0);
		WriteUInt32(storage, 20, 0);
		WriteUInt32(storage, 24, 0);
		var name = APTR.FromPointer(storage.Raw + 28);
		if (index == 0) WriteCString(name, "DH0:");
		else if (index == 1) WriteCString(name, "RAM:");
		else WriteCString(name, "SYS:");
		WriteCString(APTR.FromPointer(storage.Raw + 136), "volume");
		return true;
	}
	public int DirectoryRename(APTR path, APTR fromName, APTR toName)
	{
		LastDirectoryError = RenameResult;
		return RenameResult;
	}
	public int DirectorySetComment(APTR path, APTR name, APTR comment)
	{
		LastDirectoryError = SetCommentResult;
		return SetCommentResult;
	}
	public int DirectorySetProtection(APTR path, APTR name, uint mask)
	{
		LastDirectoryError = SetProtectionResult;
		return SetProtectionResult;
	}
	public int DirectoryError() => LastDirectoryError != 0 ? LastDirectoryError :
		DirectoryErrorCode;

	// ---- Bounded guest-backed DOS file capability --------------------------
	// Handles are opaque tokens; file bytes stay in the platform's guest arena
	// so the codec exercises the same pointer and short-transfer rules as a
	// native DOS provider.
	public APTR Open(APTR name, int mode)
	{
		DosOpenCount++;
		if (!SettingsOperationResult || (mode == MuiApplicationSettingsFileCore.OldFileMode &&
			(SettingsFileBuffer.IsNull || SettingsFileLength == 0)))
		{
			LastDosError = 205;
			return APTR.Null;
		}
		if (SettingsFileBuffer.IsNull)
		{
			SettingsFileBuffer = Allocate(65536, 0);
			if (SettingsFileBuffer.IsNull)
			{
				LastDosError = 103;
				return APTR.Null;
			}
		}
		if (mode == MuiApplicationSettingsFileCore.NewFileMode)
			SettingsFileLength = 0;
		SettingsFilePosition = 0;
		LastDosError = 0;
		return APTR.FromPointer(1);
	}

	public int Close(APTR handle)
	{
		if (handle.Raw != 1) return -1;
		DosCloseCount++;
		return 0;
	}

	public int Read(APTR handle, APTR buffer, uint length)
	{
		if (handle.Raw != 1 || buffer.IsNull || !IsMapped(buffer, length) ||
			SettingsFileBuffer.IsNull || SettingsFilePosition >= SettingsFileLength)
			return 0;
		var available = SettingsFileLength - SettingsFilePosition;
		var count = length < available ? length : available;
		Copy(APTR.FromPointer(SettingsFileBuffer.Raw + SettingsFilePosition),
			buffer, count);
		SettingsFilePosition += count;
		return unchecked((int)count);
	}

	public int Write(APTR handle, APTR buffer, uint length)
	{
		if (handle.Raw != 1 || buffer.IsNull || !IsMapped(buffer, length) ||
			SettingsFileBuffer.IsNull || SettingsFilePosition > 65536 ||
			length > 65536 - SettingsFilePosition)
			return -1;
		Copy(buffer, APTR.FromPointer(SettingsFileBuffer.Raw + SettingsFilePosition),
			length);
		SettingsFilePosition += length;
		if (SettingsFilePosition > SettingsFileLength)
			SettingsFileLength = SettingsFilePosition;
		return unchecked((int)length);
	}

	// ---- Bounded guest-backed IFF chunk capability ------------------------
	public int ReadChunkBytes(APTR handle, APTR buffer, uint length)
	{
		if (IffReadError != 0) return IffReadError;
		if (handle.Raw != 2 || buffer.IsNull || !IsMapped(buffer, length) ||
			IffBuffer.IsNull || IffPosition >= IffLength) return 0;
		var available = IffLength - IffPosition;
		var count = length < available ? length : available;
		if (IffReadChunkLimit != 0 && count > IffReadChunkLimit)
			count = IffReadChunkLimit;
		Copy(APTR.FromPointer(IffBuffer.Raw + IffPosition), buffer, count);
		IffPosition += count;
		return unchecked((int)count);
	}

	public int WriteChunkBytes(APTR handle, APTR buffer, uint length)
	{
		if (IffWriteError != 0) return IffWriteError;
		if (handle.Raw != 2 || buffer.IsNull || !IsMapped(buffer, length) ||
			IffBuffer.IsNull || IffPosition > IffCapacity ||
			length > IffCapacity - IffPosition) return -6;
		var count = length;
		if (IffWriteChunkLimit != 0 && count > IffWriteChunkLimit)
			count = IffWriteChunkLimit;
		Copy(buffer, APTR.FromPointer(IffBuffer.Raw + IffPosition), count);
		IffPosition += count;
		if (IffPosition > IffLength) IffLength = IffPosition;
		return unchecked((int)count);
	}

	public int PushChunk(APTR handle, uint type, uint id, uint size)
	{
		if (handle.Raw != 2) return -5;
		IffPushCount++;
		IffLastType = type;
		IffLastId = id;
		IffLength = 0;
		IffPosition = 0;
		return 0;
	}

	public int PopChunk(APTR handle)
	{
		if (handle.Raw != 2) return -5;
		IffPopCount++;
		return 0;
	}

	public int IoErr() => LastDosError;

	// ---- MG09 ASL/requester capability --------------------------------------
	public APTR AllocateRequest(uint requestType, APTR tags)
	{
		var result = APTR.FromPointer(NextAslRequest);
		NextAslRequest += 0x20;
		AslAllocateCount++;
		LastAslRequestType = requestType;
		LastAslTags = tags;
		return result;
	}

	public int Request(APTR requester, APTR tags)
	{
		AslRequestCount++;
		LastAslRequester = requester;
		LastAslTags = tags;
		return requester.IsNotNull ? unchecked((int)AslRequestResult) : 0;
	}

	public void FreeRequest(APTR requester)
	{
		if (requester.IsNotNull) AslFreeCount++;
		LastAslRequester = requester;
	}

	public int Request(APTR application, APTR window, uint flags, APTR title,
		APTR gadgets, APTR format, APTR parameters)
	{
		RequestCallCount++;
		LastRequestApplication = application;
		LastRequestWindow = window;
		LastRequestTitle = title;
		LastRequestGadgets = gadgets;
		LastRequestFormat = format;
		LastRequestParameters = parameters;
		return RequestResult;
	}

	public int RequestObject(APTR application, APTR window, uint flags,
		APTR title, APTR gadgets, APTR obj, APTR format, APTR parameters)
	{
		RequestObjectCallCount++;
		LastRequestApplication = application;
		LastRequestWindow = window;
		LastRequestTitle = title;
		LastRequestGadgets = gadgets;
		LastRequestObject = obj;
		LastRequestFormat = format;
		LastRequestParameters = parameters;
		return RequestObjectResult;
	}

	// ---- MG09 class-service capability --------------------------------------
	public APTR OpenLibrary(APTR name, ushort minimumVersion)
	{
		OpenLibraryCount++;
		LastOpenLibraryName = name;
		if (name.IsNull || LoadableLibraryName.IsNull) return APTR.Null;
		return CStringEquals(name, LoadableLibraryName) ? LoadableLibraryBase :
			APTR.Null;
	}

	public void CloseLibrary(APTR library)
	{
		if (library.IsNotNull) CloseLibraryCount++;
	}

	public APTR MakeCustomClass(APTR superClass, ushort instanceSize,
		APTR dispatcher, APTR libraryBase)
	{
		if (dispatcher.IsNull) return APTR.Null;
		var result = Allocate(24, 0);
		if (result.IsNull) return result;
		MakeCustomClassCount++;
		LastCustomLibraryBase = libraryBase;
		WriteUInt32(result, 0, superClass.Raw);
		WriteUInt16(result, 4, instanceSize);
		WriteUInt32(result, 8, dispatcher.Raw);
		WriteUInt32(result, 12, libraryBase.Raw);
		WriteUInt32(result, 16, 0x00C1A550u);   // MakeCustomClass marker
		return result;
	}

	public bool FreeCustomClass(APTR classPointer)
	{
		if (classPointer.IsNull || !IsMapped(classPointer, 24) ||
			ReadUInt32(classPointer, 16) != 0x00C1A550u) return false;
		FreeCustomClassCount++;
		Free(classPointer, 24);
		return true;
	}

	public APTR ResolvePublicClass(APTR classId)
	{
		ResolvePublicClassCount++;
		if (classId.IsNull || LoadablePublicClassId.IsNull) return APTR.Null;
		return CStringEquals(classId, LoadablePublicClassId) ? LoadablePublicClass :
			APTR.Null;
	}

	// The A6 library base bound to a class created by MakeCustomClass.
	public APTR CustomClassLibraryBase(APTR classPointer) =>
		classPointer.IsNotNull && IsMapped(classPointer, 20) ?
			APTR.FromPointer(ReadUInt32(classPointer, 12)) : APTR.Null;

	private bool CStringEquals(APTR a, APTR b)
	{
		if (a.Raw == b.Raw) return true;
		for (var index = 0; index < 4096; index++)
		{
			if (!IsMapped(a, (uint)index + 1) || !IsMapped(b, (uint)index + 1))
				return false;
			var left = ReadUInt8(a, index);
			var right = ReadUInt8(b, index);
			if (left != right) return false;
			if (left == 0) return true;
		}
		return false;
	}

	public void WriteCString(APTR address, string value)
	{
		var bytes = Encoding.ASCII.GetBytes(value);
		for (var index = 0; index < bytes.Length; index++)
			WriteUInt8(address, index, bytes[index]);
		WriteUInt8(address, bytes.Length, 0);
	}

	// ---- MG09 drawing-service region capability -----------------------------
	public APTR InstallClipRegion(APTR layer, APTR region)
	{
		InstallRegionCount++;
		LastRegionLayer = layer;
		LastInstalledRegion = region;
		var previous = APTR.FromPointer(NextRegionToken);
		NextRegionToken += 0x10;
		return previous;
	}

	public void RestoreClipRegion(APTR layer, APTR previousRegion)
	{
		RestoreRegionCount++;
		LastRegionLayer = layer;
		LastRestoredRegion = previousRegion;
	}

	// ---- MG09 drawing-service pen capability --------------------------------
	public int ObtainPen(APTR renderInfo, APTR penSpec, uint flags)
	{
		ObtainPenCount++;
		LastPenRenderInfo = renderInfo;
		LastPenSpec = penSpec;
		LastObtainPenFlags = flags;
		if (PenObtainFailure) return PenObtainFailureValue;
		var token = unchecked((int)NextPenToken);
		NextPenToken++;
		LastObtainedPen = token;
		return token;
	}

	public void ReleasePen(APTR renderInfo, int pen)
	{
		ReleasePenCount++;
		LastReleasedPen = pen;
		LastPenRenderInfo = renderInfo;
	}

	public bool GetRGBColor(APTR renderInfo, APTR penSpec, APTR rgbColor)
	{
		GetRGBColorCount++;
		LastPenRenderInfo = renderInfo;
		LastPenSpec = penSpec;
		if (!GetRGBColorResult || penSpec.IsNull || rgbColor.IsNull) return false;
		WriteUInt32(rgbColor, 0, RgbRed);
		WriteUInt32(rgbColor, 4, RgbGreen);
		WriteUInt32(rgbColor, 8, RgbBlue);
		return true;
	}

	// ---- MG09 Process/Slave scheduler capability ----------------------------
	public uint ProcessLaunch(APTR name, int priority, uint stackSize,
		APTR sourceClass, APTR sourceObject)
	{
		ProcessLaunchCount++;
		LastLaunchName = name;
		LastLaunchPriority = priority;
		LastLaunchStackSize = stackSize;
		LastLaunchSourceClass = sourceClass;
		LastLaunchSourceObject = sourceObject;
		if (ProcessLaunchFailure) return 0;
		var token = NextProcessToken;
		NextProcessToken++;
		return token;
	}

	public bool ProcessKill(uint taskToken)
	{
		ProcessKillCount++;
		LastKilledToken = taskToken;
		return ProcessKillResult;
	}

	public uint ProcessPoll(uint taskToken)
	{
		ProcessPollCount++;
		return ProcessPollStatus;
	}

	public void ProcessSignal(uint taskToken, uint signalMask)
	{
		ProcessSignalCount++;
		LastSignaledProcessToken = taskToken;
		LastSignaledProcessMask = signalMask;
	}

	public uint ProcessSignalsReceived(uint signalMask)
	{
		ProcessSignalsReceivedCount++;
		LastSignalsReceivedMask = signalMask;
		var delivered = ProcessPendingSignals & signalMask;
		ProcessPendingSignals &= ~delivered;
		return delivered;
	}

	// ---- MG09 external BOOPSI / datatypes seams -----------------------------
	// These deterministic defaults keep the shared platform usable by the
	// service aggregate until focused host tests inject an external class or
	// picture model. They never expose host objects to production code.

	// ---- MG09 external BOOPSI loader capability -----------------------------
	public APTR OpenExternalClass(APTR classId)
	{
		OpenExternalClassCount++;
		LastOpenedExternalClassId = classId;
		if (ExternalClassOpenFailure || classId.IsNull) return APTR.Null;
		if (LoadableExternalClassId.IsNotNull &&
			!CStringEquals(classId, LoadableExternalClassId)) return APTR.Null;
		return LoadableExternalClass.IsNotNull ? LoadableExternalClass
			: APTR.FromPointer(0x00050000);
	}

	public void CloseExternalClass(APTR classPointer)
	{
		if (classPointer.IsNotNull) CloseExternalClassCount++;
		LastClosedExternalClass = classPointer;
	}

	// ---- MG09 datatypes picture capability ----------------------------------
	public APTR AcquirePicture(APTR name, APTR screen)
	{
		AcquirePictureCount++;
		LastAcquiredPictureName = name;
		LastAcquiredPictureScreen = screen;
		if (AcquirePictureFailure || name.IsNull) return APTR.Null;
		if (AcquirablePictureName.IsNotNull &&
			!CStringEquals(name, AcquirablePictureName)) return APTR.Null;
		var result = APTR.FromPointer(NextPictureObject);
		NextPictureObject += 0x100;
		return result;
	}

	public void ReleasePicture(APTR pictureObject)
	{
		if (pictureObject.IsNotNull) ReleasePictureCount++;
		LastReleasedPicture = pictureObject;
	}

	public bool LayoutPicture(APTR pictureObject, APTR rastPort,
		APTR dimensionStorage)
	{
		LayoutPictureCount++;
		if (!LayoutPictureResult || pictureObject.IsNull ||
			dimensionStorage.IsNull || !IsMapped(dimensionStorage, 8)) return false;
		WriteUInt32(dimensionStorage, 0, unchecked((uint)PictureWidth));
		WriteUInt32(dimensionStorage, 4, unchecked((uint)PictureHeight));
		return true;
	}

	public bool DrawPicture(APTR pictureObject, APTR rastPort, int left, int top,
		int width, int height)
	{
		DrawPictureCount++;
		LastDrawnPicture = pictureObject;
		LastDrawnPictureWidth = width;
		LastDrawnPictureHeight = height;
		return DrawPictureResult && pictureObject.IsNotNull;
	}

	private int Index(APTR address, int offset, int size)
	{
		var raw = checked(address.Raw + (uint)offset);
		if (raw < _baseAddress || raw - _baseAddress > (uint)_memory.Length ||
			(uint)size > (uint)_memory.Length - (raw - _baseAddress))
			throw new ArgumentOutOfRangeException(nameof(address));
		return checked((int)(raw - _baseAddress));
	}
}
