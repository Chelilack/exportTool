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

        if (GUILayout.Button("Collect packages"))
        {
            exportSO.CollectUrlsForExport();
            /*collector.results = Collect(collector.prefabsFolder);
            EditorUtility.SetDirty(collector);*/
        }
        if (GUILayout.Button("Install packages"))
        {
            exportSO.InstallGitPackages();
            /*collector.results = Collect(collector.prefabsFolder);
            EditorUtility.SetDirty(collector);*/
        }
    }

    
}
}