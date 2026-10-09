using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// File-triggered operations in this already-open editor; no second editor instance.
[InitializeOnLoad]
public static class AutomationBridge {
    static readonly string Root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../reports"));
    static AutomationBridge(){EditorApplication.update+=Poll;}
    static void Poll(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        var request=Path.Combine(Root,"editor-request.txt");if(!File.Exists(request))return;
        string command=File.ReadAllText(request).Trim();File.Delete(request);
        try {
            switch(command){
                case "tests":ProjectTools.SelfTest();break;
                case "prepare":ProjectTools.Prepare();break;
                case "build":ProjectTools.BuildAndroid();break;
                default:throw new Exception("Unknown operation: "+command);
            }
            File.WriteAllText(Path.Combine(Root,"editor-result.txt"),"PASS "+command);
        }catch(Exception e){File.WriteAllText(Path.Combine(Root,"editor-result.txt"),"FAIL "+command+"\n"+e);Debug.LogException(e);}
    }
}
