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

		[OnDeserialized]
		private void SetValuesOnDeserialized(StreamingContext context) {
			List<Facility> removed = new List<Facility>();
			foreach(var f in facilities) {
				foreach(var p in f.GetAllPoints()) {
					if(this[p] != f) {
						removed.Add(f);
						Console.Out.WriteLine("Broken facility found");
					}
				}
			}
			foreach(var r in removed) {
				facilities.Remove(r);
			}
		}

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
			if (this[p] != null) {
				throw new Exception("already exists grid element");
			}

			GridElement t = elementType.Create();
			t.GridPoint = p;
			if (t.GetAllPoints().Count(gp=>this[gp]!=null) == 0) {
				foreach(var gp in t.GetAllPoints()) {
					this[gp] = t;
				}
				if (t is Facility) {
					facilities.Add((Facility)t);
				}
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

		/*
		public IEnumerable<Facility> SearchClosedFacilityDetailPoints(GridPoint detailPoint, int limitCount) {
//			Console.Out.WriteLine("SearchClosedFacilityDetailPoints");
			SortedList <int, Facility> closedList = new SortedList<int, Facility>();
			foreach (var facility in facilities) {
				foreach(var gridPointDistance in facility.GetAllPoints().SelectMany(p=>p.GetDetailsFromVillage())
					.Select(p=>new GridPointDistance() { GP = p, SqrDistance = p.SqrDistance(detailPoint) }).WhereMin(d=>d.SqrDistance)) {
//					Console.Out.WriteLine("facility:"+facility.GetAllPoints().Count()+" sqrDistance:"+gridPointDistance.SqrDistance);
					if (closedList.ContainsKey(gridPointDistance.SqrDistance) == false) {
						closedList.Add(gridPointDistance.SqrDistance, facility);
					}
					else {
						Console.Out.WriteLine("等距離の施設を検出。計算結果が変わる可能性があります (" + closedList[gridPointDistance.SqrDistance].FacilityID + "==" + facility.FacilityID + ")");
					}
					if (closedList.Count > limitCount) {
						closedList.Remove(closedList.Keys.Last());
					}
				}
			}

			foreach (var f in closedList) {
				Console.Out.Write(" " + f.Value.FacilityID + "(" + Math.Sqrt(f.Key) + ")");
			}
			Console.Out.WriteLine("");


			return closedList.Values;
		}
		*/

		public IEnumerable<Facility> SearchClosedFacility(ElementPoint elementPoint, int limitCount) {
			//			Console.Out.WriteLine("SearchClosedFacilityDetailPoints");
			SortedList<float, List<Facility>> closedList = new SortedList<float, List<Facility>>();
			foreach (var facility in facilities) {
				foreach (var elementPointDistance in facility.GetAllPoints().Select(p => p.GetElementPointFromVillage())
					.Select(p => new Tuple<ElementPoint,float>(p, p.SqrDistance(elementPoint) )).WhereMin(d => d.Item2)) {
					//					Console.Out.WriteLine("facility:"+facility.GetAllPoints().Count()+" sqrDistance:"+gridPointDistance.SqrDistance);
					if (closedList.ContainsKey(elementPointDistance.Item2) == false) {
						closedList.Add(elementPointDistance.Item2, new List<Facility>(new Facility[] { facility }));
					} else {
						closedList[elementPointDistance.Item2].Add(facility);
					}
					if (closedList.Count > limitCount) {
						closedList.Remove(closedList.Keys.Last());
					}
				}
			}

			foreach (var c in closedList) {
				if(c.Value.Count()>=2) {
					Console.Out.Write(" 等距離の施設を検出。計算結果が変わる可能性があります");
				}
				foreach (var f in c.Value) {
					Console.Out.Write(" " + f.FacilityID+"(" + c.Key + ")");
				}
			}
			Console.Out.WriteLine("");

			return closedList.Values.SelectMany(l=>l);
		}
		/*
		public IEnumerable<Facility> SearchClosedFacility(ElementPoint elementPoint, int limitCount) {
			SortedList<float, Facility> closedList = new SortedList<float, Facility>();
			foreach (var facility in facilities) {
				var sqrDistance = facility.ElementPoint.SqrDistance(elementPoint);

				if (closedList.ContainsKey(sqrDistance) == false) {
					closedList.Add(sqrDistance, facility);
				} else {
					Console.Out.WriteLine("等距離の施設を検出。計算結果が変わる可能性があります ("+ closedList[sqrDistance].FacilityID+"=="+ facility.FacilityID+")");
				}
				if (closedList.Count > limitCount) {
					closedList.Remove(closedList.Keys.Last());
				}
			}
			foreach(var f in closedList) {
				Console.Out.Write(" " + f.Value.FacilityID+"("+Math.Sqrt(f.Key)+")");
			}
			Console.Out.WriteLine("");

			return closedList.Values;
		}
		*/

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


		public ElementPoint GetElementPointFromVillage() {
			return new ElementPoint(X + 0.5f, Y + 0.5f);
		}


		public ElementPoint GetElementPointFromDetail() {
			return new ElementPoint(X * 0.5f + 0.25f, Y * 0.5f + 0.25f);
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

	public struct ElementPoint {
		public float X;
		public float Y;

		public ElementPoint(float x, float y) {
			X = x;
			Y = y;
		}

		public float SqrDistance(ElementPoint p) {
			float dx = p.X - X;
			float dy = p.Y - Y;
			return dx * dx + dy * dy;
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

		public ElementPoint ElementPoint {
			get {
				ElementPoint result;
				result.X = gridPoint.X + (float) this.Type.SizeX / 2;
				result.Y = gridPoint.Y + (float) this.Type.SizeY / 2;
				return result;
			}
		}

		public GridPoint GetCenterDetailPoint() {
			return new GridPoint(gridPoint.X * 2 + this.Type.SizeX, gridPoint.Y * 2 + this.Type.SizeY);
		}

		public abstract GridElementType Type {
			get;
		}

		public IEnumerable<GridPoint> GetAllPoints() {
			return this.Type.GetAllPoints(gridPoint);
		}

		public IEnumerable<GridPoint> GetAllRangeDetailPoints(int range) {
			var aqRangeMatrix = this.Type.AQRangeMatrix;
			if (aqRangeMatrix == null) {
				HashSet<GridPoint> result = new HashSet<GridPoint>();
				foreach (var vp in GetAllPoints()) {
					foreach (var dp in new RangeMatrix(range, 0.5f, 0.5f).GetPointsOriginMinus0505()) {
						result.Add(new GridPoint(vp.X * 2 + 1, vp.Y * 2 + 1) + dp);
					}

				}
				foreach (var e in result) {
					yield return e;
				}
			} else {
				var centerDP = GetCenterDetailPoint();
				for (int x = 0; x < aqRangeMatrix.GetLength(0); x++) {
					for (int y = 0; y < aqRangeMatrix.GetLength(1); y++) {
						if (aqRangeMatrix[x, y] != 0) {
							GridPoint result;
							result = centerDP;
							result.X += x;
							result.Y += y;
							yield return result;
							result = centerDP;
							result.X -= x + 1;
							result.Y += y;
							yield return result;
							result = centerDP;
							result.X += x;
							result.Y -= y + 1;
							yield return result;
							result = centerDP;
							result.X -= x + 1;
							result.Y -= y + 1;
							yield return result;
						}
					}
				}
			}
		}
	}

	[Serializable]
	public class GridElement<T> : GridElement where T : GridElement<T>, new() {
//		[NonSerialized]
		private GridElementType<T> gridElementType;

		private String typeId;


		[OnDeserialized]
		private void SetValuesOnDeserialized(StreamingContext context) {
			if(typeId == null) {
				if (gridElementType is WallType) {
					gridElementType = AQSimulator.GridElementType.TypeDictionary["Wall"] as GridElementType<T>;
				}
				if(gridElementType is CommonFacilityType) {
					if (gridElementType.SizeX == 2) {
						gridElementType = AQSimulator.GridElementType.TypeDictionary["2x2"] as GridElementType<T>;
					}
					if (gridElementType.SizeX == 3) {
						gridElementType = AQSimulator.GridElementType.TypeDictionary["3x3"] as GridElementType<T>;
					}
					if (gridElementType.SizeX == 4) {
						gridElementType = AQSimulator.GridElementType.TypeDictionary["4x4"] as GridElementType<T>;
					}
				}
			}
			else {
				gridElementType = AQSimulator.GridElementType.TypeDictionary[typeId] as GridElementType<T>;
			}
		}

		public override GridElementType Type {
			get {
				return gridElementType;
			}
		}

		internal GridElementType<T> GridElementType {
			set {
				gridElementType = value;
				typeId = gridElementType.TypeId;
			}
		}
	}

	[Serializable]
	public class Wall : GridElement<Wall> {
	}


	[Serializable]
	public class Facility : GridElement<Facility> {
		private static int idCounter = 1;

		public Facility() {
			FacilityID = idCounter++;
		}


		[OnDeserialized]
		private void SetValuesOnDeserialized(StreamingContext context) {
			FacilityID = idCounter++;
		}

		[NonSerialized]
		public int FacilityID;
	}

	[Serializable]
	public abstract class GridElementType {
		[NonSerialized]
		public static Dictionary<string, GridElementType> TypeDictionary = new Dictionary<string, GridElementType>();

		private String typeId;

		protected int sizeX;
		protected int sizeY;
		protected int[,] aqRangeMatrix;

		public GridElementType(String id) {
			typeId = id;
			TypeDictionary[id] = this;
		}

		public String TypeId {
			get {
				return typeId;
			}
		}

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

		public int[,] AQRangeMatrix {
			get {
				return aqRangeMatrix;
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
		public GridElementType(String id) : base(id) {
		}

		public override GridElement Create() {
			return new T() { GridElementType = this };
		}
	}

	[Serializable]
	public class WallType : GridElementType<Wall> {
		public static WallType Instance = new WallType();

		private WallType() : base("Wall") {
			sizeX = 1;
			sizeY = 1;
		}
	}

	[Serializable]
	public class CommonFacilityType : GridElementType<Facility> {
		public static CommonFacilityType Instance2x2 = new CommonFacilityType("2x2", 2,2,
			new int[,] {
				{2,2, 1,1, 1,1, 1,1, 1,1, 1,0,},
				{2,2, 1,1, 1,1, 1,1, 1,1, 1,0,},

				{1,1, 1,1, 1,1, 1,1, 1,1, 1,0,},
				{1,1, 1,1, 1,1, 1,1, 1,1, 1,0,},

				{1,1, 1,1, 1,1, 1,1, 1,1, 0,0,},
				{1,1, 1,1, 1,1, 1,1, 1,1, 0,0,},

				{1,1, 1,1, 1,1, 1,1, 1,1, 0,0,},
				{1,1, 1,1, 1,1, 1,1, 1,0, 0,0,},

				{1,1, 1,1, 1,1, 1,1, 0,0, 0,0,},
				{1,1, 1,1, 1,1, 1,0, 0,0, 0,0,},

				{1,1, 1,1, 0,0, 0,0, 0,0, 0,0,},
				{0,0, 0,0, 0,0, 0,0, 0,0, 0,0,},
			}
		);

		public static CommonFacilityType Instance3x3 = new CommonFacilityType("3x3", 3,3,
			new int[,] {
				{2,2,2, 1,1, 1,1, 1,1, 1,1, 1,1,},
				{2,2,2, 1,1, 1,1, 1,1, 1,1, 1,0,},
				{2,2,2, 1,1, 1,1, 1,1, 1,1, 1,0,},

				{1,1,1, 1,1, 1,1, 1,1, 1,1, 1,0,},
				{1,1,1, 1,1, 1,1, 1,1, 1,1, 1,0,},

				{1,1,1, 1,1, 1,1, 1,1, 1,1, 0,0,},
				{1,1,1, 1,1, 1,1, 1,1, 1,1, 0,0,},

				{1,1,1, 1,1, 1,1, 1,1, 1,1, 0,0,},
				{1,1,1, 1,1, 1,1, 1,1, 1,0, 0,0,},

				{1,1,1, 1,1, 1,1, 1,1, 0,0, 0,0,},
				{1,1,1, 1,1, 1,1, 1,0, 0,0, 0,0,},

				{1,1,1, 1,1, 0,0, 0,0, 0,0, 0,0,},
				{1,0,0, 0,0, 0,0, 0,0, 0,0, 0,0,},
			}
			);

		public static CommonFacilityType InstanceEagle = new CommonFacilityType("Eagle", 4, 4,
			new int[,] {
				{2,2 ,2,2, 1,1, 1,1, 1,1, 1,1, },
				{2,2 ,2,2, 1,1, 1,1, 1,1, 1,1, },

				{2,2 ,2,2, 1,1, 1,1, 1,1, 1,1, },
				{2,2 ,2,2, 1,1, 1,1, 1,1, 1,1, },

				{1,1, 1,1, 1,1, 1,1, 1,1, 1,0, },
				{1,1, 1,1, 1,1, 1,1, 1,1, 1,0, },

				{1,1, 1,1, 1,1, 1,1, 1,1, 1,0, },
				{1,1, 1,1, 1,1, 1,1, 1,1, 0,0, },

				{1,1, 1,1, 1,1, 1,1, 1,1, 0,0, },
				{1,1, 1,1, 1,1, 1,1, 1,0, 0,0, },

				{1,1, 1,1, 1,1, 1,0, 0,0, 0,0, },
				{1,1, 1,1, 0,0, 0,0, 0,0, 0,0, },
			}
			);

		public static CommonFacilityType Instance4x4 = new CommonFacilityType("4x4", 4, 4);
		public static CommonFacilityType Instance5x5 = new CommonFacilityType("5x5", 5,5);
		
		private CommonFacilityType(String id, int wx, int wy, int[,] aqRangeMatrix = null) : base(id) {
			sizeX = wx;
			sizeY = wy;
			this.aqRangeMatrix = aqRangeMatrix;
		}

	}
}