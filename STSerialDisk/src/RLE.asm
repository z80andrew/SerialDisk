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
    moveq   #0,d7     															| Init run repeat counter
do_read:
    move.b  (a4)+,d4                											| Read file byte
    jbsr	add_run																| Add byte to run
    subq.l  #1,d3                   											| Decrement bytes left to read
    tst.l   d3																	| Any bytes left to read?
    jne     do_compare															| Yes, compare with next byte
    jbsr    do_run																| No, output what's in the run so far
    jmp     99f																	| Go to end
do_compare:
    cmp.b	(a4),d4																| Compare read byte with next byte (pre-incremented)
    jeq     do_read																| Same? Read next byte
    jbsr    do_run																| Not the same, so output run
    jmp     do_read																| Read next byte

add_run:
    tst.b   d7																	| Are there any bytes in the run?
    jne     not_first															| Yes, no need to prepend first byte
    move.b	d4,d0																| Prepend byte to run
    jbsr	write_serial														| Output byte
not_first:
    addq.b  #1,d7																| Increase repeats of byte in the run
    cmp.b   #0xFF,d7															| Have we reached the maximum repeats a byte can store?
    jne     not_run																| No, no need to output this run yet
    jbsr    do_run																| Yes, output this run
not_run:
    rts

do_run:
    cmpi.b  #2,d7																| Is the number of repeats less than 2?
    jcs     end_run																| Yes, no need to output as a run
    move.b	d4,d0
    jbsr	write_serial														| Output byte
    move.b	d7,d0
    jbsr	write_serial														| Output number of repeats
end_run:
    moveq   #0,d7																| Reset number of repeats
    rts

99:
