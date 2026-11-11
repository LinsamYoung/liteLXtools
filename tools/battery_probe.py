import hashlib
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent / 'vendor'))
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from capstone.x86 import X86_OP_IMM, X86_OP_MEM

ROOT = Path(r'C:\Program Files (x86)\Lenovo\PCManager\5.1.210.10222')
OUT = Path(__file__).parent.parent / 'analysis' / 'evidence'
KEYWORDS = re.compile(r'batter|conserv|rapid|charge|energy|acpi|wmi|power|vpc|imbcontroller|\\\\\.\\|\.dll|reg.*mode|set.*mode|get.*mode', re.I)


def inspect(relative):
    path = ROOT / relative
    data = path.read_bytes()
    pe = pefile.PE(data=data)
    base = pe.OPTIONAL_HEADER.ImageBase
    imports = []
    for entry in getattr(pe, 'DIRECTORY_ENTRY_IMPORT', []):
        imports.append({'dll': entry.dll.decode(), 'symbols': [{'name': (s.name or b'').decode(errors='replace'), 'va': hex(s.address)} for s in entry.imports]})
    exports = [{'name': (s.name or b'').decode(errors='replace'), 'va': hex(base+s.address), 'rva': hex(s.address), 'forwarder': s.forwarder.decode() if s.forwarder else None} for s in getattr(getattr(pe, 'DIRECTORY_ENTRY_EXPORT', None), 'symbols', [])]
    strings = []
    for regex, enc in [(rb'[\x20-\x7e]{5,}', 'ascii'), (rb'(?:[\x20-\x7e]\x00){5,}', 'utf-16le')]:
        for match in re.finditer(regex, data):
            value = match.group().decode(enc)
            if KEYWORDS.search(value):
                strings.append({'offset': hex(match.start()), 'va': hex(base+pe.get_rva_from_offset(match.start())), 'encoding': enc, 'value': value})
    targets = {int(s['va'], 16): s['value'] for s in strings}
    targets.update({int(s['va'], 16): i['dll']+'!'+s['name'] for i in imports for s in i['symbols']})
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    md.detail = True
    md.skipdata = True
    xrefs = []
    for section in pe.sections:
        if not section.Characteristics & 0x20000000:
            continue
        for ins in md.disasm(section.get_data(), base+section.VirtualAddress):
            if ins.id == 0:
                continue
            for operand in ins.operands:
                address = operand.imm if operand.type == X86_OP_IMM else (operand.mem.disp if operand.type == X86_OP_MEM and operand.mem.base == 0 else None)
                if address in targets:
                    xrefs.append({'va': hex(ins.address), 'instruction': ins.mnemonic+' '+ins.op_str, 'target': targets[address], 'target_va': hex(address)})
    result = {'path': str(path), 'sha256': hashlib.sha256(data).hexdigest(), 'image_base': hex(base), 'imports': imports, 'exports': exports, 'strings': strings, 'xrefs': xrefs, 'limitations': ['线性反汇编只给出立即数/绝对内存操作数引用，未重建完整控制流，可能包含误解码，未执行任何目标代码。']}
    dest = OUT / ('battery-' + path.name + '.json')
    dest.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(path.name, result['sha256'])
    print('imports', ', '.join(i['dll'] for i in imports))
    print('exports', json.dumps(exports, ensure_ascii=False))
    print('strings', json.dumps(strings, ensure_ascii=False))


if __name__ == '__main__':
    for relative in sys.argv[1:]:
        inspect(relative)
