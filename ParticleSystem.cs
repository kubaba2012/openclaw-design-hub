using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OpenClaw.DesignHub;

/// <summary>
/// 粒子效果管理器
/// </summary>
public class ParticleSystem
{
    private readonly Canvas _canvas;
    private readonly DispatcherTimer _timer;
    private readonly Random _random = new();
    private readonly List<Ellipse> _particles = new();

    public ParticleSystem(Canvas canvas)
    {
        _canvas = canvas;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        // 生成新粒子
        if (_particles.Count < 20 && _random.NextDouble() < 0.3)
        {
            CreateParticle();
        }

        // 更新现有粒子
        UpdateParticles();
    }

    private void CreateParticle()
    {
        var particle = new Ellipse
        {
            Width = 3 + _random.NextDouble() * 5,
            Height = 3 + _random.NextDouble() * 5,
            Fill = new SolidColorBrush(Color.FromArgb(
                (byte)_random.Next(50, 150),
                (byte)_random.Next(99, 111),
                (byte)_random.Next(111, 241),
                (byte)_random.Next(150, 255)
            )),
            Opacity = 0.3 + _random.NextDouble() * 0.5
        };

        var x = _random.NextDouble() * _canvas.ActualWidth;
        var y = _canvas.ActualHeight - particle.Height;

        Canvas.SetLeft(particle, x);
        Canvas.SetTop(particle, y);

        _canvas.Children.Add(particle);
        _particles.Add(particle);

        // 动画
        var duration = TimeSpan.FromSeconds(2 + _random.NextDouble() * 3);
        var endY = -particle.Height - _random.NextDouble() * 50;

        var moveAnimation = new DoubleAnimation
        {
            From = y,
            To = endY,
            Duration = duration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        var opacityAnimation = new DoubleAnimation
        {
            From = particle.Opacity,
            To = 0,
            Duration = duration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        Storyboard.SetTarget(moveAnimation, particle);
        Storyboard.SetTargetProperty(moveAnimation, new PropertyPath("(Canvas.Top)"));

        Storyboard.SetTarget(opacityAnimation, particle);
        Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(UIElement.OpacityProperty));

        var storyboard = new Storyboard();
        storyboard.Children.Add(moveAnimation);
        storyboard.Children.Add(opacityAnimation);
        storyboard.Completed += (s, args) => RemoveParticle(particle);
        storyboard.Begin();
    }

    private void UpdateParticles()
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var particle = _particles[i];
            var currentY = Canvas.GetTop(particle);

            if (currentY < -50)
            {
                RemoveParticle(particle);
            }
        }
    }

    private void RemoveParticle(Ellipse particle)
    {
        if (_canvas.Children.Contains(particle))
        {
            _canvas.Children.Remove(particle);
        }
        _particles.Remove(particle);
    }

    public void Stop()
    {
        _timer.Stop();
        foreach (var particle in _particles.ToList())
        {
            RemoveParticle(particle);
        }
    }
}
