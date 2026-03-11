using Godot;
using Godot.Composition;
using System;

/// <summary>
/// 输入组件抽象基类 - 实现依赖倒置原则
/// 定义输入事件接口，允许 PlayerInput 和 AIInput 共享相同的接口
/// 使得 MovementComponent 等执行组件可以复用于玩家和 AI
/// </summary>
[GlobalClass]
[Component(typeof(CharacterBody3D))]
public abstract partial class BaseInputComponent : Node
{
    #region Events (输入事件接口)
    
    /// <summary>
    /// 移动输入事件 (WASD/方向键 或 AI 决策)
    /// Vector2: X = 左右 (-1 到 1), Y = 前后 (-1 到 1)
    /// </summary>
    public event Action<Vector2> OnMovementInput;
    
    /// <summary>
    /// 跳跃按键刚按下事件（或 AI 决定跳跃）
    /// </summary>
    public event Action OnJumpJustPressed;
    
    #endregion

    #region Export Properties
    
    /// <summary>
    /// 是否启用输入处理
    /// </summary>
    [Export] public bool InputEnabled { get; set; } = true;
    
    #endregion

    #region Godot Lifecycle
    
    public override void _Ready()
    {
        InitializeComponent();
        GD.Print($"BaseInputComponent: 已初始化 ({GetType().Name})");
    }
    
    #endregion

    #region Protected Methods (供子类触发事件)
    
    /// <summary>
    /// 触发移动输入事件
    /// 子类在检测到移动输入时调用此方法
    /// </summary>
    protected void TriggerMovementInput(Vector2 direction)
    {
        OnMovementInput?.Invoke(direction);
    }
    
    /// <summary>
    /// 触发跳跃输入事件
    /// 子类在检测到跳跃输入时调用此方法
    /// </summary>
    protected void TriggerJumpInput()
    {
        OnJumpJustPressed?.Invoke();
    }
    
    #endregion
}
