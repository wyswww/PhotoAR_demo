using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Android;
public static class PhotoARBranding {
    public static void Configure(){
        var icon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PhotoAR/Resources/AppIcon.png");if(!icon)throw new Exception("App icon missing");
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown,new[]{icon},IconKind.Any);
        var path="Assets/PhotoAR/Resources/IconBackground.asset";
        var background=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(!background){background=new Texture2D(2,2,TextureFormat.RGBA32,false);background.SetPixels(new[]{new Color(.025f,.055f,.14f),new Color(.025f,.055f,.14f),new Color(.025f,.055f,.14f),new Color(.025f,.055f,.14f)});background.Apply();AssetDatabase.CreateAsset(background,path);}
        foreach(var kind in new[]{AndroidPlatformIconKind.Adaptive,AndroidPlatformIconKind.Round,AndroidPlatformIconKind.Legacy}){
            var icons=PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android,kind);
            foreach(var slot in icons){if(kind==AndroidPlatformIconKind.Adaptive)slot.SetTextures(new[]{background,icon});else slot.SetTexture(icon);}
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android,kind,icons);
        }
    }
}
