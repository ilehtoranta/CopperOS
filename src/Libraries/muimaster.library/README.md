# muimaster.library

MG900 gives `MuiGroupLayoutPolicyStateRecord` struct-first semantic admission:
MorphOS BOOL fields (`Horizontal`, `SameWidth`, `SameHeight`, `SameSize`, and
`PageMode`) are restricted to 0/1, signed spacing retains documented default/percentage
inputs, and pixel spacing is bounded. A malformed present policy fails closed
before Group getters, min/max, and child layout; an absent record still
bootstraps from raw compatibility attributes. The regression confirms that raw
attributes and child geometry are not mutated by malformed state. Focused
common-control coverage is **168/168**, the Group layout-policy slice passes
**20/20**, the complete host suite is **1625/1625**, and the focused freestanding
gate remains **2/2**. NativeExecution builds with **0 errors** (two existing
CopperMod/SourceLink warnings). NativeRoot remains blocked by pre-existing
external CopperSharp68k AHI SDK `CS0266` errors in `Sdk.Amiga/AHI/Structures.cs`.
This remains a freestanding, C-like transition with no exception path, managed
runtime, or consumer-facing numeric object-layout offset. Contract surface:
the official [MorphOS MUI Group documentation](https://morphos-team.net/sdk/MUI/MUI_Group.html).

MG899 gives `MuiAreaGeometryStateRecord` strict, struct-first admission:
dimensions must be nonnegative and derived edges must remain representable.
Present malformed records fail closed before geometry getters, relayout, content
resolution, and drawing; compatible raw primary-coordinate changes derive
stale secondary edges into the named record, while absent neutral bootstrap
state remains valid. Focused common-control coverage is **168/168**; the
AreaLayout slice is **21/21**; the complete host suite is **1624/1624**, the
focused freestanding gate remains **2/2**, and NativeExecution builds with
**0 errors** (two existing CopperMod/SourceLink warnings). NativeRoot remains
blocked by pre-existing external CopperSharp68k AHI SDK `CS0266` errors in
`Sdk.Amiga/AHI/Structures.cs`. This remains a freestanding, C-like transition
with no exception path, managed runtime, or consumer-facing numeric
object-layout offset.

MG898 gives `MuiAreaLayoutPolicyStateRecord` strict, struct-first admission:
`ShowMe` is validated as a BOOL while fixed-size, maximum-size, inset, and
weight ULONG fields remain lossless. Absent and malformed-present records are
distinguished; malformed policy fails closed before generic getters/setters,
min/max, content resolution, Area drawing/backfill/text consumers, and group
visibility. Construction and persistence setters admit the named policy before
raw compatibility mutation. Focused common-control coverage is **168/168**;
the AreaLayout slice is **20/20**; the complete host suite is **1623/1623**,
the focused freestanding gate remains **2/2**, and NativeExecution builds with
**0 errors** (two existing CopperMod/SourceLink warnings). NativeRoot remains
blocked by pre-existing external CopperSharp68k AHI SDK `CS0266` errors in
`Sdk.Amiga/AHI/Structures.cs`. This remains a freestanding, C-like transition
with no exception path, managed runtime, or consumer-facing numeric
object-layout offset.

MG897 gives `MuiAreaRenderPolicyStateRecord` struct-first semantic admission:
BOOL-like `FillArea`, `FrameVisible`, `FramePhantomHoriz`, and `FrameDynamic`
are validated while selector and pointer fields remain lossless. Malformed
records fail closed before render-policy getters, mutation, Area layout
drawing, backfill, text metrics, text drawing, or common-control drawing.
Focused common-control coverage is **168/168**; the AreaLayout slice is
**19/19**; the complete host suite is **1622/1622**, and the focused
freestanding gate passes **2/2**. NativeRoot qualification remains blocked by
pre-existing external CopperSharp68k AHI SDK `CS0266` errors in
`Sdk.Amiga/AHI/Structures.cs`. This remains a freestanding, C-like transition
with no exception path, managed runtime, or consumer-facing numeric
object-layout offset. Contract surface: the official [MorphOS MUI Text documentation](https://morphos-team.net/sdk/MUI/MUI_Text.html).

MG896 gives `MuiAreaPresentationState` struct-first semantic admission:
`Disabled`, `ShowMe`, and `CustomBackfill` are validated as BOOL-like values
while `Background` and `Frame` remain lossless ULONG fields. Malformed records
fail closed before shared Area getters, mutation, sizing, input, backfill, or
drawing; failed BOOL mutations preserve raw state. Focused common-control
coverage is **168/168**; the complete host suite is **1621/1621**, and the
focused freestanding gate passes **2/2**. NativeRoot qualification remains
blocked by pre-existing external CopperSharp68k AHI SDK `CS0266` errors in
`Sdk.Amiga/AHI/Structures.cs`. This remains a freestanding, C-like transition
with no exception path, managed runtime, or consumer-facing numeric
object-layout offset. Contract surface: the official [MorphOS MUI Text documentation](https://morphos-team.net/sdk/MUI/MUI_Text.html).

MG895 gives `MuiTextPresentationState` strict, struct-first semantic admission:
MorphOS BOOL-like `SetMin`, `SetMax`, `SetVMax`, `Marking`, and `HiCharPresent`
fields, byte-valued `ControlChar` and `HiChar`, and the
`Nothing`/`Cutoff`/`Hide` shortening selector are validated. Malformed records
fail closed before Text getters, mutation, sizing, or drawing; failed
`TextShorten` and `TextControlChar` mutations preserve raw state. Focused
common-control coverage is **167/167**; the complete host suite is **1620/1620**,
and the focused freestanding gate passes **2/2**. NativeRoot qualification
remains blocked by pre-existing external CopperSharp68k AHI SDK `CS0266` errors
in `Sdk.Amiga/AHI/Structures.cs`. This remains a freestanding, C-like
transition with no exception path, managed runtime, or consumer-facing numeric
object-layout offset. Contract surface: the official [MorphOS MUI Text documentation](https://morphos-team.net/sdk/MUI/MUI_Text.html).

MG894 gives `MuiTextShortenedState` struct-first semantic admission:
renderer-produced `MUIA_Text_Shortened` must be exactly 0/1. Its record uses a
dedicated object-store key rather than the Area geometry key; malformed status
fails closed before Text getters or draw publication. Focused common-control
coverage is **166/166**; the complete host suite is **1619/1619**, and the
focused freestanding gate passes **2/2**. NativeRoot qualification remains
blocked by pre-existing external CopperSharp68k AHI SDK `CS0266` errors in
`Sdk.Amiga/AHI/Structures.cs`. This remains a freestanding, C-like transition
with no exception path, managed runtime, or consumer-facing numeric
object-layout offset. Contract surface: the official [MorphOS MUI Text documentation](https://morphos-team.net/sdk/MUI/MUI_Text.html).

MG893 gives `MuiTextUnicodeState` struct-first semantic admission: the named
`MUIA_Text_Unicode` BOOL must be exactly 0/1. Its record uses a dedicated
object-store key rather than the Area render-policy key; malformed records
fail closed before Text getters, metrics, or drawing. Focused common-control
coverage is **165/165**; the complete host suite is **1618/1618**, and the
focused freestanding gate passes **2/2**. NativeRoot qualification remains
blocked by pre-existing external CopperSharp68k AHI SDK `CS0266` errors in
`Sdk.Amiga/AHI/Structures.cs`. This remains a freestanding, C-like transition
with no exception path, managed runtime, or consumer-facing numeric
object-layout offset. Contract surface: the official [MorphOS MUI Text documentation](https://morphos-team.net/sdk/MUI/MUI_Text.html).

MG892 gives `MuiTextCopyState` struct-first semantic admission: the named
`MUIA_Text_Copy` BOOL must be exactly 0/1. Malformed records fail closed before
Text getters or contents-ownership changes, and Text_Contents mutation
consumes the admitted policy rather than raw storage. Focused common-control
coverage is **164/164**; the complete host suite is **1617/1617**, and the
focused freestanding gate passes **2/2**. NativeRoot qualification remains
blocked by pre-existing external CopperSharp68k AHI SDK `CS0266` errors in
`Sdk.Amiga/AHI/Structures.cs`. This remains a freestanding, C-like transition
with no exception path, managed runtime, or consumer-facing numeric
object-layout offset. Contract surface: the official [MorphOS MUI Text documentation](https://morphos-team.net/sdk/MUI/MUI_Text.html).

MG891 gives `MuiLevelmeterPresentationState` struct-first semantic admission:
the shared MorphOS `Gauge_Horiz` BOOL must be exactly 0/1. Malformed records
fail closed before Levelmeter getters, layout, or drawing, and rendering
consumes the named presentation state rather than raw storage. Focused
common-control coverage is **163/163**; the complete host suite is
**1616/1616**, and the focused freestanding gate passes **2/2**. NativeRoot
qualification remains blocked by pre-existing external CopperSharp68k AHI SDK
`CS0266` errors in `Sdk.Amiga/AHI/Structures.cs`. This remains a
freestanding, C-like transition with no exception path, managed runtime, or
consumer-facing numeric object-layout offset. Contract surface: the official
[MorphOS MUI Gauge documentation](https://morphos-team.net/sdk/MUI/MUI_Gauge.html).

MG890 gives `MuiBalancePolicyState` struct-first semantic admission: the
named `Balance_Quiet` BOOL must be exactly 0/1. Malformed records fail closed
before Balance getters or drawing, and rendering consumes the named policy
rather than raw storage. Focused common-control coverage is **162/162**; the
complete host suite is **1615/1615**, and the focused freestanding gate passes
**2/2**. NativeRoot qualification remains blocked by pre-existing external
CopperSharp68k AHI SDK `CS0266` errors in `Sdk.Amiga/AHI/Structures.cs`. This
remains a freestanding, C-like transition with no exception path, managed
runtime, or consumer-facing numeric object-layout offset. Contract surface:
the official [MorphOS MUI Balance documentation](https://morphos-team.net/sdk/MUI/MUI_Balance.html).

MG889 gives `MuiScrollbarLayoutState` struct-first semantic admission: the
named `Group_Horiz` BOOL must be exactly 0/1 and `Scrollbar_Type` must be one
of 0..4 (default, bottom, top, symmetric, or none). Construction normalizes a
nonzero orientation to TRUE; malformed records fail closed before Scrollbar
getters, synchronization, layout, or drawing. Focused common-control coverage
is **161/161**; the complete host suite is **1614/1614**, and the focused
freestanding gate passes **2/2**. NativeRoot qualification remains blocked by
pre-existing external CopperSharp68k AHI SDK `CS0266` errors in
`Sdk.Amiga/AHI/Structures.cs`. This remains a freestanding, C-like transition
with no exception path, managed runtime, or consumer-facing numeric
object-layout offset. Contract surface: the official [MorphOS MUI Scrollbar documentation](https://morphos-team.net/sdk/MUI/MUI_Scrollbar.html).

MG888 gives `MuiPropRangeState` struct-first, lossless admission: the
MorphOS LONG-valued `Entries`, `Visible`, and `First` fields reject negative
values and any first position beyond `max(Entries - Visible, 0)`. Constructor
inputs retain the existing first-position clamp; malformed records fail closed
before Prop/Scrollbar getters, movement, synchronization, layout, drawing,
range mutation, or headless MultiSet projection. Focused common-control
coverage is **160/160**; the complete host suite is **1613/1613**, and the
focused freestanding gate passes **2/2**. NativeRoot qualification remains
blocked by pre-existing external CopperSharp68k AHI SDK `CS0266` errors in
`Sdk.Amiga/AHI/Structures.cs`. This remains a freestanding, C-like transition
with no exception path, managed runtime, or consumer-facing numeric
object-layout offset. Contract surface: the official [MorphOS MUI Prop documentation](https://morphos-team.net/sdk/MUI/MUI_Prop.html).

MG887 gives `MuiPropPolicyState` struct-first, lossless semantic admission:
the named `Horizontal` and `Slider` BOOLs must be exactly 0/1,
`UseWinBorder` remains in its 0..3 policy domain, and `DeltaFactor` remains an
unrestricted ULONG. Malformed records fail closed before Prop or Scrollbar
policy getters, setters, synchronization, movement, layout, or drawing; failed
mutations leave raw compatibility storage unchanged. Focused common-control
coverage is **159/159**; the complete host suite is **1612/1612**, and the
focused freestanding gate passes **2/2**. NativeRoot qualification remains
blocked by pre-existing external CopperSharp68k AHI SDK `CS0266` errors in
`Sdk.Amiga/AHI/Structures.cs`. This remains a freestanding, C-like transition
with no exception path, managed runtime, or consumer-facing numeric
object-layout offset. Contract surface: the official [MorphOS MUI Prop documentation](https://morphos-team.net/sdk/MUI/MUI_Prop.html).

MG886 gives `MuiScalePresentationState` lossless, struct-first admission: its
named `Horizontal` BOOL must be exactly 0/1. Malformed records are rejected
before Scale layout or drawing consumers use them, and failed orientation
mutations leave raw compatibility storage unchanged. Focused common-control
coverage is **158/158**; the complete host suite is **1611/1611**, and the
focused freestanding gate passes **2/2**. NativeRoot qualification remains
blocked by pre-existing external CopperSharp68k AHI SDK `CS0266` errors in
`Sdk.Amiga/AHI/Structures.cs`. This remains a freestanding, C-like transition
with no exception path, managed runtime, or consumer-facing numeric
object-layout offset. Contract surface: the official [MorphOS MUI Scale documentation](https://morphos-team.net/sdk/MUI/MUI_Scale.html).

MG885 gives `MuiSliderPresentationState` lossless, struct-first admission:
its named `Horizontal` and `Quiet` BOOLs must both be exactly 0/1. Malformed
records are rejected before Slider layout, input, or drawing consumers use
them, and failed orientation mutations leave raw compatibility storage
unchanged. Focused common-control coverage is **157/157**; the complete host
suite is **1610/1610**, and the focused freestanding gate passes **2/2**.
NativeRoot qualification remains blocked by pre-existing external
CopperSharp68k AHI SDK `CS0266` errors in `Sdk.Amiga/AHI/Structures.cs`. This
remains a freestanding, C-like transition with no exception path, managed
runtime, or consumer-facing numeric object-layout offset. Contract surface:
the official [MorphOS MUI Slider documentation](https://morphos-team.net/sdk/MUI/MUI_Slider.html).

MG884 gives `MuiGaugeState` lossless, struct-first admission: its named
`Horizontal` BOOL must be exactly 0/1. Malformed Gauge records are rejected
before progress, divide/clamp, layout, or drawing consumers use them, and
failed mutations leave raw compatibility storage unchanged. Focused
common-control coverage is **156/156**; the complete host suite is
**1609/1609**, and the focused freestanding gate passes **2/2**. NativeRoot
qualification remains blocked by pre-existing external CopperSharp68k AHI SDK
`CS0266` errors in `Sdk.Amiga/AHI/Structures.cs`. This remains a
freestanding, C-like transition with no exception path, managed runtime, or
consumer-facing numeric object-layout offset. Contract surface: the official
[MorphOS MUI Gauge documentation](https://morphos-team.net/sdk/MUI/MUI_Gauge.html).

MG883 gives `MuiNumericState` lossless, struct-first admission: its named
`Reverse` BOOL must be exactly 0/1. Malformed Numeric records are rejected
before range, value, scaling, stringification, or default-value consumers use
them, and failed mutations leave raw compatibility storage unchanged. Focused
common-control coverage is **155/155**; the complete host suite is
**1608/1608**, and the focused freestanding gate passes **2/2**. NativeRoot
qualification remains blocked by pre-existing external CopperSharp68k AHI SDK
`CS0266` errors in `Sdk.Amiga/AHI/Structures.cs`. This remains a
freestanding, C-like transition with no exception path, managed runtime, or
consumer-facing numeric object-layout offset. Contract surface: the official
[MorphOS MUI Numeric documentation](https://morphos-team.net/sdk/MUI/MUI_Numeric.html).

MG882 gives `MuiGadgetInteractionState` lossless, struct-first admission:
`InputMode` must be 0..3 and `Selected`, `Pressed`, and `ShowSelState` must be
canonical 0/1 BOOLs. Malformed named state is rejected before getter, input,
or `Selected` setter consumers can mutate raw compatibility storage. Focused
common-control coverage is **154/154**; the complete host suite is
**1607/1607**, and the focused freestanding gate passes **2/2**. NativeRoot
qualification remains blocked by pre-existing external CopperSharp68k AHI SDK
`CS0266` errors in `Sdk.Amiga/AHI/Structures.cs`. This remains a
freestanding, C-like transition with no exception path, managed runtime, or
consumer-facing numeric object-layout offset. Contract surface: the official
[MorphOS MUI Gadget documentation](https://morphos-team.net/sdk/MUI/MUI_Gadget.html).

MG881 gives the named `MuiFloattextPolicyState` lossless `Justify` handling:
the codec preserves raw bytes, while policy admission requires exactly 0/1
before getters, setters, rebuild, drawing, or row consumers use the value.
Invalid values remain visible as malformed guest state. Focused
Listview/Floattext coverage remains **104/104**; the complete host suite is
**1606/1606**, and the focused freestanding gate passes **2/2**. NativeRoot
qualification is currently blocked by pre-existing external CopperSharp68k
AHI SDK `CS0266` errors in `Sdk.Amiga/AHI/Structures.cs`, unrelated to this
increment. This remains a freestanding, C-like transition with no exception path,
managed runtime, or consumer-facing numeric object-layout offset. Contract
surface: the official [MorphOS MUI Floattext documentation](https://morphos-team.net/sdk/MUI/MUI_Floattext.html).

MG880 gives Stringscroll policy records semantic admission: every named
normalized BOOL must be exactly 0/1 before getters, setters, scrolling, layout,
or composition reconciliation consume it. The field codec remains lossless,
so invalid values stay visible as malformed guest state instead of being
normalized or repaired. Focused Stringscroll coverage is **49/49**; the
complete host suite is **1606/1606**, NativeRoot builds with 0 errors and the
existing 12 SDK type-conflict warnings, and the focused freestanding gate
passes **2/2**. This remains a freestanding, C-like transition with no
exception path, managed runtime, or consumer-facing numeric object-layout
offset. Contract surface: the official [MorphOS MUI Stringscroll documentation](https://morphos-team.net/sdk/MUI/MUI_Stringscroll.html).

MG879 routes policy-only FORMAT inspection, public `Format` access, and FORMAT
replacement mutation through the shared named policy/descriptor projection. A
structurally valid policy cannot remain observable or be replaced through a
runtime setter when a published descriptor owner has an incompatible `Columns`
count; construction fallback is retained only while the policy record is
absent. Named-field List coverage remains **176/176**; the complete
host suite is **1606/1606**, NativeRoot builds with 0 errors and the existing
12 SDK type-conflict warnings, and the focused freestanding gate passes
**2/2**. This remains a freestanding, C-like transition with no exception
path, managed runtime, or consumer-facing numeric object-layout offset.
Contract surface: the official [MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG878 makes the named FORMAT policy and descriptor owner one bounded
cross-record projection. Consumers require descriptor `Columns` to equal the
policy count and remain within `MaxColumns`; a structurally readable mismatch
fails FORMAT count, descriptor, geometry, display-source, and inspection paths
closed. Teardown uses a valid policy count as a bounded cleanup witness so
malformed descriptor cardinality cannot walk adjacent guest storage.
Named-field List coverage remains **176/176**; the complete host suite is
**1606/1606**, NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings, and the focused freestanding gate passes **2/2**. This
remains a freestanding, C-like transition with no exception path, managed
runtime, or consumer-facing numeric object-layout offset. Contract surface: the
official [MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG877 aligns the internal `TryGetFormatDescriptorState` inspection seam with
public FORMAT admission. It now requires both the named FORMAT policy and
descriptor owner, so malformed policy state cannot expose a structurally valid
descriptor through introspection. Named-field regression coverage remains
**176/176**; the complete host suite is **1606/1606**, NativeRoot builds with 0
errors and the existing 12 SDK type-conflict warnings, and the focused
freestanding gate passes **2/2**. This remains a freestanding, C-like
transition with no exception path, managed runtime, or consumer-facing numeric
object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG876 extends FORMAT policy admission to `GetFormatDisplaySourceColumn`,
including its bounded out-of-range identity branch. A malformed published
`MuiListFormatPolicyState` now fails the mapping closed instead of remaining
observable through a request outside the descriptor table. Named-field
regression coverage remains **176/176**; the complete host suite is
**1606/1606**, NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings, and the focused freestanding gate passes **2/2**. This
remains a freestanding, C-like transition with no exception path, managed
runtime, or consumer-facing numeric object-layout offset. Contract surface: the
official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG875 makes `GetFormatColumn` require both the named
`MuiListFormatPolicyState` and `MuiListFormatDescriptorState` before exposing
a descriptor record. A malformed published policy can no longer leave a valid
descriptor vector visible through the public getter. Named-field regression
coverage remains **176/176**; the complete host suite is **1606/1606**,
NativeRoot builds with 0 errors and the existing 12 SDK type-conflict warnings,
and the focused freestanding gate passes **2/2**. This remains a freestanding,
C-like transition with no exception path, managed runtime, or consumer-facing
numeric object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG874 hardens List sorting at the named geometry/ColumnOrder boundary.
`Sort` and `SortEntries` now reject malformed published order state before
`CompareForSort` consumes FORMAT flags or mutates guest-resident entries;
ordinary absent-order sorting remains unchanged. Named-field regression
coverage remains **176/176**; the complete host suite is **1606/1606**,
NativeRoot builds with 0 errors and the existing 12 SDK type-conflict warnings,
and the focused freestanding gate passes **2/2**. This remains a freestanding,
C-like transition with no exception path, managed runtime, or consumer-facing
numeric object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG873 extends List ColumnOrder admission to the named geometry-column
projection. Layout, measurement, hit-testing, and edit geometry now reject a
malformed published `MuiListColumnOrderState` before FORMAT defaults or scalar
aliases can be consumed; the absent-order construction path remains valid.
Named-field regression coverage remains **176/176**; the complete host suite
is **1606/1606**, NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings, and the focused freestanding gate passes **2/2**. This
remains a freestanding, C-like transition with no exception path, managed
runtime, or consumer-facing numeric object-layout offset. Contract surface:
the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG872 makes List ColumnOrder projection admission-aware. An absent permutation
retains identity behavior, while a malformed published
`MuiListColumnOrderState` now fails descriptor getters and FORMAT
display-source mapping closed instead of silently bypassing the named
permutation. Lists without FORMAT and bounded out-of-range columns retain
their documented identity behavior. Named-field regression coverage remains
**176/176**; the complete host suite is **1606/1606**, NativeRoot builds with 0
errors and the existing 12 SDK type-conflict warnings, and the focused
freestanding gate passes **2/2**. This remains a freestanding, C-like
transition with no exception path, managed runtime, or consumer-facing numeric
object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG868 hardens List FORMAT descriptor ownership. The descriptor vector is now
owned by the named `MuiListFormatDescriptorState` record (`Magic`, `Columns`,
and `Values`) instead of a pointer paired with a separate scalar count.
Descriptor getters and FORMAT consumers use the admitted owner; malformed
published state fails closed and remains retained through runtime mutation,
while teardown uses bounded storage fields. Named-field regression coverage is
**176/176**; the complete host suite is **1606/1606**, NativeRoot builds with 0
errors and the existing 12 SDK type-conflict warnings, and the focused
freestanding gate passes **2/2**. This remains a freestanding, C-like
transition with no exception path, managed runtime, or consumer-facing numeric
object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG869 routes `FormatColumnCount` through the admitted
`MuiListFormatDescriptorState` count once the descriptor owner is published.
Malformed descriptor or policy state now fails the count projection closed
instead of allowing geometry, sorting, or editing to use a stale scalar; the
pre-publication path retains the policy fallback needed by construction.
Focused List coverage remains **176/176**, the complete host suite is
**1606/1606**, NativeRoot has 0 errors with the existing 12 SDK type-conflict
warnings, and the focused freestanding gate passes **2/2**. This remains a
freestanding, C-like transition with no exception path, managed runtime, or
consumer-facing numeric object-layout offset. Contract surface: the official
[MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG870 adds an explicit admitted geometry-column projection. Valid empty FORMAT
retains its single-column construction fallback, but malformed published
policy or descriptor state no longer gets normalized from zero to one. Sort
normalization, column-order building, layout, drawing, hit-testing, title-click,
metrics refresh, and edit-target paths fail closed at this boundary. Focused
List coverage remains **176/176**, the complete host suite is **1606/1606**,
NativeRoot has 0 errors with the existing 12 SDK type-conflict warnings, and
the focused freestanding gate passes **2/2**. This remains a freestanding,
C-like transition with no exception path, managed runtime, or consumer-facing
numeric object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG871 routes the visibility mask through an admission-returning
`TryGetHiddenColumns` projection. A malformed published
`MuiListColumnVisibilityState` no longer becomes an identity mask; geometry,
draw, hit-test, edit geometry, adjusted-width measurement, and metrics refresh
fail closed while the named mask remains available for bounded teardown.
Focused List coverage remains **176/176**, the complete host suite is
**1606/1606**, NativeRoot has 0 errors with the existing 12 SDK type-conflict
warnings, and the focused freestanding gate passes **2/2**. This remains a
freestanding, C-like transition with no exception path, managed runtime, or
consumer-facing numeric object-layout offset. Contract surface: the official
[MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG867 hardens List ColumnLayout publication. The cached `{offset,width}` vector
is now owned by the named `MuiListColumnLayoutState` record (`Magic`, `Width`,
`Columns`, and `Values`) instead of a pointer paired with a separate width
alias. Layout publication and draw, hit-test, and edit-placement consumers use
the admitted record; malformed published state remains retained through runtime
invalidation, while teardown uses bounded storage fields. Named-field regression
coverage is **175/175**, the complete host suite passes **1605/1605**,
NativeRoot builds with 0 errors and the existing 12 SDK type-conflict warnings,
and the focused freestanding gate passes **2/2**. This remains a freestanding,
C-like transition with no exception path, managed runtime, or consumer-facing
numeric object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG866 hardens List ColumnMetrics mutation retirement. Normal
geometry-invalidating mutations first admit the named
`MuiListColumnMetricsState`, so a malformed published metrics record and its
owned value vector remain retained through visibility invalidation; only
object teardown opts into the bounded storage reader. Named-field regression
coverage is **174/174**, the complete host suite passes **1604/1604**,
NativeRoot builds with 0 errors and the existing 12 SDK type-conflict warnings,
and the focused freestanding gate passes **2/2**. This remains a freestanding,
C-like transition with no exception path, managed runtime, or consumer-facing
numeric object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG865 hardens Listview horizontal-scroller teardown. The hidden-scroller path
admits the named `MuiListviewHorizontalScrollerState` before freeing it, so a
malformed published projection is retained instead of silently replaced during
a visibility transition. Named-field regression coverage is **104/104**, the
complete host suite passes **1603/1603**, NativeRoot builds with 0 errors and
the existing 12 SDK type-conflict warnings, and the focused freestanding gate
passes **2/2**. This remains a freestanding, C-like transition with no
exception path, managed runtime, or consumer-facing numeric object-layout
offset. Contract surface: the official [MorphOS MUI Listview
documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG864 routes Listview child render-info binding through named admission. Once
the parent `MuiListviewRenderState` is published, child binding cannot fall
back to the raw Area `RenderInfo` alias; a published null pair cannot hand
stale context to the child. Named-field regression coverage is **103/103**,
the complete host suite passes **1602/1602**, NativeRoot builds with 0 errors
and the existing 12 SDK type-conflict warnings, and the focused freestanding
gate passes **2/2**. This remains a freestanding, C-like transition with no
exception path, managed runtime, or consumer-facing numeric object-layout
offset. Contract surface: the official [MorphOS MUI Listview
documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG863 routes Listview render-context consumers through named admission. Once
`MuiListviewRenderState` is published, its complete record—including a valid
pre-setup null pair—is authoritative; the raw Area `RenderInfo` alias is
decoded only before publication, so a later scrollbar draw cannot revive
stale context behind a named struct. Named-field regression coverage is
**102/102**, the complete host suite passes **1601/1601**, NativeRoot builds
with 0 errors and the existing 12 SDK type-conflict warnings, and the focused
freestanding gate passes **2/2**. This remains a freestanding, C-like
transition with no exception path, managed runtime, or consumer-facing numeric
object-layout offset. Contract surface: the official [MorphOS MUI Listview
documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG862 routes all Listview drag consumers through named admission. Active-grab
detection, drag updates/finalization, cancellation, and pointer-release
teardown no longer trust raw drag codecs; malformed records cannot be treated
as active captures or release arbitrary guest coordinates. Focused
Listview/Floattext coverage remains **101/101**, the complete host suite passes
**1600/1600**, NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings, and the focused freestanding gate passes **2/2**. This
remains a freestanding, C-like transition with no exception path, managed
runtime, or consumer-facing numeric object-layout offset. Contract surface: the
official [MorphOS MUI Listview
documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG861 hardens Listview layout-state admission at the named-struct boundary.
The `MuiListviewLayoutState` rejects negative parent or child extents before
geometry, draw, or pointer consumers use the record. Signed coordinates remain
valid; only impossible dimensions fail closed, and malformed named state is
not rebuilt from Area aliases. Focused Listview/Floattext coverage is
**101/101**, the complete host suite passes **1600/1600**, NativeRoot builds
with 0 errors and the existing 12 SDK type-conflict warnings, and the focused
freestanding gate passes **2/2**. This remains a freestanding, C-like
transition with no exception path, managed runtime, or consumer-facing numeric
object-layout offset. Contract surface: the official [MorphOS MUI Listview
documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG860 hardens Listview selection-signal admission at the named-struct
boundary. The `MuiListviewSelectionSignalState` codec preserves guest BOOL
bytes, while semantic admission rejects impossible `SelectChange` values
before toggles, getters, or notification projections consume them. Focused
Listview/Floattext coverage is **100/100**, the complete host suite passes
**1599/1599**, NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings, and the focused freestanding gate passes **2/2**. This
remains a freestanding, C-like transition with no exception path, managed
runtime, or consumer-facing numeric object-layout offset. Contract surface:
the official [MorphOS MUI Listview
documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG859 hardens Listview click-state admission at the named-struct boundary.
The `MuiListviewClickState` codec preserves guest BOOL bytes, while semantic
admission rejects impossible `DoubleClick` and `AgainClick` values before
selection, getters, or input transitions consume them. Focused
Listview/Floattext coverage is **99/99**, the complete host suite passes
**1598/1598**, NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings, and the focused freestanding gate passes **2/2**. This
remains a freestanding, C-like transition with no exception path, managed
runtime, or consumer-facing numeric object-layout offset. Contract surface:
the official [MorphOS MUI Listview
documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG858 hardens Listview interaction-policy admission at the named-struct
boundary. The published `MuiListviewInteractionPolicyState` rejects
impossible MorphOS BOOL/enum values before normalization or setter updates can
hide them; malformed values remain visible for teardown instead of being
repaired from raw aliases. Focused Listview/Floattext coverage is **98/98**,
the complete host suite passes **1597/1597**, NativeRoot builds with 0 errors
and the existing 12 SDK type-conflict warnings, and the focused freestanding
gate passes **2/2**. This remains a freestanding, C-like transition with no
exception path, managed runtime, or consumer-facing numeric object-layout
offset. Contract surface: the official [MorphOS MUI Listview
documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG857 routes the internal List HookPolicy getter through named admission. A
malformed present policy cannot leak through the typed helper or be rebuilt
from raw hook attributes; NULL, MorphOS sentinel hooks, and opaque guest Hook
pointers remain valid inputs. Focused List coverage remains **173/173**, the
complete host suite passes **1596/1596**, NativeRoot builds with 0 errors and
the existing 12 SDK type-conflict warnings, and the focused freestanding gate
passes **2/2**. This remains a freestanding, C-like transition with no
exception path, managed runtime, or consumer-facing numeric object-layout
offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG856 hardens List `InsertPosition` admission at the named-struct boundary.
The named `MuiListInsertPositionState` rejects a present position at or above
the bounded List growth limit. The neutral initial value and stale last-result
value after clearing remain valid, while impossible unsigned projections fail
closed before getters or insertion-result publication. Focused List coverage
is **173/173**, the complete host suite passes **1596/1596**, NativeRoot builds
with 0 errors and the existing 12 SDK type-conflict warnings, and the focused
freestanding gate passes **2/2**. This remains a freestanding, C-like
transition with no exception path, managed runtime, or consumer-facing numeric
object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG855 hardens List active-cursor admission at the named-struct boundary. The
named `MuiListActiveState` rejects a present `HasActive=1` cursor whose
`Active` row is outside the current entry count. Construction still clamps raw
active requests before publishing presence, and empty-list clear/remove
transitions reset the named cursor through its structural codec. Focused List
coverage is **172/172**, the complete host suite passes **1595/1595**, NativeRoot
builds with 0 errors and the existing 12 SDK type-conflict warnings, and the
focused freestanding gate passes **2/2**. This remains a freestanding, C-like
transition with no exception path, managed runtime, or consumer-facing numeric
object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG854 hardens List ColumnMetrics admission at the named-struct boundary. The
named `MuiListColumnMetricsState` rejects a published `Width` above the signed
layout domain (`INT_MAX`) at semantic admission while the storage reader stays
permissive for teardown. Invalid present metrics fail closed and retain their
record and owned value vector. Focused List coverage remains **171/171**, the
complete host suite passes **1594/1594**, NativeRoot builds with 0 errors and
the existing 12 SDK type-conflict warnings, and the focused freestanding gate
passes **2/2**. This remains a freestanding, C-like transition with no
exception path, managed runtime, or consumer-facing numeric object-layout
offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG853 hardens List SortState admission at the named-struct boundary. The named
`MuiListSortState` rejects a published `SortColumn` outside the bounded MUI
column domain, while construction canonicalizes extreme raw requests before
normal format-column normalization. Invalid present sort state fails closed
while the record and raw projections remain retained. Focused List coverage is
**171/171**, the complete host suite passes **1594/1594**, NativeRoot builds
with 0 errors and the existing 12 SDK type-conflict warnings, and the focused
freestanding gate passes **2/2**. This remains a freestanding, C-like
transition with no exception path, managed runtime, or consumer-facing numeric
object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG852 hardens List TitleArray admission at the named-struct boundary. The
named private pointer-table record validates each present entry as a bounded
guest C string and requires its explicit NULL terminator before title getters,
row geometry, or drawing consume it. Invalid present tables fail closed while
the record and raw projections remain retained for teardown. Focused List
coverage is **170/170**, the complete host suite passes **1593/1593**,
NativeRoot builds with 0 errors and the existing 12 SDK type-conflict warnings,
and the focused freestanding gate passes **2/2**. This remains a freestanding,
C-like transition with no exception path, managed runtime, or consumer-facing
numeric object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG851 hardens List `ColumnOrder` permutation admission at the named-struct
boundary. The named `MuiListColumnOrderState` validates every guest-owned byte
as a unique, in-range display-column permutation before getters, setters, or
descriptor layout consume it. Invalid present vectors fail closed while the
record and raw projections remain retained for teardown. Focused List coverage
is **169/169**, the complete host suite passes **1592/1592**, NativeRoot builds
with 0 errors and the existing 12 SDK type-conflict warnings, and the focused
freestanding gate passes **2/2**. This remains a freestanding, C-like
transition with no exception path, managed runtime, or consumer-facing numeric
object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG850 hardens List viewport-state admission at the named-struct boundary. The
named `MuiListViewportState` requires a nonzero `LineHeight` before viewport
getters, layout, cursor, or drag consumers use it. Invalid present state fails
closed while the record and raw projections remain retained. Focused List
coverage is **168/168**, the complete host suite passes **1591/1591**,
NativeRoot builds with 0 errors and the existing 12 SDK type-conflict warnings,
and the focused freestanding gate passes **2/2**. This remains a freestanding,
C-like transition with no exception path, managed runtime, or consumer-facing
numeric object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG849 hardens List FORMAT-policy bounds admission at the named-struct boundary.
The named `MuiListFormatPolicyState` requires nonzero bounded `MaxColumns` and
installed `Columns` with `Columns <= MaxColumns`, in addition to its validated
borrowed format pointer. Invalid present policy fails closed while the record
and raw projections remain retained. Focused List coverage is **167/167**, the
complete host suite passes **1590/1590**, NativeRoot builds with 0 errors and
the existing 12 SDK type-conflict warnings, and the focused freestanding gate
passes **2/2**. This remains a freestanding, C-like transition with no
exception path, managed runtime, or consumer-facing numeric object-layout
offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG848 hardens List pool-policy admission at the named-struct boundary. The
named `MuiListPoolPolicyState` preserves `UsesExternalPool` until admission and
requires a canonical BOOL alongside the non-null opaque pool handle. Invalid
present allocator state fails closed while the policy record and raw pool
projection remain retained. Focused List coverage is **166/166**, the complete
host suite passes **1589/1589**, NativeRoot builds with 0 errors and the
existing 12 SDK type-conflict warnings, and the focused freestanding gate
passes **2/2**. This remains a freestanding, C-like transition with no
exception path, managed runtime, or consumer-facing numeric object-layout
offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG847 hardens List selection-signal admission at the named-struct boundary.
The named `MuiListSelectionSignalState` preserves `Value` until admission and
requires a canonical BOOL before selection getters, transitions, or Listview
publication consume the edge signal. Invalid present state fails closed while
the signal record and raw projection remain retained. Focused List coverage is
**165/165**, the complete host suite passes **1588/1588**, NativeRoot builds
with 0 errors and the existing 12 SDK type-conflict warnings, and the focused
freestanding gate passes **2/2**. This remains a freestanding, C-like
transition with no exception path, managed runtime, or consumer-facing numeric
object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG846 hardens List active-cursor admission at the named-struct boundary. The
named `MuiListActiveState` preserves `HasActive` until admission and requires a
canonical BOOL before active-row getters, navigation, or setters consume the
cursor. Invalid present state fails closed while the cursor and raw projection
remain retained. Focused List coverage is **164/164**, the complete host suite
passes **1587/1587**, NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings, and the focused freestanding gate passes **2/2**. This
remains a freestanding, C-like transition with no exception path, managed
runtime, or consumer-facing numeric object-layout offset. Contract surface:
the official [MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG844 hardens List redraw-state admission at the named-struct boundary. The
named `MuiListRedrawState` preserves the guest `Dirty` field until admission
and requires a canonical BOOL before quiet transitions, redraw scheduling, or
redraw-request inspection can consume the record. Invalid present state fails
closed while the record, raw projections, and coalescing counter remain
retained. Focused List coverage is **162/162**, the complete host suite passes
**1585/1585**, NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings, and the focused freestanding gate passes **2/2**. This
remains a freestanding, C-like transition with no exception path, managed
runtime, or consumer-facing numeric object-layout offset. Contract surface:
the official [MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG845 hardens List sort-state admission at the named-struct boundary. The
named sort record is now the required source for `Sort`, external-vector
sorting, and sorted insertion. A malformed present record fails closed instead
of falling back to raw column zero, while list entries and the published record
remain untouched. Focused List coverage is **163/163**, the complete host suite
passes **1586/1586**, NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings, and the focused freestanding gate passes **2/2**. This
remains a freestanding, C-like transition with no exception path, managed
runtime, or consumer-facing numeric object-layout offset. Contract surface:
the official [MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG843 hardens List click-state admission at the named-struct boundary. The
named `MuiListClickState` requires canonical `DoubleClick` and `AgainClick`
BOOLs before List getters, runtime setters, or Listview forwarding can publish
click results. Invalid present flags fail closed while the click record and raw
projections remain retained. Focused List coverage is **161/161**, the complete
host suite passes **1584/1584**, NativeRoot builds with 0 errors and the
existing 12 SDK type-conflict warnings, and the focused freestanding gate
passes **2/2**. This remains a freestanding, C-like transition with no
exception path, managed runtime, or consumer-facing numeric object-layout
offset. Contract surface: the official [MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG842 hardens List interaction-policy admission at the named-struct boundary.
The named `MuiListInteractionPolicyState` requires a canonical input BOOL and
valid MorphOS `MultiSelect` and `ScrollerPos` enum values before input or
composite-scroller consumers can use it. Invalid present values fail closed
while the policy record and raw construction projections remain retained.
Focused List coverage is **160/160**, the complete host suite passes
**1583/1583**, NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings, and the focused freestanding gate passes **2/2**. This
remains a freestanding, C-like transition with no exception path, managed
runtime, or consumer-facing numeric object-layout offset. Contract surface: the
official [MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG841 hardens List horizontal-scroller admission at the named-struct boundary.
The named `MuiListHScrollerState` requires an Auto/Always/Never policy,
canonical visibility, an exact content-minus-viewport `MaxScrollX`, and a
bounded `ScrollX` before Listview/scroller consumers can use it. Invalid
present geometry fails closed while the state record and raw construction
projection remain retained. Focused List coverage is **159/159**, the complete
host suite passes **1582/1582**, NativeRoot builds with 0 errors and the
existing 12 SDK type-conflict warnings, and the focused freestanding gate
passes **2/2**. This remains a freestanding, C-like transition with no
exception path, managed runtime, or consumer-facing numeric object-layout
offset. Contract surface: the official [MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG840 hardens List presentation-policy admission at the named-struct boundary.
The named `MuiListPresentationPolicyState` requires canonical BOOL fields, a
known MorphOS drag type, and a bounded minimum line height before List getters,
drawing, line-height, or drag consumers can use it. Invalid present values fail
closed while the policy record and raw projections remain retained. Focused
List coverage is **158/158**, the complete host suite passes **1581/1581**,
NativeRoot builds with 0 errors and the existing 12 SDK type-conflict warnings,
and the focused freestanding gate passes **2/2**. This remains a freestanding,
C-like transition with no exception path, managed runtime, or consumer-facing
numeric object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG839 hardens List pool-policy admission at the named-struct boundary. The
named `MuiListPoolPolicyState.Pool` handle requires a non-NULL opaque Exec pool
capability before List hooks or pooled allocation can consume the policy. A
present NULL handle fails closed while the policy record and raw compatibility
projection remain retained. Focused List coverage is **157/157**, the complete
host suite passes **1580/1580**, NativeRoot builds with 0 errors and the
existing 12 SDK type-conflict warnings, and the focused freestanding gate
passes **2/2**. This remains a freestanding, C-like transition with no
exception path, managed runtime, or consumer-facing numeric object-layout
offset. Contract surface: the official [MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG838 hardens List font pointer admission at the named-struct boundary. The
borrowed `MuiListFontState.Font` pointer now requires NULL inheritance or a
mapped guest `TextFont` structure before List measurement and drawing can
consume it. Stale present font pointers fail closed while the font policy block
remains retained. Focused List coverage is **156/156**, the complete host suite
passes **1579/1579**, NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings, and the freestanding gate passes **2/2**. This remains
a freestanding, C-like transition with no exception path, managed runtime, or
consumer-facing numeric object-layout offset. Contract surface: the official
[MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG837 hardens List title pointer admission at the named-struct boundary. The
named `MuiListTitleState.Value` accepts only NULL, the MorphOS TRUE custom-title
sentinel, or a bounded mapped guest C string before title-row geometry and
drawing consume it. Stale or unterminated present title pointers fail closed
while the title record remains retained. Focused List coverage is **155/155**,
the complete host suite passes **1578/1578**, NativeRoot builds with 0 errors
and the existing 12 SDK type-conflict warnings, and the freestanding gate
passes **2/2**. This remains a freestanding, C-like transition with no
exception path, managed runtime, or consumer-facing numeric object-layout
offset. Contract surface: the official [MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG836 hardens List FORMAT pointer admission at the named-struct boundary. The
caller-owned `MuiListFormatPolicyState.Format` pointer must be a bounded,
mapped guest C string before format descriptors, layout, or display code can
consume the named policy. NULL remains the documented empty-format form. Stale
or unterminated present pointers fail closed while the policy block remains
retained. Focused List coverage is **154/154**, the complete host suite passes
**1577/1577**, NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings, and the freestanding gate passes **2/2**. This remains
a freestanding, C-like transition with no exception path, managed runtime, or
consumer-facing numeric object-layout offset. Contract surface: the official
[MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG835 hardens reverse Listview ownership at the named-struct boundary. The
`MuiListviewOwnerState.Owner` pointer must identify a live Listview that
actually adopts the List before child selection can mirror `SelectChange` into
it. Null, stale, unrelated, and non-Listview owners fail closed while the
guest owner record remains retained. Focused Listview/Floattext coverage is
**97/97**, the complete host suite passes **1576/1576**, NativeRoot builds with
0 errors and the existing 12 SDK type-conflict warnings, and the freestanding
gate passes **2/2**. This remains a freestanding, C-like transition with no
exception path, managed runtime, or consumer-facing numeric object-layout
offset. Contract surface: the official [MorphOS MUI Listview
documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG834 hardens Listview child-link admission at the named-struct boundary. The
`MuiListviewChildState.Child` pointer requires a live List-backed object before
child resolution, List forwarding, selection/layout paths, or disposal can
consume it. A present but malformed or stale child record fails closed, and
generic `MUIA_Listview_List` returns neutral zero instead of exposing the raw
compatibility scalar. Focused Listview/Floattext coverage is **96/96**, the
complete host suite passes **1575/1575**, NativeRoot builds with 0 errors and
the existing 12 SDK type-conflict warnings, and the freestanding gate passes
**2/2**. This remains a freestanding, C-like transition with no exception path,
managed runtime, or numeric object-layout offset. Contract surface: the
official [MorphOS MUI Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG833 hardens external Listview scroller connection admission at the
named-struct boundary. `MuiListviewExternalScrollerConnectionState` requires a
live Prop or Scrollbar destination before reconnect, notification teardown, or
object-loss cleanup can consume the pointer. Null, stale, and non-scroller
destinations fail closed while the guest connection block remains retained.
Focused external-scroller coverage is **11/11**, the complete host suite
passes **1574/1574**, NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings, and the freestanding gate passes **2/2**. This remains
a freestanding, C-like transition with no exception path, managed runtime, or
numeric object-layout offset. Contract surface: the official [MorphOS MUI
Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG832 hardens horizontal Listview scroller-drag admission at the named-struct
boundary. `MuiListviewHorizontalScrollerDragState` validates its known flag
mask, inactive zero-state, non-negative active grab offset, and scroll origin
against the current named horizontal projection before a pointer gesture can
reuse the published record. Unknown transition bits and out-of-range origins
fail closed without replacing the guest block. Focused Listview/Floattext
coverage is **95/95**, the complete host suite passes **1573/1573**, NativeRoot
builds with 0 errors and the existing 12 SDK type-conflict warnings, and the
freestanding gate passes **2/2**. This remains a freestanding, C-like
transition with no exception path, managed runtime, or numeric object-layout
offset. Contract surface: the official [MorphOS MUI Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG831 hardens vertical Listview scroller-drag admission at the named-struct
boundary. `MuiListviewScrollerDragState` validates its known flag mask,
inactive zero-state, and non-negative active grab offset/first-row origin
before a pointer gesture can reuse the published record. Unknown transition
bits and incoherent active coordinates fail closed without replacing the guest
block. Focused Listview/Floattext coverage is **94/94**, the complete host
suite passes **1572/1572**, NativeRoot builds with 0 errors and the existing 12
SDK type-conflict warnings, and the freestanding gate passes **2/2**. This
remains a freestanding, C-like transition with no exception path, managed
runtime, or numeric object-layout offset. Contract surface: the official
[MorphOS MUI Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG830 hardens Listview drag-state admission at the named-struct boundary.
`MuiListviewDragState` validates its flag mask, inactive `Source`/`Target`
sentinels, active source row, and moved-target relationship before a pointer
gesture can reuse the published record. Unknown flags and incoherent row
transitions fail closed without replacing the guest block. Focused
Listview/Floattext coverage is **93/93**, the complete host suite passes
**1571/1571**, NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings, and the freestanding gate passes **2/2**. This remains
a freestanding, C-like transition with no exception path, managed runtime, or
numeric object-layout offset. Contract surface: the official
[MorphOS MUI Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG829 hardens Listview render-context admission at the named-struct boundary.
`MuiListviewRenderState` requires its published `RenderInfo`/`RastPort` pair to
agree with the decoded graphics record. The valid pre-setup null pair remains
allowed; mismatched or unmapped present contexts fail closed before layout,
drawing, or child render binding consumes them. Focused Listview/Floattext
coverage is **92/92**, the complete host suite passes **1570/1570**, NativeRoot
builds with 0 errors and the existing 12 SDK type-conflict warnings, and the
freestanding gate passes **2/2**. This remains a freestanding, C-like
transition with no exception path, managed runtime, or numeric object-layout
offset. Contract surface: the official
[MorphOS MUI Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG828 hardens Listview horizontal scroller admission at the named-struct
boundary. `MuiListviewHorizontalScrollerState` rejects inverted or out-of-track
geometry, impossible `ScrollX > MaxScrollX`, and inconsistent content/view
maximums before drawing or thumb input consumes the projection. Always-visible
scrollers with narrower content remain valid with a zero maximum. Focused
Listview/Floattext coverage is **91/91**, the complete host suite passes
**1569/1569**, NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings, and the freestanding gate passes **2/2**. This remains
a freestanding, C-like transition with no exception path, managed runtime, or
numeric object-layout offset. Contract surface: the official
[MorphOS MUI Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG827 hardens Listview vertical scroller admission at the named-struct
boundary. `MuiListviewScrollerState` rejects impossible visible-state ranges
before geometry, thumb input, or first-position publication consumes them;
normal records require `First <= MaxFirst <= Entries`, while the hidden
MorphOS sentinel remains valid only with a zero maximum. Focused
Listview/Floattext coverage is **90/90**, the complete host suite passes
**1568/1568**, NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings, and the freestanding gate passes **2/2**. This remains
a freestanding, C-like transition with no exception path, managed runtime, or
numeric object-layout offset. Contract surface: the official
[MorphOS MUI Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG826 hardens List header/index admission at the named-struct boundary. The
`MuiListHeaderState` storage reader validates bounded capacity/count, index
mapping, and optional image-head mapping before List consumers trust the
record. Malformed present headers fail closed during normal construction,
lookup, mutation, and entry access; cleanup can still retire structurally
valid index/slot storage after cookie corruption. Focused List coverage is
**153/153**, the complete host suite passes **1567/1567**, NativeRoot builds
with 0 errors and 0 warnings, and the freestanding gate passes **2/2**. This
remains a freestanding, C-like transition with no exception path, managed
runtime, or numeric object-layout offset. Contract surface: the official
[MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG825 hardens List ViewportState admission at the named-struct boundary. The
named viewport record distinguishes absent state from malformed present
`MuiListViewportState`; malformed state fails closed before layout, cursor
fallbacks, Active/First transitions, or drop-mark publication can rebuild it
from raw projections. Focused List coverage is **152/152** and the complete
host suite passes **1566/1566**; NativeRoot builds with 0 errors and the
existing 12 SDK type-conflict warnings. This remains a freestanding, C-like
transition with no exception path, managed runtime, or numeric object-layout
offset. Contract surface: the official [MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG824 hardens List RedrawState admission at the named-struct boundary. The
coalescing record distinguishes absent state from malformed present
`MuiListRedrawState`; malformed state fails closed before Quiet policy changes,
mutation coalescing, or platform redraw scheduling. Focused List coverage is
**151/151** and the complete host suite passes **1565/1565**; NativeRoot builds
with 0 errors and the existing 12 SDK type-conflict warnings. This remains a
freestanding, C-like transition with no exception path, managed runtime, or
numeric object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG823 hardens List ColumnMetrics admission at the named-struct boundary. The
derived metrics cache distinguishes absent state from malformed present
`MuiListColumnMetricsState`; malformed state fails closed during refresh and
geometry, while bounded teardown validates and retires its owned guest value
vector. Focused List coverage is **150/150** and the complete host suite passes
**1564/1564**; NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings. This remains a freestanding, C-like transition with no
exception path, managed runtime, or numeric object-layout offset. Contract
surface: the official [MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG822 hardens List ColumnVisibility admission at the named-struct boundary.
The owned List distinguishes an absent hidden-column state from malformed
present `MuiListColumnVisibilityState`; malformed state fails closed before
Hide/Show setters, layout, or display-column consumers can rebuild the bitmap
from raw aliases, while true absence remains a bounded bootstrap case.
Focused List coverage is **149/149** and the complete host suite passes
**1563/1563**; NativeRoot builds with 0 errors and 0 warnings. This remains a
freestanding, C-like transition with no exception path, managed runtime, or
numeric object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG821 hardens List ColumnOrder admission at the named-struct boundary. The
owned List distinguishes an absent order state from malformed present
`MuiListColumnOrderState`; malformed state fails closed before order getters,
display-column mapping, or setters can rebuild the copied permutation from the
raw caller `BYTE *`, while bounded cleanup still retires validated vector
storage. Focused List coverage is **148/148** and the complete host suite
passes **1562/1562**; NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings. This remains a freestanding, C-like transition with no
exception path, managed runtime, or numeric object-layout offset. Contract
surface: the official [MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG820 hardens List TitleArray admission at the named-struct boundary. The
owned List distinguishes an absent title-array state from malformed present
`MuiListTitleArrayState`; malformed state fails closed before title getters,
rendering/measurement, or setters can rebuild private pointer-table ownership
from the raw caller table, while bounded cleanup still retires validated
pointer storage. Focused List coverage is **147/147** and the complete host
suite passes **1561/1561**; NativeRoot builds with 0 errors and the existing 12
SDK type-conflict warnings. This remains a freestanding, C-like transition
with no exception path, managed runtime, or numeric object-layout offset.
Contract surface: the official [MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG819 hardens List FontState admission at the named-struct boundary. The owned
List distinguishes an absent `MuiListFontState` from malformed present state;
malformed state fails closed before font setters or measurement/render cursors
can rebuild it from the raw caller-owned `Font` alias, while true absence
remains a bounded bootstrap case. Focused List coverage is **146/146** and the
complete host suite passes **1560/1560**; NativeRoot builds with 0 errors and
the existing 12 SDK type-conflict warnings. This remains a freestanding,
C-like transition with no exception path, managed runtime, or numeric
object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG818 hardens List FORMAT-policy admission at the named-struct boundary. The
owned List distinguishes an absent `MuiListFormatPolicyState` from malformed
present state; malformed state fails closed before format getters,
descriptor/layout/sort consumers, or setters can rebuild it from raw
`Format`/`MaxColumns` aliases, while true absence remains a bounded bootstrap
case and teardown still retires descriptor blocks. Focused List coverage is
**145/145** and the complete host suite passes **1559/1559**; NativeRoot builds
with 0 errors and the existing 12 SDK type-conflict warnings. This remains a
freestanding, C-like transition with no exception path, managed runtime, or
numeric object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG817 hardens List interaction-policy admission at the named-struct boundary.
The owned List distinguishes an absent `MuiListInteractionPolicyState` from
malformed present state; malformed state fails closed before direct interaction
or composite consumers can recreate it from raw construction aliases, while
true absence remains a bounded bootstrap case. Focused List coverage is
**144/144** and the complete host suite passes **1558/1558**; NativeRoot builds
with 0 errors and the existing 12 SDK type-conflict warnings. This remains a
freestanding, C-like transition with no exception path, managed runtime, or
numeric object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG816 hardens List pool-policy admission at the named-struct boundary. The
owned List distinguishes an absent `MuiListPoolPolicyState` from malformed
present state; malformed state fails closed before pool getters or hook
consumers can replace allocator ownership from raw attributes, and teardown
retires malformed guest records without interpreting untrusted ownership bits.
Focused List coverage is **143/143** and the complete host suite passes
**1557/1557**; NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings. This remains a freestanding, C-like transition with no
exception path, managed runtime, or numeric object-layout offset. Contract
surface: the official [MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG815 hardens List horizontal-scroller admission at the named-struct boundary.
The owned List distinguishes an absent `MuiListHScrollerState` from malformed
present state; malformed state fails closed before viewport/scroll consumers or
state creation can replace it from the raw `HScrollerVisibility` projection,
while true absence remains a bounded bootstrap case. Focused List coverage is
**142/142** and the complete host suite passes **1556/1556**; NativeRoot builds
with 0 errors and the existing 12 SDK type-conflict warnings. This remains a
freestanding, C-like transition with no exception path, managed runtime, or
numeric object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG814 hardens List presentation-policy admission at the named-struct boundary.
The owned List distinguishes an absent `MuiListPresentationPolicyState` from
malformed present state; malformed state fails closed before policy getters,
layout, rendering, or runtime setters can replace it from raw attributes,
while true absence remains a bounded bootstrap case. Focused List coverage is
**141/141** and the complete host suite passes **1555/1555**; NativeRoot builds
with 0 errors and the existing 12 SDK type-conflict warnings. This remains a
freestanding, C-like transition with no exception path, managed runtime, or
numeric object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG813 hardens List hook-policy admission at the named-struct boundary. The owned
List distinguishes an absent `MuiListHookPolicyState` from malformed present
state; malformed state fails closed before generic hook getters,
construction/destruction/display/compare dispatch, or hook setters can replace
it from raw attributes, while true absence remains a bounded bootstrap case.
Focused List coverage is **140/140** and the complete host suite passes
**1554/1554**; NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings. This remains a freestanding, C-like transition with no
exception path, managed runtime, or numeric object-layout offset. Contract
surface: the official [MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG812 hardens List click-state admission at the named-struct boundary. The
owned List distinguishes an absent `MuiListClickState` from malformed present
state; malformed state fails closed before click getters, Listview click
publication, or click setters can replace it from raw projections, while true
absence remains a bounded bootstrap case. Focused List coverage is **139/139**
and the complete host suite passes **1553/1553**; NativeRoot builds with 0
errors and the existing 12 SDK type-conflict warnings. This remains a
freestanding, C-like transition with no exception path, managed runtime, or
numeric object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG811 hardens List sort-state admission at the named-struct boundary. The owned
List distinguishes an absent `MuiListSortState` from malformed present state;
malformed state fails closed before `SortColumn`, `TitleClick`, title-click
handling, or sort-column normalization can replace it from raw projections,
while true absence remains a bounded bootstrap case. Focused List coverage is
**138/138** and the complete host suite passes **1552/1552**; NativeRoot builds
with 0 errors and the existing 12 SDK type-conflict warnings. This remains a
freestanding, C-like transition with no exception path, managed runtime, or
numeric object-layout offset. Contract surface: the official [MorphOS MUI List
documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG810 hardens List title admission at the named-struct boundary. The owned List
distinguishes an absent `MuiListTitleState` from malformed present state;
malformed state fails closed before `Title` getters, setters, or title-row
drawing cursors can replace it from the raw scalar, while true absence remains
a bounded bootstrap case. Focused List coverage is **138/138** and the complete
host suite passes **1551/1551**; NativeRoot builds with 0 errors and the
existing 12 SDK type-conflict warnings. This remains a freestanding, C-like
transition with no exception path, managed runtime, or numeric object-layout
offset. Contract surface: the official [MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG809 hardens List `InsertPosition` admission at the named-struct boundary. The
getter-only insertion result distinguishes an absent
`MuiListInsertPositionState` from malformed present state; malformed state
fails closed before `InsertPosition` getters or insertion-result publication
can replace it from the raw scalar, while true absence remains materializable.
Focused List coverage is **136/136** and the complete host suite passes
**1550/1550**; NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings. This remains a freestanding, C-like transition with no
exception path, managed runtime, or numeric object-layout offset. Contract
surface: the official [MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG808 hardens List active-row admission at the named-struct boundary. The owned
List distinguishes an absent `MuiListActiveState` from malformed present state;
malformed state fails closed before `Active` getters, setters, or navigation can
rebuild it from the raw scalar, while true absence remains materializable. The
empty-list public zero and internal `-1` sentinel behavior is preserved.
Focused Listview/Floattext coverage is **89/89** and the complete host suite
passes **1549/1549**; NativeRoot builds with 0 errors and 0 warnings. This
remains a freestanding, C-like transition with no exception path, managed
runtime, or numeric object-layout offset. Contract surface: the official
[MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG807 hardens List selection-signal admission at the named-struct boundary.
The owned List distinguishes an absent `MuiListSelectionSignalState` from
malformed present state; malformed state fails closed before `SelectChange`
getters or selection transitions can rebuild it from the raw scalar, while true
absence remains materializable. Focused Listview/Floattext coverage is **88/88**
and the complete host suite passes **1548/1548**; NativeRoot builds with 0
errors and the existing 12 SDK type-conflict warnings. This remains a
freestanding, C-like transition with no exception path, managed runtime, or
numeric object-layout offset. Contract surface: the official [MorphOS MUI List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG806 hardens the Listview reverse-owner link at the named-struct boundary.
The List child now stores its Listview parent in `MuiListviewOwnerState`; a
present malformed record fails closed before child selection can mirror
`SelectChange` to an untrusted parent, while true absence remains materializable
during adoption. Focused Listview/Floattext coverage is **87/87** and the
complete host suite passes **1547/1547**; NativeRoot builds with 0 errors and
the existing 12 SDK type-conflict warnings. This remains a freestanding,
C-like transition with no exception path, managed runtime, or numeric
object-layout offset. Contract surface: the official [MorphOS MUI Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG805 hardens Listview child-ownership admission at the named-struct boundary.
A present malformed `MuiListviewChildState` now fails closed before child
lookup or `MUIA_Listview_List` projection can fall back to the raw parent
pointer; true absence remains materializable during construction. Focused
Listview/Floattext coverage is **85/85** and the complete host suite passes
**1545/1545**; NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings. This remains a freestanding, C-like transition with
no exception path, managed runtime, or numeric object-layout offset.
Contract surface: the official [MorphOS MUI Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG804 hardens Listview layout-state admission at the named-struct boundary. A
present malformed `MuiListviewLayoutState` now fails closed before geometry
consumers can bypass it through raw Area fallback or publish a replacement;
true absence remains materializable. Focused Listview/Floattext coverage is
**84/84** and the complete host suite passes **1544/1544**; NativeRoot builds
with 0 errors and the existing 12 SDK type-conflict warnings. This remains a
freestanding, C-like transition with no exception path, managed runtime, or
numeric object-layout offset. Contract surface: the official [MorphOS MUI Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG803 hardens Listview render-state admission at the named-struct boundary. A
present malformed `MuiListviewRenderState` now fails closed before layout or
draw fallback can replace it from the raw `RenderInfo` alias; valid
uninitialized records retain the normal setup fallback. Focused
Listview/Floattext coverage is **83/83** and the complete host suite passes
**1543/1543**; NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings. This remains a freestanding, C-like transition with
no exception path, managed runtime, or numeric object-layout offset.
Contract surface: the official [MorphOS MUI Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG802 hardens Listview horizontal viewport-state admission at the named-struct
boundary. A present malformed
`MuiListviewHorizontalScrollerState` now fails closed before geometry
recomputation can publish a replacement; true absence remains materializable.
Focused Listview/Floattext coverage is **82/82** and the complete host suite
passes **1542/1542**; NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings. This remains a freestanding, C-like transition with
no exception path, managed runtime, or numeric object-layout offset.
Contract surface: the official [MorphOS MUI Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG801 hardens Listview vertical viewport-state admission at the named-struct
boundary. A present malformed `MuiListviewScrollerState` now fails closed
before geometry recomputation or `First` updates can publish a replacement;
true absence remains materializable. Focused Listview/Floattext coverage is
**81/81** and the complete host suite passes **1541/1541**; NativeRoot builds
with 0 errors and the existing 12 SDK type-conflict warnings. This remains a
freestanding, C-like transition with no exception path, managed runtime, or
numeric object-layout offset. Contract surface: the official [MorphOS MUI Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG800 hardens Listview list drag-sort admission at the named-struct boundary.
A present malformed `MuiListviewDragState` now fails closed before
`SELECTDOWN` can arm a replacement drag; true absence remains materializable.
Focused Listview/Floattext coverage is **80/80** and the complete host suite
passes **1540/1540**; NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings. This remains a freestanding, C-like transition with
no exception path, managed runtime, or numeric object-layout offset.
Contract surface: the official [MorphOS MUI Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG799 hardens Listview horizontal scroller drag admission at the named-struct
boundary. A present malformed
`MuiListviewHorizontalScrollerDragState` now fails closed before `SELECTDOWN`
can arm a replacement pointer grab; true absence remains materializable.
Focused Listview/Floattext coverage is **79/79** and the complete host suite
passes **1539/1539**; NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings. This remains a freestanding, C-like transition with
no exception path, managed runtime, or numeric object-layout offset.
Contract surface: the official [MorphOS MUI Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG798 hardens Listview vertical scroller drag admission at the named-struct
boundary. A present malformed `MuiListviewScrollerDragState` now fails closed
before `SELECTDOWN` can arm a replacement pointer grab; true absence remains
materializable. Focused Listview/Floattext coverage is **78/78** and the
complete host suite passes **1538/1538**; NativeRoot builds with 0 errors and
the existing 12 SDK type-conflict warnings. This remains a freestanding,
C-like transition with no exception path, managed runtime, or numeric
object-layout offset. Contract surface: the official [MorphOS MUI Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG797 hardens Listview external-scroller connection admission at the
named-struct boundary. A present malformed
`MuiListviewExternalScrollerConnectionState` fails closed before reconnect
teardown, preserving the guest record and notification recipe; true absence
remains materializable. Focused external-scroller coverage is **10/10**,
Listview/Floattext coverage remains **77/77**, and the complete host suite
passes **1537/1537**; NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings. This remains a freestanding, C-like transition with
no exception path, managed runtime, or numeric object-layout offset.
Contract surface: the official [MorphOS MUI Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG796 hardens Listview click-state admission at the named-struct boundary. A
present malformed `MuiListviewClickState` fails closed before click or
keyboard selection can mutate child state; click getters reject malformed
records while true absence remains materializable. Focused Listview/Floattext
coverage is **77/77** and the complete host suite passes **1536/1536**;
NativeRoot builds with 0 errors and the existing 12 SDK type-conflict
warnings. This remains a freestanding, C-like transition with no exception
path, managed runtime, or numeric object-layout offset. Contract surface: the
official [MorphOS MUI Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG795 hardens Listview selection-signal admission at the named-struct boundary.
A present malformed `MuiListviewSelectionSignalState` fails closed before
toggle publication or `SelectChange` reads; true absence remains materializable
for setup. Focused Listview/Floattext coverage is **76/76** and the complete
host suite passes **1535/1535**; NativeRoot builds with 0 errors and the
existing 12 SDK type-conflict warnings. This remains a freestanding, C-like
transition with no exception path, managed runtime, or numeric object-layout
offset. Contract surface: the official
[MorphOS MUI Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG794 hardens Listview interaction-policy admission at the named-struct
boundary. A present malformed `MuiListviewInteractionPolicyState` fails closed
before policy setters can repair it from raw aliases; input, layout, and drag
consumers use inert policy values, while true absence remains materializable.
Focused Listview/Floattext coverage is **75/75** and the complete host suite
passes **1534/1534**; NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings. This remains a freestanding, C-like transition with
no exception path, managed runtime, or numeric object-layout offset. Contract
surface: the official
[MorphOS MUI Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG793 hardens Floattext policy admission at the named-struct boundary. A
present malformed `MuiFloattextPolicyState` fails closed before `FtText`
ownership or raw projections can change, and typed policy pointers must match
their object-owned bounded C strings. Focused Floattext coverage is **74/74**
and the complete host suite passes **1533/1533**; NativeRoot builds with 0
errors and the existing 12 SDK type-conflict warnings. This remains a
freestanding, C-like transition with no exception path, managed runtime, or
numeric object-layout offset. Contract surface: the official
[MorphOS MUI Floattext documentation](https://morphos-team.net/sdk/MUI/MUI_Floattext.html).

MG792 extends Stringscroll recompute admission to policy setters. Before
publishing policy projections, the transition validates named layout,
composition, and present scrollbar Prop state, so malformed dependent state
cannot leave a partial policy change visible. Focused Stringscroll coverage
is **49/49** and the complete host suite passes **1532/1532**; NativeRoot
builds with 0 errors and the existing 12 SDK type-conflict warnings. This
remains a freestanding, C-like transition with no exception path, managed
runtime, or numeric object-layout offset. Contract surface: the official
[MorphOS MUI Stringscroll documentation](https://morphos-team.net/sdk/MUI/MUI_Stringscroll.html).

MG791 adds Stringscroll setter preflight at the named-state boundary. Before
replacing the owned String buffer, the transition validates layout,
composition, and any present scrollbar Prop range records consumed by
recompute; malformed dependent state therefore fails closed without changing
the public pointer. Focused Stringscroll coverage is **48/48** and the
complete host suite passes **1531/1531**; NativeRoot builds with 0 errors and
the existing 12 SDK type-conflict warnings. This remains a freestanding,
C-like transition with no exception path, managed runtime, or numeric
object-layout offset. Contract surface: the official [MorphOS MUI Stringscroll documentation](https://morphos-team.net/sdk/MUI/MUI_Stringscroll.html).

This directory is reserved for CopperOS's clean-room implementation of the
public MorphOS 3.20 MUI contract for the `MorphOs320M68k` profile. It targets
freestanding MC68000 code and uses the public SDK ABI owned by
`CopperSharp.Sdk.Amiga`; it is not a generic/classic Amiga MUI implementation
and does not copy MorphOS implementation code.

MG790 hardens the Stringscroll public `String` getter at the named-state
boundary. The getter now admits `MuiStringscrollStateRecord`, validates the
object-owned bounded C string, and rejects divergence between the canonical
typed pointer and its private ownership block. Focused Stringscroll coverage
is **47/47** and the complete host suite passes **1530/1530**; NativeRoot
builds with 0 errors and the existing 12 SDK type-conflict warnings. This
remains a freestanding, C-like transition with no exception path, managed
runtime, or numeric object-layout offset. Contract surface: the official
[MorphOS MUI Stringscroll documentation](https://morphos-team.net/sdk/MUI/MUI_Stringscroll.html).

MG789 hardens String.mui scroll metrics at the named-struct boundary. A
present malformed `MuiStringScrollMetricsStateRecord` fails before
`ScrollLeft`/`ScrollTop` clamping can mutate raw projections, and failed typed
publication restores both scalar projections. The metrics dataspace key is
isolated from CommonControlCore's Text Unicode record to prevent cross-record
collisions. Focused metrics coverage is **9/9** and the complete host suite
passes **1529/1529**; NativeRoot builds with 0 errors and the existing 12 SDK
type-conflict warnings. This remains a freestanding, C-like transition with
no exception path, managed runtime, or numeric object-layout offset. Contract
surface: the official [MorphOS MUI String documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

MG788 closes the remaining Stringscroll composition inspection ambiguity.
Consumers distinguish an absent `MuiStringscrollCompositionRecord` from
malformed present state; malformed composition fails closed across scrolling,
layout, input, drawing, and cleanup instead of silently falling back to legacy
behavior. Focused Stringscroll coverage is 46/46 and the complete host suite
passes **1528/1528**. This remains a freestanding, C-like transition with no
exception path, managed runtime, or numeric object-layout offset. Contract
surface: the official [MorphOS MUI Stringscroll
documentation](https://morphos-team.net/sdk/MUI/MUI_Stringscroll.html).

MG786 closes the shared Stringscroll policy reader boundary. A present
malformed `MuiStringscrollPolicyRecord` fails closed for policy, scrolling,
and input consumers instead of falling back to raw values; recognized
Stringscroll getter failure no longer recurses into the generic getter.
Focused Stringscroll coverage is 44/44 and the complete host suite passes
**1526/1526**. This remains a freestanding, C-like transition with no
exception path, managed runtime, or numeric object-layout offset. Contract
surface: the official [MorphOS MUI Stringscroll documentation](https://morphos-team.net/sdk/MUI/MUI_Stringscroll.html).

MG785 makes the Stringscroll named state seam fail closed globally. A present
malformed `MuiStringscrollStateRecord` is no longer treated as absent by
scrolling, input, layout, or drawing consumers; the shared raw fallback is
available only when the named record is truly absent. Focused Stringscroll
coverage is 43/43 and the complete host suite passes **1525/1525**. This
remains a freestanding, C-like transition with no exception path, managed
runtime, or numeric object-layout offset. Contract surface: the official
[MorphOS MUI Stringscroll documentation](https://morphos-team.net/sdk/MUI/MUI_Stringscroll.html).

MG784 validates Stringscroll's dependent `MuiStringscrollCompositionRecord`
before scrollbar-policy changes begin. A malformed present composition record
fails closed without changing policy or scrollbar projections; an absent
record remains the lazy construction case. Focused Stringscroll coverage is
42/42 and the complete host suite passes **1524/1524**. This remains a
freestanding, C-like transition with no exception path, managed runtime, or
numeric object-layout offset. Contract surface: the official [MorphOS MUI
Stringscroll documentation](https://morphos-team.net/sdk/MUI/MUI_Stringscroll.html).

MG783 makes Stringscroll policy updates fail closed at the named-state
boundary. When a `MuiStringscrollPolicyRecord` exists, it must validate before
`NoInput`, scrollbar, or other policy projections can change; legacy absence
retains the compatibility raw fallback. Focused Stringscroll coverage is
41/41 and the complete host suite passes **1523/1523**. This remains a
freestanding, C-like transition with no exception path, managed runtime, or
numeric object-layout offset. Contract surface: the official [MorphOS MUI
Stringscroll documentation](https://morphos-team.net/sdk/MUI/MUI_Stringscroll.html).

MG782 makes Stringscroll geometry and render context updates fail closed at
their named-state boundaries. Existing `MuiStringscrollLayoutStateRecord` and
`MuiStringscrollRenderStateRecord` instances must validate before
`Width`/`Height` or `RenderInfo`/`Font` raw projections are changed; typed
publication failure restores the prior raw projection. Focused coverage is
40/40 and the complete host suite passes **1522/1522**. This remains a
freestanding, C-like transition with no exception path, managed runtime, or
numeric object-layout offset. Contract surface: the official [MorphOS MUI
Stringscroll documentation](https://morphos-team.net/sdk/MUI/MUI_Stringscroll.html).

MG781 makes Stringscroll text replacement struct-first at the named-state
admission boundary. An existing `MuiStringscrollStateRecord` must have the
expected size and valid fields before the owned string buffer is replaced;
recognized Stringscroll attributes no longer fall through to generic raw
storage when a typed transition fails. The malformed-state regression and
complete host suite pass **1520/1520**. This remains a freestanding,
C-like transition with no exception path, managed runtime, or numeric
object-layout offset. Contract surface: the official [MorphOS MUI Stringscroll
documentation](https://morphos-team.net/sdk/MUI/MUI_Stringscroll.html).

MG780 makes String `Accept`/`Reject` mutation failure-atomic across the
named filter pair. The setter snapshots `MuiStringFilterState` before changing
raw storage, rejects malformed paired state before mutation, and restores the
snapshot if publication fails. The focused malformed-pair regression and
complete host suite pass **1519/1519**. This remains a struct-first,
freestanding C-like transition with no exception path, managed runtime, or
numeric object-layout offset. Contract surface: the official [MorphOS MUI
documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

MG779 makes omitted `MUIA_String_Integer64` a handled NULL state. A fresh
String without an explicit QUAD tag now returns a valid zero pointer through
generic Get/OM_GET; malformed or partially materialized named state remains
fail-closed. Focused host coverage includes the omitted-tag and bounded
replacement paths, and the complete host suite passes **1518/1518**. This
remains a struct-first, freestanding C-like transition with no exception path,
managed runtime, or numeric object-layout offset. Contract surface: the
official [MorphOS MUI documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

MG778 makes `MUIA_String_Integer64` replacement failure-atomic. The named
owned QUAD is materialised before the shared String contents buffer is changed;
if later allocation or publication fails, the prior `High`/`Low` value and
public pointer are restored. The bounded-arena regression covers the
NULL-to-QUAD failure boundary and the complete host suite passes **1517/1517**.
This remains a struct-first, freestanding C-like transition with no exception
path, managed runtime, or numeric object-layout offset. Contract surface: the
official [MorphOS MUI documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

MG777 makes copied C-string replacement failure-atomic across Text PreParse/
Contents, String placeholder, Numeric format, Gauge info text, and Levelmeter
label. When an owned replacement buffer cannot be allocated, each setter
restores its previous raw pointer so the named guest record and public Get/
OM_GET projection remain coherent. The bounded Text PreParse regression covers
the shared path and the complete host suite passes **1516/1516**. This remains
a struct-first, freestanding C-like transition with no exception path, managed
runtime, or numeric object-layout offset. Contract surface: the official
[MorphOS MUI documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

MG776 makes `MUIA_String_Contents` replacement failure-atomic. Runtime and
persistence imports restore the previous raw pointer when the replacement
owned buffer cannot be admitted, keeping the named `MuiStringContentsState`
and public Get/OM_GET projection coherent. The focused bounded-arena
regression covers both paths and the complete host suite passes **1515/1515**.
This remains a struct-first, freestanding C-like transition with no exception
path, managed runtime, or numeric object-layout offset. Contract surface: the
official [MorphOS MUI documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

MG775 makes String construction failure-atomic for the documented
`MUIA_String_Integer` seed. If its owned contents buffer cannot be allocated or
formatted, construction fails rather than returning a live object whose named
Integer/Contents projections disagree. The bounded-arena regression and full
host suite pass **1514/1514**. This is a struct-first, freestanding C-like
transition with no exception path, managed runtime, or numeric object-layout
offset. Contract surface: the official [MorphOS MUI
documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

MG774 closes the NULL ownership boundary for copied Text PreParse, Numeric
format, Gauge info text, and Levelmeter label strings. Each NULL Set retires
the matching object-owned dataspace and publishes a NULL pointer through its
named fixed-width guest record, keeping Get/OM_GET and rendering coherent
without a private widget offset. Host coverage is **1513/1513**. The focused
four-record native HUNK emits **6848 / 6836 / 6844 bytes** for MC68000/020/040
and NativeExecution returns **42** after **3253 instructions / 32752 cycles**,
with zero framework features, managed allocations, and relocations. Contract
surface: the official [MorphOS MUI documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

MG773 extends struct-first guest-pointer admission to every copied caller-owned
C-string attribute: Text PreParse, Numeric format, Gauge info text, Levelmeter
label, String placeholder, and String/Text contents. The shared validator runs
for construction tags and runtime setters, requiring a mapped terminator before
raw publication or owned copying; malformed pointers are rejected atomically
and NULL remains the explicit empty state. Host coverage is **1512/1512**. The
focused native admission HUNK emits **1384 / 1420 / 1380 bytes** for
MC68000/020/040 and NativeExecution returns **42** after **935 instructions /
8418 cycles**, with zero framework features, managed allocations, and
relocations. Contract surface: the official [MorphOS MUI_String
documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

MG772 adds struct-first guest-pointer admission for String and Text contents.
Non-NULL caller-owned C strings must have a mapped terminator before raw
attributes or owned copies are changed; malformed contents are rejected without
mutating the previous value, while NULL remains the explicit empty state. Host
coverage is **1511/1511**. The focused native validator HUNK emits **1320 /
1352 / 1316 bytes** for MC68000/020/040 and NativeExecution returns **42** after
**392 instructions / 3890 cycles**, with zero framework features, managed
allocations, and relocations. Contract surface: the official [MorphOS
MUI_String documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

MG771 closes the struct-first `MUIA_String_Integer64` NULL boundary. Clearing
the [ISG] QUAD pointer now retires the object-owned `MuiStringInteger64Value`
copy and publishes a valid NULL getter result; the same named state can be
materialised again later. Host coverage is **1510/1510**. No managed runtime,
exception path, or numeric object-layout offset was introduced. Contract
surface: the official [MorphOS MUI_String documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

MG770 adds a focused freestanding proof for the struct-first
`MUIA_String_Integer64` QUAD boundary. Named `High`/`Low` ULONG fields
round-trip the signed minimum, zero, and bounded decimal text without managed
64-bit arithmetic. Host coverage remains **1509/1509**. The codec HUNK emits
**3784 / 3672 / 3640 bytes** on MC68000/020/040; MC68000 NativeExecution
returns **42** after **37,217 instructions / 307,574 cycles** with zero
framework/runtime features, managed allocations, and relocations. Contract
surface: the official [MorphOS MUI_String documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

MG769 fixes the String integer/MaxLen boundary: a one-byte
`MUIA_String_MaxLen` buffer now stays empty because the documented length
includes its terminating NUL, including for construction-time and runtime
`MUIA_String_Integer` updates. Host coverage is **1509/1509**. The focused
formatter HUNK emits **1424 / 1312 / 1296 bytes** on MC68000/020/040; MC68000
NativeExecution returns **42** after **248 instructions / 2708 cycles** with
zero framework/runtime features, managed allocations, and relocations. The
larger public construction root remains progressive because its compiler
closure exceeds the bounded native run. Contract surface: the official
[MorphOS MUI_String documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

MG768 keeps String placeholder ownership coherent when
`MUIA_String_Placeholder` is cleared: the named `MuiStringPlaceholderState`
publishes `APTR.Null` and the object-owned placeholder buffer is retired.
Host coverage is **1508/1508**. The placeholder-record HUNK emits **2460 /
2444 / 2444 bytes** on MC68000/020/040; MC68000 NativeExecution returns
**42** after **901 instructions / 9192 cycles** with zero framework/runtime
features, managed allocations, and relocations. Contract surface: the official
[MorphOS MUI_String documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

MG767 keeps String contents ownership coherent when `MUIA_String_Contents` is
cleared: the named `MuiStringContentsState` publishes `APTR.Null` and the
object-owned `StringCopyKey` dataspace is retired. Host coverage is
**1508/1508**. The String contents-record HUNK emits **2444 / 2428 / 2428
bytes** on MC68000/020/040; MC68000 NativeExecution returns **42** after
**901 instructions / 9192 cycles** with zero framework/runtime features,
managed allocations, and relocations. The public [MorphOS MUI_String
documentation](https://morphos-team.net/sdk/MUI/MUI_String.html) defines the
get/set contents surface; the implementation keeps the NULL pointer and
object-owned storage coherent without a private widget offset.

MG766 fixes MorphOS `MUIA_Text_Contents` NULL semantics. A NULL contents
pointer now publishes an empty named `MuiTextContentsState` and retires the
object-owned `TextCopyKey` buffer instead of leaving stale copied storage
visible. Host coverage is **1508/1508**. The contents-record HUNK emits
**2424 / 2408 / 2408 bytes** on MC68000/020/040; MC68000 NativeExecution
returns **42** after **901 instructions / 9192 cycles** with zero
framework/runtime features, managed allocations, and relocations. Contract
reference: the official [MorphOS MUI_Text documentation](https://morphos-team.net/sdk/MUI/MUI_Text.html).

MG765 adds MorphOS `MUIA_Text_Copy` (`0x80427727`, `[ISG]`) as a named
`MuiTextCopyStateRecord` with a field cursor codec. Common Get/OM_GET and
Set/OM_SET now share the normalized policy; enabling Copy materializes current
caller-owned contents, while disabling Copy governs subsequent Text_Contents
updates. Host coverage is **1508/1508**, including **1/1** focused Text_Copy
coverage. The native record HUNK emits **2468 / 2456 / 2460 bytes** on
MC68000/020/040; MC68000 NativeExecution returns **42** after **1658
instructions / 16852 cycles** with zero framework/runtime features, managed
allocations, and relocations. Contract reference: the official
[MorphOS MUI_Text documentation](https://morphos-team.net/sdk/MUI/MUI_Text.html).

MG764 adds the MorphOS Area resize lifecycle. Named 8-byte
`MuiAreaInitResizeMessage` and 4-byte `MuiAreaExitResizeMessage` records carry
the notification packets; `MuiAreaResizeStateRecord` keeps active state, flags,
and a wrapping-safe generation in guest memory. Layout dispatch handles both
methods and cleanup removes the lifecycle record; geometry remains owned by the
existing Layout path. Host coverage is **1507/1507**, including **4/4** focused
resize tests. The lifecycle HUNK emits **1224 / 1236 / 1236 bytes** for
MC68000/020/040, and MC68000 NativeExecution returns **42** after **267
instructions / 2992 cycles** with zero framework/runtime features and zero
external native targets. No managed runtime, exception path, or numeric
object-layout offset was introduced. Contract reference: the official
[MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MG763 adds the MorphOS `MUIM_DoDrag` handle entry point. The packed 16-byte
`MUIP_DoDrag` ABI is exposed through the named `MuiAreaDoDragMessage` record;
the receiver must be a live draggable object, and signed touch coordinates and
`MUIV_DoDrag_Async` flags are preserved in the typed `MuiDragRouteSample` sent
to the optional provider. Host coverage is **1503/1503**, including **12/12**
focused Area drag tests. The route-sample HUNK emits **428 / 520 / 524 bytes**
for MC68000/020/040, and MC68000 NativeExecution returns **42** after **191
instructions / 2078 cycles** with zero framework/runtime features and zero
external native targets. No managed runtime, exception path, or numeric
object-layout offset was introduced.

MG762 connects generic Area drag state to the typed pointer-capture provider.
The first accepted coordinate-bearing `MUIM_DragReport` creates a named
`MuiPointerCaptureSample` with `AreaDrag` kind and records capture ownership
in the guest drag state; finish and disposal release it. Host coverage remains
**1502/1502**, including **11/11** focused Area drag tests. The focused capture
HUNK emits **392 / 396 / 400 bytes** for MC68000/020/040, and MC68000
NativeExecution returns **42** after **88 instructions / 940 cycles** with
zero framework/runtime features and zero external native targets.

MG761 adds a named `MuiWindowDoubleClickValidation` record and the small
`MuiWindowDoubleClickProducerCore.Accept` policy. The platform/object layer
still supplies liveness and bounded Parent-chain facts before the real Publish
path commits Area state; native policy qualification is therefore independent
of the complete Window lifecycle. Host coverage remains **1502/1502**. The
policy HUNK emits **560 / 572 / 572 bytes** for MC68000/020/040, and MC68000
NativeExecution returns **42** after **136 instructions / 1,932 cycles** with
zero framework/runtime features and zero external native targets.

MG760 extracts the struct-first Window double-click publication boundary into
`MuiWindowDoubleClickProducerCore`. `PollWindowEvents` reuses the same helper,
so native producers can qualify the named target/value record and bounded
Parent-chain validation without depending on the complete Window lifecycle.
Host coverage passes **1502/1502**; the native-root managed build has zero
errors, while the standalone producer HUNK remains progressive because its
current object-core closure exceeds the bounded compiler run window. No
managed runtime, exception path, or numeric object-layout offset was added.

MG759 carries an optional producer-owned double-click decision in the named
`MuiWindowEventSample.DoubleClick` record. `PollWindowEvents` accepts the
signed value only when its target is a live descendant of the current Window,
then publishes getter-only `MUIA_DoubleClick` through the existing Area state
seam. Native hit-testing and click timing remain platform-owned; no managed
runtime, exception path, or positional object-layout state is introduced.
Host coverage passes **1502/1502**. The focused native closure is being
qualified separately.

MG758 connects the typed Window producer path to Area timer state. Pointer
enter/leave samples update armed records, and `IDCMP_INTUITICKS` (`0x00400000`)
advances the hovered object only after the producer reports the initial delay
through `MuiWindowEventSample.TimerDelayElapsed`. Host coverage passes
**1501/1501**; no managed clock, timer task, exception path, or managed runtime
state was introduced.

MG757 connects the typed timer event state to the common-control `HandleEvent`
path. The value-type `ReadTicks()` capability arms relverify Gadget
transitions on press and disarms them on release; the
input producer still owns the initial delay and IntuiTick cadence. Host
coverage passes **1500/1500**. No managed clock, timer task, exception path,
or managed runtime state was introduced.

MG756 adds a struct-first `MUIA_Timer` event-state boundary. Named relverify
press, IntuiTick, pointer enter/leave, and release inputs update a
guest-resident event record; the signed counter advances only after the
producer reports the initial delay and an in-gadget tick, while duplicate tick
identities are ignored. Scheduling and clock ownership remain outside the
freestanding core, so no timer task, managed clock, exception, or managed
runtime state is introduced. Host coverage passes **1500/1500**, focused timer
coverage passes **6/6**, and `AreaTimerEventStateRoot` is **2860 / 2892 / 2896
bytes** for MC68000/020/040 with zero framework features, managed-allocation
sites, and relocations; NativeExecution returns **42** after **2188
instructions / 22,714 cycles**. Contract reference: the official
[MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MG755 adds struct-first getter-only Area `MUIA_Timer` state. A named signed-LONG
record projects through common Get/OM_GET, runtime Set/OM_SET remains rejected,
and a typed publisher is available for the future input/event producer. The
core leaves relverify/IntuiTick cadence to that producer and adds no exceptions,
managed runtime, or private offset-backed state. Host coverage passes
**1498/1498**, focused timer coverage passes **4/4**, and `AreaTimerStateRoot`
is **2468 / 2456 / 2456 bytes** for MC68000/020/040 with zero framework
features, managed-allocation sites, and relocations; NativeExecution returns
**42** after **1172 instructions / 12,172 cycles**. Contract reference: the
official [MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MG754 adds a struct-first Area background/backfill seam. The shared MorphOS
rectangle packet remains wire-compatible while named X/Y offsets and the
operation-specific auxiliary value feed `MuiBackfillRenderRequest`; a native
provider may consume the request, and decline retains deterministic pen/fill
rendering. Host coverage passes **1494/1494**, focused backfill coverage passes
**4/4**, and focused freestanding coverage passes **2/2**. The targeted native
provider root emits **972 / 1008 / 1020 bytes** on MC68000/020/040 with zero
framework features, managed-allocation sites, and relocations; NativeExecution
returns **42** after **242 instructions / 2,666 cycles**. Contract reference:
the official [MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MG753 adds a struct-first `MUIA_DoubleBuffer` Begin/End render capability.
`MuiDoubleBufferRenderRequest` carries source/target RenderInfo and RastPort,
rectangle, and flags as named fields. Area and common-control drawing use a
provider target only during the active draw and restore the source RenderInfo;
provider decline falls back to direct rendering. Allocation and blitting stay
provider-owned, with no managed bitmap, exception path, or private object
layout offset. Host coverage passes **1490/1490**, focused DoubleBuffer
coverage passes **7/7**, and focused freestanding coverage passes **2/2**. The
targeted native seam root emits **1072 / 1100 / 1100 bytes** for
MC68000/020/040 with zero framework features, managed-allocation sites, and
relocations; NativeExecution returns **42** after **245 instructions / 2,576
cycles**. Contract reference: the official
[MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MG752 adds struct-first getter-only Area `MUIA_DoubleClick` state. The named
signed-LONG record is projected through common Get/OM_GET, while runtime Set is
rejected. A typed publication seam is available to the future input subsystem;
double-click timing semantics are intentionally not invented in this layer.
Host coverage passes **1487/1487**, focused freestanding coverage passes
**2/2**, and `AreaDoubleClickRoot` is **2484 / 2484 / 2488 bytes** for
MC68000/020/040 with zero framework features, managed-allocation sites, and
relocations; NativeExecution returns **42** after **1173 instructions /
12,164 cycles**. Contract reference: the official
[MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MG751 adds struct-first getter-only `MUIA_Window` and `MUIA_WindowObject`
relationships over the named render-info record. They return NULL before setup,
expose live named pointers during setup, and reject writes. Host coverage passes
**1483/1483**, focused freestanding coverage passes **2/2**, and
`AreaWindowRelationshipRoot` is **4044 / 4024 / 4068 bytes** for MC68000/020/040
with zero framework features, managed-allocation sites, and relocations;
NativeExecution returns **42** after **3177 instructions / 33,766 cycles**.
Contract reference: the official
[MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MG750 adds the struct-first Area `MUIA_CycleChain` signed LONG state
projection. A named record carries the scalar policy through construction,
Set/Get, and OM_GET, while Window `MUIM_Window_SetCycleChain` keeps its
independent object-vector boundary. Host coverage passes **1479/1479**, focused
freestanding coverage passes **2/2**, and `AreaCycleChainRoot` is **2476 / 2476 /
2480 bytes** for MC68000/020/040 with zero framework features,
managed-allocation sites, and relocations; NativeExecution returns **42** after
**1173 instructions / 12,164 cycles**. Contract reference: the official
[MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MG749 adds the struct-first shared Area `MUIA_ControlChar` state projection.
The normalized character is carried by a fixed-width named record through
construction, Set/Get, and OM_GET; Text.mui's `MUIA_Text_ControlChar` remains a
separate class-specific policy. Host coverage passes **1475/1475**, focused
freestanding coverage passes **2/2**, and `AreaControlCharRoot` is **2504 /
2504 / 2508 bytes** for MC68000/020/040 with zero framework features,
managed-allocation sites, and relocations; NativeExecution returns **42** after
**1177 instructions / 12,212 cycles**. Contract reference: the official
[MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MG748 adds named Area context-menu state and fixed MorphOS context-menu method
packets. Opaque menu strips/items stay outside the core, optional coordinate
storage is validated at the provider boundary, and default choices publish the
named trigger without managed state or exceptions. Host coverage passes
**1471/1471**, focused freestanding coverage passes **2/2**, and
`AreaContextMenuRoot` is **2752 / 2780 / 2784 bytes** for MC68000/020/040 with
zero framework features, managed-allocation sites, and relocations;
NativeExecution returns **42** after **1098 instructions / 10,810 cycles**.
Contract reference: the official
[MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MG747 adds named `MUIP_CreateBubble`/`MUIP_DeleteBubble` packet records and a
provider-owned opaque handle capability. Caller-owned bubble text is bounded
before crossing the capability, and no managed text, exceptions, or offset
backing is introduced. Host coverage passes **1467/1467**, focused freestanding
coverage passes **2/2**, and `AreaBubbleRoot` is **924 / 928 / 928 bytes** for
MC68000/020/040 with zero framework features, managed-allocation sites, and
relocations; NativeExecution returns **42** after **173 instructions / 1,874
cycles**. Contract reference: the official
[MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MG746 makes opened CustomFont rendering an explicit named request from the
Area and common-control draw paths rather than an accidental TextColor side
effect. This keeps the documented `MUIM_Text` route available when TextColor is
unavailable. Host coverage passes **1464/1464**, focused freestanding coverage
passes **2/2**, and `TextMethodCustomFontRenderRoot` is **996 / 1072 / 1072
bytes** for MC68000/020/040 with zero framework features, managed-allocation
sites, and relocations; NativeExecution returns **42** after **252 instructions /
2,700 cycles**. Contract reference: the official
[MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MG745 carries the named `MUIA_Unicode` policy into the `MUIM_Text`
`MuiTextMethodRenderRequest` struct. Native providers can therefore select
UTF-8 rendering from explicit request data; no private object offsets,
exceptions, managed runtime, or floating-point state is introduced. Host
coverage passes **1463/1463**, focused freestanding coverage passes **2/2**,
and `TextMethodRenderRoot` is **804 / 836 / 836 bytes** for MC68000/020/040
with zero framework features, managed-allocation sites, and relocations;
NativeExecution returns **42** after **185 instructions / 1,994 cycles**.
Contract reference: the official
[MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MG744 makes Text.mui common-control sizing and cutoff detection consume the
active opened CustomFont's named integer glyph width and line height; ordinary
fonts retain the neutral 8px/10px fallback. FixedText owned sample-buffer keys
are now distinct from the BuiltinFont state key, closing a Dataspace collision
that font resolution could expose. Host coverage passes **1463/1463**, focused
freestanding coverage passes **2/2**, and AreaCustomFontMetricsRoot is
**2,188 / 2,128 / 2,132 bytes** for MC68000/020/040 with zero framework
features, managed-allocation sites, and relocations; NativeExecution returns
**42** after **1,090 instructions / 10,928 cycles**. Contract reference: the
official [MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MG743 gives MUIM_TextDim a formatted, explicit-length fallback that shares the
bounded Text.mui scanner. Preparse escapes are skipped; Unicode spans count
UTF-8 code points and multiline spans derive maximum width plus line height.
Provider metrics remain preferred. Host coverage passes **1463/1463**, focused
freestanding coverage passes **2/2**, and TextDimensionRenderRoot is
**852 / 868 / 872 bytes** for MC68000/020/040 with zero framework features,
managed-allocation sites, and relocations; NativeExecution returns **42** after
**193 instructions / 2,112 cycles**. Contract reference: the official
[MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MG742 carries the named Text Unicode state into MuiTextDimensionRequest.
Providers receive the policy explicitly; the bounded fallback counts UTF-8
code points rather than bytes and treats malformed sequences as one byte.
Host coverage passes **1463/1463**, focused freestanding coverage passes
**2/2**, and TextDimensionRenderRoot is **852 / 868 / 872 bytes** for
MC68000/020/040 with zero framework features, managed-allocation sites, and
relocations; NativeExecution returns **42** after **193 instructions / 2,112
cycles**. Contract reference: the official
[MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MG741 promotes the MUIM_TextDim packet’s final words to named PreParse and
Flags fields and forwards the complete request through the fixed-width
MuiTextDimensionRequest. Providers may fill width/height results; the existing
graphics metrics remain the fallback when they decline. Host coverage passes
**1463/1463**, focused freestanding coverage passes **2/2**, and
TextDimensionRenderRoot is **852 / 860 / 864 bytes** for MC68000/020/040 with
zero framework features, managed-allocation sites, and relocations;
NativeExecution returns **42** after **187 instructions / 2,048 cycles**.
Contract reference: the official
[MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MG740 centralizes the bounded leading Text.mui preparse provider policy for
both Text.mui control drawing and MUIM_Text. Style, direct-colour, and
inline-image directives now use the same named struct requests before the raw
graphics Text call. Host coverage passes **1463/1463**, focused freestanding
coverage passes **2/2**, and TextMethodRenderRoot remains **796** bytes on
MC68000 and **828** on MC68020/MC68040 with zero framework features,
managed-allocation sites, and relocations; NativeExecution returns **42** after
**181 instructions / 1,944 cycles**. Full provider-native inline composition
remains progressive. Contract reference: the official
[MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MG739 gives the `MUIM_Text` packet named `PreParse` and `Flags` fields instead
of discarded reserved slots. The complete value record crosses the named
`MuiTextMethodRenderRequest` before the graphics Text call, carrying object,
RastPort, font, text, length, and rectangle data without managed strings,
exceptions, floating point, or offset-backed state. Host coverage passes
**1463/1463**, focused freestanding coverage passes **2/2**, and
`TextMethodRenderRoot` is **796** bytes on MC68000 and **828** on MC68020/MC68040
with zero framework features, managed-allocation sites, and relocations;
NativeExecution returns **42** after **181 instructions / 1,944 cycles**. Full
provider-native preparse composition remains progressive. Contract reference:
the official [MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MG738 forwards leading MorphOS Text.mui `ESC I[s]` image specifications through
the named, struct-first `MuiTextInlineImageRenderRequest`. The existing
`MuiImageSpec` value carries the parsed kind and payload fields; object,
RastPort, and placement geometry are named request fields. Host coverage passes
**1463/1463**, focused freestanding coverage passes **2/2**, and
`TextInlineImageRenderRoot` is **824** bytes on MC68000 and **856** on
MC68020/MC68040 with zero framework features, managed-allocation sites, and
relocations; NativeExecution returns **42** after **191 instructions / 2,004
cycles**. Provider lookup, lifetime, and full inline composition remain
progressive. Contract reference: the official
[MUI_Text documentation](https://morphos-team.net/sdk/MUI/MUI_Text.html).

MG737 forwards leading MorphOS Text.mui direct-colour preparse forms
(`p[RRGGBB]`, `P[RRGGBB]`, `P[AARRGGBB]`, and `P[AA------]`) through the named,
struct-first `MuiTextInlineColorRenderRequest`. The provider receives RGB,
alpha, flags, object, RastPort, and presence fields; no managed colour object,
exception, floating point, or offset-backed state is introduced. Host coverage
passes **1462/1462**, focused freestanding coverage passes **2/2**, and
`TextInlineColorRenderRoot` is **780** bytes on MC68000/020/040 with zero
framework features, managed-allocation sites, and relocations; NativeExecution
returns **42** after **137 instructions / 1,538 cycles**. Per-glyph colour runs
and provider-native compositing remain progressive. Contract reference: the
official [MUI_Text documentation](https://morphos-team.net/sdk/MUI/MUI_Text.html).

MG736 projects initializer-only `MUIA_Unicode` through the named
`MuiTextUnicodeState` guest record. Text metrics count valid UTF-8 codepoints,
malformed sequences remain bounded one-byte glyphs, and cutoff shortening keeps
multibyte sequences whole while passing the original byte span to the provider.
Host coverage passes **1461/1461**. `TextUnicodeStateRoot` is **2,356** bytes
on MC68000, **2,340** on MC68020, and **2,344** on MC68040 with zero framework
features, managed-allocation sites, and relocations; NativeExecution returns
**42** after **882 instructions / 9,032 cycles**. Provider-native UTF-8
rasterization remains progressive. Focused freestanding coverage passes
**2/2**. Contract reference: the official
[MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MG735 forwards leading Text.mui preparse soft styles (`s`, `o`, `g`, `u`, `b`,
`i`, and reset `n`) through the named `MuiTextStyleRenderRequest` before Text
drawing. The provider owns SetSoftStyle/equivalent rasterization; the core
keeps no per-character managed state, exceptions, floating point, or
offset-backed style table. Host coverage passes **1460/1460**, focused
freestanding coverage passes **2/2**, and `TextPreParseStyleRenderRoot` is
**764** bytes on MC68000 and **760** on MC68020/MC68040 with zero framework
features, managed-allocation sites, and relocations; NativeExecution returns
**42** after **133 instructions / 1,478 cycles**. Per-glyph runs and embedded image/truecolor
specs remain progressive. Contract reference: the official
[MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MG734 carries the effective `MuiCustomFontSpec` and provider font handle in the
named `MuiCustomFontRenderRequest` before Area drawing. Providers can consume
outline, glow, underline, bold, italic, and `/Crrggbb` policy without managed
font state, exceptions, floating point, or offset-backed render state. Host
coverage passes **1459/1459**, focused freestanding coverage passes **2/2**,
and `AreaCustomFontStyleRenderRoot` is **792** bytes on MC68000 and **820** on
MC68020/MC68040 with zero framework features, managed-allocation sites, and
relocations; NativeExecution returns **42** after **176 instructions / 1,882
cycles**. Actual glyph rasterization remains provider-owned. Contract grammar:
the official [MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MG733 carries the parsed `MuiCustomFontSpec` in the named TextColor
resolution request. Providers honor CustomFont `/crrggbb` before the default,
so the setup-scoped `MUIA_TextColor` getter and Area `DrawText` use the same
00RRGGBB value. Host coverage passes **1458/1458**, focused freestanding
coverage passes **2/2**, and `AreaTextColorRenderRoot` is **984** bytes on
MC68000 and **1,024** on MC68020/MC68040 with zero framework features,
managed-allocation sites, and relocations; NativeExecution returns **42** after
**219 instructions / 2,330 cycles**. The `/crrggbb` contract is tracked against
the official [MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MG732 forwards setup-scoped `MUIA_TextColor` through the named
`MuiTextColorRenderRequest` before Area, common-control, and Stringscroll
drawing. RGB remains distinct from `SetPen`; native color mapping is provider
owned. Host coverage passes **1457/1457**, focused freestanding coverage passes
**2/2**, and `AreaTextColorRenderRoot` is **704** bytes on MC68000 and **700**
bytes on MC68020/MC68040 with zero framework features, managed-allocation
sites, and relocations; NativeExecution returns **42** after **119
instructions / 1,300 cycles**.

MG731 adds the struct-first `MuiCustomFontMetrics` capability record. Provider
handles publish bounded integer glyph advance and line height, and
`TextDimensions`/`DrawText` consume those metrics through the active opaque
handle. Host coverage passes **1456/1456**, focused freestanding coverage
passes **2/2**, and `AreaCustomFontMetricsRoot` is **2,188** bytes on MC68000,
**2,128** on MC68020, and **2,132** on MC68040 with zero framework features,
managed-allocation sites, and relocations; NativeExecution returns **42** after
**1,090 instructions / 10,928 cycles**. The current projection is deterministic
fixture behavior; real rasterization remains provider-specific.

MG730 adds named, fixed-width `MUIP_OpenCustomFont` and
`MUIP_CloseCustomFont` packets and routes them through `LayoutDispatcher`.
Open returns the provider-backed opaque handle and Close validates and retires
the object-owned runtime record. Host coverage passes **1455/1455**, focused
freestanding coverage passes **2/2**, and `AreaCustomFontMessageCodecRoot` is
**2,924** bytes on MC68000, **2,912** on MC68020, and **2,912** on MC68040 with
zero framework features, managed-allocation sites, and relocations;
NativeExecution returns **42** after **1,634 instructions / 17,376 cycles**.

MG729 adds the provider-backed CustomFont lifecycle. The fixed-width
`MuiCustomFontOpenRequest` crosses `IMuiCustomFontCapability`; Setup opens the
parsed guest spec, `MuiAreaCustomFontRuntimeRecord` stores the opaque handle,
and effective resolution, setter refresh, Cleanup, and disposal close it.
No managed font/string state, exceptions, or floating-point code is used. Host
coverage passes **1453/1453**; `AreaCustomFontRuntimeCodecRoot` is **2,724**
bytes on MC68000, **2,740** on MC68020, and **2,752** on MC68040 with zero
framework features, managed-allocation sites, and relocations; NativeExecution
returns **42** after **1,930 instructions / 19,728 cycles**. Provider-specific
font rendering remains progressive.

MG728 adds the named `MuiAreaFontSelectionStateRecord` for MorphOS's
last-setter-wins relationship between `MUIA_Font` and `MUIA_CustomFont`.
Effective resolution exposes the parsed `MuiCustomFontSpec` and retains an
inherited `TextFont` pointer as a fixed-width fallback; no managed strings,
exceptions, or managed font objects are used. Host coverage passes
**1450/1450**; `AreaFontSelectionCodecRoot` is **2,628 bytes** on MC68000,
**2,636** on MC68020, and **2,644** on MC68040 with zero framework features,
managed-allocation sites, and relocations; NativeExecution returns **42** after
**1,583 instructions / 16,164 cycles**. Requested-family opening and drawing
remain a later graphics-capability increment.

MG727 adds a freestanding parser for the documented MorphOS CustomFont
specification grammar. `MuiCustomFontSpec` names the guest family span, size
mode, style flags, and packed text/outline colors without managed strings,
floating point, or host font objects. Host coverage passes **1447/1447**;
`AreaCustomFontSpecRoot` is **3,064 bytes** on MC68000, **3,100** on MC68020,
and **3,000** on MC68040 with zero framework features, managed-allocation
sites, and relocations; NativeExecution returns **42** after **3,582
instructions / 28,520 cycles**.

MG726 adds struct-first Area `MUIA_CustomFont` projection. The caller-owned
MUI font-spec pointer, explicit presence, and generation are retained in the
named `MuiAreaCustomFontStateRecord`; bounded guest-string validation rejects
bad pointers without creating managed strings or font objects. Host coverage
passes **1445/1445**; `AreaCustomFontCodecRoot` is **2,616 bytes** on MC68000,
**2,624** on MC68020, and **2,632** on MC68040 with zero framework features,
managed-allocation sites, and relocations; NativeExecution returns **42** after
**1,581 instructions / 16,164 cycles**. Font opening/spec interpretation is
intentionally deferred to a later graphics capability.

MG725 adds struct-first Area `MUIA_BuiltinFont` selector semantics. The raw
MorphOS selector and presence are retained in the named
`MuiAreaBuiltinFontStateRecord`; effective Font resolution honors explicit
selectors and follows the guest Family parent chain for selector zero. Host
coverage passes **1442/1442**; `AreaBuiltinFontCodecRoot` is **2,612 bytes** on
MC68000, **2,620** on MC68020, and **2,632** on MC68040 with zero framework
features, managed-allocation sites, and relocations; NativeExecution returns
**42** after **1,582 instructions / 16,140 cycles**.

MG724 adds struct-first Area `MUIA_TextColor` lifetime semantics. The
getter-only packed `00RRGGBB` value is resolved through the named
`MuiTextColorResolutionRequest` at setup, retained in
`MuiAreaTextColorStateRecord`, and returns zero after cleanup. Host coverage
passes **1439/1439**; `AreaTextColorCodecRoot` is **2,552 bytes** on MC68000,
**2,564** on MC68020, and **2,572** on MC68040 with zero framework features,
managed-allocation sites, and relocations; NativeExecution returns **42** after
**1,569 instructions / 16,032 cycles**.

MG723 adds struct-first Area `MUIA_Font` inheritance. Absent/zero local values
resolve through the guest-resident Family `Parent` chain via the named
`MuiControlFontResolution` value; explicit local state remains in
`MuiControlFontStateRecord`, and rendering consumes the effective font without
a managed object hierarchy. Host coverage passes **1437/1437**;
`AreaFontResolutionCodecRoot` is **1,684 bytes** on MC68000 and **1,724 bytes**
on MC68020/MC68040 with zero framework features, managed-allocation sites, and
relocations, and NativeExecution returns **42** after **333 instructions /
3,796 cycles**.

MG722 adds struct-first Area `MUIA_Floating` state projection. The documented
`[ISG] BOOL` is normalized through `MuiAreaFloatingStateRecord` for
construction, Get, Set/NoNotifySet, OM_GET, and persistence import. MorphOS's
public autodoc leaves the placement effect undocumented, so this increment
does not invent layout behavior. Host coverage passes **1436/1436**;
`AreaFloatingCodecRoot` is **2,432 bytes** on MC68000 and **2,436 bytes** on
MC68020/MC68040 with zero framework features, managed-allocation sites, and
relocations, and NativeExecution returns **42** after **1,166 instructions /
12,076 cycles**.

MG720 adds struct-first MorphOS Numeric toggle-default behavior. Numeric,
Slider, Knob, Levelmeter, and Numericbutton keyboard handling reads the named
`MuiNumericState.Default` value, clips it against the named range, and commits
through the notification-aware typed setter. Host coverage passes **1430/1430**;
the focused value-rule closure is **424 bytes** on MC68000, MC68020, and MC68040
with zero framework features, managed-allocation sites, and
relocations, and NativeExecution returns **42** after **104 instructions /
1,114 cycles**.

MG721 adds struct-first MorphOS Area fixed-text sizing. Initializer-only
`MUIA_FixWidthTxt` and `MUIA_FixHeightTxt` samples are copied into guest
Dataspace and projected through `MuiAreaFixedTextStateRecord`; common-control
AskMinMax uses bounded line metrics to set fixed-axis minimum/default/maximum
extents. Host coverage passes **1431/1431**; `AreaFixedTextCodecRoot` is
**2,548 bytes** on MC68000 and **2,560 bytes** on MC68020/MC68040 with zero
framework features, managed-allocation sites, and relocations, and
NativeExecution returns **42** after **1,542 instructions / 15,840 cycles**.

MG719 adds struct-first String.mui document-boundary navigation.
`MUIKEY_TOP` and `MUIKEY_BOTTOM` resolve to the first and last logical UTF-8
cursor positions for single-line and multiline objects, while multiline
Home/End remains line-local. The complete host suite
passes **1430/1430**; `StringMultilineBoundaryNavigationRoot` is a
**3,308-byte** MC68000 HUNK with zero framework features, managed-allocation
sites, and relocations, and NativeExecution returns **42** after **1,716
instructions / 19,078 cycles**.

MG718 adds struct-first multiline String.mui page navigation. The production
editor derives page height from the named String scroll-metrics record and
preserves logical UTF-8 columns through named line-cursor structs for
`MUIKEY_PAGEUP` and `MUIKEY_PAGEDOWN`. The complete host suite passes
**1429/1429**; `StringMultilinePageNavigationRoot` is a **3,368-byte** MC68000
HUNK with zero framework features, managed-allocation sites, and relocations,
and NativeExecution returns **42** after **10,093 instructions / 110,582
cycles**.

MG717 adds struct-first MorphOS String.mui word navigation. The production
editor handles `MUIKEY_WORDLEFT` and `MUIKEY_WORDRIGHT` over logical UTF-8
characters through named contents/cursor state. The complete host suite passes
**1428/1428**; `StringWordNavigationRoot` is a **3,484-byte** MC68000 HUNK
with zero framework features, managed-allocation sites, and relocations, and
NativeExecution returns **42** after **51,785 instructions / 461,100 cycles**.

MG716 adds the struct-first MorphOS 3.20 String.mui translated-input TAB rule:
ordinary String instances normalize TAB to one space, while multiline
instances retain the TAB byte through the named interaction policy and
`MuiUtf8Character` value boundary. The complete host suite passes
**1427/1427**; `StringTranslatedTabInputRoot` is a **1,012-byte** MC68000 HUNK
with zero framework features, managed-allocation sites, and relocations, and
NativeExecution returns **42** after **218 instructions / 2,074 cycles**.

MG715 completes the core fallback side of String.mui `SGA_REUSE`. The owning
Window has a named, fixed-width `MuiWindowEventReuseStateRecord` for its active
event and one pending reuse payload. If a native provider declines the typed
request, the window loop redispatches it only after the current walk returns,
with a bounded 16-event drain and no managed queue or recursive callback. The
host suite passes **1426/1426**; the focused MC68000 closure is **550,968
bytes**, and NativeExecution returns **42** after **4,668,410 instructions /
48,453,300 cycles**.

MG714 extends the typed String.mui EditHook reuse request with the owning MUI
Window and MUI key. `SGA_REUSE` remains valid only with `SGA_END`; a native
provider can now requeue the caller-owned event through its typed window loop
after deactivation without reconstructing state from offsets. Host coverage
is **1425/1425** and native redispatch remains provider-owned.

MG713 adds a typed String.mui EditHook reuse request: `SGA_REUSE` now produces
`MuiStringEditReuseRequest` only when paired with `SGA_END`, preserving the
caller-owned event and SGWork code for platform redispatch after deactivation.
Host coverage is **1425/1425**. The overall goal remains open.

MG712 adds a typed String.mui EditHook beep request: `SGA_BEEP` now produces
`MuiStringEditBeepRequest`, while the platform owns screen resolution and native
UI effects. Host coverage is **1424/1424**. Event reuse remains progressive.

MG711 adds typed String.mui EditHook focus routing: `SGA_NEXTACTIVE` and
`SGA_PREVACTIVE` paired with `SGA_END` now resolve the containing Window through its bounded parent
chain and advance the named cycle-chain/ActiveObject records. The current Area
activation state is deactivated during the transition. Host coverage is
**1423/1423**. Event reuse remains progressive.

MG710 adds typed String.mui EditHook termination: `SGA_END` now deactivates an
active gadget through the named Area activation state and preserves its flags.
Host coverage is **1422/1422**. Beep and event reuse remain progressive.

MG709 tightens the named SGWork action boundary for String.mui EditHook:
`BufferPos`, `WorkBuffer`, and `NumChars` are accepted as one edit only when
the hook returns `SGA_USE`; clearing that bit leaves both owned contents and
the named cursor unchanged. Host coverage is **1421/1421**. Native focus,
beep, and the remaining action-routing capabilities stay progressive.

MG707 fixes the Unicode `MUIA_String_EditHook` ABI projection: the named
`MuiStringEditWorkRecord.NumChars` field reports logical UTF-8 characters, not
the WorkBuffer byte length, so it remains consistent with `BufferPos`. Host
coverage verifies the three-character `Åβ🙂` case without adding exceptions,
managed allocation, or an offset-backed state object.

MG708 extends the same named SGWork path to callback mutations: an EditHook
that changes `NumChars` now controls the accepted logical prefix, with Unicode
truncation occurring only at complete UTF-8 character boundaries. The named
count is honored consistently even when unchanged, matching the SGWork contract.
The complete host suite passes **1421/1421**; the refreshed focused MC68000
source closure remains freestanding with zero framework features and zero
managed-allocation sites.

MG706 adds a typed MC68000 NativeRoot closure for struct-first multiline String
contents-replacement visibility. The root writes creation tags through
`MuiAslTagItemRecord`, exercises the production setter with named cursor and
scroll records, and compiles with framework features and managed-allocation
sites at zero; its focused map reports **5,934** relocations. The aggregate
map remains progressive at **5,833** relocations. The visibility helper also
normalizes its cursor argument into a local value so freestanding lowering
does not require a hidden argument store.

MG705 adds struct-first multiline String contents-replacement visibility.
Runtime `MUIA_String_Contents` replacement refreshes the named scroll metrics
after cursor clamping, then reuses line-index/logical-column reconciliation so
programmatic replacement cannot leave `ScrollTop`/`ScrollLeft` behind the
active cursor. The focused regressions and complete host suite pass
**1418/1418**, the freestanding gate passes **2/2**, and NativeRoot builds with
the existing SDK type-conflict baseline. Aggregate HUNK compilation remains
progressive with zero framework features and zero managed-allocation sites; the
measured map remains **5,833** relocations.

MG704 adds struct-first multiline String cursor visibility. Movement, committed
edits, and edit-hook cursor updates reconcile the named
`MuiStringScrollMetricsState`, keeping the active line and UTF-8 logical column
inside the visible `ScrollTop`/`ScrollLeft` rectangle. The transient
`MuiStringLineCursor` carries the line index; no private offset-backed widget
state is introduced. The focused regressions and complete host suite pass
**1417/1417**, the freestanding gate passes **2/2**, and NativeRoot builds with
the existing SDK type-conflict baseline. Aggregate HUNK compilation remains
progressive with zero framework features and zero managed-allocation sites; the
measured map remains **5,833** relocations.

MG703 adds struct-first multiline String cursor navigation. `Home` and `End`
resolve the current logical line, while `Up` and `Down` preserve the current
UTF-8 column and clamp only at adjacent line boundaries. The transient
`MuiStringLineCursor` record scans bounded guest bytes without changing the
public cursor record layout. The focused regressions and complete host suite
pass **1416/1416**, the freestanding gate passes **2/2**, and NativeRoot builds
with the existing SDK type-conflict baseline. Aggregate HUNK compilation
remains progressive with zero framework features and zero managed-allocation
sites; the measured map remains **5,833** relocations.

MG702 adds struct-first multiline String viewport scrolling. Drawing consumes
the existing named `MuiStringScrollMetricsState` for pixel `ScrollTop` and
`ScrollLeft`, skips lines outside the visible rectangle, and limits each
horizontal span through bounded UTF-8 column records. The focused regressions
and complete host suite pass **1414/1414**, the freestanding gate passes
**2/2**, and NativeRoot builds with the existing SDK type-conflict baseline.
Aggregate HUNK compilation remains progressive with zero framework features and
zero managed-allocation sites; the measured map remains **5,833** relocations.

MG701 adds struct-first String.mui multiline rendering. When the named
`MuiStringInteractionStateRecord.Multiline` policy is enabled, drawing splits
guest line-feed spans into transient `MuiStringLineSpan` values, aligns each
line using UTF-8 logical width, and advances baselines by the platform font
height. The focused regression and complete host suite pass **1413/1413**, the
freestanding gate passes **2/2**, and NativeRoot builds with the existing SDK
type-conflict baseline. Aggregate HUNK compilation remains progressive with
zero framework features and zero managed-allocation sites; the measured map
remains **5,833** relocations.

MG700 adds struct-first String.mui multiline editing. The named
`MuiStringInteractionStateRecord.Multiline` policy makes Return insert a
line-feed through the bounded contents/cursor commit path; `AdvanceOnCR` still
routes Return to the cycle chain, and multiline Return does not update
`MUIA_String_Acknowledge`. The focused regression and complete host suite pass
**1412/1412**, the freestanding gate passes **2/2**, and NativeRoot builds with
the existing SDK type-conflict baseline. Aggregate HUNK compilation remains
progressive with zero framework features and zero managed-allocation sites; the
measured map remains **5,833** relocations.

MG699 adds a struct-first String.mui AttachedList disposal-notification seam.
When an independently owned Listview is disposed, the public relationship is
changed through the same change-detected attribute path as an explicit NULL
assignment, producing one NULL-value notification before the named
`MuiStringAttachedListStateRecord` is synchronized. The focused regression and
complete host suite pass **1411/1411**, the freestanding gate passes **2/2**,
and NativeRoot builds with the existing SDK type-conflict baseline. Aggregate
HUNK compilation remains progressive with zero framework features and zero
managed-allocation sites; the measured map reports **5,833** relocations.

MG698 adds a struct-first String.mui AttachedList lifetime boundary. When an
independently owned Listview is disposed, the guest object chain is walked and
matching `MuiStringAttachedListStateRecord` values are cleared before the
target disappears. Later String getters and input forwarding see a neutral
NULL relationship rather than stale object identity. The focused regression and
complete host suite pass **1411/1411**, the freestanding gate passes **2/2**,
and NativeRoot builds with the existing SDK type-conflict baseline. Aggregate
HUNK compilation remains progressive with zero framework features and zero
managed-allocation sites; the measured map reports **5,832** relocations.

MG696 tightened the struct-first Listview child-lifetime boundary. `ChildList`
validates the named `MuiListviewChildState` pointer against the live headless
object table and List-backed class family before any forwarded operation uses
it. If a caller disposes a supplied child directly, relationship and forwarded
List getters expose neutral values instead of stale raw parent metadata. The
focused regression and complete host suite pass **1409/1409**, the freestanding
gate passes **2/2**, and NativeRoot builds with the existing SDK type-conflict
baseline. Aggregate HUNK compilation remains progressive with zero framework
features and zero managed-allocation sites; its measured map reports **5,827**
relocations.

MG697 adds a struct-first String.mui scroll-metric transition seam. Content
and Area-layout mutations compare the previous and current named
`MuiStringScrollMetricsStateRecord` and notify only changed width, height,
visible-size, and offset attributes; getter paths never notify. The new
regression and complete host suite pass **1410/1410**, the freestanding gate
passes **2/2**, and NativeRoot builds with the existing SDK type-conflict
baseline. Aggregate HUNK compilation remains progressive with zero framework
features and zero managed-allocation sites; its measured map remains **5,827**
relocations.

MG695 closes the struct-first Listview relationship-getter gap. Generic
`Get`/`OM_GET` resolves `MUIA_Listview_List` from the named adopted-child
record before parent metadata, so a stale raw parent pointer cannot replace
the typed child identity. The focused regression and complete host suite pass
**1408/1408**, the freestanding gate passes **2/2**, and NativeRoot builds with
the existing SDK type-conflict baseline. Aggregate HUNK compilation remains
progressive with zero framework features and zero managed-allocation sites;
its map reports **5,821** relocations.

MG694 adds the struct-first Listview `OM_GET` projection. Public List
attributes are resolved through the named owned child before parent metadata,
so generic getters observe authoritative `Active`, entry, and viewport state
after Listview forwarding. Private Listview policy and click records remain
class-local. The focused regression and complete host suite pass **1408/1408**,
the freestanding gate passes **2/2**, and NativeRoot builds with the existing
SDK type-conflict baseline. Aggregate HUNK compilation remains progressive
with zero framework features and zero managed-allocation sites; its map reports
**5,821** relocations.

MG693 completes the struct-first BOOPSI `OM_UPDATE` packet boundary. A named
four-field `opUpdate` record decodes the caller-owned tag list and update
flags, then reuses the typed setter path used by `OM_SET`; Listview policy and
owned-child List forwarding therefore remain struct-backed. Focused update
coverage and the complete host suite pass **1408/1408**, the freestanding gate
passes **2/2**, and NativeRoot builds with the existing SDK type-conflict
baseline. Aggregate HUNK compilation is complete with zero framework features
and zero managed-allocation sites; its progressive map reports **5,818**
relocations.

MG692 extends generic BOOPSI `OM_SET` with a struct-first Listview composite
boundary. Initialized Listview interaction-policy tags and forwarded List
attributes now enter the named runtime setters, keeping the guest policy
record and owned child List authoritative rather than storing a stale raw
scalar. The regression and complete host suite pass **1407/1407**, the
freestanding gate passes **2/2**, and NativeRoot builds with the existing SDK
type-conflict baseline. Aggregate HUNK qualification remains progressive at
**5,818** relocations.

MG691 keeps Listview external-scroller teardown native-compiler friendly. The
recorded or explicit destination is selected through an explicit named `APTR`
state path rather than an implicit conditional merge. Focused external-scroller
coverage is **9/9**, the complete host suite is **1406/1406**, the freestanding
gate is **2/2**, and NativeRoot builds with the existing SDK type-conflict
baseline. Aggregate `mui-headless` HUNK compilation now completes; the map
still reports **5,818** relocations with zero framework features and zero
managed-allocation sites, so the aggregate zero-relocation gate remains
progressive.

MG690 extends the struct-first generic BOOPSI `OM_SET` path with MorphOS
`MUIA_NoNotifyMethod`. The one-shot selector is held in the named guest state
record only during the caller-owned tag-list operation; notification payloads
are inspected through the named follow-parameter slot, suppressing only
matching method IDs while unrelated notifications remain active. The control
tag is never persisted. `OmSetMUIANoNotifyMethodSuppressesOnlyMatchingFollowMethod`
and the complete host suite pass **1406/1406**; the freestanding gate passes
**2/2**, NativeRoot builds with the existing SDK type-conflict baseline, and
aggregate native HUNK qualification remains progressive. See the MorphOS
[MUI Notify reference](https://github-wiki-see.page/m/amiga-mui/muidev/wiki/MUI_Notify)
and official [MUI notification reference](https://morphos-team.net/sdk/objectivec/MUINotify.html).

MG689 adds the struct-first generic BOOPSI `OM_SET` path. A named
`{ MethodID, AttrList, GadgetInfo }` packet record is decoded once, then the
caller-owned `TagItem` list is walked through `MuiAslTagItemRecord`. A true
`MUIA_NoNotify` tag suppresses notifications for that operation only; effective
attributes still reach their normal named setters, and the setting-only control
tag is not persisted. `OmSetMUIANoNotifySuppressesOnlyThatTagOperation` and the
complete host suite pass **1405/1405**; the freestanding gate passes **2/2** and
NativeRoot builds with the existing SDK type-conflict baseline. Aggregate native
HUNK qualification and broader MorphOS differential coverage remain
progressive. See the official MorphOS [MUI notification reference](https://morphos-team.net/sdk/objectivec/MUINotify.html).

MG688 extends the struct-first notification lifecycle through object disposal.
Disposal walks guest-resident named notification records and removes recipes
whose resolved destination is the object being torn down, including explicit
pointers and MorphOS self/ancestor destination tokens. The focused regression
and complete host suite pass **1404/1404**; the freestanding gate passes **2/2**
and NativeRoot builds with the existing 12 SDK type-conflict warnings. Broader
notification and MorphOS differential qualification remain progressive. See
the official MorphOS [MUI notification reference](https://morphos-team.net/sdk/objectivec/MUINotify.html).

MG687 extends the change-aware notification seam to composed Stringscroll
Scrollbar children. Parent-driven Entries/Visible/First updates now notify
registered destinations only when named range fields change, preserving quiet
no-op synchronization. The focused Stringscroll regression and complete host
suite pass **1403/1403**; the freestanding gate passes **2/2** and NativeRoot
builds with the existing 12 SDK type-conflict warnings. Broader notification
and differential qualification remain progressive. See the official MorphOS
[MUI notification reference](https://morphos-team.net/sdk/objectivec/MUINotify.html)
and [Stringscroll documentation](https://morphos-team.net/sdk/MUI/MUI_Stringscroll.html).

MG686 makes Scrollgroup Prop range projection notification-aware. The named
Entries/Visible/First state is compared before each projection; actual changes
dispatch registered notifications, including clamped First updates, while an
identical relayout remains quiet. `ScrollgroupPropProjectionNotifiesOnlyWhenRangeChanges`
and the complete host suite pass **1403/1403**; the freestanding gate passes
**2/2** and NativeRoot builds with the existing 12 SDK type-conflict warnings.
Broader notification and differential qualification remain progressive. See
the official MorphOS [MUI notification reference](https://morphos-team.net/sdk/objectivec/MUINotify.html).

MG685 adds struct-first Scrollgroup `UseWindowBorder` policy propagation. The
layout resolves the containing Window through the guest Parent chain, stores a
named `MuiScrollgroupBorderScrollerStateRecord`, maps FreeHorizontal and
FreeVertical to the MorphOS bottom/right border scrollers, and gives the
content the full viewport while suppressing embedded bar geometry. Window
open forwards the named attributes through `SetMuiWindowBorderScrollers`.
`ScrollgroupUseWindowBorderPropagatesTypedScrollerPolicy` and the complete host
suite pass **1402/1402**; the freestanding gate passes **2/2** and NativeRoot
builds with the existing 12 SDK type-conflict warnings. Notifications and
differential qualification remain progressive. See the MorphOS
[Scrollgroup UseWinBorder reference](https://build.alb42.de/MUIClass/html/muiclass.group/tmuiscrollgroup.usewinborder.html)
and [Window border-scroller attributes](https://github-wiki-see.page/m/amiga-mui/muidev/wiki/MUI_Window).

MG684 adds struct-first Virtgroup pointer dragging through `MUIM_HandleInput`:
the named IntuiMessage pointer record drives a guest-resident capture state,
clamps `VirtualLeft`/`VirtualTop` to the separately named display viewport, and
relayouts without managed runtime state. Missing `MUIA_Virtgroup_Input` defaults
to TRUE. Direct and dispatcher coverage pass; the complete host suite passes
**1401/1401**, the freestanding gate **2/2**, and NativeRoot builds with the
existing 12 SDK type-conflict warnings. Scrollgroup border scrollers,
notifications, and differential qualification remain progressive. See the
official [MorphOS Virtgroup documentation](https://morphos-team.net/sdk/MUI/MUI_Virtgroup.html).

MG683 adds Scrollgroup keyboard routing through the typed Prop/Scrollbar seam:
directional, page, Home, and End keys update the visible range, synchronize
Virtgroup `VirtualLeft`/`VirtualTop`, and relayout from named geometry. The
focused regression and complete host suite pass **1400/1400**, the freestanding
gate **2/2**, and NativeRoot builds with the existing 12 SDK type-conflict
warnings. Virtgroup mouse dragging, border scroller integration, notifications,
and differential qualification remain progressive. See the official
[MorphOS Scrollgroup documentation](https://morphos-team.net/sdk/MUI/MUI_Scrollgroup.html)
and [Virtgroup documentation](https://morphos-team.net/sdk/MUI/MUI_Virtgroup.html).

MG682 adds typed Scrollgroup `AutoBars` policy. Layout starts from a bar-free
viewport, adds only allowed bars required by the virtual surface, re-evaluates
the coupled axis, and publishes named horizontal/vertical visibility fields;
hidden bars receive zero geometry. The focused regression and complete host
suite pass **1400/1400**, the freestanding gate **2/2**, and NativeRoot builds
with the existing 12 SDK type-conflict warnings. Border scroller integration,
full input-event parity, notifications, and differential qualification remain
progressive. See the official
[MorphOS Scrollgroup documentation](https://morphos-team.net/sdk/MUI/MUI_Scrollgroup.html).

MG681 extends the typed Scrollgroup/Virtgroup seam: recognized Virtgroup
contents use named virtual offsets, Scrollgroup publishes the signed
`MuiScrollgroupViewportStateRecord`, and MorphOS Prop children receive typed
Entries/Visible/First projections on both axes. The focused regression and
complete host suite pass **1399/1399**, the freestanding gate **2/2**, and
NativeRoot builds with the existing 12 SDK type-conflict warnings. Auto-bar
policy, full input-event parity, notifications, and differential qualification
remain progressive. See the official
[MorphOS Scrollgroup documentation](https://morphos-team.net/sdk/MUI/MUI_Scrollgroup.html).

MG680 extends the typed Stringscroll composition seam in both directions:
externally changed child `PropFirst` values are adopted during Recompute, and
parent `SetScroll`/layout updates publish bounded offsets back through named
child Prop records. Last-published values in the guest-resident composition
record prevent feedback loops. `ComposedScrollbarChildFirstChangesAreAdoptedAndResynchronized`
and the complete host suite pass **1398/1398**; the freestanding gate passes
**2/2** and NativeRoot builds with the existing 12 SDK type-conflict warnings.
Notifications, full MorphOS input-event parity, and differential qualification
remain progressive. See the official
[MorphOS Stringscroll documentation](https://morphos-team.net/sdk/MUI/MUI_Stringscroll.html).

MG679 extends the composed Stringscroll Scrollbar seam into pointer input:
arrow-step, Prop-track, and thumb-drag routing use named child layout/range
records, and drag state remains guest-resident. `ComposedScrollbarChildrenRouteArrowAndThumbInput`
and the complete host suite pass **1397/1397**; the freestanding gate passes
**2/2** and NativeRoot builds with the existing 12 SDK type-conflict warnings.
Notifications, full MorphOS input-event parity, and differential qualification
remain progressive. See the official
[MorphOS Stringscroll documentation](https://morphos-team.net/sdk/MUI/MUI_Stringscroll.html).

MG678 adds typed automatic Stringscroll scrollbar composition. If
`scrollbar.mui` is registered and a bar pointer is omitted, the implementation
creates an owned horizontal or vertical Scrollbar child, publishes ownership
through `MuiStringscrollCompositionRecord`, synchronizes its Prop range from
the named viewport, routes layout and drawing through the common Scrollbar
seams, and disposes only owned children. Supplied bar pointers remain
caller-owned. `StringscrollBuildsTypedAutomaticScrollbarChildren` and the
complete host suite pass **1396/1396**; the freestanding gate passes **2/2** and
NativeRoot builds with the existing 12 SDK type-conflict warnings. Full input
routing, notifications, and broader MorphOS differential qualification remain
progressive. See the official
[MorphOS Stringscroll documentation](https://morphos-team.net/sdk/MUI/MUI_Stringscroll.html).

MG677 keeps MorphOS Stringscroll initializer-only `HorizBar` and `VertBar`
scrollbar object pointers in the typed guest-resident
`MuiStringscrollScrollbarRecord`. Normalized BOOL policy flags remain in the
separate policy record for layout and input, so pointer identity is not reduced
to `0`/`1`. `StringscrollPreservesInitializerScrollbarObjectPointers` and the
complete host suite pass **1395/1395**; the freestanding gate passes **2/2** and
NativeRoot builds with the existing 12 SDK type-conflict warnings. Automatic
child-gadget composition, notifications, and broader MorphOS differential
qualification remain progressive. See the official
[MorphOS Stringscroll documentation](https://morphos-team.net/sdk/MUI/MUI_Stringscroll.html).

MG676 enforces the MorphOS `MUIA_Listtree_Active` visibility contract through
the named visible-preorder traversal. Valid descendants below closed parents
remain topology-addressable but cannot become active until opened. Focused
Listtree coverage and the complete host suite pass **1394/1394**; NativeRoot
builds with the existing 12 SDK type-conflict warnings. Native HUNK execution
and broader MorphOS differential qualification remain progressive. See the
official [MorphOS Listtree documentation](https://morphos-team.net/sdk/MUI/MUI_Listtree.html).

MG675 extends the struct-first Listtree surface policy across topology
mutations. Insert, Remove, Open/Close, Sort, Move, and Exchange now re-clamp
the named `FirstVisible` record and keep the active row visible when the
visible preorder shifts. Focused Listtree coverage and the complete host suite
pass **1393/1393**; NativeRoot builds with the existing 12 SDK type-conflict
warnings. Native HUNK execution and broader MorphOS differential qualification
remain progressive. See the official [MorphOS Listtree documentation](https://morphos-team.net/sdk/MUI/MUI_Listtree.html).

MG674 keeps the headless `MUIM_MultiSet` packet route struct-first for the
MorphOS Prop/Scrollbar family. Range and policy writes publish named guest
records before notifications, `PropFirst` is clamped against `Entries` and
`Visible`, Scrollbar child forwarding uses those same records, and target
vectors consume `MuiMultiSetTargetEntry.Target` after typed decoding. Focused
coverage and the complete host suite pass **1392/1392**; NativeRoot builds with
the existing 12 SDK type-conflict warnings. Native HUNK execution and broader
MorphOS differential qualification remain progressive. See the official
[MorphOS Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG673 tightens the struct-first boundary for Listview external-scroller
ownership: `MuiListviewExternalScrollerConnectionState` now uses a named field
enum/cursor codec for both guest fields instead of direct lifecycle-word
access. The documented packed notification vector remains an ABI boundary.
Scrollbar.mui destination coverage is included. Focused coverage and the
complete host suite pass **1391/1391**; NativeRoot
builds with the existing 12 SDK type-conflict warnings. Native HUNK execution
and broader MorphOS differential qualification remain progressive. See the
official [MorphOS Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG672 closes the inverse external-scroller lifecycle path. When a Prop or
Scrollbar is disposed first, the named headless object-chain seam removes any
Listview recipe targeting it before the destination record disappears. Focused
coverage and the complete host suite pass **1390/1390**; NativeRoot builds with
the existing 12 SDK type-conflict warnings. Native HUNK execution and broader
MorphOS differential qualification remain progressive. See the official
[MorphOS Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG671 closes the external-scroller lifecycle gap. The connected Prop/Scrollbar
destination is retained in the named guest
`MuiListviewExternalScrollerConnectionState`; reconnect/disconnect resolves
that record, and Listview cleanup removes the four recipe notifications before
child and generic records are retired. Focused coverage and the complete host
suite pass **1389/1389**; NativeRoot builds with the existing 12 SDK
type-conflict warnings. Native HUNK execution and broader MorphOS differential
qualification remain progressive. See the official [MorphOS Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG670 adds the typed external-scroller notification connection recipe.
`ConnectExternalScrollerProp` installs List `TotalPixel`, `VisiblePixel`, and
`TopPixel` notifications to Prop `Entries`, `Visible`, and `First`, plus Prop
`First` feedback to List `TopPixel`; reconnecting removes the prior recipe and
partial setup rolls back. Focused coverage and the complete host suite pass
**1388/1388**; NativeRoot builds with the existing 12 SDK type-conflict
warnings. Native HUNK execution and broader MorphOS differential qualification
remain progressive. See the official [MorphOS Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG669 closes a Listview line-height projection mismatch. Composite layout now
reads the owned List's named viewport `LineHeight` before deriving visible-row
capacity; the pre-layout scroller fallback uses the same named value,
preserving `MinLineHeight` and `AutoLineHeight` through the composite boundary.
Focused coverage and the complete host suite pass **1387/1387**;
NativeRoot builds with the existing 12 SDK type-conflict warnings. Native HUNK
execution and broader MorphOS differential qualification remain progressive.
See the official [MorphOS List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG668 closes the Listview click-result event projection gap. `HandleClick`
completes the named `MuiListviewClickState` before raising `ClickColumn` for
each handled click and `AgainClick`/`DoubleClick` for their matching repeated
click classes; reset values remain internal. Focused coverage and the complete
host suite pass **1385/1385**; NativeRoot builds with the existing 12 SDK
type-conflict warnings. Native HUNK execution and broader MorphOS differential
qualification remain progressive. See the official [MorphOS Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG667 closes a Listview notification-forwarding boundary. Notifications added
for forwarded List attributes resolve to the owned List's named record, while
Listview-owned click and selection signals remain on the composite. Add/remove
share this source resolver and introduce no duplicate notification table or
private offsets. Focused coverage and the complete host suite pass
**1384/1384**; NativeRoot builds with the existing 12 SDK type-conflict
warnings. Native HUNK execution and broader MorphOS differential qualification
remain progressive. See the official [MorphOS Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG666 adds an explicit typed external-scroller seam. The named
`MuiListviewExternalScrollerState` projects the owned child List's
`MuiListViewportState` pixel values into an application-owned `Prop.mui` or
`Scrollbar.mui`; `SyncExternalScrollerProp` applies Entries, Visible, and First
through the class-aware setter, preserving Prop range records, clamping,
redraw, and optional notifications. Listview does not take ownership of the
external object. Focused coverage and the complete host suite pass
**1383/1383**; NativeRoot builds with the existing 12 SDK type-conflict
warnings. Native HUNK execution and broader MorphOS differential qualification
remain progressive. See the official [MorphOS Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG665 closes the layout-driven notification gap for List `First` and `Visible`.
The named transition seam dispatches only when layout changes a public value,
and it runs after the guest-resident viewport record plus pixel metrics have
been refreshed. Focused coverage and the complete host suite pass **1382/1382**;
NativeRoot builds with the existing 12 SDK type-conflict warnings. Native HUNK
execution and broader MorphOS differential qualification remain progressive.
See the official [MorphOS List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

MG664 adds a focused native fixture for the external List notification path.
`CollectionListViewportNotificationRoot` keeps the List viewport metrics and
callback sequence isolated from the broad render closure. MC68000 HUNK
generation succeeds at **508,400 bytes** and framework analysis is
freestanding-compatible with zero framework members and managed-allocation
sites; the native harness currently returns **58** rather than **42**, so native
execution qualification remains pending.

MG663 makes Listview's public pixel projections usable by the MorphOS external
scrollbar pattern. The named `MuiListViewportState` remains the source of
truth; `TopPixel`, `VisiblePixel`, and `TotalPixel` now use the existing
change-only notification path, so external Prop connections observe real
insert/layout/First mutations without an offset-based shadow record. Focused
coverage and the complete host suite pass **1381/1381**; NativeRoot builds and
the direct MC68000 framework report is freestanding-compatible with zero
framework members and managed-allocation sites. Native closure qualification
remains progressive. See the official [MorphOS Listview documentation](https://morphos-team.net/sdk/MUI/MUI_Listview.html).

MG662 closes a Listview viewport-safety edge. The typed
`SetScrollerFirst` seam now rejects writes while the composite has no usable
viewport (`VisibleOff` or zero visible rows), preserving the child List's
MorphOS hidden cursor sentinel instead of retaining a latent first-row value
for a later layout. Visible scrolling continues through the named scroller
state. Focused coverage passes; NativeRoot and broader native qualification
complete host coverage passes **1380/1380**; NativeRoot builds, and the direct
MC68000 framework report for the collection root is freestanding-compatible
with zero framework members and managed-allocation sites. Broader native
qualification remains progressive. The production path remains freestanding, exception-free,
managed-runtime-free, and struct-first.

MG661 routes the builtin `String` and `StringArray` ConstructHook copies
through each List's retained Exec pool. Copied strings and string-array pointer
tables are released with `FreePooled` during entry destruction, and malformed
source arrays fail before retaining pooled storage. The named
`MuiListPoolPolicyState` remains the ownership authority. Host coverage passes
**1379/1379**; NativeRoot builds and its direct MC68000 framework report is
freestanding-compatible with zero framework-member and managed-allocation
sites. Broader native closure qualification remains progressive.

MG660 gives `List.mui` the MorphOS default pool behavior. If
`MUIA_List_Pool` is omitted, the core asks the native Exec capability for a
standard opaque pool using the documented 2008 puddle and 1024 threshold
defaults. The named `MuiListPoolPolicyState` records the handle and ownership;
arbitrary ConstructHook/DestructHook callbacks receive that handle in A2, and
an owned pool is deleted only after entry teardown. Caller-supplied pools stay
borrowed. Host coverage passes **1378/1378**; NativeRoot builds and native
closure qualification remain progressive. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first.

MG653 adds named effective display flags to the retained `Listtree.mcc`
DisplayHook snapshot. `MUIA_Listtree_EmptyNodes` suppresses the indicator for
an empty logical node while preserving its list topology and public tree-node
flags; `TNF_NOSIGN`, open, and frozen state remain independently represented.
Host coverage passes **1373/1373**; native closure qualification and broader
differential behavior remain progressive.

MG654 routes visible ordinal insertion through the named visible-child
resolver. Closed parent lists expose no visible child anchor, so
`MUIV_Listtree_Insert_Flags_Visible` appends until the parent is opened; the
ordinary structural path is unchanged. Host coverage passes **1374/1374**;
native closure qualification remains progressive.

MG655 applies `Flags_Nr` and `Flags_Visible` to the `Move` destination as
well as its source. The destination ordinal is resolved after unlinking
through the named structural/visible-child anchor; closed visible lists append,
and open lists preserve predecessor order. Host coverage passes **1375/1375**;
native closure qualification remains progressive.

MG656 implements the MorphOS `MUIA_Listtree_Title` compatibility rule. The SDK
historically declares the tag as `CONST_STRPTR`, but documents that it can only
be set to `TRUE` or `FALSE`; the named presentation record therefore stores a
fixed-width `Title` BOOL. Construction tags and Set/OM_SET normalize zero to
`FALSE` and every nonzero value to `TRUE`, while Get/OM_GET return the canonical
value. `Format` remains the only caller-owned string pointer in the record.
Host coverage passes **1375/1375**; native closure qualification and broader
MorphOS differential behavior remain progressive. See the official
[MorphOS Listtree documentation](https://morphos-team.net/sdk/MUI/MUI_Listtree.html).

MG657 implements the visible title row for the boolean `MUIA_Listtree_Title`
form. Draw invokes the typed DisplayHook with A1 = `NULL`, reserves one
surface row before data rows, and retains a named snapshot with the
`DisplayTitle` flag. AskMinMax, viewport/page capacity, and pixel hit-testing
account for the title row; title-row TestPos returns no tree node. Host
coverage passes **1376/1376**; NativeRoot builds, while native execution and
broader MorphOS differential behavior remain progressive. The production path
remains freestanding, exception-free, managed-runtime-free, and struct-first.

MG659 completes the standard Exec pool boundary for arbitrary Listtree
ConstructHook Insert calls. A2 now receives one retained opaque pool handle
for the Listtree object's lifetime, while A1 carries the typed
`MuiListtreeInsertMessage`; DestructHook receives the same handle until the
last node is released. The named `MuiListtreeHookPoolStateRecord` stores the
handle and creation contract, and the platform exposes only pool lifecycle and
pooled allocation operations rather than a fabricated private pool header.
Host coverage passes **1377/1377**; NativeRoot builds with the existing SDK
type-conflict warnings, while native execution and broader MorphOS differential
behavior remain progressive. See the official [MorphOS Listtree documentation](https://morphos-team.net/sdk/MUI/MUI_Listtree.html).

MG658 corrects the arbitrary ConstructHook A1 boundary for Listtree Insert.
The hook receives a temporary guest `MuiListtreeInsertMessage` through the
named codec, including the method ID, Name, User, ListNode, PrevNode, and Flags
fields; the packet is released after the callback. A2 remains `NULL` pending a
standard Exec memory-pool capability, and Rename's direct user callback is
unchanged. Host coverage passes **1376/1376**; NativeRoot builds, while native
execution and broader MorphOS differential behavior remain progressive.

MG652 retains the latest `Listtree.mcc` DisplayHook vector in a typed guest
snapshot record until the next Draw pass or object cleanup. The record names
the producing node, bounded column count, and vector pointer; the core owns
the vector, while strings returned by the hook remain caller-owned. Host
coverage reads the snapshot through the same struct/cursor codecs and passes
**1372/1372**; native closure qualification and richer column providers remain
progressive.

MG648 implements the MorphOS `Listtree.mcc` DisplayHook callback boundary:
visible rows pass A1 = the guest tree node and A2 = a temporary typed vector
of fixed-width column-pointer records. The vector follows the bounded
`FORMAT` count and initializes every slot to `NULL`, preserving the built-in
node-name fallback in the tree column until a hook supplies replacement text.
The production path uses named guest structs and a cursor rather than managed
arrays or offset-based state. Host coverage proves hook-side replacement-string
mutation and passes **1369/1369**; native closure qualification and richer
column providers remain progressive.

MG649 adds the first MorphOS-style `FORMAT` geometry slice for Listtree pointer
hits. A temporary fixed-width guest vector parses `DELTA`/`D` gaps and
`WEIGHT`/`W` proportional widths; a focused test proves a non-equal `20/20/60`
layout. The production path remains integer-only, freestanding,
exception-free, and managed-runtime-free, with the vector freed at the pointer
boundary. Host coverage passes **1370/1370**; native execution, content
measurement, and pixel min/max policy remain progressive.

MG650 extends the geometry records with typed `MINWIDTH`/`MIW` and
`MAXWIDTH`/`MAW` constraints. Percentage values and optional `px` values are
resolved against the laid-out width after weighted allocation; content `-1`
limits retain a bounded fallback until measurement is available. Focused tests
cover pixel and percentage boundaries, and host coverage passes **1371/1371**.
Native execution and content measurement remain progressive.

MG651 aligns laid-out `MUIM_Listtree_TestPos` with the pointer hit-test. The
typed path honors surface bounds, viewport origin, row height, FORMAT geometry,
and Above/Below/Onto edge flags; the pre-layout row-index fallback remains for
headless construction probes. Focused coverage proves edge classification and
outside rejection, and host coverage passes **1372/1372**. Native execution
remains progressive.

MG647 adds MorphOS Listtree double-click notification delivery through the
existing typed notification core. Leaf pairs and node-column pairs outside the
selected `MUIA_Listtree_DoubleClick` policy publish the guest node pointer;
selected list columns retain their open/close action and frozen nodes remain
inert. Host coverage passes **1369/1369**; native closure qualification and
broader MorphOS differential behavior remain progressive.

MG646 extends the MorphOS Listtree double-click slice with a separate named
`MuiListtreeClickColumnState` guest record. Different `FORMAT` columns cannot
reuse the previous click, while same-column pairs still honor
`MUIA_Listtree_DoubleClick` Off, All, Tree, and numeric selectors. Field-scoped
node codecs preserve the public flag word in native closures. The native
production path remains freestanding, exception-free, and managed-runtime-free;
exact MorphOS display-column geometry remains progressive. The focused MC68000
closure is **548,964** bytes with **1,635 symbols**, **4,745 relocations**,
**335,402 code bytes**, and **86 loops**; native execution returns **42** after
**15,657,577 instructions / 161,919,304 cycles**. MC68020/040 closures are
**560,428 / 556,808** bytes with **4,876 / 4,823** relocations, and both host
SDK modes pass **1367/1367**.

MG644 adds the documented `MUIM_Listtree_Exchange` relative selectors for the
second tree node. `TreeNode2_Up` and `TreeNode2_Down` resolve through the named
sibling links of `TreeNode1`; the packet path ignores an unrelated `ListNode2`
and then uses the existing typed exchange rewrite. The focused MC68000 closure
is **517,824** bytes with **1,695 symbols**, **4,281 relocations**, **299,252
code bytes**, and **89 loops**; its map is zero-framework and
zero-managed-allocation, and native execution returns **42** after **5,061,764
instructions / 53,303,852 cycles**. Both host SDK modes pass **1366/1366**.
MC68020/040 regeneration for this focused entry remains progressive.

MG643 adds timestamped MorphOS-style Listtree double-click handling. Named
IntuiMessage `Seconds`/`Micros` fields feed a guest-resident typed click record;
a qualifying same-node pair toggles a list node through the enabled
`MUIA_Listtree_DoubleClick` policy. The focused MC68000 closure is **490,308**
bytes with **1,622 symbols**, **4,211 relocations**, **280,406 code bytes**, and
**81 loops**; its map is zero-framework and zero-managed-allocation, and native
execution returns **42** after **6,919,108 instructions / 73,042,606 cycles**.
Both host SDK modes pass **1366/1366**. MC68020/040 regeneration for this
focused entry remains progressive; cross-column history is covered by MG646,
while exact display-column geometry remains progressive.

MG642 adds typed MorphOS-style Listtree keyboard selection. `MUIKEY_PRESS`
selects the active node exclusively, while `MUIKEY_TOGGLE` reuses the named
per-node `MultiSelect` state. The focused MC68000 closure is **470,912** bytes
with **1,594 symbols**, **4,020 relocations**, **264,932 code bytes**, and
**80 loops**; its map is zero-framework and zero-managed-allocation, and the
native root returns **42** after **8,156,465 instructions / 85,145,430
cycles**. Both host SDK modes pass **1365/1365**. MC68020/040 regeneration
for this focused entry remains progressive.

MG641 adds typed MorphOS-style Listtree multi-selection. Exclusive clicks,
Control toggles, and Shift ranges are stored in named private node fields, with
the public TreeNode prefix unchanged; drag capture and Move commit behavior
remain independent. The focused MC68000/020/040 closures are
**478,888/491,656/487,176** bytes and the refreshed dispatcher closures are
**515,868/530,068/524,360** bytes; all maps remain zero-framework and
zero-managed-allocation. Host SDK modes pass **1364/1364**.

MG640 adds typed Listtree drag-release commits. Above, Below, and Onto marks
become named `Move` plans; cycles and same-node drops are rejected without
changing topology, and the transient header state is always cleared. The
focused native MC68000/020/040 closures are **475,848/488,184/484,048** bytes
and the main dispatcher closures are **513,652/527,620/522,036** bytes; both
host suites pass **1363/1363**. Production code remains exception-free and
managed-runtime-free.

MG638 qualifies viewport-aware `MUIM_Draw` for external `Listtree.mcc`.
The Draw path uses the named `FirstVisible` origin and row metric to invoke
`DisplayHook` only for rows in the laid-out viewport, with bounded preorder
skip/draw loops and a complete visible-walk fallback before layout. Main
MC68000/020/040 HUNK sizes are **511,724/525,212/519,952** bytes with
**3,995/4,078/3,994 relocations** and **292,786/305,942/301,018** code bytes;
the maps report zero framework/managed map features, **1,708 reachable
symbols**, and **85 loops**. Main native MC68000 execution returns **42** after
**19,981,918 instructions / 209,258,126 cycles**. The focused viewport-draw
closure is **501,904** bytes with **1,685 symbols** and **3,973 relocations**;
it reports zero framework/managed features and returns **42** after
**5,323,260 instructions / 55,633,818 cycles**. Both SDK modes pass
**1361/1361**. Richer graphics policy, pointer drag/drop, multi-select policy,
and MorphOS differential behavior remain progressive; production code remains
exception-free and managed-runtime-free.

MG637 qualifies the named `FirstVisible` viewport origin for external
`Listtree.mcc`. Layout clamps the origin to the visible display list; Active
changes keep the selected row visible, and pointer hit-testing plus TestPos use
the same named surface state. MC68000/020/040 HUNK sizes are
**511,508/524,960/519,712** bytes with **3,995/4,081/3,996 relocations** and
**292,570/305,678/300,770** code bytes; the maps report zero framework/managed
map features, **1,708 reachable symbols**, and **84 loops**. Native MC68000
execution returns **42** after **19,996,911 instructions / 209,415,920 cycles**.
Both SDK modes pass **1360/1360**. The current harness runs M68000; the 020/040
outputs are regenerated closure builds. Automatic viewport clipping, pointer
drag/drop, multi-select policy, and richer MorphOS differential behavior remain
progressive; production code remains exception-free and managed-runtime-free.

MG636 qualifies typed inherited vertical MUIKEY navigation (Up, Down, PageUp,
PageDown, Top, and Bottom) for external `Listtree.mcc`. Navigation walks the
visible preorder display list; a named surface record supplies page size and
the selected node is published through the existing Active setter/notification
seam. MC68000/020/040 HUNK sizes are **509,680/522,936/517,764** bytes with
**3,988/4,064/3,986 relocations** and **291,184/304,134/299,276** code bytes;
the maps report zero framework/managed map features, **1,704 reachable
symbols**, and **84 loops**. Native MC68000 execution returns **42** after
**15,526,052 instructions / 162,680,036 cycles**. Both SDK modes pass
**1359/1359**. The current harness runs M68000; the 020/040 outputs are
regenerated closure builds. Automatic viewport scrolling, pointer drag/drop,
multi-select policy, and richer MorphOS differential behavior remain
progressive; production code remains exception-free and managed-runtime-free.

MG635 qualifies typed inherited `MUIM_HandleInput` `SELECTUP` pointer routing
for the external `Listtree.mcc` class. A named `MuiIntuiPointerMessage` record
is decoded through the named collection input record; object-local viewport
hit-testing uses the named surface rectangle and row metric, then publishes
`MUIA_Listtree_Active` through the normal setter/notification seam. Visible-row
clicks select the node and out-of-viewport clicks remain unclaimed. MC68000/
020/040 HUNK sizes are **508,452/521,692/516,564** bytes with
**3,985/4,064/3,985 relocations** and **290,092/303,014/298,202** code bytes;
the maps report zero framework/managed map features, **1,703 reachable
symbols**, and **84 loops**. Native MC68000 execution returns **42** after
**11,889,235 instructions / 124,465,148 cycles**. Both SDK modes pass
**1358/1358**. The current harness runs M68000; the 020/040 outputs are
regenerated closure builds. Drag/drop, multi-select policy, and richer
scrolling remain progressive; production code remains exception-free and
managed-runtime-free.

MG622 qualifies the focused native dispatcher for the external MorphOS-shaped
`Listtree.mcc` class. The native fixture registers the class, creates a root
and child, routes named `MuiListtree*Message` records for `Insert`, `GetEntry`,
`Open`, `GetNr`, and `Remove`, then disposes the tree. Packet and node state
remain fixed-width structs with named fields; scalar field cursors are used
only for the current freestanding compiler boundary, so the dispatcher and
core do not substitute numeric offsets for records. MC68000/020/040 HUNK
sizes are **469,868/481,716/477,644** bytes with **3,730/3,788/3,720
relocations** and zero framework/managed map features; the MC68000 map has
**1,589 reachable symbols** and **78 loops**. Native execution returns **42**
after **3,956,556 instructions / 41,233,064 cycles**. The complete host suite
is **1350/1350** in both SDK modes. Broader MorphOS Listtree differential
coverage remains progressive; production code remains exception-free and
managed-runtime-free.

MG623 qualifies native Set/Get routing for the external MorphOS-shaped
`Listtree.mcc` class. Named `MuiListtreeSetMessage` and
`MuiListtreeGetMessage` records cover `MUIM_Set`, `MUIM_NoNotifySet`, and
`MUIM_Get`; the policy getter reads named fields from
`MuiListtreePolicyStateRecord`, and Get publishes the caller-owned ULONG
through its named scalar field. MC68000/020/040 HUNK sizes are
**472,848/484,728/480,632** bytes with **3,954/3,996/3,942 relocations** and
zero framework/managed map features; the MC68000 map has **1,594 reachable
symbols** and **78 loops**. Native execution returns **42** after
**4,272,868 instructions / 44,608,518 cycles**. The complete host suite is
**1350/1350** in both SDK modes. Richer Listtree policy and MorphOS
differential coverage remain progressive; production code remains
exception-free and managed-runtime-free.

MG624 qualifies typed Sort/Close routing for the external MorphOS-shaped
`Listtree.mcc` class. Sort preserves the public `TNF_OPEN`/`TNF_LIST` flags by
restoring the named `MuiListtreeNodeField.Flags` field after each relink, and
the native fixture completes Sort followed by Close. MC68000/020/040 HUNK
sizes are **473,780/485,888/481,668** bytes with **3,915/3,980/3,919
relocations** and **267,770/279,618/275,642** code bytes; the MC68000 map has
**1,598 reachable symbols** and **79 loops**. Native MC68000 execution
returns **42** after **4,641,601 instructions / 48,457,996 cycles**. Both SDK
modes pass **1350/1350**. The current native harness runs M68000; 020/040
outputs are regenerated closure builds. Richer MorphOS Listtree behavior
remains progressive; production code remains exception-free and
managed-runtime-free.

MG625 qualifies typed Rename/FindName routing for the external MorphOS-shaped
`Listtree.mcc` class. The native fixture renames the closed root through the
named packet and finds it recursively by the new guest string. MC68000/020/040
HUNK sizes are **476,312/488,564/484,220** bytes with **3,919/3,988/3,923
relocations** and **269,594/281,570/277,486** code bytes; the MC68000 map has
**1,604 reachable symbols** and **80 loops**. Native MC68000 execution returns
**42** after **4,918,332 instructions / 51,344,436 cycles**. Both SDK modes
pass **1350/1350**. The current native harness runs M68000; 020/040 outputs
are regenerated closure builds. Richer MorphOS Listtree behavior remains
progressive; production code remains exception-free and managed-runtime-free.

MG626 qualifies typed Move/Exchange routing for the external MorphOS-shaped
`Listtree.mcc` class. The native fixture inserts a second root, reparents the
first child beneath it through the named Move packet, exchanges the two roots,
and verifies the resulting head and parent fields through typed GetEntry.
MC68000/020/040 HUNK sizes are **478,936/491,472/486,840** bytes with
**3,857/3,925/3,858 relocations** and **271,704/283,966/279,602** code bytes;
the MC68000 map has **1,611 reachable symbols** and **81 loops**. Native
MC68000 execution returns **42** after **6,076,202 instructions /
63,445,354 cycles**. Both SDK modes pass **1350/1350**. The current native
harness runs M68000; 020/040 outputs are regenerated closure builds. Richer
MorphOS Listtree behavior remains progressive; production code remains
exception-free and managed-runtime-free.

MG634 qualifies typed inherited `MUIM_HandleInput` routing for external
`Listtree.mcc`. The named collection input record preserves the IntuiMessage
pointer and signed MUIKEY; Right opens the active unfrozen node and Left closes
it, while unsupported keys and leaves remain unclaimed. MC68000/020/040 HUNK
sizes are **505,360/518,500/513,520** bytes with **3,975/4,044/3,983
relocations** and **288,054/300,918/296,184** code bytes; the maps report zero
framework/managed map features and **1,695 reachable symbols**. Native
execution returns **42** after **11,210,659 instructions / 117,358,386 cycles**.
The complete host suite is **1357/1357** in both SDK modes. The current native
harness runs M68000; the 020/040 outputs are regenerated closure builds.
Pointer and drag/drop behavior remain delegated to the parent Listview;
production code remains exception-free and managed-runtime-free.

MG633 qualifies typed Area lifecycle routing (`MUIM_Setup`, `MUIM_Cleanup`,
`MUIM_Show`, and `MUIM_Hide`) for external `Listtree.mcc`. A named lifecycle
record stores render-info ownership and setup/visibility state; hidden Draw
calls are successful neutral no-ops. MC68000/020/040 HUNK sizes are
**504,104/517,192/512,280** bytes with **3,977/4,049/3,989 relocations** and
**287,286/300,086/295,416** code bytes; the maps report zero
framework/managed map features and **1,691 reachable symbols**. Native
execution returns **42** after **9,994,708 instructions / 104,546,378 cycles**.
The complete host suite is **1356/1356** in both SDK modes. Lifecycle and
rendering remain neutral and progressive; production code remains
exception-free and managed-runtime-free.

MG632 qualifies typed `MUIM_Layout` and `MUIM_AskMinMax` routing for the
external MorphOS-shaped `Listtree.mcc` class. A named guest surface-state
record stores the rectangle; the neutral min/max model derives default height
from visible preorder rows and a fixed row metric. MC68000/020/040 HUNK sizes
are **494,012/500,760/495,936** bytes with **3,904/3,946/3,884 relocations**
and **281,132/290,122/285,548** code bytes; the MC68000 map has **1,659
reachable symbols** and **84 loops**. Native execution returns **42** after
**9,444,773 instructions / 98,785,350 cycles**. The complete host suite is
**1355/1355** in both SDK modes. Geometry remains a neutral surface model;
production code remains exception-free and managed-runtime-free.

MG631 qualifies the external `Listtree.mcc` Draw/DisplayHook path. Typed Draw
packets run a neutral visible-preorder renderer backed by named node state and
invoke DisplayHook for each visible node; the native fixture verifies the
visible child callback with A0=Hook, A2=Listtree, and A1=child. MC68000/020/040
HUNK sizes are **487,848/500,760/495,936** bytes with
**3,877/3,946/3,884 relocations** and **277,486/290,122/285,548** code bytes;
the MC68000 map has **1,638 reachable symbols** and **83 loops**. Native
execution returns **42** after **8,932,964 instructions / 93,456,690 cycles**.
Both SDK modes pass **1354/1354**. The current native harness runs M68000;
020/040 outputs are regenerated closure builds. Rendering remains neutral,
named-struct based, exception-free, and managed-runtime-free.

MG630 qualifies the arbitrary `SortHook` callback for the external
MorphOS-shaped `Listtree.mcc` class. Typed Set and Sort packets invoke the
callback with A0=Hook, A2=left node, and A1=right node; the native adapter
compares the named node `Name` fields and records the call through the fixed
Hook ABI record. MC68000/020/040 HUNK sizes are **484,556/497,384/492,632**
bytes with **3,874/3,943/3,880 relocations** and **275,670/288,222/283,722**
code bytes; the MC68000 map has **1,626 reachable symbols** and **82 loops**.
Native execution returns **42** after **8,433,711 instructions / 88,208,660
cycles**. Both SDK modes pass **1353/1353**. The current native harness runs
M68000; 020/040 outputs are regenerated closure builds. Hook records remain
named and the production path remains exception-free and managed-runtime-free.

MG629 qualifies arbitrary ConstructHook/DestructHook callbacks for the external
MorphOS-shaped `Listtree.mcc` class. Typed Set packets publish named hook
records; Insert invokes construct with A0=Hook, A2=NULL, A1=user input and
stores its result in the named node `User` field, while Remove invokes
destruct with A0=Hook, A2=NULL, A1=that constructed pointer. MC68000/020/040
HUNK sizes are **483,904/496,684/491,948** bytes with
**3,867/3,935/3,872 relocations** and **275,048/287,554/283,072** code bytes;
the MC68000 map has **1,626 reachable symbols** and **82 loops**. Native
execution returns **42** after **7,662,462 instructions / 80,116,150 cycles**.
Both SDK modes pass **1352/1352**. The current native harness runs M68000;
020/040 outputs are regenerated closure builds. Hook records remain named and
the production path remains exception-free and managed-runtime-free.

MG628 qualifies the external MorphOS-shaped `Listtree.mcc` lifecycle-hook
boundary. Typed `MUIM_Set` packets publish named OpenHook and CloseHook
records, and typed Open/Close packets invoke them through the existing native
callback capability. The fixture verifies the named Hook call record with
MorphOS register semantics (A0=Hook base, A2=object, A1=current node) without
managed callbacks or exceptions. MC68000/020/040 HUNK sizes are
**483,424/496,148/491,420** bytes with **3,869/3,938/3,870 relocations** and
**274,558/287,006/282,550** code bytes; the MC68000 map has **1,626 reachable
symbols** and **82 loops**. Native execution returns **42** after
**6,972,660 instructions / 72,868,352 cycles**. Both SDK modes pass
**1351/1351**. The current native harness runs M68000; 020/040 outputs are
regenerated closure builds. Richer MorphOS Listtree hook behavior remains
progressive; production code remains exception-free and managed-runtime-free.

MG627 qualifies typed SetDropMark/TestPos routing for the external
MorphOS-shaped `Listtree.mcc` class. The native fixture sets a named drop mark,
routes a typed TestPos packet, and verifies the mixed-width result through the
named `TreeNode`, `Flags`, and `ListEntry` field codecs. MC68000/020/040 HUNK
sizes are **482,712/495,380/490,672** bytes with
**3,869/3,938/3,870 relocations** and **273,996/286,386/281,950** code bytes;
the MC68000 map has **1,624 reachable symbols** and **82 loops**. Native
MC68000 execution returns **42** after **6,283,650 instructions /
65,606,052 cycles**. Both SDK modes pass **1350/1350**. The current native
harness runs M68000; 020/040 outputs are regenerated closure builds. Richer
MorphOS Listtree behavior remains progressive; production code remains
exception-free and managed-runtime-free.

MG621 qualifies the native Window event-handler link lifecycle. Registered
`MUI_EventHandlerNode` records mirror the private queue through named
`Successor`/`Predecessor` fields; duplicate registration is rejected while
`MUI_EHF_ISENABLED` is set, and removal/cleanup relinks neighbors and clears
the removed node. MC68000/020/040 HUNK sizes are **447,884/459,440/455,684**
bytes with **3,336/3,383/3,369 relocations** and zero framework/managed map
features; the MC68000 map has **1,517 reachable symbols**. Native execution
returns **42** after **6,515,800 instructions / 68,258,376 cycles**. All
variants pass the zero-runtime map gate. The complete host suite is
**1349/1349** in both SDK modes. Full MorphOS differential event behavior
remains progressive.

MG620 qualifies the named `MUI_EHF_ISACTIVEGRP` relationship on native code. A
Group-owned handler becomes `MUI_EHF_ISACTIVE` while the Window active object is
below that group, receives one typed `MUIP_HandleEvent` callback through the
`DispatchEventHandlerNode` seam, and clears the flag when focus moves outside.
The closure uses `MuiEventHandlerNodeRecord` and named Parent topology; guest
strings remain the only explicit boundary. MC68000/020/040 HUNK sizes are
**458,264/470,040/466,260** bytes with **3,476/3,540/3,502 relocations**; the
MC68000 map has **1,552 reachable symbols**, zero framework members, and zero
managed allocation sites. Native execution returns **42** after
**11,558,727 instructions / 121,390,432 cycles**. All variants pass the
zero-runtime map gate. The complete host suite is **1348/1348** in both SDK
modes. Full MorphOS differential event behavior remains progressive.

MG619 qualifies the typed `MUIArea` `handledEvents` registration lifecycle on
native code. A child attached below an open Window creates the generated
`MUI_EventHandlerNode`, receives one preprocessed `MUIP_HandleEvent` packet via
the real Window poll path, and unregisters the node when detached. MC68000/
020/040 HUNK sizes are **477,232/489,852/485,468** bytes with
**3,831/3,981/3,860 relocations**; the MC68000 map has **1,596 reachable
symbols**, zero framework members, and zero managed allocation sites. Native
execution returns **42** after **10,701,281 instructions / 112,930,468 cycles**.
The complete host suite is **1348/1348** in both SDK modes. The numeric C
`MUIA_HandledEvents` value remains intentionally deferred because the
authoritative MorphOS Objective-C contract does not publish one; broader event
translation and MorphOS differential parity remain progressive.

MG618 qualifies native delivery to a registered MorphOS `MUI_EventHandlerNode`.
The preprocessed `MUIP_HandleEvent` packet now travels through the normal
`PollWindowEvents` route, where the typed `MuiEventHandlerNodeInput` and
`MuiEventHandlerNodeRecord` contract applies the GUI-mode/event-mask gate,
invokes the explicitly selected class callback, and clears the transient
`MUI_EHF_ISCALLING` bit after return. MC68000/020/040 HUNK sizes are
**476,668/489,152/484,676** bytes with **3,846/3,995/3,852 relocations**;
the MC68000 map has **1,595 reachable symbols**, zero framework members, and
zero managed allocation sites. Native execution returns **42** after
**7,278,382 instructions / 76,789,106 cycles**. All variants pass the
zero-runtime map gate. The complete host suite is **1347/1347** in both SDK
modes. Broader event translation and MorphOS differential parity remain
progressive.

MG617 qualifies the typed MorphOS preprocessed Window-event seam. The platform
fills the caller-owned `MUIP_HandleEvent` packet through the named
`MuiWindowEventInput` contract, and `PollWindowEvents` validates and routes it
through `MuiCommonHandleEventMessage` without exposing positional packet
fields to the core. MC68000/020/040 HUNK sizes are
**474,924/487,192/482,932** bytes with **3,809/3,910/3,826 relocations**;
the MC68000 map has **1,589 reachable symbols**, zero framework members, and
zero managed allocation sites. Native execution returns **42** after
**4,903,416 instructions / 51,620,664 cycles**. All variants pass the
zero-runtime map gate. The complete host suite is **1346/1346** in both SDK
modes. Broader platform event translation and MorphOS differential parity
remain progressive.

MG616 qualifies the typed MorphOS Window pointer-polling seam. The named
`MuiWindowPointerInput` carrier crosses the platform boundary by reference;
the native provider supplies the resolved object and caller-owned
`InputEvent`, and the core validates and publishes `MUIA_Window_MouseObject`
and `MUIA_Window_InputEvent` together in guest-resident event state. The
handoff is isolated in a native-lowered helper, while the existing event
routing and help/disable-key behavior remain intact. MC68000/020/040 HUNK
sizes are **476,264/487,668/484,356** bytes with
**4,105/4,139/4,136 relocations**; the MC68000 map has **1,588 reachable
symbols**, zero framework members, and zero managed allocation sites. Native
execution returns **42** after **5,953,704 instructions / 62,857,526 cycles**.
All variants pass the zero-runtime map gate. The complete host suite is
**1346/1346** in both SDK modes. Broader platform hit-testing and MorphOS
differential parity remain progressive.

MG615 qualifies the typed MorphOS Window event-polling seam. The named
`MuiWindowEventSample` carries the native window, caller-owned `InputEvent`
storage, and resolved event class through the platform contract; the poller
validates the pointers before dispatch and fills the named Amiga record via
`MuiWindowInputEventCodec`. MC68000/020/040 HUNK sizes are
**476,156/488,052/484,904** bytes with **4,137/4,298/4,267 relocations**;
the MC68000 map has **1,586 reachable symbols**, zero framework members, and
zero managed allocation sites. Native execution returns **42** after
**5,121,344 instructions / 53,990,642 cycles**. All variants pass the
zero-runtime map gate. The complete host suite is **1346/1346** in both SDK
modes. Broader platform event translation and MorphOS differential parity
remain progressive.

MG614 qualifies the typed MorphOS `MUIA_Window_InputEvent` publication seam.
The caller-owned Amiga `InputEvent` is represented by its named SDK struct and
crosses guest memory only through `MuiWindowInputEventCodec`; the window keeps
the validated pointer in named event state. MC68000/020/040 HUNK sizes are
**447,500/458,708/455,552** bytes with **3,779/3,932/3,899 relocations**; the
MC68000 framework report has **1,509 reachable methods**, zero members, and
zero managed allocation sites. Native execution returns **42** after
**1,664,554 instructions / 17,337,756 cycles**. All variants pass the
zero-runtime map gate. The complete host suite is **1345/1345** in both SDK
modes. Full platform event translation and MorphOS differential parity remain
progressive.

MG613 qualifies the typed `MUIA_Window_Open` lifecycle route. The named
`MuiWindowLifecycleStateRecord` publishes the native window handle only after
open succeeds and clears it on close; the broad Set/NoNotifySet dispatcher
also recognizes the attribute, while the native root qualifies the focused
packet seam. MC68000/020/040 HUNK sizes are **457,036/468,492/465,256** bytes
with **3,840/3,999/3,958 relocations**; the MC68000 framework report has
**1,540 reachable methods**, zero members, and zero managed allocation sites.
Native execution returns **42** after **3,939,878 instructions / 41,370,274
cycles**. All variants pass the zero-runtime map gate. The complete host suite
is **1344/1344** in both SDK modes. Guest lifecycle state remains typed and
guest-resident; MorphOS differential parity is progressive.

MG612 qualifies the live packet-facing `MUIM_MultiSet` mutation route. A named
dispatch request decodes the fixed message, validates the executor and
NULL-terminated target vector, and applies the attribute to each listed object
while leaving the executor unchanged. The MC68000 HUNK is **449,016 bytes**
with **3,765 internal relocations** and **1,515 reachable methods**; the
framework report has zero members and zero managed allocation sites. Native
execution returns **42** after **4,552,934 instructions / 47,373,940 cycles**.
MC68020/040 closures are **460,216/456,940 bytes** with **3,920/3,876
relocations** and zero-runtime maps. The complete host suite is **1344/1344**
in both SDK modes. Guest APTR reads remain confined to the unavoidable vector
wire boundary; state and packet semantics use named structs.

MG611 qualifies the typed `MUIM_MultiSet` target-vector boundary. The focused
`MultiSetTargetVectorCodecRoot` uses named `MuiMultiSetTargetVectorCursor` and
`MuiMultiSetTargetEntry` records plus the production `MultiSetVector` address
helper; it returns **42** after **1,884 instructions / 17,354 cycles**. MC68000/
020/040 HUNK sizes are **2,320/2,300/2,300** bytes with 11 reachable methods,
zero-runtime gates, and reloc-free maps. The complete host suite is
**1344/1344** in both SDK modes. Full native MultiSet object mutation and
MorphOS differential parity remain progressive.

MG602 qualifies the MorphOS external BOOPSI scratch-packet seam. `OM_SET`,
`OM_GET`, and `GM_RENDER` use scalar native-safe helpers backed by named
`MuiExternalBoopsi*` records; packed guest arithmetic remains confined to the
codec. `ExternalBoopsiPacketCodecRoot` returns **42** after **2,300
instructions / 25,520 cycles**; MC68000/020/040 HUNK sizes are
**3,452/3,496/3,508** bytes with 13 reachable methods and zero-runtime map
gates. Host malformed, null, and result-word coverage remains, and the
complete suite is **1344/1344** in both SDK modes. Full native callback
behavior and MorphOS differential parity remain progressive.

MG603 qualifies the headless creation `TagItem` boundary. Creation records
are written and walked through named `MuiAslTagItemRecord` values and the
shared cursor/vector codecs. `HeadlessCreationTagCodecRoot` returns **42**
after **1,980 instructions / 19,624 cycles**; MC68000/020/040 HUNK sizes are
**2,716/2,708/2,720** bytes with 13 reachable methods and zero-runtime map
gates. The complete host suite is **1344/1344** in both SDK modes. Full
headless object-factory behavior and MorphOS differential parity remain
progressive.

MG604 qualifies the Family projection list/vector boundary. Named list,
field-cursor, vector-entry, vector-cursor, and inline-vector records are used
by `FamilyProjectionListVectorCodecRoot`, which returns **42** after **2,355
instructions / 22,676 cycles**. MC68000/020/040 HUNK sizes are
**3,572/3,616/3,616** bytes with 15 reachable methods and zero-runtime map
gates. The complete suite remains **1344/1344** in both SDK modes. Full native
Family mutation behavior and MorphOS differential parity remain progressive.

MG605 qualifies the `MUI_MakeObjectA` generated `TagItem` boundary for the
button shape. `WriteButtonTagRecords` emits named `MuiAslTagItemRecord`
values, and `MakeObjectGeneratedTagCodecRoot` returns **42** after **9,046
instructions / 87,800 cycles**. MC68000/020/040 HUNK sizes are
**3,804/3,768/3,780** bytes with 15 reachable methods and zero-runtime map
gates. The complete suite remains **1344/1344** in both SDK modes. Other
MakeObjectA shapes, full native construction, and MorphOS differential parity
remain progressive.

MG606 qualifies the List pointer-slot boundary used by `MUIA_List_TitleArray`.
`ListPointerSlotCodecRoot` exercises named pointer-slot, field-cursor, and
bounded cursor records and returns **42** after **2,058 instructions / 19,428
cycles**. MC68000/020/040 HUNK sizes are **2,396/2,420/2,420** bytes with 11
reachable methods and zero-runtime map gates. The complete suite remains
**1344/1344** in both SDK modes. Full native TitleArray ownership/display
behavior and MorphOS differential parity remain progressive.

MG607 qualifies the larger caller-owned List pointer-vector boundary used by
external entry vectors. `ListPointerVectorCodecRoot` exercises the named
`MuiListPointerVectorCursor` with the shared slot record and returns **42**
after **1,253 instructions / 12,184 cycles**. MC68000/020/040 HUNK sizes are
**2,236/2,236/2,236** bytes with 11 reachable methods and zero-runtime map
gates. The complete suite remains **1344/1344** in both SDK modes. Full native
List pointer-vector consumers and MorphOS differential parity remain
progressive.

MG608 qualifies the List `ColumnOrder` BYTE* permutation boundary through the
named `MuiListColumnOrderByteCursor` codec. `ListColumnOrderByteCodecRoot`
returns **42** after **365 instructions / 3,924 cycles**. MC68000/020/040 HUNK
sizes are **1,096/1,096/1,096** bytes with four reachable methods and
zero-runtime map gates. The complete suite remains **1344/1344** in both SDK
modes. Full native ColumnOrder state, copying, display behavior, and MorphOS
differential parity remain progressive.

MG609 qualifies the ExternalWrapper `TagItem` route. Caller-owned creation
tags and wrapper-owned remembered BOOPSI tags are traversed through the named
`MuiExternalTagListCursor`, `MuiExternalRememberCursor`,
`MuiAslTagItemRecord`, and shared codecs. `ExternalWrapperTagItemCodecRoot`
returns **42** after **27,441 instructions / 266,026 cycles**; MC68000/020/040
HUNK sizes are **26,288/26,956/26,448** bytes with 97 reachable methods and
zero-runtime map gates. The complete suite remains **1344/1344** in both SDK
modes. Full native ExternalWrapper creation/update behavior and MorphOS
differential parity remain progressive.

MG610 qualifies the Poplist array pointer-vector route. Caller-owned and
materialized NULL-terminated string-pointer vectors use the named
`MuiPoplistArrayCursor` and `MuiPoplistArrayEntry` records;
`PoplistArrayCodecRoot` returns **42** after **134,781 instructions /
1,384,034 cycles**. MC68000/020/040 HUNK sizes are
**17,880/19,680/19,276** bytes with 56 reachable methods and zero-runtime map
gates. The complete suite remains **1344/1344** in both SDK modes. Full native
Poplist requester behavior and MorphOS differential parity remain progressive.

MG596 qualifies the MorphOS NotifyWrite typed method-header seam. Scalar
selector admission stays inside
`MuiNotifyWriteMessageCodec.TryReadMethodIdValue`, while named NotifyWrite
method and WriteLong/WriteString records remain the consumer ABI.
`NotifyWriteMethodHeaderCodecRoot` returns **42** after **482 instructions /
5,376 cycles**; MC68000/020/040 HUNK sizes are **1,668/1,664/1,664** bytes
with 8 reachable methods and zero-runtime map gates. Focused coverage is
**1/1** and the complete host suite remains **1343/1343** in both SDK modes.
Full native NotifyWrite dispatch and MorphOS differential parity remain
progressive.

MG597 qualifies the MorphOS external Listtree typed method-header seam.
Scalar selector admission stays inside
`MuiListtreeMessageCodec.TryReadMethodIdValue`, while named Listtree payload
records remain the consumer ABI. `ListtreeMethodHeaderCodecRoot` returns
**42** after **476 instructions / 5,334 cycles**; MC68000/020/040 HUNK sizes
are **2,508/2,504/2,504** bytes with 8 reachable methods and zero-runtime
map gates. Focused coverage is **1/1** and the complete host suite remains
**1343/1343** in both SDK modes. Full native Listtree dispatch and MorphOS
differential parity remain progressive.

MG598 qualifies the MorphOS Dirlist/Volumelist typed method-header seam.
Scalar selector admission stays inside
`MuiDirlistMessageCodec.TryReadMethodIdValue`, while named Dirlist packet
records remain the consumer ABI. `DirlistMethodHeaderCodecRoot` returns
**42** after **476 instructions / 5,334 cycles**; MC68000/020/040 HUNK sizes
are **1,772/1,768/1,768** bytes with 8 reachable methods and zero-runtime
map gates. Focused coverage is **1/1** and the complete host suite remains
**1343/1343** in both SDK modes. Full native Dirlist/Volumelist dispatch and
MorphOS differential parity remain progressive.

MG599 qualifies the shared MorphOS Headless typed method-header seam. Scalar
selector admission stays inside
`MuiHeadlessMessageCodec.TryReadMethodIdValue`, while the named
`MuiHeadlessMethodMessage` remains the consumer ABI. `HeadlessMethodHeaderCodecRoot`
returns **42** after **251 instructions / 2,550 cycles**; MC68000/020/040 HUNK
sizes are **1,000/1,000/1,000** bytes with 5 reachable methods and
zero-runtime map gates. Focused coverage is **1/1** and the complete host suite
remains **1343/1343** in both SDK modes. Full native Headless dispatch and
MorphOS differential parity remain progressive.

MG600 qualifies the MorphOS ShortHelp typed method-header seam. Scalar
selector admission stays inside
`MuiAreaShortHelpMessageCodec.TryReadMethodIdValue`, while the named
`MuiAreaShortHelpMethodMessage` and complete Create/Delete/Check packet
records remain the consumer ABI. `AreaShortHelpMethodHeaderCodecRoot` returns
**42** after **545 instructions / 5,802 cycles**; MC68000/020/040 HUNK sizes
are **1,904/1,900/1,900** bytes with 9 reachable methods and zero-runtime map
gates. Focused coverage is **1/1** and the complete host suite is **1344/1344**
in both SDK modes. Full native ShortHelp capability behavior and MorphOS
differential parity remain progressive.

MG601 qualifies the MorphOS Process/Slave dispatch typed method-header seam.
The fixed `{ArgumentCount, MethodID}` header admits its scalar selector through
`MuiProcessDispatchPacketCodec.TryReadMethodIdValue`, while the named
`MuiProcessDispatchPacketHeader` and argument-slot structs remain the consumer
ABI. `ProcessDispatchMethodHeaderCodecRoot` returns **42** after **372
instructions / 3,868 cycles**; MC68000/020/040 HUNK sizes are
**1,872/1,840/1,868** bytes with 8 reachable methods and zero-runtime map
gates. The complete host suite remains **1344/1344** in both SDK modes. Full
native Process/Slave dispatch and MorphOS differential parity remain
progressive.

MG595 qualifies the MorphOS Dataspace-IFF typed method-header seam. Scalar
selector admission stays inside
`MuiDataspaceIffMessageCodec.TryReadMethodIdValue`, while named ReadIFF/WriteIFF
records remain the consumer ABI. `DataspaceIffMethodHeaderCodecRoot` returns
**42** after **386 instructions / 4,158 cycles**; MC68000/020/040 HUNK sizes
are **1,408/1,404/1,404** bytes with 7 reachable methods and zero-runtime map
gates. Focused coverage is **1/1** and the complete host suite remains
**1343/1343** in both SDK modes. Full native IFF dispatch and MorphOS
differential parity remain progressive.

MG594 qualifies the MorphOS Dataspace typed method-header seam. Scalar selector
admission stays inside `MuiDataspaceMessageCodec.TryReadMethodIdValue`, while
the named `MuiDataspaceMethodMessage` record remains the consumer ABI.
`DataspaceMethodHeaderCodecRoot` returns **42** after **476 instructions /
5,334 cycles**; MC68000/020/040 HUNK sizes are **1,776/1,772/1,772** bytes
with 8 reachable methods and zero-runtime map gates. Focused coverage is
**1/1** and the complete host suite remains **1343/1343** in both SDK modes.
Full native Dataspace dispatch and MorphOS differential parity remain
progressive.

MG593 qualifies the MorphOS Datamap/Objectmap typed method-header seam.
Scalar selector admission stays inside
`MuiStoreMessageCodec.TryReadMethodIdValue`, while the named
`MuiStoreMethodMessage` record remains the consumer ABI.
`StoreMethodHeaderCodecRoot` returns **42** after **476 instructions / 5,334
cycles**; MC68000/020/040 HUNK sizes are **1,808/1,804/1,804** bytes with 8
reachable methods and zero-runtime map gates. Focused coverage is **1/1** and
the complete host suite remains **1343/1343** in both SDK modes. Full native
store dispatch and MorphOS differential parity remain progressive.

MG592 qualifies the MorphOS Window event-handler typed method-header seam.
Scalar selector admission stays inside
`MuiWindowEventHandlerPacketCodec.TryReadMethodIdValue`, while the named
handler packet retains its pointer field. `WindowEventHandlerMethodHeaderCodecRoot`
returns **42** after **551 instructions / 5,994 cycles**; MC68000/020/040 HUNK
sizes are **1,672/1,668/1,668** bytes with 8 reachable methods and
zero-runtime map gates. Focused coverage is **1/1** and the complete host suite
remains **1343/1343** in both SDK modes. Full native handler
registration/delivery and MorphOS differential parity remain progressive.

MG591 qualifies the MorphOS Application/Window menu typed method-header seam.
Scalar selector admission stays inside
`MuiApplicationMenuPacketCodec.TryReadMethodIdValue`, while named menu records
retain MenuId and State. `ApplicationMenuMethodHeaderCodecRoot` returns **42**
after **966 instructions / 10,482 cycles**; MC68000/020/040 HUNK sizes are
**1,984/1,976/1,980** bytes with 8 reachable methods and zero-runtime map
gates. Focused coverage is **1/1** and the complete host suite remains
**1343/1343** in both SDK modes. Full native menu dispatch and MorphOS
differential parity remain progressive.

MG590 qualifies the MorphOS Window SetCycleChain typed method-header seam.
Selector admission stays inside
`MuiWindowCycleChainPacketCodec.TryReadMethodIdValue`, while the named
cycle-chain record and bounded inline vector remain the consumer ABI.
`WindowCycleChainMethodHeaderCodecRoot` returns **42** after **348
instructions / 3,690 cycles**; MC68000/020/040 HUNK sizes are
**1,540/1,536/1,536** bytes, with 8 reachable methods and zero-runtime map
gates. Focused coverage is **1/1** and the complete host suite remains
**1343/1343** in both SDK modes. Full native cycle-chain mutation and MorphOS
differential parity remain progressive.

MG589 qualifies the MorphOS application method-packet typed method-header
family. ConfigId, CheckRefresh, Loop, window-method, and Snapshot retain named
packet records while scalar selector admission stays inside
`MuiApplicationMethodPacketCodec.TryReadMethodIdValue`.
`ApplicationMethodHeaderCodecRoot` returns **42** after **1,143 instructions
/ 12,388 cycles**; MC68000/020/040 HUNK sizes are **1,972/1,960/1,964**
bytes, with 8 reachable methods and zero-runtime map gates. Focused coverage
is **1/1** and the complete host suite remains **1343/1343** in both SDK modes.
Full native method-packet dispatch and MorphOS differential parity remain
progressive.

MG588 qualifies the MorphOS application-settings typed method-header family.
SetConfigItem, OpenConfigWindow, BuildSettingsPanel, and SettingsIO retain
named settings packet records while scalar selector admission stays inside
`MuiApplicationSettingsPacketCodec.TryReadMethodIdValue`.
`ApplicationSettingsMethodHeaderCodecRoot` returns **42** after **952
instructions / 10,328 cycles**; MC68000/020/040 HUNK sizes are
**1,960/1,952/1,956** bytes, with 8 reachable methods and zero-runtime map
gates. Focused coverage is **1/1** and the complete host suite remains
**1343/1343** in both SDK modes. Full native settings dispatch and MorphOS
differential parity remain progressive.

MG587 qualifies the MorphOS application-presentation typed method-header
family. ShowHelp and AboutMUI retain named presentation packet records while
scalar selector admission stays inside
`MuiApplicationPresentationPacketCodec.TryReadMethodIdValue`.
`ApplicationPresentationMethodHeaderCodecRoot` returns **42** after **583
instructions / 6,286 cycles**; MC68000/020/040 HUNK sizes are
**1,800/1,792/1,792** bytes, with 8 reachable methods and zero-runtime map
gates. Focused coverage is **1/1** and the complete host suite remains
**1343/1343** in both SDK modes. Full native presentation dispatch and
MorphOS differential parity remain progressive.

MG586 qualifies the MorphOS application-queue typed method-header family.
PushMethod and UnpushMethod retain named queue packet records and struct-shaped
request state while scalar selector admission stays inside
`MuiApplicationQueuePacketCodec.TryReadMethodIdValue`.
`ApplicationQueueMethodHeaderCodecRoot` returns **42** after **579 instructions
/ 6,260 cycles**; MC68000/020/040 HUNK sizes are **1,760/1,752/1,752** bytes,
with 8 reachable methods and zero-runtime map gates. Focused coverage is
**1/1** and the complete host suite remains **1343/1343** in both SDK modes.
Full native queue-tail dispatch and MorphOS differential parity remain
progressive.

MG585 qualifies the MorphOS application-input typed method-header family.
ReturnID, Input/NewInput, InputBuffered, and input-handler packets retain named
records while scalar selector admission stays inside
`MuiApplicationInputPacketCodec.TryReadMethodIdValue`.
`ApplicationInputMethodHeaderCodecRoot` returns **42** after **955 instructions
/ 10,360 cycles**; MC68000/020/040 HUNK sizes are **1,892/1,884/1,888** bytes,
with 8 reachable methods and zero-runtime map gates. Focused coverage is
**1/1** and the complete host suite remains **1343/1343** in both SDK modes.
Full native input queue/handler dispatch and MorphOS differential parity remain
progressive.

MG584 qualifies the MorphOS Group-ordering typed method-header family.
MoveMember, Reorder, and Sort retain named packet records and struct-shaped
inputs while scalar selector admission stays inside
`MuiGroupOrderingMessageCodec.TryReadMethodIdValue`.
`GroupOrderingRecordRoot` returns **42** after **3,975 instructions / 42,720
cycles**; MC68000/020/040 HUNK sizes are **5,152/5,092/5,112** bytes, with 22
reachable methods and zero-runtime map gates. Focused coverage is **2/2** and
the complete host suite remains **1343/1343** in both SDK modes. Full native
Group ordering dispatch and MorphOS differential parity remain progressive.

MG583 qualifies the MorphOS UpdateConfig typed method-header seam. Full-packet
validation and redraw-entry writes retain named method and explicit table
structs while scalar selector admission stays inside
`MuiUpdateConfigCore.TryReadMethodIdValue`.
`UpdateConfigMethodHeaderCodecRoot` returns **42** after **302 instructions /
3,184 cycles**; MC68000/020/040 HUNK sizes are **1,576/1,572/1,572** bytes,
with 8 reachable methods and zero-runtime map gates. Focused coverage is
**2/2** and the complete host suite remains **1343/1343** in both SDK modes.
Full native redraw-table dispatch and MorphOS differential parity remain
progressive.

MG582 qualifies the MorphOS Notify typed method-header family. Notify,
KillNotify, KillNotifyObject, Set, MultiSet, and FindObject retain named packet
structs while scalar selector admission stays inside
`MuiNotifyPacketCodec.TryReadMethodIdValue`.
`NotifyPacketCodecRoot` returns **42** after **3,637 instructions / 39,330
cycles**; MC68000/020/040 HUNK sizes are **4,008/4,068/4,072** bytes, with 14
reachable methods and zero-runtime map gates. Focused coverage is **2/2** and
the complete host suite is **1343/1343** in both SDK modes. Full native Notify
dispatch and MorphOS differential parity remain progressive.

MG581 qualifies the MorphOS Group-change typed method-header family. InitChange,
ExitChange, and ExitChange2 retain named packet structs while scalar selector
admission stays inside `MuiGroupChangeMessageCodec`.
`GroupChangePacketsRoot` returns **42** after **2,313 instructions / 24,168
cycles**; MC68000/020/040 HUNK sizes are **3,580/3,556/3,564** bytes, with 18
reachable methods and zero-runtime map gates. Focused coverage is **2/2** and
the complete host suite remains **1342/1342** in both SDK modes. Full native
Group-change dispatch and MorphOS differential parity remain progressive.

MG580 qualifies the MorphOS AreaDrag typed method-header family. Begin, Drop,
Event, Finish, Query, Report, CreateDragImage, and DeleteDragImage readers
retain named packet structs while scalar selector admission stays inside
`MuiAreaDragMessageCodec`.
`AreaDragMessageCodecRoot` returns **42** after **8,684 instructions /
93,416 cycles**; MC68000/020/040 HUNK sizes are **6,548/6,616/6,636** bytes,
with 24 reachable methods and zero-runtime map gates. Focused coverage is
**2/2** and the complete host suite remains **1342/1342** in both SDK modes.
Full native AreaDrag dispatch and MorphOS differential parity remain progressive.

MG579 qualifies the MorphOS Process specialist typed method-header family.
Process/Slave Get/Set, signal, error, dispatch, and lifecycle readers retain
named packet structs while scalar selector admission stays inside
`MuiProcessSpecialistMessageCodec`.
`ProcessSpecialistMessageCodecRoot` returns **42** after **5,760 instructions /
62,130 cycles**; MC68000/020/040 HUNK sizes are **6,052/6,072/6,072** bytes,
with 26 reachable methods and zero-runtime map gates. Focused coverage is
**2/2** and the complete host suite remains **1342/1342** in both SDK modes.
Full native Process dispatch and MorphOS differential parity remain progressive.

MG578 qualifies the MorphOS Pop specialist typed method-header family. Get/Set,
Close, and lifecycle readers retain named packet structs while scalar selector
admission stays inside `MuiPopSpecialistMessageCodec`.
`PopSpecialistMessageCodecRoot` returns **42** after **3,945 instructions /
42,548 cycles**; MC68000/020/040 HUNK sizes are **4,672/4,680/4,680** bytes,
with 21 reachable methods and zero-runtime map gates. Focused coverage is
**2/2** and the complete host suite remains **1342/1342** in both SDK modes.
Full native Pop dispatch and MorphOS differential parity remain progressive.

MG577 qualifies the MorphOS Color specialist typed method-header family. Get/Set,
pointer, RGB, and lifecycle readers retain named packet structs while scalar
selector admission stays inside `MuiColorSpecialistMessageCodec`.
`ColorSpecialistMessageCodecRoot` returns **42** after **5,321 instructions /
57,432 cycles**; MC68000/020/040 HUNK sizes are **5,484/5,504/5,508** bytes,
with 24 reachable methods and zero-runtime map gates. Focused coverage is
**2/2** and the complete host suite remains **1342/1342** in both SDK modes.
Full native Color dispatch and MorphOS differential parity remain progressive.

MG572 qualifies the common-control typed method-header path. The dispatcher
uses scalar selector admission only at the native lowering seam; the public
`MuiCommonMethodMessage` and operation-specific packet records remain named
value types. `CommonControlPacketsRoot` returns **42** with
MC68000/020/040 HUNK sizes **6,764/6,824/6,848** bytes, zero-runtime map
gates, and **2/2** focused method-header coverage. The complete host suite is
**1342/1342** in both SDK modes; full native dispatch and MorphOS differential
parity remain progressive.

MG576 qualifies Family mutation method headers for AddHead/AddTail/Remove,
Insert, Transfer, Reorder, and Sort. The named `MuiFamilyMethodMessage` and
operation records remain the struct surface; scalar selector admission stays
inside the codec. `FamilyMutationMessageCodecRoot` returns **42** after
**2,517 instructions / 27,260 cycles**; MC68000/020/040 HUNK sizes are
**3,948/3,952/3,952** bytes, with 16 reachable methods and zero-runtime map
gates. Focused coverage is **2/2** and the complete host suite remains
**1342/1342** in both SDK modes. Full native Family mutation and MorphOS
differential parity remain progressive.

MG575 qualifies the MethodID-only MorphOS Family_DoChildMethods packet. The
named `MuiFamilyDoChildMethodsMessage` remains the consumer struct; scalar
selector admission stays inside the codec. `FamilyDoChildMethodsMessageCodecRoot`
returns **42** after **439 instructions / 4,566 cycles**; MC68000/020/040 HUNK
sizes are **2,112/2,100/2,100** bytes, with 11 reachable methods and
zero-runtime map gates. Focused coverage is **4/4** and the complete host suite
remains **1342/1342** in both SDK modes. Full native Family forwarding and
MorphOS differential parity remain progressive.

MG574 qualifies the MorphOS Family_GetChild typed method header. The named
`MuiFamilyGetChildMethodMessage` and complete packet record remain the public
struct surface; scalar selector admission stays inside the codec.
`FamilyGetChildMessageCodecRoot` returns **42** after **1,055 instructions /
11,010 cycles**; MC68000/020/040 HUNK sizes are **2,568/2,568/2,568** bytes,
with 12 reachable methods and zero-runtime map gates. Focused coverage is
**4/4** and the complete host suite remains **1342/1342** in both SDK modes.
Full native Family topology dispatch and MorphOS differential parity remain
progressive.

MG573 qualifies the Misc-specialist typed method-header family. Lifecycle,
Get/Set, pointer/pair, HandleInput, and RegisterGadget packets retain named
records while scalar selector admission stays inside the codec.
`MiscSpecialistMessageCodecRoot` returns **42** after **7,088 instructions /
76,384 cycles**; MC68000/020/040 HUNK sizes are **5,744/5,804/5,808** bytes,
with 22 reachable methods and zero-runtime map gates. Focused coverage is
**2/2** and the complete host suite remains **1342/1342** in both SDK modes.
Full native Misc dispatch and MorphOS differential parity remain progressive.

MG571 qualifies the typed MorphOS collection-surface method-header family:
`Layout`, `AskMinMax`, `Draw`, `HandleInput`, and attribute packets retain named
records while scalar selector admission remains inside the shared codec.
`CollectionSurfaceMessageCodecRoot` returns **42** after **5,210 instructions /
56,292 cycles**; the MC68000/020/040 HUNK is **6,204/6,248/6,260** bytes, with
25 reachable methods and no managed members or allocations. Focused surface
coverage is **3/3**, and the complete host suite remains **1342/1342** in both
SDK modes. Full native surface dispatch and MorphOS differential parity remain
progressive.

MG570 qualifies the typed MorphOS List-basic method-header family:
`GetEntry`, `Select`, `Clear`, and `Sort` retain named packet and method
records while scalar selector admission remains inside the basic codec.
`CollectionListBasicMessageCodecRoot` returns **42** after **3,174
instructions / 34,130 cycles**; the MC68000/020/040 HUNK is
**3,920/3,928/3,932** bytes, with 17 reachable methods and no managed members
or allocations. Focused List-basic coverage is **4/4**, and the complete host
suite remains **1342/1342** in both SDK modes. Full native List basic dispatch
and MorphOS differential parity remain progressive.

MG569 qualifies the typed MorphOS List-advanced method-header family:
`InsertSingle`, `Insert`, positional/pointer/pair operations, and `CreateImage`
retain named records while scalar selector admission remains inside the
advanced codec. `CollectionListAdvancedMessageCodecRoot` returns **42** after
**4,639 instructions / 50,464 cycles**; the MC68000/020/040 HUNK is
**5,612/5,628/5,640** bytes, with 23 reachable methods and no managed members
or allocations. Focused List-advanced coverage is **3/3**, and the complete
host suite remains **1342/1342** in both SDK modes. Full native List advanced
dispatch and MorphOS differential parity remain progressive.

MG568 qualifies the typed MorphOS List-edit method-header family:
`CreateEditObject`, `Edit`, `EditDone`, and `EndEdit` retain named records with
signed row/column fields while scalar selector admission remains inside the
codec. `CollectionListEditMessageCodecRoot` returns **42** after **4,555
instructions / 49,288 cycles**; the MC68000/020/040 HUNK is
**5,452/5,472/5,488** bytes, with 21 reachable methods and no managed members
or allocations. Focused List-edit coverage is **2/2**, and the complete host
suite remains **1342/1342** in both SDK modes. Full native List editing and
MorphOS differential parity remain progressive.

MG567 qualifies the typed MorphOS List-record method-header family:
`Construct`, `Destruct`, `Display`, `Compare`, and `TestPos` retain named
operation structs while scalar selector admission remains inside the collection
codec. `CollectionListRecordMessageCodecRoot` returns **42** after **5,776
instructions / 62,506 cycles**; the MC68000/020/040 HUNK is
**5,628/5,664/5,672** bytes, with 21 reachable methods and no managed members
or allocations. Focused List-record coverage is **2/2**, and the complete host
suite remains **1342/1342** in both SDK modes. Full native List dispatch and
MorphOS differential parity remain progressive.

MG566 qualifies the typed MorphOS UserData Find/Get/Set packet family.
`UserDataMessageCodecRoot` keeps named packet records as the consumer surface
while scalar selector admission and packed guest arithmetic remain inside the
codec. It returns **42** after **1,864 instructions / 20,100 cycles**; the
MC68000/020/040 HUNK is **2,828/2,848/2,848** bytes, and all variants pass the
zero-runtime/framework gates. Focused UserData coverage is **10/10** and the
complete host suite remains **1342/1342** in both SDK modes. Full native Family
traversal and MorphOS differential parity remain progressive.

MG565 qualifies the typed MorphOS `MUIP_GoActive`/`MUIP_GoInactive` packet
family. `AreaActivationMessageCodecRoot` keeps named MethodId/Flags records as
the consumer surface while scalar selector admission and packed guest arithmetic
remain inside the codec. It returns **42** after **1,555 instructions / 16,668
cycles**; the MC68000/020/040 HUNK is **2,620/2,604/2,608** bytes, and all
variants pass the zero-runtime/framework gates. Focused Area activation coverage
is **7/7** and the complete host suite remains **1342/1342** in both SDK modes.
Full native activation policy and MorphOS differential parity remain
progressive.

MG564 qualifies the typed MorphOS `MUIM_BoopsiQuery` packet family.
`BoopsiQueryMessageCodecRoot` keeps the complete named Screen/Flags/signed-
dimensions/RenderInfo record as the consumer surface while scalar selector
admission and packed guest arithmetic remain inside the codec. It returns **42**
after **2,969 instructions / 31,822 cycles**; the MC68000/020/040 HUNK is
**3,220/3,324/3,324** bytes, and all variants pass the zero-runtime/framework
gates. Focused BoopsiQuery coverage is **5/5** and the complete host suite
remains **1342/1342** in both SDK modes. Full native BOOPSI callback behavior
and MorphOS differential parity remain progressive.

MG563 qualifies the typed MorphOS `MUIM_SetAsString` packet family.
`SetAsStringPacketsRoot` keeps named MethodId/Attribute/Format/Value records and
the caller-owned parameter boundary as the consumer surface while scalar
selector admission and packed guest arithmetic remain inside the codec. It
returns **42** after **1,326 instructions / 13,844 cycles**; the MC68000/020/040
HUNK is **3,004/2,984/2,984** bytes, and all variants pass the zero-runtime/
framework gates. Focused SetAsString coverage is **5/5** and the complete host
suite remains **1342/1342** in both SDK modes. Full native formatting behavior
and MorphOS differential parity remain progressive.

MG562 qualifies the typed MorphOS `MUIM_GetConfigItem` packet family.
`GetConfigItemMessageCodecRoot` keeps named MethodId/ConfigId/Storage records as
the consumer surface while scalar selector admission and packed guest arithmetic
remain inside the codec. It returns **42** after **1,045 instructions / 10,894
cycles**; the MC68000/020/040 HUNK is **2,544/2,544/2,544** bytes, and all
variants pass the zero-runtime/framework gates. Focused GetConfigItem coverage
is **4/4** and the complete host suite remains **1342/1342** in both SDK modes.
Full native configuration semantics and MorphOS differential parity remain
progressive.

MG561 qualifies the typed MorphOS `MUIM_Export`/`MUIM_Import` packet family.
`ObjectPersistenceMessageCodecRoot` keeps named method/dataspace records as the
consumer surface while scalar selector admission and packed guest arithmetic
remain inside the codec. It returns **42** after **1,908 instructions / 19,646
cycles**; the MC68000 HUNK is **3,440** bytes, with MC68020/MC68040 variants at
**3,424/3,424** bytes, and all variants pass the zero-runtime/framework gates.
Focused Object persistence coverage is **2/2** and the complete host suite
remains **1342/1342** in both SDK modes. Full native persistence behavior and
MorphOS differential parity remain progressive.

MG557 qualifies the typed MorphOS external-wrapper packet family for
`Boopsi.mui`/`Dtpic.mui`. Update, Get, Set, method-only, RenderInfo, AskMinMax,
and Layout packets retain named value-type records; selector validation and
bounded guest arithmetic remain inside the codec. `ExternalWrapperMessageCodecRoot`
returns **42** after **7,889 instructions / 82,618 cycles**; the MC68000 HUNK is
**6,788** bytes, with MC68020/MC68040 variants at **6,812/6,824** bytes, and all
variants pass the zero-runtime/framework gates. Focused wrapper coverage is
**83/83** and the complete host suite remains **1342/1342** in both SDK modes.
Full native wrapper dispatch/lifecycle behavior and MorphOS differential parity
remain progressive.

MG558 qualifies the typed MorphOS Menustrip/Menu/Menuitem packet family:
OM_GET, Set/NoNotifySet, Family pointer/pair verbs, popup, and method-only
records retain named value-type records; selector validation and bounded guest
arithmetic remain inside the codec. `MenuSpecialistMessageCodecRoot` returns
**42** after **6,475 instructions / 68,230 cycles**; the MC68000 HUNK is
**6,256** bytes, with MC68020/MC68040 variants at **6,288/6,292** bytes, and
all variants pass the zero-runtime/framework gates. Focused MenuSpecialist
coverage is **39/39** and the complete host suite remains **1342/1342** in both
SDK modes. Full native menu dispatch and MorphOS differential parity remain
progressive.

MG559 qualifies the shared typed MorphOS common-control packet family: signed,
numeric, stringify, HandleEvent, Get, attribute, AskMinMax, Layout, Draw,
Setup, and method-header records retain named value-type records; selector
validation and bounded guest arithmetic remain inside the codec.
`CommonControlPacketsRoot` returns **42** after **7,117 instructions / 74,444
cycles**; the MC68000 HUNK is **6,764** bytes, with MC68020/MC68040 variants at
**6,824/6,848** bytes, and all variants pass the zero-runtime/framework gates.
Focused common-control packet coverage is **3/3** and the complete host suite
remains **1342/1342** in both SDK modes. Full native common-control dispatch and
MorphOS differential parity remain progressive.

MG544 adds the live `MUIM_BoopsiQuery` path for `Boopsi.mui`.
`MuiBoopsiQueryCore.DispatchToObject` validates the complete named 40-byte
record and forwards the caller-owned packet through the existing typed
`IMuiBoopsiCapability.DoMethod` seam. Focused coverage is **2/2** and the
complete host suite is **1338/1338** in both SDK modes; malformed packets,
managed callbacks, and offset-based shadow state are not accepted.

MG545 routes public `MUI_DisposeObject` calls for standalone `Boopsi.mui` and
`Dtpic.mui` instances through `MuiExternalWrapperLifecycle`. External
classes/pictures and owned guest blocks therefore release exactly once through
the named wrapper records instead of generic headless lookup. Focused coverage
is **2/2** and the complete host suite is **1340/1340** in both SDK modes.

MG546 routes native Keyadjust raw-key text input through the named
`MuiIntuiRawKeyMessage`/`MuiIntuiMessageCodec` boundary. The 0x1C-byte prefix,
`IDCMP_RAWKEY` class, and Code field are validated in one typed codec; native
providers no longer repeat raw message offsets. Focused codec coverage is
**2/2** and the complete host suite remains **1340/1340** in both SDK modes.
Full MorphOS input-to-key-string conversion and native differential parity
remain progressive.

MG547 adds native qualification for the typed Keyadjust raw-key boundary. The
native-root project links the shared `IAmigaGuestMemory` contract, and the
Mccprefs qualification uses `MuiMiscStateCursor` plus
`MuiMiscMccprefsStateCodec` instead of an anonymous instance offset. The
MC68000 raw-key closure rejects non-RAWKEY input, returns **42**, and passes
the framework/zero-runtime gate at 3,908 bytes; MC68020 and MC68040 variants
pass the zero-runtime map gate. Full MorphOS key-string conversion and
differential parity remain progressive.

MG548 adds the typed MorphOS `checkShortHelp:mx:my:` boundary through
`MuiShortHelpCheckSample` and `IMuiShortHelpCapability`. The fixed 16-byte
packet has named `WriteCheck`/`TryReadCheck` helpers; provider identity is
validated before accepting dynamic text, while static caller-owned
`MUIA_ShortHelp` remains the fallback. Focused coverage is **2/2** and the
complete host suite is **1342/1342** in both SDK modes. Native packet
qualification returns **42** from a 2,500-byte MC68000 HUNK; MC68020 and
MC68040 variants pass the zero-runtime map gate. Bubble ownership and full
MorphOS differential parity remain progressive.

MG549 extends the named `MuiIntuiRawKeyMessage` with the Intuition Qualifier
field, completing the Class/Code/Qualifier prefix used by Keyadjust input.
`MuiIntuiMessageCodec` reads and writes the record through the cursor codec;
the Code-only helper is only a compatibility projection. Focused coverage is
**2/2** and the complete suite is **1342/1342** in both SDK modes. The MC68000
HUNK returns **42** at 4,108 bytes, with MC68020/MC68040 zero-runtime map gates
passing. Keyboard-layout conversion and MorphOS differential parity remain
progressive.

MG560 qualifies the fixed typed MorphOS `MUIM_CallHook` envelope. The named
Hook/Param1 packet record and caller-owned variadic parameter cursor remain
struct-based, while scalar method admission and bounded guest arithmetic stay
inside the codec. `CallHookMessageCodecRoot` returns **42** after **978
instructions / 10,214 cycles**; the MC68000 HUNK is **2,464** bytes, with
MC68020/MC68040 variants at **2,448/2,448** bytes, and all variants pass the
zero-runtime/framework gates. Focused CallHook coverage is **5/5** and the
complete host suite remains **1342/1342** in both SDK modes. Full native hook
dispatch and MorphOS differential parity remain progressive.

MG550 adds a native typed codec closure for the Area handled-events state and
generated event-handler node. `MuiAreaHandledEventsStateRecord` and
`MuiEventHandlerNodeRecord` preserve the mixed-width fields as named structs;
the MC68000 HUNK returns **42** at 8,144 bytes, with MC68020/MC68040
zero-runtime map gates passing. Full object/window registration and the numeric
C `MUIA_HandledEvents` ingress remain progressive until the MorphOS ABI is
authoritative.

MG551 adds native qualification for the named Window relationship record used
by `MUIA_Window_Menustrip`. RootObject, Menustrip, and RefWindow pointers are
round-tripped through one typed guest codec; the MC68000 HUNK returns **42** at
2,544 bytes, with MC68020/MC68040 2,556-byte zero-runtime map gates passing. Host tests
continue to cover live ownership, replacement, and clearing; full native
object/Family registration remains progressive.

MG552 adds native qualification for the named Window visual/event-state record
behind `MUIA_Window_NoMenus`, `MUIA_Window_HasAlpha`, `MUIA_Window_Opacity`,
`MUIA_Window_FancyDrawing`, and `MUIA_Window_MenuAction`. All fields round-trip
through a typed codec, and a corrupted cookie is rejected through its named
field cursor. The MC68000 HUNK returns **42** at 2,656 bytes; MC68020/MC68040
variants are 2,692 bytes and pass zero-runtime map gates. Full native rendering
and event transport remain progressive.

MG553 adds native qualification for the named Window
`AddEventHandler`/`RemoveEventHandler` packet. Method identity and handler
pointers round-trip through the typed `MuiWindowEventHandlerPacketInput` path,
and malformed method IDs are rejected before handler access. The MC68000 HUNK
returns **42** at 2,648 bytes; MC68020/MC68040 variants are 2,640 bytes and
pass zero-runtime map gates. Full native registration and event delivery remain
progressive beyond this packet closure.

MG554 adds native qualification for the named Window `SetCycleChain` packet.
MethodId and FirstObject round-trip through the typed codec, the inline vector
tail is bounded by overflow/mapping checks, and malformed method IDs are
rejected. The MC68000 HUNK returns **42** at 2,868 bytes; MC68020 is 2,840 bytes
and MC68040 is 2,844 bytes, all passing zero-runtime map gates. Full live
cycle-chain object mutation remains progressive.

MG555 adds native qualification for the Application/Window menu query and set
packets. Named MethodId, MenuId, and State fields round-trip through the shared
typed resolver, and invalid method IDs are rejected before payload access. The
MC68000 HUNK returns **42** at 3,828 bytes; MC68020 is 3,800 bytes and MC68040
is 3,816 bytes, all passing zero-runtime map gates. Live menu operation
forwarding remains progressive.

MG556 qualifies the typed Layout packet family. AskMinMax, Relayout,
rectangle, text, render-info, flags, TextDimensions, and Layout readers publish
named records after scalar method-header admission; the codec owns all bounded
guest arithmetic. `LayoutPacketCodecRoot` returns **42** after **6,728
instructions / 69,576 cycles**. The MC68000 HUNK is **5,628** bytes, with
MC68020/MC68040 variants at **5,716/5,736** bytes, and all variants pass the
zero-runtime map gates. Focused Layout coverage is **18/18** and the complete
host suite remains **1342/1342**. Full native layout/render behavior and
MorphOS differential parity remain progressive.

Common object help now follows the MorphOS `[ISG]` contract for
`MUIA_HelpNode` and `MUIA_HelpLine`. `MuiHelpStateRecord` keeps the opaque
guest node pointer, signed line, and generation together in object Dataspace;
direct and common-control Get/Set/NoNotifySet paths use that named state for
built-in and external objects. Raw public slots are limited to
bootstrap/reconciliation, with no managed strings, exceptions, or new
object-private offsets. Focused coverage is **3/3** and the complete host
suite is **1310/1310** in both SDK modes.

Application online help now has a typed `ShowHelpFromObject` seam. It walks
the named guest Parent chain, resolves the first non-NULL HelpNode and first
explicit HelpLine independently, and then calls the existing presentation
capability. The resolver uses no managed object graph, exceptions, or private
object offsets. Focused resolution coverage is **2/2** and the complete host
suite is **1312/1312** in both SDK modes; automatic HELP-key acquisition
remains progressive.

MG530 adds `MuiKeyadjustInputSample` and `IMuiKeyadjustInputCapability` so
`HandleInput` can consume platform-owned mouse, click-count, and multi-key
metadata through named structs. The native-root provider reports no sample;
focused metadata coverage is **1/1** and the complete host suite is
**1323/1323** in both SDK modes.

MG531 adds `MuiKeyadjustTextInputSample` to the input capability. The named
sample is the canonical text-translation result for `HandleInput`; the legacy
primitive translator remains only as a compatibility fallback. Focused
coverage is **1/1** and the complete host suite is **1324/1324** in both SDK
modes. Native translation and full MorphOS differential parity remain
progressive.

MG532 validates the caller-owned `IntuiMessage` and source `MuiKey` after both
named Keyadjust capability samples. A provider that rewrites either identity
is rejected before policy or guest-string mutation; focused coverage is **1/1**
and the complete host suite is **1325/1325** in both SDK modes.

MG533 adds `MuiShortHelpCreateSample`/`MuiShortHelpDeleteSample` and
`IMuiShortHelpCapability`. Dynamic Create/Delete results are supplied through
named structs, while static caller-owned `MUIA_ShortHelp` remains the fallback;
focused coverage is **1/1** and the complete host suite is **1326/1326** in both
SDK modes. Native temporary-string lifetime remains progressive.

MG534 adds named `MuiDragImageCreateSample`/`MuiDragImageDeleteSample` records
and `IMuiDragImageCapability` for MorphOS Area CreateDragImage/DeleteDragImage.
The 16-byte and 8-byte packets preserve signed touch coordinates, flags, and an
opaque provider-owned handle; the core validates identity but never synthesizes
bitmap state or manages the handle lifetime. Focused coverage is **1/1** and
the complete host suite is **1327/1327** in both SDK modes. Native allocation
and deletion remain progressive.

MG535 adds `MuiPointerCaptureSample` and `IMuiPointerCaptureCapability` for
Listview row and scrollbar thumb gestures. Capture is optional and is released
through the same named guest-resident state cleanup paths used for finish,
cancellation, policy changes, and disposal. Focused coverage is **1/1** and
the complete host suite is **1328/1328** in both SDK modes; native capture and
cross-window drag routing remain progressive.

MG524 adds the typed `MuiHelpTriggerInput` automatic-help seam. A preprocessed
MorphOS `MUIKEY_HELP` request reads the Window owner's named `MouseObject`,
resolves HelpNode/HelpLine through the bounded Parent chain, and is consumed
before ordinary handlers only when `ShowMuiHelp` succeeds. Focused coverage is
**5/5** and the complete host suite is **1315/1315** in both SDK modes; native
pointer tracking remains progressive.

MG525 adds the named `MuiWindowPointerInput` publication seam. A platform/input
producer supplies the window, already-resolved deepest live `MouseObject`, and
caller-owned `InputEvent` pointer in one struct; validation occurs before
either getter-only attribute changes, and failed publication restores the
previous state. Focused coverage is **1/1** and the complete host suite is
**1316/1316** in both SDK modes. Native coordinates, acquisition, hit testing,
and full MorphOS differential parity remain progressive.

MG526 extends the application platform with `ReadMuiWindowPointer`. Polling
seeds the named `MuiWindowPointerInput` with the current Window and
caller-owned InputEvent, lets a native provider resolve MouseObject, and then
publishes the complete getter-only state atomically. The native headless
provider explicitly reports no sample until hit testing is implemented.
Focused pointer publication/polling coverage is **2/2** and the complete host
suite is **1317/1317** in both SDK modes.

MG527 separates caller-owned standard `InputEvent` storage from an optional
preprocessed `MUIP_HandleEvent` packet using the named
`MuiWindowEventPollInput` and `MuiWindowEventInput` structs. The platform fills
the message through `ReadMuiWindowEvent`, and polling dispatches it only after
the named storage boundary is validated. Focused coverage is **1/1** and the
complete host suite is **1318/1318** in both SDK modes.

MG528 adds the named 16-byte `MuiAreaCheckShortHelpMessage` and routes
`MUIM_CheckShortHelp` through `MuiAreaShortHelpPacketCore.Check`. The bounded
path returns the caller-owned `MUIA_ShortHelp` pointer without managed
allocation or ownership transfer; focused coverage is **2/2** and the complete
host suite is **1320/1320** in both SDK modes. Coordinate-sensitive dynamic
help and native bubble presentation remain progressive capabilities.

MG529 routes the keyboard subset of `MUIP_HandleInput` for `Keyadjust.mui`
through `MuiMiscSpecialistCore.HandleInput` and the named
`MuiKeyadjustInputRecord`. Printable keys and platform-translated raw events
use bounded two-byte guest C-string scratch storage, with no managed
allocation or private message offsets. Focused coverage is **2/2** and the
complete host suite is **1322/1322** in both SDK modes; native event metadata
remains progressive.

`MUI_MakeObjectA` now decodes its variable parameter prefix once into the
named `MuiMakeObjectParameterRecord` through
`MuiMakeObjectParameterCodec`. Construction and tag materialization use named
`First`/`Second`/`Third`/`Fourth` fields; malformed count, null, and truncated
vectors are rejected at the guest-memory boundary. Host/source/CIL remains
**540/540**. `MakeObjectParameterCodecRoot` produces **1,176 / 1,200 / 1,208
bytes** for MC68000/020/040 with zero relocations and no framework members or
managed allocation sites; MC68000 returns **42** after **388 / 4,406**
instructions/cycles.

Application/message getters now share a typed public-admission predicate for
`MUIA_ApplicationObject`, `MUIA_AppMessage`, and `MUIA_Window_AppWindow`.
Generic common-control `OM_GET` projects these values through the named
`MuiApplicationMessageRoutingStateRecord` even for unknown or custom classes,
matching direct `Get` without exposing raw handler offsets. The slice remains
freestanding, exception-free, managed-runtime-free, and struct-first; host
coverage is **1197/1197**.

Application `Commands` and `WindowList` getters now use the same typed
common-control `OM_GET` seam. The caller-owned command table and read-only
Exec List projection remain represented by named guest structs, including
validation and topology-derived window membership; no raw handler offsets are
introduced. Host coverage is **1198/1198**.

The broader Application/Window lifecycle, identity, policy, relationship, and
focus getter family now shares that typed common-control `OM_GET` seam. The
existing named guest records remain authoritative and no raw handler offsets
are introduced; host coverage is **1199/1199**.

Window.mui control, policy, relationship, lifecycle, presentation, visual, and
event getters now share the typed common-control `OM_GET` seam as well. The
existing named guest records remain authoritative, and the native window
pointer remains capability-backed; host coverage is **1200/1200**.

Event-handler reconciliation now honors `MUI_EHF_ISACTIVEGRP` through a
bounded named-parent walk from the active or default object. Handler state
continues to cross the named codec without managed object graphs; host
coverage is **1201/1201**.

The handled-events slice adds a named `MuiAreaHandledEventsStateRecord` in
object Dataspace. It owns the event mask, Window association, and generated
`MuiEventHandlerNodeRecord`; family attach/detach and object disposal reconcile
that registration through the existing guest list, with no managed shadow
state, exceptions, or raw object offsets. Focused host coverage is **1/1**.
The complete host suite is **1202/1202**.
The numeric C `MUIA_HandledEvents` ingress remains deferred pending MorphOS ABI
verification; the [MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html)
defines the property semantics.

The same named handled-events record now carries MorphOS MUIArea's
`eventHandlerAlwaysKeys`, `eventHandlerGuiMode`, and signed
`eventHandlerPriority` policy. Policy changes detach and unregister the live
generated node before reconciling a new node, and `SetHandledEvents` preserves
the policy across event-mask updates. GUIMODE is enabled by default as in the
MorphOS API. Numeric C attribute ingress remains deferred until the ABI values
are verified.

The policy setters also work before a non-zero handled-events mask exists.
They retain the values in the same named record without allocating a
  zero-mask `MuiEventHandlerNodeRecord`; the first non-zero mask reuses that
  policy during normal Window reconciliation. Focused coverage is **2/2** and
  the complete host suite is **1298/1298** in both SDK modes.

Misc specialists now also expose the exact MorphOS `MUIP_HandleInput` frame as
the named `MuiMiscHandleInputMessage` record and bounded packet-only seam.
`IntuiMessage` and signed `MuiKey` remain typed fields; fixed guest offsets stay
inside the codec. Keyadjust's live input route remains deferred until the
MorphOS-documented conversion to `MUIA_Keyadjust_Key` is verified.

Keyadjust policy recording now consumes the named fixed-width
`MuiKeyadjustInputRecord` (`KeyText`, `IsMouse`, `ClickCount`, and `MultiKey`)
through its canonical struct overload. The primitive call shape remains only as
a compatibility wrapper; live input conversion is still intentionally pending
the undocumented MorphOS contract.

`MUIA_Keyadjust_ForceKeyCode` is represented separately by the named
`MuiKeyadjustPolicyState` as an ULONG, preserving values such as `0x80000001`
without BOOL normalization. The live event-to-key-string conversion remains
deferred until its MorphOS contract is verified.

Title Position, OnLastClose, and EventHandlerPriority now use signed fields in
the named Title state record. Negative EventHandlerPriority values round-trip
through the ULONG-shaped ABI ingress without changing the guest record layout.

Title Clickable is now a signed initialize-only field in that same named state
record. Runtime Set and generic Get paths correctly leave the `[I..]` attribute
unclaimed, while the other Title flags remain BOOL policies.

Title EventHandlerPriority now follows `[I..]` access, while OnLastClose
follows `[IS.]`: runtime priority writes and generic getters for both fields are
rejected, with Position retaining `[ISG]` behavior.

Filepanel `AcceptPattern` and `RejectPattern` now enforce MorphOS `[I..]`
access before owned-string replacement; `Drawer`, `File`, and `Pattern` retain
`[ISG]` behavior. Focused coverage is **1/1**, and the current complete host
suite is **1298/1298** in both SDK modes.

Aboutmui `Application` now enforces MorphOS `[I..]`: initialization Set is
supported, while runtime Set and generic Get are rejected before the named
window/panel state is changed. Focused coverage verifies the no-get boundary.

Common Area geometry (`LeftEdge`, `TopEdge`, `Width`, `Height`, `RightEdge`, and
`BottomEdge`) is now getter-only at the runtime setter seam. Layout and Get use
the named `MuiAreaGeometryStateRecord`; focused coverage is **1/1**, and the
current complete host suite is **1300/1300** in both SDK modes.

Collection runtime access now has explicit struct-backed setter boundaries:
Dirlist/Volumelist reject getter-only counters/status/path and initializer-only
projections, Stringscroll rejects its getter-only text and construction-only
scroll policy, and Listview rejects getter/event projections and its
construction-only interaction policy. Internal construction and named-record
publication continue through the lower-level setters; no new raw-offset state
or managed runtime was introduced. Focused collection coverage adds **2/2**
facts; the complete host suite is **1301/1301** in both SDK modes. MorphOS
access modes follow the official [MUI class index](https://morphos-team.net/sdk/index_MUI.html)
and [Dirlist documentation](https://morphos-team.net/sdk/MUI/MUI_Dirlist.html).

Listtree `MUIA_Listtree_Quiet` now follows MorphOS `[.S.]` access: the named
policy field remains runtime-settable for redraw coalescing, but construction
and generic Get/OM_GET reject it. Focused coverage adds **1/1** fact; the
complete host suite is **1301/1301** in both SDK modes. See the official
[MorphOS Listtree documentation](https://morphos-team.net/sdk/MUI/MUI_Listtree.html).

Pop specialists now follow the MorphOS access matrix: Popasl `Type` is `[I.G]`,
`MUIFontStyles` is `[ISG]`, and Popcolor `ShowAlpha` is initialization-only
`[I..]` with no generic getter. The values remain in the named
`MuiPopSpecialistState` codec; focused coverage adds **1/1** fact and the
complete host suite is **1302/1302** in both SDK modes. See the official
[MorphOS Popasl documentation](https://morphos-team.net/sdk/MUI/MUI_Popasl.html).

Coloradjust `MUIA_Coloradjust_ShowAlpha` now follows MorphOS `[ISG]`: runtime
Set updates the named `MuiColorSpecialistState` flag and Get/OM_GET exposes it.
The same numeric tag remains class-specific, so Popcolor stays
initializer-only. Focused coverage adds **1/1** dispatcher fact; the complete
host suite is **1303/1303** in both SDK modes. See the official
[MorphOS Coloradjust documentation](https://morphos-team.net/sdk/MUI/MUI_Coloradjust.html).

The obsolete-but-supported Palette specialist now follows MorphOS access modes:
Entries is `[I.G]`, while Names and Groupable are `[ISG]`. All three use the
named Color state; runtime Set is rejected for Entries and supported for Names
and Groupable. Focused coverage adds **1/1** fact and the complete host suite is
**1304/1304** in both SDK modes. See the official
[MorphOS Palette documentation](https://morphos-team.net/sdk/MUI/MUI_Palette.html).

The Aboutmui application binding has also been corrected to MorphOS `[I..]`:
it remains in the named window/panel state for lifecycle use, but generic
Get/OM_GET does not claim it. See the official
[MorphOS Aboutmui documentation](https://morphos-team.net/sdk/MUI/MUI_Aboutmui.html).

Listview's named interaction policy now follows the MorphOS access boundary:
`Input`, `MultiSelect`, and `ScrollerPos` are `[I..]` and fail closed for
Get/OM_GET, while `DragType` remains getter-visible. Internal construction uses
an explicit raw bootstrap read so the struct-backed policy still drives input,
scroller, drag, and cleanup. Existing Listview coverage is **67/67** and the
complete host suite remains **1304/1304** in both SDK modes.

Private Penadjust `MUIA_Penadjust_PSIMode` now follows MorphOS `[I..]`:
initialization records the policy in the named `MuiColorSpecialistState`, while
runtime Set and generic Get/OM_GET remain unclaimed. Focused coverage is **1/1**;
the complete host suite remains **1304/1304** in both SDK modes. The path stays
freestanding, exception-free, managed-runtime-free, and struct-first. See the
[MorphOS MUI class index](https://morphos-team.net/sdk/index_MUI.html).

`MUIA_Menuitem_Menuitem` now follows the MorphOS `[ISG]` contract. Initialization
and runtime writes adopt submenu objects through the named Family child records,
and Get/OM_GET returns the first child from that same topology. Runtime adoption
notifies only after a successful link. Focused Menu coverage adds **1/1** fact;
the complete host suite is **1305/1305** in both SDK modes. See the official
[MorphOS Menuitem documentation](https://morphos-team.net/sdk/MUI/MUI_Menuitem.html).

`MUIA_Menustrip_CaseSensitive` now follows MorphOS `[I.G]`: initialization
writes use the named Menu specialist state, Get/OM_GET exposes the flag, and
runtime Set is rejected before mutation or notification. Focused Menustrip
coverage adds **1/1** fact; the complete host suite is **1306/1306** in
both SDK modes. See the official [MorphOS Menustrip documentation](https://morphos-team.net/sdk/MUI/MUI_Menustrip.html).

`MUIA_Menuitem_Trigger` now follows MorphOS `[.SG]`: initialization writes are
rejected, while runtime Set/Get use the named `MuiMenuSpecialistState.Trigger`
field and notify only when the token changes. Focused Menuitem coverage adds
**1/1** fact; the complete host suite is **1307/1307** in both SDK modes. See
the official [MorphOS Menuitem documentation](https://morphos-team.net/sdk/MUI/MUI_Menuitem.html).

The Area registration is also exposed through the public named
`MuiAreaEventHandlerStateInput` and `MuiAreaEventHandlerPacketCore` seam. This
keeps the future Objective-C bridge on typed values while Dataspace keys,
private object layout, and generated-node ownership remain internal.

Area activation now uses one named guest-resident
`MuiAreaActivationStateRecord` Dataspace record for Active, Flags, and its
generation. `GoActive` and `GoInactive` replace that record as one state
update, while `MuiAreaActivationPacketCore.TryGet` provides a typed public
projection. The implementation remains freestanding, exception-free,
managed-runtime-free, and struct-first; native GoActive/GoInactive ABI parity
is still progressive. Focused activation coverage is **7/7**; the MG450
qualification was **1204/1204** and the current complete host suite is
**1221/1221**.

The public `MuiAreaActivationPacketCore` now also exposes typed `GoActive` and
`GoInactive` transitions for the future Objective-C bridge. Packet decoding,
Dataspace keys, and guest ownership remain internal.

MorphOS `MUIA_DoubleBuffer` is now represented by the named guest
`MuiAreaDoubleBufferStateRecord`. Generic Get/Set and OM_GET/OM_SET normalize
the BOOL through that record, and `MuiAreaDoubleBufferPacketCore` provides a
typed value seam for future bridge code. Native off-screen allocation,
render-info replacement, and blitting remain progressive; no managed bitmap,
exception path, or managed runtime state was introduced. Focused coverage is
**4/4** and the current complete host suite is **1221/1221**. The behavioral
reference is the [MorphOS MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MorphOS `MUIA_ShortHelp` now uses the named guest
`MuiAreaShortHelpStateRecord` and typed `MuiAreaShortHelpPacketCore` seam.
Generic Get/Set and OM_GET/OM_SET retain the caller-owned `OBString` pointer
without a managed string shadow. Bubble creation, hit-testing, deletion
callbacks, and native help UI remain progressive. Focused short-help coverage
is **4/4** and the current complete host suite is **1221/1221**.

The fixed `MUIP_CreateShortHelp` and `MUIP_DeleteShortHelp` packets now cross
named codecs. Create returns the caller-owned ShortHelp pointer and Delete is
an accepted non-owning no-op; dynamic CheckShortHelp and native bubble
allocation remain progressive. Focused short-help packet coverage is **6/6**
and the current complete host suite is **1221/1221**.

Common-control drawing now consumes the named `MuiAreaRenderPolicyStateRecord`
for `MUIA_FillArea`. When FillArea is false the default background clear is
suppressed while frame and content rendering continue; focused coverage is
**1/1** and the complete host suite is **1221/1221**. See the [MorphOS MUIArea
documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

`MUIA_CustomBackfill` now uses the named
`MuiAreaPresentationStateRecord` BOOL field across construction, generic
Get/Set, and OM_GET/OM_SET. Setter values normalize to zero or one; focused
coverage is **2/2** and the complete host suite is **1221/1221**. Native
custom-backfill callback behavior remains progressive.

`MUIA_Draggable` and `MUIA_Dropable` now use the named
`MuiAreaDragPolicyStateRecord` for construction, generic Get/Set, OM_GET, and
drag-policy checks. Both setters normalize BOOL values and Dropable defaults
to TRUE as documented by MorphOS; focused coverage is **2/2** and the complete
host suite is **1221/1221**.

`MUIA_FrameVisible` now lives in the named
`MuiAreaRenderPolicyStateRecord`. Generic Get/Set, OM_GET, base Area Draw, and
common-control DrawControl suppress only frame lines when it is false; fill
and content remain active. Focused coverage is **2/2** and the complete host
suite is **1221/1221**.

`MUIA_ShowSelState` now uses named fields in
`MuiGadgetInteractionStateRecord` and `MuiImageRenderStateRecord`. The
initialize-only flag defaults TRUE, is projected by Get/OM_GET, is rejected by
runtime Set/OM_SET, and controls selected Gadget borders and builtin Image
selected pens. Focused coverage is **2/2** and the complete host suite is
**1223/1223**.

The packed GadTools `NewMenu` entries consumed by `MUIO_MenustripNM` now use
`MuiNewMenuRecordCodec` and the named `MuiNewMenuRecord` fields `Type`,
`Label`, `CommandKey`, `Flags`, `MutualExclude`, and `UserData`. Validation and
tree construction no longer repeat guest offsets. Host/source/CIL remains
**540/540**. `NewMenuRecordCodecRoot` produces **1,308 / 1,348 / 1,348 bytes**
for MC68000/020/040 with zero relocations and no framework members or managed
allocation sites; MC68000 returns **42** after **330 / 3,532**
instructions/cycles. Image-menu entries remain unsupported until their
MorphOS image semantics are qualified.

The existing 40-byte MorphOS `MUIP_BoopsiQuery` alias now uses
`MuiBoopsiQueryMessageCodec` for both guest reads and writes. Consumers use
the named `MuiBoopsiQueryMessage` fields; the packed offsets are confined to
the codec. Host/source/CIL remains **540/540**. `BoopsiQueryMessageCodecRoot`
produces **1,868 / 1,972 / 1,972 bytes** for MC68000/020/040 with zero
relocations and no framework members or managed allocation sites; MC68000
returns **42** after **709 / 8,052** instructions/cycles. External BOOPSI
callback semantics remain capability-backed and separate.

The Dataspace superclass packet family now routes Add, Find, Get, Merge,
Remove, and Clear through `MuiDataspaceMessageCodec`. Consumers keep named
fixed-width records and the packed guest offsets are confined to the codec.
Host/source/CIL remains **540/540**. `DataspaceMessageCodecRoot` produces
**3,876 / 3,948 / 3,952 bytes** for MC68000/020/040 with zero relocations and
no framework members or managed allocation sites; MC68000 returns **42** after
**1,415 / 15,474** instructions/cycles.

The paired ReadIFF and WriteIFF packet boundary now uses
`MuiDataspaceIffMessageCodec`. `DataspaceIffMessageCodecRoot` produces
**2,316 / 2,356 / 2,356 bytes** for MC68000/020/040 with zero relocations and
no framework members or managed allocation sites; MC68000 returns **42** after
**703 / 7,804** instructions/cycles. Stream behavior remains capability-backed.

The fixed `MUIM_CallHook` envelope now uses `MuiCallHookMessageCodec`; the
variadic tail remains caller-owned guest storage. Host/source/CIL remains
**540/540**. `CallHookMessageCodecRoot` produces **1,332 / 1,348 / 1,348
bytes** for MC68000/020/040 with zero relocations and no framework members or
managed allocation sites; MC68000 returns **42** after **351 / 3,768**
instructions/cycles.

The fixed `MUIM_GetConfigItem` packet now uses
`MuiGetConfigItemMessageCodec`. `GetConfigItemMessageCodecRoot` produces
**1,340 / 1,356 / 1,356 bytes** for MC68000/020/040 with zero relocations and
no framework members or managed allocation sites; MC68000 returns **42** after
**351 / 3,756** instructions/cycles.

Notify `MUIM_WriteLong` and `MUIM_WriteString` now use
`MuiNotifyWriteMessageCodec`. `NotifyWriteMessageCodecRoot` produces
**2,324 / 2,356 / 2,356 bytes** for MC68000/020/040 with zero relocations and
no framework members or managed allocation sites; MC68000 returns **42** after
**703 / 7,790** instructions/cycles.

The fixed Layout packet family now uses `MuiLayoutPacketCodec` for AskMinMax,
Relayout, DrawBackground, Backfill, and Text. Consumers retain named packet
structs and the packed guest offsets are confined to the codec. Host/source/
CIL remains **540/540**. `LayoutPacketCodecRoot` produces **2,252 / 2,340 /
2,348 bytes** for MC68000/020/040 with zero relocations and no framework
members or managed allocation sites; MC68000 returns **42** after **879 /
9,468** instructions/cycles.

The bounded ASL TagItem walker now uses the named
`MuiAslTagItemRecord`/`MuiAslTagItemCodec` boundary. TAG_DONE, TAG_MORE,
TAG_SKIP, and TAG_IGNORE traversal remains unchanged; raw 8-byte record
offsets are confined to the codec. Host/source/CIL remains **540/540**.
`AslTagItemCodecRoot` produces **1,296 / 1,284 / 1,288 bytes** for
MC68000/020/040 with zero relocations and no framework members or managed
allocation sites; MC68000 returns **42** after **309 / 3,306**
instructions/cycles.

The fixed Notify packet family (`MUIM_Notify`, `MUIM_KillNotify`,
`MUIM_KillNotifyObj`, `MUIM_Set`, `MUIM_MultiSet`, and `MUIM_FindObject`) now
uses `MuiNotifyPacketCodec.PacketAddress` and named message structs. Parameter
walking and MultiSet-vector decoding are centralized, and Application
`MUIM_Set` reuses the same codec. Host/source/CIL remains **540/540**.
`NotifyPacketCodecRoot` produces **2,996 / 3,060 / 3,060 bytes** for
MC68000/020/040, with zero relocations and no framework members or managed
allocation sites; MC68000 returns **42** after **933 / 10,140**
instructions/cycles. The older broad Notify root is still host-covered but is
tracked for closure-size cleanup because its current map retains internal
relocations. This boundary uses no exceptions or managed runtime services.

Application and Window menu query/set/event-handler packets now cross named
request structs and central codecs. The `MUIM_Set` path for
`MUIA_Window_ActiveObject` uses the same typed packet boundary, so consumers
operate on named menu IDs, state, handler, and active-object fields rather
than repeating ABI offsets. Host/source/CIL remains **540/540**. MC68000
focused artifacts are **44,656**, **44,008**, **48,788**, **49,992**, and
**44,612** bytes for ApplicationMenu, WindowMenuState, WindowActiveObject,
WindowActiveObjectSpatial, and WindowEventHandler; all return **42** in native
execution. Framework analysis has no members or managed allocation sites. The
two larger ActiveObject MC68020 closure artifacts retain **17** internal method
relocations; MC68000/040 and all other affected maps are relocation-free. The
inline SetCycleChain vector remains an explicit array ABI boundary.

Window SetCycleChain now decodes through a named request struct and central
codec. The inline object vector remains an explicit array ABI boundary with
overflow and mapping checks; the typed packet result reaches the existing
failure-atomic chain core. Host/source/CIL remains **540/540**.
`WindowCycleChainRoot` produces **43,148 / 49,248 / 46,284 bytes** for
MC68000/020/040, with zero-runtime maps, and MC68000 returns **42** after
**235,109 / 2,472,400** instructions/cycles.

SetConfigItem, OpenConfigWindow, BuildSettingsPanel, and Save/Load settings
packets now use a named settings-packet request struct and central codecs.
Typed item/data, flags/class ID, panel number, and settings-name fields reach
the existing cores; SetConfigItem retains its named guest-resident state.
Host/source/CIL remains **540/540**. Their MC68000/020/040 focused artifacts
are **43,268 / 49,216 / 46,360**, **42,536 / 48,560 / 45,608**,
**42,116 / 48,100 / 45,192**, and **43,364 / 49,428 / 46,444** bytes,
with zero-runtime maps; MC68000 returns **42** after
**113,793 / 1,190,580**, **149,117 / 1,563,576**, **131,221 / 1,372,866**,
and **206,791 / 2,176,314** instructions/cycles.

Application ShowHelp and AboutMUI now use a named presentation-packet request
struct and central codec. Dispatcher consumers use typed reference-window,
help-file, node, line, and AboutMUI fields with guest-memory access confined to
the codec. Host/source/CIL remains **540/540**. Their MC68000/020/040 focused
artifacts are **42,112 / 48,100 / 45,188** and **44,540 / 50,752 / 47,664**
bytes, with zero-runtime maps; MC68000 returns **42** after
**179,229 / 1,880,818** and **354,512 / 3,722,488** instructions/cycles.

Application PushMethod and UnpushMethod now use a named queue-packet request
struct and central codec. Dispatcher consumers use typed destination, count,
target, selector, and method fields; the inline parameter block address is
derived by a named codec boundary. Host/source/CIL remains **540/540**.
`ApplicationQueueRoot` produces **44,624 / 50,628 / 47,756 bytes** for
MC68000/020/040, returns **42** after **287,955 / 3,023,546**
instructions/cycles, and has zero-runtime maps.

The Application method packet family now uses a named request struct at the
native codec boundary. DefaultConfigItem, CheckRefresh, Execute/Run, Window
setup/cleanup/depth methods, and Window Snapshot decode through typed packet
records, keeping guest-memory access in the codec and preserving the
freestanding 68k call boundary. Host/source/CIL remains **540/540**.
`ApplicationDefaultConfigRoot`, `ApplicationCheckRefreshRoot`,
`ApplicationLoopRoot`, and `WindowSnapshotRoot` produce MC68000/020/040
artifacts of **42,128/48,108/45,204**, **43,392/49,540/46,524**,
**46,472/52,720/49,668**, and **41,768/47,616/44,852** bytes respectively;
all have zero-runtime maps. MC68000 returns **42** after
**130,429/1,364,486**, **286,991/3,013,792**, **176,768/1,863,918**, and
**94,864/996,550** instructions/cycles.

ReturnID, Input/NewInput, InputBuffered, and input-handler packet decoders now
use one named codec. Dispatcher paths consume typed return IDs, signal-storage
pointers, and handler pointers. Their MC68000/020/040 focused artifacts are
**43,348/49,320/46,480**, **44,040/49,988/47,164**, **43,000/48,932/46,120**,
and **44,336/50,440/47,512** bytes; all are zero-runtime clean. MC68000
returns **42** after **149,331/1,570,710**, **133,700/1,404,724**,
**178,040/1,867,702**, and **175,220/1,836,748** instructions/cycles.
Remaining MorphOS Application behavior and ABI coverage remain progressive
work.

The Stringscroll UTF-8 metric seam now follows MorphOS 3.20's codepoint
orientation: valid UTF-8 sequences count as one visual column, CR/LF handling
preserves logical lines, malformed bytes remain visible as one-column fallback
characters, and horizontal drawing starts and ends on codepoint boundaries.
The focused native metric root is zero-runtime clean at 2,224/2,296/2,216
bytes for MC68000/020/040 and returns 42 on MC68000. Full Stringscroll input,
rendering, and MorphOS differential parity remain progressive work.

The MorphOS `String.mui` scroll attributes now reuse the same UTF-8 metric
scanner as `Stringscroll.mui`. The focused record-backed native metric root is
zero-runtime clean at 31,384/35,396/33,268 bytes for MC68000/020/040 and
returns 42 on MC68000. Full String.mui rendering/input parity remains
progressive work.

The fixed Misc specialist/object-aware packet family now uses
`MuiMiscSpecialistMessageCodec` for `OM_GET`, `OM_SET`/`MUIM_NoNotifySet`,
`OM_DISPOSE`, `Panel_Run`, `Title_*`, and `Mccprefs_RegisterGadget`. Both
dispatchers consume named lifecycle, attribute, pointer, pair, and gadget
records; packed guest offsets are confined to the codec. Host coverage is
**555/555**. The focused native boundary is zero-runtime clean at
**3,968/4,036/4,036 bytes** for MC68000/020/040 and returns **42** on
MC68000 after **1,476 instructions / 15,648 cycles**. The existing native
Misc Setup/Cleanup root separately covers the lifecycle dispatch path; full
Misc behavior and MorphOS differential parity remain progressive work.

Application/Window menu and event-handler packet decoders now use a shared
named codec boundary. Dispatcher paths consume typed menu IDs, states, and
handler pointers instead of repeating packet offsets. `ApplicationMenuStateRoot`
is **44,580 / 50,680 / 47,696 bytes** and `WindowEventHandlerRoot` is
**44,484 / 50,608 / 47,672 bytes** for MC68000/020/040; both are zero-runtime
clean. MC68000 returns **42** after **205,142 / 2,154,586** and
**130,973 / 1,373,930** instructions/cycles respectively. Remaining MorphOS
Application/Window behavior and ABI coverage remain progressive work.

Group InitChange, ExitChange, and ExitChange2 packet dispatch now uses one
central guest-memory codec. Fixed packed ABI access is confined to that codec,
while the qualification root verifies valid methods and truncated rejection.
`GroupChangePacketsRoot` is **1,964 / 1,964 / 1,960 bytes** for
MC68000/020/040, returns **42** after **625 instructions / 6,488 cycles**, and
has zero-runtime maps. Remaining MorphOS Group behavior and ABI coverage remain
progressive work.

Fixed Group change and ordering packets now use central named codecs.
MoveMember, Reorder, and Sort qualification seams accept struct-shaped inputs,
and packet consumers no longer repeat field offsets. `GroupOrderingRecordRoot`
is **3,736 / 3,700 / 3,704 bytes** for MC68000/020/040, returns **42** after
**907 instructions / 9,664 cycles**, and has zero-runtime maps. Remaining
MorphOS Group behavior and ABI coverage remain progressive work.

The Group change-bracket sidecar is a named 16-byte state record with one
central codec for depth, exit flags, and exit-request telemetry. The
qualification seam accepts a struct-shaped input and rejects unmapped state
without exposing offsets. `GroupChangeStateRecordRoot` is **1,812 / 1,796 /
1,796 bytes** for MC68000/020/040, returns **42** after **441 instructions /
4,740 cycles**, and has zero-runtime maps. Remaining MorphOS Group behavior
and ABI coverage remain progressive work.

Group forwarding and ChildList sidecar state now use central codecs with
struct-shaped public qualification inputs. The 16-byte forward record
normalizes boolean flags at the boundary, and the 32-byte ChildList record
rejects invalid capacity before publication. `GroupStateRecordRoot` is
**3,584 / 3,628 / 3,628 bytes** for MC68000/020/040, returns **42** after
**1,576 instructions / 17,540 cycles**, and has zero-runtime maps. Remaining
MorphOS Group behavior and ABI coverage remain progressive work.

The Process/Slave specialist sidecar now uses a named 52-byte fixed-width
record and central codec for class, Process state, task token, owned Name,
error/signals, flags, Slave setup/dispatch depth, and notification telemetry.
Production consumers use those named fields rather than repeating offsets;
the qualification seam accepts a struct-shaped input to keep the 68k call
boundary register-safe. `ProcessSpecialistRecordRoot` is **2,720 / 2,872 /
2,872 bytes** for MC68000/020/040, returns **42** after **934 instructions /
11,012 cycles**, and has zero-runtime maps. The broader
`ProcessSpecialistRoot` also returns **42** after **604,551 instructions /
6,347,470 cycles**. Remaining MorphOS Process/Slave behavior and ABI coverage
remain progressive work.

Application/Window queue nodes, input-handler nodes, event-handler nodes, and
SetConfigItem state now use named fixed-width records and central codecs.
Push/return/cycle/event-handler list traversal and cleanup use typed fields;
inline packet placement is retained only as the explicit packet-member ABI
boundary. `ApplicationWindowRecordRoot` is **4,052 / 4,160 / 4,160 bytes** for
MC68000/020/040, returns **42** after **1,097 instructions / 12,522 cycles**,
and has zero-runtime maps. Existing queue and event-handler closures also
return **42** after **287,570 instructions / 3,019,090 cycles** and
**130,890 instructions / 1,372,974 cycles**. Remaining MorphOS Application
behavior and ABI coverage remain progressive work.

The Area/Group layout boundary now writes the six signed 16-bit `MUI_MinMax`
result fields through a named codec, while Area drawing/text render-port lookup
reuses the named 28-byte `MUI_RenderInfo` codec. `AreaLayoutRecordRoot` is
**3,968 / 4,036 / 4,036 bytes** for MC68000/020/040, returns **42** after
**1,072 instructions / 12,190 cycles**, and has zero-runtime maps. Remaining
MorphOS layout behavior and ABI coverage remain progressive work.

Group ChildList projection entries now use a central named 16-byte codec, and
the embedded Exec List header is written through the SDK's typed list boundary.
`NextObject` and projection construction no longer repeat child-entry offsets.
`GroupChildListRoot` is **3,100 / 3,088 / 3,088 bytes** for MC68000/020/040,
returns **42** after **1,117 instructions / 12,018 cycles**, and has
zero-runtime maps. Remaining MorphOS Group behavior and ABI coverage remain
progressive work.

Group grid specifications and ActivePage state now use central named-record
codecs. `GroupGridRecordRoot` is **2,028 / 2,060 / 2,056 bytes** and
`GroupPageRecordRoot` is **1,748 / 1,736 / 1,736 bytes** for MC68000/020/040;
both return **42** with zero-runtime maps. Remaining MorphOS Group behavior
and ABI coverage remain progressive work.

The drawing service uses named fixed-width records and central codecs for its
20-byte state, clip and refresh nodes, pen leases, `MUI_RenderInfo`, and the
RastPort layer view. Clipping, refresh, pen, and layer traversal use typed
fields instead of repeated offsets. `DrawingServiceRecordRoot` is **4,952 /
4,908 / 4,908 bytes** for MC68000/020/040, returns **42** on MC68000 after
**1,301 instructions / 13,920 cycles**, and has zero-runtime maps. The broader
`DrawingServiceRoot` closure also returns **42** after **12,120 instructions /
123,028 cycles**. Remaining MorphOS drawing behavior and ABI coverage remain
progressive work.

The synchronous requester service uses a named 8-byte state struct and central
codec for magic and generation. `MUI_RequestA` and `MUI_RequestObjectA` use
those fields for readiness validation. `RequesterServiceRecordRoot` is
**1,512 / 1,500 / 1,500 bytes** for MC68000/020/040, returns **42** on MC68000
after **258 instructions / 2,628 cycles**, and has zero-runtime maps. Remaining
MorphOS requester behavior and ABI coverage remain progressive work.

The error service uses a named 16-byte state struct and central codec for
magic, version, error value, and sequence. `MUI_Error` and `MUI_SetError` use
those fields instead of repeated record offsets. `ErrorServiceRecordRoot` is
**1,680 / 1,668 / 1,668 bytes** for MC68000/020/040, returns **42** on MC68000
after **342 instructions / 3,664 cycles**, and has zero-runtime maps. Remaining
MorphOS error-service behavior and ABI coverage remain progressive work.

The ASL service uses named fixed-width structs and central codecs for its
12-byte service state and 16-byte requester lease. Allocation, request,
lease-list traversal, linking, and release use typed fields rather than
repeated record offsets. `AslServiceRecordRoot` is **2,684 / 2,664 / 2,664
bytes** for MC68000/020/040, returns **42** on MC68000 after **622
instructions / 6,596 cycles**, and has zero-runtime maps. Remaining MorphOS
ASL behavior and ABI coverage remain progressive work.

The class-service state, lease, and `MUI_CustomClass` blocks are represented by
named fixed-width structs and central codecs. Class-service initialization,
lookup, reference/object lease accounting, custom-class lifecycle, and
unlinking use those fields instead of repeated record offsets.
`ClassServiceRecordRoot` is **4,592 / 4,624 / 4,628 bytes** for
MC68000/020/040, returns **42** on MC68000 after **1,440 instructions / 15,900
cycles**, and has zero-runtime maps. Remaining MorphOS behavioral parity and
ABI coverage remain progressive work.

All headless-state consumers now use the named 32-byte state record for class
and object registry heads, lifecycle teardown, notification depth/sequence,
Group mutation snapshots, specialist class lookup, and class-service
validation. `HeadlessStatePacketRoot` is **2,000 / 2,028 / 2,028 bytes** for
MC68000/020/040, returns **42** on MC68000 after **765 instructions / 8,328
cycles**, and has zero-runtime maps. Remaining MorphOS behavioral parity and
ABI coverage remain progressive work.

The fixed 32-byte notification header is represented by named
`Next`/`Sequence`/`TriggerAttribute`/`TriggerValue`/`Destination`/
`FollowCount`/`Flags`/`Reserved` fields and a central codec. Notify allocation,
traversal, dispatch, cleanup, and trailing payload placement use those fields.
`NotificationRecordRoot` is **2,000 / 2,028 / 2,028 bytes** for MC68000/020/
040, returns **42** on MC68000 after **557 instructions / 6,170 cycles**, and
has zero-runtime maps. Remaining MorphOS behavioral parity and ABI coverage
remain progressive work.

The fixed 24-byte Store/Dataspace record is represented by named
`Next`/`Key`/`Data`/`Length`/`Flags`/`Generation` fields and a central codec.
Store lifecycle operations and Dataspace IFF entry decoding use those fields.
`StoreRecordRoot` is **1,848 / 1,832 / 1,836 bytes** for MC68000/020/040,
returns **42** on MC68000 after **432 instructions / 4,726 cycles**, and has
zero-runtime maps. Remaining MorphOS behavioral parity and ABI coverage remain
progressive work.

The fixed 16-byte Family child-list node is represented by a named
`Next`/`Previous`/`Object`/`Owner` record and central codec. Live Family
topology, `MUIM_Family_DoChildMethods`, selector/mutation projections, and
collection teardown use those fields. `ChildRecordRoot` is **1,684 / 1,672 /
1,676 bytes** for MC68000/020/040, returns **42** on MC68000 after **349
instructions / 3,754 cycles**, and has zero-runtime maps. Remaining MorphOS
behavioral parity and ABI coverage remain progressive work.

The fixed 16-byte attribute node is represented by a named record and codec.
HeadlessObjectCore, Group child/page state, and Stringscroll attribute paths
use those fields. `AttributeRecordRoot` is **1,692 / 1,680 / 1,680 bytes** for
MC68000/020/040, returns **42** on MC68000 after **346 instructions / 3,682
cycles**, and has zero-runtime maps. Remaining non-object ABI records and full
MorphOS differential parity remain progressive work.

The fixed 64-byte headless object record is represented by a named struct and
central codec. Creation, disposal, lookup, object/attribute access, link
updates, list unlinking, and attribute cleanup use those fields. The focused
`HeadlessObjectPacketRoot` is **3,208 / 3,396 / 3,396 bytes** for
MC68000/020/040, returns **42** on MC68000 after **2,358 instructions /
26,470 cycles**, and has zero-runtime maps. Remaining MorphOS behavioral
parity and ABI coverage are still progressive work.

Family topology now consumes the named object codec for parent, child-head,
child-tail, and BOOPSI links across add/remove/get/reorder/transfer and
dispose-time cleanup. `MUIM_Family_DoChildMethods` uses the same typed walk.
`FamilyDoChildMethodsRoot` is **2,044 / 2,112 / 2,036 bytes** for
MC68000/020/040, returns **42** on MC68000 after **966 instructions / 10,024
cycles**, and has zero-runtime maps. Remaining MorphOS behavioral parity and
ABI coverage remain progressive work.

The fixed `MUIM_Family_DoChildMethods` envelope now crosses the named
`MuiFamilyDoChildMethodsMessageCodec`; the live forwarding path consumes the
same central mapping check. `FamilyDoChildMethodsMessageCodecRoot` produces
**1,084 / 1,080 / 1,080 bytes** for MC68000/020/040 with 7 reachable methods,
zero relocations, zero framework members, and zero managed allocation sites;
MC68000 returns **42** after **217 instructions / 2,154 cycles**. The broader
Family forwarding root remains separately qualified at **1,236 instructions /
13,524 cycles**.

The fixed Family AddHead/AddTail/Remove, Insert, and Transfer packets now cross
`MuiFamilyMutationMessageCodec` into named child, predecessor, and family
fields. `FamilyMutationMessageCodecRoot` produces **2,512 / 2,528 / 2,528
bytes** for MC68000/020/040 with 11 reachable methods, zero relocations, zero
framework members, and zero managed allocation sites; MC68000 returns **42**
after **730 instructions / 7,594 cycles**. Reorder and Sort remain covered by
the broader Family packet root, with trailing object vectors treated as the
explicit array ABI boundary.

Group child forwarding/list projection, ActivePage state, and collection
teardown now use the named object codec for BOOPSI, attribute-head, and
child-head links. The focused Group page/forward seams are **1,152 / 1,152
bytes** on MC68000, return **42** after **282 / 3,192** and **282 / 3,176
instructions / cycles**, and have zero-runtime maps. Remaining MorphOS
behavioral parity and ABI coverage remain progressive work.

Notify parent/BOOPSI resolution and notification-list heads, plus semaphore
owner/depth/shared state, now use named fields in the object codec.
`NotifyWritePacketsRoot` is **2,260 bytes** on MC68000; the focused
`SemaphoreObjectRecordRoot` is **2,268 / 2,360 / 2,360 bytes** for
MC68000/020/040, returns **42** on MC68000 after **838 instructions / 9,402
cycles**, and has zero-runtime maps. Remaining MorphOS behavioral parity and
ABI coverage remain progressive work.

Store/Dataspace ownership uses the named object codec for the `Stores` head
across add/resize/merge/find/iterate/remove/clear, objectmap cleanup, and
teardown. `StoreObjectRecordRoot` is **2,272 / 2,316 / 2,316 bytes** for
MC68000/020/040, returns **42** on MC68000 after **800 instructions / 9,070
cycles**, and has zero-runtime maps. Remaining MorphOS behavioral parity and
ABI coverage remain progressive work.

The audited specialist consumers—CommonControl, List, Listtree, menu,
Stringscroll, and MUI lifecycle classification—now use named object fields.
No production source outside the codec boundary retains a
`MuiHeadlessLayout.Object…` read/write. `CommonControlClassRecordRoot` remains
**3,520 / 3,640 / 3,540 bytes** for MC68000/020/040, returns **42** on
MC68000 after **1,638 instructions / 15,244 cycles**, and has zero-runtime
maps. Full MorphOS differential parity remains open.

The headless class registry uses a named 28-byte record for its typed pointers,
instance size, flags, and object count. Registration, lookup, deletion, and
object accounting use the central codec; `HeadlessClassPacketRoot` is
**2,156 / 2,184 / 2,184 bytes** for MC68000/020/040 and returns **42** on
MC68000 with zero-runtime maps.

Common-control class classification and Group superclass traversal also consume
that codec. `CommonControlClassRecordRoot` is **3,520 / 3,640 / 3,540 bytes**
for MC68000/020/040 and returns **42** on MC68000 with zero-runtime maps.

The audited List, Listview, Listtree, menu, misc, process, and object-persistence
class probes use the same codec; those sources no longer duplicate the class
registry field offsets.

The shared headless guest-state header is represented by a named 32-byte
record. Initialization, Ensure, sequence allocation, and mutation tracking use
the central codec; `HeadlessStatePacketRoot` is **2,000 / 2,028 / 2,028 bytes**
for MC68000/020/040 and returns **42** on MC68000 with zero-runtime maps. The
dataspace iteration cursor remains an opaque scalar because it is not a public
record.

The latest MG09 collection slice keeps Listview and Stringscroll surface
dispatch struct-first: named records cover layout, draw, min/max,
set/no-notify-set, and Floattext append, with Listview list methods forwarding
through the same decoded records. Host/source/CIL coverage is **540/540**.
`CollectionCompositePacketsRoot` is **2,488 / 2,516 / 2,524 bytes** for
MC68000/020/040, returns **42** after **593 instructions / 6,302 cycles** on
MC68000, and has zero relocations and zero-runtime map fields.

Application settings Save/Load now uses named fixed-width header and key/length
records, and the persistence boundary consumes a named dataspace store-record
view. `ApplicationSettingsPacketRoot` is **2,444 / 2,432 / 2,428 bytes** for
MC68000/020/040, returns **42** after **622 instructions / 6,362 cycles** on
MC68000, and remains zero-runtime clean. The iteration cursor is intentionally
kept as an opaque scalar because it is not a public record.

The `MUI_Layout` dispatcher now uses named records for AskMinMax, Relayout,
DrawBackground, Backfill, and Text; existing Layout, Draw, Setup, and
text-dimension packets remain typed as well. `LayoutSurfacePacketsRoot` and
`LayoutTextPacketRoot` are **1,792/1,828/1,848** and **1,096/1,104/1,116**
bytes for MC68000/020/040 and return **42** on MC68000 with zero-runtime maps.

The common-control dispatcher now decodes payload-bearing numeric, prop, event,
attribute, geometry, draw, and setup packets into named structs. Numeric
default and Cleanup are zero-payload method-ID operations. Focused packet roots
cover these groups with MC68000/020/040 artifacts of **1,584/1,584/1,592**,
**1,556/1,560/1,560**, **940/932/936**, **1,032/1,020/1,024**, and
**1,148/1,140/1,140** bytes, all zero-runtime clean and returning **42** on
MC68000.

The production project is `CopperOS.MuiMaster.csproj`. MG02 foundations include
value-type platform capability contracts, fixed-width guest-resident state,
CopperOS development resident metadata, and the exact public vector router.
MG03 supplies the shared CopperStart Intuition BOOPSI runtime consumed through
`IMuiBoopsiCapability`; CopperOS does not maintain a second object system. MG04
qualifies the headless master lifecycle, class/object ownership, exact-message
dispatcher, Family, Notify, Dataspace, Datamap, Objectmap, and Semaphore core.
MG05 qualifies Area/Group geometry, Balance, Register, Selectgroup, Scrollgroup,
Virtgroup, redraw scheduling, and neutral rendering through explicit Graphics/
Layers capabilities. MG06 qualifies scheduler-driven Application/Window
ownership, signals, pushed methods, handlers, event polling, focus, menus,
iconification, and requesters. MG07 common controls are now active. Host/static/
CIL gates live in
`tests/MuiMaster`; the self-contained CopperSharp input closure is
`tests/MuiMaster.NativeRoot`, and `tests/MuiMaster.NativeExecution` executes its
MC68000 HUNK under Copper68k. Run the MC68000/020/040 compile/map gates and the
MC68000 return-value smoke test with:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\MuiMaster\qualify_native.ps1
```

The latest MG09 Datamap/Objectmap packet slice replaces raw dispatcher field
reads with named fixed-width records for Set, Find, Get, Iterate,
IterationKey, Remove, and Clear. Host/source/CIL coverage is **540/540**.
`StorePacketsRoot` is **5,620 / 5,636 / 5,628 bytes** for MC68000/020/040,
returns **42** after **4,152 instructions / 43,608 cycles** on MC68000, and
has zero relocations and zero-runtime map fields. Full MorphOS differential
parity remains separate progressive work.

The latest MG09 Group change packet slice replaces the remaining raw header
reads for `MUIM_Group_InitChange`, `MUIM_Group_ExitChange`, and
`MUIM_Group_ExitChange2` with named 4-byte/8-byte records. The live path keeps
the existing nested bracket state in guest memory. Host/source/CIL coverage is
**540/540**; `GroupChangePacketsRoot` is **2,140 / 2,132 / 2,128 bytes** for
MC68000/020/040 and returns **42** after **792 instructions / 7,962 cycles** on
MC68000, with zero relocations and zero-runtime map fields.

The latest MG09 GetConfigItem slice uses a named 12-byte
`{MethodID, ConfigId, Storage}` packet for both broad and focused dispatch.
Host/source/CIL coverage is **540/540**. `GetConfigItemPacketsRoot` is
**1,384 / 1,376 / 1,376 bytes** for MC68000/020/040, returns **42** after
**410 instructions / 4,206 cycles** on MC68000, and has zero relocations and
zero-runtime map fields.

The latest MG09 persistence packet slice uses a named 8-byte
`{MethodID, Dataspace}` record for both `MUIM_Export` and `MUIM_Import`.
Host/source/CIL coverage is **540/540**. `ObjectPersistencePacketsRoot` is
**1,792 / 1,780 / 1,780 bytes** for MC68000/020/040, returns **42** after
**705 instructions / 7,286 cycles** on MC68000, and has zero relocations and
zero-runtime map fields.

The fixed Export/Import envelope is also qualified independently through the
named `MuiObjectPersistenceMessage` and central
`MuiObjectPersistenceMessageCodec`. `ObjectPersistenceMessageCodecRoot`
produces **2,328 / 2,316 / 2,316 bytes** for MC68000/020/040, with 13
reachable methods, zero relocations, zero framework members, and zero managed
allocation sites; MC68000 execution returns **42** after **757 instructions /
7,776 cycles**. The root exercises both methods and a truncated-record
rejection. Class-specific persistence payload semantics remain progressive.

The latest MG09 packet slice adds `MUIM_UpdateConfig` as a complete
struct-first 332-byte record: named header, 64 redraw-object pointers, and 64
redraw flags. The codec validates the bounded count and packet mapping; live
preference propagation and redraw scheduling remain separate capability work.

The latest MG09 Family packet slice adds named 8-byte records for
`MUIM_Family_AddHead` (`0x8042E200`), `MUIM_Family_AddTail` (`0x8042D752`),
and `MUIM_Family_Remove` (`0x8042F8A9`), plus a named 12-byte record for
`MUIM_Family_Insert` (`0x80424D34`). These methods route through the live
Family ownership path. `FamilyChildPacketsRoot` qualifies the corresponding
guest head/tail/remove/insert/transfer/reorder/sort projection at **9,364 /
9,988 / 9,232 bytes** for MC68000/020/040; MC68000 returns **42** after
**13,832 instructions / 132,142 cycles**, with zero-runtime map fields.

The Family packet increment adds `MUIM_Family_DoChildMethods` as a named
4-byte record. Its live path forwards the same message to every direct child
through `IMuiBoopsiCapability`, snapshots the next link before each call, and
uses a bounded guest-resident traversal with no managed collection.

The Notify callback increment adds `MUIM_CallHook` as a named 12-byte packet.
Its fixed fields are decoded without exceptions or managed allocation, and the
existing callback capability receives A0=hook, A2=object, and A1=&param1 so
additional guest parameters remain caller-owned.

The latest MG09 Notify increment adds typed `MUIM_SetAsString` support. Its
16-byte guest header and trailing ULONG vector are decoded centrally; the live
path accepts at most eight arguments, uses the freestanding bounded formatter,
caps output at 1,024 characters, and stores an owned guest copy before setting
the requested attribute. Host/source/CIL coverage is **532/532**. The focused
`SetAsStringPacketsRoot` artifacts are **1,420 / 1,412 / 1,412 bytes** for
MC68000/020/040, with zero relocations and zero-runtime map fields; MC68000
returns **42** after **361 instructions / 3,766 cycles**. Unsupported
conversions remain separate progressive work.

The latest MG09 BOOPSI increment adds the complete typed `MUIM_BoopsiQuery`
(`0x80427157`) packet for the SDK `MUIP_BoopsiQuery` alias. Its named 40-byte
record carries screen, flags, min/max dimensions, default dimensions, and
render-info fields. Host/source/CIL coverage is **533/533**. The focused
`BoopsiQueryPacketsRoot` artifacts are **1,756 / 1,804 / 1,804 bytes** for
MC68000/020/040, with zero relocations and zero-runtime map fields; MC68000
returns **42** after **851 instructions / 9,040 cycles**. The fixed ABI record
is qualified; external BOOPSI callback semantics remain separate progressive
work.

The latest MG09 Notify increment adds named packed records for
`MUIM_WriteLong` and `MUIM_WriteString`. The live dispatcher validates mapped
guest destinations and performs bounded ULONG/string writes without
exceptions, managed allocation, or managed runtime services. Host/source/CIL
coverage is **531/531**. The focused `NotifyWritePacketsRoot` artifacts are
**2,260 / 2,248 / 2,248 bytes** for MC68000/020/040, with zero relocations and
zero-runtime map fields; MC68000 returns **42** after **821 instructions /
8,590 cycles**. The fixed `WriteLong`/`WriteString` packet boundary is
qualified; unsupported Notify formatting remains separate progressive work.

The latest MG09 Dataspace increment replaces raw dispatcher offsets with
named packed guest records for `Add`, `Find`, `Get`, `Merge`, `Remove`, and
`Clear`. `MuiDataspaceMessageCore` owns the codecs and packet writers; the
focused `DataspacePacketsRoot` covers the six forms and truncated-packet
rejection. Host/source/CIL coverage is **529/529**. MC68000/020/040 artifacts
are **4,132 / 4,148 / 4,152 bytes**, with zero relocations, framework features,
managed allocation sites, and runtime type descriptors; MC68000 returns **42**
after **2,075 instructions / 21,966 cycles**.

The latest MG09 Dataspace IFF increment adds typed `ReadIFF`/`WriteIFF`
packets and the separate `IMuiIffCapability` chunk-I/O seam. The live bridge
serializes big-endian `{id,length,data}` records, retries short transfers, and
explicitly frees temporary guest buffers; it never substitutes a DOS handle or
managed stream. Host/source/CIL is **530/530**. The focused
`DataspaceIffPacketsRoot` artifacts are **2,268 / 2,256 / 2,256 bytes** for
MC68000/020/040, with zero relocations and zero-runtime fields; MC68000
returns **42** after **842 instructions / 8,680 cycles**. See the
[MorphOS MUI Dataspace documentation](https://morphos-team.net/sdk/MUI/MUI_Dataspace.html).

The latest MG09 Family increment adds the typed `MUIM_Family_GetChild`
(`0x8042c556`) packet with named `MethodID`, `nr`, and `ref` fields. The live
dispatcher supports First, Last, Next, Previous, and Iterate selectors over
the Family topology; the focused native seam uses named child projection
records. Host/source/CIL coverage is **528/528**. MC68000/020/040 artifacts
are **2,940 / 3,124 / 2,920 bytes**, with zero relocations, framework
features, managed allocation sites, and runtime type descriptors; MC68000
returns **42** after **3,052 instructions / 31,874 cycles**. The MorphOS
Family page marks this method undocumented, so complete differential parity
remains open. See the
[MorphOS MUI Family documentation](https://morphos-team.net/sdk/MUI/MUI_Family.html).

The fixed `MUIM_Family_GetChild` record now crosses the central
`MuiFamilyGetChildMessageCodec`; consumers use named `MethodId`, `Number`, and
`Reference` fields while packed offsets remain isolated to the codec.
`FamilyGetChildMessageCodecRoot` produces **1,356 / 1,372 / 1,372 bytes** for
MC68000/020/040, with 7 reachable methods, zero relocations, zero framework
members, and zero managed allocation sites; MC68000 returns **42** after
**357 instructions / 3,812 cycles**. The focused root also rejects a
truncated packet. Selector/topology behavior remains covered by the existing
Family root.

MG08 is qualified. The current List slices include bounded guest-resident storage,
native-safe StringArray construct/display/compare/destruct behavior,
MorphOS Active/First/Quiet normalization, Select-All/Ask counting, a bounded
Format/MaxColumns column model whose non-`px` width limits use MorphOS
percentage semantics, guest-resident integer `{offset,width}` column geometry,
bounded `MUIM_List_TestPos` row/column/flag/offset hit-testing, and
bounded `MUIM_List_CreateImage`/`MUIM_List_DeleteImage` opaque guest-handle
ownership, plus non-recursive List/Listview/Floattext row geometry publication
and bounded per-cell visible-row rendering through the existing
render-info/graphics seam; this is not full visual parity. Routing is
non-recursive through the layout dispatcher. Dirlist and Volumelist now add bounded
capability-backed scans, owned records, filters, counters, sorting, failure
handling, and reread/mutator dispatch. Stringscroll now has a bounded
guest-owned string, content-driven bar visibility, min/max metrics, clamped
pixel scrolling, input policy, CRLF-safe clipped drawing, and packet routing.
Listview now publishes the owned List viewport, clamps Prop-like first-row
movement, binds the owned child to the shared render-info seam, draws visible
child rows, and draws a neutral overflow track/thumb through the existing
graphics seams.
Listtree.mcc is implemented as a standalone external component with fixed
guest-resident topology and native closure qualification; it is deliberately
not classified as a built-in `.mui` collection.
The fixed MorphOS 3.20 List edit family (`CreateEditObject`, `Edit`, `EditDone`,
and `EndEdit`) now crosses `MuiCollectionEditMessageCodec`. Production
consumers use named signed row/column and guest-pointer fields, while the
codec owns the packed guest boundary. `CollectionListEditMessageCodecRoot`
produces **3,164 / 3,196 / 3,212 bytes** for MC68000/020/040 with 13 reachable
methods, zero relocations, zero framework members, zero managed allocation
sites, and zero runtime type descriptors; MC68000 returns **42** after
**1,059 instructions / 11,088 cycles**. The existing guest-resident editor
state machine remains separately covered, and truncated `EditDone` packets are
rejected.
The fixed List Construct/Destruct, Display, Compare, and TestPos records now
use the same struct-first approach through `MuiCollectionRecordMessageCodec`.
`CollectionListRecordMessageCodecRoot` produces **3,380 / 3,428 / 3,436 bytes**
for MC68000/020/040 with 13 reachable methods and zero-runtime maps; MC68000
returns **42** after **1,340 instructions / 13,978 cycles**. Truncated TestPos
records are rejected, while variable hook payloads remain an explicit ABI
boundary.
The latest MG09 increment adds a typed CopperStart Intuition
`BoopsiNextObjectProjectionEntry` record/codec shared with the Group
ChildList projection. The `amiga.intuition.NextObject` export and vector
router now consume `GENT` entries using the caller-owned cursor; ordinary
BOOPSI objects use the typed `_Object` header codec. CopperStart Intuition
tests are **21/21**, and its freestanding native export closure passes on
MC68000/020/040 (**3/3**). Manifest admission and runtime execution
qualification remain open.
The latest MG09 Group slice adds the typed `MUIA_Group_LayoutHook` bridge.
MinMax and Layout callbacks receive the SDK `MUI_LayoutMsg` struct in guest
memory, including the read-only ChildList pointer and Group context; successful
Layout hooks can adjust virtual-group dimensions and bypass built-in child
layout. Host/source/CIL coverage is **527/527**. `GroupLayoutHookRoot` is
zero-runtime clean on MC68000/020/040 (**6,276 / 6,700 / 6,424 bytes**) and
returns **42** after **5,886 instructions / 59,976 cycles** on MC68000. This
qualifies the typed/native bridge seam; complete MorphOS callback/differential
parity remains open. See the
[MorphOS MUI Group documentation](https://morphos-team.net/sdk/MUI/MUI_Group.html).
The latest MG09 Group slice adds a typed, read-only `MUIA_Group_ChildList`
projection: an `Amiga.List` header and bounded child-entry records are rebuilt
from the Family topology after mutation and released with the Group. The local
`MuiGroupChildrenCore.NextObject` seam advances a guest cursor and returns
child BOOPSI pointers without managed enumeration. Host/source/CIL coverage is
**526/526**; `GroupChildListRoot` is zero-runtime clean on MC68000/020/040
(1,980 / 1,972 / 1,972 bytes), returning **42** after **736 instructions /
7,750 cycles** on MC68000. The CopperStart bridge is compile-closed; manifest
admission and runtime execution remain follow-up ABI tasks, so external
NextObject compatibility is not yet claimed complete. See the
[MorphOS MUI Group documentation](https://morphos-team.net/sdk/MUI/MUI_Group.html)
and [intuition `NextObject` documentation](https://morphos-team.net/sdk/intuition.html).
The preceding MG09 Group slice adds typed `MuiGroupForwardState` handling for
MorphOS `MUIA_Group_Child`, `MUIA_Group_ChildCount`, and
`MUIA_Group_Forward`. Child construction tags adopt non-null objects through
the Family seam and NULL tags fail atomically. Forward routes subsequent
attribute sets to direct children; `ForwardDepth` enables bounded descendant
propagation. Host/source/CIL coverage is **524/524**. The focused
`GroupForwardRecordRoot` is zero-runtime clean on MC68000/020/040
(1,136 / 1,124 / 1,128 bytes), returning **42** after **278 instructions /
3,128 cycles** on MC68000. The typed `LayoutHook` bridge is now present;
complete callback differential qualification remains open. See the
[MorphOS MUI Group documentation](https://morphos-team.net/sdk/MUI/MUI_Group.html).

The preceding MG09 Group slice adds typed `MuiGroupPageState` handling for
MorphOS `MUIA_Group_ActivePage`. First/Last/Prev/Next/Advance selectors are
normalized to a canonical child index, page-mode layout consumes that index,
invalid direct values are rejected, and empty Groups retain a raw selector
until children exist. Host/source/CIL coverage is **520/520**. The focused
`GroupPageRecordRoot` is zero-runtime clean on MC68000/020/040
(1,136 / 1,120 / 1,132 bytes), returning **42** after **278 instructions /
3,144 cycles** on MC68000. See the
[MorphOS MUI Group documentation](https://morphos-team.net/sdk/MUI/MUI_Group.html).

The preceding MG09 Group slice adds typed `MuiGroupGridSpec` handling for
MorphOS `Columns`, `Rows`, horizontal/vertical spacing, same-size, and
center-alignment attributes. Row-only and column-only configurations derive
the missing axis; bounded cells reuse Area min/max records and the existing
layout path without managed collections. Host/source/CIL coverage is
**518/518**, and `GroupGridRecordRoot` is zero-runtime clean on MC68000/020/040
(1,164 / 1,168 / 1,176 bytes), returning **42** after **416 instructions /
4,640 cycles** on MC68000. See the
[MorphOS MUI Group documentation](https://morphos-team.net/sdk/MUI/MUI_Group.html).

The current MG09 window packet slices add `MUIA_Window_ActiveObject` None/
Next/Prev and geometry-aware Left/Right/Up/Down selection, obsolete
`MUIM_Window_SetCycleChain`,
`MUIM_Window_Snapshot`, and `MUIM_Window_ScreenToBack`/
`MUIM_Window_ScreenToFront`. Screen-depth operations require an opened window
and use the explicit `MoveMuiScreen` capability. Spatial selection is bounded
to the copied cycle chain and Area edge attributes; its deterministic ranking
is an explicit public-ABI policy because private MorphOS tie-breaking is not
documented. Exact MorphOS settings-file encoding remains intentionally pending.
The Notify-class `MUIM_GetConfigItem` packet now supports the documented
`MUICFG_PublicScreen` item (`0x24`). It validates live-object and caller-owned
storage boundaries before publishing the opaque screen value through the
native-safe `GetMuiConfigItem` capability; unsupported items fail without a
write or capability call.
The remaining public Notify UserData methods are also routed:
`MUIM_FindUData` returns the first matching object in preorder,
`MUIM_GetUData` reads the requested attribute from that object,
`MUIM_SetUData` updates every matching object, and `MUIM_SetUDataOnce` stops
after the first. Their fixed packet records and traversal frames remain
guest-safe and allocation-free from the managed-runtime perspective.
The first MG09 service slice now provides the MorphOS-shaped `MUI_Layout`
scalar and guest-packet entry points. It routes existing native-safe collection
and Radio layout cores, publishes bounded Area geometry, and now reaches the
native-safe three-child Scrollbar composite layout. The custom/external class
gateway also has a native-safe builtin/custom lifecycle seam covering
`GetClass`/`FreeClass`, public/private custom classes, A6 binding, and deletion
guards. The loader-backed external path is now separately native-qualified for
the deterministic `mui/Foo.mcc` fixture; the broader third-party class
inventory remains open. These are progressive slices. The requester/ASL service
now adds a guest-resident lease boundary for
`AllocAslRequest`/`AslRequest`/`FreeAslRequest`, and the synchronous requester
service implements `RequestA`/`RequestObjectA` with balanced object retention
around the modal call. The ASL boundary now validates guest TagItem control
semantics (`TAG_DONE`, `TAG_IGNORE`, `TAG_MORE`, and `TAG_SKIP`) without
copying the caller's list. Synchronous requester calls now validate bounded,
caller-owned title, gadget, and format strings, measure `|`-separated gadget
alternatives, count printf-style conversions, and verify the mapped ULONG
parameter vector. The bounded requester formatter now executes integer,
string, character, binary/hexadecimal, width/precision, and literal-percent
conversions into a temporary guest C string before the synchronous platform
call; unsupported conversions remain an explicit failure. Host-native requester
UI remains deferred. The drawing service also provides strict-LIFO clipping and
clip-region stacks, balanced refresh flags, and full-token pen acquisition/
release through explicit region/pen capabilities. Public `MUI_Redraw` now validates the
guest object registry and draw intent bits before entering the native redraw
seam. Public `MUI_NewObjectA` now performs case-sensitive builtin-class lookup,
bounded TagItem validation, and guest object construction. The class-service
factory path additionally acquires external classes through the bounded
`mui/<classid>` loader seam and holds that lease in guest state until
`DisposeObjectWithClassService` removes the object. The public disposal
boundary now rejects unknown or already-disposed objects and routes the
class-service form through the same lease-aware release. The public error service now
provides guest-resident
`MUI_Error`/`MUI_SetError` state with previous-value returns and no managed
runtime state. Public IDCMP routing now retains requested event masks across
window open/close and updates the native event configuration while open. The
overall MorphOS compatibility claim is still withheld.

The bounded public `MUI_MakeObjectA` route now constructs the documented object
families (`MUIO_Label`, `MUIO_Button`, `MUIO_Checkmark`, `MUIO_Cycle`,
`MUIO_Radio`, `MUIO_Slider`, `MUIO_String`, `MUIO_PopButton`,
`MUIO_NumericButton`, spacing, bars, and bar titles) plus the MorphOS-shaped
`MUIO_MenustripNM` and `MUIO_Menuitem` families from type-specific ULONG
vectors. Menu-family specialist state is attached during construction, so menu
methods are available immediately and generic disposal releases sidecars
recursively. Cycle and Radio vectors are validated bounded guest pointer tables,
PopButton image specs accept builtin IDs or bounded C strings, and NumericButton
validates and copies its optional format string through the numeric ownership
path. The menu route parses bounded guest `NewMenu` records into owned
`Menustrip.mui`/`Menu.mui`/`Menuitem.mui` trees with title, item, sub-item,
separator, shortcut, userdata, mutual-exclusion, CheckIt/Checked/MenuToggle,
command-string, enabled, and menu-disabled behavior. All temporary class/tag
records are guest-resident and disposed before returning; menu trees are
recursively disposed. `MUIO_Menuitem_CopyStrings` now enables failure-atomic
copies of the direct item's title and shortcut; image menu entries and other
not-yet-qualified MUIO constructors remain explicitly unsupported.

The public `MUI_NewObjectA` direct and class-service factories now invoke the
class-aware common-control construction normalization after raw TagItems and
before specialist adoption. Numeric, String, Image, Prop, Gauge, and related
families therefore receive their bounded defaults, clamping, and guest-owned
payload copies through the same factory boundary; unknown/custom classes remain
on the generic path.

MorphOS 3.20 `String.mui` also exposes a bounded scroll-metric slice: pixel
`ScrollWidth`/`ScrollHeight`, laid-out visible dimensions, and clamped
`ScrollLeft`/`ScrollTop` offsets are available through common-control getters
and `OM_GET`/`OM_SET` packets. The first implementation uses a deterministic
8x10 character-cell metric and does not claim full font, UTF-8, multiline, or
Prop-binding parity.

The MG09 menu classes `Menustrip.mui`, `Menu.mui` and `Menuitem.mui` (all
`Family.mui` subclasses) now have an additive specialist family
(`MenuSpecialistCore` / `MenuSpecialistDispatcher` / `MenuSpecialistLifecycle`).
It is layered over real headless objects, and the public headless dispatcher
gives it one non-recursive first-refusal seam, so its owned parent/child hierarchy is
delegated to the frozen `MuiFamilyCore` and its scalar attributes and runtime
notifications flow through the frozen object path; a small per-object
guest-resident sidecar (linked through one private attribute id) carries the
menu-specific state. It implements every official attribute and method with its
exact id and I/S/G policy: Menustrip `CaseSensitive`/`Enabled` plus
`InitChange`/`ExitChange` change-nesting with underflow protection and
`WillOpen`/`Popup` gating; Menu `Title`/`Enabled`/`CopyStrings` with title
ownership governed by `CopyStrings`; Menuitem `Title`/`Shortcut`/`Checkit`/
`Checked`/`Toggle`/`Exclude`/`Enabled`/`CommandString`/`CopyStrings`/`Menuitem`/
`Trigger` with failure-atomic copied strings, mutual exclusion/toggle across
siblings, disabled gating, trigger publication, the one-level Menuitem nesting
rule and recursive class-owned disposal. It does not alter the frozen generic
cores or platform aggregates and needs no native-menu capability; unclaimed
packets retain the existing generic route. The
family classifies and adopts objects emitted by `MUI_MakeObjectA`, direct
`MUI_NewObjectA`, and the class-service object factory; those objects carry
their sidecars from construction onward. The slice is qualified by
`tests/MuiMaster/MuiMenuSpecialistTests.cs` and the
`MenuSpecialistRoot` and integrated `MenuDispatcherRoot` MC68000/020/040
freestanding closures (executed under Copper68k).

The Process/Slave family now has the same additive factory/lifecycle treatment.
`Process.mui` and `Slave.mui` objects created by the direct or class-service
factory receive guest-resident sidecars, and the service-capable public
dispatcher routes their specialist attributes, Process launch/process/kill/
signal methods, Slave setup/dispatch/error/signal methods, inherited Semaphore
verbs, and disposal. Scheduler behavior is an explicit `IMuiProcessCapability`
seam; the implementation creates no managed task, thread, exception, or host
runtime state. The focused `ProcessDispatcherRoot` closure is qualified on
MC68000/020/040, while the broader Process/Slave contract remains part of the
active MG09 progression.

The ten-class Misc family now also supports headless-object factory adoption.
`Keyadjust.mui`, `Panel.mui`, `Filepanel.mui`, `Fontdisplay.mui`, private
`Scrmodelist.mui`, `Argstring.mui`, `Aboutmui.mui`, `Mccprefs.mui`,
`FSProtectionBits.mui`, and `Title.mui` receive a separate guest-resident
instance sidecar after OM_NEW/tag application. Object-aware disposal releases
the sidecar's copied strings, ASL state, hook scratch, adopted rows, and other
owned blocks before the frozen object record. Standalone Misc instances retain
the family-neutral service route. The additive object dispatcher now claims
`OM_GET`, `MUIM_Set`/`MUIM_NoNotifySet`, `MUIM_Cleanup`, the Title page
methods (`MUIM_Title_New`, `MUIM_Title_FindPage`, `MUIM_Title_Close`), and
the validated `MUIM_Panel_Run` boundary, `MUIM_Filepanel_AddRow`,
`MUIM_Mccprefs_RegisterGadget`, `MUIM_Mccprefs_ConfigToGadgets`,
`MUIM_Mccprefs_GadgetsToConfig`, and `OM_DISPOSE` for adopted objects; other
packets remain unclaimed for later outer dispatch layers.

The Application dispatcher also has a bounded MorphOS
`MUIM_Application_AboutMUI` packet boundary. Its `refwindow` argument is
validated as a live MUI object (or Null), passed through the explicit
`ShowMuiAbout` platform capability, and recorded in guest-resident request
telemetry. This does not claim the rest of the Application method inventory
or a pixel-level About window implementation.

`MUIM_Application_CheckRefresh` is implemented as a bounded child-window
walk. Only application children with a live native window handle reach the
explicit `RefreshMuiWindow` capability; the check and refresh counts remain
guest-resident, and dead applications are rejected.

The Application menu compatibility family is implemented as well. The
obsolete-but-ABI-visible `GetMenuCheck`/`GetMenuState` packets use first-match
semantics across open child windows, while `SetMenuCheck`/`SetMenuState` visit
every open child. Closed windows and invalid application objects are skipped
or rejected without managed state.

The Application queue family is implemented with MorphOS semantics. `PushMethod`
copies a bounded packet (up to seven arguments) and returns its queue identifier;
`UnpushMethod` matches target object, queue identifier, and destination method
independently, with zero values acting as wildcards. Queue records stay in guest
memory and use explicit allocation/free seams; no exception or managed runtime
path is involved.

The Application `ShowHelp` packet is also bounded and native-safe. Null uses
the default public screen, `(Object *)-1` resolves to the first open child
window, and ordinary window references must be live MUI objects. Optional help
file/node strings are validated as bounded guest C strings before the explicit
platform presentation seam is called.

`MUIM_Application_DefaultConfigItem` is implemented as an application override
hook. It validates the live application, forwards the configuration identifier
through an explicit value-type capability, and records the accepted result in
guest state without introducing a managed configuration store.

The ABI-visible MorphOS V11 `MUIM_Application_SetConfigItem` packet is also
decoded as the named `{MethodID,item,data}` frame. Its private PSI payload is
opaque by design: the item/data pair and request count live in a typed
guest-resident record, Null data is accepted, non-null data is mapped before
acceptance, and teardown releases the record. No preferences format or
managed configuration store is invented at this boundary.

The documented Group change bracket is routed as exact typed packets for
`MUIM_Group_InitChange`, `MUIM_Group_ExitChange`, and
`MUIM_Group_ExitChange2`. Group subclasses are recognized through the
registered class chain. A guest-resident `MuiGroupChangeState` record carries
bounded nesting depth, ExitChange2 flags, and exit telemetry; malformed
underflow is rejected and object disposal frees the record. This is a
progressive compatibility slice, not a claim of the complete Group method
inventory or visual layout behavior.

The Group ordering packet family is also routed for
`MUIM_Group_MoveMember`, `MUIM_Group_Reorder`, and `MUIM_Group_Sort`. These
packets use named fixed-layout records, validate Group inheritance, and reuse
the bounded guest child topology for position-based moves and NULL-terminated
reorder/sort vectors. The implementation covers the documented ordering
contract; it does not claim private layout-hook or pixel-level behavior.

`MUIM_Application_OpenConfigWindow` is implemented as a bounded, non-blocking
configuration-window request. The `{flags,classid}` packet preserves the raw
flags word (MorphOS currently defines none), accepts a Null class id or a
bounded guest C string, and delegates presentation through an explicit
`OpenMuiConfigWindow` capability. The request and last arguments remain in
guest-resident telemetry; no managed preferences store or UI runtime is
introduced.

`MUIM_Application_BuildSettingsPanel` is implemented as an application
override hook. The `{number}` packet asks an explicit capability for an
optional settings-panel MUI object; non-null results must be live guest
objects, while Null is a valid “no panel” result. The selected number, result,
and request count are retained in guest state without managed UI ownership.

`MUIM_Application_Save` and `MUIM_Application_Load` now validate the paired
`{name}` packets, including the MorphOS ENV (`NULL`) and ENVARC (`(STRPTR)-1`)
selectors and bounded guest C-string paths. They delegate the actual object
graph import/export through explicit save/load capabilities and record the
last operation in guest state. The generic Notify `MUIM_Export` and
`MUIM_Import` packets validate their live `{obj,dataspace}` pair and non-zero
`MUIA_ObjectID`. The nine documented built-in forms are implemented: String
and Text use bounded NUL-terminated guest blobs copied into their existing
owned buffers, while Numeric, Radio, Cycle, List, Area selection, Menuitem,
and Group use ULONG payloads keyed by that ID. Unsupported custom payloads use
the explicit native capability. `MuiApplicationPersistenceCore` now walks the
live application Family tree in preorder using a bounded guest-resident frame
stack and reuses this Dataspace transport, while suppressing zero-ID objects.
`MuiApplicationSettingsFileCore` now connects that walk to a bounded
CopperOS-internal file format through the native-safe `IMuiDosCapability`,
including short-transfer handling and explicit scratch/Dataspace cleanup.
`ImportTransactional` clears and snapshots the live tree into a second
Dataspace, rejects Dataspace aliasing, and compensates a failing import while
preserving the failure result. Exact
MorphOS on-disk compatibility, selector path resolution, and custom-class
persistence remain open.

`MUIM_Window_Snapshot` is implemented with the MorphOS flags (`0` unsnapshot,
`1` snapshot), a required non-zero `MUIA_Window_ID`, and an explicit
`SnapshotMuiWindow` capability. The capability owns the actual settings store;
the packet slice does not claim private MorphOS on-disk encoding parity.

The obsolete-but-ABI-visible `MUIM_Window_SetCycleChain` packet validates its
inline Null-terminated MUI object vector and copies it into guest-resident
nodes. Invalid replacements preserve the previous chain, and object cleanup
releases the copied nodes without managed state.

`MUIA_Window_ActiveObject` `MUIM_Set` selectors now cover MorphOS None, Next,
Prev, Left, Right, Up, and Down values. Next, Prev, and direct object
activation require membership in the copied cycle chain; spatial navigation
uses its published Area geometry and retains focus when no candidate exists.

`MUIM_Application_Execute` and `MUIM_Application_Run` now drive the shared
guest-resident application loop through Input, pushed methods, input handlers,
and window events until `MUIV_Application_ReturnID_Quit`. Signal waiting is an
explicit non-consuming platform seam, and the native closure has a bounded
guard against malformed non-terminating guests.

Standalone MG09 records now use additive service seams. `DispatchStandaloneService`
routes the Pop*, pen/color, and Misc specialist dispatchers by their
guest-resident magic/layout, while `DispatchExternalService` routes the
Boopsi/Dtpic wrapper. Unknown instances remain unclaimed. Keeping these routers
separate from the headless Process/Slave seam preserves small zero-relocation
freestanding closures; the dedicated service roots qualify MC68000/020/040
output without managed allocations, exceptions, or runtime services.

The eventual built runtime library is staged as:

`filesystem/SYS/Libs/muimaster.library`

The fixed MorphOS 3.20 List advanced method envelopes (Insert/InsertSingle,
Remove, NextSelected, SortEntries, Move, Exchange, Jump, Redraw, CreateImage,
DeleteImage, and FloattextAppend) use named records at the dispatcher boundary.
`MuiCollectionAdvancedMessageCodec` is the only owner of their packed guest
offsets; variable hook/display payloads remain explicit ABI boundaries. Its
focused native root is zero-runtime clean at 4,008/4,036/4,040 bytes for
MC68000/020/040 and returns 42 on MC68000. This is a packet-family slice, not
completion of List or MorphOS differential compatibility.

The remaining fixed List basic envelopes (`GetEntry`, `Select`, `Clear`, and
`Sort`) follow the same struct-first boundary through
`MuiCollectionBasicMessageCodec`. The focused native root is zero-runtime
clean at 2,600/2,612/2,616 bytes for MC68000/020/040 and returns 42 on
MC68000. The broader List lifecycle and full MorphOS differential behavior
remain progressive work.

Collection surface packets (`Layout`, `AskMinMax`, `Draw`, and
`Set`/`NoNotifySet`) now use `MuiCollectionSurfaceMessageCodec`, keeping named
geometry, storage, flags, and attribute records at the dispatcher boundary.
The focused native root is zero-runtime clean at 3,348/3,388/3,396 bytes for
MC68000/020/040 and returns 42 on MC68000.

The standalone Boopsi/Dtpic wrapper packet family now uses
`MuiExternalWrapperMessageCodec` for `OM_GET`, `MUIM_Set`, `OM_UPDATE`, setup,
min/max, layout, and fixed method records. The focused native boundary is
zero-runtime clean at 5,184/5,232/5,244 bytes for MC68000/020/040 and returns
42 on MC68000.

The fixed Dirlist/Volumelist packet family now uses
`MuiDirlistMessageCodec` for `Set`/`NoNotifySet`, `ReRead`, `Rename`,
`SetComment`, `SetProtection`, `ListGetEntry`, and `ListClear`. The focused
native boundary is zero-runtime clean at 3,944/3,964/3,972 bytes for
MC68000/020/040 and returns 42 on MC68000. The dispatcher consumes named
records; full directory/volume semantics remain a separate progressive slice.

The fixed external Listtree.mcc packet family now uses
`MuiListtreeMessageCodec` for `Set`/`NoNotifySet`, `Get`, `Insert`, `Remove`,
`GetEntry`, `Open`, `Close`, `Sort`, `GetNr`, `Move`, `Exchange`, `Rename`,
`FindName`, `SetDropMark`, and `TestPos`. The focused native boundary is
zero-runtime clean at 8,540/8,700/8,720 bytes for MC68000/020/040 and returns
42 on MC68000. The external dispatcher consumes named records; full Listtree
semantics remain a separate progressive slice.

The fixed pen/color specialist packet family now uses
`MuiColorSpecialistMessageCodec` for `OM_GET`, `OM_SET`/`MUIM_NoNotifySet`,
`Pendisplay_SetColormap`, `Pendisplay_SetMUIPen`, `Pendisplay_SetRGB`, and
`OM_DISPOSE`. The focused native boundary is zero-runtime clean at
3,972/3,992/4,000 bytes for MC68000/020/040 and returns 42 on MC68000. The
specialist dispatcher consumes named records; full specialist semantics remain
a separate progressive slice.

The authoritative inventory and progressive qualification records are under
`docs/Libraries/MorphOs320Mui/`. No MorphOS-compatible version may be advertised
until the corresponding complete versioned surface passes its gate.

The fixed Process.mui, Slave.mui, and shared Semaphore.mui packet family now
uses `MuiProcessSpecialistMessageCodec` for `OM_GET`, `OM_SET`/
`MUIM_NoNotifySet`, process launch/kill/poll/signal, slave setup/cleanup/
dispatch/error/signals-received, and semaphore operations. The focused native
boundary is zero-runtime clean at 4,520/4,544/4,548 bytes for MC68000/020/040
and returns 42 on MC68000. The dispatcher consumes named records; full
Process/Slave semantics remain a separate progressive slice.

The fixed Popstring, Popobject, Poplist, Popasl, Poppen, Popcolor, and
Popscreen packet family now uses `MuiPopSpecialistMessageCodec` for `OM_GET`,
`OM_SET`/`MUIM_NoNotifySet`, `Popstring_Open`, `Popstring_Close`, `Setup`,
`Cleanup`, and `HandleInput`. The focused native boundary is zero-runtime
clean at 3,284/3,300/3,304 bytes for MC68000/020/040 and returns 42 on
MC68000. The dispatcher consumes named records; full Pop* semantics remain a
separate progressive slice.
The fixed MorphOS Menustrip/Menu/Menuitem packet family now uses
`MuiMenuSpecialistMessageCodec` for `OM_GET`, `OM_SET`/
`MUIM_NoNotifySet`, Family add/remove/insert/reorder/sort/transfer,
Menustrip change/open methods, and popup. The dispatcher consumes named
method, attribute, storage, pointer, pair, and popup records; packed guest
offsets are confined to the codec. The focused native boundary is
zero-runtime clean at **4,712/4,748/4,752 bytes** for MC68000/020/040 and
returns **42** on MC68000 after **1,628 instructions / 17,014 cycles**. Full
menu behavior and MorphOS differential parity remain separate progressive
work.

The String.mui Unicode seam now uses `MUIA_Unicode` with logical BufferPos and
DisplayPos columns. Cursor movement, backspace, and delete map through shared
UTF-8 byte-boundary helpers, while drawing clips only at complete sequences.
`StringUtf8CursorRoot` is zero-runtime clean at **2,360 / 2,440 / 2,348
bytes** for MC68000/020/040 and returns **42** on MC68000. Printable Unicode
input now uses the named `MuiUtf8Character` record and a freestanding encoder;
Unicode Accept/Reject filters compare decoded UTF-8 codepoints, while legacy
strings retain byte-set semantics. Unicode `String_MaxLen` truncation also stops
at complete logical-character boundaries. Broader String.mui parity remains
progressive. Public BufferPos/DisplayPos writes are clamped to the logical
UTF-8 contents length before storage and redraw; cursor movement, editing, and
drawing keep the cursor within the visible logical character window.

Stringscroll `MUIM_HandleInput` uses a named `MuiCollectionHandleInputMessage`
record for the MorphOS `imsg`/`muikey` packet. The current bounded input slice
handles line/page navigation, horizontal movement, top, and bottom, while
honoring `MUIA_Stringscroll_NoInput`; unsupported keys remain unclaimed by the
control behavior until additional MorphOS evidence is implemented.

Listview `MUIM_HandleInput` uses the same named packet and forwards MUIKEY
up/down/page-up/page-down/top/bottom to the owned List's active-row selectors.
The List core performs row clamping and visible-window tracking, and
`MUIA_Listview_Input` gates the behavior. Pointer, drag, and full MorphOS
Listview input semantics remain progressive.

Listview `MUIKEY_PRESS` selects the active row exclusively. `MUIKEY_TOGGLE`
inverts the active row in normal multi-select modes and remains exclusive when
`MUIV_Listview_MultiSelect_None` is selected; all changes flow through the
existing ListCore selection and notification state.

Listview pointer activation now uses a named `MuiIntuiPointerMessage` record
when `MUIKEY_NONE` carries an `IDCMP_MOUSEBUTTONS` `SELECTUP` event. The hit is
resolved through the named `MuiListTestPosResult` seam and then follows the
same click, shift-multiselect, and notification policy as direct clicks.
Guest-memory offsets remain confined to `MuiIntuiMessageCodec` and
`MuiListTestPosResultCodec`; SELECTDOWN and drag/drop remain progressive.

When both the Listview and its child List opt into immediate sortable
dragging, the pointer path additionally keeps a guest-resident
`MuiListviewDragState`: SELECTDOWN arms the source, MOUSEMOVE updates the
child's drop mark, and SELECTUP uses `MuiListCore.DragMove`. A stationary
gesture remains a normal click. External MUIM_Drag* methods, pointer capture,
and cancellation are intentionally still separate progressive work.

The local sortable drag seam now consumes MorphOS `MUIKEY_RELEASE` (**-2**) as
its cancellation edge. It clears the child drop mark and releases the named
`MuiListviewDragState` before claiming the event, even when the child list has
become empty. `MUIKEY_TOP` and `MUIKEY_BOTTOM` use the MorphOS values **6/7**;
`MUIKEY_NONE` remains **-1**. This does not claim external `MUIM_Drag*`
messages or pointer-capture/focus-loss behavior, which remain progressive.

The MorphOS Area drag method family now has a bounded struct-first baseline.
`MUIM_DragBegin`, `MUIM_DragDrop`, `MUIM_DragEvent`, `MUIM_DragFinish`,
`MUIM_DragQuery`, and `MUIM_DragReport` use named packet records in
`AreaDragMessagesCore.cs`; `MuiAreaDragState` keeps guest-resident lifecycle
state without managed allocation or exceptions. Begin requires
`MUIA_Draggable`, query requires a draggable source and `MUIA_Dropable` target,
drop/report/event update the record, and finish releases it. The focused native
codec/state roots are zero-runtime and zero-relocation clean; host coverage is
**569/569**. Pointer capture, drag images, application-level drop dispatch,
and full MorphOS differential behavior remain progressive.

The Area activation baseline now consumes the MorphOS `MUIP_GoActive` and
`MUIP_GoInactive` named `{ MethodID, flags }` packet. `GoActive` records the
flags and marks the object active; `GoInactive` records the flags and clears
active state. The state remains in the object store, so this seam uses no
managed allocation, exceptions, or unowned guest block. Per-class visual
activation effects and focus-window coordination remain progressive.

The shared common-control `MUIP_HandleEvent` packet now uses the exact named
MorphOS fields `InputMessage`, signed `MuiKey`, and `EventHandlerNode`. Common
control event behavior consumes `MuiKey` explicitly while preserving the node
pointer for a future callback-capability implementation. Callback dispatch and
full MorphOS event parity remain progressive.

The application-window event path now has a typed single-node callback seam for
`MuiEventHandlerNode`. It validates the GUI-mode flag and event mask, then
invokes the node object through `DoMethod`; the window walk reuses the same
helper. Host coverage is **574/574**, and the focused native root is
zero-runtime and zero-relocation clean. Class coercion, richer return-code
propagation, handler ordering, and complete MorphOS event parity remain
progressive.

Family reorder/sort packet vector lookup now uses the named
`MuiFamilyInlineVectorCursor` and `MuiFamilyInlineVectorCursorCodec.TryGetEntry`
helpers, layered over the typed `MuiFamilyMutationVectorEntry` record. The
fixed packet header, 4-byte pointer-vector elements, bounded mapping, and
overflow checks remain intact. Host coverage is **838/838**; native Family
inline-vector ABI and complete MorphOS differential parity remain progressive.

`MUIA_FramePhantomHoriz` is represented by the named
`MuiAreaRenderPolicyStateRecord.FramePhantomHoriz` field. It follows MorphOS's
initialize-only BOOL contract and makes both Area draw paths omit only the
horizontal frame borders while preserving vertical edges, fill, and content.

`MUIA_FrameTitle` is represented by the caller-owned `FrameTitle` pointer in
the same named render-policy record. Generic Get/OM_GET preserve the pointer,
runtime setters reject the initialize-only attribute, and both Area draw paths
render a bounded C-string centered on the frame's top edge without managed
text ownership or private object offsets.

`MUIA_FrameDynamic` is also carried by that record as a normalized BOOL.
Generic Get/OM_GET and runtime Set/OM_SET now share the named field; the
platform-specific dynamic frame-selection rules remain a separate renderer
qualification slice.

The Area layout-policy record now also feeds a named `MuiAreaContentRect`
through common-control drawing. `MUIA_InnerLeft`, `MUIA_InnerRight`,
`MUIA_InnerTop`, and `MUIA_InnerBottom` are bounded against the outer geometry,
so text, gauges, images, sliders, and scrollbar content share one inset
rectangle without private object offsets or managed geometry state.

Host qualification uses narrow, struct-based local guest-codec fallbacks for
the MUI layout/C-string helpers and Exec-list head needed by this project when
the configured SDK omits those helpers. The fallbacks are bounded,
exception-free, and do not take ownership of guest memory; focused FrameTitle
  coverage is **2/2** and the complete MUI host suite is **1290/1290** in both
local and package SDK modes. Native renderer and differential MorphOS
qualification remain progressive.

The named Area visibility state now also gates drawing: `MUIA_ShowMe=FALSE` is
a successful no-render path resolved before render-port/layer access in both
base Area and common-control draw entry points. Shown objects retain the
existing fill, content, and frame behavior.

Signed `MUIA_HorizDisappear` and `MUIA_VertDisappear` values are carried by
the named `MuiAreaDisappearPolicyStateRecord` and exposed through
`MuiAreaDisappearPacketCore`. Group minimums and horizontal/vertical relayout
remove positive priorities in ascending order, preserve spacing and weights for
remaining children, and use zero-area geometry without mutating
`MUIA_ShowMe`. Grid groups resolve the two axes independently and preserve
their original row/column cells while zeroing selected children. Page groups
now resolve one canonical active child and suppress inactive or explicitly
hidden pages with zero-area geometry. Page min/max now uses the largest child
minimum and smallest finite child maximum, centering capped active pages;
  complete suite coverage is **1290/1290** in both SDK modes. Grid dimension
normalization retains explicit divisibility/remainder state, and explicitly
  hidden children receive zero-area geometry. Group spacing preserves signed
  default/percentage inputs in named records and resolves percentages only from
  the layout extent. SameWidth and SameHeight use a named common-extent record
  so equalized children respect the smallest finite child maximum. SameSize
  enables both component policies for ordinary Groups as well as Grid Groups.
  Ordinary Groups also cap finite child bounds and honor normalized alignment
  modes on both axes. Weighted allocation returns released capped-child space to
  the remaining weights. Group maximum aggregation preserves zero as the
  unbounded sentinel for mixed finite/unbounded children. Weighted allocation
  reserves visible child minimums before sharing remaining space. Grid column
  and row placement now uses the same typed bounded axis allocator, honoring
  aggregate minimums and finite maximums while retaining zero as unbounded.
  Grid child placement honors a positive finite maximum even without a
  preferred or minimum extent. Grid SameWidth and SameHeight apply one
  bounded common child extent inside each cell. Ordinary Groups also apply
  SameHeight/SameWidth on the cross axis through the same named common-extent
  record.

Listview behavior and setter policy use the named
`MuiListviewInteractionPolicyState` guest record. Only `DragType` is exposed
through generic Get/OM_GET; `Input`, `MultiSelect`, and `ScrollerPos` are
MorphOS `[I..]` construction-only fields and fail closed before the generic
scalar fallback. Host coverage is **1184/1184**; native Listview policy ABI
and complete MorphOS differential parity remain progressive.

The current external-drag slice adds the named `MuiDragRouteSample` and
`IMuiDragRoutingCapability` for the six MorphOS Area `MUIM_Drag*` phases. The
optional provider receives complete value-type input and may return the native
method result; the core rejects input-identity changes and otherwise retains
its bounded local state machine. This is an external routing boundary, not a
managed object graph, exception path, or replacement for local Listview drag
state. Focused coverage is **1/1** and the complete host suite is **1329/1329**
in both SDK modes.

The focus-transition slice adds `MuiListviewCore.CancelPointerDragsInWindow`.
`DispatchWindowEvent` calls it for `INACTIVEWINDOW`; it walks the guest Family
hierarchy through `MuiFamilyCore.GetChild`, stops at each Listview, and reuses
the named drag/scroller records to clear drop marks and release optional
pointer capture. Focused coverage is **1/1** and the complete host suite is
**1330/1330** in both SDK modes. Native focus transitions and full MorphOS
differential parity remain progressive.

The focus-transition path also clears active Stringscroll thumb gestures on
`INACTIVEWINDOW` through `MuiStringscrollCore.CancelPointerDragForWindow` and
the named `MuiStringscrollPointerState` Dataspace record. Focused coverage is
**1/1** and the complete host suite is **1331/1331** in both SDK modes.

Stringscroll thumb ownership now uses the optional typed
`IMuiPointerCaptureCapability` seam. Horizontal and vertical starts publish a
`MuiPointerCaptureSample`; `StartX`, `StartY`, and the captured flag remain in
the guest `MuiStringscrollPointerState` struct so SELECTUP, `MUIKEY_RELEASE`,
and `INACTIVEWINDOW` all release the same named ownership record. Focused
coverage is **1/1** and the complete host suite is **1332/1332** in both SDK
modes.

MG540 closes the Stringscroll capture lifetime at collection teardown:
`MuiCollectionLifecycle` reads the named guest pointer state and releases its
`MuiPointerCaptureSample` before generic object destruction. No managed gesture
registry or private object offsets are introduced. Focused coverage is **1/1**
and the complete host suite is **1333/1333** in both SDK modes.

MG541 closes the Area drag state lifetime at object teardown:
`MuiAreaDragCore.Cleanup` clears and frees the named guest-resident
`MuiAreaDragState` before generic attribute nodes are reclaimed. This covers an
Area source that disappears without `MUIM_DragFinish`, with no managed drag
registry or raw object-offset dependency. Focused coverage is **1/1** and the
complete host suite is **1334/1334** in both SDK modes. Native teardown and
full MorphOS differential parity remain progressive.

MG542 closes the remaining Stringscroll capture lifetime path: direct
`MuiHeadlessObjectCore.DisposeObject` now invokes the typed
`MuiStringscrollCore.Cleanup` seam before Dataspace teardown. The named
`MuiStringscrollPointerState` remains authoritative, and optional pointer
capture is released without a managed registry or raw object offsets. Focused
coverage is **1/1** and the complete host suite is **1335/1335** in both SDK
modes. Native teardown and full MorphOS differential parity remain progressive.

MG543 aligns generic object teardown with collection ownership. Before raw
attributes and Dataspace are reclaimed, `MuiHeadlessObjectCore.DisposeObject`
routes Listview, Listtree, List-backed, and Stringscroll objects through their
named cleanup records. A directly disposed Listview therefore releases pointer
capture and its child List state without a managed registry or private offsets.
Focused coverage is **1/1** and the complete host suite is **1336/1336** in
both SDK modes. Native teardown and full MorphOS differential parity remain
progressive.

Floattext generic Get and collection OM_GET for `Text`, `SkipChars`, `TabSize`,
`Justify`, and shared `Width` use the named `MuiFloattextPolicyState` record.
Raw scalar reads remain confined to initialization and compatibility
synchronization so construction tags are not hidden by the public projection.

Stringscroll generic Get and collection OM_GET for `String`, `HorizBar`,
`NoInput`, `SetMin`, `SetVMin`, `UseWinBorder`, `VertBar`, and
`VertScrollerOnly` use the named `MuiStringscrollPolicyRecord`; raw scalar
storage remains only the initialization and compatibility synchronization
seam.

Dirlist and Volumelist generic Get now use the named filter and sort records;
Dirlist OM_GET publishes those values through the named ULONG result-slot
boundary. Volumelist `ExampleMode` uses its named mode record, while
construction and compatibility synchronization remain on raw attributes. Host
coverage is **1187/1187**; native Dirlist/Volumelist getter ABI and complete
MorphOS differential parity remain progressive.

Listtree policy and hook getters now prefer the named
`MuiListtreePolicyStateRecord`, including the external Listtree `Get` message's
ULONG result storage. Construction and compatibility synchronization stay on
raw attributes to preserve the external-class boundary. Host coverage is
**1188/1188**; native Listtree getter ABI and complete MorphOS differential
parity remain progressive.

Group grid policy getters now prefer the named `MuiGroupGridStateRecord` for
columns, rows, spacing, same-size, and centering. Raw attributes remain only
the bootstrap and compatibility synchronization seam, and common-control
`OM_GET` publishes the same typed projection for Group objects. Native Group
grid getter ABI and complete MorphOS differential parity remain progressive.
Host coverage is **1189/1189**.

Group `MUIA_Group_ActivePage` getters now use the named `MuiGroupPageState`
record and selector normalization for populated Groups. Zero-child Groups
retain their raw compatibility value for persistence round-trips, while both
generic `Get` and common-control `OM_GET` share the same projection. Host
coverage is **1190/1190**; native Group ActivePage ABI and complete MorphOS
differential parity remain progressive.

Group layout-policy getters now use the named `MuiGroupLayoutPolicyStateRecord`
for orientation, effective spacing, same-size policy, spacing, and page mode;
Group grid retains precedence for overlapping grid attributes. Bootstrap reads
remain on the explicit raw-attribute seam, while generic `Get` and common
control `OM_GET` share the named projection. Host coverage is **1191/1191**;
native Group layout-policy ABI and complete MorphOS differential parity remain
progressive.

Group `MUIA_Group_LayoutHook` now uses the named guest-resident
`MuiGroupLayoutHookStateRecord`; typed layout dispatch, generic `Get`, and
common-control `OM_GET` share that pointer state. Raw storage remains only the
compatibility/bootstrap seam, the state is released with the object, and the
initialize-only MorphOS contract is enforced. Focused Group layout-hook
coverage is green at **18/18**, with full host coverage at **1191/1191**;
native LayoutHook ABI and complete MorphOS differential parity remain
progressive.

Getter-only Group `MUIA_Group_ChildCount` and `MUIA_Group_ChildList` now route
through common-control `OM_GET`, including the external Group class boundary.
ChildCount remains family-derived and ChildList remains a named read-only guest
projection; raw public slots cannot replace the live values. Focused Group
children coverage is **10/10**, with full host coverage at **1192/1192**;
native child-getter ABI and complete MorphOS differential parity remain
progressive.

Generic `MUIA_Parent`, `MUIA_ObjectID`, and `MUIA_UserData` getters now use the
named `MuiHeadlessObjectRecord` for direct `Get` and common-control `OM_GET`,
including unknown and external classes. Parent relationship state remains
authoritative over raw compatibility slots. Focused metadata coverage is
**1/1**, with full host coverage at **1193/1193**; native metadata ABI and
complete MorphOS differential parity remain progressive.

Getter-only `MUIA_Family_ChildCount` and `MUIA_Family_List` now reuse the
named guest-resident child-list projection for Family-compatible class chains,
including Family, Group, application/window, and menu ownership. The count is
topology-derived, the returned `MinList` is read-only, and raw public slots
remain compatibility/bootstrap storage. Focused Family getter coverage is
**1/1**; native Family getter ABI and complete MorphOS differential parity
remain progressive.

Initialize-only `MUIA_Family_Child` tags now adopt non-null children through
the guest-resident Family topology in tag order. Runtime sets after object
initialization are rejected, and null child tags fail without leaving a
partially-created object. Focused Family coverage is **2/2**; native Family
child-tag ABI and complete MorphOS differential parity remain progressive.

`MUIA_Version` and `MUIA_Revision` now use named class metadata when a builtin
or external class is registered with explicit values. Direct `Get` and common
`OM_GET` share that projection, while unannotated classes retain raw fallback
compatibility. Focused version/revision coverage is **1/1**; the complete
per-class MorphOS metadata inventory remains progressive.

`MUIA_Group_Child` creation tags now follow the documented
`MUIA_Family_Child` alias for non-Group Family classes. Alias adoption is
initialize-only, while the existing Group-specific route remains unchanged.
Focused Family coverage is **3/3**; native alias ABI and complete MorphOS
differential parity remain progressive.

The shared Area `MUIA_Weight` input now uses the named
`MuiAreaWeightState`/`MuiAreaWeightStateRecord` structs for generic `Get` and
`OM_GET`. The default of 100, raw-only compatibility synchronization, runtime
setter behavior, and persistence/import updates remain intact while resolved
horizontal/vertical weights stay in the separate layout-policy record. Host
coverage is **1183/1183**; native Area Weight ABI and complete MorphOS
differential parity remain progressive.

Prop and Scrollbar policy scalars now use the named
`MuiPropPolicyState`/`MuiPropPolicyStateRecord` structs for generic `Get` and
`OM_GET`. Raw storage remains only the compatibility synchronization seam;
runtime DeltaFactor/Slider updates, Slider normalization, initializer-only
Horiz/UseWinBorder rules, and Scrollbar child forwarding remain intact. Host
coverage is **1182/1182**; native Prop policy ABI and complete MorphOS
differential parity remain progressive.

Image spec getter projection now routes `MUIA_Image_Spec` and
`MUIA_Image_BuiltinSpec` through the guest-resident
`MuiImageSpecStateRecord`. Separate presence fields preserve MorphOS union
semantics, runtime Image_Spec writes update the named struct, and direct,
generic, and `OM_GET` paths use raw-only bootstrap/synchronization reads.
Host coverage is **1167/1167**; native Image spec ABI and complete MorphOS
differential parity remain progressive.

Image render and legacy-pointer getters now route
`MUIA_Image_State`, `MUIA_Selected`, `MUIA_Image_FreeHoriz`, and
`MUIA_Image_FreeVert` through `MuiImageRenderStateRecord`, and
`MUIA_Image_OldImage` through `MuiImageOldImageStateRecord`. The implementation
preserves selection/free-axis synchronization, init-only OldImage behavior,
caller-owned pointers, and raw-only internal bootstrap reads. Host coverage is
**1168/1168**; native Image render/legacy getter ABI and complete MorphOS
differential parity remain progressive.

Bitmap and Bodychunk getter projection now uses named records for shared
width/height geometry, class-specific source pointers, and Bodychunk
compression/depth/masking. Class gating, caller-owned sources, default depth,
and live remap/redecode behavior remain intact, with raw-only internal
synchronization. Host coverage is **1169/1169**; native Bitmap/Bodychunk
getter ABI and complete MorphOS differential parity remain progressive.

Rectangle bar flags now route through `MuiRectanglePresentationStateRecord`,
and the optional caller-owned `MUIA_Rectangle_BarTitle` pointer through
`MuiRectangleBarTitleStateRecord`. Class gating, init-only behavior,
absent-title presence, and raw-only synchronization remain intact. Host
coverage is **1169/1169**; native Rectangle getter ABI and complete MorphOS
differential parity remain progressive.

The optional common-control `MUIA_Font` pointer now routes through
`MuiControlFontStateRecord`, and `MUIA_Image_FontMatchString` through
`MuiImageFontMatchStringStateRecord`. Presence semantics, Image class gating,
caller-owned pointer validation, runtime setter behavior, and raw-only
synchronization remain intact. Host coverage is **1169/1169**; native
Font/Image font-match getter ABI and complete MorphOS differential parity
remain progressive.

Shared Area presentation attributes (`MUIA_Disabled`, `MUIA_ShowMe`,
`MUIA_Background`, and `MUIA_Frame`) now route through
`MuiAreaPresentationStateRecord` for generic `Get` and `OM_GET`. Class gating,
runtime setter/redraw behavior, ULONG semantics, and raw-only synchronization
remain intact. Host coverage is **1169/1169**; native Area presentation getter
ABI and complete MorphOS differential parity remain progressive.

Area geometry now routes `MUIA_LeftEdge`, `MUIA_TopEdge`, `MUIA_Width`,
`MUIA_Height`, `MUIA_RightEdge`, and `MUIA_BottomEdge` through
`MuiAreaGeometryStateRecord` for generic `Get` and `OM_GET`. Signed coordinates
remain ULONG-compatible on the guest bus, with layout updates and raw-only
synchronization preserved. Host coverage is **1170/1170**; native Area geometry
getter ABI and complete MorphOS differential parity remain progressive.

Area layout policy now routes `MUIA_Weight`, `MUIA_HorizWeight`,
`MUIA_VertWeight`, `MUIA_FixWidth`, `MUIA_FixHeight`, `MUIA_MaxWidth`,
`MUIA_MaxHeight`, `MUIA_InnerLeft`, `MUIA_InnerRight`, `MUIA_InnerTop`, and
`MUIA_InnerBottom` through `MuiAreaLayoutPolicyStateRecord` for generic `Get`
and `OM_GET`. Shared-weight defaults, min/max behavior, and raw-only policy
synchronization remain intact. Host coverage is **1171/1171**; native Area
layout-policy getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_FillArea` now routes through `MuiAreaRenderPolicyStateRecord` for generic
`Get` and `OM_GET`. Runtime updates keep the same named record used by Area
drawing synchronized, with default fill behavior and raw-only reads preserved.
Host coverage is **1172/1172**; native Area render-policy getter ABI and
complete MorphOS differential parity remain progressive.

Slider `MUIA_Slider_Horiz`/`MUIA_Slider_Quiet`, Scale `MUIA_Scale_Horiz`, and
Levelmeter `MUIA_Gauge_Horiz` now route through their named presentation
records for generic `Get` and `OM_GET`. Shared-key ownership, defaults,
raw-only synchronization, and runtime Slider/Scale updates remain intact. Host
coverage is **1173/1173**; native presentation getter ABI and complete MorphOS
differential parity remain progressive.

Gadget `MUIA_InputMode`, `MUIA_Selected`, and `MUIA_Pressed` now route through
`MuiGadgetInteractionStateRecord` for generic `Get` and `OM_GET`. Class
gating, keyboard/runtime selection transitions, and raw-only interaction
synchronization remain intact. Host coverage is **1174/1174**; native Gadget
interaction getter ABI and complete MorphOS differential parity remain
progressive.

Prop/Scrollbar `MUIA_Prop_Entries`, `MUIA_Prop_Visible`, and `MUIA_Prop_First`
now route through `MuiPropRangeStateRecord`; Scrollbar
`MUIA_Group_Horiz`/`MUIA_Scrollbar_Type` use `MuiScrollbarLayoutStateRecord`.
Class gating, range movement, child forwarding, and raw-only synchronization
remain intact. Host coverage is **1175/1175**; native range/layout getter ABI
and complete MorphOS differential parity remain progressive.

Cycle/Radio `MUIA_Cycle_Entries`/`MUIA_Radio_Entries` now route through
`MuiChoiceEntriesStateRecord`, while `MUIA_Cycle_Active`/`MUIA_Radio_Active`
use `MuiChoiceActiveStateRecord` for generic `Get` and `OM_GET`. Bounded
NULL-terminated entry validation, Cycle wrap selectors, Radio selection rules,
class gating, and raw-only synchronization remain intact. Host coverage is
**1176/1176**; native Choice getter ABI and complete MorphOS differential
parity remain progressive.

String scroll width/height, visible viewport dimensions, and pixel offsets now
route through `MuiStringScrollMetricsStateRecord` for direct `Get`, generic
`Get`, and `OM_GET`. UTF-8 visual-column counting, CR/LF semantics,
layout-derived viewport values, bounded offset clamping, and raw-only
synchronization remain intact. Host coverage is **1177/1177**; native String
scroll getter ABI and complete MorphOS differential parity remain progressive.

Image `MUIA_Image_FontMatch`, `MUIA_Image_FontMatchHeight`, and
`MUIA_Image_FontMatchWidth` now route through `MuiImageFontMatchStateRecord`
for generic `Get` and `OM_GET`. Their initializer-only setter policy,
independent caller-owned `FontMatchString` record, and raw-only persistence
synchronization remain intact. Host coverage is **1178/1178**; native Image
FontMatch getter ABI and complete MorphOS differential parity remain
progressive.

Bitmap-only `MUIA_Bitmap_Alpha`, `MUIA_Bitmap_MappingTable`,
`MUIA_Bitmap_Precision`, `MUIA_Bitmap_SourceColors`,
`MUIA_Bitmap_Transparent`, and `MUIA_Bitmap_UseFriend` now route through
`MuiBitmapPolicyStateRecord` for generic `Get` and `OM_GET`. Bitmap/Bodychunk
separation, runtime `[ISG]` mutation and remap invalidation, initializer-only
`UseFriend`, and raw-only synchronization remain intact. Host coverage is
**1179/1179**; native Bitmap policy getter ABI and complete MorphOS
differential parity remain progressive.

Renderer-produced `MUIA_Bitmap_Remapped` now routes through
`MuiBitmapRemappedStateRecord` for Bitmap and Bodychunk generic `Get` and
`OM_GET`. Source rebuild, cleanup, null-on-failure behavior, get-only setter
policy, caller-owned pointer semantics, and raw-only synchronization remain
intact. Host coverage is **1180/1180**; native remapped-state getter ABI and
complete MorphOS differential parity remain progressive.

Getter-only `MUIA_Gadget_Gadget` now routes through
`MuiGadgetGadgetStateRecord` for generic `Get` and `OM_GET`. Caller-owned
relationship semantics, Gadget class gating, persistence/bootstrap
synchronization, and get-only setter behavior remain intact. Host coverage is
**1181/1181**; native Gadget relationship getter ABI and complete MorphOS
differential parity remain progressive.

Store/Objectmap/Datamap packets now resolve their fixed fields through the
named `MuiStoreFieldCursor` and `MuiStoreFieldCursorCodec` helpers. Pointer,
signed-length, key, result-storage, object, and iteration-counter payloads,
packet validation, mapping checks, and overflow rejection remain intact. Host
coverage is **905/905**; native Store/Objectmap/Datamap packet ABI and complete
MorphOS differential parity remain progressive.

Family mutation packets now resolve their fixed fields through the named
`MuiFamilyPacketFieldCursor` and `MuiFamilyPacketFieldCursorCodec` helpers,
while Family mutation list head/tail fields use
`MuiFamilyMutationListFieldCursor` and
`MuiFamilyMutationListFieldCursorCodec`. Pointer payloads, packet validation,
mapping checks, and vector-header overflow rejection remain intact. Host
coverage is **906/906**; native Family mutation packet/list ABI and complete
MorphOS differential parity remain progressive.

The fixed MorphOS `MUIM_Family_GetChild` packet now resolves its method,
signed selector, and reference fields through the named
`MuiFamilyGetChildPacketFieldCursor` and
`MuiFamilyGetChildPacketFieldCursorCodec` helpers. Packet validation, mapping
checks, and overflow rejection remain intact. Host coverage is **907/907**;
native Family_GetChild packet ABI and complete MorphOS differential parity
remain progressive.

The fixed MorphOS `MUIM_Family_DoChildMethods` method field now resolves
through the named `MuiFamilyDoChildMethodsPacketFieldCursor` and
`MuiFamilyDoChildMethodsPacketFieldCursorCodec` helpers. Method validation,
packet-size checks, mapping checks, and bounded child-forward dispatch remain
intact. Host coverage is **908/908**; native Family_DoChildMethods packet ABI
and complete MorphOS differential parity remain progressive.

The fixed MorphOS Boopsi query packet now resolves its screen, flags, signed
dimension, and RenderInfo fields through the named
`MuiBoopsiQueryPacketFieldCursor` and
`MuiBoopsiQueryPacketFieldCursorCodec` helpers. Packet validation, mapping
checks, and overflow rejection remain intact. Host coverage is **909/909**;
native Boopsi query packet ABI and complete MorphOS differential parity remain
progressive.

The fixed MorphOS `MUIM_CallHook` envelope now resolves method, hook, and
first-parameter fields through the named `MuiCallHookPacketFieldCursor` and
`MuiCallHookPacketFieldCursorCodec` helpers. Variadic-tail addressing, packet
validation, mapping checks, and overflow rejection remain intact. Host
coverage is **910/910**; native CallHook packet ABI and complete MorphOS
differential parity remain progressive.

Notify `WriteLong` and `WriteString` packets now resolve their value,
source-string, and destination-memory fields through the named
`MuiNotifyWritePacketFieldCursor` and
`MuiNotifyWritePacketFieldCursorCodec` helpers. Bounded copy behavior, packet
validation, mapping checks, and overflow rejection remain intact. Host
coverage is **911/911**; native Notify write packet ABI and complete MorphOS
differential parity remain progressive.

The fixed MorphOS `MUIM_GetConfigItem` envelope now resolves method, config-id,
and result-storage fields through the named
`MuiGetConfigItemPacketFieldCursor` and
`MuiGetConfigItemPacketFieldCursorCodec` helpers. Packet validation, mapping
checks, and overflow rejection remain intact. Host coverage is **912/912**;
native GetConfigItem packet ABI and complete MorphOS differential parity remain
progressive.

Notify UserData Find/Get/Set packets now resolve their fields through the
named `MuiNotifyUserDataPacketFieldCursor` and
`MuiNotifyUserDataPacketFieldCursorCodec` helpers, while traversal-frame
Object/NextChild fields use `MuiUDataTraversalFieldCursor` and
`MuiUDataTraversalFieldCursorCodec`. Bounded traversal, packet validation,
mapping checks, and overflow rejection remain intact. Host coverage is
**913/913**; native UserData packet/traversal ABI and complete MorphOS
differential parity remain progressive.

The fixed MorphOS `MUIM_SetAsString` envelope now resolves method, attribute,
format, and value fields through the named
`MuiSetAsStringPacketFieldCursor` and
`MuiSetAsStringPacketFieldCursorCodec` helpers. Variadic-parameter addressing,
bounded formatting, packet validation, mapping checks, and overflow rejection
remain intact. Host coverage is **914/914**; native SetAsString packet ABI and
complete MorphOS differential parity remain progressive.

The fixed MorphOS Notify, KillNotify, KillNotifyObject, Set/NoNotifySet,
MultiSet, and FindObject packets now resolve their fields through the named
`MuiNotifyPacketFieldCursor` and `MuiNotifyPacketFieldCursorCodec` helpers.
Inline-vector addressing, packet validation, mapping checks, and overflow
rejection remain intact. Host coverage is **915/915**; native Notify core
packet ABI and complete MorphOS differential parity remain progressive.

The fixed MorphOS Export/Import persistence packets now resolve method and
Dataspace fields through the named `MuiObjectPersistencePacketFieldCursor` and
`MuiObjectPersistencePacketFieldCursorCodec` helpers. Packet validation,
mapping checks, and overflow rejection remain intact. Host coverage is
**916/916**; native object persistence packet ABI and complete MorphOS
differential parity remain progressive.

The fixed MorphOS `MUIM_UpdateConfig` method, config-id, and signed
redraw-count header fields now resolve through the named
`MuiUpdateConfigPacketFieldCursor` and
`MuiUpdateConfigPacketFieldCursorCodec` helpers. Existing redraw-table
cursors, count validation, packet checks, mapping checks, and overflow
rejection remain intact. Host coverage is **917/917**; native UpdateConfig
packet ABI and complete MorphOS differential parity remain progressive.

Application ReturnID, Input/NewInput, and Add/RemoveInputHandler packet fields
now resolve through the named `MuiApplicationInputPacketFieldCursor` and
`MuiApplicationInputPacketFieldCursorCodec` helpers. The fixed 8-byte packet
records, payload selection, mapping and overflow checks remain intact. Host
coverage is **918/918**; native Application input packet ABI and complete
MorphOS differential parity remain progressive.

Application PushMethod and Unpush packet fields now resolve through the named
`MuiApplicationQueuePacketFieldCursor` and
`MuiApplicationQueuePacketFieldCursorCodec` helpers, keyed by packet kind.
The 12-byte and 16-byte packet records, parameter-tail addressing, mapping,
and overflow checks remain intact. Host coverage is **919/919**; native
Application queue packet ABI and complete MorphOS differential parity remain
progressive.

Application ShowHelp and AboutMUI packet fields now resolve through the named
`MuiApplicationPresentationPacketFieldCursor` and
`MuiApplicationPresentationPacketFieldCursorCodec` helpers, keyed by packet
kind. The 20-byte and 8-byte packet records, mapping, and overflow checks
remain intact. Host coverage is **920/920**; native Application presentation
packet ABI and complete MorphOS differential parity remain progressive.

Application SetConfigItem, OpenConfigWindow, BuildSettingsPanel, and Settings
I/O packet fields now resolve through the named
`MuiApplicationSettingsPacketFieldCursor` and
`MuiApplicationSettingsPacketFieldCursorCodec` helpers, keyed by packet kind.
The 12-byte and 8-byte packet records, mapping, and overflow checks remain
intact. Host coverage is **921/921**; native Application settings packet ABI
and complete MorphOS differential parity remain progressive.

Application ConfigID, CheckRefresh, Loop, WindowMethod, and Snapshot records
now resolve through the named `MuiApplicationMethodPacketFieldCursor` and
`MuiApplicationMethodPacketFieldCursorCodec` helpers, keyed by packet kind.
The method-only 4-byte records and snapshot/config-id payloads retain their
mapping and overflow checks. Host coverage is **922/922**; native Application
method-record ABI and complete MorphOS differential parity remain progressive.

Application and Window menu query/set packet fields now resolve through the
named `MuiApplicationMenuPacketFieldCursor` and
`MuiApplicationMenuPacketFieldCursorCodec` helpers, keyed by packet kind.
The 8-byte and 12-byte packet records, mapping, and overflow checks remain
intact. Host coverage is **923/923**; native menu packet ABI and complete
MorphOS differential parity remain progressive.

Window Add/RemoveEventHandler packet fields now resolve through the named
`MuiWindowEventHandlerPacketFieldCursor` and
`MuiWindowEventHandlerPacketFieldCursorCodec` helpers, keyed by packet kind.
The fixed 8-byte record, handler pointer, mapping, and overflow checks remain
intact. Host coverage is **924/924**; native Window event-handler packet ABI
and complete MorphOS differential parity remain progressive.

Window SetCycleChain method and first-object header fields now resolve through
the named `MuiWindowCycleChainPacketFieldCursor` and
`MuiWindowCycleChainPacketFieldCursorCodec` helpers. The fixed header remains
separate from the existing inline object-vector tail cursor. Host coverage is
**925/925**; native SetCycleChain header ABI and complete MorphOS differential
parity remain progressive.

The current `ApplicationDispatcher.cs` fixed-packet surface is now fully
struct-first: its nonzero guest-memory `Read/WriteUInt*` offset scan is empty.
Named packet structs and cursor codecs retain mapping, overflow, method, and
vector validation. Host coverage remains **925/925**; native
ApplicationDispatcher ABI and complete MorphOS differential parity remain
progressive.

The duplicate Window event-handler codec in `ApplicationWindowCore.cs` now
reuses the named event-handler packet cursor shared with the dispatcher.
Existing packet write/read, method validation, handler mapping, and
freestanding behavior remain intact. Host coverage remains **925/925**;
native ApplicationWindow event-handler ABI and complete MorphOS differential
parity remain progressive.

The persistent 20-byte ApplicationWindow node record now resolves Next, Value,
Sequence, Auxiliary, and Packet through the named
`MuiApplicationWindowNodeFieldCursor` and
`MuiApplicationWindowNodeFieldCursorCodec` helpers. Host coverage is
**926/926**; native node-record ABI and complete MorphOS differential parity
remain progressive.

The mixed-width 24-byte ApplicationWindow event-handler node now resolves
successor/predecessor links, reserved and priority bytes, flags, object/class
pointers, and events through the named `MuiEventHandlerNodeFieldCursor` and
`MuiEventHandlerNodeFieldCursorCodec` helpers. Host coverage is **927/927**;
native event-handler node ABI and complete MorphOS differential parity remain
progressive.

The remaining ApplicationWindowCore fixed records now use named cursors as
well: the 24-byte input-handler node and private SetConfigItem state no longer
use nonzero guest-memory offsets. The source-level offset audit is empty and
host coverage is **929/929**; native ApplicationWindowCore ABI and complete
MorphOS differential parity remain progressive.

The Listtree.mcc fixed records now use named field cursors: the 48-byte
header, mixed-width 64-byte tree node, and 12-byte TestPos result no longer
perform direct nonzero guest-memory field access. Host coverage is **932/932**;
native Listtree ABI and complete MorphOS differential parity remain
progressive.

The ListCore header, contiguous slot record, and image-chain record now use
named field cursors for all fixed fields, including cookie and pointer/value
validation. Host coverage is **934/934**; remaining ListCore record families,
native ABI, and complete MorphOS differential parity remain progressive.

ListCore edit state and column geometry now use named field cursors as well,
including signed row/column and pointer fields. Host coverage is **936/936**;
remaining ListCore record families, native ABI, and complete MorphOS
differential parity remain progressive.

ListCore column-metrics state now uses a named field cursor for Magic, Width,
Columns, and Values. Host coverage is **937/937**; remaining ListCore record
families, native ABI, and complete MorphOS differential parity remain
progressive.

ListCore title-array, redraw, column-visibility, column-order, and viewport
state now use the shared typed `MuiListStateFieldCursor`. Host coverage is
**938/938**; remaining ListCore records, native ABI, and complete MorphOS
differential parity remain progressive.

The Misc specialist header and Title state now use the typed
`MuiMiscRecordFieldCursor` for class, flags, notifications, page topology, and
Title scalars. Host coverage is **939/939**; remaining Misc records, native
ABI, and complete MorphOS differential parity remain progressive.

Misc Filepanel service state and owned-string slots now use the shared typed
record cursor for hook/ASL/row pointers, row counts, string pointers, and
allocation sizes. Host coverage remains **939/939**; remaining Misc records,
native ABI, and complete MorphOS differential parity remain progressive.

Misc Mccprefs, Scrmodelist, WindowPanel, and Fontdisplay state now use the
shared typed record cursor for registry/mode data, application/window pointers,
and natural-size values. Host coverage remains **939/939**; remaining Misc
records, native ABI, and complete MorphOS differential parity remain
progressive.

Misc Title-page, Mccprefs registry, and Filepanel row records now use the same
typed cursor for handles/flags, registry pointers and scalar parameters, and
row pointers. Host coverage remains **939/939**; native Misc ABI and complete
MorphOS differential parity remain progressive.

The complete 108-byte Pop* specialist instance record now uses the named
`MuiPopSpecialistRecordFieldCursor` for class state, child and hook pointers,
popup/ASL ownership pointers, selection, and notification scalars. Mapping and
overflow checks remain intact; host coverage is **940/940**. Native
PopSpecialist ABI and complete MorphOS differential parity remain progressive.

The 32-byte MUI_PenSpec copy, 64-byte ColorSpecialist state, and 12-byte
MUI_RGBColor records now use `MuiColorRecordFieldCursor`, keyed by semantic
record kind and field. Mapping and overflow checks remain intact; host coverage
is **941/941**. Native ColorSpecialist ABI and complete MorphOS differential
parity remain progressive.

The 16-byte class-service state, 44-byte class lease, and 28-byte
`MUI_CustomClass` records now use `MuiClassRecordFieldCursor`, keyed by semantic
record kind and field. Mapping and overflow checks remain intact; host coverage
is **942/942**. Native ClassService ABI and complete MorphOS differential parity
remain progressive.

Drawing-service state, clip, refresh, pen, render-info, and RasterPort records
now use `MuiDrawingRecordFieldCursor`, keyed by semantic record kind and field.
Mapping and overflow checks remain intact; host coverage is **943/943**. Native
DrawingService ABI and complete MorphOS differential parity remain progressive.

Group forward state, child-list state, child-entry, and Exec list records now
use `MuiGroupRecordFieldCursor`, including explicit byte-width handling for Exec
list fields. Mapping and overflow checks remain intact; host coverage is
**944/944**. Native GroupChildren ABI and complete MorphOS differential parity
remain progressive.

Dirlist’s byte-total QUAD, FileInfo-like entry header, and ExAll-like scan
header now use `MuiDirlistRecordFieldCursor`; variable-length string tails remain
explicit guest pointers. Mapping and overflow checks remain intact; host
coverage is **945/945**. Native Dirlist ABI and complete MorphOS differential
parity remain progressive.

The Process dispatch header and Process/Slave sidecar now use
`MuiProcessRecordFieldCursor`, covering dispatch counts/method ids, process
state, task/name ownership, signal/error/notification fields, and mapping and
overflow checks. Host coverage is **946/946**. Native ProcessSpecialist ABI and
complete MorphOS differential parity remain progressive.

Group Init/Exit, ExitChange2, and bracket-state records now use
`MuiGroupChangeRecordFieldCursor` for method/flag decoding, nested depth, and
exit counters. The remaining fixed byte reads are intentional Group.mui
C-string identity checks. Mapping and overflow checks remain intact; host
coverage is **947/947**. Native GroupChange ABI and complete MorphOS differential
parity remain progressive.

The packed 44-byte String.mui SGWork record now uses
`MuiStringEditRecordFieldCursor`, preserving pointer/value fields and the
16-bit Code, BufferPos, NumChars, and EditOp members at their exact ABI
positions. Mapping and overflow checks remain intact; host coverage is
**948/948**. Native StringEditHook ABI and complete MorphOS differential parity
remain progressive.

The 52-byte Menustrip/Menu/Menuitem specialist sidecar now uses
`MuiMenuRecordFieldCursor` for class/depth state, owned title/shortcut storage,
flags, trigger publication, and notification counters. Mapping and overflow
checks remain intact; host coverage is **949/949**. Native MenuSpecialist ABI and
complete MorphOS differential parity remain progressive.

List TestPos results, scalar storage, Listview drag state, and the Listview
IntuiMessage pointer envelope now use the named
`MuiListInputRecordFieldCursor` and codec. The 12-byte mixed-width TestPos
record, four-byte scalar/drag fields, Intuition fields at offsets `0x14`
through `0x22`, signed 16-bit conversion, mapping, and overflow checks remain
intact; host coverage is **951/951**. Native ListInputRecords ABI and complete
MorphOS differential parity remain progressive.

Application WindowList projection state and Exec projection entries now use
named state/entry field cursors. Cookie/state validation, pointer links,
projection marker, count/capacity, generation, mapping, and overflow checks
remain intact; host coverage is **952/952**. Native Application WindowList ABI
and complete MorphOS differential parity remain progressive.

The fixed 32-byte GroupGrid specification now uses the named
`MuiGroupGridSpecFieldCursor` for columns, rows, spacing, same-size, and
centering fields. Mapping and overflow checks remain intact; host coverage is
**953/953**. Native GroupGrid specification ABI and complete MorphOS
differential parity remain progressive.

The 32-byte Area drag lifecycle state now uses the named
`MuiAreaDragStateFieldCursor` for cookie, source/target, coordinates,
qualifier, event flags, and lifecycle flags. Mapping and overflow checks remain
intact; host coverage is **954/954**. Native Area drag-state ABI and complete
MorphOS differential parity remain progressive.

`MUI_MakeObjectA` parameter prefixes and packed 20-byte `NewMenu` records now
use named mixed-width field cursors. Parameter bounds, byte/word/long field
positions, mapping, and overflow checks remain intact; host coverage is
**955/955**. Native MakeObjectA/NewMenu ABI and complete MorphOS differential
parity remain progressive.

The caller-owned 8-byte graphics `Image` geometry prefix now uses the named
`MuiImageGeometryFieldCursor`, preserving signed edge words, unsigned
dimensions, mapping, and overflow checks; host coverage is **956/956**. Native
Image geometry ABI and complete MorphOS differential parity remain progressive.

Group MoveMember, Reorder, Sort, and method-header packets now use the named
`MuiGroupOrderingPacketFieldCursor`, preserving packet-kind boundaries, method
selectors, object vectors, signed positions, mapping, and overflow checks;
host coverage is **957/957**. Native Group ordering packet ABI and complete
MorphOS differential parity remain progressive.

The 12-byte ASL service state and 16-byte requester lease records now use the
named `MuiAslRecordFieldCursor`, preserving state head/generation,
requester/type/tag links, mapping, and overflow checks; host coverage is
**958/958**. Native ASL service record ABI and complete MorphOS differential
parity remain progressive.

The 20-byte Listview click-state record now uses the named
`MuiListviewClickStateFieldCursor`, preserving cookie validation, click-column,
edge-triggered flags, click-count normalization, mapping, and overflow checks;
host coverage is **959/959**. Native Listview click-state ABI and complete
MorphOS differential parity remain progressive.

Error service state, Group page state, String QUAD high/low storage, and
Requester service state now use named field cursors. Cookie/version fields,
counters/selectors, signed-value representation, mapping, and overflow checks
remain intact; host coverage is **963/963**. Native service/value-record ABI
and complete MorphOS differential parity remain progressive.

Store iteration counters now use a named Ordinal field cursor. Dataspace and
Objectmap length publication plus scalar export scratch storage use the named
`MuiGuestUlongStorageCodec`, preserving caller-owned ULONG result semantics;
host coverage is **964/964**. Native Store ABI and complete MorphOS differential
parity remain progressive.

CommonControl choice-entry text pointers and List metric values, pointer slots,
and owned-record length headers now use named field cursors. Host coverage is
**966/966**; native single-field record ABI and complete MorphOS differential
parity remain progressive.

Application UsedClasses, Poplist, Requester parameter, Process dispatch, and
UpdateConfig object/flag slots now use named field cursors while preserving
their pointer and byte-width semantics; host coverage is **967/967**. Native
slot ABI and complete MorphOS differential parity remain progressive.

Vertical `Scale.mui` drawing now emits an integer-only centre axis and
graduated horizontal ticks for the 0%..100% range, with detail adapting to
available height. Host coverage is **968/968**; native visual parity and
complete MorphOS differential parity remain progressive.

Dtpic picture layout results now resolve Width and Height through the named
`MuiExternalDtpicLayoutFieldCursor` and
`MuiExternalDtpicLayoutFieldCursorCodec` helpers. The typed 8-byte result,
mapping/overflow checks, and existing picture-acquisition behavior remain
intact. Host coverage is **862/862**; native Dtpic layout-result field ABI and
complete MorphOS differential parity remain progressive.

ExternalWrapper setup now resolves the four MUI_RenderInfo pointers through
the named `MuiExternalRenderInfoFieldCursor` and
`MuiExternalRenderInfoFieldCursorCodec` helpers. The typed 16-byte record,
mapping/overflow checks, and existing setup behavior remain intact. Host
coverage is **863/863**; native RenderInfo field ABI and complete MorphOS
differential parity remain progressive.

The stored ExternalWrapper display environment now resolves Window, Screen,
and DrawInfo through the named `MuiExternalDisplayEnvironmentFieldCursor` and
`MuiExternalDisplayEnvironmentFieldCursorCodec` helpers. The typed 12-byte
record, mapping/overflow checks, and existing display-state behavior remain
intact. Host coverage is **864/864**; native display-environment field ABI and
complete MorphOS differential parity remain progressive.

The stored ExternalWrapper RastPort pointer now resolves through the named
`MuiExternalRastPortSlotFieldCursor` and
`MuiExternalRastPortSlotFieldCursorCodec` helpers. The typed 4-byte slot,
mapping checks, and existing display-state behavior remain intact. Host
coverage is **865/865**; native RastPort-slot field ABI and complete MorphOS
differential parity remain progressive.

Application Save/Load traversal frames now resolve Object, NextChild, and
VisitMarker through the named `MuiApplicationPersistenceFrameFieldCursor` and
`MuiApplicationPersistenceFrameFieldCursorCodec` helpers. The typed 12-byte
frame, mapping/overflow checks, and existing traversal behavior remain intact.
Host coverage is **866/866**; native persistence-frame field ABI and complete
MorphOS differential parity remain progressive.

Application settings headers now resolve MagicValue, VersionValue, RecordCount,
and PayloadBytes through the named `MuiApplicationSettingsHeaderFieldCursor`
and `MuiApplicationSettingsHeaderFieldCursorCodec` helpers. The typed 16-byte
header, mapping/overflow checks, and existing settings-file behavior remain
intact. Host coverage is **867/867**; native settings-header field ABI and
complete MorphOS differential parity remain progressive.

Application settings records now resolve Key and Length through the named
`MuiApplicationSettingsRecordFieldCursor` and
`MuiApplicationSettingsRecordFieldCursorCodec` helpers. The typed 8-byte
record, mapping/overflow checks, and existing settings-file behavior remain
intact. Host coverage is **868/868**; native settings-record field ABI and
complete MorphOS differential parity remain progressive.

Dataspace IFF entry headers now resolve Id and Length through the named
`MuiDataspaceIffEntryHeaderFieldCursor` and
`MuiDataspaceIffEntryHeaderFieldCursorCodec` helpers. The typed 8-byte header,
mapping/overflow checks, and existing IFF streaming behavior remain intact.
Host coverage is **869/869**; native IFF entry-header field ABI and complete
MorphOS differential parity remain progressive.

Dataspace WriteIFF messages now resolve MethodId, Handle, Type, and Id through
the named `MuiDataspaceWriteIffFieldCursor` and
`MuiDataspaceWriteIffFieldCursorCodec` helpers. The typed 16-byte packet,
method validation, mapping/overflow checks, and existing IFF behavior remain
intact. Host coverage is **870/870**; native WriteIFF message-field ABI and
complete MorphOS differential parity remain progressive.

Dataspace ReadIFF messages now resolve MethodId and Handle through the named
`MuiDataspaceReadIffFieldCursor` and
`MuiDataspaceReadIffFieldCursorCodec` helpers. The typed 8-byte packet, method
validation, mapping/overflow checks, and existing IFF behavior remain intact.
Host coverage is **871/871**; native ReadIFF message-field ABI and complete
MorphOS differential parity remain progressive.

The shared Dataspace IFF method word now resolves through the named
`MuiDataspaceIffMethodFieldCursor` and
`MuiDataspaceIffMethodFieldCursorCodec` helpers. Method-header decoding,
mapping checks, and existing IFF behavior remain intact. Host coverage is
**872/872**; native IFF method-field ABI and complete MorphOS differential
parity remain progressive.

The shared Store/Dataspace record now resolves Next, Key, Data, Length, Flags,
and Generation through the named `MuiStoreRecordFieldCursor` and
`MuiStoreRecordFieldCursorCodec` helpers. The typed 24-byte record,
mapping/overflow checks, and existing Datamap/Objectmap behavior remain
intact. Host coverage is **873/873**; native Store/Dataspace record-field ABI
and complete MorphOS differential parity remain progressive.

The fixed 32-byte headless-state record now resolves `Magic`, `Version`,
`Classes`, `Objects`, `NextSequence`, `NotifyDepth`, `Mutation`, and `Reserved`
through the named `MuiHeadlessStateFieldCursor` and
`MuiHeadlessStateFieldCursorCodec` helpers. Class/object registry and state
semantics, mapping checks, and overflow rejection remain intact. Host coverage
is **874/874**; native headless-state record-field ABI and complete MorphOS
differential parity remain progressive.

The fixed 28-byte headless-class registry record now resolves `Next`, `Name`,
`Boopsi`, `Super`, `InstanceSize`, `Reserved`, `Flags`, and `ObjectCount`
through the named `MuiHeadlessClassFieldCursor` and
`MuiHeadlessClassFieldCursorCodec` helpers, preserving the mixed UWORD/ULONG
ABI. Class registry semantics, mapping checks, and overflow rejection remain
intact. Host coverage is **875/875**; native headless-class record-field ABI
and complete MorphOS differential parity remain progressive.

The fixed 64-byte headless-object record now resolves all sixteen pointer and
scalar fields through the named `MuiHeadlessObjectFieldCursor` and
`MuiHeadlessObjectFieldCursorCodec` helpers. Object topology, semaphore,
notification, and Store/Dataspace semantics, mapping checks, and overflow
rejection remain intact. Host coverage is **876/876**; native headless-object
record-field ABI and complete MorphOS differential parity remain progressive.

The fixed 16-byte headless-attribute record now resolves `Next`, `Id`, `Value`,
and `Generation` through the named `MuiHeadlessAttributeFieldCursor` and
`MuiHeadlessAttributeFieldCursorCodec` helpers. Attribute mutation and
notification semantics, mapping checks, and overflow rejection remain intact.
Host coverage is **877/877**; native headless-attribute record-field ABI and
complete MorphOS differential parity remain progressive.

The fixed 16-byte Family child-list record now resolves `Next`, `Previous`,
`Object`, and `Owner` through the named `MuiHeadlessChildFieldCursor` and
`MuiHeadlessChildFieldCursorCodec` helpers. Family topology and mutation
semantics, mapping checks, and overflow rejection remain intact. Host coverage
is **878/878**; native headless-child record-field ABI and complete MorphOS
differential parity remain progressive.

The fixed 32-byte headless notification header now resolves all eight fields
through the named `MuiHeadlessNotificationFieldCursor` and
`MuiHeadlessNotificationFieldCursorCodec` helpers while retaining the bounded
payload cursor. Notification sequencing, trigger, destination, follow, and
payload semantics, mapping checks, and overflow rejection remain intact. Host
coverage is **879/879**; native notification-header record-field ABI and
complete MorphOS differential parity remain progressive.

The shared four-byte `MuiGuestUlongStorage.Value` result slot now resolves
through `MuiGuestUlongStorageFieldCursor` and
`MuiGuestUlongStorageFieldCursorCodec` across specialist, external-wrapper,
common-control, and notification publication paths. Mapping checks and
overflow rejection remain intact. Host coverage is **880/880**; native
guest-ULONG storage ABI and complete MorphOS differential parity remain
progressive.

The shared 8-byte ASL `MuiAslTagItemRecord` now resolves `Tag` and `Data`
through `MuiAslTagItemFieldCursor` and `MuiAslTagItemFieldCursorCodec` while
retaining vector traversal and control-tag semantics. Even-address and mapping
checks remain intact. Host coverage is **881/881**; native ASL TagItem field
ABI and complete MorphOS differential parity remain progressive.

The 12-byte `MuiMinMaxValues` layout record now resolves all six signed UWORD
geometry fields through `MuiMinMaxFieldCursor` and
`MuiMinMaxFieldCursorCodec`, preserving AskMinMax semantics. Mapping checks and
overflow rejection remain intact. Host coverage is **882/882**; native MinMax
layout-field ABI and complete MorphOS differential parity remain progressive.

The fixed 36-byte application-command descriptor now resolves all nine fields
through `MuiApplicationCommandFieldCursor` and
`MuiApplicationCommandFieldCursorCodec` while retaining NULL-terminated table
validation. Mapping checks, signed parameter/reserved values, and overflow
rejection remain intact. Host coverage is **883/883**; native
application-command descriptor ABI and complete MorphOS differential parity
remain progressive.

The 20-byte AppMessage Exec node header now resolves all seven mixed-width
fields through `MuiAppMessageNodeFieldCursor` and
`MuiAppMessageNodeFieldCursorCodec`, preserving links, signed priority, and
message type/length semantics. Mapping checks and overflow rejection remain
intact. Host coverage is **884/884**; native AppMessage node-header ABI and
complete MorphOS differential parity remain progressive.

The fixed 8-byte `MuiWorkbenchArgumentRecord` now resolves `Lock` and `Name`
through `MuiWorkbenchArgumentFieldCursor` and
`MuiWorkbenchArgumentFieldCursorCodec` while retaining argument-vector
validation. BPTR/STRPTR semantics, mapping checks, and overflow rejection
remain intact. Host coverage is **885/885**; native Workbench-argument record
ABI and complete MorphOS differential parity remain progressive.

The fixed 86-byte AppMessage record body now resolves all nineteen mixed-width
fields through `MuiAppMessageFieldCursor` and
`MuiAppMessageFieldCursorCodec`, while node-header delegation remains intact.
Scalar, pointer, signed-coordinate, reserved-field, mapping, and overflow
behavior remain intact. Host coverage is **886/886**; native AppMessage
body-field ABI and complete MorphOS differential parity remain progressive.

The fixed MorphOS Area drag method packets now resolve their mixed-width
fields through `MuiAreaDragFieldCursor` and
`MuiAreaDragFieldCursorCodec`, keyed by packet kind. Method selectors,
pointer/value payloads, signed coordinates, packet-size checks, mapping checks,
and overflow behavior remain intact. Host coverage is **887/887**; native
Area drag packet ABI and complete MorphOS differential parity remain
progressive.

The fixed MorphOS List basic packets now resolve their fields through
`MuiCollectionBasicFieldCursor` and
`MuiCollectionBasicFieldCursorCodec`, keyed by packet kind. Method selectors,
positions, selection/storage values, packet-size checks, mapping checks, and
overflow behavior remain intact. Host coverage is **888/888**; native
Collection basic packet ABI and complete MorphOS differential parity remain
progressive.

The fixed MorphOS List advanced packets now resolve their fields through
`MuiCollectionAdvancedFieldCursor` and
`MuiCollectionAdvancedFieldCursorCodec`, keyed by packet kind. Method-group
validation, entry/position/column/pointer/pair/image payloads, packet-size
checks, mapping checks, and overflow behavior remain intact. Host coverage is
**889/889**; native Collection advanced packet ABI and complete MorphOS
differential parity remain progressive.

The fixed MorphOS collection surface packets now resolve their fields through
`MuiCollectionSurfaceFieldCursor` and
`MuiCollectionSurfaceFieldCursorCodec`, keyed by packet kind. Layout geometry,
storage/flags, IntuiMessage and signed MUI key payloads, attribute values,
packet-size checks, mapping checks, and overflow behavior remain intact. Host
coverage is **890/890**; native Collection surface packet ABI and complete
MorphOS differential parity remain progressive.

The fixed MorphOS List record packets now resolve their fields through
`MuiCollectionRecordFieldCursor` and
`MuiCollectionRecordFieldCursorCodec`, keyed by packet kind. Entry/pool,
display, compare, and hit-test payloads, method validation, packet-size
checks, mapping checks, and overflow behavior remain intact. Host coverage is
**891/891**; native Collection record packet ABI and complete MorphOS
differential parity remain progressive.

The fixed MorphOS List edit packets now resolve their fields through
`MuiCollectionEditFieldCursor` and
`MuiCollectionEditFieldCursorCodec`, keyed by packet kind. Signed row/column
conversion, entry/edit-object and mode payloads, method validation, packet-size
checks, mapping checks, and overflow behavior remain intact. Host coverage is
**892/892**; native Collection edit packet ABI and complete MorphOS
differential parity remain progressive.

The fixed MorphOS Listtree.mcc packets now resolve their fields through
`MuiListtreeFieldCursor` and `MuiListtreeFieldCursorCodec`, keyed by packet
kind. Pointer/value payloads, method-group validation, packet-size checks,
mapping checks, and overflow behavior remain intact. Host coverage is
**893/893**; native Listtree packet ABI and complete MorphOS differential
parity remain progressive.

The fixed MorphOS Dirlist/Volumelist packets now resolve their fields through
`MuiDirlistFieldCursor` and `MuiDirlistFieldCursorCodec`, keyed by packet kind.
Pointer/value payloads, method-group validation, packet-size checks, mapping
checks, and overflow behavior remain intact. Host coverage is **894/894**;
native Dirlist packet ABI and complete MorphOS differential parity remain
progressive.

The fixed MorphOS Process.mui/Slave.mui packets now resolve their fields
through `MuiProcessSpecialistFieldCursor` and
`MuiProcessSpecialistFieldCursorCodec`, keyed by packet kind.
Attribute/storage/value, signal, error, and dispatch payloads, method
validation, packet-size checks, mapping checks, and overflow behavior remain
intact. Host coverage is **895/895**; native Process/Slave packet ABI and
complete MorphOS differential parity remain progressive.

The fixed MorphOS pen/color specialist packets now resolve their fields through
`MuiColorSpecialistFieldCursor` and
`MuiColorSpecialistFieldCursorCodec`, keyed by packet kind.
Attribute/storage/value, pointer, and RGB payloads, method validation,
packet-size checks, mapping checks, and overflow behavior remain intact. Host
coverage is **896/896**; native color-specialist packet ABI and complete
MorphOS differential parity remain progressive.

The fixed MorphOS Popstring/Popobject/Popasl packets now resolve their fields
through `MuiPopSpecialistFieldCursor` and
`MuiPopSpecialistFieldCursorCodec`, keyed by packet kind.
Attribute/storage/value and close-result payloads, the tolerant method-only
close boundary, method validation, packet-size checks, mapping checks, and
overflow behavior remain intact. Host coverage is **897/897**; native Pop
specialist packet ABI and complete MorphOS differential parity remain
progressive.

The fixed MorphOS Menustrip/Menu/Menuitem packets now resolve their fields
through `MuiMenuSpecialistFieldCursor` and
`MuiMenuSpecialistFieldCursorCodec`, keyed by packet kind.
Attribute/storage/value, object-pointer, pair, and popup-coordinate payloads,
method validation, packet-size checks, mapping checks, and overflow behavior
remain intact. Host coverage is **898/898**; native menu-specialist packet ABI
and complete MorphOS differential parity remain progressive.

The fixed MorphOS `MUIP_GoActive`/`MUIP_GoInactive` packet now resolves its
method and flags through `MuiAreaActivationFieldCursor` and
`MuiAreaActivationFieldCursorCodec`, keyed by packet kind. Active/inactive
validation, packet-size checks, mapping checks, and overflow behavior remain
intact. Host coverage is **899/899**; native Area activation packet ABI and
complete MorphOS differential parity remain progressive.

The fixed MorphOS layout packets now resolve their fields through
`MuiLayoutFieldCursor` and `MuiLayoutFieldCursorCodec`, keyed by packet kind.
Storage, flags, geometry, reserved words, text, length, and RenderInfo
payloads, method validation, packet-size checks, mapping checks, and overflow
behavior remain intact. Host coverage is **900/900**; native layout packet ABI
and complete MorphOS differential parity remain progressive.

The fixed MorphOS Misc specialist packets now resolve their fields through
`MuiMiscSpecialistFieldCursor` and
`MuiMiscSpecialistFieldCursorCodec`, keyed by packet kind.
Attribute/storage/value, pointer, pair, and register-gadget payloads,
lifecycle and method validation, packet-size checks, mapping checks, and
overflow behavior remain intact. Host coverage is **901/901**; native Misc
specialist packet ABI and complete MorphOS differential parity remain
progressive.

The fixed MorphOS common-control packets now resolve their fields through
`MuiCommonFieldCursor` and `MuiCommonFieldCursorCodec`, keyed by packet kind.
Signed values, geometry, input/event payloads, storage, attribute/value
payloads, method validation, packet-size checks, mapping checks, and overflow
behavior remain intact. Host coverage is **902/902**; native common-control
packet ABI and complete MorphOS differential parity remain progressive.

The fixed MorphOS Boopsi.mui/Dtpic.mui wrapper packets now resolve their fields
through `MuiExternalWrapperFieldCursor` and
`MuiExternalWrapperFieldCursorCodec`, keyed by packet kind.
Attribute-list, gadget-info, flags, attribute/storage/value, RenderInfo, and
geometry payloads, method validation, packet-size checks, mapping checks, and
overflow behavior remain intact. Host coverage is **903/903**; native
external-wrapper packet ABI and complete MorphOS differential parity remain
progressive.

The fixed MorphOS Dataspace packets now resolve their fields through
`MuiDataspaceFieldCursor` and `MuiDataspaceFieldCursorCodec`, keyed by packet
kind. Data, signed length, ID, size-storage, and Dataspace pointer payloads,
method validation, packet-size checks, mapping checks, and overflow behavior
remain intact. Host coverage is **904/904**; native Dataspace packet ABI and
complete MorphOS differential parity remain progressive.

Boopsi OpSet, OpGet, Render, TagItem, and result workspace fields now resolve
through the semantic packet-field cursor. Method selectors, payloads, record
sizes, mapping validation, and overflow behavior remain intact. Host coverage
is **861/861**; native workspace packet ABI and complete MorphOS differential
parity remain progressive.

ExternalWrapper notification Attribute, Value, and Count now resolve through
the named notification-field cursor. Recording/query semantics, mapping
validation, and overflow behavior remain intact. Host coverage is **860/860**;
native notification-field ABI and complete MorphOS differential parity remain
progressive.

ExternalWrapper shared scratch ownership now resolves through the named
scratch-field cursor. RememberBuffer, RememberCount, WorkBuffer, mapping
validation, and overflow behavior remain intact. Host coverage is **859/859**;
native scratch-field ABI and complete MorphOS differential parity remain
progressive.

Dtpic sidecar ownership, attribute, and dimension fields now resolve through
the named Dtpic state-field cursor while retaining the typed state record. The
nine fields, mapping validation, and overflow behavior remain intact. Host
coverage is **858/858**; native Dtpic state-field ABI and complete MorphOS
differential parity remain progressive.

Boopsi resource ownership/input pointers now resolve through the named
resource-field cursor while retaining the typed resource record. All five
pointers, full-record mapping validation, and overflow behavior remain intact.
Host coverage is **857/857**; native resource-field ABI and complete MorphOS
differential parity remain progressive.

Boopsi geometry/configuration fields now resolve through the named geometry
field cursor while retaining the typed state record. All Min/Max and tag
values, full-record mapping validation, and overflow behavior remain intact.
Host coverage is **856/856**; native geometry-field ABI and complete MorphOS
differential parity remain progressive.

ExternalWrapper Magic, Class, and Flags header words now resolve through the
named header-field cursor. Cookie/class validation, lifecycle flag updates,
the 12-byte mapping contract, and overflow behavior remain intact. Host
coverage is **855/855**; native header-field ABI and complete MorphOS
differential parity remain progressive.

ExternalWrapper stored display state now composes named display-environment and
RastPort records. The 12-byte environment span, 4-byte RastPort slot, mapping
checks, and setup/cleanup behavior remain intact. Host coverage is **854/854**;
native display-record ABI and complete MorphOS differential parity remain
progressive.

Dtpic picture-layout dimensions now decode through the named
`MuiExternalDtpicLayoutResult` and codec. The 8-byte width/height record,
mapped-result validation, and failure-atomic picture publication remain intact.
Host coverage is **853/853**; native Dtpic layout-result ABI and complete
MorphOS differential parity remain progressive.

ExternalWrapper MUIM_Setup now decodes the optional four-pointer `MUI_RenderInfo`
input through the named `MuiExternalRenderInfoRecord` and codec. The 16-byte
layout and permissive null/unmapped behavior remain intact. Host coverage is
**852/852**; native RenderInfo ABI and complete MorphOS differential parity
remain progressive.

Boopsi work-buffer inline TagItem/result resolution now uses the named
`MuiExternalWorkRegionCursor` and `MuiExternalWorkRegionCursorCodec` helpers.
The fixed 16-byte inline region, 40-byte mapping contract, shared packet/result
address, and overflow behavior remain intact. Host coverage is **851/851**;
native ExternalWrapper work-region ABI and complete MorphOS differential parity
remain progressive.

ExternalWrapper fixed Boopsi/Dtpic sidecar regions now resolve through the
named `MuiExternalStateCursor` and `MuiExternalStateCursorCodec` helpers. The
documented seven-region instance layout, mapping checks, and overflow behavior
remain intact. Host coverage is **850/850**; native ExternalWrapper region ABI
and complete MorphOS differential parity remain progressive.

Misc specialist fixed-region access now uses the named `MuiMiscStateCursor` and
`MuiMiscStateCursorCodec.TryGetAddress` helpers. Title, Filepanel, Mccprefs,
Scrmodelist, Window/Panel, Protection, and Fontdisplay state access retain the
documented instance layout through semantic regions with centralized overflow
checks. Host coverage is **839/839**; native Misc state-region ABI and complete
MorphOS differential parity remain progressive.

Misc specialist owned-string slot access now uses the named
`MuiMiscOwnedStringCursor` and `MuiMiscOwnedStringCursorCodec.TryGetAddress`
helpers. Keyadjust, Argstring, and Filepanel retain their sparse instance
layout through semantic fields with centralized overflow checks, while the
8-byte slot record and failure-atomic ownership remain intact. Host coverage is
**840/840**; native Misc owned-string ABI and complete MorphOS differential
parity remain progressive.

Application PushMethod parameter-span validation now uses the named
`MuiApplicationPushMethodParameterCursor` and
`MuiApplicationPushMethodParameterCursorCodec.TryGetEntry` helpers. The fixed
12-byte packet header, 4-byte parameter records, seven-entry bound, mapping,
and overflow checks remain intact. Host coverage is **841/841**; native
PushMethod tail ABI and complete MorphOS differential parity remain progressive.

CallHook param1 and variadic parameter access now use the named
`MuiCallHookParameterCursor` and `MuiCallHookParameterCursorCodec.TryGetEntry`
helpers. The 12-byte packet envelope, 4-byte parameter records, caller-owned
tail, mapping, and overflow checks remain intact. Host coverage is **842/842**;
native CallHook parameter ABI and complete MorphOS differential parity remain
progressive.

Notify follow-parameter and MultiSet target-vector bases now use the named
`MuiNotifyInlineVectorCursor` and `MuiNotifyInlineVectorCursorCodec.TryGetAddress`
helpers. Semantic vector kinds preserve the fixed message headers, 4-byte ULONG
entries, kind-specific boundaries, and overflow checks. Host coverage is
**843/843**; native Notify inline-vector ABI and complete MorphOS differential
parity remain progressive.

Window cycle-chain packet vector lookup now uses the named
`MuiWindowCycleChainInlineVectorCursor` and
`MuiWindowCycleChainInlineVectorCodec.TryGetEntry` helpers, layered over the
typed cycle-chain slot cursor. The fixed 8-byte header, 4-byte object slots,
NULL termination, mapping, and overflow checks remain intact. Host coverage is
**844/844**; native cycle-chain vector ABI and complete MorphOS differential
parity remain progressive.

Application window-node payload access now uses the named
`MuiApplicationWindowNodePayloadCursor` and
`MuiApplicationWindowNodePayloadCursorCodec.TryGetAddress` helpers. The
20-byte node record, Packet-field boundary, requested byte-count mapping, and
overflow checks remain intact. Host coverage is **845/845**; native window-node
payload ABI and complete MorphOS differential parity remain progressive.

Headless notification payload access now uses the named
`MuiHeadlessNotificationPayloadCursor` and
`MuiHeadlessNotificationPayloadCursorCodec.TryGetAddress` helpers. The fixed
32-byte record, variable payload count, total-range mapping, zero-length payload
support, and overflow checks remain intact. Host coverage is **846/846**;
native notification payload ABI and complete MorphOS differential parity remain
progressive.

SetAsString Value address lookup now uses the named
`MuiSetAsStringValueCursor` and `MuiSetAsStringValueCursorCodec.TryGetAddress`
helpers. The 16-byte packet record, 4-byte value field, formatter-facing guest
address, mapping, and overflow checks remain intact. Host coverage is
**847/847**; native SetAsString value ABI and complete MorphOS differential
parity remain progressive.

Process/Slave dispatch argument access now uses the named
`MuiProcessArgumentCursor` and `MuiProcessArgumentCursorCodec.TryGetEntry`
helpers, with semantic kinds for caller packets and generated method messages.
The 8-byte/4-byte headers, bounded 4-byte argument slots, mapping, and overflow
checks remain intact. Host coverage is **848/848**; native Process dispatch ABI
and complete MorphOS differential parity remain progressive.

Requester format parameter reads now use the named
`MuiRequesterParameterCursor` and `MuiRequesterParameterCursorCodec.TryGetEntry`
helpers. The 4-byte slot layout, 2048-entry bound, caller-owned storage,
mapping, and overflow checks remain intact. Host coverage is **849/849**;
native requester parameter ABI and complete MorphOS differential parity remain
progressive.

List selection and `NextSelected` caller-owned four-byte storage now use the
named `MuiListScalarStorageRecord` and `MuiListScalarStorageCodec`. Host
coverage is **782/782**; native list-storage ABI and differential parity remain
progressive.

List measured-width slots now use the named `MuiListColumnMetricValue` and
`MuiListColumnMetricCodec`; title construction and display-array paths reuse
the named pointer-slot codec and size boundary. Host coverage is **783/783**;
native List metric ABI and differential parity remain progressive.

`MuiListColumnMetricsState.Values` is now a named `APTR` field decoded by
`MuiListColumnMetricsStateCodec`; metric lookup, cleanup, and publication no
longer convert the state pointer ad hoc. Host coverage is **784/784**; native
List metrics-state ABI and differential parity remain progressive.

List edit-session `Entry` and `EditObject` fields now use named `APTR` members
through `MuiListEditStateCodec`. Host coverage is **785/785**; native List
edit-state ABI and differential parity remain progressive.

Dynamic `MUIM_UpdateConfig` redraw-table flag writes now use the named
`MuiUpdateConfigFlagSlot` and `MuiUpdateConfigFlagSlotCodec` alongside the
object slot. Host coverage is **759/759**; native UpdateConfig redraw-table
ABI and complete MorphOS differential parity remain progressive.

Group reorder/sort vector reads now resolve entries through the named
`MuiFamilyMutationVectorCursor` and
`MuiFamilyMutationVectorCodec.TryGetEntry` helpers. The 4-byte object-pointer
layout, NULL termination, traversal bound, and malformed-range rejection
remain intact. Host coverage is **822/822**; native Group ordering vector ABI
and complete MorphOS differential parity remain progressive.

Headless object creation now walks TAG_IGNORE, TAG_MORE, and TAG_SKIP through
the named `MuiAslTagItemCursor` and `MuiAslTagItemVectorCodec` helpers. The
8-byte TagItem layout, bounded traversal, skip-count overflow rejection, and
malformed-range behavior remain intact. Host coverage is **822/822**; native
headless tag-walk ABI and complete MorphOS differential parity remain
progressive.

Poplist caller-array traversal, materialized-copy/terminator writes, and
selection lookup now use the named `MuiPoplistArrayCursor` and
`MuiPoplistArrayCursorCodec.TryGetEntry` helpers. The 4-byte STRPTR-slot layout,
1024-entry source bound plus terminator, ownership behavior, and malformed-range
checks remain intact. Host coverage is **832/832**; native Poplist array ABI and
complete MorphOS differential parity remain progressive.

List TitleArray/StringArray pointer-table reads and writes now resolve slots
through the named `MuiListPointerSlotCursor` and
`MuiListPointerSlotCursorCodec.TryGetEntry` helpers. The 4-byte pointer-slot
layout, bounded column behavior, terminators, ownership/copy behavior, and
malformed-range rejection remain intact. Host coverage is **823/823**; native
List pointer-table ABI and complete MorphOS differential parity remain
progressive.

List entry-index reads, writes, and destruction now resolve addresses through
the named `MuiListSlotCursor` and `MuiListSlotCursorCodec.TryGetEntry` helpers.
The 8-byte entry/flags layout, one-million-entry bound, O(1) access, ownership
flags, and malformed-range rejection remain intact. Host coverage is **824/824**;
native List index-slot ABI and complete MorphOS differential parity remain
progressive.

StringArray materialization, comparison, ownership cleanup, and display-array
reads/writes now resolve bounded slots through the named
`MuiListPointerSlotCursor` and `MuiListPointerSlotCursorCodec.TryGetEntry`
helpers. The 4-byte pointer-slot layout, 256-entry bound plus terminator,
display-column behavior, and malformed-range rejection remain intact. Host
coverage is **825/825**; native StringArray display-table ABI and complete
MorphOS differential parity remain progressive.

Caller-supplied entry vectors for List Insert, SourceArray materialization,
and SortEntries now resolve slots through the named
`MuiListPointerVectorCursor` and `MuiListPointerVectorCursorCodec.TryGetEntry`
helpers. The 4-byte pointer-slot layout, one-million-entry bound, NULL
termination, failure-atomic behavior, and malformed-range rejection remain
intact. Host coverage is **826/826**; native caller-vector ABI and complete
MorphOS differential parity remain progressive.

Measured List column-width reads and refresh writes now resolve entries through
the named `MuiListColumnMetricCursor` and
`MuiListColumnMetricCursorCodec.TryGetEntry` helpers. The 4-byte ULONG metric
layout, 64-column geometry bound, width-refresh behavior, and malformed-range
rejection remain intact. Host coverage is **827/827**; native column-metric ABI
and complete MorphOS differential parity remain progressive.

FORMAT descriptor build, validation, lookup, and cleanup now resolve records
through the named `MuiListFormatDescriptorCursor` and
`MuiListFormatDescriptorCursorCodec.TryGetEntry` helpers. The 40-byte
descriptor layout, 256-column bound, ReadArgs parsing, ownership cleanup, and
malformed-range rejection remain intact. Host coverage is **828/828**; native
FORMAT descriptor ABI and complete MorphOS differential parity remain
progressive.

Public geometry projection and cached layout reads now resolve records through
the named `MuiListColumnGeometryCursor` and
`MuiListColumnGeometryCursorCodec.TryGetEntry` helpers. The 8-byte offset/width
layout, 64-column bound, layout caching, editor-placement behavior, and
malformed-range rejection remain intact. Host coverage is **829/829**; native
column-geometry ABI and complete MorphOS differential parity remain
progressive.

ColumnOrder source parsing, guest-owned comparison, cleanup, and display lookup
now resolve bytes through the named `MuiListColumnOrderByteCursor` and
`MuiListColumnOrderByteCursorCodec.TryGetEntry` helpers. The caller-facing
BYTE* permutation, 64-column bound, packed big-endian storage, identity
completion, duplicate/range validation, and malformed-range rejection remain
intact. Host coverage is **830/830**; native ColumnOrder ABI and complete
MorphOS differential parity remain progressive.

`MUIM_UpdateConfig` redraw-object and redraw-flag table reads and writes now
resolve slots through the named `MuiUpdateConfigObjectCursor`/
`MuiUpdateConfigObjectCursorCodec` and `MuiUpdateConfigFlagCursor`/
`MuiUpdateConfigFlagCursorCodec` helpers. The 332-byte packet, 64-entry tables,
named packet fields, and malformed-range rejection remain intact. Host coverage
is **831/831**; native UpdateConfig table ABI and complete MorphOS differential
parity remain progressive.

`MUIM_Slave_Dispatch` now crosses the named
`MuiProcessDispatchPacketHeader`/`MuiProcessDispatchPacketCodec` boundary.
The codec validates the bounded argument vector and exposes each argument as
a value, keeping raw guest offsets out of the live Process/Slave dispatch
logic. Host coverage is **737/737**; native packet ABI and complete MorphOS
differential parity remain progressive.

## NotifyWrite typed method headers

NotifyWrite Long and String packet decoding now admits scalar selectors through
`MuiNotifyWriteMessageCodec.TryReadMethodIdValue` before consuming the named
method, WriteLong, and WriteString records. Focused coverage is **1/1** and
the complete host suite is **1343/1343** in both SDK modes.
`NotifyWriteMethodHeaderCodecRoot` returns **42** after **482 instructions /
5,376 cycles**; MC68000/020/040 HUNK sizes are **1,668/1,664/1,664** bytes
with 8 reachable methods and zero-runtime map gates. Full native NotifyWrite
dispatch and MorphOS differential parity remain progressive.

## Dataspace and Dataspace-IFF typed method headers

Dataspace and Dataspace-IFF packet decoding now use their named method header
codecs before consuming typed Add/Find/Get/Merge/Remove/Clear and Read/Write
records. Host coverage is **732/732**; native Dataspace packet ABI and complete
MorphOS differential parity remain progressive.

## Layout typed method headers

Layout packet decoding now uses `MuiLayoutMethodMessage` before consuming
typed AskMinMax, Relayout, rectangle, text, render-info, flags, TextDimensions,
and Layout records. Focused coverage is **18/18**, and `LayoutPacketCodecRoot`
returns **42** at **5,628/5,716/5,736** bytes for MC68000/020/040 with all
zero-runtime gates passing. Complete native layout/render behavior and MorphOS
differential parity remain progressive.

## Listtree typed method headers

Listtree packet decoding now admits scalar selectors through
`MuiListtreeMessageCodec.TryReadMethodIdValue` before consuming typed set/get,
insert/remove, open/close, sorting, movement, rename, find, drop-mark, and
test-position records. Focused coverage is **1/1** and the complete host suite
is **1343/1343** in both SDK modes. `ListtreeMethodHeaderCodecRoot` returns
**42** after **476 instructions / 5,334 cycles**; MC68000/020/040 HUNK sizes
are **2,508/2,504/2,504** bytes with 8 reachable methods and zero-runtime map
gates. Full native Listtree dispatch and MorphOS differential parity remain
progressive.

When `MuiEventHandlerNode.Class` is non-null, the callback seam now follows the
MorphOS contract and invokes `CoerceMethod(Class, Object, message)` directly;
when it is null, it uses the normal `DoMethod(Object, message)` path. The
platform contract keeps this as an opaque named class pointer, with no managed
dispatcher or exception path. Host coverage is **575/575**; priority ordering,
active/default-object precedence, richer return-code propagation, and full
MorphOS event parity remain progressive.

Event-handler registration now decodes the signed MorphOS priority byte and
keeps the guest wrapper list in descending priority order with FIFO ties.
Window delivery checks the active object first, then the default object, and
then the remaining queue. This ordering uses bounded named-record passes with
no managed arrays or exception path. Host coverage is **576/576**; GUI-state
visibility checks, richer return-code propagation, and full MorphOS event
parity remain progressive.

## Collection surface method headers

Shared Layout, AskMinMax, Draw, HandleInput, and attribute packet checks now
consume `MuiCollectionBasicMessageCodec.TryReadMethodIdValue` before decoding
their complete named records. Focused coverage is **3/3** and the complete suite
remains **1342/1342** in both SDK modes.
`CollectionSurfaceMessageCodecRoot` returns **42** after **5,210/56,292**
instructions/cycles and emits **6,204/6,248/6,260** bytes for MC68000/020/040;
full native surface dispatch and MorphOS differential parity remain progressive.

## Collection-basic method header

Collection Clear and Sort packet decoding now uses the named
`MuiCollectionMethodMessage` codec before accepting the selector. Host
coverage is **726/726**; native collection-basic packet ABI and complete
MorphOS differential parity remain progressive.

## Dirlist/Volumelist typed method headers

Dirlist/Volumelist packet decoding now uses the named `MuiDirlistMethodMessage`
codec before consuming method-only, set, rename, protection, and get-entry
records. Host coverage is **727/727**; native Dirlist/Volumelist packet ABI
and complete MorphOS differential parity remain progressive.

Family reorder/sort now obtains each object-pointer vector entry through the
named `MuiFamilyMutationVectorCursor` and
`MuiFamilyMutationVectorCodec.TryGetEntry` helpers, preserving NULL
termination, ordering, bounded mapping, overflow checks, and malformed-range
rejection. Host coverage is **807/807**; native Family vector ABI and complete
MorphOS differential parity remain progressive.

## External-wrapper typed method headers

External-wrapper packet decoding now uses the named `MuiExternalMethodMessage`
codec before consuming method-only, set, render-info, and sized records. Host
coverage is **728/728**; native external-wrapper packet ABI and complete
MorphOS differential parity remain progressive.

## Menu-specialist typed method headers

MenuSpecialist packet decoding now uses the named
`MuiMenuSpecialistMethodMessage` codec before consuming method-only, set,
pointer, pair, popup, and sized records. Host coverage is **729/729**; native
MenuSpecialist packet ABI is qualified by MG558 at **6,256/6,288/6,292** bytes
for MC68000/020/040, with MC68000 returning **42** after **6,475/68,230**
instructions/cycles. Complete MorphOS differential parity remains progressive.

## Common-control typed method headers

Common-control signed, numeric, event, attribute, layout, draw, setup, and
OM_GET readers now route selector checks through the named common method header
before decoding typed records. Host coverage is **702/702**; MG559 qualifies
the packet ABI with MC68000/020/040 artifacts of **6,764/6,824/6,848** bytes,
returning **42** on MC68000 after **7,117/74,444** instructions/cycles.
Complete MorphOS differential parity remains progressive.

GUI-mode event-handler eligibility now uses the corrected MorphOS
`MUI_EHF_GUIMODE` flag value **0x0002**. Handlers targeting disabled objects,
objects with `MUIA_ShowMe == 0`, or objects whose internal `IsShown` state is
zero are skipped. ACTIVEWINDOW, INACTIVEWINDOW, and CHANGEWINDOW event classes
remain eligible as MorphOS exceptions; non-GUI handlers retain the normal
mask/object path. The gate uses named attributes and typed records without
exceptions, managed runtime, or raw handler-node offsets. Host coverage is
**577/577**; virtual-group ancestry and richer return-code propagation remain
progressive.

The typed event-handler callback now preserves the complete
`DoMethod`/`CoerceMethod` result. Window delivery stops only for the MorphOS
`MUI_EventHandlerRC_Eat` value **1**; other non-zero values remain observable
and do not prevent the remaining priority queue from running. Host coverage is
**578/578**; virtual-group ancestry and complete MorphOS event parity remain
progressive.

GUI-mode eligibility now follows the named `Parent` chain in each live
`MuiHeadlessObjectRecord`. Disabled, hidden, or not-shown ancestors suppress
delivery just like the target object, while window-state exception classes
remain eligible. The traversal is bounded and uses no managed collection or
exception path. Host coverage is **579/579**; full MorphOS group visibility
semantics remain progressive.

The typed window event walk now honors MorphOS `MUI_EHF_ALWAYSKEYS = 0x0001`.
For a named `MUIP_HandleEvent` packet, `MuiKey != MUIKEY_NONE` marks keyboard
delivery; inactive handlers are admitted only when their
`MuiEventHandlerNode.Flags` includes `ALWAYSKEYS`. Active/default-object passes
and non-key IDCMP events retain the ordinary route. The focused fixtures use
`MuiWindowEventHandlerPacketInput`, `MuiEventHandlerNodeInput`, and the named
HandleEvent codec rather than inventing raw handler-node records. Host coverage
is **580/580**; `WindowEventHandlerRoot` emits **52,972 / 60,004 / 56,364
bytes** for MC68000/020/040, reaches 164 methods, and MC68000 returns **42**
after **1,455,700 instructions / 15,256,342 cycles**. Full MorphOS key
routing, virtual-group, and differential event behavior remain progressive.

The typed window event walk now applies MorphOS page-mode visibility. While
walking the named `MuiHeadlessObjectRecord.Parent` chain, a parent with
`MUIA_Group_PageMode != 0` admits only its `MUIA_Group_ActivePage` child; an
inactive page and all descendants are skipped for GUI-mode callbacks. Page
membership uses bounded named family records and does not mutate caller
attributes or allocate managed state. Host coverage is **581/581**.
`WindowEventHandlerRoot` emits **54,796 / 62,148 / 58,312 bytes** for
MC68000/020/040, reaches 167 methods, and MC68000 returns **42** after
**1,974,452 instructions / 20,699,164 cycles** with zero relocations,
framework members/features, managed allocations, and runtime descriptors.
Full virtual-group clipping, page transition side effects, and complete
MorphOS differential event behavior remain progressive.

GUI-mode event delivery now also checks virtual-group visibility. The typed
gate walks named parent records, recognizes virtual groups from their named
`MUIA_Virtgroup_Width` and `MUIA_Virtgroup_Height` attributes, and compares the
target area rectangle with each viewport. Targets outside a viewport are
skipped; missing geometry remains permissive until layout supplies it. The
host suite is **582/582**. `WindowEventHandlerRoot` emits **56,368 / 64,040 /
60,028 bytes** for MC68000/020/040, reaches 172 methods, and MC68000 returns
**42** after **1,985,695 instructions / 20,817,494 cycles**. The dedicated
`WindowEventHandlerVirtualGroupRoot` emits **52,332 / 59,564 / 55,928 bytes**,
reaches 171 methods, and returns **42** after **411,164 instructions /
4,321,962 cycles**. Full clipping-region propagation, scrolling, and complete
MorphOS differential event behavior remain progressive.

The typed window event walk also recognizes MorphOS `MUI_EHF_PRIORITY = 0x0800`.
Priority handlers are represented by the named application-window wrapper
record, kept in a leading partition, ordered by signed priority with FIFO ties,
and dispatched before active/default routing. A non-eating priority callback is
not visited again by later passes. Host coverage is **583/583**;
`WindowEventHandlerRoot` emits **57,444 / 65,156 / 61,172 bytes** and returns
**42** after **2,033,249 instructions / 21,317,924 cycles**, while the focused
`WindowEventHandlerPriorityRoot` emits **52,664 / 59,892 / 56,256 bytes** and
returns **42** after **236,992 instructions / 2,491,248 cycles**. The internal
`ISACTIVEGRP`, `ISACTIVE`, `ISCALLING`, and `ISENABLED` flag transitions still
require later compatibility goals.

The current MG09 event-routing slice adds struct-first active-parent keyboard
delivery. A typed `MUIP_HandleEvent` key walks the active object's named
`MuiHeadlessObjectRecord.Parent` chain before the default object, stops if the
active object changes during delivery, and excludes visited ancestors from the
remaining queue. The default pass treats only `MUI_EventHandlerRC_Eat == 1` as
terminal; non-eat method results continue. Host coverage is **584/584**. The
standard, priority, and active-parent native closures return **42** under the
freestanding M68000 profile with zero framework members, managed allocations,
relocations, and descriptors. Full MorphOS key-routing parity remains open.

The event-handler callback boundary now maintains the MorphOS
`MUI_EHF_ISCALLING = 0x4000` bit directly in the named guest
`MuiEventHandlerNode`. It is set before `DoMethod`/`CoerceMethod`, then cleared
after a struct re-read while preserving callback changes to the other fields.
The host probe observes the transient state and the native
`WindowEventHandlerCallingRoot` returns **42** under the freestanding profile.
Internal `ISACTIVE*`/`ISENABLED` transitions and automatic
`MUIA_HandledEvents` registration remain progressive.

After this callback-state update, the focused MC68000 event-route closures
remain zero-runtime-gate clean: standard **58,588 bytes**, priority **53,808**,
active-parent **54,044**, and calling-state **46,168**; each returns **42** in
the native harness.

The named event-handler record also tracks MorphOS
`MUI_EHF_ISENABLED = 0x8000`: accepted registration sets it, explicit removal
clears it, and window cleanup clears it when releasing the wrapper list.
Rejected insertion restores the caller's original flags. The focused enabled
state closure returns **42** on MC68000/020/040 qualification.

After this state update, the focused MC68000 routes requalify at **59,392**
(standard), **54,612** (priority), **54,848** (active-parent), **46,724**
(calling), and **47,792** (enabled) bytes; each returns **42** in the native
harness.

The named event-handler record now also maintains MorphOS
`MUI_EHF_ISACTIVE = 0x2000`. At registration and bounded dispatch boundaries,
the typed path derives the read-only bit from the window's active and default
object attributes; active-object transitions therefore become visible before
delivery. Explicit removal and cleanup clear both `ISACTIVE` and `ISENABLED`.
Host coverage is **587/587**. The focused active-state native root emits
**56,164 / 63,900 / 60,084 bytes** for MC68000/020/040 and returns **42** after
**396,395 instructions / 4,156,876 cycles** on MC68000. The implementation
remains freestanding, exception-free, managed-runtime-free, and struct-first;
`ISACTIVEGRP`, automatic `MUIA_HandledEvents` registration, and full MorphOS
differential event-handler behavior remain progressive.

Final focused MC68000 requalification after the typed dispatch cleanup emits
**60,236 / 55,452 / 55,692 / 46,776 / 48,568 / 56,148 bytes** for the
standard, priority, active-parent, calling, enabled, and active-state roots;
all six return **42** in the native harness and remain zero-runtime-gate clean.

The public `MUIA_Window_DisableKeys` mask is now honored at the typed
`MUIP_HandleEvent` boundary. A set bit suppresses that non-negative MUI key
before event-handler routing; `MUIKEY_NONE`, non-key packets, and out-of-range
synthetic values retain the existing route. Host coverage is **588/588**.
`WindowEventHandlerDisableKeysRoot` emits **55,704 / 63,268 / 59,508 bytes**
for MC68000/020/040 and returns **42** after **208,024 instructions /
2,188,318 cycles** on MC68000. Private `ISACTIVEGRP`, automatic
`MUIA_HandledEvents`, and complete MorphOS differential behavior remain
progressive.

The typed public window setter now covers `MUIA_Window_DefaultObject`. It
accepts a live object or `NULL`, rejects unknown guest targets without
mutation, and immediately refreshes `MUI_EHF_ISACTIVE` in the named event
handler records. Host coverage is **589/589**. The focused
`WindowEventHandlerDefaultObjectRoot` emits **50,424 / 57,272 / 53,856 bytes**
for MC68000/020/040, reaches 162 methods, and returns **42** after **298,690
instructions / 3,137,130 cycles** on MC68000. The MC68000 report is
zero-runtime and zero-relocation clean; the 68020/040 closure maps retain
**13 / 0 relocations**. Private `ISACTIVEGRP`, automatic
`MUIA_HandledEvents`, and full MorphOS differential behavior remain
progressive.

The typed public window setter now covers `MUIA_Window_Activate` through both
`Set` and `NoNotifySet`. TRUE requires an open native window and successful
platform activation; FALSE is a no-op. Host coverage is **590/590**. The
focused `WindowActivateRoot` emits **44,592 / 50,604 / 47,672 bytes** for
MC68000/020/040, reaches 149 methods, and returns **42** after **113,845
instructions / 1,197,222 cycles** on MC68000. All focused maps are
zero-runtime and zero-relocation clean. Additional MorphOS window attributes
remain progressive.

The typed public window setter now covers `MUIA_Window_Sleep`. Non-zero writes
nest the sleep counter, zero writes wake one level, the prior `MUIA_Disabled`
state is restored after the final wake, and window events are suppressed while
the depth is nonzero. Host coverage is **591/591**. `WindowSleepRoot` emits
**52,196 / 59,264 / 55,672 bytes** for MC68000/020/040, reaches 165 methods,
and returns **42** after **148,196 instructions / 1,567,420 cycles** on
MC68000. Focused maps have zero framework features and managed allocations;
busy-pointer forwarding remains a platform-contract follow-up.

The typed `MUIA_Window_Sleep` path now exposes the MorphOS busy-pointer effect
through `IMuiApplicationPlatform.SetMuiWindowBusy`. It balances the capability
at the outermost sleep/final wake boundary, releases it during close, and
replays it when a sleeping object opens. Host coverage is **591/591**. The
focused native root emits **53,548 / 60,676 / 57,004 bytes** for
MC68000/020/040, with MC68000 execution returning **42** after **204,774
instructions / 2,165,920 cycles**. The implementation remains freestanding,
exception-free, managed-runtime-free, and struct-first.

The typed `MUIA_Application_Sleep` path now implements the MorphOS nested
application sleep contract. It adjusts each owned window's named sleep depth,
suppresses application input handlers while nonzero, and applies the full
depth to windows added during sleep. Host coverage is **592/592**. The focused
native root emits **56,124 / 63,676 / 59,660 bytes** for MC68000/020/040, with
MC68000 execution returning **42** after **761,510 instructions / 8,038,202
cycles**. The implementation remains freestanding, exception-free,
managed-runtime-free, and struct-first.

The typed `MUIA_Application_Iconified` path now follows the MorphOS
iconification contract. It closes currently native owned windows while
retaining a named guest reopen marker, defers `OpenWindow` requests made while
iconified, and restores all remembered windows after uniconification. Host
coverage is **593/593**. `ApplicationIconifiedRoot` emits **48,560 / 55,104 /
51,672 bytes** for MC68000/020/040 and returns **42** after **830,581
instructions / 8,754,704 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first.

The typed `MUIA_Application_Active` path now canonicalizes commodities-facing
BOOL writes: any non-zero guest value is stored as MorphOS TRUE and zero as
FALSE. The value remains named guest state; MUI itself performs no external
action for this attribute. Host coverage is **594/594**. `ApplicationActiveRoot`
emits **43,452 / 49,488 / 46,532 bytes** for MC68000/020/040, reaches 142
methods, and returns **42** after **113,063 instructions / 1,184,588 cycles**
on MC68000. The implementation remains freestanding, exception-free,
managed-runtime-free, and struct-first. See the [MorphOS MUI Application
documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Window_VisibleOnMaximize` path now uses a named mutable
MorphOS BOOL record. `Set` and `NoNotifySet` share the focused
`DispatchWindowVisibleOnMaximize` packet seam, and every non-zero value is
canonicalized to TRUE. Maximize presentation remains a platform capability.
Host coverage is **623/623**. `WindowVisibleOnMaximizeRoot` emits **53,248 /
60,720 / 56,964 bytes** for MC68000/020/040, reaches 169 methods, and returns
**42** after **67,488 instructions / 706,114 cycles** on MC68000. The
implementation remains freestanding, exception-free, managed-runtime-free,
and struct-first; packet fields cross named records rather than raw handler
offsets. See the [MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_IsSubWindow` path now uses an initializer-only named
MorphOS BOOL record. Creation tags canonicalize non-zero values to TRUE;
later `Set`/`NoNotifySet` writes are rejected. During guest-family disposal,
flagged windows are detached and retained while ordinary children are disposed
normally. Host coverage is **624/624**. `WindowIsSubWindowRoot` emits **53,976 /
61,632 / 57,688 bytes** for MC68000/020/040, reaches 170 methods, and returns
**42** after **170,468 instructions / 1,790,440 cycles** on MC68000. The
implementation remains freestanding, exception-free, managed-runtime-free,
and struct-first; named packet records and guest object flags carry the policy
without raw handler offsets. See the [MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_RefWindow` path now retains a live MUI window target in
named state for relative placement. Self-references and non-live pointers are
rejected without mutation; coordinate calculation remains a platform seam.
Host coverage is **622/622**. `WindowRefWindowRoot` emits **53,448 / 60,968 /
57,180 bytes** for MC68000/020/040, reaches 169 methods, and returns **42**
after **124,581 instructions / 1,308,384 cycles** on MC68000. The
implementation remains freestanding, exception-free, managed-runtime-free,
and struct-first; packet fields cross named records rather than raw handler
offsets. See the [MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The public `MUIA_Window_Window` getter now exposes the opaque native window
pointer from the named lifecycle record. It returns NULL before opening and
after closing, rejects writes, and never mirrors or owns the platform object.
Host coverage is **611/611**. `WindowWindowRoot` emits **51,484 / 58,572 /
54,836 bytes** for MC68000/020/040, reaches 169 methods, and returns **42**
after **121,853 instructions / 1,282,288 cycles** on MC68000. The implementation
remains freestanding, exception-free, managed-runtime-free, and struct-first.
See the [MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_ID` path now stores a mutable ULONG identity in the
ordinary named attribute record. Both `Set` and `NoNotifySet` reach the same
named packet route, so Snapshot and other consumers observe the guest value
without a managed mirror or positional state offsets. Host coverage is
**612/612**. `WindowIdRoot` emits **51,244 / 58,320 / 54,596 bytes** for
MC68000/020/040, reaches 166 methods, and returns **42** after **64,697
instructions / 677,500 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first. See the
[MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_Screen` path now retains an explicit guest `Screen *`
selection in named window state. The getter follows the MorphOS lifecycle
contract: it returns NULL while closed and exposes the selected pointer only
after `OpenWindow`; unmapped pointers are rejected without mutation. Host
coverage is **621/621**. `WindowScreenRoot` emits **54,464 / 61,968 / 58,096
bytes** for MC68000/020/040, reaches 175 methods, and returns **42** after
**140,494 instructions / 1,478,186 cycles** on MC68000. The implementation
remains freestanding, exception-free, managed-runtime-free, and struct-first;
packet fields cross named records rather than raw handler offsets. See the
[MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_PublicScreen` path now retains the caller-owned guest
screen-name C-string in named window state. Bounded strings are validated in
place, NULL clears the value, and malformed pointers are rejected without
managed copies. Public-screen lookup remains a platform capability. Host
coverage is **620/620**. `WindowPublicScreenRoot` emits **52,832 / 60,196 /
56,432 bytes** for MC68000/020/040, reaches 168 methods, and returns **42**
after **72,916 instructions / 763,328 cycles** on MC68000. The implementation
remains freestanding, exception-free, managed-runtime-free, and struct-first;
packet fields cross named records rather than raw handler offsets. See the
[MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_ScreenTitle` path now retains the caller-owned guest
C-string pointer in named window state. Bounded strings are validated in place,
NULL clears the value, and malformed pointers are rejected without managed
copies. Host coverage is **619/619**. `WindowScreenTitleRoot` emits
**52,788 / 60,132 / 56,376 bytes** for MC68000/020/040, reaches 168 methods,
and returns **42** after **72,790 instructions / 762,312 cycles** on MC68000.
The implementation remains freestanding, exception-free, managed-runtime-
free, and struct-first; packet fields cross named records rather than raw
handler offsets. See the [MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_CloseRequest` path now uses a named mutable BOOL shared
by event polling, `Set`, `NoNotifySet`, and `Get`. Every non-zero write is
canonicalized to TRUE, so close-gadget publication and caller acknowledgement
observe one guest-resident value without managed shadow state or positional
offsets. Host coverage is **613/613**. `WindowCloseRequestRoot` emits
**51,296 / 58,376 / 54,672 bytes** for MC68000/020/040, reaches 166 methods,
and returns **42** after **66,055 instructions / 691,390 cycles** on MC68000.
The implementation remains freestanding, exception-free,
managed-runtime-free, and struct-first. See the [MorphOS MUI Window
documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_TabletMessages` path now uses an initializer-only
named MorphOS BOOL record. Creation tags canonicalize non-zero values to TRUE;
later `Set`/`NoNotifySet` writes are rejected. `OpenWindow` forwards the named
state through the explicit `SetMuiWindowTabletMessages` platform capability.
Host coverage is **625/625**. `WindowTabletMessagesRoot` emits **55,576 /
63,352 / 59,388 bytes** for MC68000/020/040, reaches 178 methods, and returns
**42** after **127,958 instructions / 1,344,152 cycles** on MC68000. The
implementation remains freestanding, exception-free, managed-runtime-free,
and struct-first; the ABI crosses named records rather than raw offsets. See
the [MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_UseBottomBorderScroller`,
`MUIA_Window_UseLeftBorderScroller`, and `MUIA_Window_UseRightBorderScroller`
paths now use mutable named MorphOS BOOL records. Creation tags and later
`Set`/`NoNotifySet` writes canonicalize non-zero values to TRUE; updates to an
open window forward the complete policy through the single typed
`SetMuiWindowBorderScrollers` platform capability. Host coverage is
**626/626**. `WindowBorderScrollersRoot` emits **57,232 / 65,180 / 61,048
bytes** for MC68000/020/040, reaches 181 methods, and returns **42** after
**192,366 instructions / 2,023,506 cycles** on MC68000. The implementation
remains freestanding, exception-free, managed-runtime-free, and struct-first;
the ABI crosses named records rather than raw offsets. See the [MorphOS MUI
Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

## String Accept/Reject filter state

The MorphOS `MUIA_String_Accept` and `MUIA_String_Reject` attributes are
exposed through the named `MuiStringFilterState` record. Each caller-owned
`[ISG]` STRPTR is checked as a bounded guest C string at construction and on
runtime `Set`/`NoNotifySet`; the original pointer is retained, with no managed
copy or positional state offset. `StringAllowsCodePoint` consumes this same
record for both byte-mode and Unicode UTF-8 filtering. Host coverage is
**639/639**; native focused qualification and complete MorphOS String parity
remain progressive.

The typed `MUIA_Window_AltHeight`, `MUIA_Window_AltWidth`,
`MUIA_Window_AltLeftEdge`, and `MUIA_Window_AltTopEdge` paths now use one
named signed-LONG geometry record. Creation tags preserve the caller's values;
later writes are rejected as initializer-only attributes. `OpenWindow` forwards
the record through `ConfigureMuiWindowAlternateGeometry`. Host coverage is
**627/627**. `WindowAlternateGeometryRoot` emits **57,124 / 65,044 / 60,936
bytes** for MC68000/020/040, reaches 180 methods, and returns **42** after
**175,860 instructions / 1,845,064 cycles** on MC68000. The implementation
remains freestanding, exception-free, managed-runtime-free, and struct-first;
the ABI crosses a named geometry record rather than raw offsets. See the
[MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_Height`, `MUIA_Window_Width`,
`MUIA_Window_LeftEdge`, and `MUIA_Window_TopEdge` paths now use one named
signed-LONG geometry record. Creation tags preserve the caller's values;
later writes are rejected as initializer-only attributes. `OpenWindow` forwards
the record through `ConfigureMuiWindowGeometry`. Host coverage is **628/628**.
`WindowGeometryRoot` emits **56,560 / 64,492 / 60,424 bytes** for
MC68000/020/040, reaches 177 methods, and returns **42** after **187,242
instructions / 1,965,090 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first; the ABI
crosses a named geometry record rather than raw offsets. See the [MorphOS MUI
Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed initializer-only gadget policy now covers
`MUIA_Window_CloseGadget`, `MUIA_Window_DepthGadget`, `MUIA_Window_DragBar`,
`MUIA_Window_SizeGadget`, and `MUIA_Window_SizeRight` in one named ULONG
record. Creation tags canonicalize non-zero values; later writes are rejected.
`OpenWindow` forwards the policy through `ConfigureMuiWindowGadgets`. Host
coverage is **629/629**. `WindowGadgetPolicyRoot` emits **57,432 / 65,452 /
61,340 bytes** for MC68000/020/040, reaches 178 methods, and returns **42**
after **213,804 instructions / 2,242,524 cycles** on MC68000. The
implementation remains freestanding, exception-free, managed-runtime-free,
and struct-first; the ABI crosses one named policy record rather than raw
offsets. See the [MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_RootObject` path now uses the guest-resident Family
child relationship as its named state. Replacing a root releases the previous
child relationship, clearing removes it, and invalid or already-parented
objects are rejected without a managed object graph or positional offsets.
Host coverage is **614/614**. `WindowRootObjectRoot` emits **52,624 / 60,008 /
56,132 bytes** for MC68000/020/040, reaches 168 methods, and returns **42**
after **196,311 instructions / 2,070,466 cycles** on MC68000. The
implementation remains freestanding, exception-free, managed-runtime-free,
and struct-first. See the [MorphOS MUI Window
documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Application_WindowList` path now exposes a read-only,
guest-resident Exec `List` projection of application-owned windows. Named
`MuiApplicationWindowListState` and `MuiApplicationWindowListEntry` structs
keep list ownership and traversal in guest memory, filter out unrelated Family
children, and rebuild after topology mutation. Host coverage is **608/608**.
`ApplicationWindowListRoot` emits **48,432 / 55,224 / 51,720 bytes** for
MC68000/020/040, reaches 155 methods, and returns **42** after **414,850
instructions / 4,354,368 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first. See the
[MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Application_Commands` path now validates and retains a
caller-owned, NUL-terminated MorphOS `MUI_Command` table through the named
`MuiApplicationCommandRecord` codec. Each command name and optional template is
validated as a bounded guest C string (or accepted as the documented
`MC_TEMPLATE_ID` sentinel), and malformed tables leave the previous pointer
unchanged. Host coverage is **609/609**. `ApplicationCommandsRoot`
emits **49,732 / 56,524 / 52,988 bytes** for MC68000/020/040, reaches 160
methods, and returns **42** after **72,174 instructions / 751,584 cycles** on
MC68000. The implementation remains freestanding, exception-free,
managed-runtime-free, and struct-first; ARexx transport and hook execution are
separate capability work.

The typed `MUIA_Application_SingleTask` and `MUIA_Application_DoubleStart`
paths now implement the MorphOS single-task lifecycle contract. A TRUE
single-task initializer claims a guest-resident application slot; a conflicting
initializer is rejected and sets `DoubleStart` on the live application. The
initializer cannot be changed after application initialization. Host coverage
is **595/595**. `ApplicationSingleTaskRoot` emits **45,208 / 51,484 / 48,352
bytes** for MC68000/020/040, reaches 145 methods, and returns **42** after
**435,682 instructions / 4,576,170 cycles** on MC68000. The implementation
remains freestanding, exception-free, managed-runtime-free, and struct-first.
See the [MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Application_DropObject` path now retains the mutable,
caller-owned pointer to a live MUI object that receives iconified-app
AppMessages. `Set` and `NoNotifySet` validate object identity, accept NULL to
clear the value, and reject invalid objects without mutation. Message delivery
remains a separate platform capability. Host coverage is **604/604**.
`ApplicationDropObjectRoot` emits **43,272 / 49,296 / 46,364 bytes** for
MC68000/020/040, reaches 142 methods, and returns **42** after **111,568
instructions / 1,172,426 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first. See the
[MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Window_AppWindow`, `MUIA_ApplicationObject`, and
`MUIA_AppMessage` paths now provide the first AppWindow transport slice.
`MUIA_Window_AppWindow` is a named BOOL on `Window.mui` and becomes immutable
after opening. `MUIA_ApplicationObject` resolves the initialized application
through the guest parent chain. `MUIA_AppMessage` is getter-only and transient:
`DispatchAppMessage` validates a named `MuiAppMessageRecord` plus its
`MuiWorkbenchArgumentRecord` array, publishes it while notifications run, and
restores the prior value immediately afterward. Host coverage is **610/610**.
`AppMessageRoot` emits **59,388 / 67,460 / 63,376 bytes** for
MC68000/020/040, reaches 186 methods, and returns **42** after **453,276
instructions / 4,755,618 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first; raw
offsets are confined to the ABI codecs. See the [MorphOS MUI Application
documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Application_IconifyTitle` path now retains a mutable,
caller-owned guest C-string pointer. Each `Set` or `NoNotifySet` validates the
bounded string before storing the pointer in named application state; NULL
clears the title and malformed guest pointers are rejected without mutation.
Host coverage is **601/601**. `ApplicationIconifyTitleRoot` emits **43,588 /
49,648 / 46,680 bytes** for MC68000/020/040, reaches 143 methods, and returns
**42** after **74,282 instructions / 778,486 cycles** on MC68000. The
implementation remains freestanding, exception-free, managed-runtime-free,
and struct-first. See the [MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Application_UseScreenNotify` path now stores the
initializer-only MorphOS BOOL in named application state. Non-zero values are
canonicalized to TRUE, zero to FALSE, and post-initialization writes are
rejected. The screen-notify transport remains a separate platform boundary;
the conservative default is disabled. Host coverage is **602/602**.
`ApplicationUseScreenNotifyRoot` emits **44,492 / 50,684 / 47,652 bytes** for
MC68000/020/040, reaches 144 methods, and returns **42** after **332,506
instructions / 3,490,766 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first. See the
[MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Application_DiskObject` path now retains a mutable,
caller-owned guest pointer to a complete Workbench `DiskObject` record. The
fixed ABI range is validated before storage, NULL clears the pointer, and
malformed or unmapped records are rejected without mutation. AppIcon
presentation remains a separate platform capability. Host coverage is
**603/603**. `ApplicationDiskObjectRoot` emits **43,256 / 49,276 / 46,352
bytes** for MC68000/020/040, reaches 142 methods, and returns **42** after
**67,880 instructions / 711,444 cycles** on MC68000. The implementation
remains freestanding, exception-free, managed-runtime-free, and struct-first.
See the [MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Application_ForceQuit` path now maintains the MorphOS
force-quit query flag. It defaults to FALSE when an application is initialized,
canonicalizes all non-zero writes to TRUE, and never invokes a host exit path;
the application remains responsible for exiting quietly after a quit ReturnID.
Host coverage is **596/596**. `ApplicationForceQuitRoot` emits **44,136 /
50,328 / 47,296 bytes** for MC68000/020/040, reaches 143 methods, and returns
**42** after **151,526 instructions / 1,590,184 cycles** on MC68000. The
implementation remains freestanding, exception-free, managed-runtime-free,
and struct-first. See the [MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Application_Window` initializer path now routes each guest
object through `AddWindow`, preserving named ownership and application-sleep
inheritance. Multiple initializer tags are accepted in order; duplicate,
invalid, and post-initialization writes are rejected. Host coverage is
**599/599**. `ApplicationWindowInitializerRoot` emits **46,820 / 53,156 /
50,020 bytes** for MC68000/020/040, reaches 151 methods, and returns **42**
after **402,848 instructions / 4,227,902 cycles** on MC68000. The
implementation remains freestanding, exception-free, managed-runtime-free,
and struct-first. See the [MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Application_HelpFile` path now retains a mutable, validated
guest C-string pointer. `MUIM_Application_ShowHelp` uses that application
value when its HelpFile field is NULL, while an explicit packet pointer still
takes precedence. Host coverage remains **598/598**. The qualified
`ApplicationShowHelpRoot` emits **49,536 / 56,204 / 52,732 bytes** for
MC68000/020/040, reaches 159 methods, and returns **42** after **494,746
instructions / 5,185,694 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first. See the
[MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Application_UseRexx` path now implements the documented
initializer-only ARexx policy. The default is TRUE; an initializer may request
FALSE before `InitializeApplication`, and any post-initialization write is
rejected without mutation. The setting remains named guest state while ARexx
transport itself remains a separate platform service. Host coverage is
**597/597**. `ApplicationUseRexxRoot` emits **44,192 / 50,380 / 47,352 bytes**
for MC68000/020/040, reaches 143 methods, and returns **42** after **320,676
instructions / 3,364,308 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first. See the
[MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed application identity-string path now covers the MorphOS `[I.G]`
attributes `MUIA_Application_Title`, `Author`, `Base`, `Copyright`,
`Description`, and `Version`. Each retains a bounded caller-owned guest
C-string pointer in the named attribute record, accepts NULL before
initialization, and rejects writes after initialization. Host coverage is
**598/598**. `ApplicationIdentityStringsRoot` emits **46,472 / 52,772 /
49,616 bytes** for MC68000/020/040, reaches 151 methods, and returns **42**
after **254,445 instructions / 2,662,062 cycles** on MC68000. The
implementation remains freestanding, exception-free, managed-runtime-free,
and struct-first. See the [MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Application_UsedClasses` path now validates the mutable
`[ISG]` guest `STRPTR` vector as a bounded, NULL-terminated list of C-string
class names. `MuiApplicationUsedClassesVectorCursor` and
`MuiApplicationUsedClassesVectorEntry` are named guest-memory records; the
vector pointer is retained in the application attribute without a
managed copy, and a NULL vector represents an empty list. Host coverage is
**600/600**. `ApplicationUsedClassesRoot` emits **43,944 / 50,004 / 47,024
bytes** for MC68000/020/040, reaches 144 methods, and returns **42** after
**70,416 instructions / 732,784 cycles** on MC68000. The implementation
remains freestanding, exception-free, managed-runtime-free, and struct-first.
See the [MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Application_MenuAction`/`MUIA_Application_MenuHelp` path now
keeps menu event state in named guest attributes. MenuAction accepts
`Set`/`NoNotifySet`; MenuHelp is getter-only and is updated through the typed
menu transport seam. Both initialize to zero, with no managed event shadow.
Host coverage is **605/605**. `ApplicationMenuEventStateRoot` emits
**44,688 / 50,876 / 47,844 bytes** for MC68000/020/040, reaches 145 methods,
and returns **42** after **192,715 instructions / 2,021,186 cycles** on
MC68000. The implementation remains freestanding, exception-free,
managed-runtime-free, and struct-first. See the [MorphOS MUI Application
documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

## Window mode policy

The MorphOS initializer-only Window mode attributes are represented by the
named `MuiWindowModePolicy` struct. `AppWindow`, `Backdrop`, `Borderless`, and
`PanelWindow` are canonicalized as ULONG/BOOL values during construction;
post-initialization writes are rejected. `OpenWindow` forwards the complete
record through the typed `ConfigureMuiWindowMode` platform capability. No
exceptions, managed allocation, or runtime-owned shadow object is introduced;
raw offsets remain confined to the guest TagItem boundary.

## Window Menustrip ownership

`MUIA_Window_Menustrip` accepts only a live, unparented `Menustrip.mui`
object. The named pointer record and the window's guest family relationship
are changed together, with rollback on failure; replacement and NULL clearing
detach the previous child without creating a managed menu graph. The root
object pointer is stored independently, so a window can own both a root object
and a Menustrip without positional-child ambiguity.

## Window FancyDrawing compatibility

The obsolete MorphOS `MUIA_Window_FancyDrawing` BOOL is retained as named
guest state and supports `Set`, `NoNotifySet`, and `Get`. It does not bypass
the normal draw lifecycle or create a rendering shortcut; the implementation
keeps this compatibility surface separate from `MUIM_Draw`.

## Window MenuAction state

`MUIA_Window_MenuAction` is retained as a named mutable ULONG event value.
Ordinary Set/Get packets and `SetWindowMenuActionValue` use the same guest
attribute record, so future Menustrip selection delivery can publish UserData
without a second shadow state or raw offset path.

The typed `MUIA_Application_Menustrip` path now adopts a live, unparented
`Menustrip.mui` object before application initialization through the
guest-resident family relationship. `Menuitem.mui` trigger selection publishes
its named `MUIA_UserData` to the owning application's MenuAction, while help
selection publishes to MenuHelp. Host coverage is **606/606**.
`ApplicationMenuTransportRoot` emits **52,220 / 59,156 / 55,444 bytes** for
MC68000/020/040, reaches 171 methods, and returns **42** after **586,012
instructions / 6,134,996 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first, with
named packet records and no raw handler offsets. See the [MorphOS MUI
Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Window_NoMenus` path now uses a named mutable BOOL record.
`Set` and `NoNotifySet` share the focused `DispatchWindowNoMenus` packet seam,
and every non-zero value is canonicalized to TRUE. The guest state is
qualified independently of menu rendering, which remains a platform
capability. Host coverage is **615/615**. `WindowNoMenusRoot` emits
**52,348 / 59,684 / 55,888 bytes** for MC68000/020/040, reaches 168 methods,
and returns **42** after **67,048 instructions / 702,220 cycles** on MC68000.
The implementation remains freestanding, exception-free,
managed-runtime-free, and struct-first; packet fields cross named records
rather than raw handler offsets. See the [MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_HasAlpha` path now uses a named mutable BOOL record.
`Set` and `NoNotifySet` share the focused `DispatchWindowHasAlpha` packet
seam, and every non-zero value is canonicalized to TRUE. Alpha-buffer and
Intuition forwarding remain a platform capability. Host coverage is
**616/616**. `WindowHasAlphaRoot` emits **52,392 / 59,740 / 55,932 bytes** for
MC68000/020/040, reaches 168 methods, and returns **42** after **67,099
instructions / 702,614 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first; packet
fields cross named records rather than raw handler offsets. See the [MorphOS
MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_Title` path now retains the caller-owned guest C-string
pointer in named window state. Bounded strings are validated in place, NULL
clears the value, and malformed pointers are rejected without managed copies.
Host coverage is **618/618**. `WindowTitleRoot` emits **52,732 / 60,076 /
56,316 bytes** for MC68000/020/040, reaches 168 methods, and returns **42**
after **72,601 instructions / 760,758 cycles** on MC68000. The implementation
remains freestanding, exception-free, managed-runtime-free, and struct-first;
packet fields cross named records rather than raw handler offsets. See the
[MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_Opacity` path now retains a bounded LONG in named
window state. Valid values are **0..255**; malformed writes are rejected
atomically without changing the previous value. Intuition opacity forwarding
remains a platform capability. Host coverage is **617/617**.
`WindowOpacityRoot` emits **52,584 / 59,948 / 56,140 bytes** for
MC68000/020/040, reaches 168 methods, and returns **42** after **72,060
instructions / 755,876 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first; packet
fields cross named records rather than raw handler offsets. See the [MorphOS
MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Application_UseCommodities` path now implements the
initializer-only BOOL policy: the MorphOS default is TRUE, a pre-
initialization FALSE value is retained in named guest state, and live writes
are rejected. Commodities transport remains a separate platform capability.
Host coverage is **607/607**. `ApplicationUseCommoditiesRoot` emits
**44,616 / 50,804 / 47,764 bytes** for MC68000/020/040, reaches 144 methods,
and returns **42** after **409,996 instructions / 4,297,218 cycles** on
MC68000. The implementation remains freestanding, exception-free,
managed-runtime-free, and struct-first. See the [MorphOS MUI Application
documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

## Window MouseObject state

`MUIA_Window_NeedsMouseObject` is an initializer-only named MorphOS BOOL:
creation tags canonicalize non-zero values to TRUE and live writes are
rejected. `MUIA_Window_MouseObject` is a getter-only named APTR. The
`PublishWindowMouseObjectValue` seam validates a live target object, rejects
self/unknown pointers, and permits NULL clearing without introducing a
managed object graph or raw handler offsets. Hit testing and pointer delivery
remain a platform capability. Host coverage is **634/634**; native focused
qualification remains progressive.

## List slot and image state

The shared List backbone uses `MuiListSlotState`/`MuiListSlotCodec` for each
fixed 8-byte index element and `MuiListImageState`/`MuiListImageCodec` for the
16-byte opaque image-handle chain. The contiguous slot array remains an
explicit ABI boundary, while slot members, image metadata, and next links are
named struct fields in all consumers. Host coverage is **666/666**; native
List slot/image ABI and complete MorphOS differential parity remain
progressive.

## List column geometry state

The fixed 8-byte `{Offset, Width}` geometry elements used by List layout,
drawing, hit-testing, and edit placement now use the named
`MuiListColumnGeometry` record and `MuiListColumnGeometryCodec`. The geometry
array remains an explicit ABI boundary; all consumers use the named fields.
Host coverage is **667/667**; native geometry ABI and complete MorphOS
differential parity remain progressive.

## List render-info state

List drawing and render-port lookup consume the named
`MuiDrawingRenderInfoRecord` through `MuiDrawingRenderInfoCodec`, including the
typed `RastPort` pointer. The List core no longer repeats the fixed
`MUI_RenderInfo` member offset. Host coverage remains **667/667**; native
render-port ABI and complete MorphOS differential parity remain progressive.

## Dirlist fixed wire records

The fixed headers of owned FileInfoBlock-like entries and transient ExAll-like
scan payloads use `MuiDirlistEntryWireState`/`MuiDirlistEntryWireCodec` and
`MuiDirlistScanEntryWireState`/`MuiDirlistScanEntryWireCodec`. Variable inline
name/comment payloads remain explicit ABI tails; fixed field accesses are
codec-only. Host coverage is **668/668**; native Dirlist entry ABI and
complete MorphOS differential parity remain progressive.

## AreaDrag typed method headers

AreaDrag Begin, Drop, Event, Finish, Query, and Report readers now route shared
selector admission through `MuiAreaDragMessageCodec.TryReadMethodIdValue`
before decoding named typed records. Host coverage is **2/2** focused and the
complete suite is **1342/1342** in both SDK modes. The native
`AreaDragMessageCodecRoot` returns **42** with MC68000/020/040 artifacts
**6,548/6,616/6,636** bytes and zero-runtime gates; full AreaDrag dispatch and
MorphOS differential parity remain progressive.

## Process specialist typed method headers

Process specialist Get/Set, signal, error, dispatch, and lifecycle readers now
route selector admission through `MuiProcessSpecialistMessageCodec.TryReadMethodIdValue`
before decoding named typed records. Host coverage is **2/2** focused and the
complete suite is **1342/1342** in both SDK modes. The native
`ProcessSpecialistMessageCodecRoot` returns **42** with MC68000/020/040 artifacts
**6,052/6,072/6,072** bytes and zero-runtime gates; full Process dispatch and
MorphOS differential parity remain progressive.

## Pop specialist typed method headers

Pop specialist Get/Set, Close, and lifecycle readers now route selector checks
through the named specialist method header before decoding typed records. Host
coverage is **708/708**; native Pop packet ABI and complete MorphOS differential
parity remain progressive.

## Color specialist typed method headers

Color specialist Get/Set, pointer, RGB, and lifecycle readers now route
selector admission through `MuiColorSpecialistMessageCodec.TryReadMethodIdValue`
before decoding named typed records. Host coverage is **2/2** focused and the
complete suite is **1342/1342** in both SDK modes. The native
`ColorSpecialistMessageCodecRoot` returns **42** with MC68000/020/040 artifacts
**5,484/5,504/5,508** bytes and zero-runtime gates; full Color dispatch and
MorphOS differential parity remain progressive.

## Family mutation typed method headers

Family AddHead/AddTail/Remove, Insert, Transfer, Reorder, and Sort readers now
route selector checks through `MuiFamilyMutationMessageCodec.TryReadMethodId`
before decoding their typed records and bounded array tails. Host coverage is
**706/706**; native Family mutation packet ABI and complete MorphOS differential
parity remain progressive.

## Family_DoChildMethods method header

Family_DoChildMethods dispatch now validates its selector through the named
`MuiFamilyDoChildMethodsMessage` and
`MuiFamilyDoChildMethodsMessageCodec.TryReadMethodId`. Host coverage is
**705/705**; native Family_DoChildMethods packet ABI and complete MorphOS
differential parity remain progressive.

## Family_GetChild method header

Family_GetChild packet decoding now obtains its selector through the named
`MuiFamilyGetChildMethodMessage` and
`MuiFamilyGetChildMessageCodec.TryReadMethodId` before validating the complete
number/reference record. Host coverage is **704/704**; native Family_GetChild
packet ABI and complete MorphOS differential parity remain progressive.

## Dirlist byte-total state

The `MUIA_Dirlist_NumBytes64` guest QUAD now crosses
`MuiDirlistByteTotalCodec` using the named `MuiDirlistByteTotalState` (`High`,
`Low`) fields. Public byte-total publication and inspection no longer repeat
word offsets. Host coverage is **669/669**; native QUAD ABI and complete
MorphOS differential parity remain progressive.

## Group-change typed method headers

InitChange, ExitChange, and ExitChange2 packet decoding now uses the named
`MuiGroupChangeMessage` codec before consuming the optional flags record. Host
coverage is **725/725**; native group-change packet ABI and complete MorphOS
differential parity remain progressive.

## Listview click state

`MuiListviewClickState`/`MuiListviewClickStateCodec` now own the fixed 20-byte
click-column, BOOL, and count record. Listview click lifecycle and result
publication consume named fields, while `DrawScroller` uses the named
`MuiDrawingRenderInfoRecord` seam for its typed `RastPort`. Host coverage is
**670/670**; native Listview click/render ABI and complete MorphOS differential
parity remain progressive.

## List header state

The shared List backbone now uses the named `MuiListHeaderState` record with
typed `Index` and `Images` pointers plus `Capacity` and `Count`. The bounded
`MuiListHeaderCodec` is the only place that knows the fixed 20-byte guest
layout; construction, slot access, image-chain management, and capacity
growth consume named fields. Host coverage is **662/662**. Native List ABI
qualification and complete MorphOS differential parity remain progressive.

## Listtree header state

The external `Listtree.mcc` core now uses the named 48-byte
`MuiListtreeHeaderState` record for root links, counters, redraw coalescing,
and drop-mark state. `MuiListtreeHeaderCodec` contains the fixed guest layout;
all lifecycle, traversal, and mutation consumers use named fields while the
public tree-node prefix and external-component packaging remain unchanged.
Host coverage is **663/663**. Native Listtree ABI and complete MorphOS
differential qualification remain progressive.

## Listtree node prefix state

The public 18-byte `MUIS_Listtree_TreeNode` prefix now crosses the named
`MuiListtreeNodePublicState` and `MuiListtreeNodePublicCodec` boundary. Cookie,
owner, name, flags, and user fields are typed; the 64-byte allocation and
private topology remain separate guest state. Host coverage is **664/664**;
native Listtree node ABI and complete MorphOS differential qualification remain
progressive.

## Listtree complete node state

The full 64-byte Listtree node now crosses the named
`MuiListtreeNodeState`/`MuiListtreeNodeCodec` boundary. Parent, child, sibling,
count, and ownership fields are typed, and the public prefix codec projects
from this complete record. Host coverage is **665/665**; native Listtree
topology ABI and complete MorphOS differential qualification remain
progressive.

## AppMessage node state

The 20-byte Exec Workbench node embedded in `MUIA_AppMessage` now uses the
named `MuiAppMessageNodeState` record (`Successor`, `Predecessor`, `Type`,
`Priority`, `Name`, `ReplyPort`, and `Length`). `MuiAppMessageNodeCodec`
contains the packed node offsets, while the surrounding 86-byte AppMessage
codec consumes typed state. Host coverage is **661/661**; native Workbench
message ABI qualification remains progressive.

## Obsolete Window Menu initializer alias

`MUIA_Window_Menu` (`0x8042DB94`) is accepted only during Window creation and
aliases the named Menustrip family relationship. The MorphOS
`MUIV_Window_Menu_NoMenu` sentinel (`-1`) clears the relationship; live writes
are rejected and no second menu graph is created. Host coverage is
**637/637**; native menu qualification remains progressive.

## Window Open lifecycle state

`MUIA_Window_Open` is represented by the named `MuiWindowLifecycleState.Open`
BOOL view and the existing opaque native-window pointer. `DispatchWindowOpen`
and the broad Set/NoNotifySet route call `SetWindowOpenValue`, so TRUE is
published only after `OpenMuiWindow` and FALSE is published after the native
window is closed. The implementation adds no managed lifecycle object and no
raw handler offsets. The complete host suite is **1344/1344** in both SDK
modes. The focused native MC68000 closure returns **42** after **3,939,878
instructions / 41,370,274 cycles**; MC68000/020/040 artifacts are
**457,036/468,492/465,256** bytes with zero-runtime maps.

## Window InputEvent state

`MUIA_Window_InputEvent` is a getter-only named pointer to the caller-owned
Amiga `InputEvent` struct. `PollWindowEvents` validates the full fixed-size
record and publishes its address before routing the event, so notifications
can observe the current record without a managed copy or positional offset.
The named `MuiWindowInputEventCodec` now round-trips the standard record at the
guest boundary. Host coverage is **637/637** for this surface; the focused
native closure is **447,500/458,708/455,552** bytes for MC68000/020/040 with
**3,779/3,932/3,899 relocations**, and execution returns **42** after
**1,664,554 instructions / 17,337,756 cycles**. Full platform event
translation and MorphOS differential behavior remain progressive.

## Window DisableKeys keyboard mask

`MUIA_Window_DisableKeys` (`0x80424C36`) is exposed as the named
`MuiWindowPublicCore.MuiWindowKeyboardState.DisableKeys` ULONG. The focused
`DispatchWindowDisableKeys` seam and the broad `Set`/`NoNotifySet` route retain
the caller's MUIKEYF bit mask; `DispatchWindowEvent` reads that same named
state before offering a preprocessed key to handlers. No managed keyboard
object, exception, or raw handler offset is introduced. Host coverage is
**638/638**; native focused qualification and complete MorphOS keyboard
parity remain progressive. The contract follows the [MorphOS MUI Window
documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

## String interaction state

`MUIA_String_Editable`, `MUIA_String_AdvanceOnCR`, and initializer-only
`MUIA_String_Multiline` are represented by the named
`MuiStringInteractionState` record. Construction and mutable setters
canonicalize MorphOS BOOL values; `Multiline` is rejected after construction.
With `AdvanceOnCR`, Return is deliberately left unclaimed so a containing
cycle-chain/input platform can perform focus advancement without a managed
focus graph in the String core. Host coverage is **640/640**; native focused
qualification and full MorphOS String focus behavior remain progressive.

## String EditHook / LonelyEditHook

`MUIA_String_EditHook` (`0x80424C33`) and `MUIA_String_LonelyEditHook`
(`0x80421569`) use the named `MuiStringEditHookState` record. The hook is
validated as a mapped guest Hook record, then invoked through the existing
`InvokeHook` capability with a named, fixed 44-byte `MuiStringEditWorkRecord`
in A2 and the `SGH_KEY` command in A1. A nonzero result can publish a bounded
WorkBuffer, cursor, and action bits; a zero result falls back to private
editing unless LonelyEditHook is enabled. No managed callback wrapper,
exception, or raw handler offset is used. Host coverage is **641/641**;
native hook/action and focus qualification remain progressive. See the
[MorphOS MUI String documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

## Stringscroll named runtime state

`Stringscroll.mui` uses the named `MuiStringscrollState` record for its
object-owned String pointer, derived content dimensions, and pixel scroll
coordinates. Recompute, scrolling, min/max, and drawing consume that record;
private store-key access remains confined to the read/write seam. Host
coverage is **647/647**; native Stringscroll rendering/input qualification
remains progressive.

## Stringscroll policy state

`Stringscroll.mui` stores its bar, minimum-size, border, and input policy in
the named `MuiStringscrollPolicyState` record. Construction and mutable writes
canonicalize `HorizBar`, `NoInput`, `SetMin`, `SetVMin`, `UseWinBorder`,
`VertBar`, and `VertScrollerOnly`; bar visibility, min/max, and input paths
consume that same state while preserving the public attribute and notification
seams. No managed policy object, exception, or private widget offset is used.
Host coverage is **648/648**; native Stringscroll policy qualification
remains progressive. The contract follows the
[MorphOS Stringscroll evidence](https://morphos-team.net/sdk/MUI/MUI_String.html).

## Stringscroll initializer scrollbar pointers

MorphOS `MUIA_Stringscroll_HorizBar` and `MUIA_Stringscroll_VertBar` are
initializer-only `MUIScrollbar *` attributes. `Stringscroll.mui` retains those
guest pointers in the named `MuiStringscrollScrollbarRecord` and exposes the
typed `MuiStringscrollScrollbarState` seam; normalized policy flags stay in the
separate policy record. This preserves supplied scrollbar identity without
introducing a managed child object. `StringscrollPreservesInitializerScrollbarObjectPointers`
and the complete host suite pass **1395/1395**; automatic child composition,
notifications, and MorphOS differential qualification remain progressive. See
the [MorphOS Stringscroll documentation](https://morphos-team.net/sdk/MUI/MUI_Stringscroll.html).

When `scrollbar.mui` is registered and either initializer pointer is omitted,
Stringscroll creates an owned child Scrollbar and records it in the named
`MuiStringscrollCompositionRecord`. The child receives named Prop range and
visibility updates from the Stringscroll viewport, uses the common Scrollbar
layout/draw seams, and is retired during Stringscroll cleanup; caller-supplied
bar objects are never disposed. `StringscrollBuildsTypedAutomaticScrollbarChildren`
covers construction, layout, drawing, and balanced teardown. Host coverage is
**1396/1396**; full child input routing and MorphOS differential qualification
remain progressive.

`Stringscroll.mui` now renders proportional horizontal and vertical scrollbar
thumbs through the named `MuiStringscrollBarGeometry` record. Thumb sizes and
travel use bounded integer arithmetic over the named viewport state; host
coverage is **969/969**, while native widget composition and complete MorphOS
differential parity remain progressive.

`Stringscroll.mui` `MUIM_HandleInput` now decodes the existing named
`MuiIntuiPointerMessage` for MorphOS SELECTUP track clicks. Horizontal and
vertical clicks map through the proportional thumb geometry into bounded pixel
scroll state, with the vertical track owning the shared bottom-right corner;
the focused host suite is **970/970**.

Thumb dragging uses the guest-resident named `MuiStringscrollPointerState`:
SELECTDOWN captures the grab offset, MOUSEMOVE updates bounded scroll, and
SELECTUP retires the state. Horizontal and vertical paths remain integer-only;
the drag record is guest-resident and uses no managed allocation, exceptions,
or managed runtime; focused host coverage is **971/971**.

`Listview.mui` scroller draw and input now share the named
`MuiListviewScrollerGeometry` record. Thumb gestures use the guest-resident
`MuiListviewScrollerDragState`, track clicks map to the child List's bounded
`MUIA_List_First`, and MUIKEY_RELEASE cancellation releases the state;
focused host coverage is **972/972**.

Listview pointer multi-selection now accepts Shift, Control, and Alt through
one named qualifier mask while retaining the existing `MUIA_Listview_MultiSelect`
policy; focused host coverage is **973/973**.

Stringscroll keyboard navigation now uses named MorphOS key constants,
including `MUIKEY_TOP`/`MUIKEY_BOTTOM` values 6/7. `MUIKEY_RELEASE` retires an
active guest-resident `MuiStringscrollPointerState` before the `NoInput` gate,
so a policy change cannot strand a thumb gesture; focused host coverage is
**974/974**.

Listview sortable dragging now moves the complete selected set as one stable
group when the source row is selected. The reorder scratch storage is a
guest-resident array of named `MuiListSlotState` records, with deterministic
cleanup and no managed arrays, exceptions, floating point, or raw consumer
offsets; focused host coverage is **975/975**.

The same typed drag state now invalidates its target when pointer hit-testing
leaves the child viewport, clears the public drop mark, and prevents SELECTUP
from reordering against stale geometry; focused host coverage is **976/976**.

Empty List active requests now normalize through the existing guest-backed
attribute state to `MUIV_List_Active_Off` (`-1`). Once a row exists, the normal
MorphOS selector and index clamping resumes; focused host coverage is
**977/977**.

Listview sortable drags now use the named `MuiListTestPosResult` boundary flags
to append when the pointer is over an empty row slot inside the child viewport.
Pointer movement outside the geometry remains cancellation, while single-row
and selected-group append moves use guest-resident named slot records; focused
host coverage is **978/978**.

`MUIM_List_NextSelected` now returns the active row as the implicit selection
when the initial named scalar cursor finds no selected rows, then publishes the
end sentinel on the next call. Explicit selected-row iteration remains
struct-backed; focused host coverage is **979/979**.

Listview title rows now resolve through named `MuiListTestPosResult` and
`MuiListColumnGeometry` state for both the title string and title-array paths.
SELECTUP publishes `MUIA_List_TitleClick`; only FORMAT columns marked
`SORTABLE` invoke the existing typed List sort seam. Focused host coverage is
**980/980**; title pixel styling and complete MorphOS differential parity remain
progressive.

`MUIKEY_PRESS` now selects the active row and publishes the configured
`MUIA_Listview_DefClickColumn` through the named `MuiListviewClickState` record.
Toggle and navigation paths remain unchanged; focused host coverage is
**981/981**.

Listview layout now treats the title row as non-data space. The child List's
visible data-row count and vertical scroller range use the shared named title
state before clamping `First`; focused host coverage is **982/982**.

MorphOS `MUIA_List_HScrollerVisibility` construction policy now uses the named
guest-resident `MuiListHScrollerState` record and field cursor. Malformed values
normalize to Auto; integer-only Always/Never/Auto resolution records content
width, viewport width, and the derived visibility bit for future Listview
horizontal-scroller composition. Focused host coverage is **984/984**; native
horizontal scroller composition remains progressive.

Listview now resolves the child List policy before layout. A visible horizontal
scroller reserves a named 16-pixel bottom band, vertical geometry ends above
that band, and both track/thumb pairs draw through the freestanding integer
graphics seam. Focused host coverage is **985/985**; horizontal thumb input and
content-offset scrolling remain progressive.

Horizontal thumb gestures now update the named `ScrollX`/`MaxScrollX` state
through a guest-resident drag record. List column layout, drawing, and TestPos
share that clamped content offset. Focused host coverage is **986/986**; native
wheel/key horizontal scrolling remains progressive.

Listview now handles MorphOS left/right key actions and NewMouse horizontal
wheel events through the same bounded named `ScrollX` state. Focused host
coverage is **987/987**; vertical wheel policy remains progressive.

Listview now also handles NewMouse `NM_WHEEL_UP`/`NM_WHEEL_DOWN` through the
child List's bounded named `First` state, preserving the Listview input gate and
viewport limits. Active pointer grabs leave wheel packets available for
drop-target forwarding. Focused host coverage is **988/988**; complete MorphOS
differential parity remains progressive.

Bounded Listview scroller movement now routes through the List class-aware
`First` setter and refreshes the named `TopPixel`/`VisiblePixel`/`TotalPixel`
viewport record without requiring a full layout pass. Focused host coverage is
**989/989**; native metric ABI and complete MorphOS differential parity remain
progressive.

Keyboard Listview navigation now refreshes that same named viewport record
after ListCore adjusts `First` to keep the active row visible. Page-down and
bottom navigation publish synchronized `TopPixel` values; focused host
coverage remains **989/989**. Native keyboard metric ABI and complete MorphOS
differential parity remain progressive.

Listview layout now refreshes the named viewport record after resize-time
visible-row publication and bounded `First` clamping. A larger viewport no
longer leaves stale `TopPixel` values; focused host coverage remains
**989/989**. Native resize metric ABI and complete MorphOS differential parity
remain progressive.

`MUIA_Listview_DragType` now projects into the owned List's named drag state,
so sortable pointer reordering can be enabled through the composite attribute
alone. Focused host coverage remains **989/989**; native drag/drop ABI and
complete MorphOS differential parity remain progressive.

Pointer activation now uses the List class-aware `Active` setter and refreshes
the named viewport record when a clicked row causes auto-visible scrolling.
Focused host coverage is **990/990**; native pointer metric ABI and complete
MorphOS differential parity remain progressive.

List insertion, removal, and clear operations now refresh the named viewport
record at the Entries publication boundary, keeping `TotalPixel` current after
layout without waiting for another input event. Focused host coverage is
**991/991**; native mutation metric ABI and complete MorphOS differential
parity remain progressive.

Row removal also clamps positive `First` values to the new legal viewport range
while preserving the non-empty `-1` sentinel and its zero `TopPixel` projection.
Focused host coverage remains **991/991**; native mutation-range ABI and
complete MorphOS differential parity remain progressive.

Selected-row removal, selected-row batches, and clearing a non-empty list now
publish one change-only `MUIA_List_SelectChange` notification; unselected
removal remains quiet. Focused host coverage is **992/992**; native selection
notification ABI and complete MorphOS differential parity remain progressive.

Listview-owned Lists now retain a named parent link and mirror child selection
changes to the composite's `MUIA_Listview_SelectChange` signal, including
selection and removal paths. The link is cleared during child cleanup; focused
coverage is **993/993**. Native Listview notification ABI and complete MorphOS
differential parity remain progressive.

Passive Listview mouse movement now remains outside row hit-testing; only an
active drag or scroller grab consumes movement. Hovering therefore cannot
change the active row or selection. Focused coverage is **994/994**; native
passive-input ABI and complete MorphOS differential parity remain progressive.

Exclusive Listview selection replacement now edits named slot records before
publishing one `MUIA_List_SelectChange` transition per click or keyboard
activation. Direct List selection and multiselect toggles remain unchanged;
focused coverage is **995/995**. Native selection-notification ABI and
complete MorphOS differential parity remain progressive.

Disabling `MUIA_Listview_Input` now immediately releases active named drag and
scroller records and clears the owned List drop marker, so a later pointer
packet cannot commit a gesture that has been disabled. Focused coverage remains
**995/995**; native pointer-capture ABI and complete MorphOS differential parity
remain progressive.

Changing `MUIA_Listview_ScrollerPos` or disabling `MUIA_Listview_DragType` now
uses the same named-grab cancellation boundary, preventing stale pointer
geometry from changing `MUIA_List_First` or committing a reorder. Focused
coverage is **996/996**; native pointer-capture ABI and complete MorphOS
differential parity remain progressive.

Listview BOOL and enum policies are now normalized at the composite boundary,
with invalid values falling back to documented defaults and `DragType` kept
coherent with the owned List. Focused coverage remains **996/996**; native
policy ABI and complete MorphOS differential parity remain progressive.

Floattext append now stages concatenated text through a named pending dataspace
record and commits the public Text pointer only after parsing succeeds, with
failure paths restoring the prior source. Focused coverage remains **996/996**;
native text transaction ABI and complete MorphOS differential parity remain
progressive.

List FORMAT geometry, column order, metrics, drawing, and explicit visibility
now use the named 256-column bound; a 65-column geometry regression and
balanced teardown are covered. Focused coverage is **997/997**; native
wide-column ABI and complete MorphOS differential parity remain progressive.

`MUIM_List_Jump` now refreshes the named viewport record and public pixel
projections immediately, so `TopPixel`, `VisiblePixel`, and `TotalPixel` stay
coherent without a later Layout pass. Focused coverage is **998/998**; native
scroller integration and complete MorphOS differential parity remain
progressive.

Direct class-aware `MUIA_List_Active` and `MUIA_List_First` writes now refresh
that same named viewport record immediately while preserving Active-driven
First clamping. Focused coverage is **999/999**; native direct-List metric ABI
and complete MorphOS differential parity remain progressive.

Boolean `MUIA_List_Title=TRUE` now invokes the display hook with a NULL entry
for the title row, including an empty list. Focused coverage is **1000/1000**;
native display-hook ABI and complete MorphOS differential parity remain
progressive.

Real MorphOS List display hooks now receive the current row through the named
`MuiListDisplayRowRecord` immediately before the logical column array, including
the `-1` title-row sentinel. Internal AdjustWidth, column-metric, and Draw
buffers reserve that prefix through a typed display-array storage record while
leaving the consumer-facing pointer table terminator-bounded. Focused coverage
is **1001/1001**; native display-hook ABI and complete MorphOS differential
parity remain progressive.

Direct List selection now consults `MUIA_List_MultiTestHook` before admitting an
unselected row through `MUIM_List_Select`, including Select-All and toggle
paths. Deselecting an already-selected row remains allowed. Focused coverage
is **1002/1002**; native MultiTestHook ABI and complete MorphOS differential
parity remain progressive.

`MUIM_List_Redraw` now honors the named visible viewport: concrete rows outside
`MUIA_List_First`/`MUIA_List_Visible` and the active-row sentinel with no active
entry are no-ops, while `MUIV_List_Redraw_All` still schedules a full refresh.
Focused coverage is **1003/1003**; native redraw ABI and complete MorphOS
differential parity remain progressive.

MorphOS 3.20's empty-list active projection is explicit: `MUIA_List_Active`
reads as zero for an empty List, while the named guest-resident
`MuiListActiveState` record preserves the internal no-active-row selector state.
Listview and Dirlist consume the typed active-row seam, so a public zero does
not become a real row until the cursor is actually established. Focused
coverage remains **1003/1003**; native active-cursor ABI and complete MorphOS
differential parity remain progressive.

List geometry publishes `MUIA_List_Visible` as the row capacity even when a
short List has fewer entries. Drawing and hit-testing stay bounded by the named
entry records; typed viewport pixels follow the geometry capacity, and
`MUIA_List_First` remains zero when no scrolling is possible. Focused coverage
is **1004/1004**; native geometry ABI and complete MorphOS differential parity
remain progressive.

When a List has no visible rectangle, its public `MUIA_List_Visible` and
`MUIA_List_First` values now use MorphOS's `-1` sentinel. The named viewport
state keeps pixel metrics bounded, Listview scroller code rejects the sentinel
as a row capacity, and a later visible layout restores normal state. Focused
coverage is **1006/1006**; native hidden-window ABI and complete MorphOS
differential parity remain progressive.

List pool construction policy now uses the named guest-resident
`MuiListPoolPolicyState` record. `MUIA_List_Pool`,
`MUIA_List_PoolPuddleSize`, and `MUIA_List_PoolThreshSize` retain MorphOS's
caller-owned pool identity and 2008/1024 defaults; the two size tags remain
construction-only and the record is released during teardown. No managed or
hidden host allocator is introduced. Focused coverage is **1008/1008**;
native pool ABI and complete MorphOS differential parity remain progressive.

Listview interaction policy now uses the named guest-resident
`MuiListviewInteractionPolicyState` record. `MUIA_Listview_Input`,
`MUIA_Listview_MultiSelect`, `MUIA_Listview_ScrollerPos`, and
`MUIA_Listview_DragType` retain their MorphOS ABI, but only `DragType` is
getter-visible; the three `[I..]` fields remain struct-backed construction and
input state. Host coverage is **1010/1010**; native policy-record ABI and
complete MorphOS differential parity remain progressive.

Listview selection-change publication now uses the named guest-resident
`MuiListviewSelectionSignalState` record. Child List selection edges toggle
that composite signal, Listview getters read the record, and application
setters reject the getter-only attribute. Host coverage is **1011/1011**;
native signal-record ABI and complete MorphOS differential parity remain
progressive.

Listview also rejects runtime writes to its getter-only `List`, `ClickColumn`,
`AgainClick`, `DoubleClick`, and `SelectChange` projections. Internal
ownership, click-state, and selection-signal updates still use named
guest-resident records. Host coverage is **1012/1012**; native mutability ABI
and complete MorphOS differential parity remain progressive.

List runtime writes now reject the MorphOS getter-only and construction-only
projections `Entries`, `Visible`, `SelectChange`, `InsertPosition`, `DropMark`,
`LineHeight`, `TotalPixel`, `VisiblePixel`, `MaxColumns`, and `SourceArray` at
the dispatcher-facing `MuiListCore.SetRuntimeAttribute` boundary. Internal
navigation, persistence, and derived-state publication retain the lower-level
named List setter without ambient mutability flags or offset-based state. Host
coverage is **1013/1013**; native mutability ABI and complete MorphOS
differential parity remain progressive. The contracts follow the
[MorphOS List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

Direct List runtime writes also reject the MorphOS `[I..]` `Input`,
`MultiSelect`, and `ScrollerPos` projections. Valid construction tags remain
in the named guest attribute store, while Listview interaction uses its separate
named policy record. Host coverage is **1014/1014**; native mutability ABI and
complete MorphOS differential parity remain progressive.

Direct List construction policy now also uses the named guest-resident
`MuiListInteractionPolicyState` record. BOOL and selector values normalize to
MorphOS defaults; explicit tags remain publicly readable, omitted defaults stay
in policy state without creating unrelated public getter attributes, and the
record is released during teardown. Host coverage is **1015/1015**; native
policy-record ABI and complete MorphOS differential parity remain progressive.
Lower-level direct List writes to these construction-only values are rejected
so the named record remains authoritative.

Direct List click projections now use the named guest-resident
`MuiListClickState` record. `AgainClick`, `ClickColumn`, `DefClickColumn`,
`DoubleClick`, and click counts remain synchronized with public attributes;
Listview click publication forwards the typed result to its owned child List,
and teardown releases the record. Host coverage is **1016/1016**; native
click-record ABI and complete MorphOS differential parity remain progressive.

Direct List hook configuration now uses the named guest-resident
`MuiListHookPolicyState` record for `ConstructHook`, `DestructHook`,
`DisplayHook`, `CompareHook`, and `MultiTestHook`. Entry ownership,
display/comparison, sorting, editing, and Listview multiselection consume the
same typed policy, and teardown releases it. Host coverage is **1017/1017**;
native hook-policy ABI and complete MorphOS differential parity remain
progressive.

Direct List sort/title interaction now uses the named guest-resident
`MuiListSortState` record for `SortColumn` and `TitleClick`. Format changes,
runtime setters, and title-click publication keep the named state and public
attributes synchronized. Host coverage is **1018/1018**; native sort/title
record ABI and complete MorphOS differential parity remain progressive.

Direct List presentation and interaction policy now uses the named
`MuiListPresentationPolicyState` record for `Editable`, `Quiet`,
`AdjustHeight`, `AdjustWidth`, `Stripes`, `ShowDropMarks`,
`DragSortable`, `DragType`, `AutoVisible`, `AutoLineHeight`, and
`MinLineHeight`. Construction/runtime normalization keeps public projections
coherent, and editing, redraw suppression, drag validation, striping, drop
marks, auto-visible navigation, and line-height calculation consume that
state. Teardown releases the record. Host coverage is **1019/1019**; native
presentation-policy ABI and complete MorphOS differential parity remain
progressive.

The named `MuiListViewportState` record now also carries the live `First`
cursor. Viewport refresh, direct navigation, and Listview scroller projection
share that field through the semantic codec while preserving existing pixel
field positions and MorphOS hidden sentinels. Host coverage is **1020/1020**;
native viewport-record ABI and complete MorphOS differential parity remain
progressive.

The same named `MuiListViewportState` record now carries the effective
`LineHeight` projection. Automatic line-height recomputation updates the
record and public attribute together, while the established pixel and `First`
field positions remain unchanged. Host coverage is **1021/1021**; native
line-height-record ABI and complete MorphOS differential parity remain
progressive.

The named `MuiListViewportState` record now also carries the visible row
capacity, including the MorphOS hidden `-1` sentinel. Navigation, redraw
visibility, and Listview scroller projection consume the typed capacity after
publication, while Layout keeps its transition value coherent before the
record is republished. Host coverage is **1022/1022**; native visible-capacity
ABI and complete MorphOS differential parity remain progressive.

The named viewport record now also carries the bounded `DropMark` insertion
cue. Drag producers and rendering update/read that typed marker while
preserving the public `-1` sentinel. Host coverage is **1023/1023**; native
drop-mark-record ABI and complete MorphOS differential parity remain
progressive.

Remaining steady-state List consumers now read `First` through the named
viewport cursor, including activation paging, hit-testing, drawing, Jump,
redraw visibility, and edit placement. Construction, Layout transitions, and
viewport refresh retain raw fallback reads where the record is being published.
Host coverage remains **1023/1023**; native first-cursor ABI and complete
MorphOS differential parity remain progressive.

The named active-cursor record now carries the selected row alongside its
presence bit. ActiveIndex, insertion/removal shifts, and empty-list handling
consume that record while retaining compatibility with raw construction
writers. Host coverage is **1024/1024**; native active-cursor ABI and complete
MorphOS differential parity remain progressive.

The scalar `MUIA_List_Title` projection now uses a named title-value record.
Title-row counting, measurement, and drawing consume that typed value while
preserving caller-owned pointers, the `TRUE` display-hook form, and
TitleArray precedence. Host coverage is **1025/1025**; native title-value ABI
and complete MorphOS differential parity remain progressive.

The getter-only `MUIA_List_SelectChange` edge now uses a named signal record.
Selection mutations update that record and the public projection together, and
Listview forwarding consumes the same typed transition. Host coverage is
**1026/1026**; native selection-signal ABI and complete MorphOS differential
parity remain progressive.

The List `FORMAT` pointer, normalized `MAXCOLUMNS` limit, and derived column
count now share the named `MuiListFormatPolicyState` record. Format
normalization, descriptor replacement, cleanup, and column consumers use the
typed policy while preserving caller-owned format strings. Host coverage is
**1027/1027**; native format-policy ABI and complete MorphOS differential
parity remain progressive.

The inherited List font pointer now uses the named `MuiListFontState` record.
Width measurement, drawing, rendering, and class-aware font updates share the
typed pointer while preserving caller ownership of the external `TextFont`.
Host coverage is **1028/1028**; native font-state ABI and complete MorphOS
differential parity remain progressive.

List baseline line-height calculation now consumes `MinLineHeight` from the
named `MuiListPresentationPolicyState` instead of rereading the raw attribute.
This keeps AskMinMax, automatic line-height refresh, and geometry policy on one
typed source. Host coverage is **1029/1029**; native baseline-policy ABI and
complete MorphOS differential parity remain progressive.

Listview's adopted `MUIA_Listview_List` relationship now uses the named
`MuiListviewChildState` record. Child lookup, getter publication, and cleanup
share the typed pointer while preserving failure-atomic adoption and the
getter-only runtime contract. Host coverage is **1030/1030**; native child
relationship ABI and complete MorphOS differential parity remain progressive.

Listview's named click state now also carries `DefClickColumn`. Keyboard and
pointer activation, getters, and runtime updates use the same typed click
record instead of a separate raw default-column projection. Host coverage is
**1031/1031**; native click-state ABI and complete MorphOS differential parity
remain progressive.

Floattext now keeps its private Text/SkipChars pointers and TabSize, Justify,
and Width policy in the named guest-resident `MuiFloattextPolicyState` record.
Parser reads, runtime policy updates, text replacement, and append commits use
that typed Dataspace record. Host coverage is **1032/1032**; native Floattext
policy ABI and complete MorphOS differential parity remain progressive.

Stringscroll now stores its seven BOOL policy attributes in the named
guest-resident `MuiStringscrollPolicyRecord`. Input, layout, scrollbar policy,
and runtime updates share that typed record while the public attribute words
remain synchronized for ABI compatibility. Host coverage is **1033/1033**;
native Stringscroll policy ABI and complete MorphOS differential parity remain
progressive.

Stringscroll now keeps `String`, `ContentWidth`, `ContentHeight`, `ScrollX`,
and `ScrollY` in the named guest-resident `MuiStringscrollStateRecord`.
Recompute, pixel scrolling, string replacement, and state readback use that
typed Dataspace record while the public/raw projections remain synchronized.
Host coverage is **1034/1034**; native Stringscroll state ABI and complete
MorphOS differential parity remain progressive.

Stringscroll Area geometry now also uses the named guest-resident
`MuiStringscrollLayoutStateRecord` for signed `Left`, `Top`, `Width`, and
`Height`. Layout, geometry reads, scrolling, clipping, and drawing share the
typed record while public Area attributes remain synchronized. Host coverage is
**1035/1035**; native layout-state ABI and complete MorphOS differential parity
remain progressive.

Stringscroll drawing context now uses the named guest-resident
`MuiStringscrollRenderStateRecord` for RenderInfo, the decoded RastPort, and
Font. Drawing and render inspection share the validated typed record while
public render attributes remain synchronized. Host coverage is **1036/1036**;
native render-state ABI and complete MorphOS differential parity remain
progressive.

Stringscroll now publishes bar visibility, effective viewport dimensions, and
maximum scroll bounds through the named guest-resident
`MuiStringscrollViewportStateRecord`. Scrolling and drawing consume that typed
derived state after recomputation. Host coverage is **1037/1037**; native
viewport-state ABI and complete MorphOS differential parity remain progressive.

Listview now publishes its signed composite and adopted-child rectangles in the
named guest-resident `MuiListviewLayoutState` record. Layout and scrollbar
geometry consume that typed state, and disposal retires it with the composite.
Host coverage is **1038/1038**; native Listview layout-state ABI and complete
MorphOS differential parity remain progressive.

Listview now keeps RenderInfo and its decoded RastPort in the named
guest-resident `MuiListviewRenderState` record. Scrollbar drawing and child
render binding use the typed context, with raw fallback only before publication.
Host coverage is **1039/1039**; native Listview render-state ABI and complete
MorphOS differential parity remain progressive.

Listview now publishes its vertical scroller projection (`Entries`, `Visible`,
`First`, and `MaxFirst`) through the named guest-resident
`MuiListviewScrollerState` record. Scroller geometry and input consume the
typed projection after child synchronization. Host coverage is **1040/1040**;
native Listview scroller-state ABI and complete MorphOS differential parity
remain progressive.

Listview now publishes horizontal track/thumb geometry and child scroll metrics
through the named guest-resident `MuiListviewHorizontalScrollerState` record.
Drawing, keyboard/wheel movement, and thumb input refresh and consume the typed
projection. Host coverage is **1041/1041**; native horizontal scroller-state
ABI and complete MorphOS differential parity remain progressive.

String.mui now publishes `BufferPos` and `DisplayPos` through the named
guest-resident `MuiStringCursorStateRecord`. Normalization, editing, cursor
visibility, and rendering share that typed projection while public attributes
remain synchronized. Host coverage is **1042/1042**; native String cursor-state
ABI and complete MorphOS differential parity remain progressive.

String.mui now publishes `MaxLen`, `Secret`, `Format`, and `Unicode` through
the named guest-resident `MuiStringPresentationStateRecord`. Normalization,
input encoding, length limits, and rendering consume the typed policy while
public attributes remain synchronized. Host coverage is **1043/1043**; native
String presentation-state ABI and complete MorphOS differential parity remain
progressive.

String.mui now publishes `Editable`, `AdvanceOnCR`, and `Multiline` through
the named guest-resident `MuiStringInteractionStateRecord`. Input gating and
CR handling consume the typed policy while public attributes remain
synchronized. Host coverage is **1044/1044**; native String interaction-state
ABI and complete MorphOS differential parity remain progressive.

String.mui spell-checking policy now uses the named guest-resident
`MuiStringSpellCheckingStateRecord`. Construction, canonical reads, and
runtime setters share the typed BOOL state while dictionary integration stays
an explicit platform capability. Host coverage is **1045/1045**; native
spellchecker ABI and complete MorphOS differential parity remain progressive.

String.mui `Acknowledge` publication now uses the named guest-resident
`MuiStringAcknowledgeStateRecord`. Return writes the current owned contents
pointer into the typed record, while Get absorbs only bounded, validated
guest-string pointers. Host coverage is **1046/1046**; native notification
timing and complete MorphOS differential parity remain progressive.

String.mui `AttachedList` now uses the named guest-resident
`MuiStringAttachedListStateRecord`. Construction, Get, runtime setters, and
Listview cursor forwarding share the validated typed relationship. Host
coverage is **1047/1047**; native attachment and complete MorphOS differential
parity remain progressive.

String.mui edit-hook policy now uses the named guest-resident
`MuiStringEditHookStateRecord` for the Hook pointer and `LonelyEditHook` BOOL.
Hook invocation, fallback policy, construction, Get, and runtime setters share
that typed state. Host coverage is **1048/1048**; native hook/action and
complete MorphOS differential parity remain progressive.

String.mui `Accept` and `Reject` now use the named guest-resident
`MuiStringFilterStateRecord`. Construction, runtime setters, raw bootstrap
synchronization, and legacy-byte/UTF-8 admission share the validated
caller-owned pointers. Host coverage is **1049/1049**; native focused String
qualification and complete MorphOS differential parity remain progressive.

String.mui ordinary `Integer` state now uses the named guest-resident
`MuiStringIntegerStateRecord`. Signed decimal seeds, runtime integer sets,
contents edits, and imports synchronize the typed value with the public ULONG
attribute. Host coverage is **1050/1050**; native numeric qualification and
complete MorphOS differential parity remain progressive.

String.mui `Placeholder` now uses the named guest-resident
`MuiStringPlaceholderStateRecord`. Construction and replacements retain an
object-owned bounded C string, while Get and drawing consume the typed pointer.
Host coverage is **1051/1051**; native placeholder qualification and complete
MorphOS differential parity remain progressive.

String.mui `Contents` now uses the named guest-resident
`MuiStringContentsStateRecord`. CopyContents publishes the object-owned
pointer through typed state, and editing, numeric synchronization, cursor
normalization, hook work, and drawing consume it. Host coverage is
**1052/1052**; native contents ABI and complete MorphOS differential parity
remain progressive.

Cycle and Radio entry vectors now use the named guest-resident
`MuiChoiceEntriesStateRecord`. Construction, active-choice navigation,
normalization, min/max sizing, and drawing consume the bounded typed vector;
direct persistence writes are folded back into the record. Host coverage is
**1053/1053**; native choice-vector ABI and complete MorphOS differential
parity remain progressive.

Text.mui `Contents` now uses the named guest-resident
`MuiTextContentsStateRecord`. Copy ownership, persistence, min/max sizing, and
drawing consume the typed contents pointer, while caller-owned references stay
unmanaged when `MUIA_Text_Copy` is false. Host coverage is **1054/1054**;
native text-contents ABI and complete MorphOS differential parity remain
progressive.

Text.mui `PreParse` now uses the named guest-resident
`MuiTextPreParseStateRecord`. Its bounded copied buffer is published through
typed state and consumed by min/max measurement and drawing; runtime
replacement preserves caller ownership. Host coverage is **1055/1055**;
native preparse ABI and complete MorphOS differential parity remain
progressive.

Numeric-family `Format` now uses the named guest-resident
`MuiNumericFormatStateRecord`. Construction and bounded replacement publish
the object-owned format through typed state, and numeric stringification reads
that state. Host coverage is **1056/1056**; native numeric-format ABI and
complete MorphOS differential parity remain progressive.

Gauge.mui `InfoText` now uses the named guest-resident
`MuiGaugeInfoTextStateRecord`. Construction and bounded replacements publish
the object-owned format through typed state, and gauge rendering consumes that
state. Host coverage is **1057/1057**; native gauge-format ABI and complete
MorphOS differential parity remain progressive.

Levelmeter.mui `Label` now uses the named guest-resident
`MuiLevelmeterLabelStateRecord`. Construction and bounded replacements
publish the object-owned label through typed state, and Levelmeter drawing
consumes that state. Host coverage is **1058/1058**; native levelmeter-label
ABI and complete MorphOS differential parity remain progressive.

Image.mui `OldImage` now uses the named guest-resident
`MuiImageOldImageStateRecord`. Construction, image sizing, and the primary
draw fallback consume the typed caller-owned pointer, while scalar projections
are folded back into that record. Host coverage is **1059/1059**; native
OldImage ABI and complete MorphOS differential parity remain progressive.

Image.mui `Image_Spec` now uses the named guest-resident
`MuiImageSpecStateRecord`. The tagged union preserves absent attributes,
builtin values, and guest specification pointers while drawing consumes the
typed state. Host coverage is **1060/1060**; native Image_Spec ABI and
complete MorphOS differential parity remain progressive.

Bitmap.mui `Bitmap` and Bodychunk.mui `Body` now share the named
guest-resident `MuiBitmapSourceStateRecord`. Construction, remap/decoding
setup, runtime source replacement, and drawing consume the class-aware typed
source pointer. Host coverage is **1061/1061**; native bitmap-source ABI and
complete MorphOS differential parity remain progressive.

Rectangle.mui `BarTitle` now uses the named guest-resident
`MuiRectangleBarTitleStateRecord`. Construction and rectangle drawing consume
the typed optional caller-owned title pointer while preserving absent titles.
Host coverage is **1062/1062**; native bar-title ABI and complete MorphOS
differential parity remain progressive.

The shared common-control `Font` pointer now uses the named guest-resident
`MuiControlFontStateRecord`. Construction, runtime projection, and drawing
preserve optional-font presence while consuming typed state. Host coverage is
**1063/1063**; native Font ABI and complete MorphOS differential parity remain
progressive.

Image.mui `FontMatchString` now uses the named guest-resident
`MuiImageFontMatchStringStateRecord`. Construction and runtime replacement
validate bounded guest strings and publish the optional caller-owned pointer
through typed state. Host coverage is **1064/1064**; native font-match ABI and
complete MorphOS differential parity remain progressive.

Bodychunk.mui decoding now uses the named guest-resident
`MuiBodychunkFormatStateRecord` for Compression, Depth, and Masking.
Construction, runtime mutation, and BODY decode consume the typed format state
without anonymous widget offsets. Host coverage is **1065/1065**; native
Bodychunk format ABI and complete MorphOS differential parity remain
progressive.

Bitmap.mui and Bodychunk.mui geometry now uses the shared named guest-resident
`MuiBitmapGeometryStateRecord` for Width and Height. Construction, runtime
mutation, min/max layout, and Bodychunk preparation consume the typed geometry
state. Host coverage is **1066/1066**; native bitmap-geometry ABI and complete
MorphOS differential parity remain progressive.

Image.mui selection and free-axis policy now use the named guest-resident
`MuiImageRenderStateRecord`. Construction, setters, input toggling, persistence,
min/max layout, and builtin-image drawing consume the shared typed state. Host
coverage is **1067/1067**; native image-render-state ABI and complete MorphOS
differential parity remain progressive.

Numeric-family controls now share the named guest-resident
`MuiNumericStateRecord` for Minimum, Maximum, Value, Default, and Reverse.
Construction, clamping, scaling, keyboard input, and numeric/levelmeter
drawing consume the typed state. Host coverage is **1069/1069**; native
Numeric-state ABI and complete MorphOS differential parity remain progressive.

Prop.mui and Scrollbar.mui now share the named guest-resident
`MuiPropRangeStateRecord` for Entries, Visible, and First. Construction,
movement, clamping, and Prop/Scrollbar drawing consume the typed range state.
Host coverage is **1070/1070**; native Prop-range ABI and complete MorphOS
differential parity remain progressive.

Gauge.mui now uses the named guest-resident `MuiGaugeStateRecord` for Maximum,
Current, Divide, and Horizontal. Construction, divide scaling, clamping,
runtime mutation, and drawing consume the typed Gauge state. Host coverage is
**1071/1071**; native Gauge-state ABI and complete MorphOS differential parity
remain progressive.

Scrollbar.mui now uses the named guest-resident `MuiScrollbarLayoutStateRecord`
for Group orientation and Scrollbar type. Child construction, Prop forwarding,
layout, and drawing consume the typed scrollbar geometry state. Host coverage
is **1072/1072**; native Scrollbar-layout ABI and complete MorphOS differential
parity remain progressive.

Slider.mui now uses the named guest-resident `MuiSliderPresentationStateRecord`
for orientation and quiet-display policy. Construction, runtime orientation
changes, min/max layout, and drawing consume the typed presentation state. Host
coverage is **1073/1073**; native Slider-presentation ABI and complete MorphOS
differential parity remain progressive.

Scale.mui now uses the named guest-resident `MuiScalePresentationStateRecord`
for orientation. Construction, runtime orientation changes, and graduated
scale drawing consume the typed presentation state. Host coverage is
**1074/1074**; native Scale-presentation ABI and complete MorphOS differential
parity remain progressive.

Gadget.mui now uses the named guest-resident `MuiGadgetInteractionStateRecord`
for InputMode, Selected, and Pressed. Construction, keyboard activation,
runtime selection changes, persistence, and drawing consume the typed
interaction state. Host coverage is **1075/1075**; native Gadget-interaction
ABI and complete MorphOS differential parity remain progressive.

Levelmeter.mui now uses the named guest-resident
`MuiLevelmeterPresentationStateRecord` for `Gauge_Horiz` orientation. Numeric
range/value behavior is unchanged, while construction and Levelmeter drawing
consume the typed presentation state. Host coverage is **1076/1076**; native
Levelmeter-presentation ABI and complete MorphOS differential parity remain
progressive.

Text.mui now uses the named guest-resident `MuiTextPresentationStateRecord`
for sizing flags, control character, marking, shortening, and high-character
policy. Construction, keyboard activation, min/max sizing, runtime mutable
attributes, and drawing consume the typed Text presentation state. Host
coverage is **1077/1077**; native Text-presentation ABI and complete MorphOS
differential parity remain progressive.

Rectangle.mui now uses the named guest-resident
`MuiRectanglePresentationStateRecord` for its horizontal and vertical bar
flags. Construction and drawing consume the typed presentation state while
the init-only MorphOS attributes remain projected through the public object
surface. Host coverage is **1078/1078**; native Rectangle-presentation ABI and
complete MorphOS differential parity remain progressive.

Common controls now share the named guest-resident
`MuiAreaPresentationStateRecord` for `Disabled`, `ShowMe`, `Background`, and
`Frame`. Construction, disabled-aware input, visibility sizing, and neutral
drawing consume the typed Area presentation state. Host coverage is
**1079/1079**; native Area-presentation ABI and complete MorphOS differential
parity remain progressive.

The shared Area layout path now uses the named guest-resident
`MuiAreaGeometryStateRecord` for signed position, size, and derived edge
values. Layout publication, Area drawing, and common-control rendering use the
typed geometry state. Host coverage is **1080/1080**; native Area-geometry ABI
and complete MorphOS differential parity remain progressive.

Floattext wrapping now resolves its effective width through the shared named
`MuiAreaGeometryStateRecord` after layout, while retaining the explicit
Floattext policy as the fallback public projection. State inspection and row
rebuilding consume the typed laid-out width instead of rereading a separate raw
scalar. Host coverage is **1081/1081**; native Floattext geometry ABI and
complete MorphOS differential parity remain progressive.

Balance adjacent-member resizing now reads neighboring rectangles as
`MuiAreaGeometryState` structs and republishes the resized records through the
shared Area layout boundary. Horizontal and vertical adjustments share typed
geometry with the public projection. Host coverage is **1082/1082**; native
Balance geometry ABI and complete MorphOS differential parity remain
progressive.

List TestPos, row drawing, and edit-object placement now resolve their
viewport rectangles through the shared named `MuiAreaGeometryStateRecord`.
Public geometry writes are reconciled at that boundary before hit-testing and
rendering. Host coverage is **1083/1083**; native List geometry ABI and
complete MorphOS differential parity remain progressive.

Listview scroller visibility and vertical/horizontal track fallbacks now read
child and composite rectangles through `MuiAreaGeometryState` structs. Layout
publication also derives its composite record from the same typed geometry
boundary. Host coverage is **1084/1084**; native Listview geometry ABI and
complete MorphOS differential parity remain progressive.

String.mui pixel scroll metrics now derive visible width and height from the
shared named `MuiAreaGeometryStateRecord`, reconciling public Area writes before
clamping scroll offsets. Host coverage is **1085/1085**; native String scroll
geometry ABI and complete MorphOS differential parity remain progressive.

Stringscroll.mui layout-state fallback and width/height setters now cross the
shared `MuiAreaGeometryState` boundary before updating the component’s own
typed layout record. Host coverage is **1086/1086**; native Stringscroll
geometry ABI and complete MorphOS differential parity remain progressive.

Dirlist and Volumelist sort policy now lives in the guest-resident named
`MuiDirlistSortStateRecord`. Canonical sort reads and runtime setters share the
record instead of rereading raw selector words. Host coverage is **1087/1087**;
native Dirlist sort-state ABI and complete MorphOS differential parity remain
progressive.

Dirlist and Volumelist filter policy now lives in the guest-resident named
`MuiDirlistFilterStateRecord`, including owned pattern pointers, normalized
BOOLs, `ExAllType`, and `FilterHook`. Scans and runtime filter setters consume
the typed record. Host coverage is **1088/1088**; native Dirlist filter-state
ABI and complete MorphOS differential parity remain progressive.

Dirlist and Volumelist scan publication now uses the guest-resident named
`MuiDirlistScanStateRecord` for status, counters, byte totals, and `IoErr`.
Scan reads and publication share that typed result boundary. Host coverage is
**1089/1089**; native Dirlist scan-state ABI and complete MorphOS differential
parity remain progressive.

Listtree object policy now uses the guest-resident named
`MuiListtreePolicyStateRecord` for active node, quiet/redraw, duplicate-name,
drag/drop, double-click, and node-hook selectors. Public object attributes stay
synchronized while internal policy reads and active-node transitions consume
the typed record. Host coverage is **1090/1090**; native Listtree policy-state
ABI and complete MorphOS differential parity remain progressive.

Volumelist `MUIA_Volumelist_ExampleMode` now uses the guest-resident named
`MuiVolumelistModeStateRecord`. Construction, population, getters, and setters
keep the public BOOL projection synchronized with the typed mode record. Host
coverage is **1091/1091**; native Volumelist mode-state ABI and complete
MorphOS differential parity remain progressive.

Virtgroup layout now republishes virtual width/height, scroll position, and
`TryFit` through the guest-resident named `MuiVirtgroupLayoutStateRecord` before
signed viewport clamping and group layout consumption. Host coverage is
**1092/1092**; native Virtgroup layout-state ABI and complete MorphOS
differential parity remain progressive.

Scrollgroup layout now republishes contents/bar pointers and free-space/no-bar
policies through the guest-resident named `MuiScrollgroupLayoutStateRecord`
before viewport and bar geometry calculation. Host coverage is **1093/1093**;
native Scrollgroup layout-state ABI and complete MorphOS differential parity
remain progressive.

Group min/max and layout now consume orientation, effective spacing, equal-size
flags, and page mode through the guest-resident named
`MuiGroupLayoutPolicyStateRecord`, including custom layout-hook boundaries. Host
coverage is **1095/1095**; native Group policy-state ABI and complete MorphOS
differential parity remain progressive.

Shared Area min/max and weighted layout now consume visibility, fixed/max
dimensions, inner margins, and effective weights through the guest-resident
named `MuiAreaLayoutPolicyStateRecord`. Host coverage is **1098/1098**; native
Area policy-state ABI and complete MorphOS differential parity remain
progressive.

Group-grid min/max and layout now consume sanitized columns/rows, spacing,
equal-size, and alignment through the guest-resident named
`MuiGroupGridStateRecord`. Host coverage is **1100/1100**; native Group-grid
policy-state ABI and complete MorphOS differential parity remain progressive.

Generic Area drawing, background fills, text dimensions, and text drawing now
consume `FillArea`, `Background`, `Frame`, and `Font` through the guest-resident
named `MuiAreaRenderPolicyStateRecord`. Host coverage is **1102/1102**; native
Area render-policy ABI and complete MorphOS differential parity remain
progressive.

Application initialization, single-task discovery, iconification transitions,
active-state writes, double-start notification, and force-quit state now use
the guest-resident named `MuiApplicationLifecycleStateRecord`. Host coverage is
**1104/1104**; native Application lifecycle ABI and complete MorphOS
differential parity remain progressive.

Native-window capability, open state, IDCMP event mask, and deferred
iconified-open state now use the guest-resident named
`MuiWindowLifecycleStateRecord`. Open/close, IDCMP, event/menu, refresh, and
application iconification paths consume the typed snapshot. Host coverage is
**1106/1106**; native Window lifecycle ABI and complete MorphOS differential
parity remain progressive.

Initializer-only alternate/primary geometry, gadget chrome, window mode,
tablet messages, and border-scroller policy now use the guest-resident named
`MuiWindowOpenPolicyStateRecord` at the native OpenWindow boundary. Host
coverage is **1108/1108**; native Window open-policy ABI and complete MorphOS
differential parity remain progressive.

Mutable Window title, requested screen, screen title, and public screen
pointers now use the guest-resident named `MuiWindowPresentationStateRecord`.
Setters/getters preserve caller-owned guest-string validation and closed-window
screen hiding. Host coverage is **1110/1110**; native Window presentation ABI
and complete MorphOS differential parity remain progressive.

Mutable Window `NoMenus`, `HasAlpha`, bounded `Opacity`, `FancyDrawing`, and
`MenuAction` values now use the guest-resident named
`MuiWindowVisualStateRecord`. Host coverage is **1112/1112**; native Window
visual-policy ABI and complete MorphOS differential parity remain progressive.

Nested Window and Application sleep depth, saved Window disabled state, and
the public sleep request use the shared guest-resident named
`MuiSleepStateRecord`. Open/close, add/remove, input suppression, application
sleep inheritance, rollback, and wake paths consume the typed snapshot with
no managed state, exceptions, or private widget offsets. Host coverage is
**1114/1114**; native sleep-state ABI and complete MorphOS differential parity
remain progressive.

Application ReturnID queue heads/tails, input-handler registration, pushed
method queue, and signal mask use the guest-resident named
`MuiApplicationSchedulerStateRecord`. Initialization, application loop,
ReturnID/Input, PushMethod/UnpushMethod, handler registration, cleanup, and
signal waits consume the typed scheduler snapshot without managed state,
exceptions, or private queue offsets. Host coverage is **1116/1116**; native
scheduler-state ABI and complete MorphOS differential parity remain
progressive.

Window snapshot flags/requests and the copied cycle-chain head, count, and
request counter use the guest-resident named
`MuiWindowInteractionStateRecord`. Snapshot, SetCycleChain, active-object
cycling/spatial selection, and disposal consume the typed interaction
snapshot without managed state, exceptions, or private chain offsets. Host
coverage is **1118/1118**; native interaction-state ABI and complete MorphOS
differential parity remain progressive.

Window close-request state and getter-only InputEvent/MouseObject pointers use
the guest-resident named `MuiWindowEventStateRecord`. Native event polling,
close-request dispatch, and pointer-publication helpers consume the typed
event snapshot without managed state, exceptions, or private pointer offsets.
Host coverage is **1120/1120**; native event-state ABI and complete MorphOS
differential parity remain progressive.

AboutMUI and ShowHelp reference windows, help name/node pointers, signed help
line, and request counters use the guest-resident named
`MuiApplicationHelpStateRecord`. Presentation methods consume the typed state
without managed state, exceptions, or private pointer offsets while retaining
MorphOS reference validation, caller-owned strings, first-open-window
resolution, and signed line semantics. Host coverage is **1122/1122**; native
help-state ABI and complete MorphOS differential parity remain progressive.

DefaultConfigItem’s requested config ID, accepted value, and saturating request
counter use the guest-resident named
`MuiApplicationDefaultConfigStateRecord`. The platform override result
publishes typed state without managed state, exceptions, or private offsets
while retaining MorphOS ULONG semantics and capability failure behavior. Host
coverage is **1124/1124**; native default-config ABI and complete MorphOS
differential parity remain progressive.

OpenConfigWindow flags, caller-owned class ID, and saturating request counter
use the guest-resident named `MuiApplicationConfigWindowStateRecord`. The
non-blocking configuration-window request publishes typed state without
managed state, exceptions, or private offsets while retaining MorphOS ULONG
flags, bounded class-ID validation, and capability failure behavior. Host
coverage is **1126/1126**; native config-window ABI and complete MorphOS
differential parity remain progressive.

BuildSettingsPanel’s requested panel number, returned panel object, and
saturating request counter use the guest-resident named
`MuiApplicationSettingsPanelStateRecord`. The application override result
publishes typed state without managed state, exceptions, or private offsets
while retaining MorphOS ULONG semantics, live-object validation, and
null-panel behavior. Host coverage is **1128/1128**; native settings-panel ABI
and complete MorphOS differential parity remain progressive.

Save/Load operation, caller-owned name selector, and saturating request, save,
and load counters use the guest-resident named
`MuiApplicationSettingsPersistenceStateRecord`. The settings persistence
boundary publishes typed state without managed state, exceptions, or private
offsets while retaining MorphOS ENV/ENVARC sentinel selectors, bounded
C-string validation, and capability failure behavior. Host coverage is
**1130/1130**; native persistence ABI and complete MorphOS differential parity
remain progressive.

CheckRefresh’s saturating check count and last refreshed-window count use the
guest-resident named `MuiApplicationRefreshStateRecord`. The refresh traversal
publishes typed state without managed state, exceptions, or private offsets
while retaining live-window filtering and MorphOS capability behavior. Host
coverage is **1132/1132**; native refresh ABI and complete MorphOS differential
parity remain progressive.

Application MenuAction and MenuHelp event values use the guest-resident named
`MuiApplicationMenuStateRecord`. Menu setters and selection publication
publish typed state without managed state, exceptions, or private offsets
while retaining MorphOS ULONG event semantics and getter-only MenuHelp
behavior. Host coverage is **1134/1134**; native menu-event ABI and complete
MorphOS differential parity remain progressive.

Application DiskObject, DropObject, and Menustrip relationships use the
guest-resident named `MuiApplicationObjectStateRecord`. Relationship setters
publish typed pointer state without managed state, exceptions, or private
offsets while retaining caller-owned DiskObject validation, live-object
validation, and menustrip family ownership. Host coverage is **1136/1136**;
native application-object ABI and complete MorphOS differential parity remain
progressive.

Application HelpFile and IconifyTitle caller-owned C-string pointers use the
guest-resident named `MuiApplicationTextStateRecord`. Text setters and
ShowHelp fallback resolution consume typed state without managed state,
exceptions, or private offsets while retaining bounded C-string validation and
MorphOS pointer semantics. Host coverage is **1138/1138**; native
application-text ABI and complete MorphOS differential parity remain
progressive.

Application Author, Base, Copyright, Description, Title, and Version
initializer pointers use the guest-resident named
`MuiApplicationIdentityStateRecord`. The initializer-only string boundary
publishes typed state without managed state, exceptions, or private offsets
while retaining bounded C-string validation, post-initialization rejection,
caller ownership, and MorphOS pointer semantics. Host coverage is
**1139/1139**; native identity ABI and complete MorphOS differential parity
remain progressive.

Application UseRexx, UseCommodities, and UseScreenNotify initializer policies
use the guest-resident named `MuiApplicationPolicyStateRecord`. Policy setters
and initialization publish typed state without managed state, exceptions, or
private offsets while retaining canonical MorphOS BOOL semantics,
initializer-only rejection, and default policy behavior. Host coverage is
**1141/1141**; native policy ABI and complete MorphOS differential parity
remain progressive.

The caller-owned `MUIA_Application_UsedClasses` STRPTR vector pointer uses the
guest-resident named `MuiApplicationUsedClassesStateRecord`. The validated
vector setter publishes typed owner state without managed state, exceptions,
or private offsets while retaining NULL-terminated vector validation and
bounded string checks. Host coverage is **1142/1142**; native UsedClasses owner
ABI and complete MorphOS differential parity remain progressive.

Repeated `MUIA_Application_Window` initializer requests use the guest-resident
named `MuiApplicationWindowRelationshipStateRecord`, retaining the last
accepted window and a saturating accepted-request count. Publication preserves
family ownership, pre-initialization-only validation, and failure-atomic
rollback without managed state, exceptions, or private offsets. Host coverage
is **1144/1144**; native relationship ABI and complete MorphOS differential
parity remain progressive.

Window `RootObject`, `Menustrip`, and `RefWindow` relationships use the
guest-resident named `MuiWindowRelationshipStateRecord`. Public getters and
validated relationship transitions publish the typed pointer snapshot while
preserving caller-owned objects, family ownership, and invalid-reference
rejection without managed state, exceptions, or private offsets. Host coverage
is **1146/1146**; native Window relationship ABI and complete MorphOS
differential parity remain progressive.

Window `Id`, `DisableKeys`, `VisibleOnMaximize`, `IsSubWindow`, and
`NeedsMouseObject` use the guest-resident named `MuiWindowControlStateRecord`.
Scalar getters and validated setters publish canonical ULONG/BOOL state while
preserving initializer-only restrictions, without managed state, exceptions, or
private offsets. Host coverage is **1148/1148**; native Window control-state ABI
and complete MorphOS differential parity remain progressive.

The opaque `MUIM_Application_SetConfigItem` item/data pair uses the named
`MuiApplicationSetConfigItemStateRecord`, with caller-owned data represented as
an explicit APTR and retained without dereferencing or copying. Mapping
validation, request counting, and cleanup remain intact. Host coverage is
**1149/1149**; native SetConfigItem ABI and complete MorphOS differential parity
remain progressive.

Transient `MUIA_AppMessage` publication and `MUIA_Window_AppWindow`
participation use the named `MuiApplicationMessageRoutingStateRecord`.
Synchronous delivery publishes and restores the caller-owned AppMessage APTR
while retaining canonical BOOL semantics, validation, and rollback. Host
coverage is **1150/1150**; native AppMessage routing ABI and complete MorphOS
differential parity remain progressive.

The caller-owned `MUIA_Application_Commands` table pointer uses the named
`MuiApplicationCommandsStateRecord`. Validated publication and getters consume
typed owner state while preserving NULL-terminated validation, explicit APTR
ownership, and failure-atomic rollback. Host coverage is **1151/1151**; native
Commands owner ABI and complete MorphOS differential parity remain progressive.

`MUIA_Window_ActiveObject` and `MUIA_Window_DefaultObject` now use the named
`MuiWindowFocusStateRecord`. Focus transitions and getter projections publish
validated object APTRs while preserving cycle-chain validation and rollback.
Host coverage is **1153/1153**; native Window focus-state ABI and complete
MorphOS differential parity remain progressive.

Public Application lifecycle attributes now project through the named
`MuiApplicationLifecycleStateRecord`. Canonical BOOL values are preserved,
publication is recursion-safe, and unrelated objects do not receive synthetic
lifecycle records. Host coverage is **1154/1154**; native Application lifecycle
getter ABI and complete MorphOS differential parity remain progressive.

`MUIA_Window_Window` and `MUIA_Window_Open` now project through the named
`MuiWindowLifecycleStateRecord`. Native window capabilities remain opaque and
Open retains canonical BOOL semantics. Host coverage is **1155/1155**; native
Window lifecycle getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_Window_CloseRequest`, `MUIA_Window_InputEvent`, and
`MUIA_Window_MouseObject` now project through the named
`MuiWindowEventStateRecord`. Close requests remain canonical BOOLs and event
object pointers remain caller-owned APTRs. Host coverage is **1156/1156**;
native Window event getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_Application_DiskObject`, `MUIA_Application_DropObject`, and
`MUIA_Application_Menustrip` now project through the named
`MuiApplicationObjectStateRecord`. Caller-owned APTR capabilities and
relationship validation remain intact. Host coverage is **1157/1157**; native
Application object getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_Application_Author`, `MUIA_Application_Base`,
`MUIA_Application_Copyright`, `MUIA_Application_Description`,
`MUIA_Application_Title`, and `MUIA_Application_Version` now project through
the named `MuiApplicationIdentityStateRecord`. Caller-owned guest C-string
pointers and initializer-only validation remain intact. Host coverage is
**1158/1158**; native Application identity getter ABI and complete MorphOS
differential parity remain progressive.

`MUIA_Application_HelpFile` and `MUIA_Application_IconifyTitle` now project
through the named `MuiApplicationTextStateRecord`. Caller-owned guest
C-string pointers and bounded string validation remain intact. Host coverage
is **1159/1159**; native Application text getter ABI and complete MorphOS
differential parity remain progressive.

`MUIA_Application_UseRexx`, `MUIA_Application_UseCommodities`, and
`MUIA_Application_UseScreenNotify` now project through the named
`MuiApplicationPolicyStateRecord`. Canonical BOOL values, initializer-only
rules, and MorphOS defaults for unconfigured applications remain intact. Host
coverage is **1160/1160**; native Application policy getter ABI and complete
MorphOS differential parity remain progressive.

`MUIA_Application_UsedClasses` now projects through the named
`MuiApplicationUsedClassesStateRecord`. The caller-owned NULL-terminated
STRPTR vector and bounded validation remain intact, and unconfigured objects
do not receive synthetic state. Host coverage is **1161/1161**; native
UsedClasses getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_Application_Window` now projects through the named
`MuiApplicationWindowRelationshipStateRecord`. Last-window and accepted-count
semantics, initializer-only relationship validation, and unconfigured-object
behavior remain intact. Host coverage is **1162/1162**; native Application
window relationship getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_Application_Sleep` now projects through the shared named
`MuiSleepStateRecord`. Nested depth/request semantics, recursion-safe raw
storage, and unconfigured-object behavior remain intact. Host coverage is
**1163/1163**; native Application sleep getter ABI and complete MorphOS
differential parity remain progressive.

`MUIA_Window_Sleep` now projects through the shared named
`MuiSleepStateRecord`. Nested depth/request semantics, saved-disabled
restoration, and recursion-safe raw storage remain intact. Host coverage is
**1164/1164**; native Window sleep getter ABI and complete MorphOS differential
parity remain progressive.

Window geometry, gadget, mode, tablet, and border-scroller getters now project
through the named `MuiWindowOpenPolicyStateRecord`. Signed LONG values,
canonical MorphOS BOOL/ULONG values, initializer-only validation, and
recursion-safe raw storage remain intact. Host coverage is **1165/1165**;
native Window open-policy getter ABI and complete MorphOS differential parity
remain progressive.

`MUIA_Application_MenuAction` and `MUIA_Application_MenuHelp` now project
through the named `MuiApplicationMenuStateRecord`. Menu UserData values and
recursion-safe raw storage remain intact. Host coverage is **1166/1166**;
native Application menu getter ABI and complete MorphOS differential parity
remain progressive.

`MUIA_Window_Sleep` is included in the public handled getter set and projects
through the named `MuiSleepStateRecord`. The obsolete `MUIA_Window_Menu` alias
projects the named `MuiWindowRelationshipStateRecord` Menustrip field. Nested
sleep depth, saved-disabled restoration, relationship identity, and
recursion-safe raw storage remain intact. Host coverage is **1166/1166**;
native alias/getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_Numeric_Value` now projects through the named `MuiNumericStateRecord`
for generic `Get` and common-control `OM_GET`. Numeric, Slider, Knob,
Numericbutton, and Levelmeter share raw-only synchronization, preserving range
clamping without getter recursion. Host coverage is **1166/1166**; native
Numeric value getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_Gauge_Current` now projects through the named `MuiGaugeStateRecord` for
generic `Get` and common-control `OM_GET`. Gauge-only classification preserves
divide/clamp behavior while Levelmeter continues using Numeric-family value
state. Host coverage is **1166/1166**; native Gauge current getter ABI and
complete MorphOS differential parity remain progressive.

The remaining public Gauge fields—`MUIA_Gauge_Max`, `MUIA_Gauge_Divide`, and
`MUIA_Gauge_Horiz`—now use the same named `MuiGaugeStateRecord` for generic
`Get` and `OM_GET`. All four Gauge getters preserve Gauge-only classification,
divide/clamp behavior, and raw-safe synchronization. Host coverage is
**1166/1166**; native Gauge state getter ABI and complete MorphOS differential
parity remain progressive.

The remaining scalar Numeric fields—`MUIA_Numeric_Min`, `MUIA_Numeric_Max`,
`MUIA_Numeric_Default`, and `MUIA_Numeric_Reverse`—now use the named
`MuiNumericStateRecord` for generic `Get` and `OM_GET`, alongside
`MUIA_Numeric_Value`. Numeric-family classification and range/scale behavior
remain intact. Host coverage is **1166/1166**; native Numeric scalar getter ABI
and complete MorphOS differential parity remain progressive.

`MUIA_Numeric_Format` now uses the named `MuiNumericFormatStateRecord` for
generic `Get` and `OM_GET`. The projection preserves the owned copy and
bounded guest C-string validation, with raw storage consulted only for
bootstrap/synchronization. Host coverage remains **1166/1166**; native Numeric
format getter ABI and complete MorphOS differential parity remain progressive.

`MUIA_Gauge_InfoText` now uses the named `MuiGaugeInfoTextStateRecord` for
generic `Get` and `OM_GET`. Gauge-only classification, owned-copy lifetime,
bounded guest C-string validation, and raw-only synchronization remain intact.
Host coverage remains **1166/1166**; native Gauge InfoText getter ABI and
complete MorphOS differential parity remain progressive.

`MUIA_Levelmeter_Label` now uses the named `MuiLevelmeterLabelStateRecord` for
generic `Get` and `OM_GET`. Levelmeter-only classification, owned-copy
lifetime, bounded guest C-string validation, and raw-only synchronization
remain intact. Host coverage remains **1166/1166**; native Levelmeter label
getter ABI and complete MorphOS differential parity remain progressive.

`MUIA_Text_Contents` now uses the named `MuiTextContentsStateRecord` for
generic `Get` and `OM_GET`. Text-only classification, the `MUIA_Text_Copy`
ownership policy, bounded guest C-string validation, and raw-only
synchronization remain intact. Host coverage remains **1166/1166**; native
Text contents getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_Text_PreParse` now uses the named `MuiTextPreParseStateRecord` for
generic `Get` and `OM_GET`. The object-owned preparse copy, Text-only
classification, bounded guest C-string validation, and raw-only
synchronization remain intact. Host coverage remains **1166/1166**; native
Text PreParse getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_String_Contents` now uses the named `MuiStringContentsStateRecord` for
generic `Get` and `OM_GET`. String-owned copies, `MUIA_String_MaxLen` bounds,
String-only classification, bounded guest C-string validation, and raw-only
synchronization remain intact. Host coverage remains **1166/1166**; native
String contents getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_String_Placeholder` now uses the named `MuiStringPlaceholderStateRecord`
for generic `Get` and `OM_GET`. The object-owned 128-byte copy, String-only
classification, bounded guest C-string validation, and raw-only
synchronization remain intact. Host coverage remains **1166/1166**; native
String placeholder getter ABI and complete MorphOS differential parity remain
progressive.

Getter-only `MUIA_String_Acknowledge` now uses the named
`MuiStringAcknowledgeStateRecord` for generic `Get` and `OM_GET`. Notification-
time publication, String-only classification, bounded guest C-string
validation, and raw-only synchronization remain intact. Host coverage remains
**1166/1166**; native String acknowledgement getter ABI and complete MorphOS
differential parity remain progressive.

`MUIA_String_AttachedList` now uses the named
`MuiStringAttachedListStateRecord` for generic `Get` and `OM_GET`. The
caller-owned live `Listview.mui` relationship, pointer-only mutation,
String input-key forwarding, and class validation remain intact. Raw-only
bootstrap keeps malformed construction pointers visible to normalization so
creation fails instead of defaulting them to NULL. Host coverage remains
**1166/1166**; native String attached-list getter ABI and complete MorphOS
differential parity remain progressive.

`MUIA_String_Integer` now uses the named `MuiStringIntegerStateRecord` for
generic `Get` and `OM_GET`. Signed ULONG semantics, contents synchronization,
and construction-time integer seeds remain intact; raw-only bootstrap prevents
an absent attribute from becoming an unintended zero seed. Host coverage
remains **1166/1166**; native String integer getter ABI and complete MorphOS
differential parity remain progressive.

`MUIA_String_SpellChecking` now uses the named
`MuiStringSpellCheckingStateRecord` for generic `Get` and `OM_GET`. MorphOS
BOOL canonicalization, construction defaults, runtime setter behavior, and
raw-only synchronization remain intact. Host coverage remains **1166/1166**;
native String spell-checking getter ABI and complete MorphOS differential parity
remain progressive.

`MUIA_String_EditHook` and `MUIA_String_LonelyEditHook` now use the named
`MuiStringEditHookStateRecord` for generic `Get` and `OM_GET`. Caller-owned Hook
mapping validation, canonical BOOL behavior, native input dispatch, and
raw-only synchronization remain intact. Host coverage remains **1166/1166**;
native String edit-hook getter ABI and complete MorphOS differential parity
remain progressive.

`MUIA_String_Accept` and `MUIA_String_Reject` now use the named
`MuiStringFilterStateRecord` for generic `Get` and `OM_GET`. Caller-owned
filter strings, pointer validation, byte/UTF-8 admission, and raw-only
synchronization remain intact. Host coverage remains **1166/1166**; native
String filter getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_String_BufferPos` and `MUIA_String_DisplayPos` now use the named
`MuiStringCursorStateRecord` for generic `Get` and `OM_GET`. Cursor clamping,
display-origin visibility updates, UTF-8 logical-column behavior, and raw-only
synchronization remain intact; internal bootstrap reads stay recursion-safe.
Host coverage remains **1166/1166**; native String cursor getter ABI and
complete MorphOS differential parity remain progressive.

`MUIA_String_Editable`, `MUIA_String_AdvanceOnCR`, and
`MUIA_String_Multiline` now use the named `MuiStringInteractionStateRecord`
for generic `Get` and `OM_GET`. MorphOS BOOL canonicalization,
initializer-only Multiline behavior, editable input gating, and raw-only
synchronization remain intact. Host coverage is **1167/1167**; native String
interaction getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_String_MaxLen`, `MUIA_String_Secret`, `MUIA_String_Format`, and
`MUIA_String_Unicode` now use the named `MuiStringPresentationStateRecord`
for generic `Get` and `OM_GET`. Initializer-only setter enforcement, format
normalization, MorphOS BOOL canonicalization, UTF-8 rendering/cursor semantics,
and raw-only synchronization remain intact. Host coverage is **1167/1167**;
native String presentation getter ABI and complete MorphOS differential parity
remain progressive.

`MUIA_String_Integer64` now projects the validated object-owned guest QUAD
pointer through the existing semantic state and two-ULONG value structs for
generic `Get` and `OM_GET`. Caller-pointer isolation, signed 64-bit
parsing/stringification, contents synchronization, malformed pointer rejection,
and raw-only internal bootstrap reads remain intact. Host coverage is
**1167/1167**; native String Integer64 getter ABI and complete MorphOS
differential parity remain progressive.

`MUIA_Text_SetMin`, `MUIA_Text_SetMax`, `MUIA_Text_SetVMax`,
`MUIA_Text_ControlChar`, `MUIA_Text_Marking`, `MUIA_Text_Shorten`, and
`MUIA_Text_HiChar` now use the named `MuiTextPresentationStateRecord` for
generic `Get` and `OM_GET`. Initializer-only versus runtime-settable
enforcement, control-character normalization, shorten-mode validation,
marking/HiChar drawing behavior, and raw-only synchronization remain intact.
Host coverage is **1167/1167**; native Text presentation getter ABI and
complete MorphOS differential parity remain progressive.

Renderer-produced `MUIA_Text_Shortened` status now uses the dedicated
`MuiTextShortenedStateRecord` for generic `Get` and `OM_GET`. Draw-time
canonical BOOL publication, get-only setter enforcement, and raw compatibility
storage remain intact without a managed status flag. Host coverage is
**1167/1167**; native Text Shortened getter ABI and complete MorphOS
differential parity remain progressive.

## Stringscroll layout state

Stringscroll Area geometry uses the named `MuiStringscrollLayoutState` record
with signed `Left`, `Top`, `Width`, and `Height` fields. Recompute, scrolling,
bar reservation, page navigation, clipping, and drawing consume that shared
layout seam instead of rereading individual Area attributes. No managed
geometry object, exception, or private widget offset is used. Host coverage is
**649/649**; native Stringscroll geometry qualification remains progressive.
The contract follows the
[MorphOS Stringscroll evidence](https://morphos-team.net/sdk/MUI/MUI_String.html).

## Stringscroll render state

Stringscroll drawing uses the named `MuiStringscrollRenderState` record for
`RenderInfo`, the decoded rastport, and `Font`. The shared guest
`MUI_RenderInfo` record is validated through `MuiDrawingRenderInfoCodec`, and
the drawing path no longer depends on a private rastport field offset. Host
coverage is **650/650**; native Stringscroll rendering qualification remains
progressive. The contract follows the
[MorphOS Stringscroll evidence](https://morphos-team.net/sdk/MUI/MUI_String.html).

## Stringscroll viewport state

Stringscroll derives effective viewport dimensions, bar visibility, and bounded
scroll limits through the named `MuiStringscrollViewportState` record.
Recompute, `SetScroll`, `GetScrollState`, page-key input, and drawing consume
one shared viewport computation. Host coverage is **651/651**; native
Stringscroll viewport qualification remains progressive. The contract follows
the [MorphOS Stringscroll evidence](https://morphos-team.net/sdk/MUI/MUI_String.html).

## Floattext named state

Floattext uses the named `MuiFloattextState` record for owned text and
skip-character pointers plus `TabSize`, `Justify`, and `Width`. Construction,
rebuild, append, direct writes, and the shared List dispatcher consume that
state; no managed text object, exception path, or private widget offset is
introduced. Host coverage is **652/652**; native Floattext layout/rendering
qualification remains progressive. The contract follows the
[MorphOS Floattext evidence](https://morphos-team.net/sdk/MUI/MUI_Floattext.html).

## Dirlist filter state

Dirlist and Volumelist filtering use the named `MuiDirlistFilterState` record
for owned accept/reject/pattern pointers, filter BOOLs, `ExAllType`, and
`FilterHook`. Construction, scans, pattern matching, and mutable writes share
that state without introducing managed filesystem objects, exceptions, or
private widget offsets. Host coverage is **653/653**; native Dirlist filtering
qualification remains progressive. The contract follows the
[MorphOS Dirlist evidence](https://morphos-team.net/sdk/MUI/MUI_Dirlist.html).

## Dirlist sort state

Dirlist and Volumelist sorting use the named `MuiDirlistSortState` record for
sort type, directory ordering, and high-low direction. Unknown selectors and
BOOL values are canonicalized before the allocation-free reorder pass. Host
coverage is **654/654**; native Dirlist sorting qualification remains
progressive. The contract follows the
[MorphOS Dirlist evidence](https://morphos-team.net/sdk/MUI/MUI_Dirlist.html).

## Dirlist scan state

Dirlist and Volumelist scan results use the named `MuiDirlistScanState` record
for status, file/drawer counters, byte totals, and captured `IoErr`. Valid and
invalid scans publish the complete record through one seam, and public getters
read that same state without managed filesystem objects, exceptions, or private
widget offsets. Host coverage is **655/655**; native scan/error timing and
complete MorphOS differential qualification remain progressive. The contract
follows the [MorphOS Dirlist evidence](https://morphos-team.net/sdk/MUI/MUI_Dirlist.html).

## Dirlist entry state

Owned FileInfoBlock-like records are exposed through the named
`MuiDirlistEntryState` record. Its bounded codec validates record size and
inline name/comment strings; sorting, path construction, and mutator methods
consume the decoded fields instead of repeating private offsets. Host coverage
is **656/656**; native entry ABI and complete MorphOS differential
qualification remain progressive. The contract follows the
[MorphOS Dirlist evidence](https://morphos-team.net/sdk/MUI/MUI_Dirlist.html).

## Dirlist scan-entry state

The transient directory-capability payload is decoded into the named
`MuiDirlistScanEntryState` record. Scan filtering, counters, and owned-record
construction consume its validated type, size, protection/date, and
name/comment fields; the Volumelist producer also uses the named writer for
type/name publication, while scratch field reads remain confined to the
decoder. Host
coverage is **657/657**; native directory-capability ABI qualification remains
progressive. The contract follows the
[MorphOS Dirlist evidence](https://morphos-team.net/sdk/MUI/MUI_Dirlist.html).

## Dirlist owned-entry writer

Variable-length FileInfoBlock-like record construction, protection updates, and
failure cleanup now use the named `MuiDirlistEntryState` writer/lifecycle seam.
The public record layout remains unchanged while fixed offsets stay inside the
codec. Host coverage remains **657/657**; native entry ABI qualification is
progressive. The contract follows the
[MorphOS Dirlist evidence](https://morphos-team.net/sdk/MUI/MUI_Dirlist.html).

## Dirlist byte-total state

The `MUIA_Dirlist_NumBytes64` guest QUAD now uses the named
`MuiDirlistByteTotalState` record and bounded read/write codec. Publication and
inspection retain the 8-byte 68k layout without exposing word offsets to
callers. Host coverage is **658/658**; native QUAD ABI qualification remains
progressive. The contract follows the
[MorphOS Dirlist evidence](https://morphos-team.net/sdk/MUI/MUI_Dirlist.html).

## String presentation state

Initializer-only `MUIA_String_MaxLen`, `MUIA_String_Secret`,
`MUIA_String_Format`, and `MUIA_String_Unicode` use the named
`MuiStringPresentationState` record. Construction canonicalizes BOOLs and
unknown alignment selectors before bounded contents ownership; bounded copy,
UTF-8 metrics, secret masking, and alignment consume the same record. No
managed text state, exception path, or private widget offset is introduced.
Host coverage remains **646/646**; native presentation qualification is
progressive. See the
[MorphOS MUI String documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

## String cursor state

`MUIA_String_BufferPos` (`0x80428B6C`) and `MUIA_String_DisplayPos`
(`0x8042CCBF`) use the named `MuiStringCursorState` record. Both positions are
normalized against the bounded guest contents length during construction,
contents replacement, and runtime Set/NoNotifySet. The guest attribute IDs
remain the ABI surface; the implementation adds no managed cursor object or
private widget offset. Host coverage is **646/646**; native cursor/scroll
qualification remains progressive. See the
[MorphOS MUI String documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

## String AttachedList

`MUIA_String_AttachedList` (`0x80420FD2`) is represented by the named
`MuiStringAttachedListState` Listview pointer. Construction and runtime writes
require a live `Listview.mui` object or NULL. Supported cursor/navigation keys
are forwarded through the existing typed `MuiListviewCore.HandleInput` seam,
so the String core does not duplicate list state or ownership. No managed
callback, exception, or raw handler offset is used. Host coverage is
**642/642**; native attachment and differential focus qualification remain
progressive. See the [MorphOS MUI String documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

## String Integer64

`MUIA_String_Integer64` (`0x80424820`) is represented by the named
`MuiStringInteger64Value` QUAD record and `MuiStringInteger64State`. The fixed
8-byte guest pointer is validated and copied into object-owned dataspace;
bounded four-16-bit-limb arithmetic renders and parses signed decimal text,
including values outside the 32-bit range. No managed 64-bit conversion,
exception, runtime object, or raw offset is used. Host coverage is
**643/643**; native focused qualification remains progressive. The contract
follows the [MorphOS MUI String documentation](https://morphos-team.net/sdk/MUI/MUI_String.html)
and MorphOS [QUAD definition](https://morphos-team.net/sdk/includes/exec/types.html).

## String SpellChecking policy

`MUIA_String_SpellChecking` (`0x804266C6`) uses the named
`MuiStringSpellCheckingState.Enabled` BOOL. Construction and mutable
Set/NoNotifySet writes canonicalize non-zero values without introducing a
managed spellchecker object or private widget offset. Dictionary, marking, and
replacement behavior remain an explicit platform service capability. Host
coverage is **644/644**; native spellchecker qualification remains progressive.
The contract follows the [MorphOS MUI String documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

## String Acknowledge state

The getter-only `MUIA_String_Acknowledge` (`0x8042026C`) uses the named
`MuiStringAcknowledgeState.Contents` pointer. On Return, the current owned
guest C string is validated before publication and notification; the path adds
no managed copy, exception, or raw widget offset. Host coverage is **645/645**;
native notification timing remains progressive. The contract follows the
[MorphOS MUI String documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

## Application persistence frame state

The guest-resident Save/Load traversal stack uses the named
`MuiApplicationPersistenceFrameState` record (`Object`, `NextChild`, and
`VisitMarker`). `MuiApplicationPersistenceFrameCodec` confines the fixed
12-byte guest layout; traversal logic consumes the named state and remains
freestanding, exception-free, managed-runtime-free, and struct-first. Host
coverage is **659/659**; native persistence qualification remains progressive.

## User Data traversal frame state

The `MUIM_FindUData`, `MUIM_GetUData`, and `MUIM_SetUData` traversal stack now
uses the named `MuiUDataTraversalFrame` record with a typed `APTR Object` and
`NextChild`. Its bounded codec validates every frame read/write, and failure
is propagated through stack setup and descent without changing the public
method behavior. Host coverage is **660/660**; native User Data traversal
qualification remains progressive.

## Common-control image geometry and render-port state

CommonControl image sizing now reads the named `MuiImageGeometryState` through
`MuiImageGeometryCodec`, and common-control drawing obtains `RastPort` through
the named `MuiDrawingRenderInfoRecord`/`MuiDrawingRenderInfoCodec` seam. Fixed
guest member offsets are confined to codecs. Host coverage is **671/671**;
native Image/RenderInfo ABI and complete MorphOS differential parity remain
progressive.

## Common-control method header

`MuiCommonControlDispatcher` now selects methods through the named
`MuiCommonMethodMessage` record via `TryReadMethodId`; the dispatcher no longer
reads the method word directly. Host coverage is **672/672**; native common
packet ABI and complete MorphOS differential parity remain progressive.

## Misc specialist typed method headers

Misc specialist lifecycle, Get/Set, pointer, pair, and gadget readers now
route selector checks through the named specialist method header before
decoding typed records. Host coverage is **703/703**; native Misc packet ABI
and complete MorphOS differential parity remain progressive.

## List advanced method headers

The List advanced packet family now routes InsertSingle, Insert, positional,
pointer, pair, and CreateImage selector checks through the named collection
method header through `MuiCollectionAdvancedMessageCodec.TryReadMethodIdValue`
before decoding each complete named record. Focused coverage is **3/3** and the
complete suite remains **1342/1342** in both SDK modes.
`CollectionListAdvancedMessageCodecRoot` returns **42** after **4,639/50,464**
instructions/cycles and emits **5,612/5,628/5,640** bytes for MC68000/020/040;
full native List advanced dispatch and MorphOS differential parity remain
progressive.

## List basic typed method headers

GetEntry, Select, Clear, and Sort packet readers now route selector checks
through `MuiCollectionBasicMessageCodec.TryReadMethodIdValue` before decoding
complete named records. Focused coverage is **4/4** and the complete suite
remains **1342/1342** in both SDK modes.
`CollectionListBasicMessageCodecRoot` returns **42** after **3,174/34,130**
instructions/cycles and emits **3,920/3,928/3,932** bytes for MC68000/020/040;
full native List basic dispatch and MorphOS differential parity remain
progressive.

## List edit method headers

The List edit packet family now routes CreateEditObject, Edit, EditDone, and
EndEdit selector checks through `MuiCollectionBasicMessageCodec.TryReadMethodIdValue`
before decoding each complete named record. Focused coverage is **2/2** and the
complete suite remains **1342/1342** in both SDK modes.
`CollectionListEditMessageCodecRoot` returns **42** after **4,555/49,288**
instructions/cycles and emits **5,452/5,472/5,488** bytes for MC68000/020/040;
full native List editing and MorphOS differential parity remain progressive.

## List record method headers

The List record packet family now routes Construct, Destruct, Display, Compare,
and TestPos selector checks through `MuiCollectionBasicMessageCodec.TryReadMethodIdValue`
before decoding each complete named record. Focused coverage is **2/2** and the
complete suite remains **1342/1342** in both SDK modes.
`CollectionListRecordMessageCodecRoot` returns **42** after **5,776/62,506**
instructions/cycles and emits **5,628/5,664/5,672** bytes for MC68000/020/040;
full native List dispatch and MorphOS differential parity remain progressive.

## SetAsString method header

SetAsString packet decoding now uses the named
`MuiSetAsStringMethodMessage` through
`MuiSetAsStringMessageCodec.TryReadMethodId`; complete attribute, format, and
value records remain codec-backed. Host coverage is **693/693**; native
SetAsString packet ABI and complete MorphOS differential parity remain
progressive.

## UserData method header

FindUData, GetUData, and SetUData packet selection now share the named
`MuiNotifyUserDataMethodMessage` through
`MuiNotifyUserDataMessageCodec.TryReadMethodId`; the existing operation records
remain named structs. Host coverage is **694/694**; native UserData packet ABI
and complete MorphOS differential parity remain progressive.

## Area activation method header

GoActive and GoInactive packet selection now uses the named
`MuiAreaActivationMethodMessage` through
`MuiAreaActivationMessageCodec.TryReadMethodId`; the shared flags record
remains struct-backed. Host coverage is **696/696**; native Area activation
packet ABI and complete MorphOS differential parity remain progressive.

## BoopsiQuery method header

BoopsiQuery packet decoding now obtains its selector through the named
`MuiBoopsiQueryMethodMessage` and
`MuiBoopsiQueryMessageCodec.TryReadMethodId` before validating the complete
geometry record. Host coverage is **695/695**; native BoopsiQuery packet ABI
and complete MorphOS differential parity remain progressive.

## Collection dispatcher method header

Collection dispatch entry points now select methods through the named
`MuiCollectionMethodMessage` via `MuiCollectionBasicMessageCodec.TryReadMethodId`.
Specialized packet codecs still validate complete records, while collection
dispatchers no longer read the method word directly. Host coverage is
**673/673**; native collection packet ABI and complete MorphOS differential
parity remain progressive.

## Application method header

The top-level dispatcher plus Application settings I/O, menu, queue, loop,
input, input-handler, application/window menu-state, window event-handler,
screen-depth, and selected window-set entry points now select methods through
the named `MuiApplicationMethodHeaderMessage` and
`MuiApplicationMethodHeaderCodec`. Their specialized packet codecs continue
to validate complete records. Host coverage is **674/674**; native
Application/Window packet ABI and complete MorphOS differential parity remain
progressive.

## Dirlist method header

Dirlist packet and full dispatch entry points now admit selectors through
`MuiDirlistMessageCodec.TryReadMethodIdValue` while retaining the named
`MuiDirlistMethodMessage`. Specialized Dirlist codecs continue to validate
complete records. Focused coverage is **1/1** and the complete host suite is
**1343/1343** in both SDK modes. Native `DirlistMethodHeaderCodecRoot` returns
**42** after **476/5,334** instructions/cycles; MC68000/020/040 artifacts are
**1,772/1,768/1,768** bytes with 8 reachable methods and zero-runtime/map
gates. Full native Dirlist/Volumelist dispatch and MorphOS differential parity
remain progressive.

## External wrapper method header

`MuiExternalWrapperDispatcher` now selects wrapper methods through the named
`MuiExternalMethodMessage` via `MuiExternalWrapperMessageCodec.TryReadMethodId`.
Specialized Boopsi/Dtpic packet codecs continue to validate complete records.
Host coverage is **676/676**; native external-wrapper packet ABI and complete
MorphOS differential parity remain progressive.

## Menu specialist method header

`MuiMenuSpecialistDispatcher` now selects Menustrip/Menu/Menuitem methods
through the named `MuiMenuSpecialistMethodMessage` via
`MuiMenuSpecialistMessageCodec.TryReadMethodId`. Specialized menu packet
codecs continue to validate complete records. Host coverage is **677/677**;
native menu-specialist packet ABI and complete MorphOS differential parity
remain progressive.

## Color specialist method header

`MuiColorSpecialistDispatcher` now selects pen/color specialist methods
through the named `MuiColorSpecialistMethodMessage` via
`MuiColorSpecialistMessageCodec.TryReadMethodId`. Specialized color packet
codecs continue to validate complete records. Host coverage is **678/678**;
native color-specialist packet ABI and complete MorphOS differential parity
remain progressive.

## Pop specialist method header

`MuiPopSpecialistDispatcher` now selects Popstring/Popobject/Popasl methods
through the named `MuiPopSpecialistMethodMessage` via
`MuiPopSpecialistMessageCodec.TryReadMethodId`. Specialized Pop packet codecs
continue to validate complete records. Host coverage is **679/679**; native
Pop-specialist packet ABI and complete MorphOS differential parity remain
progressive.

## Process specialist method header

`MuiProcessSpecialistDispatcher` now selects Process/Slave/Semaphore methods
through the named `MuiProcessSpecialistMethodMessage` via
`MuiProcessSpecialistMessageCodec.TryReadMethodId`. Specialized Process and
Slave packet codecs continue to validate complete records. Host coverage is
**680/680**; native Process/Slave packet ABI and complete MorphOS differential
parity remain progressive.

## Listtree method header

`MuiListtreeDispatcher` now admits external Listtree.mcc selectors through
`MuiListtreeMessageCodec.TryReadMethodIdValue` and retains the named
`MuiListtreeMethodMessage` for consumers. Specialized Listtree packet codecs
continue to validate complete records. Focused coverage is **1/1** and the
complete host suite is **1343/1343** in both SDK modes. Native
`ListtreeMethodHeaderCodecRoot` returns **42** after **476/5,334**
instructions/cycles; MC68000/020/040 artifacts are
**2,508/2,504/2,504** bytes with 8 reachable methods and zero-runtime/map
gates. Full native Listtree dispatch and MorphOS differential parity remain
progressive.

## Layout method header

`MuiLayoutDispatcher` now selects layout, activation, drag, and fallback
methods through the named `MuiLayoutMethodMessage` via
`MuiLayoutPacketCodec.TryReadMethodId`; all fixed layout packet readers are
also kept in that codec boundary. Host coverage is **682/682**; native layout
packet ABI and complete MorphOS differential parity remain progressive.

## Headless dispatcher method header

All headless dispatcher entry points retain the named
`MuiHeadlessMethodMessage` consumer record while admitting the fixed method
word through `MuiHeadlessMessageCodec.TryReadMethodIdValue` at the codec
boundary. Specialized Notify, Dataspace, persistence, store, family, group,
and semaphore codecs continue to validate complete records. Focused coverage
is **1/1**, the complete host suite is **1343/1343** in both SDK modes, and
`HeadlessMethodHeaderCodecRoot` is **1,000/1,000/1,000** bytes with 5
reachable methods and zero-runtime gates; MC68000 returns **42** after
**251 instructions / 2,550 cycles**. Full native Headless dispatch and
complete MorphOS differential parity remain progressive.

## Area drag method header

`MuiAreaDragCore.Dispatch` now selects drag methods through the named
`MuiAreaDragMethodMessage` via `MuiAreaDragMessageCodec.TryReadMethodId`.
Specialized begin, drop, event, finish, query, and report codecs continue to
validate complete records. Host coverage is **684/684**; native Area drag
packet ABI and complete MorphOS differential parity remain progressive.

## Family mutation method header

Family mutation record and projection entry points now select methods through
the named `MuiFamilyMethodMessage` via
`MuiFamilyMutationMessageCodec.TryReadMethodId`. Specialized child, insert,
transfer, reorder, and sort packet codecs continue to validate complete
records. Host coverage is **685/685**; native Family packet ABI and complete
MorphOS differential parity remain progressive.

## Store method header

Store/datamap/objectmap live and packet-only dispatch entry points now select
methods through the named `MuiStoreMethodMessage` via
`MuiStoreMessageCodec.TryReadMethodId`. Specialized datamap/objectmap packet
codecs continue to validate complete records. Host coverage is **686/686**;
native Store packet ABI and complete MorphOS differential parity remain
progressive.

## Dataspace method header

Dataspace packet-only method extraction now admits its scalar selector through
`MuiDataspaceMessageCodec.TryReadMethodIdValue` before named Add, Find, Get,
Merge, Remove, and Clear records are validated. Focused coverage is **1/1** and
the complete host suite is **1343/1343** in both SDK modes.
`DataspaceMethodHeaderCodecRoot` returns **42** after **476 instructions /
5,334 cycles**; MC68000/020/040 HUNK sizes are **1,776/1,772/1,772** bytes
with 8 reachable methods and zero-runtime map gates. Full native Dataspace
dispatch and MorphOS differential parity remain progressive.

## Dataspace-IFF method header

Dataspace-IFF packet-only method extraction now admits scalar selectors through
`MuiDataspaceIffMessageCodec.TryReadMethodIdValue` before named ReadIFF and
WriteIFF records are validated. Focused coverage is **1/1** and the complete
host suite is **1343/1343** in both SDK modes.
`DataspaceIffMethodHeaderCodecRoot` returns **42** after **386 instructions /
4,158 cycles**; MC68000/020/040 HUNK sizes are **1,408/1,404/1,404** bytes
with 7 reachable methods and zero-runtime map gates. Full native IFF dispatch
and MorphOS differential parity remain progressive.

## NotifyWrite method header

NotifyWrite packet-only method extraction now uses
`MuiNotifyWriteMessageCodec.TryReadMethodIdValue` for scalar selector admission
before the named `MuiNotifyWriteMethodMessage` record. Specialized WriteLong
and WriteString codecs continue to validate complete records. Focused coverage
is **1/1** and the complete host suite is **1343/1343** in both SDK modes.
Native `NotifyWriteMethodHeaderCodecRoot` returns **42** after **482/5,376**
instructions/cycles; MC68000/020/040 artifacts are
**1,668/1,664/1,664** bytes with 8 reachable methods and zero-runtime/map
gates. Full native NotifyWrite dispatch and MorphOS differential parity remain
progressive.

## CallHook method header

CallHook packet decoding now obtains its selector through the named
`MuiCallHookMethodMessage` and `MuiCallHookMessageCodec.TryReadMethodId` before
validating the complete hook envelope. The existing named hook and parameter
fields remain intact. Host coverage is **691/691**; native CallHook packet ABI
and complete MorphOS differential parity remain progressive.

## Object persistence method header

Object persistence packet-only method extraction now uses the named
`MuiObjectPersistenceMethodMessage` through
`MuiObjectPersistenceMessageCodec.TryReadMethodId`; the existing scalar helper
remains a struct-backed adapter. The Export and Import codecs continue to
validate complete records. Host coverage is **690/690**; native object
persistence packet ABI and complete MorphOS differential parity remain
progressive.

## GetConfigItem method header

GetConfigItem packet decoding now obtains its selector through the named
`MuiGetConfigItemMethodMessage` and
`MuiGetConfigItemMessageCodec.TryReadMethodId` before validating the complete
config-item envelope. Host coverage is **692/692**; native GetConfigItem
packet ABI and complete MorphOS differential parity remain progressive.

## Layout service packet header

`MuiLayoutServiceCore.Dispatch` now decodes guest packets through the named
`MuiLayoutMessage` using `MuiLayoutPacketCodec.TryReadLayout`. Fixed layout
packet fields remain in the central struct-backed codec, keeping the service
freestanding and offset-light. Host coverage is **711/711**; native
layout-service packet ABI and complete MorphOS differential parity remain
progressive.

## Group change method header

Packet-only Group change dispatch now admits its selector through the scalar
`MuiGroupChangeMessageCodec.TryReadMethodIdValue` seam while retaining the
named `MuiGroupChangeMessage` and `MuiGroupExitChange2Message` records. Focused
coverage is **2/2** and the complete suite is **1342/1342** in both SDK modes.
`GroupChangePacketsRoot` returns **42** with MC68000/020/040 artifacts
**3,580/3,556/3,564** bytes and zero-runtime gates; full Group-change dispatch
and MorphOS differential parity remain progressive.

## Notify method header

Notify, KillNotify, KillNotifyObject, Set, MultiSet, and FindObject readers now
consume the named `MuiNotifyMethodMessage` through
`MuiNotifyPacketCodec.TryReadMethodId` before reading their payload records;
native selector admission uses the scalar
`MuiNotifyPacketCodec.TryReadMethodIdValue` helper while public consumers keep
the named structs. Host coverage is **1343/1343**; native evidence is
**4,008/4,068/4,072** bytes, **14** reachable methods, and **2/2** focused
coverage. Full native Notify dispatch and complete MorphOS differential parity
remain progressive.

## UpdateConfig method header

UpdateConfig full-packet validation and redraw-entry writes now consume the
named `MuiUpdateConfigMethodMessage` through
`MuiUpdateConfigCore.TryReadMethodId`; native selector admission uses the
scalar `MuiUpdateConfigCore.TryReadMethodIdValue` helper while redraw tables
remain explicit value-type records. Host coverage is **1343/1343**; native
evidence is **1,576/1,572/1,572** bytes, **8** reachable methods, and **2/2**
focused coverage. Full native redraw-table dispatch and complete MorphOS
differential parity remain progressive.

## Group ordering method header

Group MoveMember, Reorder, and Sort readers now consume the named
`MuiGroupOrderingMethodMessage` through
`MuiGroupOrderingMessageCodec.TryReadMethodId` before reading their payload
records; native selector admission uses the scalar
`MuiGroupOrderingMessageCodec.TryReadMethodIdValue` helper while public
consumers keep named structs. Host coverage is **1343/1343**; native evidence
is **5,152/5,092/5,112** bytes, **22** reachable methods, and **2/2** focused
coverage. Full native Group ordering dispatch and complete MorphOS differential
parity remain progressive.

## Application input method headers

Application ReturnId, Input, InputBuffered, and InputHandler readers now use
the named `MuiApplicationMethodHeaderMessage` codec before consuming typed
payload records; native selector admission uses the packet-kind-aware scalar
`MuiApplicationInputPacketCodec.TryReadMethodIdValue` helper while public
consumers keep named structs. Host coverage is **1343/1343**; native evidence
is **1,892/1,884/1,888** bytes, **8** reachable methods, and **1/1** focused
coverage. Full native input queue/handler dispatch and complete MorphOS
differential parity remain progressive.

## Application queue method headers

Application PushMethod and UnpushMethod readers now use the named
`MuiApplicationMethodHeaderMessage` codec before consuming queue payload
records; native selector admission uses the scalar
`MuiApplicationQueuePacketCodec.TryReadMethodIdValue` helper while public
consumers keep named queue structs. Host coverage is **1343/1343**; native
evidence is **1,760/1,752/1,752** bytes, **8** reachable methods, and **1/1**
focused coverage. Full native queue-tail dispatch and complete MorphOS
differential parity remain progressive.

## Application presentation method headers

Application ShowHelp and AboutMUI readers now use the named
`MuiApplicationMethodHeaderMessage` codec before consuming presentation
payload records; native selector admission uses the scalar
`MuiApplicationPresentationPacketCodec.TryReadMethodIdValue` helper while
public consumers keep named presentation structs. Host coverage is **1343/1343**;
native evidence is **1,800/1,792/1,792** bytes, **8** reachable methods, and
**1/1** focused coverage. Full native presentation dispatch and complete
MorphOS differential parity remain progressive.

## Application settings method headers

Application SetConfigItem, OpenConfigWindow, BuildSettingsPanel, and Load/Save
settings readers now use the named `MuiApplicationMethodHeaderMessage` codec
before consuming settings payload records. Scalar selector admission now uses
`MuiApplicationSettingsPacketCodec.TryReadMethodIdValue` while public
consumers keep named packet structs. Host coverage is **1343/1343**, focused
coverage is **1/1**, and the native method-header root is
**1,960/1,952/1,956** bytes with 8 reachable methods and zero-runtime gates.
Full native settings dispatch and complete MorphOS differential parity remain
progressive.

## Application method-packet headers

Application ConfigId, CheckRefresh, Execute/Run, window-method, and Snapshot
readers now use the named `MuiApplicationMethodHeaderMessage` codec before
consuming typed payload records. Scalar selector admission now uses
`MuiApplicationMethodPacketCodec.TryReadMethodIdValue` while public consumers
keep named application/window packet structs. Host coverage is **1343/1343**,
focused coverage is **1/1**, and the native method-header root is
**1,972/1,960/1,964** bytes with 8 reachable methods and zero-runtime gates.
Full native method-packet dispatch and complete MorphOS differential parity
remain progressive.

## Window cycle-chain method header

Window SetCycleChain packet decoding now admits the scalar selector through
`MuiWindowCycleChainPacketCodec.TryReadMethodIdValue`, while the named
`MuiWindowCycleChainMessage` record retains the first-object field and bounded
inline vector payload. Focused coverage is **1/1** and the complete host suite
is **1343/1343** in both SDK modes. `WindowCycleChainMethodHeaderCodecRoot`
returns **42** after **348 instructions / 3,690 cycles**; MC68000/020/040 HUNK
sizes are **1,540/1,536/1,536** bytes with 8 reachable methods and
zero-runtime map gates. Full native cycle-chain mutation and MorphOS
differential parity remain progressive.

## Application/window menu method headers

Application and window menu query/set readers now admit scalar selectors through
`MuiApplicationMenuPacketCodec.TryReadMethodIdValue` before consuming named
MenuId/State payload records. Focused coverage is **1/1** and the complete host
suite is **1343/1343** in both SDK modes. `ApplicationMenuMethodHeaderCodecRoot`
returns **42** after **966 instructions / 10,482 cycles**; MC68000/020/040 HUNK
sizes are **1,984/1,976/1,980** bytes with 8 reachable methods and
zero-runtime map gates. Full native menu dispatch and MorphOS differential
parity remain progressive.

## Window event-handler method header

Window AddEventHandler and RemoveEventHandler packet decoding now admits scalar
selectors through `MuiWindowEventHandlerPacketCodec.TryReadMethodIdValue`
before consuming the named handler pointer field. Focused coverage is **1/1**
and the complete host suite is **1343/1343** in both SDK modes.
`WindowEventHandlerMethodHeaderCodecRoot` returns **42** after **551
instructions / 5,994 cycles**; MC68000/020/040 HUNK sizes are
**1,672/1,668/1,668** bytes with 8 reachable methods and zero-runtime map
gates. Full native handler registration/delivery and MorphOS differential
parity remain progressive.

The public `MuiWindowEventHandlerPacketCore` helper now uses the named
`MuiWindowEventHandlerPacket` record and central codec for both reads and
writes. Its packed guest offsets are isolated at the codec boundary, and
unknown methods are rejected before a packet is exposed to callers. Host
coverage is **735/735**; native packet ABI and complete MorphOS differential
parity remain progressive.

`MuiCollectionDispatcher.TryDispatch` now delegates exclusively to the
named-codec `TryDispatchPacket` route. The former duplicate offset-based
fallback for List, Listview, Stringscroll, and Floattext is no longer a live
decode path; malformed recognized packets are claimed by the typed codec and
unclaimed objects continue outward. Host coverage is **736/736**; native
collection packet ABI and complete MorphOS differential parity remain
progressive.

## Datamap/Objectmap typed method headers

Datamap and Objectmap packet decoding now admits scalar selectors through
`MuiStoreMessageCodec.TryReadMethodIdValue` before consuming named set, get,
key, counter, and clear records. Focused coverage is **1/1** and the complete
host suite is **1343/1343** in both SDK modes. `StoreMethodHeaderCodecRoot`
returns **42** after **476 instructions / 5,334 cycles**; MC68000/020/040 HUNK
sizes are **1,808/1,804/1,804** bytes with 8 reachable methods and
zero-runtime map gates. Full native store dispatch and MorphOS differential
parity remain progressive.

The live external BOOPSI wrapper now constructs its `OM_SET`, `OM_GET`,
`GM_RENDER`, inline `TagItem`, and result-word scratch frames through named
`MuiExternalBoopsi*` records and `MuiExternalBoopsiPacketCodec`. Packed guest
offsets remain confined to that codec for geometry, drawing, and attribute
pass-through. Host coverage is **738/738**; native external BOOPSI packet ABI
and complete MorphOS differential parity remain progressive.

Headless object creation now consumes the shared named
`MuiAslTagItemRecord` through `MuiAslTagItemCodec`. `TAG_DONE`, `TAG_IGNORE`,
`TAG_MORE`, and `TAG_SKIP` traversal therefore uses named `Tag`/`Data` fields;
packed guest reads remain confined to the codec boundary. Host coverage is
**739/739**; `HeadlessCreationTagCodecRoot` is **2,716/2,708/2,720** bytes
for MC68000/020/040 and returns **42** after **1,980/19,624**
instructions/cycles. The complete host suite is **1344/1344** in both SDK
modes. Full headless object-factory behavior and complete MorphOS differential
parity remain progressive.

Family projection add/remove/insert/transfer/reorder/sort paths now consume
named `MuiFamilyMutationListRecord` and `MuiFamilyMutationVectorEntry` records
through central codecs. Packed list and vector field access remains confined
to those adapters. Host coverage remains in the complete **1344/1344** suite;
`FamilyProjectionListVectorCodecRoot` is **3,572/3,616/3,616** bytes for
MC68000/020/040 and returns **42** after **2,355/22,676** instructions/cycles.
Full native Family mutation behavior and complete MorphOS differential parity
remain progressive.

`MUI_MakeObjectA` now emits generated attributes and `TAG_DONE` through the
shared named `MuiAslTagItemRecord` and `MuiAslTagItemCodec`. Button, control,
label, and menu-family construction paths therefore keep packed TagItem access
at the codec boundary. Host coverage is **741/741**; native MakeObjectA
TagItem ABI and complete MorphOS differential parity remain progressive.

The private `MUIA_List_TitleArray` pointer table now uses the named
`MuiListPointerSlotRecord` and `MuiListPointerSlotCodec`; its state table
pointer is typed as an `APTR`. Construction, validation, copying, and
terminator handling keep packed slot access at the codec boundary. Host
coverage remains in the complete **1344/1344** suite;
`ListPointerSlotCodecRoot` is **2,396/2,420/2,420** bytes for MC68000/020/040
and returns **42** after **2,058/19,428** instructions/cycles. Full native
TitleArray behavior and complete MorphOS differential parity remain
progressive.

List `StringArray` validation, duplication, display-copy, lookup, and teardown
now reuse `MuiListPointerSlotRecord` and `MuiListPointerSlotCodec`, sharing the
typed pointer-table boundary with TitleArray. Host coverage remains **742/742**;
native List StringArray ABI and complete MorphOS differential parity remain
progressive.

List external entry vectors, StringArray edit/copy paths, display arrays,
insertion/sort inputs, and `GetEntry` storage now reuse
`MuiListPointerSlotRecord` and `MuiListPointerSlotCodec`. Host coverage remains
in the complete **1344/1344** suite; `ListPointerVectorCodecRoot` is
**2,236/2,236/2,236** bytes for MC68000/020/040 and returns **42** after
**1,253/12,184** instructions/cycles. Full native List pointer-vector
consumers and complete MorphOS differential parity remain progressive.

The private `MUIA_List_ColumnOrder` state now stores its BYTE* payload in a
named `APTR` field of `MuiListColumnOrderState`. State construction,
validation, copying, freeing, and display-column lookup consume that typed
field. Host coverage is **743/743**; native byte-cursor ABI is covered by
`ListColumnOrderByteCodecRoot` (**1,096/1,096/1,096** bytes, **42**, and
**365/3,924** instructions/cycles). The complete suite remains **1344/1344**
in both SDK modes; full native ColumnOrder behavior and complete MorphOS
differential parity remain progressive.

ExternalWrapper creation-tag patching, remembered BOOPSI tag storage and
reapplication, and OM_UPDATE notification walks now use the shared named
`MuiAslTagItemRecord` and `MuiAslTagItemCodec`. Host coverage is **744/744**;
`ExternalWrapperTagItemCodecRoot` now returns **42** after **27,441/266,026**
instructions/cycles, with MC68000/020/040 artifacts of
**26,288/26,956/26,448** bytes, 97 reachable methods, and zero-runtime gates.
The complete host suite remains **1344/1344** in both SDK modes; full native
ExternalWrapper creation/update behavior and complete MorphOS differential
parity remain progressive.

Family reorder and Group ordering vector consumers now use the named
`MuiFamilyMutationVectorEntry` and `MuiFamilyMutationVectorCodec`, with Family
reorder address arithmetic guarded before codec entry. Host coverage is
**745/745**; native Family/Group vector ABI and complete MorphOS differential
parity remain progressive.

Poplist caller-array traversal, materialized-array copying and terminator
writes, and selection lookup now use the named `MuiPoplistArrayEntry` and
`MuiPoplistArrayEntryCodec`. Host coverage is **746/746**;
`PoplistArrayCodecRoot` now returns **42** after **134,781/1,384,034**
instructions/cycles, with MC68000/020/040 artifacts of
**17,880/19,680/19,276** bytes, 56 reachable methods, and zero-runtime gates.
The complete host suite remains **1344/1344** in both SDK modes; full native
Poplist requester behavior and complete MorphOS differential parity remain
progressive.

The `MUIM_MultiSet` target vector now uses the named `MuiMultiSetTargetEntry`
and `MuiMultiSetTargetEntryCodec` during target counting and mutation. Host
coverage is **747/747**; native Notify MultiSet ABI and complete MorphOS
differential parity remain progressive.

Cycle/Radio choice-array counting, Radio child construction, and active
selection lookup now use the named `MuiChoiceEntry` and `MuiChoiceEntryCodec`.
Host coverage is **748/748**; native common-control choice-array ABI and
complete MorphOS differential parity remain progressive.

`MUI_MakeObjectA` Cycle/Radio entry-vector validation now reuses the named
`MuiChoiceEntry` and `MuiChoiceEntryCodec`, rejecting malformed caller arrays
before object creation. Host coverage is **749/749**; native MakeObject
choice-vector ABI and complete MorphOS differential parity remain progressive.

The inline `MUIM_Slave_Dispatch` ULONG vector now uses the named
`MuiProcessDispatchArgumentSlot` and `MuiProcessDispatchArgumentSlotCodec` for
packet reads and reconstructed BOOPSI message writes. Host coverage is
**750/750**; native Process/Slave dispatch ABI and complete MorphOS
differential parity remain progressive.

`MUIM_Notify` follow-parameter trigger-value substitution now uses the named
`MuiNotifyFollowParameterSlot` and `MuiNotifyFollowParameterSlotCodec`.
Host coverage is **751/751**; native Notify follow-vector ABI and complete
MorphOS differential parity remain progressive.

Requester formatting for `MUI_RequestA` and `MUI_RequestObjectA` now reads
ULONG parameters through the named `MuiRequesterParameterSlot` and
`MuiRequesterParameterSlotCodec`. Host coverage is **752/752**; native
requester parameter-vector ABI and complete MorphOS differential parity remain
progressive.

Dynamic `MUIM_UpdateConfig` redraw-table object writes now use the named
`MuiUpdateConfigObjectSlot` and `MuiUpdateConfigObjectSlotCodec`. Host coverage
is **753/753**; native UpdateConfig redraw-table ABI and complete MorphOS
differential parity remain progressive.

The public 12-byte `MUI_RGBColor` block now uses the named
`MuiColorRgbRecord` and `MuiColorRgbCodec` throughout color-specialist
component, copy, and packed-color paths. Host coverage is **754/754**; native
color RGB ABI and complete MorphOS differential parity remain progressive.

Adopted Filepanel rows now use the named `MuiFilepanelRowRecord` and
`MuiFilepanelRowCodec` during row addition and recursive disposal. Host
coverage is **755/755**; native Filepanel row-table ABI and complete MorphOS
differential parity remain progressive.

Title page creation, lookup, close/compaction, and clearing now use the named
`MuiTitlePageRecord` and `MuiTitlePageCodec`. Host coverage is **756/756**;
native Title page-table ABI and complete MorphOS differential parity remain
progressive.

Mccprefs gadget registration replacement, append, unregister compaction, and
clearing now use the named `MuiMccprefsRegistryRecord` and
`MuiMccprefsRegistryCodec`. Host coverage is **757/757**; native Mccprefs
registry ABI and complete MorphOS differential parity remain progressive.

Private Scrmodelist mode append and indexed lookup now use the named
`MuiScrmodelistModeRecord` and `MuiScrmodelistModeCodec`. Host coverage is
**758/758**; native Scrmodelist mode-table ABI and complete MorphOS differential
parity remain progressive.

Group child-list and Application window-list projections now use the named
`MuiGroupExecListRecord` and `MuiGroupExecListCodec` for complete 14-byte Exec
`List` writes, including padding. Host coverage is **760/760**; native Exec
List projection ABI and complete MorphOS differential parity remain
progressive.

Menu/Menustrip/Menuitem sidecars now use the named `MuiMenuSpecialistState`
and `MuiMenuSpecialistStateCodec` for class, ownership, flags, trigger, and
notification state. Host coverage is **761/761**; native Menu sidecar ABI and
complete MorphOS differential parity remain progressive.

Pendisplay/Colorfield/Coloradjust/Palette/Penadjust instance blocks now use
the named `MuiColorSpecialistState` and `MuiColorSpecialistStateCodec` for
lifecycle, pointers, flags, and notifications. Host coverage is **762/762**;
native Color specialist ABI and complete MorphOS differential parity remain
progressive.

Popstring/Popobject/Poplist/Popasl/Popcolor/Poppen instances now use the named
`MuiPopSpecialistState` and `MuiPopSpecialistStateCodec` for class, ownership,
hooks, arrays, ASL state, selection, and notifications. Host coverage is
**763/763**; native Pop specialist ABI and complete MorphOS differential
parity remain progressive.

Misc specialist instances now use the named `MuiMiscSpecialistHeader` and
`MuiMiscSpecialistHeaderCodec` for shared class, flags, and notification state
while retaining complete 196-byte validation and class-specific regions. Host
coverage is **764/764**; native Misc specialist ABI and complete MorphOS
differential parity remain progressive.

Title specialists now use the named `MuiMiscTitleState` and
`MuiMiscTitleStateCodec` for page storage, counts, active-page state, sequence,
position, priority, and close policy. Host coverage is **765/765**; native
Title specialist ABI and complete MorphOS differential parity remain
progressive.

Filepanel service state now uses the named `MuiMiscFilepanelServiceState` and
`MuiMiscFilepanelServiceStateCodec` for FilterFunc, ASL state, adopted rows,
row count, and hook scratch. Host coverage is **766/766**; native Filepanel
service-state ABI and complete MorphOS differential parity remain progressive.

Misc owned strings now use the named `MuiMiscOwnedStringSlot` and
`MuiMiscOwnedStringSlotCodec` across Keyadjust, Argstring, and Filepanel.
Host coverage is **767/767**; native Misc string-slot ABI and complete
MorphOS differential parity remain progressive.

Mccprefs registry state now uses the named `MuiMiscMccprefsState` and
`MuiMiscMccprefsStateCodec` for registry storage/count and config-transfer
references. Host coverage is **768/768**; native Mccprefs state ABI and
complete MorphOS differential parity remain progressive.

Private Scrmodelist state now uses the named `MuiMiscScrmodelistState` and
`MuiMiscScrmodelistStateCodec` for mode-table storage/count and active-mode
state. Host coverage is **769/769**; native Scrmodelist state ABI and complete
MorphOS differential parity remain progressive.

Aboutmui and Panel references now use the named `MuiMiscWindowPanelState` and
`MuiMiscWindowPanelStateCodec` for caller-owned application/window bindings.
Host coverage is **770/770**; native Aboutmui/Panel reference ABI and complete
MorphOS differential parity remain progressive.

FSProtectionBits flags now use the named `MuiMiscProtectionState` and
`MuiMiscProtectionStateCodec`. Host coverage is **771/771**; native
FSProtectionBits ABI and complete MorphOS differential parity remain
progressive.

Fontdisplay natural-size state now uses the named `MuiMiscFontdisplaySize` and
`MuiMiscFontdisplaySizeCodec` for MUIM_Draw width/height publication. Host
coverage is **772/772**; native Fontdisplay size ABI and complete MorphOS
differential parity remain progressive.

External-wrapper magic, class, and lifecycle flags now use the named
`MuiExternalWrapperHeader` and `MuiExternalWrapperHeaderCodec`. Host coverage
is **773/773**; native external-wrapper header ABI and complete MorphOS
differential parity remain progressive.

Boopsi min/max dimensions and creation-tag IDs now use the named
`MuiExternalBoopsiGeometryState` and `MuiExternalBoopsiGeometryCodec`. Host
coverage is **774/774**; native Boopsi geometry ABI and complete MorphOS
differential parity remain progressive.

Setup-time Window, Screen, DrawInfo, and RastPort references now use the named
`MuiExternalDisplayState` and `MuiExternalDisplayStateCodec`. Host coverage is
**775/775**; native display-state ABI and complete MorphOS differential parity
remain progressive.

Boopsi private/public class references, opened-class ownership, object handles,
and creation tags now use the named `MuiExternalBoopsiResourceState` and
`MuiExternalBoopsiResourceCodec`. Host coverage is **776/776**; native Boopsi
resource ABI and complete MorphOS differential parity remain progressive.

Remember-buffer storage/count and the shared work scratch pointer now use the
named `MuiExternalScratchState` and `MuiExternalScratchStateCodec`. Host
coverage is **777/777**; native scratch-state ABI and complete MorphOS
differential parity remain progressive.

Dtpic caller/owned names, picture handles, alpha, minimums, and natural
dimensions now use the named `MuiExternalDtpicState` and
`MuiExternalDtpicStateCodec`. Host coverage is **778/778**; native Dtpic state
ABI and complete MorphOS differential parity remain progressive.

External-wrapper notification attribute/value and count now use the named
`MuiExternalNotificationState` and `MuiExternalNotificationStateCodec`. Host
coverage is **779/779**; native notification-state ABI and complete MorphOS
differential parity remain progressive.

The external-wrapper raw instance audit removed unused offset helpers. Fixed
wrapper state is now represented through named records/codecs, with no direct
instance-field reads or writes in the MUI library. Host coverage remains
**779/779**; native ABI and differential parity remain progressive.

The color specialist's internally authored 32-byte `MUI_PenSpec` copy now uses
the named `MuiColorPenSpecRecord` and `MuiColorPenSpecCodec`, including
reserved-word preservation. Host coverage is **780/780**; native pen-spec ABI
and differential parity remain progressive.

Specialized Color, Popstring, Fontdisplay, and external-wrapper AskMinMax paths
now reuse the named `MuiMinMaxValues`/`MuiMinMaxRecordCodec` boundary. Host
coverage is **781/781**; native min/max ABI and differential parity remain
progressive.

List FORMAT descriptors now represent PREPARSE and owned PREPARSE storage as
named `APTR` fields in `MuiListFormatDescriptor`; only the named descriptor
codec converts those fields to the 40-byte guest wire layout. Replacement,
cleanup, and teardown preserve ownership behavior. Host coverage is
**786/786**; native FORMAT descriptor ABI and complete MorphOS differential
parity remain progressive.

Caller-owned List records now expose their self-describing four-byte size
header through the named `MuiListOwnedRecordHeader` and
`MuiListOwnedRecordHeaderCodec`; `FreeOwnedRecord` no longer reads the size
from an ad hoc offset. Host coverage is **787/787**; native owned-record ABI
and complete MorphOS differential parity remain progressive.

Dataspace IFF stream entries now use the named
`MuiDataspaceIffEntryHeader`/`MuiDataspaceIffEntryHeaderCodec` boundary for
their `Id` and `Length` fields. `ReadIFF` and `WriteIFF` preserve short
transfers and entry-size validation without ad hoc header access. Host
coverage is **788/788**; native Dataspace IFF header ABI and complete MorphOS
differential parity remain progressive.

`MUIM_Window_SetCycleChain` vector elements now use the named
`MuiApplicationWindowCycleChainSlot`/`MuiApplicationWindowCycleChainSlotCodec`
boundary. `SetCycleChain` preserves the NULL-terminated APTR contract,
failure-atomic replacement, and chain ownership. Host coverage is **789/789**;
native cycle-chain vector ABI and complete MorphOS differential parity remain
progressive.

Application input signal publication now uses the named
`MuiApplicationWindowSignalStorage`/`MuiApplicationWindowSignalStorageCodec`
boundary. ReturnID delivery still clears storage, while signal polling writes
the pending mask and keeps null/unmapped callers harmless. Host coverage is
**790/790**; native signal-storage ABI and complete MorphOS differential parity
remain progressive.

`MUIA_Application_UsedClasses` STRPTR vector elements now use the named
`MuiApplicationUsedClassesVectorEntry`/`MuiApplicationUsedClassesVectorEntryCodec`
boundary. Validation preserves NULL termination and bounded guest-string
checks without the previous direct helper construction. Host coverage is
**791/791**; native UsedClasses vector ABI and complete MorphOS differential
parity remain progressive.

Application settings headers and records now use the named
`MuiApplicationSettingsHeaderCodec`/`MuiApplicationSettingsRecordCodec`
boundary. The packet transport preserves magic/version, record counts,
payload lengths, and record key/length fields without embedding their offsets
in the transport adapter. Host coverage is **792/792**; native settings
transport ABI and complete MorphOS differential parity remain progressive.

Datamap/Objectmap iteration now represents its caller-owned four-byte ordinal
as `MuiStoreIterationCounter` and routes reads/advancement through
`MuiStoreIterationCounterCodec`. Traversal and exhaustion behavior are
unchanged. Host coverage is **793/793**; native Store iteration ABI and
complete MorphOS differential parity remain progressive.

`MUIM_GetConfigItem` result publication now uses the named
`MuiNotifyConfigStorage`/`MuiNotifyConfigStorageCodec` boundary. Public-screen
selection, validation ordering, capability failures, and null/unmapped
rejection are unchanged. Host coverage is **794/794**; native config-storage
ABI and complete MorphOS differential parity remain progressive.

`MUIM_Listtree_TestPos` now publishes its mixed-width 12-byte result through
the named `MuiListtreeTestPosResult`/`MuiListtreeTestPosResultCodec` boundary.
Node, drop flags, list entry, and list flags retain their MorphOS layout and
semantics. Host coverage is **795/795**; native Listtree TestPos ABI and
complete MorphOS differential parity remain progressive.

The repeated caller-owned four-byte ULONG result slot used by `opGet` paths is
now represented by `MuiGuestUlongStorage` and
`MuiGuestUlongStorageCodec`. Specialist, external-wrapper, common-control,
Listtree, and Notify UserData publication keeps its existing result and
mapping behavior. Host coverage is **796/796**; native opGet storage ABI and
complete MorphOS differential parity remain progressive.

Application/Window queue nodes now expose their inline method payload through
`MuiApplicationWindowNodeCodec.TryGetPayload`. Pushed-method copying and
dispatch plus timed input-handler dispatch retain their bounded mapping and
cleanup behavior without recomputing the payload offset in production logic.
Host coverage is **797/797**; native inline-payload ABI and complete MorphOS
differential parity remain progressive.

Family reorder/sort object-pointer vectors now use named base and indexed-entry
helpers on `MuiFamilyMutationMessageCodec`. Dispatch, vector construction, and
projection traversal preserve NULL termination, mapping/overflow checks, and
ordering behavior. Host coverage is **798/798**; native Family vector ABI and
complete MorphOS differential parity remain progressive.

CallHook invocation now obtains the A1 first-parameter address through
`MuiCallHookMessageCodec.TryGetFirstParameter`, preserving fixed packet
validation, mapping checks, and callback delivery. Host coverage is
**799/799**; native CallHook tail ABI and complete MorphOS differential parity
remain progressive.

Application command-table validation now obtains indexed 36-byte records through
`MuiApplicationCommandTableCodec.TryGetEntry`, preserving NULL termination,
bounded mapping, overflow, and string validation. Host coverage is **800/800**;
native Application command-table ABI and complete MorphOS differential parity
remain progressive.

UsedClasses validation now obtains indexed STRPTR slots through
`MuiApplicationUsedClassesVectorCodec.TryGetEntry`, preserving NULL
termination, bounded mapping/overflow checks, and string validation. Host
coverage is **801/801**; native UsedClasses vector ABI and complete MorphOS
differential parity remain progressive.

Application Save/Load traversal now obtains depth-indexed guest stack frames
through `MuiApplicationPersistenceFrameCodec.TryGetFrame`. Preorder traversal,
child-frame writes, cleanup, and malformed-stack checks remain unchanged. Host
coverage is **802/802**; native persistence frame ABI and complete MorphOS
differential parity remain progressive.

Application WindowList projection construction now obtains indexed current and
successor entries through `MuiApplicationWindowListEntryVectorCodec.TryGetEntry`.
Exec links, projection ordering, cleanup, and malformed-range checks remain
unchanged. Host coverage is **803/803**; native WindowList entry ABI and
complete MorphOS differential parity remain progressive.

AppMessage validation now obtains indexed Workbench argument records through
`MuiWorkbenchArgumentVectorCodec.TryGetEntry`, preserving the 8-byte record
shape, full-span mapping, overflow checks, and guest string validation. Host
coverage is **804/804**; native AppMessage argument ABI and complete MorphOS
differential parity remain progressive.

ASL TagItem traversal now uses the named `MuiAslTagItemCursor` and
`MuiAslTagItemVectorCodec` helpers for TAG_MORE, TAG_SKIP, TAG_IGNORE, and
normal successor movement. Control-tag semantics, bounded mapping, overflow
checks, and malformed-list rejection remain unchanged. Host coverage is
**805/805**; native ASL TagItem ABI and complete MorphOS differential parity
remain progressive.

Group child-list projection construction and the two-entry qualification seam
now obtain indexed entries through
`MuiGroupChildListEntryVectorCodec.TryGetEntry`. Exec links, ordering,
cleanup, bounded mapping, overflow checks, and malformed-range rejection
remain unchanged. Host coverage is **806/806**; native Group child-list ABI
and complete MorphOS differential parity remain progressive.

Notify UserData traversal now obtains current and child stack frames through
the named `MuiUDataTraversalCursor` and
`MuiUDataTraversalFrameCodec.TryGetEntry` helpers. Preorder traversal, depth
limits, cleanup, bounded mapping, overflow checks, and malformed-range
rejection remain unchanged. Host coverage is **808/808**; native Notify
UserData frame ABI and complete MorphOS differential parity remain progressive.

MUIM_MultiSet target traversal now obtains each 4-byte target slot through the
named `MuiMultiSetTargetVectorCursor` and
`MuiMultiSetTargetVectorCodec.TryGetEntry` helpers. NULL termination, the
256-entry limit, bounded mapping, overflow checks, and malformed-range
rejection remain unchanged. Host coverage is **809/809**; native MultiSet
vector ABI and complete MorphOS differential parity remain progressive.

SetCycleChain replacement now walks caller-owned object slots through the
named `MuiApplicationWindowCycleChainCursor` and
`MuiApplicationWindowCycleChainVectorCodec.TryGetEntry` helpers. Four-byte
slots, traversal bounds, NULL termination, failure-atomic cleanup, and
malformed-range rejection remain intact. Host coverage is **815/815**; native
SetCycleChain cursor ABI and complete MorphOS differential parity remain
progressive.

Notification dispatch now obtains copied MUIM_Notify follow-parameter slots
through the named `MuiNotifyFollowParameterVectorCursor` and
`MuiNotifyFollowParameterVectorCodec.TryGetEntry` helpers. Sentinel
substitution, the 256-entry bound, bounded mapping, overflow checks, and
malformed-range rejection remain unchanged. Host coverage is **810/810**;
native follow-parameter ABI and complete MorphOS differential parity remain
progressive.

Notification creation and dispatch now obtain the trailing payload address
through `MuiHeadlessNotificationCodec.TryGetPayload`, preserving the 32-byte
header, follow-parameter copies, bounded total-size mapping, overflow checks,
and malformed-range rejection. Host coverage is **811/811**; native
notification payload ABI and complete MorphOS differential parity remain
progressive.

SetAsString Apply now obtains the caller-owned parameter tail through
`MuiSetAsStringMessageCodec.TryGetParameters`, preserving the 16-byte fixed
packet, value-tail address, bounded mapping, overflow checks, and
malformed-range rejection. Host coverage is **812/812**; native SetAsString
tail ABI and complete MorphOS differential parity remain progressive.

Application PushMethod dispatch now obtains its caller-owned argument tail
through the named `MuiApplicationPushMethodParameter` record and
`MuiApplicationQueuePacketCodec.TryGetParameters`. The 12-byte packet,
four-byte argument slots, seven-argument limit, mapping/overflow checks, and
malformed-range rejection remain intact. Host coverage is **813/813**; native
PushMethod tail ABI and complete MorphOS differential parity remain
progressive.

Window SetCycleChain dispatch now obtains the inline object vector through
`MuiWindowCycleChainPacketCodec.TryGetVector`, preserving the 8-byte packet,
four-byte object-pointer slots, NULL termination, mapping/overflow checks, and
malformed-range rejection. Host coverage is **814/814**; native SetCycleChain
vector ABI and complete MorphOS differential parity remain progressive.

Application settings Save/Load chunk transfers now resolve source and
destination addresses through the named `MuiApplicationSettingsTransferCursor`
and `MuiApplicationSettingsTransferCursorCodec.TryGetAddress` helpers. Short
transfer retries, mapping/overflow checks, and caller-owned buffers remain
intact. Host coverage is **816/816**; native settings-file transfer ABI and
complete MorphOS differential parity remain progressive.

Dataspace IFF Read/Write chunk transfers now resolve header and payload
addresses through the named `MuiDataspaceIffTransferCursor` and
`MuiDataspaceIffTransferCursorCodec.TryGetAddress` helpers. Short-transfer
retries, mapping/overflow checks, and bounded payload handling remain intact.
Host coverage is **817/817**; native Dataspace IFF transfer ABI and complete
MorphOS differential parity remain progressive.

ExternalWrapper creation-tag and OM_UPDATE walks now obtain TagItem addresses
through the named `MuiExternalTagListCursor` and
`MuiExternalTagListCursorCodec.TryGetEntry` helpers. The 8-byte TagItem
layout, 64-entry bound, TAG_DONE termination, and malformed-range checks remain
intact. Host coverage is **818/818**; native ExternalWrapper tag-list ABI and
complete MorphOS differential parity remain progressive.

ExternalWrapper remembered TagItem add/save/reapply paths now resolve their
five-entry slots through the named `MuiExternalRememberCursor` and
`MuiExternalRememberCursorCodec.TryGetEntry` helpers. The 8-byte layout,
five-tag limit, mapping/overflow checks, and malformed-range rejection remain
intact. Host coverage is **819/819**; native remembered-tag ABI and complete
MorphOS differential parity remain progressive.

Boopsi geometry and pass-through OM_SET marshalling now resolve all five
work-buffer TagItem slots through the named `MuiExternalBoopsiTagCursor` and
`MuiExternalBoopsiTagCursorCodec.TryGetEntry` helpers. The 8-byte layout,
bounded work-buffer contract, and mapping/overflow checks remain intact. Host
coverage is **820/820**; native Boopsi work-buffer ABI and complete MorphOS
differential parity remain progressive.

Cycle/Radio choice-vector counting, active lookup, and Radio child construction
now resolve entries through the named `MuiChoiceEntryCursor` and
`MuiChoiceEntryCursorCodec.TryGetEntry` helpers. The 4-byte STRPTR layout,
NULL termination, 4096-entry bound, and mapping/overflow checks remain intact.
Host coverage is **821/821**; native choice-vector ABI and complete MorphOS
differential parity remain progressive.

MUI_MakeObjectA generated TagItem slots now use the named
`MuiAslTagItemCursor` boundary, Cycle/Radio validation reuses
`MuiChoiceEntryCursor`, and NewMenu parsing uses the named `MuiNewMenuCursor`.
The 8-byte TagItem, 4-byte STRPTR, and 20-byte NewMenu layouts plus their
bounds remain intact. Host coverage is **822/822**; native MakeObjectA vector
ABI and complete MorphOS differential parity remain progressive.

Group reorder/sort vector reads now resolve entries through the named
`MuiFamilyMutationVectorCursor` and
`MuiFamilyMutationVectorCodec.TryGetEntry` helpers. The 4-byte object-pointer
layout, NULL termination, traversal bound, and malformed-range rejection
remain intact. Host coverage is **822/822**; native Group ordering vector ABI
and complete MorphOS differential parity remain progressive.

Headless object creation now walks TAG_IGNORE, TAG_MORE, and TAG_SKIP through
the named `MuiAslTagItemCursor` and `MuiAslTagItemVectorCodec` helpers. The
8-byte TagItem layout, bounded traversal, skip-count overflow rejection, and
malformed-range behavior remain intact. Host coverage is **822/822**; native
headless tag-walk ABI and complete MorphOS differential parity remain
progressive.

Application Save/Load traversal frames now use the named
`MuiApplicationPersistenceFrameCursor` and
`MuiApplicationPersistenceFrameCursorCodec.TryGetEntry` helpers, with the
existing frame codec retained as a typed wrapper. The 12-byte frame layout,
256-entry depth bound, traversal cleanup, and malformed-range checks remain
intact. Host coverage is **833/833**; native persistence frame ABI and complete
MorphOS differential parity remain progressive.

Mccprefs registration, replacement, removal, and table access now use the
named `MuiMccprefsRegistryCursor` and
`MuiMccprefsRegistryCursorCodec.TryGetEntry` helpers. The 24-byte registry
record layout, 64-entry bound, caller-owned references, failure-atomic updates,
and malformed-range checks remain intact. Host coverage is **834/834**; native
Mccprefs registry ABI and complete MorphOS differential parity remain
progressive.

Filepanel adopted-row insertion and recursive disposal now use the named
`MuiFilepanelRowCursor` and `MuiFilepanelRowCursorCodec.TryGetEntry` helpers.
The 8-byte `{label, contents}` row layout, 64-entry bound, failure-atomic
adoption, and malformed-range checks remain intact. Host coverage is
**835/835**; native Filepanel row ABI and complete MorphOS differential parity
remain progressive.

Title page creation, compaction, close, and lookup now use the named
`MuiTitlePageCursor` and `MuiTitlePageCursorCodec.TryGetEntry` helpers. The
8-byte `{handle, flags}` page layout, 64-entry bound, active-page adjustment,
and malformed-range checks remain intact. Host coverage is **836/836**; native
Title page ABI and complete MorphOS differential parity remain progressive.

Scrmodelist mode append and indexed lookup now use the named
`MuiScrmodelistModeCursor` and `MuiScrmodelistModeCursorCodec.TryGetEntry`
helpers. The 4-byte mode record layout, 256-entry bound, private-class
behavior, and malformed-range checks remain intact. Host coverage is
**837/837**; native Scrmodelist mode ABI and complete MorphOS differential
parity remain progressive.

Family reorder/sort packet vector lookup now uses the named
`MuiFamilyInlineVectorCursor` and `MuiFamilyInlineVectorCursorCodec.TryGetEntry`
helpers, layered over the typed `MuiFamilyMutationVectorEntry` record. The
fixed packet header, 4-byte pointer-vector elements, bounded mapping, and
overflow checks remain intact. Host coverage is **838/838**; native Family
inline-vector ABI and complete MorphOS differential parity remain progressive.
