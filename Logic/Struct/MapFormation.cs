using LogicUnit.Interface;
using System.Collections.Generic;
using System.Linq;
using LogicUnit.Data;

namespace LogicUnit
{
    public class MapFormation : IMap
    {
        public int MinPlayer { get; private set; }
        public int MaxPlayer { get; private set; }
        public int MaxStep { get; private set; }
        public int Allow => 1;
        public int NotAllow => 0;
        internal DataBox DataBox { get; private set; }
        private int[,] entry = new int[,]
        {
            { 1,0,0,1,0,0,1 },
            { 0,0,1,1,1,0,0 },
            { 0,1,0,1,0,1,0 },
            { 1,1,1,0,1,1,1 },
            { 0,1,0,1,0,1,0 },
            { 0,0,1,1,1,0,0 },
            { 1,0,0,1,0,0,1 }
        };
        private int[,] scores = new int[,]
        {
            { 2,0,0,4,0,0,2 },
            { 0,0,2,2,2,0,0 },
            { 0,4,0,4,0,4,0 },
            { 2,2,3,0,3,2,2 },
            { 0,4,0,4,0,4,0 },
            { 0,0,2,2,2,0,0 },
            { 2,0,0,4,0,0,2 }
        };
        
        private List<Point3Line> p3LineList;
        private List<Point3Line> P3LineList
        {
            get
            {
                if (p3LineList == null)
                {
                    int[,] points = new int[,]
                    { //Horizontal
                        {0,0, 3,0, 6,0 },{0,3, 1,3, 2,3 },{0,6, 3,6, 6,6 },{1,2, 3,2, 5,2 },
                        {1,4, 3,4, 5,4 },{2,1, 3,1, 4,1 },{2,5, 3,5, 4,5 },{4,3, 5,3, 6,3 },
                        //Vertical
                        {0,0, 0,3, 0,6 },{1,2, 1,3, 1,4 },{3,0, 3,1, 3,2 },
                        {3,4, 3,5, 3,6 },{5,2, 5,3, 5,4 },{6,0, 6,3, 6,6 },
                        //Declining
                        {1,2, 2,3, 3,4 },{1,4, 2,5, 3,6 },{3,0, 4,1, 5,2 },{3,2, 4,3, 5,4 },//-1
                        {1,2, 2,1, 3,0 },{1,4, 2,3, 3,2 },{3,4, 4,3, 5,2 },{3,6, 4,5, 5,4 },//1
                    };
                    p3LineList = new List<Point3Line>();
                    for (int i = 0; i < points.GetLength(0); ++i)
                    {
                        Point3Line xy3Line = new Point3Line()
                        {
                            X1 = points[i, 0],
                            Y1 = points[i, 1],
                            X2 = points[i, 2],
                            Y2 = points[i, 3],
                            X3 = points[i, 4],
                            Y3 = points[i, 5]
                        };
                        p3LineList.Add(xy3Line);
                    }
                }
                return p3LineList;
            }
        }

        public MapFormation(int minPlayer, int maxPlayer, int maxStep)
        {
            this.MinPlayer = minPlayer;
            this.MaxPlayer = maxPlayer;
            this.MaxStep = maxStep;
            DataBox = new DataBox(EntryCopy(), Allow, NotAllow);
        }
        
        public int[,] EntryCopy()
        {
            return ArrayHelper.CopyMatrix(entry);
        }

        public IEnumerable<Point3Line> LinesOf(IntPoint p)
        {
            return P3LineList.Where(line => line.IsInLine(p.X, p.Y));
        }

        public IEnumerable<Point3Line> LinesOf(int x, int y)
        {
            return P3LineList.Where(line => line.IsInLine(x, y));
        }

        public IEnumerable<Point3Line> Lines()
        {
            return P3LineList;
        }

        public int[,] CurrentData()
        {
            return DataBox.Copy();
        }
    }
}
