	mc68040
	section	rom_code,code

C68K_entry_003Amanaged:
	movea.l	$0004.w,a6
	move.l	a6,_ExecBase
C68K_method_003A06000324:
	move.l	d2,-(a7)
	lea	-20(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
C68K_method_003A06000324_003ABB0000:
	move.l	a7,d1
	moveq	#0,d0
	movea.l	d1,a0
	clr.l	0(a0)
	lea	(a7),a0
	moveq	#4,d0
	move.l	d0,-(a7)
	movea.l	#$00082000,a1
	moveq	#4,d0
	moveq	#16,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	addq.l	#4,a7
	move.l	a0,d0
	move.l	d0,4(a7)
	move.l	4(a7),d0
	tst.l	d0
	beq.w	C68K_method_003A06000324_003ABB0007
C68K_method_003A06000324_003ABB0001:
	lea	(a7),a0
	move.l	4(a7),d1
	move.l	a7,d0
	addq.l	#8,d0
	movea.l	d1,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600098B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003A06000324_003ABB0007
C68K_method_003A06000324_003ABB0002:
	lea	(a7),a1
	move.l	4(a7),d2
	lea	12(a7),a0
	move.l	a0,-(a7)
	movea.l	a1,a0
	movea.l	d2,a1
	move.l	#$00083000,d0
	moveq	#8,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	addq.l	#4,a7
	tst.b	d0
	beq.s	C68K_method_003A06000324_003ABB0007
C68K_method_003A06000324_003ABB0003:
	lea	(a7),a0
	move.l	12(a7),d0
	movea.l	d0,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000953_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003A06000324_003ABB0007
C68K_method_003A06000324_003ABB0004:
	lea	(a7),a0
	move.l	12(a7),d0
	movea.l	d0,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000954_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003A06000324_003ABB0007
C68K_method_003A06000324_003ABB0005:
	lea	(a7),a0
	move.l	12(a7),d1
	move.l	a7,d0
	addi.l	#$00000010,d0
	movea.l	d1,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000957_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003A06000324_003ABB0007
C68K_method_003A06000324_003ABB0006:
	lea	(a7),a0
	move.l	12(a7),d0
	movea.l	d0,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000958_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003A06000324_003ABB0008
C68K_method_003A06000324_003ABB0007:
	moveq	#0,d0
	lea	20(a7),a7
	move.l	(a7)+,d2
	rts
C68K_method_003A06000324_003ABB0008:
	lea	(a7),a0
	move.l	8(a7),d0
	movea.l	d0,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	shi	d0
	extb.l	d0
	neg.l	d0
	lea	20(a7),a7
	move.l	(a7)+,d2
	rts
C68K_method_003A06000324_003Aend:
C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	d2-d5/a2,-(a7)
	lea	-12(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	move.l	a1,8(a7)
	move.l	d1,d3
	move.l	d0,d2
	movea.l	a0,a2
C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	8(a7),d0
	tst.l	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002
C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	moveq	#0,d0
	movea.l	d0,a0
	lea	12(a7),a7
	movem.l	(a7)+,d2-d5/a2
	rts
C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	move.l	a2,d0
	moveq	#32,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001D
	move.l	a0,d0
	move.l	d0,(a7)
	move.l	(a7),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005
C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	move.l	(a7),d1
	moveq	#3,d0
	and.l	d0,d1
	tst.l	d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005
C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	move.l	(a7),d4
	move.l	a2,d0
	movea.l	d4,a0
	moveq	#32,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001A
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006
C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	move.l	(a7),d1
	movea.l	a2,a0
	movea.l	d1,a1
	moveq	#32,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600098F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	moveq	#0,d0
	movea.l	d0,a0
	lea	12(a7),a7
	movem.l	(a7)+,d2-d5/a2
	rts
C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	move.l	a2,d0
	moveq	#36,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001D
	move.l	a0,d0
	move.l	d0,4(a7)
	move.l	4(a7),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A
C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
	move.l	4(a7),d1
	moveq	#3,d0
	and.l	d0,d1
	tst.l	d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A
C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
	move.l	4(a7),d4
	move.l	a2,d0
	movea.l	d4,a0
	moveq	#36,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001A
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A
C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009:
	move.l	(a7),d5
	move.l	4(a7),d4
	movea.l	d5,a0
	moveq	#32,d0
	movea.l	d4,a1
	moveq	#36,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600098E
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B
C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A:
	move.l	4(a7),d1
	movea.l	a2,a0
	movea.l	d1,a1
	moveq	#36,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600098F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	move.l	(a7),d1
	movea.l	a2,a0
	movea.l	d1,a1
	moveq	#32,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600098F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	moveq	#0,d0
	movea.l	d0,a0
	lea	12(a7),a7
	movem.l	(a7)+,d2-d5/a2
	rts
C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B:
	move.l	(a7),d4
	move.l	a2,d0
	movea.l	d4,a0
	moveq	#32,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000018
	move.l	4(a7),d4
	move.l	a2,d0
	movea.l	d4,a0
	moveq	#36,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000018
	move.l	(a7),d1
	move.l	36(a7),d0
	move.l	d0,-(a7)
	movea.l	a2,a0
	movea.l	d1,a1
	move.l	d2,d0
	move.l	d3,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000993_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	addq.l	#4,a7
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C:
	move.l	4(a7),d2
	move.l	8(a7),d0
	move.l	(a7),d1
	movea.l	a2,a0
	movea.l	d2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A060009A0_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D:
	move.l	4(a7),d1
	movea.l	a2,a0
	movea.l	d1,a1
	moveq	#36,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600098F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	move.l	(a7),d1
	movea.l	a2,a0
	movea.l	d1,a1
	moveq	#32,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600098F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	moveq	#0,d0
	movea.l	d0,a0
	lea	12(a7),a7
	movem.l	(a7)+,d2-d5/a2
	rts
C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E:
	move.l	4(a7),d0
	movea.l	d0,a0
	lea	12(a7),a7
	movem.l	(a7)+,d2-d5/a2
	rts
C68K_method_003ACopperStart_002EDos_003A0600098A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A0600098B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	pea	(a2)
	pea	(a3)
	lea	-68(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	clr.l	64(a7)
	movea.l	a0,a3
	movea.l	d0,a2
C68K_method_003ACopperStart_002EDos_003A0600098B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	moveq	#0,d0
	move.l	d0,(a2)
	move.l	a7,d0
	movea.l	a3,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600099C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600098B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A0600098B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	lea	(a7),a0
	move.l	8(a0),d1
	moveq	#48,d0
	and.l	d0,d1
	tst.l	d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600098B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A0600098B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	lea	(a7),a0
	move.l	16(a0),d1
	move.l	a7,d0
	addi.l	#$00000024,d0
	movea.l	a3,a0
	movea.l	d1,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000994_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600098B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A0600098B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	moveq	#0,d0
	lea	68(a7),a7
	movea.l	(a7)+,a3
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A0600098B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	lea	(a7),a0
	move.l	16(a0),d0
	move.l	d0,(a2)
	moveq	#1,d0
	lea	68(a7),a7
	movea.l	(a7)+,a3
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A0600098B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	movem.l	d2-d5/a2-a5,-(a7)
	lea	-72(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	clr.l	64(a7)
	move.l	d0,68(a7)
	move.l	d1,d3
	movea.l	a0,a2
	movea.l	108(a7),a3
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	moveq	#0,d0
	move.l	d0,(a3)
	movea.l	a2,a0
	move.l	a7,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600099C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	lea	(a7),a0
	move.l	8(a0),d1
	moveq	#1,d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	lea	(a7),a0
	move.l	16(a0),d1
	move.l	a7,d0
	addi.l	#$00000024,d0
	movea.l	a2,a0
	movea.l	d1,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000994_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	move.l	68(a7),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	tst.l	d3
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	move.l	#$000000FF,d0
	cmp.l	d0,d3
	bhi.s	C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	move.l	68(a7),d1
	moveq	#-1,d0
	sub.l	d3,d0
	cmp.l	d0,d1
	bhi.s	C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
	move.l	a2,d0
	movea.l	68(a7),a0
	move.l	d3,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001A
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
	moveq	#0,d0
	lea	72(a7),a7
	movem.l	(a7)+,d2-d5/a2-a5
	rts
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009:
	moveq	#72,d4
	add.l	d3,d4
	addq.l	#1,d4
	lea	36(a7),a0
	move.l	20(a0),d0
	lea	36(a7),a0
	move.l	24(a0),d1
	muls.l	d1,d0
	move.l	d0,d2
	tst.l	d2
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A:
	moveq	#72,d0
	cmp.l	d0,d4
	bcs.s	C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B:
	moveq	#72,d1
	add.l	d3,d1
	addq.l	#1,d1
	cmp.l	d3,d1
	bcc.s	C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C:
	moveq	#0,d0
	lea	72(a7),a7
	movem.l	(a7)+,d2-d5/a2-a5
	rts
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D:
	move.l	a2,d0
	move.l	d4,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001D
	move.l	a0,d0
	movea.l	d0,a4
	movea.l	a2,a0
	movea.l	a4,a1
	move.l	d4,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000960_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E:
	movea.l	a2,a0
	movea.l	a4,a1
	move.l	d4,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600098F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	moveq	#0,d0
	lea	72(a7),a7
	movem.l	(a7)+,d2-d5/a2-a5
	rts
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F:
	move.l	a2,d0
	move.l	d2,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001D
	move.l	a0,d0
	movea.l	d0,a5
	movea.l	a2,a0
	movea.l	a5,a1
	move.l	d2,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000960_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0011
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010:
	movea.l	a4,a0
	move.l	d4,d0
	movea.l	a5,a1
	move.l	d2,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000962
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0012
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0011:
	movea.l	a2,a0
	movea.l	a5,a1
	move.l	d2,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600098F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	movea.l	a2,a0
	movea.l	a4,a1
	move.l	d4,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600098F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	moveq	#0,d0
	lea	72(a7),a7
	movem.l	(a7)+,d2-d5/a2-a5
	rts
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0012:
	move.l	a2,d0
	movea.l	a4,a0
	move.l	d4,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000018
	move.l	a2,d0
	movea.l	a5,a0
	move.l	d2,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000018
	movea.l	a4,a1
	move.l	68(a7),d1
	moveq	#0,d5
	lea	36(a7),a0
	move.l	20(a0),d0
	move.l	d0,-(a7)
	move.l	d2,-(a7)
	move.l	a5,-(a7)
	move.l	d5,-(a7)
	move.l	d3,-(a7)
	movea.l	a2,a0
	move.l	d4,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	lea	20(a7),a7
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0014
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0013:
	lea	(a7),a0
	move.l	16(a0),d1
	move.l	a4,d0
	movea.l	a2,a0
	movea.l	d1,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0015
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0014:
	movea.l	a2,a0
	movea.l	a5,a1
	move.l	d2,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600098F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	movea.l	a2,a0
	movea.l	a4,a1
	move.l	d4,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600098F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	moveq	#0,d0
	lea	72(a7),a7
	movem.l	(a7)+,d2-d5/a2-a5
	rts
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0015:
	move.l	a4,(a3)
	moveq	#1,d0
	lea	72(a7),a7
	movem.l	(a7)+,d2-d5/a2-a5
	rts
C68K_method_003ACopperStart_002EDos_003A06000953_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A0600095F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	pea	(a2)
	pea	(a3)
	lea	-108(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	clr.l	64(a7)
	clr.l	68(a7)
	movea.l	a1,a2
	movea.l	a0,a3
C68K_method_003ACopperStart_002EDos_003A06000953_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	a7,d0
	movea.l	a3,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600094A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000953_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A06000953_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	lea	(a7),a0
	move.l	24(a0),d0
	move.l	d0,72(a7)
	move.l	28(a0),d0
	move.l	d0,76(a7)
	move.l	32(a0),d0
	move.l	d0,80(a7)
	move.l	36(a0),d0
	move.l	d0,84(a7)
	move.l	40(a0),d0
	move.l	d0,88(a7)
	move.l	44(a0),d0
	move.l	d0,92(a7)
	move.l	48(a0),d0
	move.l	d0,96(a7)
	move.l	52(a0),d0
	move.l	d0,100(a7)
	move.l	56(a0),d0
	move.l	d0,104(a7)
	lea	72(a7),a0
	move.l	28(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000953_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A06000953_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	lea	(a7),a0
	move.l	20(a0),d1
	moveq	#1,d0
	and.l	d0,d1
	tst.l	d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000953_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A06000953_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	moveq	#0,d0
	lea	108(a7),a7
	movea.l	(a7)+,a3
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A06000953_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	lea	(a7),a0
	lea	20(a0),a0
	move.l	(a0),d1
	moveq	#1,d0
	or.l	d0,d1
	move.l	d1,(a0)
	move.l	a7,d0
	movea.l	a3,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600094B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	andi.l	#$000000FF,d0
	lea	108(a7),a7
	movea.l	(a7)+,a3
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A06000953_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000954_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	pea	(a2)
	pea	(a3)
	lea	-108(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	clr.l	64(a7)
	clr.l	68(a7)
	movea.l	a1,a2
	movea.l	a0,a3
C68K_method_003ACopperStart_002EDos_003A06000954_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	a7,d0
	movea.l	a3,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600094A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000954_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A06000954_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	lea	(a7),a0
	move.l	24(a0),d0
	move.l	d0,72(a7)
	move.l	28(a0),d0
	move.l	d0,76(a7)
	move.l	32(a0),d0
	move.l	d0,80(a7)
	move.l	36(a0),d0
	move.l	d0,84(a7)
	move.l	40(a0),d0
	move.l	d0,88(a7)
	move.l	44(a0),d0
	move.l	d0,92(a7)
	move.l	48(a0),d0
	move.l	d0,96(a7)
	move.l	52(a0),d0
	move.l	d0,100(a7)
	move.l	56(a0),d0
	move.l	d0,104(a7)
	lea	72(a7),a0
	move.l	32(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000954_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A06000954_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	lea	(a7),a0
	move.l	20(a0),d1
	moveq	#2,d0
	and.l	d0,d1
	tst.l	d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000954_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A06000954_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	moveq	#0,d0
	lea	108(a7),a7
	movea.l	(a7)+,a3
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A06000954_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	lea	(a7),a0
	lea	20(a0),a0
	move.l	(a0),d1
	moveq	#2,d0
	or.l	d0,d1
	move.l	d1,(a0)
	move.l	a7,d0
	movea.l	a3,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600094B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	andi.l	#$000000FF,d0
	lea	108(a7),a7
	movea.l	(a7)+,a3
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A06000954_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000957_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	a2-a4,-(a7)
	lea	-72(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	clr.l	64(a7)
	clr.l	68(a7)
	movea.l	a1,a3
	movea.l	a0,a4
	movea.l	d0,a2
C68K_method_003ACopperStart_002EDos_003A06000957_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	moveq	#0,d0
	move.b	d0,(a2)
	move.l	a7,d0
	movea.l	a4,a0
	movea.l	a3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600094A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000957_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A06000957_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	lea	(a7),a0
	move.l	20(a0),d1
	moveq	#1,d0
	and.l	d0,d1
	tst.l	d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000957_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A06000957_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	lea	(a7),a0
	lea	24(a0),a1
	movea.l	a4,a0
	move.l	a2,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000940_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000957_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A06000957_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	moveq	#0,d0
	lea	72(a7),a7
	movem.l	(a7)+,a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A06000957_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	lea	(a7),a0
	lea	20(a0),a0
	move.l	(a0),d1
	moveq	#-2,d0
	and.l	d0,d1
	move.l	d1,(a0)
	move.l	a7,d0
	movea.l	a4,a0
	movea.l	a3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600094B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	andi.l	#$000000FF,d0
	lea	72(a7),a7
	movem.l	(a7)+,a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A06000957_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000958_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	pea	(a2)
	pea	(a3)
	lea	-72(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	clr.l	64(a7)
	clr.l	68(a7)
	movea.l	a1,a2
	movea.l	a0,a3
C68K_method_003ACopperStart_002EDos_003A06000958_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	a7,d0
	movea.l	a3,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600094A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000958_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A06000958_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	lea	(a7),a0
	move.l	20(a0),d1
	moveq	#2,d0
	and.l	d0,d1
	tst.l	d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000958_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A06000958_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	lea	(a7),a0
	lea	24(a0),a1
	movea.l	a3,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600093F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000958_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A06000958_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	moveq	#0,d0
	lea	72(a7),a7
	movea.l	(a7)+,a3
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A06000958_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	lea	(a7),a0
	lea	20(a0),a0
	move.l	(a0),d1
	moveq	#-3,d0
	and.l	d0,d1
	move.l	d1,(a0)
	move.l	a7,d0
	movea.l	a3,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600094B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	andi.l	#$000000FF,d0
	lea	72(a7),a7
	movea.l	(a7)+,a3
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A06000958_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	d2-d4/a2-a4,-(a7)
	lea	-104(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	clr.l	64(a7)
	clr.l	68(a7)
	clr.l	72(a7)
	clr.l	76(a7)
	clr.l	80(a7)
	clr.l	84(a7)
	clr.l	88(a7)
	clr.l	92(a7)
	clr.l	96(a7)
	clr.l	100(a7)
	movea.l	a1,a4
	movea.l	a0,a2
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	movea.l	a2,a0
	movea.l	a4,a1
	move.l	a7,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000994_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	moveq	#0,d0
	lea	104(a7),a7
	movem.l	(a7)+,d2-d4/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D:
	movea.l	a2,a0
	move.l	a7,d0
	addi.l	#$00000020,d0
	movea.l	a3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000952_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F:
	lea	32(a7),a0
	move.l	8(a0),d4
	lea	32(a7),a0
	lea	64(a0),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010:
	andi.l	#$000000FF,d3
	lea	32(a7),a0
	move.l	64(a0),d0
	movea.l	a2,a0
	movea.l	d0,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000982_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	andi.l	#$000000FF,d0
	or.l	d0,d3
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0011:
	movea.l	a2,a0
	move.l	a7,d0
	addi.l	#$00000020,d0
	movea.l	a3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000952_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0012:
	lea	32(a7),a0
	lea	68(a0),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0013:
	move.l	d4,d0
	move.l	d2,d4
	move.l	d0,d2
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009:
	andi.l	#$000000FF,d3
	lea	32(a7),a0
	move.l	68(a0),d0
	movea.l	a2,a0
	movea.l	d0,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000982_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	andi.l	#$000000FF,d0
	or.l	d3,d0
	move.l	d0,d3
	move.l	d2,d1
	move.l	d4,d2
	move.l	d1,d4
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A:
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0014:
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0015:
	addq.l	#1,d2
	movea.l	d4,a3
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B:
	lea	(a7),a0
	move.l	12(a0),d0
	cmp.l	d0,d2
	bcs.w	C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0016:
	move.l	d3,d2
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C:
	movea.l	a2,a0
	movea.l	a4,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.l	d0
	shi	d0
	extb.l	d0
	neg.l	d0
	andi.l	#$000000FF,d2
	or.l	d2,d0
	lea	104(a7),a7
	movem.l	(a7)+,d2-d4/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	lea	(a7),a0
	move.l	8(a0),d0
	movea.l	d0,a3
	moveq	#0,d0
	move.b	d0,d3
	moveq	#0,d2
	bra.s	C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E:
	move.b	d3,d0
	andi.l	#$000000FF,d0
	lea	104(a7),a7
	movem.l	(a7)+,d2-d4/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600001D:
C68K_method_003ACopperStart_002EDos_003A0600097F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	pea	(a6)
	move.l	d1,d0
C68K_method_003ACopperStart_002EDos_003A0600001D_003ABB0000:
	move.l	#$00010001,d1
	movea.l	_ExecBase,a6
	jsr	-198(a6)
	movea.l	d0,a0
	movea.l	(a7)+,a6
	rts
C68K_method_003ACopperStart_002EDos_003A0600001A:
C68K_method_003ACopperStart_002EDos_003A0600001D_003Aend:
	move.l	d2,-(a7)
	move.l	a0,d2
C68K_method_003ACopperStart_002EDos_003A0600001A_003ABB0000:
	moveq	#-1,d0
	sub.l	d1,d0
	cmp.l	d0,d2
	shi	d0
	extb.l	d0
	neg.l	d0
	tst.l	d0
	seq	d0
	extb.l	d0
	neg.l	d0
	move.l	(a7)+,d2
	rts
C68K_method_003ACopperStart_002EDos_003A0600001A_003Aend:
C68K_method_003ACopperStart_002EDos_003A0600098F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	move.l	d2,-(a7)
	pea	(a2)
	subq.l	#4,a7
	move.l	a1,(a7)
	move.l	d0,d2
	movea.l	a0,a2
C68K_method_003ACopperStart_002EDos_003A0600098F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	(a7),d0
	tst.l	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600098F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002
C68K_method_003ACopperStart_002EDos_003A0600098F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	addq.l	#4,a7
	movea.l	(a7)+,a2
	move.l	(a7)+,d2
	rts
C68K_method_003ACopperStart_002EDos_003A0600098F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	movea.l	(a7),a0
	move.l	a2,d0
	move.l	d2,d1
	bsr.s	C68K_method_003ACopperStart_002EDos_003A0600001A
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600098F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A0600098F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	movea.l	(a7),a0
	move.l	a2,d0
	move.l	d2,d1
	bsr.s	C68K_method_003ACopperStart_002EDos_003A06000018
C68K_method_003ACopperStart_002EDos_003A0600098F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	movea.l	(a7),a0
	move.l	a2,d0
	move.l	d2,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001E
	addq.l	#4,a7
	movea.l	(a7)+,a2
	move.l	(a7)+,d2
	rts
C68K_method_003ACopperStart_002EDos_003A0600098E:
C68K_method_003ACopperStart_002EDos_003A0600098F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	movem.l	d2-d4,-(a7)
	move.l	a1,d2
	move.l	a0,d3
	move.l	d0,d4
C68K_method_003ACopperStart_002EDos_003A0600098E_003ABB0000:
	moveq	#-1,d0
	sub.l	d4,d0
	cmp.l	d0,d3
	bhi.s	C68K_method_003ACopperStart_002EDos_003A0600098E_003ABB0002
C68K_method_003ACopperStart_002EDos_003A0600098E_003ABB0001:
	moveq	#-1,d0
	sub.l	d1,d0
	cmp.l	d0,d2
	bls.s	C68K_method_003ACopperStart_002EDos_003A0600098E_003ABB0003
C68K_method_003ACopperStart_002EDos_003A0600098E_003ABB0002:
	moveq	#1,d0
	movem.l	(a7)+,d2-d4
	rts
C68K_method_003ACopperStart_002EDos_003A0600098E_003ABB0003:
	move.l	d2,d0
	add.l	d1,d0
	cmp.l	d0,d3
	bcc.s	C68K_method_003ACopperStart_002EDos_003A0600098E_003ABB0005
C68K_method_003ACopperStart_002EDos_003A0600098E_003ABB0004:
	add.l	d4,d3
	cmp.l	d3,d2
	scs	d0
	extb.l	d0
	neg.l	d0
	movem.l	(a7)+,d2-d4
	rts
C68K_method_003ACopperStart_002EDos_003A0600098E_003ABB0005:
	moveq	#0,d0
	movem.l	(a7)+,d2-d4
	rts
C68K_method_003ACopperStart_002EDos_003A06000018:
C68K_method_003ACopperStart_002EDos_003A0600098E_003Aend:
	move.l	d2,-(a7)
	move.l	d3,-(a7)
	move.l	d1,d2
	movea.l	a0,a1
C68K_method_003ACopperStart_002EDos_003A06000018_003ABB0000:
	moveq	#0,d3
C68K_method_003ACopperStart_002EDos_003A06000018_003ABB0002:
	cmp.l	d2,d3
	bcc.s	C68K_method_003ACopperStart_002EDos_003A06000018_003ABB0003
C68K_method_003ACopperStart_002EDos_003A06000018_003ABB0001:
C68K_method_003ACopperStart_002EDos_003A06000018_003ABB0004:
	movea.l	a1,a0
	move.l	d3,d0
	moveq	#0,d1
	adda.l	d0,a0
	move.b	d1,(a0)
	addq.l	#1,d3
	bra.s	C68K_method_003ACopperStart_002EDos_003A06000018_003ABB0002
C68K_method_003ACopperStart_002EDos_003A06000018_003ABB0003:
	move.l	(a7)+,d3
	move.l	(a7)+,d2
	rts
C68K_method_003ACopperStart_002EDos_003A06000018_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000993_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	d2-d4/a2-a3,-(a7)
	lea	-64(a7),a7
	movea.l	a1,a2
	movea.l	a0,a3
	move.l	88(a7),d2
	move.l	d1,d3
	move.l	d0,d4
C68K_method_003ACopperStart_002EDos_003A06000993_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	lea	32(a7),a0
	movea.l	a0,a1
	moveq	#0,d0
	moveq	#7,d1
C68K_generated_003Aallocated_aggregate_zero_loop_003A0:
	move.l	d0,(a1)+
	dbra	d1,C68K_generated_003Aallocated_aggregate_zero_loop_003A0
	lea	32(a7),a0
	move.l	#$44515247,d0
	move.l	d0,(a0)
	lea	32(a7),a0
	moveq	#1,d0
	move.l	d0,4(a0)
	lea	32(a7),a0
	move.l	d4,16(a0)
	lea	32(a7),a0
	move.l	d3,20(a0)
	lea	32(a7),a0
	move.l	d2,24(a0)
	lea	32(a7),a0
	move.l	(a0),d0
	move.l	d0,(a7)
	move.l	4(a0),d0
	move.l	d0,4(a7)
	move.l	8(a0),d0
	move.l	d0,8(a7)
	move.l	12(a0),d0
	move.l	d0,12(a7)
	move.l	16(a0),d0
	move.l	d0,16(a7)
	move.l	20(a0),d0
	move.l	d0,20(a7)
	move.l	24(a0),d0
	move.l	d0,24(a7)
	move.l	28(a0),d0
	move.l	d0,28(a7)
	move.l	a7,d0
	movea.l	a3,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000991_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	andi.l	#$000000FF,d0
	lea	64(a7),a7
	movem.l	(a7)+,d2-d4/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A06000993_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A060009A0_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	a2-a4,-(a7)
	lea	-108(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	clr.l	64(a7)
	clr.l	68(a7)
	clr.l	72(a7)
	clr.l	76(a7)
	clr.l	80(a7)
	clr.l	84(a7)
	clr.l	88(a7)
	clr.l	92(a7)
	clr.l	96(a7)
	clr.l	100(a7)
	move.l	d0,104(a7)
	movea.l	a1,a2
	movea.l	d1,a4
	movea.l	a0,a3
C68K_method_003ACopperStart_002EDos_003A060009A0_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	104(a7),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A060009A0_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002
C68K_method_003ACopperStart_002EDos_003A060009A0_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	move.l	a7,d0
	addi.l	#$00000024,d0
	movea.l	a3,a0
	movea.l	a4,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000994_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A060009A0_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A060009A0_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	moveq	#0,d0
	lea	108(a7),a7
	movem.l	(a7)+,a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A060009A0_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	lea	68(a7),a0
	movea.l	a0,a1
	moveq	#0,d0
	moveq	#8,d1
C68K_generated_003Aallocated_aggregate_zero_loop_003A1:
	move.l	d0,(a1)+
	dbra	d1,C68K_generated_003Aallocated_aggregate_zero_loop_003A1
	lea	68(a7),a0
	move.l	#$44514854,d0
	move.l	d0,(a0)
	lea	68(a7),a0
	moveq	#1,d0
	move.l	d0,4(a0)
	lea	68(a7),a0
	moveq	#1,d0
	move.l	d0,8(a0)
	lea	68(a7),a1
	movea.l	104(a7),a0
	move.l	a0,12(a1)
	lea	68(a7),a0
	move.l	a4,16(a0)
	lea	68(a7),a0
	move.l	(a0),d0
	move.l	d0,(a7)
	move.l	4(a0),d0
	move.l	d0,4(a7)
	move.l	8(a0),d0
	move.l	d0,8(a7)
	move.l	12(a0),d0
	move.l	d0,12(a7)
	move.l	16(a0),d0
	move.l	d0,16(a7)
	move.l	20(a0),d0
	move.l	d0,20(a7)
	move.l	24(a0),d0
	move.l	d0,24(a7)
	move.l	28(a0),d0
	move.l	d0,28(a7)
	move.l	32(a0),d0
	move.l	d0,32(a7)
	move.l	a7,d0
	movea.l	a3,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600099D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	andi.l	#$000000FF,d0
	lea	108(a7),a7
	movem.l	(a7)+,a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600099C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A060009A0_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	pea	(a2)
	pea	(a3)
	lea	-40(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	move.l	a1,36(a7)
	movea.l	a0,a2
	movea.l	d0,a3
C68K_method_003ACopperStart_002EDos_003A0600099C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	movea.l	a3,a0
	moveq	#0,d0
	moveq	#8,d1
C68K_generated_003Aallocated_aggregate_zero_loop_003A2:
	move.l	d0,(a0)+
	dbra	d1,C68K_generated_003Aallocated_aggregate_zero_loop_003A2
	move.l	36(a7),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600099C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A0600099C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	move.l	36(a7),d1
	moveq	#3,d0
	and.l	d0,d1
	tst.l	d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600099C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A0600099C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	movea.l	36(a7),a0
	move.l	a2,d0
	moveq	#36,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001A
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600099C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A0600099C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	moveq	#0,d0
	lea	40(a7),a7
	movea.l	(a7)+,a3
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A0600099C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	lea	(a7),a0
	movea.l	a0,a1
	moveq	#0,d0
	moveq	#8,d1
C68K_generated_003Aallocated_aggregate_zero_loop_003A3:
	move.l	d0,(a1)+
	dbra	d1,C68K_generated_003Aallocated_aggregate_zero_loop_003A3
	lea	(a7),a1
	movea.l	36(a7),a0
	moveq	#0,d0
	move.l	(a0),d0
	move.l	d0,(a1)
	lea	(a7),a1
	movea.l	36(a7),a0
	moveq	#4,d0
	move.l	4(a0),d0
	move.l	d0,4(a1)
	lea	(a7),a1
	movea.l	36(a7),a0
	moveq	#8,d0
	move.l	8(a0),d0
	move.l	d0,8(a1)
	lea	(a7),a1
	movea.l	36(a7),a0
	moveq	#12,d0
	move.l	12(a0),d0
	move.l	d0,12(a1)
	lea	(a7),a1
	movea.l	36(a7),a0
	moveq	#16,d0
	move.l	16(a0),d0
	move.l	d0,16(a1)
	lea	(a7),a1
	movea.l	36(a7),a0
	moveq	#20,d0
	move.l	20(a0),d0
	move.l	d0,20(a1)
	lea	(a7),a1
	movea.l	36(a7),a0
	moveq	#24,d0
	move.l	24(a0),d0
	move.l	d0,24(a1)
	lea	(a7),a1
	movea.l	36(a7),a0
	moveq	#28,d0
	move.l	28(a0),d0
	move.l	d0,28(a1)
	lea	(a7),a1
	movea.l	36(a7),a0
	moveq	#32,d0
	move.l	32(a0),d0
	move.l	d0,32(a1)
	lea	(a7),a0
	move.l	(a0),d0
	move.l	d0,(a3)
	move.l	4(a0),d0
	move.l	d0,4(a3)
	move.l	8(a0),d0
	move.l	d0,8(a3)
	move.l	12(a0),d0
	move.l	d0,12(a3)
	move.l	16(a0),d0
	move.l	d0,16(a3)
	move.l	20(a0),d0
	move.l	d0,20(a3)
	move.l	24(a0),d0
	move.l	d0,24(a3)
	move.l	28(a0),d0
	move.l	d0,28(a3)
	move.l	32(a0),d0
	move.l	d0,32(a3)
	movea.l	a2,a0
	movea.l	a3,a1
	lea	40(a7),a7
	movea.l	(a7)+,a3
	movea.l	(a7)+,a2
	bra.w	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
C68K_method_003ACopperStart_002EDos_003A06000994_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A0600099C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	pea	(a2)
	pea	(a3)
	movea.l	d0,a2
	movea.l	a0,a3
C68K_method_003ACopperStart_002EDos_003A06000994_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	movea.l	a3,a0
	move.l	a2,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000990_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000994_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002
C68K_method_003ACopperStart_002EDos_003A06000994_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	movea.l	a3,a0
	movea.l	a2,a1
	movea.l	(a7)+,a3
	movea.l	(a7)+,a2
	bra.w	C68K_method_003ACopperStart_002EDos_003A06000998_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
C68K_method_003ACopperStart_002EDos_003A06000994_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	moveq	#0,d0
	movea.l	(a7)+,a3
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A06000960_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A06000994_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	move.l	d2,-(a7)
	subq.l	#4,a7
	move.l	a1,(a7)
	movea.l	a0,a1
	move.l	d0,d2
C68K_method_003ACopperStart_002EDos_003A06000960_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	(a7),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000960_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005
C68K_method_003ACopperStart_002EDos_003A06000960_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	move.l	(a7),d1
	moveq	#3,d0
	and.l	d0,d1
	tst.l	d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000960_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005
C68K_method_003ACopperStart_002EDos_003A06000960_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	tst.l	d2
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000960_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005
C68K_method_003ACopperStart_002EDos_003A06000960_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	move.l	(a7),d1
	moveq	#-1,d0
	sub.l	d2,d0
	cmp.l	d0,d1
	bhi.s	C68K_method_003ACopperStart_002EDos_003A06000960_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005
C68K_method_003ACopperStart_002EDos_003A06000960_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	movea.l	(a7),a0
	move.l	a1,d0
	move.l	d2,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001A
	andi.l	#$000000FF,d0
	addq.l	#4,a7
	move.l	(a7)+,d2
	rts
C68K_method_003ACopperStart_002EDos_003A06000960_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	moveq	#0,d0
	addq.l	#4,a7
	move.l	(a7)+,d2
	rts
C68K_method_003ACopperStart_002EDos_003A06000960_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000962:
	movem.l	d2-d4,-(a7)
	move.l	a1,d2
	move.l	a0,d3
	move.l	d0,d4
C68K_method_003ACopperStart_002EDos_003A06000962_003ABB0000:
	moveq	#-1,d0
	sub.l	d4,d0
	cmp.l	d0,d3
	bhi.s	C68K_method_003ACopperStart_002EDos_003A06000962_003ABB0004
C68K_method_003ACopperStart_002EDos_003A06000962_003ABB0001:
	moveq	#-1,d0
	sub.l	d1,d0
	cmp.l	d0,d2
	bhi.s	C68K_method_003ACopperStart_002EDos_003A06000962_003ABB0004
C68K_method_003ACopperStart_002EDos_003A06000962_003ABB0002:
	move.l	d2,d0
	add.l	d1,d0
	cmp.l	d0,d3
	bcc.s	C68K_method_003ACopperStart_002EDos_003A06000962_003ABB0004
C68K_method_003ACopperStart_002EDos_003A06000962_003ABB0003:
	add.l	d4,d3
	cmp.l	d3,d2
	scs	d0
	extb.l	d0
	neg.l	d0
	movem.l	(a7)+,d2-d4
	rts
C68K_method_003ACopperStart_002EDos_003A06000962_003ABB0004:
	moveq	#0,d0
	movem.l	(a7)+,d2-d4
	rts
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A06000962_003Aend:
	movem.l	d2-d5/a2-a4,-(a7)
	lea	-192(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	clr.l	64(a7)
	clr.l	68(a7)
	clr.l	72(a7)
	clr.l	76(a7)
	clr.l	80(a7)
	clr.l	84(a7)
	clr.l	88(a7)
	clr.l	92(a7)
	clr.l	96(a7)
	clr.l	100(a7)
	clr.l	104(a7)
	clr.l	108(a7)
	clr.l	112(a7)
	clr.l	116(a7)
	clr.l	120(a7)
	clr.l	124(a7)
	clr.l	128(a7)
	clr.l	132(a7)
	clr.l	136(a7)
	clr.l	140(a7)
	clr.l	144(a7)
	clr.l	148(a7)
	clr.l	152(a7)
	clr.l	156(a7)
	clr.l	160(a7)
	clr.l	164(a7)
	clr.l	168(a7)
	clr.l	172(a7)
	clr.l	176(a7)
	move.l	a1,180(a7)
	move.l	d1,184(a7)
	move.l	232(a7),188(a7)
	movea.l	228(a7),a3
	move.l	240(a7),d5
	move.l	236(a7),d4
	move.l	d0,d3
	movea.l	a0,a2
	move.l	224(a7),d2
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	180(a7),d0
	tst.l	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	move.l	180(a7),d1
	moveq	#3,d0
	and.l	d0,d1
	tst.l	d1
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	move.l	184(a7),d0
	tst.l	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	tst.l	d2
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	move.l	#$000000FF,d0
	cmp.l	d0,d2
	bhi.s	C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	movea.l	a2,a0
	movea.l	184(a7),a1
	move.l	d2,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600095A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	moveq	#72,d1
	add.l	d2,d1
	move.l	d1,d0
	addq.l	#1,d0
	cmp.l	d0,d3
	bcs.s	C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
	move.l	180(a7),d1
	moveq	#-1,d0
	sub.l	d3,d0
	cmp.l	d0,d1
	bhi.s	C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
	move.l	a2,d0
	movea.l	180(a7),a0
	move.l	d3,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001A
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009:
	movea.l	180(a7),a0
	move.l	d3,d0
	movea.l	184(a7),a1
	move.l	d2,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600095B
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A:
	move.l	188(a7),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B:
	move.l	188(a7),d1
	moveq	#-1,d0
	sub.l	d4,d0
	cmp.l	d0,d1
	bhi.s	C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C:
	movea.l	180(a7),a0
	move.l	d3,d0
	movea.l	188(a7),a1
	move.l	d4,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600095B
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D:
	moveq	#0,d0
	lea	192(a7),a7
	movem.l	(a7)+,d2-d5/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F:
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0016:
	movea.l	184(a7),a0
	move.l	d1,d0
	adda.l	d0,a0
	move.b	(a0),d0
	andi.l	#$000000FF,d0
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0011:
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0017:
	addq.l	#1,d1
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0012:
	move.l	d1,d0
	cmp.l	d2,d0
	bcs.s	C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0013:
	movea.l	188(a7),a1
	move.l	d4,d0
	move.l	d5,d1
	lea	(a7),a0
	move.l	a0,-(a7)
	movea.l	a2,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600093C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	addq.l	#4,a7
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0015
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0014:
	moveq	#0,d0
	lea	192(a7),a7
	movem.l	(a7)+,d2-d5/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E:
	moveq	#0,d1
	bra.s	C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0012
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010:
	moveq	#0,d0
	lea	192(a7),a7
	movem.l	(a7)+,d2-d5/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0015:
	movea.l	180(a7),a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000949
	move.l	a0,d0
	movea.l	d0,a4
	move.l	a2,d0
	movea.l	184(a7),a0
	move.l	d2,d1
	movea.l	a4,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000019
	move.l	d2,d0
	moveq	#0,d1
	movea.l	a4,a0
	andi.l	#$000000FF,d1
	adda.l	d0,a0
	move.b	d1,(a0)
	lea	108(a7),a0
	movea.l	a0,a1
	moveq	#0,d0
	moveq	#17,d1
C68K_generated_003Aallocated_aggregate_zero_loop_003A4:
	move.l	d0,(a1)+
	dbra	d1,C68K_generated_003Aallocated_aggregate_zero_loop_003A4
	lea	108(a7),a0
	move.l	#$44514348,d0
	move.l	d0,(a0)
	lea	108(a7),a0
	moveq	#2,d0
	move.l	d0,4(a0)
	lea	108(a7),a0
	move.l	a3,8(a0)
	lea	108(a7),a0
	move.l	d3,12(a0)
	lea	108(a7),a0
	move.l	d2,16(a0)
	lea	108(a7),a0
	moveq	#0,d0
	clr.l	20(a0)
	lea	108(a7),a1
	lea	(a7),a0
	move.l	(a0),d0
	move.l	d0,24(a1)
	move.l	4(a0),d0
	move.l	d0,28(a1)
	move.l	8(a0),d0
	move.l	d0,32(a1)
	move.l	12(a0),d0
	move.l	d0,36(a1)
	move.l	16(a0),d0
	move.l	d0,40(a1)
	move.l	20(a0),d0
	move.l	d0,44(a1)
	move.l	24(a0),d0
	move.l	d0,48(a1)
	move.l	28(a0),d0
	move.l	d0,52(a1)
	move.l	32(a0),d0
	move.l	d0,56(a1)
	lea	108(a7),a0
	move.l	(a0),d0
	move.l	d0,36(a7)
	move.l	4(a0),d0
	move.l	d0,40(a7)
	move.l	8(a0),d0
	move.l	d0,44(a7)
	move.l	12(a0),d0
	move.l	d0,48(a7)
	move.l	16(a0),d0
	move.l	d0,52(a7)
	move.l	20(a0),d0
	move.l	d0,56(a7)
	move.l	24(a0),d0
	move.l	d0,60(a7)
	move.l	28(a0),d0
	move.l	d0,64(a7)
	move.l	32(a0),d0
	move.l	d0,68(a7)
	move.l	36(a0),d0
	move.l	d0,72(a7)
	move.l	40(a0),d0
	move.l	d0,76(a7)
	move.l	44(a0),d0
	move.l	d0,80(a7)
	move.l	48(a0),d0
	move.l	d0,84(a7)
	move.l	52(a0),d0
	move.l	d0,88(a7)
	move.l	56(a0),d0
	move.l	d0,92(a7)
	move.l	60(a0),d0
	move.l	d0,96(a7)
	move.l	64(a0),d0
	move.l	d0,100(a7)
	move.l	68(a0),d0
	move.l	d0,104(a7)
	movea.l	a2,a0
	movea.l	180(a7),a1
	move.l	a7,d0
	addi.l	#$00000024,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600094B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	andi.l	#$000000FF,d0
	lea	192(a7),a7
	movem.l	(a7)+,d2-d5/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A06000951_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	d2-d4/a2-a5,-(a7)
	lea	-392(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	clr.l	64(a7)
	clr.l	68(a7)
	clr.l	72(a7)
	clr.l	76(a7)
	clr.l	80(a7)
	clr.l	84(a7)
	clr.l	88(a7)
	clr.l	92(a7)
	clr.l	96(a7)
	clr.l	100(a7)
	clr.l	104(a7)
	clr.l	108(a7)
	clr.l	112(a7)
	clr.l	116(a7)
	clr.l	120(a7)
	clr.l	124(a7)
	clr.l	128(a7)
	clr.l	132(a7)
	clr.l	136(a7)
	clr.l	140(a7)
	clr.l	144(a7)
	clr.l	148(a7)
	clr.l	152(a7)
	clr.l	156(a7)
	clr.l	160(a7)
	clr.l	164(a7)
	clr.l	168(a7)
	clr.l	172(a7)
	movea.l	d0,a3
	movea.l	a1,a5
	movea.l	a0,a2
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	a7,d0
	movea.l	a2,a0
	movea.l	a5,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000990_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	lea	(a7),a1
	movea.l	a2,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000998_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	move.l	a7,d0
	addi.l	#$00000020,d0
	movea.l	a2,a0
	movea.l	a3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000952_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	lea	(a7),a0
	move.l	12(a0),d1
	lea	(a7),a0
	move.l	16(a0),d0
	cmp.l	d0,d1
	bcc.w	C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	lea	32(a7),a0
	lea	8(a0),a0
	move.l	(a0),d0
	tst.l	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	lea	32(a7),a0
	move.l	20(a0),d0
	tst.l	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	lea	32(a7),a0
	lea	64(a0),a0
	move.l	(a0),d0
	tst.l	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
	lea	32(a7),a0
	lea	68(a0),a0
	move.l	(a0),d0
	tst.l	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
	lea	32(a7),a0
	move.l	24(a0),d0
	move.l	d0,176(a7)
	move.l	28(a0),d0
	move.l	d0,180(a7)
	move.l	32(a0),d0
	move.l	d0,184(a7)
	move.l	36(a0),d0
	move.l	d0,188(a7)
	move.l	40(a0),d0
	move.l	d0,192(a7)
	move.l	44(a0),d0
	move.l	d0,196(a7)
	move.l	48(a0),d0
	move.l	d0,200(a7)
	move.l	52(a0),d0
	move.l	d0,204(a7)
	move.l	56(a0),d0
	move.l	d0,208(a7)
	lea	176(a7),a0
	move.l	8(a0),d1
	lea	(a7),a0
	move.l	20(a0),d0
	cmp.l	d0,d1
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009:
	lea	32(a7),a0
	move.l	24(a0),d0
	move.l	d0,212(a7)
	move.l	28(a0),d0
	move.l	d0,216(a7)
	move.l	32(a0),d0
	move.l	d0,220(a7)
	move.l	36(a0),d0
	move.l	d0,224(a7)
	move.l	40(a0),d0
	move.l	d0,228(a7)
	move.l	44(a0),d0
	move.l	d0,232(a7)
	move.l	48(a0),d0
	move.l	d0,236(a7)
	move.l	52(a0),d0
	move.l	d0,240(a7)
	move.l	56(a0),d0
	move.l	d0,244(a7)
	lea	212(a7),a0
	move.l	4(a0),d2
	lea	(a7),a0
	move.l	20(a0),d0
	lea	(a7),a0
	move.l	24(a0),d1
	muls.l	d1,d0
	cmp.l	d0,d2
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A:
	lea	32(a7),a0
	move.l	24(a0),d0
	move.l	d0,248(a7)
	move.l	28(a0),d0
	move.l	d0,252(a7)
	move.l	32(a0),d0
	move.l	d0,256(a7)
	move.l	36(a0),d0
	move.l	d0,260(a7)
	move.l	40(a0),d0
	move.l	d0,264(a7)
	move.l	44(a0),d0
	move.l	d0,268(a7)
	move.l	48(a0),d0
	move.l	d0,272(a7)
	move.l	52(a0),d0
	move.l	d0,276(a7)
	move.l	56(a0),d0
	move.l	d0,280(a7)
	lea	248(a7),a0
	move.l	28(a0),d1
	moveq	#1,d0
	cmp.l	d0,d1
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B:
	lea	32(a7),a0
	move.l	24(a0),d0
	move.l	d0,284(a7)
	move.l	28(a0),d0
	move.l	d0,288(a7)
	move.l	32(a0),d0
	move.l	d0,292(a7)
	move.l	36(a0),d0
	move.l	d0,296(a7)
	move.l	40(a0),d0
	move.l	d0,300(a7)
	move.l	44(a0),d0
	move.l	d0,304(a7)
	move.l	48(a0),d0
	move.l	d0,308(a7)
	move.l	52(a0),d0
	move.l	d0,312(a7)
	move.l	56(a0),d0
	move.l	d0,316(a7)
	lea	284(a7),a0
	move.l	32(a0),d1
	moveq	#1,d0
	cmp.l	d0,d1
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C:
	lea	32(a7),a0
	move.l	24(a0),d0
	move.l	d0,320(a7)
	move.l	28(a0),d0
	move.l	d0,324(a7)
	move.l	32(a0),d0
	move.l	d0,328(a7)
	move.l	36(a0),d0
	move.l	d0,332(a7)
	move.l	40(a0),d0
	move.l	d0,336(a7)
	move.l	44(a0),d0
	move.l	d0,340(a7)
	move.l	48(a0),d0
	move.l	d0,344(a7)
	move.l	52(a0),d0
	move.l	d0,348(a7)
	move.l	56(a0),d0
	move.l	d0,352(a7)
	lea	320(a7),a0
	move.l	24(a0),d0
	tst.l	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D:
	lea	32(a7),a0
	move.l	24(a0),d0
	move.l	d0,356(a7)
	move.l	28(a0),d0
	move.l	d0,360(a7)
	move.l	32(a0),d0
	move.l	d0,364(a7)
	move.l	36(a0),d0
	move.l	d0,368(a7)
	move.l	40(a0),d0
	move.l	d0,372(a7)
	move.l	44(a0),d0
	move.l	d0,376(a7)
	move.l	48(a0),d0
	move.l	d0,380(a7)
	move.l	52(a0),d0
	move.l	d0,384(a7)
	move.l	56(a0),d0
	move.l	d0,388(a7)
	lea	356(a7),a0
	move.l	20(a0),d0
	tst.l	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E:
	moveq	#0,d0
	lea	392(a7),a7
	movem.l	(a7)+,d2-d4/a2-a5
	rts
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010:
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0019:
	move.l	a4,d1
	move.l	a3,d0
	cmp.l	d0,d1
	beq.w	C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0013
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0011:
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001A:
	move.l	a7,d0
	addi.l	#$00000068,d0
	movea.l	a2,a0
	movea.l	a4,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000952_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0013
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0012:
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001B:
	movea.l	a3,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000949
	move.l	a0,d4
	lea	32(a7),a0
	move.l	16(a0),d3
	movea.l	a4,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000949
	move.l	a0,d1
	lea	104(a7),a0
	move.l	16(a0),d0
	move.l	d0,-(a7)
	movea.l	a2,a0
	movea.l	d4,a1
	move.l	d3,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600099A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	addq.l	#4,a7
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0013
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0014:
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001C:
	lea	104(a7),a0
	move.l	8(a0),d0
	movea.l	d0,a4
	addq.l	#1,d2
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0015:
	lea	(a7),a0
	move.l	12(a0),d0
	cmp.l	d0,d2
	bcs.s	C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0016:
	lea	32(a7),a1
	lea	(a7),a0
	move.l	8(a0),d0
	move.l	d0,8(a1)
	move.l	a7,d0
	addi.l	#$00000020,d0
	movea.l	a2,a0
	movea.l	a3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600094B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0018
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0017:
	moveq	#0,d0
	lea	392(a7),a7
	movem.l	(a7)+,d2-d4/a2-a5
	rts
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F:
	lea	(a7),a0
	move.l	8(a0),d0
	movea.l	d0,a4
	moveq	#0,d2
	bra.s	C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0015
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0013:
	moveq	#0,d0
	lea	392(a7),a7
	movem.l	(a7)+,d2-d4/a2-a5
	rts
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0018:
	lea	(a7),a0
	move.l	a3,8(a0)
	lea	(a7),a0
	lea	12(a0),a0
	move.l	(a0),d1
	addq.l	#1,d1
	move.l	d1,(a0)
	move.l	a7,d0
	movea.l	a2,a0
	movea.l	a5,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000991_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	andi.l	#$000000FF,d0
	lea	392(a7),a7
	movem.l	(a7)+,d2-d4/a2-a5
	rts
C68K_method_003ACopperStart_002EDos_003A0600094A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A06000996_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	pea	(a2)
	pea	(a3)
	lea	-76(a7),a7
	move.l	a1,72(a7)
	movea.l	a0,a3
	movea.l	d0,a2
C68K_method_003ACopperStart_002EDos_003A0600094A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	movea.l	a2,a0
	moveq	#0,d0
	moveq	#17,d1
C68K_generated_003Aallocated_aggregate_zero_loop_003A5:
	move.l	d0,(a0)+
	dbra	d1,C68K_generated_003Aallocated_aggregate_zero_loop_003A5
	move.l	72(a7),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600094A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A0600094A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	move.l	72(a7),d1
	moveq	#3,d0
	and.l	d0,d1
	tst.l	d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600094A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A0600094A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	movea.l	72(a7),a0
	move.l	a3,d0
	moveq	#72,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001A
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600094A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A0600094A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	moveq	#0,d0
	lea	76(a7),a7
	movea.l	(a7)+,a3
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A0600094A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	movea.l	72(a7),a1
	lea	(a7),a0
	move.l	a0,-(a7)
	movea.l	a3,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600094D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	addq.l	#4,a7
	lea	(a7),a0
	move.l	(a0),d0
	move.l	d0,(a2)
	move.l	4(a0),d0
	move.l	d0,4(a2)
	move.l	8(a0),d0
	move.l	d0,8(a2)
	move.l	12(a0),d0
	move.l	d0,12(a2)
	move.l	16(a0),d0
	move.l	d0,16(a2)
	move.l	20(a0),d0
	move.l	d0,20(a2)
	move.l	24(a0),d0
	move.l	d0,24(a2)
	move.l	28(a0),d0
	move.l	d0,28(a2)
	move.l	32(a0),d0
	move.l	d0,32(a2)
	move.l	36(a0),d0
	move.l	d0,36(a2)
	move.l	40(a0),d0
	move.l	d0,40(a2)
	move.l	44(a0),d0
	move.l	d0,44(a2)
	move.l	48(a0),d0
	move.l	d0,48(a2)
	move.l	52(a0),d0
	move.l	d0,52(a2)
	move.l	56(a0),d0
	move.l	d0,56(a2)
	move.l	60(a0),d0
	move.l	d0,60(a2)
	move.l	64(a0),d0
	move.l	d0,64(a2)
	move.l	68(a0),d0
	move.l	d0,68(a2)
	movea.l	72(a7),a1
	movea.l	a3,a0
	move.l	a2,d0
	lea	76(a7),a7
	movea.l	(a7)+,a3
	movea.l	(a7)+,a2
	bra.w	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
C68K_method_003ACopperStart_002EDos_003A0600094A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A0600094B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	a2-a5,-(a7)
	movea.l	d0,a2
	movea.l	a1,a3
	movea.l	a0,a5
C68K_method_003ACopperStart_002EDos_003A0600094B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	movea.l	a5,a0
	movea.l	a3,a1
	move.l	a2,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600094B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002
C68K_method_003ACopperStart_002EDos_003A0600094B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	moveq	#0,d0
	movem.l	(a7)+,a2-a5
	rts
C68K_method_003ACopperStart_002EDos_003A0600094B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	move.l	(a2),d1
	movea.l	a3,a0
	moveq	#0,d0
	move.l	d1,(a0)
	move.l	4(a2),d1
	movea.l	a3,a0
	moveq	#4,d0
	move.l	d1,4(a0)
	lea	8(a2),a0
	move.l	(a0),d0
	move.l	d0,d1
	movea.l	a3,a0
	moveq	#8,d0
	move.l	d1,8(a0)
	move.l	12(a2),d1
	movea.l	a3,a0
	moveq	#12,d0
	move.l	d1,12(a0)
	move.l	16(a2),d1
	movea.l	a3,a0
	moveq	#16,d0
	move.l	d1,16(a0)
	move.l	20(a2),d1
	movea.l	a3,a0
	moveq	#20,d0
	move.l	d1,20(a0)
	lea	24(a2),a4
	movea.l	a5,a0
	movea.l	a3,a1
	move.l	a4,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000950_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	move.l	60(a2),d1
	movea.l	a3,a0
	moveq	#60,d0
	move.l	d1,60(a0)
	lea	64(a2),a0
	move.l	(a0),d0
	move.l	d0,d1
	movea.l	a3,a0
	moveq	#64,d0
	move.l	d1,64(a0)
	lea	68(a2),a0
	move.l	(a0),d0
	move.l	d0,d1
	movea.l	a3,a0
	moveq	#68,d0
	move.l	d1,68(a0)
	moveq	#1,d0
	movem.l	(a7)+,a2-a5
	rts
C68K_method_003ACopperStart_002EDos_003A06000940_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A0600094B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	pea	(a2)
	pea	(a3)
	movea.l	a1,a2
	movea.l	d0,a3
C68K_method_003ACopperStart_002EDos_003A06000940_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	moveq	#0,d0
	move.b	d0,(a3)
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000940_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002
C68K_method_003ACopperStart_002EDos_003A06000940_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	moveq	#0,d0
	movea.l	(a7)+,a3
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A06000940_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	move.l	28(a2),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000940_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A06000940_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	move.l	32(a2),d0
	tst.l	d0
	shi	d0
	extb.l	d0
	neg.l	d0
C68K_method_003ACopperStart_002EDos_003A06000940_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	move.b	d0,(a3)
	moveq	#0,d0
	clr.l	28(a2)
	moveq	#1,d0
	movea.l	(a7)+,a3
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A06000940_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	moveq	#0,d0
	bra.s	C68K_method_003ACopperStart_002EDos_003A06000940_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005
C68K_method_003ACopperStart_002EDos_003A0600093F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A06000940_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	pea	(a2)
	movea.l	a1,a2
C68K_method_003ACopperStart_002EDos_003A0600093F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600093F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002
C68K_method_003ACopperStart_002EDos_003A0600093F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	moveq	#0,d0
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A0600093F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	lea	20(a2),a0
	move.l	(a0),d1
	move.l	24(a2),d0
	add.l	d0,d1
	move.l	d1,(a0)
	moveq	#0,d0
	clr.l	24(a2)
	moveq	#0,d0
	clr.l	32(a2)
	moveq	#1,d0
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A0600093F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000952_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A06000952_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	bra.w	C68K_method_003ACopperStart_002EDos_003A0600094A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
C68K_method_003ACopperStart_002EDos_003A06000952_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000982_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	d2/a2-a3,-(a7)
	lea	-64(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	movea.l	a1,a3
	movea.l	a0,a2
C68K_method_003ACopperStart_002EDos_003A06000982_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	a7,d0
	movea.l	a2,a0
	movea.l	a3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000970_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000982_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002
C68K_method_003ACopperStart_002EDos_003A06000982_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	moveq	#0,d0
	lea	64(a7),a7
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A06000982_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	move.l	a7,d0
	addi.l	#$00000020,d0
	movea.l	a2,a0
	movea.l	a3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000971_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	move.l	d0,d2
	moveq	#2,d0
	cmp.l	d0,d2
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000982_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A06000982_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	lea	32(a7),a0
	move.l	28(a0),d1
	lea	(a7),a0
	move.l	28(a0),d0
	cmp.l	d0,d1
	seq	d0
	extb.l	d0
	neg.l	d0
	tst.l	d0
	seq	d0
	extb.l	d0
	neg.l	d0
	lea	64(a7),a7
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A06000982_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	moveq	#1,d0
	cmp.l	d0,d2
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000982_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007
C68K_method_003ACopperStart_002EDos_003A06000982_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	moveq	#3,d0
	cmp.l	d0,d2
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000982_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007
C68K_method_003ACopperStart_002EDos_003A06000982_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	moveq	#0,d0
	lea	64(a7),a7
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A06000982_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
	move.l	a7,d0
	addi.l	#$00000020,d0
	movea.l	a2,a0
	movea.l	a3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000982_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009
C68K_method_003ACopperStart_002EDos_003A06000982_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
	moveq	#0,d0
	lea	64(a7),a7
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A06000982_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009:
	moveq	#3,d0
	cmp.l	d0,d2
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000982_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B
C68K_method_003ACopperStart_002EDos_003A06000982_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A:
	lea	32(a7),a1
	movea.l	a2,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000983_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
C68K_method_003ACopperStart_002EDos_003A06000982_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B:
	move.l	a2,d0
	movea.l	a3,a0
	moveq	#48,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001E
	lea	32(a7),a0
	move.l	8(a0),d2
	lea	32(a7),a0
	move.l	(a0),d1
	move.l	a2,d0
	movea.l	d2,a0
	movea.l	d1,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000031
	moveq	#1,d0
	lea	64(a7),a7
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A06000982_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	movem.l	d2-d5/a2-a4,-(a7)
	lea	-176(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	clr.l	64(a7)
	clr.l	68(a7)
	clr.l	72(a7)
	clr.l	76(a7)
	clr.l	80(a7)
	clr.l	84(a7)
	clr.l	88(a7)
	clr.l	92(a7)
	clr.l	96(a7)
	clr.l	100(a7)
	movea.l	a1,a4
	movea.l	a0,a2
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	a7,d0
	movea.l	a2,a0
	movea.l	a4,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000994_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	moveq	#0,d0
	lea	176(a7),a7
	movem.l	(a7)+,d2-d5/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B:
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0016:
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0017:
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0018:
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0019:
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001A:
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001B:
	addq.l	#1,d2
	movea.l	d5,a3
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C:
	cmp.l	d4,d2
	bcc.w	C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E:
	move.l	a7,d0
	addi.l	#$00000020,d0
	movea.l	a2,a0
	movea.l	a3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000952_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F:
	lea	32(a7),a0
	move.l	8(a0),d5
	lea	32(a7),a0
	move.l	20(a0),d0
	tst.l	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010:
	lea	32(a7),a0
	lea	64(a0),a0
	move.l	(a0),d0
	tst.l	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0011:
	lea	32(a7),a0
	lea	68(a0),a0
	move.l	(a0),d0
	tst.l	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0012:
	lea	32(a7),a0
	move.l	24(a0),d0
	move.l	d0,104(a7)
	move.l	28(a0),d0
	move.l	d0,108(a7)
	move.l	32(a0),d0
	move.l	d0,112(a7)
	move.l	36(a0),d0
	move.l	d0,116(a7)
	move.l	40(a0),d0
	move.l	d0,120(a7)
	move.l	44(a0),d0
	move.l	d0,124(a7)
	move.l	48(a0),d0
	move.l	d0,128(a7)
	move.l	52(a0),d0
	move.l	d0,132(a7)
	move.l	56(a0),d0
	move.l	d0,136(a7)
	lea	104(a7),a0
	move.l	28(a0),d0
	tst.l	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0013:
	lea	32(a7),a0
	move.l	24(a0),d0
	move.l	d0,140(a7)
	move.l	28(a0),d0
	move.l	d0,144(a7)
	move.l	32(a0),d0
	move.l	d0,148(a7)
	move.l	36(a0),d0
	move.l	d0,152(a7)
	move.l	40(a0),d0
	move.l	d0,156(a7)
	move.l	44(a0),d0
	move.l	d0,160(a7)
	move.l	48(a0),d0
	move.l	d0,164(a7)
	move.l	52(a0),d0
	move.l	d0,168(a7)
	move.l	56(a0),d0
	move.l	d0,172(a7)
	lea	140(a7),a0
	move.l	32(a0),d0
	tst.l	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009:
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0014:
	movea.l	a2,a0
	movea.l	a4,a1
	move.l	a3,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600095E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A:
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0015:
	addq.l	#1,d3
	bra.w	C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	lea	(a7),a0
	move.l	8(a0),d0
	movea.l	d0,a3
	lea	(a7),a0
	move.l	12(a0),d4
	moveq	#0,d3
	moveq	#0,d2
	bra.w	C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D:
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001C:
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001D:
	move.l	d3,d0
	lea	176(a7),a7
	movem.l	(a7)+,d2-d5/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600001E:
C68K_method_003ACopperStart_002EDos_003A0600095D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	move.l	d2,-(a7)
	pea	(a6)
	subq.l	#4,a7
	move.l	a0,(a7)
	move.l	d1,d2
C68K_method_003ACopperStart_002EDos_003A0600001E_003ABB0000:
	move.l	(a7),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600001E_003ABB0003
C68K_method_003ACopperStart_002EDos_003A0600001E_003ABB0001:
	tst.l	d2
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600001E_003ABB0003
C68K_method_003ACopperStart_002EDos_003A0600001E_003ABB0002:
	move.l	(a7),d0
	movea.l	d0,a1
	move.l	d2,d0
	movea.l	_ExecBase,a6
	jsr	-210(a6)
C68K_method_003ACopperStart_002EDos_003A0600001E_003ABB0003:
	addq.l	#4,a7
	movea.l	(a7)+,a6
	move.l	(a7)+,d2
	rts
C68K_method_003ACopperStart_002EDos_003A0600001E_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000991_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	pea	(a2)
	subq.l	#4,a7
	move.l	a1,(a7)
	movea.l	d0,a2
	movea.l	a0,a1
C68K_method_003ACopperStart_002EDos_003A06000991_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	(a7),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000991_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A06000991_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	move.l	(a7),d1
	moveq	#3,d0
	and.l	d0,d1
	tst.l	d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000991_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A06000991_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	movea.l	(a7),a0
	move.l	a1,d0
	moveq	#32,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001A
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000991_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A06000991_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	movea.l	a2,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000992
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000991_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005
C68K_method_003ACopperStart_002EDos_003A06000991_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	moveq	#0,d0
	addq.l	#4,a7
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A06000991_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	movea.l	(a7),a0
	move.l	(a2),d1
	moveq	#0,d0
	move.l	d1,(a0)
	movea.l	(a7),a0
	move.l	4(a2),d1
	moveq	#4,d0
	move.l	d1,4(a0)
	movea.l	(a7),a1
	lea	8(a2),a0
	move.l	(a0),d0
	move.l	d0,d1
	movea.l	a1,a0
	moveq	#8,d0
	move.l	d1,8(a0)
	movea.l	(a7),a0
	move.l	12(a2),d1
	moveq	#12,d0
	move.l	d1,12(a0)
	movea.l	(a7),a0
	move.l	16(a2),d1
	moveq	#16,d0
	move.l	d1,16(a0)
	movea.l	(a7),a0
	move.l	20(a2),d1
	moveq	#20,d0
	move.l	d1,20(a0)
	movea.l	(a7),a0
	move.l	24(a2),d1
	moveq	#24,d0
	move.l	d1,24(a0)
	movea.l	(a7),a0
	move.l	28(a2),d1
	moveq	#28,d0
	move.l	d1,28(a0)
	moveq	#1,d0
	addq.l	#4,a7
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A06000991_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A0600099D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	pea	(a2)
	pea	(a3)
	subq.l	#4,a7
	move.l	a1,(a7)
	movea.l	d0,a2
	movea.l	a0,a3
C68K_method_003ACopperStart_002EDos_003A0600099D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	(a7),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600099D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A0600099D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	move.l	(a7),d1
	moveq	#3,d0
	and.l	d0,d1
	tst.l	d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600099D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A0600099D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	movea.l	(a7),a0
	move.l	a3,d0
	moveq	#36,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001A
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600099D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A0600099D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	movea.l	a3,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600099D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005
C68K_method_003ACopperStart_002EDos_003A0600099D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	moveq	#0,d0
	addq.l	#4,a7
	movea.l	(a7)+,a3
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A0600099D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	movea.l	(a7),a0
	move.l	(a2),d1
	moveq	#0,d0
	move.l	d1,(a0)
	movea.l	(a7),a0
	move.l	4(a2),d1
	moveq	#4,d0
	move.l	d1,4(a0)
	movea.l	(a7),a0
	move.l	8(a2),d1
	moveq	#8,d0
	move.l	d1,8(a0)
	movea.l	(a7),a1
	lea	12(a2),a0
	move.l	(a0),d0
	move.l	d0,d1
	movea.l	a1,a0
	moveq	#12,d0
	move.l	d1,12(a0)
	movea.l	(a7),a1
	lea	16(a2),a0
	move.l	(a0),d0
	move.l	d0,d1
	movea.l	a1,a0
	moveq	#16,d0
	move.l	d1,16(a0)
	movea.l	(a7),a1
	lea	20(a2),a0
	move.l	(a0),d0
	move.l	d0,d1
	movea.l	a1,a0
	moveq	#20,d0
	move.l	d1,20(a0)
	movea.l	(a7),a1
	lea	24(a2),a0
	move.l	(a0),d0
	move.l	d0,d1
	movea.l	a1,a0
	moveq	#24,d0
	move.l	d1,24(a0)
	movea.l	(a7),a0
	move.l	28(a2),d1
	moveq	#28,d0
	move.l	d1,28(a0)
	movea.l	(a7),a0
	move.l	32(a2),d1
	moveq	#32,d0
	move.l	d1,32(a0)
	moveq	#1,d0
	addq.l	#4,a7
	movea.l	(a7)+,a3
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A06000014:
C68K_method_003ACopperStart_002EDos_003A06000014_003ABB0000:
C68K_method_003ACopperStart_002EDos_003A0600099D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	move.l	d1,d0
	adda.l	d0,a0
	move.l	(a0),d0
	rts
C68K_method_003ACopperStart_002EDos_003A06000014_003Aend:
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	d2/a2-a3,-(a7)
	movea.l	a0,a3
	movea.l	a1,a2
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	8(a2),d1
	moveq	#24,d0
	cmp.l	d0,d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	move.l	8(a2),d1
	moveq	#32,d0
	cmp.l	d0,d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	move.l	8(a2),d1
	moveq	#40,d0
	cmp.l	d0,d1
	seq	d2
	extb.l	d2
	neg.l	d2
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	move.l	(a2),d1
	move.l	#$44514854,d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	move.l	4(a2),d1
	moveq	#1,d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	lea	12(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
	lea	16(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
	move.l	32(a2),d0
	tst.l	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009:
	tst.b	d2
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A:
	lea	20(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B:
	move.l	20(a2),d0
	movea.l	a3,a0
	movea.l	d0,a1
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000194_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C:
	moveq	#0,d0
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	moveq	#1,d2
	bra.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D:
	move.l	8(a2),d1
	moveq	#1,d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0012
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E:
	lea	24(a2),a0
	move.l	(a0),d0
	tst.l	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0011
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F:
	lea	20(a2),a0
	move.l	(a0),d0
	tst.l	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0011
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010:
	move.l	28(a2),d0
	tst.l	d0
	seq	d0
	extb.l	d0
	neg.l	d0
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0011:
	moveq	#0,d0
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0012:
	move.l	8(a2),d1
	moveq	#2,d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0016
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0013:
	lea	24(a2),a0
	move.l	(a0),d0
	tst.l	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0015
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0014:
	move.l	28(a2),d0
	tst.l	d0
	seq	d0
	extb.l	d0
	neg.l	d0
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0015:
	moveq	#0,d0
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0016:
	move.l	8(a2),d1
	moveq	#4,d0
	cmp.l	d0,d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0018
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0017:
	move.l	8(a2),d1
	moveq	#12,d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001D
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0018:
	lea	20(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001C
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0019:
	lea	24(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001C
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001A:
	move.l	28(a2),d0
	tst.l	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001C
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001B:
	move.l	24(a2),d0
	movea.l	a3,a0
	movea.l	d0,a1
	movem.l	(a7)+,d2/a2-a3
	bra.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000178_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001C:
	moveq	#0,d0
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001D:
	move.l	8(a2),d1
	moveq	#24,d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0021
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001E:
	lea	24(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0020
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001F:
	move.l	28(a2),d0
	tst.l	d0
	seq	d0
	extb.l	d0
	neg.l	d0
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0020:
	moveq	#0,d0
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0021:
	move.l	8(a2),d1
	moveq	#32,d0
	cmp.l	d0,d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0022:
	move.l	8(a2),d1
	moveq	#40,d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0026
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023:
	lea	24(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0025
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0024:
	move.l	28(a2),d0
	tst.l	d0
	shi	d0
	extb.l	d0
	neg.l	d0
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0025:
	moveq	#0,d0
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0026:
	moveq	#0,d0
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A06000990_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A0600099F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	pea	(a2)
	pea	(a3)
	lea	-36(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	move.l	a1,32(a7)
	movea.l	a0,a3
	movea.l	d0,a2
C68K_method_003ACopperStart_002EDos_003A06000990_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	movea.l	a2,a0
	moveq	#0,d0
	moveq	#7,d1
C68K_generated_003Aallocated_aggregate_zero_loop_003A6:
	move.l	d0,(a0)+
	dbra	d1,C68K_generated_003Aallocated_aggregate_zero_loop_003A6
	move.l	32(a7),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000990_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A06000990_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	move.l	32(a7),d1
	moveq	#3,d0
	and.l	d0,d1
	tst.l	d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000990_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A06000990_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	movea.l	32(a7),a0
	move.l	a3,d0
	moveq	#32,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001A
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000990_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A06000990_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	moveq	#0,d0
	lea	36(a7),a7
	movea.l	(a7)+,a3
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A06000990_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	lea	(a7),a0
	movea.l	a0,a1
	moveq	#0,d0
	moveq	#7,d1
C68K_generated_003Aallocated_aggregate_zero_loop_003A7:
	move.l	d0,(a1)+
	dbra	d1,C68K_generated_003Aallocated_aggregate_zero_loop_003A7
	lea	(a7),a1
	movea.l	32(a7),a0
	moveq	#0,d0
	move.l	(a0),d0
	move.l	d0,(a1)
	lea	(a7),a1
	movea.l	32(a7),a0
	moveq	#4,d0
	move.l	4(a0),d0
	move.l	d0,4(a1)
	lea	(a7),a1
	movea.l	32(a7),a0
	moveq	#8,d0
	move.l	8(a0),d0
	move.l	d0,8(a1)
	lea	(a7),a1
	movea.l	32(a7),a0
	moveq	#12,d0
	move.l	12(a0),d0
	move.l	d0,12(a1)
	lea	(a7),a1
	movea.l	32(a7),a0
	moveq	#16,d0
	move.l	16(a0),d0
	move.l	d0,16(a1)
	lea	(a7),a1
	movea.l	32(a7),a0
	moveq	#20,d0
	move.l	20(a0),d0
	move.l	d0,20(a1)
	lea	(a7),a1
	movea.l	32(a7),a0
	moveq	#24,d0
	move.l	24(a0),d0
	move.l	d0,24(a1)
	lea	(a7),a1
	movea.l	32(a7),a0
	moveq	#28,d0
	move.l	28(a0),d0
	move.l	d0,28(a1)
	lea	(a7),a0
	move.l	(a0),d0
	move.l	d0,(a2)
	move.l	4(a0),d0
	move.l	d0,4(a2)
	move.l	8(a0),d0
	move.l	d0,8(a2)
	move.l	12(a0),d0
	move.l	d0,12(a2)
	move.l	16(a0),d0
	move.l	d0,16(a2)
	move.l	20(a0),d0
	move.l	d0,20(a2)
	move.l	24(a0),d0
	move.l	d0,24(a2)
	move.l	28(a0),d0
	move.l	d0,28(a2)
	movea.l	a2,a0
	lea	36(a7),a7
	movea.l	(a7)+,a3
	movea.l	(a7)+,a2
	bra.w	C68K_method_003ACopperStart_002EDos_003A06000992
C68K_method_003ACopperStart_002EDos_003A06000990_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000998_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	d2/a2-a3,-(a7)
	lea	-76(a7),a7
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	clr.l	64(a7)
	clr.l	68(a7)
	clr.l	72(a7)
	movea.l	a0,a2
	movea.l	a1,a3
C68K_method_003ACopperStart_002EDos_003A06000998_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	8(a3),d0
	move.l	d0,(a7)
	moveq	#0,d2
	bra.s	C68K_method_003ACopperStart_002EDos_003A06000998_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005
C68K_method_003ACopperStart_002EDos_003A06000998_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
C68K_method_003ACopperStart_002EDos_003A06000998_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
	move.l	(a7),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000998_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A06000998_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
C68K_method_003ACopperStart_002EDos_003A06000998_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
	move.l	(a7),d1
	move.l	a7,d0
	addq.l	#4,d0
	movea.l	a2,a0
	movea.l	d1,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000952_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000998_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A06000998_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
C68K_method_003ACopperStart_002EDos_003A06000998_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009:
	lea	4(a7),a0
	move.l	8(a0),d0
	move.l	d0,(a7)
	addq.l	#1,d2
C68K_method_003ACopperStart_002EDos_003A06000998_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	move.l	12(a3),d0
	cmp.l	d0,d2
	bcs.s	C68K_method_003ACopperStart_002EDos_003A06000998_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001
C68K_method_003ACopperStart_002EDos_003A06000998_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	move.l	(a7),d0
	tst.l	d0
	seq	d0
	extb.l	d0
	neg.l	d0
	andi.l	#$000000FF,d0
	lea	76(a7),a7
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A06000998_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	moveq	#0,d0
	lea	76(a7),a7
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A0600095A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A06000998_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	move.l	d2,-(a7)
	subq.l	#4,a7
	move.l	a1,(a7)
	movea.l	a0,a1
	move.l	d0,d2
C68K_method_003ACopperStart_002EDos_003A0600095A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	(a7),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600095A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A0600095A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	move.l	(a7),d1
	moveq	#-1,d0
	sub.l	d2,d0
	cmp.l	d0,d1
	bhi.s	C68K_method_003ACopperStart_002EDos_003A0600095A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A0600095A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	movea.l	(a7),a0
	move.l	a1,d0
	move.l	d2,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001A
	andi.l	#$000000FF,d0
	addq.l	#4,a7
	move.l	(a7)+,d2
	rts
C68K_method_003ACopperStart_002EDos_003A0600095A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	moveq	#0,d0
	addq.l	#4,a7
	move.l	(a7)+,d2
	rts
C68K_method_003ACopperStart_002EDos_003A0600095A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A0600095B:
	movem.l	d2-d4,-(a7)
	move.l	a1,d2
	move.l	a0,d3
	move.l	d0,d4
C68K_method_003ACopperStart_002EDos_003A0600095B_003ABB0000:
	tst.l	d4
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600095B_003ABB0004
C68K_method_003ACopperStart_002EDos_003A0600095B_003ABB0001:
	tst.l	d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600095B_003ABB0004
C68K_method_003ACopperStart_002EDos_003A0600095B_003ABB0002:
	move.l	d2,d0
	add.l	d1,d0
	cmp.l	d0,d3
	bcc.s	C68K_method_003ACopperStart_002EDos_003A0600095B_003ABB0004
C68K_method_003ACopperStart_002EDos_003A0600095B_003ABB0003:
	add.l	d4,d3
	cmp.l	d3,d2
	scs	d0
	extb.l	d0
	neg.l	d0
	movem.l	(a7)+,d2-d4
	rts
C68K_method_003ACopperStart_002EDos_003A0600095B_003ABB0004:
	moveq	#0,d0
	movem.l	(a7)+,d2-d4
	rts
C68K_method_003ACopperStart_002EDos_003A06000012:
C68K_method_003ACopperStart_002EDos_003A06000012_003ABB0000:
C68K_method_003ACopperStart_002EDos_003A0600095B_003Aend:
	move.l	d1,d0
	adda.l	d0,a0
	move.b	(a0),d0
	andi.l	#$000000FF,d0
	andi.l	#$000000FF,d0
	rts
C68K_method_003ACopperStart_002EDos_003A06000012_003Aend:
C68K_method_003ACopperStart_002EDos_003A0600093C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	d2-d3/a2-a3,-(a7)
	lea	-40(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	move.l	a1,36(a7)
	movea.l	a0,a3
	move.l	d1,d2
	move.l	d0,d3
	movea.l	60(a7),a2
C68K_method_003ACopperStart_002EDos_003A0600093C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	movea.l	a2,a0
	moveq	#0,d0
	moveq	#8,d1
C68K_generated_003Aallocated_aggregate_zero_loop_003A8:
	move.l	d0,(a0)+
	dbra	d1,C68K_generated_003Aallocated_aggregate_zero_loop_003A8
	move.l	36(a7),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600093C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007
C68K_method_003ACopperStart_002EDos_003A0600093C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	tst.l	d3
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600093C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007
C68K_method_003ACopperStart_002EDos_003A0600093C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	tst.l	d2
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600093C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007
C68K_method_003ACopperStart_002EDos_003A0600093C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	cmp.l	d3,d2
	bhi.s	C68K_method_003ACopperStart_002EDos_003A0600093C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007
C68K_method_003ACopperStart_002EDos_003A0600093C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	move.l	d3,d0
	move.l	d2,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000947
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600093C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007
C68K_method_003ACopperStart_002EDos_003A0600093C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	move.l	36(a7),d1
	moveq	#-1,d0
	sub.l	d3,d0
	cmp.l	d0,d1
	bhi.s	C68K_method_003ACopperStart_002EDos_003A0600093C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007
C68K_method_003ACopperStart_002EDos_003A0600093C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	movea.l	36(a7),a0
	move.l	d3,d1
	move.l	a3,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001A
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600093C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008
C68K_method_003ACopperStart_002EDos_003A0600093C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
	moveq	#0,d0
	lea	40(a7),a7
	movem.l	(a7)+,d2-d3/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A0600093C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
	lea	(a7),a0
	movea.l	a0,a1
	moveq	#0,d0
	moveq	#8,d1
C68K_generated_003Aallocated_aggregate_zero_loop_003A9:
	move.l	d0,(a1)+
	dbra	d1,C68K_generated_003Aallocated_aggregate_zero_loop_003A9
	lea	(a7),a1
	movea.l	36(a7),a0
	move.l	a0,(a1)
	lea	(a7),a0
	move.l	d3,4(a0)
	lea	(a7),a0
	move.l	d2,8(a0)
	lea	(a7),a0
	moveq	#1,d0
	move.l	d0,28(a0)
	lea	(a7),a0
	moveq	#1,d0
	move.l	d0,32(a0)
	lea	(a7),a0
	move.l	(a0),d0
	move.l	d0,(a2)
	move.l	4(a0),d0
	move.l	d0,4(a2)
	move.l	8(a0),d0
	move.l	d0,8(a2)
	move.l	12(a0),d0
	move.l	d0,12(a2)
	move.l	16(a0),d0
	move.l	d0,16(a2)
	move.l	20(a0),d0
	move.l	d0,20(a2)
	move.l	24(a0),d0
	move.l	d0,24(a2)
	move.l	28(a0),d0
	move.l	d0,28(a2)
	move.l	32(a0),d0
	move.l	d0,32(a2)
	moveq	#1,d0
	lea	40(a7),a7
	movem.l	(a7)+,d2-d3/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A0600093C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000949:
	move.l	a0,d1
C68K_method_003ACopperStart_002EDos_003A06000949_003ABB0000:
	moveq	#72,d0
	add.l	d0,d1
	movea.l	d1,a0
	rts
C68K_method_003ACopperStart_002EDos_003A06000019:
C68K_method_003ACopperStart_002EDos_003A06000949_003Aend:
	movem.l	d2-d3/a2,-(a7)
	move.l	d1,d2
	movea.l	a0,a2
C68K_method_003ACopperStart_002EDos_003A06000019_003ABB0000:
	moveq	#0,d3
C68K_method_003ACopperStart_002EDos_003A06000019_003ABB0002:
	cmp.l	d2,d3
	bcc.s	C68K_method_003ACopperStart_002EDos_003A06000019_003ABB0003
C68K_method_003ACopperStart_002EDos_003A06000019_003ABB0001:
C68K_method_003ACopperStart_002EDos_003A06000019_003ABB0004:
	movea.l	a2,a0
	move.l	d3,d0
	adda.l	d0,a0
	move.b	(a0),d0
	andi.l	#$000000FF,d0
	move.b	d0,d1
	movea.l	a1,a0
	move.l	d3,d0
	andi.l	#$000000FF,d1
	adda.l	d0,a0
	move.b	d1,(a0)
	addq.l	#1,d3
	bra.s	C68K_method_003ACopperStart_002EDos_003A06000019_003ABB0002
C68K_method_003ACopperStart_002EDos_003A06000019_003ABB0003:
	movem.l	(a7)+,d2-d3/a2
	rts
C68K_method_003ACopperStart_002EDos_003A06000015:
C68K_method_003ACopperStart_002EDos_003A06000019_003Aend:
	move.l	d2,-(a7)
	move.l	a1,d2
	move.l	d1,d0
C68K_method_003ACopperStart_002EDos_003A06000015_003ABB0000:
	move.b	d2,d1
	andi.l	#$000000FF,d1
	adda.l	d0,a0
	move.b	d1,(a0)
	move.l	(a7)+,d2
	rts
C68K_method_003ACopperStart_002EDos_003A06000015_003Aend:
C68K_method_003ACopperStart_002EDos_003A0600099A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	d2-d4/a2-a3,-(a7)
	movea.l	d1,a3
	movea.l	a1,a2
	move.l	24(a7),d1
	move.l	d0,d3
C68K_method_003ACopperStart_002EDos_003A0600099A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	cmp.l	d1,d3
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600099A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002
C68K_method_003ACopperStart_002EDos_003A0600099A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	moveq	#0,d0
	movem.l	(a7)+,d2-d4/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A0600099A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
C68K_method_003ACopperStart_002EDos_003A0600099A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
	movea.l	a2,a0
	move.l	d2,d0
	adda.l	d0,a0
	move.b	(a0),d0
	andi.l	#$000000FF,d0
	andi.l	#$000000FF,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600099B
	move.b	d0,d4
	movea.l	a3,a0
	move.l	d2,d0
	adda.l	d0,a0
	move.b	(a0),d0
	andi.l	#$000000FF,d0
	andi.l	#$000000FF,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600099B
	cmp.b	d0,d4
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600099A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A0600099A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
C68K_method_003ACopperStart_002EDos_003A0600099A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009:
	addq.l	#1,d2
C68K_method_003ACopperStart_002EDos_003A0600099A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	cmp.l	d3,d2
	bcs.s	C68K_method_003ACopperStart_002EDos_003A0600099A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A0600099A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
	moveq	#1,d0
	movem.l	(a7)+,d2-d4/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A0600099A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	moveq	#0,d2
	bra.s	C68K_method_003ACopperStart_002EDos_003A0600099A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006
C68K_method_003ACopperStart_002EDos_003A0600099A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	moveq	#0,d0
	movem.l	(a7)+,d2-d4/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A0600094D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A0600099A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	movem.l	a2-a4,-(a7)
	lea	-108(a7),a7
	movea.l	a1,a2
	movea.l	a0,a3
C68K_method_003ACopperStart_002EDos_003A0600094D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	lea	(a7),a0
	movea.l	a0,a1
	moveq	#0,d0
	moveq	#17,d1
C68K_generated_003Aallocated_aggregate_zero_loop_003A10:
	move.l	d0,(a1)+
	dbra	d1,C68K_generated_003Aallocated_aggregate_zero_loop_003A10
	lea	(a7),a1
	movea.l	a2,a0
	moveq	#0,d0
	move.l	(a0),d0
	move.l	d0,(a1)
	lea	(a7),a1
	movea.l	a2,a0
	moveq	#4,d0
	move.l	4(a0),d0
	move.l	d0,4(a1)
	lea	(a7),a1
	movea.l	a2,a0
	moveq	#8,d0
	move.l	8(a0),d0
	move.l	d0,8(a1)
	lea	(a7),a1
	movea.l	a2,a0
	moveq	#12,d0
	move.l	12(a0),d0
	move.l	d0,12(a1)
	lea	(a7),a1
	movea.l	a2,a0
	moveq	#16,d0
	move.l	16(a0),d0
	move.l	d0,16(a1)
	lea	(a7),a1
	movea.l	a2,a0
	moveq	#20,d0
	move.l	20(a0),d0
	move.l	d0,20(a1)
	lea	(a7),a4
	lea	72(a7),a0
	move.l	a0,-(a7)
	movea.l	a3,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600094F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	addq.l	#4,a7
	lea	72(a7),a0
	move.l	(a0),d0
	move.l	d0,24(a4)
	move.l	4(a0),d0
	move.l	d0,28(a4)
	move.l	8(a0),d0
	move.l	d0,32(a4)
	move.l	12(a0),d0
	move.l	d0,36(a4)
	move.l	16(a0),d0
	move.l	d0,40(a4)
	move.l	20(a0),d0
	move.l	d0,44(a4)
	move.l	24(a0),d0
	move.l	d0,48(a4)
	move.l	28(a0),d0
	move.l	d0,52(a4)
	move.l	32(a0),d0
	move.l	d0,56(a4)
	lea	(a7),a1
	movea.l	a2,a0
	moveq	#60,d0
	move.l	60(a0),d0
	move.l	d0,60(a1)
	lea	(a7),a1
	movea.l	a2,a0
	moveq	#64,d0
	move.l	64(a0),d0
	move.l	d0,64(a1)
	lea	(a7),a1
	movea.l	a2,a0
	moveq	#68,d0
	move.l	68(a0),d0
	move.l	d0,68(a1)
	lea	(a7),a0
	movea.l	124(a7),a1
	move.l	(a0),d0
	move.l	d0,(a1)
	move.l	4(a0),d0
	move.l	d0,4(a1)
	move.l	8(a0),d0
	move.l	d0,8(a1)
	move.l	12(a0),d0
	move.l	d0,12(a1)
	move.l	16(a0),d0
	move.l	d0,16(a1)
	move.l	20(a0),d0
	move.l	d0,20(a1)
	move.l	24(a0),d0
	move.l	d0,24(a1)
	move.l	28(a0),d0
	move.l	d0,28(a1)
	move.l	32(a0),d0
	move.l	d0,32(a1)
	move.l	36(a0),d0
	move.l	d0,36(a1)
	move.l	40(a0),d0
	move.l	d0,40(a1)
	move.l	44(a0),d0
	move.l	d0,44(a1)
	move.l	48(a0),d0
	move.l	d0,48(a1)
	move.l	52(a0),d0
	move.l	d0,52(a1)
	move.l	56(a0),d0
	move.l	d0,56(a1)
	move.l	60(a0),d0
	move.l	d0,60(a1)
	move.l	64(a0),d0
	move.l	d0,64(a1)
	move.l	68(a0),d0
	move.l	d0,68(a1)
	lea	108(a7),a7
	movem.l	(a7)+,a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A0600094D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	movem.l	d2-d3/a2-a4,-(a7)
	subq.l	#4,a7
	move.l	a1,(a7)
	movea.l	a0,a3
	movea.l	d0,a2
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	(a7),d0
	tst.l	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	move.l	(a7),d1
	moveq	#3,d0
	and.l	d0,d1
	tst.l	d1
	bne.w	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	move.l	(a2),d1
	move.l	#$44514348,d0
	cmp.l	d0,d1
	bne.w	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	move.l	4(a2),d1
	moveq	#2,d0
	cmp.l	d0,d1
	bne.w	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	move.l	16(a2),d0
	tst.l	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	move.l	16(a2),d1
	move.l	#$000000FF,d0
	cmp.l	d0,d1
	bhi.w	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	move.l	20(a2),d1
	moveq	#-4,d0
	and.l	d0,d1
	tst.l	d1
	bne.w	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
	move.l	60(a2),d0
	tst.l	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
	move.l	12(a2),d2
	moveq	#72,d1
	move.l	16(a2),d0
	add.l	d0,d1
	addq.l	#1,d1
	cmp.l	d1,d2
	bcs.w	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009:
	move.l	(a7),d2
	moveq	#-1,d1
	move.l	12(a2),d0
	sub.l	d0,d1
	cmp.l	d1,d2
	bhi.w	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A:
	movea.l	(a7),a0
	move.l	12(a2),d1
	move.l	a3,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001A
	tst.b	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B:
	lea	24(a2),a1
	movea.l	a3,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000941_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C:
	lea	64(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D:
	move.l	64(a2),d0
	movea.l	a3,a0
	movea.l	d0,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000986_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E:
	lea	68(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F:
	move.l	68(a2),d0
	movea.l	a3,a0
	movea.l	d0,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000986_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010:
	lea	64(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0013
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0011:
	lea	68(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0013
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0012:
	move.l	64(a2),d3
	move.l	68(a2),d2
	movea.l	d3,a0
	moveq	#48,d0
	movea.l	d2,a1
	moveq	#48,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600095B
	tst.b	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0013:
	lea	64(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0015
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0014:
	movea.l	(a7),a0
	move.l	12(a2),d0
	move.l	64(a2),d2
	movea.l	d2,a1
	moveq	#48,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600095B
	tst.b	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0015:
	lea	68(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0017
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0016:
	movea.l	(a7),a0
	move.l	12(a2),d0
	move.l	68(a2),d2
	movea.l	d2,a1
	moveq	#48,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600095B
	tst.b	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0017:
	lea	64(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0019
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0018:
	lea	24(a2),a0
	move.l	(a0),d3
	lea	24(a2),a0
	move.l	4(a0),d0
	move.l	64(a2),d2
	movea.l	d3,a0
	movea.l	d2,a1
	moveq	#48,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600095B
	tst.b	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0019:
	lea	68(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001B
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001A:
	lea	24(a2),a0
	move.l	(a0),d3
	lea	24(a2),a0
	move.l	4(a0),d0
	move.l	68(a2),d2
	movea.l	d3,a0
	movea.l	d2,a1
	moveq	#48,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600095B
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001B:
	lea	64(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001D
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001C:
	move.l	20(a2),d1
	moveq	#1,d0
	and.l	d0,d1
	tst.l	d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001D:
	lea	68(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001F
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001E:
	move.l	20(a2),d1
	moveq	#2,d0
	and.l	d0,d1
	tst.l	d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001F:
	move.l	20(a2),d1
	moveq	#1,d0
	and.l	d0,d1
	tst.l	d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0021
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0020:
	lea	24(a2),a0
	move.l	28(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0021:
	move.l	20(a2),d1
	moveq	#2,d0
	and.l	d0,d1
	tst.l	d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0024
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0022:
	lea	24(a2),a0
	move.l	32(a0),d0
	tst.l	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0024
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0023:
	moveq	#0,d0
	addq.l	#4,a7
	movem.l	(a7)+,d2-d3/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0029:
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0031:
	movea.l	a4,a0
	move.l	d1,d0
	adda.l	d0,a0
	move.b	(a0),d0
	andi.l	#$000000FF,d0
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB002A
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB002B:
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0032:
	addq.l	#1,d1
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB002C:
	move.l	16(a2),d0
	cmp.l	d0,d1
	bcs.s	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0029
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB002D:
	moveq	#1,d0
	addq.l	#4,a7
	movem.l	(a7)+,d2-d3/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0024:
	movea.l	(a7),a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000949
	move.l	a0,d0
	movea.l	d0,a4
	move.l	a4,d0
	move.l	d0,d2
	moveq	#-1,d1
	move.l	16(a2),d0
	sub.l	d0,d1
	subq.l	#1,d1
	cmp.l	d1,d2
	bhi.s	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0027
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0025:
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB002E:
	move.l	16(a2),d1
	addq.l	#1,d1
	move.l	a3,d0
	movea.l	a4,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001A
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0027
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0026:
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB002F:
	move.l	16(a2),d0
	movea.l	a4,a0
	adda.l	d0,a0
	move.b	(a0),d0
	andi.l	#$000000FF,d0
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0028
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0027:
	moveq	#0,d0
	addq.l	#4,a7
	movem.l	(a7)+,d2-d3/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0028:
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0030:
	moveq	#0,d1
	bra.s	C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB002C
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB002A:
	moveq	#0,d0
	addq.l	#4,a7
	movem.l	(a7)+,d2-d3/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A06000017:
C68K_method_003ACopperStart_002EDos_003A0600094C_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	move.l	d2,-(a7)
	move.l	a1,d2
	move.l	d1,d0
C68K_method_003ACopperStart_002EDos_003A06000017_003ABB0000:
	move.l	d2,d1
	adda.l	d0,a0
	move.l	d1,(a0)
	move.l	(a7)+,d2
	rts
C68K_method_003ACopperStart_002EDos_003A06000017_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000950_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	pea	(a2)
	movea.l	d0,a2
C68K_method_003ACopperStart_002EDos_003A06000950_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	lea	(a2),a0
	move.l	(a0),d0
	move.l	d0,d1
	movea.l	a1,a0
	moveq	#24,d0
	move.l	d1,24(a0)
	move.l	4(a2),d1
	movea.l	a1,a0
	moveq	#28,d0
	move.l	d1,28(a0)
	move.l	8(a2),d1
	movea.l	a1,a0
	moveq	#32,d0
	move.l	d1,32(a0)
	move.l	12(a2),d1
	movea.l	a1,a0
	moveq	#36,d0
	move.l	d1,36(a0)
	move.l	16(a2),d1
	movea.l	a1,a0
	moveq	#40,d0
	move.l	d1,40(a0)
	move.l	20(a2),d1
	movea.l	a1,a0
	moveq	#44,d0
	move.l	d1,44(a0)
	move.l	24(a2),d1
	movea.l	a1,a0
	moveq	#48,d0
	move.l	d1,48(a0)
	move.l	28(a2),d1
	movea.l	a1,a0
	moveq	#52,d0
	move.l	d1,52(a0)
	move.l	32(a2),d1
	movea.l	a1,a0
	moveq	#56,d0
	move.l	d1,56(a0)
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A06000950_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	movem.l	d2-d3/a2-a3,-(a7)
	movea.l	a0,a2
	movea.l	a1,a3
C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	lea	(a3),a0
	move.l	(a0),d0
	tst.l	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F
C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	move.l	4(a3),d0
	tst.l	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F
C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	move.l	8(a3),d0
	tst.l	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F
C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	move.l	8(a3),d1
	move.l	4(a3),d0
	cmp.l	d0,d1
	bhi.w	C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F
C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	move.l	4(a3),d0
	move.l	8(a3),d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000947
	tst.b	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F
C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	lea	(a3),a0
	move.l	(a0),d0
	move.l	d0,d2
	moveq	#-1,d1
	move.l	4(a3),d0
	sub.l	d0,d1
	cmp.l	d1,d2
	bhi.s	C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F
C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	move.l	(a3),d2
	move.l	4(a3),d1
	move.l	a2,d0
	movea.l	d2,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001A
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F
C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
	move.l	12(a3),d1
	move.l	4(a3),d0
	cmp.l	d0,d1
	bcc.s	C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F
C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
	move.l	16(a3),d1
	move.l	4(a3),d0
	cmp.l	d0,d1
	bcc.s	C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F
C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009:
	move.l	24(a3),d1
	move.l	8(a3),d0
	cmp.l	d0,d1
	bcc.s	C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F
C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A:
	move.l	20(a3),d2
	move.l	4(a3),d1
	move.l	24(a3),d0
	sub.l	d0,d1
	cmp.l	d1,d2
	bhi.s	C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F
C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B:
	move.l	28(a3),d1
	moveq	#1,d0
	cmp.l	d0,d1
	bhi.s	C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F
C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C:
	move.l	32(a3),d1
	moveq	#1,d0
	cmp.l	d0,d1
	bhi.s	C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F
C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D:
	move.l	32(a3),d0
	tst.l	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010
C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E:
	move.l	24(a3),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010
C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F:
	moveq	#0,d0
	movem.l	(a7)+,d2-d3/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010:
	move.l	20(a3),d1
	move.l	24(a3),d0
	add.l	d0,d1
	move.l	16(a3),d3
	move.l	12(a3),d0
	move.l	4(a3),d2
	movea.l	d2,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000945
	cmp.l	d0,d3
	seq	d0
	extb.l	d0
	neg.l	d0
	movem.l	(a7)+,d2-d3/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000970_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	a2-a5,-(a7)
	lea	-152(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	clr.l	64(a7)
	clr.l	68(a7)
	clr.l	72(a7)
	clr.l	76(a7)
	clr.l	80(a7)
	clr.l	84(a7)
	clr.l	88(a7)
	clr.l	92(a7)
	clr.l	96(a7)
	clr.l	100(a7)
	clr.l	104(a7)
	clr.l	108(a7)
	clr.l	112(a7)
	clr.l	116(a7)
	clr.l	120(a7)
	clr.l	124(a7)
	clr.l	128(a7)
	clr.l	132(a7)
	clr.l	136(a7)
	clr.l	140(a7)
	clr.l	144(a7)
	clr.l	148(a7)
	movea.l	a1,a5
	movea.l	a0,a3
	movea.l	d0,a2
C68K_method_003ACopperStart_002EDos_003A06000970_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	movea.l	a2,a0
	moveq	#0,d0
	moveq	#7,d1
C68K_generated_003Aallocated_aggregate_zero_loop_003A11:
	move.l	d0,(a0)+
	dbra	d1,C68K_generated_003Aallocated_aggregate_zero_loop_003A11
	move.l	a7,d0
	movea.l	a3,a0
	movea.l	a5,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000987_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000970_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002
C68K_method_003ACopperStart_002EDos_003A06000970_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	lea	(a7),a0
	move.l	12(a0),d1
	move.l	a7,d0
	addi.l	#$00000030,d0
	movea.l	a3,a0
	movea.l	d1,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000952_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000970_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A06000970_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	moveq	#0,d0
	lea	152(a7),a7
	movem.l	(a7)+,a2-a5
	rts
C68K_method_003ACopperStart_002EDos_003A06000970_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	lea	(a7),a0
	move.l	32(a0),d1
	moveq	#82,d0
	cmp.l	d0,d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000970_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005
C68K_method_003ACopperStart_002EDos_003A06000970_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	lea	48(a7),a0
	move.l	68(a0),d1
C68K_method_003ACopperStart_002EDos_003A06000970_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	move.l	a5,d0
	cmp.l	d0,d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000970_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008
C68K_method_003ACopperStart_002EDos_003A06000970_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
	moveq	#0,d0
	lea	152(a7),a7
	movem.l	(a7)+,a2-a5
	rts
C68K_method_003ACopperStart_002EDos_003A06000970_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	lea	48(a7),a0
	move.l	64(a0),d1
	bra.s	C68K_method_003ACopperStart_002EDos_003A06000970_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006
C68K_method_003ACopperStart_002EDos_003A06000970_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
	lea	120(a7),a0
	movea.l	a0,a1
	moveq	#0,d0
	moveq	#7,d1
C68K_generated_003Aallocated_aggregate_zero_loop_003A12:
	move.l	d0,(a1)+
	dbra	d1,C68K_generated_003Aallocated_aggregate_zero_loop_003A12
	lea	120(a7),a1
	lea	(a7),a0
	move.l	16(a0),d0
	move.l	d0,(a1)
	lea	120(a7),a4
	lea	(a7),a0
	move.l	16(a0),d0
	movea.l	d0,a0
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000089
	move.l	a0,d0
	move.l	d0,4(a4)
	lea	120(a7),a1
	lea	(a7),a0
	move.l	20(a0),d0
	move.l	d0,8(a1)
	lea	120(a7),a1
	lea	(a7),a0
	move.l	24(a0),d0
	move.l	d0,12(a1)
	lea	120(a7),a1
	lea	(a7),a0
	move.l	28(a0),d0
	move.l	d0,16(a1)
	lea	120(a7),a1
	lea	(a7),a0
	move.l	32(a0),d0
	move.l	d0,20(a1)
	lea	120(a7),a1
	lea	(a7),a0
	move.l	36(a0),d0
	move.l	d0,24(a1)
	lea	120(a7),a1
	lea	(a7),a0
	move.l	40(a0),d0
	move.l	d0,28(a1)
	lea	120(a7),a0
	move.l	(a0),d0
	move.l	d0,(a2)
	move.l	4(a0),d0
	move.l	d0,4(a2)
	move.l	8(a0),d0
	move.l	d0,8(a2)
	move.l	12(a0),d0
	move.l	d0,12(a2)
	move.l	16(a0),d0
	move.l	d0,16(a2)
	move.l	20(a0),d0
	move.l	d0,20(a2)
	move.l	24(a0),d0
	move.l	d0,24(a2)
	move.l	28(a0),d0
	move.l	d0,28(a2)
	movea.l	a3,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000970_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A
C68K_method_003ACopperStart_002EDos_003A06000970_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009:
	lea	(a7),a0
	move.l	12(a0),d1
	lea	48(a7),a0
	move.l	a0,-(a7)
	movea.l	a3,a0
	movea.l	a5,a1
	move.l	a2,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	addq.l	#4,a7
	andi.l	#$000000FF,d0
	lea	152(a7),a7
	movem.l	(a7)+,a2-a5
	rts
C68K_method_003ACopperStart_002EDos_003A06000970_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A:
	moveq	#0,d0
	lea	152(a7),a7
	movem.l	(a7)+,a2-a5
	rts
C68K_method_003ACopperStart_002EDos_003A06000970_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000971_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	d2/a2-a4,-(a7)
	movea.l	a1,a3
	movea.l	a0,a4
	movea.l	d0,a2
C68K_method_003ACopperStart_002EDos_003A06000971_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	movea.l	a2,a0
	moveq	#0,d0
	moveq	#7,d1
C68K_generated_003Aallocated_aggregate_zero_loop_003A13:
	move.l	d0,(a0)+
	dbra	d1,C68K_generated_003Aallocated_aggregate_zero_loop_003A13
	movea.l	a4,a0
	movea.l	a3,a1
	move.l	a2,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000970_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000971_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002
C68K_method_003ACopperStart_002EDos_003A06000971_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	moveq	#0,d0
	movem.l	(a7)+,d2/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A06000971_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	movea.l	a4,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	move.l	d0,d2
	tst.l	d2
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000971_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005
C68K_method_003ACopperStart_002EDos_003A06000971_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	movea.l	a4,a0
	movea.l	a3,a1
	move.l	a2,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000973_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000971_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005
C68K_method_003ACopperStart_002EDos_003A06000971_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	moveq	#0,d0
	movem.l	(a7)+,d2/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A06000971_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	move.l	d2,d0
	movem.l	(a7)+,d2/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A06000971_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	a2-a4,-(a7)
	lea	-120(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	clr.l	64(a7)
	clr.l	68(a7)
	clr.l	72(a7)
	clr.l	76(a7)
	clr.l	80(a7)
	clr.l	84(a7)
	clr.l	88(a7)
	clr.l	92(a7)
	clr.l	96(a7)
	clr.l	100(a7)
	clr.l	104(a7)
	clr.l	108(a7)
	clr.l	112(a7)
	clr.l	116(a7)
	movea.l	a1,a4
	movea.l	a0,a3
	movea.l	d0,a2
C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	movea.l	a2,a0
	moveq	#0,d0
	moveq	#7,d1
C68K_generated_003Aallocated_aggregate_zero_loop_003A14:
	move.l	d0,(a0)+
	dbra	d1,C68K_generated_003Aallocated_aggregate_zero_loop_003A14
	movea.l	a3,a0
	movea.l	a4,a1
	move.l	a2,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000970_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	move.l	a7,d0
	movea.l	a3,a0
	movea.l	a4,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000987_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	lea	(a7),a0
	move.l	12(a0),d1
	move.l	a7,d0
	addi.l	#$00000030,d0
	movea.l	a3,a0
	movea.l	d1,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000952_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	moveq	#0,d0
	lea	120(a7),a7
	movem.l	(a7)+,a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	move.l	20(a2),d1
	moveq	#82,d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008
C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	lea	48(a7),a0
	move.l	64(a0),d1
	move.l	a4,d0
	cmp.l	d0,d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007
C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	moveq	#0,d0
	lea	120(a7),a7
	movem.l	(a7)+,a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
	lea	48(a7),a0
	moveq	#0,d0
	clr.l	64(a0)
C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B:
	lea	(a7),a0
	move.l	12(a0),d1
	move.l	a7,d0
	addi.l	#$00000030,d0
	movea.l	a3,a0
	movea.l	d1,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600094B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C:
	moveq	#0,d0
	lea	120(a7),a7
	movem.l	(a7)+,a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
	lea	48(a7),a0
	move.l	68(a0),d1
	move.l	a4,d0
	cmp.l	d0,d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A
C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009:
	moveq	#0,d0
	lea	120(a7),a7
	movem.l	(a7)+,a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A:
	lea	48(a7),a0
	moveq	#0,d0
	clr.l	68(a0)
	bra.s	C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B
C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D:
	move.l	a3,d0
	movea.l	a4,a0
	moveq	#48,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000018
	moveq	#1,d0
	lea	120(a7),a7
	movem.l	(a7)+,a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A06000972_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000983_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	move.l	d2,-(a7)
	move.l	d3,-(a7)
C68K_method_003ACopperStart_002EDos_003A06000983_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	28(a1),d2
	move.l	28(a1),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000983_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002
C68K_method_003ACopperStart_002EDos_003A06000983_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	moveq	#0,d3
C68K_method_003ACopperStart_002EDos_003A06000983_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	move.l	4(a1),d1
	move.l	28(a1),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000983_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005
C68K_method_003ACopperStart_002EDos_003A06000983_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
C68K_method_003ACopperStart_002EDos_003A06000983_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	movea.l	d1,a1
	move.l	d2,d0
	move.l	d3,d1
	move.l	(a7)+,d3
	move.l	(a7)+,d2
	bra.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000084_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
C68K_method_003ACopperStart_002EDos_003A06000983_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	move.l	#$000000DF,d3
	bra.s	C68K_method_003ACopperStart_002EDos_003A06000983_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A06000983_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	moveq	#-1,d2
	bra.s	C68K_method_003ACopperStart_002EDos_003A06000983_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006
C68K_method_003ACopperStart_002EDos_003A06000031:
C68K_method_003ACopperStart_002EDos_003A06000983_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	pea	(a6)
	subq.l	#4,a7
	move.l	a1,(a7)
C68K_method_003ACopperStart_002EDos_003A06000031_003ABB0000:
	move.l	(a7),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000031_003ABB0002
C68K_method_003ACopperStart_002EDos_003A06000031_003ABB0001:
	move.l	(a7),d0
	movea.l	d0,a1
	movea.l	_ExecBase,a6
	jsr	-378(a6)
C68K_method_003ACopperStart_002EDos_003A06000031_003ABB0002:
	addq.l	#4,a7
	movea.l	(a7)+,a6
	rts
C68K_method_003ACopperStart_002EDos_003A06000031_003Aend:
C68K_method_003ACopperStart_002EDos_003A0600095E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	d2/a2-a4,-(a7)
	lea	-288(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	clr.l	64(a7)
	clr.l	68(a7)
	movea.l	a1,a4
	movea.l	d0,a3
	movea.l	a0,a2
C68K_method_003ACopperStart_002EDos_003A0600095E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	a7,d0
	movea.l	a2,a0
	movea.l	a3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000952_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A0600095E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009
C68K_method_003ACopperStart_002EDos_003A0600095E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	lea	(a7),a0
	move.l	20(a0),d0
	tst.l	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A0600095E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009
C68K_method_003ACopperStart_002EDos_003A0600095E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	lea	(a7),a0
	lea	64(a0),a0
	move.l	(a0),d0
	tst.l	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A0600095E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009
C68K_method_003ACopperStart_002EDos_003A0600095E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	lea	(a7),a0
	lea	68(a0),a0
	move.l	(a0),d0
	tst.l	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A0600095E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009
C68K_method_003ACopperStart_002EDos_003A0600095E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	lea	(a7),a0
	move.l	24(a0),d0
	move.l	d0,72(a7)
	move.l	28(a0),d0
	move.l	d0,76(a7)
	move.l	32(a0),d0
	move.l	d0,80(a7)
	move.l	36(a0),d0
	move.l	d0,84(a7)
	move.l	40(a0),d0
	move.l	d0,88(a7)
	move.l	44(a0),d0
	move.l	d0,92(a7)
	move.l	48(a0),d0
	move.l	d0,96(a7)
	move.l	52(a0),d0
	move.l	d0,100(a7)
	move.l	56(a0),d0
	move.l	d0,104(a7)
	lea	72(a7),a0
	move.l	28(a0),d0
	tst.l	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A0600095E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009
C68K_method_003ACopperStart_002EDos_003A0600095E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	lea	(a7),a0
	move.l	24(a0),d0
	move.l	d0,108(a7)
	move.l	28(a0),d0
	move.l	d0,112(a7)
	move.l	32(a0),d0
	move.l	d0,116(a7)
	move.l	36(a0),d0
	move.l	d0,120(a7)
	move.l	40(a0),d0
	move.l	d0,124(a7)
	move.l	44(a0),d0
	move.l	d0,128(a7)
	move.l	48(a0),d0
	move.l	d0,132(a7)
	move.l	52(a0),d0
	move.l	d0,136(a7)
	move.l	56(a0),d0
	move.l	d0,140(a7)
	lea	108(a7),a0
	move.l	32(a0),d0
	tst.l	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600095E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009
C68K_method_003ACopperStart_002EDos_003A0600095E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	movea.l	a2,a0
	movea.l	a3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000959_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600095E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009
C68K_method_003ACopperStart_002EDos_003A0600095E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
	move.l	a7,d0
	movea.l	a2,a0
	movea.l	a3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000952_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600095E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009
C68K_method_003ACopperStart_002EDos_003A0600095E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
	movea.l	a2,a0
	movea.l	a4,a1
	move.l	a3,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600095E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A
C68K_method_003ACopperStart_002EDos_003A0600095E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009:
	moveq	#0,d0
	lea	288(a7),a7
	movem.l	(a7)+,d2/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600095E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A:
	lea	(a7),a0
	move.l	24(a0),d0
	move.l	d0,144(a7)
	move.l	28(a0),d0
	move.l	d0,148(a7)
	move.l	32(a0),d0
	move.l	d0,152(a7)
	move.l	36(a0),d0
	move.l	d0,156(a7)
	move.l	40(a0),d0
	move.l	d0,160(a7)
	move.l	44(a0),d0
	move.l	d0,164(a7)
	move.l	48(a0),d0
	move.l	d0,168(a7)
	move.l	52(a0),d0
	move.l	d0,172(a7)
	move.l	56(a0),d0
	move.l	d0,176(a7)
	lea	144(a7),a0
	move.l	(a0),d2
	lea	(a7),a0
	move.l	24(a0),d0
	move.l	d0,180(a7)
	move.l	28(a0),d0
	move.l	d0,184(a7)
	move.l	32(a0),d0
	move.l	d0,188(a7)
	move.l	36(a0),d0
	move.l	d0,192(a7)
	move.l	40(a0),d0
	move.l	d0,196(a7)
	move.l	44(a0),d0
	move.l	d0,200(a7)
	move.l	48(a0),d0
	move.l	d0,204(a7)
	move.l	52(a0),d0
	move.l	d0,208(a7)
	move.l	56(a0),d0
	move.l	d0,212(a7)
	lea	180(a7),a0
	move.l	4(a0),d1
	move.l	a2,d0
	movea.l	d2,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000018
	lea	(a7),a0
	move.l	24(a0),d0
	move.l	d0,216(a7)
	move.l	28(a0),d0
	move.l	d0,220(a7)
	move.l	32(a0),d0
	move.l	d0,224(a7)
	move.l	36(a0),d0
	move.l	d0,228(a7)
	move.l	40(a0),d0
	move.l	d0,232(a7)
	move.l	44(a0),d0
	move.l	d0,236(a7)
	move.l	48(a0),d0
	move.l	d0,240(a7)
	move.l	52(a0),d0
	move.l	d0,244(a7)
	move.l	56(a0),d0
	move.l	d0,248(a7)
	lea	216(a7),a0
	move.l	(a0),d2
	lea	(a7),a0
	move.l	24(a0),d0
	move.l	d0,252(a7)
	move.l	28(a0),d0
	move.l	d0,256(a7)
	move.l	32(a0),d0
	move.l	d0,260(a7)
	move.l	36(a0),d0
	move.l	d0,264(a7)
	move.l	40(a0),d0
	move.l	d0,268(a7)
	move.l	44(a0),d0
	move.l	d0,272(a7)
	move.l	48(a0),d0
	move.l	d0,276(a7)
	move.l	52(a0),d0
	move.l	d0,280(a7)
	move.l	56(a0),d0
	move.l	d0,284(a7)
	lea	252(a7),a0
	move.l	4(a0),d1
	move.l	a2,d0
	movea.l	d2,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001E
	lea	(a7),a0
	move.l	12(a0),d1
	move.l	a2,d0
	movea.l	a3,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000018
	lea	(a7),a0
	move.l	12(a0),d1
	move.l	a2,d0
	movea.l	a3,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001E
	moveq	#1,d0
	lea	288(a7),a7
	movem.l	(a7)+,d2/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600095E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000992:
	move.l	d2,-(a7)
C68K_method_003ACopperStart_002EDos_003A06000992_003ABB0000:
	move.l	(a0),d1
	move.l	#$44515247,d0
	cmp.l	d0,d1
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000992_003ABB000B
C68K_method_003ACopperStart_002EDos_003A06000992_003ABB0001:
	move.l	4(a0),d1
	moveq	#1,d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000992_003ABB000B
C68K_method_003ACopperStart_002EDos_003A06000992_003ABB0002:
	move.l	16(a0),d1
	moveq	#0,d0
	cmp.l	d0,d1
	bls.s	C68K_method_003ACopperStart_002EDos_003A06000992_003ABB000B
C68K_method_003ACopperStart_002EDos_003A06000992_003ABB0003:
	move.l	#$00000400,d0
	cmp.l	d0,d1
	bhi.s	C68K_method_003ACopperStart_002EDos_003A06000992_003ABB000B
C68K_method_003ACopperStart_002EDos_003A06000992_003ABB0004:
	move.l	12(a0),d1
	move.l	16(a0),d0
	cmp.l	d0,d1
	bhi.s	C68K_method_003ACopperStart_002EDos_003A06000992_003ABB000B
C68K_method_003ACopperStart_002EDos_003A06000992_003ABB0005:
	move.l	12(a0),d0
	tst.l	d0
	seq	d2
	extb.l	d2
	neg.l	d2
	lea	8(a0),a1
	move.l	(a1),d0
	tst.l	d0
	seq	d0
	extb.l	d0
	neg.l	d0
	andi.l	#$000000FF,d0
	cmp.l	d0,d2
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000992_003ABB000B
C68K_method_003ACopperStart_002EDos_003A06000992_003ABB0006:
	move.l	20(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000992_003ABB000B
C68K_method_003ACopperStart_002EDos_003A06000992_003ABB0007:
	move.l	24(a0),d1
	moveq	#0,d0
	cmp.l	d0,d1
	bls.s	C68K_method_003ACopperStart_002EDos_003A06000992_003ABB000B
C68K_method_003ACopperStart_002EDos_003A06000992_003ABB0008:
	moveq	#64,d0
	cmp.l	d0,d1
	bhi.s	C68K_method_003ACopperStart_002EDos_003A06000992_003ABB000B
C68K_method_003ACopperStart_002EDos_003A06000992_003ABB0009:
	move.l	20(a0),d1
	move.l	#$03FFFFFF,d0
	cmp.l	d0,d1
	bhi.s	C68K_method_003ACopperStart_002EDos_003A06000992_003ABB000B
C68K_method_003ACopperStart_002EDos_003A06000992_003ABB000A:
	move.l	28(a0),d0
	tst.l	d0
	seq	d0
	extb.l	d0
	neg.l	d0
	move.l	(a7)+,d2
	rts
C68K_method_003ACopperStart_002EDos_003A06000992_003ABB000B:
	moveq	#0,d0
	move.l	(a7)+,d2
	rts
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000194_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A06000992_003Aend:
	subq.l	#4,a7
	move.l	a1,(a7)
	movea.l	a0,a1
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000194_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	(a7),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000194_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000194_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	move.l	(a7),d1
	moveq	#-35,d0
	cmp.l	d0,d1
	bhi.s	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000194_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000194_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	movea.l	(a7),a0
	move.l	a1,d0
	moveq	#34,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001A
	andi.l	#$000000FF,d0
	addq.l	#4,a7
	rts
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000194_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	moveq	#0,d0
	addq.l	#4,a7
	rts
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000178_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000194_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	subq.l	#4,a7
	move.l	a1,(a7)
	movea.l	a0,a1
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000178_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	(a7),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000178_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000178_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	move.l	(a7),d1
	moveq	#-93,d0
	cmp.l	d0,d1
	bhi.s	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000178_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000178_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	movea.l	(a7),a0
	move.l	a1,d0
	moveq	#92,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001A
	andi.l	#$000000FF,d0
	addq.l	#4,a7
	rts
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000178_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	moveq	#0,d0
	addq.l	#4,a7
	rts
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000178_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000947:
	movem.l	d2-d5,-(a7)
	move.l	d0,d2
	movea.l	d1,a0
C68K_method_003ACopperStart_002EDos_003A06000947_003ABB0000:
	move.l	a0,d0
	tst.l	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000947_003ABB0002
C68K_method_003ACopperStart_002EDos_003A06000947_003ABB0001:
	moveq	#0,d0
	movem.l	(a7)+,d2-d5
	rts
C68K_method_003ACopperStart_002EDos_003A06000947_003ABB0005:
C68K_method_003ACopperStart_002EDos_003A06000947_003ABB000C:
	move.l	a1,d0
	adda.l	d0,a1
	adda.l	d1,a1
	move.l	a1,d1
	move.l	a0,d0
	cmp.l	d0,d1
	bcs.s	C68K_method_003ACopperStart_002EDos_003A06000947_003ABB0007
C68K_method_003ACopperStart_002EDos_003A06000947_003ABB0006:
C68K_method_003ACopperStart_002EDos_003A06000947_003ABB000D:
	move.l	a0,d0
	suba.l	d0,a1
C68K_method_003ACopperStart_002EDos_003A06000947_003ABB0007:
C68K_method_003ACopperStart_002EDos_003A06000947_003ABB000E:
	move.l	d3,d0
	lsr.l	#1,d0
	move.l	d0,d3
C68K_method_003ACopperStart_002EDos_003A06000947_003ABB0008:
	tst.l	d3
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000947_003ABB0009
C68K_method_003ACopperStart_002EDos_003A06000947_003ABB0003:
C68K_method_003ACopperStart_002EDos_003A06000947_003ABB000A:
	move.l	d2,d0
	and.l	d3,d0
	tst.l	d0
	shi	d1
	extb.l	d1
	neg.l	d1
	move.l	a0,d0
	lsr.l	#1,d0
	move.l	d0,d4
	move.l	a0,d5
	moveq	#1,d0
	and.l	d0,d5
	add.l	d5,d4
	move.l	a1,d0
	cmp.l	d4,d0
	bcs.s	C68K_method_003ACopperStart_002EDos_003A06000947_003ABB0005
C68K_method_003ACopperStart_002EDos_003A06000947_003ABB000B:
	move.l	d1,d4
C68K_method_003ACopperStart_002EDos_003A06000947_003ABB0004:
	move.l	a1,d1
	move.l	a0,d0
	sub.l	d1,d0
	suba.l	d0,a1
	adda.l	d4,a1
	bra.s	C68K_method_003ACopperStart_002EDos_003A06000947_003ABB0007
C68K_method_003ACopperStart_002EDos_003A06000947_003ABB0002:
	movea.l	#$00000000,a1
	move.l	#$80000000,d3
	bra.s	C68K_method_003ACopperStart_002EDos_003A06000947_003ABB0008
C68K_method_003ACopperStart_002EDos_003A06000947_003ABB0009:
C68K_method_003ACopperStart_002EDos_003A06000947_003ABB000F:
	move.l	a1,d0
	tst.l	d0
	seq	d0
	extb.l	d0
	neg.l	d0
	movem.l	(a7)+,d2-d5
	rts
C68K_method_003ACopperStart_002EDos_003A06000947_003Aend:
C68K_method_003ACopperStart_002EDos_003A0600099B:
	move.l	d0,d1
C68K_method_003ACopperStart_002EDos_003A0600099B_003ABB0000:
	moveq	#97,d0
	cmp.b	d0,d1
	blt.s	C68K_method_003ACopperStart_002EDos_003A0600099B_003ABB0002
C68K_method_003ACopperStart_002EDos_003A0600099B_003ABB0001:
	moveq	#122,d0
	cmp.b	d0,d1
	ble.s	C68K_method_003ACopperStart_002EDos_003A0600099B_003ABB0003
C68K_method_003ACopperStart_002EDos_003A0600099B_003ABB0002:
	move.b	d1,d0
	andi.l	#$000000FF,d0
	rts
C68K_method_003ACopperStart_002EDos_003A0600099B_003ABB0003:
	moveq	#32,d0
	sub.b	d0,d1
	move.l	d1,d0
	andi.l	#$000000FF,d0
	rts
C68K_method_003ACopperStart_002EDos_003A0600094F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A0600099B_003Aend:
	pea	(a2)
	lea	-36(a7),a7
	movea.l	a1,a2
C68K_method_003ACopperStart_002EDos_003A0600094F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	lea	(a7),a0
	movea.l	a0,a1
	moveq	#0,d0
	moveq	#8,d1
C68K_generated_003Aallocated_aggregate_zero_loop_003A15:
	move.l	d0,(a1)+
	dbra	d1,C68K_generated_003Aallocated_aggregate_zero_loop_003A15
	lea	(a7),a1
	movea.l	a2,a0
	moveq	#24,d0
	move.l	24(a0),d0
	move.l	d0,(a1)
	lea	(a7),a1
	movea.l	a2,a0
	moveq	#28,d0
	move.l	28(a0),d0
	move.l	d0,4(a1)
	lea	(a7),a1
	movea.l	a2,a0
	moveq	#32,d0
	move.l	32(a0),d0
	move.l	d0,8(a1)
	lea	(a7),a1
	movea.l	a2,a0
	moveq	#36,d0
	move.l	36(a0),d0
	move.l	d0,12(a1)
	lea	(a7),a1
	movea.l	a2,a0
	moveq	#40,d0
	move.l	40(a0),d0
	move.l	d0,16(a1)
	lea	(a7),a1
	movea.l	a2,a0
	moveq	#44,d0
	move.l	44(a0),d0
	move.l	d0,20(a1)
	lea	(a7),a1
	movea.l	a2,a0
	moveq	#48,d0
	move.l	48(a0),d0
	move.l	d0,24(a1)
	lea	(a7),a1
	movea.l	a2,a0
	moveq	#52,d0
	move.l	52(a0),d0
	move.l	d0,28(a1)
	lea	(a7),a1
	movea.l	a2,a0
	moveq	#56,d0
	move.l	56(a0),d0
	move.l	d0,32(a1)
	lea	(a7),a0
	movea.l	44(a7),a1
	move.l	(a0),d0
	move.l	d0,(a1)
	move.l	4(a0),d0
	move.l	d0,4(a1)
	move.l	8(a0),d0
	move.l	d0,8(a1)
	move.l	12(a0),d0
	move.l	d0,12(a1)
	move.l	16(a0),d0
	move.l	d0,16(a1)
	move.l	20(a0),d0
	move.l	d0,20(a1)
	move.l	24(a0),d0
	move.l	d0,24(a1)
	move.l	28(a0),d0
	move.l	d0,28(a1)
	move.l	32(a0),d0
	move.l	d0,32(a1)
	lea	36(a7),a7
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A06000941_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A06000941_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
C68K_method_003ACopperStart_002EDos_003A0600094F_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	bra.w	C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
C68K_method_003ACopperStart_002EDos_003A06000941_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000986_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	subq.l	#4,a7
	move.l	a1,(a7)
	movea.l	a0,a1
C68K_method_003ACopperStart_002EDos_003A06000986_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	(a7),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000986_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A06000986_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	move.l	(a7),d1
	moveq	#3,d0
	and.l	d0,d1
	tst.l	d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000986_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A06000986_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	move.l	(a7),d1
	moveq	#-49,d0
	cmp.l	d0,d1
	bhi.s	C68K_method_003ACopperStart_002EDos_003A06000986_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A06000986_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	movea.l	(a7),a0
	move.l	a1,d0
	moveq	#48,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001A
	andi.l	#$000000FF,d0
	addq.l	#4,a7
	rts
C68K_method_003ACopperStart_002EDos_003A06000986_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	moveq	#0,d0
	addq.l	#4,a7
	rts
C68K_method_003ACopperStart_002EDos_003A06000945:
C68K_method_003ACopperStart_002EDos_003A06000986_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	move.l	d2,-(a7)
	move.l	a0,d2
C68K_method_003ACopperStart_002EDos_003A06000945_003ABB0000:
	sub.l	d0,d2
	cmp.l	d2,d1
	bcc.s	C68K_method_003ACopperStart_002EDos_003A06000945_003ABB0002
C68K_method_003ACopperStart_002EDos_003A06000945_003ABB0001:
	add.l	d1,d0
	move.l	(a7)+,d2
	rts
C68K_method_003ACopperStart_002EDos_003A06000945_003ABB0002:
	sub.l	d2,d1
	move.l	d1,d0
	move.l	(a7)+,d2
	rts
C68K_method_003ACopperStart_002EDos_003A06000945_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000987_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	a2-a4,-(a7)
	lea	-48(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	movea.l	a1,a3
	movea.l	a0,a4
	movea.l	d0,a2
C68K_method_003ACopperStart_002EDos_003A06000987_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	movea.l	a2,a0
	moveq	#0,d0
	moveq	#11,d1
C68K_generated_003Aallocated_aggregate_zero_loop_003A16:
	move.l	d0,(a0)+
	dbra	d1,C68K_generated_003Aallocated_aggregate_zero_loop_003A16
	movea.l	a4,a0
	movea.l	a3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000986_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000987_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002
C68K_method_003ACopperStart_002EDos_003A06000987_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	moveq	#0,d0
	lea	48(a7),a7
	movem.l	(a7)+,a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A06000987_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	lea	(a7),a0
	movea.l	a0,a1
	moveq	#0,d0
	moveq	#11,d1
C68K_generated_003Aallocated_aggregate_zero_loop_003A17:
	move.l	d0,(a1)+
	dbra	d1,C68K_generated_003Aallocated_aggregate_zero_loop_003A17
	lea	(a7),a1
	movea.l	a3,a0
	moveq	#0,d0
	move.l	(a0),d0
	move.l	d0,(a1)
	lea	(a7),a1
	movea.l	a3,a0
	moveq	#4,d0
	move.l	4(a0),d0
	move.l	d0,4(a1)
	lea	(a7),a1
	movea.l	a3,a0
	moveq	#8,d0
	move.l	8(a0),d0
	move.l	d0,8(a1)
	lea	(a7),a1
	movea.l	a3,a0
	moveq	#12,d0
	move.l	12(a0),d0
	move.l	d0,12(a1)
	lea	(a7),a1
	movea.l	a3,a0
	moveq	#16,d0
	move.l	16(a0),d0
	move.l	d0,16(a1)
	lea	(a7),a1
	movea.l	a3,a0
	moveq	#20,d0
	move.l	20(a0),d0
	move.l	d0,20(a1)
	lea	(a7),a1
	movea.l	a3,a0
	moveq	#24,d0
	move.l	24(a0),d0
	move.l	d0,24(a1)
	lea	(a7),a1
	movea.l	a3,a0
	moveq	#28,d0
	move.l	28(a0),d0
	move.l	d0,28(a1)
	lea	(a7),a1
	movea.l	a3,a0
	moveq	#32,d0
	move.l	32(a0),d0
	move.l	d0,32(a1)
	lea	(a7),a1
	movea.l	a3,a0
	moveq	#36,d0
	move.l	36(a0),d0
	move.l	d0,36(a1)
	lea	(a7),a1
	movea.l	a3,a0
	moveq	#40,d0
	move.l	40(a0),d0
	move.l	d0,40(a1)
	lea	(a7),a1
	movea.l	a3,a0
	moveq	#44,d0
	move.l	44(a0),d0
	move.l	d0,44(a1)
	lea	(a7),a0
	move.l	(a0),d0
	move.l	d0,(a2)
	move.l	4(a0),d0
	move.l	d0,4(a2)
	move.l	8(a0),d0
	move.l	d0,8(a2)
	move.l	12(a0),d0
	move.l	d0,12(a2)
	move.l	16(a0),d0
	move.l	d0,16(a2)
	move.l	20(a0),d0
	move.l	d0,20(a2)
	move.l	24(a0),d0
	move.l	d0,24(a2)
	move.l	28(a0),d0
	move.l	d0,28(a2)
	move.l	32(a0),d0
	move.l	d0,32(a2)
	move.l	36(a0),d0
	move.l	d0,36(a2)
	move.l	40(a0),d0
	move.l	d0,40(a2)
	move.l	44(a0),d0
	move.l	d0,44(a2)
	movea.l	a4,a0
	movea.l	a3,a1
	move.l	a2,d0
	lea	48(a7),a7
	movem.l	(a7)+,a2-a4
	bra.w	C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000089:
C68K_method_003ACopperStart_002EDos_003A06000987_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	move.l	a0,d1
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000089_003ABB0000:
	moveq	#20,d0
	add.l	d0,d1
	movea.l	d1,a0
	rts
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000089_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	d2/a2-a3,-(a7)
	lea	-100(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	clr.l	64(a7)
	clr.l	68(a7)
	clr.l	72(a7)
	clr.l	76(a7)
	clr.l	80(a7)
	clr.l	84(a7)
	clr.l	88(a7)
	clr.l	92(a7)
	clr.l	96(a7)
	movea.l	a0,a3
	movea.l	a1,a2
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	lea	(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	lea	4(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	lea	8(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	lea	12(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	move.l	20(a2),d1
	moveq	#82,d0
	cmp.l	d0,d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	move.l	20(a2),d1
	moveq	#87,d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	move.l	28(a2),d1
	move.l	24(a2),d0
	cmp.l	d0,d1
	bhi.s	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
	move.l	24(a2),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
	lea	16(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009:
	lea	16(a2),a0
	move.l	(a0),d0
	move.l	d0,d2
	moveq	#-1,d1
	move.l	24(a2),d0
	sub.l	d0,d1
	cmp.l	d1,d2
	bhi.s	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A:
	move.l	16(a2),d2
	move.l	24(a2),d1
	move.l	a3,d0
	movea.l	d2,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001A
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B:
	move.l	(a2),d0
	movea.l	a3,a0
	movea.l	d0,a1
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000087_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C:
	move.l	(a2),d0
	movea.l	d0,a0
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000089
	move.l	a0,d1
	move.l	4(a2),d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D:
	move.l	4(a2),d0
	movea.l	a3,a0
	movea.l	d0,a1
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000081_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E:
	moveq	#0,d0
	lea	100(a7),a7
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F:
	move.l	(a2),d0
	lea	(a7),a0
	move.l	a0,-(a7)
	movea.l	a3,a0
	movea.l	d0,a1
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000189_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	addq.l	#4,a7
	move.l	4(a2),d0
	lea	28(a7),a0
	move.l	a0,-(a7)
	movea.l	a3,a0
	movea.l	d0,a1
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000082_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	addq.l	#4,a7
	lea	(a7),a0
	lea	(a0),a0
	lea	16(a0),a0
	move.l	(a0),d0
	move.l	d0,d1
	lea	4(a2),a0
	move.l	(a0),d0
	cmp.l	d0,d1
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001E
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010:
	lea	28(a7),a0
	move.l	(a0),d1
	move.l	(a2),d0
	cmp.l	d0,d1
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001E
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0011:
	lea	28(a7),a0
	move.l	4(a0),d1
	move.l	8(a2),d0
	cmp.l	d0,d1
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001E
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0012:
	lea	(a7),a0
	move.l	20(a0),d1
	move.l	8(a2),d0
	cmp.l	d0,d1
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001E
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0013:
	lea	28(a7),a0
	move.l	8(a0),d1
	move.l	20(a2),d0
	cmp.l	d0,d1
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001E
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0014:
	lea	28(a7),a0
	move.l	20(a0),d1
	lea	12(a2),a0
	move.l	(a0),d0
	cmp.l	d0,d1
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001E
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0015:
	lea	28(a7),a0
	move.l	24(a0),d1
	lea	16(a2),a0
	move.l	(a0),d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001E
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0016:
	lea	28(a7),a0
	move.l	28(a0),d1
	moveq	#0,d0
	cmp.l	d0,d1
	blt.s	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001E
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0017:
	lea	28(a7),a0
	move.l	28(a0),d1
	move.l	24(a2),d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001E
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0018:
	move.l	12(a2),d1
	move.l	a7,d0
	addi.l	#$0000004C,d0
	movea.l	a3,a0
	movea.l	d1,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000963_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001E
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0019:
	lea	76(a7),a0
	move.l	20(a0),d1
	moveq	#1,d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001E
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001A:
	lea	76(a7),a0
	move.l	16(a0),d2
	move.l	20(a2),d1
	moveq	#82,d0
	cmp.l	d0,d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001C
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001B:
	moveq	#2,d0
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001D:
	cmp.l	d0,d2
	seq	d0
	extb.l	d0
	neg.l	d0
	lea	100(a7),a7
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001C:
	moveq	#1,d0
	bra.s	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001D
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001E:
	moveq	#0,d0
	lea	100(a7),a7
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	movem.l	d2-d3/a2-a6,-(a7)
	movea.l	a0,a5
	movea.l	d0,a3
	movea.l	32(a7),a2
	movea.l	d1,a6
	movea.l	a1,a4
C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	moveq	#48,d2
	movea.l	a4,a0
	move.l	d2,d0
	move.l	12(a2),d1
	movea.l	a6,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600095B
	tst.b	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C
C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	move.l	d2,d0
	lea	24(a2),a0
	move.l	(a0),d3
	lea	24(a2),a0
	move.l	4(a0),d1
	movea.l	a4,a0
	movea.l	d3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600095B
	tst.b	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C
C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	movea.l	a4,a0
	move.l	d2,d0
	move.l	(a3),d3
	movea.l	d3,a1
	moveq	#68,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600095B
	tst.b	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C
C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	movea.l	a4,a0
	move.l	d2,d0
	move.l	12(a3),d3
	movea.l	d3,a1
	moveq	#24,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600095B
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C
C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	move.l	24(a3),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006
C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	movea.l	a4,a0
	move.l	d2,d0
	move.l	16(a3),d3
	move.l	24(a3),d1
	movea.l	d3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600095B
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C
C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	lea	64(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009
C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
	move.l	64(a2),d1
	move.l	a4,d0
	cmp.l	d0,d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009
C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
	movea.l	a4,a0
	move.l	d2,d0
	move.l	64(a2),d3
	move.l	d2,d1
	movea.l	d3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600095B
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C
C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009:
	lea	68(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A:
	move.l	68(a2),d1
	move.l	a4,d0
	cmp.l	d0,d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B:
	movea.l	a4,a0
	move.l	d2,d0
	move.l	68(a2),d3
	move.l	d2,d1
	movea.l	d3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600095B
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C:
	moveq	#0,d0
	movem.l	(a7)+,d2-d3/a2-a6
	rts
C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D:
	move.l	a5,d0
	movea.l	a4,a0
	move.l	d2,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001A
	andi.l	#$000000FF,d0
	movem.l	(a7)+,d2-d3/a2-a6
	rts
C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A06000974_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	movem.l	d2/a2-a4,-(a7)
	subq.l	#8,a7
	clr.l	0(a7)
	clr.l	4(a7)
	movea.l	a1,a2
	movea.l	a0,a3
C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	movea.l	a3,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002
C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	moveq	#0,d0
	addq.l	#8,a7
	movem.l	(a7)+,d2/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	move.l	24(a2),d2
	move.l	28(a2),d0
	sub.l	d0,d2
	lea	16(a2),a0
	move.l	(a0),d0
	movea.l	d0,a4
	move.l	28(a2),d0
	adda.l	d0,a4
	move.l	20(a2),d1
	moveq	#82,d0
	cmp.l	d0,d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	move.l	12(a2),d0
	move.l	d2,d1
	lea	(a7),a0
	move.l	a0,-(a7)
	movea.l	a3,a0
	movea.l	d0,a1
	move.l	a4,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000968_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	addq.l	#4,a7
C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007
C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	lea	(a7),a0
	move.l	4(a0),d0
	cmp.l	d2,d0
	bls.s	C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008
C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
	moveq	#0,d0
	addq.l	#8,a7
	movem.l	(a7)+,d2/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	move.l	12(a2),d0
	move.l	d2,d1
	lea	(a7),a0
	move.l	a0,-(a7)
	movea.l	a3,a0
	movea.l	d0,a1
	move.l	a4,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000967_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	addq.l	#4,a7
	bra.s	C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005
C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
	lea	28(a2),a1
	move.l	(a1),d1
	lea	(a7),a0
	move.l	4(a0),d0
	add.l	d0,d1
	move.l	d1,(a1)
	lea	(a7),a0
	move.l	(a0),d0
	cmpi.l	#$00000000,d0
	beq.s	C68K_generated_003Aallocated_002Dswitch_002Dedge_003A18
	cmpi.l	#$00000001,d0
	beq.s	C68K_generated_003Aallocated_002Dswitch_002Dedge_003A19
	cmpi.l	#$00000002,d0
	beq.s	C68K_generated_003Aallocated_002Dswitch_002Dedge_003A20
	cmpi.l	#$00000003,d0
	beq.s	C68K_generated_003Aallocated_002Dswitch_002Dedge_003A21
	bra.s	C68K_generated_003Aallocated_002Dswitch_002Dedge_003A22
C68K_generated_003Aallocated_002Dswitch_002Dedge_003A18:
	bra.s	C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A
C68K_generated_003Aallocated_002Dswitch_002Dedge_003A19:
	bra.s	C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_generated_003Aallocated_002Dswitch_002Dedge_003A20:
	bra.s	C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B
C68K_generated_003Aallocated_002Dswitch_002Dedge_003A21:
	bra.s	C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F
C68K_generated_003Aallocated_002Dswitch_002Dedge_003A22:
C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009:
	moveq	#0,d0
	addq.l	#8,a7
	movem.l	(a7)+,d2/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A:
	movea.l	a3,a0
	movea.l	a2,a1
	addq.l	#8,a7
	movem.l	(a7)+,d2/a2-a4
	bra.w	C68K_method_003ACopperStart_002EDos_003A06000977_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B:
	move.l	20(a2),d1
	moveq	#82,d0
	cmp.l	d0,d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C:
	moveq	#0,d0
	addq.l	#8,a7
	movem.l	(a7)+,d2/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D:
	movea.l	a3,a0
	movea.l	a2,a1
	addq.l	#8,a7
	movem.l	(a7)+,d2/a2-a4
	bra.w	C68K_method_003ACopperStart_002EDos_003A06000977_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E:
	moveq	#2,d0
	addq.l	#8,a7
	movem.l	(a7)+,d2/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F:
	moveq	#3,d0
	addq.l	#8,a7
	movem.l	(a7)+,d2/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600096E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000973_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	a2-a4,-(a7)
	lea	-48(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	movea.l	d0,a2
	movea.l	a1,a3
	movea.l	a0,a4
C68K_method_003ACopperStart_002EDos_003A06000973_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	a7,d0
	movea.l	a4,a0
	movea.l	a3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000987_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000973_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009
C68K_method_003ACopperStart_002EDos_003A06000973_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	movea.l	a4,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000973_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009
C68K_method_003ACopperStart_002EDos_003A06000973_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	move.l	28(a2),d1
	lea	(a7),a0
	move.l	40(a0),d0
	cmp.l	d0,d1
	bcs.s	C68K_method_003ACopperStart_002EDos_003A06000973_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009
C68K_method_003ACopperStart_002EDos_003A06000973_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	lea	(a7),a0
	move.l	16(a0),d1
	move.l	(a2),d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000973_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009
C68K_method_003ACopperStart_002EDos_003A06000973_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	lea	(a7),a0
	move.l	20(a0),d1
	move.l	8(a2),d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000973_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009
C68K_method_003ACopperStart_002EDos_003A06000973_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	lea	(a7),a0
	move.l	24(a0),d1
	move.l	12(a2),d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000973_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009
C68K_method_003ACopperStart_002EDos_003A06000973_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	lea	(a7),a0
	move.l	28(a0),d1
	move.l	16(a2),d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000973_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009
C68K_method_003ACopperStart_002EDos_003A06000973_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
	lea	(a7),a0
	move.l	32(a0),d1
	move.l	20(a2),d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000973_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009
C68K_method_003ACopperStart_002EDos_003A06000973_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
	lea	(a7),a0
	move.l	36(a0),d1
	move.l	24(a2),d0
	cmp.l	d0,d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000973_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A
C68K_method_003ACopperStart_002EDos_003A06000973_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009:
	moveq	#0,d0
	lea	48(a7),a7
	movem.l	(a7)+,a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A06000973_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A:
	lea	(a7),a0
	move.l	28(a2),d0
	move.l	d0,40(a0)
	move.l	a7,d0
	movea.l	a4,a0
	movea.l	a3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000988_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	andi.l	#$000000FF,d0
	lea	48(a7),a7
	movem.l	(a7)+,a2-a4
	rts
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000084_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A06000973_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	movem.l	d2/a2-a3,-(a7)
	move.l	d1,d2
	move.l	d0,d1
	movea.l	a1,a2
	movea.l	a0,a3
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000084_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	movea.l	a3,a0
	movea.l	a2,a1
	moveq	#12,d0
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000086_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	movea.l	a3,a0
	movea.l	a2,a1
	moveq	#16,d0
	move.l	d2,d1
	movem.l	(a7)+,d2/a2-a3
	bra.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000086_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000084_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000959_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	pea	(a2)
	pea	(a3)
	lea	-180(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	clr.l	64(a7)
	clr.l	68(a7)
	movea.l	a1,a2
	movea.l	a0,a3
C68K_method_003ACopperStart_002EDos_003A06000959_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	a7,d0
	movea.l	a3,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600094A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A06000959_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006
C68K_method_003ACopperStart_002EDos_003A06000959_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	lea	(a7),a0
	move.l	20(a0),d0
	tst.l	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000959_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006
C68K_method_003ACopperStart_002EDos_003A06000959_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	lea	(a7),a0
	lea	64(a0),a0
	move.l	(a0),d0
	tst.l	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000959_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006
C68K_method_003ACopperStart_002EDos_003A06000959_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	lea	(a7),a0
	lea	68(a0),a0
	move.l	(a0),d0
	tst.l	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000959_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006
C68K_method_003ACopperStart_002EDos_003A06000959_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	lea	(a7),a0
	move.l	24(a0),d0
	move.l	d0,72(a7)
	move.l	28(a0),d0
	move.l	d0,76(a7)
	move.l	32(a0),d0
	move.l	d0,80(a7)
	move.l	36(a0),d0
	move.l	d0,84(a7)
	move.l	40(a0),d0
	move.l	d0,88(a7)
	move.l	44(a0),d0
	move.l	d0,92(a7)
	move.l	48(a0),d0
	move.l	d0,96(a7)
	move.l	52(a0),d0
	move.l	d0,100(a7)
	move.l	56(a0),d0
	move.l	d0,104(a7)
	lea	72(a7),a0
	move.l	28(a0),d0
	tst.l	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000959_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006
C68K_method_003ACopperStart_002EDos_003A06000959_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	lea	(a7),a0
	move.l	24(a0),d0
	move.l	d0,108(a7)
	move.l	28(a0),d0
	move.l	d0,112(a7)
	move.l	32(a0),d0
	move.l	d0,116(a7)
	move.l	36(a0),d0
	move.l	d0,120(a7)
	move.l	40(a0),d0
	move.l	d0,124(a7)
	move.l	44(a0),d0
	move.l	d0,128(a7)
	move.l	48(a0),d0
	move.l	d0,132(a7)
	move.l	52(a0),d0
	move.l	d0,136(a7)
	move.l	56(a0),d0
	move.l	d0,140(a7)
	lea	108(a7),a0
	move.l	32(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000959_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007
C68K_method_003ACopperStart_002EDos_003A06000959_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	moveq	#0,d0
	lea	180(a7),a7
	movea.l	(a7)+,a3
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A06000959_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
	lea	(a7),a0
	lea	24(a0),a1
	lea	(a7),a0
	move.l	24(a0),d0
	move.l	d0,144(a7)
	move.l	28(a0),d0
	move.l	d0,148(a7)
	move.l	32(a0),d0
	move.l	d0,152(a7)
	move.l	36(a0),d0
	move.l	d0,156(a7)
	move.l	40(a0),d0
	move.l	d0,160(a7)
	move.l	44(a0),d0
	move.l	d0,164(a7)
	move.l	48(a0),d0
	move.l	d0,168(a7)
	move.l	52(a0),d0
	move.l	d0,172(a7)
	move.l	56(a0),d0
	move.l	d0,176(a7)
	lea	144(a7),a0
	move.l	16(a0),d0
	move.l	d0,12(a1)
	lea	(a7),a0
	lea	24(a0),a0
	moveq	#0,d0
	clr.l	20(a0)
	lea	(a7),a0
	lea	24(a0),a0
	moveq	#0,d0
	clr.l	24(a0)
	move.l	a7,d0
	movea.l	a3,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600094B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	andi.l	#$000000FF,d0
	lea	180(a7),a7
	movea.l	(a7)+,a3
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A06000959_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	d2-d3/a2-a3,-(a7)
	lea	-328(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	clr.l	64(a7)
	clr.l	68(a7)
	clr.l	72(a7)
	clr.l	76(a7)
	clr.l	80(a7)
	clr.l	84(a7)
	clr.l	88(a7)
	clr.l	92(a7)
	clr.l	96(a7)
	clr.l	100(a7)
	clr.l	104(a7)
	clr.l	108(a7)
	clr.l	112(a7)
	clr.l	116(a7)
	clr.l	120(a7)
	clr.l	124(a7)
	clr.l	128(a7)
	clr.l	132(a7)
	clr.l	136(a7)
	clr.l	140(a7)
	clr.l	144(a7)
	clr.l	148(a7)
	clr.l	152(a7)
	clr.l	156(a7)
	clr.l	160(a7)
	clr.l	164(a7)
	clr.l	168(a7)
	clr.l	172(a7)
	clr.l	176(a7)
	move.l	d0,324(a7)
	movea.l	a1,a3
	movea.l	a0,a2
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	324(a7),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	move.l	a7,d0
	movea.l	a2,a0
	movea.l	a3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000990_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	lea	(a7),a1
	movea.l	a2,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000998_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	moveq	#0,d0
	lea	328(a7),a7
	movem.l	(a7)+,d2-d3/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001D:
	move.l	a7,d0
	addi.l	#$00000024,d0
	movea.l	a2,a0
	movea.l	d3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000952_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001E:
	move.l	d3,d1
	move.l	324(a7),d0
	cmp.l	d0,d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001F:
	move.l	d3,32(a7)
	lea	36(a7),a0
	move.l	8(a0),d3
	addq.l	#1,d2
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001B:
	lea	(a7),a0
	move.l	12(a0),d0
	cmp.l	d0,d2
	bcs.s	C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001C:
	moveq	#0,d0
	lea	328(a7),a7
	movem.l	(a7)+,d2-d3/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	moveq	#0,d0
	move.l	d0,32(a7)
	lea	(a7),a0
	move.l	8(a0),d3
	moveq	#0,d2
	bra.s	C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001B
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	moveq	#0,d0
	lea	328(a7),a7
	movem.l	(a7)+,d2-d3/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009:
	lea	36(a7),a0
	move.l	20(a0),d0
	tst.l	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A:
	lea	36(a7),a0
	lea	64(a0),a0
	move.l	(a0),d0
	tst.l	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B:
	lea	36(a7),a0
	lea	68(a0),a0
	move.l	(a0),d0
	tst.l	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C:
	lea	36(a7),a0
	move.l	24(a0),d0
	move.l	d0,180(a7)
	move.l	28(a0),d0
	move.l	d0,184(a7)
	move.l	32(a0),d0
	move.l	d0,188(a7)
	move.l	36(a0),d0
	move.l	d0,192(a7)
	move.l	40(a0),d0
	move.l	d0,196(a7)
	move.l	44(a0),d0
	move.l	d0,200(a7)
	move.l	48(a0),d0
	move.l	d0,204(a7)
	move.l	52(a0),d0
	move.l	d0,208(a7)
	move.l	56(a0),d0
	move.l	d0,212(a7)
	lea	180(a7),a0
	move.l	28(a0),d0
	tst.l	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D:
	lea	36(a7),a0
	move.l	24(a0),d0
	move.l	d0,216(a7)
	move.l	28(a0),d0
	move.l	d0,220(a7)
	move.l	32(a0),d0
	move.l	d0,224(a7)
	move.l	36(a0),d0
	move.l	d0,228(a7)
	move.l	40(a0),d0
	move.l	d0,232(a7)
	move.l	44(a0),d0
	move.l	d0,236(a7)
	move.l	48(a0),d0
	move.l	d0,240(a7)
	move.l	52(a0),d0
	move.l	d0,244(a7)
	move.l	56(a0),d0
	move.l	d0,248(a7)
	lea	216(a7),a0
	move.l	32(a0),d0
	tst.l	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E:
	lea	36(a7),a0
	move.l	24(a0),d0
	move.l	d0,252(a7)
	move.l	28(a0),d0
	move.l	d0,256(a7)
	move.l	32(a0),d0
	move.l	d0,260(a7)
	move.l	36(a0),d0
	move.l	d0,264(a7)
	move.l	40(a0),d0
	move.l	d0,268(a7)
	move.l	44(a0),d0
	move.l	d0,272(a7)
	move.l	48(a0),d0
	move.l	d0,276(a7)
	move.l	52(a0),d0
	move.l	d0,280(a7)
	move.l	56(a0),d0
	move.l	d0,284(a7)
	lea	252(a7),a0
	move.l	20(a0),d0
	tst.l	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F:
	lea	36(a7),a0
	move.l	24(a0),d0
	move.l	d0,288(a7)
	move.l	28(a0),d0
	move.l	d0,292(a7)
	move.l	32(a0),d0
	move.l	d0,296(a7)
	move.l	36(a0),d0
	move.l	d0,300(a7)
	move.l	40(a0),d0
	move.l	d0,304(a7)
	move.l	44(a0),d0
	move.l	d0,308(a7)
	move.l	48(a0),d0
	move.l	d0,312(a7)
	move.l	52(a0),d0
	move.l	d0,316(a7)
	move.l	56(a0),d0
	move.l	d0,320(a7)
	lea	288(a7),a0
	move.l	24(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0011
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010:
	moveq	#0,d0
	lea	328(a7),a7
	movem.l	(a7)+,d2-d3/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0011:
	move.l	32(a7),d0
	tst.l	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0013
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0012:
	lea	(a7),a1
	lea	36(a7),a0
	move.l	8(a0),d0
	move.l	d0,8(a1)
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0017:
	lea	36(a7),a0
	moveq	#0,d0
	clr.l	8(a0)
	lea	(a7),a0
	lea	12(a0),a0
	move.l	(a0),d1
	subq.l	#1,d1
	move.l	d1,(a0)
	movea.l	324(a7),a1
	move.l	a7,d0
	addi.l	#$00000024,d0
	movea.l	a2,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600094B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0019
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0018:
	move.l	a7,d0
	movea.l	a2,a0
	movea.l	a3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000991_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	andi.l	#$000000FF,d0
	lea	328(a7),a7
	movem.l	(a7)+,d2-d3/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0013:
	move.l	32(a7),d1
	move.l	a7,d0
	addi.l	#$0000006C,d0
	movea.l	a2,a0
	movea.l	d1,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000952_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0015
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0014:
	moveq	#0,d0
	lea	328(a7),a7
	movem.l	(a7)+,d2-d3/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0015:
	lea	108(a7),a1
	lea	36(a7),a0
	move.l	8(a0),d0
	move.l	d0,8(a1)
	move.l	32(a7),d1
	move.l	a7,d0
	addi.l	#$0000006C,d0
	movea.l	a2,a0
	movea.l	d1,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600094B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0017
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0016:
	moveq	#0,d0
	lea	328(a7),a7
	movem.l	(a7)+,d2-d3/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0019:
	moveq	#0,d0
	lea	328(a7),a7
	movem.l	(a7)+,d2-d3/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A06000997_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	movem.l	d2/a2-a3,-(a7)
	lea	-160(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	clr.l	64(a7)
	clr.l	68(a7)
	clr.l	72(a7)
	clr.l	76(a7)
	clr.l	80(a7)
	clr.l	84(a7)
	clr.l	88(a7)
	clr.l	92(a7)
	clr.l	96(a7)
	clr.l	100(a7)
	clr.l	104(a7)
	clr.l	108(a7)
	clr.l	112(a7)
	clr.l	116(a7)
	clr.l	120(a7)
	clr.l	124(a7)
	clr.l	128(a7)
	clr.l	132(a7)
	clr.l	136(a7)
	clr.l	140(a7)
	clr.l	144(a7)
	clr.l	148(a7)
	clr.l	152(a7)
	clr.l	156(a7)
	movea.l	a0,a3
	move.l	a1,d1
	movea.l	d0,a2
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	(a2),d2
	move.l	#$4451504E,d0
	cmp.l	d0,d2
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	move.l	4(a2),d2
	moveq	#1,d0
	cmp.l	d0,d2
	bne.w	C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	move.l	8(a2),d0
	cmp.l	d1,d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	lea	12(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	lea	16(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	lea	20(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	lea	24(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
	move.l	32(a2),d1
	moveq	#82,d0
	cmp.l	d0,d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
	move.l	32(a2),d1
	moveq	#87,d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009:
	move.l	40(a2),d1
	move.l	36(a2),d0
	cmp.l	d0,d1
	bhi.s	C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A:
	move.l	44(a2),d1
	moveq	#1,d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B:
	move.l	36(a2),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C:
	lea	28(a2),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D:
	move.l	16(a2),d0
	movea.l	a3,a0
	movea.l	d0,a1
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000087_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E:
	moveq	#0,d0
	lea	160(a7),a7
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F:
	move.l	16(a2),d0
	movea.l	d0,a0
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000089
	move.l	a0,d2
	lea	128(a7),a0
	movea.l	a0,a1
	moveq	#0,d0
	moveq	#7,d1
C68K_generated_003Aallocated_aggregate_zero_loop_003A23:
	move.l	d0,(a1)+
	dbra	d1,C68K_generated_003Aallocated_aggregate_zero_loop_003A23
	lea	128(a7),a0
	move.l	16(a2),d0
	move.l	d0,(a0)
	lea	128(a7),a0
	move.l	d2,4(a0)
	lea	128(a7),a0
	move.l	20(a2),d0
	move.l	d0,8(a0)
	lea	128(a7),a0
	move.l	24(a2),d0
	move.l	d0,12(a0)
	lea	128(a7),a0
	move.l	28(a2),d0
	move.l	d0,16(a0)
	lea	128(a7),a0
	move.l	32(a2),d0
	move.l	d0,20(a0)
	lea	128(a7),a0
	move.l	36(a2),d0
	move.l	d0,24(a0)
	lea	128(a7),a0
	move.l	40(a2),d0
	move.l	d0,28(a0)
	lea	128(a7),a0
	move.l	(a0),d0
	move.l	d0,(a7)
	move.l	4(a0),d0
	move.l	d0,4(a7)
	move.l	8(a0),d0
	move.l	d0,8(a7)
	move.l	12(a0),d0
	move.l	d0,12(a7)
	move.l	16(a0),d0
	move.l	d0,16(a7)
	move.l	20(a0),d0
	move.l	d0,20(a7)
	move.l	24(a0),d0
	move.l	d0,24(a7)
	move.l	28(a0),d0
	move.l	d0,28(a7)
	lea	(a7),a1
	movea.l	a3,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000976_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0017
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010:
	move.l	24(a2),d1
	move.l	a7,d0
	addi.l	#$00000020,d0
	movea.l	a3,a0
	movea.l	d1,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000963_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0017
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0011:
	lea	32(a7),a0
	move.l	12(a0),d1
	move.l	12(a2),d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0017
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0012:
	move.l	12(a2),d1
	move.l	a7,d0
	addi.l	#$00000038,d0
	movea.l	a3,a0
	movea.l	d1,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000952_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0017
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0013:
	lea	56(a7),a0
	move.l	20(a0),d2
	move.l	32(a2),d1
	moveq	#82,d0
	cmp.l	d0,d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0015
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0014:
	moveq	#2,d0
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0016:
	and.l	d0,d2
	tst.l	d2
	shi	d0
	extb.l	d0
	neg.l	d0
	lea	160(a7),a7
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0015:
	moveq	#1,d0
	bra.s	C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0016
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0017:
	moveq	#0,d0
	lea	160(a7),a7
	movem.l	(a7)+,d2/a2-a3
	rts
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000087_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000087_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	moveq	#68,d0
	moveq	#2,d1
	bra.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000061_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000081_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000081_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000087_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	moveq	#48,d0
	moveq	#2,d1
	bra.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000061_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000081_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000189_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	a2-a4,-(a7)
	lea	-48(a7),a7
	movea.l	a1,a3
	movea.l	a0,a4
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000189_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	lea	(a7),a0
	clr.l	0(a0)
	clr.l	4(a0)
	clr.l	8(a0)
	clr.l	12(a0)
	clr.l	16(a0)
	clr.l	20(a0)
	clr.l	24(a0)
	lea	(a7),a2
	lea	28(a7),a0
	move.l	a0,-(a7)
	movea.l	a4,a0
	movea.l	a3,a1
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A0600016A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	addq.l	#4,a7
	lea	28(a7),a0
	move.l	(a0),d0
	move.l	d0,(a2)
	move.l	4(a0),d0
	move.l	d0,4(a2)
	move.l	8(a0),d0
	move.l	d0,8(a2)
	move.l	12(a0),d0
	move.l	d0,12(a2)
	move.l	16(a0),d0
	move.l	d0,16(a2)
	lea	(a7),a2
	movea.l	a4,a0
	movea.l	a3,a1
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000188_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	move.l	a0,d0
	move.l	d0,20(a2)
	lea	(a7),a1
	movea.l	a3,a0
	moveq	#18,d0
	move.w	18(a0),d0
	andi.l	#$0000FFFF,d0
	move.w	d0,24(a1)
	lea	(a7),a0
	movea.l	64(a7),a1
	move.l	(a0),d0
	move.l	d0,(a1)
	move.l	4(a0),d0
	move.l	d0,4(a1)
	move.l	8(a0),d0
	move.l	d0,8(a1)
	move.l	12(a0),d0
	move.l	d0,12(a1)
	move.l	16(a0),d0
	move.l	d0,16(a1)
	move.l	20(a0),d0
	move.l	d0,20(a1)
	move.l	24(a0),d0
	move.l	d0,24(a1)
	lea	48(a7),a7
	movem.l	(a7)+,a2-a4
	rts
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000082_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000189_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	movem.l	a2-a4,-(a7)
	lea	-48(a7),a7
	movea.l	a1,a2
	movea.l	a0,a3
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000082_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	lea	(a7),a0
	movea.l	a0,a1
	moveq	#0,d0
	moveq	#11,d1
C68K_generated_003Aallocated_aggregate_zero_loop_003A24:
	move.l	d0,(a1)+
	dbra	d1,C68K_generated_003Aallocated_aggregate_zero_loop_003A24
	lea	(a7),a1
	movea.l	a2,a0
	moveq	#0,d0
	move.l	(a0),d0
	move.l	d0,(a1)
	lea	(a7),a1
	movea.l	a2,a0
	moveq	#4,d0
	move.l	4(a0),d0
	move.l	d0,4(a1)
	lea	(a7),a4
	movea.l	a3,a0
	movea.l	a2,a1
	moveq	#8,d0
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000085_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	move.l	d0,8(a4)
	lea	(a7),a4
	movea.l	a3,a0
	movea.l	a2,a1
	moveq	#12,d0
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000085_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	move.l	d0,12(a4)
	lea	(a7),a4
	movea.l	a3,a0
	movea.l	a2,a1
	moveq	#16,d0
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000085_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	move.l	d0,16(a4)
	lea	(a7),a4
	movea.l	a3,a0
	movea.l	a2,a1
	moveq	#20,d0
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000085_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	move.l	d0,20(a4)
	lea	(a7),a4
	movea.l	a3,a0
	movea.l	a2,a1
	moveq	#24,d0
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000085_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	move.l	d0,24(a4)
	lea	(a7),a4
	movea.l	a3,a0
	movea.l	a2,a1
	moveq	#28,d0
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000085_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	move.l	d0,28(a4)
	lea	(a7),a4
	movea.l	a3,a0
	movea.l	a2,a1
	moveq	#32,d0
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000085_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	move.l	d0,32(a4)
	lea	(a7),a4
	movea.l	a3,a0
	movea.l	a2,a1
	moveq	#36,d0
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000085_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	move.l	d0,36(a4)
	lea	(a7),a4
	movea.l	a3,a0
	movea.l	a2,a1
	moveq	#40,d0
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000085_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	move.l	d0,40(a4)
	lea	(a7),a4
	movea.l	a3,a0
	movea.l	a2,a1
	moveq	#44,d0
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000085_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	move.l	d0,44(a4)
	lea	(a7),a0
	movea.l	64(a7),a1
	move.l	(a0),d0
	move.l	d0,(a1)
	move.l	4(a0),d0
	move.l	d0,4(a1)
	move.l	8(a0),d0
	move.l	d0,8(a1)
	move.l	12(a0),d0
	move.l	d0,12(a1)
	move.l	16(a0),d0
	move.l	d0,16(a1)
	move.l	20(a0),d0
	move.l	d0,20(a1)
	move.l	24(a0),d0
	move.l	d0,24(a1)
	move.l	28(a0),d0
	move.l	d0,28(a1)
	move.l	32(a0),d0
	move.l	d0,32(a1)
	move.l	36(a0),d0
	move.l	d0,36(a1)
	move.l	40(a0),d0
	move.l	d0,40(a1)
	move.l	44(a0),d0
	move.l	d0,44(a1)
	lea	48(a7),a7
	movem.l	(a7)+,a2-a4
	rts
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000082_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000963_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	pea	(a2)
	lea	-28(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	move.l	a1,24(a7)
	movea.l	a0,a1
	movea.l	d0,a2
C68K_method_003ACopperStart_002EDos_003A06000963_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	clr.l	0(a2)
	clr.l	4(a2)
	clr.l	8(a2)
	clr.l	12(a2)
	clr.l	16(a2)
	clr.l	20(a2)
	move.l	24(a7),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000963_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A06000963_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	move.l	24(a7),d1
	moveq	#3,d0
	and.l	d0,d1
	tst.l	d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000963_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A06000963_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	move.l	24(a7),d1
	moveq	#-25,d0
	cmp.l	d0,d1
	bhi.s	C68K_method_003ACopperStart_002EDos_003A06000963_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A06000963_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	movea.l	24(a7),a0
	move.l	a1,d0
	moveq	#24,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001A
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000963_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005
C68K_method_003ACopperStart_002EDos_003A06000963_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	moveq	#0,d0
	lea	28(a7),a7
	movea.l	(a7)+,a2
	rts
C68K_method_003ACopperStart_002EDos_003A06000963_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	lea	(a7),a0
	clr.l	0(a0)
	clr.l	4(a0)
	clr.l	8(a0)
	clr.l	12(a0)
	clr.l	16(a0)
	clr.l	20(a0)
	lea	(a7),a1
	movea.l	24(a7),a0
	moveq	#0,d0
	move.l	(a0),d0
	move.l	d0,(a1)
	lea	(a7),a1
	movea.l	24(a7),a0
	moveq	#4,d0
	move.l	4(a0),d0
	move.l	d0,4(a1)
	lea	(a7),a1
	movea.l	24(a7),a0
	moveq	#8,d0
	move.l	8(a0),d0
	move.l	d0,8(a1)
	lea	(a7),a1
	movea.l	24(a7),a0
	moveq	#12,d0
	move.l	12(a0),d0
	move.l	d0,12(a1)
	lea	(a7),a1
	movea.l	24(a7),a0
	moveq	#16,d0
	move.l	16(a0),d0
	move.l	d0,16(a1)
	lea	(a7),a1
	movea.l	24(a7),a0
	moveq	#20,d0
	move.l	20(a0),d0
	move.l	d0,20(a1)
	lea	(a7),a0
	move.l	(a0),d0
	move.l	d0,(a2)
	move.l	4(a0),d0
	move.l	d0,4(a2)
	move.l	8(a0),d0
	move.l	d0,8(a2)
	move.l	12(a0),d0
	move.l	d0,12(a2)
	move.l	16(a0),d0
	move.l	d0,16(a2)
	move.l	20(a0),d0
	move.l	d0,20(a2)
	movea.l	24(a7),a0
	movea.l	a2,a1
	lea	28(a7),a7
	movea.l	(a7)+,a2
	bra.w	C68K_method_003ACopperStart_002EDos_003A06000965
C68K_method_003ACopperStart_002EDos_003A06000963_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000968_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	d2/a2-a5,-(a7)
	lea	-112(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	clr.l	64(a7)
	clr.l	68(a7)
	clr.l	72(a7)
	clr.l	76(a7)
	clr.l	80(a7)
	clr.l	84(a7)
	clr.l	88(a7)
	clr.l	92(a7)
	move.l	d1,d2
	movea.l	d0,a3
	movea.l	a1,a5
	movea.l	a0,a4
	movea.l	136(a7),a2
C68K_method_003ACopperStart_002EDos_003A06000968_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	lea	96(a7),a0
	move.l	a0,-(a7)
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600096C
	addq.l	#4,a7
	lea	96(a7),a0
	move.l	(a0),d0
	move.l	d0,(a2)
	move.l	4(a0),d0
	move.l	d0,4(a2)
	move.l	a7,d1
	lea	24(a7),a0
	move.l	a0,-(a7)
	movea.l	a4,a0
	movea.l	a5,a1
	moveq	#2,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600096A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	addq.l	#4,a7
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000968_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002
C68K_method_003ACopperStart_002EDos_003A06000968_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	moveq	#0,d0
	lea	112(a7),a7
	movem.l	(a7)+,d2/a2-a5
	rts
C68K_method_003ACopperStart_002EDos_003A06000968_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	lea	(a7),a0
	move.l	12(a0),d0
	lea	104(a7),a0
	move.l	a0,-(a7)
	movea.l	a4,a0
	movea.l	d0,a1
	move.l	a3,d0
	move.l	d2,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000956_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	addq.l	#4,a7
	lea	104(a7),a0
	move.l	(a0),d0
	move.l	d0,(a2)
	move.l	4(a0),d0
	move.l	d0,4(a2)
	move.l	(a2),d1
	moveq	#4,d0
	cmp.l	d0,d1
	seq	d0
	extb.l	d0
	neg.l	d0
	tst.l	d0
	seq	d0
	extb.l	d0
	neg.l	d0
	lea	112(a7),a7
	movem.l	(a7)+,d2/a2-a5
	rts
C68K_method_003ACopperStart_002EDos_003A06000967_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A06000968_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	movem.l	d2/a2-a5,-(a7)
	lea	-112(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	clr.l	64(a7)
	clr.l	68(a7)
	clr.l	72(a7)
	clr.l	76(a7)
	clr.l	80(a7)
	clr.l	84(a7)
	clr.l	88(a7)
	clr.l	92(a7)
	move.l	d1,d2
	movea.l	d0,a3
	movea.l	a1,a5
	movea.l	a0,a4
	movea.l	136(a7),a2
C68K_method_003ACopperStart_002EDos_003A06000967_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	lea	96(a7),a0
	move.l	a0,-(a7)
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600096C
	addq.l	#4,a7
	lea	96(a7),a0
	move.l	(a0),d0
	move.l	d0,(a2)
	move.l	4(a0),d0
	move.l	d0,4(a2)
	move.l	a7,d1
	lea	24(a7),a0
	move.l	a0,-(a7)
	movea.l	a4,a0
	movea.l	a5,a1
	moveq	#1,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600096A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	addq.l	#4,a7
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000967_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002
C68K_method_003ACopperStart_002EDos_003A06000967_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	moveq	#0,d0
	lea	112(a7),a7
	movem.l	(a7)+,d2/a2-a5
	rts
C68K_method_003ACopperStart_002EDos_003A06000967_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	lea	(a7),a0
	move.l	12(a0),d0
	lea	104(a7),a0
	move.l	a0,-(a7)
	movea.l	a4,a0
	movea.l	d0,a1
	move.l	a3,d0
	move.l	d2,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000955_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	addq.l	#4,a7
	lea	104(a7),a0
	move.l	(a0),d0
	move.l	d0,(a2)
	move.l	4(a0),d0
	move.l	d0,4(a2)
	move.l	(a2),d1
	moveq	#4,d0
	cmp.l	d0,d1
	seq	d0
	extb.l	d0
	neg.l	d0
	tst.l	d0
	seq	d0
	extb.l	d0
	neg.l	d0
	lea	112(a7),a7
	movem.l	(a7)+,d2/a2-a5
	rts
C68K_method_003ACopperStart_002EDos_003A06000967_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000977_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	move.l	d2,-(a7)
C68K_method_003ACopperStart_002EDos_003A06000977_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	4(a1),d2
	move.l	28(a1),d0
	movea.l	d2,a1
	moveq	#0,d1
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000084_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	moveq	#1,d0
	move.l	(a7)+,d2
	rts
C68K_method_003ACopperStart_002EDos_003A06000977_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000988_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	a2-a4,-(a7)
	movea.l	d0,a2
	movea.l	a1,a3
	movea.l	a0,a4
C68K_method_003ACopperStart_002EDos_003A06000988_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	movea.l	a4,a0
	movea.l	a3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000986_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000988_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002
C68K_method_003ACopperStart_002EDos_003A06000988_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	movea.l	a4,a0
	movea.l	a3,a1
	move.l	a2,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000989_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000988_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A06000988_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	moveq	#0,d0
	movem.l	(a7)+,a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A06000988_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	move.l	(a2),d1
	movea.l	a3,a0
	moveq	#0,d0
	move.l	d1,(a0)
	move.l	4(a2),d1
	movea.l	a3,a0
	moveq	#4,d0
	move.l	d1,4(a0)
	lea	8(a2),a0
	move.l	(a0),d0
	move.l	d0,d1
	movea.l	a3,a0
	moveq	#8,d0
	move.l	d1,8(a0)
	lea	12(a2),a0
	move.l	(a0),d0
	move.l	d0,d1
	movea.l	a3,a0
	moveq	#12,d0
	move.l	d1,12(a0)
	lea	16(a2),a0
	move.l	(a0),d0
	move.l	d0,d1
	movea.l	a3,a0
	moveq	#16,d0
	move.l	d1,16(a0)
	lea	20(a2),a0
	move.l	(a0),d0
	move.l	d0,d1
	movea.l	a3,a0
	moveq	#20,d0
	move.l	d1,20(a0)
	lea	24(a2),a0
	move.l	(a0),d0
	move.l	d0,d1
	movea.l	a3,a0
	moveq	#24,d0
	move.l	d1,24(a0)
	lea	28(a2),a0
	move.l	(a0),d0
	move.l	d0,d1
	movea.l	a3,a0
	moveq	#28,d0
	move.l	d1,28(a0)
	move.l	32(a2),d1
	movea.l	a3,a0
	moveq	#32,d0
	move.l	d1,32(a0)
	move.l	36(a2),d1
	movea.l	a3,a0
	moveq	#36,d0
	move.l	d1,36(a0)
	move.l	40(a2),d1
	movea.l	a3,a0
	moveq	#40,d0
	move.l	d1,40(a0)
	move.l	44(a2),d1
	movea.l	a3,a0
	moveq	#44,d0
	move.l	d1,44(a0)
	moveq	#1,d0
	movem.l	(a7)+,a2-a4
	rts
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000086_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000086_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
C68K_method_003ACopperStart_002EDos_003A06000988_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	movea.l	a1,a0
	adda.l	d0,a0
	move.l	d1,(a0)
	rts
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000061_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000086_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	move.l	d2,-(a7)
	move.l	d3,-(a7)
	subq.l	#4,a7
	move.l	a1,(a7)
	movea.l	a0,a1
	move.l	d0,d2
	move.l	d1,d3
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000061_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	(a7),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000061_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000061_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	move.l	(a7),d1
	subq.l	#1,d3
	and.l	d3,d1
	tst.l	d1
	bne.s	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000061_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000061_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	move.l	(a7),d1
	moveq	#-1,d0
	sub.l	d2,d0
	cmp.l	d0,d1
	bhi.s	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000061_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000061_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	movea.l	(a7),a0
	move.l	a1,d0
	move.l	d2,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001A
	andi.l	#$000000FF,d0
	addq.l	#4,a7
	move.l	(a7)+,d3
	move.l	(a7)+,d2
	rts
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000061_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	moveq	#0,d0
	addq.l	#4,a7
	move.l	(a7)+,d3
	move.l	(a7)+,d2
	rts
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000061_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A0600016A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	a2-a4,-(a7)
	lea	-20(a7),a7
	movea.l	a1,a2
	movea.l	a0,a4
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A0600016A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	lea	(a7),a0
	clr.l	0(a0)
	clr.l	4(a0)
	clr.l	8(a0)
	clr.l	12(a0)
	clr.l	16(a0)
	lea	(a7),a3
	movea.l	a4,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000166_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	move.l	a0,d0
	move.l	d0,(a3)
	lea	(a7),a3
	movea.l	a4,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000167_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	move.l	a0,d0
	move.l	d0,4(a3)
	lea	(a7),a3
	movea.l	a4,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000168_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	andi.l	#$000000FF,d0
	move.b	d0,8(a3)
	lea	(a7),a1
	movea.l	a2,a0
	moveq	#9,d0
	move.b	9(a0),d0
	andi.l	#$000000FF,d0
	extb.l	d0
	move.b	d0,12(a1)
	lea	(a7),a3
	movea.l	a4,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000169_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	move.l	a0,d0
	move.l	d0,16(a3)
	lea	(a7),a0
	movea.l	36(a7),a1
	move.l	(a0),d0
	move.l	d0,(a1)
	move.l	4(a0),d0
	move.l	d0,4(a1)
	move.l	8(a0),d0
	move.l	d0,8(a1)
	move.l	12(a0),d0
	move.l	d0,12(a1)
	move.l	16(a0),d0
	move.l	d0,16(a1)
	lea	20(a7),a7
	movem.l	(a7)+,a2-a4
	rts
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A0600016A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000188_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000188_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	movea.l	a1,a0
	moveq	#14,d0
	move.l	14(a0),d0
	movea.l	d0,a0
	rts
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000188_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000013:
C68K_method_003ACopperStart_002EDos_003A06000013_003ABB0000:
	move.l	d1,d0
	adda.l	d0,a0
	move.w	(a0),d0
	andi.l	#$0000FFFF,d0
	andi.l	#$0000FFFF,d0
	rts
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000085_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000085_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
C68K_method_003ACopperStart_002EDos_003A06000013_003Aend:
	movea.l	a1,a0
	adda.l	d0,a0
	move.l	(a0),d0
	rts
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000085_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000965:
C68K_method_003ACopperStart_002EDos_003A06000965_003ABB0000:
	move.l	(a1),d1
	move.l	#$44514648,d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000965_003ABB0009
C68K_method_003ACopperStart_002EDos_003A06000965_003ABB0001:
	move.l	4(a1),d1
	moveq	#1,d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000965_003ABB0009
C68K_method_003ACopperStart_002EDos_003A06000965_003ABB0002:
	move.l	8(a1),d1
	move.l	a0,d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000965_003ABB0009
C68K_method_003ACopperStart_002EDos_003A06000965_003ABB0003:
	lea	12(a1),a0
	move.l	(a0),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000965_003ABB0009
C68K_method_003ACopperStart_002EDos_003A06000965_003ABB0004:
	move.l	16(a1),d1
	moveq	#1,d0
	cmp.l	d0,d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000965_003ABB0006
C68K_method_003ACopperStart_002EDos_003A06000965_003ABB0005:
	move.l	16(a1),d1
	moveq	#2,d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000965_003ABB0009
C68K_method_003ACopperStart_002EDos_003A06000965_003ABB0006:
	move.l	20(a1),d1
	moveq	#1,d0
	cmp.l	d0,d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000965_003ABB0008
C68K_method_003ACopperStart_002EDos_003A06000965_003ABB0007:
	move.l	20(a1),d1
	moveq	#2,d0
	cmp.l	d0,d1
	seq	d0
	extb.l	d0
	neg.l	d0
	rts
C68K_method_003ACopperStart_002EDos_003A06000965_003ABB0008:
	moveq	#1,d0
	rts
C68K_method_003ACopperStart_002EDos_003A06000965_003ABB0009:
	moveq	#0,d0
	rts
C68K_method_003ACopperStart_002EDos_003A06000965_003Aend:
C68K_method_003ACopperStart_002EDos_003A0600096C:
	subq.l	#8,a7
C68K_method_003ACopperStart_002EDos_003A0600096C_003ABB0000:
	lea	(a7),a0
	clr.l	0(a0)
	clr.l	4(a0)
	lea	(a7),a0
	moveq	#4,d0
	move.l	d0,(a0)
	lea	(a7),a0
	movea.l	12(a7),a1
	move.l	(a0),d0
	move.l	d0,(a1)
	move.l	4(a0),d0
	move.l	d0,4(a1)
	addq.l	#8,a7
	rts
C68K_method_003ACopperStart_002EDos_003A0600096A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A0600096C_003Aend:
	movem.l	d2/a2-a5,-(a7)
	move.l	d0,d2
	movea.l	a1,a4
	movea.l	a0,a5
	movea.l	24(a7),a3
	movea.l	d1,a2
C68K_method_003ACopperStart_002EDos_003A0600096A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	clr.l	0(a2)
	clr.l	4(a2)
	clr.l	8(a2)
	clr.l	12(a2)
	clr.l	16(a2)
	clr.l	20(a2)
	movea.l	a3,a0
	moveq	#0,d0
	moveq	#17,d1
C68K_generated_003Aallocated_aggregate_zero_loop_003A25:
	move.l	d0,(a0)+
	dbra	d1,C68K_generated_003Aallocated_aggregate_zero_loop_003A25
	movea.l	a5,a0
	movea.l	a4,a1
	move.l	a2,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000963_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600096A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007
C68K_method_003ACopperStart_002EDos_003A0600096A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	move.l	20(a2),d1
	moveq	#1,d0
	cmp.l	d0,d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600096A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007
C68K_method_003ACopperStart_002EDos_003A0600096A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	move.l	16(a2),d0
	cmp.l	d2,d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600096A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007
C68K_method_003ACopperStart_002EDos_003A0600096A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	move.l	12(a2),d0
	movea.l	a5,a0
	movea.l	d0,a1
	move.l	a3,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000952_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600096A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007
C68K_method_003ACopperStart_002EDos_003A0600096A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	move.l	20(a3),d0
	and.l	d2,d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600096A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007
C68K_method_003ACopperStart_002EDos_003A0600096A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	move.l	12(a2),d2
	move.l	12(a3),d1
	movea.l	a4,a0
	moveq	#24,d0
	movea.l	d2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600095B
	tst.b	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600096A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007
C68K_method_003ACopperStart_002EDos_003A0600096A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	lea	24(a3),a0
	move.l	(a0),d2
	lea	24(a3),a0
	move.l	4(a0),d1
	movea.l	a4,a0
	moveq	#24,d0
	movea.l	d2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600095B
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600096A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008
C68K_method_003ACopperStart_002EDos_003A0600096A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
	moveq	#0,d0
	movem.l	(a7)+,d2/a2-a5
	rts
C68K_method_003ACopperStart_002EDos_003A0600096A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
	moveq	#1,d0
	movem.l	(a7)+,d2/a2-a5
	rts
C68K_method_003ACopperStart_002EDos_003A06000956_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A0600096A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	movem.l	d2/a2-a4,-(a7)
	lea	-80(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	clr.l	64(a7)
	clr.l	68(a7)
	clr.l	72(a7)
	clr.l	76(a7)
	move.l	d1,d2
	movea.l	d0,a3
	movea.l	a1,a2
	movea.l	a0,a4
C68K_method_003ACopperStart_002EDos_003A06000956_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	a7,d0
	movea.l	a4,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600094A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000956_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002
C68K_method_003ACopperStart_002EDos_003A06000956_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	lea	(a7),a0
	move.l	20(a0),d1
	moveq	#2,d0
	and.l	d0,d1
	tst.l	d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000956_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A06000956_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	movea.l	100(a7),a0
	move.l	a0,-(a7)
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600096C
	addq.l	#4,a7
	lea	80(a7),a7
	movem.l	(a7)+,d2/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A06000956_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	lea	(a7),a0
	lea	24(a0),a1
	lea	72(a7),a0
	move.l	a0,-(a7)
	movea.l	a4,a0
	move.l	a3,d0
	move.l	d2,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	addq.l	#4,a7
	lea	72(a7),a0
	move.l	(a0),d1
	moveq	#4,d0
	cmp.l	d0,d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000956_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006
C68K_method_003ACopperStart_002EDos_003A06000956_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	move.l	a7,d0
	movea.l	a4,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600094B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000956_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006
C68K_method_003ACopperStart_002EDos_003A06000956_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	lea	72(a7),a0
	movea.l	100(a7),a1
	move.l	(a0),d0
	move.l	d0,(a1)
	move.l	4(a0),d0
	move.l	d0,4(a1)
	lea	80(a7),a7
	movem.l	(a7)+,d2/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A06000956_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	movea.l	100(a7),a0
	move.l	a0,-(a7)
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600096C
	addq.l	#4,a7
	lea	80(a7),a7
	movem.l	(a7)+,d2/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A06000955_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperStart_002EDos_003A06000956_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	movem.l	d2/a2-a4,-(a7)
	lea	-80(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	clr.l	16(a7)
	clr.l	20(a7)
	clr.l	24(a7)
	clr.l	28(a7)
	clr.l	32(a7)
	clr.l	36(a7)
	clr.l	40(a7)
	clr.l	44(a7)
	clr.l	48(a7)
	clr.l	52(a7)
	clr.l	56(a7)
	clr.l	60(a7)
	clr.l	64(a7)
	clr.l	68(a7)
	clr.l	72(a7)
	clr.l	76(a7)
	move.l	d1,d2
	movea.l	d0,a3
	movea.l	a1,a2
	movea.l	a0,a4
C68K_method_003ACopperStart_002EDos_003A06000955_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	move.l	a7,d0
	movea.l	a4,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600094A_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000955_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002
C68K_method_003ACopperStart_002EDos_003A06000955_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	lea	(a7),a0
	move.l	20(a0),d1
	moveq	#1,d0
	and.l	d0,d1
	tst.l	d1
	bne.s	C68K_method_003ACopperStart_002EDos_003A06000955_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003
C68K_method_003ACopperStart_002EDos_003A06000955_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	movea.l	100(a7),a0
	move.l	a0,-(a7)
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600096C
	addq.l	#4,a7
	lea	80(a7),a7
	movem.l	(a7)+,d2/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A06000955_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	lea	(a7),a0
	lea	24(a0),a1
	lea	72(a7),a0
	move.l	a0,-(a7)
	movea.l	a4,a0
	move.l	a3,d0
	move.l	d2,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	addq.l	#4,a7
	lea	72(a7),a0
	move.l	(a0),d1
	moveq	#4,d0
	cmp.l	d0,d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000955_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006
C68K_method_003ACopperStart_002EDos_003A06000955_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	move.l	a7,d0
	movea.l	a4,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600094B_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000955_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006
C68K_method_003ACopperStart_002EDos_003A06000955_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	lea	72(a7),a0
	movea.l	100(a7),a1
	move.l	(a0),d0
	move.l	d0,(a1)
	move.l	4(a0),d0
	move.l	d0,4(a1)
	lea	80(a7),a7
	movem.l	(a7)+,d2/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A06000955_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	movea.l	100(a7),a0
	move.l	a0,-(a7)
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600096C
	addq.l	#4,a7
	lea	80(a7),a7
	movem.l	(a7)+,d2/a2-a4
	rts
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000166_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000166_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
C68K_method_003ACopperStart_002EDos_003A06000955_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
	movea.l	a1,a0
	moveq	#0,d0
	move.l	(a0),d0
	movea.l	d0,a0
	rts
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000166_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000167_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000167_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	movea.l	a1,a0
	moveq	#4,d0
	move.l	4(a0),d0
	movea.l	d0,a0
	rts
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000167_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000168_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000168_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	movea.l	a1,a0
	moveq	#8,d0
	move.b	8(a0),d0
	andi.l	#$000000FF,d0
	andi.l	#$000000FF,d0
	rts
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000168_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000169_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000169_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	movea.l	a1,a0
	moveq	#10,d0
	move.l	10(a0),d0
	movea.l	d0,a0
	rts
C68K_method_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A06000169_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	d2-d6/a2-a4,-(a7)
	lea	-16(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	movea.l	d0,a4
	move.l	d1,d6
	movea.l	a1,a2
	movea.l	a0,a3
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	lea	(a7),a0
	move.l	a0,-(a7)
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600096C
	addq.l	#4,a7
	movea.l	a3,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	movea.l	a3,a0
	move.l	a4,d4
	move.l	d6,d0
	movea.l	a4,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000943_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	tst.l	d6
	beq.w	C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	movea.l	d4,a0
	move.l	d6,d0
	move.l	(a2),d2
	move.l	4(a2),d1
	movea.l	d2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000944
	tst.b	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	lea	(a7),a0
	movea.l	52(a7),a1
	move.l	(a0),d0
	move.l	d0,(a1)
	move.l	4(a0),d0
	move.l	d0,4(a1)
	lea	16(a7),a7
	movem.l	(a7)+,d2-d6/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B:
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0017:
	move.l	20(a2),d1
	move.l	24(a2),d0
	add.l	d0,d1
	move.l	4(a2),d0
	sub.l	d1,d0
	move.l	d0,d1
	move.l	d1,d0
	tst.l	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0011
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C:
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0018:
	move.l	8(a2),d3
	move.l	24(a2),d0
	sub.l	d0,d3
	move.l	d6,d0
	sub.l	d2,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000946
	move.l	d3,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000946
	move.l	d0,d3
	move.l	4(a2),d1
	move.l	16(a2),d0
	sub.l	d0,d1
	move.l	d3,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000946
	tst.l	d0
	beq.w	C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0019:
	move.l	d0,d3
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E:
	movea.l	d4,a4
	adda.l	d2,a4
	lea	(a2),a0
	move.l	(a0),d0
	movea.l	d0,a1
	move.l	16(a2),d0
	adda.l	d0,a1
	move.l	a3,d0
	move.l	d3,d1
	movea.l	a4,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000019
	move.l	16(a2),d0
	move.l	d3,d1
	move.l	4(a2),d5
	movea.l	d5,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000945
	move.l	d0,16(a2)
	lea	24(a2),a0
	move.l	(a0),d0
	add.l	d3,d0
	move.l	d0,(a0)
	add.l	d3,d2
	move.l	24(a2),d1
	move.l	8(a2),d0
	cmp.l	d0,d1
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010:
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001B:
	cmp.l	d6,d2
	bcs.w	C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0011:
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001C:
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001D:
	lea	8(a7),a0
	clr.l	0(a0)
	clr.l	4(a0)
	lea	8(a7),a0
	cmp.l	d6,d2
	beq.w	C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0013
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0012:
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001E:
	moveq	#1,d0
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0014:
	move.l	d0,(a0)
	lea	8(a7),a0
	move.l	d2,4(a0)
	lea	8(a7),a0
	movea.l	52(a7),a1
	move.l	(a0),d0
	move.l	d0,(a1)
	move.l	4(a0),d0
	move.l	d0,4(a1)
	lea	16(a7),a7
	movem.l	(a7)+,d2-d6/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F:
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001A:
	lea	20(a2),a0
	move.l	(a0),d1
	move.l	8(a2),d0
	add.l	d0,d1
	move.l	d1,(a0)
	moveq	#0,d0
	clr.l	24(a2)
	bra.s	C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0015:
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0016:
	move.l	28(a2),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	move.l	32(a2),d0
	tst.l	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
	lea	8(a7),a0
	clr.l	0(a0)
	clr.l	4(a0)
	lea	8(a7),a0
	moveq	#3,d0
	move.l	d0,(a0)
	lea	8(a7),a0
	movea.l	52(a7),a1
	move.l	(a0),d0
	move.l	d0,(a1)
	move.l	4(a0),d0
	move.l	d0,4(a1)
	lea	16(a7),a7
	movem.l	(a7)+,d2-d6/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
	tst.l	d6
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009:
	lea	8(a7),a0
	clr.l	0(a0)
	clr.l	4(a0)
	lea	8(a7),a0
	moveq	#0,d0
	clr.l	0(a0)
	lea	8(a7),a0
	movea.l	52(a7),a1
	move.l	(a0),d0
	move.l	d0,(a1)
	move.l	4(a0),d0
	move.l	d0,4(a1)
	lea	16(a7),a7
	movem.l	(a7)+,d2-d6/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A:
	moveq	#0,d2
	bra.w	C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D:
	movea.l	52(a7),a0
	move.l	a0,-(a7)
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600096C
	addq.l	#4,a7
	lea	16(a7),a7
	movem.l	(a7)+,d2-d6/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0013:
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB001F:
	moveq	#0,d0
	bra.w	C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0014
C68K_method_003ACopperStart_002EDos_003A0600093D_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	movem.l	d2-d7/a2-a4,-(a7)
	lea	-16(a7),a7
	clr.l	0(a7)
	clr.l	4(a7)
	clr.l	8(a7)
	clr.l	12(a7)
	movea.l	d0,a4
	move.l	d1,d2
	movea.l	a1,a2
	movea.l	a0,a3
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	lea	(a7),a0
	move.l	a0,-(a7)
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600096C
	addq.l	#4,a7
	movea.l	a3,a0
	movea.l	a2,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000942_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	movea.l	a3,a0
	move.l	a4,d6
	move.l	d2,d0
	movea.l	a4,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000943_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	tst.l	d2
	beq.w	C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	movea.l	d6,a0
	move.l	d2,d0
	move.l	(a2),d3
	move.l	4(a2),d1
	movea.l	d3,a1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000944
	tst.b	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	lea	(a7),a0
	movea.l	56(a7),a1
	move.l	(a0),d0
	move.l	d0,(a1)
	move.l	4(a0),d0
	move.l	d0,4(a1)
	lea	16(a7),a7
	movem.l	(a7)+,d2-d7/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F:
	move.l	4(a2),d1
	move.l	12(a2),d0
	sub.l	d0,d1
	move.l	d2,d0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000946
	move.l	d0,d3
	lea	(a2),a0
	move.l	(a0),d0
	movea.l	d0,a0
	move.l	12(a2),d0
	adda.l	d0,a0
	movea.l	d6,a1
	adda.l	d4,a1
	move.l	a3,d0
	move.l	d3,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000019
	move.l	12(a2),d0
	move.l	d3,d1
	move.l	4(a2),d5
	movea.l	d5,a0
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000945
	move.l	d0,12(a2)
	move.l	d4,d0
	add.l	d3,d0
	sub.l	d3,d2
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010:
	tst.l	d2
	beq.w	C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0011
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0016:
	move.l	d0,d4
	bra.s	C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000F
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0012:
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0013:
	move.l	28(a2),d0
	tst.l	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0006:
	lea	8(a7),a0
	clr.l	0(a0)
	clr.l	4(a0)
	lea	8(a7),a0
	moveq	#3,d0
	move.l	d0,(a0)
	lea	8(a7),a0
	movea.l	56(a7),a1
	move.l	(a0),d0
	move.l	d0,(a1)
	move.l	4(a0),d0
	move.l	d0,4(a1)
	lea	16(a7),a7
	movem.l	(a7)+,d2-d7/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0007:
	tst.l	d2
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0008:
	lea	8(a7),a0
	clr.l	0(a0)
	clr.l	4(a0)
	lea	8(a7),a0
	moveq	#0,d0
	clr.l	0(a0)
	lea	8(a7),a0
	movea.l	56(a7),a1
	move.l	(a0),d0
	move.l	d0,(a1)
	move.l	4(a0),d0
	move.l	d0,4(a1)
	lea	16(a7),a7
	movem.l	(a7)+,d2-d7/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0009:
	move.l	20(a2),d0
	tst.l	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000A:
	lea	8(a7),a0
	clr.l	0(a0)
	clr.l	4(a0)
	lea	8(a7),a0
	move.l	32(a2),d0
	tst.l	d0
	bne.s	C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000B:
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0014:
	moveq	#2,d0
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D:
	move.l	d0,(a0)
	lea	8(a7),a0
	movea.l	56(a7),a1
	move.l	(a0),d0
	move.l	d0,(a1)
	move.l	4(a0),d0
	move.l	d0,4(a1)
	lea	16(a7),a7
	movem.l	(a7)+,d2-d7/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000C:
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0015:
	moveq	#1,d0
	bra.s	C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000D
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB000E:
	move.l	d2,d0
	move.l	20(a2),d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A06000946
	move.l	d0,d7
	move.l	d7,d2
	moveq	#0,d0
	bra.w	C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0010
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0011:
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0017:
	lea	20(a2),a0
	move.l	(a0),d0
	sub.l	d7,d0
	move.l	d0,(a0)
	lea	8(a7),a0
	clr.l	0(a0)
	clr.l	4(a0)
	lea	8(a7),a0
	moveq	#0,d0
	clr.l	0(a0)
	lea	8(a7),a0
	move.l	d7,4(a0)
	lea	8(a7),a0
	movea.l	56(a7),a1
	move.l	(a0),d0
	move.l	d0,(a1)
	move.l	4(a0),d0
	move.l	d0,4(a1)
	lea	16(a7),a7
	movem.l	(a7)+,d2-d7/a2-a4
	rts
C68K_method_003ACopperStart_002EDos_003A0600093E_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000943_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform:
	move.l	d2,-(a7)
	subq.l	#4,a7
	move.l	a1,(a7)
	movea.l	a0,a1
	move.l	d0,d2
C68K_method_003ACopperStart_002EDos_003A06000943_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0000:
	tst.l	d2
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000943_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005
C68K_method_003ACopperStart_002EDos_003A06000943_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0001:
	move.l	(a7),d0
	tst.l	d0
	beq.s	C68K_method_003ACopperStart_002EDos_003A06000943_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A06000943_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0002:
	move.l	(a7),d1
	moveq	#-1,d0
	sub.l	d2,d0
	cmp.l	d0,d1
	bhi.s	C68K_method_003ACopperStart_002EDos_003A06000943_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004
C68K_method_003ACopperStart_002EDos_003A06000943_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0003:
	movea.l	(a7),a0
	move.l	a1,d0
	move.l	d2,d1
	bsr.w	C68K_method_003ACopperStart_002EDos_003A0600001A
	andi.l	#$000000FF,d0
	addq.l	#4,a7
	move.l	(a7)+,d2
	rts
C68K_method_003ACopperStart_002EDos_003A06000943_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0004:
	moveq	#0,d0
	addq.l	#4,a7
	move.l	(a7)+,d2
	rts
C68K_method_003ACopperStart_002EDos_003A06000943_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003ABB0005:
	moveq	#1,d0
	addq.l	#4,a7
	move.l	(a7)+,d2
	rts
C68K_method_003ACopperStart_002EDos_003A06000943_003Aconstructed_003Atype_003D_003Bmethod_003DCopperStart_002EDos_002ECopperSharpNativeDosPlatform_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000944:
	move.l	d2,-(a7)
	move.l	d3,-(a7)
	move.l	a1,d2
	move.l	a0,d3
C68K_method_003ACopperStart_002EDos_003A06000944_003ABB0000:
	add.l	d2,d1
	cmp.l	d1,d3
	bcc.s	C68K_method_003ACopperStart_002EDos_003A06000944_003ABB0002
C68K_method_003ACopperStart_002EDos_003A06000944_003ABB0001:
	add.l	d0,d3
	cmp.l	d3,d2
	scs	d0
	extb.l	d0
	neg.l	d0
	move.l	(a7)+,d3
	move.l	(a7)+,d2
	rts
C68K_method_003ACopperStart_002EDos_003A06000944_003ABB0002:
	moveq	#0,d0
	move.l	(a7)+,d3
	move.l	(a7)+,d2
	rts
C68K_method_003ACopperStart_002EDos_003A06000944_003Aend:
C68K_method_003ACopperStart_002EDos_003A06000946:
C68K_method_003ACopperStart_002EDos_003A06000946_003ABB0000:
	cmp.l	d1,d0
	bcs.s	C68K_method_003ACopperStart_002EDos_003A06000946_003ABB0002
C68K_method_003ACopperStart_002EDos_003A06000946_003ABB0001:
	move.l	d1,d0
	rts
C68K_method_003ACopperStart_002EDos_003A06000946_003ABB0002:
	rts
C68K_export_003Acopperstart_002Edos_002Equeue_002Dchannel_002Dlifecycle:
C68K_method_003ACopperStart_002EDos_003A06000946_003Aend:
	movem.l	d2-d7/a2-a6,-(a7)
	movea.l	$0004.w,a6
	move.l	a6,_ExecBase
	bsr.w	C68K_method_003A06000324
	movem.l	(a7)+,d2-d7/a2-a6
	rts

	section	rom_rodata,data
C68K_interface_003ACopperSharp_002ESdk_002EAmiga_002ESupport_003A02000003:
	dc.w	$0000
	dc.w	$0000
C68K_interface_003ACopperStart_002EDos_003A02000155:
	dc.w	$0000
	dc.w	$0000
C68K_interface_003ACopperStart_002EDos_003A02000158:
	dc.w	$0000
	dc.w	$0000
	dc.w	$0000

	section	ram_bss,bss
_ExecBase:
	ds.b	4
