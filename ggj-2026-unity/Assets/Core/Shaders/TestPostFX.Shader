Shader "Custom/TestPostFX" 
{
  Properties 
  {
  }

  HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    // The Blit.hlsl file provides the vertex shader (Vert),
    // the input structure (Attributes), and the output structure (Varyings)
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    sampler2D _MainTex;
    sampler2D _SourceTex;
    float4 _MainTex_TexelSize;

    struct VertexData 
    {
      float4 vertex : POSITION;
      float2 uv : TEXCOORD0;
    };

    struct Interpolators 
    {
      float4 pos : SV_POSITION;
      float2 uv : TEXCOORD0;
    };
  ENDHLSL

  SubShader 
  {
    Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
    LOD 100
    
    Cull Off
    ZTest Always
    ZWrite Off

    // Color pass
    Pass 
    {
      HLSLPROGRAM
        #pragma vertex Vert
        #pragma fragment FragmentProgram

        float4 FragmentProgram (Varyings i) : SV_Target 
        {
          float4 c = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord);
          c.r = 0;
          return c;
        }
      ENDHLSL
    }

    // Final sample
    Pass 
    {
      HLSLPROGRAM
        #pragma vertex Vert
        #pragma fragment FragmentProgram

        float4 FragmentProgram (Varyings i) : SV_Target 
        {
          float4 c = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord);
          return c;
        }
      ENDHLSL
    }
  }
}