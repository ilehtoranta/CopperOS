using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.Shell;

/// <summary>Lifecycle stored for one top-level Shell Execute runner.</summary>
public enum ShellScriptRunnerState : uint
{
	Running = 1,
	Pending = 2,
	Completed = 3,
	Failed = 4,
}

/// <summary>
/// Fixed-width Shell-owned storage for a persistent script engine invocation.
/// The buffers are deliberately explicit; no managed workspace or script text
/// survives the handoff.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 2, Size = 140)]
public struct ShellScriptRunnerRecord
{
	public uint Magic;
	public uint Version;
	public APTR Cli;
	public APTR Frame;
	public BPTR Input;
	public APTR Line;
	public APTR CommandName;
	public APTR Token;
	public APTR First;
	public APTR Second;
	public APTR Third;
	public APTR Fourth;
	public APTR ErrorCodes;
	public APTR RedirectionCommand;
	public APTR RedirectionInput;
	public APTR RedirectionOutput;
	public APTR RedirectionError;
	public APTR AliasLine;
	public APTR LookupPath;
	public ShellScriptRunnerState State;
	public int Result;
	public uint Steps;
	/// <summary>0 for borrowed input, 1 for an owned DOS handle, 2 for Shell-owned command text.</summary>
	public uint InputOwned;
	/// <summary>Invocation-owned raw arguments retained for the script frame.</summary>
	public APTR ScriptArguments;
	public uint ScriptArgumentLength;
	/// <summary>Runner-owned storage for the active .KEY template.</summary>
	public APTR ScriptKeyTemplate;
	public uint ScriptKeyTemplateLength;
	/// <summary>Runner-owned storage reserved for a unique Execute work-file path.</summary>
	public APTR ScriptTemporaryPath;
	public uint ScriptTemporaryPathLength;
	/// <summary>Runner-owned original script path for a later Execute pre-scan pass.</summary>
	public APTR ScriptSourcePath;
	public uint ScriptSourcePathLength;
	/// <summary>Stable workspace for a resumable prompt template snapshot.</summary>
	public APTR PromptTemplate;
	/// <summary>Open prompt-capture reader reclaimed with its runner.</summary>
	public BPTR PromptCaptureInput;
	public uint PromptCapturePathLength;
	/// <summary>Owned ErrorCodes allocation size in bytes.</summary>
	public uint ErrorCodeBytes;
}

public static class ShellScriptRunnerCodec
{
	public const uint Magic = 0x5352_554E;
	public const uint Version = 9;
	public const uint Size = 140;
	// Storage shared with CopperOS ShellScriptFrameCodec and its workspace.
	public const uint FrameBytes = 152;
	// One capture-prefix length word plus a complete script-line template.
	public const uint PromptTemplateBytes = 4 + 4096;
	public const uint TemporaryPathBytes = 512;
	public const uint KeyTemplateBytes = 4096;
	public const uint KeyResultBytes = 1024;
    // Exact fixed workspace: four 4 KiB lines, four 512-byte command areas,
    // three 512-byte redirection paths, name, lookup and temporary paths.
    public const uint WorkspaceBytes = 21248;
    public const uint WorkspaceName = 4096;
    public const uint WorkspaceToken = 4352;
    public const uint WorkspaceFirst = 8448;
    public const uint WorkspaceSecond = 8960;
    public const uint WorkspaceThird = 9472;
    public const uint WorkspaceFourth = 9984;
    public const uint WorkspaceRedirection = 10496;
    public const uint WorkspaceInput = 14592;
    public const uint WorkspaceOutput = 15104;
    public const uint WorkspaceError = 15616;
    public const uint WorkspaceAlias = 16128;
    public const uint WorkspaceLookup = 20224;
    public const uint WorkspaceTemporary = 20736;

	public const uint PromptCapturePathOffset = 256;

	public static bool Initialize<TMemory>(ref TMemory memory, APTR record,
		in ShellScriptRunnerRecord value)
		where TMemory : struct, IAmigaGuestMemory
	{
		if (!Valid(ref memory, record, in value)) return false;
		memory.Clear(record, Size);
		memory.WriteUInt32(record, 0, Magic);
		memory.WriteUInt32(record, 4, Version);
		return Write(ref memory, record, in value);
	}

