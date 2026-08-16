using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Guest-resident decoded arguments for MorphOS Echo.
/// </summary>
public struct EchoArguments
{
    public APTR Message;
    public uint MessageLength;
    public uint NoLine;
    public uint HasFirst;
    public uint First;
    public uint HasLength;
    public uint Length;
    public APTR ToPath;
    public uint ToPathLength;
}
