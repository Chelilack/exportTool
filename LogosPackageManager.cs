using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace export
{
    public class LogosPackageManager : EditorWindow
    {
        private bool toggleValue;
        private string repoPath;
        private string[] packagesNames;
        private Dictionary<string, bool> toggles = new Dictionary<string, bool>();

        [MenuItem("Tools/Logos Package Manager")]
        public static void ShowWindow()
        {
            GetWindow<LogosPackageManager>("Logos Package Manager");
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Packages", EditorStyles.boldLabel);

            GUILayout.Space(10);
            
            GUIStyle toggleStyle = new GUIStyle(GUI.skin.toggle);
            toggleStyle.padding.left += 5;
            if (packagesNames != null && packagesNames.Length == toggles.Keys.Count )
            {
                foreach (string packageName in packagesNames)
                {
                    toggles[packageName] = GUILayout.Toggle(
                        toggles[packageName],
                        packageName,
                        toggleStyle,
                        GUILayout.ExpandWidth(true)
                    );
                    GUILayout.Space(10);
                }
            }




            if (GUILayout.Button("Install"))
            {
                repoPath = PackagesRepo.Initialize();
            }
            
            if (GUILayout.Button("GetPackagesNames") )
            {
                if (repoPath != null)
                {
                    packagesNames = PackagesRepo.GetPackagesNames(repoPath);
                    foreach (var packageName in packagesNames)
                    {
                        toggles[packageName] = false;
                    }
                }
                else
                {
                    Debug.Log("Repo Initialized wrong");
                }
            }
            
            if (GUILayout.Button("InstallPackages"))
            {
                string[] selectedKeys = toggles
                    .Where(kvp => kvp.Value)
                    .Select(kvp => kvp.Key)
                    .ToArray();
                Debug.Log("Selected Keys: " + string.Join(", ", selectedKeys));
                PackagesRepo.InstallPackages(repoPath, selectedKeys);
            }
            if (GUILayout.Button("ImportPackages"))
            {
                PackagesRepo.ImportUnityPackages(repoPath);
            }
            GUILayout.EndVertical();
        }
    }
}