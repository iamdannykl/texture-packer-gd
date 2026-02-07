using Godot;
using System.Collections.Generic;
using System.Linq;
/// <summary>
/// 切割器
/// </summary>
public partial class Slicer : Node
{
    /// <summary>
    /// 精灵数据：包含ID和矩形区域
    /// </summary>
    public class SpriteData
    {
        public int Id { get; set; }
        public Rect2 Rect { get; set; }

        public SpriteData(int id, Rect2 rect)
        {
            Id = id;
            Rect = rect;
        }
    }
    [Export]
    public TextureRect ImportTextureRect;
    [Export]
    public TextureRect BackgroundTextureRect;
    [Export]
    public Color BackgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    [Export]
    public Color PreviewColor = new Color(0f, 1f, 0f, 0.8f);
    [Export]
    public float PreviewLineWidth = 1f;
    [Export]
    public bool ShowPreview = true;
    [Export]
    public bool EnableManualSelection = true;
    [Export]
    public bool FilterContainedRects = true;
    [Export]
    public bool SnapPreviewToPixels = true;
    [Export]
    public float AlphaThreshold = 0.1f;
    [Export]
    public int MinRegionArea = 4;
    [Export]
    public int MinRegionSize = 1;
    [Export]
    public int IdFontSize = 16;

    private SlicePreview _preview;
    private FileDialog _fileDialog;
    private FileDialog _exportDialog;
    private FileDialog _importJsonDialog;
    private readonly List<SpriteData> _sliceRects = new List<SpriteData>();
    private string _currentAtlasPath = "";
    /// <summary>
    /// 导入按钮按下事件
    /// </summary>
    public void OnImportButtonPressed()
    {
        EnsureFileDialog();
        _fileDialog.PopupCenteredRatio(0.8f);
    }
    /// <summary>
    /// 切割按钮按下事件
    /// </summary>
    public void OnSliceButtonPressed()
    {
        AutoSliceFromTexture();
    }
    /// <summary>
    /// 导出JSON按钮按下事件
    /// </summary>
    public void OnExportButtonPressed()
    {
        if (_sliceRects.Count == 0)
        {
            GD.PushWarning("No slices to export. Please slice the texture first.");
            return;
        }

        EnsureExportDialog();
        _exportDialog.PopupCenteredRatio(0.8f);
    }
    /// <summary>
    /// 导入JSON按钮按下事件
    /// </summary>
    public void OnImportJsonButtonPressed()
    {
        EnsureImportJsonDialog();
        _importJsonDialog.PopupCenteredRatio(0.8f);
    }

    public override void _Ready()
    {
        if (ImportTextureRect == null)
        {
            ImportTextureRect = GetTree().CurrentScene?.GetNodeOrNull<TextureRect>("TextureRect");
        }

        EnsurePreviewLayer();
        EnsureFileDialog();
    }

    public void PreviewSlices(IEnumerable<Rect2> rects)
    {
        EnsurePreviewLayer();
        if (_preview == null)
        {
            return;
        }

        _sliceRects.Clear();
        if (rects != null)
        {
            int id = 0;
            foreach (var rect in rects)
            {
                _sliceRects.Add(new SpriteData(id++, rect));
            }
        }

        _preview.Visible = ShowPreview;
        _preview.SetSliceData(_sliceRects);
    }

    public void ClearPreview()
    {
        if (_preview == null)
        {
            return;
        }

        _sliceRects.Clear();
        _preview.SetSliceData(null);
    }

    private void EnsurePreviewLayer()
    {
        if (_preview != null || ImportTextureRect == null)
        {
            return;
        }

        // 获取Camera2D引用（通常在当前Viewport中）
        Camera2D camera = GetViewport()?.GetCamera2D();

        _preview = new SlicePreview
        {
            Name = "SlicePreview",
            MouseFilter = Control.MouseFilterEnum.Stop,
            FocusMode = Control.FocusModeEnum.All,
            ClipContents = true,
            PreviewColor = PreviewColor,
            LineWidth = PreviewLineWidth,
            Target = ImportTextureRect,
            Camera = camera,
            EnableManualSelection = EnableManualSelection,
            SnapToPixels = SnapPreviewToPixels,
            MinRectSize = MinRegionSize,
            IdFontSize = IdFontSize
        };

        _preview.RectsChanged += OnRectsChanged;

        ImportTextureRect.AddChild(_preview);
        _preview.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _preview.SetOffsetsPreset(Control.LayoutPreset.FullRect);
        _preview.Visible = ShowPreview;
    }

