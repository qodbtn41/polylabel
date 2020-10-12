using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolyLabel
{
    public class PolyLabel
    {
        public static PolyLabel _polyLabel = null;

        public static PolyLabel GetPolyLabel()
        {
            if (_polyLabel == null)
                _polyLabel = new PolyLabel();

            return _polyLabel;
        }

        private PolyLabel()
        {
        }

        Queue queue = new Queue();

        private float CompareMax(Cell a, Cell b)
        {
            return b.max - a.max;
        }

        Cell GetCentroidCell(List<PointF> polygon)
        {
            float area = 0;
            float x = 0;
            float y = 0;
            List<PointF> points = polygon;

            for (int i = 0, len = points.Count, j = len - 1; i < len; j = i++)
            {
                PointF a = points[i];
                PointF b = points[j];
                float f = a.X * b.Y - b.X * a.Y;
                x += (a.X + b.X) * f;
                y += (a.Y + b.Y) * f;
                area += f * 3;
            }
            if (area == 0)
                return new Cell(points[0].X, points[0].Y, 0, polygon);

            return new Cell(x / area, y / area, 0, polygon);
        }

        public PointF GetPolyLabel(List<PointF> polygon, float precision = 1.0f, bool debug = false)
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            for (int i = 0; i < polygon.Count; i++)
            {
                PointF p = polygon[i];
                if (i != 0 || p.X < minX) minX = p.X;
                if (i != 0 || p.Y < minY) minY = p.Y;
                if (i != 0 || p.X > maxX) maxX = p.X;
                if (i != 0 || p.Y > maxY) maxY = p.Y;
            }

            float width = maxX - minX;
            float height = maxY - minY;
            float cellSize = Math.Min(width, height);
            float h = cellSize / 2;

            Queue<Cell> cellQueue = new Queue<Cell>();
            //    var cellQueue = new Queue(null, compareMax);

            if (cellSize == 0)
                return new PointF(minX, minY);

            for (var x = minX; x < maxX; x += cellSize)
            {
                for (var y = minY; y < maxY; y += cellSize)
                {
                    cellQueue.Enqueue(new Cell(x + h, y + h, h, polygon));
                }
            }

            Cell bestCell = GetCentroidCell(polygon);

            Cell bBoxCell = new Cell(minX + width / 2, minY + height / 2, 0, polygon);
            if (bBoxCell.d > bestCell.d)
                bestCell = bBoxCell;

            int numProbes = cellQueue.Count;

            while (cellQueue.Count != 0) {
                // pick the most promising cell from the queue
                var cell = cellQueue.Dequeue();

                // update the best cell if we found a better one
                if (cell.d > bestCell.d) {
                    bestCell = cell;
                    if (debug)
                        Console.WriteLine("found best {0} after {1} probes", Math.Round(1e4 * cell.d) / 1e4, numProbes);
                }

                // do not drill down further if there's no chance of a better solution
                if (cell.max - bestCell.d <= precision)
                    continue;

                // split the cell into four cells
                h = cell.h / 2;
                cellQueue.Enqueue(new Cell(cell.x - h, cell.y - h, h, polygon));
                cellQueue.Enqueue(new Cell(cell.x + h, cell.y - h, h, polygon));
                cellQueue.Enqueue(new Cell(cell.x - h, cell.y + h, h, polygon));
                cellQueue.Enqueue(new Cell(cell.x + h, cell.y + h, h, polygon));
                numProbes += 4;
            }

            if (debug)
            {
                Console.WriteLine("num probes: " + numProbes);
                Console.WriteLine("best distance: " + bestCell.d);
            }

            return new PointF(bestCell.x, bestCell.y);
        }



        class Cell
        {
            public float x, y, h, d, max;
            public Cell(float x, float y, float h, List<PointF> polygon)
            {
                this.x = x;
                this.y = y;
                this.h = h;
                this.d = PointToPolygonDist(x, y, polygon);
                this.max = Convert.ToSingle(this.d + this.h * Math.Sqrt(2));
            }

            float PointToPolygonDist(float x, float y, List<PointF> polygon)
            {
                bool inside = false;
                float minDistSq = float.PositiveInfinity;

                for (int i = 0, len = polygon.Count, j = len - 1; i < len; j = i++)
                {
                    PointF a = polygon[i];
                    PointF b = polygon[j];

                    if ((a.Y > y != b.Y > y) && (x < (b.X - a.X) * (y - a.Y) / (b.Y - a.Y) + a.X))
                        inside = !inside;

                    minDistSq = Math.Min(minDistSq, GetSeqDistSq(x, y, a, b));
                }

                return Convert.ToSingle((inside ? 1 : -1) * Math.Sqrt(minDistSq));
            }

            float GetSeqDistSq(float px, float py, PointF a, PointF b)
            {
                float x = a.X;
                float y = a.Y;
                float dx = b.X - x;
                float dy = b.Y - y;

                if (dx != 0 || dy != 0)
                {

                    var t = ((px - x) * dx + (py - y) * dy) / (dx * dx + dy * dy);

                    if (t > 1)
                    {
                        x = b.X;
                        y = b.Y;

                    }
                    else if (t > 0)
                    {
                        x += dx * t;
                        y += dy * t;
                    }
                }

                dx = px - x;
                dy = py - y;

                return dx * dx + dy * dy;
            }
        }
    }
}
