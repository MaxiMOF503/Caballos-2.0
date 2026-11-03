using UnityEngine;
using UnityEngine.UI;

public sealed class HorseVisualAnimator : MonoBehaviour
{
    public HorseVisualProfile Profile { get; private set; }
    public bool IsRunning { get; private set; }
    Image image;
    Sprite baseSprite;
    float elapsed;

    public void Bind(Caballo horse, Image target)
    {
        image = target;
        var catalog = HorseVisualCatalog.Default;
        Profile = catalog != null ? catalog.Resolve(horse.visualProfileId) : null;
        baseSprite = Profile != null ? Profile.baseSprite : horse.sprite;
        StopRun();
        if (image != null) image.preserveAspect = true;
    }

    public void StartRun()
    {
        elapsed = 0f;
        IsRunning = Profile != null && Profile.IsValid;
        if (IsRunning && image != null) image.sprite = Profile.runFrames[0];
    }

    public void StopRun()
    {
        IsRunning = false;
        if (image != null) image.sprite = baseSprite;
    }

    void Update()
    {
        if (!IsRunning || image == null) return;
        elapsed = Mathf.Repeat(elapsed + Time.deltaTime, 8f / Profile.framesPerSecond);
        image.sprite = Profile.runFrames[Mathf.FloorToInt(elapsed * Profile.framesPerSecond) % 8];
    }

    void OnDisable() => StopRun();
}
