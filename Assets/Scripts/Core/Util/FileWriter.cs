using System.IO;
using UnityEngine;

namespace Chess.Core.Util
{
    public class FileWriter
    {
        public static void WriteToTextAsset_EditorOnly(TextAsset textAsset, string text, bool append) {
#if UNITY_EDITOR
            string outputPath = UnityEditor.AssetDatabase.GetAssetPath(textAsset);
            StreamWriter writer = new StreamWriter(outputPath, append);
            writer.Write (text);
            writer.Close ();
#endif
        }
    }
}