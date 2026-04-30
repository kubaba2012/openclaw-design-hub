using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OpenClaw.DesignHub.Skins;

public interface IBubbleSkin
{
    string Name { get; }
    bool IsLoaded { get; }
    Exception? LoadError { get; }

    FrameworkElement CreateVisual();
    void StartAnimation();
    void StopAnimation();
}

public class BubbleSkinManager
{
    private IBubbleSkin _currentSkin;
    private readonly IBubbleSkin _fallbackSkin;
    private readonly List<IBubbleSkin> _availableSkins = new();

    public IBubbleSkin CurrentSkin => _currentSkin;
    public IReadOnlyList<IBubbleSkin> AvailableSkins => _availableSkins;

    public event EventHandler<SkinChangedEventArgs>? SkinChanged;

    public BubbleSkinManager()
    {
        _fallbackSkin = new DefaultBubbleSkin();
        _currentSkin = _fallbackSkin;
        _availableSkins.Add(_fallbackSkin);
    }

    public bool LoadSkin(IBubbleSkin skin)
    {
        try
        {
            skin.CreateVisual();
            _currentSkin = skin;

            if (!_availableSkins.Contains(skin))
            {
                _availableSkins.Add(skin);
            }

            SkinChanged?.Invoke(this, new SkinChangedEventArgs(skin, null));
            return true;
        }
        catch (Exception ex)
        {
            return SwitchToFallback($"Load skin '{skin.Name}' failed: {ex.Message}");
        }
    }

    public bool SwitchToFallback(string? reason = null)
    {
        var previousSkin = _currentSkin;
        _currentSkin = _fallbackSkin;

        if (previousSkin != _fallbackSkin)
        {
            previousSkin?.StopAnimation();
            SkinChanged?.Invoke(this, new SkinChangedEventArgs(_fallbackSkin, reason));
        }

        return true;
    }

    public bool SwitchSkin(string skinName)
    {
        var skin = _availableSkins.FirstOrDefault(s => s.Name == skinName);
        if (skin != null && skin != _currentSkin)
        {
            try
            {
                skin.CreateVisual();
                _currentSkin.StopAnimation();
                _currentSkin = skin;
                skin.StartAnimation();
                SkinChanged?.Invoke(this, new SkinChangedEventArgs(skin, null));
                return true;
            }
            catch (Exception ex)
            {
                return SwitchToFallback($"Switch to '{skinName}' failed: {ex.Message}");
            }
        }
        return false;
    }
}

public class SkinChangedEventArgs : EventArgs
{
    public IBubbleSkin NewSkin { get; }
    public string? Reason { get; }

    public SkinChangedEventArgs(IBubbleSkin newSkin, string? reason)
    {
        NewSkin = newSkin;
        Reason = reason;
    }
}

public class DefaultBubbleSkin : IBubbleSkin
{
    public string Name => "Default";
    public bool IsLoaded { get; private set; }
    public Exception? LoadError { get; private set; }

    public FrameworkElement CreateVisual()
    {
        var grid = new Grid { Width = 100, Height = 100 };

        var ellipse = new System.Windows.Shapes.Ellipse
        {
            Width = 80,
            Height = 80,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var gradient = new RadialGradientBrush
        {
            Center = new Point(0.3, 0.3),
            RadiusX = 0.8,
            RadiusY = 0.8,
            GradientStops = new GradientStopCollection
            {
                new(Color.FromRgb(224, 231, 255), 0),
                new(Color.FromRgb(129, 140, 248), 0.5),
                new(Color.FromRgb(99, 102, 241), 1)
            }
        };

        ellipse.Fill = gradient;
        grid.Children.Add(ellipse);
        IsLoaded = true;
        return grid;
    }

    public void StartAnimation() { }
    public void StopAnimation() { }
}

public class GifBubbleSkin : IBubbleSkin
{
    private FrameworkElement? _visual;

    public string Name { get; }
    public bool IsLoaded { get; private set; }
    public Exception? LoadError { get; private set; }
    public string GifPath { get; }

    private static readonly string DefaultGifUrl = "https://media2.giphy.com/media/v1.Y2lkPTc5MGI3NjExbWVpa2toeXU2cTQweXB0c2h4dGw1OG40bXFxMXY2c240OGhyZGV1NyZlcD12MV9pbnRlcm5hbF9naWZfYnlfaWQmY3Q9Zw/tBRQNyh6fKBpSy2oif/giphy.gif";

    public GifBubbleSkin(string name, string? gifPath = null)
    {
        Name = name;
        GifPath = gifPath ?? DefaultGifUrl;
    }

    public FrameworkElement CreateVisual()
    {
        try
        {
            System.Windows.Media.Imaging.BitmapImage bitmap;

            if (GifPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                GifPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(GifPath, UriKind.Absolute);
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
            }
            else if (File.Exists(GifPath))
            {
                bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(GifPath, UriKind.Absolute);
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
            }
            else
            {
                throw new FileNotFoundException($"GIF file not found: {GifPath}");
            }

            _visual = new System.Windows.Controls.Image
            {
                Width = 100,
                Height = 100,
                Source = bitmap,
                Stretch = Stretch.Uniform
            };

            IsLoaded = true;
            return _visual;
        }
        catch (Exception ex)
        {
            LoadError = ex;
            IsLoaded = false;
            throw;
        }
    }

    public void StartAnimation() { }
    public void StopAnimation() { }
}

public class StoryboardBubbleSkin : IBubbleSkin
{
    private FrameworkElement? _visual;
    private System.Windows.Media.Animation.Storyboard? _storyboard;

    public string Name { get; set; } = "Default";
    public bool IsLoaded { get; private set; }
    public Exception? LoadError { get; private set; }

    public FrameworkElement CreateVisual()
    {
        try
        {
            var ellipse = new System.Windows.Shapes.Ellipse
            {
                Width = 80,
                Height = 80,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var gradient = new RadialGradientBrush
            {
                Center = new Point(0.3, 0.3),
                GradientStops = new GradientStopCollection
                {
                    new(Colors.White, 0),
                    new(Color.FromRgb(199, 210, 254), 0.5),
                    new(Color.FromRgb(129, 140, 248), 1)
                }
            };

            ellipse.Fill = gradient;

            var scaleTransform = new ScaleTransform(1, 1);
            ellipse.RenderTransform = scaleTransform;
            ellipse.RenderTransformOrigin = new Point(0.5, 0.5);

            _visual = ellipse;

            _storyboard = new System.Windows.Media.Animation.Storyboard { RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever };

            var scaleXAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.9,
                To = 1.1,
                Duration = TimeSpan.FromSeconds(2),
                AutoReverse = true,
                EasingFunction = new System.Windows.Media.Animation.SineEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut }
            };

            var scaleYAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.9,
                To = 1.1,
                Duration = TimeSpan.FromSeconds(2),
                AutoReverse = true,
                EasingFunction = new System.Windows.Media.Animation.SineEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut }
            };

            System.Windows.Media.Animation.Storyboard.SetTarget(scaleXAnimation, _visual);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));

            System.Windows.Media.Animation.Storyboard.SetTarget(scaleYAnimation, _visual);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));

            _storyboard.Children.Add(scaleXAnimation);
            _storyboard.Children.Add(scaleYAnimation);

            IsLoaded = true;
            return _visual;
        }
        catch (Exception ex)
        {
            LoadError = ex;
            IsLoaded = false;
            throw;
        }
    }

    public void StartAnimation()
    {
        _storyboard?.Begin();
    }

    public void StopAnimation()
    {
        _storyboard?.Stop();
    }
}