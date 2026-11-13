using UnityEngine;
using UnityEngine.UI;

public sealed class HorseVisualAnimator : MonoBehaviour
{
    public HorseVisualProfile Profile { get; private set; }
    public bool IsRunning { get; private set; }
    Image image;
    Sprite baseSprite;
    float elapsed;

    public void Bind(Caballo horse, Image target, bool presentacionCarrera = false)
    {
        image = target;
        var catalog = HorseVisualCatalog.Default;
        Profile = catalog != null ? catalog.ResolveForRace(horse) : null;
        baseSprite = Profile != null ? Profile.baseSprite : horse.sprite;
        bool normalizada = presentacionCarrera && image != null && Profile != null && Profile.IsValid;
        if (normalizada)
        {
            PresentacionCaballoCarrera.Configurar(image, Profile);
            // A supported stance from the same sheet, also used at the finish.
            baseSprite = Profile.runFrames[2];
        }
        else if (presentacionCarrera && image != null)
        {
            // Keep the previous layout for a legacy sprite with no animation.
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = new Vector2(0, -18);
            rect.localScale = Vector3.one;
        }
        StopRun();
        if (image != null) image.preserveAspect = !normalizada;
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
