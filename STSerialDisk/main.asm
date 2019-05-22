.include "../gemdos.asm"
.include "../bios.asm"
.include "../xbios.asm"

.macro SendStartMagic
	Bconout	#1,#0x18
	Bconout	#1,#0x03
	Bconout	#1,#0x20
	Bconout	#1,#0x06
.endm

|-------------------------------------------------------------------------------

|.equ hdv_init, 		0x46a															| Vector to the initialisation routines for the floppy disk drives. (Unused?)
.equ hdv_bpb, 		0x472															| Vector to routine that establishes the BPB of a BIOS drive.
.equ hdv_rw, 		0x476															| Vector to the routine for reading and writing of blocks to BIOS drives.
.equ hdv_mediach, 	0x47e															| Vector to routine for establishing the media-change status of a BIOS drive. The BIOS device number is passed on the stack (4(sp)).
|.equ _hdv_boot, 	0x47a															| Vector to the routine for loading the boot sector.
.equ _drvbits, 		0x4c2															| Bit-table for the mounted drives of the BIOS.
|.equ _dskbufp, 		0x4c6															| Pointer to a 1024-byte buffer for reading and writing to floppy disks or hard drives. (Unused?)
																					| Also used by the VDI.

.equ cmd_read, 		0x00
.equ cmd_write, 	0x01
.equ cmd_mediach,	0x02
.equ cmd_bpb, 		0x03

|-------------------------------------------------------------------------------

.text

|-------------------------------------------------------------------------------

start:
	move.l	4(sp),a0															| Our base page.
	add.l	#0x100,d7															| Size of base page. (always 0x100)
	move.l	0xc(a0),d7															| TEXT. (Code to execute)
	add.l	0x14(a0),d7															| DATA. (Initialised vars)
	add.l	0x1c(a0),d7															| BSS. (Uninitialised vars)

	jbsr	create_crc32_table													| Jump to subroutine create_crc32_table

	Super	0																	| Enable supervisor mode?
	move.l	d0,a0																| Move d0 to a0 - why?

	bset.b	#4,_drvbits+2.w														| Set drive "M:" as mounted

	move.l	hdv_bpb.w,old_hdv_bpb												| Move a word of hdv_bpb to old_hdv_bpb so we can call the stock method when needed
	move.l	hdv_rw.w,old_hdv_rw													| Move a word of hdv_rw to old_hdv_rw so we can call the stock method when needed
	move.l	hdv_mediach.w,old_hdv_mediach										| Move a word of hdv_mediach to old_hdv_mediach so we can call the stock method when needed

	move.l	#_hdv_bpb,hdv_bpb.w													| Move address of label _hdv_bpb to a word of hdv_bpb to override the default method
	move.l	#_hdv_rw,hdv_rw.w													| Move address of label _hdv_rw to a word of hdv_rw to override the default method
	move.l	#_hdv_mediach,hdv_mediach.w											| Move address of label _hdv_mediach to a word of hdv_mediach to override the default method

	Super	(a0)																| Disable supervisor mode?

	Cursconf #0,#0																| Hide cursor, 0 blink rate
	jbsr	welcome_message

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
	cmp		#12,d0																| Drive "M:" is BIOS device number 12. Compare 12 with device number in d0
	jne		1f																	| Not drive M, jump forward to label 1:

	move.l	a1,a0																| Drive M, move a1 to a0 (use custom method)
1:
	jmp		(a0)																| Jump to address pointed by a0

|-------------------------------------------------------------------------------

_bpb:
	SendStartMagic

	| Send the command.

	Bconout	#1,#cmd_bpb

	| Get the BPB.

	lea		disk_bpb,a3															| Load the address of label disk_bpb into a3
	move	#9*2-1,d3															| Move the length of bpb into d3 (9 parameters of 2 bytes each, -1 for 0-based index)
1:
	Bconin	#1																	| Read a byte from BIOS console serial
	move.b	d0,(a3)+															| Move the received byte from d0 into a3, increment a3

	dbf		d3,1b																| Decrement d3; jump backwards to label 1: while d3 is not zero

	| Calculate the shift for sector byte size computation

	move	#0x100,d0															| Move 256 (initial sector size value) into d0
	moveq	#8,d1																| Move 8 (initial left shift value == *256) into d1
1:
	add		d0,d0																| Double the value of d0 (i.e. shift 1 bit left)
	addq	#1,d1																| Increase the required left bit shift by 1

	cmp		disk_bpb,d0															| Compare device sector size (bpb offset 0, 2 bytes) with sector size in d0
	jne		1b																	| Jump if not equal - backwards to label 1:

	move	d1,sector_size_shift_value											| Move the left shift value into a variable

	move.l	#disk_bpb,d0														| Move the address of label disk_bpb into d0

	rts																			| Return to caller

|-------------------------------------------------------------------------------

_rw:
	SendStartMagic

	| Send the command.

	move	4(sp),d0															| "rwflag" (0: read, 1: write).
	Bconout	#1,d0																| Send read or write command

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

	moveq	#0,d3																| clear d3
	move	10(sp),d3															| move "count" (number of sectors) to d3
	move	sector_size_shift_value,d0											| move sector shift (in bits) to d0
	lsl.l	d0,d3																| shift left, i.e. multiply number of sectors by bytes per sector to get total bytes

	| Get the destination/source buffer address.

	move.l	6(sp),a3															| "buf" (1024KiB buffer address).
	|move.l	disk_buffer,a3														| "buf" (8192KiB buffer address).

	tst		4(sp)																| set flags based on value of "rwflag" (0: read, 1: write).
	jeq		2f																	| Jump if equal (zero) - forwards to label 2:

	| Write data.
