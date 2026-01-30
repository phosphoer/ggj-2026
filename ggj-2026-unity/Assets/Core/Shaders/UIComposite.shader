Shader "Custom/UIComposite" 
{
  Properties 
  {
    _UITex ("UI Tex", 2D) = "white"
  }

  HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    // The Blit.hlsl file provides the vertex shader (Vert),
    // the input structure (Attributes), and the output structure (Varyings)
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    TEXTURE2D_X(_UITex);
  ENDHLSL

  SubShader 
  {
    Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
    LOD 100

    Cull Off
    ZTest Always
    ZWrite Off

    Pass 
    {
      HLSLPROGRAM
        #pragma vertex Vert
        #pragma fragment FragmentProgram

        float4 FragmentProgram (Varyings i) : SV_Target 
        {
          float4 mainTex = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord);
          float4 uiTex = SAMPLE_TEXTURE2D(_UITex, sampler_LinearClamp, i.texcoord);
          // uiTex.a = pow(uiTex.a, 1.0/2.2);
          float4 diffuse = mainTex * (1 - uiTex.a) + uiTex;
          return diffuse;
        }
      ENDHLSL
    }
  }
}