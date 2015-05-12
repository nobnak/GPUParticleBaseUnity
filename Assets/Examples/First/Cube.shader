Shader "Custom/Cube" {
	Properties {
		_Color ("Color", Color) = (1, 1, 1, 1)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			float4 _Color;
			
			struct Particle {
				float life;
				float3 velocity;
				float3 position;
			};
			StructuredBuffer<float3> VertexBuf;
			StructuredBuffer<Particle> ParticleBuf;
			
			struct vsin {
				uint vid : SV_VertexID;
				uint iid : SV_InstanceID;
			};
			struct vs2ps {
				float4 vertex : POSITION;
			};
			
			vs2ps vert(vsin IN) {
				float3 vertex = VertexBuf[IN.vid];
				Particle p = ParticleBuf[IN.iid];
				
				vertex += p.position;
				if (p.life <= 0)
					vertex = 0;
				
				vs2ps OUT;
				OUT.vertex = mul(UNITY_MATRIX_MVP, float4(vertex, 1));
				return OUT;
			}
			float4 frag(vs2ps IN) : COLOR {
				return _Color;
			}
			ENDCG
		}
	} 
	FallBack Off
}
