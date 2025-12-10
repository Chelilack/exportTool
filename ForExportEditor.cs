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

        if (GUILayout.Button("Collect ASMDEFs"))
        {
            var result = exportSO.FindAllTypes(Path.GetDirectoryName(AssetDatabase.GetAssetPath(exportSO)));
            Debug.Log($"Result: {string.Join(',', result)}");
            var result2 = exportSO.GetHashSetAsmdef(result);
            Debug.Log($"Result2: {string.Join(',', result2)}");
            var result3 = exportSO.GetPackageName(result2);
            Debug.Log($"Result3: {string.Join(',', result3)}");
            var result4 = exportSO.GetPackageURLWithHash(result3);
            Debug.Log($"Result4: {string.Join(',', result4)}");
            /*collector.results = Collect(collector.prefabsFolder);
            EditorUtility.SetDirty(collector);*/
        }
    }

    
}
}