using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;
namespace CopperOS.Commands.AddBuffersNativeRoot;
/// <summary>Private receipt root for Copy TestLoop.</summary>
public static class NativeMorphOSCopyLoopGuardEntry
{
 [M68kEntryPoint] public static int Main(int bytes, CONST_STRPTR control)
 {
  var wb=NativeCommandStartup.ReceiveWorkbenchMessage();if(!NativeCommandStartup.OpenDos(37))return NativeCommandStartup.Finish(DOS.RETURN_FAIL,0,wb);if(wb.IsNotNull)return NativeCommandStartup.Finish(DOS.RETURN_ERROR,(int)DOS.Error.ObjectWrongType,wb);if(bytes!=12||control.IsNull)return NativeCommandStartup.Finish(DOS.RETURN_ERROR,(int)DOS.Error.LineTooLong,APTR.Null);var a=control.Address;var loop=NativeMorphOSCopyLoopGuard.HasLoop(new BPTR(APTR.ReadUInt32(a,0)),new BPTR(APTR.ReadUInt32(a,4)));APTR.WriteUInt32(control,8,loop?1u:0);return NativeCommandStartup.Finish(DOS.RETURN_OK,0,APTR.Null);
 }
}
