using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;

public class Interlocking : MonoBehaviour {
	public const string KERNEL_COUNT = "Count";
	public const int N_GROUP_THREADS = 64;
	
	public const string PROP_COUNTER_BUF = "_Counter";
	public const string PROP_OUTPUT_BUF = "_Output";

	public int nGroupsX = 128;
	public ComputeShader compute;

	int _kernelCount;
	uint[] _counts;
	uint[] _outputs;
	ComputeBuffer _countBuf;
	ComputeBuffer _outputBuf;

	void OnEnable() {
		_kernelCount = compute.FindKernel(KERNEL_COUNT);
		_counts = new uint[] { 0 };
		_outputs = new uint[nGroupsX * N_GROUP_THREADS];
		_countBuf = new ComputeBuffer(_counts.Length, Marshal.SizeOf(typeof(uint)), ComputeBufferType.Raw);
		_outputBuf = new ComputeBuffer(_outputs.Length, Marshal.SizeOf(typeof(uint)), ComputeBufferType.Raw);
	}
	void OnDisable() {
		_countBuf.Release();
		_outputBuf.Release();
	}
	void Start() {
		StartCoroutine(Progress());
	}
	void Update() {
		_countBuf.SetData(_counts);

		compute.SetBuffer(_kernelCount, PROP_COUNTER_BUF, _countBuf);
		compute.SetBuffer(_kernelCount, PROP_OUTPUT_BUF, _outputBuf);
		compute.Dispatch(_kernelCount, nGroupsX, 1, 1);
	}

	IEnumerator Progress() {
		while (true) {
			yield return new WaitForSeconds(1f);

			_outputBuf.GetData(_outputs);
			System.Array.Sort(_outputs);
			var log = new StringBuilder();
			for (var i = 0; i < _outputs.Length; i++)
				log.AppendFormat(" {0}", _outputs[i]);
			Debug.LogFormat("Output : {0}", log);
		}
	}
}
