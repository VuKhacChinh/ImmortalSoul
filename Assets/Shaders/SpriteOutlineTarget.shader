Shader "Custom/SpriteOutlineTarget8Dir"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1,0,0,1)
        _OutlineSize ("Outline Size", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            float4 _OutlineColor;
            float _OutlineSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                float2 offset = _MainTex_TexelSize.xy * _OutlineSize;

                float alphaSum = 0;

                alphaSum += tex2D(_MainTex, i.uv + float2(offset.x,0)).a;
                alphaSum += tex2D(_MainTex, i.uv - float2(offset.x,0)).a;
                alphaSum += tex2D(_MainTex, i.uv + float2(0,offset.y)).a;
                alphaSum += tex2D(_MainTex, i.uv - float2(0,offset.y)).a;

                alphaSum += tex2D(_MainTex, i.uv + offset).a;
                alphaSum += tex2D(_MainTex, i.uv - offset).a;
                alphaSum += tex2D(_MainTex, i.uv + float2(offset.x,-offset.y)).a;
                alphaSum += tex2D(_MainTex, i.uv + float2(-offset.x,offset.y)).a;

                if(col.a == 0 && alphaSum > 0)
                {
                    return _OutlineColor;
                }

                return col;
            }

            ENDCG
        }
    }
}