using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class CPUBased : MonoBehaviour {
	public const string PROP_PARTICLE_BUF = "ParticleBuf";
	public const string PROP_VERTEX_BUF = "VertexBuf";

	public ComputeShader compute;
	public GameObject fab;
	public Material instanceMat;
	public int nInstances = 10000;

	GPUParticle[] _particles;
	ComputeBuffer _particleBuf;
	ComputeBuffer _vertexBuf;

	void Start () {	
		_particles = new GPUParticle[nInstances];
		_particleBuf = new ComputeBuffer(_particles.Length, Marshal.SizeOf(_particles[0]));
		for (var i = 0; i < _particles.Length; i++) {
			var p = _particles[i];
			p.velocity = 1f * Random.onUnitSphere;
			p.position = 10f * Random.insideUnitSphere;
			_particles[i] = p;
		}
		_particleBuf.SetData(_particles);

		var mesh = fab.GetComponent<MeshFilter>().sharedMesh;
		var expandedVertices = new Vector3[mesh.triangles.Length];
		for (var i = 0; i < expandedVertices.Length; i++)
			expandedVertices[i] = fab.transform.TransformPoint(mesh.vertices[mesh.triangles[i]]);
		_vertexBuf = new ComputeBuffer(expandedVertices.Length, Marshal.SizeOf(expandedVertices[0]));
		_vertexBuf.SetData(expandedVertices);
	}
	void OnDestroy() {
		_particleBuf.Release();
		_vertexBuf.Release();
	}
	void Update () {
		var dt = Time.deltaTime;
		for (var i = 0; i < _particles.Length; i++) {
			var p = _particles[i];
			p.time += dt;
			p.position += p.velocity * dt;
			_particles[i] = p;
		}
		_particleBuf.SetData(_particles);
	}
	void OnRenderObject() {
		if (_vertexBuf == null)
			return;

		GL.PushMatrix();
		GL.MultMatrix(transform.localToWorldMatrix);
		instanceMat.SetPass(0);
		instanceMat.SetBuffer(PROP_PARTICLE_BUF, _particleBuf);
		instanceMat.SetBuffer(PROP_VERTEX_BUF, _vertexBuf);
		Graphics.DrawProcedural(MeshTopology.Triangles, _vertexBuf.count, nInstances);
		GL.PopMatrix();
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct GPUParticle {
		public float time;
		public Vector3 position;
		public Vector3 velocity;
	}
}
