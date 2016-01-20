using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Xml;

namespace AQSimulator {
	[Serializable]
	public class Village : ReachableMap.SearchTarget {
		private int sizeX = 50;
		private int sizeY = 50;

		private GridElement[,] grid;

		private List<Facility> facilities = new List<Facility>();

		public Village() {
			grid = new GridElement[sizeX, sizeY];
		}

		[field: NonSerialized]
		public event Action<GridPoint> OnGridChanged = delegate { };

		public int SizeX {
			get {
				return sizeX;
			}
		}

		public int SizeY {
			get {
				return sizeY;
			}
		}

		public GridElement this[int x, int y] {
			set {
				grid[x, y] = value;
				OnGridChanged(new GridPoint(x, y));
			}
			get {
				return grid[x, y];
			}
		}


		public GridElement this[GridPoint p] {
			set {
				grid[p.X, p.Y] = value;
				OnGridChanged(p);
			}
			get {
				return grid[p.X, p.Y];
			}
		}

		public IEnumerable<Facility> Facilities {
			get {
				return facilities;
			}
		}

		public bool IsWalkable(GridPoint p) {
			return this[p] == null;
		}

		public int GetWalkCost(GridPoint p) {
			if(this[p] == null) {
				return 1;
			}
			else {
				return 0;
			}
		}

		public GridElement Build(GridPoint p, GridElementType elementType) {
			GridElement t = elementType.Create();
			t.GridPoint = p;
			if (t.GetAllPoints().Count(gp=>this[gp]!=null) == 0) {
				foreach(var gp in t.GetAllPoints()) {
					this[gp] = t;
				}
			}
			if(t is Facility) {
				facilities.Add((Facility)t);
			}
			return t;
		}

		public GridElement Destroy(GridElement elem) {
			GridPoint p = elem.GridPoint;
			GridElementType elementType = elem.Type;
			for (int x = p.X; x < p.X + elementType.SizeX; x++) {
				for (int y = p.Y; y < p.Y + elementType.SizeY; y++) {
					this[x, y] = null;
				}
			}
			if(elem is Facility) {
				facilities.Remove((Facility)elem);
			}

			return elem;
		}

		public IEnumerable<GridPoint> SearchClosedFacilityDetailPoints(GridPoint detailPoint, int limitCount) {
			SortedList<int, GridPoint> closedList = new SortedList<int, GridPoint>();
			foreach (var facility in facilities) {
				foreach(var gridPointDistance in facility.GetAllPoints().SelectMany(p=>p.GetDetailsFromVillage())
					.Select(p=>new GridPointDistance() { GP = p, SqrDistance = p.SqrDistance(detailPoint) }).WhereMin(d=>d.SqrDistance)) {
					if (closedList.ContainsKey(gridPointDistance.SqrDistance) == false) {
						closedList.Add(gridPointDistance.SqrDistance, gridPointDistance.GP);
					}
					else {
						Console.Error.WriteLine("等距離の施設を検出。計算結果が変わる可能性があります");
					}
					if(closedList.Count > limitCount) {
						closedList.Remove(closedList.Keys.Last());
					}
				}
			}
			return closedList.Values;
		}

		private struct GridPointDistance {
			public GridPoint GP;
			public int SqrDistance;
		}

		public ReachableMap GetReachableMap() {
			return new ReachableMap(this);
		}

		public bool IsValidGridPoint(GridPoint p) {
			return 0 <= p.X && p.X < this.SizeX && 0 <= p.Y && p.Y < this.SizeY;
		}

		public DetailMap GetDetailMap() {
			return new DetailMap(this);
		}

		public class DetailMap : AStarPathFinder.SearchTarget {
			private Village village;

			public DetailMap(Village village) {
				this.village = village;
			}

			public int SizeX {
				get {
					return village.SizeX * 2;
				}
			}

			public int SizeY {
				get {
					return village.SizeY * 2;
				}
			}

