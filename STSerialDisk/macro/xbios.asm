| ---------------------------------------------------------------------------------------------------------------------------------
| IMPORTANT NOTE:
| The XBIOS receives its parameters from the stack. Only registers d3-d7 and a3-a7 are saved, all others may be altered by the call.
| ---------------------------------------------------------------------------------------------------------------------------------

.macro	Gettime
	move	#23,-(sp)
	trap	#14
	addq	#2,sp
.endm

.macro	Logbase
	move	#3,-(sp)
	trap	#14
	addq	#2,sp
.endm

.macro	Physbase
	move	#2,-(sp)
	trap	#14
	addq	#2,sp
.endm

.macro	Setscreen logLoc, physLoc, res
	move	\res,-(sp)
	pea		\physLoc
	pea		\logLoc
	move	#5,-(sp)
	trap	#14
	lea		12(sp),sp
.endm

.macro	Rsconf baud, ctr, ucr, rsr, tsr, scr
	move	\scr,-(sp)
	move	\tsr,-(sp)
	move	\rsr,-(sp)
	move	\ucr,-(sp)
	move	\ctr,-(sp)
	move	\baud,-(sp)
	move	#15,-(sp)
	trap	#14
	lea		14(sp),sp
.endm

.macro	Vsync
	move	#37,-(sp)
	trap	#14
	addq	#2,sp
.endm

| Falcon

.macro	Montype
	move	#89,-(sp)
	trap	#14
	addq	#2,sp
.endm

.macro	Vsetmode modecode
	move	\modecode,-(sp)
	move	#88,-(sp)
	trap	#14
	addq	#4,sp
.endm

.macro	Dsp_Available xavailable, yavailable
	pea		\yavailable
	pea		\xavailable
	move	#106,-(sp)
	trap	#14
	lea		10(sp),sp
.endm

.macro	Dsp_LoadProgram file, ability, buffer
	pea		\buffer
	move	\ability,-(sp)
	pea		\file
	move	#108,-(sp)
	trap	#14
	lea		12(sp),sp
.endm

.macro	Dsp_ExecProg ability, codesize, codeptr
	move	\ability,-(sp)
	move	\codesize,-(sp)
	pea		\codeptr
	move	#109,-(sp)
	trap	#14
	lea		12(sp),sp
.endm

.macro	Dsp_LodToBinary buffer, filename
	pea		\buffer,-(sp)
	pea		\filename,-(sp)
	move	#111,-(sp)
	trap	#14
	lea		10(sp),sp
.endm

.macro	WavePlay flags, rate, sptr, slen
	move.l	\slen,-(sp)
	pea		\sptr
	move.l	\rate,-(sp)
	move	\flags,-(sp)
	move	#165,-(sp)
	trap	#14
	lea		16(sp),sp
.endm

| func:
| 0	Switch cursor off (hide it)
| 1	Switch cursor on
| 2	Enable cursor blink
| 3	Disable cursor blink
| 4	The blink rate of the cursor will be set to the value rate
| 5	Returns the current blink rate
.macro Cursconf func, rate
	move.w    \rate,-(sp)
	move.w    \func,-(sp)
	move.w    #21,-(sp)
	trap      #14
	addq.l    #6,sp
.endm

.macro Supexec func
	pea       \func			| Offset 2
	move.w    #38,-(sp)		| Offset 0
	trap      #14			| Call XBIOS
	addq.l    #6,sp			| Correct stack
.endm
