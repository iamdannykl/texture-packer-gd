# Texture Packer GD

**一个为没有内置图集切割功能的游戏引擎准备的精灵图集切割工具**

[![Godot Engine](https://img.shields.io/badge/Godot-4.0+-blue.svg)](https://godotengine.org/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Language: C#](https://img.shields.io/badge/Language-C%23-purple.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)

## 🎯 项目简介

Texture Packer GD 是一个基于 Godot 4 和 C# 开发的开源图集切割工具。它专为那些缺少内置 Sprite Atlas 切割功能的游戏引擎设计，帮助开发者快速、精确地从大型图集中提取精灵素材。

### 为什么需要这个工具？

许多游戏引擎（如 Unity、Godot）自带强大的图集切割工具，但也有很多引擎和框架并不提供这样的功能。当你拿到一个包含数十甚至上百个精灵的大图集时，手动记录每个精灵的坐标和尺寸是一件非常痛苦的事情。

**Texture Packer GD 解决了这个问题！**

## ✨ 核心特性

### 🤖 智能自动切割

- **基于 Alpha 通道的 Flood Fill 算法**：自动检测图集中的非透明连通区域
- **可配置阈值**：调整透明度阈值、最小区域面积和尺寸过滤器
- **嵌套矩形过滤**：自动移除包含关系的冗余矩形

### ✏️ 精确手动编辑

- **拖拽创建**：鼠标拖拽即可手动添加切割区域
- **8点调整手柄**：提供 4 个角点 + 4 个边中点的调整手柄
- **系统原生光标**：鼠标悬停时自动显示对应的调整光标（↔ ↕ ⤢ ⤡）
- **键盘删除**：选中后按 Delete/Backspace 删除区域
- **像素对齐**：可选的像素级精确对齐功能

### 🎨 专业预览系统

- **实时预览**：所有切割区域以绿框形式实时显示
- **缩放自适应**：线宽和控制点大小随相机缩放自动调整，始终保持 1 像素清晰度
- **ID 显示**：选中区域时显示对应的精灵 ID
- **可配置颜色**：自定义预览框颜色和 ID 字体大小

### 📦 完整的导入/导出

- **JSON 格式导出**：包含精灵 ID、坐标、尺寸和图集路径
- **JSON 格式导入**：支持重新加载之前的切割方案
- **跨平台路径**：自动处理不同操作系统的文件路径

### ⚙️ 强大的控制功能

- **撤销/重做**：支持 Ctrl+Z (Windows/Linux) 或 ⌘+Z (macOS) 撤销操作（最多 50 步）
- **相机控制**：鼠标中键拖拽平移，滚轮缩放，支持缩放到光标位置
- **Inspector 配置**：所有参数均可通过 Godot Inspector 调整

## 🚀 快速开始

### 环境要求

- Godot 4.0 或更高版本
- .NET 6.0 或更高版本
- 支持 Windows、macOS、Linux

### 安装步骤

1. **克隆仓库**

```bash
git clone https://github.com/yourusername/texture-packer-gd.git
cd texture-packer-gd
```

2. **在 Godot 中打开项目**

```bash
# 使用 Godot 4.0+ 打开 project.godot
godot project.godot
```

3. **构建 C# 项目**
   在 Godot 编辑器中点击 "Build" 按钮构建 C# 项目。

### 基本使用

#### 1. 设置场景

创建一个场景，包含以下节点：

```
Main (Node)
├── Camera2D (Locator.cs)
├── TextureRect
└── Slicer (Slicer.cs)
    ├── ImportButton
    ├── SliceButton
    ├── ExportButton
    └── ImportJsonButton
```

#### 2. 连接信号

将按钮的 `pressed()` 信号连接到 Slicer 节点的对应方法：

- **Import Button** → `OnImportButtonPressed()`
- **Slice Button** → `OnSliceButtonPressed()`
- **Export JSON** → `OnExportButtonPressed()`
- **Import JSON** → `OnImportJsonButtonPressed()`

#### 3. 配置参数

在 Inspector 中调整 Slicer 参数：

| 参数                   | 说明                            | 默认值          |
| ---------------------- | ------------------------------- | --------------- |
| Import Texture Rect    | 用于显示图集的 TextureRect 节点 | -               |
| Preview Color          | 预览框颜色                      | 绿色(0,1,0,0.8) |
| Preview Line Width     | 预览框线宽                      | 1.0             |
| Alpha Threshold        | 透明度阈值                      | 0.1             |
| Min Region Area        | 最小区域面积（像素）            | 4               |
| Min Region Size        | 最小区域尺寸（像素）            | 1               |
| Id Font Size           | ID 文字大小                     | 16              |
| Filter Contained Rects | 过滤嵌套矩形                    | true            |
| Snap Preview To Pixels | 像素对齐                        | true            |

## 📋 操作指南

### 导入图集

1. 点击 "Import" 按钮
2. 选择图集文件（支持 PNG、JPG、WebP、TGA、BMP）
3. 图集将显示在 TextureRect 中

### 自动切割

1. 导入图集后，点击 "Slice" 按钮
2. 工具将自动检测所有非透明区域
3. 绿色框显示切割结果

### 手动调整

- **添加区域**：在空白处拖拽创建新的切割区域
- **调整大小**：点击选中后，拖动 8 个控制点调整
- **删除区域**：选中后按 Delete 或 Backspace
- **撤销操作**：Ctrl+Z (Windows/Linux) 或 ⌘+Z (macOS)

### 导出 JSON

1. 编辑完成后点击 "Export JSON"
2. 选择保存位置
3. 生成的 JSON 文件包含所有精灵信息

### 导入 JSON

1. 点击 "Import JSON"
2. 选择之前导出的 JSON 文件
3. 自动加载图集和切割数据

## 📊 JSON 格式

### 导出示例

```json
{
  "atlas": "/path/to/your/spritesheet.png",
  "sprites": [
    {
      "id": 0,
      "x": 10,
      "y": 20,
      "width": 64,
      "height": 64
    },
    {
      "id": 1,
      "x": 100,
      "y": 50,
      "width": 32,
      "height": 32
    }
  ]
}
```

### 字段说明

| 字段    | 类型   | 说明                            |
| ------- | ------ | ------------------------------- |
| atlas   | string | 图集文件的完整路径              |
| sprites | array  | 精灵数据数组                    |
| id      | int    | 精灵唯一标识符                  |
| x       | int    | 精灵左上角 X 坐标（相对于图集） |
| y       | int    | 精灵左上角 Y 坐标（相对于图集） |
| width   | int    | 精灵宽度（像素）                |
| height  | int    | 精灵高度（像素）                |

### 在其他引擎中使用

导出的 JSON 可以在任何支持 JSON 解析的游戏引擎中使用：

**C# 示例**

```csharp
// 加载 JSON
var json = LoadJsonFile("sprites.json");
var atlas = LoadTexture(json.atlas);

foreach (var sprite in json.sprites)
{
    var rect = new Rectangle(sprite.x, sprite.y, sprite.width, sprite.height);
    var texture = atlas.GetRegion(rect);
    // 使用 texture...
}
```

**JavaScript 示例**

```javascript
// 加载 JSON
const data = await fetch("sprites.json").then((r) => r.json());
const atlas = await loadImage(data.atlas);

data.sprites.forEach((sprite) => {
  const texture = ctx.getImageData(
    sprite.x,
    sprite.y,
    sprite.width,
    sprite.height,
  );
  // 使用 texture...
});
```

## ⌨️ 快捷键

| 快捷键             | 功能           |
| ------------------ | -------------- |
| 鼠标中键拖拽       | 平移画布       |
| 鼠标滚轮           | 缩放画布       |
| 鼠标左键拖拽       | 创建切割区域   |
| 鼠标左键点击控制点 | 调整区域大小   |
| Delete / Backspace | 删除选中区域   |
| Ctrl+Z / ⌘+Z       | 撤销上一步操作 |

## 🎓 技术实现

### 核心算法

#### Flood Fill 切割算法

使用基于栈的 Flood Fill 算法检测非透明连通区域：

```csharp
private static Rect2 FloodFillBounds(Image image, int startX, int startY,
                                      int width, int height, float alphaThreshold)
{
    Stack<Vector2I> stack = new Stack<Vector2I>();
    // 4方向扩散检测透明度
    // 记录边界框 minX, minY, maxX, maxY
    // 返回精确的矩形区域
}
```

#### 像素完美渲染

通过反向缩放补偿确保线条在任何缩放级别都保持 1 像素清晰：

```csharp
float zoom = Camera.Zoom.X;
float screenLineWidth = LineWidth / zoom;  // 反向补偿
float screenHandleSize = Max(2f, HandlePixels / zoom);  // 最小 2 像素
```

#### 坐标系统

使用标准图像坐标系：

- 原点 (0,0) 在左上角
- X 轴向右为正
- Y 轴向下为正

与 Godot、Unity、PNG 等标准一致，无需额外转换。

### 架构设计

```
Slicer (主控制器)
├── SpriteData (数据模型)
├── SlicePreview (预览渲染)
│   ├── 绘制逻辑
│   ├── 输入处理
│   ├── 撤销栈
│   └── 控制点系统
└── 算法模块
    ├── Flood Fill
    ├── 嵌套过滤
    └── 坐标转换
```

## 🤝 贡献指南

欢迎贡献代码、报告问题或提出新功能建议！

### 如何贡献

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启 Pull Request

### 报告问题

在 [Issues](https://github.com/yourusername/texture-packer-gd/issues) 页面提交问题时，请包含：

- Godot 版本
- .NET SDK 版本
- 操作系统
- 复现步骤
- 预期行为
- 实际行为
- 截图（如果适用）

## 📝 待办事项

- [ ] 添加多选和批量操作
- [ ] 支持基于网格的切割模式
- [ ] 添加命令行接口（CLI）
- [ ] 支持更多导出格式（XML, Plain Text）
- [ ] 添加精灵命名功能
- [ ] 实现重做功能（Ctrl+Y）
- [ ] 添加精灵预览缩略图
- [ ] 支持动画帧序列检测

## 📄 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件

MIT 许可证允许您：

- ✅ 商业使用
- ✅ 修改代码
- ✅ 分发
- ✅ 私人使用

## 🙏 致谢

- [Godot Engine](https://godotengine.org/) - 强大的开源游戏引擎
- [Pixelorama](https://github.com/Orama-Interactive/Pixelorama) - 像素完美渲染的灵感来源

## 📧 联系方式

如有问题或建议，欢迎通过以下方式联系：

- 提交 [Issue](https://github.com/yourusername/texture-packer-gd/issues)
- 发送邮件至：your.email@example.com

---

**用 ❤️ 和 ☕ 制作，献给所有为游戏开发而奋斗的程序员们！**

如果这个工具帮助了你，请给个 ⭐ Star 支持一下！
