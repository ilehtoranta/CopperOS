using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Source-bound control plane for the persistent ConClip worker.  The helper
/// only performs the MorphOS source's OFF operation; normal startup and the
/// worker service remain separate until their guest lifecycle is qualified.
/// </summary>
internal static class NativeConClipControl
{
    public const uint CtrlCSignal = 1u << 12;
    public const string RendezvousPort = "ConClip.rendezvous";

    /// <summary>
    /// Signals the existing worker while Exec's public port list is protected.
    /// Returns false when no worker is published; it does not create one.
    /// </summary>
    public static bool TryStopExistingWorker()
    {
        Exec.Forbid();
        var port = Exec.FindPort(RendezvousPort);
        if (port.IsNull)
        {
            Exec.Permit();
            return false;
        }

        var target = APTR.FromPointer(APTR.ReadUInt32(port,
            ExecLayout.MsgPort.SignalTask));
        if (target.IsNull)
        {
            Exec.Permit();
            return false;
        }

        Exec.Signal(target, CtrlCSignal);
        Exec.Permit();
        return true;
    }
}
