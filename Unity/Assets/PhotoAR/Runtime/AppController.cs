using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;
using UnityEngine.SpatialTracking;
namespace PhotoAR {
public class AppController:MonoBehaviour {
    public string endpoint="http://127.0.0.1:8090";
    public string photoPath="";
    string status="Choose a photo or run an independent test.";
    IPlacementProvider placement;ARPlacement ar;Camera view;
    GameObject model,anchor;byte[] photo;
    bool busy;bool placed;bool panelOpen=true;bool arUnsupported;Vector2 scroll;
    public string NativeProbe {get;private set;}
    const string Demo="v -1 -1 0 1 0 0\nv 1 -1 0 0 1 0\nv 0 1 0 0 0 1\nv 0 0 2 1 1 0\nf 1 3 2\nf 1 2 4\nf 2 3 4\nf 3 1 4\n";
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="EasyARSurface")return;if(FindFirstObjectByType<AppController>()==null)new GameObject("PhotoAR").AddComponent<AppController>();}
    void Start() {
        Application.targetFrameRate=60;gameObject.name="PhotoAR";
        endpoint=PlayerPrefs.GetString("PhotoAR.Endpoint",endpoint);
#if UNITY_ANDROID && !UNITY_EDITOR
        try {using(var gallery=new AndroidJavaClass("com.photoar.Gallery")) {
            var cached=gallery.CallStatic<string>("cachedPhotoPath");if(File.Exists(cached))photo=File.ReadAllBytes(cached);
        }}catch(Exception error){Debug.LogWarning(error.Message);}
#endif
        if(Camera.main)Destroy(Camera.main.gameObject);
        var originObject=new GameObject("XR Origin");var origin=originObject.AddComponent<XROrigin>();
        var offset=new GameObject("Camera Offset");offset.transform.SetParent(originObject.transform,false);origin.CameraFloorOffsetObject=offset;
        var cam=new GameObject("AR Camera");cam.tag="MainCamera";cam.transform.SetParent(offset.transform,false);
        view=cam.AddComponent<Camera>();view.nearClipPlane=.05f;origin.Camera=view;
        if(Application.isEditor) {
            view.transform.position=new Vector3(0,1.6f,-2);view.transform.LookAt(new Vector3(0,0,1.5f));
            placement=new SimulatedPlacement(view);
            foreach(var p in ((SimulatedPlacement)placement).planes) {
                var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Simulated plane";
                floor.transform.position=(p[0]+p[2])*.5f;floor.transform.localScale=new Vector3(p[1].x-p[0].x,.01f,p[2].z-p[1].z);
            }
            status="Editor simulation: AR sensors and anchors are simulated.";
        } else {
            var sessionObject=new GameObject("AR Session");
            sessionObject.AddComponent<ARInputManager>();
            sessionObject.AddComponent<ARSession>();
            cam.AddComponent<ARCameraManager>();cam.AddComponent<ARCameraBackground>();
            var driver=cam.AddComponent<TrackedPoseDriver>();driver.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice,TrackedPoseDriver.TrackedPose.Center);
            ar=originObject.AddComponent<ARPlacement>();ar.view=view;
            ar.planes=originObject.AddComponent<ARPlaneManager>();ar.planes.requestedDetectionMode=PlaneDetectionMode.Horizontal|PlaneDetectionMode.Vertical;
            ar.anchors=originObject.AddComponent<ARAnchorManager>();placement=ar;
            if(Debug.isDebugBuild)gameObject.AddComponent<DeviceDiagnostics>().ar=ar;
            StartCoroutine(Permissions());
#if UNITY_ANDROID
            try {
                using(var bridge=new AndroidJavaClass("com.photoar.ARCoreProbe"))NativeProbe=bridge.CallStatic<string>("inspect");
                if(NativeProbe.Contains("UnavailableDeviceNotCompatibleException"))OnARCompatibilityIssue();
            }catch(Exception error){NativeProbe=error.Message;}
#endif
        }
        var light=new GameObject("Light").AddComponent<Light>();light.type=LightType.Directional;
#if UNITY_ANDROID && !UNITY_EDITOR
        if(Debug.isDebugBuild) {
            using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using(var activity=player.GetStatic<AndroidJavaObject>("currentActivity"))
            using(var intent=activity.Call<AndroidJavaObject>("getIntent")) {
                if(intent.Call<string>("getStringExtra","photoar_test")=="generate")GeneratePhoto();
            }
        }
