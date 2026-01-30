Shader "Custom/SimplePostFX" 
{
  Properties 
  {
    _BlurTex ("Blur Tex", 2D) = "white"
    _BloomTex ("Bloom Tex", 2D) = "white"
    _BloomParams ("Bloom Params", Vector) = (1, 1, 1, 0)
    _BlurParams ("Blur Params", Vector) = (1, 0, 0, 1)
    _ColorParams ("Color Params", Vector) = (1, 1, 0, 0)
    _ColorBrightness ("Color Brightness", float) = 1
    _ChannelMixerRed ("Channel Mixer Red", Vector) = (1, 0, 0, 0)
    _ChannelMixerGreen ("Channel Mixer Green", Vector) = (0, 1, 0, 0)
    _ChannelMixerBlue ("Channel Mixer Blue", Vector) = (0, 0, 1, 0)
  }

  HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    // The Blit.hlsl file provides the vertex shader (Vert),
    // the input structure (Attributes), and the output structure (Varyings)
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    TEXTURE2D_X(_BlurTex);
    TEXTURE2D_X(_BloomTex);
    float4 _BloomTex_TexelSize;

    // R = Filter Size, G = Threshold, B = Intensity, A = Soft Threshold
    float4 _BloomParams;

    // R = Filter Size, G = Unused, B = Unused, A = Opacity
    float4 _BlurParams;

    // R = Saturation, G = Contrast, B = Temperature, A = Tint
    float4 _ColorParams;
    float _ColorBrightness;

    // Channel mixers
    float4 _ChannelMixerRed;
    float4 _ChannelMixerGreen;
    float4 _ChannelMixerBlue;

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

    float3 Prefilter (float3 c)
    {
      float brightness = max(c.r, max(c.g, c.b));
      float knee = _BloomParams.g * _BloomParams.a;
      float soft = brightness - _BloomParams.g + knee;
      soft = clamp(soft, 0, 2 * knee);
      soft = soft * soft / (4 * knee + 0.00001);
      float contribution = max(soft, brightness - _BloomParams.g);
      contribution /= max(brightness, 0.00001);
      return c * contribution;
    }

    float4 Sample (float2 uv) 
    {
			return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
		}

    float4 SampleBloom (float2 uv) 
    {
			return SAMPLE_TEXTURE2D(_BloomTex, sampler_LinearClamp, uv);
		}

    float4 SampleBoxBloom (float2 uv, float delta) 
    {
			float4 o = _BloomTex_TexelSize.xyxy * float2(-delta, delta).xxyy;
			float4 s =
				SampleBloom(uv + o.xy) + SampleBloom(uv + o.zy) +
				SampleBloom(uv + o.xw) + SampleBloom(uv + o.zw);
			return s * 0.25;
		}

    float4 SampleBox (float2 uv, float delta) 
    {
			float4 o = _BlitTexture_TexelSize.xyxy * float2(-delta, delta).xxyy;
			float4 s =
				Sample(uv + o.xy) + Sample(uv + o.zy) +
				Sample(uv + o.xw) + Sample(uv + o.zw);
			return s * 0.25;
		}

    float3 Saturation (float3 c, float saturationDelta)
    {
      const float3 kLuminance = float3(0.3086, 0.6094, 0.0820);
      float3 intensity = dot(c, kLuminance);
      return lerp(intensity, c, saturationDelta);
    }

    float3 Contrast (float3 c, float contrast)
    {
      const float kContrastMidpoint = 0.21763; // 0.5^2.2
      return (c - kContrastMidpoint) * contrast + kContrastMidpoint;
    }

    float3 ChannelMixer (float3 c, float3 channelRed, float3 channelGreen, float3 channelBlue)
    {
      return float3(dot(c, channelRed), dot(c, channelGreen), dot(c, channelBlue));
    }

    // From https://docs.unity3d.com/Packages/com.unity.shadergraph@6.9/manual/White-Balance-Node.html
    float3 WhiteBalance (float3 c, float temperature, float tint)
    {
      // Range ~[-1.67;1.67] works best
      float t1 = temperature * 10 / 6;
      float t2 = tint * 10 / 6;

      // Get the CIE xy chromaticity of the reference white point.
      // Note: 0.31271 = x value on the D65 white point
      float x = 0.31271 - t1 * (t1 < 0 ? 0.1 : 0.05);
      float standardIlluminantY = 2.87 * x - 3 * x * x - 0.27509507;
      float y = standardIlluminantY + t2 * 0.05;

      // Calculate the coefficients in the LMS space.
      const float3 w1 = float3(0.949237, 1.03542, 1.08728); // D65 white point

      // CIExyToLMS
      float Y = 1;
      float X = Y * x / y;
      float Z = Y * (1 - x - y) / y;
      float L = 0.7328 * X + 0.4296 * Y - 0.1624 * Z;
      float M = -0.7036 * X + 1.6975 * Y + 0.0061 * Z;
      float S = 0.0030 * X + 0.0136 * Y + 0.9834 * Z;
      float3 w2 = float3(L, M, S);

      float3 balance = float3(w1.x / w2.x, w1.y / w2.y, w1.z / w2.z);

      const float3x3 LIN_2_LMS_MAT = {
          3.90405e-1, 5.49941e-1, 8.92632e-3,
          7.08416e-2, 9.63172e-1, 1.35775e-3,
          2.31082e-2, 1.28021e-1, 9.36245e-1
      };

      const float3x3 LMS_2_LIN_MAT = {
          2.85847e+0, -1.62879e+0, -2.48910e-2,
          -2.10182e-1,  1.15820e+0,  3.24281e-4,
          -4.18120e-2, -1.18169e-1,  1.06867e+0
      };

      float3 lms = mul(LIN_2_LMS_MAT, c);
      lms *= balance;
      return mul(LMS_2_LIN_MAT, lms);
    }
  ENDHLSL

  SubShader 
  {
    Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
    LOD 100

    Cull Off
    ZTest Always
    ZWrite Off

    // Downsample Prefilter
    Pass 
    {
      HLSLPROGRAM
        #pragma vertex Vert
        #pragma fragment FragmentProgram

        float4 FragmentProgram (Varyings i) : SV_Target 
        {
          return float4(Prefilter(SampleBox(i.texcoord, _BloomParams.r).rgb), 1);
        }
      ENDHLSL
    }

    // Downsample
    Pass 
    {
      HLSLPROGRAM
        #pragma vertex Vert
        #pragma fragment FragmentProgram

        float4 FragmentProgram (Varyings i) : SV_Target 
        {
          return float4(SampleBox(i.texcoord, _BloomParams.r).rgb, 1);
        }
      ENDHLSL
    }

    // Upsample
    Pass 
    {
      Blend One One 

      HLSLPROGRAM
        #pragma vertex Vert
        #pragma fragment FragmentProgram

        float4 FragmentProgram (Varyings i) : SV_Target 
        {
          return float4(_BloomParams.b * SampleBox(i.texcoord, _BloomParams.r * 0.5).rgb, 1);
        }
      ENDHLSL
    }

    // Downsample Blur
    Pass 
    {
      HLSLPROGRAM
        #pragma vertex Vert
        #pragma fragment FragmentProgram

        float4 FragmentProgram (Varyings i) : SV_Target 
        {
          return float4(SampleBox(i.texcoord, _BlurParams.r).rgb, 1);
        }
      ENDHLSL
    }

    // Upsample blur
    Pass 
    {
      Blend SrcAlpha OneMinusSrcAlpha 

      HLSLPROGRAM
        #pragma vertex Vert
        #pragma fragment FragmentProgram

        float4 FragmentProgram (Varyings i) : SV_Target 
        {
          return float4(SampleBox(i.texcoord, _BlurParams.r).rgb, 1);
        }
      ENDHLSL
    }

    // Final sample blur
    Pass 
    {
      HLSLPROGRAM
        #pragma vertex Vert
        #pragma fragment FragmentProgram

        float4 FragmentProgram (Varyings i) : SV_Target 
        {
          float4 sourceImage = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord);
          float4 blurredImage = SAMPLE_TEXTURE2D(_BlurTex, sampler_LinearClamp, i.texcoord);
          return saturate(lerp(sourceImage, blurredImage, _BlurParams.a));
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
          c.rgb *= _ColorBrightness;
          c.rgb += _BloomParams.b * SampleBoxBloom(i.texcoord, _BloomParams.r * 0.5).rgb;
          c.rgb = Saturation(c.rgb, _ColorParams.r);
          c.rgb = Contrast(c.rgb, _ColorParams.g);
          c.rgb = ChannelMixer(c.rgb, _ChannelMixerRed, _ChannelMixerGreen, _ChannelMixerBlue);
          c.rgb = WhiteBalance(c.rgb, _ColorParams.b, _ColorParams.a);
          return saturate(c);
        }
      ENDHLSL
    }
  }
}