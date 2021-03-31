using System.Collections.Generic;
using System.IO;

namespace R2API.Utils {

    internal static class DirectoryUtilities {
        private static bool _alreadyPrintedFolderStructure;
        private static bool _bepinexFolderPrinted;

        private static readonly HashSet<string> BannedFolders = new HashSet<string> {
            "MonoBleedingEdge",
            "Risk of Rain 2_Data"
        };

        internal static void LogFolderStructureAsTree(string directory) {
            if (_alreadyPrintedFolderStructure)
                return;
            WriteFolderStructure(directory);
            if (!_bepinexFolderPrinted) {
                WriteFolderStructure(BepInEx.Paths.BepInExRootPath);
            }
            _alreadyPrintedFolderStructure = true;
        }

        private static void WriteFolderStructure(string directory) {
            R2API.Logger.LogDebug("");
            R2API.Logger.LogDebug($"+ {new DirectoryInfo(directory).Name}");

            foreach (string dir in Directory.GetDirectories(directory)) {
                WriteFolderStructureRecursively(dir);
            }

            string[] files = Directory.GetFiles(directory);
            for (int i = 1; i <= files.Length; i++) {
                var fileInfo = new FileInfo(files[i - 1]);
                R2API.Logger.LogDebug(
                    $"{GenerateSpaces(0)}{(i != files.Length ? "|" : "`")}---- {fileInfo.Name} ({ParseSize(fileInfo.Length)})");
            }
        }

        private static void WriteFolderStructureRecursively(string directory, int spaces = 0) {
            var dirInfo = new DirectoryInfo(directory);
            R2API.Logger.LogDebug($"{GenerateSpaces(spaces)}|---+ {dirInfo.Name}");

            if (!_bepinexFolderPrinted && BepInEx.Paths.BepInExRootPath == directory) {
                _bepinexFolderPrinted = true;
            }

            if (dirInfo.Parent != null && (BannedFolders.Contains(dirInfo.Name) ||
                                           BannedFolders.Contains($"{dirInfo.Parent.Name}/{dirInfo.Name}"))) {
                R2API.Logger.LogDebug($"{GenerateSpaces(spaces + 4)}`---- (Folder content not shown)");
                return;
            }

            foreach (string dir in Directory.GetDirectories(directory)) {
                WriteFolderStructureRecursively(dir, spaces + 4);
            }

            string[] files = Directory.GetFiles(directory);
            for (int i = 1; i <= files.Length; i++) {
                var fileInfo = new FileInfo(files[i - 1]);
                R2API.Logger.LogDebug(
                    $"{GenerateSpaces(spaces + 4)}{(i != files.Length ? "|" : "`")}---- {fileInfo.Name} ({ParseSize(fileInfo.Length)})");
            }
        }

        private static string ParseSize(long lSize) {
            string[] units = { "B", "KB", "MB", "GB" };
            float size = lSize;
            int unit = 0;

            while (size > 1024) {
                unit++;
                size /= 1024;
            }

            string number = size.ToString("F2");

            return number + units[unit];
        }

        private static string GenerateSpaces(int spaces) {
            string s = "";
            for (int i = 1; i <= spaces; i += 4)
                s += "|   ";
            return s;
        }
    }
}
