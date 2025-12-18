using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using export;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "forexport", menuName = "Scriptable Objects/forexport")]
public class Forexport : ScriptableObject
{
    public List<string> gitUrls;
    private HashSet<string> asmdefNewPackages = new HashSet<string>(); // for packages that was created in this project and pushed on git 
    public string packageName;
    public string exportPath;
    
    public void CollectUrlsForExport()
    {
        exportPath = PackagesRepo.InitializeExportRepo();
        var result = FindAllTypes(Path.GetDirectoryName(AssetDatabase.GetAssetPath(this)));
        var result2 = GetHashSetAsmdef(result);
        var result3 = GetPackageName(result2);
        var result4 = GetPackageURLWithHash(result3);
        var result5 = GetGitURLWithHash();
        Debug.Log($"Result4: {string.Join(',', result4)}");
        gitUrls = result4.ToList();
        gitUrls.AddRange(result5.ToList());
    }
    private Dictionary<string, string> asmdefPackageNames = new Dictionary<string, string>();
    /*{
        { "testPackage", "com.chelilack.testpackage" },
        { "secondTest", "com.chelilack.secondtestpackage" }
    };*/

    public  HashSet<Type> FindAllTypes(string folderPath)
    {
        HashSet<Type> uniqueTypes = new HashSet<Type>();
        
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

        foreach (string guid in guids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
                continue;

            MonoBehaviour[] monos = root.GetComponentsInChildren<MonoBehaviour>(true);

            foreach (var mono in monos)
            {
                if (mono == null)
                    continue; 

                Type type = mono.GetType();
                if (type != null)
                {
                    uniqueTypes.Add(type);
                }
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        return uniqueTypes;
    }

    public HashSet<string> GetHashSetAsmdef( HashSet<Type> types)
    {
        HashSet<string> asmdefNames = new HashSet<string>();

        foreach (var type in types)
        {
            if (type == null)
                continue;
            string asmName = type.Assembly.GetName().Name;

            asmdefNames.Add(asmName);
        }
        return asmdefNames;
    }

    public HashSet<string> GetPackageName(HashSet<string> asmdefs)
    {
        HashSet<string> packageNames = new HashSet<string>();
        foreach (var asmdef in asmdefs)
        {
            if (asmdefPackageNames.ContainsKey(asmdef))
            {
                packageNames.Add(asmdefPackageNames[asmdef]);
            }
            else
            {
                asmdefNewPackages.Add(asmdef);
            }
        }
        foreach (var packageName in asmdefNewPackages)
        {
            Debug.Log($"{packageName} -- {FindGitDirUpFromAsmdef(packageName)}, {GetRemoteUrl(FindGitDirUpFromAsmdef(packageName))}");
            Debug.Log($"{packageName} -- {FindPackageJsonForAsmdef(packageName)}");
        }
        
        return packageNames;
    }
    private string GetGitPackageHash(string packageName)
    {
        string path = Path.Combine(Application.dataPath, "../Packages/packages-lock.json");
        string json = File.ReadAllText(path);
        
        int index = json.IndexOf($"\"{packageName}\"", StringComparison.Ordinal);
        if (index < 0) return "not found";
        
        int hashIndex = json.IndexOf("\"hash\":", index, StringComparison.Ordinal);
        int start = json.IndexOf("\"", hashIndex + 7, StringComparison.Ordinal) + 1;
        int end = json.IndexOf("\"", start, StringComparison.Ordinal);

        return json.Substring(start, end - start);
    }
    
    private string GetGitPackageUrlSimple(string packageName)
    {
        string path = Path.Combine(Application.dataPath, "../Packages/packages-lock.json");
        string json = File.ReadAllText(path);
        
        int index = json.IndexOf($"\"{packageName}\"", StringComparison.Ordinal);
        if (index < 0) return "not found";
        
        int versionIndex = json.IndexOf("\"version\":", index, StringComparison.Ordinal);
        int start = json.IndexOf("\"", versionIndex + 10, StringComparison.Ordinal) + 1;
        int end = json.IndexOf("\"", start, StringComparison.Ordinal);

        string fullUrl = json.Substring(start, end - start);
        
        int hashPos = fullUrl.IndexOf('#');
        if (hashPos >= 0)
            return fullUrl.Substring(0, hashPos);

        return fullUrl;
    }

    public HashSet<string> GetPackageURLWithHash(HashSet<string> packageNames)
    {
        HashSet<string> result = new HashSet<string>();

        foreach (var packageName in packageNames)
        {

            string url = GetGitPackageUrlSimple(packageName);
            string hash = GetGitPackageHash(packageName);

            if (url == "not found")
            {
                Debug.Log($"[GetPackageURLsWithHash] URL not found for package: {packageName}");
                //if ()
                continue;                
            }


            //return $"{url}#{hash}";
            if (string.IsNullOrEmpty(hash) || hash == "not found")
            {
                Debug.Log($"[GetPackageURLsWithHash] Hash not found for package: {packageName}");
                result.Add(url);
                continue;
            }
            
            result.Add($"{url}#{hash}");
        }
        return result;
    }
    public HashSet<string> GetGitURLWithHash()
    {
        HashSet<string> result = new HashSet<string>();
        foreach (var asmdef in asmdefNewPackages)
        {
            string gitFolderPath = FindGitDirUpFromAsmdef(asmdef);
            string url = GetRemoteUrl(gitFolderPath);
            string hash = GetCommitHashForBranch(gitFolderPath,"master");
            result.Add($"{url}#{hash}");
        }
        return result;
    }
    public void InstallGitPackages()
    {
        if (gitUrls == null || gitUrls.Count == 0)
        {
            Debug.Log("[InstallGitPackages] gitUrls list is empty.");
            return;
        }

        EditorCoroutineUtility.StartCoroutineOwnerless(InstallPackagesCoroutine());
    }

    private System.Collections.IEnumerator InstallPackagesCoroutine()
    {
        Debug.Log($"[InstallGitPackages] Installing {gitUrls.Count} packages...");

        foreach (var url in gitUrls)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Debug.Log("[InstallGitPackages] Empty or null URL skipped.");
                continue;
            }

            Debug.Log($"[InstallGitPackages] Adding package: {url}");

            AddRequest request = Client.Add(url);
            
            while (!request.IsCompleted)
                yield return null;

            if (request.Status == StatusCode.Success)
            {
                Debug.Log($"[InstallGitPackages] Installed: {request.Result.name} {request.Result.version}");
            }
            else if (request.Status >= StatusCode.Failure)
            {
                Debug.LogError(
                    $"[InstallGitPackages] Failed to install {url}\n" +
                    $"Error: {request.Error?.message}"
                );
            }
        }
        Debug.Log("[InstallGitPackages] All Add operations completed.");
    }
    
