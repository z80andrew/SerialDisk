|-------------------------------------------------------------------------------
| BIOS Bconout
| Writes a byte to the serial port
|
| Input
| d0.b	byte to send
|
| Output
|
| Corrupts
| d1, d2 corrupted by BIOS calls
|
write_serial:
	move	d0,-(sp)															| Byte to write
	move.w	serial_device,d0													| Get output device ID
	move	d0,-(sp)															| Output device
	move	#3,-(sp)
	trap	#13
	addq.l	#6,sp
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
	pea		(a0)																| String to print
	move.w	#9,-(sp)
	trap	#1
	addq.l	#6,sp
rts

|-------------------------------------------------------------------------------
| GEM Cconin
| Reads a character from the console
|
| Input
|
| Output
| d0 = read character
|
| Corrupts
|
read_char:
	move	#1,-(sp)
	trap	#1
	addq	#2,sp
rts


|-------------------------------------------------------------------------------
| GEM Fopen
| Opens a file
|
| Input
| a0 = address of a null-terminated string
|
| Output
| d0 = file handle
|
| Corrupts
|
file_open:
	move.w	#0,-(sp)															| Read-only
	pea		(a0)																| Filename
	move.w	#61,-(sp)
	trap	#1
	addq.l	#8,sp
rts
