|---------------------------------------------------------
|
|	LZ4 block 68k small depacker
|	Written by Arnaud Carré ( @leonard_coder )
|	https://github.com/arnaud-carre/lz4-68k
|
|	LZ4 technology by Yann Collet ( https://lz4.github.io/lz4/ )
|
|	Modified by z80andrew for use with SerialDisk
|
|---------------------------------------------------------

| Smallest version
|
| input: a5.l : output buffer
|		 temp_long: long address of compressed data length
|
| output: -ve on failure
|
| corrupts:
| a3, a5
| d0, d2, d3, d4, d5

lz4_depack:
			moveq.l	#0,d4
			moveq.l	#0,d5
			moveq.l	#0,d3

tokenLoop:
			jbsr	read_serial
			tst		d0
			jmi		readEnd
			move.b	d0,d5
			add.l	#1,d4

			move.l	d5,d2
			lsr.b	#4,d2
			beq		lenOffset

			bsr		readLen

litcopy:
			jbsr	read_serial
			tst		d0
			jmi		readEnd
			move.b	d0,(a5)+
			add.l	#1,d4

			subq.l	#1,d2			| block could be > 64KiB
			bne		litcopy

			| end test is always done just after literals
			cmp.l	temp_long,d4
			beq		readEnd

lenOffset:
			jbsr	read_serial
			tst		d0
			jlt		readEnd
			move.b	d0,d3

			jbsr	read_serial
			tst		d0
			jmi		readEnd
			move.b	d0,-(a7)

			move.w	(a7)+,d2
			move.b	d3,d2

			add.l	#2,d4

			movea.l	a5,a3
			sub.l	d2,a3		| d2 bits 31..16 are always 0 here
			moveq	#0x0f,d2
			and.w	d5,d2

			bsr		readLen

			addq.l	#4,d2

copy:		move.b	(a3)+,(a5)+
			subq.l	#1,d2
			bne		copy
			bra		tokenLoop

readLen:	cmp.b	#15,d2
			bne		readEnd
readLoop:
			jbsr	read_serial
			tst		d0
			jmi		readEnd
			move.b	d0,d3
			add.l	#1,d4

			add.l	d3,d2				| final len could be > 64KiB
			not.b	d3
			beq		readLoop
readEnd:
			rts
