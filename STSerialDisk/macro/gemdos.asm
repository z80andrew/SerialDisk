| ---------------------------------------------------------------------------------------------------------------------------------
| IMPORTANT NOTE:
| The GEMDOS receives its parameters from the stack. Only registers d3-d7 and a3-a7 are saved, all others may be altered by the call.
| ---------------------------------------------------------------------------------------------------------------------------------

.macro	Cauxin
	move	#3,-(sp)
	trap	#1
	addq	#2,sp
.endm

.macro	Cauxis
	move	#18,-(sp)
	trap	#1
	addq	#2,sp
.endm

.macro	Cauxos
	move	#19,-(sp)
	trap	#1
	addq	#2,sp
.endm

.macro	Cauxout c
	move	\c,-(sp)
	move	#4,-(sp)
	trap	#1
	addq	#4,sp
.endm

.macro	Cconin
	move	#1,-(sp)
	trap	#1
	addq	#2,sp
.endm

.macro	Cconis
	move	#11,-(sp)
	trap	#1
	addq	#2,sp
.endm

.macro	Cconos
	move	#16,-(sp)
	trap	#1
	addq	#2,sp
.endm

.macro	Cconout c
	move.w	\c,-(sp)
	move.w	#2,-(sp)
	trap	#1
	addq.l	#4,sp
.endm

.macro	Cconws str
	pea		\str
	move.w	#9,-(sp)
	trap	#1
	addq.l	#6,sp
.endm

.macro	Fclose handle
	move.w	\handle,-(sp)
	move.w	#62,-(sp)
	trap	#1
	addq.l	#4,sp
.endm

.macro	Fcreate fname, attribs
	move	\attribs,-(sp)
	pea		\fname
	move	#60,-(sp)
	trap	#1
	addq	#8,sp
.endm

.macro	Fopen fname, mode
	move.w	\mode,-(sp)
	pea		\fname
	move.w	#61,-(sp)
	trap	#1
	addq.l	#8,sp
.endm

.macro	Fread handle, count, buffer
	pea		\buffer
	move.l	\count,-(sp)
	move.w	\handle,-(sp)
	move.w	#63,-(sp)
	trap	#1
	lea		12(sp),sp
.endm

.macro	Fwrite handle, count, buffer
	pea		\buffer
	move.l	\count,-(sp)
	move	\handle,-(sp)
	move	#64,-(sp)
	trap	#1
	lea		12(sp),sp
.endm

.macro	Fseek offset, handle, seekmode
	move	\seekmode,-(sp)
	move	\handle,-(sp)
	move.l	\offset,-(sp)
	move	#66,-(sp)
	trap	#1
	lea		10(sp),sp
.endm

.macro	Mxalloc amount, mode
	move	\mode,-(sp)
	move.l	\amount,-(sp)
	move	#68,-(sp)
	trap	#1
	addq	#8,sp
.endm

| amount: number of bytes to allocate
.macro	Malloc amount
	move.l	\amount,-(sp)
	move	#72,-(sp)
	trap	#1
	addq.l	#6,sp
.endm

| block: address of block to release
.macro	Mfree saddr
	pea		\saddr
	move	#73,-(sp)
	trap	#1
	addq	#6,sp
.endm

.macro	Mshrink block, newsize
	move.l	\newsize,-(sp)
	pea		\block
	clr.w	-(sp)
	move	#74,-(sp)
	trap	#1
	lea		12(sp),sp
.endm

.macro	Pterm0
	clr		-(sp)
	trap	#1
.endm

.macro	Pterm retcode
	move	\retcode,-(sp)
	move	#76,-(sp)
	trap	#1
.endm

.macro	Ptermres keep, ret
	move	\ret,-(sp)
	move.l	\keep,-(sp)
	move	#49,-(sp)
	trap	#1
	addq.l	#8,sp
.endm

.macro Super stack
	pea		\stack
	move.w	#32,-(sp)
	trap	#1
	addq.l	#6,sp
.endm
