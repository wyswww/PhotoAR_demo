using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;
namespace PhotoAR {
// Local development-build evidence only; no images or remote telemetry.
public class DeviceDiagnostics:MonoBehaviour {
    public ARPlacement ar;
    int frames;
    [Serializable] class State {public string arState,reason,loader,arcoreProbe,status;public int planes,cameraFrames,vertices;public bool cameraPermission,modelLoaded,cameraRunning,sessionRunning,inputRunning,meshVisible;public string selectedPlane;public float visibleArea;public Vector3 modelRotation;}
    IEnumerator Start(){
        var camera=FindFirstObjectByType<ARCameraManager>();if(camera!=null)camera.frameReceived+=OnFrame;
        while(true){
            var s=new State{arState=ARSession.state.ToString(),reason=ARSession.notTrackingReason.ToString(),
                planes=ar==null?0:ar.planes.trackables.count,modelLoaded=GameObject.Find("RotatingModel")!=null};
            s.cameraFrames=frames;s.cameraRunning=camera!=null&&camera.subsystem!=null&&camera.subsystem.running;
            var session=FindFirstObjectByType<ARSession>();s.sessionRunning=session!=null&&session.subsystem!=null&&session.subsystem.running;
            var input=FindFirstObjectByType<ARInputManager>();s.inputRunning=input!=null&&input.subsystem!=null&&input.subsystem.running;
            s.loader=XRGeneralSettings.Instance?.Manager?.activeLoader?.name;
            s.arcoreProbe=FindFirstObjectByType<AppController>()?.NativeProbe;
            s.status=FindFirstObjectByType<AppController>()?.TestStatus;
            var model=GameObject.Find("RotatingModel");
            if(model){s.modelRotation=model.transform.localEulerAngles;var renderer=model.GetComponentInChildren<MeshRenderer>();s.meshVisible=renderer!=null&&renderer.isVisible;s.vertices=model.GetComponentInChildren<MeshFilter>().sharedMesh.vertexCount;}
#if UNITY_ANDROID && !UNITY_EDITOR
            s.cameraPermission=UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera);
#endif
            if(ar!=null&&ar.TryCandidate(out var pose,out var id,out var area)){s.selectedPlane=id;s.visibleArea=area;}
            File.WriteAllText(Path.Combine(Application.persistentDataPath,"ar-diagnostics.json"),JsonUtility.ToJson(s,true));
            yield return new WaitForSecondsRealtime(2);
        }
    }
    void OnFrame(ARCameraFrameEventArgs args){frames++;}
}
}
