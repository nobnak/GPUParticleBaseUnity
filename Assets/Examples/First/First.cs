using UnityEngine;
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;
using GPUParticleSystem;

public class First : MonoBehaviour {
	public const string KERNEL_UPDATE = "Update";
	public const string PROP_TIME_DELTA = "dt";

	public const string PROP_VERTEX_BUF = "VertexBuf";
	public const string PROP_PARTICLE_BUF = "ParticleBuf";

	public int capacity = 64;
	public float speed = 1f;
	public bool splash;
	public ComputeShader compute;
	public Material mat;
	public GameObject fab;

	int _kernelUpdate;
	GPUParticleService<GPUParticle> _particles;
	ComputeBuffer _vertexBuf;

	void OnEnable() {
		_kernelUpdate = compute.FindKernel(KERNEL_UPDATE);
		_particles = new GPUParticleService<GPUParticle>(compute, capacity);

		var mesh = fab.GetComponent<MeshFilter>().sharedMesh;
		var nonIndexedVertices = new Vector3[mesh.triangles.Length];
		for (var i = 0; i < mesh.triangles.Length; i++)
			nonIndexedVertices[i] = fab.transform.TransformPoint(mesh.vertices[mesh.triangles[i]]);
		_vertexBuf = new ComputeBuffer(nonIndexedVertices.Length, Marshal.SizeOf(nonIndexedVertices[0]));
		_vertexBuf.SetData(nonIndexedVertices);
	}
	void OnDisable() {
		_particles.Dispose();
		_vertexBuf.Release();
	}
	void Start() {
		StartCoroutine(Progress());
	}
	void Update () {
#if true
		_particles.Prepare(_kernelUpdate);
		compute.SetFloat(PROP_TIME_DELTA, Time.deltaTime);
		compute.Dispatch(_kernelUpdate, _particles.NGroupsX, _particles.NGroupsY, 1);

		if (splash) {
			var mousePos = Input.mousePosition;
			mousePos.z = 10f;
			var ray = Camera.main.ScreenPointToRay(mousePos);
			var localDir = transform.InverseTransformDirection(ray.direction);
			var localPos = transform.InverseTransformPoint(ray.origin);

			var particles = new GPUParticle[]{ new GPUParticle(){ 
					life = 5, velocity = speed * localDir, position = localPos } };
			_particles.Emit(particles);
		}
#else
		if (splash) {
			var particles = new GPUParticle[]{
				new GPUParticle(){ life = 5, position = 10f * Random.insideUnitSphere } };
			_particles.Emit(particles);
		}
#endif
	}
	void OnDrawGizmosSelected() {
		if (_particles != null)
			DrawGizmos(transform);
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

	void OnRenderObject() {
		GL.PushMatrix();
		GL.MultMatrix(transform.localToWorldMatrix);
		mat.SetPass(0);
		mat.SetBuffer(PROP_VERTEX_BUF, _vertexBuf);
		mat.SetBuffer(PROP_PARTICLE_BUF, _particles.ParticleBuf);
		Graphics.DrawProcedural(MeshTopology.Triangles, _vertexBuf.count, _particles.Capacity);
        GL.PopMatrix();
    }

    IEnumerator Progress() {
		while (true) {
			yield return new WaitForSeconds(1f);
			
			var count = _particles.GetDeadCount();
			Debug.LogFormat("Particle Dead Count = {0}/{1}", count, _particles.Capacity);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct GPUParticle {
		public float life;
		public Vector3 velocity;
		public Vector3 position;
	}
}
