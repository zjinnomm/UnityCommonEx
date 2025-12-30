using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEditor;

namespace UnityCommonEx
{

    public static class ScatteredDataPack
    {

        public static void DataPack(string sourcePath, string targetPath)
        {
            var config = JsonUtil.Read<ScatteredDataLoadConfig>(sourcePath);
            config.Prepare();

            string filePath = Path.Join("Assets", "Resources", $"{targetPath}.txt");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                File.Delete($"{filePath}.meta");
            }

            using (var fs = File.OpenWrite(filePath))
            {
                config.Dump(fs);
            }
            AssetDatabase.Refresh();
        }

    }

}