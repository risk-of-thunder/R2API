using BepInEx.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace R2API;

internal static class Log
{
    static ManualLogSource _logSource;

    public static void Init(ManualLogSource logSource)
    {
        _logSource = logSource;
    }

    [Conditional("DEBUG")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Debug(object data)
    {
        _logSource.LogDebug(data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Info(object data)
    {
        _logSource.LogInfo(data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Message(object data)
    {
        _logSource.LogMessage(data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Warning(object data)
    {
        _logSource.LogWarning(data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Error(object data)
    {
        _logSource.LogError(data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Fatal(object data)
    {
        _logSource.LogFatal(data);
    }
}
