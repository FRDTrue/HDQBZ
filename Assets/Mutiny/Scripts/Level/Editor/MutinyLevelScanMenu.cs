using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mutiny.Levels.Editor
{
    public static class MutinyLevelScanMenu
    {
        [MenuItem("Mutiny/Levels/Scan All XML")]
        public static void ScanAllXml()
        {
            string input = Path.Combine(Application.dataPath, "Mutiny", "Data", "Levels");
            string output = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "Docs",
                "10-OriginalEvidence",
                "Artifacts",
                "LevelScan"));
            try
            {
                MutinyLevelScanResult result = MutinyLevelScanner.Scan(input, output);
                string message = $"Level scan: {result.Parsed}/{result.Files} parsed, {result.Errors} errors; " +
                    $"tiles={result.TileTypes}, objects={result.ObjectTypes}, attributes={result.Attributes}. " +
                    $"Reports: {output}";
                if (result.Errors > 0 || result.Files != 18)
                    Debug.LogError(message);
                else
                    Debug.Log(message);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
