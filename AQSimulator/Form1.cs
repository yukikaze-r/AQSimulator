using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Xml;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace AQSimulator {
	public partial class Form1 : Form {
		private Village village = new Village();
		private Simulator simulator = null;

		private double viewRadian = Math.PI * (30d / 180d);
		private double gridElementLength = 16;
		private Size gridElementSize;
		private Point originalPoint;


		public Form1() {
			InitializeComponent();
			gridElementSize.Width = (int)(gridElementLength * Math.Cos(viewRadian));
			gridElementSize.Height = (int)(gridElementLength * Math.Sin(viewRadian));
			originalPoint = new Point(village.SizeY * gridElementSize.Width, (village.SizeX + village.SizeY) * gridElementSize.Height);
		}

		private void Form1_Load(object sender, EventArgs e) {
			this.SetStyle(ControlStyles.DoubleBuffer, true);
			this.SetStyle(ControlStyles.UserPaint, true);
			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			village.OnGridChanged += Village_GridChanged;
		}

		private void Village_GridChanged(GridPoint gp) {
			Invalidate(GetMinMaxRectangle(GetGridElementGraphicsPoints(gp.X, gp.Y)));
		}

		private void Form1_Paint(object sender, PaintEventArgs e) {
			e.Graphics.Clear(Color.FromKnownColor(KnownColor.Black));
			/*
			foreach(var f in village.Facilities) {
				foreach (var vp in f.GetAllPoints()) {
					foreach (var dp in new RangeMatrix(10, 0.5f, 0.5f).GetPointsOriginMinus0505()) {
						DrawRange(e.Graphics, new GridPoint(vp.X * 2 + 1, vp.Y * 2 + 1) + dp);

					}
				}
			}*/


			GridElementType gridElementType = GetSelectedToolGridType();
			if (gridElementType != null) {
				Point cp = this.PointToClient(Cursor.Position);
				foreach (var p in gridElementType.GetAllPoints(GetVillagePointFromGraphicsPoint(cp))) {
					if (village.IsValidGridPoint(p)) {
						DrawCursor(e.Graphics, p.X, p.Y);
					}
				}
			}

			for (int x = 0; x < village.SizeX; x++) {
				for (int y = 0; y < village.SizeY; y++) {
					DrawGridElement(e.Graphics, x, y, village[x, y]);
				}
			}

			if(simulator != null) {
				DrawAQ(e.Graphics, simulator.AQDetailPoint);
			}
		}


		private void DrawRange(Graphics g, GridPoint detailPoint) {
			Point[] points = GetDetailGraphicsPoints(detailPoint.X, detailPoint.Y);
			g.FillPolygon(Brushes.Green, points);
		}

		private void DrawAQ(Graphics g, GridPoint aqDetailPoint) {
			Point[] points = GetDetailGraphicsPoints(aqDetailPoint.X, aqDetailPoint.Y);
			g.FillPolygon(Brushes.Red, points);
			Pen pen = new Pen(Color.FromKnownColor(KnownColor.White));
			g.DrawPolygon(pen, points);
			pen.Dispose();
		}

		private void DrawCursor(Graphics g, int x, int y) {
			Point[] points = GetGridElementGraphicsPoints(x, y);
			g.FillPolygon(Brushes.Gray, points);
			Pen pen = new Pen(Color.FromKnownColor(KnownColor.White));
			g.DrawPolygon(pen, points);
			pen.Dispose();
		}

		private void DrawGridElement(Graphics g, int x, int y, GridElement gridElement) {
			Point[] points = GetGridElementGraphicsPoints(x, y);
			if(gridElement != null) {
				g.FillPolygon(GetGridBrush(gridElement), points);
			}
			Pen pen = new Pen(Color.FromKnownColor(KnownColor.Gray));
			g.DrawPolygon(pen, points);
			pen.Dispose();
		}

		private Brush GetGridBrush(GridElement gridElement) {
			if(gridElement is Wall) {
				return Brushes.White;
			}
			if(gridElement is Facility) {
				return Brushes.Yellow;
			}
			throw new Exception();
		}

		private Point[] GetGridElementGraphicsPoints(int x, int y) {
			return new Point[] {
				GetGraphicsPointFromVillagePoint(new GridPoint(x,y)),
				GetGraphicsPointFromVillagePoint(new GridPoint(x+1,y)),
				GetGraphicsPointFromVillagePoint(new GridPoint(x+1,y+1)),
				GetGraphicsPointFromVillagePoint(new GridPoint(x,y+1)),
			};
		}

		private Point[] GetDetailGraphicsPoints(int x, int y) {
			return new Point[] {
				GetGraphicsPointFromDetailPoint(new GridPoint(x,y)),
				GetGraphicsPointFromDetailPoint(new GridPoint(x+1,y)),
				GetGraphicsPointFromDetailPoint(new GridPoint(x+1,y+1)),
				GetGraphicsPointFromDetailPoint(new GridPoint(x,y+1)),
			};
		}

		private Point GetGraphicsPointFromVillagePoint(GridPoint villagePoint) {
			return new Point(
				(villagePoint.X - villagePoint.Y) * gridElementSize.Width + originalPoint.X,
				(-villagePoint.X - villagePoint.Y) * gridElementSize.Height + originalPoint.Y);
		}

		private GridPoint GetVillagePointFromGraphicsPoint(Point graphicsPoint) {
			double xminusy = (graphicsPoint.X - originalPoint.X) / (double)gridElementSize.Width;
			double minusxminusy = (graphicsPoint.Y - originalPoint.Y) / (double)gridElementSize.Height;
			return new GridPoint((int)((xminusy - minusxminusy) / 2), (int)(-(xminusy + minusxminusy) / 2));
		}


		private Point GetGraphicsPointFromDetailPoint(GridPoint detailPoint) {
			return new Point(
				(detailPoint.X - detailPoint.Y) * gridElementSize.Width / 2 + originalPoint.X,
				(-detailPoint.X - detailPoint.Y) * gridElementSize.Height / 2 + originalPoint.Y);
		}

		private GridPoint GetDetailPointFromGraphicsPoint(Point graphicsPoint) {
			double xminusy = (graphicsPoint.X - originalPoint.X) / ((double)gridElementSize.Width / 2);
			double minusxminusy = (graphicsPoint.Y - originalPoint.Y) / ((double)gridElementSize.Height / 2);
			return new GridPoint((int)((xminusy - minusxminusy) / 2), (int)(-(xminusy + minusxminusy) / 2));
		}

		private void Form1_MouseClick(object sender, MouseEventArgs e) {
			GridPoint vp = GetVillagePointFromGraphicsPoint(new Point(e.X, e.Y));
			if(village.IsValidGridPoint(vp)) {
				if(aqButton.Checked) {
					simulator = new Simulator(village, GetDetailPointFromGraphicsPoint(new Point(e.X, e.Y)));
					Invalidate(GetMinMaxRectangle(GetDetailGraphicsPoints(simulator.AQDetailPoint.X, simulator.AQDetailPoint.Y)));
				}
				else {
					GridElement element;
					if (village[vp.X, vp.Y] != null) {
						element = village.Destroy(village[vp]);
					}
					else {
						element = village.Build(vp, GetSelectedToolGridType());
					}
//					InvalidateGrids(element.Type, element.GridPoint);
				}
			}
		}

		private Rectangle GetMinMaxRectangle(IEnumerable<Point> points) {
			int sx = int.MaxValue;
			int sy = int.MaxValue;
			int ex = 0;
			int ey = 0;
			foreach(Point p in points) {
				if (p.X < sx) {
					sx = p.X;
				}
				if (p.Y < sy) {
					sy = p.Y;
				}
				if(ex < p.X) {
					ex = p.X;
				}
				if (ey < p.Y) {
					ey = p.Y;
				}
			}
			return new Rectangle(sx, sy, ex - sx, ey - sy);
		}
		
		private GridElementType GetSelectedToolGridType() {
			if (wallButton.Checked) {
				return WallType.Instance;
			}
			if (facility2x2Button.Checked) {
				return CommonFacilityType.Instance2x2;
			}
			if (facility3x3Button.Checked) {
				return CommonFacilityType.Instance3x3;
			}
			if (facility4x4Button.Checked) {
				return CommonFacilityType.Instance4x4;
			}
			if (facility5x5Button.Checked) {
				return CommonFacilityType.Instance5x5;
			}
			return null;
		}

		private void InvalidateGrids(GridElementType gridElementType, GridPoint point) {
			Invalidate(GetMinMaxRectangle(gridElementType.GetAllPoints(point).SelectMany(gp => GetGridElementGraphicsPoints(gp.X, gp.Y))));
		}

		private Point oldMouseLocation;

		private void Form1_MouseMove(object sender, MouseEventArgs e) {
			GridPoint gp1 = GetVillagePointFromGraphicsPoint(e.Location);
			GridPoint gp2 = GetVillagePointFromGraphicsPoint(oldMouseLocation);

			GridElementType gridElementType = GetSelectedToolGridType();
			if(gridElementType != null && !gp1.Equals(gp2)) {
				InvalidateGrids(gridElementType, gp1);
				InvalidateGrids(gridElementType, gp2);
			}
			oldMouseLocation = e.Location;
		}

		private void stepButton_Click(object sender, EventArgs e) {
			if(simulator != null) {
				GridPoint oldAQPoint = simulator.AQDetailPoint;
				simulator.DoTick();
				if(oldAQPoint.Equals(simulator.AQDetailPoint) == false) {
					Invalidate(GetMinMaxRectangle(GetDetailGraphicsPoints(oldAQPoint.X, oldAQPoint.Y)));
					Invalidate(GetMinMaxRectangle(GetDetailGraphicsPoints(simulator.AQDetailPoint.X, simulator.AQDetailPoint.Y)));
				}
				oldAQPoint = simulator.AQDetailPoint;
			}
		}

		private void SaveFileToolStripMenuItem_Click(object sender, EventArgs e) {
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.FileName = "新しいファイル.village";
			sfd.Filter =
				"villageファイル(*.village)|*.village|すべてのファイル(*.*)|*.*";
			sfd.FilterIndex = 1;
			sfd.Title = "保存先のファイルを選択してください";
			sfd.RestoreDirectory = true;
			sfd.OverwritePrompt = true;
			sfd.CheckPathExists = true;
			if (sfd.ShowDialog() == DialogResult.OK) {
				using (Stream stream = File.OpenWrite(sfd.FileName)) {
					BinaryFormatter formatter = new BinaryFormatter();
					formatter.Serialize(stream, village);
				}
			}
		}

		private void OpenFileToolStripMenuItem_Click(object sender, EventArgs e) {
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.FileName = "default.html";
			ofd.Filter =
				"villageファイル(*.village)|*.village|すべてのファイル(*.*)|*.*";
			ofd.FilterIndex = 1;
			ofd.Title = "開くファイルを選択してください";
			ofd.RestoreDirectory = true;
			ofd.CheckFileExists = true;
			ofd.CheckPathExists = true;

			if (ofd.ShowDialog() == DialogResult.OK) {

				using (Stream stream = File.OpenRead(ofd.FileName)) {
					BinaryFormatter formatter = new BinaryFormatter();

					village.OnGridChanged -= Village_GridChanged;
					village = (Village) formatter.Deserialize(stream);
					village.OnGridChanged += Village_GridChanged;
					Invalidate();
				}

			}
		}
	}
}
