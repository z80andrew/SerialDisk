.include "../gemdos.asm"
.include "../bios.asm"

|-------------------------------------------------------------------------------

.equ hdv_init, 0x46a															| Vector to the initialisation routines for the floppy disk drives. (Unused?)
.equ hdv_bpb, 0x472																| Vector to routine that establishes the BPB of a BIOS drive.
.equ hdv_rw, 0x476																| Vector to the routine for reading and writing of blocks to BIOS drives.
.equ hdv_mediach, 0x47e															| Vector to routine for establishing the media-change status of a BIOS drive. The BIOS device number is passed on the stack (4(sp)).
.equ _hdv_boot, 0x47a															| Vector to the routine for loading the boot sector.
.equ _drvbits, 0x4c2															| Bit-table for the mounted drives of the BIOS.
.equ _dskbufp, 0x4c6															| Pointer to a 1024-byte buffer for reading and writing to floppy disks or hard drives. (Unused?)
																				| Also used by the VDI.

|-------------------------------------------------------------------------------

.text

|-------------------------------------------------------------------------------

start:
	move.l	4(sp),a0															| Our base page.
	move.l	0xc(a0),d7															| TEXT. (Code to execute)
	add.l	0x14(a0),d7															| DATA. (Initialised vars)
	add.l	0x1c(a0),d7															| BSS. (Uninitialised vars)
	add.l	#0x100,d7															| Size of base page. (always 0x100)

	jbsr	create_crc32_table													| Jump to subroutine create_crc32_table

	Super	0
	move.l	d0,a0																| Move d0 to a0

	bset.b	#4,_drvbits+2.w														| Drive "M:".

	move.l	hdv_bpb.w,old_hdv_bpb												| Move a word of hdv_bpb to old_hdv_bpb so we can call the stock method if needed
	move.l	hdv_rw.w,old_hdv_rw													| Move a word of hdv_rw to old_hdv_rw so we can call the stock method if needed
	move.l	hdv_mediach.w,old_hdv_mediach										| Move a word of hdv_mediach to old_hdv_mediach so we can call the stock method if needed

	move.l	#_hdv_bpb,hdv_bpb.w													| Move address of label _hdv_bpb to a word of hdv_bpb to override the default method
	move.l	#_hdv_rw,hdv_rw.w													| Move address of label _hdv_rw to a word of hdv_rw to override the default method
	move.l	#_hdv_mediach,hdv_mediach.w											| Move address of label _hdv_mediach to a word of hdv_mediach to override the default method

	Super	(a0)

	Ptermres d7,#0																| Terminate and stay resident

|-------------------------------------------------------------------------------

_hdv_bpb:
	move	4(sp),d0															| BIOS (disk) device number. Move word from offset 4 of SP address value into d0.
	move.l	old_hdv_bpb,a0														| Move address of stock hdv_bpb into a0
	lea		_bpb,a1																| Load the address of custom _bpb into a1

	jra		1f																	| Jump relative address - forward to label 1:

_hdv_rw:
	move	14(sp),d0															| "dev" (BIOS (disk) device number). Move word from offset 14 of SP address value into d0.
	move.l	old_hdv_rw,a0														| Move address of stock hdv_rw into a0
	lea		_rw,a1																| Load the address of custom _rw into a1

	jra		1f																	| Jump relative address - forward to label 1:

_hdv_mediach:
	move	4(sp),d0															| "dev" (BIOS (disk) device number). Move word from offset 4 of SP address value into d0.
	move.l	old_hdv_mediach,a0													| Move address of stock hdv_mediach into a0
	lea		_mediach,a1															| Load the address of custom _mediach into a1
1:
	cmp		#12,d0																| Drive "M:" is BIOS device number 12. Compare 12 with d0.
	jne		1f																	| Not drive M, jump relative address - forward to label 1:

	move.l	a1,a0																| Drive M, move a1 to a0 (use custom method)
1:
	jmp		(a0)																| Jump to address pointed by a0

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

	dbf		d3,1b																| Decrement d3; jump backwards to label 1: while d3 is not zero

	| Calculate the shift for sector size computation.

	move	#0x100,d0
	moveq	#8,d1
1:
	add		d0,d0
	addq	#1,d1

	cmp		disk_bpb,d0
	jne		1b																	| Jump if not equal - backwards to label 1:

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
	jeq		2f																	| Jump if equal - forwards to label 2:

	| Write data.
1:
	move.b	(a3)+,d0
	Bconout	#1,d0

	subq.l	#1,d3
	jne		1b																	| Jump if not equal - backwards to label 1:

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
	jne		1b																	| Jump if not equal - backwards to label 1:

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
	jne		_rw																	| Jump if not equal - to label _rw

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
