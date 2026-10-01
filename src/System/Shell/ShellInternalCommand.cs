using Amiga;

namespace CopperOS.Shell;

/// <summary>Internal Shell command identities from the frozen MorphOS inventory.</summary>
public enum ShellInternalCommand : int
{
    Unknown = 0, Alias, Ask, CD, Cls, Echo, Else, EndCLI, EndIf, EndShell,
    EndSkip, Failat, Fault, Get, Getenv, If, Lab, NewCLI, NewShell, Path,
    Prompt, Quit, Resident, Run, Set, Setenv, Skip, Stack, Unalias, Unset,
    Unsetenv, Why,
}

/// <summary>Resolves internal command names before resident or filesystem lookup.</summary>
public static class ShellInternalCommandResolver
{
	private const uint MaximumCommandIdentity = 31;

    public static ShellInternalCommand Resolve<TPlatform>(
        ref TPlatform platform, APTR name, uint length)
        where TPlatform : struct, IShellPlatform
    {
        if (name.IsNull || length == 0 || length > 8 ||
            name.Raw > uint.MaxValue - length || !platform.IsMapped(name, length))
            return ShellInternalCommand.Unknown;

        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x416C6961u, 0x73000000u)) return ShellInternalCommand.Alias;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x41736B00u, 0)) return ShellInternalCommand.Ask;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x43440000u, 0)) return ShellInternalCommand.CD;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x436C7300u, 0)) return ShellInternalCommand.Cls;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x4563686Fu, 0)) return ShellInternalCommand.Echo;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x456C7365u, 0)) return ShellInternalCommand.Else;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x456E6443u, 0x4C490000u)) return ShellInternalCommand.EndCLI;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x456E6449u, 0x66000000u)) return ShellInternalCommand.EndIf;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x456E6453u, 0x68656C6Cu)) return ShellInternalCommand.EndShell;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x456E6453u, 0x6B697000u)) return ShellInternalCommand.EndSkip;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x4661696Cu, 0x61740000u)) return ShellInternalCommand.Failat;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x4661756Cu, 0x74000000u)) return ShellInternalCommand.Fault;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x47657400u, 0)) return ShellInternalCommand.Get;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x47657465u, 0x6E760000u)) return ShellInternalCommand.Getenv;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x49660000u, 0)) return ShellInternalCommand.If;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x4C616200u, 0)) return ShellInternalCommand.Lab;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x4E657743u, 0x4C490000u)) return ShellInternalCommand.NewCLI;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x4E657753u, 0x68656C6Cu)) return ShellInternalCommand.NewShell;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x50617468u, 0)) return ShellInternalCommand.Path;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x50726F6Du, 0x70740000u)) return ShellInternalCommand.Prompt;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x51756974u, 0)) return ShellInternalCommand.Quit;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x52657369u, 0x64656E74u)) return ShellInternalCommand.Resident;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x52756E00u, 0)) return ShellInternalCommand.Run;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x53657400u, 0)) return ShellInternalCommand.Set;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x53657465u, 0x6E760000u)) return ShellInternalCommand.Setenv;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x536B6970u, 0)) return ShellInternalCommand.Skip;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x53746163u, 0x6B000000u)) return ShellInternalCommand.Stack;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x556E616Cu, 0x69617300u)) return ShellInternalCommand.Unalias;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x556E7365u, 0x74000000u)) return ShellInternalCommand.Unset;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x556E7365u, 0x74656E76u)) return ShellInternalCommand.Unsetenv;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x57687900u, 0)) return ShellInternalCommand.Why;
        return ShellInternalCommand.Unknown;
    }

	/// <summary>
	/// Resolves a command only when DOS says its CLI-owned built-in is active.
	/// An inactive internal name continues through ordinary resident/file lookup.
	/// </summary>
	public static ShellInternalCommand ResolveAvailable<TPlatform>(
		ref TPlatform platform, APTR cli, APTR name, uint length)
		where TPlatform : struct, IShellPlatform
	{
		if (cli.IsNull) return ShellInternalCommand.Unknown;
		var command = Resolve(ref platform, name, length);
		if (command == ShellInternalCommand.Unknown ||
			!platform.TryGetInternalCommandEnabled(cli, (uint)command,
				out var enabled) || enabled > 1 || enabled == 0)
			return ShellInternalCommand.Unknown;
		return command;
	}

	/// <summary>
	/// Writes the active internal command names as one name per line. MorphOS
	/// classifies these as internal rather than SYSTEM resident components, so
	/// they are omitted from a SYSTEM-only listing.
	/// </summary>
	public static bool WriteResidentNames<TPlatform>(ref TPlatform platform,
		APTR cli, BPTR output, uint systemOnly)
		where TPlatform : struct, IShellPlatform
	{
		if (cli.IsNull || output.IsNull || systemOnly > 1) return false;
		if (systemOnly != 0) return true;
		for (var identity = 1u; identity <= MaximumCommandIdentity; identity++)
		{
			if (!TryGetNameRecord((ShellInternalCommand)identity, out var name))
				return false;
			if (!platform.TryGetInternalCommandEnabled(cli, identity,
				out var enabled) || enabled > 1) return false;
			if (enabled == 0) continue;
			for (var index = 0u; index < name.Length; index++)
			{
				var packed = index < 4 ? name.FirstWord : name.SecondWord;
				var shift = unchecked((int)(24 - (index & 3) * 8));
				var value = unchecked((byte)(packed >> shift));
				if (platform.WriteByte(output, value) < 0) return false;
			}
			if (platform.WriteByte(output, (byte)'\n') < 0) return false;
		}
		return true;
	}

	/// <summary>Gets the allocation-free display name for one command identity.</summary>
	public static bool TryGetNameRecord(ShellInternalCommand command,
		out ShellInternalCommandNameRecord name)
	{
		name = command switch
		{
			ShellInternalCommand.Alias => new(0x416C6961u, 0x73000000u, 5),
			ShellInternalCommand.Ask => new(0x41736B00u, 0, 3),
			ShellInternalCommand.CD => new(0x43440000u, 0, 2),
			ShellInternalCommand.Cls => new(0x436C7300u, 0, 3),
			ShellInternalCommand.Echo => new(0x4563686Fu, 0, 4),
			ShellInternalCommand.Else => new(0x456C7365u, 0, 4),
			ShellInternalCommand.EndCLI => new(0x456E6443u, 0x4C490000u, 6),
			ShellInternalCommand.EndIf => new(0x456E6449u, 0x66000000u, 5),
			ShellInternalCommand.EndShell => new(0x456E6453u, 0x68656C6Cu, 8),
			ShellInternalCommand.EndSkip => new(0x456E6453u, 0x6B697000u, 7),
			ShellInternalCommand.Failat => new(0x4661696Cu, 0x61740000u, 6),
			ShellInternalCommand.Fault => new(0x4661756Cu, 0x74000000u, 5),
			ShellInternalCommand.Get => new(0x47657400u, 0, 3),
			ShellInternalCommand.Getenv => new(0x47657465u, 0x6E760000u, 6),
			ShellInternalCommand.If => new(0x49660000u, 0, 2),
			ShellInternalCommand.Lab => new(0x4C616200u, 0, 3),
			ShellInternalCommand.NewCLI => new(0x4E657743u, 0x4C490000u, 6),
			ShellInternalCommand.NewShell => new(0x4E657753u, 0x68656C6Cu, 8),
			ShellInternalCommand.Path => new(0x50617468u, 0, 4),
			ShellInternalCommand.Prompt => new(0x50726F6Du, 0x70740000u, 6),
			ShellInternalCommand.Quit => new(0x51756974u, 0, 4),
			ShellInternalCommand.Resident => new(0x52657369u, 0x64656E74u, 8),
			ShellInternalCommand.Run => new(0x52756E00u, 0, 3),
			ShellInternalCommand.Set => new(0x53657400u, 0, 3),
			ShellInternalCommand.Setenv => new(0x53657465u, 0x6E760000u, 6),
			ShellInternalCommand.Skip => new(0x536B6970u, 0, 4),
			ShellInternalCommand.Stack => new(0x53746163u, 0x6B000000u, 5),
			ShellInternalCommand.Unalias => new(0x556E616Cu, 0x69617300u, 7),
			ShellInternalCommand.Unset => new(0x556E7365u, 0x74000000u, 5),
			ShellInternalCommand.Unsetenv => new(0x556E7365u, 0x74656E76u, 7),
			ShellInternalCommand.Why => new(0x57687900u, 0, 3),
			_ => default,
		};
		return name.Length != 0;
	}
}

/// <summary>Two packed words and a byte count for allocation-free command names.</summary>
public readonly struct ShellInternalCommandNameRecord
{
	public ShellInternalCommandNameRecord(uint firstWord, uint secondWord,
		uint length)
	{
		FirstWord = firstWord;
		SecondWord = secondWord;
		Length = length;
	}

	public uint FirstWord { get; }
	public uint SecondWord { get; }
	public uint Length { get; }
}