	public static bool TryRead<TMemory>(ref TMemory memory, APTR record,
		out ShellScriptRunnerRecord value)
		where TMemory : struct, IAmigaGuestMemory
	{
		value = default;
		if (record.IsNull || !memory.IsMapped(record, Size) ||
			memory.ReadUInt32(record, 0) != Magic ||
			memory.ReadUInt32(record, 4) != Version)
			return false;
		value.Magic = Magic;
		value.Version = Version;
		value.Cli = APTR.FromPointer(memory.ReadUInt32(record, 8));
		value.Frame = APTR.FromPointer(memory.ReadUInt32(record, 12));
		value.Input = BPTR.FromRaw(memory.ReadUInt32(record, 16));
		value.Line = APTR.FromPointer(memory.ReadUInt32(record, 20));
		value.CommandName = APTR.FromPointer(memory.ReadUInt32(record, 24));
		value.Token = APTR.FromPointer(memory.ReadUInt32(record, 28));
		value.First = APTR.FromPointer(memory.ReadUInt32(record, 32));
		value.Second = APTR.FromPointer(memory.ReadUInt32(record, 36));
		value.Third = APTR.FromPointer(memory.ReadUInt32(record, 40));
		value.Fourth = APTR.FromPointer(memory.ReadUInt32(record, 44));
		value.ErrorCodes = APTR.FromPointer(memory.ReadUInt32(record, 48));
		value.RedirectionCommand = APTR.FromPointer(memory.ReadUInt32(record, 52));
		value.RedirectionInput = APTR.FromPointer(memory.ReadUInt32(record, 56));
		value.RedirectionOutput = APTR.FromPointer(memory.ReadUInt32(record, 60));
		value.RedirectionError = APTR.FromPointer(memory.ReadUInt32(record, 64));
		value.AliasLine = APTR.FromPointer(memory.ReadUInt32(record, 68));
		value.LookupPath = APTR.FromPointer(memory.ReadUInt32(record, 72));
		value.State = (ShellScriptRunnerState)memory.ReadUInt32(record, 76);
		value.Result = unchecked((int)memory.ReadUInt32(record, 80));
		value.Steps = memory.ReadUInt32(record, 84);
		value.InputOwned = memory.ReadUInt32(record, 88);
		value.ScriptArguments = APTR.FromPointer(memory.ReadUInt32(record, 92));
		value.ScriptArgumentLength = memory.ReadUInt32(record, 96);
		value.ScriptKeyTemplate = APTR.FromPointer(memory.ReadUInt32(record, 100));
		value.ScriptKeyTemplateLength = memory.ReadUInt32(record, 104);
		value.ScriptTemporaryPath = APTR.FromPointer(memory.ReadUInt32(record, 108));
		value.ScriptTemporaryPathLength = memory.ReadUInt32(record, 112);
		value.ScriptSourcePath = APTR.FromPointer(memory.ReadUInt32(record, 116));
		value.ScriptSourcePathLength = memory.ReadUInt32(record, 120);
		value.PromptTemplate = APTR.FromPointer(memory.ReadUInt32(record, 124));
		value.PromptCaptureInput = BPTR.FromRaw(
			memory.ReadUInt32(record, 128));
		value.PromptCapturePathLength = memory.ReadUInt32(record, 132);
		value.ErrorCodeBytes = memory.ReadUInt32(record, 136);
		return Valid(ref memory, record, in value);
	}

	public static bool TrySetState<TMemory>(ref TMemory memory, APTR record,
		ShellScriptRunnerState state, int result, uint steps)
		where TMemory : struct, IAmigaGuestMemory
	{
		if (!ValidState(state) || !TryRead(ref memory, record, out var value))
			return false;
		value.State = state;
		value.Result = result;
		value.Steps = steps;
		return Write(ref memory, record, in value);
	}

