using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "forexport", menuName = "Scriptable Objects/forexport")]
public class Forexport : ScriptableObject
{
    public List<string> gitUrls;

    public void CollectUrlsForExport()
    {
        var result = FindAllTypes(Path.GetDirectoryName(AssetDatabase.GetAssetPath(this)));
        var result2 = GetHashSetAsmdef(result);
        var result3 = GetPackageName(result2);
        var result4 = GetPackageURLWithHash(result3);
        Debug.Log($"Result4: {string.Join(',', result4)}");
        gitUrls = result4.ToList();
    }
    private Dictionary<string,string> asmdefPackageNames = new Dictionary<string,string>
    {
        { "testPackage", "com.chelilack.testpackage" },
        { "secondTest", "com.chelilack.secondtestpackage" }
    };

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
        }
        return packageNames;
    }
    public string GetGitPackageHash(string packageName)
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
    
    string GetGitPackageUrlSimple(string packageName)
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
}