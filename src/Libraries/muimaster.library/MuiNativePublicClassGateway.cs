/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// First public-vector slice for the real resident library. The gateway owns a
// provider operation for the whole call, then enters the serialized class
// service. Both tokens are existing named value records; copying them never
// creates another reference. No managed state or exception path crosses the
// 68k boundary.
internal static class MuiNativePublicClassGateway
{
	internal static APTR GetClass(APTR library, APTR classId)
	{
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return APTR.Null;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return APTR.Null;
		}

		var result = MuiNativeOwnedClassServices.GetClass(ref service, classId);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation))
			return APTR.Null;
		if (!MuiNativeProviderOwner.Leave(ref operation)) return APTR.Null;
		return result;
	}

	internal static void FreeClass(APTR library, APTR classPointer)
	{
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return;
		}

		MuiNativeOwnedClassServices.FreeClass(ref service, classPointer);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation)) return;
		MuiNativeProviderOwner.Leave(ref operation);
	}

	internal static APTR CreateCustomClass(APTR library, APTR libraryBase,
		APTR superName, APTR superClass, int dataSize, APTR dispatcher)
	{
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return APTR.Null;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return APTR.Null;
		}

		var result = MuiNativeOwnedClassServices.CreateCustomClass(ref service,
			libraryBase, superName, superClass, dataSize, dispatcher);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation))
			return APTR.Null;
		if (!MuiNativeProviderOwner.Leave(ref operation)) return APTR.Null;
		return result;
	}

	internal static bool DeleteCustomClass(APTR library, APTR customClass)
	{
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return false;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return false;
		}

		var result = MuiNativeOwnedClassServices.DeleteCustomClass(ref service,
			customClass);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation))
			return false;
		if (!MuiNativeProviderOwner.Leave(ref operation)) return false;
		return result;
	}

	internal static APTR NewObjectA(APTR library, APTR className, APTR tags)
	{
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return APTR.Null;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return APTR.Null;
		}

		var result = MuiNativeOwnedClassServices.NewObject(ref service,
			className, tags);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation))
			return APTR.Null;
		if (!MuiNativeProviderOwner.Leave(ref operation)) return APTR.Null;
		return result;
	}

	internal static APTR MakeObjectA(APTR library, uint type, APTR parameters)
	{
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return APTR.Null;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return APTR.Null;
		}

		var result = MuiNativeOwnedClassServices.MakeObjectA(ref service,
			type, parameters);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation))
			return APTR.Null;
		if (!MuiNativeProviderOwner.Leave(ref operation)) return APTR.Null;
		return result;
	}

	internal static bool DisposeObject(APTR library, APTR obj)
	{
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return false;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return false;
		}

		var result = MuiNativeOwnedClassServices.DisposeObject(ref service, obj);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation))
			return false;
		if (!MuiNativeProviderOwner.Leave(ref operation)) return false;
		return result;
	}

	internal static void RequestIDCMP(APTR library, APTR obj, uint flags) =>
		ChangeIDCMP(library, obj, flags, true);

	internal static void RejectIDCMP(APTR library, APTR obj, uint flags) =>
		ChangeIDCMP(library, obj, flags, false);

	private static void ChangeIDCMP(APTR library, APTR obj, uint flags,
		bool request)
	{
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return;
		}

		MuiNativeOwnedClassServices.ChangeIDCMP(ref service, obj, flags,
			request);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation))
			return;
		MuiNativeProviderOwner.Leave(ref operation);
	}

	internal static APTR AllocAslRequest(APTR library, uint requestType,
		APTR tags)
	{
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return APTR.Null;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return APTR.Null;
		}

		var result = MuiNativeOwnedClassServices.AllocAslRequest(ref service,
			requestType, tags);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation))
			return APTR.Null;
		if (!MuiNativeProviderOwner.Leave(ref operation)) return APTR.Null;
		return result;
	}

	internal static int AslRequest(APTR library, APTR requester, APTR tags)
	{
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return 0;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return 0;
		}

		var result = MuiNativeOwnedClassServices.AslRequest(ref service,
			requester, tags);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation))
			return 0;
		if (!MuiNativeProviderOwner.Leave(ref operation)) return 0;
		return result;
	}

	internal static void FreeAslRequest(APTR library, APTR requester)
	{
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return;
		}

		MuiNativeOwnedClassServices.FreeAslRequest(ref service, requester);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation))
			return;
		MuiNativeProviderOwner.Leave(ref operation);
	}

	internal static int RequestA(APTR library, APTR application, APTR window,
		uint flags, APTR title, APTR gadgets, APTR format, APTR parameters)
	{
		var request = new MuiRequesterCallRecord
		{
			Application = application,
			Window = window,
			Flags = flags,
			Title = title,
			Gadgets = gadgets,
			Format = format,
			Parameters = parameters,
		};
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return 0;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return 0;
		}

		var result = MuiNativeOwnedClassServices.RequestA(ref service, request);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation))
			return 0;
		if (!MuiNativeProviderOwner.Leave(ref operation)) return 0;
		return result;
	}

	internal static int RequestObjectA(APTR library, APTR application, APTR window,
		uint flags, APTR title, APTR gadgets, APTR obj, APTR format,
		APTR parameters)
	{
		var request = new MuiRequesterCallRecord
		{
			Application = application,
			Window = window,
			Flags = flags,
			Title = title,
			Gadgets = gadgets,
			Object = obj,
			Format = format,
			Parameters = parameters,
		};
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return 0;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return 0;
		}

		var result = MuiNativeOwnedClassServices.RequestObjectA(ref service,
			request);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation))
			return 0;
		if (!MuiNativeProviderOwner.Leave(ref operation)) return 0;
		return result;
	}

	internal static bool Redraw(APTR library, APTR obj, uint flags)
	{
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return false;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return false;
		}

		var result = MuiNativeOwnedClassServices.Redraw(ref service, obj, flags);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation))
			return false;
		if (!MuiNativeProviderOwner.Leave(ref operation)) return false;
		return result;
	}

	internal static bool Layout(APTR library, APTR obj, int left, int top,
		int width, int height, uint flags)
	{
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return false;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return false;
		}

		var result = MuiNativeOwnedClassServices.Layout(ref service, obj, left,
			top, width, height, flags);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation))
			return false;
		if (!MuiNativeProviderOwner.Leave(ref operation)) return false;
		return result;
	}

	internal static APTR AddClipping(APTR library, APTR renderInfo, int left,
		int top, int width, int height)
	{
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return APTR.Null;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return APTR.Null;
		}
		var result = MuiNativeOwnedClassServices.AddClipping(ref service,
			renderInfo, left, top, width, height);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation))
			return APTR.Null;
		if (!MuiNativeProviderOwner.Leave(ref operation)) return APTR.Null;
		return result;
	}

	internal static bool RemoveClipping(APTR library, APTR renderInfo,
		APTR handle)
	{
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return false;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return false;
		}
		var result = MuiNativeOwnedClassServices.RemoveClipping(ref service,
			renderInfo, handle);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation))
			return false;
		if (!MuiNativeProviderOwner.Leave(ref operation)) return false;
		return result;
	}

	internal static APTR AddClipRegion(APTR library, APTR renderInfo,
		APTR region)
	{
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return APTR.Null;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return APTR.Null;
		}
		var result = MuiNativeOwnedClassServices.AddClipRegion(ref service,
			renderInfo, region);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation))
			return APTR.Null;
		if (!MuiNativeProviderOwner.Leave(ref operation)) return APTR.Null;
		return result;
	}

	internal static bool RemoveClipRegion(APTR library, APTR renderInfo,
		APTR handle)
	{
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return false;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return false;
		}
		var result = MuiNativeOwnedClassServices.RemoveClipRegion(ref service,
			renderInfo, handle);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation))
			return false;
		if (!MuiNativeProviderOwner.Leave(ref operation)) return false;
		return result;
	}

	internal static bool BeginRefresh(APTR library, APTR renderInfo, uint flags)
	{
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return false;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return false;
		}
		var result = MuiNativeOwnedClassServices.BeginRefresh(ref service,
			renderInfo, flags);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation))
			return false;
		if (!MuiNativeProviderOwner.Leave(ref operation)) return false;
		return result;
	}

	internal static bool EndRefresh(APTR library, APTR renderInfo, uint flags)
	{
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return false;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return false;
		}
		var result = MuiNativeOwnedClassServices.EndRefresh(ref service,
			renderInfo, flags);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation))
			return false;
		if (!MuiNativeProviderOwner.Leave(ref operation)) return false;
		return result;
	}

	internal static int ObtainPen(APTR library, APTR renderInfo, APTR penSpec,
		uint flags)
	{
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return -1;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return -1;
		}
		var result = MuiNativeOwnedClassServices.ObtainPen(ref service,
			renderInfo, penSpec, flags);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation))
			return -1;
		if (!MuiNativeProviderOwner.Leave(ref operation)) return -1;
		return result;
	}

	internal static bool ReleasePen(APTR library, APTR renderInfo, int pen)
	{
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return false;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return false;
		}
		var result = MuiNativeOwnedClassServices.ReleasePen(ref service,
			renderInfo, pen);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation))
			return false;
		if (!MuiNativeProviderOwner.Leave(ref operation)) return false;
		return result;
	}

	internal static bool GetRGBColor(APTR library, APTR renderInfo,
		APTR penSpec, APTR rgbColor)
	{
		if (MuiNativeProviderOwner.Enter(library, out var operation) !=
			MuiNativeClassLeaseResult.Acquired) return false;
		if (!MuiNativeProviderOwner.TryEnterService(ref operation, out var service))
		{
			MuiNativeProviderOwner.Leave(ref operation);
			return false;
		}
		var result = MuiNativeOwnedClassServices.GetRGBColor(ref service,
			renderInfo, penSpec, rgbColor);
		if (!MuiNativeProviderOwner.TryLeaveService(ref service, out operation))
			return false;
		if (!MuiNativeProviderOwner.Leave(ref operation)) return false;
		return result;
	}
}
