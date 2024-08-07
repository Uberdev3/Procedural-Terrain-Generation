Shader "Unlit/TerrainShaderNew"
{
    Properties
    {
        _MainCol("Color", Color) = (1,1,1,1)
         _FlatColor("FlatColor", Color) = (1,1,1,1)
        _SideColor("SideColor", Color) = (1,1,1,1)
        _TopColor("TopColor", Color) = (1,1,1,1)
        _FlatColorIntensity("FlatColorIntensity", Float) = 0.5
        _MinTopColorHeight("MinTopColorHeight", Float) = 5

        
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #include "UnityCG.cginc"
        float4 _FlatColor;
            float4 _SideColor;
            struct MeshData
            {
                float4 vertex : POSITION;
                float3 normals : NORMAL;
            };

            struct Interpolators
            {
                float3 normal : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float vertexHeight : TEXCOORD1;
                //float4 lerpedColor : TEXCOORD1;
            };

            float4 _MainCol;
            float _TintStrength;
            float _FlatColorIntensity;
            float _MinTopColorHeight;
            float4 _TopColor;
            Interpolators vert (MeshData v)
            {
                Interpolators i;
                i.vertex = UnityObjectToClipPos(v.vertex);
                i.normal = mul((float3x3)unity_ObjectToWorld, v.normals);
                i.vertexHeight = v.vertex.y;
                
                return i;

            }

            fixed4 frag(Interpolators i) : SV_Target
            {
                float4 lerpedColor;
            float upDot = dot(i.normal, float3(0, 1, 0));
               if (upDot * _FlatColorIntensity > 0.7) {
                if (i.vertexHeight >= _MinTopColorHeight)
                {
                    lerpedColor = _TopColor;
                }
                else
                {
                    lerpedColor = _FlatColor;
                }
                // Surface is facing sufficiently upwards, make it grassy
            }
            else {
                lerpedColor = _SideColor;
                // Surface is sloped/vertical, make it rocky 
            }
            return lerpedColor;
            
            }
            ENDCG
        }
    }
}
