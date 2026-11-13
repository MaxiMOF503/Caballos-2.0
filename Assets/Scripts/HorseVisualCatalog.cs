using UnityEngine;

[CreateAssetMenu(menuName = "Caballos/Visual catalog")]
public sealed class HorseVisualCatalog : ScriptableObject
{
    readonly System.Collections.Generic.HashSet<string> avisosSplash = new();
    readonly System.Collections.Generic.Dictionary<Sprite, Sprite> presentaciones = new();
    readonly System.Collections.Generic.Dictionary<string, HorseVisualProfile> carreras = new();
    readonly System.Collections.Generic.HashSet<string> avisosCarrera = new();
    public HorseVisualProfile[] profiles;
    [Tooltip("Activar únicamente cuando estén validados los 100 perfiles de carrera. Evita mezclar lotes parciales.")]
    public bool catalogAnimationsEnabled;
    public static HorseVisualCatalog Default => Resources.Load<HorseVisualCatalog>("HorseVisualCatalog");

    public HorseVisualProfile Resolve(string id)
    {
        if (profiles == null || string.IsNullOrEmpty(id)) return null;
        foreach (var profile in profiles)
            if (profile != null && profile.profileId == id) return profile;
        return null;
    }

    public HorseVisualProfile ResolveForRace(Caballo horse)
    {
        if (horse == null) return null;
        if (!catalogAnimationsEnabled) return Resolve(horse.visualProfileId);
        if (string.IsNullOrEmpty(horse.catalogIdentityId)) return Resolve(horse.visualProfileId);
        var identity = CatalogoCaballos.Buscar(horse.catalogIdentityId);
        if (identity == null || identity.visualProfileId != horse.visualProfileId) return Missing(horse.catalogIdentityId);
        if (!carreras.TryGetValue(identity.id, out var profile) || profile == null)
        {
            profile = Resources.Load<HorseVisualProfile>("HorseRaceProfiles/" + identity.id);
            if (profile != null) carreras[identity.id] = profile;
        }
        var splash = RecursosCatalogo.Cargar(identity.splashRecurso);
        if (profile == null || !profile.IsValid || profile.catalogIdentityId != identity.id ||
            profile.splashResource != identity.splashRecurso || profile.baseSprite != splash)
            return Missing(identity.id);
        return profile;
    }

    HorseVisualProfile Missing(string id)
    {
        if (avisosCarrera.Add(id)) Debug.LogError("Animación propia de catálogo ausente o inválida: " + id +
            ". No se sustituye por una animación antigua.");
        return null;
    }

    public bool RestoreCatalogVisual(Caballo horse)
    {
        if (horse == null || !catalogAnimationsEnabled) return false;
        var identity = CatalogoCaballos.Buscar(horse.catalogIdentityId);
        if (identity == null && string.IsNullOrEmpty(horse.catalogIdentityId) && Resolve(horse.visualProfileId) != null)
        {
            // Migración por pelaje persistido, nunca por nombre solamente. Conserva el
            // nombre y el ID de instancia aunque procedan de una partida anterior.
            var named = CatalogoCaballos.PorNombre(horse.nombre);
            identity = named != null && named.visualProfileId == horse.visualProfileId ? named : null;
            if (identity == null)
                foreach (var candidate in CatalogoCaballos.Todos)
                    if (candidate.visualProfileId == horse.visualProfileId) { identity = candidate; break; }
        }
        if (identity == null || identity.visualProfileId != horse.visualProfileId) return false;
        var splash = RecursosCatalogo.Cargar(identity.splashRecurso);
        if (splash == null) return false;
        horse.catalogIdentityId = identity.id;
        horse.sprite = splash;
        return true;
    }

    public Sprite Splash(Caballo caballo)
    {
        if (caballo == null) return null;
        var identidad = CatalogoCaballos.Buscar(caballo.catalogIdentityId);
        if (identidad != null && identidad.visualProfileId == caballo.visualProfileId)
        {
            var ilustracion = RecursosCatalogo.Cargar(identidad.splashRecurso);
            if (ilustracion != null)
            {
                Rect bounds = identidad.presentationBounds;
                return Presentacion(ilustracion, new Rect(bounds.x * ilustracion.rect.width,
                    bounds.y * ilustracion.rect.height, bounds.width * ilustracion.rect.width,
                    bounds.height * ilustracion.rect.height));
            }
        }
        var perfil = Resolve(caballo.visualProfileId);
        if (perfil?.splash != null) return perfil.splash;
        if (avisosSplash.Add(caballo.visualProfileId ?? "sin-perfil"))
            Debug.LogWarning("Splash dedicado ausente para " + caballo.visualProfileId +
                ". Se utiliza su sprite base correspondiente.");
        var original = perfil?.baseSprite ?? caballo.sprite;
        if (original == null) return null;
        // Límites de alfa calculados al importar. Las texturas y los frames originales se conservan.
        Rect limites = perfil != null ? perfil.presentationBounds : default;
        return Presentacion(original, limites);
    }

    Sprite Presentacion(Sprite original, Rect limites)
    {
        if (limites.width <= 0 || limites.height <= 0) return original;
        if (presentaciones.TryGetValue(original, out var existente) && existente != null) return existente;
        var recorte = new Rect(original.rect.position + limites.position, limites.size);
        var presentacion = Sprite.Create(original.texture, recorte, new Vector2(.5f,.5f),
            original.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        presentacion.name = original.name + "_presentacion";
        presentacion.hideFlags = HideFlags.DontSave;
        presentaciones[original] = presentacion;
        return presentacion;
    }

    public bool AssignNew(Caballo horse)
    {
        if (horse == null || !string.IsNullOrEmpty(horse.visualProfileId)) return false;
        if (profiles == null || profiles.Length == 0) return false;
        var profile = profiles[Random.Range(0, profiles.Length)];
        if (profile == null || !profile.IsValid) return false;
        horse.visualProfileId = profile.profileId;
        horse.sprite = profile.baseSprite;
        return true;
    }

    // Legacy identities retain their exact spriteIndex; no guessed skin substitution.
    public void Restore(Caballo horse, int legacyIndex, Sprite legacySprite)
    {
        if (RestoreCatalogVisual(horse)) return;
        if (string.IsNullOrEmpty(horse.visualProfileId))
            horse.visualProfileId = "legacy:" + legacyIndex;
        var profile = Resolve(horse.visualProfileId);
        horse.sprite = profile != null ? profile.baseSprite : legacySprite;
        if (profile == null && !horse.visualProfileId.StartsWith("legacy:"))
            Debug.LogWarning("Perfil visual ausente: " + horse.visualProfileId + ". Se conserva su ID.");
    }
}
