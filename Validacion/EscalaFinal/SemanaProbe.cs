using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Object=UnityEngine.Object;
[InitializeOnLoad] public static class SemanaProbe {
 const string K="SemanaProbe.Active", O="C:/Caballos/Validation/Semana/"; static IEnumerator test; static List<string> checks=new(); static GameManager g;
 static SemanaProbe(){EditorApplication.update+=Step;}
 public static void Run(){if(!Application.isBatchMode||string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CABALLOS_VALIDATION_SAVE_PATH")))throw new Exception("Aislado requerido");EditorSceneManager.OpenScene(GameLaunch.HubScenePath);EditorApplication.delayCall+=()=>{SessionState.SetBool(K,true);EditorApplication.isPlaying=true;};}
 static void Step(){if(!SessionState.GetBool(K,false)||!EditorApplication.isPlaying)return;try{test??=Tests();if(!test.MoveNext())End(0);}catch(Exception e){Debug.LogException(e);End(1);}}
 static IEnumerator Tests(){while(GameManager.Instancia==null||GameManager.Instancia.caballosJugador.Count==0)yield return null;g=GameManager.Instancia;yield return null;
 g.IniciarNuevaPartida();Check(g.dinero==2500&&g.dia==1,"Nueva partida 2500 día1");Check(Moneda.Texto(2500)=="$2.500","Moneda");
 foreach(int day in new[]{7,14,21})foreach(int offset in new[]{-1,0,700}){g.IniciarNuevaPartida();g.dia=day;int meta=GameManager.ObtenerCuotaSemana(day/7);g.dinero=meta+offset;g.GuardarPartida();Load();g.SiguienteDia();Check(g.Derrotado==(offset<0),"Umbral "+day+" "+offset);Check(g.dinero==meta+offset,"Sin descuento");Check(g.dia==(offset<0?day:day+1),"Día correcto");g.GuardarPartida();Load();Check(g.Derrotado==(offset<0),"Carga conserva cierre");}
 g.IniciarNuevaPartida();g.dinero=0;g.GuardarPartida();Check(!g.Derrotado,"Cero permitido");g.dinero=100;var auction=g.SubastaActual;auction.precioInicial=50;foreach(var b in auction.postores)b.retirado=true;Check(g.PujarSubasta(auction.id,100,out _)&&!g.Derrotado&&g.SaldoTotal==100,"Reserva no es pérdida");Check(!g.PujarSubasta(auction.id,10000,out _),"Puja impagable rechazada");g.dinero=-101;g.GuardarPartida();Check(g.Derrotado&&g.SaldoTotal==-1,"Negativo total confirmado");Load();Check(g.Derrotado,"Derrota negativa persistente");yield return null;
 var go=Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(c=>c.name=="GameOver").gameObject;Check(go.activeInHierarchy&&go.GetComponentsInChildren<Button>().Length==2,"Sólo dos botones bloqueo");var pause=Object.FindFirstObjectByType<MenuPausa>();Call(pause,"ProcesarEscape");Check(go.activeInHierarchy,"Escape no elimina derrota");Render("gameover");go.GetComponentsInChildren<Button>().Single(b=>b.name=="Nueva partida").onClick.Invoke();yield return null;Check(!g.Derrotado&&g.dinero==2500&&g.dia==1,"Botón nueva partida");
 // Reward on last day must be settled before weekly evaluation, with no duplicate settlement.
 g.dia=7;g.dinero=3000;g.ElegirCarrera(g.ObtenerOfertaCarreras()[0].id,out _);var race=g.ObtenerCarreraProgramada();g.IniciarCarreraProgramada(race);g.dinero=3000-race.PremioGanador;race.clasificacionFinal=race.participantes.Select((p,i)=>new LlegadaCarrera{idCaballo=p.caballo.id,posicion=i+1,tiempo=10+i}).ToList();var winner=g.CrearVistaParticipante(race.participantes[0]);g.FinalizarCarreraProgramada(race,winner,null);Check(g.SaldoTotal==3000&&!g.Derrotado&&g.dia==8,"Premio del día7 alimenta cierre");g.FinalizarCarreraProgramada(race,winner,null);Check(g.SaldoTotal==3000&&g.dia==8,"Liquidación única");
 g.IniciarNuevaPartida();g.dia=7;g.dinero=10000;g.ElegirCarrera(g.ObtenerOfertaCarreras()[0].id,out _);race=g.ObtenerCarreraProgramada();g.IniciarCarreraProgramada(race);g.dinero=2500-race.PremioGanador;race.clasificacionFinal=race.participantes.Select((p,i)=>new LlegadaCarrera{idCaballo=p.caballo.id,posicion=i+1,tiempo=10+i}).ToList();g.FinalizarCarreraProgramada(race,g.CrearVistaParticipante(race.participantes[0]),null);yield return null;Check(g.Derrotado&&!go.activeInHierarchy,"Espera podio antes de Game Over");g.CerrarResultadosDerrota();yield return null;Check(g.Derrotado,"Derrota semanal visible");Render("semanal");
 // Legacy load never overwrites money or retrospectively applies weekly goals.
 SaveSystem.IntentarCargar(out var data);data.derrota=null;data.dinero=100;data.dia=15;SaveSystem.Guardar(data);Load();Check(!g.Derrotado&&g.dinero==100&&g.dia==15,"Migración sin derrota retroactiva");
 g.IniciarNuevaPartida();g.dinero=10000;g.ElegirCarrera(g.ObtenerOfertaCarreras()[0].id,out _);g.SetState(Interactable.Tipo.TV);yield return null;var ui=UIManager.instance.viewCarrera.GetComponentInChildren<CarreraUI>();ui.enabled=false;ui.IniciarCarrera();Call(ui,"ActualizarPresentacion",0f,true);Check(ui.contenedorCaballos.childCount>=5,"Cinco participantes");Check(!ui.contenedorCaballos.GetComponentsInChildren<Image>().Any(i=>i.name=="PuntoCompetitivo"),"Sin puntos renderizados");var actor=ui.contenedorCaballos.Cast<Transform>().First(t=>t.Find("IndicadorJugador")!=null);Check(actor!=null,"Una flecha propia");yield return null;Render("carrera");
 Call(ui,"ActualizarPresentacion",2.1f,false);Check(!actor.Find("IndicadorJugador").gameObject.activeSelf,"A soltada");Call(ui,"ActualizarPresentacion",.1f,true);Check(actor.Find("IndicadorJugador").gameObject.activeSelf,"A mantenida");
 var order=(ClasificacionCarrera)typeof(CarreraUI).GetField("clasificacion",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ui);
 for(int frame=0;frame<10000&&!order.Completa;frame++)Call(ui,"AvanzarCarrera",.02f);
 Check(order.Completa&&order.Resultados.Count==g.UltimoResultadoCarrera.carrera.participantes.Count,"Carrera completa con escala nueva");
 g.VolverAlHub();g.dinero=-1;g.GuardarPartida();g.CerrarResultadosDerrota();yield return null;g.MenuDesdeDerrota();yield return null;while(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=GameLaunch.MenuScenePath)yield return null;Check(true,"Botón menú principal carga menú sin reinicio");Debug.Log("SEMANA PASS: "+checks.Count);
 }
 static void Load(){Check((bool)Call(g,"CargarPartida"),"Carga aislada");}
 static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(o,a);
 static void Check(bool v,string s){if(!v)throw new Exception(s);checks.Add(s);}
 static void Render(string name){var camera=new GameObject("RenderPrueba").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-100);var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas).ToArray();var modes=canvases.Select(c=>c.renderMode).ToArray();var cams=canvases.Select(c=>c.worldCamera).ToArray();for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=RenderMode.ScreenSpaceCamera;canvases[i].worldCamera=camera;canvases[i].planeDistance=1;}var rt=new RenderTexture(1600,900,24);camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;var t=new Texture2D(1600,900,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1600,900),0,0);t.Apply();File.WriteAllBytes(O+name+".png",t.EncodeToPNG());RenderTexture.active=null;for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cams[i];}Object.Destroy(camera.gameObject);Object.Destroy(t);Object.Destroy(rt);}
 static void End(int code){SessionState.SetBool(K,false);File.WriteAllLines(O+"pruebas.txt",checks);EditorApplication.Exit(code);}
}
