#ifndef _GPU_PARTICLE_INCLUDE_
#define _GPU_PARTICLE_INCLUDE_

#define PARTICLE_MAX_DISPATCH_X 8192
#define PARTICLE_NUM_THREADS_X 64



AppendStructuredBuffer<uint> Particle_DeadAppendBuf;
ConsumeStructuredBuffer<uint> Particle_DeadConsumeBuf;
RWStructuredBuffer<Particle> Particle_InitialBuf;
RWByteAddressBuffer Particle_CounterCurrBuf;
RWByteAddressBuffer Particle_CounterPrevBuf;



uint Particle_Index(uint3 id) { return id.x + id.y * PARTICLE_MAX_DISPATCH_X; }
void Particle_Dead(uint i) {
	Particle_CounterCurrBuf.InterlockedAdd(0, +1);
	Particle_DeadAppendBuf.Append(i);
}
uint Particle_Birth() {
	Particle_CounterCurrBuf.InterlockedAdd(0, -1);
	return Particle_DeadConsumeBuf.Consume(); 
}



[numthreads(1,1,1)]
void Particle_Emit(uint3 id : SV_DispatchThreadID) {
	uint i = Particle_Index(id);
	uint count = Particle_CounterPrevBuf.Load(0);
	if (i >= count)
		return;
	uint pid = Particle_Birth();
	ParticleBuf[pid] = Particle_InitialBuf[i];
}

[numthreads(1,1,1)]
void Particle_Copy(uint3 id : SV_DispatchThreadID) {
	uint i = 4 * Particle_Index(id);
	Particle_CounterPrevBuf.Store(i, Particle_CounterCurrBuf.Load(i));
}

#endif