# TCP连接流程分析与断线处理

## 一、TCP连接过程分析

### 1. 连接流程概览

```
用户点击连接按钮
    ↓
MainV2.doConnect()
    ↓
创建TcpSerial对象
    ↓
comPort.BaseStream = tcpSerial
    ↓
comPort.Open()
    ↓
tcpSerial.Open()
    ↓
InitTCPClient()
    ↓
new TcpClient(host, port) → TCP三次握手
    ↓
连接成功
```

### 2. 详细函数调用链

#### **第一层：用户界面触发**
```
MainV2.cs
├── MenuConnect_Click() 或相关连接按钮事件
└── doConnect(MAVLinkInterface comPort, string portname, string baud, ...)
```

#### **第二层：连接处理**
```
MainV2.cs::doConnect()
├── 判断连接类型（case "TCP"）
├── 创建TcpSerial对象: new TcpSerial()
├── 设置BaseStream: comPort.BaseStream = tcpSerial
├── 设置连接参数（Host, Port等）
└── 调用: comPort.Open(false, skipconnectcheck, showui)
```

#### **第三层：MAVLink接口打开**
```
MAVLinkInterface.cs::Open(bool getparams, bool skipconnectedcheck, bool showui)
├── 检查BaseStream是否已打开
├── 创建进度对话框
├── 启动后台任务:
│   ├── FrmProgressReporterDoWorkAndParams() 或
│   └── FrmProgressReporterDoWorkNOParams()
└── 在后台任务中调用: BaseStream.Open()
```

#### **第四层：TCP连接核心实现**
```
CommsTCPSerial.cs::Open()
├── 检查连接状态（防止重复打开）
├── 获取用户输入（Host和Port）
│   ├── OnSettings() 获取配置
│   └── OnInputBoxShow() 用户输入对话框
├── 保存配置
└── InitTCPClient(host, port)
```

#### **第五层：TCP客户端初始化**
```
CommsTCPSerial.cs::InitTCPClient(string host, string port)
├── new TcpClient(host, int.Parse(port))
│   └── 【底层TCP三次握手发生】
│       ├── SYN 包发送
│       ├── SYN-ACK 包接收
│       └── ACK 包发送
├── 设置TCP选项:
│   ├── client.NoDelay = true (禁用Nagle算法)
│   └── client.Client.NoDelay = true
├── VerifyConnected() 验证连接
└── reconnectnoprompt = true (标记已连接)
```

## 二、核心代码片段

### 2.1 MainV2.cs - doConnect函数（TCP部分）

```csharp
// MainV2.cs:1923-1941
case "TCP":
    var tcpSerial = new TcpSerial();
    comPort.BaseStream = tcpSerial;
    _connectionControl.CMB_serialport.Text = "TCP";
    
    // 设置默认端口，避免弹出端口输入框
    tcpSerial.Port = AutoConnectManager.GetPortForHost(AutoConnectManager.PrimaryTcpHost);
    
    // 根据连接类型设置标志
    if (AutoConnectManager.IsAutoConnecting)
    {
        AutoConnectManager.MarkAutoConnect();
    }
    else
    {
        AutoConnectManager.MarkManualConnect();
    }
    break;
```

### 2.2 CommsTCPSerial.cs - Open函数

```csharp
// CommsTCPSerial.cs:101-157
public void Open()
{
    try
    {
        inOpen = true;
        closed = false;

        if (client.Client.Connected)
        {
            log.Warn("tcpserial socket already open");
            return;
        }

        var dest = Port;
        var host = "127.0.0.1";

        if (Host == "")
        {
            dest = OnSettings("TCP_port" + ConfigRef, dest);
            host = OnSettings("TCP_host" + ConfigRef, host);

            if (!reconnectnoprompt)
            {
                if (inputboxreturn.Cancel == OnInputBoxShow("remote host",
                    "Enter host name/ip (ensure remote end is already started)", ref host))
                    throw new Exception("Canceled by request");
                if (inputboxreturn.Cancel == OnInputBoxShow("remote Port", "Enter remote port", ref dest))
                    throw new Exception("Canceled by request");
            }
            Host = host;
        }
        else
        {
            host = Host;
        }

        Port = dest;

        log.InfoFormat("TCP Open {0} {1}", Host, Port);

        OnSettings("TCP_port" + ConfigRef, Port, true);
        OnSettings("TCP_host" + ConfigRef, Host, true);

        InitTCPClient(Host, Port);
    }
    catch
    {
        // disable if the first connect fails
        autoReconnect = false;
        throw;
    }
    finally
    {
        inOpen = false;
    }
}
```

