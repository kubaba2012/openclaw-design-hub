using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using OpenClaw.DesignHub.Modules.Protocol;
using Serilog;

namespace OpenClaw.DesignHub.Modules.Channel;

/// <summary>
/// OpenClaw Gateway Channel 客户端
/// </summary>
public class ChannelClient : IChannelClient
{
    private readonly ILogger _logger = App.Logger.ForContext<ChannelClient>();
    private readonly ClientWebSocket _ws = new();
    private readonly CancellationTokenSource _cts = new();

    private string _channelId = string.Empty;
    private string _sessionId = string.Empty;
    private string _gatewayToken = string.Empty;
    private string _identityPath = string.Empty;

    private bool _isConnected;
    private bool _isRegistered;

    public bool IsConnected => _isConnected && _ws.State == WebSocketState.Open;
    public string ChannelId => _channelId;

    public event EventHandler<ChatMessage>? MessageReceived;
    public event EventHandler<bool>? ConnectionStateChanged;

    /// <summary>
    /// Gateway 地址
    /// </summary>
    public string GatewayUrl { get; set; } = $"ws://127.0.0.1:{OpenClawProtocol.GatewayPort}";

    /// <summary>
    /// 回调地址（用于 channel.register）
    /// </summary>
    public string CallbackUrl { get; set; } = $"ws://localhost:{OpenClawProtocol.CadServicePort}";

    /// <summary>
    /// Channel 类型
    /// </summary>
    public string ChannelType { get; set; } = OpenClawProtocol.ChannelType;

    public ChannelClient()
    {
        LoadIdentity();
    }

    /// <summary>
    /// 加载设备身份（用于签名认证）
    /// </summary>
    private void LoadIdentity()
    {
        var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _identityPath = Path.Combine(homePath, ".openclaw", "identity");

        // 读取 gateway token
        var tokenPath = Path.Combine(_identityPath, "gateway-token");
        if (File.Exists(tokenPath))
        {
            _gatewayToken = File.ReadAllText(tokenPath).Trim();
            _logger.Debug("已加载 Gateway Token");
        }
        else
        {
            _logger.Warning("未找到 Gateway Token 文件: {Path}", tokenPath);
        }
    }

