| Atari memory addresses
.equ hdv_bpb, 		0x472														| Vector to routine that establishes the BPB of a BIOS drive.
.equ hdv_rw, 		0x476														| Vector to the routine for reading and writing of blocks to BIOS drives.
.equ hdv_mediach, 	0x47e														| Vector to routine for establishing the media-change status of a BIOS drive. The BIOS device number is passed on the stack (4(sp)).
.equ _drvbits, 		0x4c2														| Bit-table for the mounted drives of the BIOS.
.equ _bufl,			0x4b2														| Two (GEMDOS) buffer-list  headers.
.equ _vbclock,		0x462														| Vertical blank count (long)
.equ palmode,		0xFFFF820A													| PAL/NTSC mode (byte)
.equ screenres,		0xFFFF8260													| Screen resolution (byte)

| SerialDisk commands
.equ cmd_read, 		0x00
.equ cmd_write, 	0x01
.equ cmd_bpb, 		0x02

| SerialDisk data flags
.equ compression_isenabled,	0x00

| Other constants
.equ wait_secs,				0x01												| Time for pauses (secs)
.equ serial_timeout_secs,	0x05												| Serial read timeout (secs)
.equ crc32_poly,			0x04c11db7											| Polynomial for CRC32 calculation
.equ ascii_offset,			0x41												| Offset from number to its ASCII equivalent
.equ screenres_high,		0x02												| Value of byte at (screenres) when in hi resolution mode

| Screen refresh rates
.equ pal_hz,				0x32												| 50Hz
.equ ntsc_hz,				0x3C												| 60Hz
.equ hires_hz,				0x48												| 72Hz (although more accurately 71.2-71.4Hz)
