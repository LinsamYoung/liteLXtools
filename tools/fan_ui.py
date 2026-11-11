import pathlib,re,json,sys
sys.stdout.reconfigure(encoding='utf-8')
root=pathlib.Path(r'C:\Program Files (x86)\Lenovo\PCManager\5.1.210.10222')
res=[]
for p in (root/'skin').rglob('*.js'):
 txt=p.read_text(encoding='utf-8',errors='replace')
 for m in re.finditer('do-dustremoval|cancel-dustremoval|get-dustremovalpermission|DustRemovalStatusResponse|DustRemovalPermissionResponse|tools_coolclean|SetFanCooling|GetFanCoolingStatus',txt):
  res.append({'file':str(p),'offset':m.start(),'needle':m.group(),'context':txt[max(0,m.start()-220):m.end()+300]})
pathlib.Path('analysis/evidence/fan-ui.json').write_text(json.dumps(res,ensure_ascii=False,indent=2),encoding='utf-8')
for x in res:print(json.dumps(x,ensure_ascii=False))
for p in [pathlib.Path(r'C:\Windows\System32\DriverStore\FileRepository\acpivpc.inf_amd64_07db38bc9e02b27d\AcpiVpc.sys')]:
 b=p.read_bytes()
 print('DRIVER',p)
 for m in re.finditer(rb'(?:[\x20-\x7e]\x00){5,}',b):
  s=m.group().decode('utf-16le')
  if re.search('Energy|Device|VPC|ACPI|Dust|Fan',s,re.I):print(hex(m.start()),s)
