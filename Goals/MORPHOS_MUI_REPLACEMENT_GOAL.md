# Goal: MorphOS 3.20-Compatible MUI for CopperOS

## Summary

Build a progressively qualified `muimaster.library` and all standard MUI
classes documented by the MorphOS 3.20 SDK. Production code will use
constrained, C-like C# lowered by CopperSharp to freestanding MC68000 code,
with no managed runtime, managed allocation, exceptions, or FPU requirement.

Implementation lives in CopperOS; shared ABI corrections belong in
CopperSharp68k, and required BOOPSI/Intuition/Layers integration fixes belong
in CopperStart. The final artifact maps to
`filesystem/SYS/Libs/muimaster.library`.

Use the [MorphOS 3.20 MUI index](https://morphos-team.net/sdk/index_MUI.html),
[official MUImaster documentation](https://morphos-team.net/sdk/MUI/MUImaster.html),
and installed SDK headers as primary authorities. Do not redistribute MorphOS
headers, documentation, examples, or copied implementation text because the
[SDK documentation states redistribution restrictions](https://morphos-team.net/sdk/index.html).

## Goal Contract and Architecture

- Create an active progressive goal and a master
  `MORPHOS_MUI_REPLACEMENT_GOAL.md`, supported by an ABI/class inventory,
  completion ledger, progress log, and qualification report.
- Define one `MorphOs320M68k` profile. Classic MUI, PPC-native output,
  third-party MCC implementations, pixel-identical MorphOS styling, and private
  MorphOS vectors are outside this goal.
- Cover every standard class in the official MorphOS 3.20 MUI index. External
  classes must be discoverable through the documented loader contract, but
  third-party `.mcc` behavior is not reimplemented.
- Keep MUI semantics in CopperOS. Production code calls other system libraries
  through SDK ABI declarations and narrow native adapters; it must not compile
  CopperStart implementations into `muimaster.library`.
- Use compile-time-constrained value-type platforms for guest memory, Exec
  allocation, BOOPSI dispatch, Intuition, Layers, Graphics, DOS, ASL, input,
  timers, callbacks, and library loading.
- Store objects, classes, notifications, ownership links, lists, strings,
  handlers, and persistent state in fixed-width guest memory allocated through
  Exec. Do not use CLR objects, arrays, collections, dictionaries, delegates,
  LINQ, tasks, reflection, boxing, or host handles.
- Represent suspension and guest callbacks with explicit fixed-width
  continuation/result records. Expected failures return `NULL`, `FALSE`,
  documented method results, `MUI_Error`, or `IoErr`; production code contains
  no `throw`, `try`, or `catch`.
- Use integer or fixed-point arithmetic internally. Carry ABI floating-point
  values bit-exactly and introduce freestanding software arithmetic only when
  an official public method requires it; never require an FPU.
- Development builds use a CopperOS identity. Advertise a MorphOS-compatible
  library version only after every API and class assigned to that version
  passes its gate.
- Provide functional neutral rendering, layout, input, and redraw behavior.
  MorphOS theme parsing, preference fidelity, artwork, and pixel matching remain
  a later goal.

## Progressive Implementation Goals

### MG00 — Freeze authorities and inventory

- Correct existing README language from generic Amiga MUI to MorphOS 3.20 MUI.
- Inventory all public master-library functions, LVOs, registers, versions,
  structures, flags, tags, methods, attributes, message layouts, standard
  classes, and external-class rules.
- Compare the official SDK against the existing
  `CopperSharp.Sdk.Amiga.MUIMaster` declarations and record every missing,
  conflicting, obsolete, or unverified item.
- Classify packaging from evidence instead of assuming every `.mui` class is
  built into the library.

### MG01 — Qualify the shared SDK ABI

- Make `CopperSharp.Sdk.Amiga` the sole owner of public MUI names and layouts;
  prohibit shadow ABI declarations in CopperOS.
- Add typed public structures and big-endian codecs where the current generated
  constant surface is insufficient.
- Add exhaustive tests for numeric values, packing, offsets, signedness, LVOs,
  register annotations, version admission, and the complete class inventory.

### MG02 — Establish the freestanding project

- Add `CopperOS.MuiMaster` and focused test/native-root projects to
  `CopperOS.sln`.
- Establish the production capability contracts, guest-resident library
  base/private root, error state, class registry, allocation policy, resident
  metadata, and vector router.
- Add static and compiled-artifact gates rejecting forbidden CIL, managed
  allocation, exception regions, framework/runtime features, unresolved
  imports, and host dependencies.

### MG03 — Complete the BOOPSI prerequisite

- Audit and extend CopperStart Intuition's BOOPSI support for class
  creation/deletion, method and super-method dispatch, instance data,
  `OM_NEW/SET/GET/DISPOSE`, retain/release, subclass tracking, and reentrant
  dispatch.
- Prove MUI can use these facilities without maintaining a second incompatible
  object system.

### MG04 — Implement the headless object core

- Implement master-library creation/disposal, built-in and external class
  lookup, custom classes, tag iteration, ownership, reference handling, Family,
  Notify, Dataspace, Datamap, Objectmap, and Semaphore behavior.
- Implement notification setup/removal, trigger values, chained methods,
  recursion bounds, mutation during notification, and deterministic disposal.

### MG05 — Implement Area and layout foundations

- Implement Area, Group, Balance, Register, Selectgroup, Scrollgroup, Virtgroup,
  geometry negotiation, weights, spacing, min/default/max sizes, show/hide,
  setup/cleanup, and redraw scheduling.
- Add a deterministic neutral frame/background/text/image rendering layer over
  Graphics and Layers capabilities.

### MG06 — Implement application and window behavior

- Implement Application, Window, event dispatch, signal masks, return IDs,
  input handlers, window ownership, open/close lifecycle, focus, keyboard
  cycling, menus, iconification contracts, and requester coordination.
- Keep event processing explicit and scheduler-driven; do not use threads,
  tasks, async methods, or managed callbacks.

### MG07 — Implement common controls

- Complete Text, Rectangle, Image, Bitmap, Bodychunk, Gauge, Levelmeter,
  Numeric, Slider, Knob, Numericbutton, String, Cycle, Radio, Prop, Scrollbar,
  Scale, Gadget, and related documented classes.
- Qualify constructor attributes, set/get behavior, methods, notifications,
  layout, input, redraw, disabled state, and disposal for every class.

### MG08 — Implement complex collections

- Complete List, Listview, Listtree, Dirlist, Volumelist, Floattext,
  Stringscroll, and associated scrolling classes.
- Cover hook/method-based construction, destruction, display and comparison,
  multicolumn data, selection, sorting, insertion/removal, ownership, directory
  failures, and large-list performance.

### MG09 — Complete services and specialist classes

- Implement Pop*, requester, ASL, pen/color, palette, panel, adjuster,
  process/slave, Boopsi, Dtpic, menu, preference-facing, and every other
  inventoried standard class.
- Complete clipping, refresh, pen acquisition, `MUI_Layout`, requester-object
  retention, custom-class gateways, and external-class loading.

### MG10 — Close the inventory

- Require every inventoried function, class, method, attribute, and structure
  to have an implemented, obsolete, unsupported-by-profile, or
  external-component disposition.
- Reject unconditional-success stubs, silent no-ops, host dictionaries,
  untested declarations, and placeholder classes as completion.

### MG11 — Build and integrate the native library

- Produce an exception-free MC68000 resident/HUNK library and stage it as
  `filesystem/SYS/Libs/muimaster.library`.
- Compile the complete closure for MC68020 and MC68040 without changing
  semantics.
- Integrate disk-library loading with CopperStart and exercise real Exec,
  Intuition, Layers, Graphics, DOS, and input boundaries without moving MUI
  into ROM.

### MG12 — Final qualification

- Run ABI, semantic, malformed-input, allocation-failure, callback-reentrancy,
  lifecycle, native parity, application integration, resource-leak, and
  deterministic performance suites.
- Run the existing MUISunflower and MUITaskList clients plus focused
  applications covering notifications, custom classes, lists, menus,
  requesters, and external loading.
- Record SDK-conformance completion separately from MorphOS differential
  completion. Because only the SDK is currently available, the final
  MorphOS-compatibility claim remains gated on later black-box traces from a
  licensed MorphOS 3.20 installation.

## Test and Acceptance Contract

- Every public ABI row and official standard class is represented in generated
  inventories with zero unexplained gaps.
- Production MC68000 artifacts use CopperSharp freestanding mode, no memory
  manager, and exception-disabled lowering; reports show zero managed
  allocations, exception metadata, runtime/framework members, host imports,
  and unresolved relocations.
- Object creation and tag processing are failure-atomic; partial objects,
  ownership edges, notifications, classes, pens, windows, and allocations are
  rolled back correctly.
- Notification mutation, nested dispatch, callback suspension, window close,
  disposal order, invalid guest pointers, malformed tag lists, and allocation
  failure receive explicit tests.
- Native MC68000 execution matches pure semantic traces; MC68020 and MC68040
  builds pass the same ABI and zero-runtime gates.
- No compatible version is advertised until its complete versioned surface
  passes.
- The active goal is complete only after all documented gates, including
  licensed MorphOS differential qualification, pass.

## Assumptions

- The no-exception/no-managed-runtime rule applies to all production and
  native-reachable code. Host test frameworks and build tooling may use .NET,
  but production sources must remain valid in the constrained subset.
- MC68000 is the behavioral baseline; no hardware floating-point unit is
  assumed.
- All three repositories may be updated, but unrelated work is preserved and
  public SDK changes receive compatibility tests before consumers change.
- Official identifiers and ABI facts may be recorded with original CopperOS
  descriptions; copyrighted MorphOS documentation or implementation material
  will not be copied into the repositories.
