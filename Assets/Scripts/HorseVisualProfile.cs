using UnityEngine;

[CreateAssetMenu(menuName = "Caballos/Visual profile")]
public sealed class HorseVisualProfile : ScriptableObject
{
    public string profileId;
    public string catalogIdentityId;
    public string splashResource;
    public bool productionReady;
    public Sprite baseSprite;
    public Sprite splash;
    public Rect presentationBounds;
    public Sprite[] runFrames = new Sprite[8];
    [Range(1f, 24f)] public float framesPerSecond = 9f;
    public bool provisional = true;
    public string source;

    public bool IsValid
    {
        get
        {
            if (string.IsNullOrEmpty(profileId) || baseSprite == null ||
                runFrames == null || runFrames.Length != 8 || framesPerSecond <= 0f) return false;
            foreach (Sprite frame in runFrames) if (frame == null) return false;
            if (!string.IsNullOrEmpty(catalogIdentityId))
            {
                if (!productionReady || provisional || string.IsNullOrEmpty(splashResource)) return false;
                foreach (var frame in runFrames)
                    if ((frame.rect.size - runFrames[0].rect.size).sqrMagnitude > .01f ||
                        (frame.pivot - runFrames[0].pivot).sqrMagnitude > .01f) return false;
            }
            return true;
        }
    }
}