	/// <summary>
	/// Publishes a replacement reader and its runner-owned temporary path after
	/// the caller has created both. The Shell closes the reader before deleting
	/// its temporary path.
	/// </summary>
	public static bool TrySetTemporaryInput<TMemory>(ref TMemory memory,
		APTR record, BPTR input, APTR path, uint pathLength)
		where TMemory : struct, IAmigaGuestMemory
	{
		if (input.IsNull || path.IsNull || pathLength == 0 || pathLength >= 256 ||
			path.Raw > uint.MaxValue - pathLength ||
			!memory.IsMapped(path, pathLength + 1) ||
			memory.ReadUInt8(path, unchecked((int)pathLength)) != 0 ||
			!TryRead(ref memory, record, out var value))
			return false;
		if (value.ScriptTemporaryPath.IsNull ||
			!memory.IsMapped(value.ScriptTemporaryPath, 256)) return false;
		if (path.Raw != value.ScriptTemporaryPath.Raw) return false;
		value.Input = input;
		value.ScriptTemporaryPathLength = pathLength;
		return Write(ref memory, record, in value);
	}

	/// <summary>Transfers the live reader between a work file and borrowed input.</summary>
	public static bool TrySetInput<TMemory>(ref TMemory memory, APTR record,
		BPTR input, uint inputOwned, uint temporaryPathLength)
		where TMemory : struct, IAmigaGuestMemory
	{
		if (input.IsNull || inputOwned > 1 || temporaryPathLength >= 256 ||
			!TryRead(ref memory, record, out var value)) return false;
		value.Input = input;
		value.InputOwned = inputOwned;
		value.ScriptTemporaryPathLength = temporaryPathLength;
		return Write(ref memory, record, in value);
	}

	/// <summary>Tracks prompt capture ownership for bounded cleanup.</summary>
	public static bool TrySetPromptCapture<TMemory>(ref TMemory memory,
		APTR record, APTR path, uint pathLength, BPTR input)
		where TMemory : struct, IAmigaGuestMemory
	{
		if (!TryRead(ref memory, record, out var value) ||
			value.ScriptTemporaryPath.IsNull || path.IsNull || pathLength == 0 ||
			pathLength >= 256 || path.Raw != value.ScriptTemporaryPath.Raw + PromptCapturePathOffset ||
			path.Raw > uint.MaxValue - pathLength ||
			!memory.IsMapped(path, pathLength + 1) ||
			memory.ReadUInt8(path, unchecked((int)pathLength)) != 0)
			return false;
		value.PromptCapturePathLength = pathLength;
		value.PromptCaptureInput = input;
		return Write(ref memory, record, in value);
	}

	private static bool Write<TMemory>(ref TMemory memory, APTR record,
		in ShellScriptRunnerRecord value)
		where TMemory : struct, IAmigaGuestMemory
	{
		if (!Valid(ref memory, record, in value)) return false;
		memory.WriteUInt32(record, 8, value.Cli.Raw);
		memory.WriteUInt32(record, 12, value.Frame.Raw);
		memory.WriteUInt32(record, 16, value.Input.Raw);
		memory.WriteUInt32(record, 20, value.Line.Raw);
		memory.WriteUInt32(record, 24, value.CommandName.Raw);
		memory.WriteUInt32(record, 28, value.Token.Raw);
		memory.WriteUInt32(record, 32, value.First.Raw);
		memory.WriteUInt32(record, 36, value.Second.Raw);
		memory.WriteUInt32(record, 40, value.Third.Raw);
		memory.WriteUInt32(record, 44, value.Fourth.Raw);
		memory.WriteUInt32(record, 48, value.ErrorCodes.Raw);
		memory.WriteUInt32(record, 52, value.RedirectionCommand.Raw);
		memory.WriteUInt32(record, 56, value.RedirectionInput.Raw);
		memory.WriteUInt32(record, 60, value.RedirectionOutput.Raw);
		memory.WriteUInt32(record, 64, value.RedirectionError.Raw);
		memory.WriteUInt32(record, 68, value.AliasLine.Raw);
		memory.WriteUInt32(record, 72, value.LookupPath.Raw);
		memory.WriteUInt32(record, 76, (uint)value.State);
		memory.WriteUInt32(record, 80, unchecked((uint)value.Result));
		memory.WriteUInt32(record, 84, value.Steps);
		memory.WriteUInt32(record, 88, value.InputOwned);
		memory.WriteUInt32(record, 92, value.ScriptArguments.Raw);
		memory.WriteUInt32(record, 96, value.ScriptArgumentLength);
		memory.WriteUInt32(record, 100, value.ScriptKeyTemplate.Raw);
		memory.WriteUInt32(record, 104, value.ScriptKeyTemplateLength);
		memory.WriteUInt32(record, 108, value.ScriptTemporaryPath.Raw);
		memory.WriteUInt32(record, 112, value.ScriptTemporaryPathLength);
		memory.WriteUInt32(record, 116, value.ScriptSourcePath.Raw);
		memory.WriteUInt32(record, 120, value.ScriptSourcePathLength);
		memory.WriteUInt32(record, 124, value.PromptTemplate.Raw);
		memory.WriteUInt32(record, 128, value.PromptCaptureInput.Raw);
		memory.WriteUInt32(record, 132, value.PromptCapturePathLength);
		memory.WriteUInt32(record, 136, value.ErrorCodeBytes);
		return true;
	}

