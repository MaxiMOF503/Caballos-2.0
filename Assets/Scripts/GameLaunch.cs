using UnityEngine;

public enum GameLaunchMode { NewGame, LoadGame }

// One-use intent; no persistent GameManager is carried from menu to hub.
public static class GameLaunch
{
    public const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    public const string HubScenePath = "Assets/Scenes/hub.unity";
    static GameLaunchMode? pending;
    static string error;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { pending = null; error = null; }
    public static void Request(GameLaunchMode mode) { pending = mode; error = null; }
    public static bool TryConsume(out GameLaunchMode mode)
    {
        mode = pending.GetValueOrDefault();
        bool exists = pending.HasValue;
        pending = null;
        return exists;
    }
    public static void Fail(string message) { pending = null; error = message; }
    public static string TakeError() { string message = error; error = null; return message; }
}
