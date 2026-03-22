# godot-statecharts 节点参考手册

## 核心节点

### StateChart
**用途**: 状态机根节点，包含整个状态机逻辑

**关键属性**:
- `track_in_editor` - 是否在编辑器中调试追踪
- `warn_on_sending_unknown_events` - 发送未定义事件时是否警告
- `initial_expression_properties` - 表达式属性初始值（用于 Guard）

**关键方法**:
- `send_event(event:StringName)` - 发送事件触发状态转换
- `set_expression_property(name, value)` - 设置表达式属性
- `freeze()` / `thaw()` - 冻结/解冻状态机

**信号**:
- `event_received(event)` - 接收到事件时触发

**使用场景**: 作为 Player3D 的子节点，管理整个角色状态机

---

### AtomicState
**用途**: 原子状态，不包含子状态的最底层状态

**特点**:
- 不能有子状态（只能有 Transition）
- 是状态层级的叶子节点
- 进入时触发 `state_entered` 信号
- 退出时触发 `state_exited` 信号

**使用场景**: 
- `GroundMode` - 地面移动状态
- `FlyMode` - 飞行移动状态
- `Normal` - 正常动作状态
- `Attacked` - 受击状态
- `Dead` - 死亡状态

---

### CompoundState
**用途**: 复合状态，包含多个子状态，同一时间只有一个子状态激活

**关键属性**:
- `initial_state` - 初始子状态路径（必须设置）

**信号**:
- `child_state_entered()` - 子状态进入时触发
- `child_state_exited()` - 子状态退出时触发

**特点**:
- 必须有至少 2 个子状态
- 进入时自动激活 initial_state
- 支持 HistoryState 记忆上次激活的子状态

**使用场景**:
- `Movement` - 包含 GroundMode 和 FlyMode
- `Action` - 包含 Normal, Attacked, Dead

---

### ParallelState
**用途**: 并行状态，所有子状态同时激活

**特点**:
- 进入时激活所有子状态
- 退出时退出所有子状态
- 子状态之间独立运行
- 必须有至少 2 个子状态

**使用场景**:
- `Root` - 同时管理 Movement 和 Action 两个维度的状态

---

### Transition
**用途**: 状态转换，定义从一个状态到另一个状态的转换规则

**关键属性**:
- `to` - 目标状态路径（必须设置）
- `event` - 触发事件名（空则为自动转换）
- `guard` - 转换条件（Guard 节点）
- `delay_in_seconds` - 延迟时间表达式（默认 "0.0"）

**信号**:
- `taken()` - 转换执行时触发

**转换类型**:
1. **事件转换**: `event` 不为空，由 `send_event()` 触发
2. **自动转换**: `event` 为空，进入状态时自动尝试
3. **延迟转换**: `delay_in_seconds > 0`，延迟后执行

**使用场景**:
- `toggle_fly` 事件: GroundMode ↔ FlyMode
- `on_hit` 事件: Normal → Attacked
- `on_dead` 事件: Normal/Attacked → Dead
- `hit_recovered` 事件: Attacked → Normal

---

### HistoryState
**用途**: 历史状态，记忆上次激活的子状态

**关键属性**:
- `deep` - 是否深度历史（记忆整个子状态树）
- `default_state` - 无历史时的默认状态

**特点**:
- 必须是 CompoundState 的子节点
- 不能有子节点
- 浅历史：只记忆直接子状态
- 深历史：记忆整个子状态层级

**使用场景**: 
- 暂停后恢复到暂停前的状态
- 切换场景后恢复角色状态

---

## 动画集成节点（已弃用）

### AnimationPlayerState
**用途**: 进入状态时自动播放 AnimationPlayer 动画

**关键属性**:
- `animation_player` - AnimationPlayer 节点路径
- `animation_name` - 动画名称（空则使用状态名）
- `custom_blend` - 自定义混合时间
- `custom_speed` - 自定义播放速度
- `from_end` - 是否从末尾播放

**状态**: ⚠️ 已弃用，将在未来版本移除

**替代方案**: 使用信号驱动的 AnimationControllerComponent

---

### AnimationTreeState
**用途**: 进入状态时自动切换 AnimationTree 状态机

**关键属性**:
- `animation_tree` - AnimationTree 节点路径
- `state_name` - 状态机中的状态名（空则使用状态名）

**状态**: ⚠️ 已弃用，将在未来版本移除

**替代方案**: 使用信号驱动的 AnimationControllerComponent

---

## Guard 节点（转换条件）