### 2.3 CommsTCPSerial.cs - InitTCPClient函数

```csharp
// CommsTCPSerial.cs:159-169
private void InitTCPClient(string host, string port)
{
    client = new TcpClient(host, int.Parse(port));  // ← TCP三次握手在这里发生

    client.NoDelay = true;
    client.Client.NoDelay = true;

    VerifyConnected();

    reconnectnoprompt = true;
}
```

### 2.4 连接状态检查

```csharp
// CommsTCPSerial.cs:77-96
public bool IsOpen
{
    get
    {
        try
        {
            if (client == null) return false;
            if (client.Client == null) return false;

            if (autoReconnect && client.Client.Connected == false && !inOpen && !closed)
                doAutoReconnect();  // ← 自动重连逻辑

            return client.Client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
```

## 三、TCP因外界因素断开时的处理机制

### 3.1 断线检测机制

#### **方式1：读取时检测**
```csharp
// CommsTCPSerial.cs:171-191
public int Read(byte[] readto, int offset, int length)
{
    VerifyConnected();  // ← 每次读取前验证连接
    try
    {
        if (length < 1) return 0;
        
        return client.Client.Receive(readto, offset, length, SocketFlags.None);
        // ← 如果连接已断开，Receive会抛出异常
    }
    catch
    {
        throw new Exception("Socket Closed");
    }
}
```

#### **方式2：写入时检测**
```csharp
// CommsTCPSerial.cs:241-251
public void Write(byte[] write, int offset, int length)
{
    VerifyConnected();
    try
    {
        client.Client.Send(write, length, SocketFlags.None);
        // ← 如果连接已断开，Send会抛出异常
    }
    catch
    {
        // 静默失败，不抛出异常
    }
}
```

#### **方式3：连接状态属性检查**
```csharp
// CommsTCPSerial.cs:77-96
public bool IsOpen
{
    get
    {
        // 每次访问IsOpen时都会检查 client.Client.Connected
        if (autoReconnect && client.Client.Connected == false && !inOpen && !closed)
            doAutoReconnect();
        
        return client.Client.Connected;
    }
}
```

### 3.2 断线后的自动重连机制

#### **自动重连函数**
```csharp
// CommsTCPSerial.cs:330-361
private void doAutoReconnect()
{
    if (!autoReconnect)
        return;
    try
    {
        if (DateTime.Now > lastReconnectTime)  // 限制重连频率（5秒）
        {
            try
            {
                client.Dispose();
            }
            catch { }

            client = new TcpClient();

            var host = OnSettings("TCP_host" + ConfigRef, "");
            var port = int.Parse(OnSettings("TCP_port" + ConfigRef, ""));

            log.InfoFormat("doAutoReconnect {0} {1}", host, port);

            var task = client.ConnectAsync(host, port);  // 异步重连

            lastReconnectTime = DateTime.Now.AddSeconds(5);  // 5秒内不再重连
        }
    }
    catch { }
}
```

#### **手动重连机制**
```csharp
// CommsTCPSerial.cs:363-386
private void VerifyConnected()
{
    if (!IsOpen)
    {
        try
        {
            client.Dispose();
        }
        catch { }

        // 如果之前已建立过连接，且有重试次数，则尝试重连
        if (client != null && retrys > 0)
        {
            log.Info("tcp reconnect");
            client = new TcpClient();
            client.Connect(OnSettings("TCP_host" + ConfigRef, ""), 
                          int.Parse(OnSettings("TCP_port" + ConfigRef, "")));
            retrys--;  // 减少重试次数
        }

        throw new Exception("The socket/serialproxy is closed");
    }
}
```

### 3.3 断线后的影响

当TCP连接因外界因素断开时（网络故障、服务器关闭、超时等），会发生以下情况：

