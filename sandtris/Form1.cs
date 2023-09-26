using System.Configuration;
using System.Diagnostics;
using System.Drawing.Text;
using System.Reflection;

namespace sandtris
{
    public partial class Form1 : Form
    {
        const int TETROMINO_SIZE = 8;
        static int uiScale = 2;

        struct Cell
        {
            public Color Color;
            public byte ID;
            public uint TetrominoID;
        }
        static uint currentTetrominoId;
        static byte currentTetrominoRotIndex;
        static uint lastTetrominoCollisionId;
        static Random r = new();
        List<bool[,]> tetrominoShapes = new()
        {
            new bool[,] { { true, true, true, true } }, // I
            new bool[,] { { true, true }, { true, true } }, // O
            new bool[,] { { true, true, false }, { false, true, true } }, // S
            new bool[,] { { false, true, true }, { true, true, false } }, // Z
            new bool[,] { { true, false, false }, { true, true, true } }, // L
            new bool[,] { { false, false, true }, { true, true, true } }, // J
            new bool[,] { { true, true, true }, { false, true, false } } // T
        };
        List<byte> tetrominoRotationIndices = new()
        {
            1,1,1,1,2,1,1
        };
        List<Color> palette = new()
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
        Bitmap nextTetrominoBitmap = new(TETROMINO_SIZE * 4, TETROMINO_SIZE * 4);

        Cell[,] map;

        PrivateFontCollection pfc;
        Font myFont;

        List<Bitmap> patterns;
        List<Point> currentTetrominoCorners = new();

        byte currentTetrominoColorIndex;
        byte currentTetrominoPatternIndex;

        byte nextTetrominoColorIndex;
        byte nextTetrominoPatternIndex;
        int nextTetrominoShapeIndex;

        int score = 0;

        bool gameOver = false;
        bool paused = false;
        PictureBox gameOverPictureBox;

        bool rotate = false;
        bool moveLeft = false;
        bool moveRight = false;
        bool hardDrop;

        static SolidBrush wallDark = new(Color.FromArgb(51, 51, 51));
        static SolidBrush wallNormal = new(Color.FromArgb(102, 102, 102));
        static SolidBrush wallLight = new(Color.FromArgb(153, 153, 153));
        Color bgColor = Color.FromArgb(255, 20, 20, 20);

        public Form1()
        {
            InitializeComponent();

            pfc = new PrivateFontCollection();
            pfc.AddFontFile("CompassGold.ttf");

            patterns = new List<Bitmap>();
            foreach (var item in Directory.GetFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "*.png"))
            {
                patterns.Add((Bitmap)Image.FromFile(item));
            }

            gameOverPictureBox = new()
            {
                Anchor = AnchorStyles.None,
                Width = Width,
                Height = Height,
                BackColor = Color.Transparent,
                Location = new Point(0, 0)
            };

            gameOverPictureBox.Paint += GameOverPictureBox_Paint;
            Controls.Add(gameOverPictureBox);
            gameOverPictureBox.Visible = false;

            ResetGame();

