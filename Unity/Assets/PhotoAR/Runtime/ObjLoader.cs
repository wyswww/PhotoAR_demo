using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;
namespace PhotoAR {
public class MeshData {
    public List<Vector3> vertices;public List<Color> colors;public List<int> triangles;
}
public static class ObjLoader {
    static float F(string s)=>float.Parse(s,CultureInfo.InvariantCulture);
    public static MeshData ParseData(string text) {
        var v=new List<Vector3>();var c=new List<Color>();var triangles=new List<int>();
        foreach(var line in text.Split('\n')) {
            var t=line.Trim().Split((char[])null,StringSplitOptions.RemoveEmptyEntries);
            if(t.Length==0||t[0].StartsWith("#"))continue;
            if(t[0]=="v") {
                if(t.Length<4)throw new FormatException("Invalid vertex");
                // TripoSR Z-up right-handed -> Unity Y-up left-handed.
                v.Add(new Vector3(F(t[1]),F(t[3]),F(t[2])));
                c.Add(t.Length>=7?new Color(F(t[4]),F(t[5]),F(t[6]),1):Color.gray);
                if(v.Count>1000000)throw new FormatException("Mesh exceeds vertex budget");
            } else if(t[0]=="f") {
                var face=new List<int>();
                for(int k=1;k<t.Length;k++) {int n=int.Parse(t[k].Split('/')[0],CultureInfo.InvariantCulture);int index=n<0?v.Count+n:n-1;
                    if(index<0||index>=v.Count)throw new FormatException("Invalid face index");face.Add(index);}
                for(int k=1;k<face.Count-1;k++){triangles.Add(face[0]);triangles.Add(face[k+1]);triangles.Add(face[k]);}
                if(triangles.Count>6000000)throw new FormatException("Mesh exceeds triangle budget");
            }
        }
        if(v.Count<3||triangles.Count<3)throw new FormatException("Empty mesh");
        return new MeshData{vertices=v,colors=c,triangles=triangles};
    }
    public static Mesh Parse(string text) {
        var data=ParseData(text);
        var mesh=new Mesh {indexFormat=IndexFormat.UInt32};mesh.SetVertices(data.vertices);mesh.SetColors(data.colors);mesh.SetTriangles(data.triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    public static GameObject Create(string text,float meters) {
        var mesh=Parse(text);var b=mesh.bounds;
        float size=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));if(size<1e-6)throw new FormatException("Zero size mesh");
        var root=new GameObject("RotatingModel");var child=new GameObject("Mesh");child.transform.SetParent(root.transform,false);
        child.AddComponent<MeshFilter>().sharedMesh=mesh;
        var shader=Shader.Find("PhotoAR/VertexColor");if(!shader)throw new InvalidOperationException("Vertex color shader missing");
        child.AddComponent<MeshRenderer>().sharedMaterial=new Material(shader);
        child.transform.localScale=Vector3.one*(meters/size);
        child.transform.localPosition=-new Vector3(b.center.x,b.min.y,b.center.z)*(meters/size);
        root.AddComponent<Spin>();return root;
    }
}
public class Spin:MonoBehaviour {
    public float degreesPerSecond=30;public bool running=true;
    public void Step(float delta) {if(running)transform.Rotate(Vector3.up,degreesPerSecond*delta,Space.Self);}
    void Update()=>Step(Time.deltaTime);
}
}
