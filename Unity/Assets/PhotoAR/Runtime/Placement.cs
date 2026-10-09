using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
namespace PhotoAR {
public interface IPlacementProvider {bool TryCandidate(out Pose pose,out string id,out float area);}
public class ARPlacement:MonoBehaviour,IPlacementProvider {
    public ARPlaneManager planes;public ARAnchorManager anchors;public Camera view;
    public ARPlane selected;
    public bool TryCandidate(out Pose pose,out string id,out float area) {
        pose=default;id=null;area=0;selected=null;
        foreach(var plane in planes.trackables) {
            if(plane.trackingState!=TrackingState.Tracking||plane.subsumedBy!=null||plane.boundary.Length<3)continue;
            var boundary=new List<Vector2>();var world=new List<Vector3>();
            foreach(var p in plane.boundary){boundary.Add(p);world.Add(plane.transform.TransformPoint(new Vector3(p.x,0,p.y)));}
            float visible=Geometry.VisibleArea(view,world);if(visible<=area)continue;
            Vector2 center;try {center=Geometry.Center(boundary);}catch {continue;}
            area=visible;selected=plane;id=plane.trackableId.ToString();
            pose=new Pose(plane.transform.TransformPoint(new Vector3(center.x,0,center.y)),plane.transform.rotation);
        }return selected!=null;
    }
    public async Awaitable<Transform> Anchor(Pose pose) {
        // Explicit failure; a plain Transform would not be a tracked AR anchor.
        if(anchors.subsystem==null)return null;
        var result=await anchors.TryAddAnchorAsync(pose);
        return result.status.IsSuccess()?result.value.transform:null;
    }
}
public class SimulatedPlacement:IPlacementProvider {
    readonly Camera camera;
    public readonly List<Vector3[]> planes=new List<Vector3[]> {
        new [] {new Vector3(-1,0,1),new Vector3(1,0,1),new Vector3(1,0,3),new Vector3(-1,0,3)},
        new [] {new Vector3(-.3f,.3f,.4f),new Vector3(.3f,.3f,.4f),new Vector3(.3f,.3f,1),new Vector3(-.3f,.3f,1)} };
    public SimulatedPlacement(Camera view){camera=view;}
    public bool TryCandidate(out Pose pose,out string id,out float area) {
        pose=default;id=null;area=0;
        for(int i=0;i<planes.Count;i++) {var points=new List<Vector3>(planes[i]);float a=Geometry.VisibleArea(camera,points);if(a<=area)continue;
            area=a;id="sim-"+i;var center=Vector3.zero;foreach(var p in points)center+=p;
            pose=new Pose(center/points.Count,Quaternion.identity);
        }return id!=null;
    }
}
}