### ExpressionGuard
**用途**: 使用表达式判断转换条件

**关键属性**:
- `expression` - 表达式字符串（如 "health > 0"）

**可用变量**: StateChart 的 expression_properties

---

### StateIsActiveGuard
**用途**: 检查特定状态是否激活

**关键属性**:
- `state` - 要检查的状态路径

---

### AllOfGuard
**用途**: 所有子 Guard 都满足时才通过（AND 逻辑）

---

### AnyOfGuard
**用途**: 任一子 Guard 满足时就通过（OR 逻辑）

---

### NotGuard
**用途**: 反转子 Guard 的结果（NOT 逻辑）

---

## 在 Player3D 中的使用

### 推荐结构

```
Player3D (CharacterBody3D)
├── StateChart
│   └── Root (ParallelState)
│       ├── Movement (CompoundState, initial: GroundMode)
│       │   ├── GroundMode (AtomicState)
│       │   ├── FlyMode (AtomicState)
│       │   ├── Transition (event: toggle_fly, from: GroundMode, to: FlyMode)
│       │   └── Transition (event: toggle_fly, from: FlyMode, to: GroundMode)
│       └── Action (CompoundState, initial: Normal)
│           ├── Normal (AtomicState)
│           ├── Attacked (AtomicState)
│           ├── Dead (AtomicState)
│           ├── Transition (event: on_hit, from: Normal, to: Attacked)
│           ├── Transition (event: on_dead, from: Normal, to: Dead)
│           ├── Transition (event: hit_recovered, from: Attacked, to: Normal)
│           └── Transition (event: on_dead, from: Attacked, to: Dead)
├── AnimationControllerComponent
├── GroundMovementComponent
├── FlyMovementComponent
└── PlayerInputComponent
```

### 信号连接

**AnimationControllerComponent**:
- `GroundMode.state_entered` → `AnimationControllerComponent.EnterGroundMode()`
- `FlyMode.state_entered` → `AnimationControllerComponent.EnterFlyMode()`

**GroundMovementComponent**:
- 使用 `BindComponentToState(parent, "StateChart/Root/Movement/GroundMode")`

**FlyMovementComponent**:
- 使用 `BindComponentToState(parent, "StateChart/Root/Movement/FlyMode")`

### 事件发送

**PlayerInputComponent**:
```csharp
if (Input.IsActionJustPressed("toggle_fly"))
    parent.SendStateEvent("toggle_fly");
```

**HealthComponent**:
```csharp
if (_currentHealth <= 0)
    parent.SendStateEvent("on_dead");
else
    parent.SendStateEvent("on_hit");
```

---

## 调试工具

### StateChartDebugger
**用途**: 实时查看状态机状态

**使用方法**:
1. 在主场景（非 Player3D）添加 UI Layer
2. 添加 StateChartDebugger 节点
3. 设置 `Initial node to watch` = `%Player3D`（使用 Unique Name）
4. 运行游戏查看实时状态

**显示内容**:
- 当前激活的状态（绿色高亮）
- 状态切换历史
- 待处理的事件队列

---

## 性能优化建议

1. **避免深层嵌套**: 状态层级不要超过 3-4 层
2. **使用 ParallelState 分离关注点**: Movement 和 Action 独立管理
3. **缓存状态引用**: 不要每帧调用 `GetNode()`
4. **使用 freeze()/thaw()**: 暂停时冻结状态机节省性能
5. **避免频繁事件**: 合并相似事件，减少状态转换次数

---

## 常见模式

### 临时状态（Attacked）
```
Normal → Attacked (on_hit)
Attacked → Normal (hit_recovered, delay: 1.0)
```

### 不可逆状态（Dead）
```
Normal/Attacked → Dead (on_dead)
Dead 无出口转换
```

### 切换状态（GroundMode ↔ FlyMode）
```
GroundMode ↔ FlyMode (toggle_fly)
使用同一事件双向切换
```

### 条件转换（使用 Guard）
```
Transition:
  event: "try_fly"
  guard: ExpressionGuard("stamina > 10")
  to: FlyMode
```

---

## 总结

godot-statecharts 提供了完整的状态机解决方案：

- **StateChart**: 状态机根节点
- **AtomicState**: 叶子状态
- **CompoundState**: 单选容器（一次一个子状态）
- **ParallelState**: 多选容器（所有子状态同时激活）
- **Transition**: 状态转换规则
- **HistoryState**: 历史记忆
- **Guard**: 转换条件

配合 Power Switch 架构，实现组件生命周期的自动管理，达到极致解耦。
