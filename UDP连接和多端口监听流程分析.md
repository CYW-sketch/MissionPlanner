# UDP连接和多端口监听流程分析

## 一、核心概念

### 1. 关键变量
- `EnableDualListen`: 是否启用双端口监听（默认false，配置中设置为true）
- `_manualConnectedOnce`: 标记是否已经手动连接过一次
- `_udpPortA`: 14551（默认被动端口）
- `_udpPortB`: 14552
- `_passiveMav`: 被动监听的MAVLink接口实例
- `_passiveQuality`: 被动监听端口的连接质量（0.0-1.0）
- `_qualityThreshold`: 质量阈值（默认0.7）
- `_qualityDifferenceThreshold`: 质量差异阈值（默认0.1）

### 2. 端口选择逻辑
- **主动端口规则**：用户手动选择（如14550、14551、14552等）
- **被动端口规则**：
  - 如果主动端口是14551 → 被动端口为14552
  - 如果主动端口是14552 → 被动端口为14551
  - 如果主动端口是其他端口 → 被动端口默认14551

---

## 二、UDP连接流程

### 阶段1：用户发起连接

**触发点**：用户点击"连接"按钮
**位置**：`butConnect_Click` → `CMB_serialport_SelectedIndexChanged`

**流程**：
```
1. 检查连接类型（comPortName）
   ↓
2. 如果是"UDP"或"UDP:端口"格式
   ↓
3. 调用 SelectUdpPort() 弹出端口输入对话框
   ↓
4. 用户输入端口号（如14551）
   ↓
5. 创建 UdpSerial 对象，设置端口
   ↓
6. 调用 AutoConnectManager.MarkManualConnect()
   ↓
7. 调用 doConnect(comPort, "preset", "0")
```

**关键代码位置**：
- `MainV2.cs:2394-2419` - UDP连接处理逻辑
- `MainV2.cs:1942-1961` - doConnect中的UDP分支
- `MainV2.cs:5359-5399` - SelectUdpPort端口选择对话框

---

### 阶段2：连接初始化

**触发点**：`doConnect()` 方法执行
**位置**：`MainV2.cs:1901`

**流程**：
```
1. comPort.Open() 打开UDP连接
   ↓
2. 连接成功后，在UI线程中执行：
   ↓
3. AutoConnectManager.Initialize()
   ↓
4. AutoConnectManager.EnableAutoConnect()
```

**关键操作**：
- `Initialize()`: 启动连接状态监控定时器（每秒检查一次）
- 订阅主动链路的质量监控（使用Reactive LINQ）
- 启动质量报告定时器（每30秒报告一次）

**关键代码位置**：
- `MainV2.cs:5489-5527` - Initialize方法
- `MainV2.cs:5505-5515` - 主动链路质量监控订阅

---

### 阶段3：被动监听启动

**触发条件**：
- `EnableDualListen == true`
- `_manualConnectedOnce == true`（已手动连接过一次）

**触发点**：通常在连接成功后，通过回调或定时器触发
**位置**：`MainV2.cs:5529-5599`

**流程**：
```
1. SetupPassiveListener() 被调用
   ↓
2. 检查是否为UDP连接类型
   ↓
3. 获取当前主动端口（如14551）
   ↓
4. 根据规则选择被动端口（如14552）
   ↓
5. 创建新的 UdpSerial 对象
   ↓
6. 在后台线程中：
   - udp.Open() 打开被动监听
   - 创建被动MAVLink接口
   - _passiveMav.Open() 连接被动端口
   ↓
7. SetupPassiveQualityMonitoring() 启动被动质量监控
```

**关键代码位置**：
- `MainV2.cs:5547-5587` - UDP被动监听设置
- `MainV2.cs:5604-5638` - 被动质量监控设置

**被动监听特点**：
- 在后台线程中异步启动，不阻塞主连接
- 使用 `getparams: false, skipconnectedcheck: true, showui: false` 静默连接
- 不显示连接UI，避免干扰用户体验

---

## 三、多端口监听工作机制

### 1. 双端口监听原理

**主动端口（Active Port）**：
- 用户手动选择的端口
- 作为主连接，处理所有通信
- UI显示此端口的连接状态
- 质量监控每1秒更新一次

**被动端口（Passive Port）**：
- 自动选择的备用端口
- 在后台静默监听
- 仅用于质量监控和自动切换
- 质量监控每1秒更新一次

### 2. 质量监控系统

**主动链路监控**：
```csharp
// 位置：MainV2.cs:5505-5515
mav.WhenPacketReceived.Buffer(TimeSpan.FromSeconds(_qualityWindowSec))
mav.WhenPacketLost.Buffer(TimeSpan.FromSeconds(_qualityWindowSec))
→ 计算质量 = RX / (RX + Lost)
→ 调用 EvaluateQualityAndMaybeSwitch(quality)
```

**被动链路监控**：
```csharp
// 位置：MainV2.cs:5614-5628
_passiveMav.WhenPacketReceived.Buffer(...)
_passiveMav.WhenPacketLost.Buffer(...)
→ 计算 _passiveQuality = RX / (RX + Lost)
→ 每5秒报告一次质量
```

**质量计算**：
- 质量 = 接收到的数据包数 / (接收到的数据包数 + 丢失的数据包数)
- 范围：0.0（完全失败）到1.0（完美连接）
- 使用滑动窗口（默认3秒）计算平均值

---

### 3. 自动切换决策

**触发条件**：`EvaluateQualityAndMaybeSwitch()` 每秒调用
**位置**：`MainV2.cs:6094-6168`

