using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using OpenClaw.DesignHub.Modules.Channel;
using OpenClaw.DesignHub.ViewModels;

namespace OpenClaw.DesignHub;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly MainViewModel _viewModel;
    private bool _isExpanded;
    private bool _isDragging;
    private Point _dragStartPoint;
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OpenClaw.DesignHub", "debug.log");

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            _isExpanded = value;
            OnPropertyChanged(nameof(IsExpanded));
            if (value)
                Expand();
            else
                Collapse();
            Log($"IsExpanded = {value}");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
        Log("MainWindow ctor start");

        InitializeComponent();
        Log("InitializeComponent done, IsExpanded=" + _isExpanded);

        Resources.Add("BoolToColorConverter", new BoolToColorConverter());
        Resources.Add("InverseBoolToVisConverter", new InverseBoolToVisibilityConverter());
        Resources.Add("RoleToVisConverter", new RoleToVisibilityConverter());

        var channel = new ChannelClient();
        _viewModel = new MainViewModel(channel);
        DataContext = _viewModel;

        Log("MainWindow ctor done");
    }

    private void OnWindowMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;

        if (!_isExpanded)
        {
            Log("Left click: expanding");
            IsExpanded = true;
            e.Handled = true;
            return;
        }

        // Expanded: start drag
        Log("Left click: starting drag");
        _isDragging = true;
        _dragStartPoint = e.GetPosition(this);
        CaptureMouse();
        e.Handled = true;
    }

    private void OnWindowMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;
        var pos = e.GetPosition(this);
        var delta = pos - _dragStartPoint;
        Left += delta.X;
        Top += delta.Y;
    }

    private void OnWindowMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        ReleaseMouseCapture();
        e.Handled = true;
    }

    private void OnWindowMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        Log("Right click: showing context menu");
        if (e.ChangedButton != MouseButton.Right) return;

        var menu = new System.Windows.Controls.ContextMenu();

        var expandItem = new System.Windows.Controls.MenuItem { Header = _isExpanded ? "收起" : "展开" };
        expandItem.Click += (s, args) => IsExpanded = !_isExpanded;

        var exitItem = new System.Windows.Controls.MenuItem { Header = "退出" };
        exitItem.Click += (s, args) =>
        {
            Log("Exit from context menu");
            Application.Current.Shutdown();
        };

        menu.Items.Add(expandItem);
        menu.Items.Add(new System.Windows.Controls.Separator());
        menu.Items.Add(exitItem);

        menu.PlacementTarget = this;
        menu.IsOpen = true;
        e.Handled = true;
    }

    private void OnCollapseClick(object sender, RoutedEventArgs e)
    {
        IsExpanded = false;
    }

    private void OnInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            if (_viewModel?.SendMessageCommand?.CanExecute(null) == true)
                _viewModel.SendMessageCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void Expand()
    {
        Log("Expand() called");
        var sb = Resources["ExpandStoryboard"] as Storyboard;
        Log("ExpandStoryboard=" + (sb != null));
        sb?.Begin();
    }

    private void Collapse()
    {
        Log("Collapse() called");
        var sb = Resources["CollapseStoryboard"] as Storyboard;
        sb?.Begin();
    }

    private static void Log(string msg)
    {
        try { File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n"); } catch { }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

#region Converters
public class BoolToColorConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => value is true ? Brushes.LimeGreen : Brushes.Red;
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}

public class InverseBoolToVisibilityConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => value is false ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}

public class RoleToVisibilityConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => value as string == parameter as string ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}
#endregion