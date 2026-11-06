namespace CopperOS.Shell;

/// <summary>Fixed guest workspace used by a Shell script runner.</summary>
public static class ShellScriptWorkspaceLayout
{
    public const uint LineBytes = 4096;
    public const uint NameBytes = 256;
    public const uint TokenBytes = 4096;
    public const uint SmallBytes = 512;

    public const uint Name = LineBytes;
    public const uint Token = Name + NameBytes;
    public const uint First = Token + TokenBytes;
    public const uint Second = First + SmallBytes;
    public const uint Third = Second + SmallBytes;
    public const uint Fourth = Third + SmallBytes;
    public const uint Redirection = Fourth + SmallBytes;
    public const uint Input = Redirection + LineBytes;
    public const uint Output = Input + SmallBytes;
    public const uint Error = Output + SmallBytes;
    public const uint Alias = Error + SmallBytes;
    public const uint Lookup = Alias + LineBytes;
    public const uint Temporary = Lookup + SmallBytes;
    public const uint Bytes = Temporary + SmallBytes;
}
