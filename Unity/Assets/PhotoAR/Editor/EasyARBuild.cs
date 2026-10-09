using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Management;
using easyar;
using PhotoAR;
public static class EasyARBuild {
    [MenuItem("PhotoAR/EasyAR/Build independent surface test")]
    public static void Build(){
        ProjectTools.Prepare();
        var xr=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);xr.InitManagerOnStart=false;EditorUtility.SetDirty(xr);
        Directory.CreateDirectory("Assets/PhotoAR/LocalSecrets");
        var settings=AssetDatabase.LoadAssetAtPath<EasyARSettings>("Assets/PhotoAR/LocalSecrets/EasyARSettings.asset");
        if(!settings){settings=ScriptableObject.CreateInstance<EasyARSettings>();AssetDatabase.CreateAsset(settings,"Assets/PhotoAR/LocalSecrets/EasyARSettings.asset");}
        var keyPath=Environment.GetEnvironmentVariable("PHOTOAR_EASYAR_KEY_FILE");
        if(!string.IsNullOrEmpty(keyPath))settings.LicenseKey=File.ReadAllText(keyPath).Trim().Trim('\uFEFF');
        if(string.IsNullOrWhiteSpace(settings.LicenseKey))throw new Exception("EasyAR license file required.");
        // The SDK's build verifier uses internal APIs; check before it can include key fragments in an error log.
        var engine=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("easyar.Engine")).First(t=>t!=null);
        var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
        var verify=engine.GetMethod("isLicenseKeyMatched",flags);var variant=engine.GetMethod("variant",flags).Invoke(null,null);
        var android=Enum.Parse(verify.GetParameters()[3].ParameterType,"Android");
        if(!(bool)verify.Invoke(null,new object[]{settings.LicenseKey,"com.photoar.local",variant,android}))throw new Exception("EasyAR license does not match Android package com.photoar.local or SDK variant. Key omitted.");
        settings.InitializeOnStartup=true;settings.VerifyLicenseWhenBuild=true;
        settings.ARCoreSDK=EasyARSettings.ARCoreType.External;
        EditorUtility.SetDirty(settings);EditorBuildSettings.AddConfigObject("EasyAR.Settings",settings,true);
        var scene=EditorSceneManager.OpenScene("Assets/EasyARSurfaceSample/Scenes/SurfaceTracking.unity");
        foreach(var name in new[]{"Canvas","Sample","EventSystem"}){var go=GameObject.Find(name);if(go)UnityEngine.Object.DestroyImmediate(go);}
        var target=UnityEngine.Object.FindFirstObjectByType<SurfaceTargetController>();
        if(!target)throw new Exception("Official sample surface target missing");
        for(int i=target.transform.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(target.transform.GetChild(i).gameObject);
        var app=new GameObject("PhotoAR").AddComponent<EasyARSurfaceApp>();app.target=target;app.session=UnityEngine.Object.FindFirstObjectByType<easyar.ARSession>();
        app.initialModel=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/PhotoAR/TestAssets/PhoneModel.txt");
        if(!app.initialModel)throw new Exception("Independent model fixture missing");
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/EasyARSurface.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/EasyARSurface.unity",true)};
        AssetDatabase.SaveAssets();Directory.CreateDirectory("../builds");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/EasyARSurface.unity"},locationPathName="../builds/PhotoAR-EasyAR.apk",target=BuildTarget.Android,options=BuildOptions.Development});
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("EasyAR surface test build failed");
        Debug.Log("PHOTOAR_EASYAR_BUILD_OK");
    }
}