#endif
    }
    public void OnARCompatibilityIssue() {
        arUnsupported=true;status="ARCore cannot create a session on this device. Photo-to-3D, preview and rotation remain available.";
        var session=FindFirstObjectByType<ARSession>();if(session)session.enabled=false;
        var input=FindFirstObjectByType<ARInputManager>();if(input)input.enabled=false;
        if(ar){ar.planes.enabled=false;ar.anchors.enabled=false;}
        view.GetComponent<ARCameraManager>().enabled=false;view.GetComponent<ARCameraBackground>().enabled=false;
        view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=new Color(.06f,.09f,.15f);
    }
    IEnumerator Permissions() {
#if UNITY_ANDROID && !UNITY_EDITOR
        if(!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
#endif
        yield return null;
    }
    void OnGUI() {
        float scale=Mathf.Max(1,Screen.width/650f);GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
        if(GUI.Button(new Rect(10,10,180,34),panelOpen?"Hide controls":"Show controls"))panelOpen=!panelOpen;
        if(!panelOpen){GUI.Label(new Rect(10,50,Screen.width/scale-20,70),status);return;}
        GUILayout.BeginArea(new Rect(10,50,Screen.width/scale-20,Mathf.Min(380,Screen.height/scale-65)),GUI.skin.box);
        scroll=GUILayout.BeginScrollView(scroll);
        GUILayout.Label("PhotoAR | Photo -> TripoSR -> AR");GUILayout.Label(status);
        if(ar!=null)GUILayout.Label("AR state: "+ARSession.state+" | tracked planes: "+ar.planes.trackables.count);
        GUILayout.Label("ARCore is provided by Google LLC and governed by the Google Privacy Policy.");
        if(GUILayout.Button("Google Privacy Policy"))Application.OpenURL("https://policies.google.com/privacy");
        GUILayout.Label("Local server URL");endpoint=GUILayout.TextField(endpoint);
        if(GUILayout.Button("Save server URL",GUILayout.Height(36)))PlayerPrefs.SetString("PhotoAR.Endpoint",endpoint);
        GUI.enabled=!busy;
        if(Application.isEditor){GUILayout.Label("Photo path (Editor only)");photoPath=GUILayout.TextField(photoPath);}
        if(GUILayout.Button("1. Select photo",GUILayout.Height(40)))ChoosePhoto();
        if(photo!=null)GUILayout.Label("Photo ready: "+photo.Length+" bytes");
        if(GUILayout.Button("2. Generate model",GUILayout.Height(40))) {
            GeneratePhoto();
        }
        if(GUILayout.Button("Test model loading + rotation (no server)",GUILayout.Height(40)))LoadModel(Demo);
        if(GUILayout.Button("Test largest visible plane",GUILayout.Height(40))) {
            if(placement.TryCandidate(out var p,out var id,out var a))status=$"Plane {id}: viewport area {a:P1}; center {p.position}";
            else status="No tracked visible plane. Move the camera slowly.";
        }
        if(GUILayout.Button("3. Place at largest plane center",GUILayout.Height(40)))Place();
        if(GUILayout.Button("Pause / resume rotation",GUILayout.Height(40))&&model){var spin=model.GetComponent<Spin>();spin.running=!spin.running;}
        if(GUILayout.Button("Reset placement",GUILayout.Height(40)))ResetPlacement();
        GUI.enabled=true;
        GUILayout.EndScrollView();GUILayout.EndArea();
    }
    public void GeneratePhoto() {
        if(busy)return;
        if(photo==null){status="Select a photo first.";return;}
        busy=true;StartCoroutine(new GenerationClient{baseUrl=endpoint}.Generate(photo,
            s=>status="Generation: "+s,s=>{busy=false;LoadModel(s);},s=>{busy=false;status="Generation failed: "+s;}));
    }
    public string TestStatus=>status;
    void ChoosePhoto() {
        if(Application.isEditor){try {photo=File.ReadAllBytes(photoPath);status="Photo loaded.";}catch(Exception e){status=e.Message;}return;}
#if UNITY_ANDROID
        using(var bridge=new AndroidJavaClass("com.photoar.Gallery"))bridge.CallStatic("pick","PhotoAR");
#else
        status="Gallery is currently implemented for Android.";
#endif
    }
    public void OnPhotoSelected(string path){try {photo=File.ReadAllBytes(path);status="Photo selected.";}catch(Exception e){status=e.Message;}}
    public void OnPhotoError(string message){status=message;}
    void LoadModel(string obj) {
        try {
            var next=ObjLoader.Create(obj,.3f);ResetPlacement();if(model)Destroy(model);model=next;
            model.transform.position=view.transform.position+view.transform.forward*1.0f;
            status="Model loaded. Rotating preview; choose placement.";
            panelOpen=false;
        }catch(Exception e){status="Model load failed: "+e.Message;}
    }
    async void Place() {
        if(arUnsupported){status="ARCore reports this device incompatible. Use a compatible device for real AR placement.";return;}
        if(busy)return;if(!model){status="Load a model first.";return;}
        if(placed){status="Already anchored. Reset to choose a new plane.";return;}
        if(!placement.TryCandidate(out var pose,out var id,out var area)){status="No visible tracked plane.";return;}
        busy=true;
        try {
            Transform parent;
            if(ar){parent=await ar.Anchor(pose);if(parent==null){status="AR anchor creation failed. Please retry.";return;}anchor=parent.gameObject;}
            else {anchor=new GameObject("Simulated Anchor");parent=anchor.transform;parent.SetPositionAndRotation(pose.position,pose.rotation);}
            if(!model){Destroy(anchor);return;}
            model.transform.SetParent(parent,false);model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;
            placed=true;status=$"Placed at {id} center. Anchor fixed; model rotates.";
            panelOpen=false;
        }catch(Exception e){status="Placement failed: "+e.Message;}finally {busy=false;}
    }
    void ResetPlacement(){if(model)model.transform.SetParent(null,true);if(anchor)Destroy(anchor);anchor=null;placed=false;}
}
}
