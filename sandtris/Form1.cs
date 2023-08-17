using System.Collections;
using System.Diagnostics;
using System.Drawing.Design;
using System.Resources;

namespace sandtris
{
    public partial class Form1 : Form
    {
        struct Cell
        {
            public Color Color;
            public byte ID;
            public uint TetrominoID;
        }
        static uint currentTetrominoId;
        static byte currentTetrominoRotIndex;
        static uint lastTetrominoCollisionId;
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
        List<byte> tetrominoRotationIndices = new List<byte>
        {
            1,1,1,1,2,1,1
        };
        List<Color> palette = new List<Color>()
        {
            Color.Transparent,
            Color.Blue,
            Color.Red,
            Color.Green,
            //Color.Purple,
            Color.Wheat,
            //Color.DarkGray,
        };
        Bitmap bmp;
        const int TETROMINO_SIZE = 8;
        Cell[,] map;
        static Random r = new Random();
        public void SetCell(int x, int y, byte id, Color color, uint tetrominoId)
        {
            if (x < 0 || y < 0 || x >= map.GetLength(0) || y >= map.GetLength(1))
            {
                return;
            }
            map[x, y].ID = id;
            map[x, y].Color = color;
            map[x, y].TetrominoID = tetrominoId;

            bmp.SetPixel(x, y, color);
        }
        public Form1()
        {
            InitializeComponent();
            ResourceManager rm = new ResourceManager(typeof(Form1));

            patterns = rm.GetResourceSet(System.Globalization.CultureInfo.CurrentCulture, true, true)!
                         .Cast<DictionaryEntry>()
                         .Where(x => x.Value.GetType() == typeof(Bitmap))
                         .Select(x => x.Value)
                         .Cast<Bitmap>()
                         .ToList();
            ClearMap();
            SpawnTetromino();
            //SetCell(10, 10, 1, Color.Black);
            Width = bmp.Width * uiScale;
            Height = bmp.Height * uiScale;
            DoubleBuffered = true;
            
            Console.WriteLine("lol");
        }
        List<Bitmap> patterns;
        private void ClearMap()
        {
            map = new Cell[10 * TETROMINO_SIZE, 24 * TETROMINO_SIZE];
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
            lastTetrominoCollisionId = 0;
            currentTetrominoId = 0;
        }

