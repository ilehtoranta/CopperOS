/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// The MorphOS MUIM_Drag* methods are delivered to objects, but the native
// window/application layer owns hit testing and cross-window routing.  Keep
// that boundary explicit: the core supplies one fixed, struct-backed sample
// and the provider may handle it without exposing a managed object graph.
public enum MuiDragRoutePhase : uint
{
	Begin = 1,
	Query = 2,
	Report = 3,
	Drop = 4,
	Finish = 5,
	Event = 6,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiDragRouteSample
{
	public MuiDragRoutePhase Phase;
	public APTR Window;
	public APTR Source;
	public APTR Target;
	public APTR DragImage;
	public APTR IntuiMessage;
	public int X;
	public int Y;
	public int Update;
	public int DropFollows;
	public int MuiKey;
	public uint MousePointerType;
	public uint Qualifier;
	public uint Flags;
	// The provider writes the MorphOS method result here.  Query uses 0/1;
	// Report uses Abort/Continue/Lock/Refresh; the remaining methods use 0/1.
	public uint Result;
}

