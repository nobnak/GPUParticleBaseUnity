using UnityEngine;
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;
using GPUParticleSystem;

public class First : MonoBehaviour {
	public const string KERNEL_UPDATE = "Update";
	public const string PROP_TIME_DELTA = "dt";

	public int capacity = 64;
	public float speed = 1f;
	public ComputeShader compute;

	int _kernelUpdate;
	GPUParticleService<GPUParticle> _particles;

	void Start () {
		_kernelUpdate = compute.FindKernel(KERNEL_UPDATE);
		_particles = new GPUParticleService<GPUParticle>(compute, capacity);
		StartCoroutine(Progress());
	}
	void OnDestroy() {
		_particles.Dispose();
	}
	void Update () {
		compute.SetFloat(PROP_TIME_DELTA, Time.deltaTime);
		compute.SetBuffer(_kernelUpdate, GPUParticleService<GPUParticle>.PROP_PARTICLE_BUF, _particles.ParticleBuf);
		compute.SetBuffer(_kernelUpdate, GPUParticleService<GPUParticle>.PROP_DEADLIST_APPEND_BUF, _particles.DeadBuf);
		compute.Dispatch(_kernelUpdate, _particles.NGroupsX, _particles.NGroupsY, 1);

		if (Input.GetMouseButtonDown(0)) {
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			var localDir = transform.InverseTransformDirection(ray.direction);
			var localPos = transform.InverseTransformPoint(ray.origin);

			var particles = new GPUParticle[]{ new GPUParticle(){ 
					life = 30, velocity = speed * localDir, position = localPos } };
			_particles.Emit(particles);
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
		public Vector3 velocity;
		public Vector3 position;
	}
}
