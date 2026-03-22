# StateChart 场景结构创建指南

## 目标结构

```
Player3D (CharacterBody3D)
└── StateChart
    └── Root (ParallelState)
        ├── Movement (CompoundState)
        │   ├── GroundMode (AtomicState) [Initial]
        │   └── FlyMode (AtomicState)
        └── Action (CompoundState)
            ├── Normal (AtomicState) [Initial]
            ├── Attacked (AtomicState)
            └── Dead (AtomicState)
```

## 创建步骤

### 1. 添加 StateChart 根节点

1. 打开 Player3D 场景
2. 右键点击 Player3D 节点 → Add Child Node
3. 搜索 `StateChart`，选择并添加
4. 重命名为 `StateChart`

### 2. 添加 Root (ParallelState)

1. 右键点击 StateChart → Add Child Node
2. 搜索 `ParallelState`，选择并添加
3. 重命名为 `Root`

### 3. 创建 Movement 分支

**添加 Movement (CompoundState)：**
1. 右键点击 Root → Add Child Node
2. 搜索 `CompoundState`，选择并添加
3. 重命名为 `Movement`
4. 在 Inspector 中设置 `Initial State` = `GroundMode`

**添加 GroundMode (AtomicState)：**
1. 右键点击 Movement → Add Child Node
2. 搜索 `AtomicState`，选择并添加
3. 重命名为 `GroundMode`

**添加 FlyMode (AtomicState)：**
1. 右键点击 Movement → Add Child Node
2. 搜索 `AtomicState`，选择并添加
3. 重命名为 `FlyMode`

**添加 Transitions：**
1. 右键点击 Movement → Add Child Node
2. 搜索 `Transition`，选择并添加
3. 在 Inspector 中配置：
   - `From` = `GroundMode`
   - `To` = `FlyMode`
   - `Event` = `toggle_fly`
4. 重复步骤 1-3，添加反向转换：
   - `From` = `FlyMode`
   - `To` = `GroundMode`
   - `Event` = `toggle_fly`

### 4. 创建 Action 分支

**添加 Action (CompoundState)：**
1. 右键点击 Root → Add Child Node
2. 搜索 `CompoundState`，选择并添加
3. 重命名为 `Action`
4. 在 Inspector 中设置 `Initial State` = `Normal`

**添加 Normal (AtomicState)：**
1. 右键点击 Action → Add Child Node
2. 搜索 `AtomicState`，选择并添加
3. 重命名为 `Normal`

**添加 Attacked (AtomicState)：**
1. 右键点击 Action → Add Child Node
2. 搜索 `AtomicState`，选择并添加
3. 重命名为 `Attacked`

**添加 Dead (AtomicState)：**
1. 右键点击 Action → Add Child Node
2. 搜索 `AtomicState`，选择并添加
3. 重命名为 `Dead`

**添加 Transitions：**

Normal → Attacked:
1. 右键点击 Action → Add Child Node → Transition
2. 配置：`From` = `Normal`, `To` = `Attacked`, `Event` = `on_hit`

Normal → Dead:
1. 右键点击 Action → Add Child Node → Transition
2. 配置：`From` = `Normal`, `To` = `Dead`, `Event` = `on_dead`

Attacked → Normal:
1. 右键点击 Action → Add Child Node → Transition
2. 配置：`From` = `Attacked`, `To` = `Normal`, `Event` = `hit_recovered`

Attacked → Dead:
1. 右键点击 Action → Add Child Node → Transition
2. 配置：`From` = `Attacked`, `To` = `Dead`, `Event` = `on_dead`

### 5. 连接信号到 AnimationControllerComponent

**GroundMode 信号：**
1. 选择 `StateChart/Root/Movement/GroundMode` 节点
2. 切换到 Node 面板（右侧，信号图标）
3. 双击 `state_entered()` 信号
4. 选择 `AnimationControllerComponent` 节点
5. Method 输入：`EnterGroundMode`
6. 点击 Connect

**FlyMode 信号：**
1. 选择 `StateChart/Root/Movement/FlyMode` 节点
2. 双击 `state_entered()` 信号
3. 选择 `AnimationControllerComponent` 节点
4. Method 输入：`EnterFlyMode`
5. 点击 Connect