#### **1. 立即影响**
- ✅ **读取操作失败**: `Read()` 方法会抛出 `Exception("Socket Closed")`
- ✅ **写入操作失败**: `Write()` 方法静默失败（捕获异常但不抛出）
- ✅ **连接状态变为false**: `IsOpen` 属性返回 `false`
- ✅ **MAVLink通信中断**: 无法接收或发送MAVLink数据包

#### **2. 自动处理机制**
- ✅ **自动重连尝试**: 如果 `autoReconnect = true`，系统会在访问 `IsOpen` 时触发自动重连
- ✅ **重连频率限制**: 自动重连有5秒的冷却时间，避免频繁重连
- ✅ **手动重连**: 在 `VerifyConnected()` 中，如果有剩余重试次数（`retrys > 0`），会尝试重新连接

#### **3. 上层应用处理**
```csharp
// MAVLinkInterface.cs 中的读取循环可能会捕获异常
try {
    // 读取MAVLink数据包
    packet = ReadPacket();
} catch (Exception ex) {
    // 连接断开，上层应用需要处理
    log.Error("Connection lost", ex);
    // 可能触发断线事件或重连逻辑
}
```

### 3.4 断线场景示例

#### **场景1：网络突然断开**
```
1. TCP连接正常
2. 网络突然断开（拔网线、WiFi断开等）
3. 下次Read()/Write()操作时，Socket.Send/Receive抛出异常
4. VerifyConnected()检测到连接断开
5. 如果autoReconnect=true，触发doAutoReconnect()
6. 尝试重新连接（异步）
```

#### **场景2：服务器主动关闭连接**
```
1. 服务器发送FIN包，主动关闭连接
2. 客户端收到FIN包，连接状态变为半关闭
3. 客户端下次操作时检测到连接断开
4. 触发重连机制
```

#### **场景3：连接超时**
```
1. 长时间无数据交互
2. TCP Keep-Alive可能超时（取决于系统配置）
3. 连接被操作系统关闭
4. 下次操作时检测到断开
5. 触发重连
```

## 四、最佳实践建议

### 4.1 错误处理
- ✅ 在使用 `Read()`/`Write()` 时，应该捕获异常
- ✅ 定期检查 `IsOpen` 状态
- ✅ 监听连接状态变化事件

### 4.2 重连策略
- ✅ 启用 `autoReconnect` 实现自动重连
- ✅ 设置合理的 `retrys` 重试次数
- ✅ 实现指数退避策略（当前实现是固定5秒间隔）

### 4.3 状态监控
- ✅ 在UI层显示连接状态
- ✅ 记录断线日志
- ✅ 向用户提示连接状态变化

## 五、相关代码文件

1. **ExtLibs/Comms/CommsTCPSerial.cs** - TCP连接核心实现
2. **MainV2.cs** - 主程序连接逻辑
3. **ExtLibs/ArduPilot/Mavlink/MAVLinkInterface.cs** - MAVLink接口层
4. **Controls/ConnectionControl.cs** - 连接控制UI组件

## 六、连接图标变化机制

### 6.1 图标状态说明

连接图标有两种状态：

#### **1. 断开连接状态（Connect Icon）**
- **图标样式**: 红黑分离的插头图标（两个插头部分未连接）
- **显示时机**: 当 `comPort.BaseStream.IsOpen == false` 时
- **图标资源**: 
  - 浅色主题: `light_connect_icon.png` 或 `Properties.Resources.light_connect_icon`
  - 深色主题: `dark_connect_icon.png` 或 `Properties.Resources.dark_connect_icon`
- **文字标签**: "连接" 或 `Strings.CONNECTc`
- **Image.Tag**: `"Connect"`

#### **2. 已连接状态（Disconnect Icon）**
- **图标样式**: 已连接的插头图标（两个插头部分合拢，通常为绿色或蓝色）
- **显示时机**: 当 `comPort.BaseStream.IsOpen == true` 时
- **图标资源**:
  - 浅色主题: `light_disconnect_icon.png` 或 `Properties.Resources.light_disconnect_icon`
  - 深色主题: `dark_disconnect_icon.png` 或 `Properties.Resources.dark_disconnect_icon`
- **文字标签**: "断开" 或 `Strings.DISCONNECTc`
- **Image.Tag**: `"Disconnect"`

### 6.2 图标更新机制

