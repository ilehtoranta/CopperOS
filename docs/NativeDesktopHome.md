# Native CopperOS Desktop and Home

`C:Desktop` starts a separate DOS Process named `CopperOS Desktop`. It opens a
640 x 256, two-plane PAL screen and one 620 x 232 simple-refresh window. Home
uses native rectangles and the compact ROM font through the Window RastPort.
There are no host UI or filesystem substitutes.

The foreground command waits for the Process exit callback and then returns to
its CLI. The process has a 32 KiB stack, 256 bytes of private state and an 8 KiB
cleanup reserve. Its public `CopperOS.Desktop` message port also provides a
single-instance claim. The callback's message and port belong to the waiting
launcher, so a failed CreateNewProc leaves no child-owned resources to reclaim.

Home has one IDCMP loop: drain GetMsg, handle each message, ReplyMsg exactly once,
paint changed state, then Wait for the port or Ctrl-C. Refresh messages use
BeginRefresh, the same Home renderer, and EndRefresh(TRUE). There is no idle
redraw loop. The app never reads a message after replying to it.

Selection changes repaint the old and new tiles; actions repaint the status
panel. Full paints yield between eight-row fill bands and text calls so the
cooperative console task can drain physical key releases during drawing. The
renderer never consumes IDCMP messages. Refresh still renders the complete Home
through the provider's damage clip, including partially exposed areas.

Keyboard: Tab/Shift-Tab and arrows select a tile, Return/Space activates it,
F/E/A select Files/Editor/About, H returns to the welcome text, Escape dismisses
About or exits, and Ctrl-C exits. Mouse activation requires a matching left
press/release inside a tile or Console button. Focus loss cancels a pending
press. The native close gadget also returns to the console. The follow-up
[Files view](NativeDesktopFiles.md) opens from Files and the
[Editor view](NativeDesktopEditor.md) opens from Editor or a selected text file.
Dirty documents protect exit from every view. The historical Home-only evidence
below predates these follow-ups.

Startup unwinds acquired resources in reverse order. Shutdown releases the
cleanup reserve before deleting window layers, drains and replies while
forbidden, detaches UserPort, disables IDCMP, and closes the window and screen
before deleting its port or releasing provider pins. The bounded provider can
retain a window if its void CloseWindow encounters an allocation failure;
teardown retains its owners and retries until the public screen window list no
longer contains it. Public Schedule and last-close opportunities let the input
worker retire its suspended frames before the process exits.

The GUI HUNK is loaded and registered before creating the child's stack and
screen. This ordering reduces the large loader's temporary memory demand. Home
code and GUI code are DOS system residents until reset, each registered once.
Keeping Home code alive is necessary because an exit callback may still be
returning when its awakened parent resumes. Per-view state, screen bitplanes,
layers, ports, input worker and process stack have shorter lifetimes.
One reset-lived 32-byte entry/exit veneer block preserves D2-D7 and A2-A6 across
the compiled roots, including the A4/A5 values needed by DOS continuations.
The resident module caches this block; later startup failures reuse it.

## Build and native replay

The development provider is the qualified compact text/input HUNK with SHA-256
`9B3352B2D8EE2A2A144FAA089C954B926A4E734CBA6EF2B4151AB8E7B4E04ABD`.
The required 512 KiB ROM is
`492EF10926F73EDCF3EC7CAFC331E54A84C418924F765BA93AA4D504B20990CA`.
It fixes native self-removal to reclaim the task's public MemList after selecting
the next context. The earlier console ROM retains a completed Desktop's process
and stack allocation. The artifact builder checks both complete file hashes:

```powershell
dotnet run --project tools/Desktop.Builder/CopperOS.Desktop.Builder.csproj -c Release -- `
  ../CopperStart/artifacts/intuition-text-20261018/native-v3 `
  ../CopperStart/artifacts/copperos-home-20261018/rom-reap-v1 artifacts/home-new
```

It produces `C/Desktop`, `L/CopperHome`, `L/CopperGUI`, a native map and a build
manifest. A build against current local compiler/SDK sources is a development
build; the native qualification freezes the actual compiler and SDK inputs.
Run `Desktop` from the qualified DOS startup sequence or console. Install all
three files together. Do not substitute the older `L:CopperDesktop` Workbench
provider path.

The CopperStart replay entry is
[`Test-NativeCopperOsHome.ps1`](../../CopperStart/build/Test-NativeCopperOsHome.ps1).
It freezes fixture sources, Desktop sources and managed inputs, verifies exact
ROM/provider hashes, builds the original native application, and executes real
floppy boot journeys on MC68000 PAL OCS with 512 KiB Chip + 512 KiB Slow, no Fast
RAM. Its output layout can be copied to a development image. No ROM is rebuilt
or enlarged by this feature. This profile remains separate from CopperScreen's
shipping HDF ROM/media pin; changing that pin requires its own joined replay.

The qualified Home module is 7,196 bytes; its command is 368 bytes and the GUI
provider is 256,972 bytes. Four interactive lifetimes and seven injected startup
failures return to the same 496,504-byte free-heap baseline. The lowest free heap
sample at an event Wait is 342,584 bytes, including the replay's own probe and
extra bootstrap stack. This sample is not a loader high-water or largest-block
measurement. Startup/feedback latency and 100-cycle release gates still need
separate qualification.
The first checked Home is field 8,436 in the instrumented floppy journey;
loader and renderer latency remain work before a user-facing release.

## Files and Editor next

Keep both views inside this process, renderer and IDCMP loop initially. Route
refresh, focus, keyboard and mouse events through an explicit active-view state;
do not add independent consumers of the window's message port.

1. **Files:** the [native Files view](NativeDesktopFiles.md) adds bounded DOS
   directory browsing, selection/scrolling, path/parent navigation and transaction
   rollback inside this process. Mounted-volume discovery, paging beyond the
   fixed cache remain. Text files now open in Editor. Add rename/copy/delete
   only after writable-media failure replays cover them.
2. **Editor:** the [native view](NativeDesktopEditor.md) implements the 16 KiB
   Latin-1 buffer, original case-preserving font, cursor/viewport, editing,
   New/Open and dirty-document decisions. Safe Save/Save As is next. Qualify
   recoverable temporary/backup replacement against actual writable DOS media,
   including short writes, failed close/rename and full/read-only disks.
   Multi-byte key strings, clipboard, Unicode and proportional fonts remain
   separate work.
3. **Join the workflows:** Files Open retains its directory/selection and the
   old document until a complete candidate is ready. Add save, Files reload and
   byte-exact reopen. Measure complete peaks and largest free blocks with both
   views resident; public Wait samples alone are not a peak budget.
4. **Release integration:** add these exact modules to the CopperOS image,
   qualify boot, browsing, edit/save/reopen and host focus-loss recovery on the
   selected shipping ROM/media pair, and then update the consumer pin. Home
   alone does not qualify the shipping HDF profile or constitute a performance
   acceptance measurement.
