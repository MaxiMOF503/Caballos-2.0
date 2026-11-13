from pathlib import Path
import hashlib,json,re
p=Path(r'C:\Caballos\Caballos-2.0')
w=Path(r'C:\Caballos\Validation\Clasificacion')
c=Path(r'C:\Caballos\Validation\MainMenu')
h=lambda f:hashlib.sha256(f.read_bytes()).hexdigest()
protected=['CameraController.cs','HorseVisualAnimator.cs','ProgresionCaballo.cs','EstabloView.cs','MenuPausa.cs','CursorCaballos.cs']
for name in protected:
    assert h(p/'Assets/Scripts'/name)==h(w/'Antes'/name),name
assert h(p/'Assets/Scenes/hub.unity')==h(w/'Antes/hub.unity')
a=(p/'Assets/Scripts/CarreraUI.cs').read_text(encoding='utf-8-sig').split('public class CarreraUI')[0]
b=(w/'Antes/CarreraUI.cs').read_text(encoding='utf-8-sig').split('public class CarreraUI')[0]
assert a==b,'Motor de velocidad/energía modificado'
files=['ClasificacionCarrera.cs','ResultadosCarreraView.cs','CarreraUI.cs','CarreraProgramada.cs','GameManager.Carrera.cs','GameManager.cs','GameData.cs']
for name in files:
    file=p/'Assets/Scripts'/name
    assert h(file)==h(c/'Assets/Scripts'/name),name+' no corresponde a la copia validada'
    assert Path(str(file)+'.meta').is_file(),name+' sin meta'
assert h(p/'Assets/Editor/BetaJugableValidation.cs')==h(c/'Assets/Editor/BetaJugableValidation.cs')
new_guids=[]
for name in files[:2]:
    meta=(p/'Assets/Scripts'/(name+'.meta')).read_text()
    guid=re.search(r'^guid: ([a-f0-9]{32})',meta,re.M)[1]
    new_guids.append(guid)
    assert h(p/'Assets/Scripts'/(name+'.meta'))==h(c/'Assets/Scripts'/(name+'.meta'))
for guid in new_guids:
    found=[meta for meta in (p/'Assets').rglob('*.meta') if re.search(r'^guid: '+guid+r'$',meta.read_text(encoding='utf-8-sig',errors='replace'),re.M)]
    assert len(found)==1,(guid,found)
records=json.loads((p/'Catalogos/Animaciones/Produccion.json').read_text(encoding='utf-8'))
assert len(records)==100
for row in records:
    assert h(p/row['archivo'])==row['sha256PNG'],row['id']+' animación editada'
    assert h(p/row['referenciaSplash'])==row['sha256Splash'],row['id']+' splash editado'
    profile=p/'Assets/Resources/HorseRaceProfiles'/(row['id']+'.asset')
    assert h(profile)==h(c/profile.relative_to(p))
    assert h(Path(str(profile)+'.meta'))==h(c/(str(profile.relative_to(p))+'.meta'))
    text=profile.read_text(encoding='utf-8-sig')
    assert len(re.findall('fileID:',text.split('  runFrames:\n',1)[1].split('  framesPerSecond:',1)[0]))==8
archive=Path(r'C:\Caballos\Builds\Caballos-2.0-Beta-v0.1-Windows.zip')
assert h(archive).upper()=='E5D2CE8B9729EB9E83EEE9001A6E549451C509322C48CE76F2589F814E8A779F'
result=dict(motorSimulacionConservado=True,camaraHubConservada=True,escenaHubConservada=True,
    scriptsProtegidosConservados=protected,perfilesConservados=100,framesConservados=800,
    spritesSplashConservados=100,codigoCoincideConCopiaValidada=True,metasNuevosUnicos=2,zipAnteriorConservado=True,
    shaScripts={name:h(p/'Assets/Scripts'/name) for name in files})
(w/'integridad.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,ensure_ascii=False))
