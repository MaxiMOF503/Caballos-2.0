using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class HorseRaceCatalogAuthoring
{
    public static void Generate()
    {
        if(!Application.isBatchMode || !Path.GetFullPath(Application.dataPath).StartsWith("C:\\Caballos\\Validation\\MainMenu\\",StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Autoría únicamente en copia aislada");
        Directory.CreateDirectory("Assets/Resources/HorseRaceProfiles");AssetDatabase.Refresh();
        int count=0;
        foreach(var identity in CatalogoCaballos.Todos)
        {
            string sheet="Assets/Art/HorseRaceCatalog/"+identity.id+".png";
            if(!File.Exists(sheet))continue;
            var importer=(TextureImporter)AssetImporter.GetAtPath(sheet);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.maxTextureSize=1024;importer.spritePixelsPerUnit=100;
            importer.alphaIsTransparency=true;importer.isReadable=false;importer.mipmapEnabled=false;
            importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Point;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Standalone",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.BC7,compressionQuality=100});
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            importer.GetSourceTextureWidthAndHeight(out int width,out int height);
            if(Math.Abs(width-2*height)>2)throw new InvalidOperationException("Hoja no es 4x2 de celdas cuadradas: "+sheet);
            var factories=new SpriteDataProviderFactories();factories.Init();
            var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
            var previous=provider.GetSpriteRects();
            var rects=Enumerable.Range(0,8).Select(i=>
            {
                string name=identity.id+"_run_"+(i+1).ToString("00");
                return new SpriteRect{name=name,rect=new Rect(i%4*width/4f,(1-i/4)*height/2f,width/4f,height/2f),
                    pivot=new Vector2(.5f,.5f),alignment=SpriteAlignment.Center,
                    spriteID=previous.FirstOrDefault(r=>r.name==name)?.spriteID??GUID.Generate()};
            }).ToArray();
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
            provider.Apply();importer.SaveAndReimport();
            var frames=AssetDatabase.LoadAllAssetsAtPath(sheet).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
            if(frames.Length!=8)throw new InvalidOperationException("No hay ocho sub-sprites: "+sheet);
            string path="Assets/Resources/HorseRaceProfiles/"+identity.id+".asset";
            var profile=AssetDatabase.LoadAssetAtPath<HorseVisualProfile>(path);
            if(profile==null){profile=ScriptableObject.CreateInstance<HorseVisualProfile>();AssetDatabase.CreateAsset(profile,path);}
            profile.profileId="catalogo:"+identity.id;profile.catalogIdentityId=identity.id;profile.splashResource=identity.splashRecurso;
            profile.baseSprite=Resources.Load<Sprite>(identity.splashRecurso);profile.splash=profile.baseSprite;
            profile.runFrames=frames;profile.framesPerSecond=9;profile.provisional=false;profile.productionReady=true;
            profile.source="Galope generado desde el splash propio; 8 fases, hoja 4x2. Ver Catalogos/Animaciones/Produccion.json.";
            if(!profile.IsValid)throw new InvalidOperationException("Perfil nuevo incompleto: "+identity.id);
            EditorUtility.SetDirty(profile);count++;
        }
        var catalog=HorseVisualCatalog.Default;
        catalog.catalogAnimationsEnabled=count==100;EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        File.WriteAllText("C:/Caballos/Validation/AnimacionCamara/perfiles-generados.txt",count+" perfiles; "+count*8+" frames; 9 FPS.");
        Debug.Log("HORSE RACE AUTHORING PASS: "+count+" perfiles");
        var textures=Resources.LoadAll<HorseVisualProfile>("HorseRaceProfiles").Select(p=>p.runFrames[0].texture).Distinct().ToArray();
        Debug.Log("ATLAS MEMORY: "+textures.Length+" texturas, "+textures.Sum(t=>UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t))+" bytes reportados en Editor");
    }
}
