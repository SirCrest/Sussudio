namespace Sussudio.Services.Preview;

internal static class PreviewShaderSources
{
    internal const string RendererModeNv12 = "Nv12Shader";
    internal const string RendererModeHdr = "HdrShader";

    internal const string FullscreenVertex = """
        struct VSOutput {
            float4 position : SV_POSITION;
            float2 texcoord : TEXCOORD0;
        };

        VSOutput main(uint vertexId : SV_VertexID) {
            VSOutput output;
            output.texcoord = float2((vertexId << 1) & 2, vertexId & 2);
            output.position = float4(output.texcoord * float2(2.0, -2.0) + float2(-1.0, 1.0), 0.0, 1.0);
            return output;
        }
        """;

    internal const string HdrTonemapPixel = """
        cbuffer ViewportInfo : register(b0) {
            float2 vpOrigin;
            float2 vpSize;
        };

        Texture2D<float> yPlane : register(t0);
        Texture2D<float2> uvPlane : register(t1);
        SamplerState bilinearSampler : register(s0);

        static const float PQ_m1 = 0.1593017578125;
        static const float PQ_m2 = 78.84375;
        static const float PQ_c1 = 0.8359375;
        static const float PQ_c2 = 18.8515625;
        static const float PQ_c3 = 18.6875;

        float3 PQ_EOTF(float3 N) {
            float3 Np = pow(max(N, 0.0), 1.0 / PQ_m2);
            float3 numerator = max(Np - PQ_c1, 0.0);
            float3 denominator = PQ_c2 - PQ_c3 * Np;
            return pow(numerator / denominator, 1.0 / PQ_m1);
        }

        float3 BT2020_to_BT709(float3 c) {
            return float3(
                 1.6605 * c.r - 0.5877 * c.g - 0.0728 * c.b,
                -0.1246 * c.r + 1.1329 * c.g - 0.0083 * c.b,
                -0.0182 * c.r - 0.1006 * c.g + 1.1187 * c.b
            );
        }

        float3 LinearToSRGB(float3 c) {
            float3 lo = 12.92 * c;
            float3 hi = 1.055 * pow(max(c, 1e-6), 1.0 / 2.4) - 0.055;
            return float3(
                c.r <= 0.0031308 ? lo.r : hi.r,
                c.g <= 0.0031308 ? lo.g : hi.g,
                c.b <= 0.0031308 ? lo.b : hi.b
            );
        }

        float4 main(float4 pos : SV_Position) : SV_Target {
            float2 uv = (pos.xy - vpOrigin) / vpSize;

            float y_raw = yPlane.Sample(bilinearSampler, uv);
            float2 uv_raw = uvPlane.Sample(bilinearSampler, uv);

            float Y = saturate((y_raw - 64.0 / 1023.0) * 1023.0 / (940.0 - 64.0));
            float Cb = (uv_raw.x - 512.0 / 1023.0) * 1023.0 / (960.0 - 64.0);
            float Cr = (uv_raw.y - 512.0 / 1023.0) * 1023.0 / (960.0 - 64.0);

            float3 rgb;
            rgb.r = Y + 1.4746 * Cr;
            rgb.g = Y - 0.16455 * Cb - 0.57135 * Cr;
            rgb.b = Y + 1.8814 * Cb;
            rgb = saturate(rgb);

            float3 linearScene = PQ_EOTF(rgb) * 10000.0;
            linearScene /= 100.0;

            float3 bt709 = BT2020_to_BT709(linearScene);
            bt709 = max(bt709, 0.0);

            float3 tonemapped = bt709 / (1.0 + bt709);
            float3 srgb = LinearToSRGB(tonemapped);
            return float4(saturate(srgb), 1.0);
        }
        """;

    internal const string HdrPassthroughPixel = """
        cbuffer ViewportInfo : register(b0) {
            float2 vpOrigin;
            float2 vpSize;
        };

        Texture2D<float> yPlane : register(t0);
        Texture2D<float2> uvPlane : register(t1);
        SamplerState bilinearSampler : register(s0);

        float4 main(float4 pos : SV_Position) : SV_Target {
            float2 uv = (pos.xy - vpOrigin) / vpSize;

            float y_raw = yPlane.Sample(bilinearSampler, uv);
            float2 uv_raw = uvPlane.Sample(bilinearSampler, uv);

            // Narrow-range P010 to normalized YCbCr (same as tonemap shader)
            float Y = saturate((y_raw - 64.0 / 1023.0) * 1023.0 / (940.0 - 64.0));
            float Cb = (uv_raw.x - 512.0 / 1023.0) * 1023.0 / (960.0 - 64.0);
            float Cr = (uv_raw.y - 512.0 / 1023.0) * 1023.0 / (960.0 - 64.0);

            // BT.2020 YCbCr to RGB (preserve PQ encoding, no EOTF/tonemap/OETF)
            float3 rgb;
            rgb.r = Y + 1.4746 * Cr;
            rgb.g = Y - 0.16455 * Cb - 0.57135 * Cr;
            rgb.b = Y + 1.8814 * Cb;
            return float4(saturate(rgb), 1.0);
        }
        """;

    internal const string Nv12Pixel = """
        cbuffer ViewportInfo : register(b0)
        {
            float2 vpOrigin;
            float2 vpSize;
        };

        Texture2D<float> yPlane : register(t0);
        Texture2D<float2> uvPlane : register(t1);
        SamplerState bilinear : register(s0);

        float4 main(float4 pos : SV_Position) : SV_Target
        {
            float2 uv = (pos.xy - vpOrigin) / vpSize;

            float y = yPlane.Sample(bilinear, uv).r;
            float2 uv2 = uvPlane.Sample(bilinear, uv);
            float cb = uv2.r - 0.501960784f;
            float cr = uv2.g - 0.501960784f;

            float r = saturate(y + 1.57480f * cr);
            float g = saturate(y - 0.18732f * cb - 0.46812f * cr);
            float b = saturate(y + 1.85560f * cb);
            return float4(r, g, b, 1.0f);
        }
        """;
}
