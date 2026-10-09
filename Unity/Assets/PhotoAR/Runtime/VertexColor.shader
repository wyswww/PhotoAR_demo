Shader "PhotoAR/VertexColor" {
SubShader { Tags { "RenderType"="Opaque" } Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct Input { float4 vertex:POSITION; float4 color:COLOR; float3 normal:NORMAL; };
struct Output { float4 pos:SV_POSITION; float4 color:COLOR; float3 normal:TEXCOORD0; };
Output vert(Input v) { Output o;o.pos=UnityObjectToClipPos(v.vertex);o.color=v.color;o.normal=UnityObjectToWorldNormal(v.normal);return o; }
fixed4 frag(Output i):SV_Target { float light=.45+.55*abs(dot(normalize(i.normal),normalize(float3(.3,.8,.5))));return fixed4(i.color.rgb*light,1); }
ENDCG
} } }
