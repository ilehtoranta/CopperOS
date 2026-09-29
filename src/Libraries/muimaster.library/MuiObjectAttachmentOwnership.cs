/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

namespace CopperOS.MuiMaster;

// Ownership disposition is independent of the returned initialized object.
// Transferred does not promise that every later cleanup operation succeeded.
internal enum MuiObjectAttachmentOwnership : byte
{
	Unresolved,
	AlreadyRegistered,
	Transferred,
	// Dispatcher attachment established absence but has not published a sidecar.
	// The caller still owns the native object and must choose its cleanup path.
	CallerOwned,
}
