/* SPDX-License-Identifier: MIT */
using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.MuiMaster;

internal partial struct MuiNativeBuiltinPlatform
{
    private APTR GraphicsBase => MuiNativeClassOwnerCodec.TryRead(ref this, Context, out var owner) ? owner.GraphicsBase : APTR.Null;
    private APTR SelectFont(APTR rp, APTR font)
    {
        var previous = APTR.FromPointer(ReadUInt32(rp, GraphicsLayout.RastPort.Font));
        if (IsNativeFont(font) && font != previous) MuiNativeBuiltinGraphicsCalls.SetFont(GraphicsBase, rp, font);
        return previous;
    }
    // Negative MUIV_Font_* values select configured UI roles, not TextFont*.
    // The initial native profile uses the screen's real font for these roles.
    private static bool IsNativeFont(APTR font) => font.Raw >= 256 && unchecked((int)font.Raw) > 0;
    public int TextWidth(APTR rp, APTR font, APTR text, int length)
    {
        if (rp.IsNull || text.IsNull || length <= 0 || GraphicsBase.IsNull) return 0;
        var previous = SelectFont(rp, font);
        var result = MuiNativeBuiltinGraphicsCalls.TextLength(GraphicsBase, rp, text, (uint)length);
        if (previous.IsNotNull && font.IsNotNull && previous != font) MuiNativeBuiltinGraphicsCalls.SetFont(GraphicsBase, rp, previous);
        return result;
    }
    public int TextHeight(APTR rp, APTR font)
    {
        if (rp.IsNull || GraphicsBase.IsNull) return 0;
        var previous = SelectFont(rp, font);
        var result = LayersRastPortCodec.ReadTextHeight(ref this, rp);
        if (previous.IsNotNull && font.IsNotNull && previous != font) MuiNativeBuiltinGraphicsCalls.SetFont(GraphicsBase, rp, previous);
        return result;
    }
    public void SetPen(APTR rp, uint pen)
    {
        var bitmap = LayersRastPortCodec.ReadBitMap(ref this, rp);
        if (bitmap.IsNotNull)
        {
            var depth = LayersBitMapCodec.ReadDepth(ref this, bitmap);
            if (depth > 0 && depth < 8 && pen >= (1u << depth))
                pen = pen == 8 && depth >= 2 ? 3u : 1u;
        }
        // The existing low-depth public screen may not contain all decorative
        // MUI pens. Use its foreground/selection colors instead of wrapping
        // an absent pen back to the background bit pattern.
        MuiNativeBuiltinGraphicsCalls.SetAPen(GraphicsBase, rp, pen);
    }
    public void SetBackgroundPen(APTR rp, uint background)
    {
        var bitmap = LayersRastPortCodec.ReadBitMap(ref this, rp);
        var depth = bitmap.IsNull ? 0 : LayersBitMapCodec.ReadDepth(ref this, bitmap);
        MuiNativeBuiltinGraphicsCalls.SetAPen(GraphicsBase, rp,
            MuiNativeIndexedBackground.Resolve(background, depth));
    }
    public void FillRectangle(APTR rp, int left, int top, int right, int bottom) => MuiNativeBuiltinGraphicsCalls.RectFill(GraphicsBase, rp, left, top, right, bottom);
    public void DrawLine(APTR rp, int x1, int y1, int x2, int y2)
    {
        MuiNativeBuiltinGraphicsCalls.Move(GraphicsBase, rp, x1, y1);
        MuiNativeBuiltinGraphicsCalls.Draw(GraphicsBase, rp, x2, y2);
    }
    public void DrawText(APTR rp, APTR font, int left, int baseline, APTR text, int length)
    {
        if (rp.IsNull || text.IsNull || length <= 0 || GraphicsBase.IsNull) return;
        var previous = SelectFont(rp, font);
        MuiNativeBuiltinGraphicsCalls.SetDrMd(GraphicsBase, rp, 1); // JAM1, preserve the already painted background.
        MuiNativeBuiltinGraphicsCalls.Move(GraphicsBase, rp, left, baseline);
        MuiNativeBuiltinGraphicsCalls.Text(GraphicsBase, rp, text, (uint)length);
        if (previous.IsNotNull && font.IsNotNull && previous != font) MuiNativeBuiltinGraphicsCalls.SetFont(GraphicsBase, rp, previous);
    }
    public void DrawImage(APTR rp, APTR image, int left, int top, int width, int height)
    {
        var native = Native;
        if (rp.IsNotNull && image.IsNotNull && native.IntuitionBase.IsNotNull)
            MuiNativeBuiltinGraphicsCalls.DrawImage(native.IntuitionBase, rp, image, left, top);
    }
    public bool ScheduleRedraw(APTR obj, uint flags) => MuiNativeBuiltinPresentation.DrawObject(ref this, obj, flags);
    private APTR RasterLayer(APTR rp) => rp.IsNull ? APTR.Null : LayersRastPortCodec.ReadLayer(ref this, rp);
    public bool LockLayer(APTR rp) => Native.LockPaintLayer(RasterLayer(rp));
    public void UnlockLayer(APTR rp) => Native.UnlockPaintLayer(RasterLayer(rp));
    // These per-object paint scopes must retain the visible ClipRects. A
    // Layers BeginUpdate/EndUpdate pair would restrict each object to damage
    // and let the first Group clear damage needed by its later children.
    // Intuition's enclosing BeginRefresh/EndRefresh owns that update mode.
    public bool BeginUpdate(APTR rp) => Native.LockLayer(RasterLayer(rp));
    public void EndUpdate(APTR rp, bool completed) => Native.UnlockLayer(RasterLayer(rp));
    public APTR PushClip(APTR rp, int left, int top, int width, int height) => Native.PushClip(RasterLayer(rp), left, top, width, height);
    public void PopClip(APTR rp, APTR previous) => Native.PopClip(RasterLayer(rp), previous);
    // Optional providers decline explicitly; existing control cores retain their
    // direct bitmap, pen, font and text paths. No success is manufactured.
    public bool BeginMuiDoubleBuffer(ref MuiDoubleBufferRenderRequest request) => false;
    public bool EndMuiDoubleBuffer(ref MuiDoubleBufferRenderRequest request, bool completed) => false;
    public bool ApplyMuiBackfill(ref MuiBackfillRenderRequest request) => false;
    public bool ResolveMuiTextColor(ref MuiTextColorResolutionRequest request)
    {
        // This indexed-screen profile has no RGB override. Available=0 is a
        // successful resolution to the normal pen policy; false means an
        // actual setup failure in the Area contract.
        request.Available = 0;
        return request.Object.IsNotNull && request.RenderInfo.IsNotNull;
    }
    public bool ApplyMuiTextColor(ref MuiTextColorRenderRequest request) => false;
    public bool ApplyMuiCustomFont(ref MuiCustomFontRenderRequest request) => false;
    public bool ApplyMuiTextStyle(ref MuiTextStyleRenderRequest request) => false;
    public bool ApplyMuiTextInlineColor(ref MuiTextInlineColorRenderRequest request) => false;
    public bool ApplyMuiTextInlineImage(ref MuiTextInlineImageRenderRequest request) => false;
    public bool ApplyMuiTextMethod(ref MuiTextMethodRenderRequest request) => false;
    public bool ApplyMuiTextDimensions(ref MuiTextDimensionRequest request) => false;
    public bool ReadMuiKeyadjustTextInput(ref MuiKeyadjustTextInputSample input) => false;
    public int TranslateTextInput(APTR message)
    {
        if (!MuiNativeApplicationWindowEvents.TryTranslateInputEvent(ref this, message, out var input)) return -1;
        var storage = Allocate(MuiWindowInputEventRecord.Size, MuiHeadlessLayout.AllocationFlags);
        if (storage.IsNull) return -1;
        var native = Native;
        var translated = MuiWindowInputEventCodec.WriteRecord(ref this, storage, input) &&
            native.TryMapRawKey(storage, out var character) ? (int)character : -1;
        Free(storage, MuiWindowInputEventRecord.Size);
        return translated;
    }
    public bool DisplayMuiBeep(ref MuiStringEditBeepRequest request) => false;
    public bool ReuseMuiInput(ref MuiStringEditReuseRequest request) => false;
    public uint ReadTicks()
    {
        var native = Native;
        var time = Allocate(8, MuiHeadlessLayout.AllocationFlags);
        if (time.IsNull || native.IntuitionBase.IsNull) { if (time.IsNotNull) Free(time, 8); return 0; }
        MuiNativeBuiltinGraphicsCalls.CurrentTime(native.IntuitionBase, time, APTR.FromPointer(time.Raw + 4));
        var ticks = ReadUInt32(time) * 50u + ReadUInt32(time, 4) / 20000u;
        Free(time, 8);
        return ticks;
    }
}

