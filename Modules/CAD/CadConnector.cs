using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Serilog;

namespace OpenClaw.DesignHub.Modules.CAD;

/// <summary>
/// CAD 数据连接器（预留，用于与专业软件数据交换）
/// </summary>
public class CadConnector : IDisposable
{
    private readonly ILogger _logger = App.Logger.ForContext<CadConnector>();
    private readonly ClientWebSocket _ws = new();
    private readonly CancellationTokenSource _cts = new();

    private bool _isConnected;

    public bool IsConnected => _isConnected && _ws.State == WebSocketState.Open;

    /// <summary>
    /// CAD 服务地址
    /// </summary>
    public string ServiceUrl { get; set; } = "ws://localhost:8765";

    /// <summary>
    /// 收到 CAD 数据事件
    /// </summary>
    public event EventHandler<string>? DataReceived;

    /// <summary>
    /// 连接到 CAD 服务
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected) return;

        try
        {
            _logger.Information("正在连接 CAD 服务: {Url}", ServiceUrl);
            await _ws.ConnectAsync(new Uri(ServiceUrl), cancellationToken);

            _isConnected = true;
            _logger.Information("已连接到 CAD 服务");

            _ = ReceiveLoopAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "连接 CAD 服务失败");
            throw;
        }
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (!_isConnected) return;

        try
        {
            _cts.Cancel();
            if (_ws.State == WebSocketState.Open)
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "断开", CancellationToken.None);
            }
            _isConnected = false;
            _logger.Information("已断开 CAD 服务连接");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "断开 CAD 连接时出错");
        }
    }

    /// <summary>
    /// 请求完整数据
    /// </summary>
    public async Task RequestFullDataAsync()
    {
        if (!IsConnected) return;

        var request = new
        {
            messageId = Guid.NewGuid().ToString("N"),
            timestamp = DateTime.UtcNow.ToString("O"),
            type = "full_data.request",
            payload = new { }
        };

        var json = JsonSerializer.Serialize(request);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
    }

    /// <summary>
    /// 执行 CAD 命令
    /// </summary>
    public async Task ExecuteCommandAsync(string command, object? parameters = null)
    {
        if (!IsConnected) return;

        var request = new
        {
            messageId = Guid.NewGuid().ToString("N"),
            timestamp = DateTime.UtcNow.ToString("O"),
            type = "command.request",
            payload = new
            {
                command = command,
                parameters = parameters
            }
        };

        var json = JsonSerializer.Serialize(request);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
    }

    private async Task ReceiveLoopAsync()
    {
        var buffer = new byte[8192];

        try
        {
            while (!_cts.Token.IsCancellationRequested && _ws.State == WebSocketState.Open)
            {
                var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await DisconnectAsync();
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    DataReceived?.Invoke(this, json);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常退出
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "CAD 数据接收错误");
            _isConnected = false;
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _ws.Dispose();
        GC.SuppressFinalize(this);
    }
}