    public string FindPackageJsonForAsmdef(string asmdefName)
    {
        string guid = AssetDatabase.FindAssets($"{asmdefName} t:asmdef").FirstOrDefault();

        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogWarning($"ASMDEF '{asmdefName}' не найден.");
            return null;
        }

        string asmdefPath = AssetDatabase.GUIDToAssetPath(guid);
        string directory = Path.GetDirectoryName(asmdefPath).Replace("\\", "/");
        
        string found = FindPackageJsonInDirectory(directory);
        if (found != null)
            return found;
        
        while (true)
        {
            var parent = Directory.GetParent(directory)?.FullName;
            if (parent == null)
                break;

            directory = parent.Replace("\\", "/");

            found = FindPackageJsonInDirectory(directory);
            if (found != null)
                return found;
        }

        Debug.LogWarning($"Для ASMDEF '{asmdefName}' не найден подходящий JSON-файл пакета.");
        return null;
    }
    private string FindPackageJsonInDirectory(string directory)
    {
        var jsonFiles = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);

        foreach (var file in jsonFiles)
        {
            if (IsUnityPackageJson(file))
                return file;
        }

        return null;
    }
    
    private bool IsUnityPackageJson(string path)
    {
        try
        {
            string content = File.ReadAllText(path);
            return content.Contains("\"name\"") && content.Contains("\"version\"");
        }
        catch
        {
            return false;
        }
    }
    
    public string GetCommitHashForBranch(string gitFolderPath, string branchName)
    {
        if (string.IsNullOrEmpty(gitFolderPath) || !Directory.Exists(gitFolderPath))
        {
            Debug.LogError("Git folder not found: " + gitFolderPath);
            return null;
        }
        
        string branchRefPath = Path.Combine(gitFolderPath, "refs", "heads", branchName);

        if (File.Exists(branchRefPath))
        {
            string hash = File.ReadAllText(branchRefPath).Trim();
            return hash;
        }
        
        string packedRefsPath = Path.Combine(gitFolderPath, "packed-refs");
        if (File.Exists(packedRefsPath))
        {
            Debug.Log($"packed-refs occured: {packedRefsPath}");
        }

        Debug.LogWarning($"Коммит ветки '{branchName}' не найден в: {gitFolderPath}");
        return null;
    }
    public string FindGitDirUpFromAsmdef(string asmdefName)
    {
        string guid = AssetDatabase.FindAssets($"{asmdefName} t:asmdef").FirstOrDefault();
        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogWarning($"ASMDEF '{asmdefName}' не найден.");
            return null;
        }

        string asmdefPath = AssetDatabase.GUIDToAssetPath(guid);
        string currentDir = Path.GetDirectoryName(asmdefPath);

        while (!string.IsNullOrEmpty(currentDir))
        {
            string gitPath = Path.Combine(currentDir, ".git");
            
            if (Directory.Exists(gitPath))
                return NormalizePath(gitPath);
            
            if (File.Exists(gitPath))
            {
                string resolved = ResolveGitDirFromGitFile(gitPath);
                if (!string.IsNullOrEmpty(resolved) && Directory.Exists(resolved))
                    return NormalizePath(resolved);
            }

            var parent = Directory.GetParent(currentDir);
            if (parent == null) break;
            currentDir = parent.FullName;
        }

        return null;
    }

    private string ResolveGitDirFromGitFile(string gitFilePath)
    {
        // Содержимое обычно: "gitdir: /path/to/actual/git/dir"
        string text = File.ReadAllText(gitFilePath).Trim();
        const string prefix = "gitdir:";
        if (!text.StartsWith(prefix))
            return null;

        string pathPart = text.Substring(prefix.Length).Trim();
        
        string baseDir = Path.GetDirectoryName(gitFilePath);
        string combined = Path.IsPathRooted(pathPart) ? pathPart : Path.GetFullPath(Path.Combine(baseDir, pathPart));
        return combined;
    }

    private string NormalizePath(string p) => p.Replace("\\", "/");
    
    public static string GetRemoteUrl(string gitDirPath, string remoteName = "origin")
    {
        if (string.IsNullOrWhiteSpace(gitDirPath))
            throw new ArgumentException("gitDirPath is null/empty");

        string configPath = Path.Combine(gitDirPath, "config");
        if (!File.Exists(configPath))
            return null;

        string targetSection = $"remote \"{remoteName}\"";
        bool inTargetSection = false;

        foreach (var raw in File.ReadLines(configPath))
        {
            string line = raw.Trim();

            if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";"))
                continue;

            // Секция: [remote "origin"]
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                string sectionName = line.Substring(1, line.Length - 2).Trim();
                inTargetSection = string.Equals(sectionName, targetSection, StringComparison.Ordinal);
                continue;
            }

            if (!inTargetSection)
                continue;
            
            int eq = line.IndexOf('=');
            if (eq < 0)
                continue;
            
            string key = line.Substring(0, eq).Trim();
            string value = line.Substring(eq + 1).Trim();

            if (string.Equals(key, "url", StringComparison.OrdinalIgnoreCase))
                return value;
        }

        return null;
    }
    
    public void ExportFolder(string sourceFolder, string outputPath)
    {
        if (packageName == null)
        {
            Debug.LogError("packageName is null");
            return;
        }
        if (!AssetDatabase.IsValidFolder(sourceFolder))
        {
            Debug.LogError($"Папка не найдена: {sourceFolder}");
            return;
        }
        CollectUrlsForExport();
        
        string[] assetGuids = AssetDatabase.FindAssets("", new[] { sourceFolder });
        string[] assetPaths = new string[assetGuids.Length];

        for (int i = 0; i < assetGuids.Length; i++)
        {
            assetPaths[i] = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
        }
        
        AssetDatabase.ExportPackage(
            assetPaths,
            outputPath+$"\\{packageName}.unitypackage",
            ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies
        );
        PackagesRepo.ExportToRepo(exportPath, outputPath+$"\\{packageName}.unitypackage");
        
        

        Debug.Log($"Экспорт завершён: {outputPath}");
    }
}