internal static class MuiNativeIndexedBackground
{
    // The initial indexed profile has no decorative background image provider.
    // An unavailable value uses the screen background, keeping pen 1 available
    // for text. Reusing the foreground fallback makes Calc's display invisible.
    internal static uint Resolve(uint background, int depth) =>
        depth > 0 && depth < 8 && background >= (1u << depth) ? 0u : background;
}

// Register assignments and vector identities follow the SDK's Graphics and
// Intuition declarations; A6 explicitly borrows the current owner's provider.
[AmigaLibrary("graphics.library", AmigaLibraryBasePolicy.CallerProvided)]
internal static class MuiNativeBuiltinGraphicsCalls
{
    [AmigaLvo(-270)]
    internal static extern void WaitTOF([M68kRegister(M68kRegister.A6)] APTR library);
    [AmigaLvo(-54)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern short TextLength([M68kRegister(M68kRegister.A6)] APTR lib, [M68kRegister(M68kRegister.A1)] APTR rp, [M68kRegister(M68kRegister.A0)] APTR text, [M68kRegister(M68kRegister.D0)] uint length);
    [AmigaLvo(-66)]
    internal static extern void SetFont([M68kRegister(M68kRegister.A6)] APTR lib, [M68kRegister(M68kRegister.A1)] APTR rp, [M68kRegister(M68kRegister.A0)] APTR font);
    [AmigaLvo(-342)]
    internal static extern void SetAPen([M68kRegister(M68kRegister.A6)] APTR lib, [M68kRegister(M68kRegister.A1)] APTR rp, [M68kRegister(M68kRegister.D0)] uint pen);
    [AmigaLvo(-354)]
    internal static extern void SetDrMd([M68kRegister(M68kRegister.A6)] APTR lib, [M68kRegister(M68kRegister.A1)] APTR rp, [M68kRegister(M68kRegister.D0)] uint mode);
    [AmigaLvo(-240)]
    internal static extern void Move([M68kRegister(M68kRegister.A6)] APTR lib, [M68kRegister(M68kRegister.A1)] APTR rp, [M68kRegister(M68kRegister.D0)] int x, [M68kRegister(M68kRegister.D1)] int y);
    [AmigaLvo(GraphicsLayersLvo.Draw)]
    internal static extern void Draw([M68kRegister(M68kRegister.A6)] APTR lib, [M68kRegister(M68kRegister.A1)] APTR rp, [M68kRegister(M68kRegister.D0)] int x, [M68kRegister(M68kRegister.D1)] int y);
    [AmigaLvo(GraphicsLayersLvo.RectFill)]
    internal static extern void RectFill([M68kRegister(M68kRegister.A6)] APTR lib, [M68kRegister(M68kRegister.A1)] APTR rp, [M68kRegister(M68kRegister.D0)] int left, [M68kRegister(M68kRegister.D1)] int top, [M68kRegister(M68kRegister.D2)] int right, [M68kRegister(M68kRegister.D3)] int bottom);
    [AmigaLvo(GraphicsLayersLvo.Text)]
    internal static extern void Text([M68kRegister(M68kRegister.A6)] APTR lib, [M68kRegister(M68kRegister.A1)] APTR rp, [M68kRegister(M68kRegister.A0)] APTR text, [M68kRegister(M68kRegister.D0)] uint length);
    [AmigaLvo(IntuitionLvo.DrawImage)]
    [AmigaLibrary("intuition.library", AmigaLibraryBasePolicy.CallerProvided)]
    internal static extern void DrawImage([M68kRegister(M68kRegister.A6)] APTR lib, [M68kRegister(M68kRegister.A0)] APTR rp, [M68kRegister(M68kRegister.A1)] APTR image, [M68kRegister(M68kRegister.D0)] int left, [M68kRegister(M68kRegister.D1)] int top);
    [AmigaLvo(IntuitionLvo.CurrentTime)]
    [AmigaLibrary("intuition.library", AmigaLibraryBasePolicy.CallerProvided)]
    internal static extern void CurrentTime([M68kRegister(M68kRegister.A6)] APTR lib, [M68kRegister(M68kRegister.A0)] APTR seconds, [M68kRegister(M68kRegister.A1)] APTR micros);
}
