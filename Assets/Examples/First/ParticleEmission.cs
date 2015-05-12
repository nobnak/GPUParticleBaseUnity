using UnityEngine;
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;

namespace GPUParticle {

	public class ParticleEmission : MonoBehaviour {
		public int capacity = 64;
		public float radius = 10f;
		public ComputeShader compute;

		ParticleService<GPUParticle> _particles;

		void Start () {
			_particles = new ParticleService<GPUParticle>(compute, capacity);
			StartCoroutine(Progress());
		}
		void OnDestroy() {
			_particles.Dispose();
		}
		void Update () {

			if (Input.GetMouseButtonDown(0)) {
				var particles = new GPUParticle[]{ new GPUParticle(){ 
						life = 30, position = radius * Random.insideUnitSphere } };
				_particles.Emit(particles);
				Debug.LogFormat("DeadList Counter : {0}", _particles.GetCount(_particles.DeadBuf));
			}
		}
		void OnDrawGizmosSelected() {
			if (_particles != null)
				DrawGizmos(transform);
		}

		IEnumerator Progress() {
			while (true) {
				yield return new WaitForSeconds(1f);

				var count = _particles.GetCount(_particles.DeadBuf);
				var particles = new GPUParticle[capacity];
				_particles.ParticleBuf.GetData(particles);

				var log = new StringBuilder();
				for (var i = 0; i < particles.Length; i++)
					log.AppendFormat(" {0:f0}", particles[i].life);
				Debug.LogFormat("Particles (Deads={0}) : {1}", count, log);
			}
		}
		void DrawGizmos(Transform parent) {
			var _alphabets = _particles.GetParticles ();
			var size = 1f * Vector3.one;
			for (var i = 0; i < _alphabets.Length; i++) {
				var alph = _alphabets[i];
				if (alph.life > 0)
					Gizmos.DrawWireCube(parent.TransformPoint(alph.position), size);
			}
		}
				
		[StructLayout(LayoutKind.Sequential)]
		public struct GPUParticle {
			public float life;
			public Vector3 position;
		}
	}
}