    private void EnsureFileDialog()
    {
        if (_fileDialog != null)
        {
            return;
        }

        _fileDialog = new FileDialog
        {
            Name = "ImportDialog",
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Filters = new string[]
            {
                "*.png ; PNG Images",
                "*.jpg ; JPEG Images",
                "*.jpeg ; JPEG Images",
                "*.webp ; WebP Images",
                "*.tga ; TGA Images",
                "*.bmp ; BMP Images"
            },
            UseNativeDialog = true
        };

        _fileDialog.FileSelected += OnFileSelected;
        AddChild(_fileDialog);
    }

    private void EnsureExportDialog()
    {
        if (_exportDialog != null)
        {
            return;
        }

        _exportDialog = new FileDialog
        {
            Name = "ExportDialog",
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.SaveFile,
            Filters = new string[]
            {
                "*.json ; JSON Files"
            },
            UseNativeDialog = true
        };

        _exportDialog.FileSelected += OnExportFileSelected;
        AddChild(_exportDialog);
    }

    private void EnsureImportJsonDialog()
    {
        if (_importJsonDialog != null)
        {
            return;
        }

        _importJsonDialog = new FileDialog
        {
            Name = "ImportJsonDialog",
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Filters = new string[]
            {
                "*.json ; JSON Files"
            },
            UseNativeDialog = true
        };

        _importJsonDialog.FileSelected += OnImportJsonFileSelected;
        AddChild(_importJsonDialog);
    }

    private void OnImportJsonFileSelected(string path)
    {
        // 读取JSON文件
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError($"Failed to open JSON file: {path}");
            return;
        }

        string jsonContent = file.GetAsText();
        var json = new Json();
        var parseResult = json.Parse(jsonContent);
        if (parseResult != Error.Ok)
        {
            GD.PushError($"Failed to parse JSON: {json.GetErrorMessage()}");
            return;
        }

        var rootData = json.Data.AsGodotDictionary();
        if (!rootData.ContainsKey("atlas") || !rootData.ContainsKey("sprites"))
        {
            GD.PushError("Invalid JSON format: missing 'atlas' or 'sprites' field");
            return;
        }

        // 加载图集
        string atlasPath = rootData["atlas"].AsString();
        if (!FileAccess.FileExists(atlasPath))
        {
            GD.PushError($"Atlas file not found: {atlasPath}");
            return;
        }

        Image image = Image.LoadFromFile(atlasPath);
        if (image == null)
        {
            GD.PushError($"Failed to load atlas image: {atlasPath}");
            return;
        }

        // 设置图集
        ImageTexture texture = ImageTexture.CreateFromImage(image);
        ImportTextureRect.Texture = texture;
        _currentAtlasPath = atlasPath;
        UpdateBackground(image);

        // 加载精灵数据
        _sliceRects.Clear();
        var spritesArray = rootData["sprites"].AsGodotArray();
        foreach (var item in spritesArray)
        {
            var spriteDict = item.AsGodotDictionary();
            int id = spriteDict["id"].AsInt32();
            int x = spriteDict["x"].AsInt32();
            int y = spriteDict["y"].AsInt32();
            int width = spriteDict["width"].AsInt32();
            int height = spriteDict["height"].AsInt32();

            var rect = new Rect2(x, y, width, height);
            _sliceRects.Add(new SpriteData(id, rect));
        }

        // 更新预览
        EnsurePreviewLayer();
        if (_preview != null)
        {
            _preview.Visible = ShowPreview;
            _preview.SetSliceData(_sliceRects);
        }

