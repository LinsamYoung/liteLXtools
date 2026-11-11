10004020  push     ebp
10004021  mov      ebp, esp
10004023  sub      esp, 0x20
10004026  mov      eax, dword ptr [0x1003b070]
1000402b  xor      eax, ebp
1000402d  mov      dword ptr [ebp - 4], eax
10004030  lea      eax, [ebp - 0x1c]
10004033  push     eax
10004034  push     0x28
10004036  call     dword ptr [0x1002a084]
1000403c  push     eax
1000403d  call     dword ptr [0x1002a014]
10004043  test     eax, eax
10004045  je       0x10004096
10004047  lea      eax, [ebp - 0x14]
1000404a  push     eax
1000404b  push     0x1003650c
10004050  push     0
10004052  call     dword ptr [0x1002a004]
10004058  lea      eax, [ebp - 8]
1000405b  mov      dword ptr [ebp - 0x18], 1
10004062  push     eax
10004063  push     0
10004065  push     0
10004067  lea      eax, [ebp - 0x18]
1000406a  mov      dword ptr [ebp - 0xc], 2
10004071  push     eax
10004072  push     0
10004074  push     dword ptr [ebp - 0x1c]
10004077  call     dword ptr [0x1002a000]
1000407d  call     dword ptr [0x1002a0c4]
10004083  neg      eax
10004085  sbb      eax, eax
10004087  inc      eax
10004088  mov      ecx, dword ptr [ebp - 4]
1000408b  xor      ecx, ebp
1000408d  call     0x1000c22e
10004092  mov      esp, ebp
10004094  pop      ebp
10004095  ret      
10004096  mov      ecx, dword ptr [ebp - 4]
10004099  xor      eax, eax
1000409b  xor      ecx, ebp
1000409d  call     0x1000c22e
100040a2  mov      esp, ebp
100040a4  pop      ebp
100040a5  ret      
100040a6  int3     
100040a7  int3     
100040a8  int3     
100040a9  int3     
100040aa  int3     
100040ab  int3     
100040ac  int3     
100040ad  int3     
100040ae  int3     
100040af  int3     
100040b0  push     ebp
100040b1  mov      ebp, esp
100040b3  sub      esp, 0x14
100040b6  mov      eax, dword ptr [0x1003b070]
100040bb  xor      eax, ebp
100040bd  mov      dword ptr [ebp - 4], eax
100040c0  mov      eax, dword ptr [ebp + 8]
100040c3  push     ebx
100040c4  push     esi
100040c5  push     edi
100040c6  mov      dword ptr [ebp - 0xc], edx
100040c9  xor      esi, esi
100040cb  mov      dword ptr [ebp - 0x10], ecx
100040ce  mov      dword ptr [ebp - 0x14], eax
100040d1  call     0x10004020
100040d6  test     eax, eax
100040d8  jne      0x100040ed
100040da  xor      eax, eax
100040dc  pop      edi
100040dd  pop      esi
100040de  pop      ebx
100040df  mov      ecx, dword ptr [ebp - 4]
100040e2  xor      ecx, ebp
100040e4  call     0x1000c22e
100040e9  mov      esp, ebp
100040eb  pop      ebp
100040ec  ret      
100040ed  push     0x10036548
100040f2  push     0x1003656c
100040f7  call     dword ptr [0x1002a0ac]
100040fd  push     eax
100040fe  call     dword ptr [0x1002a030]
10004104  mov      ebx, eax
10004106  test     ebx, ebx
10004108  je       0x100040da
1000410a  push     0x8000
1000410f  call     0x10013d2c
10004114  mov      edi, eax
10004116  add      esp, 4
10004119  test     edi, edi
1000411b  je       0x10004163
1000411d  lea      eax, [ebp - 8]
10004120  mov      dword ptr [ebp - 8], 7
10004127  push     eax
10004128  push     0x8000
1000412d  push     edi
1000412e  push     dword ptr [ebp - 0xc]
10004131  push     dword ptr [ebp - 0x10]
10004134  call     ebx
10004136  mov      esi, eax
10004138  test     esi, esi
1000413a  jne      0x10004144
1000413c  call     dword ptr [0x1002a0c4]
10004142  jmp      0x1000415a
10004144  cmp      dword ptr [ebp + 0xc], esi
10004147  jb       0x10004158
10004149  push     esi
1000414a  push     edi
1000414b  push     dword ptr [ebp - 0x14]
1000414e  call     0x1000f060
10004153  add      esp, 0xc
10004156  jmp      0x1000415a
10004158  xor      esi, esi
1000415a  push     edi
1000415b  call     0x10011817
10004160  add      esp, 4
10004163  mov      ecx, dword ptr [ebp - 4]
10004166  mov      eax, esi
10004168  pop      edi
10004169  pop      esi
1000416a  xor      ecx, ebp
1000416c  pop      ebx
1000416d  call     0x1000c22e
10004172  mov      esp, ebp
10004174  pop      ebp
10004175  ret      
10004176  int3     
10004177  int3     
10004178  int3     
10004179  int3     
1000417a  int3     
1000417b  int3     
1000417c  int3     
1000417d  int3     
1000417e  int3     
1000417f  int3     
10004180  push     ebp
10004181  mov      ebp, esp
10004183  and      esp, 0xfffffff8
10004186  push     ecx
10004187  push     ebx
10004188  push     0
1000418a  xor      bl, bl
1000418c  call     dword ptr [0x1002a200]
10004192  test     eax, eax
10004194  jns      0x100041b0
10004196  push     0
10004198  call     dword ptr [0x1002a200]
1000419e  test     eax, eax
100041a0  jns      0x100041b0
100041a2  cmp      eax, 0x80010106
100041a7  je       0x100041b0
100041a9  mov      edx, 0x100365e0
100041ae  jmp      0x100041ed
100041b0  push     0x1003cea4
100041b5  push     0x100365a0
100041ba  call     dword ptr [0x1002a204]
100041c0  test     eax, eax
100041c2  js       0x100041e8
100041c4  push     0x1003cea0
100041c9  push     0x1003706c
100041ce  push     0x15
100041d0  push     0
100041d2  push     0x1003cea4
100041d7  call     dword ptr [0x1002a210]
100041dd  test     eax, eax
100041df  js       0x10004200
100041e1  mov      al, 1
100041e3  pop      ebx
100041e4  mov      esp, ebp
100041e6  pop      ebp
100041e7  ret      
100041e8  mov      edx, 0x100365c8
100041ed  mov      ecx, 0x1003c138
100041f2  call     0x100089c0
100041f7  push     eax
100041f8  call     0x10008d50
100041fd  add      esp, 4
10004200  mov      al, bl
10004202  pop      ebx
10004203  mov      esp, ebp
10004205  pop      ebp
10004206  ret      
10004207  int3     
10004208  int3     
10004209  int3     
1000420a  int3     
1000420b  int3     
1000420c  int3     
1000420d  int3     
1000420e  int3     
1000420f  int3     
10004210  push     ebp
10004211  mov      ebp, esp
10004213  and      esp, 0xfffffff8
10004216  sub      esp, 0x24
10004219  mov      eax, dword ptr [0x1003b070]
1000421e  xor      eax, esp
10004220  mov      dword ptr [esp + 0x20], eax
10004224  push     esi
10004225  mov      esi, ecx
10004227  mov      dword ptr [esp + 8], 0xffffffff
1000422f  mov      dword ptr [esi], 0
10004235  mov      ecx, dword ptr [0x1003cea0]
1000423b  test     ecx, ecx
1000423d  je       0x100042d7
10004243  lea      edx, [esp + 8]
10004247  mov      dword ptr [esp + 0xc], 1
1000424f  mov      eax, dword ptr [ecx]
10004251  push     edx
10004252  lea      edx, [esp + 0x10]
10004256  push     edx
10004257  push     0x18
10004259  push     0x10037028
1000425e  push     ecx
1000425f  call     dword ptr [eax + 0xac]
10004265  test     eax, eax
10004267  js       0x100042ae
10004269  mov      eax, dword ptr [esp + 8]
1000426d  test     eax, eax
1000426f  jne      0x10004285
10004271  mov      eax, dword ptr [esp + 0x14]
10004275  pop      esi
10004276  mov      ecx, dword ptr [esp + 0x20]
1000427a  xor      ecx, esp
1000427c  call     0x1000c22e
10004281  mov      esp, ebp
10004283  pop      ebp
10004284  ret      
10004285  push     eax
10004286  push     ecx
10004287  mov      edx, 0x100365f4
1000428c  mov      ecx, 0x1003c138
10004291  call     0x100089c0
10004296  add      esp, 4
10004299  mov      ecx, eax
1000429b  call     0x10005710
100042a0  push     eax
100042a1  call     0x10008d50
100042a6  mov      dword ptr [esi], 0xffffffff
100042ac  jmp      0x100042f2
100042ae  push     eax
100042af  push     ecx
100042b0  mov      edx, 0x10036620
100042b5  mov      ecx, 0x1003c138
100042ba  call     0x100089c0
100042bf  add      esp, 4
100042c2  mov      ecx, eax
100042c4  call     0x100058b0
100042c9  push     eax
100042ca  call     0x10008d50
100042cf  mov      dword ptr [esi], 0xfffffffe
100042d5  jmp      0x100042f2
100042d7  mov      edx, 0x1003664c
100042dc  mov      ecx, 0x1003c138
100042e1  call     0x100089c0
100042e6  push     eax
100042e7  call     0x10008d50
100042ec  mov      dword ptr [esi], 0xfffffffd
100042f2  mov      ecx, dword ptr [esp + 0x28]
100042f6  add      esp, 4
100042f9  mov      eax, dword ptr [esp + 8]
100042fd  pop      esi
100042fe  xor      ecx, esp
10004300  call     0x1000c22e
10004305  mov      esp, ebp
10004307  pop      ebp
10004308  ret      
10004309  int3     
1000430a  int3     
1000430b  int3     
1000430c  int3     
1000430d  int3     
1000430e  int3     
1000430f  int3     
10004310  push     ebp
10004311  mov      ebp, esp
10004313  sub      esp, 0x20
10004316  mov      eax, dword ptr [0x1003b070]
1000431b  xor      eax, ebp
1000431d  mov      dword ptr [ebp - 4], eax
10004320  push     esi
10004321  push     edi
10004322  mov      edi, ecx
10004324  mov      dword ptr [ebp - 8], 0xffffffff
1000432b  mov      esi, edx
1000432d  mov      edx, 0x10036680
10004332  push     edi
10004333  push     ecx
10004334  mov      ecx, 0x1003c138
10004339  mov      dword ptr [esi], 0
1000433f  call     0x100089c0
10004344  add      esp, 4
10004347  mov      ecx, eax
10004349  call     0x10005710
1000434e  push     eax
1000434f  call     0x10008d50
10004354  mov      ecx, dword ptr [0x1003cea0]
1000435a  add      esp, 4
1000435d  test     ecx, ecx
1000435f  je       0x100043c5
10004361  lea      edx, [ebp - 8]
10004364  mov      dword ptr [ebp - 0xc], 0
1000436b  push     edx
1000436c  lea      edx, [ebp - 0x20]
1000436f  mov      dword ptr [ebp - 0x20], 1
10004376  push     edx
10004377  xorps    xmm0, xmm0
1000437a  movups   xmmword ptr [ebp - 0x1c], xmm0
1000437e  push     0x18
10004380  mov      dword ptr [ebp - 0x18], edi
10004383  mov      dword ptr [ebp - 0x14], 0
1000438a  mov      eax, dword ptr [ecx]
1000438c  push     0x10037028
10004391  push     ecx
10004392  call     dword ptr [eax + 0xb0]
10004398  test     eax, eax
1000439a  jns      0x100043e3
1000439c  push     eax
1000439d  push     ecx
1000439e  mov      edx, 0x100366c8
100043a3  mov      ecx, 0x1003c138
100043a8  call     0x100089c0
100043ad  add      esp, 4
100043b0  mov      ecx, eax
100043b2  call     0x100058b0
100043b7  push     eax
100043b8  call     0x10008d50
100043bd  mov      dword ptr [esi], 0xfffffffe
100043c3  jmp      0x100043e0
100043c5  mov      edx, 0x100366f4
100043ca  mov      ecx, 0x1003c138
100043cf  call     0x100089c0
100043d4  push     eax
100043d5  call     0x10008d50
100043da  mov      dword ptr [esi], 0xfffffffd
100043e0  add      esp, 4
100043e3  mov      ecx, dword ptr [ebp - 4]
100043e6  mov      eax, dword ptr [ebp - 8]
100043e9  xor      ecx, ebp
100043eb  pop      edi
100043ec  pop      esi
100043ed  call     0x1000c22e
100043f2  mov      esp, ebp
100043f4  pop      ebp
100043f5  ret      
100043f6  int3     
100043f7  int3     
100043f8  int3     
100043f9  int3     
100043fa  int3     
100043fb  int3     
100043fc  int3     
100043fd  int3     
100043fe  int3     
100043ff  int3     