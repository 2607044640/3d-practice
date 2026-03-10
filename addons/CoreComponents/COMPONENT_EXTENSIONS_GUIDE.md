# ComponentExtensions 使用指南

## 概述

`ComponentExtensions.cs` 提供了一组扩展方法，简化组件查找和事件订阅。

---

## 基础查找方法

### GetComponentInChildren<T>()

在子节点中查找指定类型的组件（支持多态）。

```csharp
// 查找输入组件
var input = parent.GetComponentInChildren<BaseInputComponent>();

// 查找相机
var camera = parent.GetComponentInChildren<Camera3D>();
```

### GetComponentsInChildren<T>()

查找所有指定类型的组件。

```csharp
// 查找所有碰撞体
var colliders = parent.GetComponentsInChildren<CollisionShape3D>();

foreach (var collider in colliders)
{
    GD.Print($"Found: {collider.Name}");
}
```

### GetRequiredComponentInChildren<T>()

查找组件，未找到则报错。

```csharp
// 必须存在的组件
var input = parent.GetRequiredComponentInChildren<BaseInputComponent>();
// 如果未找到，会自动打印错误日志
```

---

## 输入组件专用方法

### FindAndSubscribeInput()

一行代码完成查找和订阅！

```csharp
public partial class MovementComponent : Node
{
    private BaseInputComponent _inputComponent;
    
    public void OnEntityReady()
    {
        // 旧方式：20+ 行代码
        // foreach (var child in parent.GetChildren())
        // {
        //     if (child is BaseInputComponent inputComp)
        //     {
        //         _inputComponent = inputComp;
        //         break;
        //     }
        // }
        // _inputComponent.OnMovementInput += HandleMovementInput;
        // _inputComponent.OnJumpJustPressed += HandleJumpInput;
        
        // 新方式：1 行代码！
        _inputComponent = parent.FindAndSubscribeInput(
            HandleMovementInput,
            HandleJumpInput
        );
    }
    
    private void HandleMovementInput(Vector2 dir) { /* ... */ }
    private void HandleJumpInput() { /* ... */ }
}
```

**只订阅移动输入：**
```csharp
_inputComponent = parent.FindAndSubscribeInput(HandleMovementInput);
```

### UnsubscribeInput()

取消订阅输入事件。

```csharp
public override void _ExitTree()
{
    // 旧方式
    // if (_inputComponent != null)
    // {
    //     _inputComponent.OnMovementInput -= HandleMovementInput;
    //     _inputComponent.OnJumpJustPressed -= HandleJumpInput;
    // }
    
    // 新方式
    _inputComponent?.UnsubscribeInput(HandleMovementInput, HandleJumpInput);
}
```

---

## 调试方法

### PrintComponents()

打印节点的所有子组件（调试用）。

```csharp
public override void _Ready()
{
    parent.PrintComponents();
    // 输出：
    // === Components in Player3D ===
    //   - PlayerInputComponent (PlayerInputComponent)
    //   - MovementComponent (MovementComponent)
    //   - CharacterRotationComponent (CharacterRotationComponent)
    //   - ...
}
```

---

## 完整示例

### MovementComponent

```csharp
[GlobalClass]
[Component(typeof(CharacterBody3D))]
public partial class MovementComponent : Node
{
    private BaseInputComponent _inputComponent;
    
    public override void _Ready()
    {
        InitializeComponent();
    }
    
    public void OnEntityReady()
    {
        _inputComponent = parent.FindAndSubscribeInput(
            HandleMovementInput,
            HandleJumpInput
        );
    }
    
    public override void _ExitTree()
    {
        _inputComponent?.UnsubscribeInput(HandleMovementInput, HandleJumpInput);
    }
    
    private void HandleMovementInput(Vector2 dir)
    {
        // 处理移动
    }
    
    private void HandleJumpInput()
    {
        // 处理跳跃
    }
}
```

### CharacterRotationComponent

```csharp
[GlobalClass]
[Component(typeof(CharacterBody3D))]
public partial class CharacterRotationComponent : Node
{
    private BaseInputComponent _inputComponent;
    
    public override void _Ready()
    {
        InitializeComponent();
    }
    
    public void OnEntityReady()
    {
        _inputComponent = parent.FindAndSubscribeInput(HandleMovementInput);
    }
    
    public override void _ExitTree()
    {
        _inputComponent?.UnsubscribeInput(HandleMovementInput);
    }
    
    private void HandleMovementInput(Vector2 dir)
    {
        // 处理旋转
    }
}
```

---

## 优势

- ✅ **代码简洁**：从 20+ 行减少到 1 行
- ✅ **类型安全**：泛型支持编译期检查
- ✅ **支持多态**：自动识别子类
- ✅ **易于维护**：逻辑集中在扩展方法中
- ✅ **零学习成本**：类似 Unity 的 GetComponent API

---

## 性能

- **查找性能**：O(n) 遍历子节点，通常 n < 10
- **运行时开销**：极小，仅在初始化时调用一次
- **内存开销**：零额外内存

---

## 扩展建议

如果需要更多功能，可以添加：

```csharp
// 在父节点中查找
public static T GetComponentInParent<T>(this Node node) where T : Node
{
    var current = node.GetParent();
    while (current != null)
    {
        if (current is T component)
            return component;
        current = current.GetParent();
    }
    return null;
}

// 在兄弟节点中查找
public static T GetComponentInSiblings<T>(this Node node) where T : Node
{
    var parent = node.GetParent();
    if (parent == null) return null;
    
    return parent.GetComponentInChildren<T>();
}
```
