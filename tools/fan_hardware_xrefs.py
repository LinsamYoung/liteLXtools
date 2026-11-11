import sys,pathlib,json,re
sys.path.insert(0,str(pathlib.Path('tools/vendor').resolve()))
import pefile,capstone
p=pathlib.Path(r'C:\Program Files (x86)\Lenovo\PCManager\5.1.210.10222\plugins\WSHardwarePlugin.dll');pe=pefile.PE(str(p));base=pe.OPTIONAL_HEADER.ImageBase
cs=capstone.Cs(capstone.CS_ARCH_X86,capstone.CS_MODE_32);cs.detail=True;cs.skipdata=True
obj=json.loads(pathlib.Path('analysis/evidence/fan-hardware-pe.json').read_text(encoding='utf-8'))
wanted={int(s['va'],16):s['value'] for s in obj['strings'] if any(x in s['value'] for x in ['Fan_Get_FullSpeed','Fan_Set_FullSpeed','SetFanCooling','GetFanCoolingStatus','fan dust','DoDustRemoval','CancelDustRemoval','DustRemovalByWMI','FanRun'])}
ins=[]
for sec in pe.sections:
 if sec.Characteristics&0x20000000:ins+=list(cs.disasm(sec.get_data(),base+sec.VirtualAddress))
rows=[]
for idx,i in enumerate(ins):
 refs=[]
 if i.id == 0: continue
 for op in i.operands:
  if op.type==capstone.x86.X86_OP_IMM and op.imm in wanted:refs.append(wanted[op.imm])
 if refs:
  lines=[hex(x.address)+' '+x.mnemonic+' '+x.op_str for x in ins[max(0,idx-15):idx+24]]
  rows.append({'refs':refs,'address':hex(i.address),'assembly':lines})
pathlib.Path('analysis/evidence/fan-hardware-xrefs.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
for r in rows:print('\nREF',r['refs'],r['address']);print('\n'.join(r['assembly']))

