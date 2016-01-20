using System;
using System.Collections.Generic;


namespace AQSimulator {
	public class RangeMatrix {

		private static Dictionary<long, RangeMatrix> rangeDictionary = new Dictionary<long, RangeMatrix>();
		private static Dictionary<int, RangeMatrix> dictionary = new Dictionary<int, RangeMatrix>();

		public static RangeMatrix Get(int r) {
			RangeMatrix ret;
			if (!dictionary.TryGetValue(r, out ret)) {
				ret = new RangeMatrix(r);
				dictionary.Add(r, ret);
			}
			return ret;
		}

		

		public static RangeMatrix Get(int minR, int maxR) {
			//		return Get(maxR) - Get(minR - 1);
			//		Pair<int,int> key = new Pair<int, int>(minR, maxR);
			long key = minR + (long)maxR * int.MaxValue;
			RangeMatrix ret;
			if (!rangeDictionary.TryGetValue(key, out ret)) {
				ret = Get(maxR) - Get(minR - 1);
				rangeDictionary.Add(key, ret);
			}
			return ret;
		}

		private bool[,] matrix;

		private RangeMatrix(int r) {
			matrix = new bool[r+1,r+1];

			int sqrR = r * r;

			for (int i = 0; i < r + 1; i++) {
				for (int j = 0; j < r + 1; j++) {
					int sqrD = i * i + j * j;
					matrix[i, j] = sqrD <= sqrR;
				}
			}
		}

		public RangeMatrix(int r, float dx, float dy) {
			matrix = new bool[r + 1, r + 1];

			for (int x = 0; x <= r; x++) {
				for (int y = 0; y <= r; y++) {
					float px = x + dx;
					float py = y + dy;
					matrix[x, y] = px * px + py * py <= r * r;
				}
			}
		}
		private RangeMatrix(RangeMatrix a) {
			matrix = (bool[,])a.matrix.Clone();
		}
		
		// range: 1
		// 1 0
		// 1 1

		// {0,0} {0,1} {0,-1}, {1,0}, {-1,0}

		//   1 
		// 1 1 1 
		//   1

		public IEnumerable<GridPoint> GetPoints() {
			GridPoint ret;
			for (int i = 0; i < matrix.GetLength(0); i++) {
				for (int j = 0; j < matrix.GetLength(1); j++) {
					if (matrix[i, j]) {
						ret.X = i;
						ret.Y = j;
						yield return ret;
						if (i != 0) {
							ret.X = -i;
							ret.Y = j;
							yield return ret;
						}
						if (j != 0) {
							ret.X = i;
							ret.Y = -j;
							yield return ret;
						}
						if (i != 0 && j != 0) {
							ret.X = -i;
							ret.Y = -j;
							yield return ret;
						}
					}
				}
			}
		}


		// range: 1
		// 1 0
		// 1 1

		// {0,0} {0,1} {1,0} {-1,0} {-1,1} {-2,0} {0,-1} {1,-1} {1,-2} {-1,-1} {-2,-1} {-1,-2} 

		//    -2 -1 0 1
		// 1      1 1 
		// 0    1 1 1 1 
		// -1   1 1 1 1
		// -2     1 1

		public IEnumerable<GridPoint> GetPointsOriginMinus0505() {
			GridPoint ret;
			for (int i = 0; i < matrix.GetLength(0); i++) {
				for (int j = 0; j < matrix.GetLength(1); j++) {
					if (matrix[i, j]) {
						ret.X = i;
						ret.Y = j;
						yield return ret;
						ret.X = -i-1;
						ret.Y = j;
						yield return ret;
						ret.X = i;
						ret.Y = -j-1;
						yield return ret;
						ret.X = -i-1;
						ret.Y = -j-1;
						yield return ret;
					}
				}
			}
		}
		public static RangeMatrix operator -(RangeMatrix a, RangeMatrix b) {
			RangeMatrix result = new RangeMatrix(a);
			for (int i = 0; i < b.matrix.GetLength(0); i++) {
				for (int j = 0; j < b.matrix.GetLength(1); j++) {
					if (b.matrix[i, j]) {
						result.matrix[i, j] = false;
					}
				}
			}
			return result;
		}


	}
}