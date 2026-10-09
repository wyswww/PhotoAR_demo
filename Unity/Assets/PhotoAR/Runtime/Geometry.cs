using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhotoAR {
public static class Geometry {
    public static float Area(IReadOnlyList<Vector2> p) {
        double sum=0;
        for(int i=0;i<p.Count;i++) { var a=p[i]; var b=p[(i+1)%p.Count]; sum+=(double)a.x*b.y-(double)b.x*a.y; }
        return (float)Math.Abs(sum*.5);
    }
    public static Vector2 Center(IReadOnlyList<Vector2> p) {
        double sum=0,x=0,y=0;
        for(int i=0;i<p.Count;i++) {var a=p[i];var b=p[(i+1)%p.Count];double c=(double)a.x*b.y-(double)b.x*a.y;sum+=c;x+=(a.x+b.x)*c;y+=(a.y+b.y)*c;}
        if(Math.Abs(sum)<1e-8) throw new ArgumentException("Degenerate plane boundary");
        return new Vector2((float)(x/(3*sum)),(float)(y/(3*sum)));
    }
    public static List<Vector2> ClipViewport(List<Vector2> p) {
        p=Clip(p,v=>v.x,0,true);p=Clip(p,v=>v.x,1,false);
        p=Clip(p,v=>v.y,0,true);return Clip(p,v=>v.y,1,false);
    }
    static List<Vector2> Clip(List<Vector2> p,Func<Vector2,float> axis,float bound,bool greater) {
        var r=new List<Vector2>(); if(p.Count==0)return r;
        var a=p[p.Count-1];bool insideA=greater?axis(a)>=bound:axis(a)<=bound;
        foreach(var b in p) { bool insideB=greater?axis(b)>=bound:axis(b)<=bound;
            if(insideA!=insideB)r.Add(Vector2.Lerp(a,b,(bound-axis(a))/(axis(b)-axis(a))));
            if(insideB)r.Add(b); a=b;insideA=insideB;
        } return r;
    }
    public static float VisibleArea(Camera camera,List<Vector3> world) {
        // Clip to the camera near plane before perspective division.
        var clipped=new List<Vector3>(); if(world.Count<3)return 0;
        var a=world[world.Count-1];float da=camera.transform.InverseTransformPoint(a).z-camera.nearClipPlane;
        foreach(var b in world) {float db=camera.transform.InverseTransformPoint(b).z-camera.nearClipPlane;
            if((da>=0)!=(db>=0))clipped.Add(Vector3.Lerp(a,b,da/(da-db)));
            if(db>=0)clipped.Add(b);a=b;da=db;
        }
        var projected=new List<Vector2>();foreach(var v in clipped){var q=camera.WorldToViewportPoint(v);projected.Add(new Vector2(q.x,q.y));}
        return Area(ClipViewport(projected));
    }
}
}