### 6. 验证结构

完成后，场景树应该显示：

```
Player3D
├── StateChart
│   └── Root (ParallelState)
│       ├── Movement (CompoundState) [initial: GroundMode]
│       │   ├── GroundMode (AtomicState) [绿色信号图标]
│       │   ├── FlyMode (AtomicState) [绿色信号图标]
│       │   ├── Transition (toggle_fly: GroundMode → FlyMode)
│       │   └── Transition (toggle_fly: FlyMode → GroundMode)
│       └── Action (CompoundState) [initial: Normal]
│           ├── Normal (AtomicState)
│           ├── Attacked (AtomicState)
│           ├── Dead (AtomicState)
│           ├── Transition (on_hit: Normal → Attacked)
│           ├── Transition (on_dead: Normal → Dead)
│           ├── Transition (hit_recovered: Attacked → Normal)
│           └── Transition (on_dead: Attacked → Dead)
├── AnimationControllerComponent
├── GroundMovementComponent
├── FlyMovementComponent
└── PlayerInputComponent
```

## 测试

### 测试飞行切换

1. 运行游戏（F5）
2. 按 F 键切换飞行模式
3. 查看控制台输出：
   ```
   [AnimationController] 进入飞行模式
   [StateChart] ⚡ FlyMovementComponent 已唤醒
   [StateChart] 💤 GroundMovementComponent 已休眠
   ```

### 测试地面移动

1. 确保在地面模式（按 F 切换回来）
2. 使用 WASD 移动
3. 按 Space 跳跃
4. 查看动画切换（Idle → Run → Jump）

### 测试飞行移动

1. 按 F 进入飞行模式
2. 使用 WASD 水平移动
3. 按 Space 上升
4. 按 Ctrl 下降
5. 查看动画切换（Idle → Run）

## 调试工具

### 添加 StateChartDebugger

1. 在主场景（不是 Player3D）中添加 UI Layer
2. 右键点击 UI Layer → Add Child Node
3. 搜索 `StateChartDebugger`，选择并添加
4. 在 Inspector 中设置：
   - `Initial node to watch` = `%Player3D`（使用 Unique Name）
5. 运行游戏，可以实时看到状态切换

### 查看状态历史

StateChartDebugger 会显示：
- 当前激活的状态（绿色高亮）
- 状态切换历史
- 待处理的事件队列

## 常见问题

### 信号连接失败

**症状**：按 F 键无反应，控制台无输出

**解决**：
1. 检查 Input Map 是否添加了 `toggle_fly` 动作
2. 检查 PlayerInputComponent 是否发送了事件
3. 检查 Transition 的 Event 名称是否正确（区分大小写）

### 组件未休眠

**症状**：切换到飞行模式后，地面移动仍然生效

**解决**：
1. 检查 GroundMovementComponent 是否调用了 `BindComponentToState()`
2. 检查路径是否正确：`"StateChart/Root/Movement/GroundMode"`
3. 查看控制台是否有 "已绑定到状态" 的日志

### 动画不切换

**症状**：切换模式后动画保持不变

**解决**：
1. 检查 AnimationControllerComponent 的信号连接
2. 查看控制台是否有 "进入XX模式" 的日志
3. 检查 AnimationPlayer 是否有对应的动画

## 扩展：添加更多事件

### 添加受击事件

在 HealthComponent 中：
```csharp
public void TakeDamage(float damage)
{
    _currentHealth -= damage;
    
    if (_currentHealth <= 0)
    {
        parent.SendStateEvent("on_dead");
    }
    else
    {
        parent.SendStateEvent("on_hit");
    }
}
```

### 添加恢复事件

在 Attacked 状态中添加 Timer：
1. 右键点击 Attacked → Add Child Node → Timer
2. 设置 Wait Time = 1.0（受击硬直时间）
3. 设置 One Shot = true
4. 连接 Timer 的 `timeout()` 信号到 StateChart
5. 在信号处理中调用：`parent.SendStateEvent("hit_recovered")`

## 总结

StateChart 结构创建完成后：
- Movement 和 Action 状态可以独立变化（ParallelState）
- 组件生命周期由状态机自动管理（Power Switch）
- 所有状态转换在编辑器中可视化
- 易于调试和扩展
