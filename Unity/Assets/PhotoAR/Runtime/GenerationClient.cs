using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
namespace PhotoAR {
[Serializable] public class Job {public string id,state,model_url,error;}
public class GenerationClient {
    public string baseUrl="http://127.0.0.1:8090";
    public IEnumerator Generate(byte[] photo,Action<string> progress,Action<string> complete,Action<string> fail) {
        var form=new WWWForm();form.AddBinaryData("photo",photo,"photo.png","image/png");
        Job job;
        using(var req=UnityWebRequest.Post(baseUrl.TrimEnd('/')+"/jobs",form)) {
            req.timeout=30;yield return req.SendWebRequest();
            if(req.result!=UnityWebRequest.Result.Success){fail(req.error+" "+req.downloadHandler.text);yield break;}
            job=JsonUtility.FromJson<Job>(req.downloadHandler.text);
        }
        float deadline=Time.realtimeSinceStartup+660;
        while(job.state!="succeeded") {
            if(job.state=="failed"){fail(job.error);yield break;}
            if(Time.realtimeSinceStartup>deadline){fail("Generation timed out");yield break;}
            progress(job.state);yield return new WaitForSecondsRealtime(1);
            using(var req=UnityWebRequest.Get(baseUrl.TrimEnd('/')+"/jobs/"+job.id)) {
                req.timeout=15;yield return req.SendWebRequest();
                if(req.result!=UnityWebRequest.Result.Success){fail(req.error);yield break;}
                job=JsonUtility.FromJson<Job>(req.downloadHandler.text);
            }
        }
        using(var req=UnityWebRequest.Get(baseUrl.TrimEnd('/')+job.model_url)) {
            req.timeout=60;yield return req.SendWebRequest();
            if(req.result!=UnityWebRequest.Result.Success){fail(req.error);yield break;}
            complete(req.downloadHandler.text);
        }
    }
}
}