            typeof(Panel).InvokeMember("DoubleBuffered", BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic, null, panel1, new object[] { true });
            panel1.Paint += Panel1_Paint;
            panel1.BackColor = bgColor;
            BackColor = bgColor;
            Debug.WriteLine(panel1.Location.ToString());
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            UpdateUIScale(-1);
        }
        private void RescaleUI()
        {
            // Adjust font based on the uiScale
            myFont = new Font(pfc.Families[0], 6 * uiScale);

            // Rescale various UI elements
            ClientSize = new Size((int)(bmp.Width * uiScale * 1.75), (bmp.Height - (4 * TETROMINO_SIZE)) * uiScale);
            panel1.Width = bmp.Width * uiScale;
            panel1.Height = (bmp.Height - (4 * TETROMINO_SIZE)) * uiScale;
            panel1.Left = 4 * uiScale;
            panel1.Top = 0;
            gameOverPictureBox.Width = ClientSize.Width;
            gameOverPictureBox.Height = ClientSize.Height;
            Debug.WriteLine(panel1.Location.ToString());

        }
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
        public void UpdateUIScale(int scaleValue)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            // If -1 is passed in, load the value from the config or set defaults
            if (scaleValue == -1)
            {
                if (config.AppSettings.Settings["uiScale"] != null)
                {
                    // Try to parse the uiScale from the app.config
                    if (int.TryParse(config.AppSettings.Settings["uiScale"].Value, out int parsedValue) && parsedValue >= 1 && parsedValue <= 9)
                    {
                        uiScale = parsedValue;
                    }
                    else
                    {
                        // If parsing failed or value is out of bounds, set to a default
                        uiScale = 2;
                    }
                }
                else
                {
                    // If "uiScale" doesn't exist in app.config, set default and also add to app.config
                    uiScale = 2;
                    config.AppSettings.Settings.Add("uiScale", uiScale.ToString());
                    config.Save(ConfigurationSaveMode.Modified);
                }
            }
            else if (scaleValue >= 1 && scaleValue <= 9)
            {
                // If a valid scale value is passed, update both in-memory and config
                uiScale = scaleValue;

                if (config.AppSettings.Settings["uiScale"] != null)
                {
                    config.AppSettings.Settings["uiScale"].Value = scaleValue.ToString();
                }
                else
                {
                    config.AppSettings.Settings.Add("uiScale", scaleValue.ToString());
                }

                config.Save(ConfigurationSaveMode.Modified);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(scaleValue), "Valid scale values are between 1 and 9 or -1 to load from config.");
            }
            RescaleUI();
        }
        private void GameOverPictureBox_Paint(object? sender, PaintEventArgs e)
        {
            const string gameOverText = "GAME OVER";
            const string newGameText = "1: New Game";
            const string exitText = "2: Exit";

            SizeF size = e.Graphics.MeasureString(gameOverText, myFont);
            PointF location = new((gameOverPictureBox.Width - size.Width) / 2, gameOverPictureBox.Height / 2 - size.Height);
            e.Graphics.DrawString(gameOverText, myFont, Brushes.Red, location);

            PointF newGameLocation = new((gameOverPictureBox.Width - size.Width) / 2, gameOverPictureBox.Height / 2);
            PointF exitLocation = new((gameOverPictureBox.Width - size.Width) / 2, gameOverPictureBox.Height / 2 + size.Height);

            e.Graphics.DrawString(newGameText, myFont, Brushes.Yellow, newGameLocation);
            e.Graphics.DrawString(exitText, myFont, Brushes.Yellow, exitLocation);
        }

        private void ResetGame()
        {
            gameOverPictureBox.Visible = false;
            inputTimer.Start();
            logicTimer.Start();
            ClearMap();
            nextTetrominoBitmap = new Bitmap(TETROMINO_SIZE * 4, TETROMINO_SIZE * 4);
            currentTetrominoId = 0;
            score = 0;
            //currentTetrominoColorIndex = 0;
            //currentTetrominoPatternIndex = 0;
            //currentTetrominoRotIndex = 0;
            lastTetrominoCollisionId = 0;
            nextTetrominoShapeIndex = r.Next(tetrominoShapes.Count);
            nextTetrominoColorIndex = (byte)r.Next(1, palette.Count);
            nextTetrominoPatternIndex = (byte)r.Next(patterns.Count);
            SpawnTetromino();
        }

        private void Panel1_Paint(object? sender, PaintEventArgs e)
        {
            //foreach (var item in currentTetrominoCorners)
            //{
            //    if (item.Y < bmp.Height)
            //    {
            //        bmp.SetPixel(item.X, item.Y, Color.HotPink);
            //    }
            //}
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            RectangleF sourceRect = new RectangleF(0, 4 * TETROMINO_SIZE, bmp.Width, bmp.Height - 4 * TETROMINO_SIZE);

            // Destination rectangle (where and how big the source should be drawn)
            RectangleF destRect = new RectangleF(0, 0, bmp.Width * uiScale, (bmp.Height - 4 * TETROMINO_SIZE) * uiScale);

            // Draw the image
            e.Graphics.DrawImage(bmp, destRect, sourceRect, GraphicsUnit.Pixel);
        }

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

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (gameOver)
            {
                if (e.KeyCode == Keys.D1) //new game
                {
                    gameOver = false;
                    ResetGame();
                }
                else if (e.KeyCode == Keys.D2) //quit
                {
                    Environment.Exit(0);
                }
            }
            else
            {
                if (e.KeyCode == Keys.Escape)
                {
                    paused = !paused;
                    if (paused)
                    {
                        logicTimer.Stop();
                        inputTimer.Stop();
                    }
                    else
                    {
                        logicTimer.Start();
                        inputTimer.Start();
                    }
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
                if (e.KeyCode == Keys.Space)
                {
                    rotate = true;
                }
                if (e.KeyCode == Keys.R)
                {
                    ResetGame();
                }
                if (e.KeyCode == Keys.F)
                {
                    CheckGameOver(true);
                }
                if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9) // For top row number keys
                {
                    // Subtracting with Keys.D1 gives 0 for '1', 1 for '2'... so add 1.
                    int pressedNumber = e.KeyCode - Keys.D1 + 1;
                    UpdateUIScale(pressedNumber);
                }
                // If you also want to consider the number pad:
                else if (e.KeyCode >= Keys.NumPad1 && e.KeyCode <= Keys.NumPad9)
                {
                    int pressedNumber = e.KeyCode - Keys.NumPad1 + 1;
                    UpdateUIScale(pressedNumber);
                }
            }
        }
        void SpawnTetromino()
        {
            CheckGameOver();
            if (gameOver)
            {
                return;
            }
            Debug.WriteLine("Spawning tetromino");

            // Use the next tetromino values
            int index = nextTetrominoShapeIndex;
            bool[,] shape = tetrominoShapes[index];
            currentTetrominoRotIndex = tetrominoRotationIndices[index];
            currentTetrominoId++;
            currentTetrominoCorners.Clear();
            int xOffset = (map.GetLength(0) / TETROMINO_SIZE / 2) - 1;
            currentTetrominoColorIndex = nextTetrominoColorIndex;
            currentTetrominoPatternIndex = nextTetrominoPatternIndex;
            ClearCurrentTetromino();
            for (int y = 0; y < shape.GetLength(1); y++)
            {
                for (int x = 0; x < shape.GetLength(0); x++)
                {
                    if (shape[x, y])
                    {
                        Point toBeAdded = new((x + xOffset) * TETROMINO_SIZE, y * TETROMINO_SIZE);
                        currentTetrominoCorners.Add(toBeAdded);
                    }
                }
            }
            nextTetrominoShapeIndex = r.Next(tetrominoShapes.Count);
            nextTetrominoColorIndex = (byte)r.Next(1, palette.Count);
            nextTetrominoPatternIndex = (byte)r.Next(patterns.Count);
            DrawCurrentTetromino();
            DrawPreviewTetromino(0, 0, nextTetrominoBitmap);

            //Debug.WriteLine("\n\n\n\n\n\n\nnext:\n" + shapeNames[nextTetrominoShapeIndex] + "\n" + palette[nextTetrominoColorIndex].ToString() + "\npattern: " + nextTetrominoPatternIndex);
        }
        void DrawPreviewTetromino(int baseX, int baseY, Bitmap previewBitmap)
        {
            for (int y = 0; y < previewBitmap.Height; y++)
            {
                for (int x = 0; x < previewBitmap.Width; x++)
                {
                    previewBitmap.SetPixel(x, y, Color.Transparent);
                }
            }
            bool[,] shape = tetrominoShapes[nextTetrominoShapeIndex];

            for (int y1 = 0; y1 < shape.GetLength(1); y1++)
            {
                for (int x1 = 0; x1 < shape.GetLength(0); x1++)
                {
                    if (shape[x1, y1])
                    {
                        DrawPreviewCell(baseX + x1 * TETROMINO_SIZE, baseY + y1 * TETROMINO_SIZE, previewBitmap);
                    }
                }
            }
        }
        void DrawPreviewCell(int baseX, int baseY, Bitmap previewBitmap, bool fillWithTransparent = false)
        {
            if (fillWithTransparent)
            {
                for (int y1 = 0; y1 < TETROMINO_SIZE; y1++)
                {
                    for (int x1 = 0; x1 < TETROMINO_SIZE; x1++)
                    {
                        SetPreviewPixel(baseX + x1, baseY + y1, Color.Transparent, previewBitmap);

                    }
                }
            }
            for (int y1 = 0; y1 < TETROMINO_SIZE; y1++)
            {
                for (int x1 = 0; x1 < TETROMINO_SIZE; x1++)
                {
                    float uvX = x1 / scalingFactor;
                    float uvY = y1 / scalingFactor;
                    Color patternColor = patterns[nextTetrominoPatternIndex].GetPixel((int)uvX, (int)uvY);

                    Color tintedColor = Color.FromArgb(
                        patternColor.R * palette[nextTetrominoColorIndex].R / 255,
                        patternColor.G * palette[nextTetrominoColorIndex].G / 255,
                        patternColor.B * palette[nextTetrominoColorIndex].B / 255
                    );

                    SetPreviewPixel(baseX + x1, baseY + y1, tintedColor, previewBitmap);
                }
            }
        }

        static void SetPreviewPixel(int x, int y, Color color, Bitmap previewBitmap)
        {
            if (x < 0 || y < 0 || x >= previewBitmap.Width || y >= previewBitmap.Height)
            {
                return;
            }
            previewBitmap.SetPixel(x, y, color);
        }
        static float scalingFactor = TETROMINO_SIZE / 8f; // Assuming TETROMINO_SIZE is a multiple of 8
        void DrawCell(int baseX, int baseY, bool fillWithTransparent = false)
        {

            if (fillWithTransparent)
            {
                for (int y1 = 0; y1 < TETROMINO_SIZE; y1++)
                {
                    for (int x1 = 0; x1 < TETROMINO_SIZE; x1++)
                    {
                        SetCell(baseX + x1, baseY + y1, 0, Color.Transparent, 0);
                    }
                }
            }
            else
            {
                for (int y1 = 0; y1 < TETROMINO_SIZE; y1++)
                {
                    for (int x1 = 0; x1 < TETROMINO_SIZE; x1++)
                    {
                        float uvX = x1 / scalingFactor;
                        float uvY = y1 / scalingFactor;
                        Color patternColor = patterns[currentTetrominoPatternIndex].GetPixel((int)uvX, (int)uvY);

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
            //left side
            e.Graphics.FillRectangle(wallNormal, 0, 0, 4 * uiScale, Height);
            e.Graphics.FillRectangle(wallDark, panel1.Left - uiScale, 0, uiScale, panel1.Height);
            e.Graphics.FillRectangle(wallLight, 0, 0, 4 * uiScale, uiScale);

            //right side
            e.Graphics.FillRectangle(wallNormal, panel1.Right, 0, 4 * uiScale, Height);
            e.Graphics.FillRectangle(wallLight, panel1.Right, 0, uiScale, Height);
            e.Graphics.FillRectangle(wallLight, panel1.Right, 0, 4 * uiScale, uiScale);
            string stringToDraw = "Score: " + score + "\n\n\n";
            SizeF stringSize = e.Graphics.MeasureString(stringToDraw, myFont);
            e.Graphics.DrawString(stringToDraw, myFont, Brushes.White, panel1.Right + (5 * uiScale), 0);
            float pictureHeight = stringSize.Height;
            stringToDraw = "Next:";
            stringSize = e.Graphics.MeasureString(stringToDraw, myFont);
            e.Graphics.DrawString(stringToDraw, myFont, Brushes.White, panel1.Right + (5 * uiScale), pictureHeight);

            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.DrawImage(nextTetrominoBitmap, panel1.Right + (5 * uiScale) + stringSize.Width, pictureHeight, nextTetrominoBitmap.Width * uiScale / 2, nextTetrominoBitmap.Height * uiScale / 2);
            //bottom (unused)
            //e.Graphics.FillRectangle(Brushes.Gray, panel1.Left, panel1.Bottom, panel1.Width, 4 * uiScale);
        }
        private void Update(object sender, EventArgs e)
        {
            if (currentTetrominoCorners.Count == 0)
            {
                SpawnTetromino();
                lastTetrominoCollisionId = Math.Max(lastTetrominoCollisionId, map[currentTetrominoCorners[0].X, currentTetrominoCorners[0].Y].TetrominoID);
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
            Queue<(int, int)> queue = new();

            for (int y = 0; y < map.GetLength(1); y++)
            {
                if (map[0, y].ID > 0 && map[0, y].TetrominoID != currentTetrominoId) // Starting from leftmost edge and ensure it's not the current tetromino
                {
                    List<(int, int)> currentCells = new();
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
                        score += currentCells.Count;
                        logicTimer.Stop();
                        inputTimer.Stop();
                        foreach (var (cellX, cellY) in currentCells)
                        {
                            SetCell(cellX, cellY, 0, Color.White, currentTetrominoId);
                        }
                        Refresh();
                        currentCells.Shuffle();
                        int counter = 0;
                        while (true)
                        {
                            foreach (var (cellX, cellY) in currentCells)
                            {
                                SetCell(cellX, cellY, 0, Color.Transparent, currentTetrominoId);
                                if (counter >= 40)
                                {
                                    counter = 0;
                                    Refresh();
                                    Thread.Sleep(2);
                                }
                                counter++;
                            }
                            break;
                        }
                        logicTimer.Start();
                        inputTimer.Start();
                    }
                }
            }
            #endregion
            #region Rotation & Movement

            for (int i = 0; i < currentTetrominoCorners.Count; i++)
            {
                currentTetrominoCorners[i] = new Point(currentTetrominoCorners[i].X, currentTetrominoCorners[i].Y + 1);
            }
            #endregion
        }

        private void CheckGameOver(bool forceFail = false)
        {
            for (int x = 0; x < map.GetLength(0); x++)
            {
                if (forceFail || map[x, TETROMINO_SIZE * 4].Color != Color.Transparent)
                {
                    gameOver = true;
                    gameOverPictureBox.BringToFront();
                    gameOverPictureBox.Visible = true;
                    Debug.WriteLine("GAMEOVER");
                    inputTimer.Stop();
                    logicTimer.Stop();
                    Refresh();
                    return;
                }
            }
        }


        private bool CurrentTetrominoIsOShape()
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
            if (CurrentTetrominoIsOShape())
            {
                return;
            }

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
            List<Point> newCorners = new();
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

        static void MoveCorners(List<Point> corners, int dx, int dy)
        {
            for (int i = 0; i < corners.Count; i++)
            {
                corners[i] = new Point(corners[i].X + dx, corners[i].Y + dy);
            }
        }
        List<Point> ComputeRotatedCorners()
        {
            Point center = currentTetrominoCorners[currentTetrominoRotIndex];
            List<Point> newCorners = new();

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
            List<Point> newCorners = new();

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
            List<Point> newCorners = new();

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

        private void timer2_Tick(object sender, EventArgs e)
        {
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
            panel1.Invalidate();
            Invalidate();
        }
    }
}