using System;

public static class UmeLanguage
{
    // 0=“ú–{Œê, 1=English
    public static int Index { get; private set; } = 0;
    public static event Action<int> Changed;

    public static void Set(int index)
    {
        Index = (index <= 0) ? 0 : 1;
        Changed?.Invoke(Index);
    }
}
