using UnityEngine;

[CreateAssetMenu(menuName = "Caballos/Visual catalog")]
public sealed class HorseVisualCatalog : ScriptableObject
{
    public HorseVisualProfile[] profiles;
    public static HorseVisualCatalog Default => Resources.Load<HorseVisualCatalog>("HorseVisualCatalog");

    public HorseVisualProfile Resolve(string id)
    {
        if (profiles == null || string.IsNullOrEmpty(id)) return null;
        foreach (var profile in profiles)
            if (profile != null && profile.profileId == id) return profile;
        return null;
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
        if (string.IsNullOrEmpty(horse.visualProfileId))
            horse.visualProfileId = "legacy:" + legacyIndex;
        var profile = Resolve(horse.visualProfileId);
        horse.sprite = profile != null ? profile.baseSprite : legacySprite;
        if (profile == null && !horse.visualProfileId.StartsWith("legacy:"))
            Debug.LogWarning("Perfil visual ausente: " + horse.visualProfileId + ". Se conserva su ID.");
    }
}
