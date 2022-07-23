.include "../macro/gemdos.asm"
.include "../macro/bios.asm"
.include "../macro/xbios.asm"
.include "../src/constants.asm"

|-------------------------------------------------------------------------------

.text

|-------------------------------------------------------------------------------

start:
	move.l	4(sp),a0															| Our base page.
	move.l	0xc(a0),d7															| TEXT. (Code to execute)
	add.l	0x14(a0),d7															| DATA. (Initialised vars)
	add.l	0x1c(a0),d7															| BSS. (Uninitialised vars)
	add.l	#0x100,d7															| Size of base page. (always 0x100)

	| Free unused memory

	move.l    d7,-(sp)      													| Return to the stack
    move.l    a0,-(sp)      													| Basepage address to stack
    clr.w     -(sp)         													| Fill parameter
    move.w    #0x4a,-(sp)    													| Set command Mshrink
    trap      #1            													| Call GEMDOS
    lea       0xc(sp),sp     													| Correct stack

	Cursconf #0,#0																| Hide cursor, 0 blink rate

	movea.l	#str_welcome,a0
	jbsr	print_string
	jbsr	create_crc32_table

	| Check resource file is present
	Fopen	const_res_filename,#0												| Attempt to open resource file
	tst.w	d0																	| Check return value
	jpl		1f																	| Result positive, file found

	movea.l	#str_err_res_not_found,a0											| Display the resource file not found message
	jbsr	print_string
	movea.l	#str_press_any_key,a0												| Display the resource file not found message
	jbsr	print_string

	Cconin

	Pterm	#0
1:


	| Read config file

	jbsr	read_config_file
	tst.w	d0																	| Test config file valid
	jpl		read_config_file_done												| Result positive = config file valid

	| Config file is invalid

	move	d0,d3

	move.w	#err_prefix,d0
	jbsr	print_resource_string
	move.w	#err_config_invalid,d0
	jbsr	print_resource_string

	cmp		#err_disk_id_out_of_range,d3
	jne		1f
	move.w	#err_disk_id,d0
	jbsr	print_resource_string
	jmp		2f
1:
	cmp		#err_sector_size_out_of_range,d3
	jne		2f
	move.w	#err_sector_size,d0
	jbsr	print_resource_string
2:

err_config_file_end:
	move.w	#msg_press_any_key,d0
	jbsr	print_resource_string

	Cconin

	Pterm 	#0

read_config_file_done:
	| Mount drive

	Supexec	mount_drive
	tst.w	d0																	| Test drive mounted successfully
	jpl		2f																	| Result positive = drive mounted successfully

	| Drive is already mounted

	move.w	#err_prefix,d0
	jbsr	print_resource_string
	move.w	disk_identifier,d0													| Move disk_id into d0
	addi.w	#ascii_alpha_offset,d0												| Convert to ASCII representation
	Cconout	d0

	move.w	#err_drive_already_mounted,d0
	jbsr	print_resource_string
	move.w	#msg_press_any_key,d0
	jbsr	print_resource_string

	Cconin

	Pterm 	#0
2:
	| Allocate disk buffers

	Supexec allocate_buffers

	tst.w	d0																	| Test buffer allocated successfully
	jpl		3f																	| Result positive = buffer allocated successfully

	| Buffers could not be allocated

	move.w	#err_prefix,d0
	jbsr	print_resource_string
	move.w	#err_buffer_allocation,d0
	jbsr	print_resource_string
	move.w	#msg_press_any_key,d0
	jbsr	print_resource_string

	Cconin

	Pterm 	#0

3:
	| Drive mounted successfully

	move.w	#msg_drive_mounted,d0
	jbsr	print_resource_string

	move.w	disk_identifier,d0													| Move disk_id into d0
	addi.w	#ascii_alpha_offset,d0												| Convert to ASCII representation
	Cconout	d0

	Supexec	config_drive_rw

4:
	| Determine screen refresh rate
	Supexec	set_refresh_rate

	Supexec wait

start_end:
	Ptermres d7,#0

|-------------------------------------------------------------------------------
| Reads display refresh rate and stores it in memory to be used for
| timing calculations
|
| Input
|
| Output
| variables: refresh_rate
|
| Corrupts
|

set_refresh_rate:
	move.b	(screenres), d0
	cmp.b	#screenres_high, d0
	jne		not_hires
	move.w	#hires_hz, refresh_rate
	jmp		set_refresh_rate_end