#### **核心更新函数**
```csharp
// MainV2.cs:3013-3056
private void UpdateConnectIcon()
{
    // 限制更新频率：500毫秒内只更新一次，避免频繁刷新
    if ((DateTime.UtcNow - connectButtonUpdate).Milliseconds > 500)
    {
        if (comPort.BaseStream.IsOpen)  // 检查连接状态
        {
            // 如果图标不是"已连接"状态，则更新为"已连接"图标
            if (this.MenuConnect.Image == null || 
                (string)this.MenuConnect.Image.Tag != "Disconnect")
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    this.MenuConnect.Image = displayicons.disconnect;  // 设置为已连接图标
                    this.MenuConnect.Image.Tag = "Disconnect";
                    this.MenuConnect.Text = Strings.DISCONNECTc;
                    _connectionControl.IsConnected(true);  // 更新连接控件状态
                });
            }
        }
        else  // 连接已断开
        {
            // 如果图标不是"断开连接"状态，则更新为"断开连接"图标
            if (this.MenuConnect.Image != null && 
                (string)this.MenuConnect.Image.Tag != "Connect")
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    this.MenuConnect.Image = displayicons.connect;  // 设置为断开连接图标
                    this.MenuConnect.Image.Tag = "Connect";
                    this.MenuConnect.Text = Strings.CONNECTc;
                    _connectionControl.IsConnected(false);  // 更新连接控件状态
                    if (_connectionStats != null)
                    {
                        _connectionStats.StopUpdates();  // 停止连接统计更新
                    }
                });
            }
        }
        
        connectButtonUpdate = DateTime.UtcNow;  // 记录更新时间
    }
}
```

### 6.3 图标更新触发时机

#### **1. 连接成功时**
```csharp
// MainV2.cs:2309-2310
// 在MAVLink连接成功后，立即更新图标
this.MenuConnect.Image = displayicons.disconnect;
```

#### **2. 连接失败时**
```csharp
// MainV2.cs:2126-2127
// 连接失败后立即更新图标
_connectionControl.IsConnected(false);
UpdateConnectIcon();
```

#### **3. 断开连接时**
```csharp
// MainV2.cs:2336-2337
// 断开连接时更新图标
_connectionControl.IsConnected(false);
UpdateConnectIcon();
```

#### **4. 主循环定期更新**
```csharp
// MainV2.cs:3195
// 在主循环中定期调用，检查连接状态并更新图标
UpdateConnectIcon();
```

#### **5. 手动更新**
```csharp
// MainV2.cs:1530-1543
// 通过状态变更事件手动更新
private void UpdateConnectionStatus(bool isConnected)
{
    _connectionControl.IsConnected(isConnected);
    
    if (isConnected)
    {
        this.MenuConnect.Image = displayicons.disconnect;
        this.MenuConnect.Image.Tag = "Disconnect";
    }
    else
    {
        this.MenuConnect.Image = displayicons.connect;
        this.MenuConnect.Image.Tag = "Connect";
    }
}
```

### 6.4 图标变化流程

#### **连接流程中的图标变化**
```
初始状态（断开）
    ↓ 显示: displayicons.connect（红黑分离插头）
用户点击连接
    ↓ 
doConnect() 开始
    ↓ 图标不变（仍为断开状态）
comPort.Open() 执行
    ↓ 可能显示连接进度对话框
TCP连接建立成功
    ↓ 
comPort.BaseStream.IsOpen = true
    ↓
UpdateConnectIcon() 或直接设置
    ↓ 图标立即变化
显示: displayicons.disconnect（已连接插头）
```

#### **断线时的图标变化**
```
连接正常状态
    ↓ 显示: displayicons.disconnect（已连接插头）
TCP连接断开（网络故障、服务器关闭等）
    ↓
comPort.BaseStream.IsOpen = false
    ↓
主循环检测到状态变化 或 读写操作异常触发
    ↓
UpdateConnectIcon() 被调用
    ↓
检查 IsOpen 状态，发现为 false
    ↓ 图标立即变化
显示: displayicons.connect（红黑分离插头）
```

### 6.5 图标资源定义

#### **图标类定义**
```csharp
// MainV2.cs:62-76
public abstract class menuicons
{
    public abstract Image connect { get; }      // 断开连接图标
    public abstract Image disconnect { get; }   // 已连接图标
    // ... 其他图标
}
```

