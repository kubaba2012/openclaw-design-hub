namespace OpenClaw.DesignHub.Modules.Channel;

/// <summary>
/// Channel 客户端接口
/// </summary>
public interface IChannelClient : IDisposable
{
    /// <summary>
    /// 是否已连接
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Channel ID（注册成功后分配）
    /// </summary>
    string ChannelId { get; }

    /// <summary>
    /// 收到消息事件
    /// </summary>
    event EventHandler<Protocol.ChatMessage>? MessageReceived;

    /// <summary>
    /// 连接状态变化事件
    /// </summary>
    event EventHandler<bool>? ConnectionStateChanged;

    /// <summary>
    /// 连接到 Gateway
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 断开连接
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// 发送聊天消息
    /// </summary>
    Task SendMessageAsync(Protocol.ChatMessage message);
}