not_hires:
	move.b	(palmode), d0
	tst.b	d0																	| 0 = NTSC, otherwise PAL
	jne		pal
	move.w	#ntsc_hz, refresh_rate
	jmp		set_refresh_rate_end
pal:
	move.w	#pal_hz, refresh_rate
set_refresh_rate_end:
	rts

|-------------------------------------------------------------------------------
| Replace pointers for default disk routines with pointers
| to custom routines
|
| Input
|
| Output
| variables: old_hdv_bpb, old_hdv_rw, old_hdv_mediach
|
| Corrupts
|

config_drive_rw:
	move.l	hdv_bpb.w,old_hdv_bpb												| Move a word of hdv_bpb to old_hdv_bpb so we can call the stock method when needed
	move.l	hdv_rw.w,old_hdv_rw													| Move a word of hdv_rw to old_hdv_rw so we can call the stock method when needed
	move.l	hdv_mediach.w,old_hdv_mediach										| Move a word of hdv_mediach to old_hdv_mediach so we can call the stock method when needed

	move.l	#_hdv_bpb,hdv_bpb.w													| Move address of label _hdv_bpb to a word of hdv_bpb to override the default method
	move.l	#_hdv_rw,hdv_rw.w													| Move address of label _hdv_rw to a word of hdv_rw to override the default method
	move.l	#_hdv_mediach,hdv_mediach.w											| Move address of label _hdv_mediach to a word of hdv_mediach to override the default method
rts

|-------------------------------------------------------------------------------
| Modified _hdv_bpb, _hdv_rw and _hdv_mediach
| Jumps to default or custom routine address depending on drive ID
|
| Input
| variables: old_hdv_bpb, old_hdv_rw, old_hdv_mediach, disk_identifier
|
| Output
|
| Corrupts
| a0, a1

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
	cmp.w	disk_identifier,d0													| Compare drive id with device number in d0
	jne		1f																	| Drive ids don't match, jump forward to label 1:

	move.l	a1,a0																| Drive ids match, move a1 to a0 (use custom method)
1:
	jmp		(a0)																| Jump to address pointed by a0

|-------------------------------------------------------------------------------
| Sends the Get BIOS Parameter Block command over serial and
| reads the result
|
| Input
|
| Output
| variables: disk_bpb, sector_size_shift_value
| Address of received BPB
|
| Corrupts
| d1, d3
| a3

_bpb:
	jbsr send_start_magic

	| Send the command.

	move.b	#cmd_bpb,d0
	jbsr 	write_serial

	| Get the BPB.

	lea		disk_bpb,a3															| Load the address of label disk_bpb into a3
	move	#9*2-1,d3															| Move the length of bpb into d3 (9 parameters, 2 bytes each, -1 for 0-based index)
1:
	jbsr	read_serial
	cmp.w	#-1,d0																| Check for error
	jne		2f
	move.l	#0,d0																| Returning -1 as an error causes a crash, so set to 0
	rts

2:
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

	move.l	#disk_bpb,d0														| Move the address of populated disk_bpb into d0
99:
	rts																			| Return to caller

|-------------------------------------------------------------------------------
| Sends the Read/Write command over serial and reads the result
|
| Input
| variables: sector_size_shift_value
|
| Output
| 0 on success
| -1 on error
|
| Corrupts
| variables: temp_long
| d3, d4
| a3, a4

_rw:
	jbsr send_start_magic

	| Send the command.

	move	4(sp),d0															| "rwflag" (0: read, 1: write).
	jbsr	write_serial														| Send read or write command

	| Send the start sector.

	move.b	12(sp),d0
	jbsr	write_serial

	move.b	12+1(sp),d0
	jbsr	write_serial

	| Send the number of sectors.

	move.b	10(sp),d0
	jbsr	write_serial

	move.b	10+1(sp),d0
	jbsr	write_serial

	| Get the destination/source buffer address.

	move.l	6(sp),a4															| buffer address
	clr.l	d3
	move	10(sp),d3															| Move "count" (number of sectors) to d3
	move	sector_size_shift_value,d0											| Move sector shift (num. bits) to d0
	lsl.l	d0,d3																| Shift "count" left, i.e. multiply number of sectors by bytes per sector to get total bytes

	tst		4(sp)																| Set flags based on value of "rwflag" (0: read, 1: write).
	jeq		_rw_read

