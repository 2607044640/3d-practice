# Godot.Composition 重构迁移指南

## 📋 重构概述

本次重构将传统的 Godot 组件架构迁移到 `Godot.Composition` 框架，实现真正的组件化和依赖注入。

## 🎯 核心改变

### 1. Entity（实体）
**之前：** Player3D 包含大量逻辑、组件引用、事件转发
**之后：** Player3D 只是一个纯粹的容器，只调用 `InitializeEntity()`

### 2. Component（组件）
**之前：** 组件通过 `[Export]` 或 `GetNode()` 获取其他组件
**之后：** 组件通过 `[ComponentDependency]` 自动注入依赖

### 3. 事件订阅
**之前：** Entity 作为中介者订阅和转发事件
**之后：** 组件在 `OnEntityReady()` 中自己订阅事件

## 📦 安装步骤

### 步骤 1：安装 NuGet 包

```cmd
cd c:\Godot\3d-practice
dotnet add package Godot.Composition
```

### 步骤 2：替换文件

将以下文件替换为重构版本：

1. `Scripts/Player3D.cs` → `Scripts/Player3D_Refactored.cs`
2. `addons/CoreComponents/PlayerInputComponent.cs` → `PlayerInputComponent_Refactored.cs`
3. `addons/CoreComponents/MovementComponent.cs` → `MovementComponent_Refactored.cs`
4. `addons/CoreComponents/AnimationControllerComponent.cs` → `AnimationControllerComponent_Refactored.cs`

新增文件：
- `addons/CoreComponents/CharacterRotationComponent.cs`
- `addons/CoreComponents/CameraControlComponent.cs`

### 步骤 3：更新场景文件

在 `Player3D.tscn` 中添加新组件：

1. 打开 `Scenes/Player3D.tscn`
2. 在 Player3D 节点下添加：
   - `CharacterRotationComponent` 节点
   - `CameraControlComponent` 节点

或者手动编辑 `.tscn` 文件，添加：

```
[node name="CharacterRotationComponent" type="Node" parent="."]
script = ExtResource("path_to_CharacterRotationComponent.cs")

[node name="CameraControlComponent" type="Node" parent="."]
script = ExtResource("path_to_CameraControlComponent.cs")
```

### 步骤 4：编译项目

```cmd
dotnet build 3dPractice.sln
```

## 🔍 重构详解

### Player3D.cs

**之前（180+ 行）：**
```csharp
public partial class Player3D : CharacterBody3D
{
    [Export] public PlayerInputComponent InputComponent { get; set; }
    [Export] public MovementComponent MovementComponent { get; set; }
    
    private void InitializeComponents() { ... }
    private void SubscribeToComponentEvents() { ... }
    private void HandleMovementInput(Vector2 inputDir) { ... }
    private void UpdateCharacterRotation(double delta) { ... }
    private void HandleCameraInput(InputEvent @event) { ... }
}
```

**之后（10 行）：**
```csharp
[Entity]
public partial class Player3D : CharacterBody3D
{
    public override void _Ready()
    {
        InitializeEntity();
    }
}
```

### MovementComponent.cs

**关键改变：**

1. 添加标签：
```csharp
[Component(typeof(CharacterBody3D))]
[ComponentDependency(typeof(PlayerInputComponent))]
```

2. 使用 `parent` 访问 Entity：
```csharp
// 之前
Body.Velocity = velocity;
Body.MoveAndSlide();

// 之后
parent.Velocity = velocity;
parent.MoveAndSlide();
```

3. 在 `OnEntityReady()` 中订阅事件：
```csharp
public void OnEntityReady()
{
    // playerInputComponent 是自动生成的魔法变量
    playerInputComponent.OnMovementInput += HandleMovementInput;
    playerInputComponent.OnJumpJustPressed += HandleJumpInput;
}
```

4. 自己处理 `_PhysicsProcess`：
```csharp
// 之前：由 Player3D 调用
// Player3D._PhysicsProcess() -> MovementComponent.ProcessPhysics()

// 之后：组件自己处理
public override void _PhysicsProcess(double delta)
{
    ProcessPhysics(delta);
}
```

### CharacterRotationComponent.cs（新组件）

从 Player3D 中抽离的角色旋转逻辑：

```csharp
[Component(typeof(CharacterBody3D))]
[ComponentDependency(typeof(PlayerInputComponent))]
public partial class CharacterRotationComponent : Node
{
    public void OnEntityReady()
    {
        playerInputComponent.OnMovementInput += HandleMovementInput;
    }
    
    private void UpdateCharacterRotation(double delta) { ... }
}
```

### CameraControlComponent.cs（新组件）

从 Player3D 中抽离的相机控制逻辑：

```csharp
[Component(typeof(CharacterBody3D))]
public partial class CameraControlComponent : Node
{
    public override void _UnhandledInput(InputEvent @event)
    {
        HandleCameraInput(@event);
    }
}
```

## 🎨 架构对比

### 之前（中介者模式）

```
Player3D (中介者)
├─ 获取 InputComponent
├─ 获取 MovementComponent
├─ 订阅 InputComponent.OnMovementInput
├─ 转发给 MovementComponent.UpdateMovementDirection()
└─ 自己处理相机和旋转
```

### 之后（组件自治）

```
Player3D (纯容器)
└─ InitializeEntity()

PlayerInputComponent
└─ 发出事件

MovementComponent
├─ [ComponentDependency(PlayerInputComponent)]
└─ OnEntityReady() 中订阅事件

CharacterRotationComponent
├─ [ComponentDependency(PlayerInputComponent)]
└─ OnEntityReady() 中订阅事件

CameraControlComponent
└─ 独立处理相机
```

## ✅ 验证清单

- [ ] 安装 Godot.Composition NuGet 包
- [ ] 替换所有重构文件
- [ ] 在场景中添加新组件节点
- [ ] 编译成功（无错误）
- [ ] 运行游戏，测试移动
- [ ] 测试跳跃
- [ ] 测试相机旋转
- [ ] 测试角色朝向
- [ ] 测试动画切换

## 🚀 优势

1. **Entity 极简化**：Player3D 从 180+ 行减少到 10 行
2. **组件自治**：每个组件自己管理依赖和事件
3. **零 GetNode**：所有依赖通过 Source Generator 自动注入
4. **类型安全**：编译期检查依赖关系
5. **易于测试**：组件完全解耦，可独立测试
6. **易于扩展**：添加新组件无需修改 Entity

## 📝 注意事项

1. **partial class**：所有 Entity 和 Component 必须是 `partial class`
2. **魔法变量**：依赖注入的变量名是首字母小写的类型名（如 `playerInputComponent`）
3. **OnEntityReady()**：事件订阅必须在这里完成，不能在 `_Ready()` 中
4. **parent 变量**：由 Source Generator 自动生成，直接使用即可

## 🔧 故障排除

### 编译错误："InitializeEntity() 不存在"
- 确保类声明为 `partial class`
- 确保添加了 `[Entity]` 标签
- 重新构建项目

### 编译错误："playerInputComponent 不存在"
- 确保添加了 `[ComponentDependency(typeof(PlayerInputComponent))]`
- 确保类声明为 `partial class`
- 重新构建项目

### 运行时错误："组件未找到"
- 确保所有组件节点都在场景中
- 确保组件脚本正确附加到节点
- 检查 Godot 编辑器的输出日志

## 📚 参考资源

- Godot.Composition GitHub: https://github.com/MysteriousMilk/Godot.Composition
- C# Source Generators: https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview
