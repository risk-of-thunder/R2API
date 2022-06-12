using BepInEx.Logging;
using System.Collections.Generic;
using System.Linq;

namespace R2API.Utils;

public static class ManualLogSourceExtension
{

    public static void LogBlockError(this ManualLogSource? logger, IEnumerable<string?>? lines, int width = 70) =>
        logger.LogBlock(LogLevel.Error, "ERROR", lines, width);

    public static void LogBlockWarning(this ManualLogSource? logger, IEnumerable<string?>? lines, int width = 70) =>
        logger.LogBlock(LogLevel.Warning, "WARNING", lines, width);

    public static void LogBlock(this ManualLogSource? logger, LogLevel level, string? header, IEnumerable<string?>? lines, int width = 70)
    {
        var barrier = new string('*', width + 2);
        var empty = CenterText("", width);

        logger.Log(level, barrier);
        logger.Log(level, empty);
        logger.Log(level, CenterText($"!{header}!", width));

        lines.ToList().ForEach(x => logger.Log(level, CenterText(x, width)));

        logger.Log(level, empty);
        logger.Log(level, barrier);
    }

    // ReSharper disable FormatStringProblem
    public static string CenterText(string? text = "", int width = 70) =>
        string.Format("*{0," + (width / 2 + text.Length / 2) + "}{1," + (width / 2 - text.Length / 2) + "}*", text, " ");

    // ReSharper restore FormatStringProblem
}