_rw_write:
	movem.l	d3/a4,-(sp)															| Push buffer address and data length to the stack

	jbsr	read_serial															| Receive serial data flags
	tst.w	d0
	jmi		99f

	btst	#compression_isenabled,d0											| Check for compression flag
	jeq		_rw_write_uncompressed

	.include "../src/RLE.asm"
	jmp	_rw_write_crc32
_rw_write_uncompressed:
	move.b	(a4)+,d0															| Move buffer address into d0, increment to next byte in rw struct
	jbsr	write_serial														| Write byte to serial
	subq.l	#1,d3																| Decrement number of bytes remaining
	jne		_rw_write_uncompressed
_rw_write_crc32:
	movem.l	(sp)+,d3/a4															| Pop buffer address and data length off the stack
	move.l	d3,d0
	move.l	a4,a0

	jbsr	calculate_crc32														| Get CRC32
	move.l	d0,temp_long
	| Send CRC32 checksum.
	move	#4-1,d4																| Copy byte count into counter (There are 4 bytes to read, -1 for 0-based index)
	lea		temp_long,a3														| Load address to store CRC32 checksum
1:
	move.b	(a3)+,d0
	jbsr	write_serial
	dbf		d4,1b

	jbsr	read_serial															| Receive CRC32 comparison result
	tst.b	d0
	jeq		_rw_write															| CRC32 mismatch, resend data
	jmi		99f																	| Serial receive error
_rw_write_end:
	clr.l	d0																	| Success return value
99:
	rts

_rw_read:
	movem.l	d3,-(sp)															| Push uncompressed data length on to stack

	move.l	a4,a5																| Copy destination address so it can be used again later

	jbsr	read_serial															| Receive serial data flags
	tst.w	d0
	jmi		99f

	btst	#compression_isenabled,d0											| Check for compression flag

	jeq		2f

	| Receive compressed data length

	move	#4-1,d3																| Copy byte count into counter (There are 4 bytes to read, -1 for 0-based index)
	lea		temp_long,a3														| Load address to store compressed data length
1:
	jbsr	read_serial
	tst.w	d0
	jmi		99f

	move.b	d0,(a3)+

	dbf		d3,1b

	| Receive compressed data bytes
	jbsr	lz4_depack
	tst		d0
	jmi		99f

	jmp		3f

2:
	| Receive uncompressed data bytes

	jbsr	read_serial
	tst.w	d0
	jmi		99f
	move.b	d0,(a5)+															| Add read byte to buffer, increment address to next buffer byte

	subq.l	#1,d3																| Subtract 1 from number of sectors
	jne		2b																	| Jump if number of sectors != 0 - backwards to label 1:

3:
	| Receive remote CRC32 checksum.

	move	#4-1,d3																| Copy byte count into counter (There are 4 bytes to read, -1 for 0-based index)
	lea		temp_long,a3														| Load address to store CRC32 checksum
4:
	jbsr	read_serial
	tst.w	d0
	jmi		99f
	move.b	d0,(a3)+

	dbf		d3,4b

	| Calculate local CRC32 checksum.

	move.l	a4,a0																| Copy address of received data
	movem.l	(sp)+,d0															| Pop uncompressed data length off the stack

	jbsr	calculate_crc32														| Get CRC32 from subroutine

	cmp.l	temp_long,d0														| Compare calculated CRC32 with received CRC32
	jne		_rw																	| Retry read / write if CRCs do not match

	clr.l	d0																	| Success return value
99:
	rts


|-------------------------------------------------------------------------------
| Media changed status. Always 0 (not changed).

_mediach:
	moveq.l	#0,d0
	rts

|-------------------------------------------------------------------------------
| Mounts a disk drive
|
| Input
| variables: disk_identifier
|
| Output
| ID of mounted drive
| -1 if drive already mounted
|
| Corrupts
| d1

mount_drive:
	clr.l	d0
	move.w	disk_identifier,d0													| Copy the virtual disk id
	move.l	_drvbits,d1															| Load the list of mounted drives
	btst	d0,d1																| Has this drive id already been mounted?

	jeq		1f																	| Result is 0; this drive is not mounted

	move.w	#-1,d0																| Failure return value
	jmp 	99f
1:
	bset.l	d0,d1																| Set the bit at location d0 in mounted drives (always long-word between registers)
	move.l	d1,_drvbits															| Put the updated list of mounted drives into memory
