using System;

public enum MainMenuIntent
{
    None = 0,
    ContinueRun = 1,
    NewRun = 2
}

public static class MainMenuFlow
{
    public static MainMenuIntent PendingIntent { get; set; } = MainMenuIntent.None;

    public static void Clear()
    {
        PendingIntent = MainMenuIntent.None;
    }
}
