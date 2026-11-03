using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MainMenuHoverValidation
{
    const string Active="MainMenuHoverValidation.Active",Done="MainMenuHoverValidation.Done";
    const string Report="HoverValidation/results.txt";
    static double started;
    static string error;
    static int checks;
    static MainMenuHoverValidation()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Done,false))
            {SessionState.SetBool(Done,false);if(Application.isBatchMode)EditorApplication.Exit(0);}
        };
        Application.logMessageReceived+=(message,trace,type)=>
        {
            if(!SessionState.GetBool(Active,false)||(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert))return;
            if(trace.Contains("UnityEditor.Search.SearchInit.IndexationOnStartup"))
            {File.AppendAllText("HoverValidation/editor-diagnostics.txt",message+"\n"+trace+"\n");return;}
            error=message+"\n"+trace;
        };
    }
    public static void Run()
    {
        try
        {
            Directory.CreateDirectory("HoverValidation");File.WriteAllText(Report,"");
            Check(Application.isBatchMode,"headless test");
            string save=Environment.GetEnvironmentVariable("CABALLOS_VALIDATION_SAVE_PATH");
            Check(!string.IsNullOrEmpty(save)&&Path.GetFullPath(save).StartsWith(Path.GetFullPath("../MainMenuSave")),"isolated save");
            Directory.CreateDirectory(save);File.WriteAllText(Path.Combine(save,"gameData.json"),"{}");
            MainMenuHoverSetup.ApplyToScene();
            var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
            var group=menu.menuButtons.GetComponent<MenuDiamondSelection>();
            Check(group.options.Length==4,"4 options");
            foreach(var option in group.options)
            {
                Check(option.button.transition==Selectable.Transition.None,"transition none");
                var image=option.button.GetComponent<Image>();
                Check(image.enabled&&image.raycastTarget&&image.color.a==0,"transparent graphic remains raycastable");
                Check(!option.indicator.gameObject.activeSelf&&!option.indicator.raycastTarget,"initially hidden and nonblocking");
                Check(option.indicator.transform.parent==option.transform,"image-relative indicator parent");
                Check(option.GetComponent<Animator>()==null&&option.GetComponent<Shadow>()==null,"no other button visual effect");
            }
            Vector2[] centers={new Vector2(105,397),new Vector2(105,479),new Vector2(105.5f,552.5f),new Vector2(106,628)};
            for(int i=0;i<4;i++)
            {
                var button=(RectTransform)group.options[i].transform;
                var glow=group.options[i].indicator.rectTransform;
                var span=button.anchorMax-button.anchorMin;
                var center=button.anchorMin+Vector2.Scale((glow.anchorMin+glow.anchorMax)*.5f,span);
                Check(Vector2.Distance(center,new Vector2(centers[i].x/1659f,1-centers[i].y/948f))<.00001f,"measured diamond center "+i);
                foreach(var size in new[]{new Vector2(1920,1080),new Vector2(1600,900),new Vector2(1366,768),new Vector2(1280,720)})
                {
                    float scale=Mathf.Min(size.x/1659,size.y/948);
                    var pixels=Vector2.Scale(Vector2.Scale(glow.anchorMax-glow.anchorMin,span),new Vector2(1659,948))*scale;
                    Check(Mathf.Abs(pixels.x-23*scale)<.001f&&Mathf.Abs(pixels.y-23*scale)<.001f,"uniform diamond size "+size);
                }
            }
            var repair=group.options[0].GetComponentInChildren<MenuDiamondEmptyMask>(true);
            Check(repair!=null&&!repair.raycastTarget&&repair.leftRows.Length==27&&repair.rightRows.Length==27,"baked first-diamond mark covered without blocking pointer");
            Check(repair.leftRows.All(c=>c.r<.8f)&&repair.rightRows.All(c=>c.r<.8f),"empty repair uses background colors, not gold");
            Check(repair.transform.GetSiblingIndex()<group.options[0].indicator.transform.GetSiblingIndex(),"fill draws above empty repair");
            File.AppendAllText(Report,"Edit Mode PASS: transparent Button/Image with Transition=None; four hidden solid fills; original first-diamond baked selection covered by non-raycast background interpolation; centers/scale checked at four resolutions.\n");
            SessionState.SetBool(Active,true);SessionState.SetBool(Done,false);EditorApplication.isPlaying=true;
        }
        catch(Exception ex){Fail(ex);}
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    static void Expect(MenuDiamondSelection group,MenuDiamondOption owner)
    {
        Check(group.options.Count(o=>o.indicator.gameObject.activeSelf)==(owner==null?0:1),"exclusive active count");
        Check(group.ActiveOption==owner,"correct owner");
        if(owner!=null)Check(owner.indicator.gameObject.activeSelf,"correct diamond active");
        foreach(var option in group.options)Check(option.button.GetComponent<Image>().color.a==0,"button stays transparent");
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Active,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        try
        {
            if(error!=null)throw new Exception(error);
            if(started==0){started=EditorApplication.timeSinceStartup;return;}
            if(EditorApplication.timeSinceStartup-started<2)return;
            var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
            var group=menu.menuButtons.GetComponent<MenuDiamondSelection>();
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            Expect(group,null); // Programmatic default selection must not glow.
            foreach(var option in group.options)
            {
                ExecuteEvents.Execute(option.gameObject,pointer,ExecuteEvents.pointerEnterHandler);Expect(group,option);
                ExecuteEvents.Execute(option.gameObject,pointer,ExecuteEvents.pointerExitHandler);Expect(group,null);
            }
            for(int i=0;i<400;i++)
            {
                var old=group.options[i%4];var next=group.options[(i+1)%4];
                ExecuteEvents.Execute(old.gameObject,pointer,ExecuteEvents.pointerEnterHandler);Expect(group,old);
                ExecuteEvents.Execute(next.gameObject,pointer,ExecuteEvents.pointerEnterHandler);Expect(group,next);
                ExecuteEvents.Execute(old.gameObject,pointer,ExecuteEvents.pointerExitHandler);Expect(group,next);
                ExecuteEvents.Execute(next.gameObject,pointer,ExecuteEvents.pointerExitHandler);Expect(group,null);
            }
            EventSystem.current.SetSelectedGameObject(menu.newGameButton.gameObject);
            ExecuteEvents.Execute(menu.newGameButton.gameObject,new AxisEventData(EventSystem.current){moveDir=MoveDirection.Down,moveVector=Vector2.down},ExecuteEvents.moveHandler);
            Expect(group,group.options[1]);
            ExecuteEvents.Execute(group.options[3].gameObject,pointer,ExecuteEvents.pointerEnterHandler);Expect(group,group.options[3]);
            ExecuteEvents.Execute(group.options[3].gameObject,pointer,ExecuteEvents.pointerExitHandler);Expect(group,group.options[1]);
            group.options[3].OnPointerEnter(pointer);Expect(group,group.options[3]);
            group.options[3].OnPointerDown(pointer);group.options[3].OnPointerExit(pointer);Expect(group,null);
            // A stationary hovered button regains ownership when the mouse moves
            // after keyboard navigation, even without a new PointerEnter.
            group.options[2].OnPointerMove(pointer);Expect(group,group.options[2]);
            EventSystem.current.SetSelectedGameObject(menu.newGameButton.gameObject);
            ExecuteEvents.Execute(menu.newGameButton.gameObject,new AxisEventData(EventSystem.current){moveDir=MoveDirection.Down,moveVector=Vector2.down},ExecuteEvents.moveHandler);
            Expect(group,group.options[1]);
            group.options[2].OnPointerMove(pointer);Expect(group,group.options[2]);
            group.options[2].OnPointerExit(pointer);Expect(group,group.options[1]);
            EventSystem.current.SetSelectedGameObject(menu.newGameButton.gameObject);
            ExecuteEvents.Execute(menu.newGameButton.gameObject,new AxisEventData(EventSystem.current){moveDir=MoveDirection.Down,moveVector=Vector2.down},ExecuteEvents.moveHandler);
            Expect(group,group.options[1]);
            menu.OpenSettings();group.SendMessage("LateUpdate");Expect(group,null);
            menu.CloseSettings();Expect(group,null);
            group.options[1].button.interactable=false;
            group.options[1].OnPointerEnter(pointer);Expect(group,null);
            group.options[1].button.interactable=true;
            group.options[2].OnPointerEnter(pointer);Expect(group,group.options[2]);
            menu.menuButtons.interactable=false;group.SendMessage("LateUpdate");Expect(group,null);
            menu.menuButtons.interactable=true;
            Canvas.ForceUpdateCanvases();
            var raycaster=menu.GetComponentInChildren<GraphicRaycaster>();
            foreach(var option in group.options)
            {
                var rect=(RectTransform)option.transform;
                pointer.position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
                var results=new System.Collections.Generic.List<RaycastResult>();raycaster.Raycast(pointer,results);
                Check(results.Any(r=>r.gameObject==option.gameObject),"transparent button receives real UGUI raycast: "+option.name);
                Check(!results.Any(r=>r.gameObject==option.indicator.gameObject),"glow does not intercept pointer");
            }
            // Invoke the existing Button click routes, without OS input.
            ExecuteEvents.Execute(menu.newGameButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            Check(menu.confirmationPanel.activeSelf&&!menu.IsBusy,"new game click unchanged");menu.CloseConfirmation();
            ExecuteEvents.Execute(menu.loadGameButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            Check(!menu.IsBusy&&!string.IsNullOrEmpty(menu.statusText.text),"load click unchanged");
            ExecuteEvents.Execute(menu.settingsButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            Check(menu.settingsPanel.activeSelf,"settings click unchanged");menu.CloseSettings();
            ExecuteEvents.Execute(menu.exitButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            Check(menu.confirmationPanel.activeSelf,"exit click unchanged");menu.CloseConfirmation();
            var geometry=group.options[0].indicator;
            var helper=new VertexHelper();
            typeof(MenuDiamondGlow).GetMethod("OnPopulateMesh",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.DeclaredOnly).Invoke(geometry,new object[]{helper});
            Check(helper.currentVertCount==5&&helper.currentIndexCount==12,"solid diamond center mesh, no rectangular quad");helper.Dispose();
            Check(geometry.color.a==1,"solid opaque gold fill");
            File.AppendAllText(Report,"Play Mode PASS: empty initial state; all 4 pointer enter/exit; 400 rapid transitions including delayed exits; genuine keyboard focus restored after hover exit; mouse click clears stale keyboard focus; modal reset; UGUI raycasts and all 4 existing click routes; solid center-filled diamond topology. Assertions: "+checks+"\nNo desktop input or rendered visual test performed. Background repair appearance requires manual visual review.\n");
            SessionState.SetBool(Active,false);SessionState.SetBool(Done,true);EditorApplication.isPlaying=false;
        }
        catch(Exception ex){Fail(ex);}
    }
    static void Fail(Exception ex)
    {
        Directory.CreateDirectory("HoverValidation");File.AppendAllText(Report,"FAILED: "+ex+"\n");
        SessionState.SetBool(Active,false);Debug.LogException(ex);if(Application.isBatchMode)EditorApplication.Exit(1);
    }
}