99:
	rts

|-------------------------------------------------------------------------------
| Sends the start communication command to SerialDisk
|
| Input
|
| Output
|
| Corrupts
|

send_start_magic:
	move.b		#0x18,d0
	jbsr		write_serial
	moveq		#0x03,d0
	jbsr		write_serial
	move.b		#0x20,d0
	jbsr		write_serial
	moveq		#0x06,d0
	jbsr		write_serial

	rts

|-------------------------------------------------------------------------------
| Creates a CRC32/POSIX checksum lookup table in memory
|
| Input
|
| Output
| variables: crc32_table
|
| Corrupts
| d0, d1, d2
| a0

create_crc32_table:
	lea		crc32_table,a0

	clr.l	d0																	| Clear d0
1:
	move.l	d0,d1																| Clear d1

	moveq	#8-1,d2																| Put bit counter in d2
2:
	add.l	d1,d1																| Double d1 (shift left 1 bit)
	jcc		3f																	| If zero, skip polynomial XOR

	eor.l	#crc32_poly,d1														| XOR polynomial with d1
3:
	dbf		d2,2b																| Decrement loop counter, process next bit

	move.l	d1,(a0)+															| Put result into table, increment to next table entry

	add.l	#0x01000000,d0														| Add 0x01000000 to d0 (0xFF*0x01000000 == 0x100000000 which overflows to 0x0)
	jne		1b																	| Haven't reached end of table, process next table entry

	rts

|-------------------------------------------------------------------------------
| Calculates a CRC32/POSIX checksum
|
| Input
| variables: crc32_table
| a0.l = data address
| d0.l = data length
|
| Output
| CRC32/POSIX checksum.
|
| Corrupts
| d1, d7
| a0, a1

calculate_crc32:
	move.l	d0,d7																| Copy data length
	lea		crc32_table,a1														| Load address of precomputed CRC32 table
	clr.l	d0
1:
	rol.l	#8,d0																| Rotate d0 left 8 bits
	moveq	#0,d1																| Set value of d1 to 0
	move.b	(a0)+,d1															| Move a byte of a0 into d1 then increment a0
	eor.b	d0,d1																| Exclusive OR a byte of d0 and d1
	add		d1,d1																| Double the value of d1
	add		d1,d1																| Double the value of d1
	move.l	(a1,d1.w),d1														| offset into table entry d1 and move the value into d1
	clr.b	d0																	| Clear a byte of d0
	eor.l	d1,d0																| Exclusive OR long of d1 and d0

	subq.l	#1,d7																| Subtract 1 from remaining data length
	jne		1b

	eor.l   #0xFFFFFFFF,d0       												| Invert checksum

	rts

|-------------------------------------------------------------------------------
| Pauses application for a set period
|
| Input
|
| Output
|
| Corrupts
| d5, d6
| a5

wait:
    lea     _vbclock,a5
	move.l  (a5),d5      														| Store current VBL count

	clr.l	d0
	move.w	refresh_rate, d0
	mulu.w	#wait_secs, d0

	add.l	d0, d5																| Increase to target VBL count

1:
	lea     _vbclock,a5
	move.l  (a5),d6      														| Current VLBANKs

	cmp.l    d5,d6       														| Compare max VLBANKs with current VLBANKs
	jgt	     2f     															| Timeout if current VLBANKs is greater than max VLBANKs

	jra      1b        															| Loop if current VLBANKs is less than max VLBANKs
2:
	rts

|-------------------------------------------------------------------------------
| Checks for availability of serial data in buffer for a set period
|
| Input
|
| Output
| 0 if data in buffer
| -1 if no data found in buffer within period
|
| Corrupts
| d6, d7
| a6

read_serial:
	movem.l	d1-d2,-(sp)															| Push registers to the stack which are affected by BIOS calls
    lea     _vbclock,a6
	move.l  (a6),d6      														| Store current VLBANK count

	clr.l	d0
	move.w	refresh_rate, d0
	mulu.w	#serial_timeout_secs, d0

	add.l	d0, d6																| Increase to target VLBANK count
1:
    Bconstat serial_device														| Read the serial buffer state
	tst     d0           														| Test for presence of data in buffer
	jne     3f     		 														| There is data - stop checking

	move.l  (a6),d7      														| Current VLBANKs

	cmp.l   d6,d7       														| Compare max VLBANKs with current VLBANKs
	jgt	    2f     																| Timeout if current VLBANKs is greater than max VLBANKs

	jra     1b        															| Check serial status again if current VLBANKs is less than max VLBANKs
