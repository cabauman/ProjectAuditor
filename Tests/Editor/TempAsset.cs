using System;
using System.IO;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;

namespace Unity.ProjectAuditor.EditorTests
{
    public class TempAsset
    {
        const string k_TempFolder = "ProjectAuditor-Temp";

        public readonly string relativePath;

        public string fileName
        {
            get { return Path.GetFileName(relativePath); }
        }

        private TempAsset(string fileName)
        {
            relativePath = PathUtils.Combine("Assets", k_TempFolder, fileName);

            if (!File.Exists(relativePath))
                Directory.CreateDirectory(Path.GetDirectoryName(relativePath));
        }

        public TempAsset(string fileName, string content) :
            this(fileName)
        {
            File.WriteAllText(relativePath, content);

            Assert.True(File.Exists(relativePath));

            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
        }

        public TempAsset(string fileName, byte[] byteContent) :
            this(fileName)
        {
            File.WriteAllBytes(relativePath, byteContent);

            Assert.True(File.Exists(relativePath));

            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
        }

        public static TempAsset Save(UnityEngine.Object asset, string fileName)
        {
            var tempAsset = new TempAsset(fileName);
            AssetDatabase.CreateAsset(asset, tempAsset.relativePath);
            AssetDatabase.ImportAsset(tempAsset.relativePath, ImportAssetOptions.ForceUpdate);

            return tempAsset;
        }

        public static void Cleanup()
        {
            var path = Path.Combine("Assets", k_TempFolder);
            if (Directory.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
