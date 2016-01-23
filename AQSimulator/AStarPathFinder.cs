using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace AQSimulator {
	public class AStarPathFinder {

		public interface SearchTarget {
			int GetWalkCost(GridPoint pos);
			int SizeX {
				get;
			}
			int SizeY {
				get;
			}
		}

		private readonly GridPoint startPos;
		private readonly GridPoint goalPos;
		private readonly SearchTarget searchTarget;
		private readonly int limitCost;

		private List<GridPoint> path;
		private int pathCost;

		private static PosCostList sortedOpens = new PosCostList();
		private bool[,] opens;
		private bool[,] closed;
		private GridPoint[,] parents;
		private int[,] fstars;
		
		public AStarPathFinder(SearchTarget searchTarget, GridPoint goalPos, GridPoint startPos, int limitCost = int.MaxValue) {
			this.searchTarget = searchTarget;
			this.startPos = startPos;
			this.goalPos = goalPos;
			this.limitCost = limitCost;
			this.path = null;
			sortedOpens.Clear();

			Calculate();
		}



		private void Calculate() {
			int w = searchTarget.SizeX;
			int h = searchTarget.SizeY;
			opens = new bool[w,h];
			closed = new bool[w, h];
			parents = new GridPoint[w, h];
			fstars = new int[w,h];

			int fstar = GetHeuris(startPos);
			fstars[startPos.X,startPos.Y] = fstar;
			sortedOpens.Add(fstar, startPos);
			opens[startPos.X,startPos.Y] = true;
			GridPoint n;
			while (sortedOpens.PopLowest(out fstar, out n)) {
				int x = n.X;
				int y = n.Y;
				opens[x,y] = false;
				closed[x,y] = true;
				if (n.Equals(goalPos)) {
					CreatePath(n);
					pathCost = fstar;
					break;
				}
				int gstar = fstar - GetHeuris(n);

				if (y != 0) {
					if (x != 0) {
						Walk(x-1,y-1, n, 7, gstar);
					}
					Walk(x,y-1, n, 5, gstar);
					if (x != w - 1) {
						Walk(x+1,y-1, n, 7, gstar);
					}
				}
				if (x != 0) {
					Walk(x - 1,y, n, 5, gstar);
				}
				if (x != w - 1) {
					Walk(x + 1,y, n, 5, gstar);
				}
				if (y != h - 1) {
					if (x != 0) {
						Walk(x-1, y +1, n, 7, gstar);
					}
					Walk(x,y+1, n, 5, gstar);
					if (x != w - 1) {
						Walk(x+1,y+1, n, 7, gstar);
					}
				}
			}

			opens = null;
			closed = null;

			//if (path == null) {
			//	new Map(fstars, walkCostMap.Width, walkCostMap.Height).Dump("fstar.csv");
			//	new Map(parents, walkCostMap.Width, walkCostMap.Height).Dump("parents.csv");
			//	Map m = new Map(walkCostMap.Width, walkCostMap.Height);
			//	for (int i = 0; i < m.Height; i++) {
			//		for (int j = 0; j < m.Width; j++) {
			//			m[m.GetPosFromXY(j, i)] = m.GetPosFromXY(j, i);
			//		}
			//	}
			//	m.Dump("pos.csv");
			//	Debug.Log("##### route not found #### goal:"+goalPos+" start:"+startPos);
			//}
		}

		private void Walk(int mx,int my, GridPoint n, int cost, int gstar) {
			GridPoint m;
			m.X = mx;
			m.Y = my;
			int costM = searchTarget.GetWalkCost(m);
			if (costM == 0) {
				return;
			}

			int fdash = gstar + GetHeuris(m) + cost;
			if(fdash > limitCost) {
				return;
			}
			if (opens[mx,my] == false && closed[mx,my] == false) {
				sortedOpens.Add(fdash, m);
				fstars[mx,my] = fdash;
				opens[mx,my] = true;
				parents[mx, my] = n;
			} else if (opens[mx,my]) {
				if (fdash < fstars[mx, my]) {
					sortedOpens.Replace(fstars[mx, my], fdash, m);
					fstars[mx, my] = fdash;
					parents[mx, my] = n;
				}
			} else { // closed[m]==true
				if (fdash < fstars[mx, my]) {
					sortedOpens.Add(fdash, m);
					fstars[mx, my] = fdash;
					opens[mx, my] = true;
					closed[mx, my] = false;
					parents[mx, my] = n;
				}
			}
		}


		public List<GridPoint> Path {
			get {
				return path;
			}
		}

		public int Cost {
			get {
				return pathCost;
			}
		}

		private void CreatePath(GridPoint goal) {
			path = new List<GridPoint>();
			GridPoint n = goal;
			while (n.X != 0 || n.Y != 0) {
				path.Add(n);
				n = parents[n.X,n.Y];
			}
			path.Reverse();

		}

		private int GetHeuris(GridPoint pos) {
			int dx = Math.Abs(goalPos.X - pos.X);
			int dy = Math.Abs(goalPos.Y - pos.Y);
			if (dx > dy) {
				return dx * 5 + dy * 2; // (dx - dy) * 5 + dy * 7
			} else {
				return dy * 5 + dx * 2;
			}
		}

		public IEnumerator<GridPoint> PathEnumerator {
			get {
				if (path == null) {
					return new List<GridPoint>().GetEnumerator();
				} else {
					var result = path.GetEnumerator();
					result.MoveNext();
					return result;
				}
			}
		}


		private class PosCostList {

			private int min = int.MaxValue;
			private int count = 0;
			private List<GridPoint>[] costs = new List<GridPoint>[1000];

			public void Add(int score, GridPoint pos) {
				while (score >= costs.Length) {
					List<GridPoint>[] newCosts = new List<GridPoint>[costs.Length * 2];
					for (int i = 0; i < costs.Length; i++) {
						newCosts[i] = costs[i];
					}
					costs = newCosts;
				}

				List<GridPoint> list = costs[score];
				if (list == null) {
					costs[score] = list = new List<GridPoint>();
				}
				list.Add(pos);
				count++;
				min = Math.Min(min, score);
			}

			public void Replace(int oldScore, int newScore, GridPoint pos) {
				costs[oldScore].Remove(pos);
				count--;
				Add(newScore, pos);
			}

			public bool PopLowest(out int score, out GridPoint pos) {
				if (count == 0) {
					score = 0;
					pos = new GridPoint();
					return false;
				}

				while (true) {
					List<GridPoint> list = costs[min];
					if (list != null && list.Count >= 1) {
						pos = list[list.Count - 1];
						list.RemoveAt(list.Count - 1);
						score = min;
						count--;
						return true;
					}
					min++;
				}
			}

			public void Clear() {
				if (count == 0) {
					return;
				}
				while (true) {
					List<GridPoint> list = costs[min];
					if (list != null && list.Count >= 1) {
						count -= list.Count;
						list.Clear();
						if (count == 0) {
							break;
						}
					}
					min++;
				}
				min = int.MaxValue;
			}
		}


	}
}