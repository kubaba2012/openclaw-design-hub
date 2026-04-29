using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using OpenClaw.DesignHub.Modules.Channel;
using OpenClaw.DesignHub.ViewModels;

namespace OpenClaw.DesignHub;

/// <summary>
/// 主窗口 - 灵动岛悬浮球
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly MainViewModel _viewModel;
    private bool _isExpanded;
    private Point _dragStart;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));

                if (value)
                    Expand();
                else
                    Collapse();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow()
    {
        InitializeComponent();

        // 注册转换器
        Resources.Add("BoolToColorConverter", new BoolToColorConverter());
        Resources.Add("InverseBoolToVisConverter", new InverseBoolToVisibilityConverter());
        Resources.Add("RoleToVisConverter", new RoleToVisibilityConverter());

        // 初始化 ViewModel
        var channel = new ChannelClient();
        _viewModel = new MainViewModel(channel);
        DataContext = _viewModel;

        // 订阅展开命令
        MouseDown += OnMouseDown;
    }

    /// <summary>
    /// 鼠标按下 - 开始拖动或展开
    /// </summary>
    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && !IsExpanded)
        {
            _dragStart = e.GetPosition(this);
        }
    }

    /// <summary>
    /// 拖动开始
    /// </summary>
    private void OnDragStart(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            if (!IsExpanded)
            {
                // 展开
                IsExpanded = true;
                e.Handled = true;
            }
            else
            {
                // 拖动
                DragMove();
            }
        }
    }

    /// <summary>
    /// 收起按钮点击
    /// </summary>
    private void OnCollapseClick(object sender, RoutedEventArgs e)
    {
        IsExpanded = false;
    }

    /// <summary>
    /// 输入框按键
    /// </summary>
    private void OnInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            // 发送消息
            if (_viewModel.SendMessageCommand.CanExecute(null))
            {
                _viewModel.SendMessageCommand.Execute(null);
            }
            e.Handled = true;
        }
    }

    /// <summary>
    /// 展开
    /// </summary>
    private void Expand()
    {
        var storyboard = (Storyboard)Resources["ExpandStoryboard"];
        storyboard.Begin();
    }

    /// <summary>
    /// 收起
    /// </summary>
    private void Collapse()
    {
        var storyboard = (Storyboard)Resources["CollapseStoryboard"];
        storyboard.Begin();
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

#region 转换器

/// <summary>
/// 布尔值转颜色（连接状态指示）
/// </summary>
public class BoolToColorConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value is true ? Brushes.LimeGreen : Brushes.Red;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值反转转可见性
/// </summary>
public class InverseBoolToVisibilityConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value is false ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 角色转可见性
/// </summary>
public class RoleToVisibilityConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        var role = value as string;
        var expected = parameter as string;
        return role == expected ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

#endregion