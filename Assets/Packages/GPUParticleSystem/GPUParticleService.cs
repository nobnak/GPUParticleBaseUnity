using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;

namespace GPUParticleSystem {
	public class GPUParticleService<T> : System.IDisposable {
		public const string KERNEL_INIT = "Particle_Init";
		public const string KERNEL_EMIT = "Particle_Emit";

		public const string PROP_PARTICLE_BUF = "ParticleBuf";
		public const string PROP_DEADLIST_CONSUME_BUF = "Particle_DeadListConsumeBuf";
		public const string PROP_DEADLIST_APPEND_BUF = "Particle_DeadListAppendBuf";
		public const string PROP_INITIAL_BUF = "Particle_InitialBuf";
		public const string PROP_COUNTER_BUF = "Particle_CounterBuf";

		public const int N_THREADS_X = 64;
		public const int N_THREADS_Y = 1;
		public const int MAX_DISPATCHES_X = 8192;

		public readonly int Capacity;
		public readonly int KernelInit;
		public readonly int KernelEmit;
		public readonly ComputeShader Compute;
		public readonly ComputeBuffer ParticleBuf;
		public readonly ComputeBuffer DeadBuf;
		public readonly ComputeBuffer InitialBuf;
		public readonly ComputeBuffer CounterBuf;

		public readonly int NGroupsX;
		public readonly int NGroupsY;
		uint[] _counts;
		T[] _particles;

		public GPUParticleService(ComputeShader compute, int desiredCapacity) {
			ShaderUtil.DispatchSize(desiredCapacity, N_THREADS_X, N_THREADS_Y, MAX_DISPATCHES_X, 
			                               out NGroupsX, out NGroupsY);
			this.Capacity = NGroupsX * NGroupsY * N_THREADS_X * N_THREADS_Y;
			
			this.Compute = compute;
			this.KernelInit = compute.FindKernel (KERNEL_INIT);
			this.KernelEmit = compute.FindKernel (KERNEL_EMIT);
			this.ParticleBuf = new ComputeBuffer(Capacity, Marshal.SizeOf(typeof(T)));
			this.InitialBuf = new ComputeBuffer(N_THREADS_X, Marshal.SizeOf(typeof(int)));
			this.DeadBuf = new ComputeBuffer(Capacity, Marshal.SizeOf(typeof(int)), ComputeBufferType.Append);
			this.CounterBuf = new ComputeBuffer(4, Marshal.SizeOf(typeof(int)), ComputeBufferType.Raw);

			this._counts = new uint[CounterBuf.count];
			this._particles = new T[Capacity];

			compute.SetBuffer(KernelInit, PROP_DEADLIST_APPEND_BUF, DeadBuf);
			compute.Dispatch(KernelInit, NGroupsX, NGroupsY, 1);
		}

		public void Emit(T[] particles) {
			ComputeBuffer.CopyCount(DeadBuf, CounterBuf, 0);

			InitialBuf.SetData(particles);
			Compute.SetBuffer(KernelEmit, PROP_PARTICLE_BUF, ParticleBuf);
			Compute.SetBuffer(KernelEmit, PROP_INITIAL_BUF, InitialBuf);
			Compute.SetBuffer(KernelEmit, PROP_COUNTER_BUF, CounterBuf);
			Compute.SetBuffer(KernelEmit, PROP_DEADLIST_CONSUME_BUF, DeadBuf);
			Compute.Dispatch(KernelEmit, particles.Length, 1, 1);
		}
		public uint GetCount(ComputeBuffer buf) {
			ComputeBuffer.CopyCount(buf, CounterBuf, 0);
			CounterBuf.GetData(_counts);
			return _counts[0];
		}
		public T[] GetParticles() {
			ParticleBuf.GetData(_particles);
			return _particles;
		}

		#region IDisposable implementation
		public void Dispose () {
			ParticleBuf.Release();
			InitialBuf.Release();
			DeadBuf.Release();
			CounterBuf.Release();
		}
		#endregion
	}
}
