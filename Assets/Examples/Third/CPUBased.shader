Shader "Custom/CPUBased" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			sampler2D _MainTex;
			float4 _Color;
			
			struct vsin {
				uint vid : SV_VertexID;
				uint iid : SV_InstanceID;
			};
			struct vs2ps {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};
			
			struct Particle {
				float time;
				float3 position;
				float3 velocity;
			};
			
			StructuredBuffer<Particle> ParticleBuf;
			StructuredBuffer<float3> VertexBuf;
			
			vs2ps vert(vsin IN) {
				Particle p = ParticleBuf[IN.iid];
				float3 vertex = VertexBuf[IN.vid];
				vertex += p.position;
			
				vs2ps OUT;
				OUT.vertex = mul(UNITY_MATRIX_MVP, float4(vertex, 1));
				OUT.uv = 0;
				return OUT;
			}
			float4 frag(vs2ps IN) : COLOR {
				return _Color * tex2D(_MainTex, IN.uv);
			}
			ENDCG
		}
	} 
	FallBack Off
}
