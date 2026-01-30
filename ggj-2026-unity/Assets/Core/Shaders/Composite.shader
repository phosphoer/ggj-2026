Shader "Custom/Composite"
{
  Properties
  {
    _MainTex ("Texture", 2D) = "white" {}
    _UITex ("UI Texture", 2D) = "clear" {}
  }
  SubShader
  {
    Cull Off
    ZTest Always
    ZWrite Off

    Pass
    {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag

      #include "UnityCG.cginc"

      struct appdata
      {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
      };

      struct v2f
      {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
      };

      sampler2D _MainTex;
      sampler2D _UITex;

      v2f vert(appdata v)
      {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
        return o;
      }

      fixed4 frag(v2f i) : SV_Target
      {
        float4 mainTex = tex2D(_MainTex, i.uv);
        float4 uiTex = tex2D(_UITex, i.uv);
        float4 diffuse = mainTex * (1 - uiTex.a) + uiTex;
        return diffuse;
      }
      ENDCG
    }
  }

  Fallback "VertexLit"
}