2:
	move	#-1,d0
	jmp		99f
3:
	Bconin	serial_device														| Read byte from serial port
99:
	movem.l	(sp)+,d1-d2															| Restore registers affected by BIOS calls
	rts

|-------------------------------------------------------------------------------
| Writes a byte to the serial port
|
| Input
| d0.b	byte to send
|
| Output
|
| Corrupts
| d1, d2 corrupted by BIOS calls
write_serial:
	move	d0,-(sp)
	move.w	serial_device,d0
	move	d0,-(sp)
	move	#3,-(sp)
	trap	#13
	addq.l	#6,sp
	rts

|-------------------------------------------------------------------------------
| Reads configuration file if available and sets variables
|
| Input
|
| Output
| variables: sector_size_shift_value, disk_identifier
| 0 on success
| -1 on error
|
| Corrupts
| variables: disk_bpb
| d1

read_config_file:
	move	#0x4d,disk_identifier												| Set default disk id as ASCII 'M'
	move	#0x0d,sector_size_shift_value										| Set default sector size shift
	move	#0x31,serial_device													| Set default serial device to ASCII '1'

	Fopen	const_config_filename,#0											| Attempt to open config file
	tst.w	d0																	| Check return value
	jmi		1f																	| Return value is negative (failed), skip read attempt
	Fread	d0,#3,temp_long														| Read first 3 bytes into temp variable
	Fclose	d0																	| Close the file handle

	move.w	#msg_config_found,d0												| Display the config file found message
	jbsr	print_resource_string

	| Read disk ASCII ID

	move.b	temp_long,disk_identifier+1

	cmp.w	#0x50,disk_identifier												| Compare read byte with ASCII 'P'
	jgt		config_drive_err													| Read character is > ASCII 'P' so it is invalid

	cmp.w	#0x43,disk_identifier												| Compare read byte with ASCII 'C'
	jlt		config_drive_err													| Read character is < ASCII 'C' so it is invalid

	| Read disk buffer size

	clr		d1
	move.b	temp_long+1,d1

	cmp		#0x34,d1															| Compare read byte with ASCII '4'
	jgt		sector_size_err														| Read character is > ASCII 4 so it is invalid

	cmp		#0x30,d1															| Compare read byte with ASCII '0'
	jlt		sector_size_err														| Read character is < ASCII 0 so it is invalid

	sub		#0x27,d1															| Translate config value to number of required left shifts for sector size calculation

	move	d1,sector_size_shift_value

	| Read serial device ASCII ID

	move.b	temp_long+2,serial_device+1
	jmp		read_config_file_success

config_drive_err:
	move	#err_disk_id_out_of_range, d0
	rts

sector_size_err:
	move	#err_sector_size_out_of_range, d0
	rts

read_config_file_success:
	subi.w	#ascii_alpha_offset,disk_identifier									| Convert the ASCII character for disk ID to its numeric value
	subi.w	#ascii_number_offset,serial_device									| Convert the ASCII character for serial device to its numeric value
	clr.l	d0																	| Success return value

	rts

|-------------------------------------------------------------------------------
| Allocates and assigns new disk buffers to enable disks >32MiB
|
| Input
| variables: sector_size_shift_value
|
| Output
| 0 on success
| -1 on error
|
| Corrupts
| d1, d2
| a1, a4, a5, a6

allocate_buffers:
	move	sector_size_shift_value,d0											| Copy sector shift (bits)

	cmp		#0x09,d0															| Check if sector shift is 0x09 (same as default buffer size)
	jeq		99f																	| Sector size is 512KiB, so no need to allocate new buffer. Skip to end

	moveq	#1,d1																| Init d1 with sector size value of 1
	lsl.w	d0,d1																| Shift sector size left, i.e. multiply number of sectors by bytes per sector to get total bytes
	move.w	d1,d2																| Copy resultant size of 1 sector
	lsl.w 	#0x02,d1															| Shift sector bytes value 2 bits left (i.e. multiply by 4) to get final buffer size
	Malloc 	d1

	tst     d0           														| Test for null buffer pointer
	jne     1f     		 														| Buffer is allocated, continue
	moveq	#-1,d0																| Failure return value
	rts

