import sys,pathlib,re,json,hashlib
sys.path.insert(0,str(pathlib.Path('tools/vendor').resolve()))
import pefile
sys.stdout.reconfigure(encoding='utf-8')
r=pathlib.Path(r'C:\Program Files (x86)\Lenovo\PCManager\5.1.210.10222')
p=r/'plugins/WSHardwarePlugin.dll';b=p.read_bytes();pe=pefile.PE(data=b)
s=[]
for enc,pat in [('ascii',rb'[\x20-\x7e]{5,}'),('utf16',rb'(?:[\x20-\x7e]\x00){5,}')]:
 for m in re.finditer(pat,b):
  t=m.group().decode('utf-16le' if enc=='utf16' else 'ascii')
  if re.search('Energy|Dust|Fan|Cool|Control|BIOS|Acpi|Wmi|SmartService|IdeaFan',t,re.I):s.append({'offset':hex(m.start()),'va':hex(pe.OPTIONAL_HEADER.ImageBase+pe.get_rva_from_offset(m.start())),'encoding':enc,'value':t})
obj={'path':str(p),'sha256':hashlib.sha256(b).hexdigest(),'imports':[{'dll':d.dll.decode(),'names':[i.name.decode(errors='replace') for i in d.imports if i.name]} for d in pe.DIRECTORY_ENTRY_IMPORT],'strings':s}
pathlib.Path('analysis/evidence/fan-hardware-pe.json').write_text(json.dumps(obj,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(obj,ensure_ascii=False,indent=2))
t=(r/'skin/dist/js/main.js').read_text(encoding='utf-8')
for a,z in [(723650,725500),(901800,909500)]:print('JS',a,t[a:z])
