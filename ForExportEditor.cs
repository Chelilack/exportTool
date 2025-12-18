using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace export
{

[CustomEditor(typeof(Forexport))]
public class ForExportEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var exportSO = (Forexport)target;
        string assetPath = AssetDatabase.GetAssetPath(exportSO);

        if (GUILayout.Button("Collect packages"))
        {
            exportSO.CollectUrlsForExport();
        }
        if (GUILayout.Button("Install packages"))
        {
            exportSO.InstallGitPackages();
        }
        if (GUILayout.Button("Export folder"))
        {
            exportSO.ExportFolder( Path.GetDirectoryName(assetPath),exportSO.exportPath);
        }
    }

    
}
}