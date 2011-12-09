\ assembler.fs ---

\ Copyright 2011 (C) David Vazquez

\ This file is part of Eulex.

\ Eulex is free software: you can redistribute it and/or modify
\ it under the terms of the GNU General Public License as published by
\ the Free Software Foundation, either version 3 of the License, or
\ (at your option) any later version.

\ Eulex is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU General Public License for more details.

\ You should have received a copy of the GNU General Public License
\ along with Eulex.  If not, see <http://www.gnu.org/licenses/>.

vocabulary Assembler
get-current
also Assembler definitions

DECIMAL

\ Assembler output

0 value asmfd

\ Emit the low byte of a word without pop it
: lb dup 255 and asmfd emit-file throw asmfd flush-file throw ;
\ Shift 8 bits to the right
: 8>> 8 rshift ;

: byte lb drop ;                       (  8 bits )
: word lb 8>> lb drop ;                ( 16 bits )
: dword lb 8>> lb 8>> lb 8>> lb drop ; ( 32 bits )


\ Instructions with no operands
: single-instruction ( opcode -- )
    create c, does> c@ byte ;

HEX
60 single-instruction pusha
61 single-instruction popa
90 single-instruction nop
C3 single-instruction ret
CF single-instruction iret
FA single-instruction cli
FB single-instruction sti
DECIMAL

1 constant OP-AL
2 constant OP-AX
4 constant OP-EAX
8 constant OP-REG8
16 constant OP-REG16
32 constant OP-REG32
64 constant OP-SREG
128 constant OP-IMM
256 constant OP-MEM8
512 constant OP-MEM16
1024 constant OP-MEM32

\ Registers

: reg8  create , does> @  OP-REG8 swap ;
: reg16 create , does> @ OP-REG16 swap ;
: reg32 create , does> @ OP-REG32 swap ;
: sreg  create , does> @  OP-SREG swap ;

: %al  OP-AL OP-REG8 or 0 ;
: %ax  OP-AX OP-REG16 or 0 ;
: %eax OP-EAX OP-REG32 or 0 ;

( 0 reg32 %eax   0 reg16 %ax     0 reg8 %al )   0 sreg %es
1 reg32 %ecx     1 reg16 %cx     1 reg8 %cl     1 sreg %cs
2 reg32 %edx     2 reg16 %dx     2 reg8 %dl     2 sreg %ss
3 reg32 %ebx     3 reg16 %bx     3 reg8 %bl     3 sreg %ds
4 reg32 %esp     4 reg16 %sp     4 reg8 %ah     4 sreg %fs
5 reg32 %ebp     5 reg16 %bp     5 reg8 %ch     5 sreg %gs
6 reg32 %esi     6 reg16 %si     6 reg8 %dh
7 reg32 %edi     7 reg16 %di     7 reg8 %bh

\ Immediate values
: # OP-IMM ;


\ Memory references

\ The more general memory reference mode is
\     base + index*scale + displacement
\ where BASE and INDEX are 32bits registers, SCALE is 1, 2 or 4, and
\ DISPLACEMENT is an immediate offset.
\
\ The following variables contain each one of the parts in the general
\ addressing mode. A value of -1 where a register is expected means
\ that it is omitted. Note that is it not the ModR/M either thea SIB
\ bytes. They are encoded later from this variables, however.
variable base
variable index
variable scale
variable displacement

: reset-addressing-mode
    -1 base !
    -1 index !
    1 scale !
    0 displacement ! ;

: check-reg32
    over OP-REG32 and 0=
    abort" Addressing mode must use 32bits registers." ;

: B check-reg32 base ! DROP ;
: I check-reg32 index ! DROP ;
: S scale ! ;
: D displacement ! ;

\ For addressing modes without base
: #PTR8 D OP-MEM8 0 ;
: #PTR16 D OP-MEM16 0 ;
: #PTR32 D OP-MEM32 0 ;
' #PTR32 alias #PTR

: 1* 1 S ;
: 2* 2 S ;
: 4* 4 S ;
: 8* 8 S ;

\ BASE                               BASE + DISP                   INDEX
: [%eax] %eax B OP-MEM32 0 ;       : +[%eax] D [%eax] ;          : >%eax %eax I ;
: [%ecx] %ecx B OP-MEM32 0 ;       : +[%ecx] D [%ecx] ;          : >%ecx %ecx I ;
: [%edx] %edx B OP-MEM32 0 ;       : +[%edx] D [%edx] ;          : >%edx %edx I ;
: [%ebx] %ebx B OP-MEM32 0 ;       : +[%ebx] D [%ebx] ;          : >%ebx %ebx I ;
: [%esp] %esp B OP-MEM32 0 ;       : +[%esp] D [%esp] ;          ( %esp is not a valid index )
: [%ebp] %ebp B OP-MEM32 0 ;       : +[%ebp] D [%ebp] ;          : >%ebp %ebp I ;
: [%esi] %esi B OP-MEM32 0 ;       : +[%esi] D [%esi] ;          : >%esi %esi I ;
: [%edi] %edi B OP-MEM32 0 ;       : +[%edi] D [%edi] ;          : >%edi %edi I ;

