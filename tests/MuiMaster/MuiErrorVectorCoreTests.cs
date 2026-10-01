using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiErrorVectorCoreTests
{
	private struct DosPlatform : IMuiErrorDosPlatform
	{
		internal const uint DosBaseAddress = 0x1234;
		internal int IoError;
		internal int OpenCount;
		internal int CloseCount;
		internal int ReadCount;
		internal int WriteCount;

		public APTR OpenDosLibrary()
		{
			OpenCount++;
			return APTR.FromPointer(DosBaseAddress);
		}

		public void CloseDosLibrary(APTR dosBase)
		{
			Assert.Equal(DosBaseAddress, dosBase.Raw);
			CloseCount++;
		}

		public int ReadIoErr(APTR dosBase)
		{
			Assert.Equal(DosBaseAddress, dosBase.Raw);
			ReadCount++;
			return IoError;
		}

		public void WriteIoErr(APTR dosBase, int error)
		{
			Assert.Equal(DosBaseAddress, dosBase.Raw);
			WriteCount++;
			IoError = error;
		}
	}

	[Fact]
	public void ErrorReadsTheCallingDosErrorAndClosesItsBorrowedBase()
	{
		var platform = new DosPlatform { IoError = -27 };

		Assert.Equal(-27, MuiErrorVectorCore.Error(ref platform));
		Assert.Equal(1, platform.OpenCount);
		Assert.Equal(1, platform.ReadCount);
		Assert.Equal(1, platform.CloseCount);
		Assert.Equal(0, platform.WriteCount);
	}

	[Fact]
	public void SetErrorForwardsTheValueWithoutInventingAReturnValue()
	{
		var platform = new DosPlatform { IoError = 9 };

		MuiErrorVectorCore.SetError(ref platform, -42);
		Assert.Equal(-42, platform.IoError);
		Assert.Equal(1, platform.OpenCount);
		Assert.Equal(1, platform.WriteCount);
		Assert.Equal(1, platform.CloseCount);
		Assert.Equal(0, platform.ReadCount);
	}
}