			public int GetWalkCost(GridPoint pos) {
				return village.GetWalkCost(new GridPoint(pos.X / 2, pos.Y / 2));
			}
		}
	}
	
	[Serializable]
	public struct GridPoint {
		public int X;
		public int Y;

		public GridPoint(int x, int y) {
			X = x;
			Y = y;
		}

		public int SqrDistance(GridPoint p) {
			int dx = p.X - X;
			int dy = p.Y - Y;
			return dx * dx + dy * dy;
		}

		public IEnumerable<GridPoint> GetDetailsFromVillage() {
			for(int x=0; x<2; x++) {
				for(int y=0; y<2; y++) {
					yield return new GridPoint(X * 2 + x, Y * 2 + y);
				}
			}
		}

		public GridPoint GetVillageFromDetail() {
			return new GridPoint(X / 2, Y / 2);
		}

		public static GridPoint operator +(GridPoint a, GridPoint b) {
			GridPoint result;
			result.X = a.X + b.X;
			result.Y = a.Y + b.Y;
			return result;
		}
	}

	[Serializable]
	public abstract class GridElementType {
		protected int sizeX;
		protected int sizeY;

		public int SizeX {
			get {
				return sizeX;
			}
		}

		public int SizeY {
			get {
				return sizeY;
			}
		}

		public abstract GridElement Create();


		public IEnumerable<GridPoint> GetAllPoints(GridPoint gridPoint) {
			for (int i = 0; i < SizeX; i++) {
				for (int j = 0; j < SizeY; j++) {
					yield return new GridPoint(gridPoint.X + i, gridPoint.Y + j);
				}
			}
		}
	}

	[Serializable]
	public class GridElementType<T> : GridElementType where T : GridElement<T>, new() {
		public override GridElement Create() {
			return new T() { GridElementType = this };
		}
	}

	[Serializable]
	public abstract class GridElement {
		private GridPoint gridPoint;

		public GridPoint GridPoint {
			set {
				gridPoint = value;
			}
			get {
				return gridPoint;
			}
		}

		public abstract GridElementType Type {
			get;
		}

		public IEnumerable<GridPoint> GetAllPoints() {
			return this.Type.GetAllPoints(gridPoint);
		}

		public IEnumerable<GridPoint> GetAllRangeDetailPoints(int range) {
			HashSet<GridPoint> result = new HashSet<GridPoint>();
			foreach(var vp in GetAllPoints()) {
				foreach(var dp in new RangeMatrix(range,0.5f,0.5f).GetPointsOriginMinus0505()) {
					result.Add(new GridPoint(vp.X * 2 + 1, vp.Y * 2 + 1) + dp);
				}

			}
			return result;
		}
	}

	[Serializable]
	public class GridElement<T> : GridElement where T : GridElement<T>, new() {
		private GridElementType<T> gridElementType;

		public override GridElementType Type {
			get {
				return gridElementType;
			}
		}

		internal GridElementType<T> GridElementType {
			set {
				gridElementType = value;
			}
		}
	}

	[Serializable]
	public class Wall : GridElement<Wall> {
	}

	[Serializable]
	public class WallType : GridElementType<Wall> {
		public static WallType Instance = new WallType();

		private WallType() {
			sizeX = 1;
			sizeY = 1;
		}
	}

	[Serializable]
	public class Facility : GridElement<Facility> {
	}

	[Serializable]
	public class CommonFacilityType : GridElementType<Facility> {
		public static CommonFacilityType Instance2x2 = new CommonFacilityType(2,2);
		public static CommonFacilityType Instance3x3 = new CommonFacilityType(3,3);
		public static CommonFacilityType Instance4x4 = new CommonFacilityType(4,4);
		public static CommonFacilityType Instance5x5 = new CommonFacilityType(5,5);

		private CommonFacilityType(int wx, int wy) {
			sizeX = wx;
			sizeY = wy;
		}
	}
}
