# OpenClaw DesignHub

OpenClaw 设计中台 - 灵动岛悬浮对话窗口

## 功能特性

- **灵动岛 UI**：类似 Apple Dynamic Island 的悬浮球设计
- **展开对话**：点击悬浮球展开为完整对话面板
- **Channel 连接**：连接到 OpenClaw Gateway 进行消息收发
- **CAD 数据交换**：预留端口，后期对接专业软件

## 技术栈

- .NET 8 WPF
- MVVM 架构 (CommunityToolkit.Mvvm)
- Serilog 日志

## 项目结构

```
OpenClaw.DesignHub/
├── App.xaml(.cs)              # 应用入口
├── MainWindow.xaml(.cs)       # 主窗口（灵动岛）
├── ViewModels/
│   └── MainViewModel.cs       # 主视图模型
├── Modules/
│   ├── Channel/               # OpenClaw Channel 连接模块
│   │   ├── IChannelClient.cs
│   │   └── ChannelClient.cs
│   ├── Protocol/              # 消息协议
│   │   └── OpenClawProtocol.cs
│   └── CAD/                   # CAD 数据交换（预留）
│       └── CadConnector.cs
├── Services/                  # 服务层
└── Resources/
    └── Styles.xaml            # 统一样式
```

## 使用说明

### 前置条件

1. 安装 .NET 8 SDK
2. OpenClaw Gateway 运行在 `ws://127.0.0.1:9988`
3. Gateway Token 文件位于 `~/.openclaw/identity/gateway-token`

### 运行

```bash
cd OpenClaw.DesignHub
dotnet run
```

### 操作

- **展开**：点击悬浮球
- **收起**：点击右上角 ✕ 按钮
- **拖动**：展开状态下拖动标题栏
- **发送消息**：输入文字后按 Enter 或点击发送按钮

## 配置

### Gateway 连接

默认连接 `ws://127.0.0.1:9988`，可在 `ChannelClient.cs` 中修改：

```csharp
public string GatewayUrl { get; set; } = "ws://127.0.0.1:9988";
```

### CAD 数据服务

默认连接 `ws://localhost:8765`，可在 `CadConnector.cs` 中修改：

```csharp
public string ServiceUrl { get; set; } = "ws://localhost:8765";
```

## 架构说明

### Channel 模块

负责与 OpenClaw Gateway 的 WebSocket 连接：
- 连接认证（挑战-响应签名）
- Channel 注册
- 消息收发
- 心跳保活

### CAD 模块

预留的 CAD 数据交换接口：
- 连接到 AutoCAD 插件的 WebSocket 服务
- 请求图纸数据
- 执行 CAD 命令

### UI 模块

灵动岛 + 对话面板：
- 悬浮球状态（收起）
- 对话面板状态（展开）
- 平滑动画过渡
- 消息气泡展示

## 开发计划

- [ ] Ed25519 真实签名
- [ ] 消息持久化
- [ ] 多会话支持
- [ ] CAD 数据展示
- [ ] 主题切换
- [ ] 系统托盘

## License

MIT