        int uiScale = 4;
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Application.Exit();
            }
            if (e.KeyCode == Keys.A)
            {
                moveLeft = true;
            }
            if (e.KeyCode == Keys.D)
            {
                moveRight = true;
            }
            if (e.KeyCode == Keys.S)
            {
                hardDrop = true;
            }
            //if (e.KeyCode == Keys.F)
            //{
            //    SpawnTetromino();
            //}
            if (e.KeyCode == Keys.Space)
            {
                rotate = true;
            }
            //if (e.KeyCode == Keys.C)
            //{
            //    clear = true;
            //}
        }
        List<Point> currentTetrominoCorners = new();
        byte currentTetrominoColorIndex;
        byte currentTetrominoPatternIndex;
        void SpawnTetromino()
        {
            int index = r.Next(tetrominoShapes.Count);
            bool[,] shape = tetrominoShapes[index];
            currentTetrominoRotIndex = tetrominoRotationIndices[index];
            Debug.WriteLine("Current Tetromino #" + index);
            currentTetrominoId++;
            currentTetrominoCorners.Clear();
            int xOffset = (map.GetLength(0) / TETROMINO_SIZE / 2) - 1;
            currentTetrominoColorIndex = (byte)r.Next(1, palette.Count);
            currentTetrominoPatternIndex = (byte)r.Next(patterns.Count);

            for (int y = 0; y < shape.GetLength(1); y++)
            {
                for (int x = 0; x < shape.GetLength(0); x++)
                {
                    if (shape[x, y])
                    {
                        Point tba = new Point((x + xOffset) * TETROMINO_SIZE, y * TETROMINO_SIZE);
                        currentTetrominoCorners.Add(tba);
                        DrawCell(tba.X,tba.Y);
                    }
                }
            }
        }
        void DrawCell(int baseX, int baseY, bool fillWithTransparent = false)
        {
            if (fillWithTransparent)
            {
                for (int y1 = 0; y1 < TETROMINO_SIZE; y1++)
                {
                    for (int x1 = 0; x1 < TETROMINO_SIZE; x1++)
                    {
                        SetCell(baseX + x1, baseY + y1, 0, Color.Transparent,0);
                    }
                }
            }
            else
            {
                for (int y1 = 0; y1 < TETROMINO_SIZE; y1++)
                {
                    for (int x1 = 0; x1 < TETROMINO_SIZE; x1++)
                    {
                        Color patternColor = patterns[currentTetrominoPatternIndex].GetPixel(x1 % 8, y1 % 8);

                        // Tint the grayscale pattern using the tetromino's color
                        Color tintedColor = Color.FromArgb(
                            patternColor.R * palette[currentTetrominoColorIndex].R / 255,
                            patternColor.G * palette[currentTetrominoColorIndex].G / 255,
                            patternColor.B * palette[currentTetrominoColorIndex].B / 255
                        );

                        SetCell(baseX + x1, baseY + y1, currentTetrominoColorIndex, tintedColor, currentTetrominoId);
                    }
                }
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            //foreach (var item in currentTetrominoCorners)
            //{
            //    //Debug.WriteLine(item.ToString());
            //    bmp.SetPixel(item.X, item.Y, Color.HotPink);
            //}
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.DrawImage(bmp, 0, 0, Width, Height);
        }
        private void Update(object sender, EventArgs e)
        {
            if (currentTetrominoCorners.Count == 0)
            {
                SpawnTetromino();
            }
            for (int y = map.GetLength(1) - 2; y >= 0; y--)
            {
                for (int x = 0; x < map.GetLength(0); x++)
                {
                    #region Movement
                    if (map[x, y].ID > 0) //if i exist
                    {
                        if (map[x, y + 1].ID == 0) //if below is empty - fall
                        {
                            SetCell(x, y + 1, map[x, y].ID, map[x, y].Color, map[x, y].TetrominoID);
                            SetCell(x, y, 0, Color.Transparent, map[x, y].TetrominoID);
                            if (y + 1 == map.GetLength(1) - 1)
                            {
                                if (map[x, y].TetrominoID > lastTetrominoCollisionId)
                                {
                                    SpawnTetromino();
                                    lastTetrominoCollisionId = Math.Max(lastTetrominoCollisionId, map[x, y].TetrominoID);
                                }
                            }
                        }
                        else
                        {
                            if (map[x, y].TetrominoID > lastTetrominoCollisionId)
                            {
                                SpawnTetromino();
                                lastTetrominoCollisionId = Math.Max(lastTetrominoCollisionId, map[x, y].TetrominoID);
                            }
                            if (x == 0) //left border - can only go right
                            {
                                if (map[x + 1, y + 1].ID == 0)
                                {
                                    SetCell(x + 1, y + 1, map[x, y].ID, map[x, y].Color, map[x, y].TetrominoID);
                                    SetCell(x, y, 0, Color.Transparent, map[x, y].TetrominoID);
                                }
                            }
                            else if (x == map.GetLength(0) - 1) //right border - can only go left
                            {
                                if (map[x - 1, y + 1].ID == 0)
                                {
                                    SetCell(x - 1, y + 1, map[x, y].ID, map[x, y].Color, map[x, y].TetrominoID);
                                    SetCell(x, y, 0, Color.Transparent, map[x, y].TetrominoID);
                                }
                            }
                            else //otherwise randomly choose a direction
                            {
                                if (r.NextDouble() >= 0.5)
                                {
                                    if (map[x + 1, y + 1].ID == 0)
                                    {
                                        SetCell(x + 1, y + 1, map[x, y].ID, map[x, y].Color, map[x, y].TetrominoID);
                                        SetCell(x, y, 0, Color.Transparent, map[x, y].TetrominoID);
                                    }
                                }
                                else
                                {
                                    if (map[x - 1, y + 1].ID == 0)
                                    {
                                        SetCell(x - 1, y + 1, map[x, y].ID, map[x, y].Color, map[x, y].TetrominoID);
                                        SetCell(x, y, 0, Color.Transparent, map[x, y].TetrominoID);
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
                if (map[0, y].ID > 0 && map[0, y].TetrominoID != currentTetrominoId) // Starting from leftmost edge and ensure it's not the current tetromino
                {
                    List<(int, int)> currentCells = new List<(int, int)>();
                    bool reachedRightEdge = false;
                    int startingID = map[0, y].ID;  // ID of the cell on the leftmost edge

                    queue.Enqueue((0, y));
                    while (queue.Count > 0)
                    {
                        var (cx, cy) = queue.Dequeue();
                        if (visited[cx, cy] || map[cx, cy].ID != startingID || map[cx, cy].TetrominoID == currentTetrominoId) continue;  // Ensure only cells with the same ID are processed and it's not the current tetromino

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

                                if (nx >= 0 && nx < map.GetLength(0) && ny >= 0 && ny < map.GetLength(1) && !visited[nx, ny] && map[nx, ny].ID == startingID && map[nx, ny].TetrominoID != currentTetrominoId)
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
                            SetCell(cellX, cellY, 0, Color.Transparent, currentTetrominoId);
                        }
                    }
                }
            }
            #endregion
            #region Rotation & Movement

            for (int i = 0; i < currentTetrominoCorners.Count; i++)
            {
                currentTetrominoCorners[i] = new Point(currentTetrominoCorners[i].X, currentTetrominoCorners[i].Y + 1);
            }
            if (rotate)
            {
                RotateCurrentTetromino();
                rotate = false;
            }
            if (moveLeft)
            {
                MoveLeft();
            }
            else if (moveRight)
            {
                MoveRight();
            }
            if (hardDrop)
            {
                hardDrop = false;
                HardDropCurrentTetromino();
            }

            #endregion
            //if (mLeft)
            //{
            //    Point ptc = PointToClient(Cursor.Position);
            //    SetCell(ptc.X / uiScale, ptc.Y / uiScale, 1, Color.Red, currentTetrominoId);
            //    //mLeft = false;
            //}
            //else if (mRight)
            //{
            //    Point ptc = PointToClient(Cursor.Position);
            //    SetCell(ptc.X / uiScale, ptc.Y / uiScale, 2, Color.Black, currentTetrominoId);
            //    //mRight = false;
            //}
            Invalidate();
        }
        bool mLeft = false;
        bool mRight = false;
        bool rotate = false;
        bool moveLeft = false;
        bool moveRight = false;
        private bool clear;
        private bool hardDrop;

        private bool currentTetrominoIsOShape()
        {
            if (currentTetrominoCorners.Count != 4) return false; // The O shape has 4 corners.

            // Sort corners by their X and then Y coordinates for consistent ordering.
            var sortedCorners = currentTetrominoCorners.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();

            // Check the relative positions of the corners to each other.
            return sortedCorners[1].X == sortedCorners[0].X && sortedCorners[1].Y == sortedCorners[0].Y + TETROMINO_SIZE &&
                   sortedCorners[2].X == sortedCorners[0].X + TETROMINO_SIZE && sortedCorners[2].Y == sortedCorners[0].Y &&
                   sortedCorners[3].X == sortedCorners[0].X + TETROMINO_SIZE && sortedCorners[3].Y == sortedCorners[0].Y + TETROMINO_SIZE;
        }
        void RotateCurrentTetromino()
        {
            if (!(currentTetrominoCorners != null && currentTetrominoCorners.Count > 0))
            {
                return;
            }
            if (currentTetrominoIsOShape())
            {
                return;
            }

            Point center = currentTetrominoCorners[currentTetrominoRotIndex];

            // Step 1: Rotate the tetromino
            List<Point> newCorners = ComputeRotatedCorners();

            // Step 2: Adjust for out of bounds
            BringWithinBounds(newCorners);

            // Step 3: Check for collisions with existing tetrominos
            // If colliding, attempt wall kick
            if (CheckCollision(newCorners))
            {
                bool foundSpace = false;
                // Attempt to move left
                for (int i = 1; i < TETROMINO_SIZE; i++)
                {
                    MoveCorners(newCorners, -i, 0);
                    if (!CheckCollision(newCorners))
                    {
                        foundSpace = true;
                        break;
                    }
                    // Reset the corners before attempting to move right
                    newCorners = ComputeRotatedCorners();
                    BringWithinBounds(newCorners);
                }

                // Attempt to move right if no space was found on the left
                if (!foundSpace)
                {
                    for (int i = 1; i < TETROMINO_SIZE; i++)
                    {
                        MoveCorners(newCorners, i, 0);
                        if (!CheckCollision(newCorners))
                        {
                            foundSpace = true;
                            break;
                        }
                        // Reset the corners for the next iteration
                        newCorners = ComputeRotatedCorners();
                        BringWithinBounds(newCorners);
                    }
                }

                // Step 4: If no fitting space was found, revert the rotation
                if (!foundSpace)
                {
                    return;
                }
            }

            // If we made it here, we're clear to draw the rotated tetromino
            ClearCurrentTetromino();
            currentTetrominoCorners = newCorners;

            DrawCurrentTetromino();

            // Update current tetromino corners to the new rotated positions
        }
        void HardDropCurrentTetromino()
        {
            List<Point> newCorners;

            // Try moving the tetromino down until it can't be moved any further
            ClearCurrentTetromino();

            while (true)
            {
                newCorners = CalculateNewCorners(0, 1);
                if (WillCollide(newCorners))
                {
                    break;
                }
                currentTetrominoCorners = newCorners;
            }

            // Now that the tetromino has landed, draw it at its final position
            DrawCurrentTetromino();

            // You can add additional steps here if needed, such as checking for completed lines
        }

        bool WillCollide(List<Point> newCorners)
        {
            foreach (Point corner in newCorners)
            {
                for (int y = 0; y < TETROMINO_SIZE; y++)
                {
                    for (int x = 0; x < TETROMINO_SIZE; x++)
                    {
                        int newX = corner.X + x;
                        int newY = corner.Y + y;

                        // Check for out-of-bounds
                        if (newX < 0 || newX >= map.GetLength(0) || newY >= map.GetLength(1))
                        {
                            return true;
                        }

                        // Check for collision with existing tetrominos
                        if (map[newX, newY].ID > 0 && map[newX, newY].TetrominoID != currentTetrominoId)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        List<Point> CalculateNewCorners(int dx, int dy)
        {
            List<Point> newCorners = new List<Point>();
            foreach (Point corner in currentTetrominoCorners)
            {
                newCorners.Add(new Point(corner.X + dx, corner.Y + dy));
            }
            return newCorners;
        }
        void BringWithinBounds(List<Point> corners)
        {
            int minX = corners.Min(p => p.X);
            int maxX = corners.Max(p => p.X);

            while (minX < 0)
            {
                MoveCorners(corners, 1, 0);
                minX++;
            }

            while (maxX >= map.GetLength(0))
            {
                MoveCorners(corners, -1, 0);
                maxX--;
            }
        }

        void MoveCorners(List<Point> corners, int dx, int dy)
        {
            for (int i = 0; i < corners.Count; i++)
            {
                corners[i] = new Point(corners[i].X + dx, corners[i].Y + dy);
            }
        }
        List<Point> ComputeRotatedCorners()
        {
            Point center = currentTetrominoCorners[currentTetrominoRotIndex];
            List<Point> newCorners = new List<Point>();

            foreach (Point corner in currentTetrominoCorners)
            {
                int newX = center.Y - corner.Y + center.X;
                int newY = corner.X - center.X + center.Y;
                newCorners.Add(new Point(newX, newY));
            }

            // Check for wall collisions and adjust accordingly
            foreach (Point corner in newCorners.ToList())
            {
                if (corner.X < 0)
                {
                    for (int i = 0; i < newCorners.Count; i++)
                    {
                        newCorners[i] = new Point(newCorners[i].X + 1, newCorners[i].Y);
                    }
                }
                else if (corner.X >= map.GetLength(0))
                {
                    for (int i = 0; i < newCorners.Count; i++)
                    {
                        newCorners[i] = new Point(newCorners[i].X - 1, newCorners[i].Y);
                    }
                }
            }

            return newCorners;
        }

        bool CheckCollision(List<Point> corners)
        {
            foreach (Point corner in corners)
            {
                if (!IsWithinBounds(corner.X, corner.Y, TETROMINO_SIZE, TETROMINO_SIZE))
                    return true;

                for (int y = 0; y < TETROMINO_SIZE; y++)
                {
                    for (int x = 0; x < TETROMINO_SIZE; x++)
                    {
                        int checkX = corner.X + x;
                        int checkY = corner.Y + y;

                        if (map[checkX, checkY].ID > 0 && map[checkX, checkY].TetrominoID != currentTetrominoId)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        bool IsWithinBounds(int x, int y, int width = 1, int height = 1)
        {
            return x >= 0 && (x + width) <= map.GetLength(0) && y >= 0 && (y + height) <= map.GetLength(1);
        }
        void MoveLeft()
        {
            List<Point> newCorners = new List<Point>();

            foreach (Point corner in currentTetrominoCorners)
            {
                newCorners.Add(new Point(corner.X - 1, corner.Y));
            }

            if (!CheckCollision(newCorners))
            {
                ClearCurrentTetromino();
                currentTetrominoCorners = newCorners;

                DrawCurrentTetromino();
            }
        }

        void MoveRight()
        {
            List<Point> newCorners = new List<Point>();

            foreach (Point corner in currentTetrominoCorners)
            {
                newCorners.Add(new Point(corner.X + 1, corner.Y));
            }

            if (!CheckCollision(newCorners))
            {

                ClearCurrentTetromino();
                currentTetrominoCorners = newCorners;
                DrawCurrentTetromino();
            }
        }
        void ClearCurrentTetromino()
        {
            foreach (Point corner in currentTetrominoCorners)
            {
                DrawCell(corner.X, corner.Y, true);
            }
        }
        void DrawCurrentTetromino()
        {
            foreach (Point corner in currentTetrominoCorners)
            {
                DrawCell(corner.X, corner.Y);
            }
        }
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

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A)
            {
                moveLeft = false;
            }
            if (e.KeyCode == Keys.D)
            {
                moveRight = false;
            }
        }
    }
}