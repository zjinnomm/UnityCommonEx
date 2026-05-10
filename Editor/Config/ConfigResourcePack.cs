using System.IO;
using System.Text;
using UnityEditor;

namespace UnityCommonEx
{
    public static class ConfigResourcePack
    {
        public static string Pack(string sourceDir, string targetPath)
        {
            string filePath = Path.Join("Assets", "Resources", $"{targetPath}.txt");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                File.Delete($"{filePath}.meta");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using (var fs = File.OpenWrite(filePath))
            using (var writer = new BinaryWriter(fs, Encoding.UTF8))
            {
                foreach (string iniPath in Directory.EnumerateFiles(sourceDir, "*.ini", SearchOption.AllDirectories))
                {
                    string relativePath = Path.GetRelativePath(sourceDir, iniPath).Replace('\\', '/');
                    byte[] bytes = File.ReadAllBytes(iniPath);
                    writer.Write(relativePath);
                    writer.Write(bytes.Length);
                    writer.Write(bytes);
                }
            }

            AssetDatabase.Refresh();
            return filePath;
        }
    }
}
