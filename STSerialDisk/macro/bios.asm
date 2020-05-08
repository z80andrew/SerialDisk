| ---------------------------------------------------------------------------------------------------------------------------------
| IMPORTANT NOTE:
| The BIOS receives its parameters from the stack. Only registers d3-d7 and a3-a7 are saved, all others may be altered by the call.
| ---------------------------------------------------------------------------------------------------------------------------------

.macro	Bconin dev
	move	\dev,-(sp)
	move	#2,-(sp)
	trap	#13
	addq.l	#4,sp
.endm

.macro	Bconout dev, c
	move	\c,-(sp)
	move	\dev,-(sp)
	move	#3,-(sp)
	trap	#13
	addq.l	#6,sp
.endm

.macro	Bconstat dev
	move	\dev,-(sp)
	move	#1,-(sp)
	trap	#13
	addq.l	#4,sp
.endm

.macro	Bcostat dev
	move	\dev,-(sp)
	move	#8,-(sp)
	trap	#13
	addq	#4,sp
.endm

.macro	Drvmap
	move	#10,-(sp)
	trap	#13
	addq	#2,sp
.endm

.macro	Getbpb dev
	move	\dev,-(sp)
	move	#7,-(sp)
	trap	#13
	addq.l	#4,sp
.endm

.macro	Getmpb p_mpb
	pea		\p_mpb
	clr		-(sp)
	trap	#13
	addq	#6,sp
.endm

.macro	Kbshift mode
	move	\mode,-(sp)
	move	#11,-(sp)
	trap	#13
	addq	#4,sp
.endm

.macro	Mediach dev
	move	\dev,-(sp)
	move	#9,-(sp)
	trap	#13
	addq.l	#4,sp
.endm

.macro	Rwabs rwflag, buf, count, recno, dev, lrecno
	move.l	\lrecno,-(sp)
	move	\dev,-(sp)
	move	\recno,-(sp)
	move	\count,-(sp)
	pea		\buf
	move	\rwflag,-(sp)
	move	#4,-(sp)
	trap	#13
	lea		$12(sp),sp
.endm

.macro	Setexc vecnum, vec
	pea		\vec
	move	\vecnum,-(sp)
	move	#5,-(sp)
	trap	#13
	addq	#8,sp
.endm

.macro	Tickcal
	move	#6,-(sp)
	trap	#13
	addq	#2,sp
.endm

