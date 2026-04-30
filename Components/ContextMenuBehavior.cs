using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenClaw.DesignHub.Components;

/// <summary>
/// 右键菜单行为（Attached Behavior）
/// 
/// 使用方式：
/// &lt;Border behaviors:ContextMenuBehavior.IsEnabled="True"
///                behaviors:ContextMenuBehavior.MenuItemsSource="{Binding ContextMenuItems}" /&gt;
/// </summary>
public static class ContextMenuBehavior
{
    #region IsEnabled

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(ContextMenuBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    #endregion

    #region MenuItemsSource

    public static readonly DependencyProperty MenuItemsSourceProperty =
        DependencyProperty.RegisterAttached(
            "MenuItemsSource",
            typeof(object),
            typeof(ContextMenuBehavior),
            new PropertyMetadata(null, OnMenuItemsSourceChanged));

    public static object GetMenuItemsSource(DependencyObject obj) => obj.GetValue(MenuItemsSourceProperty);
    public static void SetMenuItemsSource(DependencyObject obj, object value) => obj.SetValue(MenuItemsSourceProperty, value);

    #endregion

    #region RightClickCommand

    public static readonly DependencyProperty RightClickCommandProperty =
        DependencyProperty.RegisterAttached(
            "RightClickCommand",
            typeof(ICommand),
            typeof(ContextMenuBehavior),
            new PropertyMetadata(null));

    public static ICommand GetRightClickCommand(DependencyObject obj) => (ICommand)obj.GetValue(RightClickCommandProperty);
    public static void SetRightClickCommand(DependencyObject obj, ICommand value) => obj.SetValue(RightClickCommandProperty, value);

    #endregion

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;

        if ((bool)e.NewValue)
        {
            element.MouseRightButtonDown += OnMouseRightButtonDown;
        }
        else
        {
            element.MouseRightButtonDown -= OnMouseRightButtonDown;
        }
    }

    private static void OnMenuItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // 当菜单数据源变化时，可以触发更新
    }

    private static void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element) return;

        var cmd = GetRightClickCommand(element);
        if (cmd?.CanExecute(null) == true)
        {
            cmd.Execute(null);
            e.Handled = true;
        }

        // 抛出右键点击事件
        RightClickEvents.RaiseRightClicked(element);
    }
}

/// <summary>
/// 右键菜单事件路由
/// </summary>
public static class RightClickEvents
{
    public static event EventHandler<FrameworkElement>? RightClicked;

    public static void RaiseRightClicked(FrameworkElement element)
    {
        RightClicked?.Invoke(null, element);
    }
}

/// <summary>
/// 菜单项模型
/// </summary>
public class ContextMenuItem
{
    public string Header { get; set; } = string.Empty;
    public ICommand? Command { get; set; }
    public object? CommandParameter { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsSeparator { get; set; }
    public List<ContextMenuItem> Items { get; set; } = new();
}