import pathlib,re,json,sys
sys.stdout.reconfigure(encoding='utf-8')
root=pathlib.Path(r'C:\Program Files (x86)\Lenovo\PCManager\5.1.210.10222')
res=[]
for p in (root/'skin/dist/js').glob('*.js'):
 txt=p.read_text(encoding='utf-8',errors='replace')
 for m in re.finditer('showCoolClean|coolclean|dustremoval|FanCooling|DustRemoval|fanCool',txt,re.I):
  res.append({'file':str(p),'offset':m.start(),'needle':m.group(),'context':txt[max(0,m.start()-130):m.end()+220]})
pathlib.Path('analysis/evidence/fan-ui-all.json').write_text(json.dumps(res,ensure_ascii=False,indent=2),encoding='utf-8')
print('matches',len(res))
for x in res[:45]:print(json.dumps(x,ensure_ascii=False))
for p in list((root/'plugins').glob('*.dll'))+[root/'CommonLibPlugin.dll',root/'WrapPlugin.dll',root/'GameSettingsPlugin.dll']:
 b=p.read_bytes()
 if re.search('CoolClean|DustRemoval|FanCooling'.encode('utf-16le'),b):print('MATCH DLL',p.name,len(b))
