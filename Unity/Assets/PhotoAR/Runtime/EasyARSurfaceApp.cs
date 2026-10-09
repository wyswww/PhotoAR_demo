using System;
using System.IO;
using System.Collections;
using UnityEngine;
using easyar;
namespace PhotoAR {
// Independent surface tracking experiment. Does not claim plane detection.
public class EasyARSurfacePlacement : IPlacementProvider {
    readonly SurfaceTargetController target;
    public EasyARSurfacePlacement(SurfaceTargetController target){this.target=target;}
    public bool TryCandidate(out Pose pose,out string id,out float area){
        pose=default;id=null;area=0;
        if(!target||!target.IsTracked)return false;
        pose=new Pose(target.transform.position,target.transform.rotation);id="easyar-surface";return true;
    }
}
public class EasyARSurfaceApp : MonoBehaviour {
    public easyar.ARSession session; public SurfaceTargetController target;
    public TextAsset initialModel;
    string status="对准有纹理的表面，缓慢移动手机"; int frames,found,lost;
    GameObject model; byte[] photo; bool busy;
    [Serializable] class State {public string engine="EasyAR Surface Tracking",session,status;public int cameraFrames,found,lost,vertices;public bool tracked,modelVisible,rotationRunning,modelActive;public float rotationSpeed;public Vector3 cameraPosition,rotation;}
    void Awake(){gameObject.name="PhotoAR";session.InputFrameUpdate+=_=>frames++;target.TargetFound+=()=>found++;target.TargetLost+=()=>lost++;}
    IEnumerator Start(){
        Application.targetFrameRate=60;
#if UNITY_ANDROID && !UNITY_EDITOR
        if(!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
        try{using(var bridge=new AndroidJavaClass("com.photoar.Gallery")){var path=bridge.CallStatic<string>("cachedPhotoPath");if(File.Exists(path))photo=File.ReadAllBytes(path);}}catch(Exception){status="选择照片，创建你的 3D 模型";}
#endif
        Load(initialModel.text);
        mobileUI=new GameObject("PhotoAR mobile UI").AddComponent<PhotoARMobileUI>();mobileUI.Initialize(this);
        var diagnostics=FindFirstObjectByType<DiagnosticsController>();if(diagnostics){diagnostics.DeveloperModeSwitch=DiagnosticsController.DeveloperModeSwitchType.Custom;diagnostics.CustomDeveloperModeSwitch=()=>false;diagnostics.MessageOutput.SessionDump=DiagnosticsController.SessionDumpOutputMode.None;diagnostics.MessageOutput.Warning=diagnostics.MessageOutput.Error=diagnostics.MessageOutput.SenseError=diagnostics.MessageOutput.SessionError=DiagnosticsController.MessageOutputMode.Log;}

#if UNITY_ANDROID && !UNITY_EDITOR
        if(Debug.isDebugBuild){using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))using(var activity=player.GetStatic<AndroidJavaObject>("currentActivity"))using(var intent=activity.Call<AndroidJavaObject>("getIntent")){var test=intent.Call<string>("getStringExtra","photoar_test");if(test=="rotation")StartCoroutine(RotationTest());if(test=="library")mobileUI.OpenLibrary();if(test=="library-select")StartCoroutine(mobileUI.VerifyLibrarySelection());}}
#endif
        while(true){
            var camera=session.Assembly?.Camera;
            var s=new State{session=session.State.ToString(),status=status,cameraFrames=frames,found=found,lost=lost,tracked=target.IsTracked,cameraPosition=camera?camera.transform.position:Vector3.zero};
            if(model){s.vertices=model.GetComponentInChildren<MeshFilter>().sharedMesh.vertexCount;s.rotation=model.transform.localEulerAngles;s.modelVisible=model.GetComponentInChildren<MeshRenderer>().isVisible;s.rotationRunning=model.GetComponent<Spin>().running;s.rotationSpeed=model.GetComponent<Spin>().degreesPerSecond;s.modelActive=model.activeInHierarchy;}
            File.WriteAllText(Path.Combine(Application.persistentDataPath,"easyar-diagnostics.json"),JsonUtility.ToJson(s,true));
            yield return new WaitForSecondsRealtime(2);
        }
    }
    void Load(string obj){try{var next=ObjLoader.Create(obj,.3f);if(model)Destroy(model);model=next;model.name="SurfaceRotatingModel";model.transform.SetParent(target.transform,false);model.GetComponent<Spin>().degreesPerSecond=60;SetRotation(true);status="模型已就绪，可重新放置或从模型库切换";}catch(Exception e){status=e.Message;}}
    public void SetRotation(bool enabled){if(model)model.GetComponent<Spin>().running=enabled;}
    IEnumerator RotationTest(){
        // Verify the same explicit controls used by the UI on the real device.
        while(!target.IsTracked)yield return null;
        SetRotation(true);var start=model.transform.localRotation;yield return new WaitForSeconds(1);
        bool advances=Quaternion.Angle(start,model.transform.localRotation)>25;
        SetRotation(false);var paused=model.transform.localRotation;yield return new WaitForSeconds(1);
        bool stops=Quaternion.Angle(paused,model.transform.localRotation)<.1f;
        SetRotation(true);var resumed=model.transform.localRotation;yield return new WaitForSeconds(1);
        bool restarts=Quaternion.Angle(resumed,model.transform.localRotation)>25;
        File.WriteAllText(Path.Combine(Application.persistentDataPath,"rotation-test.txt"),$"Start: {advances}; Stop: {stops}; Resume: {restarts}; FinalRunning: {model.GetComponent<Spin>().running}");
    }
    public string CurrentModelName {get;private set;}="测试模型";
    public string Status=>status;
    public bool Busy=>busy;
    public bool HasPhoto=>photo!=null;
    public byte[] PhotoBytes=>photo;
    public bool IsTracked=>target&&target.IsTracked;
    public bool IsRotating=>model&&model.GetComponent<Spin>().running;
    PhotoARMobileUI mobileUI;
    public void Realign(){target.AlignTo(new Vector2(.5f,.5f));status="已重新定位到画面中央";}
    public void ChoosePhoto(){
#if UNITY_ANDROID && !UNITY_EDITOR
        using(var gallery=new AndroidJavaClass("com.photoar.Gallery"))gallery.CallStatic("pick","PhotoAR");
#endif
    }
    public void LoadExample(){Load(initialModel.text);CurrentModelName="测试模型";}
    public void RunRotationTest(){StartCoroutine(RotationTest());status="正在检查自转，请保持手机稳定";}
    public void SelectSavedModel(SavedModel item,Action done,Action<string> failed){
        if(busy)return;busy=true;status="正在加载已保存的模型…";
        StartCoroutine(new ModelLibraryClient().Load(item,text=>{busy=false;Load(text);CurrentModelName=item.name;Realign();done();},error=>{busy=false;status=error;failed(error);}));
    }
    public void Generate(){if(busy)return;if(photo==null){status="请先选择照片";return;}busy=true;StartCoroutine(new GenerationClient{baseUrl="http://127.0.0.1:8090"}.Generate(photo,s=>status=s=="queued"?"已加入生成队列…":"正在生成 3D 模型，请稍候…",s=>{busy=false;Load(s);CurrentModelName="新生成模型";Realign();},s=>{busy=false;status="生成失败，请检查电脑连接后重试";Debug.LogWarning(s);}));}
    public void OnPhotoSelected(string path){try{photo=File.ReadAllBytes(path);status="照片已选好，点击生成并放置";mobileUI?.RefreshPhoto();}catch(Exception e){status=e.Message;}}
    public void OnPhotoError(string message){status=message;}
}
}