\ Override size of the memory reference
: PTR8 NIP OP-MEM8 SWAP ;
: PTR16 NIP OP-MEM16 SWAP ;
: PTR32 NIP OP-MEM32 SWAP ; \ Default


\ PATTERN-MACHING

variable inst#op

: operands inst#op ! ;
' operands alias operand

\ Operands pattern maching

: 1-op-match ( op mask -- op flag )
    2 pick and 0<> ;

: 2-op-match ( op1 op2 mask1 mask2 -- op1 op2 flag )
    3 pick and 0<> swap
    5 pick and 0<> and ;

: op-match ( ops .. masks ... -- ops .. flag )
    inst#op @ 1 = if 1-op-match else 2-op-match then ;

\ Patterns for the dispatcher
' OP-AL    alias al
' OP-AX    alias ax
' OP-EAX   alias eax
' OP-REG8  alias reg8
' OP-REG16 alias reg16
' OP-REG32 alias reg32
' OP-SREG  alias sreg
' OP-IMM   alias imm
' OP-MEM8  alias mem8
' OP-MEM16 alias mem16
' OP-MEM32 alias mem32
\ Multicase patterns
-1 constant any
al ax or eax or constant acc
reg8 reg16 or reg32 or constant reg
mem8 mem16 or mem32 or constant mem
reg8 mem8 or constant r/m8
reg16 mem16 or constant r/m16
reg32 mem32 or constant r/m32
reg mem or constant r/m

: (no-dispatch)
    true abort" The instruction does not support that operands." ;

0 constant begin-dispatch immediate

: ` postpone postpone ; immediate

: dispatch:
    1+ >r
    ` op-match ` if
    r>
; immediate compile-only

: ::
    >r ` else r>
; immediate compile-only

: end-dispatch
    ` (no-dispatch)
    0 ?do ` then loop
; immediate compile-only


\ INSTRUCTION ENCODING

\ Parts of the instruction and the size in bytes of them in the
\ current instruction. A size of zero means this part is not present.
variable inst-size-override?
variable inst-opcode
variable inst-opcode-size
variable inst-modr/m
variable inst-modr/m-size
variable inst-sib
variable inst-sib-size
variable inst-disp
variable inst-disp-size
variable inst-imm
variable inst-imm-size

\ Initialize the assembler state for a new instruction. It must be
\ called in the beginning of each instruction.

: 0! 0 swap ! ;
: reset-instruction
    reset-addressing-mode
    inst-size-override? off
    inst-opcode 0!
    1 inst-opcode-size !
    inst-modr/m 0!
    inst-modr/m-size 0!
    inst-sib 0!
    inst-sib-size 0!
    inst-disp 0!
    inst-disp-size 0!
    inst-imm 0!
    inst-imm-size 0! ;
latestxt execute

\ Words to fill instruction's data

\ Set the size-override prefix.
: size-override inst-size-override? on ;

\ Set some bits in the opcode field.
: |opcode ( u -- )
    inst-opcode @ or inst-opcode ! ;

: clear-bits ( mask value -- value* )
    swap invert and ;

: set-bits! ( x mask addr -- )
    dup >r @ over swap clear-bits -rot and or r> ! ;

: set-modr/m-bits!
    inst-modr/m set-bits!
    1 inst-modr/m-size ! ;

: set-sib-bits!
    inst-sib set-bits!
    1 inst-sib-size ! ;

: mod!    6 lshift %11000000 set-modr/m-bits! ;
: op/reg! 3 lshift %00111000 set-modr/m-bits! ;
: r/m!             %00000111 set-modr/m-bits! ;

: s! 6 lshift %11000000 set-sib-bits! ;
: i! 3 lshift %00111000 set-sib-bits! ;
: b!          %00000111 set-sib-bits! ;

\ Set the displacement field.
: disp! inst-disp ! ;
: disp-size! inst-disp-size ! ;
: disp8! disp! 1 disp-size! ;
: disp16! disp! 2 disp-size! ;
: disp32! disp! 4 disp-size! ;

\ Set the immediate field.
: imm! inst-imm ! ;
: imm-size! inst-imm-size ! ;
: imm8! imm! 1 imm-size! ;
: imm16! imm! 2 imm-size! ;
: imm32! imm! 4 imm-size! ;

: flush-value ( x size -- )
    case
        0 of drop  endof
        1 of byte  endof
        2 of word  endof
        4 of dword endof
        true abort" Invalid number of bytes."
    endcase ;

: flush-instruction
    \ Prefixes
    inst-size-override? @ if $66 byte endif
    \ Opcode, modr/m and sib
    inst-opcode @ inst-opcode-size @ flush-value
    inst-modr/m @ inst-modr/m-size @ flush-value
    inst-sib    @ inst-sib-size    @ flush-value
    \ Displacement and immediate
    inst-disp @ inst-disp-size @ flush-value
    inst-imm  @ inst-imm-size  @ flush-value
    reset-instruction ;

