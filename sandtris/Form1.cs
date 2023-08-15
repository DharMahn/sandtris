namespace sandtris
{
    public partial class Form1 : Form
    {
        struct Cell
        {
            public Color Color;
            public byte ID;
        }
        List<bool[,]> tetrominoShapes = new List<bool[,]>
        {
            new bool[,] { { true, true, true, true } }, // I
            new bool[,] { { true, true }, { true, true } }, // O
            new bool[,] { { true, true, false }, { false, true, true } }, // S
            new bool[,] { { false, true, true }, { true, true, false } }, // Z
            new bool[,] { { true, false, false }, { true, true, true } }, // L
            new bool[,] { { false, false, true }, { true, true, true } }, // J
            new bool[,] { { true, true, true }, { false, true, false } } // T
        };
        List<Color> palette = new List<Color>()
        {
            Color.Transparent,
            Color.Blue,
            Color.Red,
            Color.Green,
            Color.Purple,
            Color.Wheat,
            Color.DarkGray,
        };
        Bitmap bmp;
        const int CELL_SIZE = 16;
        Cell[,] map;
        static Random r = new Random();
        public void SetCell(int x, int y, byte id, Color color)
        {
            if (x < 0 || y < 0 || x >= map.GetLength(0) || y >= map.GetLength(1))
            {
                return;
            }
            map[x, y].ID = id;
            map[x, y].Color = color;
            bmp.SetPixel(x, y, color);
        }
        public Form1()
        {
            InitializeComponent();
            map = new Cell[10 * CELL_SIZE, 10 * CELL_SIZE];
            for (int y = 0; y < map.GetLength(1); y++)
            {
                for (int x = 0; x < map.GetLength(0); x++)
                {
                    map[x, y] = new Cell
                    {
                        Color = Color.Transparent,
                        ID = 0,
                    };
                }
            }
            bmp = new Bitmap(map.GetLength(0), map.GetLength(1));
            //SetCell(10, 10, 1, Color.Black);
            Width = bmp.Width * uiScale;
            Height = bmp.Height * uiScale;
            DoubleBuffered = true;
        }

        int uiScale = 4;
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Application.Exit();
            }
            if (e.KeyCode == Keys.F)
            {
                SpawnTetromino(tetrominoShapes[r.Next(tetrominoShapes.Count)]);
            }
        }

        void SpawnTetromino(bool[,] shape)
        {
            int xOffset = (map.GetLength(0) / CELL_SIZE / 2) - 1;
            byte randomNum = (byte)r.Next(1,palette.Count);

            for (int y = 0; y < shape.GetLength(1); y++)
            {
                for (int x = 0; x < shape.GetLength(0); x++)
                {
                    if (shape[x,y])
                    {
                        for (int y1 = 0; y1 < CELL_SIZE; y1++)
                        {
                            for (int x1 = 0; x1 < CELL_SIZE; x1++)
                            {
                                SetCell(((x+xOffset) * CELL_SIZE) + x1, (y * CELL_SIZE) + y1, randomNum, palette[randomNum]);
                            }
                        }
                    }
                }
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.DrawImage(bmp, 0, 0, Width, Height);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            for (int y = map.GetLength(1) - 2; y >= 0; y--)
            {
                for (int x = 0; x < map.GetLength(0); x++)
                {
                    #region Movement
                    if (map[x, y].ID > 0) //if i exist
                    {
                        if (map[x, y + 1].ID == 0) //if below is empty - fall
                        {
                            SetCell(x, y + 1, map[x, y].ID, map[x, y].Color);
                            SetCell(x, y, 0, Color.Transparent);
                        }
                        else
                        {
                            if (x == 0) //left border - can only go right
                            {
                                if (map[x + 1, y + 1].ID == 0)
                                {
                                    SetCell(x + 1, y + 1, map[x, y].ID, map[x, y].Color);
                                    SetCell(x, y, 0, Color.Transparent);
                                }
                            }
                            else if (x == map.GetLength(0) - 1) //right border - can only go left
                            {
                                if (map[x - 1, y + 1].ID == 0)
                                {
                                    SetCell(x - 1, y + 1, map[x, y].ID, map[x, y].Color);
                                    SetCell(x, y, 0, Color.Transparent);
                                }
                            }
                            else //otherwise randomly choose a direction
                            {
                                if (r.NextDouble() >= 0.5)
                                {
                                    if (map[x + 1, y + 1].ID == 0)
                                    {
                                        SetCell(x + 1, y + 1, map[x, y].ID, map[x, y].Color);
                                        SetCell(x, y, 0, Color.Transparent);
                                    }
                                }
                                else
                                {
                                    if (map[x - 1, y + 1].ID == 0)
                                    {
                                        SetCell(x - 1, y + 1, map[x, y].ID, map[x, y].Color);
                                        SetCell(x, y, 0, Color.Transparent);
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                }
            }
            #region BFS Line Connection Check
            bool[,] visited = new bool[map.GetLength(0), map.GetLength(1)];
            Queue<(int, int)> queue = new Queue<(int, int)>();

            for (int y = 0; y < map.GetLength(1); y++)
            {
                if (map[0, y].ID > 0) // Starting from leftmost edge
                {
                    List<(int, int)> currentCells = new List<(int, int)>();
                    bool reachedRightEdge = false;
                    int startingID = map[0, y].ID;  // ID of the cell on the leftmost edge

                    queue.Enqueue((0, y));
                    while (queue.Count > 0)
                    {
                        var (cx, cy) = queue.Dequeue();
                        if (visited[cx, cy] || map[cx, cy].ID != startingID) continue;  // Ensure only cells with the same ID are processed

                        visited[cx, cy] = true;
                        currentCells.Add((cx, cy));

                        if (cx == map.GetLength(0) - 1)
                        {
                            reachedRightEdge = true;
                        }

                        // Check all 8 neighbors
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                int nx = cx + dx;
                                int ny = cy + dy;

                                if (nx >= 0 && nx < map.GetLength(0) && ny >= 0 && ny < map.GetLength(1) && !visited[nx, ny] && map[nx, ny].ID == startingID)
                                {
                                    queue.Enqueue((nx, ny));
                                }
                            }
                        }
                    }

                    if (reachedRightEdge)
                    {
                        foreach (var (cellX, cellY) in currentCells)
                        {
                            SetCell(cellX, cellY, 0, Color.Transparent);
                        }
                    }
                }
            }
            #endregion
            if (mLeft)
            {
                Point ptc = PointToClient(Cursor.Position);
                SetCell(ptc.X / uiScale, ptc.Y / uiScale, 1, Color.Red);
                //mLeft = false;
            }
            else if (mRight)
            {
                Point ptc = PointToClient(Cursor.Position);
                SetCell(ptc.X / uiScale, ptc.Y / uiScale, 2, Color.Black);
                //mRight = false;
            }
            Invalidate();
        }
        bool mLeft = false;
        bool mRight = false;
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                mRight = true;
            }
            else if (e.Button == MouseButtons.Left)
            {
                mLeft = true;
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                mRight = false;
            }
            else if (e.Button == MouseButtons.Left)
            {
                mLeft = false;
            }
        }
    }

}