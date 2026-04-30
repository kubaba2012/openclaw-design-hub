using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenClaw.DesignHub.Components;

/// <summary>
/// 悬浮球拖拽行为（Attached Behavior）
/// 
/// 使用方式：
/// &lt;Border behaviors:DraggableBubbleBehavior.IsEnabled="True"
///                behaviors:DraggableBubbleBehavior.ClickExpand="True" /&gt;
/// </summary>
public static class DraggableBubbleBehavior
{
    #region IsEnabled

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DraggableBubbleBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    #endregion

    #region ClickExpand

    /// <summary>
    /// 单击是否展开
    /// </summary>
    public static readonly DependencyProperty ClickExpandProperty =
        DependencyProperty.RegisterAttached(
            "ClickExpand",
            typeof(bool),
            typeof(DraggableBubbleBehavior),
            new PropertyMetadata(true));

    public static bool GetClickExpand(DependencyObject obj) => (bool)obj.GetValue(ClickExpandProperty);
    public static void SetClickExpand(DependencyObject obj, bool value) => obj.SetValue(ClickExpandProperty, value);

    #endregion

    #region ClickThreshold

    /// <summary>
    /// 判定为点击的最大移动距离（像素）
    /// </summary>
    public static readonly DependencyProperty ClickThresholdProperty =
        DependencyProperty.RegisterAttached(
            "ClickThreshold",
            typeof(double),
            typeof(DraggableBubbleBehavior),
            new PropertyMetadata(8.0));

    public static double GetClickThreshold(DependencyObject obj) => (double)obj.GetValue(ClickThresholdProperty);
    public static void SetClickThreshold(DependencyObject obj, double value) => obj.SetValue(ClickThresholdProperty, value);

    #endregion

    #region DragStartedCommand

    public static readonly DependencyProperty DragStartedCommandProperty =
        DependencyProperty.RegisterAttached(
            "DragStartedCommand",
            typeof(ICommand),
            typeof(DraggableBubbleBehavior),
            new PropertyMetadata(null));

    public static ICommand GetDragStartedCommand(DependencyObject obj) => (ICommand)obj.GetValue(DragStartedCommandProperty);
    public static void SetDragStartedCommand(DependencyObject obj, ICommand value) => obj.SetValue(DragStartedCommandProperty, value);

    #endregion

    #region ClickCommand

    public static readonly DependencyProperty ClickCommandProperty =
        DependencyProperty.RegisterAttached(
            "ClickCommand",
            typeof(ICommand),
            typeof(DraggableBubbleBehavior),
            new PropertyMetadata(null));

    public static ICommand GetClickCommand(DependencyObject obj) => (ICommand)obj.GetValue(ClickCommandProperty);
    public static void SetClickCommand(DependencyObject obj, ICommand value) => obj.SetValue(ClickCommandProperty, value);

    #endregion

    #region Dragging

    /// <summary>
    /// 是否正在拖拽中
    /// </summary>
    public static readonly DependencyProperty IsDraggingProperty =
        DependencyProperty.RegisterAttached(
            "IsDragging",
            typeof(bool),
            typeof(DraggableBubbleBehavior),
            new PropertyMetadata(false));

    public static bool GetIsDragging(DependencyObject obj) => (bool)obj.GetValue(IsDraggingProperty);
    public static void SetIsDragging(DependencyObject obj, bool value) => obj.SetValue(IsDraggingProperty, value);

    #endregion

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;

        if ((bool)e.NewValue)
        {
            element.MouseLeftButtonDown += OnMouseDown;
            element.MouseMove += OnMouseMove;
            element.MouseLeftButtonUp += OnMouseUp;
            element.MouseLeave += OnMouseLeave;
        }
        else
        {
            element.MouseLeftButtonDown -= OnMouseDown;
            element.MouseMove -= OnMouseMove;
            element.MouseLeftButtonUp -= OnMouseUp;
            element.MouseLeave -= OnMouseLeave;
        }
    }

    private static readonly Dictionary<UIElement, DragState> _states = new();

    private static DragState GetState(UIElement element)
    {
        if (!_states.TryGetValue(element, out var state))
        {
            state = new DragState();
            _states[element] = state;
        }
        return state;
    }

    private static void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not UIElement element) return;
        var state = GetState(element);
        state.Reset();

        // 记录鼠标在屏幕上的起始位置
        state.ScreenStart = e.GetPosition(null);
        state.ScreenCurrent = state.ScreenStart; // 初始化当前位置
        state.StartPosition = e.GetPosition(element);

        // 记录窗口起始位置
        var window = Window.GetWindow(element);
        if (window != null)
        {
            state.WindowStart = new Point(window.Left, window.Top);
        }

        state.IsDragging = true;
        element.CaptureMouse();
        SetIsDragging(element, true);

        var cmd = GetDragStartedCommand(element);
        if (cmd?.CanExecute(null) == true)
            cmd.Execute(null);

        e.Handled = true;
    }

    private static void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not UIElement element) return;
        var state = GetState(element);

        if (!state.IsDragging) return;

        // 更新当前屏幕位置
        state.ScreenCurrent = e.GetPosition(null);

        // 计算屏幕坐标偏移
        var deltaX = state.ScreenCurrent.X - state.ScreenStart.X;
        var deltaY = state.ScreenCurrent.Y - state.ScreenStart.Y;

        var window = Window.GetWindow(element);
        if (window != null)
        {
            window.Left = state.WindowStart.X + deltaX;
            window.Top = state.WindowStart.Y + deltaY;
        }
    }

    private static void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not UIElement element) return;
        var state = GetState(element);

        if (!state.IsDragging)
        {
            element.ReleaseMouseCapture();
            return;
        }

        state.IsDragging = false;
        element.ReleaseMouseCapture();
        SetIsDragging(element, false);

        // 用屏幕坐标计算移动距离
        var distance = (state.ScreenCurrent - state.ScreenStart).Length;
        var threshold = GetClickThreshold(element);
        var clickExpand = GetClickExpand(element);

        // 调试日志
        System.Diagnostics.Debug.WriteLine($"[DragBehavior] Distance={distance:F1}, Threshold={threshold}, ClickExpand={clickExpand}");

        if (distance < threshold && clickExpand)
        {
            System.Diagnostics.Debug.WriteLine("[DragBehavior] Raising Click event");
            var cmd = GetClickCommand(element);
            if (cmd?.CanExecute(null) == true)
                cmd.Execute(null);
            else
                BubbleEvents.RaiseClicked(element);
        }

        e.Handled = true;
    }

    private static void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is not UIElement element) return;
        var state = GetState(element);

        if (state.IsDragging)
        {
            state.IsDragging = false;
            element.ReleaseMouseCapture();
            SetIsDragging(element, false);
        }
    }

    private class DragState
    {
        public Point StartPosition;      // 鼠标在元素内的起始位置（备用）
        public Point ScreenStart;        // 鼠标在屏幕上的起始位置
        public Point ScreenCurrent;      // 鼠标在屏幕上的当前位置
        public Point WindowStart;        // 窗口起始位置
        public bool IsDragging;

        public void Reset()
        {
            StartPosition = default;
            ScreenStart = default;
            ScreenCurrent = default;
            WindowStart = default;
            IsDragging = false;
        }
    }
}

/// <summary>
/// 悬浮球事件路由（用于解耦）
/// </summary>
public static class BubbleEvents
{
    public static event EventHandler<UIElement>? Clicked;

    public static void RaiseClicked(UIElement element)
    {
        Clicked?.Invoke(null, element);
    }
}