100046a0  push     ebp
100046a1  mov      ebp, esp
100046a3  and      esp, 0xfffffff8
100046a6  sub      esp, 0x14
100046a9  mov      eax, dword ptr [0x1003b070]
100046ae  xor      eax, esp
100046b0  mov      dword ptr [esp + 0x10], eax
100046b4  push     ebx
100046b5  push     esi
100046b6  push     edi
100046b7  mov      edi, ecx
100046b9  mov      edx, 0x10036770
100046be  mov      ecx, 0x1003c138
100046c3  xor      bl, bl
100046c5  call     0x100089c0
100046ca  push     eax
100046cb  call     0x10008d50
100046d0  add      esp, 4
100046d3  test     edi, edi
100046d5  jne      0x10004703
100046d7  mov      edx, 0x10036788
100046dc  mov      ecx, 0x1003c138
100046e1  call     0x100089c0
100046e6  push     eax
100046e7  call     0x10008d50
100046ec  add      esp, 4
100046ef  xor      al, al
100046f1  pop      edi
100046f2  pop      esi
100046f3  pop      ebx
100046f4  mov      ecx, dword ptr [esp + 0x10]
100046f8  xor      ecx, esp
100046fa  call     0x1000c22e
100046ff  mov      esp, ebp
10004701  pop      ebp
10004702  ret      
10004703  lea      eax, [esp + 0x10]
10004707  mov      edx, 0x100367a0
1000470c  push     0xa
1000470e  push     eax
1000470f  mov      ecx, 0x100367f0
10004714  call     0x100040b0
10004719  add      esp, 8
1000471c  mov      esi, eax
1000471e  mov      edx, 0x10036818
10004723  push     0xa
10004725  push     ecx
10004726  push     esi
10004727  push     ecx
10004728  mov      ecx, 0x1003c138
1000472d  call     0x100089c0
10004732  add      esp, 4
10004735  mov      ecx, eax
10004737  call     0x10005710
1000473c  mov      edx, 0x10036800
10004741  mov      ecx, eax
10004743  call     0x100089c0
10004748  add      esp, 4
1000474b  mov      ecx, eax
1000474d  call     0x10005710
10004752  push     eax
10004753  call     0x10008d50
10004758  add      esp, 4
1000475b  cmp      esi, 0xa
1000475e  jne      0x10004789
10004760  movq     xmm0, qword ptr [esp + 0x10]
10004766  mov      bl, 1
10004768  mov      ax, word ptr [esp + 0x18]
1000476d  movq     qword ptr [edi], xmm0
10004771  mov      word ptr [edi + 8], ax
10004775  mov      al, bl
10004777  pop      edi
10004778  pop      esi
10004779  pop      ebx
1000477a  mov      ecx, dword ptr [esp + 0x10]
1000477e  xor      ecx, esp
10004780  call     0x1000c22e
10004785  mov      esp, ebp
10004787  pop      ebp
10004788  ret      
10004789  mov      edx, 0x10036820
1000478e  mov      ecx, 0x1003c138
10004793  call     0x100089c0
10004798  push     eax
10004799  call     0x10008d50
1000479e  mov      ecx, dword ptr [esp + 0x20]
100047a2  add      esp, 4
100047a5  mov      al, bl
100047a7  pop      edi
100047a8  pop      esi
100047a9  pop      ebx
100047aa  xor      ecx, esp
100047ac  call     0x1000c22e
100047b1  mov      esp, ebp
100047b3  pop      ebp
100047b4  ret      
100047b5  int3     
100047b6  int3     
100047b7  int3     
100047b8  int3     
100047b9  int3     
100047ba  int3     
100047bb  int3     
100047bc  int3     
100047bd  int3     
100047be  int3     
100047bf  int3     
100047c0  push     ecx
100047c1  push     ebx
100047c2  push     esi
100047c3  mov      esi, ecx
100047c5  mov      edx, 0x10036840
100047ca  mov      ecx, 0x1003c138
100047cf  call     0x100089c0
100047d4  push     eax
100047d5  call     0x10008d50
100047da  add      esp, 4
100047dd  test     esi, esi
100047df  jne      0x100047ff
100047e1  mov      edx, 0x10036788
100047e6  mov      ecx, 0x1003c138
100047eb  call     0x100089c0
100047f0  push     eax
100047f1  call     0x10008d50
100047f6  add      esp, 4
100047f9  xor      al, al
100047fb  pop      esi
100047fc  pop      ebx
100047fd  pop      ecx
100047fe  ret      
100047ff  xor      ebx, ebx
10004801  call     0x10004020
10004806  test     eax, eax
10004808  je       0x10004844
1000480a  push     0x1003657c
1000480f  push     0x1003656c
10004814  call     dword ptr [0x1002a0ac]
1000481a  push     eax
1000481b  call     dword ptr [0x1002a030]
10004821  test     eax, eax
10004823  je       0x10004844
10004825  push     7
10004827  push     0xa
10004829  push     esi
1000482a  push     0x100367a0
1000482f  push     0x100367f0
10004834  call     eax
10004836  lea      ebx, [eax - 1]
10004839  neg      ebx
1000483b  sbb      ebx, ebx
1000483d  inc      ebx
1000483e  call     dword ptr [0x1002a0c4]
10004844  test     bl, bl
10004846  mov      eax, 0x10036860
1000484b  mov      edx, 0x10036858
10004850  mov      ecx, 0x1003c138
10004855  cmove    edx, eax
10004858  call     0x100089c0
1000485d  pop      esi
1000485e  mov      al, bl
10004860  pop      ebx
10004861  pop      ecx
10004862  ret      
10004863  int3     
10004864  int3     
10004865  int3     
10004866  int3     
10004867  int3     
10004868  int3     
10004869  int3     
1000486a  int3     
1000486b  int3     
1000486c  int3     
1000486d  int3     
1000486e  int3     
1000486f  int3     