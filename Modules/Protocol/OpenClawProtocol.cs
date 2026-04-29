using System.Text.Json.Serialization;

namespace OpenClaw.DesignHub.Modules.Protocol;

/// <summary>
/// OpenClaw Gateway 消息协议
/// </summary>
public static class OpenClawProtocol
{
    /// <summary>Gateway 默认端口</summary>
    public const int GatewayPort = 9988;

    /// <summary>本地 CAD 服务端口</summary>
    public const int CadServicePort = 8765;

    /// <summary>Channel 类型标识</summary>
    public const string ChannelType = "designhub";
}

/// <summary>
/// 基础消息结构
/// </summary>
public class BaseMessage
{
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("O");

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public object? Payload { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// 连接挑战消息
/// </summary>
public class ConnectChallenge : BaseMessage
{
    public ConnectChallenge()
    {
        Type = "connect.challenge";
    }

    public string? Challenge { get; set; }
    public string? SessionId { get; set; }
}

/// <summary>
/// Channel 注册消息
/// </summary>
public class ChannelRegister : BaseMessage
{
    public ChannelRegister()
    {
        Type = "channel.register";
    }

    public string? Channel { get; set; }
    public string? CallbackUrl { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Channel 注册响应
/// </summary>
public class ChannelRegisterResponse : BaseMessage
{
    public bool Success { get; set; }
    public string? ChannelId { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// 聊天消息
/// </summary>
public class ChatMessage : BaseMessage
{
    public ChatMessage()
    {
        Type = "chat.message";
    }

    public string? Role { get; set; }  // user / assistant
    public string? Content { get; set; }
    public string? SessionKey { get; set; }
}

/// <summary>
/// 心跳消息
/// </summary>
public class HeartbeatMessage : BaseMessage
{
    public HeartbeatMessage()
    {
        Type = "heartbeat";
    }
}

/// <summary>
/// 心跳响应
/// </summary>
public class HeartbeatAck : BaseMessage
{
    public HeartbeatAck()
    {
        Type = "heartbeat.ack";
    }
}