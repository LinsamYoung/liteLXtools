import sys,json,pathlib,re,hashlib
sys.path.insert(0,str(pathlib.Path('tools/vendor').resolve()))
import pefile
root=pathlib.Path(r'C:\Program Files (x86)\Lenovo\PCManager\5.1.210.10222')
files=['plugins/IdeaFanPlugin.dll','GameSettingsPlugin.dll','Modules/drivers/LnCoolit/x64/LnCoolit.sys']
out=[]
for rel in files:
 p=root/rel; raw=p.read_bytes(); pe=pefile.PE(data=raw)
 item={'path':str(p),'sha256':hashlib.sha256(raw).hexdigest(),'image_base':hex(pe.OPTIONAL_HEADER.ImageBase),'machine':hex(pe.FILE_HEADER.Machine),'exports':[], 'imports':[], 'strings':[]}
 for e in getattr(pe,'DIRECTORY_ENTRY_EXPORT',type('o',(),{'symbols':[]})()).symbols:
  item['exports'].append({'name':e.name.decode(errors='replace') if e.name else None,'rva':hex(e.address),'ordinal':e.ordinal})
 for d in getattr(pe,'DIRECTORY_ENTRY_IMPORT',[]): item['imports'].append({'dll':d.dll.decode(),'names':[i.name.decode(errors='replace') if i.name else '#'+str(i.ordinal) for i in d.imports]})
 for enc,pat in [('ascii',rb'[\x20-\x7e]{5,}'),('utf16',rb'(?:[\x20-\x7e]\x00){5,}')]:
  for m in re.finditer(pat,raw):
   s=m.group().decode('utf-16le' if enc=='utf16' else 'ascii')
   if re.search('fan|dust|energy|acpi|wmi|cool|clean|device|driver|bios|dll|root|Lenovo',s,re.I):item['strings'].append({'offset':hex(m.start()),'encoding':enc,'value':s})
 out.append(item)
pathlib.Path('analysis/evidence/fan-pe.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf-8')
for i in out:
 print(i['path'],i['sha256']);print('EXPORTS',json.dumps(i['exports']));print('IMPORTS',json.dumps(i['imports'])); print('STRINGS',json.dumps(i['strings'],ensure_ascii=False))
