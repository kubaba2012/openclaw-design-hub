using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenClaw.DesignHub.Modules.Channel;
using OpenClaw.DesignHub.Modules.Protocol;
using Serilog;

namespace OpenClaw.DesignHub.ViewModels;

/// <summary>
/// 聊天消息项
/// </summary>
public partial class ChatItem : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString("N");

    [ObservableProperty]
    private string _role = string.Empty; // user / assistant / system

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private DateTime _timestamp = DateTime.Now;

    [ObservableProperty]
    private bool _isPending;
}

/// <summary>
/// 主视图模型
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ILogger _logger = App.Logger.ForContext<MainViewModel>();
    private readonly IChannelClient _channel;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _connectionStatus = "未连接";

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private bool _isSending;

    public ObservableCollection<ChatItem> Messages { get; } = new();

    public ICommand ToggleExpandCommand { get; }
    public ICommand ExpandCommand { get; }
    public ICommand SendMessageCommand { get; }
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }

    public MainViewModel(IChannelClient channel)
    {
        _channel = channel;

        ToggleExpandCommand = new RelayCommand(() => IsExpanded = !IsExpanded);
        ExpandCommand = new RelayCommand(() => IsExpanded = true);
        SendMessageCommand = new AsyncRelayCommand(SendMessageAsync, () => !string.IsNullOrWhiteSpace(InputText) && !IsSending && IsConnected);
        ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => !IsConnected);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync, () => IsConnected);

        // 订阅 Channel 事件
        _channel.MessageReceived += OnMessageReceived;
        _channel.ConnectionStateChanged += OnConnectionStateChanged;

        // 添加欢迎消息
        Messages.Add(new ChatItem
        {
            Role = "system",
            Content = "欢迎使用 OpenClaw 设计中台",
            Timestamp = DateTime.Now
        });
    }

    /// <summary>
    /// 连接到 Gateway
    /// </summary>
    private async Task ConnectAsync()
    {
        try
        {
            ConnectionStatus = "正在连接...";
            await _channel.ConnectAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "连接失败");
            ConnectionStatus = "连接失败";
            MessageBox.Show($"连接失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    private async Task DisconnectAsync()
    {
        await _channel.DisconnectAsync();
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText)) return;

        var content = InputText;
        InputText = string.Empty;
        IsSending = true;

        try
        {
            // 添加用户消息
            var userMsg = new ChatItem
            {
                Role = "user",
                Content = content,
                Timestamp = DateTime.Now
            };
            Messages.Add(userMsg);

            // 发送到 Gateway
            var message = new ChatMessage
            {
                Role = "user",
                Content = content
            };

            await _channel.SendMessageAsync(message);

            // 添加待定回复（等待实际响应）
            var pendingMsg = new ChatItem
            {
                Role = "assistant",
                Content = "正在思考...",
                Timestamp = DateTime.Now,
                IsPending = true
            };
            Messages.Add(pendingMsg);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "发送消息失败");
            Messages.Add(new ChatItem
            {
                Role = "system",
                Content = $"发送失败: {ex.Message}",
                Timestamp = DateTime.Now
            });
        }
        finally
        {
            IsSending = false;
        }
    }

    /// <summary>
    /// 处理收到的消息
    /// </summary>
    private void OnMessageReceived(object? sender, ChatMessage message)
    {
        // 在 UI 线程执行
        Application.Current.Dispatcher.Invoke(() =>
        {
            // 移除待定消息
            var pending = Messages.FirstOrDefault(m => m.IsPending && m.Role == "assistant");
            if (pending != null)
            {
                pending.Content = message.Content ?? string.Empty;
                pending.IsPending = false;
                pending.Timestamp = DateTime.Now;
            }
            else
            {
                // 添加新消息
                Messages.Add(new ChatItem
                {
                    Role = message.Role ?? "assistant",
                    Content = message.Content ?? string.Empty,
                    Timestamp = DateTime.Now
                });
            }
        });
    }

    /// <summary>
    /// 处理连接状态变化
    /// </summary>
    private void OnConnectionStateChanged(object? sender, bool isConnected)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsConnected = isConnected;
            ConnectionStatus = isConnected ? $"已连接 ({_channel.ChannelId})" : "已断开";

            if (isConnected)
            {
                Messages.Add(new ChatItem
                {
                    Role = "system",
                    Content = "已连接到 OpenClaw Gateway",
                    Timestamp = DateTime.Now
                });
            }
            else
            {
                Messages.Add(new ChatItem
                {
                    Role = "system",
                    Content = "已断开连接",
                    Timestamp = DateTime.Now
                });
            }
        });
    }
}