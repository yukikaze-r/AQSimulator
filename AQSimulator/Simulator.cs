using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AQSimulator {
	class Simulator {
		private Village village;
		private GridPoint aqPoint;
		private int AQRange = 5 * 2;
		private IEnumerator<GridPoint> pathIt = null;
		private GridElement target = null;

		public Simulator(Village village, GridPoint aqPoint) {
			this.village = village;
			this.aqPoint = aqPoint;
			DoTick();
		}

		public GridPoint AQDetailPoint {
			get {
				return aqPoint;
			}
		}


		public GridPoint AQVillagePoint {
			get {
				return aqPoint.GetVillageFromDetail();
			}
		}

		public ElementPoint AQElementPoint {
			get {
				ElementPoint result;
				result.X = aqPoint.X * 0.5f + 0.25f;
				result.Y = aqPoint.Y * 0.5f + 0.25f;
				return result;
			}
		}


		public void DoTick() {
			if(pathIt == null || pathIt.MoveNext() == false) {
				if(target != null) {
					village.Destroy(target);
				}
				pathIt = CalculatePath();
				if (pathIt != null) {
					pathIt.MoveNext();
				}
			}

			if(pathIt != null) {
				this.aqPoint = pathIt.Current;
			}

			// 最短の3つの施設のうちもっとも近い施設から攻撃可能か調べ、攻撃可能な施設をタゲる
			// たぶん：ただし最短３施設がどれも移動可能エリアの場合、もっとも近い施設をタゲる
			// タゲったあとはもっとも近い射撃可能位置を探索（たぶん。距離が長すぎると打ち切り最短の壁をタゲ）
		}
		/*
		private IEnumerator<GridPoint> CalculatePath() {
			foreach (var fp in village.SearchClosedFacilityDetailPoints(aqPoint, 3).Select(dp=>dp.GetVillageFromDetail())) {
				IEnumerable<GridPoint> attackableArea = village[fp].GetAllRangeDetailPoints(AQRange).Intersect(
					village.GetReachableMap().GetAllReachable(this.AQVillagePoint).SelectMany(p=>p.GetDetailsFromVillage()));
				if (attackableArea.Count() >= 1) {
					target = village[fp];
					return attackableArea.Select(goalPos => new AStarPathFinder(village.GetDetailMap(), goalPos, aqPoint))
						.WhereMin(astar => astar.Cost).Select(astar=>astar.Path).First().GetEnumerator();
				}
			}
			Console.Out.WriteLine("最短3つは攻撃不可");
			return null; // 最短3つは攻撃不可
		}*/


		private IEnumerator<GridPoint> CalculatePath() {
			/*
			foreach (var facility in village.SearchClosedFacility(this.AQElementPoint, 3)) {
				IEnumerable<GridPoint> attackableArea = facility.GetAllRangeDetailPoints(AQRange).Intersect(
					village.GetReachableMap().GetAllReachable(this.AQVillagePoint).SelectMany(p => p.GetDetailsFromVillage()));
				if (attackableArea.Count() >= 1) {
					this.target = facility;
					return attackableArea.Select(goalPos => new AStarPathFinder(village.GetDetailMap(), goalPos, aqPoint))
						.WhereMin(astar => astar.Cost).Select(astar => astar.Path).First().GetEnumerator();
				}
			}
			Console.Out.WriteLine("最短3つは攻撃不可");
			return null; // 最短3つは攻撃不可*/
			var closedTuple = SeachClosedAStarPath().WhereMin(t => t.Item2.Cost).First();
			this.target = closedTuple.Item1;
			return closedTuple.Item2.Path.GetEnumerator();
		}

		private IEnumerable<Tuple<Facility,AStarPathFinder>> SeachClosedAStarPath() {
			foreach (var facility in village.SearchClosedFacilityDetailPoints(this.AQDetailPoint, 3)) {
				IEnumerable<GridPoint> attackableArea = facility.GetAllRangeDetailPoints(AQRange).Intersect(
					village.GetReachableMap().GetAllReachable(this.AQVillagePoint).SelectMany(p => p.GetDetailsFromVillage()));
				if (attackableArea.Count() >= 1) {
					this.target = facility;
					foreach(var astar in attackableArea.Select(goalPos => new AStarPathFinder(village.GetDetailMap(), goalPos, aqPoint))) {
						yield return Tuple.Create(facility,astar);
					}
				}
			}
		}
	}
}
