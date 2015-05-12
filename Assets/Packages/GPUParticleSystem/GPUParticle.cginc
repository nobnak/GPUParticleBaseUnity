#ifndef _GPU_PARTICLE_INCLUDE_
#define _GPU_PARTICLE_INCLUDE_

#define PARTICLE_MAX_DISPATCH_X 8192
#define PARTICLE_NUM_THREADS_X 64



AppendStructuredBuffer<uint> Particle_DeadListAppendBuf;
ConsumeStructuredBuffer<uint> Particle_DeadListConsumeBuf;
RWStructuredBuffer<Particle> Particle_InitialBuf;
RWByteAddressBuffer Particle_CounterBuf;



uint Particle_Index(uint3 id) { return id.x + id.y * PARTICLE_MAX_DISPATCH_X; }
void Particle_Dead(uint i) { Particle_DeadListAppendBuf.Append(i); }
uint Particle_Birth() { return Particle_DeadListConsumeBuf.Consume(); }



[numthreads(1,1,1)]
void Particle_Emit(uint3 id : SV_DispatchThreadID) {
	uint i = Particle_Index(id);
	uint count = Particle_CounterBuf.Load(0);
	if (i >= count)
		return;
	uint pid = Particle_Birth();
	ParticleBuf[pid] = Particle_InitialBuf[i];
}

[numthreads(PARTICLE_NUM_THREADS_X,1,1)]
void Particle_Init(uint3 id : SV_DispatchThreadID) {
	uint i = Particle_Index(id);
	Particle_Dead(i);
}

#endif