    /// <summary>
    /// 连接到 Gateway
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            _logger.Warning("已连接，跳过");
            return;
        }

        try
        {
            _logger.Information("正在连接 Gateway: {Url}", GatewayUrl);

            // 设置子协议
            _ws.Options.AddSubProtocol("json");

            // 连接
            await _ws.ConnectAsync(new Uri(GatewayUrl), cancellationToken);

            _isConnected = true;
            ConnectionStateChanged?.Invoke(this, true);

            _logger.Information("已连接到 Gateway");

            // 启动接收循环
            _ = ReceiveLoopAsync();

            // 等待 challenge 并完成注册
            await WaitForChallengeAndRegisterAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "连接 Gateway 失败");
            _isConnected = false;
            ConnectionStateChanged?.Invoke(this, false);
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
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "断开连接", CancellationToken.None);
            }

            _isConnected = false;
            _isRegistered = false;
            ConnectionStateChanged?.Invoke(this, false);

            _logger.Information("已断开 Gateway 连接");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "断开连接时出错");
        }
    }

    /// <summary>
    /// 接收消息循环
    /// </summary>
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
                    _logger.Information("Gateway 关闭连接");
                    await DisconnectAsync();
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleMessageAsync(json);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Debug("接收循环已取消");
        }
        catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
        {
            _logger.Warning("连接意外关闭");
            _isConnected = false;
            ConnectionStateChanged?.Invoke(this, false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "接收循环错误");
            _isConnected = false;
            ConnectionStateChanged?.Invoke(this, false);
        }
    }

    /// <summary>
    /// 处理收到的消息
    /// </summary>
    private async Task HandleMessageAsync(string json)
    {
        try
        {
            var baseMsg = JsonSerializer.Deserialize<BaseMessage>(json);
            if (baseMsg == null) return;

            _logger.Debug("收到消息: {Type}", baseMsg.Type);

            switch (baseMsg.Type)
            {
                case "connect.challenge":
                    await HandleChallengeAsync(json);
                    break;

                case "channel.register.response":
                    HandleRegisterResponse(json);
                    break;

                case "chat.message":
                    var chatMsg = JsonSerializer.Deserialize<ChatMessage>(json);
                    if (chatMsg != null)
                    {
                        MessageReceived?.Invoke(this, chatMsg);
                    }
                    break;

                case "heartbeat":
                    await SendHeartbeatAckAsync();
                    break;

                default:
                    _logger.Debug("未处理的消息类型: {Type}", baseMsg.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "处理消息失败: {Json}", json);
        }
    }

    /// <summary>
    /// 处理连接挑战
    /// </summary>
    private async Task HandleChallengeAsync(string json)
    {
        var challenge = JsonSerializer.Deserialize<ConnectChallenge>(json);
        if (challenge == null) return;

        _sessionId = challenge.SessionId ?? string.Empty;
        _logger.Information("收到挑战, SessionId: {SessionId}", _sessionId);

        // 生成签名（简化版，实际应使用 Ed25519）
        var signature = GenerateSignature(challenge.Challenge ?? "");

        // 发送认证响应
        var authResponse = new BaseMessage
        {
            Type = "connect.auth",
            Payload = new
            {
                sessionId = _sessionId,
                token = _gatewayToken,
                signature = signature
            }
        };

        await SendRawAsync(authResponse);
        _logger.Information("已发送认证响应");

        // 注册 channel
        await RegisterChannelAsync();
    }

    /// <summary>
    /// 注册 Channel
    /// </summary>
    private async Task RegisterChannelAsync()
    {
        var register = new ChannelRegister
        {
            Channel = ChannelType,
            CallbackUrl = CallbackUrl,
            Description = "OpenClaw 设计中台",
            Payload = new
            {
                version = "1.0.0",
                capabilities = new[] { "chat", "cad.query" }
            }
        };

        await SendRawAsync(register);
        _logger.Information("已发送 Channel 注册请求: {Channel}", ChannelType);
    }

    /// <summary>
    /// 处理注册响应
    /// </summary>
    private void HandleRegisterResponse(string json)
    {
        var response = JsonSerializer.Deserialize<ChannelRegisterResponse>(json);
        if (response == null) return;

        if (response.Success)
        {
            _channelId = response.ChannelId ?? string.Empty;
            _isRegistered = true;
            _logger.Information("Channel 注册成功: {ChannelId}", _channelId);
        }
        else
        {
            _logger.Error("Channel 注册失败: {Error}", response.Error);
        }
    }

    /// <summary>
    /// 发送聊天消息
    /// </summary>
    public async Task SendMessageAsync(ChatMessage message)
    {
        if (!IsConnected)
        {
            _logger.Warning("未连接，无法发送消息");
            return;
        }

        await SendRawAsync(message);
        _logger.Debug("已发送消息: {Type}", message.Type);
    }

    /// <summary>
    /// 发送原始消息
    /// </summary>
    private async Task SendRawAsync(BaseMessage message)
    {
        if (_ws.State != WebSocketState.Open) return;

        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var bytes = Encoding.UTF8.GetBytes(json);
        await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
    }

    /// <summary>
    /// 发送心跳响应
    /// </summary>
    private async Task SendHeartbeatAckAsync()
    {
        var ack = new HeartbeatAck();
        await SendRawAsync(ack);
    }

    /// <summary>
    /// 生成签名（简化版）
    /// </summary>
    private string GenerateSignature(string challenge)
    {
        // TODO: 实现真正的 Ed25519 签名
        // 目前使用简单哈希作为占位
        var data = $"{challenge}:{_gatewayToken}";
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// 等待挑战并完成注册
    /// </summary>
    private async Task WaitForChallengeAndRegisterAsync()
    {
        // 给一点时间让 challenge 消息到达
        await Task.Delay(1000);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _ws.Dispose();
        GC.SuppressFinalize(this);
    }
}