10004870  push     ebp
10004871  mov      ebp, esp
10004873  sub      esp, 0xc
10004876  push     esi
10004877  lea      ecx, [ebp - 0xc]
1000487a  call     0x100046a0
1000487f  mov      ecx, dword ptr [ebp + 8]
10004882  mov      edx, dword ptr [ebp + 0xc]
10004885  movzx    esi, al
10004888  mov      al, byte ptr [ebp - 0xc]
1000488b  mov      byte ptr [ecx], al
1000488d  mov      eax, esi
1000488f  mov      cl, byte ptr [ebp - 3]
10004892  mov      byte ptr [edx], cl
10004894  mov      edx, dword ptr [ebp + 0x14]
10004897  mov      ecx, dword ptr [ebp - 0xb]
1000489a  pop      esi
1000489b  mov      dword ptr [edx], ecx
1000489d  mov      edx, dword ptr [ebp + 0x10]
100048a0  mov      ecx, dword ptr [ebp - 7]
100048a3  mov      dword ptr [edx], ecx
100048a5  mov      esp, ebp
100048a7  pop      ebp
100048a8  ret      
100048a9  int3     
100048aa  int3     
100048ab  int3     
100048ac  int3     
100048ad  int3     
100048ae  int3     
100048af  int3     
100048b0  push     ebp
100048b1  mov      ebp, esp
100048b3  and      esp, 0xfffffff8
100048b6  sub      esp, 0x10
100048b9  mov      eax, dword ptr [0x1003b070]
100048be  xor      eax, esp
100048c0  mov      dword ptr [esp + 0xc], eax
100048c4  push     ebx
100048c5  mov      bl, byte ptr [ebp + 8]
100048c8  mov      edx, 0x10036868
100048cd  push     esi
100048ce  mov      ecx, 0x1003c138
100048d3  call     0x100089c0
100048d8  mov      dl, bl
100048da  mov      ecx, eax
100048dc  call     0x100098a0
100048e1  push     eax
100048e2  call     0x10008d50
100048e7  add      esp, 4
100048ea  lea      ecx, [esp + 8]
100048ee  call     0x100046a0
100048f3  lea      ecx, [esp + 8]
100048f7  mov      byte ptr [esp + 8], bl
100048fb  call     0x100047c0
10004900  mov      bl, byte ptr [esp + 8]
10004904  mov      edx, 0x10036888
10004909  mov      ecx, 0x1003c138
1000490e  movzx    esi, al
10004911  call     0x100089c0
10004916  mov      dl, bl
10004918  mov      ecx, eax
1000491a  call     0x100098a0
1000491f  push     eax
10004920  call     0x10008d50
10004925  mov      ecx, dword ptr [esp + 0x18]
10004929  add      esp, 4
1000492c  mov      eax, esi
1000492e  pop      esi
1000492f  pop      ebx
10004930  xor      ecx, esp
10004932  call     0x1000c22e
10004937  mov      esp, ebp
10004939  pop      ebp
1000493a  ret      
1000493b  int3     
1000493c  int3     
1000493d  int3     
1000493e  int3     
1000493f  int3     
10004940  push     ebp
10004941  mov      ebp, esp
10004943  sub      esp, 0x234
10004949  mov      eax, dword ptr [0x1003b070]
1000494e  xor      eax, ebp
10004950  mov      dword ptr [ebp - 4], eax
10004953  push     ebx
10004954  push     esi
10004955  push     edi
10004956  push     0x100368a4
1000495b  push     ecx
1000495c  call     0x10014668
10004961  mov      ebx, eax
10004963  add      esp, 8
10004966  test     ebx, ebx
10004968  je       0x10004a28
1000496e  push     2
10004970  push     0
10004972  push     ebx
10004973  call     0x10014954
10004978  add      esp, 0xc
1000497b  push     ebx
1000497c  call     0x10014f4d
10004981  add      esp, 4
10004984  mov      edi, eax
10004986  push     0
10004988  push     0
1000498a  push     ebx
1000498b  call     0x10014954
10004990  add      esp, 0xc
10004993  mov      eax, 0x200
10004998  cmp      edi, eax
1000499a  cmovg    edi, eax
1000499d  push     eax
1000499e  lea      eax, [ebp - 0x208]
100049a4  push     0
100049a6  push     eax
100049a7  call     0x1000eac0
100049ac  add      esp, 0xc
100049af  lea      eax, [ebp - 0x208]
100049b5  push     ebx
100049b6  push     edi
100049b7  push     1
100049b9  push     eax
100049ba  call     0x10015124
100049bf  add      esp, 0x10
100049c2  test     eax, eax
100049c4  je       0x10004a28
100049c6  or       esi, 0xffffffff
100049c9  lea      ecx, [ebp - 0x208]
100049cf  test     edi, edi
100049d1  je       0x100049ed
100049d3  movzx    eax, byte ptr [ecx]
100049d6  lea      ecx, [ecx + 1]
100049d9  xor      eax, esi
100049db  shr      esi, 8
100049de  movzx    eax, al
100049e1  xor      esi, dword ptr [eax*4 + 0x10036c28]
100049e8  sub      edi, 1
100049eb  jne      0x100049d3
100049ed  lea      ecx, [ebp - 0x230]
100049f3  not      esi
100049f5  call     0x10008e00
100049fa  push     ebx
100049fb  mov      dword ptr [ebp - 0x22c], esi
10004a01  call     0x10014527
10004a06  add      esp, 4
10004a09  lea      ecx, [ebp - 0x230]
10004a0f  call     0x10008f20
10004a14  movzx    eax, al
10004a17  pop      edi
10004a18  pop      esi
10004a19  pop      ebx
10004a1a  mov      ecx, dword ptr [ebp - 4]
10004a1d  xor      ecx, ebp
10004a1f  call     0x1000c22e
10004a24  mov      esp, ebp
10004a26  pop      ebp
10004a27  ret      
10004a28  mov      ecx, dword ptr [ebp - 4]
10004a2b  xor      eax, eax
10004a2d  pop      edi
10004a2e  pop      esi
10004a2f  xor      ecx, ebp
10004a31  pop      ebx
10004a32  call     0x1000c22e
10004a37  mov      esp, ebp
10004a39  pop      ebp
10004a3a  ret      
10004a3b  int3     
10004a3c  int3     
10004a3d  int3     
10004a3e  int3     
10004a3f  int3     
10004a40  push     ebp
10004a41  mov      ebp, esp
10004a43  push     -1
10004a45  push     0x10028c33
10004a4a  mov      eax, dword ptr fs:[0]
10004a50  push     eax
10004a51  sub      esp, 0x7c
10004a54  mov      eax, dword ptr [0x1003b070]
10004a59  xor      eax, ebp
10004a5b  mov      dword ptr [ebp - 0x10], eax
10004a5e  push     ebx
10004a5f  push     esi
10004a60  push     edi
10004a61  push     eax
10004a62  lea      eax, [ebp - 0xc]
10004a65  mov      dword ptr fs:[0], eax
10004a6b  push     0x100368a4
10004a70  push     ecx
10004a71  call     0x10014668
10004a76  mov      esi, eax
10004a78  add      esp, 8
10004a7b  test     esi, esi
10004a7d  je       0x10004bf1
10004a83  push     2
10004a85  push     0
10004a87  push     esi
10004a88  call     0x10014954
10004a8d  add      esp, 0xc
10004a90  push     esi
10004a91  call     0x10014f4d
10004a96  add      esp, 4
10004a99  mov      edi, eax
10004a9b  push     0
10004a9d  push     0
10004a9f  push     esi
10004aa0  call     0x10014954
10004aa5  mov      eax, 0x1400000
10004aaa  add      esp, 0xc
10004aad  cmp      edi, eax
10004aaf  cmovg    edi, eax
10004ab2  push     edi
10004ab3  call     0x1000c814
10004ab8  add      esp, 4
10004abb  mov      ebx, eax
10004abd  push     esi
10004abe  push     edi
10004abf  push     1
10004ac1  push     ebx
10004ac2  mov      dword ptr [ebx], 0
10004ac8  call     0x10015124
10004acd  add      esp, 0x10
10004ad0  test     eax, eax
10004ad2  je       0x10004bba
10004ad8  push     edi
10004ad9  push     ebx
10004ada  lea      ecx, [ebp - 0x2c]
10004add  mov      dword ptr [ebp - 0x18], 0xf
10004ae4  mov      dword ptr [ebp - 0x1c], 0
10004aeb  mov      byte ptr [ebp - 0x2c], 0
10004aef  call     0x10005250
10004af4  mov      dword ptr [ebp - 4], 0
10004afb  lea      edx, [ebp - 0x2c]
10004afe  lea      ecx, [ebp - 0x14]
10004b01  mov      dword ptr [ebp - 0x14], 0
10004b08  call     0x10002a00
10004b0d  mov      byte ptr [ebp - 4], 1
10004b11  lea      ecx, [ebp - 0x84]
10004b17  mov      edx, dword ptr [ebp - 0x14]
10004b1a  call     0x100024e0
10004b1f  mov      edx, eax
10004b21  mov      byte ptr [ebp - 4], 2
10004b25  lea      ecx, [ebp - 0x44]
10004b28  call     0x100025a0
10004b2d  lea      ecx, [ebp - 0x84]
10004b33  mov      byte ptr [ebp - 4], 4
10004b37  call     0x10005480
10004b3c  lea      ecx, [ebp - 0x6c]
10004b3f  call     0x10008e00
10004b44  cmp      dword ptr [ebp - 0x30], 0x10
10004b48  lea      eax, [ebp - 0x44]
10004b4b  lea      ecx, [ebp - 0x6c]
10004b4e  cmovae   eax, dword ptr [ebp - 0x44]
10004b52  movups   xmm0, xmmword ptr [eax]
10004b55  movups   xmmword ptr [ebp - 0x68], xmm0
10004b59  movups   xmm0, xmmword ptr [eax + 0x10]
10004b5d  movups   xmmword ptr [ebp - 0x58], xmm0
10004b61  call     0x10008f20
10004b66  movzx    edi, al
10004b69  push     edi
10004b6a  push     0x456
10004b6f  push     0x100368a8
10004b74  push     0x100368c4
10004b79  push     3
10004b7b  call     0x10002b50
10004b80  push     eax
10004b81  call     0x10002f10
10004b86  add      esp, 0x18
10004b89  lea      ecx, [ebp - 0x44]
10004b8c  call     0x10005480
10004b91  mov      byte ptr [ebp - 4], 0
10004b95  or       eax, 0xffffffff
10004b98  mov      edx, dword ptr [ebp - 0x14]
10004b9b  add      edx, -0x10
10004b9e  lock xadd dword ptr [edx + 0xc], eax
10004ba3  dec      eax
10004ba4  test     eax, eax
10004ba6  jg       0x10004bb0
10004ba8  mov      ecx, dword ptr [edx]
10004baa  push     edx
10004bab  mov      eax, dword ptr [ecx]
10004bad  call     dword ptr [eax + 4]
10004bb0  lea      ecx, [ebp - 0x2c]
10004bb3  call     0x10005480
10004bb8  jmp      0x10004bdb
10004bba  push     0x45a
10004bbf  push     0x100368a8
10004bc4  push     0x100368f8
10004bc9  push     3
10004bcb  call     0x10002b50
10004bd0  push     eax
10004bd1  call     0x10002f10
10004bd6  add      esp, 0x14
10004bd9  xor      edi, edi
10004bdb  push     ebx
10004bdc  call     0x1000c5a0
10004be1  add      esp, 4
10004be4  push     esi
10004be5  call     0x10014527
10004bea  add      esp, 4
10004bed  mov      eax, edi
10004bef  jmp      0x10004c12
10004bf1  push     0x461
10004bf6  push     0x100368a8
10004bfb  push     0x10036930
10004c00  push     3
10004c02  call     0x10002b50
10004c07  push     eax
10004c08  call     0x10002f10
10004c0d  add      esp, 0x14
10004c10  xor      eax, eax
10004c12  mov      ecx, dword ptr [ebp - 0xc]
10004c15  mov      dword ptr fs:[0], ecx
10004c1c  pop      ecx
10004c1d  pop      edi
10004c1e  pop      esi
10004c1f  pop      ebx
10004c20  mov      ecx, dword ptr [ebp - 0x10]
10004c23  xor      ecx, ebp
10004c25  call     0x1000c22e
10004c2a  mov      esp, ebp
10004c2c  pop      ebp
10004c2d  ret      
10004c2e  int3     
10004c2f  int3     
10004c30  push     ebp
10004c31  mov      ebp, esp
10004c33  and      esp, 0xfffffff8
10004c36  sub      esp, 0x28
10004c39  lea      ecx, [esp]
10004c3c  call     0x10008e00
10004c41  cmp      dword ptr [esp], 0x20003
10004c48  jb       0x10004c75
10004c4a  push     0x46a
10004c4f  push     0x100368a8
10004c54  push     0x10036970
10004c59  push     3
10004c5b  call     0x10002b50
10004c60  push     eax
10004c61  call     0x10002f10
10004c66  mov      ecx, dword ptr [ebp + 8]
10004c69  add      esp, 0x14
10004c6c  call     0x10004a40
10004c71  mov      esp, ebp
10004c73  pop      ebp
10004c74  ret      
10004c75  mov      ecx, dword ptr [ebp + 8]
10004c78  call     0x10004940
10004c7d  mov      esp, ebp
10004c7f  pop      ebp
10004c80  ret      
10004c81  int3     
10004c82  int3     
10004c83  int3     
10004c84  int3     
10004c85  int3     
10004c86  int3     
10004c87  int3     
10004c88  int3     
10004c89  int3     
10004c8a  int3     
10004c8b  int3     
10004c8c  int3     
10004c8d  int3     
10004c8e  int3     
10004c8f  int3     
10004c90  push     ebp
10004c91  mov      ebp, esp
10004c93  push     -1
10004c95  push     0x10028c70
10004c9a  mov      eax, dword ptr fs:[0]
10004ca0  push     eax
10004ca1  sub      esp, 0x30
10004ca4  mov      eax, dword ptr [0x1003b070]
10004ca9  xor      eax, ebp
10004cab  mov      dword ptr [ebp - 0x14], eax
10004cae  push     ebx
10004caf  push     esi
10004cb0  push     edi
10004cb1  push     eax
10004cb2  lea      eax, [ebp - 0xc]
10004cb5  mov      dword ptr fs:[0], eax
10004cbb  mov      dword ptr [ebp - 0x10], esp
10004cbe  mov      dword ptr [ebp - 0x3c], ecx
10004cc1  mov      dword ptr [ebp - 4], 0
10004cc8  xor      ebx, ebx
10004cca  mov      byte ptr [ebp - 4], 2
10004cce  push     ebx
10004ccf  push     ebx
10004cd0  push     ebx
10004cd1  push     3
10004cd3  push     ebx
10004cd4  push     ebx
10004cd5  push     ebx
10004cd6  push     -1
10004cd8  push     ebx
10004cd9  mov      dword ptr [ebp - 0x24], ebx
10004cdc  mov      dword ptr [ebp - 0x20], ebx
10004cdf  call     dword ptr [0x1002a208]
10004ce5  lea      edi, [ebx + 1]
10004ce8  test     eax, eax
10004cea  jns      0x10004cf4
10004cec  cmp      eax, 0x80010119
10004cf1  cmovne   ebx, edi
10004cf4  lea      eax, [ebp - 0x24]
10004cf7  mov      dword ptr [ebp - 0x24], 0
10004cfe  push     eax
10004cff  push     0x1002d588
10004d04  push     1
10004d06  push     0
10004d08  push     0x1002d578
10004d0d  call     dword ptr [0x1002a210]
10004d13  test     eax, eax
10004d15  jns      0x10004d5e
10004d17  mov      byte ptr [ebp - 4], 0
10004d1b  or       esi, 0xffffffff
10004d1e  mov      edx, dword ptr [ebp + 8]
10004d21  mov      eax, esi
10004d23  add      edx, -0x10
10004d26  lock xadd dword ptr [edx + 0xc], eax
10004d2b  dec      eax
10004d2c  test     eax, eax
10004d2e  jg       0x10004d38
10004d30  mov      ecx, dword ptr [edx]
10004d32  push     edx
10004d33  mov      eax, dword ptr [ecx]
10004d35  call     dword ptr [eax + 4]
10004d38  mov      dword ptr [ebp - 4], 0xffffffff
10004d3f  mov      edx, dword ptr [ebp + 0xc]
10004d42  add      edx, -0x10
10004d45  lock xadd dword ptr [edx + 0xc], esi
10004d4a  dec      esi
10004d4b  test     esi, esi
10004d4d  jg       0x10004d57
10004d4f  mov      ecx, dword ptr [edx]
10004d51  push     edx
10004d52  mov      eax, dword ptr [ecx]
10004d54  call     dword ptr [eax + 4]
10004d57  mov      eax, edi
10004d59  jmp      0x10004ecd
10004d5e  mov      eax, dword ptr [ebp - 0x24]
10004d61  lea      edx, [ebp - 0x20]
10004d64  push     edx
10004d65  push     0
10004d67  push     0
10004d69  push     0
10004d6b  push     0
10004d6d  push     0
10004d6f  push     0
10004d71  mov      dword ptr [ebp - 0x20], 0
10004d78  mov      ecx, dword ptr [eax]
10004d7a  push     0x100369d4
10004d7f  push     eax
10004d80  call     dword ptr [ecx + 0xc]
10004d83  test     eax, eax
10004d85  js       0x10004d17
10004d87  push     0
10004d89  push     0
10004d8b  push     3
10004d8d  push     3
10004d8f  push     0
10004d91  push     0
10004d93  push     0xa
10004d95  push     dword ptr [ebp - 0x20]
10004d98  call     dword ptr [0x1002a20c]
10004d9e  test     eax, eax
10004da0  js       0x10004d17
10004da6  mov      eax, dword ptr [ebp + 8]
10004da9  cmp      dword ptr [eax - 4], 1
10004dad  jle      0x10004dbd
10004daf  push     dword ptr [eax - 0xc]
10004db2  lea      ecx, [ebp + 8]
10004db5  call     0x10006d80
10004dba  mov      eax, dword ptr [ebp + 8]
10004dbd  push     eax
10004dbe  call     dword ptr [0x1002a1b0]
10004dc4  mov      ecx, dword ptr [ebp - 0x20]
10004dc7  lea      esi, [ebp - 0x1c]
10004dca  push     esi
10004dcb  push     0
10004dcd  push     0x30
10004dcf  push     eax
10004dd0  mov      dword ptr [ebp - 0x1c], 0
10004dd7  mov      edx, dword ptr [ecx]
10004dd9  push     0x100369ec
10004dde  push     ecx
10004ddf  call     dword ptr [edx + 0x50]
10004de2  test     eax, eax
10004de4  js       0x10004d17
10004dea  mov      esi, dword ptr [0x1002a1ac]
10004df0  mov      dword ptr [ebp - 0x28], 0
10004df7  mov      ecx, dword ptr [ebp - 0x1c]
10004dfa  test     ecx, ecx
10004dfc  je       0x10004e58
10004dfe  lea      edx, [ebp - 0x18]
10004e01  mov      dword ptr [ebp - 0x18], 0
10004e08  mov      eax, dword ptr [ecx]
10004e0a  push     edx
10004e0b  lea      edx, [ebp - 0x28]
10004e0e  push     edx
10004e0f  push     1
10004e11  push     -1
10004e13  push     ecx
10004e14  call     dword ptr [eax + 0x10]
10004e17  test     eax, eax
10004e19  jne      0x10004df7
10004e1b  cmp      dword ptr [ebp - 0x18], eax
10004e1e  je       0x10004e55
10004e20  lea      eax, [ebp - 0x38]
10004e23  push     eax
10004e24  call     esi
10004e26  mov      eax, dword ptr [ebp - 0x28]
10004e29  lea      edx, [ebp - 0x38]
10004e2c  push     0
10004e2e  push     0
10004e30  push     edx
10004e31  mov      ecx, dword ptr [eax]
10004e33  push     0
10004e35  push     dword ptr [ebp + 0xc]
10004e38  push     eax
10004e39  call     dword ptr [ecx + 0x10]
10004e3c  test     eax, eax
10004e3e  jne      0x10004df7
10004e40  push     dword ptr [ebp - 0x30]
10004e43  mov      ecx, dword ptr [ebp - 0x3c]
10004e46  call     0x10005e20
10004e4b  lea      eax, [ebp - 0x38]
10004e4e  push     eax
10004e4f  call     dword ptr [0x1002a1b4]
10004e55  mov      ecx, dword ptr [ebp - 0x1c]
10004e58  mov      edx, dword ptr [ebp - 0x28]
10004e5b  test     edx, edx
10004e5d  je       0x10004e68
10004e5f  mov      eax, dword ptr [edx]
10004e61  push     edx
10004e62  call     dword ptr [eax + 8]
10004e65  mov      ecx, dword ptr [ebp - 0x1c]
10004e68  test     ecx, ecx
10004e6a  je       0x10004e84
10004e6c  mov      eax, dword ptr [ecx]
10004e6e  push     ecx
10004e6f  call     dword ptr [eax + 8]
10004e72  jmp      0x10004e84
10004e74  mov      dword ptr [ebp - 0x3c], 1
10004e7b  mov      eax, 0x10004e81
10004e80  ret      
10004e81  mov      ebx, dword ptr [ebp - 0x3c]
10004e84  mov      dword ptr [ebp - 4], 1
10004e8b  or       esi, 0xffffffff
10004e8e  mov      byte ptr [ebp - 4], 0
10004e92  mov      eax, esi
10004e94  mov      edx, dword ptr [ebp + 8]
10004e97  add      edx, -0x10
10004e9a  lock xadd dword ptr [edx + 0xc], eax
10004e9f  dec      eax
10004ea0  test     eax, eax
10004ea2  jg       0x10004eac
10004ea4  mov      ecx, dword ptr [edx]
10004ea6  push     edx
10004ea7  mov      eax, dword ptr [ecx]
10004ea9  call     dword ptr [eax + 4]
10004eac  mov      dword ptr [ebp - 4], 0xffffffff
10004eb3  mov      eax, dword ptr [ebp + 0xc]
10004eb6  add      eax, -0x10
10004eb9  lock xadd dword ptr [eax + 0xc], esi
10004ebe  dec      esi
10004ebf  test     esi, esi
10004ec1  jg       0x10004ecb
10004ec3  mov      ecx, dword ptr [eax]
10004ec5  push     eax
10004ec6  mov      edx, dword ptr [ecx]
10004ec8  call     dword ptr [edx + 4]
10004ecb  mov      eax, ebx
10004ecd  mov      ecx, dword ptr [ebp - 0xc]
10004ed0  mov      dword ptr fs:[0], ecx
10004ed7  pop      ecx
10004ed8  pop      edi
10004ed9  pop      esi
10004eda  pop      ebx
10004edb  mov      ecx, dword ptr [ebp - 0x14]
10004ede  xor      ecx, ebp
10004ee0  call     0x1000c22e
10004ee5  mov      esp, ebp
10004ee7  pop      ebp
10004ee8  ret      
10004ee9  int3     
10004eea  int3     
10004eeb  int3     
10004eec  int3     
10004eed  int3     
10004eee  int3     
10004eef  int3     
10004ef0  int3     
10004ef1  int3     
10004ef2  int3     
10004ef3  int3     
10004ef4  int3     
10004ef5  int3     
10004ef6  int3     
10004ef7  int3     
10004ef8  int3     
10004ef9  int3     
10004efa  int3     
10004efb  int3     
10004efc  int3     
10004efd  int3     
10004efe  int3     
10004eff  int3     
10004f00  push     ebp
10004f01  mov      ebp, esp
10004f03  sub      esp, 0xc
10004f06  mov      eax, dword ptr [0x1003b070]
10004f0b  xor      eax, ebp
10004f0d  mov      dword ptr [ebp - 4], eax
10004f10  push     esi
10004f11  push     edi
10004f12  call     0x100014a0
10004f17  mov      ecx, eax
10004f19  test     ecx, ecx
10004f1b  je       0x100050a1
10004f21  mov      eax, dword ptr [ecx]
10004f23  call     dword ptr [eax + 0xc]
10004f26  push     ecx
10004f27  add      eax, 0x10
10004f2a  mov      ecx, esp
10004f2c  push     0x100369f4
10004f31  mov      dword ptr [ebp - 8], eax
10004f34  call     0x10005f00
10004f39  push     ecx
10004f3a  mov      ecx, esp
10004f3c  push     0x10036a08
10004f41  call     0x10005f00
10004f46  lea      ecx, [ebp - 8]
10004f49  call     0x10004c90
10004f4e  mov      esi, dword ptr [ebp - 8]
10004f51  add      esp, 8
10004f54  test     eax, eax
10004f56  jne      0x10005075
10004f5c  mov      eax, dword ptr [esi - 0xc]
10004f5f  test     eax, eax
10004f61  je       0x10005075
10004f67  js       0x10004f8e
10004f69  push     0x10036a50
10004f6e  push     esi
10004f6f  call     0x1000e6d6
10004f74  add      esp, 8
10004f77  test     eax, eax
10004f79  je       0x10004f8e
10004f7b  sub      eax, esi
10004f7d  sar      eax, 1
10004f7f  cmp      eax, -1
10004f82  je       0x10004f8e
10004f84  mov      edi, 1
10004f89  jmp      0x10005077
10004f8e  cmp      dword ptr [esi - 0xc], 0
10004f92  jl       0x10004fb9
10004f94  push     0x10036a58
10004f99  push     esi
10004f9a  call     0x1000e6d6
10004f9f  add      esp, 8
10004fa2  test     eax, eax
10004fa4  je       0x10004fb9
10004fa6  sub      eax, esi
10004fa8  sar      eax, 1
10004faa  cmp      eax, -1
10004fad  je       0x10004fb9
10004faf  mov      edi, 2
10004fb4  jmp      0x10005077
10004fb9  push     ecx
10004fba  push     0x10036a60
10004fbf  lea      ecx, [ebp - 8]
10004fc2  call     0x10005de0
10004fc7  cmp      eax, -1
10004fca  je       0x10004fd6
10004fcc  mov      edi, 3
10004fd1  jmp      0x10005077
10004fd6  push     ecx
10004fd7  push     0x10036a68
10004fdc  lea      ecx, [ebp - 8]
10004fdf  call     0x10005de0
10004fe4  cmp      eax, -1
10004fe7  je       0x10004ff3
10004fe9  mov      edi, 4
10004fee  jmp      0x10005077
10004ff3  push     ecx
10004ff4  push     0x10036a84
10004ff9  lea      ecx, [ebp - 8]
10004ffc  call     0x10005de0
10005001  cmp      eax, -1
10005004  je       0x1000500d
10005006  mov      edi, 5
1000500b  jmp      0x10005077
1000500d  push     ecx
1000500e  push     0x10036a9c
10005013  lea      ecx, [ebp - 8]
10005016  call     0x10005de0
1000501b  cmp      eax, -1
1000501e  je       0x10005027
10005020  mov      edi, 6
10005025  jmp      0x10005077
10005027  push     ecx
10005028  push     0x10036ab0
1000502d  lea      ecx, [ebp - 8]
10005030  call     0x10005de0
10005035  cmp      eax, -1
10005038  je       0x10005041
1000503a  mov      edi, 7