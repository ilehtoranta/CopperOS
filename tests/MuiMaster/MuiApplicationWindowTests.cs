using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationWindowTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationPresentationReadersUseNamedMethodHeader()
	{
		const uint aboutMethod = 0x8042D21Du;
		const uint showHelpMethod = 0x80426479u;
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, aboutMethod);
		platform.WriteUInt32(packet, 4, 0x1300);
		var aboutRequest = new MuiApplicationPresentationPacketCodec.PresentationPacketAddress
		{
			Address = packet,
			Method = aboutMethod,
		};
		Assert.True(MuiApplicationPresentationPacketCodec.TryReadAboutMui(
			ref platform, ref aboutRequest, out var about));
		Assert.Equal(aboutMethod, about.MethodId);
		platform.WriteUInt32(packet, 0, showHelpMethod);
		platform.WriteUInt32(packet, 4, 0x1300);
		platform.WriteUInt32(packet, 8, 0x1400);
		platform.WriteUInt32(packet, 12, 0x1500);
		platform.WriteUInt32(packet, 16, 9);
		var helpRequest = new MuiApplicationPresentationPacketCodec.PresentationPacketAddress
		{
			Address = packet,
			Method = showHelpMethod,
		};
		Assert.True(MuiApplicationPresentationPacketCodec.TryReadShowHelp(
			ref platform, ref helpRequest, out var help));
		Assert.Equal(showHelpMethod, help.MethodId);
		Assert.Equal(9u, help.Line);
		platform.WriteUInt32(packet, 0, 0xDEADBEEFu);
		Assert.False(MuiApplicationPresentationPacketCodec.TryReadAboutMui(
			ref platform, ref aboutRequest, out _));
	}

	[Fact]
	public void ApplicationPresentationPacketFieldCursorUsesNamedMixedBoundaries()
	{
		var platform = CreatePlatform(out _);
		var showHelp = APTR.FromPointer(0x1200);
		var aboutMui = APTR.FromPointer(0x1240);

		Assert.True(MuiApplicationPresentationPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, showHelp,
			MuiApplicationPresentationPacketKind.ShowHelp,
			MuiApplicationPresentationPacketField.MethodId, 0x80426479u));
		Assert.True(MuiApplicationPresentationPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, showHelp,
			MuiApplicationPresentationPacketKind.ShowHelp,
			MuiApplicationPresentationPacketField.ReferenceWindow, 0x1300u));
		Assert.True(MuiApplicationPresentationPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, showHelp,
			MuiApplicationPresentationPacketKind.ShowHelp,
			MuiApplicationPresentationPacketField.HelpFile, 0x1400u));
		Assert.True(MuiApplicationPresentationPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, showHelp,
			MuiApplicationPresentationPacketKind.ShowHelp,
			MuiApplicationPresentationPacketField.Node, 0x1500u));
		Assert.True(MuiApplicationPresentationPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, showHelp,
			MuiApplicationPresentationPacketKind.ShowHelp,
			MuiApplicationPresentationPacketField.Line, 9u));
		Assert.True(MuiApplicationPresentationPacketFieldCursorCodec.TryReadUInt32(
			ref platform, showHelp,
			MuiApplicationPresentationPacketKind.ShowHelp,
			MuiApplicationPresentationPacketField.Line, out var line));
		Assert.Equal(9u, line);

		Assert.True(MuiApplicationPresentationPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, aboutMui,
			MuiApplicationPresentationPacketKind.AboutMui,
			MuiApplicationPresentationPacketField.MethodId, 0x8042D21Du));
		Assert.True(MuiApplicationPresentationPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, aboutMui,
			MuiApplicationPresentationPacketKind.AboutMui,
			MuiApplicationPresentationPacketField.ReferenceWindow, 0x1600u));
		Assert.True(MuiApplicationPresentationPacketFieldCursorCodec.TryReadUInt32(
			ref platform, aboutMui,
			MuiApplicationPresentationPacketKind.AboutMui,
			MuiApplicationPresentationPacketField.ReferenceWindow,
			out var referenceWindow));
		Assert.Equal(0x1600u, referenceWindow);

		Assert.False(MuiApplicationPresentationPacketFieldCursorCodec.TryReadUInt32(
			ref platform, aboutMui,
			MuiApplicationPresentationPacketKind.AboutMui,
			MuiApplicationPresentationPacketField.HelpFile, out _));
		Assert.False(MuiApplicationPresentationPacketFieldCursorCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0xFFFFFFF0u),
			MuiApplicationPresentationPacketKind.ShowHelp,
			MuiApplicationPresentationPacketField.Line, out _));
	}

	[Fact]
	public void ApplicationSettingsReadersUseNamedMethodHeader()
	{
		const uint setConfigMethod = 0x80424A80u;
		const uint openConfigMethod = 0x804299BAu;
		const uint buildPanelMethod = 0x8042B58Fu;
		const uint loadMethod = 0x8042F90Du;
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, setConfigMethod);
		platform.WriteUInt32(packet, 4, 0x22);
		platform.WriteUInt32(packet, 8, 0x1300);
		var setRequest = new MuiApplicationSettingsPacketCodec.SettingsPacketAddress
		{
			Address = packet,
			Method = setConfigMethod,
		};
		Assert.True(MuiApplicationSettingsPacketCodec.TryReadSetConfigItem(
			ref platform, ref setRequest, out var set));
		Assert.Equal(setConfigMethod, set.MethodId);
		platform.WriteUInt32(packet, 0, openConfigMethod);
		var openRequest = new MuiApplicationSettingsPacketCodec.SettingsPacketAddress
		{
			Address = packet,
			Method = openConfigMethod,
		};
		Assert.True(MuiApplicationSettingsPacketCodec.TryReadOpenConfigWindow(
			ref platform, ref openRequest, out var open));
		Assert.Equal(openConfigMethod, open.MethodId);
		platform.WriteUInt32(packet, 0, buildPanelMethod);
		var buildRequest = new MuiApplicationSettingsPacketCodec.SettingsPacketAddress
		{
			Address = packet,
			Method = buildPanelMethod,
		};
		Assert.True(MuiApplicationSettingsPacketCodec.TryReadBuildSettingsPanel(
			ref platform, ref buildRequest, out var build));
		Assert.Equal(buildPanelMethod, build.MethodId);
		platform.WriteUInt32(packet, 0, loadMethod);
		platform.WriteUInt32(packet, 4, 0x1400);
		var ioRequest = new MuiApplicationSettingsPacketCodec.SettingsPacketAddress
		{
			Address = packet,
			Method = loadMethod,
		};
		Assert.True(MuiApplicationSettingsPacketCodec.TryReadSettingsIo(ref platform,
			ref ioRequest, out var io));
		Assert.Equal(loadMethod, io.MethodId);
		platform.WriteUInt32(packet, 0, 0xDEADBEEFu);
		Assert.False(MuiApplicationSettingsPacketCodec.TryReadSettingsIo(
			ref platform, ref ioRequest, out _));
	}

	[Fact]
	public void ApplicationSettingsPacketFieldCursorUsesNamedMixedBoundaries()
	{
		var platform = CreatePlatform(out _);
		var setConfig = APTR.FromPointer(0x1200);
		var openConfig = APTR.FromPointer(0x1240);
		var buildPanel = APTR.FromPointer(0x1280);
		var settingsIo = APTR.FromPointer(0x12C0);

		Assert.True(MuiApplicationSettingsPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, setConfig,
			MuiApplicationSettingsPacketKind.SetConfigItem,
			MuiApplicationSettingsPacketField.MethodId, 0x80424A80u));
		Assert.True(MuiApplicationSettingsPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, setConfig,
			MuiApplicationSettingsPacketKind.SetConfigItem,
			MuiApplicationSettingsPacketField.Item, 0x22u));
		Assert.True(MuiApplicationSettingsPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, setConfig,
			MuiApplicationSettingsPacketKind.SetConfigItem,
			MuiApplicationSettingsPacketField.Data, 0x1300u));
		Assert.True(MuiApplicationSettingsPacketFieldCursorCodec.TryReadUInt32(
			ref platform, setConfig,
			MuiApplicationSettingsPacketKind.SetConfigItem,
			MuiApplicationSettingsPacketField.Data, out var data));
		Assert.Equal(0x1300u, data);

		Assert.True(MuiApplicationSettingsPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, openConfig,
			MuiApplicationSettingsPacketKind.OpenConfigWindow,
			MuiApplicationSettingsPacketField.MethodId, 0x804299BAu));
		Assert.True(MuiApplicationSettingsPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, openConfig,
			MuiApplicationSettingsPacketKind.OpenConfigWindow,
			MuiApplicationSettingsPacketField.Flags, 3u));
		Assert.True(MuiApplicationSettingsPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, openConfig,
			MuiApplicationSettingsPacketKind.OpenConfigWindow,
			MuiApplicationSettingsPacketField.ClassId, 0x1400u));

		Assert.True(MuiApplicationSettingsPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, buildPanel,
			MuiApplicationSettingsPacketKind.BuildSettingsPanel,
			MuiApplicationSettingsPacketField.Number, 4u));
		Assert.True(MuiApplicationSettingsPacketFieldCursorCodec.TryReadUInt32(
			ref platform, buildPanel,
			MuiApplicationSettingsPacketKind.BuildSettingsPanel,
			MuiApplicationSettingsPacketField.Number, out var number));
		Assert.Equal(4u, number);

		Assert.True(MuiApplicationSettingsPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, settingsIo,
			MuiApplicationSettingsPacketKind.SettingsIo,
			MuiApplicationSettingsPacketField.Name, 0x1500u));
		Assert.True(MuiApplicationSettingsPacketFieldCursorCodec.TryReadUInt32(
			ref platform, settingsIo,
			MuiApplicationSettingsPacketKind.SettingsIo,
			MuiApplicationSettingsPacketField.Name, out var name));
		Assert.Equal(0x1500u, name);

		Assert.False(MuiApplicationSettingsPacketFieldCursorCodec.TryReadUInt32(
			ref platform, settingsIo,
			MuiApplicationSettingsPacketKind.SettingsIo,
			MuiApplicationSettingsPacketField.ClassId, out _));
		Assert.False(MuiApplicationSettingsPacketFieldCursorCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0xFFFFFFF0u),
			MuiApplicationSettingsPacketKind.SetConfigItem,
			MuiApplicationSettingsPacketField.Data, out _));
	}

	[Fact]
	public void ApplicationSettingsRecordsUseNamedCodecs()
	{
		var platform = CreatePlatform(out _);
		var headerAddress = APTR.FromPointer(0x12C0);
		var recordAddress = APTR.FromPointer(0x1300);
		var expectedHeader = new MuiApplicationSettingsHeader
		{
			MagicValue = 0x4D554953,
			VersionValue = 2,
			RecordCount = 3,
			PayloadBytes = 64,
		};
		var expectedRecord = new MuiApplicationSettingsRecord
		{
			Key = 0x1020,
			Length = 12,
		};

		Assert.True(MuiApplicationSettingsHeaderCodec.Write(ref platform,
			headerAddress, expectedHeader));
		Assert.True(MuiApplicationSettingsHeaderCodec.TryRead(ref platform,
			headerAddress, out var actualHeader));
		Assert.Equal(expectedHeader.MagicValue, actualHeader.MagicValue);
		Assert.Equal(expectedHeader.VersionValue, actualHeader.VersionValue);
		Assert.Equal(expectedHeader.RecordCount, actualHeader.RecordCount);
		Assert.Equal(expectedHeader.PayloadBytes, actualHeader.PayloadBytes);

		Assert.True(MuiApplicationSettingsRecordCodec.Write(ref platform,
			recordAddress, expectedRecord));
		Assert.True(MuiApplicationSettingsRecordCodec.TryRead(ref platform,
			recordAddress, out var actualRecord));
		Assert.Equal(expectedRecord.Key, actualRecord.Key);
		Assert.Equal(expectedRecord.Length, actualRecord.Length);
		Assert.False(MuiApplicationSettingsRecordCodec.TryRead(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void ApplicationSettingsHeaderFieldCursorUsesNamedBoundary()
	{
		var platform = CreatePlatform(out _);
		var header = APTR.FromPointer(0x12C0);
		var cursor = default(MuiApplicationSettingsHeaderFieldCursor);
		cursor.Header = header;
		cursor.Field = MuiApplicationSettingsHeaderField.MagicValue;
		Assert.True(MuiApplicationSettingsHeaderFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var address));
		Assert.Equal(header.Raw, address.Raw);
		cursor.Field = MuiApplicationSettingsHeaderField.VersionValue;
		Assert.True(MuiApplicationSettingsHeaderFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(header.Raw + 4, address.Raw);
		cursor.Field = MuiApplicationSettingsHeaderField.RecordCount;
		Assert.True(MuiApplicationSettingsHeaderFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(header.Raw + 8, address.Raw);
		cursor.Field = MuiApplicationSettingsHeaderField.PayloadBytes;
		Assert.True(MuiApplicationSettingsHeaderFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(header.Raw + 12, address.Raw);
		Assert.True(MuiApplicationSettingsHeaderFieldCursorCodec.TryWrite(ref
			platform, header, MuiApplicationSettingsHeaderField.MagicValue,
			0x4D554953));
		Assert.True(MuiApplicationSettingsHeaderFieldCursorCodec.TryWrite(ref
			platform, header, MuiApplicationSettingsHeaderField.PayloadBytes, 64));
		Assert.True(MuiApplicationSettingsHeaderFieldCursorCodec.TryRead(ref
			platform, header, MuiApplicationSettingsHeaderField.MagicValue,
			out var magic));
		Assert.Equal(0x4D554953u, magic);
		Assert.True(MuiApplicationSettingsHeaderFieldCursorCodec.TryRead(ref
			platform, header, MuiApplicationSettingsHeaderField.PayloadBytes,
			out var payloadBytes));
		Assert.Equal(64u, payloadBytes);
		cursor.Header = APTR.FromPointer(0xfffffffcu);
		Assert.False(MuiApplicationSettingsHeaderFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ApplicationSettingsRecordFieldCursorUsesNamedBoundary()
	{
		var platform = CreatePlatform(out _);
		var record = APTR.FromPointer(0x1300);
		var cursor = default(MuiApplicationSettingsRecordFieldCursor);
		cursor.Record = record;
		cursor.Field = MuiApplicationSettingsRecordField.Key;
		Assert.True(MuiApplicationSettingsRecordFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var address));
		Assert.Equal(record.Raw, address.Raw);
		cursor.Field = MuiApplicationSettingsRecordField.Length;
		Assert.True(MuiApplicationSettingsRecordFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(record.Raw + 4, address.Raw);
		Assert.True(MuiApplicationSettingsRecordFieldCursorCodec.TryWrite(ref
			platform, record, MuiApplicationSettingsRecordField.Key, 0x1020));
		Assert.True(MuiApplicationSettingsRecordFieldCursorCodec.TryWrite(ref
			platform, record, MuiApplicationSettingsRecordField.Length, 12));
		Assert.True(MuiApplicationSettingsRecordFieldCursorCodec.TryRead(ref
			platform, record, MuiApplicationSettingsRecordField.Key, out var key));
		Assert.Equal(0x1020u, key);
		Assert.True(MuiApplicationSettingsRecordFieldCursorCodec.TryRead(ref
			platform, record, MuiApplicationSettingsRecordField.Length,
			out var length));
		Assert.Equal(12u, length);
		cursor.Record = APTR.FromPointer(0xfffffffcu);
		Assert.False(MuiApplicationSettingsRecordFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ApplicationSettingsTransferCursorUsesNamedChunkBoundary()
	{
		var platform = CreatePlatform(out _);
		var cursor = new MuiApplicationSettingsTransferCursor
		{
			Base = APTR.FromPointer(0x1800),
			Offset = 4,
		};

		Assert.True(MuiApplicationSettingsTransferCursorCodec.TryGetAddress(
			ref platform, cursor, 4, out var address));
		Assert.Equal(APTR.FromPointer(0x1804), address);
		cursor.Base = APTR.FromPointer(0x20FFC);
		cursor.Offset = 0;
		Assert.True(MuiApplicationSettingsTransferCursorCodec.TryGetAddress(
			ref platform, cursor, 4, out address));
		Assert.Equal(APTR.FromPointer(0x20FFC), address);
		Assert.False(MuiApplicationSettingsTransferCursorCodec.TryGetAddress(
			ref platform, cursor, 5, out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFF0);
		Assert.False(MuiApplicationSettingsTransferCursorCodec.TryGetAddress(
			ref platform, cursor, 4, out _));
		Assert.False(MuiApplicationSettingsTransferCursorCodec.TryGetAddress(
			ref platform, default, 1, out _));
	}

	[Fact]
	public void ApplicationMethodReadersUseNamedMethodHeader()
	{
		const uint configMethod = 0x8042D934u;
		const uint refreshMethod = 0x80424D68u;
		const uint loopMethod = 0x804253F3u;
		const uint windowMethod = 0x8042C34Cu;
		const uint snapshotMethod = 0x8042945Eu;
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, configMethod);
		platform.WriteUInt32(packet, 4, 0x44);
		var configRequest = new MuiApplicationMethodPacketCodec.MethodPacketAddress
		{
			Address = packet,
			Method = configMethod,
		};
		Assert.True(MuiApplicationMethodPacketCodec.TryReadConfigId(ref platform,
			ref configRequest, out var config));
		Assert.Equal(configMethod, config.MethodId);
		platform.WriteUInt32(packet, 0, refreshMethod);
		var refreshRequest = new MuiApplicationMethodPacketCodec.MethodPacketAddress
		{
			Address = packet,
			Method = refreshMethod,
		};
		Assert.True(MuiApplicationMethodPacketCodec.TryReadCheckRefresh(
			ref platform, ref refreshRequest, out var refresh));
		Assert.Equal(refreshMethod, refresh.MethodId);
		platform.WriteUInt32(packet, 0, loopMethod);
		var loopRequest = new MuiApplicationMethodPacketCodec.MethodPacketAddress
		{
			Address = packet,
			Method = loopMethod,
		};
		Assert.True(MuiApplicationMethodPacketCodec.TryReadLoop(ref platform,
			ref loopRequest, out var loop));
		Assert.Equal(loopMethod, loop.MethodId);
		platform.WriteUInt32(packet, 0, windowMethod);
		var windowRequest = new MuiApplicationMethodPacketCodec.MethodPacketAddress
		{
			Address = packet,
			Method = windowMethod,
		};
		Assert.True(MuiApplicationMethodPacketCodec.TryReadWindowMethod(
			ref platform, ref windowRequest, out var window));
		Assert.Equal(windowMethod, window.MethodId);
		platform.WriteUInt32(packet, 0, snapshotMethod);
		platform.WriteUInt32(packet, 4, 1);
		var snapshotRequest = new MuiApplicationMethodPacketCodec.MethodPacketAddress
		{
			Address = packet,
			Method = snapshotMethod,
		};
		Assert.True(MuiApplicationMethodPacketCodec.TryReadSnapshot(ref platform,
			ref snapshotRequest, out var snapshot));
		Assert.Equal(snapshotMethod, snapshot.MethodId);
		Assert.Equal(1u, snapshot.Flags);
		platform.WriteUInt32(packet, 0, 0xDEADBEEFu);
		Assert.False(MuiApplicationMethodPacketCodec.TryReadSnapshot(ref platform,
			ref snapshotRequest, out _));
	}

	[Fact]
	public void ApplicationMethodPacketFieldCursorUsesNamedMixedBoundaries()
	{
		var platform = CreatePlatform(out _);
		var config = APTR.FromPointer(0x1200);
		var snapshot = APTR.FromPointer(0x1240);
		var checkRefresh = APTR.FromPointer(0x1280);

		Assert.True(MuiApplicationMethodPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, config, MuiApplicationMethodPacketKind.ConfigId,
			MuiApplicationMethodPacketField.MethodId, 0x8042D934u));
		Assert.True(MuiApplicationMethodPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, config, MuiApplicationMethodPacketKind.ConfigId,
			MuiApplicationMethodPacketField.ConfigId, 0x44u));
		Assert.True(MuiApplicationMethodPacketFieldCursorCodec.TryReadUInt32(
			ref platform, config, MuiApplicationMethodPacketKind.ConfigId,
			MuiApplicationMethodPacketField.ConfigId, out var configId));
		Assert.Equal(0x44u, configId);

		Assert.True(MuiApplicationMethodPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, snapshot, MuiApplicationMethodPacketKind.Snapshot,
			MuiApplicationMethodPacketField.MethodId, 0x8042945Eu));
		Assert.True(MuiApplicationMethodPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, snapshot, MuiApplicationMethodPacketKind.Snapshot,
			MuiApplicationMethodPacketField.Flags, 1u));
		Assert.True(MuiApplicationMethodPacketFieldCursorCodec.TryReadUInt32(
			ref platform, snapshot, MuiApplicationMethodPacketKind.Snapshot,
			MuiApplicationMethodPacketField.Flags, out var flags));
		Assert.Equal(1u, flags);

		Assert.True(MuiApplicationMethodPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, checkRefresh,
			MuiApplicationMethodPacketKind.CheckRefresh,
			MuiApplicationMethodPacketField.MethodId, 0x80424D68u));
		Assert.True(MuiApplicationMethodPacketFieldCursorCodec.TryReadUInt32(
			ref platform, checkRefresh,
			MuiApplicationMethodPacketKind.CheckRefresh,
			MuiApplicationMethodPacketField.MethodId, out var methodId));
		Assert.Equal(0x80424D68u, methodId);

		Assert.False(MuiApplicationMethodPacketFieldCursorCodec.TryReadUInt32(
			ref platform, checkRefresh,
			MuiApplicationMethodPacketKind.CheckRefresh,
			MuiApplicationMethodPacketField.Flags, out _));
		Assert.False(MuiApplicationMethodPacketFieldCursorCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0xFFFFFFF0u),
			MuiApplicationMethodPacketKind.Snapshot,
			MuiApplicationMethodPacketField.Flags, out _));
	}

	[Fact]
	public void ApplicationMenuPacketFieldCursorUsesNamedMixedBoundaries()
	{
		var platform = CreatePlatform(out _);
		var applicationQuery = APTR.FromPointer(0x1200);
		var applicationSet = APTR.FromPointer(0x1240);
		var windowQuery = APTR.FromPointer(0x1280);
		var windowSet = APTR.FromPointer(0x12C0);

		Assert.True(MuiApplicationMenuPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, applicationQuery,
			MuiApplicationMenuPacketKind.ApplicationQuery,
			MuiApplicationMenuPacketField.MenuId, 7u));
		Assert.True(MuiApplicationMenuPacketFieldCursorCodec.TryReadUInt32(
			ref platform, applicationQuery,
			MuiApplicationMenuPacketKind.ApplicationQuery,
			MuiApplicationMenuPacketField.MenuId, out var applicationMenu));
		Assert.Equal(7u, applicationMenu);

		Assert.True(MuiApplicationMenuPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, applicationSet,
			MuiApplicationMenuPacketKind.ApplicationSet,
			MuiApplicationMenuPacketField.MenuId, 8u));
		Assert.True(MuiApplicationMenuPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, applicationSet,
			MuiApplicationMenuPacketKind.ApplicationSet,
			MuiApplicationMenuPacketField.State, 1u));

		Assert.True(MuiApplicationMenuPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, windowQuery,
			MuiApplicationMenuPacketKind.WindowQuery,
			MuiApplicationMenuPacketField.MenuId, 9u));
		Assert.True(MuiApplicationMenuPacketFieldCursorCodec.TryReadUInt32(
			ref platform, windowQuery,
			MuiApplicationMenuPacketKind.WindowQuery,
			MuiApplicationMenuPacketField.MenuId, out var windowMenu));
		Assert.Equal(9u, windowMenu);

		Assert.True(MuiApplicationMenuPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, windowSet, MuiApplicationMenuPacketKind.WindowSet,
			MuiApplicationMenuPacketField.MenuId, 10u));
		Assert.True(MuiApplicationMenuPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, windowSet, MuiApplicationMenuPacketKind.WindowSet,
			MuiApplicationMenuPacketField.State, 0u));
		Assert.True(MuiApplicationMenuPacketFieldCursorCodec.TryReadUInt32(
			ref platform, windowSet, MuiApplicationMenuPacketKind.WindowSet,
			MuiApplicationMenuPacketField.State, out var state));
		Assert.Equal(0u, state);

		Assert.False(MuiApplicationMenuPacketFieldCursorCodec.TryReadUInt32(
			ref platform, applicationQuery,
			MuiApplicationMenuPacketKind.ApplicationQuery,
			MuiApplicationMenuPacketField.State, out _));
		Assert.False(MuiApplicationMenuPacketFieldCursorCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0xFFFFFFF0u),
			MuiApplicationMenuPacketKind.WindowSet,
			MuiApplicationMenuPacketField.State, out _));
	}

	[Fact]
	public void WindowEventHandlerPacketFieldCursorUsesNamedBoundary()
	{
		var platform = CreatePlatform(out _);
		var add = APTR.FromPointer(0x1200);
		var remove = APTR.FromPointer(0x1240);

		Assert.True(MuiWindowEventHandlerPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, add, MuiWindowEventHandlerPacketKind.Add,
			MuiWindowEventHandlerPacketField.MethodId,
			MuiApplicationDispatcher.WindowAddEventHandlerMethod));
		Assert.True(MuiWindowEventHandlerPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, add, MuiWindowEventHandlerPacketKind.Add,
			MuiWindowEventHandlerPacketField.Handler, 0x1500u));
		Assert.True(MuiWindowEventHandlerPacketFieldCursorCodec.TryReadUInt32(
			ref platform, add, MuiWindowEventHandlerPacketKind.Add,
			MuiWindowEventHandlerPacketField.Handler, out var addHandler));
		Assert.Equal(0x1500u, addHandler);

		Assert.True(MuiWindowEventHandlerPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, remove, MuiWindowEventHandlerPacketKind.Remove,
			MuiWindowEventHandlerPacketField.MethodId,
			MuiApplicationDispatcher.WindowRemoveEventHandlerMethod));
		Assert.True(MuiWindowEventHandlerPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, remove, MuiWindowEventHandlerPacketKind.Remove,
			MuiWindowEventHandlerPacketField.Handler, 0x1600u));
		Assert.True(MuiWindowEventHandlerPacketFieldCursorCodec.TryReadUInt32(
			ref platform, remove, MuiWindowEventHandlerPacketKind.Remove,
			MuiWindowEventHandlerPacketField.Handler, out var removeHandler));
		Assert.Equal(0x1600u, removeHandler);

		Assert.False(MuiWindowEventHandlerPacketFieldCursorCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0xFFFFFFF0u),
			MuiWindowEventHandlerPacketKind.Add,
			MuiWindowEventHandlerPacketField.Handler, out _));
	}

	[Fact]
	public void WindowCycleChainReaderUsesNamedMethodHeader()
	{
		const uint method = 0x80426510u;
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, method);
		platform.WriteUInt32(packet, 4, 0x1300);
		var request = new MuiWindowCycleChainPacketCodec.CycleChainPacketAddress
		{
			Address = packet,
			Method = method,
		};
		Assert.True(MuiWindowCycleChainPacketCodec.TryRead(ref platform,
			ref request, out var value));
		Assert.Equal(method, value.MethodId);
		Assert.Equal(0x1300u, value.FirstObject);
		platform.WriteUInt32(packet, 0, 0xDEADBEEFu);
		Assert.False(MuiWindowCycleChainPacketCodec.TryRead(ref platform,
			ref request, out _));
	}

	[Fact]
	public void WindowCycleChainPacketFieldCursorUsesNamedHeaderBoundary()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiWindowCycleChainPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiWindowCycleChainPacketField.MethodId,
			0x80426510u));
		Assert.True(MuiWindowCycleChainPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiWindowCycleChainPacketField.FirstObject,
			0x1300u));
		Assert.True(MuiWindowCycleChainPacketFieldCursorCodec.TryReadUInt32(
			ref platform, packet, MuiWindowCycleChainPacketField.FirstObject,
			out var firstObject));
		Assert.Equal(0x1300u, firstObject);
		Assert.False(MuiWindowCycleChainPacketFieldCursorCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0xFFFFFFF0u),
			MuiWindowCycleChainPacketField.FirstObject, out _));
	}

	[Fact]
	public void WindowCycleChainVectorUsesNamedTailBoundary()
	{
		var platform = CreatePlatform(out _);
		var message = APTR.FromPointer(0x1800);

		Assert.True(MuiWindowCycleChainPacketCodec.TryGetVector(ref platform,
			message, out var vector));
		Assert.Equal(APTR.FromPointer(0x1804), vector);
		Assert.True(MuiWindowCycleChainPacketCodec.TryGetVector(ref platform,
			APTR.FromPointer(0x20FF8), out vector));
		Assert.Equal(APTR.FromPointer(0x20FFC), vector);
		Assert.False(MuiWindowCycleChainPacketCodec.TryGetVector(ref platform,
			APTR.FromPointer(0x20FFC), out _));
		Assert.False(MuiWindowCycleChainPacketCodec.TryGetVector(ref platform,
			APTR.FromPointer(0xFFFFFFF0), out _));
	}

	[Fact]
	public void WindowCycleChainInlineCursorUsesNamedPacketBoundary()
	{
		var platform = CreatePlatform(out _);
		var cursor = default(MuiWindowCycleChainInlineVectorCursor);
		cursor.Message = APTR.FromPointer(0x1800);
		cursor.Index = 2;
		Assert.True(MuiWindowCycleChainInlineVectorCodec.TryGetEntry(
			ref platform, cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x180C), address);
		cursor.Message = APTR.FromPointer(0xFFFFFFF0);
		Assert.False(MuiWindowCycleChainInlineVectorCodec.TryGetEntry(
			ref platform, cursor, out _));
	}

	[Fact]
	public void WindowCycleChainCursorUsesNamedEntryBoundary()
	{
		var platform = CreatePlatform(out _);
		var cursor = new MuiApplicationWindowCycleChainCursor
		{
			Base = APTR.FromPointer(0x1800),
			Index = 1,
		};

		Assert.True(MuiApplicationWindowCycleChainVectorCodec.TryGetEntry(
			ref platform, cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x1804), address);
		cursor.Base = APTR.FromPointer(0x20FF8);
		cursor.Index = 1;
		Assert.True(MuiApplicationWindowCycleChainVectorCodec.TryGetEntry(
			ref platform, cursor, out address));
		Assert.Equal(APTR.FromPointer(0x20FFC), address);
		cursor.Index = 2;
		Assert.False(MuiApplicationWindowCycleChainVectorCodec.TryGetEntry(
			ref platform, cursor, out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFF0);
		cursor.Index = 0;
		Assert.False(MuiApplicationWindowCycleChainVectorCodec.TryGetEntry(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ApplicationMethodHeaderCodecUsesNamedField()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1500);
		var expected = new MuiApplicationMethodHeaderMessage
		{
			MethodId = MuiApplicationDispatcher.ApplicationInputMethod,
		};
		Assert.True(MuiApplicationMethodHeaderCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiApplicationMethodHeaderCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.MethodId, actual.MethodId);
		Assert.False(MuiApplicationMethodHeaderCodec.TryRead(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void WindowCycleChainSlotUsesNamedObjectPointer()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1540);
		var expected = new MuiApplicationWindowCycleChainSlot
		{
			Object = APTR.FromPointer(0x1600),
		};

		Assert.True(MuiApplicationWindowCycleChainSlotCodec.Write(ref platform,
			address, expected));
		Assert.True(MuiApplicationWindowCycleChainSlotCodec.TryRead(ref platform,
			address, out var actual));
		Assert.Equal(expected.Object, actual.Object);
		Assert.False(MuiApplicationWindowCycleChainSlotCodec.TryRead(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void WindowSignalStorageUsesNamedValue()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1580);
		var expected = new MuiApplicationWindowSignalStorage
		{
			Signals = 0x20,
		};

		Assert.True(MuiApplicationWindowSignalStorageCodec.Write(ref platform,
			address, expected));
		Assert.True(MuiApplicationWindowSignalStorageCodec.TryRead(ref platform,
			address, out var actual));
		Assert.Equal(expected.Signals, actual.Signals);
		Assert.False(MuiApplicationWindowSignalStorageCodec.TryRead(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void WindowNodePayloadAddressUsesNamedRecordBoundary()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1600);

		Assert.True(MuiApplicationWindowNodeCodec.TryGetPayload(ref platform,
			address, MuiApplicationMethodHeaderMessage.Size, out var payload));
		Assert.Equal(APTR.FromPointer(0x1610), payload);
		Assert.False(MuiApplicationWindowNodeCodec.TryGetPayload(ref platform,
			APTR.Null, MuiApplicationMethodHeaderMessage.Size, out _));
		Assert.False(MuiApplicationWindowNodeCodec.TryGetPayload(ref platform,
			APTR.FromPointer(0x20FFC), 8, out _));
	}

	[Fact]
	public void WindowNodePayloadCursorUsesNamedRecordBoundary()
	{
		var platform = CreatePlatform(out _);
		var cursor = default(MuiApplicationWindowNodePayloadCursor);
		cursor.Node = APTR.FromPointer(0x1600);
		cursor.ByteCount = 8;
		Assert.True(MuiApplicationWindowNodePayloadCursorCodec.TryGetAddress(
			ref platform, cursor, out var payload));
		Assert.Equal(APTR.FromPointer(0x1610), payload);
		cursor.Node = APTR.FromPointer(0xFFFFFFF0);
		Assert.False(MuiApplicationWindowNodePayloadCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void WindowNodeFieldCursorUsesNamedRecordBoundary()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1800);
		Assert.True(MuiApplicationWindowNodeFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationWindowNodeField.Next, 0x1900u));
		Assert.True(MuiApplicationWindowNodeFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationWindowNodeField.Value, 0x1A00u));
		Assert.True(MuiApplicationWindowNodeFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationWindowNodeField.Sequence, 2u));
		Assert.True(MuiApplicationWindowNodeFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationWindowNodeField.Auxiliary, 3u));
		Assert.True(MuiApplicationWindowNodeFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationWindowNodeField.Packet, 0x1B00u));
		Assert.True(MuiApplicationWindowNodeFieldCursorCodec.TryReadUInt32(
			ref platform, address, MuiApplicationWindowNodeField.Packet,
			out var packet));
		Assert.Equal(0x1B00u, packet);
		Assert.False(MuiApplicationWindowNodeFieldCursorCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0xFFFFFFF0u),
			MuiApplicationWindowNodeField.Packet, out _));
	}

	[Fact]
	public void EventHandlerNodeFieldCursorUsesNamedMixedRecordBoundary()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1C00);
		Assert.True(MuiEventHandlerNodeFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiEventHandlerNodeField.NodeSuccessor, 0x1D00u));
		Assert.True(MuiEventHandlerNodeFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiEventHandlerNodeField.NodePredecessor, 0x1E00u));
		Assert.True(MuiEventHandlerNodeFieldCursorCodec.TryWriteUInt8(ref platform,
			address, MuiEventHandlerNodeField.Reserved, 0xA5));
		Assert.True(MuiEventHandlerNodeFieldCursorCodec.TryWriteUInt8(ref platform,
			address, MuiEventHandlerNodeField.Priority, 0xFE));
		Assert.True(MuiEventHandlerNodeFieldCursorCodec.TryWriteUInt16(ref platform,
			address, MuiEventHandlerNodeField.Flags, 0xC123));
		Assert.True(MuiEventHandlerNodeFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiEventHandlerNodeField.Object, 0x1F00u));
		Assert.True(MuiEventHandlerNodeFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiEventHandlerNodeField.Class, 0x2000u));
		Assert.True(MuiEventHandlerNodeFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiEventHandlerNodeField.Events, 0x01020304u));

		Assert.True(MuiEventHandlerNodeFieldCursorCodec.TryReadUInt8(ref platform,
			address, MuiEventHandlerNodeField.Priority, out var priority));
		Assert.Equal(0xFE, priority);
		Assert.True(MuiEventHandlerNodeFieldCursorCodec.TryReadUInt16(ref platform,
			address, MuiEventHandlerNodeField.Flags, out var flags));
		Assert.Equal((ushort)0xC123, flags);
		Assert.True(MuiEventHandlerNodeFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiEventHandlerNodeField.Events, out var events));
		Assert.Equal(0x01020304u, events);
		Assert.False(MuiEventHandlerNodeFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiEventHandlerNodeField.Flags, out _));
		Assert.False(MuiEventHandlerNodeFieldCursorCodec.TryReadUInt8(ref platform,
			APTR.FromPointer(0xFFFFFFF0u), MuiEventHandlerNodeField.Priority,
			out _));
	}

	[Fact]
	public void InputHandlerFieldCursorUsesNamedRecordBoundary()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1D00);
		Assert.True(MuiInputHandlerFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiInputHandlerField.NodeSuccessor, 0x1E00u));
		Assert.True(MuiInputHandlerFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiInputHandlerField.NodePredecessor, 0x1F00u));
		Assert.True(MuiInputHandlerFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiInputHandlerField.Object, 0x2000u));
		Assert.True(MuiInputHandlerFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiInputHandlerField.Events, 0x01020304u));
		Assert.True(MuiInputHandlerFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiInputHandlerField.Reserved, 0x05060708u));
		Assert.True(MuiInputHandlerFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiInputHandlerField.Packet, 0x2100u));

		Assert.True(MuiInputHandlerFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiInputHandlerField.Object, out var @object));
		Assert.Equal(0x2000u, @object);
		Assert.True(MuiInputHandlerFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiInputHandlerField.Packet, out var packet));
		Assert.Equal(0x2100u, packet);
		Assert.False(MuiInputHandlerFieldCursorCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0xFFFFFFF0u), MuiInputHandlerField.Packet, out _));
	}

	[Fact]
	public void ApplicationSetConfigItemFieldCursorUsesNamedRecordBoundary()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1E00);
		Assert.True(MuiApplicationSetConfigItemStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiApplicationSetConfigItemStateField.Magic,
			0x41534349u));
		Assert.True(MuiApplicationSetConfigItemStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiApplicationSetConfigItemStateField.Item, 0x22u));
		Assert.True(MuiApplicationSetConfigItemStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiApplicationSetConfigItemStateField.Data, 0x1300u));
		Assert.True(MuiApplicationSetConfigItemStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiApplicationSetConfigItemStateField.Requests, 3u));
		Assert.True(MuiApplicationSetConfigItemStateFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiApplicationSetConfigItemStateField.Data,
			out var data));
		Assert.Equal(0x1300u, data);
		Assert.True(MuiApplicationSetConfigItemStateFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiApplicationSetConfigItemStateField.Requests,
			out var requests));
		Assert.Equal(3u, requests);
		Assert.True(MuiApplicationSetConfigItemStateFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiApplicationSetConfigItemStateField.Magic,
			out var cookie));
		Assert.Equal(0x41534349u, cookie);
		Assert.False(MuiApplicationSetConfigItemStateFieldCursorCodec.TryReadUInt32(ref platform, address,
			unchecked((MuiApplicationSetConfigItemStateField)255),
			out _));
		Assert.False(MuiApplicationSetConfigItemStateFieldCursorCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0xFFFFFFF0u),
			MuiApplicationSetConfigItemStateField.Data,
			out _));
		var expected = default(MuiApplicationSetConfigItemStateRecord);
		expected.Magic = MuiApplicationSetConfigItemStateRecord.Cookie;
		expected.Item = 0x22;
		expected.Data = APTR.FromPointer(0x1300);
		expected.Requests = 3;
		Assert.True(MuiApplicationSetConfigItemStateRecordCodec.Write(ref platform,
			address, expected));
		Assert.True(MuiApplicationSetConfigItemStateRecordCodec.TryRead(
			ref platform, address, out var decoded));
		Assert.Equal(expected.Magic, decoded.Magic);
		Assert.Equal(expected.Item, decoded.Item);
		Assert.Equal(expected.Data, decoded.Data);
		Assert.Equal(expected.Requests, decoded.Requests);
	}

	[Fact]
	public void SetConfigItemRecordUsesNamedOpaqueApointer()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x3A00);
		Assert.True(MuiApplicationWindowCore.WriteSetConfigItemRecord(
			ref platform, address, 0x44, 0x3A80, 5));
		Assert.True(MuiApplicationSetConfigItemStateRecordCodec.TryRead(
			ref platform, address, out var value));
		Assert.Equal(MuiApplicationSetConfigItemStateRecord.Cookie, value.Magic);
		Assert.Equal(0x44u, value.Item);
		Assert.Equal(APTR.FromPointer(0x3A80), value.Data);
		Assert.Equal(5u, value.Requests);
	}

	[Fact]
	public void ApplicationLifecycleStateCodecUsesNamedFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1F00);
		var value = default(MuiApplicationLifecycleStateRecord);
		value.Magic = MuiApplicationLifecycleStateRecord.Cookie;
		value.Initialized = 1;
		value.Iconified = 1;
		value.Active = 1;
		value.SingleTask = 1;
		value.DoubleStart = 2;
		value.ForceQuit = 3;
		Assert.True(MuiApplicationLifecycleStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationLifecycleStateRecordCodec.TryRead(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Initialized, decoded.Initialized);
		Assert.Equal(value.Iconified, decoded.Iconified);
		Assert.Equal(value.Active, decoded.Active);
		Assert.Equal(value.SingleTask, decoded.SingleTask);
		Assert.Equal(value.DoubleStart, decoded.DoubleStart);
		Assert.Equal(value.ForceQuit, decoded.ForceQuit);
		var cursor = default(MuiApplicationLifecycleStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiApplicationLifecycleStateField.Active;
		Assert.True(MuiApplicationLifecycleStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 12, fieldAddress.Raw);
		cursor.Field = (MuiApplicationLifecycleStateField)255;
		Assert.False(MuiApplicationLifecycleStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ApplicationLifecyclePublishesNamedGuestRecord()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0x20));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationLifecycleState(
			ref platform, State, application, out var lifecycle));
		Assert.Equal(MuiApplicationLifecycleStateRecord.Cookie, lifecycle.Magic);
		Assert.Equal(1u, lifecycle.Initialized);
		Assert.Equal(0u, lifecycle.Iconified);
		Assert.Equal(0u, lifecycle.Active);
		Assert.Equal(0u, lifecycle.SingleTask);
		Assert.Equal(0u, lifecycle.DoubleStart);
		Assert.Equal(0u, lifecycle.ForceQuit);

		Assert.True(MuiApplicationWindowCore.SetApplicationActiveValue(
			ref platform, State, application, 0x12345678));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationLifecycleState(
			ref platform, State, application, out lifecycle));
		Assert.Equal(1u, lifecycle.Active);

		Assert.True(MuiApplicationWindowCore.SetIconified(ref platform, State,
			application, true));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationLifecycleState(
			ref platform, State, application, out lifecycle));
		Assert.Equal(1u, lifecycle.Iconified);
		Assert.True(MuiApplicationWindowCore.SetIconified(ref platform, State,
			application, false));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationLifecycleState(
			ref platform, State, application, out lifecycle));
		Assert.Equal(0u, lifecycle.Iconified);
	}

	[Fact]
	public void ApplicationLifecycleGettersProjectNamedState()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiApplicationWindowCore.SetApplicationActiveValue(
			ref platform, State, application, 0x1234));
		Assert.True(MuiApplicationWindowCore.SetIconified(ref platform, State,
			application, true));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x804260AB, out var active));
		Assert.Equal(1u, active);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x8042A07F, out var iconified));
		Assert.Equal(1u, iconified);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x7FFE0044, out var initialized));
		Assert.Equal(1u, initialized);
		Assert.True(MuiApplicationWindowCore.TryGetApplicationLifecycleState(
			ref platform, State, application, out var lifecycle));
		Assert.Equal(1u, lifecycle.Initialized);
		Assert.Equal(1u, lifecycle.Active);
		Assert.Equal(1u, lifecycle.Iconified);
	}

	[Fact]
	public void WindowLifecycleStateCodecUsesNamedFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1FC0);
		var value = default(MuiWindowLifecycleStateRecord);
		value.Magic = MuiWindowLifecycleStateRecord.Cookie;
		value.NativeWindow = APTR.FromPointer(0x2200);
		value.Open = 1;
		value.EventMask = 0x1234;
		value.IconifiedOpen = 1;
		Assert.True(MuiWindowLifecycleStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiWindowLifecycleStateRecordCodec.TryRead(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.NativeWindow, decoded.NativeWindow);
		Assert.Equal(value.Open, decoded.Open);
		Assert.Equal(value.EventMask, decoded.EventMask);
		Assert.Equal(value.IconifiedOpen, decoded.IconifiedOpen);
		var cursor = default(MuiWindowLifecycleStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiWindowLifecycleStateField.EventMask;
		Assert.True(MuiWindowLifecycleStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 12, fieldAddress.Raw);
		cursor.Field = (MuiWindowLifecycleStateField)255;
		Assert.False(MuiWindowLifecycleStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void WindowLifecyclePublishesNamedGuestRecordAcrossOpenAndClose()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0x1200));
		Assert.True(MuiApplicationWindowCore.TryGetWindowLifecycleState(
			ref platform, State, window, out var lifecycle));
		Assert.Equal(MuiWindowLifecycleStateRecord.Cookie, lifecycle.Magic);
		Assert.True(lifecycle.NativeWindow.IsNotNull);
		Assert.Equal(1u, lifecycle.Open);
		Assert.Equal(0x1200u, lifecycle.EventMask);
		Assert.Equal(0u, lifecycle.IconifiedOpen);

		Assert.True(MuiApplicationWindowCore.RequestIDCMP(ref platform, State,
			window, 0x0004));
		Assert.True(MuiApplicationWindowCore.TryGetWindowLifecycleState(
			ref platform, State, window, out lifecycle));
		Assert.Equal(0x1204u, lifecycle.EventMask);

		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
		Assert.True(MuiApplicationWindowCore.TryGetWindowLifecycleState(
			ref platform, State, window, out lifecycle));
		Assert.True(lifecycle.NativeWindow.IsNull);
		Assert.Equal(0u, lifecycle.Open);
		Assert.Equal(0x1204u, lifecycle.EventMask);
		Assert.Equal(0u, lifecycle.IconifiedOpen);
	}

	[Fact]
	public void WindowOpenPolicyCodecUsesNamedSignedAndBooleanFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x2040);
		var value = default(MuiWindowOpenPolicyStateRecord);
		value.Magic = MuiWindowOpenPolicyStateRecord.Cookie;
		value.AlternateHeight = -1;
		value.AlternateWidth = 640;
		value.AlternateLeftEdge = -8;
		value.AlternateTopEdge = 12;
		value.Height = 480;
		value.Width = 800;
		value.LeftEdge = 4;
		value.TopEdge = 6;
		value.CloseGadget = 1;
		value.DepthGadget = 1;
		value.DragBar = 1;
		value.SizeGadget = 1;
		value.SizeRight = 1;
		value.AppWindow = 1;
		value.Backdrop = 0;
		value.Borderless = 1;
		value.PanelWindow = 0;
		value.TabletMessages = 1;
		value.UseBottomBorderScroller = 1;
		value.UseLeftBorderScroller = 0;
		value.UseRightBorderScroller = 1;
		Assert.True(MuiWindowOpenPolicyStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiWindowOpenPolicyStateRecordCodec.TryRead(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.AlternateHeight, decoded.AlternateHeight);
		Assert.Equal(value.AlternateLeftEdge, decoded.AlternateLeftEdge);
		Assert.Equal(value.Height, decoded.Height);
		Assert.Equal(value.Width, decoded.Width);
		Assert.Equal(value.CloseGadget, decoded.CloseGadget);
		Assert.Equal(value.AppWindow, decoded.AppWindow);
		Assert.Equal(value.TabletMessages, decoded.TabletMessages);
		Assert.Equal(value.UseRightBorderScroller,
			decoded.UseRightBorderScroller);
		var cursor = default(MuiWindowOpenPolicyStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiWindowOpenPolicyStateField.UseRightBorderScroller;
		Assert.True(MuiWindowOpenPolicyStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 84, fieldAddress.Raw);
		cursor.Field = (MuiWindowOpenPolicyStateField)255;
		Assert.False(MuiWindowOpenPolicyStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void WindowOpenPublishesTypedPolicyBeforeNativeConfiguration()
	{
		var platform = CreatePlatform(out var cl);
		var tags = APTR.FromPointer(0x1700);
		platform.WriteUInt32(tags, 0, MuiWindowPublicCore.AltHeight);
		platform.WriteUInt32(tags, 4, unchecked((uint)-1));
		platform.WriteUInt32(tags, 8, MuiWindowPublicCore.AltWidth);
		platform.WriteUInt32(tags, 12, 640);
		platform.WriteUInt32(tags, 16, MuiWindowPublicCore.AltLeftEdge);
		platform.WriteUInt32(tags, 20, unchecked((uint)-8));
		platform.WriteUInt32(tags, 24, MuiWindowPublicCore.Height);
		platform.WriteUInt32(tags, 28, 480);
		platform.WriteUInt32(tags, 32, MuiWindowPublicCore.Width);
		platform.WriteUInt32(tags, 36, 800);
		platform.WriteUInt32(tags, 40, MuiWindowPublicCore.CloseGadget);
		platform.WriteUInt32(tags, 44, 1);
		platform.WriteUInt32(tags, 48, MuiWindowPublicCore.AppWindow);
		platform.WriteUInt32(tags, 52, 1);
		platform.WriteUInt32(tags, 56, MuiWindowPublicCore.TabletMessages);
		platform.WriteUInt32(tags, 60, 1);
		platform.WriteUInt32(tags, 64,
			MuiWindowPublicCore.UseRightBorderScroller);
		platform.WriteUInt32(tags, 68, 1);
		platform.WriteUInt32(tags, 72, 0);
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			tags);
		Assert.True(window.IsNotNull);

		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.True(MuiApplicationWindowCore.TryGetWindowOpenPolicyState(
			ref platform, State, window, out var policy));
		Assert.Equal(MuiWindowOpenPolicyStateRecord.Cookie, policy.Magic);
		Assert.Equal(-1, policy.AlternateHeight);
		Assert.Equal(640, policy.AlternateWidth);
		Assert.Equal(-8, policy.AlternateLeftEdge);
		Assert.Equal(480, policy.Height);
		Assert.Equal(800, policy.Width);
		Assert.Equal(1u, policy.CloseGadget);
		Assert.Equal(1u, policy.AppWindow);
		Assert.Equal(1u, policy.TabletMessages);
		Assert.Equal(1u, policy.UseRightBorderScroller);
		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
	}

	[Fact]
	public void WindowOpenPolicyGettersProjectNamedState()
	{
		var platform = CreatePlatform(out var cl);
		var tags = APTR.FromPointer(0x1800);
		platform.WriteUInt32(tags, 0, MuiWindowPublicCore.AltHeight);
		platform.WriteUInt32(tags, 4, unchecked((uint)-12));
		platform.WriteUInt32(tags, 8, MuiWindowPublicCore.Height);
		platform.WriteUInt32(tags, 12, 480);
		platform.WriteUInt32(tags, 16, MuiWindowPublicCore.CloseGadget);
		platform.WriteUInt32(tags, 20, 7);
		platform.WriteUInt32(tags, 24, MuiWindowPublicCore.AppWindow);
		platform.WriteUInt32(tags, 28, 2);
		platform.WriteUInt32(tags, 32,
			MuiWindowPublicCore.UseRightBorderScroller);
		platform.WriteUInt32(tags, 36, 4);
		platform.WriteUInt32(tags, 40, 0);
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			tags);
		Assert.True(window.IsNotNull);

		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.AltHeight, out var altHeight));
		Assert.Equal(unchecked((uint)-12), altHeight);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Height, out var height));
		Assert.Equal(480u, height);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.CloseGadget, out var closeGadget));
		Assert.Equal(1u, closeGadget);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.AppWindow, out var appWindow));
		Assert.Equal(1u, appWindow);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.UseRightBorderScroller, out var rightScroller));
		Assert.Equal(1u, rightScroller);

		Assert.True(MuiApplicationWindowCore.TryGetWindowOpenPolicyState(
			ref platform, State, window, out var policy));
		Assert.Equal(-12, policy.AlternateHeight);
		Assert.Equal(480, policy.Height);
		Assert.Equal(1u, policy.CloseGadget);
		Assert.Equal(1u, policy.AppWindow);
		Assert.Equal(1u, policy.UseRightBorderScroller);
	}

	[Fact]
	public void WindowPresentationStateCodecUsesNamedPointerFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x2140);
		var value = default(MuiWindowPresentationStateRecord);
		value.Magic = MuiWindowPresentationStateRecord.Cookie;
		value.Title = APTR.FromPointer(0x2200);
		value.Screen = APTR.FromPointer(0x2300);
		value.ScreenTitle = APTR.FromPointer(0x2400);
		value.PublicScreen = APTR.FromPointer(0x2500);
		Assert.True(MuiWindowPresentationStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiWindowPresentationStateRecordCodec.TryRead(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Title, decoded.Title);
		Assert.Equal(value.Screen, decoded.Screen);
		Assert.Equal(value.ScreenTitle, decoded.ScreenTitle);
		Assert.Equal(value.PublicScreen, decoded.PublicScreen);
		var cursor = default(MuiWindowPresentationStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiWindowPresentationStateField.PublicScreen;
		Assert.True(MuiWindowPresentationStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 16, fieldAddress.Raw);
		cursor.Field = (MuiWindowPresentationStateField)255;
		Assert.False(MuiWindowPresentationStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void WindowPresentationPublishesNamedPointersAndHidesClosedScreen()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var title = APTR.FromPointer(0x2200);
		var screen = APTR.FromPointer(0x2300);
		var screenTitle = APTR.FromPointer(0x2400);
		var publicScreen = APTR.FromPointer(0x2500);
		platform.WriteCString(title, "CopperOS");
		platform.WriteCString(screenTitle, "Workbench");
		platform.WriteCString(publicScreen, "Public");
		var objectRecord = MuiHeadlessObjectCore.FindObject(ref platform, State,
			window);
		Assert.True(MuiWindowPublicCore.TrySet(ref platform, State, objectRecord,
			MuiWindowPublicCore.Title, title.Raw, false, out var handled));
		Assert.True(handled);
		Assert.True(MuiWindowPublicCore.TrySet(ref platform, State, objectRecord,
			MuiWindowPublicCore.Screen, screen.Raw, false, out handled));
		Assert.True(handled);
		Assert.True(MuiWindowPublicCore.TrySet(ref platform, State, objectRecord,
			MuiWindowPublicCore.ScreenTitle, screenTitle.Raw, false, out handled));
		Assert.True(handled);
		Assert.True(MuiWindowPublicCore.TrySet(ref platform, State, objectRecord,
			MuiWindowPublicCore.PublicScreen, publicScreen.Raw, false,
			out handled));
		Assert.True(handled);
		Assert.True(MuiWindowPublicCore.TryGetWindowPresentationState(
			ref platform, State, window, out var presentation));
		Assert.Equal(MuiWindowPresentationStateRecord.Cookie, presentation.Magic);
		Assert.Equal(title, presentation.Title);
		Assert.Equal(screen, presentation.Screen);
		Assert.Equal(screenTitle, presentation.ScreenTitle);
		Assert.Equal(publicScreen, presentation.PublicScreen);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Title, out var titleValue));
		Assert.Equal(title.Raw, titleValue);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Screen, out var closedScreen));
		Assert.Equal(0u, closedScreen);

		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Screen, out var openScreen));
		Assert.Equal(screen.Raw, openScreen);
		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Screen, out closedScreen));
		Assert.Equal(0u, closedScreen);
		Assert.True(MuiWindowPublicCore.TryGetWindowPresentationState(
			ref platform, State, window, out presentation));
		Assert.Equal(screen, presentation.Screen);
	}

	[Fact]
	public void WindowVisualStateCodecUsesNamedPolicyFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x2600);
		var value = default(MuiWindowVisualStateRecord);
		value.Magic = MuiWindowVisualStateRecord.Cookie;
		value.NoMenus = 1;
		value.HasAlpha = 0;
		value.Opacity = 128;
		value.FancyDrawing = 1;
		value.MenuAction = 0xCAFE;
		Assert.True(MuiWindowVisualStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiWindowVisualStateRecordCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.NoMenus, decoded.NoMenus);
		Assert.Equal(value.HasAlpha, decoded.HasAlpha);
		Assert.Equal(value.Opacity, decoded.Opacity);
		Assert.Equal(value.FancyDrawing, decoded.FancyDrawing);
		Assert.Equal(value.MenuAction, decoded.MenuAction);
		var cursor = default(MuiWindowVisualStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiWindowVisualStateField.MenuAction;
		Assert.True(MuiWindowVisualStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 20, fieldAddress.Raw);
		cursor.Field = (MuiWindowVisualStateField)255;
		Assert.False(MuiWindowVisualStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void WindowVisualPolicyPublishesCanonicalValues()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x2680);
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, MuiWindowPublicCore.NoMenus,
			0x1234));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowNoMenus(
			ref platform, State, window, packet));
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, MuiWindowPublicCore.HasAlpha,
			0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowHasAlpha(
			ref platform, State, window, packet));
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, MuiWindowPublicCore.Opacity,
			128));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowOpacity(
			ref platform, State, window, packet));
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set,
			MuiWindowPublicCore.FancyDrawing, 0x55));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowFancyDrawing(
			ref platform, State, window, packet));
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, MuiWindowPublicCore.MenuAction,
			0xCAFE));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowMenuAction(
			ref platform, State, window, packet));

		Assert.True(MuiWindowPublicCore.TryGetWindowVisualState(ref platform,
			State, window, out var visual));
		Assert.Equal(MuiWindowVisualStateRecord.Cookie, visual.Magic);
		Assert.Equal(1u, visual.NoMenus);
		Assert.Equal(0u, visual.HasAlpha);
		Assert.Equal(128u, visual.Opacity);
		Assert.Equal(1u, visual.FancyDrawing);
		Assert.Equal(0xCAFEu, visual.MenuAction);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.MenuAction, out var menuAction));
		Assert.Equal(0xCAFEu, menuAction);
	}

	[Fact]
	public void SleepStateCodecUsesNamedFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x26C0);
		var value = default(MuiSleepStateRecord);
		value.Magic = MuiSleepStateRecord.Cookie;
		value.Depth = 2;
		value.SavedDisabled = 1;
		value.Request = 3;
		Assert.True(MuiSleepStateRecordCodec.Write(ref platform, address, value));
		Assert.True(MuiSleepStateRecordCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Depth, decoded.Depth);
		Assert.Equal(value.SavedDisabled, decoded.SavedDisabled);
		Assert.Equal(value.Request, decoded.Request);
		var cursor = default(MuiSleepStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiSleepStateField.Request;
		Assert.True(MuiSleepStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 12, fieldAddress.Raw);
		cursor.Field = (MuiSleepStateField)255;
		Assert.False(MuiSleepStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
	}

	[Fact]
	public void SleepTransitionsPublishNamedWindowAndApplicationState()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, window,
			0x80423661, 1, false));
		Assert.True(MuiApplicationWindowCore.SetSleepValue(ref platform, State,
			window, 1));
		Assert.True(MuiApplicationWindowCore.TryGetWindowSleepState(ref platform,
			State, window, out var windowSleep));
		Assert.Equal(MuiSleepStateRecord.Cookie, windowSleep.Magic);
		Assert.Equal(1u, windowSleep.Depth);
		Assert.Equal(1u, windowSleep.SavedDisabled);
		Assert.Equal(1u, windowSleep.Request);
		Assert.True(MuiApplicationWindowCore.SetSleepValue(ref platform, State,
			window, 0));
		Assert.True(MuiApplicationWindowCore.TryGetWindowSleepState(ref platform,
			State, window, out windowSleep));
		Assert.Equal(0u, windowSleep.Depth);
		Assert.Equal(1u, windowSleep.SavedDisabled);
		Assert.Equal(0u, windowSleep.Request);

		var application = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiApplicationWindowCore.SetApplicationSleepValue(ref platform,
			State, application, 1));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationSleepState(
			ref platform, State, application, out var applicationSleep));
		Assert.Equal(MuiSleepStateRecord.Cookie, applicationSleep.Magic);
		Assert.Equal(1u, applicationSleep.Depth);
		Assert.Equal(0u, applicationSleep.SavedDisabled);
		Assert.Equal(1u, applicationSleep.Request);
		Assert.True(MuiApplicationWindowCore.SetApplicationSleepValue(ref platform,
			State, application, 0));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationSleepState(
			ref platform, State, application, out applicationSleep));
		Assert.Equal(0u, applicationSleep.Depth);
		Assert.Equal(0u, applicationSleep.Request);
	}

	[Fact]
	public void ApplicationSchedulerStateCodecUsesNamedQueueFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x2740);
		var value = default(MuiApplicationSchedulerStateRecord);
		value.Magic = MuiApplicationSchedulerStateRecord.Cookie;
		value.ReturnHead = APTR.FromPointer(0x2800);
		value.ReturnTail = APTR.FromPointer(0x2820);
		value.InputHandlers = APTR.FromPointer(0x2840);
		value.SignalMask = 0x20;
		value.PushHead = APTR.FromPointer(0x2860);
		value.PushTail = APTR.FromPointer(0x2880);
		Assert.True(MuiApplicationSchedulerStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationSchedulerStateRecordCodec.TryRead(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.ReturnHead, decoded.ReturnHead);
		Assert.Equal(value.ReturnTail, decoded.ReturnTail);
		Assert.Equal(value.InputHandlers, decoded.InputHandlers);
		Assert.Equal(value.SignalMask, decoded.SignalMask);
		Assert.Equal(value.PushHead, decoded.PushHead);
		Assert.Equal(value.PushTail, decoded.PushTail);
		var cursor = default(MuiApplicationSchedulerStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiApplicationSchedulerStateField.PushTail;
		Assert.True(MuiApplicationSchedulerStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 24, fieldAddress.Raw);
		cursor.Field = (MuiApplicationSchedulerStateField)255;
		Assert.False(MuiApplicationSchedulerStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ApplicationSchedulerPublishesQueueAndSignalState()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var destination = Object(ref platform, cl);
		var parameters = APTR.FromPointer(0x28C0);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0x20));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationSchedulerState(
			ref platform, State, application, out var scheduler));
		Assert.Equal(MuiApplicationSchedulerStateRecord.Cookie, scheduler.Magic);
		Assert.Equal(0x20u, scheduler.SignalMask);
		Assert.True(scheduler.ReturnHead.IsNull);
		Assert.True(scheduler.InputHandlers.IsNull);
		Assert.True(scheduler.PushHead.IsNull);

		Assert.True(MuiApplicationWindowCore.ReturnId(ref platform, State,
			application, 42));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationSchedulerState(
			ref platform, State, application, out scheduler));
		Assert.True(scheduler.ReturnHead.IsNotNull);
		Assert.Equal(scheduler.ReturnHead, scheduler.ReturnTail);
		Assert.Equal(42u, MuiApplicationWindowCore.Input(ref platform, State,
			application, APTR.FromPointer(0x2900)));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationSchedulerState(
			ref platform, State, application, out scheduler));
		Assert.True(scheduler.ReturnHead.IsNull);
		Assert.True(scheduler.ReturnTail.IsNull);

		var handler = APTR.FromPointer(0x2940);
		platform.WriteUInt32(handler, 8, destination.Raw);
		platform.WriteUInt32(handler, 12, 0x20);
		platform.WriteUInt32(handler, 20, 0x90000002);
		Assert.True(MuiApplicationWindowCore.AddInputHandler(ref platform, State,
			application, handler));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationSchedulerState(
			ref platform, State, application, out scheduler));
		Assert.True(scheduler.InputHandlers.IsNotNull);
		Assert.True(MuiApplicationWindowCore.RemoveInputHandler(ref platform, State,
			application, handler));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationSchedulerState(
			ref platform, State, application, out scheduler));
		Assert.True(scheduler.InputHandlers.IsNull);

		platform.WriteUInt32(parameters, 0, 0x1234);
		Assert.NotEqual(0u, MuiApplicationWindowCore.PushMethod(ref platform,
			State, application, destination, 1, parameters));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationSchedulerState(
			ref platform, State, application, out scheduler));
		Assert.True(scheduler.PushHead.IsNotNull);
		Assert.Equal(scheduler.PushHead, scheduler.PushTail);
		MuiApplicationWindowCore.DispatchPushedMethod(ref platform, State,
			application);
		Assert.True(MuiApplicationWindowCore.TryGetApplicationSchedulerState(
			ref platform, State, application, out scheduler));
		Assert.True(scheduler.PushHead.IsNull);
		Assert.True(scheduler.PushTail.IsNull);
	}

	[Fact]
	public void WindowInteractionStateCodecUsesNamedSnapshotAndChainFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x29C0);
		var value = default(MuiWindowInteractionStateRecord);
		value.Magic = MuiWindowInteractionStateRecord.Cookie;
		value.SnapshotFlags = 1;
		value.SnapshotRequests = 2;
		value.CycleChainHead = APTR.FromPointer(0x2A00);
		value.CycleChainCount = 3;
		value.CycleChainRequests = 4;
		Assert.True(MuiWindowInteractionStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiWindowInteractionStateRecordCodec.TryRead(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.SnapshotFlags, decoded.SnapshotFlags);
		Assert.Equal(value.SnapshotRequests, decoded.SnapshotRequests);
		Assert.Equal(value.CycleChainHead, decoded.CycleChainHead);
		Assert.Equal(value.CycleChainCount, decoded.CycleChainCount);
		Assert.Equal(value.CycleChainRequests, decoded.CycleChainRequests);
		var cursor = default(MuiWindowInteractionStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiWindowInteractionStateField.CycleChainRequests;
		Assert.True(MuiWindowInteractionStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 20, fieldAddress.Raw);
		cursor.Field = (MuiWindowInteractionStateField)255;
		Assert.False(MuiWindowInteractionStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void WindowInteractionPublishesSnapshotAndCycleChainState()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var first = Object(ref platform, cl);
		var second = Object(ref platform, cl);
		var vector = APTR.FromPointer(0x2A40);
		platform.WriteUInt32(vector, 0, first.Raw);
		platform.WriteUInt32(vector, 4, second.Raw);
		platform.WriteUInt32(vector, 8, 0);
		Assert.True(MuiApplicationWindowCore.SetCycleChain(ref platform, State,
			window, vector));
		Assert.True(MuiApplicationWindowCore.TryGetWindowInteractionState(
			ref platform, State, window, out var interaction));
		Assert.Equal(MuiWindowInteractionStateRecord.Cookie, interaction.Magic);
		Assert.Equal(0u, interaction.SnapshotFlags);
		Assert.Equal(0u, interaction.SnapshotRequests);
		Assert.Equal(2u, interaction.CycleChainCount);
		Assert.Equal(1u, interaction.CycleChainRequests);
		Assert.True(interaction.CycleChainHead.IsNotNull);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, window,
			0x804201BD, 0x43555052, false));
		Assert.True(MuiApplicationWindowCore.SnapshotWindow(ref platform, State,
			window, 1));
		Assert.True(MuiApplicationWindowCore.TryGetWindowInteractionState(
			ref platform, State, window, out interaction));
		Assert.Equal(1u, interaction.SnapshotFlags);
		Assert.Equal(1u, interaction.SnapshotRequests);
		Assert.True(MuiApplicationWindowCore.SnapshotWindow(ref platform, State,
			window, 0));
		Assert.True(MuiApplicationWindowCore.TryGetWindowInteractionState(
			ref platform, State, window, out interaction));
		Assert.Equal(0u, interaction.SnapshotFlags);
		Assert.Equal(2u, interaction.SnapshotRequests);
	}

	[Fact]
	public void WindowEventStateCodecUsesNamedPointerAndBooleanFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x2B00);
		var value = default(MuiWindowEventStateRecord);
		value.Magic = MuiWindowEventStateRecord.Cookie;
		value.CloseRequest = 1;
		value.InputEvent = APTR.FromPointer(0x2B40);
		value.MouseObject = APTR.FromPointer(0x2B80);
		Assert.True(MuiWindowEventStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiWindowEventStateRecordCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.CloseRequest, decoded.CloseRequest);
		Assert.Equal(value.InputEvent, decoded.InputEvent);
		Assert.Equal(value.MouseObject, decoded.MouseObject);
		var cursor = default(MuiWindowEventStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiWindowEventStateField.MouseObject;
		Assert.True(MuiWindowEventStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 12, fieldAddress.Raw);
		cursor.Field = (MuiWindowEventStateField)255;
		Assert.False(MuiWindowEventStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
	}

	[Fact]
	public void WindowEventStatePublishesCloseRequestAndGetterPointers()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var mouseObject = Object(ref platform, cl);
		var eventStorage = APTR.FromPointer(0x2BC0);
		Assert.True(MuiApplicationWindowCore.PublishWindowInputEventValue(
			ref platform, State, window, eventStorage));
		Assert.True(MuiApplicationWindowCore.TryGetWindowEventState(ref platform,
			State, window, out var eventState));
		Assert.Equal(MuiWindowEventStateRecord.Cookie, eventState.Magic);
		Assert.Equal(0u, eventState.CloseRequest);
		Assert.Equal(eventStorage, eventState.InputEvent);
		Assert.True(eventState.MouseObject.IsNull);

		Assert.True(MuiApplicationWindowCore.PublishWindowMouseObjectValue(
			ref platform, State, window, mouseObject));
		Assert.True(MuiApplicationWindowCore.TryGetWindowEventState(ref platform,
			State, window, out eventState));
		Assert.Equal(mouseObject, eventState.MouseObject);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, window,
			MuiWindowPublicCore.CloseRequest, 7, false));
		Assert.True(MuiApplicationWindowCore.TryGetWindowEventState(ref platform,
			State, window, out eventState));
		Assert.Equal(1u, eventState.CloseRequest);
		Assert.True(MuiApplicationWindowCore.PublishWindowInputEventValue(
			ref platform, State, window, APTR.Null));
		Assert.True(MuiApplicationWindowCore.TryGetWindowEventState(ref platform,
			State, window, out eventState));
		Assert.True(eventState.InputEvent.IsNull);
	}

	[Fact]
	public void WindowEventGettersProjectNamedState()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var mouseObject = Object(ref platform, cl);
		var eventStorage = APTR.FromPointer(0x2C40);
		Assert.True(MuiApplicationWindowCore.PublishWindowInputEventValue(
			ref platform, State, window, eventStorage));
		Assert.True(MuiApplicationWindowCore.PublishWindowMouseObjectValue(
			ref platform, State, window, mouseObject));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, window,
			MuiWindowPublicCore.CloseRequest, 9, false));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.InputEvent, out var inputEvent));
		Assert.Equal(eventStorage.Raw, inputEvent);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.MouseObject, out var currentMouse));
		Assert.Equal(mouseObject.Raw, currentMouse);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.CloseRequest, out var closeRequest));
		Assert.Equal(1u, closeRequest);
		Assert.True(MuiApplicationWindowCore.TryGetWindowEventState(ref platform,
			State, window, out var eventState));
		Assert.Equal(inputEvent, eventState.InputEvent.Raw);
		Assert.Equal(currentMouse, eventState.MouseObject.Raw);
		Assert.Equal(closeRequest, eventState.CloseRequest);
	}

	[Fact]
	public void ApplicationHelpStateCodecUsesNamedAboutAndHelpFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x2C00);
		var value = default(MuiApplicationHelpStateRecord);
		value.Magic = MuiApplicationHelpStateRecord.Cookie;
		value.AboutReferenceWindow = APTR.FromPointer(0x2C40);
		value.AboutRequests = 1;
		value.HelpWindow = APTR.FromPointer(0x2C80);
		value.HelpName = APTR.FromPointer(0x2CC0);
		value.HelpNode = APTR.FromPointer(0x2D00);
		value.HelpLine = unchecked((uint)-3);
		value.HelpRequests = 2;
		Assert.True(MuiApplicationHelpStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiApplicationHelpStateRecordCodec.TryRead(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.AboutReferenceWindow, decoded.AboutReferenceWindow);
		Assert.Equal(value.AboutRequests, decoded.AboutRequests);
		Assert.Equal(value.HelpWindow, decoded.HelpWindow);
		Assert.Equal(value.HelpName, decoded.HelpName);
		Assert.Equal(value.HelpNode, decoded.HelpNode);
		Assert.Equal(value.HelpLine, decoded.HelpLine);
		Assert.Equal(value.HelpRequests, decoded.HelpRequests);
		var cursor = default(MuiApplicationHelpStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiApplicationHelpStateField.HelpRequests;
		Assert.True(MuiApplicationHelpStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 28, fieldAddress.Raw);
		cursor.Field = (MuiApplicationHelpStateField)255;
		Assert.False(MuiApplicationHelpStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ApplicationHelpPublishesNamedAboutAndShowHelpState()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var referenceWindow = Object(ref platform, cl);
		var name = APTR.FromPointer(0x2D40);
		var node = APTR.FromPointer(0x2D80);
		platform.WriteCString(name, "SYS:Help.guide");
		platform.WriteCString(node, "main");
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiApplicationWindowCore.AboutMUI(ref platform, State,
			application, referenceWindow));
		Assert.True(MuiApplicationWindowCore.ShowHelp(ref platform, State,
			application, referenceWindow, name, node, -3));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationHelpState(
			ref platform, State, application, out var helpState));
		Assert.Equal(MuiApplicationHelpStateRecord.Cookie, helpState.Magic);
		Assert.Equal(referenceWindow, helpState.AboutReferenceWindow);
		Assert.Equal(1u, helpState.AboutRequests);
		Assert.Equal(referenceWindow, helpState.HelpWindow);
		Assert.Equal(name, helpState.HelpName);
		Assert.Equal(node, helpState.HelpNode);
		Assert.Equal(unchecked((uint)-3), helpState.HelpLine);
		Assert.Equal(1u, helpState.HelpRequests);
	}

	[Fact]
	public void ApplicationDefaultConfigStateCodecUsesNamedResultFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x2E00);
		var value = default(MuiApplicationDefaultConfigStateRecord);
		value.Magic = MuiApplicationDefaultConfigStateRecord.Cookie;
		value.ConfigId = 0x44;
		value.Value = 0x12345678;
		value.Requests = 7;
		Assert.True(MuiApplicationDefaultConfigStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationDefaultConfigStateRecordCodec.TryRead(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.ConfigId, decoded.ConfigId);
		Assert.Equal(value.Value, decoded.Value);
		Assert.Equal(value.Requests, decoded.Requests);
		var cursor = default(MuiApplicationDefaultConfigStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiApplicationDefaultConfigStateField.Requests;
		Assert.True(MuiApplicationDefaultConfigStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 12, fieldAddress.Raw);
		cursor.Field = (MuiApplicationDefaultConfigStateField)255;
		Assert.False(MuiApplicationDefaultConfigStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void DefaultConfigItemPublishesNamedResultState()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		platform.DefaultConfigItemValue = 0x12345678;
		Assert.Equal(0x12345678u, MuiApplicationWindowCore.DefaultConfigItem(
			ref platform, State, application, 0x44));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationDefaultConfigState(
			ref platform, State, application, out var configState));
		Assert.Equal(MuiApplicationDefaultConfigStateRecord.Cookie,
			configState.Magic);
		Assert.Equal(0x44u, configState.ConfigId);
		Assert.Equal(0x12345678u, configState.Value);
		Assert.Equal(1u, configState.Requests);

		platform.DefaultConfigItemValue = 0xCAFEBABE;
		Assert.Equal(0xCAFEBABEu, MuiApplicationWindowCore.DefaultConfigItem(
			ref platform, State, application, 0x45));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationDefaultConfigState(
			ref platform, State, application, out configState));
		Assert.Equal(0x45u, configState.ConfigId);
		Assert.Equal(0xCAFEBABEu, configState.Value);
		Assert.Equal(2u, configState.Requests);
	}

	[Fact]
	public void ApplicationConfigWindowStateCodecUsesNamedFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x2E80);
		var value = default(MuiApplicationConfigWindowStateRecord);
		value.Magic = MuiApplicationConfigWindowStateRecord.Cookie;
		value.Flags = 3;
		value.ClassId = APTR.FromPointer(0x2EC0);
		value.Requests = 5;
		Assert.True(MuiApplicationConfigWindowStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationConfigWindowStateRecordCodec.TryRead(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Flags, decoded.Flags);
		Assert.Equal(value.ClassId, decoded.ClassId);
		Assert.Equal(value.Requests, decoded.Requests);
		var cursor = default(MuiApplicationConfigWindowStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiApplicationConfigWindowStateField.Requests;
		Assert.True(MuiApplicationConfigWindowStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 12, fieldAddress.Raw);
		cursor.Field = (MuiApplicationConfigWindowStateField)255;
		Assert.False(MuiApplicationConfigWindowStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void OpenConfigWindowPublishesNamedRequestState()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var classId = APTR.FromPointer(0x2F00);
		platform.WriteCString(classId, "MUI:Config");
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiApplicationWindowCore.OpenConfigWindow(ref platform, State,
			application, 3, classId));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationConfigWindowState(
			ref platform, State, application, out var configState));
		Assert.Equal(MuiApplicationConfigWindowStateRecord.Cookie,
			configState.Magic);
		Assert.Equal(3u, configState.Flags);
		Assert.Equal(classId, configState.ClassId);
		Assert.Equal(1u, configState.Requests);

		Assert.True(MuiApplicationWindowCore.OpenConfigWindow(ref platform, State,
			application, 0, APTR.Null));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationConfigWindowState(
			ref platform, State, application, out configState));
		Assert.Equal(0u, configState.Flags);
		Assert.True(configState.ClassId.IsNull);
		Assert.Equal(2u, configState.Requests);
	}

	[Fact]
	public void ApplicationSettingsPanelStateCodecUsesNamedFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x2F40);
		var value = default(MuiApplicationSettingsPanelStateRecord);
		value.Magic = MuiApplicationSettingsPanelStateRecord.Cookie;
		value.Number = 9;
		value.Panel = APTR.FromPointer(0x2F80);
		value.Requests = 4;
		Assert.True(MuiApplicationSettingsPanelStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationSettingsPanelStateRecordCodec.TryRead(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Number, decoded.Number);
		Assert.Equal(value.Panel, decoded.Panel);
		Assert.Equal(value.Requests, decoded.Requests);
		var cursor = default(MuiApplicationSettingsPanelStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiApplicationSettingsPanelStateField.Panel;
		Assert.True(MuiApplicationSettingsPanelStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 8, fieldAddress.Raw);
		cursor.Field = (MuiApplicationSettingsPanelStateField)255;
		Assert.False(MuiApplicationSettingsPanelStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void BuildSettingsPanelPublishesNamedState()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var panel = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		platform.SettingsPanelResult = panel;
		Assert.Equal(panel, MuiApplicationWindowCore.BuildSettingsPanel(ref platform,
			State, application, 3));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationSettingsPanelState(
			ref platform, State, application, out var panelState));
		Assert.Equal(MuiApplicationSettingsPanelStateRecord.Cookie,
			panelState.Magic);
		Assert.Equal(3u, panelState.Number);
		Assert.Equal(panel, panelState.Panel);
		Assert.Equal(1u, panelState.Requests);

		platform.SettingsPanelResult = APTR.Null;
		Assert.True(MuiApplicationWindowCore.BuildSettingsPanel(ref platform, State,
			application, 4).IsNull);
		Assert.True(MuiApplicationWindowCore.TryGetApplicationSettingsPanelState(
			ref platform, State, application, out panelState));
		Assert.Equal(4u, panelState.Number);
		Assert.True(panelState.Panel.IsNull);
		Assert.Equal(2u, panelState.Requests);
	}

	[Fact]
	public void ApplicationSettingsPersistenceStateCodecUsesNamedFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x3040);
		var value = default(MuiApplicationSettingsPersistenceStateRecord);
		value.Magic = MuiApplicationSettingsPersistenceStateRecord.Cookie;
		value.Operation = 1;
		value.Name = APTR.FromPointer(uint.MaxValue);
		value.Requests = 7;
		value.Saves = 4;
		value.Loads = 3;
		Assert.True(MuiApplicationSettingsPersistenceStateRecordCodec.Write(
			ref platform, address, value));
		Assert.True(MuiApplicationSettingsPersistenceStateRecordCodec.TryRead(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Operation, decoded.Operation);
		Assert.Equal(value.Name, decoded.Name);
		Assert.Equal(value.Requests, decoded.Requests);
		Assert.Equal(value.Saves, decoded.Saves);
		Assert.Equal(value.Loads, decoded.Loads);
		var cursor = default(
			MuiApplicationSettingsPersistenceStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiApplicationSettingsPersistenceStateField.Loads;
		Assert.True(
			MuiApplicationSettingsPersistenceStateFieldCursorCodec.TryGetAddress(
				ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 20, fieldAddress.Raw);
		cursor.Field = (MuiApplicationSettingsPersistenceStateField)255;
		Assert.False(
			MuiApplicationSettingsPersistenceStateFieldCursorCodec.TryGetAddress(
				ref platform, cursor, out _));
	}

	[Fact]
	public void SaveAndLoadPublishNamedPersistenceState()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var name = APTR.FromPointer(0x3080);
		platform.WriteCString(name, "ENV:CopperOS.prefs");
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiApplicationWindowCore.SaveApplicationSettings(ref platform,
			State, application, name));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationSettingsPersistenceState(
			ref platform, State, application, out var settingsState));
		Assert.Equal(MuiApplicationSettingsPersistenceStateRecord.Cookie,
			settingsState.Magic);
		Assert.Equal(1u, settingsState.Operation);
		Assert.Equal(name, settingsState.Name);
		Assert.Equal(1u, settingsState.Requests);
		Assert.Equal(1u, settingsState.Saves);
		Assert.Equal(0u, settingsState.Loads);

		var envArc = APTR.FromPointer(uint.MaxValue);
		Assert.True(MuiApplicationWindowCore.LoadApplicationSettings(ref platform,
			State, application, envArc));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationSettingsPersistenceState(
			ref platform, State, application, out settingsState));
		Assert.Equal(0u, settingsState.Operation);
		Assert.Equal(envArc, settingsState.Name);
		Assert.Equal(2u, settingsState.Requests);
		Assert.Equal(1u, settingsState.Saves);
		Assert.Equal(1u, settingsState.Loads);
	}

	[Fact]
	public void ApplicationRefreshStateCodecUsesNamedFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x30C0);
		var value = default(MuiApplicationRefreshStateRecord);
		value.Magic = MuiApplicationRefreshStateRecord.Cookie;
		value.Checks = 9;
		value.RefreshedWindows = 5;
		Assert.True(MuiApplicationRefreshStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationRefreshStateRecordCodec.TryRead(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Checks, decoded.Checks);
		Assert.Equal(value.RefreshedWindows, decoded.RefreshedWindows);
		var cursor = default(MuiApplicationRefreshStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiApplicationRefreshStateField.RefreshedWindows;
		Assert.True(MuiApplicationRefreshStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 8, fieldAddress.Raw);
		cursor.Field = (MuiApplicationRefreshStateField)255;
		Assert.False(MuiApplicationRefreshStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void CheckRefreshPublishesNamedState()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var window = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, window));
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.True(MuiApplicationWindowCore.CheckRefresh(ref platform, State,
			application));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationRefreshState(
			ref platform, State, application, out var refreshState));
		Assert.Equal(MuiApplicationRefreshStateRecord.Cookie,
			refreshState.Magic);
		Assert.Equal(1u, refreshState.Checks);
		Assert.Equal(1u, refreshState.RefreshedWindows);

		Assert.True(MuiApplicationWindowCore.CheckRefresh(ref platform, State,
			application));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationRefreshState(
			ref platform, State, application, out refreshState));
		Assert.Equal(2u, refreshState.Checks);
		Assert.Equal(1u, refreshState.RefreshedWindows);
	}

	[Fact]
	public void ApplicationAndWindowLifecycleIsSchedulerDrivenAndOwned()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var window = Object(ref platform, cl);
		var first = Object(ref platform, cl);
		var second = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0x20));
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, window));
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0x1234));
		Assert.Equal(1u, platform.WindowOpenCount);
		Assert.Equal(0x1234u, platform.WindowEventMask);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, window, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, window, second));
		var cyclePacket = APTR.FromPointer(0x1600);
		platform.WriteUInt32(cyclePacket, 0, 0x80426510); // SetCycleChain
		platform.WriteUInt32(cyclePacket, 4, first.Raw);
		platform.WriteUInt32(cyclePacket, 8, second.Raw);
		platform.WriteUInt32(cyclePacket, 12, 0);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowCycleChain(
			ref platform, State, window, cyclePacket));
		Assert.True(MuiApplicationWindowCore.Activate(ref platform, State, window,
			first));
		Assert.True(MuiApplicationWindowCore.CycleActive(ref platform, State,
			window, true));
		Assert.Equal(2u, platform.WindowActivationCount);

		Assert.True(MuiApplicationWindowCore.ReturnId(ref platform, State,
			application, 42));
		Assert.Equal(0x20u, platform.SignaledMask);
		var signals = APTR.FromPointer(0x1200);
		Assert.Equal(42u, MuiApplicationWindowCore.Input(ref platform, State,
			application, signals));
		Assert.Equal(0u, platform.ReadUInt32(signals, 0));
		Assert.Equal(0u, MuiApplicationWindowCore.Input(ref platform, State,
			application, signals));
		Assert.Equal(0x20u, platform.ReadUInt32(signals, 0));

		Assert.True(MuiApplicationWindowCore.SetMenu(ref platform, State, window,
			7, true, true, true));
		Assert.True(MuiApplicationWindowCore.SetIconified(ref platform, State,
			application, true));
		Assert.True(MuiApplicationWindowCore.Requester(ref platform, State,
			application, window, APTR.FromPointer(0x1500), true));
		Assert.Equal(1u, platform.MenuOperationCount);
		Assert.True(platform.Iconified);
		Assert.Equal(1u, platform.RequesterOperationCount);
		Assert.True(MuiApplicationWindowCore.RemoveWindow(ref platform, State,
			application, window));
		Assert.Equal(1u, platform.WindowCloseCount);
	}

	[Fact]
	public void WindowActivateSetUsesMorphosWriteOneSemantics()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1700);
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, 0x80428D2F, 1));
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.Equal(0u, platform.WindowActivationCount);

		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.Equal(1u, platform.WindowActivationCount);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x80428D2F, out var active));
		Assert.Equal(1u, active);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.NoNotifySet, 0x80428D2F, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowActivate(
			ref platform, State, window, packet));
		Assert.Equal(1u, platform.WindowActivationCount);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x80428D2F, out active));
		Assert.Equal(1u, active);

		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, 0x80428D2F, 1));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowActivate(
			ref platform, State, window, packet));
	}

	[Fact]
	public void WindowSleepSetNestsAndSuppressesInputUntilFinalWake()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1780);
		var eventMessage = APTR.FromPointer(0x17C0);
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.False(platform.WindowBusy);
		Assert.Equal(0u, platform.WindowBusyOperationCount);
		Assert.True(MuiCommonControlPacketCore.WriteMethod(ref platform,
			eventMessage, 0x90000001));
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, 0x8042E7DB, 1));
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x8042E7DB, out var sleepDepth));
		Assert.Equal(1u, sleepDepth);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x80423661, out var disabled));
		Assert.Equal(1u, disabled);
		Assert.True(platform.WindowBusy);
		Assert.Equal(1u, platform.WindowBusyOperationCount);
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.NoNotifySet, 0x8042E7DB, 1));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowSleep(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x8042E7DB, out sleepDepth));
		Assert.Equal(2u, sleepDepth);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, 0x8042E7DB, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x8042E7DB, out sleepDepth));
		Assert.Equal(1u, sleepDepth);
		Assert.True(platform.WindowBusy);
		Assert.Equal(1u, platform.WindowBusyOperationCount);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x80423661, out disabled));
		Assert.Equal(1u, disabled);

		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowSleep(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x8042E7DB, out sleepDepth));
		Assert.Equal(0u, sleepDepth);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x80423661, out disabled));
		Assert.Equal(0u, disabled);
		Assert.False(platform.WindowBusy);
		Assert.Equal(2u, platform.WindowBusyOperationCount);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowSleep(
			ref platform, State, window, packet));

		// A sleep requested while closed is replayed to the next native window.
		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, 0x8042E7DB, 1));
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.False(platform.WindowBusy);
		Assert.Equal(2u, platform.WindowBusyOperationCount);
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.True(platform.WindowBusy);
		Assert.Equal(3u, platform.WindowBusyOperationCount);
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, 0x8042E7DB, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.False(platform.WindowBusy);
		Assert.Equal(4u, platform.WindowBusyOperationCount);
		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
	}

	[Fact]
	public void WindowSleepGetterProjectsNamedState()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, window,
			0x80423661u, 1, false));
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.True(MuiApplicationWindowCore.SetSleepValue(ref platform, State,
			window, 1));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Sleep, out var value));
		Assert.Equal(1u, value);
		Assert.True(MuiWindowPublicCore.TryGet(ref platform, State, window,
			MuiWindowPublicCore.Sleep, out value, out var sleepHandled));
		Assert.True(sleepHandled);
		Assert.Equal(1u, value);
		Assert.True(MuiApplicationWindowCore.TryGetWindowSleepState(ref platform,
			State, window, out var sleepState));
		Assert.Equal(value, sleepState.Request);
		Assert.Equal(1u, sleepState.Depth);
		Assert.Equal(1u, sleepState.SavedDisabled);

		Assert.True(MuiApplicationWindowCore.SetSleepValue(ref platform, State,
			window, 0));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Sleep, out value));
		Assert.Equal(0u, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x80423661u, out var disabled));
		Assert.Equal(1u, disabled);
		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
	}

	[Fact]
	public void ApplicationSleepNestsAcrossOwnedWindowsAndIsInherited()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var first = Object(ref platform, cl);
		var second = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1A00);
		var eventMessage = APTR.FromPointer(0x1A40);
		var handler = APTR.FromPointer(0x1A80);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiCommonControlPacketCore.WriteMethod(ref platform,
			eventMessage, 0x90000001));

		// Sleep before adding windows; AddWindow must inherit the application
		// depth and OpenWindow must replay the busy-pointer side effect.
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, 0x80425711, 1));
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x80425711, out var applicationSleep));
		Assert.Equal(1u, applicationSleep);
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, first));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, first,
			0x8042E7DB, out var sleepDepth));
		Assert.Equal(1u, sleepDepth);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, first,
			0x80423661, out var disabled));
		Assert.Equal(1u, disabled);
		Assert.False(platform.WindowBusy);
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State, first,
			0));
		Assert.True(platform.WindowBusy);
		Assert.Equal(1u, platform.WindowBusyOperationCount);
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, second));
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State, second,
			0));
		Assert.Equal(2u, platform.WindowBusyOperationCount);
		platform.WriteUInt32(handler, 8, first.Raw);
		platform.WriteUInt32(handler, 12, 0x20);
		platform.WriteUInt32(handler, 20, 0x90000002);
		Assert.True(MuiApplicationWindowCore.AddInputHandler(ref platform, State,
			application, handler));
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchInputHandlers(
			ref platform, State, application, 0x20));
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, first, eventMessage, 4));

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.NoNotifySet, 0x80425711, 1));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationSleep(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x80425711, out applicationSleep));
		Assert.Equal(2u, applicationSleep);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, first,
			0x8042E7DB, out sleepDepth));
		Assert.Equal(2u, sleepDepth);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, second,
			0x8042E7DB, out sleepDepth));
		Assert.Equal(2u, sleepDepth);
		Assert.Equal(2u, platform.WindowBusyOperationCount);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, 0x80425711, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationSleep(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x80425711, out applicationSleep));
		Assert.Equal(0u, applicationSleep);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, first,
			0x8042E7DB, out sleepDepth));
		Assert.Equal(0u, sleepDepth);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, first,
			0x80423661, out disabled));
		Assert.Equal(0u, disabled);
		Assert.False(platform.WindowBusy);
		Assert.Equal(4u, platform.WindowBusyOperationCount);
		Assert.Equal(1u, MuiApplicationWindowCore.DispatchInputHandlers(
			ref platform, State, application, 0x20));
		Assert.True(MuiApplicationWindowCore.RemoveWindow(ref platform, State,
			application, first));
		Assert.True(MuiApplicationWindowCore.RemoveWindow(ref platform, State,
			application, second));
	}

	[Fact]
	public void ApplicationSleepGetterProjectsNamedState()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x80425711u, out _));
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiApplicationWindowCore.SetApplicationSleepValue(ref platform,
			State, application, 1));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x80425711u, out var value));
		Assert.Equal(1u, value);
		Assert.True(MuiApplicationWindowCore.TryGetApplicationSleepState(
			ref platform, State, application, out var sleepState));
		Assert.Equal(value, sleepState.Request);
		Assert.Equal(1u, sleepState.Depth);

		Assert.True(MuiApplicationWindowCore.SetApplicationSleepValue(ref platform,
			State, application, 1));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x80425711u, out value));
		Assert.Equal(2u, value);
		Assert.True(MuiApplicationWindowCore.SetApplicationSleepValue(ref platform,
			State, application, 0));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x80425711u, out value));
		Assert.Equal(1u, value);
	}

	[Fact]
	public void ApplicationIconifiedClosesReopensAndDefersWindowOpenRequests()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var first = Object(ref platform, cl);
		var second = Object(ref platform, cl);
		var third = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1B00);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, first));
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, second));
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			first, 0));
		Assert.Equal(1u, platform.WindowOpenCount);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, 0x8042A07F, 1));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationIconified(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x8042A07F, out var iconified));
		Assert.Equal(1u, iconified);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			first, 0x80428AA0, out var open));
		Assert.Equal(0u, open);
		Assert.Equal(1u, platform.WindowCloseCount);

		// Repeating the same write is idempotent and does not close again.
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationIconified(
			ref platform, State, application, packet));
		Assert.Equal(1u, platform.WindowCloseCount);

		// A window already owned by the application and a new window added while
		// iconified both defer their native open until the application returns.
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, 0x80428AA0, 1));
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			second, packet));
		Assert.Equal(1u, platform.WindowOpenCount);
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, third));
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			third, 0));
		Assert.Equal(1u, platform.WindowOpenCount);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.NoNotifySet, 0x8042A07F, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationIconified(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x8042A07F, out iconified));
		Assert.Equal(0u, iconified);
		Assert.Equal(4u, platform.WindowOpenCount);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			first, 0x80428AA0, out open));
		Assert.Equal(1u, open);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			second, 0x80428AA0, out open));
		Assert.Equal(1u, open);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			third, 0x80428AA0, out open));
		Assert.Equal(1u, open);

		Assert.True(MuiApplicationWindowCore.RemoveWindow(ref platform, State,
			application, first));
		Assert.True(MuiApplicationWindowCore.RemoveWindow(ref platform, State,
			application, second));
		Assert.True(MuiApplicationWindowCore.RemoveWindow(ref platform, State,
			application, third));
	}

	[Fact]
	public void ApplicationActiveCanonicalizesCommoditiesBooleanWrites()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1B80);
		const uint active = 0x804260AB;

		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, active, 0x12345678));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationActive(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, active, out var value));
		Assert.Equal(1u, value);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.NoNotifySet, active, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, active, out value));
		Assert.Equal(0u, value);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, 0x80425711, 1));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationActive(
			ref platform, State, application, packet));
	}

	[Fact]
	public void ApplicationSingleTaskRejectsSecondStartAndSignalsExistingApplication()
	{
		var platform = CreatePlatform(out var cl);
		var first = Object(ref platform, cl);
		var second = Object(ref platform, cl);
		var third = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1C00);
		const uint singleTask = 0x8042A2C8;
		const uint doubleStart = 0x80423BC6;

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, singleTask, 0xCAFE));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationSingleTask(
			ref platform, State, first, packet));
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, first, 0));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, first,
			singleTask, out var value));
		Assert.Equal(1u, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, first,
			doubleStart, out value));
		Assert.Equal(0u, value);

		// An initializer-style TRUE on a second application is rejected during
		// application initialization, and the live application is notified.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, second,
			singleTask, 1, false));
		Assert.False(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, second, 0));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, first,
			doubleStart, out value));
		Assert.Equal(1u, value);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.NoNotifySet, doubleStart, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationDoubleStart(
			ref platform, State, first, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, first,
			doubleStart, out value));
		Assert.Equal(0u, value);

		Assert.False(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, second, 0));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, first,
			doubleStart, out value));
		Assert.Equal(1u, value);

		// SingleTask is initializer-only after the application is live.
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, singleTask, 0));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationSingleTask(
			ref platform, State, first, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, first,
			singleTask, out value));
		Assert.Equal(1u, value);

		// Applications that do not request SingleTask remain allowed.
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, third, 0));
	}

	[Fact]
	public void ApplicationForceQuitCanonicalizesQueryFlag()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1C80);
		const uint forceQuit = 0x804257DF;

		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, forceQuit, out var value));
		Assert.Equal(0u, value);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, forceQuit, 0xDEADBEEF));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationForceQuit(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, forceQuit, out value));
		Assert.Equal(1u, value);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.NoNotifySet, forceQuit, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, forceQuit, out value));
		Assert.Equal(0u, value);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, 0x804260AB, 1));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationForceQuit(
			ref platform, State, application, packet));
	}

	[Fact]
	public void ApplicationUseRexxHonorsInitializerOnlyPolicy()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1D00);
		const uint useRexx = 0x80422387;

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, useRexx, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationUseRexx(
			ref platform, State, application, packet));
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, useRexx, out var value));
		Assert.Equal(0u, value);

		// The default for an application that did not provide the initializer is
		// TRUE, while a live application's initializer cannot be changed.
		var defaultApplication = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, defaultApplication, 0));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			defaultApplication, useRexx, out value));
		Assert.Equal(1u, value);
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, useRexx, 1));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationUseRexx(
			ref platform, State, defaultApplication, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			defaultApplication, useRexx, out value));
		Assert.Equal(1u, value);
	}

	[Fact]
	public void ApplicationUseCommoditiesHonorsInitializerOnlyPolicy()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1D80);
		const uint useCommodities = 0x80425EE5;

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, useCommodities, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationUseCommodities(
			ref platform, State, application, packet));
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, useCommodities, out var value));
		Assert.Equal(0u, value);

		var defaultApplication = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, defaultApplication, 0));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			defaultApplication, useCommodities, out value));
		Assert.Equal(1u, value);
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, useCommodities, 0));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationUseCommodities(
			ref platform, State, defaultApplication, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			defaultApplication, useCommodities, out value));
		Assert.Equal(1u, value);
	}

	[Fact]
	public void ApplicationWindowInitializerTagsOwnMultipleChildWindows()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var firstWindow = Object(ref platform, cl);
		var secondWindow = Object(ref platform, cl);
		var thirdWindow = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1E80);
		const uint applicationWindow = 0x8042BFE0;

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, applicationWindow, firstWindow.Raw));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationWindow(
			ref platform, State, application, packet));
		Assert.Equal(firstWindow, MuiFamilyCore.GetChild(ref platform, State,
			application, 0, APTR.Null));

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, applicationWindow,
			secondWindow.Raw));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationWindow(
			ref platform, State, application, packet));
		Assert.Equal(secondWindow, MuiFamilyCore.GetChild(ref platform, State,
			application, 1, APTR.Null));

		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, applicationWindow, thirdWindow.Raw));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationWindow(
			ref platform, State, application, packet));
		Assert.Equal(APTR.Null, MuiFamilyCore.GetChild(ref platform, State,
			application, 2, APTR.Null));

		var invalidApplication = Object(ref platform, cl);
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, applicationWindow, 0x21000));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationWindow(
			ref platform, State, invalidApplication, packet));
	}

	[Fact]
	public void ApplicationWindowListProjectsOwnedWindowsAsTypedExecList()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var first = Object(ref platform, cl);
		var second = Object(ref platform, cl);
		var unrelatedFamilyChild = Object(ref platform, cl);
		const uint windowList = MuiApplicationWindowListCore.WindowList;

		// The public list contains application-owned windows, not every child
		// in the Family list (for example, a menu strip is a separate child).
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, first));
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, second));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, application,
			unrelatedFamilyChild));

		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, windowList, out var listRaw));
		var list = APTR.FromPointer(listRaw);
		Assert.True(list.IsNotNull);
		var cursorRaw = LayersExecListCodec.ReadHead(ref platform, list).Raw;
		Assert.Equal(first, MuiApplicationWindowListCore.NextObject(ref platform,
			list, ref cursorRaw));
		Assert.Equal(second, MuiApplicationWindowListCore.NextObject(ref platform,
			list, ref cursorRaw));
		Assert.Equal(APTR.Null, MuiApplicationWindowListCore.NextObject(ref platform,
			list, ref cursorRaw));
		Assert.False(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			application, windowList, 0, false));

		// Family mutation invalidates the cached projection and produces a new
		// typed list without rebuilding a managed object graph.
		var third = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, third));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, windowList, out listRaw));
		list = APTR.FromPointer(listRaw);
		cursorRaw = LayersExecListCodec.ReadHead(ref platform, list).Raw;
		Assert.Equal(first, MuiApplicationWindowListCore.NextObject(ref platform,
			list, ref cursorRaw));
		Assert.Equal(second, MuiApplicationWindowListCore.NextObject(ref platform,
			list, ref cursorRaw));
		Assert.Equal(third, MuiApplicationWindowListCore.NextObject(ref platform,
			list, ref cursorRaw));
		Assert.Equal(APTR.Null, MuiApplicationWindowListCore.NextObject(ref platform,
			list, ref cursorRaw));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			application));
	}

	[Fact]
	public void ApplicationUsedClassesValidatesGuestStringVector()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x2500);
		var vector = APTR.FromPointer(0x2600);
		var firstName = APTR.FromPointer(0x2700);
		var secondName = APTR.FromPointer(0x2800);
		const uint usedClasses = 0x8042E9A7;
		platform.WriteCString(firstName, "Listtree.mcc");
		platform.WriteCString(secondName, "Busy.mcc");
		platform.WriteUInt32(vector, 0, firstName.Raw);
		platform.WriteUInt32(vector, 4, secondName.Raw);
		platform.WriteUInt32(vector, 8, 0);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, usedClasses, vector.Raw));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationUsedClasses(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, usedClasses, out var value));
		Assert.Equal(vector.Raw, value);
		Assert.True(MuiApplicationWindowCore.TryGetApplicationUsedClassesState(
			ref platform, State, application, out var usedClassesState));
		Assert.Equal(MuiApplicationUsedClassesStateRecord.Cookie,
			usedClassesState.Magic);
		Assert.Equal(vector, usedClassesState.Vector);

		platform.WriteUInt32(vector, 4, 0x21000);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationUsedClasses(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, usedClasses, out value));
		Assert.Equal(vector.Raw, value);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, usedClasses, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationUsedClasses(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, usedClasses, out value));
		Assert.Equal(0u, value);
		Assert.True(MuiApplicationWindowCore.TryGetApplicationUsedClassesState(
			ref platform, State, application, out usedClassesState));
		Assert.True(usedClassesState.Vector.IsNull);
	}

	[Fact]
	public void ApplicationUsedClassesGetterProjectsNamedState()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var vector = APTR.FromPointer(0x2A80);
		var className = APTR.FromPointer(0x2AC0);
		platform.WriteCString(className, "Listtree.mcc");
		platform.WriteUInt32(vector, 0, className.Raw);
		platform.WriteUInt32(vector, 4, 0);

		Assert.True(MuiApplicationWindowCore.SetApplicationUsedClassesValue(
			ref platform, State, application, vector.Raw));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x8042E9A7u, out var value));
		Assert.Equal(vector.Raw, value);
		Assert.True(MuiApplicationWindowCore.TryGetApplicationUsedClassesState(
			ref platform, State, application, out var usedClassesState));
		Assert.Equal(value, usedClassesState.Vector.Raw);

		Assert.True(MuiApplicationWindowCore.SetApplicationUsedClassesValue(
			ref platform, State, application, 0));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x8042E9A7u, out value));
		Assert.Equal(0u, value);
		Assert.True(MuiApplicationWindowCore.TryGetApplicationUsedClassesState(
			ref platform, State, application, out usedClassesState));
		Assert.True(usedClassesState.Vector.IsNull);
	}

	[Fact]
	public void ApplicationUsedClassesStateCodecUsesNamedVectorPointer()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x35C0);
		var value = default(MuiApplicationUsedClassesStateRecord);
		value.Magic = MuiApplicationUsedClassesStateRecord.Cookie;
		value.Vector = APTR.FromPointer(0x3600);
		Assert.True(MuiApplicationUsedClassesStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationUsedClassesStateRecordCodec.TryRead(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Vector, decoded.Vector);
		var cursor = default(MuiApplicationUsedClassesStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiApplicationUsedClassesStateField.Vector;
		Assert.True(MuiApplicationUsedClassesStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 4, fieldAddress.Raw);
		cursor.Field = (MuiApplicationUsedClassesStateField)255;
		Assert.False(MuiApplicationUsedClassesStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ApplicationWindowRelationshipStateCodecUsesNamedFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x3640);
		var value = default(MuiApplicationWindowRelationshipStateRecord);
		value.Magic = MuiApplicationWindowRelationshipStateRecord.Cookie;
		value.LastWindow = APTR.FromPointer(0x3680);
		value.AddedCount = 3;
		Assert.True(MuiApplicationWindowRelationshipStateRecordCodec.Write(
			ref platform, address, value));
		Assert.True(MuiApplicationWindowRelationshipStateRecordCodec.TryRead(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.LastWindow, decoded.LastWindow);
		Assert.Equal(value.AddedCount, decoded.AddedCount);
		var cursor = default(
			MuiApplicationWindowRelationshipStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiApplicationWindowRelationshipStateField.AddedCount;
		Assert.True(
			MuiApplicationWindowRelationshipStateFieldCursorCodec.TryGetAddress(
				ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 8, fieldAddress.Raw);
		cursor.Field = (MuiApplicationWindowRelationshipStateField)255;
		Assert.False(
			MuiApplicationWindowRelationshipStateFieldCursorCodec.TryGetAddress(
				ref platform, cursor, out _));
	}

	[Fact]
	public void ApplicationWindowRelationshipPublishesLastWindowAndCount()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var first = Object(ref platform, cl);
		var second = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.SetApplicationWindowValue(
			ref platform, State, application, first.Raw));
		Assert.True(MuiApplicationWindowCore.SetApplicationWindowValue(
			ref platform, State, application, second.Raw));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationWindowRelationshipState(
			ref platform, State, application, out var relationship));
		Assert.Equal(MuiApplicationWindowRelationshipStateRecord.Cookie,
			relationship.Magic);
		Assert.Equal(second, relationship.LastWindow);
		Assert.Equal(2u, relationship.AddedCount);

		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		var third = Object(ref platform, cl);
		Assert.False(MuiApplicationWindowCore.SetApplicationWindowValue(
			ref platform, State, application, third.Raw));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationWindowRelationshipState(
			ref platform, State, application, out relationship));
		Assert.Equal(second, relationship.LastWindow);
		Assert.Equal(2u, relationship.AddedCount);
	}

	[Fact]
	public void ApplicationWindowGetterProjectsNamedRelationshipState()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var first = Object(ref platform, cl);
		var second = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.SetApplicationWindowValue(
			ref platform, State, application, first.Raw));
		Assert.True(MuiApplicationWindowCore.SetApplicationWindowValue(
			ref platform, State, application, second.Raw));

		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x8042BFE0u, out var value));
		Assert.Equal(second.Raw, value);
		Assert.True(MuiApplicationWindowCore.TryGetApplicationWindowRelationshipState(
			ref platform, State, application, out var relationship));
		Assert.Equal(value, relationship.LastWindow.Raw);
		Assert.Equal(2u, relationship.AddedCount);

		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		var rejected = Object(ref platform, cl);
		Assert.False(MuiApplicationWindowCore.SetApplicationWindowValue(
			ref platform, State, application, rejected.Raw));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x8042BFE0u, out value));
		Assert.Equal(second.Raw, value);
	}

	[Fact]
	public void WindowRelationshipStateCodecUsesNamedPointers()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x3700);
		var value = default(MuiWindowRelationshipStateRecord);
		value.Magic = MuiWindowRelationshipStateRecord.Cookie;
		value.RootObject = APTR.FromPointer(0x3740);
		value.Menustrip = APTR.FromPointer(0x3780);
		value.RefWindow = APTR.FromPointer(0x37C0);
		Assert.True(MuiWindowRelationshipStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiWindowRelationshipStateRecordCodec.TryRead(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.RootObject, decoded.RootObject);
		Assert.Equal(value.Menustrip, decoded.Menustrip);
		Assert.Equal(value.RefWindow, decoded.RefWindow);
		var cursor = default(MuiWindowRelationshipStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiWindowRelationshipStateField.RefWindow;
		Assert.True(MuiWindowRelationshipStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 12, fieldAddress.Raw);
		cursor.Field = (MuiWindowRelationshipStateField)255;
		Assert.False(MuiWindowRelationshipStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void WindowRelationshipPublishesRootMenuAndReferencePointers()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var root = Object(ref platform, cl);
		var reference = Object(ref platform, cl);
		var name = APTR.FromPointer(0x3800);
		platform.WriteCString(name, "Menustrip.mui");
		var stripClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(2), false);
		var strip = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			stripClass, APTR.Null);
		Assert.True(MuiMenuSpecialistCore.Attach(ref platform, State, strip,
			MuiMenuSpecialistClass.Menustrip).IsNotNull);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, window,
			MuiWindowPublicCore.RootObject, root.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Menustrip, strip.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, window,
			MuiWindowPublicCore.RefWindow, reference.Raw, false));
		Assert.True(MuiWindowPublicCore.TryGetWindowRelationshipState(
			ref platform, State, window, out var relationship));
		Assert.Equal(MuiWindowRelationshipStateRecord.Cookie,
			relationship.Magic);
		Assert.Equal(root, relationship.RootObject);
		Assert.Equal(strip, relationship.Menustrip);
		Assert.Equal(reference, relationship.RefWindow);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, window,
			MuiWindowPublicCore.RootObject, 0, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Menustrip, 0, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, window,
			MuiWindowPublicCore.RefWindow, 0, false));
		Assert.True(MuiWindowPublicCore.TryGetWindowRelationshipState(
			ref platform, State, window, out relationship));
		Assert.True(relationship.RootObject.IsNull);
		Assert.True(relationship.Menustrip.IsNull);
		Assert.True(relationship.RefWindow.IsNull);
	}

	[Fact]
	public void WindowControlStateCodecUsesNamedScalarFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x3840);
		var value = default(MuiWindowControlStateRecord);
		value.Magic = MuiWindowControlStateRecord.Cookie;
		value.Id = 0x12345678;
		value.DisableKeys = 0x000000A5;
		value.VisibleOnMaximize = 1;
		value.IsSubWindow = 1;
		value.NeedsMouseObject = 0;
		Assert.True(MuiWindowControlStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiWindowControlStateRecordCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Id, decoded.Id);
		Assert.Equal(value.DisableKeys, decoded.DisableKeys);
		Assert.Equal(value.VisibleOnMaximize, decoded.VisibleOnMaximize);
		Assert.Equal(value.IsSubWindow, decoded.IsSubWindow);
		Assert.Equal(value.NeedsMouseObject, decoded.NeedsMouseObject);
		var cursor = default(MuiWindowControlStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiWindowControlStateField.NeedsMouseObject;
		Assert.True(MuiWindowControlStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 20, fieldAddress.Raw);
		cursor.Field = (MuiWindowControlStateField)255;
		Assert.False(MuiWindowControlStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void WindowControlPublishesCanonicalScalarState()
	{
		var platform = CreatePlatform(out var cl);
		var tags = APTR.FromPointer(0x38C0);
		platform.WriteUInt32(tags, 0, MuiWindowPublicCore.IsSubWindow);
		platform.WriteUInt32(tags, 4, 9);
		platform.WriteUInt32(tags, 8, MuiWindowPublicCore.NeedsMouseObject);
		platform.WriteUInt32(tags, 12, 3);
		platform.WriteUInt32(tags, 16, 0);
		platform.WriteUInt32(tags, 20, 0);
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			tags);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Id, 0xCAFE, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, window,
			MuiWindowPublicCore.DisableKeys, 0x00FF00FF, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, window,
			MuiWindowPublicCore.VisibleOnMaximize, 7, false));

		Assert.True(MuiWindowPublicCore.TryGetWindowControlState(ref platform, State,
			window, out var control));
		Assert.Equal(MuiWindowControlStateRecord.Cookie, control.Magic);
		Assert.Equal(0xCAFEu, control.Id);
		Assert.Equal(0x00FF00FFu, control.DisableKeys);
		Assert.Equal(1u, control.VisibleOnMaximize);
		Assert.Equal(1u, control.IsSubWindow);
		Assert.Equal(1u, control.NeedsMouseObject);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.VisibleOnMaximize, out var visible));
		Assert.Equal(1u, visible);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.IsSubWindow, out var subWindow));
		Assert.Equal(1u, subWindow);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.NeedsMouseObject, out var needsMouseObject));
		Assert.Equal(1u, needsMouseObject);
	}

	[Fact]
	public void ApplicationUsedClassesVectorEntryUsesNamedPointer()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x2B00);
		var expected = new MuiApplicationUsedClassesVectorEntry
		{
			Name = APTR.FromPointer(0x2C00),
		};

		Assert.True(MuiApplicationUsedClassesVectorEntryCodec.Write(ref platform,
			address, expected));
		Assert.True(MuiApplicationUsedClassesVectorEntryCodec.TryRead(ref platform,
			address, out var actual));
		Assert.Equal(expected.Name, actual.Name);
		Assert.False(MuiApplicationUsedClassesVectorEntryCodec.TryRead(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void ApplicationCommandTableEntryUsesNamedCursorBoundary()
	{
		var platform = CreatePlatform(out _);
		var cursor = new MuiApplicationCommandTableCursor
		{
			Base = APTR.FromPointer(0x1800),
			Index = 2,
		};

		Assert.True(MuiApplicationCommandTableCodec.TryGetEntry(ref platform,
			cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x1848), address);
		cursor.Base = APTR.FromPointer(0x20FFC);
		cursor.Index = 0;
		Assert.False(MuiApplicationCommandTableCodec.TryGetEntry(ref platform,
			cursor, out _));
	}

	[Fact]
	public void ApplicationCommandFieldCursorUsesNamedBoundary()
	{
		var platform = CreatePlatform(out _);
		var record = APTR.FromPointer(0x1d00);
		var cursor = default(MuiApplicationCommandFieldCursor);
		cursor.Record = record;
		var fields = new[]
		{
			MuiApplicationCommandField.Name,
			MuiApplicationCommandField.Template,
			MuiApplicationCommandField.Parameters,
			MuiApplicationCommandField.Hook,
			MuiApplicationCommandField.Reserved0,
			MuiApplicationCommandField.Reserved1,
			MuiApplicationCommandField.Reserved2,
			MuiApplicationCommandField.Reserved3,
			MuiApplicationCommandField.Reserved4,
		};
		for (var i = 0; i < fields.Length; i++)
		{
			cursor.Field = fields[i];
			Assert.True(MuiApplicationCommandFieldCursorCodec.TryGetAddress(
				ref platform, cursor, out var address));
			Assert.Equal(record.Raw + (uint)(i * 4), address.Raw);
			Assert.True(MuiApplicationCommandFieldCursorCodec.TryWrite(ref platform,
				record, fields[i], (uint)(0x7000 + i)));
		}
		Assert.True(MuiApplicationCommandFieldCursorCodec.TryRead(ref platform,
			record, MuiApplicationCommandField.Parameters, out var parameters));
		Assert.Equal(0x7002u, parameters);
		Assert.True(MuiApplicationCommandRecordCodec.TryRead(ref platform, record,
			out var decoded));
		Assert.Equal(0x7000u, decoded.Name.Raw);
		Assert.Equal(0x7002, decoded.Parameters);
		Assert.Equal(0x7008, decoded.Reserved4);
		cursor.Field = (MuiApplicationCommandField)255;
		Assert.False(MuiApplicationCommandFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
		cursor.Record = APTR.FromPointer(0xfffffff0u);
		cursor.Field = MuiApplicationCommandField.Reserved4;
		Assert.False(MuiApplicationCommandFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ApplicationMessageNodeFieldCursorUsesNamedMixedWidthBoundary()
	{
		var platform = CreatePlatform(out _);
		var record = APTR.FromPointer(0x1e00);
		var cursor = default(MuiAppMessageNodeFieldCursor);
		cursor.Record = record;
		var fields = new[]
		{
			MuiAppMessageNodeField.Successor,
			MuiAppMessageNodeField.Predecessor,
			MuiAppMessageNodeField.Type,
			MuiAppMessageNodeField.Priority,
			MuiAppMessageNodeField.Name,
			MuiAppMessageNodeField.ReplyPort,
			MuiAppMessageNodeField.Length,
		};
		var offsets = new uint[] { 0, 4, 8, 9, 10, 14, 18 };
		for (var i = 0; i < fields.Length; i++)
		{
			cursor.Field = fields[i];
			Assert.True(MuiAppMessageNodeFieldCursorCodec.TryGetAddress(
				ref platform, cursor, out var address));
			Assert.Equal(record.Raw + offsets[i], address.Raw);
		}
		Assert.True(MuiAppMessageNodeFieldCursorCodec.TryWriteUInt32(ref platform,
			record, MuiAppMessageNodeField.Successor, 0x8000));
		Assert.True(MuiAppMessageNodeFieldCursorCodec.TryWriteUInt32(ref platform,
			record, MuiAppMessageNodeField.Name, 0x8100));
		Assert.True(MuiAppMessageNodeFieldCursorCodec.TryWriteUInt8(ref platform,
			record, MuiAppMessageNodeField.Type, 5));
		Assert.True(MuiAppMessageNodeFieldCursorCodec.TryWriteUInt8(ref platform,
			record, MuiAppMessageNodeField.Priority, 0xf8));
		Assert.True(MuiAppMessageNodeFieldCursorCodec.TryWriteUInt16(ref platform,
			record, MuiAppMessageNodeField.Length, 12));
		Assert.True(MuiAppMessageNodeCodec.TryRead(ref platform, record,
			out var decoded));
		Assert.Equal(0x8000u, decoded.Successor.Raw);
		Assert.Equal(0x8100u, decoded.Name.Raw);
		Assert.Equal((byte)5, decoded.Type);
		Assert.Equal((sbyte)-8, decoded.Priority);
		Assert.Equal((ushort)12, decoded.Length);
		cursor.Field = (MuiAppMessageNodeField)255;
		Assert.False(MuiAppMessageNodeFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
		cursor.Record = APTR.FromPointer(0xfffffff0u);
		cursor.Field = MuiAppMessageNodeField.Length;
		Assert.False(MuiAppMessageNodeFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ApplicationUsedClassesEntryUsesNamedCursorBoundary()
	{
		var platform = CreatePlatform(out _);
		var cursor = new MuiApplicationUsedClassesVectorCursor
		{
			Base = APTR.FromPointer(0x1900),
			Index = 2,
		};

		Assert.True(MuiApplicationUsedClassesVectorCodec.TryGetEntry(
			ref platform, cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x1908), address);
		cursor.Base = APTR.FromPointer(0x20FFE);
		cursor.Index = 0;
		Assert.False(MuiApplicationUsedClassesVectorCodec.TryGetEntry(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ApplicationWindowListEntryUsesNamedCursorBoundary()
	{
		var platform = CreatePlatform(out _);
		var cursor = new MuiApplicationWindowListEntryCursor
		{
			Base = APTR.FromPointer(0x1A00),
			Index = 2,
		};

		Assert.True(MuiApplicationWindowListEntryVectorCodec.TryGetEntry(
			ref platform, cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x1A20), address);
		cursor.Base = APTR.FromPointer(0x20FFF);
		cursor.Index = 0;
		Assert.False(MuiApplicationWindowListEntryVectorCodec.TryGetEntry(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ApplicationWindowListFieldsUseNamedRecordBoundaries()
	{
		var platform = CreatePlatform(out _);
		var stateAddress = APTR.FromPointer(0x1B00);
		var stateCursor = new MuiApplicationWindowListStateFieldCursor
		{
			Record = stateAddress,
			Field = MuiApplicationWindowListStateField.Generation,
		};
		Assert.True(MuiApplicationWindowListStateFieldCursorCodec.TryGetAddress(
			ref platform, stateCursor, out var fieldAddress));
		Assert.Equal(APTR.FromPointer(0x1B1C), fieldAddress);
		Assert.True(MuiApplicationWindowListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, stateAddress, MuiApplicationWindowListStateField.Generation,
			7));
		Assert.True(MuiApplicationWindowListStateFieldCursorCodec.TryReadUInt32(
			ref platform, stateAddress, MuiApplicationWindowListStateField.Generation,
			out var generation));
		Assert.Equal(7u, generation);

		var entryAddress = APTR.FromPointer(0x1C00);
		var entryCursor = new MuiApplicationWindowListEntryFieldCursor
		{
			Record = entryAddress,
			Field = MuiApplicationWindowListEntryField.Object,
		};
		Assert.True(MuiApplicationWindowListEntryFieldCursorCodec.TryGetAddress(
			ref platform, entryCursor, out fieldAddress));
		Assert.Equal(APTR.FromPointer(0x1C08), fieldAddress);
		Assert.True(MuiApplicationWindowListEntryFieldCursorCodec.TryWriteUInt32(
			ref platform, entryAddress, MuiApplicationWindowListEntryField.Object,
			0x1234));
		Assert.True(MuiApplicationWindowListEntryFieldCursorCodec.TryReadUInt32(
			ref platform, entryAddress, MuiApplicationWindowListEntryField.Object,
			out var objectValue));
		Assert.Equal(0x1234u, objectValue);
		Assert.False(MuiApplicationWindowListStateFieldCursorCodec.TryReadUInt32(
			ref platform, stateAddress,
			unchecked((MuiApplicationWindowListStateField)255), out _));
		Assert.False(MuiApplicationWindowListEntryFieldCursorCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x20FFF),
			MuiApplicationWindowListEntryField.Reserved, out _));
	}

	[Fact]
	public void ApplicationIconifyTitleRetainsValidatedMutableGuestPointer()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x2900);
		var title = APTR.FromPointer(0x2A00);
		const uint iconifyTitle = 0x80422CB8;
		platform.WriteCString(title, "CopperOS");

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, iconifyTitle, title.Raw));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationIconifyTitle(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, iconifyTitle, out var value));
		Assert.Equal(title.Raw, value);

		platform.WriteCString(title, "CopperOS MUI");
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationIconifyTitle(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, iconifyTitle, out value));
		Assert.Equal(title.Raw, value);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, iconifyTitle, 0x21000));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationIconifyTitle(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, iconifyTitle, out value));
		Assert.Equal(title.Raw, value);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, iconifyTitle, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationIconifyTitle(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, iconifyTitle, out value));
		Assert.Equal(0u, value);
	}

	[Fact]
	public void ApplicationTextStateCodecUsesNamedPointers()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x3280);
		var value = default(MuiApplicationTextStateRecord);
		value.Magic = MuiApplicationTextStateRecord.Cookie;
		value.HelpFile = APTR.FromPointer(0x32C0);
		value.IconifyTitle = APTR.FromPointer(0x3300);
		Assert.True(MuiApplicationTextStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationTextStateRecordCodec.TryRead(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.HelpFile, decoded.HelpFile);
		Assert.Equal(value.IconifyTitle, decoded.IconifyTitle);
		var cursor = default(MuiApplicationTextStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiApplicationTextStateField.IconifyTitle;
		Assert.True(MuiApplicationTextStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 8, fieldAddress.Raw);
		cursor.Field = (MuiApplicationTextStateField)255;
		Assert.False(MuiApplicationTextStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ApplicationTextPublicationUsesNamedGuestPointers()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var helpFile = APTR.FromPointer(0x3340);
		var iconifyTitle = APTR.FromPointer(0x3380);
		platform.WriteCString(helpFile, "SYS:Help.guide");
		platform.WriteCString(iconifyTitle, "CopperOS");
		Assert.True(MuiApplicationWindowCore.SetApplicationHelpFileValue(
			ref platform, State, application, helpFile.Raw));
		Assert.True(MuiApplicationWindowCore.SetApplicationIconifyTitleValue(
			ref platform, State, application, iconifyTitle.Raw));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationTextState(
			ref platform, State, application, out var textState));
		Assert.Equal(MuiApplicationTextStateRecord.Cookie, textState.Magic);
		Assert.Equal(helpFile, textState.HelpFile);
		Assert.Equal(iconifyTitle, textState.IconifyTitle);

		Assert.True(MuiApplicationWindowCore.SetApplicationHelpFileValue(
			ref platform, State, application, 0));
		Assert.True(MuiApplicationWindowCore.SetApplicationIconifyTitleValue(
			ref platform, State, application, 0));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationTextState(
			ref platform, State, application, out textState));
		Assert.True(textState.HelpFile.IsNull);
		Assert.True(textState.IconifyTitle.IsNull);
	}

	[Fact]
	public void ApplicationTextGettersProjectNamedState()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var helpFile = APTR.FromPointer(0x3600);
		var iconifyTitle = APTR.FromPointer(0x3640);
		platform.WriteCString(helpFile, "SYS:Help.guide");
		platform.WriteCString(iconifyTitle, "CopperOS");
		Assert.True(MuiApplicationWindowCore.SetApplicationHelpFileValue(
			ref platform, State, application, helpFile.Raw));
		Assert.True(MuiApplicationWindowCore.SetApplicationIconifyTitleValue(
			ref platform, State, application, iconifyTitle.Raw));

		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x804293F4u, out var helpValue));
		Assert.Equal(helpFile.Raw, helpValue);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x80422CB8u, out var titleValue));
		Assert.Equal(iconifyTitle.Raw, titleValue);

		Assert.True(MuiApplicationWindowCore.TryGetApplicationTextState(
			ref platform, State, application, out var textState));
		Assert.Equal(helpValue, textState.HelpFile.Raw);
		Assert.Equal(titleValue, textState.IconifyTitle.Raw);
	}

	[Fact]
	public void ApplicationUseScreenNotifyIsInitializerOnlyNamedState()
	{
		var platform = CreatePlatform(out var cl);
		var defaultApplication = Object(ref platform, cl);
		const uint useScreenNotify = 0x80420861;
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, defaultApplication, 0));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			defaultApplication, useScreenNotify, out var defaultValue));
		Assert.Equal(0u, defaultValue);

		var application = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x2B00);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, useScreenNotify, 2));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationUseScreenNotify(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, useScreenNotify, out var value));
		Assert.Equal(1u, value);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, useScreenNotify, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationUseScreenNotify(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, useScreenNotify, out value));
		Assert.Equal(0u, value);

		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, useScreenNotify, 1));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationUseScreenNotify(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, useScreenNotify, out value));
		Assert.Equal(0u, value);
	}

	[Fact]
	public void ApplicationDiskObjectRetainsValidatedGuestStructurePointer()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x2C00);
		var diskObject = APTR.FromPointer(0x2D00);
		const uint diskObjectAttribute = 0x804235CB;
		platform.WriteUInt8(diskObject, (int)Amiga.DiskObject.Size - 1, 0xA5);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, diskObjectAttribute, diskObject.Raw));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationDiskObject(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, diskObjectAttribute, out var value));
		Assert.Equal(diskObject.Raw, value);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, diskObjectAttribute, 0x21000));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationDiskObject(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, diskObjectAttribute, out value));
		Assert.Equal(diskObject.Raw, value);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, diskObjectAttribute, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationDiskObject(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, diskObjectAttribute, out value));
		Assert.Equal(0u, value);
	}

	[Fact]
	public void ApplicationDropObjectRequiresLiveGuestObject()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var dropObject = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x2E00);
		const uint dropObjectAttribute = 0x80421266;

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, dropObjectAttribute, dropObject.Raw));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationDropObject(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, dropObjectAttribute, out var value));
		Assert.Equal(dropObject.Raw, value);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, dropObjectAttribute, 0x21000));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationDropObject(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, dropObjectAttribute, out value));
		Assert.Equal(dropObject.Raw, value);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, dropObjectAttribute, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationDropObject(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, dropObjectAttribute, out value));
		Assert.Equal(0u, value);
	}

	[Fact]
	public void ApplicationObjectStateCodecUsesNamedPointers()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x3140);
		var value = default(MuiApplicationObjectStateRecord);
		value.Magic = MuiApplicationObjectStateRecord.Cookie;
		value.DiskObject = APTR.FromPointer(0x3180);
		value.DropObject = APTR.FromPointer(0x31C0);
		value.Menustrip = APTR.FromPointer(0x3200);
		Assert.True(MuiApplicationObjectStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationObjectStateRecordCodec.TryRead(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.DiskObject, decoded.DiskObject);
		Assert.Equal(value.DropObject, decoded.DropObject);
		Assert.Equal(value.Menustrip, decoded.Menustrip);
		var cursor = default(MuiApplicationObjectStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiApplicationObjectStateField.Menustrip;
		Assert.True(MuiApplicationObjectStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 12, fieldAddress.Raw);
		cursor.Field = (MuiApplicationObjectStateField)255;
		Assert.False(MuiApplicationObjectStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ApplicationObjectPublicationUsesNamedPointers()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var dropObject = Object(ref platform, cl);
		var diskObject = APTR.FromPointer(0x3240);
		platform.WriteUInt8(diskObject, (int)Amiga.DiskObject.Size - 1, 0xA5);
		Assert.True(MuiApplicationWindowCore.SetApplicationDiskObjectValue(
			ref platform, State, application, diskObject.Raw));
		Assert.True(MuiApplicationWindowCore.SetApplicationDropObjectValue(
			ref platform, State, application, dropObject.Raw));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationObjectState(
			ref platform, State, application, out var objectState));
		Assert.Equal(MuiApplicationObjectStateRecord.Cookie, objectState.Magic);
		Assert.Equal(diskObject, objectState.DiskObject);
		Assert.Equal(dropObject, objectState.DropObject);
		Assert.True(objectState.Menustrip.IsNull);

		Assert.True(MuiApplicationWindowCore.SetApplicationDiskObjectValue(
			ref platform, State, application, 0));
		Assert.True(MuiApplicationWindowCore.SetApplicationDropObjectValue(
			ref platform, State, application, 0));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationObjectState(
			ref platform, State, application, out objectState));
		Assert.True(objectState.DiskObject.IsNull);
		Assert.True(objectState.DropObject.IsNull);
	}

	[Fact]
	public void ApplicationObjectGettersProjectNamedState()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var dropObject = Object(ref platform, cl);
		var diskObject = APTR.FromPointer(0x3240);
		platform.WriteUInt8(diskObject, (int)Amiga.DiskObject.Size - 1, 0xA5);
		var name = APTR.FromPointer(0x3280);
		platform.WriteCString(name, "Menustrip.mui");
		var stripClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(3), false);
		var menustrip = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			stripClass, APTR.Null);
		Assert.True(MuiMenuSpecialistCore.Attach(ref platform, State, menustrip,
			MuiMenuSpecialistClass.Menustrip).IsNotNull);

		Assert.True(MuiApplicationWindowCore.SetApplicationDiskObjectValue(
			ref platform, State, application, diskObject.Raw));
		Assert.True(MuiApplicationWindowCore.SetApplicationDropObjectValue(
			ref platform, State, application, dropObject.Raw));
		Assert.True(MuiApplicationWindowCore.SetApplicationMenustripValue(
			ref platform, State, application, menustrip.Raw));

		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x804235CB, out var diskValue));
		Assert.Equal(diskObject.Raw, diskValue);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x80421266, out var dropValue));
		Assert.Equal(dropObject.Raw, dropValue);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x804252D9, out var menustripValue));
		Assert.Equal(menustrip.Raw, menustripValue);

		Assert.True(MuiApplicationWindowCore.TryGetApplicationObjectState(
			ref platform, State, application, out var objectState));
		Assert.Equal(diskValue, objectState.DiskObject.Raw);
		Assert.Equal(dropValue, objectState.DropObject.Raw);
		Assert.Equal(menustripValue, objectState.Menustrip.Raw);
	}

	[Fact]
	public void ApplicationMenuActionAndHelpUseTypedGuestState()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x2F00);
		const uint menuAction = 0x80428961;
		const uint menuHelp = 0x8042540B;

		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, menuAction, out var value));
		Assert.Equal(0u, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, menuHelp, out value));
		Assert.Equal(0u, value);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, menuAction, 0xCAFE));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationMenuAction(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, menuAction, out value));
		Assert.Equal(0xCAFEu, value);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, menuHelp, 0xBEEF));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationMenuAction(
			ref platform, State, application, packet));
		Assert.True(MuiApplicationWindowCore.PublishApplicationMenuHelpValue(
			ref platform, State, application, 0xBEEF));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, menuHelp, out value));
		Assert.Equal(0xBEEFu, value);
	}

	[Fact]
	public void ApplicationMenuGettersProjectNamedState()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiApplicationWindowCore.SetApplicationMenuActionValue(
			ref platform, State, application, 0xCAFE));
		Assert.True(MuiApplicationWindowCore.PublishApplicationMenuHelpValue(
			ref platform, State, application, 0xBEEF));

		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x80428961u, out var action));
		Assert.Equal(0xCAFEu, action);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x8042540Bu, out var help));
		Assert.Equal(0xBEEFu, help);
		Assert.True(MuiApplicationWindowCore.TryGetApplicationMenuState(
			ref platform, State, application, out var menuState));
		Assert.Equal(action, menuState.MenuAction);
		Assert.Equal(help, menuState.MenuHelp);
	}

	[Fact]
	public void ApplicationMenuStateCodecUsesNamedFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x3100);
		var value = default(MuiApplicationMenuStateRecord);
		value.Magic = MuiApplicationMenuStateRecord.Cookie;
		value.MenuAction = 0xCAFE;
		value.MenuHelp = 0xBEEF;
		Assert.True(MuiApplicationMenuStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiApplicationMenuStateRecordCodec.TryRead(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.MenuAction, decoded.MenuAction);
		Assert.Equal(value.MenuHelp, decoded.MenuHelp);
		var cursor = default(MuiApplicationMenuStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiApplicationMenuStateField.MenuHelp;
		Assert.True(MuiApplicationMenuStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 8, fieldAddress.Raw);
		cursor.Field = (MuiApplicationMenuStateField)255;
		Assert.False(MuiApplicationMenuStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ApplicationMenuPublicationUpdatesNamedState()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiApplicationWindowCore.SetApplicationMenuActionValue(
			ref platform, State, application, 0xCAFE));
		Assert.True(MuiApplicationWindowCore.PublishApplicationMenuHelpValue(
			ref platform, State, application, 0xBEEF));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationMenuState(
			ref platform, State, application, out var menuState));
		Assert.Equal(MuiApplicationMenuStateRecord.Cookie, menuState.Magic);
		Assert.Equal(0xCAFEu, menuState.MenuAction);
		Assert.Equal(0xBEEFu, menuState.MenuHelp);
	}

	[Fact]
	public void ApplicationIdentityStateCodecUsesNamedPointers()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x33C0);
		var value = default(MuiApplicationIdentityStateRecord);
		value.Magic = MuiApplicationIdentityStateRecord.Cookie;
		value.Author = APTR.FromPointer(0x3400);
		value.Base = APTR.FromPointer(0x3440);
		value.Copyright = APTR.FromPointer(0x3480);
		value.Description = APTR.FromPointer(0x34C0);
		value.Title = APTR.FromPointer(0x3500);
		value.Version = APTR.FromPointer(0x3540);
		Assert.True(MuiApplicationIdentityStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationIdentityStateRecordCodec.TryRead(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Author, decoded.Author);
		Assert.Equal(value.Base, decoded.Base);
		Assert.Equal(value.Copyright, decoded.Copyright);
		Assert.Equal(value.Description, decoded.Description);
		Assert.Equal(value.Title, decoded.Title);
		Assert.Equal(value.Version, decoded.Version);
		var cursor = default(MuiApplicationIdentityStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiApplicationIdentityStateField.Version;
		Assert.True(MuiApplicationIdentityStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 24, fieldAddress.Raw);
		cursor.Field = (MuiApplicationIdentityStateField)255;
		Assert.False(MuiApplicationIdentityStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ApplicationIdentityStringsRemainInitializerOnlyGuestPointers()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1E00);
		var titleText = APTR.FromPointer(0x1F00);
		var authorText = APTR.FromPointer(0x2000);
		var baseText = APTR.FromPointer(0x2100);
		var copyrightText = APTR.FromPointer(0x2200);
		var descriptionText = APTR.FromPointer(0x2300);
		var versionText = APTR.FromPointer(0x2400);
		platform.WriteCString(titleText, "CopperOS");
		platform.WriteCString(authorText, "Copper Team");
		platform.WriteCString(baseText, "COPPEROS");
		platform.WriteCString(copyrightText, "2026 CopperOS");
		platform.WriteCString(descriptionText, "MorphOS-compatible MUI");
		platform.WriteCString(versionText, "$VER: CopperOS 0.1");
		var attributes = new[]
		{
			(0x804281B8u, titleText),
			(0x80424842u, authorText),
			(0x8042E07Au, baseText),
			(0x8042EF4Du, copyrightText),
			(0x80421FC6u, descriptionText),
			(0x8042B33Fu, versionText)
		};

		foreach (var (attribute, text) in attributes)
		{
			Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
				packet, MuiCommonControlPacketCore.Set, attribute, text.Raw));
			Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform,
				State, application, packet));
			Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
				application, attribute, out var value));
			Assert.Equal(text.Raw, value);
		}
		Assert.True(MuiApplicationWindowCore.TryGetApplicationIdentityState(
			ref platform, State, application, out var identityState));
		Assert.Equal(MuiApplicationIdentityStateRecord.Cookie,
			identityState.Magic);
		Assert.Equal(authorText, identityState.Author);
		Assert.Equal(baseText, identityState.Base);
		Assert.Equal(copyrightText, identityState.Copyright);
		Assert.Equal(descriptionText, identityState.Description);
		Assert.Equal(titleText, identityState.Title);
		Assert.Equal(versionText, identityState.Version);

		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		foreach (var (attribute, text) in attributes)
		{
			Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
				application, attribute, out var value));
			Assert.Equal(text.Raw, value);
			Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
				packet, MuiCommonControlPacketCore.Set, attribute, 0));
			Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform,
				State, application, packet));
			Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
				application, attribute, out value));
			Assert.Equal(text.Raw, value);
		}

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, 0x804281B8u, 0xFFFFFF00));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationTitle(
			ref platform, State, application, packet));
	}

	[Fact]
	public void ApplicationIdentityGettersProjectNamedState()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var values = new[]
		{
			(0x80424842u, APTR.FromPointer(0x3680)),
			(0x8042E07Au, APTR.FromPointer(0x36C0)),
			(0x8042EF4Du, APTR.FromPointer(0x3700)),
			(0x80421FC6u, APTR.FromPointer(0x3740)),
			(0x804281B8u, APTR.FromPointer(0x3780)),
			(0x8042B33Fu, APTR.FromPointer(0x37C0)),
		};
		foreach (var (attribute, text) in values)
		{
			platform.WriteCString(text, "CopperOS");
			Assert.True(MuiApplicationWindowCore.SetApplicationInitializerStringValue(
				ref platform, State, application, attribute, text.Raw));
			Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
				application, attribute, out var value));
			Assert.Equal(text.Raw, value);
		}

		Assert.True(MuiApplicationWindowCore.TryGetApplicationIdentityState(
			ref platform, State, application, out var identityState));
		Assert.Equal(values[0].Item2.Raw, identityState.Author.Raw);
		Assert.Equal(values[1].Item2.Raw, identityState.Base.Raw);
		Assert.Equal(values[2].Item2.Raw, identityState.Copyright.Raw);
		Assert.Equal(values[3].Item2.Raw, identityState.Description.Raw);
		Assert.Equal(values[4].Item2.Raw, identityState.Title.Raw);
		Assert.Equal(values[5].Item2.Raw, identityState.Version.Raw);
	}

	[Fact]
	public void ApplicationPolicyStateCodecUsesNamedBooleanFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x3580);
		var value = default(MuiApplicationPolicyStateRecord);
		value.Magic = MuiApplicationPolicyStateRecord.Cookie;
		value.UseRexx = 0;
		value.UseCommodities = 1;
		value.UseScreenNotify = 1;
		Assert.True(MuiApplicationPolicyStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationPolicyStateRecordCodec.TryRead(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.UseRexx, decoded.UseRexx);
		Assert.Equal(value.UseCommodities, decoded.UseCommodities);
		Assert.Equal(value.UseScreenNotify, decoded.UseScreenNotify);
		var cursor = default(MuiApplicationPolicyStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiApplicationPolicyStateField.UseScreenNotify;
		Assert.True(MuiApplicationPolicyStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 12, fieldAddress.Raw);
		cursor.Field = (MuiApplicationPolicyStateField)255;
		Assert.False(MuiApplicationPolicyStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ApplicationPolicyPublicationUsesNamedState()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.SetApplicationUseRexxValue(
			ref platform, State, application, 0));
		Assert.True(MuiApplicationWindowCore.SetApplicationUseCommoditiesValue(
			ref platform, State, application, 1));
		Assert.True(MuiApplicationWindowCore.SetApplicationUseScreenNotifyValue(
			ref platform, State, application, 1));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationPolicyState(
			ref platform, State, application, out var policyState));
		Assert.Equal(MuiApplicationPolicyStateRecord.Cookie, policyState.Magic);
		Assert.Equal(0u, policyState.UseRexx);
		Assert.Equal(1u, policyState.UseCommodities);
		Assert.Equal(1u, policyState.UseScreenNotify);

		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationPolicyState(
			ref platform, State, application, out policyState));
		Assert.Equal(0u, policyState.UseRexx);
		Assert.Equal(1u, policyState.UseCommodities);
		Assert.Equal(1u, policyState.UseScreenNotify);
	}

	[Fact]
	public void ApplicationPolicyGettersProjectNamedState()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.SetApplicationUseRexxValue(
			ref platform, State, application, 2));
		Assert.True(MuiApplicationWindowCore.SetApplicationUseCommoditiesValue(
			ref platform, State, application, 0));
		Assert.True(MuiApplicationWindowCore.SetApplicationUseScreenNotifyValue(
			ref platform, State, application, 4));

		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x80422387u, out var useRexx));
		Assert.Equal(1u, useRexx);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x80425EE5u, out var useCommodities));
		Assert.Equal(0u, useCommodities);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x80420861u, out var useScreenNotify));
		Assert.Equal(1u, useScreenNotify);

		Assert.True(MuiApplicationWindowCore.TryGetApplicationPolicyState(
			ref platform, State, application, out var policyState));
		Assert.Equal(useRexx, policyState.UseRexx);
		Assert.Equal(useCommodities, policyState.UseCommodities);
		Assert.Equal(useScreenNotify, policyState.UseScreenNotify);
	}

	[Fact]
	public void WindowActiveObjectHandlesMorphosNoneNextAndPreviousSelectors()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var first = Object(ref platform, cl);
		var second = Object(ref platform, cl);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, window, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, window, second));
		var cyclePacket = APTR.FromPointer(0x1600);
		platform.WriteUInt32(cyclePacket, 0, 0x80426510); // SetCycleChain
		platform.WriteUInt32(cyclePacket, 4, first.Raw);
		platform.WriteUInt32(cyclePacket, 8, second.Raw);
		platform.WriteUInt32(cyclePacket, 12, 0);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowCycleChain(
			ref platform, State, window, cyclePacket));
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.True(MuiApplicationWindowCore.Activate(ref platform, State, window,
			first));

		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0x8042549A); // MUIM_Set
		platform.WriteUInt32(packet, 4, 0x80427925); // MUIA_Window_ActiveObject
		platform.WriteUInt32(packet, 8, uint.MaxValue); // Next
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x80427925, out var active));
		Assert.Equal(second.Raw, active);

		platform.WriteUInt32(packet, 8, uint.MaxValue - 1); // Prev
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x80427925, out active));
		Assert.Equal(first.Raw, active);

		platform.WriteUInt32(packet, 8, 0); // None
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x80427925, out active));
		Assert.Equal(0u, active);

		platform.WriteUInt32(packet, 8, second.Raw);
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x80427925, out active));
		Assert.Equal(second.Raw, active);

		// Spatial selectors require geometry and a cycle chain; this packet has
		// neither, so the current active object remains unchanged.
		platform.WriteUInt32(packet, 8, uint.MaxValue - 2);
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x80427925, out active));
		Assert.Equal(second.Raw, active);
	}

	[Fact]
	public void WindowActiveObjectSpatialSelectorsUseCycleChainGeometry()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var center = Object(ref platform, cl);
		var left = Object(ref platform, cl);
		var right = Object(ref platform, cl);
		var up = Object(ref platform, cl);
		var down = Object(ref platform, cl);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, window, center));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, window, left));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, window, right));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, window, up));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, window, down));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, center,
			50, 50, 10, 10));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, left,
			20, 50, 10, 10));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, right,
			80, 50, 10, 10));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, up,
			50, 20, 10, 10));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, down,
			50, 80, 10, 10));
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.True(MuiApplicationWindowCore.Activate(ref platform, State,
			window, center));

		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0x80426510); // MUIM_Window_SetCycleChain
		platform.WriteUInt32(packet, 4, center.Raw);
		platform.WriteUInt32(packet, 8, left.Raw);
		platform.WriteUInt32(packet, 12, right.Raw);
		platform.WriteUInt32(packet, 16, up.Raw);
		platform.WriteUInt32(packet, 20, down.Raw);
		platform.WriteUInt32(packet, 24, 0);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowCycleChain(
			ref platform, State, window, packet));

		platform.WriteUInt32(packet, 0, 0x8042549A); // MUIM_Set
		platform.WriteUInt32(packet, 4, 0x80427925); // ActiveObject
		platform.WriteUInt32(packet, 8, uint.MaxValue - 2); // Left
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x80427925, out var active));
		Assert.Equal(left.Raw, active);

		Assert.True(MuiApplicationWindowCore.Activate(ref platform, State,
			window, center));
		platform.WriteUInt32(packet, 8, uint.MaxValue - 3); // Right
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x80427925, out active));
		Assert.Equal(right.Raw, active);

		Assert.True(MuiApplicationWindowCore.Activate(ref platform, State,
			window, center));
		platform.WriteUInt32(packet, 8, uint.MaxValue - 4); // Up
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x80427925, out active));
		Assert.Equal(up.Raw, active);

		Assert.True(MuiApplicationWindowCore.Activate(ref platform, State,
			window, center));
		platform.WriteUInt32(packet, 8, uint.MaxValue - 5); // Down
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x80427925, out active));
		Assert.Equal(down.Raw, active);
	}

	[Fact]
	public void WindowActiveObjectDirectAssignmentRequiresCycleChainMembership()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var member = Object(ref platform, cl);
		var outsider = Object(ref platform, cl);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, window, member));
		var cyclePacket = APTR.FromPointer(0x1600);
		platform.WriteUInt32(cyclePacket, 0, 0x80426510); // SetCycleChain
		platform.WriteUInt32(cyclePacket, 4, member.Raw);
		platform.WriteUInt32(cyclePacket, 8, 0);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowCycleChain(
			ref platform, State, window, cyclePacket));
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.True(MuiApplicationWindowCore.Activate(ref platform, State,
			window, member));

		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0x8042549A); // MUIM_Set
		platform.WriteUInt32(packet, 4, 0x80427925); // ActiveObject
		platform.WriteUInt32(packet, 8, outsider.Raw);
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x80427925, out var active));
		Assert.Equal(member.Raw, active);

		platform.WriteUInt32(packet, 8, member.Raw);
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, window,
			0x7FFE0039, 0x1F00, false));
		platform.WriteUInt32(packet, 8, uint.MaxValue); // Next on malformed chain
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x80427925, out active));
		Assert.Equal(member.Raw, active);
	}

	[Fact]
	public void WindowFocusStatePublishesActiveAndDefaultObjects()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var member = Object(ref platform, cl);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, window, member));
		var cyclePacket = APTR.FromPointer(0x1600);
		platform.WriteUInt32(cyclePacket, 0, 0x80426510);
		platform.WriteUInt32(cyclePacket, 4, member.Raw);
		platform.WriteUInt32(cyclePacket, 8, 0);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowCycleChain(
			ref platform, State, window, cyclePacket));
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.True(MuiApplicationWindowCore.Activate(ref platform, State, window,
			member));
		Assert.True(MuiApplicationWindowCore.SetDefaultObjectValue(ref platform,
			State, window, member));
		Assert.True(MuiApplicationWindowCore.TryGetWindowFocusState(ref platform,
			State, window, out var focus));
		Assert.Equal(MuiWindowFocusStateRecord.Cookie, focus.Magic);
		Assert.Equal(member, focus.ActiveObject);
		Assert.Equal(member, focus.DefaultObject);

		Assert.True(MuiApplicationWindowCore.SetActiveObjectValue(ref platform,
			State, window, 0));
		Assert.True(MuiApplicationWindowCore.TryGetWindowFocusState(ref platform,
			State, window, out focus));
		Assert.True(focus.ActiveObject.IsNull);
		Assert.Equal(member, focus.DefaultObject);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x80427925, out var active));
		Assert.Equal(0u, active);
	}

	[Fact]
	public void WindowFocusStateCodecUsesNamedFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x3C80);
		var value = default(MuiWindowFocusStateRecord);
		value.Magic = MuiWindowFocusStateRecord.Cookie;
		value.ActiveObject = APTR.FromPointer(0x3D00);
		value.DefaultObject = APTR.FromPointer(0x3D40);
		Assert.True(MuiWindowFocusStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiWindowFocusStateRecordCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.ActiveObject, decoded.ActiveObject);
		Assert.Equal(value.DefaultObject, decoded.DefaultObject);
		var cursor = default(MuiWindowFocusStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiWindowFocusStateField.DefaultObject;
		Assert.True(MuiWindowFocusStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 8, fieldAddress.Raw);
		cursor.Field = (MuiWindowFocusStateField)255;
		Assert.False(MuiWindowFocusStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void WindowScreenDepthPacketsRequireAnOpenWindow()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0x8042913D); // ScreenToBack
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.Equal(0u, platform.ScreenDepthOperationCount);

		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.Equal(1u, platform.ScreenDepthOperationCount);
		Assert.False(platform.LastScreenDepthToFront);

		platform.WriteUInt32(packet, 0, 0x804227A4); // ScreenToFront
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.Equal(2u, platform.ScreenDepthOperationCount);
		Assert.True(platform.LastScreenDepthToFront);

		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.Equal(2u, platform.ScreenDepthOperationCount);
	}

	[Fact]
	public void RequestAndRejectIdcmpRetainFlagsAcrossWindowOpen()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.RequestIDCMP(ref platform, State,
			window, 0x200));
		Assert.Equal(0u, platform.WindowEventMask);
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.Equal(0x200u, platform.WindowEventMask);
		Assert.True(MuiApplicationWindowCore.RequestIDCMP(ref platform, State,
			window, 0x004));
		Assert.Equal(0x204u, platform.WindowEventMask);
		Assert.True(MuiApplicationWindowCore.RejectIDCMP(ref platform, State,
			window, 0x200));
		Assert.Equal(0x004u, platform.WindowEventMask);
		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
		Assert.True(MuiApplicationWindowCore.RejectIDCMP(ref platform, State,
			window, 0x004));
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.Equal(0u, platform.WindowEventMask);
	}

	[Fact]
	public void WindowEventHandlersDispatchAndRemoveDeterministically()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var target = Object(ref platform, cl);
		var handler = APTR.FromPointer(0x1300);
		var message = APTR.FromPointer(0x1400);
		platform.WriteUInt16(handler, 10, 0x0002);
		platform.WriteUInt32(handler, 12, target.Raw);
		platform.WriteUInt32(handler, 20, 4);
		platform.WriteUInt32(message, 0, 0x90000001);
		Assert.True(MuiApplicationWindowCore.AddEventHandler(ref platform, State,
			window, handler));
		Assert.Equal(1u, MuiApplicationWindowCore.DispatchWindowEvent(ref platform,
			State, window, message, 4));
		Assert.Equal(target, platform.LastDispatchObject);
		Assert.True(MuiApplicationWindowCore.RemoveEventHandler(ref platform,
			State, window, handler));
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchWindowEvent(ref platform,
			State, window, message, 4));
	}

	[Fact]
	public void ExactApplicationPacketsQueueAndDeliverReturnIds()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0x40));
		var packet = APTR.FromPointer(0x1200);
		var signals = APTR.FromPointer(0x1240);
		platform.WriteUInt32(packet, 0, 0x804276EF);
		platform.WriteUInt32(packet, 4, 99);
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		platform.WriteUInt32(packet, 0, 0x8042D0F5);
		platform.WriteUInt32(packet, 4, signals.Raw);
		Assert.Equal(99u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
	}

	[Fact]
	public void ExecuteAndRunDriveTheApplicationLoopUntilQuit()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0x40));
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiApplicationWindowCore.ReturnId(ref platform, State,
			application, uint.MaxValue));
		platform.WriteUInt32(packet, 0, 0x804253F3); // Execute
		Assert.Equal(uint.MaxValue, MuiApplicationDispatcher.Dispatch(ref platform,
			State, application, packet));
		Assert.Equal(0u, platform.WaitMuiSignalsCount);

		Assert.True(MuiApplicationWindowCore.ReturnId(ref platform, State,
			application, uint.MaxValue));
		platform.WriteUInt32(packet, 0, 0x90420103); // Run
		Assert.Equal(uint.MaxValue, MuiApplicationDispatcher.Dispatch(ref platform,
			State, application, packet));
		Assert.Equal(0u, platform.WaitMuiSignalsCount);
	}

	[Fact]
	public void AboutMuiValidatesReferenceAndUsesPresentationSeam()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var referenceWindow = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0x8042D21D);

		// A null reference is permitted by MorphOS and is passed to the platform.
		platform.WriteUInt32(packet, 4, 0);
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.Equal(1u, platform.AboutMUIRequestCount);
		Assert.Equal(application, platform.LastAboutMUIApplication);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x7FFE0021, out var requests));
		Assert.Equal(1u, requests);

		// A live MUI Window object is accepted and retained as the last reference.
		platform.WriteUInt32(packet, 4, referenceWindow.Raw);
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.Equal(referenceWindow, platform.LastAboutMUIReference);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x7FFE0020, out var storedReference));
		Assert.Equal(referenceWindow.Raw, storedReference);

		// Arbitrary pointers are not MUI Window objects and must not reach the seam.
		platform.WriteUInt32(packet, 4, 0x1F00);
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.Equal(2u, platform.AboutMUIRequestCount);
	}

	[Fact]
	public void ShowHelpResolvesFirstOpenWindowAndValidatesGuestStrings()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var openWindow = Object(ref platform, cl);
		var closedWindow = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, openWindow));
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, closedWindow));
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			openWindow, 0));
		var name = APTR.FromPointer(0x1300);
		var node = APTR.FromPointer(0x1400);
		var helpFile = APTR.FromPointer(0x1500);
		var replacementHelpFile = APTR.FromPointer(0x1600);
		platform.WriteCString(name, "SYS:Help.guide");
		platform.WriteCString(node, "main");
		platform.WriteCString(helpFile, "SYS:Fallback.guide");
		platform.WriteCString(replacementHelpFile, "SYS:Replacement.guide");
		var packet = APTR.FromPointer(0x1200);
		var helpFilePacket = APTR.FromPointer(0x1700);
		const uint helpFileAttribute = 0x804293F4;
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			helpFilePacket, MuiCommonControlPacketCore.Set, helpFileAttribute,
			helpFile.Raw));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationHelpFile(
			ref platform, State, application, helpFilePacket));
		platform.WriteUInt32(packet, 0, 0x80426479);
		platform.WriteUInt32(packet, 4, uint.MaxValue);
		platform.WriteUInt32(packet, 8, name.Raw);
		platform.WriteUInt32(packet, 12, node.Raw);
		platform.WriteUInt32(packet, 16, unchecked((uint)-3));

		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.Equal(1u, platform.ShowHelpRequestCount);
		Assert.Equal(application, platform.LastShowHelpApplication);
		Assert.Equal(openWindow, platform.LastShowHelpWindow);
		Assert.Equal(name, platform.LastShowHelpName);
		Assert.Equal(node, platform.LastShowHelpNode);
		Assert.Equal(-3, platform.LastShowHelpLine);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x7FFE0028, out var requests));
		Assert.Equal(1u, requests);

		// Null window/name/node selects the default public screen and the
		// application's HelpFile fallback without dereferencing guest strings.
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, 0);
		platform.WriteUInt32(packet, 12, 0);
		platform.WriteUInt32(packet, 16, 7);
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.Equal(APTR.Null, platform.LastShowHelpWindow);
		Assert.Equal(helpFile, platform.LastShowHelpName);
		Assert.Equal(2u, platform.ShowHelpRequestCount);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			helpFilePacket, MuiCommonControlPacketCore.NoNotifySet,
			helpFileAttribute, replacementHelpFile.Raw));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationHelpFile(
			ref platform, State, application, helpFilePacket));
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.Equal(replacementHelpFile, platform.LastShowHelpName);
		Assert.Equal(3u, platform.ShowHelpRequestCount);

		// A malformed C string and an arbitrary window pointer are rejected
		// before the presentation seam is reached.
		platform.WriteUInt32(packet, 8, 0x21000);
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		platform.WriteUInt32(packet, 8, 0);
		platform.WriteUInt32(packet, 4, 0x1F00);
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.Equal(3u, platform.ShowHelpRequestCount);
	}

	[Fact]
	public void DefaultConfigItemUsesApplicationOverrideCapability()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		platform.DefaultConfigItemValue = 0x12345678;
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0x8042D934);
		platform.WriteUInt32(packet, 4, 0x44);
		Assert.Equal(0x12345678u, MuiApplicationDispatcher.Dispatch(ref platform,
			State, application, packet));
		Assert.Equal(1u, platform.DefaultConfigRequestCount);
		Assert.Equal(application, platform.LastDefaultConfigApplication);
		Assert.Equal(0x44u, platform.LastDefaultConfigId);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x7FFE0029, out var storedId));
		Assert.Equal(0x44u, storedId);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x7FFE002A, out var storedValue));
		Assert.Equal(0x12345678u, storedValue);

		var dead = APTR.FromPointer(0x21000);
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			dead, packet));
		Assert.Equal(1u, platform.DefaultConfigRequestCount);
	}

	[Fact]
	public void SetConfigItemRetainsOpaqueGuestDataAndCleansUp()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		var data = APTR.FromPointer(0x1700);
		platform.WriteUInt8(data, 0, 0xA5);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0x80424A80); // SetConfigItem
		platform.WriteUInt32(packet, 4, 0x34);
		platform.WriteUInt32(packet, 8, data.Raw);
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.True(MuiApplicationWindowCore.ReadSetConfigItemState(ref platform,
			State, application, out var item, out var storedData, out var requests));
		Assert.Equal(0x34u, item);
		Assert.Equal(data.Raw, storedData);
		Assert.Equal(1u, requests);

		// A null opaque payload is retained without being dereferenced.
		platform.WriteUInt32(packet, 4, 0x35);
		platform.WriteUInt32(packet, 8, 0);
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.True(MuiApplicationWindowCore.ReadSetConfigItemState(ref platform,
			State, application, out item, out storedData, out requests));
		Assert.Equal(0x35u, item);
		Assert.Equal(0u, storedData);
		Assert.Equal(2u, requests);

		// An unmapped non-null payload is rejected before the guest record changes.
		platform.WriteUInt32(packet, 8, 0x21000);
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.True(MuiApplicationWindowCore.ReadSetConfigItemState(ref platform,
			State, application, out item, out storedData, out requests));
		Assert.Equal(0x35u, item);
		Assert.Equal(0u, storedData);
		Assert.Equal(2u, requests);

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			application));
		Assert.False(MuiApplicationWindowCore.ReadSetConfigItemState(ref platform,
			State, application, out _, out _, out _));
	}

	[Fact]
	public void OpenConfigWindowValidatesClassIdAndUsesPresentationSeam()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		var classId = APTR.FromPointer(0x1300);
		platform.WriteCString(classId, "CopperOS");
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0x804299BA);
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, classId.Raw);

		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.Equal(1u, platform.OpenConfigWindowRequestCount);
		Assert.Equal(application, platform.LastOpenConfigWindowApplication);
		Assert.Equal(0u, platform.LastOpenConfigWindowFlags);
		Assert.Equal(classId, platform.LastOpenConfigWindowClassId);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x7FFE002C, out var flags));
		Assert.Equal(0u, flags);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x7FFE002D, out var storedClassId));
		Assert.Equal(classId.Raw, storedClassId);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x7FFE002E, out var requests));
		Assert.Equal(1u, requests);

		// Null selects the platform's default configuration class and does not
		// dereference caller memory.
		platform.WriteUInt32(packet, 8, 0);
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.Equal(APTR.Null, platform.LastOpenConfigWindowClassId);
		Assert.Equal(2u, platform.OpenConfigWindowRequestCount);

		// A malformed guest string and an invalid application are rejected before
		// the explicit presentation capability is reached.
		platform.WriteUInt32(packet, 8, 0x21000);
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		var dead = APTR.FromPointer(0x21000);
		platform.WriteUInt32(packet, 8, 0);
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			dead, packet));
		Assert.Equal(2u, platform.OpenConfigWindowRequestCount);
	}

	[Fact]
	public void WindowSnapshotRequiresIdAndAcceptsOnlyMorphosFlags()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0x8042945E);
		platform.WriteUInt32(packet, 4, 1);

		// Snapshotting without MUIA_Window_ID is rejected before the platform
		// settings capability is reached.
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.Equal(0u, platform.WindowSnapshotCount);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, window,
			0x804201BD, 0x43555052, false));
		// MorphOS requires the ID, not an already-open native window.
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.Equal(1u, platform.WindowSnapshotCount);
		Assert.Equal(window, platform.LastSnapshotMuiWindow);
		Assert.Equal(1u, platform.LastSnapshotFlags);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x7FFE0037, out var flags));
		Assert.Equal(1u, flags);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x7FFE0038, out var requests));
		Assert.Equal(1u, requests);

		// Zero clears the remembered position and remains a valid request.
		platform.WriteUInt32(packet, 4, 0);
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.Equal(2u, platform.WindowSnapshotCount);
		Assert.Equal(0u, platform.LastSnapshotFlags);

		// MorphOS defines no other flag values.
		platform.WriteUInt32(packet, 4, 2);
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.Equal(2u, platform.WindowSnapshotCount);
	}

	[Fact]
	public void WindowCycleChainCopiesInlineObjectsAndRejectsInvalidReplacement()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var first = Object(ref platform, cl);
		var second = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0x80426510);
		platform.WriteUInt32(packet, 4, first.Raw);
		platform.WriteUInt32(packet, 8, second.Raw);
		platform.WriteUInt32(packet, 12, 0);

		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x7FFE003A, out var count));
		Assert.Equal(2u, count);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x7FFE003B, out var requests));
		Assert.Equal(1u, requests);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x7FFE0039, out var head));
		Assert.NotEqual(0u, head);

		// A bad member must not replace or discard the existing copied chain.
		platform.WriteUInt32(packet, 4, 0x1F00);
		platform.WriteUInt32(packet, 8, 0);
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x7FFE003A, out count));
		Assert.Equal(2u, count);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x7FFE0039, out var retainedHead));
		Assert.Equal(head, retainedHead);

		// Disposal reaches CleanupRecords and releases the copied chain nodes.
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, window));
	}

	[Fact]
	public void BuildSettingsPanelReturnsOnlyLiveApplicationPanelObjects()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var panel = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		platform.SettingsPanelResult = panel;
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0x8042B58F);
		platform.WriteUInt32(packet, 4, 3);

		Assert.Equal(panel.Raw, MuiApplicationDispatcher.Dispatch(ref platform,
			State, application, packet));
		Assert.Equal(1u, platform.BuildSettingsPanelRequestCount);
		Assert.Equal(application, platform.LastBuildSettingsPanelApplication);
		Assert.Equal(3u, platform.LastBuildSettingsPanelNumber);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x7FFE002F, out var number));
		Assert.Equal(3u, number);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x7FFE0030, out var storedPanel));
		Assert.Equal(panel.Raw, storedPanel);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x7FFE0031, out var requests));
		Assert.Equal(1u, requests);

		// A valid application may report that it has no panel for a number.
		platform.SettingsPanelResult = APTR.Null;
		platform.WriteUInt32(packet, 4, 4);
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.Equal(2u, platform.BuildSettingsPanelRequestCount);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x7FFE002F, out number));
		Assert.Equal(4u, number);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x7FFE0030, out storedPanel));
		Assert.Equal(0u, storedPanel);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x7FFE0031, out requests));
		Assert.Equal(2u, requests);

		// A platform capability must not expose an arbitrary pointer as a MUI
		// object; the call is rejected before guest telemetry is updated.
		platform.SettingsPanelResult = APTR.FromPointer(0x21000);
		platform.WriteUInt32(packet, 4, 5);
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.Equal(3u, platform.BuildSettingsPanelRequestCount);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x7FFE002F, out number));
		Assert.Equal(4u, number);

		var dead = APTR.FromPointer(0x21000);
		platform.SettingsPanelResult = panel;
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			dead, packet));
		Assert.Equal(3u, platform.BuildSettingsPanelRequestCount);
	}

	[Fact]
	public void ApplicationSaveAndLoadValidateNamesAndPreserveEnvSelectors()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0x804227EF); // Save
		platform.WriteUInt32(packet, 4, 0); // MUIV_Application_Save_ENV
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.Equal(1u, platform.SettingsSaveRequestCount);
		Assert.Equal(APTR.Null, platform.LastSettingsName);

		platform.WriteUInt32(packet, 0, 0x8042F90D); // Load
		platform.WriteUInt32(packet, 4, uint.MaxValue); // MUIV_Application_Load_ENVARC
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.Equal(1u, platform.SettingsLoadRequestCount);
		Assert.Equal(uint.MaxValue, platform.LastSettingsName.Raw);

		var name = APTR.FromPointer(0x1300);
		platform.WriteCString(name, "ENV:CopperOS.prefs");
		platform.WriteUInt32(packet, 4, name.Raw);
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.Equal(2u, platform.SettingsLoadRequestCount);
		Assert.Equal(name, platform.LastSettingsName);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x7FFE0032, out var operation));
		Assert.Equal(0u, operation);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x7FFE0033, out var storedName));
		Assert.Equal(name.Raw, storedName);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x7FFE0034, out var requests));
		Assert.Equal(3u, requests);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x7FFE0035, out var saves));
		Assert.Equal(1u, saves);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x7FFE0036, out var loads));
		Assert.Equal(2u, loads);

		// Malformed names and a failed native persistence capability are rejected
		// without changing guest telemetry.
		platform.WriteUInt32(packet, 4, 0x21000);
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		platform.SettingsOperationResult = false;
		platform.WriteUInt32(packet, 4, name.Raw);
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.Equal(2u, platform.SettingsLoadRequestCount);
		platform.SettingsOperationResult = true;
		var dead = APTR.FromPointer(0x21000);
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			dead, packet));
		Assert.Equal(2u, platform.SettingsLoadRequestCount);
	}

	[Fact]
	public void ApplicationSettingsUseBoundedGuestDosFileRoundTrip()
	{
		var platform = CreatePlatform(out var applicationClass);
		var application = Object(ref platform, applicationClass);
		var numericName = APTR.FromPointer(0x1180);
		var dataspaceName = APTR.FromPointer(0x11C0);
		platform.WriteCString(numericName, "Numeric.mui");
		platform.WriteCString(dataspaceName, "Dataspace.mui");
		var numericClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, numericName, APTR.Null, 0, APTR.FromPointer(1));
		Assert.NotEqual(APTR.Null, numericClass);
		Assert.NotEqual(APTR.Null, MuiHeadlessObjectCore.RegisterBuiltinClass(
			ref platform, State, dataspaceName, APTR.Null, 0,
			APTR.FromPointer(1)));
		var numeric = MuiCommonControlCore.CreateControl(ref platform, State,
			numericClass, APTR.Null);
		Assert.NotEqual(APTR.Null, numeric);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, application, numeric));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			numeric, 0x8042D76E, 0xB001, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			numeric, 0x8042AE3A, 73, false));
		platform.UseSettingsFile = true;
		var packet = APTR.FromPointer(0x1300);
		platform.WriteUInt32(packet, 0, 0x804227EF);
		platform.WriteUInt32(packet, 4, 0);
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.True(platform.SettingsFileLength >=
			MuiApplicationSettingsFileCore.HeaderSize);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			numeric, 0x8042AE3A, 7, false));
		platform.WriteUInt32(packet, 0, 0x8042F90D);
		platform.WriteUInt32(packet, 4, uint.MaxValue);
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			numeric, 0x8042AE3A, out var restored));
		Assert.Equal(73u, restored);
		Assert.Equal(1u, platform.SettingsSaveRequestCount);
		Assert.Equal(1u, platform.SettingsLoadRequestCount);
		Assert.Equal(2u, platform.DosOpenCount);
		Assert.Equal(2u, platform.DosCloseCount);
	}

	[Fact]
	public void CheckRefreshVisitsOnlyOpenApplicationWindows()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var openWindow = Object(ref platform, cl);
		var closedWindow = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, openWindow));
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, closedWindow));
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			openWindow, 0));
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0x80424D68);

		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.Equal(1u, platform.RefreshMuiWindowCount);
		Assert.Equal(openWindow, platform.LastRefreshedMuiWindow);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x7FFE0022, out var checks));
		Assert.Equal(1u, checks);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x7FFE0023, out var refreshed));
		Assert.Equal(1u, refreshed);

		// A dead application is rejected before the capability is reached.
		var dead = APTR.FromPointer(0x1F00);
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			dead, packet));
		Assert.Equal(1u, platform.RefreshMuiWindowCount);
	}

	[Fact]
	public void ApplicationMenuPacketsTraverseOpenChildWindows()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var openWindow = Object(ref platform, cl);
		var closedWindow = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, openWindow));
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, closedWindow));
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			openWindow, 0));
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0x8042C0A7); // GetMenuCheck
		platform.WriteUInt32(packet, 4, 7);
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		platform.WriteUInt32(packet, 0, 0x8042A707); // SetMenuCheck
		platform.WriteUInt32(packet, 8, 0);
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		platform.WriteUInt32(packet, 0, 0x80428BEF); // SetMenuState
		platform.WriteUInt32(packet, 8, 0);
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.Equal(2u, platform.MenuOperationCount);

		var dead = APTR.FromPointer(0x1F00);
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			dead, packet));
		Assert.Equal(2u, platform.MenuOperationCount);
	}

	[Fact]
	public void ApplicationCommandsValidatesNamedGuestCommandTable()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var table = APTR.FromPointer(0x2A00);
		var name = APTR.FromPointer(0x2B00);
		var template = APTR.FromPointer(0x2B20);
		var secondName = APTR.FromPointer(0x2B40);
		var packet = APTR.FromPointer(0x2C00);
		const uint commands = MuiApplicationCommandsCore.Commands;
		platform.WriteCString(name, "RESCAN");
		platform.WriteCString(template, "PATTERN/K");
		platform.WriteCString(secondName, "ABOUT");
		var first = default(MuiApplicationCommandRecord);
		first.Name = name;
		first.Template = template;
		first.Parameters = 1;
		first.Hook = APTR.FromPointer(0x2D00);
		first.Reserved2 = 0x1234;
		var second = default(MuiApplicationCommandRecord);
		second.Name = secondName;
		second.Template = APTR.FromPointer(
			MuiApplicationCommandsCore.MagicTemplate);
		second.Parameters = 0;
		var terminator = default(MuiApplicationCommandRecord);
		Assert.True(MuiApplicationCommandRecordCodec.Write(ref platform, table,
			first));
		Assert.True(MuiApplicationCommandRecordCodec.Write(ref platform,
			APTR.FromPointer(table.Raw + MuiApplicationCommandRecord.Size), second));
		Assert.True(MuiApplicationCommandRecordCodec.Write(ref platform,
			APTR.FromPointer(table.Raw + 2 * MuiApplicationCommandRecord.Size),
			terminator));

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, commands, table.Raw));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationCommands(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, commands, out var value));
		Assert.Equal(table.Raw, value);
		Assert.True(MuiApplicationCommandsCore.TryGetApplicationCommandsState(
			ref platform, State, application, out var commandState));
		Assert.Equal(MuiApplicationCommandsStateRecord.Cookie,
			commandState.Magic);
		Assert.Equal(table, commandState.Table);
		Assert.True(MuiApplicationCommandRecordCodec.TryRead(ref platform, table,
			out var decoded));
		Assert.Equal(first.Name, decoded.Name);
		Assert.Equal(first.Template, decoded.Template);
		Assert.Equal(first.Parameters, decoded.Parameters);
		Assert.Equal(first.Reserved2, decoded.Reserved2);

		// A malformed nested string is rejected without replacing the installed
		// caller-owned table pointer.
		platform.WriteUInt32(table, 0, 0x21000);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationCommands(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, commands, out value));
		Assert.Equal(table.Raw, value);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, commands, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationCommands(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, commands, out value));
		Assert.Equal(0u, value);
		Assert.True(MuiApplicationCommandsCore.TryGetApplicationCommandsState(
			ref platform, State, application, out commandState));
		Assert.True(commandState.Table.IsNull);
	}

	[Fact]
	public void ApplicationCommandsStateCodecUsesNamedFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x3B80);
		var value = default(MuiApplicationCommandsStateRecord);
		value.Magic = MuiApplicationCommandsStateRecord.Cookie;
		value.Table = APTR.FromPointer(0x3C00);
		Assert.True(MuiApplicationCommandsStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationCommandsStateRecordCodec.TryRead(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Table, decoded.Table);
		var cursor = default(MuiApplicationCommandsStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiApplicationCommandsStateField.Table;
		Assert.True(MuiApplicationCommandsStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 4, fieldAddress.Raw);
		cursor.Field = (MuiApplicationCommandsStateField)255;
		Assert.False(MuiApplicationCommandsStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void AppWindowPublishesTransientAppMessagesAndFindsApplicationObject()
	{
		var platform = CreatePlatform(out var applicationClass);
		var application = Object(ref platform, applicationClass);
		var windowName = APTR.FromPointer(0x2E00);
		platform.WriteCString(windowName, "Window.mui");
		var windowClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			windowName, APTR.Null, 0, APTR.FromPointer(1), false);
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			windowClass, APTR.Null);
		var target = Object(ref platform, applicationClass);
		Assert.NotEqual(APTR.Null, window);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			APTR.FromPointer(0x2E40), MuiCommonControlPacketCore.Set,
			MuiApplicationMessageCore.WindowAppWindow, 1));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowAppWindow(
			ref platform, State, window, APTR.FromPointer(0x2E40)));
		Assert.True(MuiApplicationMessageCore.TryGetApplicationMessageRoutingState(
			ref platform, State, window, out var windowRouting));
		Assert.Equal(MuiApplicationMessageRoutingStateRecord.Cookie,
			windowRouting.Magic);
		Assert.Equal(1u, windowRouting.WindowAppWindow);
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, window));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, window, target));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, target,
			MuiApplicationMessageCore.ApplicationObject, out var applicationValue));
		Assert.Equal(application.Raw, applicationValue);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, target,
			MuiApplicationMessageCore.AppMessage, out var initialMessage));
		Assert.Equal(0u, initialMessage);
		Assert.True(MuiApplicationMessageCore.TryGetApplicationMessageRoutingState(
			ref platform, State, target, out var initialRouting));
		Assert.True(initialRouting.AppMessage.IsNull);
		Assert.Equal(0u, initialRouting.WindowAppWindow);

		var argumentList = APTR.FromPointer(0x2E80);
		var argumentName = APTR.FromPointer(0x2EA0);
		var message = APTR.FromPointer(0x2F00);
		platform.WriteCString(argumentName, "PROGDIR:drop");
		Assert.True(MuiWorkbenchArgumentRecordCodec.Write(ref platform,
			argumentList, new MuiWorkbenchArgumentRecord
			{
				Name = argumentName
			}));
		Assert.True(MuiAppMessageRecordCodec.Write(ref platform, message,
			new MuiAppMessageRecord
			{
				Type = 8,
				UserData = 0x55,
				NumberOfArguments = 1,
				ArgumentList = argumentList
			}));
		var follow = APTR.FromPointer(0x2F80);
		platform.WriteUInt32(follow, 0, 0x90000001);
		Assert.True(MuiNotifyCore.Add(ref platform, State, target,
			MuiApplicationMessageCore.AppMessage,
			(uint)Amiga.MUI.Value.EveryTime, application, 1, follow));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchAppMessage(ref platform,
			State, target, message));
		Assert.Equal(1u, platform.DispatchCount);
		Assert.Equal(application.Raw, platform.LastDispatchObject.Raw);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, target,
			MuiApplicationMessageCore.AppMessage, out var clearedMessage));
		Assert.Equal(0u, clearedMessage);
		Assert.True(MuiApplicationMessageCore.TryGetApplicationMessageRoutingState(
			ref platform, State, target, out var clearedRouting));
		Assert.True(clearedRouting.AppMessage.IsNull);

		platform.WriteUInt32(argumentList, 4, 0x21000);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchAppMessage(ref platform,
			State, target, message));
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			APTR.FromPointer(0x2E40), MuiCommonControlPacketCore.NoNotifySet,
			MuiApplicationMessageCore.WindowAppWindow, 0));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowAppWindow(
			ref platform, State, window, APTR.FromPointer(0x2E40)));
	}

	[Fact]
	public void ApplicationMessageRoutingStateCodecUsesNamedFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x3AC0);
		var value = default(MuiApplicationMessageRoutingStateRecord);
		value.Magic = MuiApplicationMessageRoutingStateRecord.Cookie;
		value.AppMessage = APTR.FromPointer(0x3B00);
		value.WindowAppWindow = 1;
		Assert.True(MuiApplicationMessageRoutingStateRecordCodec.Write(
			ref platform, address, value));
		Assert.True(MuiApplicationMessageRoutingStateRecordCodec.TryRead(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.AppMessage, decoded.AppMessage);
		Assert.Equal(value.WindowAppWindow, decoded.WindowAppWindow);
		var cursor = default(
			MuiApplicationMessageRoutingStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiApplicationMessageRoutingStateField.WindowAppWindow;
		Assert.True(
			MuiApplicationMessageRoutingStateFieldCursorCodec.TryGetAddress(
				ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 8, fieldAddress.Raw);
		cursor.Field = (MuiApplicationMessageRoutingStateField)255;
		Assert.False(
			MuiApplicationMessageRoutingStateFieldCursorCodec.TryGetAddress(
				ref platform, cursor, out _));
	}

	[Fact]
	public void AppMessageNodeCodecUsesNamedWorkbenchFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x2F00);
		var expected = new MuiAppMessageNodeState
		{
			Successor = APTR.FromPointer(0x3100),
			Predecessor = APTR.FromPointer(0x3200),
			Type = 8,
			Priority = -2,
			Name = APTR.FromPointer(0x3300),
			ReplyPort = APTR.FromPointer(0x3400),
			Length = 86
		};

		Assert.True(MuiAppMessageNodeCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiAppMessageNodeCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.Successor, actual.Successor);
		Assert.Equal(expected.Predecessor, actual.Predecessor);
		Assert.Equal(expected.Type, actual.Type);
		Assert.Equal(expected.Priority, actual.Priority);
		Assert.Equal(expected.Name, actual.Name);
		Assert.Equal(expected.ReplyPort, actual.ReplyPort);
		Assert.Equal(expected.Length, actual.Length);
		Assert.False(MuiAppMessageNodeCodec.TryRead(ref platform, APTR.Null,
			out _));
	}

	[Fact]
	public void ApplicationMessageFieldCursorUsesNamedMixedWidthBoundary()
	{
		var platform = CreatePlatform(out _);
		var record = APTR.FromPointer(0x3000);
		var cursor = default(MuiAppMessageFieldCursor);
		cursor.Record = record;
		var fields = new[]
		{
			MuiAppMessageField.Type,
			MuiAppMessageField.UserData,
			MuiAppMessageField.Id,
			MuiAppMessageField.NumberOfArguments,
			MuiAppMessageField.ArgumentList,
			MuiAppMessageField.Version,
			MuiAppMessageField.Class,
			MuiAppMessageField.MouseX,
			MuiAppMessageField.MouseY,
			MuiAppMessageField.Seconds,
			MuiAppMessageField.Micros,
			MuiAppMessageField.Reserved0,
			MuiAppMessageField.Reserved1,
			MuiAppMessageField.Reserved2,
			MuiAppMessageField.Reserved3,
			MuiAppMessageField.Reserved4,
			MuiAppMessageField.Reserved5,
			MuiAppMessageField.Reserved6,
			MuiAppMessageField.Reserved7,
		};
		var offsets = new uint[]
			{ 20, 22, 26, 30, 34, 38, 40, 42, 44, 46, 50, 54, 58, 62, 66, 70, 74, 78, 82 };
		for (var i = 0; i < fields.Length; i++)
		{
			cursor.Field = fields[i];
			Assert.True(MuiAppMessageFieldCursorCodec.TryGetAddress(ref platform,
				cursor, out var address));
			Assert.Equal(record.Raw + offsets[i], address.Raw);
		}
		Assert.True(MuiAppMessageFieldCursorCodec.TryWriteUInt16(ref platform,
			record, MuiAppMessageField.Type, 8));
		Assert.True(MuiAppMessageFieldCursorCodec.TryWriteUInt32(ref platform,
			record, MuiAppMessageField.UserData, 0x55));
		Assert.True(MuiAppMessageFieldCursorCodec.TryWriteUInt32(ref platform,
			record, MuiAppMessageField.NumberOfArguments, unchecked((uint)-2)));
		Assert.True(MuiAppMessageFieldCursorCodec.TryWriteUInt32(ref platform,
			record, MuiAppMessageField.ArgumentList, 0x3500));
		Assert.True(MuiAppMessageFieldCursorCodec.TryWriteUInt16(ref platform,
			record, MuiAppMessageField.MouseX, unchecked((ushort)-4)));
		Assert.True(MuiAppMessageFieldCursorCodec.TryWriteUInt16(ref platform,
			record, MuiAppMessageField.MouseY, 12));
		Assert.True(MuiAppMessageFieldCursorCodec.TryWriteUInt32(ref platform,
			record, MuiAppMessageField.Reserved7, 0xdeadbeef));
		Assert.True(MuiAppMessageRecordCodec.TryRead(ref platform, record,
			out var decoded));
		Assert.Equal((ushort)8, decoded.Type);
		Assert.Equal(0x55u, decoded.UserData);
		Assert.Equal(-2, decoded.NumberOfArguments);
		Assert.Equal(0x3500u, decoded.ArgumentList.Raw);
		Assert.Equal((short)-4, decoded.MouseX);
		Assert.Equal((short)12, decoded.MouseY);
		Assert.Equal(0xdeadbeefu, decoded.Reserved7);
		cursor.Field = (MuiAppMessageField)255;
		Assert.False(MuiAppMessageFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
		cursor.Record = APTR.FromPointer(0xfffffff0u);
		cursor.Field = MuiAppMessageField.Reserved7;
		Assert.False(MuiAppMessageFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
	}

	[Fact]
	public void ApplicationMessageArgumentAddressUsesNamedCursorBoundary()
	{
		var platform = CreatePlatform(out _);
		var cursor = new MuiWorkbenchArgumentVectorCursor
		{
			Base = APTR.FromPointer(0x1800),
			Index = 2,
		};

		Assert.True(MuiWorkbenchArgumentVectorCodec.TryGetEntry(ref platform,
			cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x1810), address);
		cursor.Base = APTR.FromPointer(0x20FFC);
		cursor.Index = 0;
		Assert.False(MuiWorkbenchArgumentVectorCodec.TryGetEntry(ref platform,
			cursor, out _));
	}

	[Fact]
	public void WorkbenchArgumentFieldCursorUsesNamedBoundary()
	{
		var platform = CreatePlatform(out _);
		var record = APTR.FromPointer(0x1f00);
		var cursor = default(MuiWorkbenchArgumentFieldCursor);
		cursor.Record = record;
		cursor.Field = MuiWorkbenchArgumentField.Lock;
		Assert.True(MuiWorkbenchArgumentFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var lockAddress));
		Assert.Equal(record.Raw, lockAddress.Raw);
		cursor.Field = MuiWorkbenchArgumentField.Name;
		Assert.True(MuiWorkbenchArgumentFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var nameAddress));
		Assert.Equal(record.Raw + 4, nameAddress.Raw);
		Assert.True(MuiWorkbenchArgumentFieldCursorCodec.TryWrite(ref platform,
			record, MuiWorkbenchArgumentField.Lock, 0x1200));
		Assert.True(MuiWorkbenchArgumentFieldCursorCodec.TryWrite(ref platform,
			record, MuiWorkbenchArgumentField.Name, 0x1300));
		Assert.True(MuiWorkbenchArgumentRecordCodec.TryRead(ref platform, record,
			out var decoded));
		Assert.Equal(0x1200u, decoded.Lock.Raw);
		Assert.Equal(0x1300u, decoded.Name.Raw);
		cursor.Field = (MuiWorkbenchArgumentField)255;
		Assert.False(MuiWorkbenchArgumentFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
		cursor.Record = APTR.FromPointer(0xfffffff0u);
		cursor.Field = MuiWorkbenchArgumentField.Name;
		Assert.False(MuiWorkbenchArgumentFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void WindowWindowGetterTracksNativeWindowLifetime()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Window, out var beforeOpen));
		Assert.Equal(0u, beforeOpen);
		Assert.False(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Window, 0x90000000, false));

		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Window, out var openWindow));
		Assert.Equal(0x1820u, openWindow);

		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Window, out var afterClose));
		Assert.Equal(0u, afterClose);
	}

	[Fact]
	public void WindowLifecycleGettersProjectNamedState()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Window, out var beforeWindow));
		Assert.Equal(0u, beforeWindow);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Open, out var beforeOpen));
		Assert.Equal(0u, beforeOpen);

		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0x1200));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Window, out var nativeWindow));
		Assert.Equal(0x1820u, nativeWindow);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Open, out var open));
		Assert.Equal(1u, open);
		Assert.True(MuiApplicationWindowCore.TryGetWindowLifecycleState(
			ref platform, State, window, out var lifecycle));
		Assert.Equal(nativeWindow, lifecycle.NativeWindow.Raw);
		Assert.Equal(open, lifecycle.Open);

		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Window, out nativeWindow));
		Assert.Equal(0u, nativeWindow);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Open, out open));
		Assert.Equal(0u, open);
	}

	[Fact]
	public void WindowIdUsesNamedMutableStateAndSetPackets()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Id, out var before));
		Assert.Equal(0u, before);

		var packet = APTR.FromPointer(0x2E40);
		platform.WriteUInt32(packet, 0, MuiCommonControlPacketCore.Set);
		platform.WriteUInt32(packet, 4, MuiWindowPublicCore.Id);
		platform.WriteUInt32(packet, 8, 0x4D554931);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowId(ref platform,
			State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Id, out var first));
		Assert.Equal(0x4D554931u, first);

		platform.WriteUInt32(packet, 0, MuiCommonControlPacketCore.NoNotifySet);
		platform.WriteUInt32(packet, 8, 0x4D554932);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowId(ref platform,
			State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Id, out var second));
		Assert.Equal(0x4D554932u, second);
	}

	[Fact]
	public void PushedMethodsAndTimedHandlersUseCopiedBoundedGuestRecords()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var target = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0x20));
		var parameters = APTR.FromPointer(0x1200);
		Assert.Equal(0u, MuiApplicationWindowCore.PushMethod(ref platform, State,
			application, target, 8, parameters));
		platform.WriteUInt32(parameters, 0, 0x90000001);
		platform.WriteUInt32(parameters, 4, 77);
		var firstId = MuiApplicationWindowCore.PushMethod(ref platform, State,
			application, target, 2, parameters);
		Assert.NotEqual(0u, firstId);
		platform.WriteUInt32(parameters, 4, 99);
		Assert.Equal(1u, MuiApplicationWindowCore.DispatchPushedMethod(ref platform,
			State, application));
		Assert.Equal(77u, platform.LastDispatchArgument);
		var secondId = MuiApplicationWindowCore.PushMethod(ref platform, State,
			application, target, 2, parameters);
		Assert.NotEqual(0u, secondId);
		var duplicateId = MuiApplicationWindowCore.PushMethod(ref platform, State,
			application, target, 2, parameters);
		Assert.NotEqual(0u, duplicateId);
		Assert.NotEqual(secondId, duplicateId);
		var otherTarget = Object(ref platform, cl);
		var thirdId = MuiApplicationWindowCore.PushMethod(ref platform, State,
			application, otherTarget, 2, parameters);
		Assert.NotEqual(0u, thirdId);
		var packetTarget = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1400);
		platform.WriteUInt32(packet, 0, 0x80429EF8);
		platform.WriteUInt32(packet, 4, packetTarget.Raw);
		platform.WriteUInt32(packet, 8, 1);
		platform.WriteUInt32(packet, 12, 0x90000003);
		var packetId = MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet);
		Assert.NotEqual(0u, packetId);
		platform.WriteUInt32(packet, 0, 0x804211DD);
		platform.WriteUInt32(packet, 8, packetId);
		platform.WriteUInt32(packet, 12, 0);
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			application, packet));
		Assert.Equal(2u, MuiApplicationWindowCore.UnpushMethod(ref platform, State,
			application, target, 0, 0x90000001));
		Assert.Equal(1u, MuiApplicationWindowCore.UnpushMethod(ref platform, State,
			application, APTR.Null, 0, 0));
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchPushedMethod(ref platform,
			State, application));

		var handler = APTR.FromPointer(0x1300);
		platform.WriteUInt32(handler, 8, target.Raw);
		platform.WriteUInt32(handler, 12, 10u << 16);
		platform.WriteUInt32(handler, 20, 0x90000002);
		Assert.True(MuiApplicationWindowCore.AddInputHandler(ref platform, State,
			application, handler));
		platform.Ticks = 9;
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchInputHandlers(ref platform,
			State, application, 0));
		platform.Ticks = 10;
		Assert.Equal(1u, MuiApplicationWindowCore.DispatchInputHandlers(ref platform,
			State, application, 0));
		Assert.Equal(0x90000002u, platform.LastDispatchMethod);
	}

	[Fact]
	public void EventPollingSetsCloseRequestAndDispatchesHandlers()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var window = Object(ref platform, cl);
		var target = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0x20));
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, window));
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State, window,
			0x200));
		var handler = APTR.FromPointer(0x1300);
		platform.WriteUInt16(handler, 10, 0x0002);
		platform.WriteUInt32(handler, 12, target.Raw);
		platform.WriteUInt32(handler, 20, 0x200);
		Assert.True(MuiApplicationWindowCore.AddEventHandler(ref platform, State,
			window, handler));
		platform.PendingWindowEvent = 0x200;
		var eventStorage = APTR.FromPointer(0x1400);
		Assert.Equal(1u, MuiApplicationWindowCore.PollWindowEvents(ref platform,
			State, application, eventStorage));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.CloseRequest, out var closeRequest));
		Assert.Equal(1u, closeRequest);
	}

	[Fact]
	public void WindowInputEventPublishesNamedCallerOwnedRecordPointer()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var window = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0x20));
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, window));
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0x200));
		var eventStorage = APTR.FromPointer(0x1480);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.InputEvent, out var initial));
		Assert.Equal(0u, initial);

		platform.PendingWindowEvent = 0x200;
		Assert.Equal(0u, MuiApplicationWindowCore.PollWindowEvents(ref platform,
			State, application, eventStorage));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.InputEvent, out var current));
		Assert.Equal(eventStorage.Raw, current);

		Assert.False(MuiApplicationWindowCore.PublishWindowInputEventValue(
			ref platform, State, window, APTR.FromPointer(0x7FFF0)));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.InputEvent, out current));
		Assert.Equal(eventStorage.Raw, current);
		Assert.True(MuiApplicationWindowCore.PublishWindowInputEventValue(
			ref platform, State, window, APTR.Null));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.InputEvent, out current));
		Assert.Equal(0u, current);
		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
	}

	[Fact]
	public void ObsoleteWindowMenuInitializerAliasesMenustripAndNoMenu()
	{
		var platform = CreatePlatform(out var windowClass);
		var name = APTR.FromPointer(0x14C0);
		platform.WriteCString(name, "Menustrip.mui");
		var stripClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(2), false);
		var strip = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			stripClass, APTR.Null);
		Assert.True(MuiMenuSpecialistCore.Attach(ref platform, State, strip,
			MuiMenuSpecialistClass.Menustrip).IsNotNull);

		var tags = APTR.FromPointer(0x1500);
		platform.WriteUInt32(tags, 0, MuiWindowPublicCore.Menu);
		platform.WriteUInt32(tags, 4, strip.Raw);
		platform.WriteUInt32(tags, 8, 0);
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			windowClass, tags);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Menustrip, out var stored));
		Assert.Equal(strip.Raw, stored);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Menu, out stored));
		Assert.Equal(strip.Raw, stored);
		Assert.True(MuiWindowPublicCore.TryGet(ref platform, State, window,
			MuiWindowPublicCore.Menu, out stored, out var menuHandled));
		Assert.True(menuHandled);
		Assert.Equal(strip.Raw, stored);
		Assert.Equal(window, MuiHeadlessObjectCore.ParentObject(ref platform,
			State, strip));

		Assert.False(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Menu, 0, false));

		var noMenuTags = APTR.FromPointer(0x1540);
		platform.WriteUInt32(noMenuTags, 0, MuiWindowPublicCore.Menu);
		platform.WriteUInt32(noMenuTags, 4, uint.MaxValue);
		platform.WriteUInt32(noMenuTags, 8, 0);
		var noMenuWindow = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			State, windowClass, noMenuTags);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			noMenuWindow, MuiWindowPublicCore.Menustrip, out stored));
		Assert.Equal(0u, stored);
	}

	[Fact]
	public void WindowCloseRequestCanonicalizesSetPackets()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1500);
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, MuiWindowPublicCore.CloseRequest, 7));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowCloseRequest(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.CloseRequest, out var requested));
		Assert.Equal(1u, requested);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, MuiWindowPublicCore.CloseRequest,
			0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowCloseRequest(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.CloseRequest, out var cleared));
		Assert.Equal(0u, cleared);
	}

	[Fact]
	public void WindowRootObjectUsesGuestFamilyOwnership()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var first = Object(ref platform, cl);
		var second = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1600);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.RootObject, out var initial));
		Assert.Equal(0u, initial);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, MuiWindowPublicCore.RootObject,
			first.Raw));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowRootObject(
			ref platform, State, window, packet));
		Assert.Equal(first, MuiFamilyCore.GetChild(ref platform, State, window,
			0, APTR.Null));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.RootObject, out var firstValue));
		Assert.Equal(first.Raw, firstValue);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, MuiWindowPublicCore.RootObject,
			second.Raw));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowRootObject(
			ref platform, State, window, packet));
		Assert.Equal(second, MuiFamilyCore.GetChild(ref platform, State, window,
			0, APTR.Null));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.RootObject, out var secondValue));
		Assert.Equal(second.Raw, secondValue);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, MuiWindowPublicCore.RootObject, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowRootObject(
			ref platform, State, window, packet));
		Assert.Equal(APTR.Null, MuiFamilyCore.GetChild(ref platform, State, window,
			0, APTR.Null));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.RootObject, out var cleared));
		Assert.Equal(0u, cleared);
	}

	[Fact]
	public void WindowNoMenusCanonicalizesSetPackets()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1700);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.NoMenus, out var initial));
		Assert.Equal(0u, initial);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, MuiWindowPublicCore.NoMenus, 9));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowNoMenus(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.NoMenus, out var disabled));
		Assert.Equal(1u, disabled);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, MuiWindowPublicCore.NoMenus, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowNoMenus(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.NoMenus, out var enabled));
		Assert.Equal(0u, enabled);
	}

	[Fact]
	public void WindowHasAlphaCanonicalizesSetPackets()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1780);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.HasAlpha, out var initial));
		Assert.Equal(0u, initial);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, MuiWindowPublicCore.HasAlpha, 7));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowHasAlpha(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.HasAlpha, out var enabled));
		Assert.Equal(1u, enabled);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, MuiWindowPublicCore.HasAlpha, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowHasAlpha(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.HasAlpha, out var disabled));
		Assert.Equal(0u, disabled);
	}

	[Fact]
	public void WindowOpacityAcceptsBoundedValuesAndRejectsMalformedWrites()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x17C0);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Opacity, out var initial));
		Assert.Equal(0u, initial);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, MuiWindowPublicCore.Opacity, 128));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowOpacity(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Opacity, out var middle));
		Assert.Equal(128u, middle);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, MuiWindowPublicCore.Opacity, 256));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowOpacity(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Opacity, out var unchanged));
		Assert.Equal(128u, unchanged);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, MuiWindowPublicCore.Opacity, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowOpacity(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Opacity, out var cleared));
		Assert.Equal(0u, cleared);
	}

	[Fact]
	public void WindowTitleRetainsCallerOwnedGuestString()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1800);
		var title = APTR.FromPointer(0x1900);
		platform.WriteCString(title, "Main window");
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Title, out var initial));
		Assert.Equal(0u, initial);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, MuiWindowPublicCore.Title, title.Raw));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowTitle(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Title, out var stored));
		Assert.Equal(title.Raw, stored);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, MuiWindowPublicCore.Title,
			0x50FFF));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowTitle(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Title, out var unchanged));
		Assert.Equal(title.Raw, unchanged);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, MuiWindowPublicCore.Title, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowTitle(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Title, out var cleared));
		Assert.Equal(0u, cleared);
	}

	[Fact]
	public void WindowScreenSelectsExplicitScreenAndHidesItWhileClosed()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x18C0);
		var screen = APTR.FromPointer(0x1A00);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Screen, out var initial));
		Assert.Equal(0u, initial);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, MuiWindowPublicCore.Screen, screen.Raw));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowScreen(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Screen, out var closed));
		Assert.Equal(0u, closed);

		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Screen, out var open));
		Assert.Equal(screen.Raw, open);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, MuiWindowPublicCore.Screen,
			0x50FFF));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowScreen(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Screen, out var unchanged));
		Assert.Equal(screen.Raw, unchanged);

		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Screen, out var afterClose));
		Assert.Equal(0u, afterClose);
	}

	[Fact]
	public void WindowRefWindowRetainsLiveTargetAndRejectsInvalidPointers()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var reference = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x18E0);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.RefWindow, out var initial));
		Assert.Equal(0u, initial);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, MuiWindowPublicCore.RefWindow,
			reference.Raw));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowRefWindow(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.RefWindow, out var stored));
		Assert.Equal(reference.Raw, stored);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, MuiWindowPublicCore.RefWindow,
			window.Raw));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowRefWindow(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.RefWindow, out var unchanged));
		Assert.Equal(reference.Raw, unchanged);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, MuiWindowPublicCore.RefWindow,
			0x50FFF));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowRefWindow(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.RefWindow, out var stillStored));
		Assert.Equal(reference.Raw, stillStored);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, MuiWindowPublicCore.RefWindow, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowRefWindow(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.RefWindow, out var cleared));
		Assert.Equal(0u, cleared);
	}

	[Fact]
	public void WindowVisibleOnMaximizeCanonicalizesSetPackets()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x18F0);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.VisibleOnMaximize, out var initial));
		Assert.Equal(0u, initial);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set,
			MuiWindowPublicCore.VisibleOnMaximize, 7));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowVisibleOnMaximize(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.VisibleOnMaximize, out var enabled));
		Assert.Equal(1u, enabled);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet,
			MuiWindowPublicCore.VisibleOnMaximize, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowVisibleOnMaximize(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.VisibleOnMaximize, out var disabled));
		Assert.Equal(0u, disabled);
	}

	[Fact]
	public void WindowIsSubWindowIsInitializerOnlyAndSurvivesOwnerDisposal()
	{
		var platform = CreatePlatform(out var cl);
		var tags = APTR.FromPointer(0x1A00);
		Assert.True(platform.IsMapped(tags, 24));
		platform.WriteUInt32(tags, 0, MuiWindowPublicCore.IsSubWindow);
		platform.WriteUInt32(tags, 4, 7);
		platform.WriteUInt32(tags, 8, 0);
		var subWindow = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			cl, tags);
		var ordinaryWindow = Object(ref platform, cl);
		var application = Object(ref platform, cl);
		Assert.NotEqual(APTR.Null, subWindow);
		Assert.NotEqual(APTR.Null, ordinaryWindow);
		Assert.NotEqual(APTR.Null, application);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			subWindow, MuiWindowPublicCore.IsSubWindow, out var initial));
		Assert.Equal(1u, initial);

		var packet = APTR.FromPointer(0x1A40);
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.NoNotifySet,
			MuiWindowPublicCore.IsSubWindow, 0));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowIsSubWindow(
			ref platform, State, subWindow, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			subWindow, MuiWindowPublicCore.IsSubWindow, out var unchanged));
		Assert.Equal(1u, unchanged);

		Assert.True(MuiFamilyCore.AddTail(ref platform, State, application,
			subWindow));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, application,
			ordinaryWindow));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			application));
		Assert.NotEqual(APTR.Null, MuiHeadlessObjectCore.FindObject(ref platform,
			State, subWindow));
		Assert.Equal(APTR.Null, MuiHeadlessObjectCore.FindObject(ref platform,
			State, ordinaryWindow));
		Assert.Equal(APTR.Null, MuiFamilyCore.GetChild(ref platform, State,
			application, 0, APTR.Null));

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			subWindow));
	}

	[Fact]
	public void WindowTabletMessagesIsInitializerOnlyAndForwardsOnOpen()
	{
		var platform = CreatePlatform(out var cl);
		var tags = APTR.FromPointer(0x1B00);
		platform.WriteUInt32(tags, 0, MuiWindowPublicCore.TabletMessages);
		platform.WriteUInt32(tags, 4, 7);
		platform.WriteUInt32(tags, 8, 0);
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			cl, tags);
		Assert.NotEqual(APTR.Null, window);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.TabletMessages, out var initial));
		Assert.Equal(1u, initial);

		var packet = APTR.FromPointer(0x1B40);
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.NoNotifySet,
			MuiWindowPublicCore.TabletMessages, 0));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowTabletMessages(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.TabletMessages, out var unchanged));
		Assert.Equal(1u, unchanged);

		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.Equal(1u, platform.WindowTabletMessagesOperationCount);
		Assert.True(platform.WindowTabletMessages);
		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
	}

	[Fact]
	public void WindowBorderScrollersAreMutableAndForwardAsNamedState()
	{
		var platform = CreatePlatform(out var cl);
		var tags = APTR.FromPointer(0x1C00);
		platform.WriteUInt32(tags, 0, MuiWindowPublicCore.UseBottomBorderScroller);
		platform.WriteUInt32(tags, 4, 7);
		platform.WriteUInt32(tags, 8, MuiWindowPublicCore.UseLeftBorderScroller);
		platform.WriteUInt32(tags, 12, 0);
		platform.WriteUInt32(tags, 16, MuiWindowPublicCore.UseRightBorderScroller);
		platform.WriteUInt32(tags, 20, 9);
		platform.WriteUInt32(tags, 24, 0);
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			cl, tags);
		Assert.NotEqual(APTR.Null, window);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.UseBottomBorderScroller, out var bottom));
		Assert.Equal(1u, bottom);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.UseLeftBorderScroller, out var left));
		Assert.Equal(0u, left);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.UseRightBorderScroller, out var right));
		Assert.Equal(1u, right);

		var packet = APTR.FromPointer(0x1C40);
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.NoNotifySet,
			MuiWindowPublicCore.UseLeftBorderScroller, 7));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowUseLeftBorderScroller(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.UseLeftBorderScroller, out var unchanged));
		Assert.Equal(1u, unchanged);
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.NoNotifySet,
			MuiWindowPublicCore.UseLeftBorderScroller, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowUseLeftBorderScroller(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.UseLeftBorderScroller, out unchanged));
		Assert.Equal(0u, unchanged);

		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.Equal(1u, platform.WindowBorderScrollerOperationCount);
		Assert.True(platform.WindowUseBottomBorderScroller);
		Assert.False(platform.WindowUseLeftBorderScroller);
		Assert.True(platform.WindowUseRightBorderScroller);
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.NoNotifySet,
			MuiWindowPublicCore.UseRightBorderScroller, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowUseRightBorderScroller(
			ref platform, State, window, packet));
		Assert.False(platform.WindowUseRightBorderScroller);
		Assert.Equal(2u, platform.WindowBorderScrollerOperationCount);
		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
	}

	[Fact]
	public void WindowAlternateGeometryIsInitializerOnlyAndForwardsAsRecord()
	{
		var platform = CreatePlatform(out var cl);
		var tags = APTR.FromPointer(0x1D00);
		platform.WriteUInt32(tags, 0, MuiWindowPublicCore.AltHeight);
		platform.WriteUInt32(tags, 4, unchecked((uint)-1));
		platform.WriteUInt32(tags, 8, MuiWindowPublicCore.AltWidth);
		platform.WriteUInt32(tags, 12, 640);
		platform.WriteUInt32(tags, 16, MuiWindowPublicCore.AltLeftEdge);
		platform.WriteUInt32(tags, 20, unchecked((uint)-16));
		platform.WriteUInt32(tags, 24, MuiWindowPublicCore.AltTopEdge);
		platform.WriteUInt32(tags, 28, 24);
		platform.WriteUInt32(tags, 32, 0);
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			cl, tags);
		Assert.NotEqual(APTR.Null, window);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.AltHeight, out var height));
		Assert.Equal(unchecked((uint)-1), height);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.AltWidth, out var width));
		Assert.Equal(640u, width);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.AltLeftEdge, out var left));
		Assert.Equal(unchecked((uint)-16), left);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.AltTopEdge, out var top));
		Assert.Equal(24u, top);

		var packet = APTR.FromPointer(0x1D50);
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.NoNotifySet,
			MuiWindowPublicCore.AltWidth, 800));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowAlternateGeometry(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.AltWidth, out var unchanged));
		Assert.Equal(640u, unchanged);

		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.Equal(1u, platform.WindowAlternateGeometryOperationCount);
		Assert.Equal(-1, platform.WindowAlternateGeometry.Height);
		Assert.Equal(640, platform.WindowAlternateGeometry.Width);
		Assert.Equal(-16, platform.WindowAlternateGeometry.LeftEdge);
		Assert.Equal(24, platform.WindowAlternateGeometry.TopEdge);
		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
	}

	[Fact]
	public void WindowGeometryIsInitializerOnlyAndForwardsAsRecord()
	{
		var platform = CreatePlatform(out var cl);
		var tags = APTR.FromPointer(0x1E00);
		platform.WriteUInt32(tags, 0, MuiWindowPublicCore.Height);
		platform.WriteUInt32(tags, 4, 240);
		platform.WriteUInt32(tags, 8, MuiWindowPublicCore.Width);
		platform.WriteUInt32(tags, 12, 320);
		platform.WriteUInt32(tags, 16, MuiWindowPublicCore.LeftEdge);
		platform.WriteUInt32(tags, 20, unchecked((uint)-2));
		platform.WriteUInt32(tags, 24, MuiWindowPublicCore.TopEdge);
		platform.WriteUInt32(tags, 28, 8);
		platform.WriteUInt32(tags, 32, 0);
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			cl, tags);
		Assert.NotEqual(APTR.Null, window);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Height, out var height));
		Assert.Equal(240u, height);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Width, out var width));
		Assert.Equal(320u, width);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.LeftEdge, out var left));
		Assert.Equal(unchecked((uint)-2), left);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.TopEdge, out var top));
		Assert.Equal(8u, top);

		var packet = APTR.FromPointer(0x1E50);
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.NoNotifySet,
			MuiWindowPublicCore.Width, 900));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowGeometry(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Width, out var unchanged));
		Assert.Equal(320u, unchanged);

		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.Equal(1u, platform.WindowGeometryOperationCount);
		Assert.Equal(240, platform.WindowGeometry.Height);
		Assert.Equal(320, platform.WindowGeometry.Width);
		Assert.Equal(-2, platform.WindowGeometry.LeftEdge);
		Assert.Equal(8, platform.WindowGeometry.TopEdge);
		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
	}

	[Fact]
	public void WindowGadgetPolicyIsInitializerOnlyAndForwardsAsRecord()
	{
		var platform = CreatePlatform(out var cl);
		var tags = APTR.FromPointer(0x1F00);
		platform.WriteUInt32(tags, 0, MuiWindowPublicCore.CloseGadget);
		platform.WriteUInt32(tags, 4, 7);
		platform.WriteUInt32(tags, 8, MuiWindowPublicCore.DepthGadget);
		platform.WriteUInt32(tags, 12, 0);
		platform.WriteUInt32(tags, 16, MuiWindowPublicCore.DragBar);
		platform.WriteUInt32(tags, 20, 9);
		platform.WriteUInt32(tags, 24, MuiWindowPublicCore.SizeGadget);
		platform.WriteUInt32(tags, 28, 1);
		platform.WriteUInt32(tags, 32, MuiWindowPublicCore.SizeRight);
		platform.WriteUInt32(tags, 36, 0);
		platform.WriteUInt32(tags, 40, 0);
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			cl, tags);
		Assert.NotEqual(APTR.Null, window);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.CloseGadget, out var closeGadget));
		Assert.Equal(1u, closeGadget);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.DepthGadget, out var depthGadget));
		Assert.Equal(0u, depthGadget);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.DragBar, out var dragBar));
		Assert.Equal(1u, dragBar);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.SizeGadget, out var sizeGadget));
		Assert.Equal(1u, sizeGadget);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.SizeRight, out var sizeRight));
		Assert.Equal(0u, sizeRight);

		var packet = APTR.FromPointer(0x1F70);
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.NoNotifySet,
			MuiWindowPublicCore.DragBar, 0));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowGadgetPolicy(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.DragBar, out var unchanged));
		Assert.Equal(1u, unchanged);

		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.Equal(1u, platform.WindowGadgetPolicyOperationCount);
		Assert.Equal(1u, platform.WindowGadgetPolicy.CloseGadget);
		Assert.Equal(0u, platform.WindowGadgetPolicy.DepthGadget);
		Assert.Equal(1u, platform.WindowGadgetPolicy.DragBar);
		Assert.Equal(1u, platform.WindowGadgetPolicy.SizeGadget);
		Assert.Equal(0u, platform.WindowGadgetPolicy.SizeRight);
		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
	}

	[Fact]
	public void WindowModePolicyIsInitializerOnlyAndForwardsAsRecord()
	{
		var platform = CreatePlatform(out var cl);
		var tags = APTR.FromPointer(0x2000);
		platform.WriteUInt32(tags, 0, MuiWindowPublicCore.AppWindow);
		platform.WriteUInt32(tags, 4, 7);
		platform.WriteUInt32(tags, 8, MuiWindowPublicCore.Backdrop);
		platform.WriteUInt32(tags, 12, 0);
		platform.WriteUInt32(tags, 16, MuiWindowPublicCore.Borderless);
		platform.WriteUInt32(tags, 20, 9);
		platform.WriteUInt32(tags, 24, MuiWindowPublicCore.PanelWindow);
		platform.WriteUInt32(tags, 28, 1);
		platform.WriteUInt32(tags, 32, 0);
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			cl, tags);
		Assert.NotEqual(APTR.Null, window);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.AppWindow, out var appWindow));
		Assert.Equal(1u, appWindow);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Backdrop, out var backdrop));
		Assert.Equal(0u, backdrop);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Borderless, out var borderless));
		Assert.Equal(1u, borderless);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.PanelWindow, out var panelWindow));
		Assert.Equal(1u, panelWindow);

		var packet = APTR.FromPointer(0x2070);
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.NoNotifySet,
			MuiWindowPublicCore.Borderless, 0));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowModePolicy(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.Borderless, out var unchanged));
		Assert.Equal(1u, unchanged);

		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.Equal(1u, platform.WindowModePolicyOperationCount);
		Assert.Equal(1u, platform.WindowModePolicy.AppWindow);
		Assert.Equal(0u, platform.WindowModePolicy.Backdrop);
		Assert.Equal(1u, platform.WindowModePolicy.Borderless);
		Assert.Equal(1u, platform.WindowModePolicy.PanelWindow);
		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
	}

	[Fact]
	public void WindowMenustripUsesOwnedFamilyRelationship()
	{
		var platform = CreatePlatform(out var windowClass);
		var window = Object(ref platform, windowClass);
		var root = Object(ref platform, windowClass);
		var name = APTR.FromPointer(0x1200);
		platform.WriteCString(name, "Menustrip.mui");
		var stripClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(2), false);
		var strip = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			stripClass, APTR.Null);
		var replacement = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			stripClass, APTR.Null);
		Assert.True(MuiMenuSpecialistCore.Attach(ref platform, State, strip,
			MuiMenuSpecialistClass.Menustrip).IsNotNull);
		Assert.True(MuiMenuSpecialistCore.Attach(ref platform, State, replacement,
			MuiMenuSpecialistClass.Menustrip).IsNotNull);

		var packet = APTR.FromPointer(0x1240);
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, MuiWindowPublicCore.Menustrip,
			strip.Raw));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowMenustrip(
			ref platform, State, window, packet));
		Assert.Equal(window, MuiHeadlessObjectCore.ParentObject(ref platform, State,
			strip));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Menustrip, out var stored));
		Assert.Equal(strip.Raw, stored);
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, MuiWindowPublicCore.RootObject,
			root.Raw));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowRootObject(
			ref platform, State, window, packet));
		Assert.Equal(strip.Raw, MuiFamilyCore.GetChild(ref platform, State, window,
			0, APTR.Null).Raw);
		Assert.Equal(root.Raw, MuiFamilyCore.GetChild(ref platform, State, window,
			1, APTR.Null).Raw);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Menustrip, out stored));
		Assert.Equal(strip.Raw, stored);

		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, MuiWindowPublicCore.Menustrip,
			replacement.Raw));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowMenustrip(
			ref platform, State, window, packet));
		Assert.Equal(APTR.Null, MuiHeadlessObjectCore.ParentObject(ref platform,
			State, strip));
		Assert.Equal(window, MuiHeadlessObjectCore.ParentObject(ref platform, State,
			replacement));

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, MuiWindowPublicCore.Menustrip, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowMenustrip(
			ref platform, State, window, packet));
		Assert.Equal(APTR.Null, MuiHeadlessObjectCore.ParentObject(ref platform,
			State, replacement));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Menustrip, out stored));
		Assert.Equal(0u, stored);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.RootObject, out stored));
		Assert.Equal(root.Raw, stored);
		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
	}

	[Fact]
	public void WindowFancyDrawingPreservesObsoleteCompatibilityState()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1280);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.FancyDrawing, out var initial));
		Assert.Equal(0u, initial);
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, MuiWindowPublicCore.FancyDrawing, 7));
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.FancyDrawing, out var enabled));
		Assert.Equal(1u, enabled);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet,
			MuiWindowPublicCore.FancyDrawing, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowFancyDrawing(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.FancyDrawing, out var disabled));
		Assert.Equal(0u, disabled);
	}

	[Fact]
	public void WindowMenuActionUsesNamedEventStateAndPublicationHelper()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x12C0);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.MenuAction, out var initial));
		Assert.Equal(0u, initial);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, MuiWindowPublicCore.MenuAction,
			0x12345678));
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.MenuAction, out var stored));
		Assert.Equal(0x12345678u, stored);

		Assert.True(MuiApplicationWindowCore.SetWindowMenuActionValue(
			ref platform, State, window, 0xCAFEBABEu));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.MenuAction, out stored));
		Assert.Equal(0xCAFEBABEu, stored);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, MuiWindowPublicCore.MenuAction,
			0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowMenuAction(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.MenuAction, out stored));
		Assert.Equal(0u, stored);
	}

	[Fact]
	public void WindowMouseObjectUsesInitializerFlagAndGetterPublicationSeam()
	{
		var platform = CreatePlatform(out var cl);
		var tags = APTR.FromPointer(0x1300);
		platform.WriteUInt32(tags, 0, MuiWindowPublicCore.NeedsMouseObject);
		platform.WriteUInt32(tags, 4, 7);
		platform.WriteUInt32(tags, 8, 0);
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			tags);
		var target = Object(ref platform, cl);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.NeedsMouseObject, out var needs));
		Assert.Equal(1u, needs);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.MouseObject, out var initial));
		Assert.Equal(0u, initial);

		var packet = APTR.FromPointer(0x1340);
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet,
			MuiWindowPublicCore.NeedsMouseObject, 0));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowNeedsMouseObject(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.NeedsMouseObject, out needs));
		Assert.Equal(1u, needs);

		Assert.True(MuiApplicationWindowCore.PublishWindowMouseObjectValue(
			ref platform, State, window, target));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.MouseObject, out var current));
		Assert.Equal(target.Raw, current);
		Assert.False(MuiApplicationWindowCore.PublishWindowMouseObjectValue(
			ref platform, State, window, APTR.FromPointer(0x7FFF0)));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.MouseObject, out current));
		Assert.Equal(target.Raw, current);
		Assert.True(MuiApplicationWindowCore.PublishWindowMouseObjectValue(
			ref platform, State, window, APTR.Null));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.MouseObject, out current));
		Assert.Equal(0u, current);
	}

	[Fact]
	public void WindowDisableKeysUsesNamedKeyboardStateAndFocusedPacketRoute()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1360);
		var keyboard = default(MuiWindowPublicCore.MuiWindowKeyboardState);
		Assert.Equal(0u, keyboard.DisableKeys);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.DisableKeys, out var initial));
		Assert.Equal(0u, initial);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, MuiWindowPublicCore.DisableKeys,
			1u << 2));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowDisableKeys(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.DisableKeys, out var masked));
		Assert.Equal(1u << 2, masked);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, MuiWindowPublicCore.DisableKeys,
			0xA5A5u));
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.DisableKeys, out var restored));
		Assert.Equal(0xA5A5u, restored);
	}

	[Fact]
	public void WindowOpenAttributeUsesTypedLifecycleAndNamedGetterState()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1380);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Open, out var initial));
		Assert.Equal(0u, initial);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, MuiWindowPublicCore.Open, 7));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowOpen(
			ref platform, State, window, packet));
		Assert.Equal(1u, platform.WindowOpenCount);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Open, out var open));
		Assert.Equal(1u, open);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet, MuiWindowPublicCore.Open, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowOpen(
			ref platform, State, window, packet));
		Assert.Equal(1u, platform.WindowCloseCount);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.Open, out open));
		Assert.Equal(0u, open);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, MuiWindowPublicCore.Open, 1));
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.Equal(2u, platform.WindowOpenCount);
		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
	}

	[Fact]
	public void WindowScreenTitleRetainsCallerOwnedGuestString()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1840);
		var title = APTR.FromPointer(0x1940);
		platform.WriteCString(title, "CopperOS");
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.ScreenTitle, out var initial));
		Assert.Equal(0u, initial);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, MuiWindowPublicCore.ScreenTitle,
			title.Raw));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowScreenTitle(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.ScreenTitle, out var stored));
		Assert.Equal(title.Raw, stored);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet,
			MuiWindowPublicCore.ScreenTitle, 0x50FFF));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowScreenTitle(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.ScreenTitle, out var unchanged));
		Assert.Equal(title.Raw, unchanged);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, MuiWindowPublicCore.ScreenTitle, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowScreenTitle(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.ScreenTitle, out var cleared));
		Assert.Equal(0u, cleared);
	}

	[Fact]
	public void WindowPublicScreenRetainsCallerOwnedGuestString()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1880);
		var screenName = APTR.FromPointer(0x1980);
		platform.WriteCString(screenName, "Workbench");
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.PublicScreen, out var initial));
		Assert.Equal(0u, initial);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, MuiWindowPublicCore.PublicScreen,
			screenName.Raw));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowPublicScreen(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.PublicScreen, out var stored));
		Assert.Equal(screenName.Raw, stored);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.NoNotifySet,
			MuiWindowPublicCore.PublicScreen, 0x50FFF));
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowPublicScreen(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.PublicScreen, out var unchanged));
		Assert.Equal(screenName.Raw, unchanged);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform, packet,
			MuiCommonControlPacketCore.Set, MuiWindowPublicCore.PublicScreen, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowPublicScreen(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.PublicScreen, out var cleared));
		Assert.Equal(0u, cleared);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR cl)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Application.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR Object(ref MuiHeadlessTestPlatform platform, APTR cl) =>
		MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl, APTR.Null);
}
