using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AQSimulator {
	class RangePoints {
		private int range;
		private float dx;
		private float dy;

		public RangePoints(int range, float dx, float dy) {
			this.range = range;
			this.dx = dx;
			this.dy = dy;
		}

		public IEnumerable<GridPoint> GetAll() {
			for (int x = -range; x <= range; x++) {
				for (int y = -range; y <= range; y++) {
					float px = x + dx;
					float py = y + dy;
					if (px * px + py * py <= range * range) {
						yield return new GridPoint(x, y);
					}
				}
			}
		}
	}
}
