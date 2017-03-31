Shader "Terrain/TerrainShader" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_SecondTex("Second (RGB)",2D) = "white"{}
		_ThirdTex("ThirdTex (RGB)",2D) = "white"{}
		_FourthTex("FourthTex (RGB)",2D) = "white"{}
		_Mask("Mask(RG)",2D) = "white"{}
	}
	SubShader{
		Tags{ "Queue" = "Geometry" "RenderType" = "Opaque" }
		// ZWrite Off
		LOD 200

		CGPROGRAM
// #pragma surface surf Unlit
#pragma surface surf Lambert
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _SecondTex;
		sampler2D _ThirdTex;
		sampler2D _FourthTex;
		sampler2D _Mask;

		struct Input {
			float2 uv_MainTex;
			//float2 uv_SecondTex;
			//float2 uv_ThirdTex;
			//float2 uv_FourthTex;
			float2 uv_Mask;
		};

		void surf(Input IN, inout SurfaceOutput o) {
			half4 c1 = tex2D(_MainTex, IN.uv_MainTex);
			half4 c2 = tex2D(_SecondTex, IN.uv_MainTex);
			half4 c3 = tex2D(_ThirdTex, IN.uv_MainTex);
			half4 c4 = tex2D(_FourthTex, IN.uv_MainTex);
			//half4 c2 = tex2D(_SecondTex, IN.uv_SecondTex);
			//half4 c3 = tex2D(_ThirdTex, IN.uv_ThirdTex);
			//half4 c4 = tex2D(_FourthTex, IN.uv_FourthTex);
			half4 cm = tex2D(_Mask, IN.uv_Mask);
			//o.Albedo = c1.rgb*cm.r + c2.rgb*cm.g + c3.rgb*cm.b;
			o.Albedo = c1.rgb*cm.r + c2.rgb*cm.g + c3.rgb*cm.b + c4.rgb*cm.a;
			o.Alpha = c1.a;
		}

		// inline half4 LightingUnlit(SurfaceOutput s, fixed3 lightDir, fixed atten)
		// {
		// 	half4 c = half4(1, 1, 1, 1);
		// 	c.rgb = s.Albedo;
		// 	c.a = s.Alpha;
		// 	return c;
		// }

		ENDCG
	}
	FallBack "Diffuse"
}