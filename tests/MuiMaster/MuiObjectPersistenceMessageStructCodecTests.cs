/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiObjectPersistenceMessageStructCodecTests
{
	[Fact]
	public void ExportAndImportPacketsRoundTripThroughSequentialRecord()
	{
		Assert.Equal(8, Unsafe.SizeOf<MuiObjectPersistenceMessage>());
		Assert.Equal(4, Unsafe.SizeOf<MuiObjectPersistenceMethodMessage>());

		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2400);
		var dataspace = APTR.FromPointer(0x3000);

		Assert.True(MuiObjectPersistenceMessageStructCodec.TryWrite(ref platform,
			address, MuiObjectPersistenceMessageCodec.ExportMethod, dataspace));
		Assert.True(MuiObjectPersistenceMessageStructCodec.TryRead(ref platform,
			address, out var export));
		Assert.Equal(MuiObjectPersistenceMessageCodec.ExportMethod,
			export.MethodId);
		Assert.Equal(dataspace, export.Dataspace);

		Assert.True(MuiObjectPersistenceMessageStructCodec.TryWrite(ref platform,
			address, MuiObjectPersistenceMessageCodec.ImportMethod, dataspace));
		Assert.True(MuiObjectPersistenceMessageStructCodec.TryRead(ref platform,
			address, out var import));
		Assert.Equal(MuiObjectPersistenceMessageCodec.ImportMethod,
			import.MethodId);
		Assert.Equal(dataspace, import.Dataspace);
	}

	[Fact]
	public void ExportAndImportPacketsRejectIncompleteGuestRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var crossingEnd = APTR.FromPointer(0x20FFD);
		Assert.False(MuiObjectPersistenceMessageStructCodec.TryWrite(ref platform,
			crossingEnd, MuiObjectPersistenceMessageCodec.ExportMethod,
			APTR.FromPointer(0x3000)));
		Assert.False(MuiObjectPersistenceMessageStructCodec.TryRead(ref platform,
			crossingEnd, out _));
		Assert.False(MuiObjectPersistenceMessageStructCodec.TryReadMethodIdValue(
			ref platform, APTR.Null, out _));
	}
}
