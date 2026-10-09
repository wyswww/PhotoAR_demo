using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using PhotoAR;

[InitializeOnLoad]
public static class RuntimeSmokeTest {
    static double started;
    static int phase;
    static AppController app;
    static GameObject model,anchor;
    static Quaternion rotation;
    static Vector3 anchorPosition;
    static bool downloaded;
    static RuntimeSmokeTest(){EditorApplication.update+=Tick;}
    public static void Begin(){
        ProjectTools.Prepare();SessionState.SetBool("PhotoAR.Smoke",true);EditorApplication.isPlaying=true;
    }
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Invoke(string method,params object[] arguments)=>typeof(AppController).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(app,arguments);
    static void Tick(){
        if(!SessionState.GetBool("PhotoAR.Smoke",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        try {
            if(phase==0){
                if(Time.frameCount<3)return;
                app=UnityEngine.Object.FindFirstObjectByType<AppController>();Check(app!=null,"App bootstrap failed");
                Check(GameObject.Find("AR Camera")!=null,"Simulation camera missing");
                Invoke("LoadModel",File.ReadAllText(Environment.GetEnvironmentVariable("PHOTOAR_TEST_MODEL")));
                model=GameObject.Find("RotatingModel");Check(model!=null,"Real mesh load failed");
                Check(!UnityEditor.ShaderUtil.ShaderHasError(model.GetComponentInChildren<MeshRenderer>().sharedMaterial.shader),"Shader compilation failed");
                rotation=model.transform.rotation;started=EditorApplication.timeSinceStartup;phase=1;
            }else if(phase==1&&EditorApplication.timeSinceStartup-started>1){
                Check(Quaternion.Angle(rotation,model.transform.rotation)>10,"Model did not rotate over real frames");
                Invoke("Place");anchor=GameObject.Find("Simulated Anchor");Check(anchor!=null,"Simulated placement failed");
                Check(model.transform.parent==anchor.transform&&model.transform.localPosition.sqrMagnitude<1e-6,"Model not centered at anchor");
                anchorPosition=anchor.transform.position;started=EditorApplication.timeSinceStartup;phase=2;
            }else if(phase==2&&EditorApplication.timeSinceStartup-started>1){
                Check((anchor.transform.position-anchorPosition).sqrMagnitude<1e-6&&Quaternion.Angle(anchor.transform.rotation,Quaternion.identity)<.01,"Anchor moved with rotation");
                model.GetComponent<Spin>().running=false;rotation=model.transform.rotation;started=EditorApplication.timeSinceStartup;phase=3;
            }else if(phase==3&&EditorApplication.timeSinceStartup-started>.5){
                Check(Quaternion.Angle(rotation,model.transform.rotation)<.01,"Rotation pause failed");
                app.StartCoroutine(new GenerationClient{baseUrl="http://127.0.0.1:8091"}.Generate(
                    File.ReadAllBytes(Environment.GetEnvironmentVariable("PHOTOAR_TEST_PHOTO")),s=>{},
                    text=>{Invoke("LoadModel",text);downloaded=true;},error=>Finish(false,"Unity HTTP generation failed: "+error)));
                started=EditorApplication.timeSinceStartup;phase=4;
            }else if(phase==4){
                if(downloaded){Check(GameObject.Find("RotatingModel")!=null,"Downloaded model not loaded");Finish(true,"Real mesh, shader validation, frame rotation, simulated center placement, fixed anchor, pause, Unity HTTP upload/poll/download/load");}
                else if(EditorApplication.timeSinceStartup-started>45)Finish(false,"Unity HTTP timeout");
            }
        }catch(Exception e){Finish(false,e.ToString());}
    }
    static void Finish(bool success,string message){
        SessionState.SetBool("PhotoAR.Smoke",false);
        string reports=Path.GetFullPath(Path.Combine(Application.dataPath,"../../reports"));Directory.CreateDirectory(reports);
        File.WriteAllText(Path.Combine(reports,"runtime-smoke.txt"),(success?"PASS ":"FAIL ")+message);
        Debug.Log((success?"PHOTOAR_RUNTIME_OK ":"PHOTOAR_RUNTIME_FAILED ")+message);
        EditorApplication.Exit(success?0:1);
    }
}
