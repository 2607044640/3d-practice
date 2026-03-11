# CoreComponents 架构说明

## 依赖注入方案

### 问题
Godot.Composition 插件的 `ComponentContainer` 只注册具体类型和接口，不注册基类。
这导致 `[ComponentDependency(typeof(BaseInputComponent))]` 无法工作。

### 解决方案
使用 `ComponentExtensions.cs` 提供的扩展方法手动查找组件。

### 使用方法

```csharp
[Component(typeof(CharacterBody3D))]
public partial class MovementComponent : Node
{
    private BaseInputComponent _inputComponent;
    
    public void OnEntityReady()
    {
        // 一行代码搞定查找和订阅
        _inputComponent = parent.FindAndSubscribeInput(
            HandleMovementInput,
            HandleJumpInput
        );
    }
    
    public override void _ExitTree()
    {
        // 取消订阅
        _inputComponent?.UnsubscribeInput(HandleMovementInput, HandleJumpInput);
    }
}
```

### 优势
- ✅ 简单可靠，一行代码解决
- ✅ 支持多态（可以查找基类）
- ✅ 无需修改插件
- ✅ 无运行时反射开销

### 文件位置
- 扩展方法：`addons/CoreComponents/ComponentExtensions.cs`
- 使用示例：`addons/CoreComponents/MovementComponent.cs`