#### **浅色主题图标**
```csharp
// MainV2.cs:169-187
public override Image connect
{
    get
    {
        // 优先使用外部文件，否则使用内嵌资源
        if (File.Exists($"{running_directory}light_connect_icon.png"))
            return Image.FromFile($"{running_directory}light_connect_icon.png");
        else
            return global::MissionPlanner.Properties.Resources.light_connect_icon;
    }
}

public override Image disconnect
{
    get
    {
        if (File.Exists($"{running_directory}light_disconnect_icon.png"))
            return Image.FromFile($"{running_directory}light_disconnect_icon.png");
        else
            return global::MissionPlanner.Properties.Resources.light_disconnect_icon;
    }
}
```

#### **深色主题图标**
```csharp
// MainV2.cs:310-324
public override Image connect
{
    get
    {
        if (File.Exists($"{running_directory}dark_connect_icon.png"))
            return Image.FromFile($"{running_directory}dark_connect_icon.png");
        else
            return global::MissionPlanner.Properties.Resources.dark_connect_icon;
    }
}

public override Image disconnect
{
    get
    {
        if (File.Exists($"{running_directory}dark_disconnect_icon.png"))
            return Image.FromFile($"{running_directory}dark_disconnect_icon.png");
        else
            return global::MissionPlanner.Properties.Resources.dark_disconnect_icon;
    }
}
```

### 6.6 关键特性

#### **1. 更新频率限制**
- ✅ 500毫秒内只更新一次，避免UI频繁刷新
- ✅ 通过 `connectButtonUpdate` 时间戳控制

#### **2. 状态检查优化**
- ✅ 只在状态真正改变时才更新图标
- ✅ 通过 `Image.Tag` 判断当前状态，避免不必要的更新

#### **3. 线程安全**
- ✅ 使用 `BeginInvoke()` 确保UI更新在主线程执行
- ✅ 避免跨线程访问UI控件导致的异常

#### **4. 主题适配**
- ✅ 自动根据当前主题选择对应的图标
- ✅ 支持自定义图标文件（优先级高于内嵌资源）

### 6.7 图标变化示例

#### **场景1：正常连接**
```
1. 初始: 红黑分离插头（断开状态）
2. 点击连接按钮
3. 连接中: 图标保持不变（或显示进度对话框）
4. 连接成功: 立即变为已连接插头（绿色/蓝色）
```

#### **场景2：连接失败**
```
1. 初始: 红黑分离插头（断开状态）
2. 点击连接按钮
3. 连接中: 图标保持不变
4. 连接失败: 立即变回红黑分离插头
```

#### **场景3：连接后断线**
```
1. 已连接: 显示已连接插头
2. 网络断开: TCP连接断开
3. 主循环检测: 定期调用 UpdateConnectIcon()
4. 状态检测: comPort.BaseStream.IsOpen == false
5. 图标更新: 立即变为红黑分离插头
```

## 七、总结

TCP连接过程：
1. 用户触发 → `doConnect()` → 创建 `TcpSerial` → `Open()` → `InitTCPClient()` → TCP三次握手

断线处理：
1. 检测机制：`IsOpen`属性、`VerifyConnected()`、读写操作异常
2. 重连机制：自动重连（`doAutoReconnect()`）和手动重连（`VerifyConnected()`中的重试）
3. 影响范围：读写操作失败、MAVLink通信中断，但可以通过重连机制恢复

图标变化：
1. 两种状态：断开连接（红黑分离插头）和已连接（合拢插头）
2. 更新机制：通过 `UpdateConnectIcon()` 函数，基于 `comPort.BaseStream.IsOpen` 状态更新
3. 触发时机：连接成功/失败、断开连接、主循环定期检查
4. 性能优化：500毫秒更新频率限制、状态检查避免重复更新

关键点：
- TCP连接在 `InitTCPClient()` 中的 `new TcpClient(host, port)` 时建立
- 断线检测主要通过 `client.Client.Connected` 状态和读写异常
- 重连机制有频率限制和重试次数限制，避免过度消耗资源
- 图标变化实时反映连接状态，提供直观的用户反馈

