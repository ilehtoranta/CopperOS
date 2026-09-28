using Amiga;

namespace CopperOS.Shell;

/// <summary>Builds a bounded, invocation-unique Execute work-file path.</summary>
public static class ShellScriptTemporaryPath
{
	private const uint PrefixLength = 10; // T:Execute.
	private const uint PathLength = PrefixLength + 8;
	private const uint PromptPrefixLength = 9; // T:Prompt.
	private const uint PromptPathLength = PromptPrefixLength + 8;

    public static bool TryBuild<TPlatform>(ref TPlatform platform, APTR frame,
        APTR destination, uint capacity, out uint length)
        where TPlatform : struct, IShellPlatform
    {
        length = 0;
        if (frame.IsNull || destination.IsNull || capacity <= PathLength ||
            !platform.IsMapped(destination, capacity)) return false;
        platform.WriteUInt8(destination, 0, (byte)'T');
        platform.WriteUInt8(destination, 1, (byte)':');
        platform.WriteUInt8(destination, 2, (byte)'E');
        platform.WriteUInt8(destination, 3, (byte)'x');
        platform.WriteUInt8(destination, 4, (byte)'e');
        platform.WriteUInt8(destination, 5, (byte)'c');
        platform.WriteUInt8(destination, 6, (byte)'u');
        platform.WriteUInt8(destination, 7, (byte)'t');
        platform.WriteUInt8(destination, 8, (byte)'e');
        platform.WriteUInt8(destination, 9, (byte)'.');
        for (var index = 0u; index < 8; index++)
        {
            var shift = unchecked((int)((7u - index) * 4));
            var digit = (byte)((frame.Raw >> shift) & 15u);
            platform.WriteUInt8(destination, unchecked((int)(PrefixLength + index)),
                (byte)(digit < 10 ? '0' + digit : 'A' + digit - 10));
        }
        platform.WriteUInt8(destination, unchecked((int)PathLength), 0);
        length = PathLength;
        return true;
    }

	/// <summary>Builds a prompt-capture path distinct from Execute work files.</summary>
	public static bool TryBuildPrompt<TPlatform>(ref TPlatform platform,
		APTR frame, APTR destination, uint capacity, out uint length)
		where TPlatform : struct, IShellPlatform
	{
		length = 0;
		if (frame.IsNull || destination.IsNull || capacity <= PromptPathLength ||
			!platform.IsMapped(destination, capacity)) return false;
		platform.WriteUInt8(destination, 0, (byte)'T');
		platform.WriteUInt8(destination, 1, (byte)':');
		platform.WriteUInt8(destination, 2, (byte)'P');
		platform.WriteUInt8(destination, 3, (byte)'r');
		platform.WriteUInt8(destination, 4, (byte)'o');
		platform.WriteUInt8(destination, 5, (byte)'m');
		platform.WriteUInt8(destination, 6, (byte)'p');
		platform.WriteUInt8(destination, 7, (byte)'t');
		platform.WriteUInt8(destination, 8, (byte)'.');
		for (var index = 0u; index < 8; index++)
		{
			var shift = unchecked((int)((7u - index) * 4));
			var digit = (byte)((frame.Raw >> shift) & 15u);
			platform.WriteUInt8(destination,
				unchecked((int)(PromptPrefixLength + index)),
				(byte)(digit < 10 ? '0' + digit : 'A' + digit - 10));
		}
		platform.WriteUInt8(destination, unchecked((int)PromptPathLength), 0);
		length = PromptPathLength;
		return true;
	}
}
