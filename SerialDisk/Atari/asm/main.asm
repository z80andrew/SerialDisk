.include "../gemdos.asm"
.include "../bios.asm"

|-------------------------------------------------------------------------------

.equ hdv_init, 0x46a
.equ hdv_bpb, 0x472
.equ hdv_rw, 0x476
.equ hdv_mediach, 0x47e
.equ _drvbits, 0x4c2
.equ _dskbufp, 0x4c6

|-------------------------------------------------------------------------------

.text

|-------------------------------------------------------------------------------

start:
	move.l	4(sp),a0															| Our base page.
	move.l	0xc(a0),d7															| TEXT.
	add.l	0x14(a0),d7															| DATA.
	add.l	0x1c(a0),d7															| BSS.
	add.l	#0x100,d7															| Size of base page.

	jbsr	create_crc32_table

	Super	0
	move.l	d0,a0

	bset.b	#4,_drvbits+2.w														| Drive "M:".

	move.l	hdv_bpb.w,old_hdv_bpb
	move.l	hdv_rw.w,old_hdv_rw
	move.l	hdv_mediach.w,old_hdv_mediach

	move.l	#_hdv_bpb,hdv_bpb.w
	move.l	#_hdv_rw,hdv_rw.w
	move.l	#_hdv_mediach,hdv_mediach.w

	Super	(a0)

	Ptermres d7,#0

|-------------------------------------------------------------------------------

_hdv_bpb:
	move	4(sp),d0															| BIOS device number.
	move.l	old_hdv_bpb,a0
	lea		_bpb,a1

	jra		1f

_hdv_rw:
	move	14(sp),d0															| "dev" (BIOS device number).
	move.l	old_hdv_rw,a0
	lea		_rw,a1

	jra		1f

_hdv_mediach:
	move	4(sp),d0															| "dev" (BIOS device number).
	move.l	old_hdv_mediach,a0
	lea		_mediach,a1
1:
	cmp		#12,d0																| Drive "M:" is BIOS device number 12.
	jne		1f

	move.l	a1,a0
1:
	jmp		(a0)

|-------------------------------------------------------------------------------

_bpb:
	| Send the start communication magic.

	Bconout	#1,#0x18
	Bconout	#1,#0x03
	Bconout	#1,#0x20
	Bconout	#1,#0x06

	| Send the command.

	Bconout	#1,#3

	| Get the BPB.

	lea		disk_bpb,a3
	move	#9*2-1,d3
1:
	Bconin	#1
	move.b	d0,(a3)+

	dbf		d3,1b

	| Calculate the shift for sector size computation.

	move	#0x100,d0
	moveq	#8,d1
1:
	add		d0,d0
	addq	#1,d1

	cmp		disk_bpb,d0
	jne		1b

	move	d1,sector_size_shift_value

	move.l	#disk_bpb,d0

	rts

|-------------------------------------------------------------------------------

_rw:
	| Send the start communication magic.

	Bconout	#1,#0x18
	Bconout	#1,#0x03
	Bconout	#1,#0x20
	Bconout	#1,#0x06

	| Send the command.

	move	4(sp),d0															| "rwflag" (0: read, 1: write).
	Bconout	#1,d0

	| Send the start sector.

	Bconout	#1,#0
	Bconout	#1,#0

	move.b	12(sp),d0
	Bconout	#1,d0

	move.b	12+1(sp),d0
	Bconout	#1,d0

	| Send the number of sectors.

	Bconout	#1,#0
	Bconout	#1,#0

	move.b	10(sp),d0
	Bconout	#1,d0

	move.b	10+1(sp),d0
	Bconout	#1,d0

	moveq	#0,d3
	move	10(sp),d3															| "count" (number of sectors).
	move	sector_size_shift_value,d0
	lsl.l	d0,d3

	| Get the destination/source buffer address.

	move.l	6(sp),a3															| "buf" (buffer address).

	tst		4(sp)																| "rwflag" (0: read, 1: write).
	jeq		2f

	| Write data.
1:
	move.b	(a3)+,d0
	Bconout	#1,d0

	subq.l	#1,d3
	jne		1b

	clr.l	d0

	rts
2:
	| Read data.

	move.l	a3,a4
	move.l	d3,d4
1:
	Bconin	#1
	move.b	d0,(a3)+

	subq.l	#1,d3
	jne		1b

	| Receive remote CRC32 checksum.

	move	#4-1,d3
	lea		received_crc32,a3
1:
	Bconin	#1
	move.b	d0,(a3)+

	dbf		d3,1b

	| Send the end communication magic.

	Bconout	#1,#0x02
	Bconout	#1,#0x02
	Bconout	#1,#0x19
	Bconout	#1,#0x61

	| Calculate local CRC32 checksum.

	move.l	a4,a0
	move.l	d4,d0
	jbsr	calculate_crc32

	cmp.l	received_crc32,d0
	jne		_rw

	clr.l	d0

	rts

|-------------------------------------------------------------------------------

_mediach:
	| Send the start communication magic.

	Bconout	#1,#0x18
	Bconout	#1,#0x03
	Bconout	#1,#0x20
	Bconout	#1,#0x06

	| Send the command.

	Bconout	#1,#0x02

	| Get the media changed status.

	Bconin	#1
	and.l	#0xff,d0

	rts

|-------------------------------------------------------------------------------

create_crc32_table:
	lea		crc32_table,a0

	clr.l	d0
1:
	move.l	d0,d1

	moveq	#8-1,d2
2:
	add.l	d1,d1
	jcc		3f

	eor.l	#0x04c11db7,d1
3:
	dbf		d2,2b

	move.l	d1,(a0)+

	add.l	#0x01000000,d0
	jne		1b

	rts

|-------------------------------------------------------------------------------

| a0.l = buffer address.
| d0.l = buffer size.
|
| d0.l = CRC32 checksum.

calculate_crc32:
	move.l	d0,d7
	lea		crc32_table,a1
	clr.l	d0
1:
	rol.l	#8,d0
	moveq	#0,d1
	move.b	(a0)+,d1
	eor.b	d0,d1
	add		d1,d1
	add		d1,d1
	move.l	(a1,d1.w),d1
	clr.b	d0
	eor.l	d1,d0

	subq.l	#1,d7
	jne		1b

	rts

|-------------------------------------------------------------------------------

.data

|-------------------------------------------------------------------------------

|-------------------------------------------------------------------------------

.bss

|-------------------------------------------------------------------------------

disk_bpb:
	ds.w	9

sector_size_shift_value:
	ds.w	1

old_hdv_bpb:
	ds.l	1

old_hdv_rw:
	ds.l	1

old_hdv_mediach:
	ds.l	1

crc32_table:
	ds.l	256

received_crc32:
	ds.l	1

|-------------------------------------------------------------------------------

.end

|-------------------------------------------------------------------------------
