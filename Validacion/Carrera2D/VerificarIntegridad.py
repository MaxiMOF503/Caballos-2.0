from pathlib import Path
import hashlib,json,re
p=Path(r'C:\Caballos\Caballos-2.0');w=Path(r'C:\Caballos\Validation\Carrera2D');c=Path(r'C:\Caballos\Validation\MainMenu')
h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
protected=['CameraController.cs','EstabloView.cs','TiendaView.cs','GameManager.Subastas.cs','HorseVisualAnimator.cs','ClasificacionCarrera.cs','ResultadosCarreraView.cs']
for n in protected: assert h(p/'Assets/Scripts'/n)==h(w/'Antes'/n),n+' modificado'
assert h(p/'Assets/Scenes/hub.unity')==h(w/'Antes/hub.unity')
a=(p/'Assets/Scripts/CarreraUI.cs').read_text(encoding='utf-8-sig').split('public class CarreraUI')[0]
b=(w/'Antes/CarreraUI.cs').read_text(encoding='utf-8-sig').split('public class CarreraUI')[0]
a=a.replace('public void Actualizar(float deltaTime, float distanciaCarrera, float ritmoIniciacion = 0)','public void Actualizar(float deltaTime, float distanciaCarrera)')
start=a.index('        if (ritmoIniciacion > 0)');end=a.index('        ActualizarEventoSuerte(deltaTime);',start)
a=a[:start]+a[end:]
assert a==b,'Fórmulas normales cambiaron fuera de la nueva rama de iniciación'
names=['ReglasCarrera.cs','PistaLateralCarrera.cs','CarreraUI.cs','CarreraProgramada.cs','GameManager.Carrera.cs','GameManager.cs','GameData.cs','CatalogoCarreras.cs','SeleccionCarreraView.cs','TablonView.cs','TablonLayout.cs']
for n in names:
    f=p/'Assets/Scripts'/n;assert h(f)==h(c/'Assets/Scripts'/n),n+' difiere de la copia validada'
    assert Path(str(f)+'.meta').is_file(),n+' sin .meta'
for folder in ['Assets/Editor/BetaJugableValidation.cs','Assets/Resources/Carreras/Catalogo.asset']:
    assert h(p/folder)==h(c/folder)
guids=[]
for n in names[:2]:
    m=p/'Assets/Scripts'/(n+'.meta');guid=re.search(r'^guid: ([a-f0-9]{32})',m.read_text(),re.M)[1];guids.append(guid)
    assert h(m)==h(c/'Assets/Scripts'/m.name)
for guid in guids:
    found=[m for m in (p/'Assets').rglob('*.meta') if re.search(r'^guid: '+guid+'$',m.read_text(encoding='utf-8-sig',errors='replace'),re.M)]
    assert len(found)==1,found
rows=json.loads((p/'Catalogos/Animaciones/Produccion.json').read_text(encoding='utf-8'))
assert len(rows)==100
for r in rows:
    assert h(p/r['archivo'])==r['sha256PNG'];assert h(p/r['referenciaSplash'])==r['sha256Splash']
    profile=p/'Assets/Resources/HorseRaceProfiles'/(r['id']+'.asset')
    assert h(profile)==h(c/profile.relative_to(p))
    assert h(Path(str(profile)+'.meta'))==h(c/(str(profile.relative_to(p))+'.meta'))
assert h(Path(r'C:\Caballos\Builds\Caballos-2.0-Beta-v0.1-Windows.zip')).upper()=='E5D2CE8B9729EB9E83EEE9001A6E549451C509322C48CE76F2589F814E8A779F'
result=dict(escenaHubConservada=True,camaraHubEstabloTiendaSubastasSinCambios=True,animadorYResultadosPreviosConservados=True,
    formulasNormalesConservadas=True,perfilesConservados=100,framesConservados=800,splashConservados=100,metasNuevosUnicos=2,
    codigoIgualALaCopiaValidada=True,buildAnteriorConservada=True,shaScripts={n:h(p/'Assets/Scripts'/n) for n in names})
(w/'integridad.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print(json.dumps(result,ensure_ascii=False))