**切换决策算法**（`ShouldSwitchToPassive()`）：
```csharp
// 位置：MainV2.cs:6205-6226

条件1: 主动质量 < 阈值 && 被动质量 >= 阈值
  → 切换（主动质量差，被动质量好）

条件2: 被动质量 > 主动质量 + 差异阈值（0.1）
  → 切换（被动质量显著更好）

条件3: 主动质量 < 阈值*0.5 && 被动质量 > 阈值*0.7
  → 切换（主动质量很低，被动质量较好）
```

**切换限制**：
- 最小切换间隔：`_minSwitchIntervalSec`（默认10秒）
- 防止频繁切换

**切换流程**：
```
1. ShouldSwitchToPassive() 返回 true
   ↓
2. 检查是否超过最小切换间隔
   ↓
3. 调用 SwitchToPassivePort()
   ↓
4. 断开当前主动连接
   ↓
5. 将被动端口设置为新的主动端口
   ↓
6. 重新打开连接
   ↓
7. 重新调用 SetupPassiveListener()
   （现在原来的主动端口变成被动端口）
```

**关键代码位置**：
- `MainV2.cs:6205-6226` - 切换决策算法
- `MainV2.cs:6231-6276` - 执行切换操作

---

### 4. 连接状态监控

**定时器**：每秒执行一次
**位置**：`MainV2.cs:5846-5894`

**监控内容**：
1. **连接状态检查**：
   - 检查 `BaseStream.IsOpen`
   - 检查是否有有效数据包（2秒内）

2. **数据包超时检测**：
   - 如果超过2秒没收到数据包 → 触发重连

3. **连接断开处理**：
   - UDP连接断开 → `AttemptUdpReconnect()`
   - TCP连接断开 → `AttemptReconnect()`

**重连逻辑**（`AttemptUdpReconnect()`）：
```
1. 检查是否有被动监听可用
   ↓
2. 如果有 → 直接切换到被动端口
   ↓
3. 如果没有 → 尝试重新设置被动监听
   ↓
4. 等待2秒让被动监听建立
   ↓
5. 切换到被动端口
```

---

## 四、完整执行时序图

```
用户操作
  │
  ├─→ 选择UDP连接
  │     │
  │     ├─→ SelectUdpPort() 弹出端口输入对话框
  │     │     │
  │     │     └─→ 用户输入端口（如14551）
  │     │
  │     └─→ 创建UdpSerial对象
  │           │
  │           └─→ doConnect() 打开连接
  │                 │
  │                 ├─→ AutoConnectManager.Initialize()
  │                 │     │
  │                 │     ├─→ 启动连接监控定时器（1秒）
  │                 │     └─→ 订阅主动链路质量监控
  │                 │
  │                 └─→ SetupPassiveListener() [如果启用双监听]
  │                       │
  │                       ├─→ 选择被动端口（如14552）
  │                       ├─→ 后台线程打开被动监听
  │                       └─→ SetupPassiveQualityMonitoring()
  │
  ├─→ [每秒执行] 连接状态检查
  │     │
  │     ├─→ 检查连接是否断开
  │     ├─→ 检查数据包超时
  │     └─→ 如有问题 → AttemptUdpReconnect()
  │
  ├─→ [实时] 主动链路质量监控
  │     │
  │     └─→ EvaluateQualityAndMaybeSwitch()
  │           │
  │           └─→ [如果满足切换条件] SwitchToPassivePort()
  │
  └─→ [实时] 被动链路质量监控
        │
        └─→ 更新 _passiveQuality
```

---

## 五、关键方法说明

### SetupPassiveListener()
**功能**：设置被动监听端口
**触发时机**：
- 手动连接成功后
- 端口切换后（重新设置被动监听）
- UDP重连时尝试重新建立

**关键逻辑**：
- 根据主动端口智能选择被动端口
- 在后台线程中异步启动
- 静默连接，不显示UI

### SwitchToPassivePort()
**功能**：切换到被动监听端口
**触发时机**：
- 主动链路质量差，被动链路质量好时
- 主动连接断开时

**执行步骤**：
1. 获取被动端口号
2. 断开当前主动连接
3. 将被动端口设置为新的主动端口
4. 重新打开连接
5. 重新设置被动监听（原来的主动端口变成被动端口）

### EvaluateQualityAndMaybeSwitch()
**功能**：评估链路质量并决定是否切换
**执行频率**：每秒执行一次
**决策逻辑**：使用`ShouldSwitchToPassive()`算法

### CheckConnectionStatus()
**功能**：检查连接状态
**执行频率**：每秒执行一次
**检查内容**：
- 连接是否打开
- 是否有有效数据包（2秒内）

---

## 六、配置参数

### 质量阈值
- `QualityThreshold`: 0.7（连接质量阈值）
- `QualityDifferenceThreshold`: 0.1（质量差异阈值）
- `QualityWindowSec`: 3（质量计算窗口秒数）

### 切换参数
- `MinSwitchIntervalSec`: 10（最小切换间隔秒数）
- 连接超时：2秒（无数据包认为断开）

### 端口配置
- `_udpPortA`: "14551"
- `_udpPortB`: "14552"
- 默认端口：14551

---

## 七、注意事项

1. **被动监听启动条件**：
   - 必须已手动连接过一次（`_manualConnectedOnce == true`）
   - 必须启用双监听（`EnableDualListen == true`）

2. **端口切换的限制**：
   - 最小切换间隔：10秒
   - 防止频繁切换造成连接不稳定

3. **TCP被动监听**：
   - 已在代码中禁用，避免干扰UDP连接

4. **连接状态判断**：
   - 使用`_passiveMav != null && _passiveMav.BaseStream?.IsOpen == true && _passiveQualitySub != null`
   - 确保被动监听真正启动并接收数据

5. **质量报告**：
   - 主动链路：每5秒报告一次
   - 被动链路：每5秒报告一次
   - 总体状态：每30秒报告一次

