using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace PhotoAR {
// Presentation only. Reconstruction and tracking stay in separate components.
public class PhotoARMobileUI:MonoBehaviour {
    EasyARSurfaceApp app;Font font;Sprite rounded;RectTransform safe,panel,modal;
    Text status,tracking,rotation,photoLabel,current,libraryStatus;RawImage preview;
    Button generate,select;GameObject details;bool expanded=true;int libraryVersion;
    readonly List<Texture2D> thumbnails=new List<Texture2D>();Texture2D photoTexture;RectTransform libraryContent;SavedModel firstSaved;
    readonly Color ink=new Color(.055f,.075f,.13f,.96f),card=new Color(.1f,.13f,.21f,.96f),teal=new Color(.23f,.91f,.79f),muted=new Color(.61f,.68f,.78f);
    public void Initialize(EasyARSurfaceApp controller){
        app=controller;font=Resources.Load<Font>("UIChinese");if(!font)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");rounded=MakeRounded();
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=0;
        gameObject.AddComponent<GraphicRaycaster>();
        if(!EventSystem.current){var es=new GameObject("PhotoAR UI Input");es.AddComponent<EventSystem>();es.AddComponent<StandaloneInputModule>();}
        safe=Rect("Safe Area",transform);safe.anchorMin=new Vector2(Screen.safeArea.xMin/Screen.width,Screen.safeArea.yMin/Screen.height);safe.anchorMax=new Vector2(Screen.safeArea.xMax/Screen.width,Screen.safeArea.yMax/Screen.height);safe.offsetMin=safe.offsetMax=Vector2.zero;
        var header=Top("Brand",safe,32,110,1016,106);Background(header,ink);
        var brandIcon=Rect("App mark",header);Set(brandIcon,20,18,68,68);var mark=brandIcon.gameObject.AddComponent<RawImage>();mark.texture=Resources.Load<Texture2D>("AppIcon");mark.raycastTarget=false;
        Label(header,"PhotoAR",110,12,420,48,38,Color.white);Label(header,"让照片，走进现实",110,61,460,32,23,muted);
        tracking=Label(header,"正在寻找表面",642,32,350,44,25,teal,TextAnchor.MiddleRight);
        // Thin center reticle leaves the camera scene unobstructed.
        var reticle=Rect("Placement reticle",safe);reticle.anchorMin=reticle.anchorMax=new Vector2(.5f,.56f);reticle.sizeDelta=new Vector2(70,70);reticle.anchoredPosition=Vector2.zero;
        foreach(var p in new[]{new Vector2(0,0),new Vector2(54,0),new Vector2(0,54),new Vector2(54,54)}){var h=Rect("corner",reticle);Set(h,p.x,p.y,16,3);h.gameObject.AddComponent<Image>().color=new Color(1,1,1,.8f);var v=Rect("corner",reticle);Set(v,p.x+(p.x>0?13:0),p.y,3,16);v.gameObject.AddComponent<Image>().color=new Color(1,1,1,.8f);}
        panel=Bottom("Control card",safe,32,30,1016,600);Background(panel,ink);
        Label(panel,"AR 创作台",30,24,520,48,32,Color.white);
        var toggle=Action(panel,"收起",820,25,164,50,()=>TogglePanel(),card,22);toggle.name="Panel toggle";
        current=Label(panel,"当前模型",30,85,950,40,25,muted);
        rotation=Label(panel,"自转开启",30,131,350,36,24,teal);
        Action(panel,"模型库",392,125,275,65,()=>OpenLibrary(),card,25);
        Action(panel,"重新放置",692,125,292,65,()=>app.Realign(),card,25);
        Action(panel,"开始自转",30,212,465,68,()=>app.SetRotation(true),card,25);
        Action(panel,"停止自转",518,212,466,68,()=>app.SetRotation(false),card,25);
        details=new GameObject("Generation controls",typeof(RectTransform));details.transform.SetParent(panel,false);var d=(RectTransform)details.transform;d.anchorMin=Vector2.zero;d.anchorMax=Vector2.one;d.offsetMin=d.offsetMax=Vector2.zero;
        var photoCard=Rect("Selected photo",d);Set(photoCard,30,312,954,94);Background(photoCard,card);
        var thumb=Rect("Photo preview",photoCard);Set(thumb,12,12,70,70);preview=thumb.gameObject.AddComponent<RawImage>();preview.color=Color.white;preview.raycastTarget=false;
        photoLabel=Label(photoCard,"选择一张物体照片",102,9,800,42,26,Color.white);Label(photoCard,"主体清晰、背景简洁，生成效果更好",102,53,820,30,21,muted);
        select=Action(d,"选择照片",30,428,300,80,()=>app.ChoosePhoto(),card,27);
        generate=Action(d,"生成并放置",354,428,630,80,()=>app.Generate(),teal,29,true);
        status=Label(d,"",30,524,930,42,23,muted);
        var footer=Bottom("Footer",safe,32,0,1016,27);Label(footer,"表面跟踪体验",0,0,600,26,19,new Color(.9f,.93f,1,.85f));Action(footer,"独立测试",790,0,225,27,()=>OpenTests(),Color.clear,19);
        RefreshPhoto();
    }
    void Update(){if(!app)return;tracking.text=app.IsTracked?"●  表面已跟踪":"○  缓慢移动手机";tracking.color=app.IsTracked?teal:muted;rotation.text=app.IsRotating?"●  自转开启":"○  自转已停止";rotation.color=app.IsRotating?teal:muted;current.text="当前 · "+app.CurrentModelName;status.text=app.Status;generate.interactable=!app.Busy&&app.HasPhoto;select.interactable=!app.Busy;generate.GetComponentInChildren<Text>().text=app.Busy?"正在处理…":"生成并放置";}
    void TogglePanel(){expanded=!expanded;details.SetActive(expanded);panel.sizeDelta=new Vector2(1016,expanded?600:305);panel.Find("Panel toggle").GetComponentInChildren<Text>().text=expanded?"收起":"展开";}
    public void RefreshPhoto(){if(photoTexture)Destroy(photoTexture);if(app.PhotoBytes!=null){photoTexture=new Texture2D(2,2);photoTexture.LoadImage(app.PhotoBytes);preview.texture=photoTexture;photoLabel.text="照片已就绪";}else{preview.texture=Resources.Load<Texture2D>("AppIcon");photoLabel.text="选择一张物体照片";}}
    RectTransform NewModal(string title,string subtitle){CloseModal();modal=Rect("Modal",safe);modal.anchorMin=Vector2.zero;modal.anchorMax=Vector2.one;modal.offsetMin=modal.offsetMax=Vector2.zero;modal.gameObject.AddComponent<Image>().color=new Color(.015f,.025f,.05f,.84f);
        var sheet=Rect("Sheet",modal);sheet.anchorMin=new Vector2(0,.06f);sheet.anchorMax=new Vector2(1,.88f);sheet.offsetMin=new Vector2(32,0);sheet.offsetMax=new Vector2(-32,0);Background(sheet,ink);
        Label(sheet,title,30,30,710,56,38,Color.white);Label(sheet,subtitle,30,96,950,72,23,muted);Action(sheet,"关闭",810,33,175,54,()=>CloseModal(),card,23);return sheet;}
    public void OpenLibrary(){var sheet=NewModal("我的模型","选择已生成的模型，直接放置到当前场景");Action(sheet,"刷新列表",30,178,280,62,()=>OpenLibrary(),card,24);libraryStatus=Label(sheet,"正在读取电脑上的模型…",334,183,620,55,23,muted);
        var viewport=Rect("Model list",sheet);viewport.anchorMin=new Vector2(0,0);viewport.anchorMax=new Vector2(1,1);viewport.offsetMin=new Vector2(28,28);viewport.offsetMax=new Vector2(-28,-265);viewport.gameObject.AddComponent<RectMask2D>();
        var content=Rect("Content",viewport);libraryContent=content;firstSaved=null;content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;
        var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
        int version=libraryVersion;StartCoroutine(new ModelLibraryClient().List(items=>{if(version!=libraryVersion)return;firstSaved=items.Length>0?items[0]:null;libraryStatus.text=items.Length==0?"还没有模型，请先生成":$"共 {items.Length} 个模型";content.sizeDelta=new Vector2(0,items.Length*170);
            for(int i=0;i<items.Length;i++){var item=items[i];var row=Action(content,"",0,i*170,954,150,()=>{if(!app.Busy){libraryStatus.text="正在加载模型…";app.SelectSavedModel(item,()=>CloseModal(),error=>{if(libraryStatus)libraryStatus.text=error;});}},card,24);
                var r=(RectTransform)row.transform;var img=Rect("Thumbnail",r);Set(img,16,16,118,118);var raw=img.gameObject.AddComponent<RawImage>();raw.texture=Resources.Load<Texture2D>("AppIcon");raw.raycastTarget=false;
                Label(r,item.name,160,24,585,47,28,Color.white);Label(r,$"{item.bytes/1024f:0} KB  ·  已生成",160,83,580,35,22,muted);Label(r,"放置 →",764,52,173,45,26,teal);
                StartCoroutine(new ModelLibraryClient().Thumbnail(item,tex=>{if(version!=libraryVersion||!raw){Destroy(tex);return;}thumbnails.Add(tex);raw.texture=tex;}));
            }
        },error=>{if(version==libraryVersion)libraryStatus.text=error;}));
    }
    public IEnumerator VerifyLibrarySelection(){
        OpenLibrary();float deadline=Time.realtimeSinceStartup+20;
        while(firstSaved==null&&Time.realtimeSinceStartup<deadline)yield return null;
        if(firstSaved==null){System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath,"library-test.txt"),"FAIL: no saved model list");yield break;}
        var expected=firstSaved;int count=libraryContent.childCount;
        libraryContent.GetChild(0).GetComponent<Button>().onClick.Invoke();deadline=Time.realtimeSinceStartup+65;
        while(app.Busy&&Time.realtimeSinceStartup<deadline)yield return null;
        bool passed=!app.Busy&&app.CurrentModelName==expected.name&&app.IsRotating;
        System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath,"library-test.txt"),$"Selection via UI button: {passed}; SavedModels: {count}; Rotation: {app.IsRotating}");
    }
    void OpenTests(){var sheet=NewModal("独立测试","检查功能时使用，不影响电脑上保存的模型");Action(sheet,"检查开始、停止、恢复自转",30,202,954,85,()=>{app.RunRotationTest();CloseModal();},card,27);Action(sheet,"重新加载测试模型",30,316,954,85,()=>{app.LoadExample();CloseModal();},card,27);Action(sheet,"检查模型库连接",30,430,954,85,()=>OpenLibrary(),card,27);Label(sheet,"跟踪丢失时，模型暂时隐藏；对准有纹理的表面重新定位。",30,550,940,110,25,muted);}
    void CloseModal(){libraryVersion++;if(modal)Destroy(modal.gameObject);modal=null;foreach(var t in thumbnails)if(t)Destroy(t);thumbnails.Clear();}
    void OnDestroy(){CloseModal();if(photoTexture)Destroy(photoTexture);if(rounded){Destroy(rounded.texture);Destroy(rounded);}}
    RectTransform Rect(string name,Transform parent){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return (RectTransform)go.transform;}
    void Set(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    RectTransform Top(string n,Transform p,float x,float y,float w,float h){var r=Rect(n,p);Set(r,x,y,w,h);return r;}
    RectTransform Bottom(string n,Transform p,float x,float y,float w,float h){var r=Rect(n,p);r.anchorMin=r.anchorMax=Vector2.zero;r.pivot=Vector2.zero;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;}
    void Background(RectTransform r,Color color){var i=r.gameObject.AddComponent<Image>();i.sprite=rounded;i.type=Image.Type.Sliced;i.color=color;}
    Text Label(RectTransform parent,string value,float x,float y,float w,float h,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft){var r=Rect("Text",parent);Set(r,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=color;t.alignment=align;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;return t;}
    Button Action(RectTransform p,string label,float x,float y,float w,float h,Action click,Color color,int size,bool dark=false){var r=Rect(label,p);Set(r,x,y,w,h);Background(r,color);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();var colors=b.colors;colors.highlightedColor=new Color(.85f,.95f,1);colors.pressedColor=new Color(.7f,.85f,.9f);colors.disabledColor=new Color(.45f,.5f,.6f,.8f);b.colors=colors;b.onClick.AddListener(()=>click());Label(r,label,12,0,w-24,h,size,dark?ink:Color.white,TextAnchor.MiddleCenter);return b;}
    Sprite MakeRounded(){const int size=64;var tex=new Texture2D(size,size,TextureFormat.RGBA32,false);var pixels=new Color[size*size];for(int y=0;y<size;y++)for(int x=0;x<size;x++){float dx=Mathf.Max(16-x,x-47),dy=Mathf.Max(16-y,y-47);float distance=Mathf.Sqrt(Mathf.Pow(Mathf.Max(dx,0),2)+Mathf.Pow(Mathf.Max(dy,0),2));pixels[y*size+x]=new Color(1,1,1,Mathf.Clamp01(16-distance));}tex.SetPixels(pixels);tex.Apply();return Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(18,18,18,18));}
}
}
