|-------------------------------------------------------------------------------
| Compresses a series of bytes with run-length compression
| and send the compressed bytes over serial
|
| Input
| a4.l = address of data to be compressed
| d3.l = number of bytes to compress
|
| Output
| output: -ve on failure
|
| Corrupts
| d3, d4, d7
| a4

rle_compress:
    moveq   #0,d7                   | repeat counter
do_read:
    move.b  (a4)+,d4                | read file byte
    jbsr	add_run
    subq    #1,d3                   | decrement bytes left to read
    tst     d3
    jne     do_compare
    jbsr    do_run
    jmp     99f
do_compare:
    cmp.b	(a4),d4
    jeq     do_read
    jbsr    do_run
    jmp     do_read

add_run:
    tst.b   d7
    jne     not_first
    Bconout	#1,d4
not_first:
    addq.b  #1,d7
    cmp.b   #0xFF,d7
    jne     not_run
    jbsr    do_run
not_run:
    rts

do_run:
    cmpi.b  #2,d7
    jcs     end_run
    move.b	d4,d0
    jbsr	write_serial
    move.b	d7,d0
    jbsr	write_serial
end_run:
    moveq   #0,d7
    rts

99:
