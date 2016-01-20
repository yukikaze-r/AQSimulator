using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AQSimulator {
	public class ReachableMap {
		public interface SearchTarget {
			bool IsWalkable(GridPoint point);
			int SizeX {
				get;
			}
			int SizeY {
				get;
			}
		}

		private int[,] reachableMap;
		private int reachableCount;

		private Stack<GridPoint> reachables = new Stack<GridPoint>();
		private SearchTarget target;

		public ReachableMap(SearchTarget target) {
			this.reachableCount = 0;
			this.target = target;
			reachableMap = new int[target.SizeX,target.SizeY];

			reachableCount = 1;
			for (int x = 0; x < target.SizeX; x++) {
				for (int y = 0; y < target.SizeY; y++) {
					if (InitializeReachable(new GridPoint(x, y), reachableCount)) {
						Calculate(reachableCount);
						reachableCount++;
					}
				}
			}

		}
		
		private void Calculate(int reachableCount) {
			while (reachables.Count >= 1) {
				GridPoint ngp;
				GridPoint gp = reachables.Pop();
				ngp.X = gp.X - 1;
				ngp.Y = gp.Y;
				if (ngp.X >= 0) {
					InitializeReachable(ngp, reachableCount);
				}
				ngp.X = gp.X;
				ngp.Y = gp.Y - 1;
				if(ngp.Y >= 1) {
					InitializeReachable(ngp, reachableCount);
				}
				ngp.X = gp.X + 1;
				ngp.Y = gp.Y;
				if (ngp.X < target.SizeX) {
					InitializeReachable(ngp, reachableCount);
				}
				ngp.X = gp.X;
				ngp.Y = gp.Y + 1;
				if (ngp.Y < target.SizeY) {
					InitializeReachable(ngp, reachableCount);
				}
			}
		}

		private bool InitializeReachable(GridPoint pos, int reachableCount) {
			if ((reachableMap[pos.X,pos.Y] == 0 || reachableMap[pos.X, pos.Y] > reachableCount) && target.IsWalkable(pos)) {
				reachables.Push(pos);
				reachableMap[pos.X,pos.Y] = reachableCount;
				return true;
			} else {
				return false;
			}
		}
		

		public bool IsReachable(GridPoint pos1, GridPoint pos2) {
			return reachableMap[pos1.X,pos1.Y] == reachableMap[pos2.X,pos2.Y];
		}

		public IEnumerable<GridPoint> GetAllReachable(GridPoint pos) {
			int level = reachableMap[pos.X, pos.Y];
			for (int x=0; x < target.SizeX; x++) {
				for(int y=0; y<target.SizeY;y++) {
					if(reachableMap[x, y] == level) {
						yield return new GridPoint(x, y);
					}
				}
			}
		}
		

	}

}