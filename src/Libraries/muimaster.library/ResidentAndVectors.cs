/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

public enum MuiVectorId : byte
{
	NewObjectA, DisposeObject, RequestA, AllocAslRequest, AslRequest,
	FreeAslRequest, Error, SetError, GetClass, FreeClass, RequestIDCMP,
	RejectIDCMP, Redraw, CreateCustomClass, DeleteCustomClass, MakeObjectA,
	Layout, ObtainPen, ReleasePen, AddClipping, RemoveClipping, AddClipRegion,
	RemoveClipRegion, BeginRefresh, EndRefresh, GetRGBColor, RequestObjectA,
}

public static class MuiResidentMetadata
{
	public static CString DevelopmentName => CString.FromLiteral("copperos-muimaster.library");
	public const ushort DevelopmentVersion = 0;
	public const ushort DevelopmentRevision = 1;
	public const int FirstLvo = -30;
	public const int LastLvo = -756;
	public const int VectorStride = 6;
}

public static class MuiVectorRouter
{
	public static bool TryResolve(int lvo, out MuiVectorId vector)
	{
		switch (lvo)
		{
			case -30: vector = MuiVectorId.NewObjectA; return true;
			case -36: vector = MuiVectorId.DisposeObject; return true;
			case -42: vector = MuiVectorId.RequestA; return true;
			case -48: vector = MuiVectorId.AllocAslRequest; return true;
			case -54: vector = MuiVectorId.AslRequest; return true;
			case -60: vector = MuiVectorId.FreeAslRequest; return true;
			case -66: vector = MuiVectorId.Error; return true;
			case -72: vector = MuiVectorId.SetError; return true;
			case -78: vector = MuiVectorId.GetClass; return true;
			case -84: vector = MuiVectorId.FreeClass; return true;
			case -90: vector = MuiVectorId.RequestIDCMP; return true;
			case -96: vector = MuiVectorId.RejectIDCMP; return true;
			case -102: vector = MuiVectorId.Redraw; return true;
			case -108: vector = MuiVectorId.CreateCustomClass; return true;
			case -114: vector = MuiVectorId.DeleteCustomClass; return true;
			case -120: vector = MuiVectorId.MakeObjectA; return true;
			case -126: vector = MuiVectorId.Layout; return true;
			case -156: vector = MuiVectorId.ObtainPen; return true;
			case -162: vector = MuiVectorId.ReleasePen; return true;
			case -168: vector = MuiVectorId.AddClipping; return true;
			case -174: vector = MuiVectorId.RemoveClipping; return true;
			case -180: vector = MuiVectorId.AddClipRegion; return true;
			case -186: vector = MuiVectorId.RemoveClipRegion; return true;
			case -192: vector = MuiVectorId.BeginRefresh; return true;
			case -198: vector = MuiVectorId.EndRefresh; return true;
			case -690: vector = MuiVectorId.GetRGBColor; return true;
			case -756: vector = MuiVectorId.RequestObjectA; return true;
			default: vector = default; return false;
		}
	}
}
