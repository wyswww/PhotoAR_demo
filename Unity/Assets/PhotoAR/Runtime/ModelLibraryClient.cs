using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
namespace PhotoAR {
[Serializable] public class SavedModel {public string id,name,model_url,thumbnail_url;public double created;public long bytes;}
[Serializable] public class SavedModels {public SavedModel[] models;}
public class ModelLibraryClient {
    public string baseUrl="http://127.0.0.1:8090";
    public IEnumerator List(Action<SavedModel[]> complete,Action<string> fail){
        using(var r=UnityWebRequest.Get(baseUrl+"/models")){r.timeout=15;yield return r.SendWebRequest();
            if(r.result!=UnityWebRequest.Result.Success){fail("无法连接模型库，请检查电脑服务和 USB 连接。");yield break;}
            SavedModels data=null;try{data=JsonUtility.FromJson<SavedModels>(r.downloadHandler.text);}catch(Exception){}
            if(data==null){fail("模型库返回的数据无法读取。");yield break;}complete(data.models??Array.Empty<SavedModel>());
        }
    }
    public IEnumerator Load(SavedModel item,Action<string> complete,Action<string> fail){
        using(var r=UnityWebRequest.Get(baseUrl+item.model_url)){r.timeout=60;yield return r.SendWebRequest();if(r.result!=UnityWebRequest.Result.Success){fail("模型加载失败，请检查电脑连接后重试。");yield break;}complete(r.downloadHandler.text);}
    }
    public IEnumerator Thumbnail(SavedModel item,Action<Texture2D> complete){
        using(var r=UnityWebRequest.Get(baseUrl+item.thumbnail_url)){r.timeout=15;yield return r.SendWebRequest();if(r.result==UnityWebRequest.Result.Success){var texture=new Texture2D(2,2);if(texture.LoadImage(r.downloadHandler.data))complete(texture);else UnityEngine.Object.Destroy(texture);}}
    }
}
}