	private static bool Valid<TMemory>(ref TMemory memory, APTR record,
		in ShellScriptRunnerRecord value)
		where TMemory : struct, IAmigaGuestMemory =>
		record.IsNotNull && memory.IsMapped(record, Size) &&
		value.Cli.IsNotNull && value.Frame.IsNotNull && value.Input.IsNotNull &&
		value.InputOwned <= 2 && (value.InputOwned != 2 || ShellCommandSource.IsValid(ref memory, value.Input)) &&
		value.Line.IsNotNull && value.CommandName.IsNotNull &&
		value.Token.IsNotNull && value.First.IsNotNull && value.Second.IsNotNull &&
		value.Third.IsNotNull && value.Fourth.IsNotNull &&
		(value.ErrorCodes.IsNull ? value.ErrorCodeBytes == 0 : value.ErrorCodeBytes != 0 &&
		(value.ErrorCodeBytes & 3) == 0 && memory.IsMapped(value.ErrorCodes, value.ErrorCodeBytes)) &&
		value.RedirectionCommand.IsNotNull &&
		value.RedirectionInput.IsNotNull && value.RedirectionOutput.IsNotNull &&
		value.RedirectionError.IsNotNull && value.AliasLine.IsNotNull &&
		value.LookupPath.IsNotNull &&
		(value.ScriptArgumentLength == 0 ? value.ScriptArguments.IsNull :
			value.ScriptArguments.IsNotNull && value.ScriptArgumentLength <= 65_535 &&
			value.ScriptArguments.Raw <= uint.MaxValue - value.ScriptArgumentLength &&
			memory.IsMapped(value.ScriptArguments, value.ScriptArgumentLength)) &&
		(value.ScriptKeyTemplateLength == 0 ? true :
			value.ScriptKeyTemplate.IsNotNull &&
			value.ScriptKeyTemplateLength < 4096 &&
			memory.IsMapped(value.ScriptKeyTemplate,
				value.ScriptKeyTemplateLength + 1)) &&
		value.ScriptTemporaryPath.IsNotNull &&
		value.ScriptTemporaryPathLength < 256 &&
			value.ScriptTemporaryPath.Raw <= uint.MaxValue -
				value.ScriptTemporaryPathLength &&
			memory.IsMapped(value.ScriptTemporaryPath,
				TemporaryPathBytes) &&
		value.PromptCapturePathLength < 256 &&
		(value.PromptTemplate.IsNull
			? value.PromptCaptureInput.IsNull && value.PromptCapturePathLength == 0 &&
				(value.InputOwned != 0 || memory.IsMapped(value.Frame, FrameBytes) && (memory.ReadUInt32(value.Frame, 60) & 128) == 0)
			: memory.IsMapped(value.PromptTemplate, PromptTemplateBytes)) &&
		(value.ScriptSourcePathLength == 0 ? value.ScriptSourcePath.IsNull :
			value.ScriptSourcePath.IsNotNull &&
			value.ScriptSourcePathLength <= 65_535 &&
			value.ScriptSourcePath.Raw <= uint.MaxValue -
				value.ScriptSourcePathLength - 1 &&
			memory.IsMapped(value.ScriptSourcePath,
				value.ScriptSourcePathLength + 1) &&
			memory.ReadUInt8(value.ScriptSourcePath,
				unchecked((int)value.ScriptSourcePathLength)) == 0) &&
		ValidState(value.State);

	private static bool ValidState(ShellScriptRunnerState state) =>
		state is ShellScriptRunnerState.Running or
			ShellScriptRunnerState.Pending or
			ShellScriptRunnerState.Completed or
			ShellScriptRunnerState.Failed;
}