        GD.Print($"Successfully imported {_sliceRects.Count} sprites from: {path}");
    }

    private void OnExportFileSelected(string path)
    {
        // 确保文件扩展名为.json
        if (!path.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase))
        {
            path += ".json";
        }

        // 构建JSON数据
        var spriteData = new Godot.Collections.Array();
        foreach (var sprite in _sliceRects)
        {
            var spriteDict = new Godot.Collections.Dictionary
            {
                ["id"] = sprite.Id,
                ["x"] = (int)sprite.Rect.Position.X,
                ["y"] = (int)sprite.Rect.Position.Y,
                ["width"] = (int)sprite.Rect.Size.X,
                ["height"] = (int)sprite.Rect.Size.Y
            };
            spriteData.Add(spriteDict);
        }

        var rootData = new Godot.Collections.Dictionary
        {
            ["atlas"] = _currentAtlasPath,
            ["sprites"] = spriteData
        };

        // 转换为JSON字符串
        string jsonString = Json.Stringify(rootData, "  ");

        // 保存到文件
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PushError($"Failed to open file for writing: {path}");
            return;
        }

        file.StoreString(jsonString);
        GD.Print($"Successfully exported {_sliceRects.Count} sprites to: {path}");
    }

    private void OnFileSelected(string path)
    {
        if (ImportTextureRect == null)
        {
            return;
        }

        Image image = Image.LoadFromFile(path);
        if (image == null)
        {
            GD.PushWarning($"Failed to load image: {path}");
            return;
        }

        ImageTexture texture = ImageTexture.CreateFromImage(image);
        ImportTextureRect.Texture = texture;
        _currentAtlasPath = path;
        UpdateBackground(image);
        ClearPreview();
    }

    private void UpdateBackground(Image image)
    {
        if (BackgroundTextureRect == null || image == null)
        {
            return;
        }

        // 创建一个浅灰色背景，方便查看图集边界
        Image bgImage = Image.CreateEmpty(image.GetWidth(), image.GetHeight(), false, Image.Format.Rgba8);
        bgImage.Fill(BackgroundColor); // 浅灰色背景
        ImageTexture bgTexture = ImageTexture.CreateFromImage(bgImage);
        BackgroundTextureRect.Texture = bgTexture;
    }

    private void AutoSliceFromTexture()
    {
        if (ImportTextureRect == null || ImportTextureRect.Texture == null)
        {
            GD.PushWarning("No texture to slice.");
            return;
        }

        Image image = ImportTextureRect.Texture.GetImage();
        if (image == null)
        {
            GD.PushWarning("Texture has no readable image data.");
            return;
        }

        List<Rect2> rects = AutoSliceByAlpha(image, AlphaThreshold, MinRegionArea, MinRegionSize);
        if (FilterContainedRects)
        {
            rects = RemoveContainedRects(rects);
        }
        PreviewSlices(rects);
    }

    private static List<Rect2> RemoveContainedRects(List<Rect2> rects)
    {
        List<Rect2> result = new List<Rect2>();
        for (int i = 0; i < rects.Count; i++)
        {
            Rect2 inner = rects[i];
            bool contained = false;
            for (int j = 0; j < rects.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                if (ContainsRect(rects[j], inner))
                {
                    contained = true;
                    break;
                }
            }

            if (!contained)
            {
                result.Add(inner);
            }
        }

        return result;
    }

    private static bool ContainsRect(Rect2 outer, Rect2 inner)
    {
        const float epsilon = 0.001f;
        Vector2 outerEnd = outer.Position + outer.Size;
        Vector2 innerEnd = inner.Position + inner.Size;

        return inner.Position.X + epsilon >= outer.Position.X
            && inner.Position.Y + epsilon >= outer.Position.Y
            && innerEnd.X <= outerEnd.X + epsilon
            && innerEnd.Y <= outerEnd.Y + epsilon;
    }

    private static List<Rect2> AutoSliceByAlpha(Image image, float alphaThreshold, int minArea, int minSize)
    {
        int width = image.GetWidth();
        int height = image.GetHeight();
        bool[] visited = new bool[width * height];
        List<Rect2> results = new List<Rect2>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = x + (y * width);
                if (visited[index])
                {
                    continue;
                }

                Color color = image.GetPixel(x, y);
                if (color.A <= alphaThreshold)
                {
                    visited[index] = true;
                    continue;
                }

                Rect2 rect = FloodFillBounds(image, x, y, width, height, alphaThreshold, visited);
                int area = (int)(rect.Size.X * rect.Size.Y);
                if (area >= minArea && rect.Size.X >= minSize && rect.Size.Y >= minSize)
                {
                    results.Add(rect);
                }
            }
        }

        return results;
    }

    private static Rect2 FloodFillBounds(Image image, int startX, int startY, int width, int height, float alphaThreshold, bool[] visited)
    {
        int minX = startX;
        int minY = startY;
        int maxX = startX;
        int maxY = startY;

        Stack<Vector2I> stack = new Stack<Vector2I>();
        stack.Push(new Vector2I(startX, startY));

        while (stack.Count > 0)
        {
            Vector2I p = stack.Pop();
            int x = p.X;
            int y = p.Y;
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                continue;
            }

            int index = x + (y * width);
            if (visited[index])
            {
                continue;
            }

            Color color = image.GetPixel(x, y);
            if (color.A <= alphaThreshold)
            {
                visited[index] = true;
                continue;
            }

            visited[index] = true;

            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;

            stack.Push(new Vector2I(x + 1, y));
            stack.Push(new Vector2I(x - 1, y));
            stack.Push(new Vector2I(x, y + 1));
            stack.Push(new Vector2I(x, y - 1));
        }

        return new Rect2(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);
    }

    private void OnRectsChanged(IReadOnlyList<Rect2> rects)
    {
        // 保持现有ID，只更新矩形或添加新的
        var existingIds = _sliceRects.Select(s => s.Id).ToHashSet();
        int nextId = existingIds.Count > 0 ? existingIds.Max() + 1 : 0;

        _sliceRects.Clear();
        for (int i = 0; i < rects.Count; i++)
        {
            int id = i < existingIds.Count ? i : nextId++;
            _sliceRects.Add(new SpriteData(id, rects[i]));
        }
    }

    private sealed partial class SlicePreview : Control
    {
        public TextureRect Target;
        public Camera2D Camera;
        public Color PreviewColor = new Color(0f, 1f, 0f, 0.8f);
        public float LineWidth = 2f;
        public bool EnableManualSelection = true;
        public bool SnapToPixels = true;
        public float MinRectSize = 1f;
        public int IdFontSize = 16;

        public event System.Action<IReadOnlyList<Rect2>> RectsChanged;

        private readonly List<SpriteData> _sprites = new List<SpriteData>();
        private readonly Stack<List<SpriteData>> _undoHistory = new Stack<List<SpriteData>>();
        private const int MaxUndoSteps = 50;
        private bool _dragging;
        private bool _resizing;
        private Vector2 _dragStartLocal;
        private Vector2 _dragCurrentLocal;
        private Rect2 _resizeStartRect;
        private int _selectedIndex = -1;
        private ResizeHandle _resizeHandle = ResizeHandle.None;
        private const float HandlePixels = 4f;
        private float _lastZoom = 1f;
        private int _nextId = 0;

        public override void _Process(double delta)
        {
            // 监听zoom变化，触发重绘以更新线宽和控制点大小
            if (Camera != null)
            {
                float currentZoom = Camera.Zoom.X;
                if (Mathf.Abs(currentZoom - _lastZoom) > 0.001f)
                {
                    _lastZoom = currentZoom;
                    QueueRedraw();
                }
            }
        }

        public void SetSliceData(IEnumerable<SpriteData> sprites)
        {
            _sprites.Clear();
            if (sprites != null)
            {
                _sprites.AddRange(sprites);
                _nextId = _sprites.Count > 0 ? _sprites.Max(s => s.Id) + 1 : 0;
            }

            _selectedIndex = -1;
            _undoHistory.Clear(); // 重新加载数据时清空历史

            QueueRedraw();
        }

        private void SaveHistory()
        {
            // 保存当前状态到历史栈
            var snapshot = _sprites.Select(s => new SpriteData(s.Id, s.Rect)).ToList();
            _undoHistory.Push(snapshot);

            // 限制历史记录数量
            if (_undoHistory.Count > MaxUndoSteps)
            {
                var temp = _undoHistory.Reverse().Take(MaxUndoSteps).Reverse().ToList();
                _undoHistory.Clear();
                foreach (var item in temp)
                {
                    _undoHistory.Push(item);
                }
            }
        }

        private void Undo()
        {
            if (_undoHistory.Count == 0)
            {
                return;
            }

            var previousState = _undoHistory.Pop();
            _sprites.Clear();
            _sprites.AddRange(previousState.Select(s => new SpriteData(s.Id, s.Rect)));
            _nextId = _sprites.Count > 0 ? _sprites.Max(s => s.Id) + 1 : 0;
            _selectedIndex = -1;
            NotifyRectsChanged();
            QueueRedraw();
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (!EnableManualSelection || Target == null || Target.Texture == null)
            {
                return;
            }

            if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
            {
                // Ctrl+Z (Windows/Linux) or Command+Z (macOS) 撤销
                bool undoKey = keyEvent.Keycode == Key.Z &&
                              (keyEvent.CtrlPressed || keyEvent.MetaPressed);
                if (undoKey)
                {
                    Undo();
                    return;
                }

                if (keyEvent.Keycode == Key.Delete || keyEvent.Keycode == Key.Backspace)
                {
                    DeleteSelectedRect();
                }

                return;
            }

            if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed)
                {
                    GrabFocus();
                    if (TryHitRect(mouseButton.Position, out int index, out ResizeHandle handle))
                    {
                        _selectedIndex = index;
                        _resizeHandle = handle;
                        _resizing = handle != ResizeHandle.None;
                        if (_resizing)
                        {
                            SaveHistory(); // 开始调整前保存历史
                        }
                        _resizeStartRect = _sprites[index].Rect;
                    }
                    else
                    {
                        _selectedIndex = -1;
                        _dragging = true;
                        _dragStartLocal = mouseButton.Position;
                        _dragCurrentLocal = _dragStartLocal;
                    }

                    QueueRedraw();
                }
                else
                {
                    if (_resizing)
                    {
                        _resizing = false;
                        _resizeHandle = ResizeHandle.None;
                        NotifyRectsChanged();
                    }

                    if (_dragging)
                    {
                        _dragging = false;
                        _dragCurrentLocal = mouseButton.Position;
                        if (TryGetTextureRect(_dragStartLocal, _dragCurrentLocal, out Rect2 texRect)
                            && texRect.Size.X >= MinRectSize
                            && texRect.Size.Y >= MinRectSize)
                        {
                            SaveHistory(); // 添加新矩形前保存历史
                            _sprites.Add(new SpriteData(_nextId++, texRect));
                            _selectedIndex = _sprites.Count - 1;
                            NotifyRectsChanged();
                        }
                    }

                    QueueRedraw();
                }
            }
            else if (@event is InputEventMouseMotion mouseMotion && _dragging)
            {
                _dragCurrentLocal = mouseMotion.Position;
                QueueRedraw();
            }
            else if (@event is InputEventMouseMotion resizeMotion && _resizing)
            {
                if (TryGetTexturePoint(resizeMotion.Position, out Vector2 texPoint))
                {
                    Rect2 resized = ApplyResize(_resizeStartRect, texPoint, _resizeHandle, MinRectSize);
                    var sprite = _sprites[_selectedIndex];
                    sprite.Rect = resized;
                    _sprites[_selectedIndex] = sprite;
                    QueueRedraw();
                }
            }
            else if (@event is InputEventMouseMotion hoverMotion)
            {
                UpdateCursor(hoverMotion.Position);
            }
        }

        public override void _Draw()
        {
            if (Target == null || Target.Texture == null || _sprites.Count == 0)
            {
                DrawDragRect();
                return;
            }

            Vector2 texSize = Target.Texture.GetSize();
            if (texSize.X <= 0f || texSize.Y <= 0f)
            {
                return;
            }

            // 获取camera zoom，计算屏幕空间线宽（保持1像素）
            float zoom = Camera != null ? Camera.Zoom.X : 1f;
            float screenLineWidth = LineWidth / zoom;

            Rect2 drawRect = GetTextureDrawRect(Target, texSize);
            Vector2 scale = new Vector2(drawRect.Size.X / texSize.X, drawRect.Size.Y / texSize.Y);

            for (int i = 0; i < _sprites.Count; i++)
            {
                var sprite = _sprites[i];
                Rect2 rect = sprite.Rect;
                Vector2 pos = drawRect.Position + rect.Position * scale;
                Vector2 size = rect.Size * scale;
                Rect2 screenRect = SnapRect(new Rect2(pos, size));
                DrawRect(screenRect, PreviewColor, false, screenLineWidth);

                // 只在选中时显示ID（在矩形左上角附近）
                if (i == _selectedIndex)
                {
                    string idText = $"#{sprite.Id}";
                    Vector2 textPos = screenRect.Position + new Vector2(2, -2); // 稍微偏移
                    DrawString(ThemeDB.FallbackFont, textPos, idText, HorizontalAlignment.Left, -1, IdFontSize, PreviewColor);
                }
            }

            DrawHandles();

            DrawDragRect();
        }

        private void DrawDragRect()
        {
            if (!_dragging || Target == null || Target.Texture == null)
            {
                return;
            }

            float zoom = Camera != null ? Camera.Zoom.X : 1f;
            float screenLineWidth = LineWidth / zoom;

            Vector2 texSize = Target.Texture.GetSize();
            Rect2 drawRect = GetTextureDrawRect(Target, texSize);
            Rect2 localRect = RectFromPoints(_dragStartLocal, _dragCurrentLocal);

            Rect2 clipped = localRect.Intersection(drawRect);
            if (clipped.Size.X <= 0f || clipped.Size.Y <= 0f)
            {
                return;
            }

            DrawRect(SnapRect(clipped), PreviewColor, false, screenLineWidth);
        }

        private void DrawHandles()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _sprites.Count)
            {
                return;
            }

            float zoom = Camera != null ? Camera.Zoom.X : 1f;
            float screenLineWidth = LineWidth / zoom;
            // 设置最小尺寸，防止缩放太大时看不见
            float screenHandleSize = Mathf.Max(2f, HandlePixels / zoom);

            Vector2 texSize = Target.Texture.GetSize();
            Rect2 drawRect = GetTextureDrawRect(Target, texSize);
            Vector2 scale = new Vector2(drawRect.Size.X / texSize.X, drawRect.Size.Y / texSize.Y);

            Rect2 rect = _sprites[_selectedIndex].Rect;
            Rect2 screenRect = new Rect2(drawRect.Position + rect.Position * scale, rect.Size * scale);
            Vector2 handleSize = new Vector2(screenHandleSize, screenHandleSize);

            // 控制点位置：TopLeft, Top, TopRight, Left, Right, BottomLeft, Bottom, BottomRight
            // 调整为外侧偏移，使控制点完全在绿框外侧
            Vector2 offset = handleSize * 0.5f;
            Vector2[] points = new Vector2[]
            {
                new Vector2(screenRect.Position.X - offset.X, screenRect.Position.Y - offset.Y), // TopLeft
                new Vector2(screenRect.Position.X + screenRect.Size.X * 0.5f - offset.X, screenRect.Position.Y - offset.Y), // Top
                new Vector2(screenRect.Position.X + screenRect.Size.X - offset.X, screenRect.Position.Y - offset.Y), // TopRight
                new Vector2(screenRect.Position.X - offset.X, screenRect.Position.Y + screenRect.Size.Y * 0.5f - offset.Y), // Left
                new Vector2(screenRect.Position.X + screenRect.Size.X - offset.X, screenRect.Position.Y + screenRect.Size.Y * 0.5f - offset.Y), // Right
                new Vector2(screenRect.Position.X - offset.X, screenRect.Position.Y + screenRect.Size.Y - offset.Y), // BottomLeft
                new Vector2(screenRect.Position.X + screenRect.Size.X * 0.5f - offset.X, screenRect.Position.Y + screenRect.Size.Y - offset.Y), // Bottom
                new Vector2(screenRect.Position.X + screenRect.Size.X - offset.X, screenRect.Position.Y + screenRect.Size.Y - offset.Y) // BottomRight
            };

            foreach (Vector2 p in points)
            {
                Rect2 handleRect = new Rect2(p, handleSize);
                DrawRect(SnapRect(handleRect), PreviewColor, false, screenLineWidth);
            }
        }

        private Rect2 SnapRect(Rect2 rect)
        {
            if (!SnapToPixels)
            {
                return rect;
            }

            Vector2 pos = new Vector2(Mathf.Round(rect.Position.X), Mathf.Round(rect.Position.Y));
            Vector2 size = new Vector2(Mathf.Round(rect.Size.X), Mathf.Round(rect.Size.Y));
            return new Rect2(pos, size);
        }

        private void NotifyRectsChanged()
        {
            var rects = _sprites.Select(s => s.Rect).ToList();
            RectsChanged?.Invoke(rects);
        }

        private void DeleteSelectedRect()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _sprites.Count)
            {
                return;
            }

            SaveHistory(); // 删除前保存历史
            _sprites.RemoveAt(_selectedIndex);
            _selectedIndex = -1;
            NotifyRectsChanged();
            QueueRedraw();
        }

        private bool TryHitRect(Vector2 localPoint, out int index, out ResizeHandle handle)
        {
            index = -1;
            handle = ResizeHandle.None;
            if (!TryGetTexturePoint(localPoint, out Vector2 texPoint))
            {
                return false;
            }

            Vector2 texHandle = GetHandleSizeInTexture();
            for (int i = _sprites.Count - 1; i >= 0; i--)
            {
                Rect2 rect = _sprites[i].Rect;
                if (!IsPointNearRect(rect, texPoint, texHandle, out handle))
                {
                    continue;
                }

                index = i;
                return true;
            }

            return false;
        }

        private bool TryHitRectBody(Vector2 localPoint, out int index)
        {
            index = -1;
            if (!TryGetTexturePoint(localPoint, out Vector2 texPoint))
            {
                return false;
            }

            for (int i = _sprites.Count - 1; i >= 0; i--)
            {
                if (_sprites[i].Rect.HasPoint(texPoint))
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        private void UpdateCursor(Vector2 localPoint)
        {
            if (TryHitRect(localPoint, out _, out ResizeHandle handle))
            {
                MouseDefaultCursorShape = GetCursorShape(handle);
            }
            else
            {
                MouseDefaultCursorShape = CursorShape.Arrow;
            }
        }

        private static CursorShape GetCursorShape(ResizeHandle handle)
        {
            switch (handle)
            {
                case ResizeHandle.Left:
                case ResizeHandle.Right:
                    return CursorShape.Hsize;
                case ResizeHandle.Top:
                case ResizeHandle.Bottom:
                    return CursorShape.Vsize;
                case ResizeHandle.TopLeft:
                case ResizeHandle.BottomRight:
                    return CursorShape.Fdiagsize;
                case ResizeHandle.TopRight:
                case ResizeHandle.BottomLeft:
                    return CursorShape.Bdiagsize;
                default:
                    return CursorShape.Arrow;
            }
        }

        private bool TryGetTexturePoint(Vector2 localPoint, out Vector2 texPoint)
        {
            texPoint = Vector2.Zero;
            Vector2 texSize = Target.Texture.GetSize();
            if (texSize.X <= 0f || texSize.Y <= 0f)
            {
                return false;
            }

            Rect2 drawRect = GetTextureDrawRect(Target, texSize);
            if (!drawRect.HasPoint(localPoint))
            {
                return false;
            }

            Vector2 scale = new Vector2(drawRect.Size.X / texSize.X, drawRect.Size.Y / texSize.Y);
            texPoint = (localPoint - drawRect.Position) / scale;
            texPoint.X = Mathf.Clamp(texPoint.X, 0f, texSize.X);
            texPoint.Y = Mathf.Clamp(texPoint.Y, 0f, texSize.Y);
            return true;
        }

        private Vector2 GetHandleSizeInTexture()
        {
            Vector2 texSize = Target.Texture.GetSize();
            Rect2 drawRect = GetTextureDrawRect(Target, texSize);
            Vector2 scale = new Vector2(drawRect.Size.X / texSize.X, drawRect.Size.Y / texSize.Y);

            // 根据camera zoom调整控制点大小，保持屏幕上固定像素，设置最小值
            float zoom = Camera != null ? Camera.Zoom.X : 1f;
            float screenHandleSize = Mathf.Max(2f, HandlePixels / zoom);

            float handleX = scale.X > 0f ? screenHandleSize / scale.X : screenHandleSize;
            float handleY = scale.Y > 0f ? screenHandleSize / scale.Y : screenHandleSize;
            return new Vector2(handleX, handleY);
        }

        private static bool IsPointNearRect(Rect2 rect, Vector2 point, Vector2 threshold, out ResizeHandle handle)
        {
            handle = ResizeHandle.None;

            float left = rect.Position.X;
            float top = rect.Position.Y;
            float right = rect.Position.X + rect.Size.X;
            float bottom = rect.Position.Y + rect.Size.Y;

            bool nearLeft = Mathf.Abs(point.X - left) <= threshold.X;
            bool nearRight = Mathf.Abs(point.X - right) <= threshold.X;
            bool nearTop = Mathf.Abs(point.Y - top) <= threshold.Y;
            bool nearBottom = Mathf.Abs(point.Y - bottom) <= threshold.Y;

            bool withinX = point.X >= left - threshold.X && point.X <= right + threshold.X;
            bool withinY = point.Y >= top - threshold.Y && point.Y <= bottom + threshold.Y;
            if (!withinX || !withinY)
            {
                return false;
            }

            if (nearLeft && nearTop) handle = ResizeHandle.TopLeft;
            else if (nearRight && nearTop) handle = ResizeHandle.TopRight;
            else if (nearLeft && nearBottom) handle = ResizeHandle.BottomLeft;
            else if (nearRight && nearBottom) handle = ResizeHandle.BottomRight;
            else if (nearLeft) handle = ResizeHandle.Left;
            else if (nearRight) handle = ResizeHandle.Right;
            else if (nearTop) handle = ResizeHandle.Top;
            else if (nearBottom) handle = ResizeHandle.Bottom;
            return handle != ResizeHandle.None;
        }

        private static Rect2 ApplyResize(Rect2 rect, Vector2 point, ResizeHandle handle, float minSize)
        {
            float left = rect.Position.X;
            float top = rect.Position.Y;
            float right = rect.Position.X + rect.Size.X;
            float bottom = rect.Position.Y + rect.Size.Y;

            if (handle == ResizeHandle.Left || handle == ResizeHandle.TopLeft || handle == ResizeHandle.BottomLeft)
            {
                left = Mathf.Min(point.X, right - minSize);
            }
            if (handle == ResizeHandle.Right || handle == ResizeHandle.TopRight || handle == ResizeHandle.BottomRight)
            {
                right = Mathf.Max(point.X, left + minSize);
            }
            if (handle == ResizeHandle.Top || handle == ResizeHandle.TopLeft || handle == ResizeHandle.TopRight)
            {
                top = Mathf.Min(point.Y, bottom - minSize);
            }
            if (handle == ResizeHandle.Bottom || handle == ResizeHandle.BottomLeft || handle == ResizeHandle.BottomRight)
            {
                bottom = Mathf.Max(point.Y, top + minSize);
            }

            return new Rect2(left, top, right - left, bottom - top);
        }

        private enum ResizeHandle
        {
            None,
            Left,
            Right,
            Top,
            Bottom,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        private bool TryGetTextureRect(Vector2 localA, Vector2 localB, out Rect2 texRect)
        {
            texRect = new Rect2();
            Vector2 texSize = Target.Texture.GetSize();
            if (texSize.X <= 0f || texSize.Y <= 0f)
            {
                return false;
            }

            Rect2 drawRect = GetTextureDrawRect(Target, texSize);
            Rect2 localRect = RectFromPoints(localA, localB);
            Rect2 clipped = localRect.Intersection(drawRect);
            if (clipped.Size.X <= 0f || clipped.Size.Y <= 0f)
            {
                return false;
            }

            Vector2 scale = new Vector2(drawRect.Size.X / texSize.X, drawRect.Size.Y / texSize.Y);
            Vector2 texPos = (clipped.Position - drawRect.Position) / scale;
            Vector2 texSizeOut = clipped.Size / scale;

            texPos.X = Mathf.Clamp(texPos.X, 0f, texSize.X);
            texPos.Y = Mathf.Clamp(texPos.Y, 0f, texSize.Y);
            texSizeOut.X = Mathf.Clamp(texSizeOut.X, 0f, texSize.X - texPos.X);
            texSizeOut.Y = Mathf.Clamp(texSizeOut.Y, 0f, texSize.Y - texPos.Y);

            texRect = new Rect2(texPos, texSizeOut);
            return texRect.Size.X > 0f && texRect.Size.Y > 0f;
        }

        private static Rect2 RectFromPoints(Vector2 a, Vector2 b)
        {
            Vector2 pos = new Vector2(Mathf.Min(a.X, b.X), Mathf.Min(a.Y, b.Y));
            Vector2 size = new Vector2(Mathf.Abs(a.X - b.X), Mathf.Abs(a.Y - b.Y));
            return new Rect2(pos, size);
        }

        private static Rect2 GetTextureDrawRect(TextureRect textureRect, Vector2 texSize)
        {
            Vector2 containerSize = textureRect.Size;
            TextureRect.StretchModeEnum mode = textureRect.StretchMode;

            switch (mode)
            {
                case TextureRect.StretchModeEnum.Scale:
                case TextureRect.StretchModeEnum.Tile:
                    return new Rect2(Vector2.Zero, containerSize);
                case TextureRect.StretchModeEnum.Keep:
                    return new Rect2(Vector2.Zero, texSize);
                case TextureRect.StretchModeEnum.KeepCentered:
                    {
                        Vector2 pos = (containerSize - texSize) * 0.5f;
                        return new Rect2(pos, texSize);
                    }
                case TextureRect.StretchModeEnum.KeepAspect:
                case TextureRect.StretchModeEnum.KeepAspectCentered:
                case TextureRect.StretchModeEnum.KeepAspectCovered:
                    {
                        float scale = (mode == TextureRect.StretchModeEnum.KeepAspectCovered)
                            ? Mathf.Max(containerSize.X / texSize.X, containerSize.Y / texSize.Y)
                            : Mathf.Min(containerSize.X / texSize.X, containerSize.Y / texSize.Y);

                        Vector2 size = texSize * scale;
                        Vector2 pos = mode == TextureRect.StretchModeEnum.KeepAspectCentered
                            ? (containerSize - size) * 0.5f
                            : Vector2.Zero;

                        return new Rect2(pos, size);
                    }
                default:
                    return new Rect2(Vector2.Zero, containerSize);
            }
        }
    }
}