using Godot;
using Godot.Composition;
using System;

/// <summary>
/// 实体输入接口 - 定义所有输入组件的契约
/// 继承 IComponent 以支持 Godot.Composition 的自动依赖注入
/// 允许 PlayerInput 和 AIInput 共享相同的接口，使得 MovementComponent 等执行组件可以复用
/// </summary>
public interface IEntityInput : IComponent
{
    /// <summary>
    /// 移动输入事件 (WASD/方向键 或 AI 决策)
    /// Vector2: X = 左右 (-1 到 1), Y = 前后 (-1 到 1)
    /// </summary>
    event Action<Vector2> OnMovementInput;
    
    /// <summary>
    /// 跳跃按键刚按下事件（或 AI 决定跳跃）
    /// </summary>
    event Action OnJumpJustPressed;
    
    /// <summary>
    /// 是否启用输入处理
    /// 当为 false 时，实现类不应触发任何事件
    /// </summary>
    bool InputEnabled { get; set; }
}
