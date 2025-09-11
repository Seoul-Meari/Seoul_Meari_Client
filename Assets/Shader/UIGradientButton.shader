Shader "UI/UIGradientButton (PremulBright, AspectFixed, NoRadius)"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint (vertex color multiply)", Color) = (1,1,1,1)

        // Gradient
        _GradientStartColor ("Gradient Start Color", Color) = (0.7,0.3,1,1)
        _GradientEndColor   ("Gradient End Color", Color)   = (0.3,0.7,1,1)
        _AngleDeg           ("Gradient Angle (deg)", Range(0,360)) = 0
        _TExp               ("Gradient Emphasis (pow)", Range(0.5, 2.0)) = 0.85

        // Outline (UV 단위)
        _OutlineColor       ("Outline Color", Color) = (1,1,1,0)
        _OutlineThickness   ("Outline Thickness (UV)", Range(0,0.1)) = 0.0

        // Edge feather (AA)
        _Feather            ("Edge Feather (px)", Range(0.5, 2.5)) = 1.0

        // Options
        _IgnoreVertexColor  ("Ignore Vertex Color (0/1)", Range(0,1)) = 1

        // RectTransform 크기(픽셀) - 스크립트에서 세팅
        _RectSize           ("Rect Size (px, xy)", Vector) = (100, 50, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off

        // Premultiplied Alpha
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0; // 0..1
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // xy = 1/width,height

            fixed4 _Color;

            fixed4 _GradientStartColor;
            fixed4 _GradientEndColor;
            float  _AngleDeg;
            float  _TExp;

            fixed4 _OutlineColor;
            float  _OutlineThickness;

            float  _Feather;
            float  _IgnoreVertexColor;

            float4 _RectSize; // x=width(px), y=height(px)

            // Axis-aligned box SDF (no radius)
            // b: half size
            float sdBox(float2 p, float2 b)
            {
                float2 d = abs(p) - b;
                return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
            }

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color  = v.color * _Color;
                o.uv     = v.texcoord; // UI 스프라이트 UV 0..1
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // --- Texture mask
                fixed4 tex = tex2D(_MainTex, i.uv);

                // --- Gradient factor (임의 각도) + 강조(pow)
                float ang = radians(_AngleDeg);
                float2 dir = float2(cos(ang), sin(ang));
                float t = dot(i.uv - 0.5, dir) + 0.5; // 0..1
                t = saturate(t);
                t = pow(t, _TExp);

                fixed4 startCol = _GradientStartColor;
                fixed4 endCol   = _GradientEndColor;
                fixed4 gradCol  = lerp(startCol, endCol, t);

                // --- Aspect 보정
                float w = max(_RectSize.x, 1.0);
                float h = max(_RectSize.y, 1.0);
                float aspect = w / h;               // >1: 가로가 더 김
                float2 scale = float2(aspect, 1.0); // x만 늘려 동일 '거리' 기준

                // 중심 기준 좌표(-0.5..0.5) → 보정된 공간
                float2 p_corr = (i.uv - 0.5) * scale;

                // 외곽/내곽 반폭(보정된 공간에서 동일 스케일 적용)
                float outline = _OutlineThickness;
                float2 halfOuter = float2(0.5, 0.5) * scale;
                float2 halfInner = (float2(0.5, 0.5) - outline) * scale;

                // SDF (no radius)
                float dOuter = sdBox(p_corr, halfOuter);
                float dInner = sdBox(p_corr, halfInner);

                // --- Feather/AA
                float px = (_MainTex_TexelSize.x + _MainTex_TexelSize.y) * 0.5;
                float aa = max(fwidth(dOuter), _Feather * px);

                float fillAlpha    = smoothstep(0.0, -aa, dInner);
                float outlineAlpha = saturate( smoothstep(aa, -aa, dOuter) - smoothstep(aa, -aa, dInner) );

                // --- Premultiplied 합성
                fixed4 outlineCol = _OutlineColor;
                outlineCol.rgb *= outlineCol.a;

                gradCol.rgb *= gradCol.a;

                fixed4 col = outlineCol * outlineAlpha;
                col = lerp(col, gradCol, fillAlpha);

                // 스프라이트 알파로 마스킹
                col.rgb *= tex.a;
                col.a   *= tex.a;

                // 버텍스 컬러 옵션
                if (_IgnoreVertexColor < 0.5)
                {
                    col.rgb *= i.color.rgb;
                    col.a   *= i.color.a;
                }

                return col;
            }
            ENDCG
        }
    }
}