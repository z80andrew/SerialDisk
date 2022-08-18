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
.equ compression_isenabled,	0x00												| Bit position of compression flag

| Other constants
.equ wait_secs,				0x02												| Time for pauses (secs)
.equ serial_timeout_secs,	0x0A												| Serial read timeout (secs)
.equ crc32_poly,			0x04c11db7											| Polynomial for CRC32 calculation
.equ ascii_alpha_offset,	0x41												| Offset from number to its ASCII equivalent (for letters)
.equ ascii_number_offset,	0x30												| Offset from number to its ASCII equivalent (for numbers)
.equ screenres_high,		0x02												| Value of byte at (screenres) when in hi resolution mode
.equ res_string_length,		0x40												| Fixed length of each string in resource file

| Screen refresh rates
.equ pal_hz,				0x32												| 50Hz
.equ ntsc_hz,				0x3C												| 60Hz
.equ hires_hz,				0x48												| 72Hz (although more accurately 71.2-71.4Hz)

| Resource file indexes
.equ res_msg_press_any_key,			0x00
.equ res_msg_drive_mounted,			0x01
.equ res_msg_config_found,			0x02
.equ res_msg_config_not_found,		0x09
.equ res_msg_buffer,				0x0A

.equ res_msg_modem,					0x0C
.equ res_msg_modem1,				0x0D
.equ res_msg_modem2,				0x0E
.equ res_msg_serial1,				0x0F
.equ res_msg_serial2,				0x10

.equ res_err_prefix,				0x03
.equ res_err_buffer_allocation,		0x04
.equ res_err_config_invalid,		0x05
.equ res_err_drive_already_mounted,	0x06
.equ res_err_sector_size,			0x07
.equ res_err_disk_id,				0x08
.equ res_err_serial_dev,			0x0B

| Error codes
.equ err_disk_id_out_of_range,		-1
.equ err_sector_size_out_of_range,	-2
.equ err_serial_device_out_of_range,-3
