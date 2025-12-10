
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "forexport", menuName = "Scriptable Objects/forexport")]
public class Forexport : ScriptableObject
{
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

        // находим название пакета
        int index = json.IndexOf($"\"{packageName}\"", StringComparison.Ordinal);
        if (index < 0) return "not found";

        // ищем "hash": "..."
        int hashIndex = json.IndexOf("\"hash\":", index, StringComparison.Ordinal);
        int start = json.IndexOf("\"", hashIndex + 7, StringComparison.Ordinal) + 1;
        int end = json.IndexOf("\"", start, StringComparison.Ordinal);

        return json.Substring(start, end - start);
    }
    
    string GetGitPackageUrlSimple(string packageName)
    {
        string path = Path.Combine(Application.dataPath, "../Packages/packages-lock.json");
        string json = File.ReadAllText(path);

        // Находим пакет
        int index = json.IndexOf($"\"{packageName}\"", StringComparison.Ordinal);
        if (index < 0) return "not found";

        // Находим "version": "..."
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
                Debug.LogWarning($"[GetPackageURLsWithHash] URL not found for package: {packageName}");
                continue;                
            }


            //return $"{url}#{hash}";
            if (string.IsNullOrEmpty(hash) || hash == "not found")
            {
                Debug.LogWarning($"[GetPackageURLsWithHash] Hash not found for package: {packageName}");
                result.Add(url);
                continue;
            }
            // Формируем "<URL>#<hash>"
            result.Add($"{url}#{hash}");
        }

        return result;
    }

}