1:
	move.b	(a3)+,d0															| move buffer address into d0, increment to next byte in rw struct
	Bconout	#1,d0																| Write byte to serial

	subq.l	#1,d3																| subtract 1 from number of sectors
	jne		1b																	| Jump if number of sectors != 0 - backwards to label 1:

	clr.l	d0

	rts
2:
	| Read data.

	move.l	a3,a4																| Move address of received data into a4
	move.l	d3,d4																| Move length of received data into d4
1:
	Bconin	#1																	| Read byte from serial
	move.b	d0,(a3)+															| Add read byte to buffer, increment address to next buffer byte

	subq.l	#1,d3																| Subtract 1 from number of sectors
	jne		1b																	| Jump if number of sectors != 0 - backwards to label 1:

	| Receive remote CRC32 checksum.

	move	#4-1,d3																| Move counter into d3 (There are 4 bytes to read, -1 for 0-based index)
	lea		received_crc32,a3													| Load address of label received_crc32 into a3
1:
	Bconin	#1																	| Read a byte from BIOS console serial
	move.b	d0,(a3)+

	dbf		d3,1b																| Decrement d3, jump backwards to label 1: if not zero

	| Send the end communication magic.

	Bconout	#1,#0x02
	Bconout	#1,#0x02
	Bconout	#1,#0x19
	Bconout	#1,#0x61

	| Calculate local CRC32 checksum.

	move.l	a4,a0																| Move address of received data into a0
	move.l	d4,d0																| Move length of received data into d0
	jbsr	calculate_crc32														| Get CRC32 from subroutine

	cmp.l	received_crc32,d0													| Compare calculated CRC32 in d0 with received CRC32
	jne		_rw																	| Jump if not equal - to label _rw

	clr.l	d0																	| Clear d0

	rts																			| Return to caller

|-------------------------------------------------------------------------------

_mediach:
	SendStartMagic

	| Send the command.

	Bconout	#1,#cmd_mediach

	| Get the media changed status.

	Bconin	#1
	and.l	#0xff,d0

	rts

|-------------------------------------------------------------------------------

| Sends the start communication "magic numbers" to SerialDisk
send_start_magic:
	Bconout	#1,#0x18
	Bconout	#1,#0x03
	Bconout	#1,#0x20
	Bconout	#1,#0x06

	rts

|-------------------------------------------------------------------------------

create_crc32_table:
	lea		crc32_table,a0

	clr.l	d0																	| clear d0
1:
	move.l	d0,d1																| clear d1

	moveq	#8-1,d2																| put bit counter in d2
2:
	add.l	d1,d1																| double d1 (shift left 1 bit)
	jcc		3f																	| if zero, skip polynomial XOR

	eor.l	#0x04c11db7,d1														| XOR polynomial with d1
3:
	dbf		d2,2b																| decrement loop counter, process next bit

	move.l	d1,(a0)+															| put result into table, increment a0 to next table entry

	add.l	#0x01000000,d0														| add 0x01000000 to d0 (0xFF*0x01000000 == 0x100000000 which overflows to 0x0)
	jne		1b																	| haven't reached end of table, process next table entry

	rts

|-------------------------------------------------------------------------------
| a0.l = buffer address.
| d0.l = buffer size.
|
| d0.l = CRC32-POSIX checksum.

calculate_crc32:
	move.l	d0,d7																| Move long d0 into d7 (data length)
	lea		crc32_table,a1														| Load address of crc32_table into a1
	clr.l	d0																	| Clear d0
1:
	rol.l	#8,d0																| Rotate d0 left 8 bits
	moveq	#0,d1																| Set value of d1 to 0
	move.b	(a0)+,d1															| Move a byte of a0 into d1 then increment a0
	eor.b	d0,d1																| Exclusive OR a byte of d0 and d1
	add		d1,d1																| Double the value of d1 (shift left)
	add		d1,d1																| Double the value of d1 (shift left)
	move.l	(a1,d1.w),d1														| offset into table entry d1 and move the value into d1
	clr.b	d0																	| Clear a byte of d0
	eor.l	d1,d0																| Exclusive OR long of d1 and d0

	subq.l	#1,d7																| Subtract 1 from d7 (remaining data length)
	jne		1b																	| Jump if not zero - backwards to label 1:

	eor.l   #0xFFFFFFFF,d0       												| invert checksum

	rts																			| Return to caller

welcome_message:
	Cconws	welcome_string

	move.w #0x32,d1																| 50 = 1 second at 50Hz
1:
	|move.w #37,-(a7) 															| Set up wait for vertical blank (50Hz on PAL)
 	|trap #14 																	| Call XBIOS - wait for vblank

 	dbf d1,1b 																	| If counter != 0, loop

	rts

|-------------------------------------------------------------------------------

.data

|-------------------------------------------------------------------------------

welcome_string:
	.asciz	"SerialDisk v2.2\r\nConfigured on drive M"

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

disk_buffer:
	ds.b	8192																| 8192KiB is the maximum sector size
																				| Can this be replaced with a Malloc()?

|-------------------------------------------------------------------------------

.end

|-------------------------------------------------------------------------------
