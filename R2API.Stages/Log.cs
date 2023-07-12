using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace R2API;

internal class Log
{
    private static ManualLogSource logger = null;

    internal Log(ManualLogSource logger_)
    {
        logger = logger_;
    }

    internal static void Debug(object data)
    {
        if (Application.isEditor)
            UnityEngine.Debug.Log(data);
        else
            logger.LogDebug(data);
    }

    internal static void Error(object data)
    {
        if (Application.isEditor)
            UnityEngine.Debug.LogError(data);
        else
            logger.LogError(data);

    }

    internal static void Fatal(object data)
    {
        if (Application.isEditor)
            UnityEngine.Debug.LogError(data);
        else
            logger.LogFatal(data);
    }

    internal static void Info(object data)
    {
        if (Application.isEditor)
            UnityEngine.Debug.Log(data);
        else
            logger.LogInfo(data);
    }

    internal static void Message(object data)
    {
        if (Application.isEditor)
            UnityEngine.Debug.Log(data);
        else
            logger.LogMessage(data);
    }

    internal static void Warning(object data)
    {
        if (Application.isEditor)
            UnityEngine.Debug.LogWarning(data);
        else
            logger.LogWarning(data);
    }
}
