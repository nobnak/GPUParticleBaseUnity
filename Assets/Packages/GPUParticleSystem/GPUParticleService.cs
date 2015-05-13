using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;

namespace GPUParticleSystem {
	public class GPUParticleService<T> : System.IDisposable {
		public const string KERNEL_INIT = "Particle_Init";
		public const string KERNEL_EMIT = "Particle_Emit";
		public const string KERNEL_COPY = "Particle_Copy";

		public const string PROP_PARTICLE_BUF = "ParticleBuf";
		public const string PROP_DEAD_APPEND_BUF = "Particle_DeadAppendBuf";
		public const string PROP_DEAD_CONSUME_BUF = "Particle_DeadConsumeBuf";
		public const string PROP_INITIAL_BUF = "Particle_InitialBuf";
		public const string PROP_COUNTER_CURR_BUF = "Particle_CounterCurrBuf";
		public const string PROP_COUNTER_PREV_BUF = "Particle_CounterPrevBuf";

		public const int N_THREADS_X = 64;
		public const int N_THREADS_Y = 1;
		public const int MAX_DISPATCHES_X = 8192;

		public readonly int Capacity;
		public readonly int KernelInit;
		public readonly int KernelEmit;
		public readonly int KernelCopy;
		public readonly ComputeShader Compute;
		public readonly ComputeBuffer ParticleBuf;
		public readonly ComputeBuffer DeadBuf;
		public readonly ComputeBuffer InitialBuf;
		public readonly ComputeBuffer CounterCurrBuf;
		public readonly ComputeBuffer CounterPrevBuf;

		public readonly int NGroupsX;
		public readonly int NGroupsY;

		uint[] _counts;
		T[] _particles;
		T[] _initials;

		public GPUParticleService(ComputeShader compute, int desiredCapacity) {
			ShaderUtil.DispatchSize(desiredCapacity, N_THREADS_X, N_THREADS_Y, MAX_DISPATCHES_X, 
			                               out NGroupsX, out NGroupsY);
			this.Capacity = NGroupsX * NGroupsY * N_THREADS_X * N_THREADS_Y;
			
			this.Compute = compute;
			this.KernelInit = compute.FindKernel(KERNEL_INIT);
			this.KernelEmit = compute.FindKernel(KERNEL_EMIT);
			this.KernelCopy = compute.FindKernel(KERNEL_COPY);
			this.ParticleBuf = new ComputeBuffer(Capacity, Marshal.SizeOf(typeof(T)));
			this.InitialBuf = new ComputeBuffer(N_THREADS_X, Marshal.SizeOf(typeof(T)));
			this.DeadBuf = new ComputeBuffer(Capacity, Marshal.SizeOf(typeof(uint)), ComputeBufferType.Append);
			this.CounterCurrBuf = new ComputeBuffer(1, Marshal.SizeOf(typeof(uint)), ComputeBufferType.Raw);
			this.CounterPrevBuf = new ComputeBuffer(1, Marshal.SizeOf (typeof(uint)), ComputeBufferType.Raw);

			this._particles = new T[ParticleBuf.count];
			this._initials = new T[InitialBuf.count];
			this._counts = new uint[]{ 0 };
			CounterCurrBuf.SetData (_counts);
			ParticleBuf.SetData (_particles);
			DeadBuf.SetData (new uint[0]);

			compute.SetBuffer (KernelInit, PROP_DEAD_APPEND_BUF, DeadBuf);
			compute.SetBuffer (KernelInit, PROP_COUNTER_CURR_BUF, CounterCurrBuf);
			compute.Dispatch (KernelInit, NGroupsX, NGroupsY, 1);
		}

		public void Emit(T[] particles) {
			Compute.SetBuffer (KernelCopy, PROP_COUNTER_CURR_BUF, CounterCurrBuf);
			Compute.SetBuffer (KernelCopy, PROP_COUNTER_PREV_BUF, CounterPrevBuf);
			Compute.Dispatch (KernelCopy, 1, 1, 1);

			var len = Mathf.Min(particles.Length, _initials.Length);
			System.Array.Copy(particles, _initials, len);
			InitialBuf.SetData(_initials);

			Compute.SetBuffer(KernelEmit, PROP_PARTICLE_BUF, ParticleBuf);
			Compute.SetBuffer(KernelEmit, PROP_INITIAL_BUF, InitialBuf);
			Compute.SetBuffer(KernelEmit, PROP_COUNTER_CURR_BUF, CounterCurrBuf);
			Compute.SetBuffer(KernelEmit, PROP_COUNTER_PREV_BUF, CounterPrevBuf);
			Compute.SetBuffer(KernelEmit, PROP_DEAD_CONSUME_BUF, DeadBuf);
			Compute.Dispatch(KernelEmit, len, 1, 1);
		}
		public void Prepare(int kernel) {
			Compute.SetBuffer(kernel, PROP_PARTICLE_BUF, ParticleBuf);
			Compute.SetBuffer(kernel, PROP_DEAD_APPEND_BUF, DeadBuf);
			Compute.SetBuffer(kernel, PROP_COUNTER_CURR_BUF, CounterCurrBuf);
			Compute.SetBuffer(kernel, PROP_COUNTER_PREV_BUF, CounterPrevBuf);
		}
		public uint GetDeadCount() {
			CounterCurrBuf.GetData(_counts);
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
			CounterCurrBuf.Release();
			CounterPrevBuf.Release();
		}
		#endregion
	}
}
