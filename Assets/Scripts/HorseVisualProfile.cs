using UnityEngine;

[CreateAssetMenu(menuName = "Caballos/Visual profile")]
public sealed class HorseVisualProfile : ScriptableObject
{
    public string profileId;
    public Sprite baseSprite;
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
            return true;
        }
    }
}