\ Set size-override prefix if some of the operands is a r/m16.
: size-override?
    begin-dispatch
    any r/m16 dispatch: size-override ::
    r/m16 any dispatch: size-override ::
    exit
    end-dispatch ;

: <=x<= ( n1 n2 n3 -- n1<=n2<=n3 )
    over -rot <= >r <= r> and ;

\ return the mod value for a given displacement.
: disp>mod ( n -- 0|1|2 )
    ?dup 0= if
        0
    else
        -128 swap 127 <=x<= if 1 else 2 then
    endif ;

: scale>s ( scale -- s )
    case
        1 of 0 endof
        2 of 1 endof
        4 of 2 endof
        8 of 3 endof
        true s" Bad scale value."
    endcase ;


\ Memory reference encoding

: null-displacement?
    displacement @ 0= ;

\ Encode the displacement in the displacement field and the mod field
\ of the modr/m byte. It is a general encoding which may be necessary
\ to modify for special rules.
: encode-displacement
    displacement @ dup disp>mod dup mod!
    case
        0 of 0 disp-size! drop    endof
        1 of 1 disp-size! disp8!  endof
        2 of 4 disp-size! disp32! endof
    endcase ;

\ Encode memory references where there is not an index register. It
\ covers memory references of the form BASE + DISP, where BASE and
\ DISP are optional.
: encode-non-indexed-mref
    scale @ 1 <> abort" Scaled memory reference without index."
    base @ -1 = if
        5 r/m! displacement @ disp32!   \ only displacement
    else
        encode-displacement
        \ Special case: the ModR/M byte cannot encode [%EBP] as it is
        \ used to encode `only displacement' memory references, so we
        \ force a 8bits zero displacement.
        %ebp nip base @ = null-displacement? and if 1 mod! 0 disp8! endif
        \ Encode the base register in the ModR/M byte. If it is %esp,
        \ it requires to include the SIB byte.
        base @ r/m!
        \ NOTE: 4 means no index in SIB.
        %esp nip base @ = if base @ B! 4 I! endif
    endif ;

\ Encode memory references with an index register. It is encoded to
\ the SIB byte generally.
: encode-indexed-mref
    base @ -1 = if
        \ Special case: INDEX*SCALE + DISP. If SCALE is 1, we can
        \ encode the memory reference as a non-indexed. Otherwise, we
        \ have to force disp to 32bits.
        scale @ 1 = if
            index @ base ! -1 index ! encode-non-indexed-mref
        else
            0 mod! 4 r/m!
            scale @ scale>s s! index @ I! 5 B!
            displacement @ disp32!
        endif
    else
        \ More general addressing mode. We write R/M to 4 to specify a
        \ SIB byte, and write scale, index and base to it.
        encode-displacement 4 r/m!
        scale @ scale>s s! index @ i! base @ b!
    endif ;

\ Encode a general memory reference from the variables BASE, INDEX,
\ SCALE and DISPLACEMENT to the current instruction.
: encode-mref
    index @ -1 = if
        encode-non-indexed-mref
    else
        encode-indexed-mref
    endif ;


\ Check that the size of both operands is the same or signal an error.
: same-size
    begin-dispatch
      imm   any dispatch: ::
     r/m8  r/m8 dispatch: ::
    r/m16 r/m16 dispatch: ::
    r/m32 r/m32 dispatch: ::
    true abort" The size of the operands must match." ::
    end-dispatch ;

: mov-imm-reg
    size-override?
    begin-dispatch
    imm reg8  dispatch: |opcode $0 |opcode DROP  imm8! DROP ::
    imm reg16 dispatch: |opcode $8 |opcode DROP imm16! DROP ::
    imm reg32 dispatch: |opcode $8 |opcode DROP imm32! DROP ::
    end-dispatch ;

: mov-imm-mem
    size-override?
    encode-mref
    begin-dispatch
    imm mem8  dispatch: 0 |opcode 2DROP imm8!  DROP ::
    imm mem16 dispatch: 1 |opcode 2DROP imm16! DROP ::
    imm mem32 dispatch: 1 |opcode 2DROP imm32! DROP ::
    end-dispatch ;

: mov 2 operands same-size
    s" forth.core" w/o bin create-file throw to asmfd
    begin-dispatch
    imm reg dispatch: $B0 |opcode mov-imm-reg ::
    imm mem dispatch: $C6 |opcode mov-imm-mem ::
    mem acc dispatch: ::
    acc mem dispatch: ::
    r/m reg dispatch: ::
    reg r/m dispatch: ::
    end-dispatch
    flush-instruction
    asmfd close-file throw ;


SET-CURRENT
( PREVIOUS )


\ Local Variables:
\ forth-local-words: ((("begin-dispatch" "end-dispatch" "dispatch:" "::")
\                      compile-only (font-lock-keyword-face . 2)))
\ End:

\ assembler.fs ends here
