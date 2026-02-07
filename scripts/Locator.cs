using Godot;
using System;
/// <summary>
/// 定位器，实现画布的平移与缩放
/// </summary>
public partial class Locator : Camera2D
{
	private bool _isPanning = false;
	private Vector2 _lastMousePos;
	[Export]
	public float ZoomStep = 0.1f;
	[Export]
	public float MinZoom = 0.1f;
	[Export]
	public float MaxZoom = 10f;
	[Export]
	public bool WheelReverse = false;
	public override void _Input(InputEvent @event)
	{
		// 鼠标中键按下：开始平移
		if (@event is InputEventMouseButton mouseButton)
		{
			// 鼠标滚轮缩放：画布放大/缩小
			if (mouseButton.ButtonIndex == MouseButton.WheelUp || mouseButton.ButtonIndex == MouseButton.WheelDown)
			{
				if (mouseButton.Pressed)
				{
					Vector2 mouseWorldBefore = GetGlobalMousePosition();
					float direction = mouseButton.ButtonIndex == MouseButton.WheelUp ? (WheelReverse ? -1f : 1f) : (WheelReverse ? 1f : -1f);
					float zoomFactor = 1f + (direction * ZoomStep);
					Vector2 targetZoom = Zoom * zoomFactor;
					float clampedX = Mathf.Clamp(targetZoom.X, MinZoom, MaxZoom);
					float clampedY = Mathf.Clamp(targetZoom.Y, MinZoom, MaxZoom);
					Zoom = new Vector2(clampedX, clampedY);
					Vector2 mouseWorldAfter = GetGlobalMousePosition();
					Position += mouseWorldBefore - mouseWorldAfter;
				}

				return;
			}

			if (mouseButton.ButtonIndex == MouseButton.Middle)
			{
				if (mouseButton.Pressed)
				{
					_isPanning = true;
					_lastMousePos = mouseButton.Position;
					SetProcess(true); // 确保 _Process 被调用（可选，也可在 _Input 中处理）
				}
				else
				{
					_isPanning = false;
				}
			}
		}

		// 鼠标移动：如果正在平移，则更新相机位置
		if (@event is InputEventMouseMotion mouseMotion && _isPanning)
		{
			Vector2 currentPos = mouseMotion.Position;
			Vector2 delta = currentPos - _lastMousePos;

			// 将屏幕像素偏移转换为世界坐标偏移（除以当前缩放）
			Vector2 worldDelta = delta / Zoom;

			// 反向移动相机（鼠标右移 → 相机左移 → 画面右移）
			Position -= worldDelta;

			_lastMousePos = currentPos;
		}
	}
}