1:
	| Data buffer 1

	move.l	d0,a6																| Copy address of new buffer

	move.l	(_bufl),a4															| Copy first data BCB pointer
	move.l	(a4),a5																| Copy next BCB pointer
	add.l	#0x10,a4															| Offset to buffer pointer

	move.l	(a4),a1																| Copy original buffer address
	move.l	a6,(a4)																| Set buffer address to new allocated area of RAM

	jbsr copy_buffer

	| Data buffer 2

	add.l	#0x10,a5															| Offset to buffer pointer
	move.l	(a5),a1																| Copy original buffer address
	move.l	a6,(a5)																| Set buffer address to new allocated area of RAM

	jbsr copy_buffer															| Offset to next sector area in buffer

	| FAT buffer 1

	move.l	(_bufl+0x04),a4														| Copy first FAT BCB pointer
	move.l	(a4),a5																| Copy next BCB pointer
	add.l	#0x10,a4															| Offset to buffer pointer
	move.l	(a4),a1																| Copy original buffer address
	move.l	a6,(a4)																| Set buffer address to new allocated area of RAM

	jbsr	copy_buffer

	| FAT buffer 2

	add.l	#0x10,a5															| Offset to buffer pointer
	move.l	(a5),a1																| Copy original buffer address
	move.l	a6,(a5)																| Set buffer address to new allocated area of RAM

	jbsr copy_buffer

	clr.l	d0																	| Success return value
99:
rts

|-------------------------------------------------------------------------------
| Copies 0x200 bytes from input buffer to output buffer and returns output buffer
| at defined offset from input address
|
| Input
| a1.l = source buffer address
| a6.l = destination buffer address
| d2.w = output offset for destination buffer
|
| Output
| a6.l = a6 + d2
|
| Corrupts
| d3

copy_buffer:
	move	#0x200,d3															| Put byte count into counter (512)
1:
	move.b	(a1)+,(a6)+
	dbf		d3,1b

	sub.l	#0x201,a6															| Move back to beginning of destination buffer
	add.l	d2,a6																| Offset destination buffer
rts

|-------------------------------------------------------------------------------
| Prints a string from a file
|
| Input
| d0 = position of string to display
|
| Output
|
| Corrupts
| d0, d1, d2
| temp_long+7

print_resource_string:
	move.l	d0,d1
	Fopen	const_res_filename,#0												| Attempt to open resource file
	tst.w	d0																	| Check return value
	jmi		2f																	| Return value is negative (failed)

	move.l	d0,d2
	Fseek	d1,d0,#0

print_next_char:
	Fread 	d2,#1,temp_long+7
	move.b	temp_long+7,d0
	tst.b d0
	jeq end_res_read
	Cconout d0
	jmp print_next_char
end_res_read:
	Fclose	d2
	jmp		2f																	| Close the file handle
2:
	clr		d0
	rts

|-------------------------------------------------------------------------------
| GEM Cconws
| Prints a string to the console
|
| Input
| a0 = address of a null-terminated string
|
| Output
|
| Corrupts
|
print_string:
	pea		(a0)
	move.w	#9,-(sp)
	trap	#1
	addq.l	#6,sp
rts
|-------------------------------------------------------------------------------

.include "../src/LZ4_serial.asm"

.data

|-------------------------------------------------------------------------------

| Filenames

const_config_filename:
	.asciz	"SERDISK.CFG"

const_res_filename:
	.asciz	"SERDISK.MSG"

| Messages

str_welcome:
	.asciz	"SerialDisk v3.0 beta\r\n"

str_press_any_key:
	.asciz	"Press any key"

| Errors

str_err_res_not_found:
	.asciz	"Resource file SERDISK.MSG missing\r\n"

|-------------------------------------------------------------------------------

.bss

| ds.w is the smallest size that can be specified here
|-------------------------------------------------------------------------------

disk_identifier:
	ds.w	0x01

disk_bpb:
	ds.w	0x09

sector_size_shift_value:
	ds.w	0x01

old_hdv_bpb:
	ds.l	0x01

old_hdv_rw:
	ds.l	0x01

old_hdv_mediach:
	ds.l	0x01

crc32_table:
	ds.l	0x100

refresh_rate:
	ds.w	0x01

temp_long:
	ds.l	0x01

serial_device:
	ds.w	0x01

|-------------------------------------------------------------------------------

.end

|-------------------------------------------------------------------------------
