using UnityEngine;
using System.Collections;

namespace Common {
	public static class ShaderUtil {	
		
		public static void DispatchSize(int count, int nThreadsX, int nThreadsY, int maxDispatchesX, out int nGroupsX, out int nGroupsY) {
			var blockSize = nThreadsX * nThreadsY;
			var maxBlocksX = maxDispatchesX / nThreadsX;
			var maxRowBlockSize = maxBlocksX * blockSize;
			
			if (count <= maxRowBlockSize) {
				nGroupsX = (count - 1) / blockSize + 1;
				nGroupsY = 1;
			} else {
				nGroupsX = maxBlocksX;
				nGroupsY = (count - 1) / maxRowBlockSize + 1;
			}
			Debug.Log(string.Format("DispatchSize : {0}x{1} from count={2} block={3}x{4} maxDispatchesX={5}",
			                        nGroupsX, nGroupsY, count, nThreadsX, nThreadsY, maxDispatchesX));
		}
	}
}