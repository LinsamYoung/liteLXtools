"""只读输出 PE 身份、导入导出、带偏移字符串和选定地址反汇编。"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import sys

sys.path.insert(0, str(Path(__file__).parent / 'vendor'))
import pefile
import capstone


def inspect(path, pattern):
    data = Path(path).read_bytes()
    pe = pefile.PE(data=data)
    strings = []
    for encoding, regex in [('ascii', rb'[\x20-\x7e]{5,}'), ('utf-16le', rb'(?:[\x20-\x7e]\x00){5,}')]:
        for match in re.finditer(regex, data):
            value = match.group().decode(encoding)
            if re.search(pattern, value, re.I):
                strings.append(dict(offset=hex(match.start()), value=value, encoding=encoding))
    exports = [dict(name=(s.name or b'').decode(errors='replace'), rva=hex(s.address), forwarder=str(s.forwarder))
               for s in getattr(pe, 'DIRECTORY_ENTRY_EXPORT', type('Empty', (), {'symbols': []})).symbols]
    imports = [dict(dll=e.dll.decode(), symbols=[dict(name=(s.name or b'').decode(errors='replace'), iat=hex(s.address)) for s in e.imports])
               for e in getattr(pe, 'DIRECTORY_ENTRY_IMPORT', [])]
    return dict(path=str(Path(path).resolve()), sha256=hashlib.sha256(data).hexdigest(), machine=hex(pe.FILE_HEADER.Machine),
                image_base=hex(pe.OPTIONAL_HEADER.ImageBase), exports=exports, imports=imports, strings=strings)


def disassemble(path, rva, size):
    pe = pefile.PE(path)
    engine = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_64 if pe.FILE_HEADER.Machine == 0x8664 else capstone.CS_MODE_32)
    return '\n'.join('%08x  %-8s %s' % (i.address, i.mnemonic, i.op_str)
                     for i in engine.disasm(pe.get_data(rva, size), pe.OPTIONAL_HEADER.ImageBase + rva))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('path')
    parser.add_argument('--pattern', default=r'logo|bios|efi|wmi|acpi|energy|battery|fan|dust|charge|\.dll|\.sys')
    parser.add_argument('--rva', type=lambda x: int(x, 0))
    parser.add_argument('--size', type=lambda x: int(x, 0), default=512)
    parser.add_argument('--out')
    args = parser.parse_args()
    result = disassemble(args.path, args.rva, args.size) if args.rva is not None else json.dumps(inspect(args.path, args.pattern), ensure_ascii=False, indent=2)
    if args.out:
        Path(args.out).write_text(result, encoding='utf-8')
    else:
        print(result)
