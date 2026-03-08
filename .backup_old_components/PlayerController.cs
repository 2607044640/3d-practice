using Godot;

/// <summary>
/// 玩家控制器 - 作为 Mediator 统筹 Input 和 Movement 组件
/// 遵循 "Call Down, Signal Up" 原则：
/// - 订阅子组件的事件（Signal Up）
/// - 调用子组件的方法（Call Down）
/// </summary>
[GlobalClass]
public partial class PlayerController : CharacterBody3D
{
    #region Export Properties (依赖注入)
    
    /// <summary>
    /// 输入组件引用（在编辑器中指定）
    /// </summary>
    [Export] public PlayerInputComponent InputComponent { get; set; }
    
    /// <summary>
    /// 移动组件引用（在编辑器中指定）
    /// </summary>
    [Export] public MovementComponent MovementComponent { get; set; }
    
    // TODO: 添加更多组件引用
    // [Export] public AnimationController AnimationController { get; set; }
    // [Export] public HealthComponent HealthComponent { get; set; }
    
    #endregion

    #region Godot Lifecycle
    
    public override void _Ready()
    {
        // 如果未在编辑器中设置，尝试自动获取子节点
        if (InputComponent == null)
        {
            InputComponent = GetNodeOrNull<PlayerInputComponent>("PlayerInputComponent");
        }
        
        if (MovementComponent == null)
        {
            MovementComponent = GetNodeOrNull<MovementComponent>("MovementComponent");
        }
        
        // 验证组件引用
        if (InputComponent == null)
        {
            GD.PushError("PlayerController: InputComponent 未找到！请确保场景中有 PlayerInputComponent 子节点。");
            return;
        }
        
        if (MovementComponent == null)
        {
            GD.PushError("PlayerController: MovementComponent 未找到！请确保场景中有 MovementComponent 子节点。");
            return;
        }
        
        // 订阅输入组件的事件（Signal Up）
        InputComponent.OnMovementInput += HandleMovementInput;
        InputComponent.OnJumpJustPressed += HandleJumpInput;
        
        // TODO: 订阅更多事件
        // InputComponent.OnSprintPressed += HandleSprintPressed;
        // MovementComponent.OnJumped += HandleJumped;
    }
    
    public override void _ExitTree()
    {
        // 取消订阅事件（防止内存泄漏）
        if (InputComponent != null)
        {
            InputComponent.OnMovementInput -= HandleMovementInput;
            InputComponent.OnJumpJustPressed -= HandleJumpInput;
        }
    }
    
    public override void _PhysicsProcess(double delta)
    {
        // 调用移动组件的物理处理（Call Down）
        MovementComponent?.ProcessPhysics(delta);
    }
    
    #endregion

    #region Event Handlers (事件处理 - Mediator 逻辑)
    
    /// <summary>
    /// 处理移动输入事件
    /// </summary>
    private void HandleMovementInput(Vector2 inputDir)
    {
        // 将输入传递给移动组件（Call Down）
        MovementComponent?.UpdateMovementDirection(inputDir);
        
        // TODO: 同时通知动画组件
        // AnimationController?.UpdateMovementBlend(inputDir.Length());
    }
    
    /// <summary>
    /// 处理跳跃输入事件
    /// </summary>
    private void HandleJumpInput()
    {
        // 告诉移动组件执行跳跃（Call Down）
        MovementComponent?.PerformJump();
        
        // TODO: 同时通知动画组件
        // AnimationController?.PlayJumpAnimation();
    }
    
    // TODO: 更多事件处理方法
    // private void HandleSprintPressed() { }
    // private void HandleJumped() { }
    
    #endregion

    #region TODO: 高级功能
    
    // TODO: 状态机集成
    // 如果游戏逻辑复杂，可以在这里集成 FSM (Finite State Machine)
    // private PlayerStateMachine _stateMachine;
    
    // TODO: 调试可视化
    // 在编辑器中显示当前状态、速度等信息
    // public override void _Draw() { }
    
    #endregion
}
