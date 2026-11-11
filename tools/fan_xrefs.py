import sys,pathlib,json,re
sys.path.insert(0,str(pathlib.Path('tools/vendor').resolve()))
import pefile,capstone
p=pathlib.Path(r'C:\Program Files (x86)\Lenovo\PCManager\5.1.210.10222\plugins\IdeaFanPlugin.dll');pe=pefile.PE(str(p));raw=p.read_bytes();base=pe.OPTIONAL_HEADER.ImageBase
cs=capstone.Cs(capstone.CS_ARCH_X86,capstone.CS_MODE_32);cs.detail=True
imports={i.address:i.name.decode() for d in pe.DIRECTORY_ENTRY_IMPORT for i in d.imports if i.name}
strings=json.loads(pathlib.Path('analysis/evidence/fan-pe.json').read_text(encoding='utf-8'))[0]['strings']; wanted={base+pe.get_rva_from_offset(int(s['offset'],16)):s['value'] for s in strings if s['encoding']=='utf16' and any(x in s['value'] for x in ['EnergyDrv','DoDustRemoval','CancelDustRemoval','fan dust','DoDeviceIoControl','get-dust','do-dust','cancel-dust'])}
ins=[]
for sec in pe.sections:
 if sec.Characteristics&0x20000000:ins+=list(cs.disasm(sec.get_data(),base+sec.VirtualAddress))
rows=[]
for idx,i in enumerate(ins):
 refs=[]
 for op in i.operands:
  if op.type==capstone.x86.X86_OP_IMM and op.imm in wanted:refs.append(wanted[op.imm])
  if op.type==capstone.x86.X86_OP_MEM and op.mem.disp in imports and imports[op.mem.disp] in ['DeviceIoControl','CreateFileW']:refs.append(imports[op.mem.disp])
 if refs:
  lines=[hex(x.address)+' '+x.mnemonic+' '+x.op_str for x in ins[max(0,idx-16):idx+14]]
  rows.append({'refs':refs,'address':hex(i.address),'assembly':lines})
pathlib.Path('analysis/evidence/fan-xrefs.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
for r in rows:print('\nREF',r['refs'],r['address']);print('\n'.join(r['assembly']))
