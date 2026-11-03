using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MainMenuHoverSetup
{
    public static void ApplyToScene()
    {
        string before = File.ReadAllText(GameLaunch.MenuScenePath);
        EditorSceneManager.OpenScene(GameLaunch.MenuScenePath);
        Configure(Object.FindFirstObjectByType<MainMenuController>());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        // Unity can normalize unrelated Text properties on save (e.g. font max
        // size). Keep every pre-existing block outside the five visual owners
        // byte-for-byte, rather than changing panels or other menu objects.
        const string pattern = @"(?ms)^--- !u!\d+ &(\d+)\r?\n.*?(?=^--- !u!|\z)";
        var originals = Regex.Matches(before, pattern).Cast<Match>().ToDictionary(m => m.Groups[1].Value, m => m.Value);
        var owners = new HashSet<string>();
        foreach (var pair in originals)
        {
            if (Regex.IsMatch(pair.Value, @"(?m)^  m_Name: (MenuButtons|Button_NewGame|Button_LoadGame|Button_Settings|Button_Exit)\r?$")) owners.Add(pair.Key);
            if (Regex.IsMatch(pair.Value, @"(?m)^  m_Name: (DiamondGlow|DiamondFill|EmptyDiamondRepair)\r?$")) owners.Add(pair.Key);
        }
        if (owners.Count < 5) throw new System.InvalidOperationException("Expected menu visual owners.");
        string after = File.ReadAllText(GameLaunch.MenuScenePath);
        after = Regex.Replace(after, pattern, match =>
        {
            string id = match.Groups[1].Value;
            if (!originals.ContainsKey(id) || owners.Contains(id)) return match.Value;
            var owner = Regex.Match(match.Value, @"m_GameObject: \{fileID: (\d+)\}");
            return owner.Success && owners.Contains(owner.Groups[1].Value) ? match.Value : originals[id];
        });
        File.WriteAllText(GameLaunch.MenuScenePath, after);
        AssetDatabase.ImportAsset(GameLaunch.MenuScenePath, ImportAssetOptions.ForceUpdate);
    }

    public static void Configure(MainMenuController menu)
    {
        var host = menu.menuButtons.gameObject;
        var selection = host.GetComponent<MenuDiamondSelection>();
        if (selection == null) selection = host.AddComponent<MenuDiamondSelection>();
        var buttons = new[] {menu.newGameButton, menu.loadGameButton, menu.settingsButton, menu.exitButton};
        // Centers measured directly in the unchanged 1659 x 948 artwork.
        var centers = new[] {new Vector2(105,397),new Vector2(105,479),new Vector2(105.5f,552.5f),new Vector2(106,628)};
        float[] tops = {361,443,517,591};
        selection.options = new MenuDiamondOption[4];
        for (int i=0;i<4;i++)
        {
            var button = buttons[i];
            button.transition = Selectable.Transition.None;
            var image = button.GetComponent<Image>();
            image.enabled = true; image.raycastTarget = true; image.color = Color.clear;
            image.canvasRenderer.SetColor(Color.white);
            var option = button.GetComponent<MenuDiamondOption>();
            if (option == null) option = button.gameObject.AddComponent<MenuDiamondOption>();
            option.button = button; option.selection = selection;
            if (option.indicator == null)
            {
                var obj = new GameObject("DiamondGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(MenuDiamondGlow));
                obj.transform.SetParent(button.transform, false);
                option.indicator = obj.GetComponent<MenuDiamondGlow>();
            }
            var rect = option.indicator.rectTransform;
            option.indicator.gameObject.name="DiamondFill";
            rect.anchorMin = new Vector2((centers[i].x-11.5f-78)/460f, 1-(centers[i].y+11.5f-tops[i])/73f);
            rect.anchorMax = new Vector2((centers[i].x+11.5f-78)/460f, 1-(centers[i].y-11.5f-tops[i])/73f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            option.indicator.color = new Color(1f,.84f,.36f,1f);
            option.indicator.raycastTarget = false;
            option.indicator.gameObject.SetActive(false);
            EditorUtility.SetDirty(button); EditorUtility.SetDirty(image); EditorUtility.SetDirty(option);
            selection.options[i] = option;
        }
        var first=buttons[0].transform;
        var repair=first.GetComponentInChildren<MenuDiamondEmptyMask>(true);
        if(repair==null)
        {
            var obj=new GameObject("EmptyDiamondRepair",typeof(RectTransform),typeof(CanvasRenderer),typeof(MenuDiamondEmptyMask));
            obj.transform.SetParent(first,false);repair=obj.GetComponent<MenuDiamondEmptyMask>();
        }
        repair.transform.SetAsFirstSibling();
        repair.rectTransform.anchorMin=new Vector2((105-13-78)/460f,1-(397+13-361)/73f);
        repair.rectTransform.anchorMax=new Vector2((105+13-78)/460f,1-(397-13-361)/73f);
        repair.rectTransform.offsetMin=repair.rectTransform.offsetMax=Vector2.zero;
        repair.raycastTarget=false;repair.leftRows=new Color[27];repair.rightRows=new Color[27];
        var source=new Texture2D(2,2);source.LoadImage(File.ReadAllBytes(MainMenuBuilder.ArtworkPath));
        for(int row=0;row<27;row++)
        {
            int y=source.height-1-(397-(row-13));
            repair.leftRows[row]=source.GetPixel(85,y);repair.rightRows[row]=source.GetPixel(125,y);
        }
        Object.DestroyImmediate(source);EditorUtility.SetDirty(repair);
        EditorUtility.SetDirty(selection);
    }
}
