using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace export
{
    public static class PackagesRepo
    {
        static string repoUrl = "https://github.com/Chelilack/ForDesigner.git";
        public static string InitializeImportRepo() 
        {
            string tempRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "UnityGitTemp"
            );

            string repoPath = Path.Combine(tempRoot, "RemoteImportRepo");

            Directory.CreateDirectory(tempRoot);
            Debug.Log($"created repo in {tempRoot}");

            if (!Directory.Exists(Path.Combine(repoPath, ".git")))
            {
                GitReader.Run(
                    $"clone --no-checkout {repoUrl} \"{repoPath}\"",
                    tempRoot
                );

                GitReader.Run("sparse-checkout init --no-cone", repoPath);
            }
            return repoPath;
            
        }
        public static string InitializeExportRepo() 
        {
            string tempRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "UnityGitTemp"
            );

            string repoPath = Path.Combine(tempRoot, "RemoteExportRepo");

            Directory.CreateDirectory(tempRoot);
            Debug.Log($"created repo in {tempRoot}");

            if (!Directory.Exists(Path.Combine(repoPath, ".git")))
            {
                // Клонируем без checkout
                GitReader.Run(
                    $"clone --no-checkout {repoUrl} \"{repoPath}\"",
                    tempRoot
                );

            }
            return repoPath;
            
        }
        public static string[] GetPackagesNames(string repoPath)
        {
            string branch = "origin/main";

            string output = GitReader.Run(
                $"ls-tree -r --name-only {branch}",
                repoPath
            );
            string[] allFiles = output
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            string[] packageNames = allFiles
                .Where(f => f.EndsWith(".unitypackage"))
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .ToArray();
            return packageNames;
        }

        public static void InstallPackages(string repoPath, string[] packageNames)
        {
            string[] packagePaths = packageNames
                .Select(name => $"/\"{name}.unitypackage\"")
                .ToArray();
            Debug.Log(string.Join(',',packagePaths));
            string filesArg = string.Join(" ", packagePaths);

            GitReader.Run("fetch origin", repoPath);
            
            GitReader.Run(
                $"sparse-checkout set {filesArg}",
                repoPath
            );
            
            GitReader.Run(
                "checkout -f -q main",
                repoPath
            );
        }
        public static void ImportUnityPackages(string repoPath)
        {

            if (!Directory.Exists(repoPath))
            {
                UnityEngine.Debug.LogError($"Папка не найдена: {repoPath}");
                return;
            }

            string[] packageFiles = Directory.GetFiles(
                repoPath,
                "*.unitypackage",
                SearchOption.AllDirectories
            );

            if (packageFiles.Length == 0)
            {
                UnityEngine.Debug.Log("Файлы .unitypackage не найдены.");
                return;
            }

            foreach (string packagePath in packageFiles)
            {
                string normalizedPath = packagePath.Replace("\\", "/");
                
                UnityEngine.Debug.Log($"Импорт пакета: {normalizedPath}");
                AssetDatabase.ImportPackage(normalizedPath, false);
            }
            UnityEngine.Debug.Log("Импорт всех unitypackage завершён.");
        }
        static public void ExportToRepo(string repoPath, string packagePath)
        {
            
            string relativePath = packagePath
                .Replace(repoPath, "")
                .TrimStart('/', '\\');
            
            Debug.Log($"relativePath:{relativePath}");

            GitReader.Run("fetch origin", repoPath);
            GitReader.Run("read-tree origin/main", repoPath);
            GitReader.Run($"add --sparse \"{relativePath}\"", repoPath);

            string commitMessage = $"Update unitypackage: {Path.GetFileName(packagePath)}";

            GitReader.Run($"commit -m \"{commitMessage}\"", repoPath);

            GitReader.Run("push origin main", repoPath);

            Debug.Log("Git add / commit / push выполнены.");
        }
    }
}