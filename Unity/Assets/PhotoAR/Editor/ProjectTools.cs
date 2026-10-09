using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using PhotoAR;
public static class ProjectTools {
    [MenuItem("PhotoAR/Prepare Android project")]
    public static void Prepare() {
        Directory.CreateDirectory("Assets/Scenes");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("PhotoAR").AddComponent<AppController>();
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/PhotoAR.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/PhotoAR.unity",true)};
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.photoar.local");
        PlayerSettings.productName="PhotoAR";PlayerSettings.companyName="PhotoAR";
        PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3});
        PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
        PlayerSettings.insecureHttpOption=InsecureHttpOption.AlwaysAllowed;
        var playerSettings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        playerSettings.FindProperty("activeInputHandler").intValue=2;
        var entry=playerSettings.FindProperty("androidApplicationEntry");if(entry!=null)entry.intValue=1;
        playerSettings.ApplyModifiedProperties();
        var settings=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
        if(settings==null) {
            var perTarget=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(perTarget,"Assets/XRSettings.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey,perTarget,true);
            settings=ScriptableObject.CreateInstance<XRGeneralSettings>();
            AssetDatabase.AddObjectToAsset(settings,perTarget);perTarget.SetSettingsForBuildTarget(BuildTargetGroup.Android,settings);
        }
        if(settings.Manager==null){settings.Manager=ScriptableObject.CreateInstance<XRManagerSettings>();AssetDatabase.AddObjectToAsset(settings.Manager,settings);}
        if(!XRPackageMetadataStore.AssignLoader(settings.Manager,"UnityEngine.XR.ARCore.ARCoreLoader",BuildTargetGroup.Android))throw new Exception("ARCore loader assignment failed");
        settings.InitManagerOnStart=true;
        var arcore=UnityEditor.XR.ARCore.ARCoreSettings.GetOrCreateSettings();
        arcore.depth=UnityEditor.XR.ARCore.ARCoreSettings.Requirement.Optional;
        UnityEditor.XR.ARCore.ARCoreSettings.currentSettings=arcore;
        // Keep the runtime-created vertex color material shader in player builds.
        var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");var shader=Shader.Find("PhotoAR/VertexColor");bool found=false;
        for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;
        if(!found){shaders.InsertArrayElementAtIndex(shaders.arraySize);shaders.GetArrayElementAtIndex(shaders.arraySize-1).objectReferenceValue=shader;graphics.ApplyModifiedProperties();}
        PhotoARBranding.Configure();AssetDatabase.SaveAssets();Debug.Log("PHOTOAR_PREPARE_OK");
    }
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    [MenuItem("PhotoAR/Run independent self tests")]
    public static void SelfTest() {
        TestGeometry();TestModel();TestRotation();TestPlacement();
        Directory.CreateDirectory("../reports");File.WriteAllText("../reports/unity-selftest.txt","PASS: geometry, mesh, rotation, anchor immobility, projected plane selection");
        Debug.Log("PHOTOAR_SELFTEST_OK");
    }
    [MenuItem("PhotoAR/Tests/1 Plane geometry")]
    public static void TestGeometry() {
        var p=new[]{new Vector2(0,0),new Vector2(4,0),new Vector2(4,2),new Vector2(0,2)};
        Check(Mathf.Abs(Geometry.Area(p)-8)<.001f,"Polygon area");
        Check((Geometry.Center(p)-new Vector2(2,1)).magnitude<.001f,"Polygon centroid");
        var clipped=Geometry.ClipViewport(new System.Collections.Generic.List<Vector2>{new Vector2(-1,-1),new Vector2(2,-1),new Vector2(2,2),new Vector2(-1,2)});
        Check(Mathf.Abs(Geometry.Area(clipped)-1)<.001f,"Viewport clipping");
        Debug.Log("PHOTOAR_GEOMETRY_OK");
    }
    [MenuItem("PhotoAR/Tests/2 Model loader")]
    public static void TestModel() {
        var mesh=ObjLoader.Parse("v 0 0 0 1 0 0\nv 1 0 0 0 1 0\nv 0 0 1 0 0 1\nf -3 -2 -1\n");
        Check(mesh.vertexCount==3&&mesh.triangles.Length==3,"Mesh topology");
        Check(mesh.vertices[2]==Vector3.up,"Coordinate conversion");
        Check(mesh.colors[0]==Color.red,"Vertex colors");UnityEngine.Object.DestroyImmediate(mesh);
        bool invalid=false;try{ObjLoader.Parse("v 0 0 0\nf 1 2 3");}catch(FormatException){invalid=true;}Check(invalid,"Malformed mesh rejection");
        Debug.Log("PHOTOAR_MODEL_OK");
    }
    [MenuItem("PhotoAR/Tests/3 Rotation and fixed anchor")]
    public static void TestRotation() {
        var anchor=new GameObject("Test anchor");anchor.transform.position=new Vector3(1,2,3);
        var child=new GameObject("Test model");child.transform.SetParent(anchor.transform,false);var spin=child.AddComponent<Spin>();
        spin.Step(3);Check(Quaternion.Angle(child.transform.localRotation,Quaternion.Euler(0,90,0))<.01f,"Spin rate");
        Check(anchor.transform.position==new Vector3(1,2,3)&&anchor.transform.rotation==Quaternion.identity,"Anchor immobility");
        spin.running=false;var before=child.transform.rotation;spin.Step(5);Check(child.transform.rotation==before,"Spin pause");
        UnityEngine.Object.DestroyImmediate(anchor);
        Debug.Log("PHOTOAR_ROTATION_OK");
    }
    [MenuItem("PhotoAR/Tests/4 Largest projected plane")]
    public static void TestPlacement() {
        var cameraObject=new GameObject("Test camera");var camera=cameraObject.AddComponent<Camera>();camera.transform.position=new Vector3(0,1.6f,-2);camera.transform.LookAt(new Vector3(0,0,1.5f));
        var provider=new SimulatedPlacement(camera);Check(provider.TryCandidate(out var pose,out var id,out var area)&&area>0,"Simulated plane candidate");
        var a0=Geometry.VisibleArea(camera,new System.Collections.Generic.List<Vector3>(provider.planes[0]));
        var a1=Geometry.VisibleArea(camera,new System.Collections.Generic.List<Vector3>(provider.planes[1]));
        Check(id==(a0>=a1?"sim-0":"sim-1"),"Largest projected plane selection");
        provider.planes.Clear();Check(!provider.TryCandidate(out pose,out id,out area),"Empty planes");
        UnityEngine.Object.DestroyImmediate(cameraObject);
        Debug.Log("PHOTOAR_PLACEMENT_OK");
    }
    public static void Validate(){Prepare();SelfTest();}
    [MenuItem("PhotoAR/Build Android APK")]
    public static void BuildAndroid() {
        Prepare();Directory.CreateDirectory("../builds");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/PhotoAR.unity"},locationPathName="../builds/PhotoAR.apk",target=BuildTarget.Android,options=BuildOptions.Development});
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Android build failed: "+report.summary.result);
    }
}
