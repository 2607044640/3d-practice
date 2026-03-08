using Godot;

/// <summary>
/// 移动组件 - 仅负责物理计算与位移
/// 遵循单一职责原则：只处理移动，不处理输入或动画
/// </summary>
[GlobalClass]
public partial class MovementComponent : Node
{
    #region Export Properties (依赖注入)
    
    /// <summary>
    /// 需要控制的物理身体（会自动获取父节点）
    /// </summary>
    [Export] public CharacterBody3D Body { get; set; }
    
    /// <summary>
    /// 相机引用（用于计算相对于相机的移动方向）
    /// </summary>
    [Export] public Camera3D Camera { get; set; }
    
    /// <summary>
    /// 移动速度 (米/秒)
    /// </summary>
    [Export] public float Speed { get; set; } = 5.0f;
    
    /// <summary>
    /// 跳跃初速度 (米/秒)
    /// </summary>
    [Export] public float JumpVelocity { get; set; } = 4.5f;
    
    /// <summary>
    /// 重力加速度 (使用项目设置中的默认值)
    /// </summary>
    [Export] public float Gravity { get; set; } = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
    
    #endregion

    #region Private State
    
    // 当前移动方向 (归一化的 2D 输入)
    private Vector2 _currentInputDirection = Vector2.Zero;
    
    // 是否请求跳跃
    private bool _jumpRequested = false;
    
    #endregion

    #region Godot Lifecycle
    
    public override void _Ready()
    {
        // 如果未在编辑器中设置，自动获取父节点作为 Body
        if (Body == null)
        {
            Body = GetParent<CharacterBody3D>();
        }
        
        // 自动查找相机
        if (Camera == null && Body != null)
        {
            Camera = Body.GetNodeOrNull<Camera3D>("CameraPivot/SpringArm3D/Camera3D");
        }
        
        if (Body == null)
        {
            GD.PushError("MovementComponent: 无法找到 CharacterBody3D！");
        }
        else
        {
            GD.Print($"MovementComponent: 已连接到 Body: {Body.Name}");
            GD.Print($"MovementComponent: Speed={Speed}, JumpVelocity={JumpVelocity}, Gravity={Gravity}");
        }
        
        if (Camera == null)
        {
            GD.PushWarning("MovementComponent: 未找到相机，将使用角色本地坐标系移动。");
        }
        else
        {
            GD.Print("MovementComponent: 相机已连接，将使用相机相对移动 ✓");
        }
    }
    
    #endregion

    #region Public API (供外部调用)
    
    /// <summary>
    /// 更新移动方向（由 InputComponent 或 Controller 调用）
    /// </summary>
    /// <param name="inputDir">输入方向 (X: 左右, Y: 前后)</param>
    public void UpdateMovementDirection(Vector2 inputDir)
    {
        _currentInputDirection = inputDir;
    }
    
    /// <summary>
    /// 执行跳跃（由 InputComponent 或 Controller 调用）
    /// </summary>
    public void PerformJump()
    {
        _jumpRequested = true;
    }
    
    /// <summary>
    /// 物理帧处理（必须在 _PhysicsProcess 中调用）
    /// </summary>
    /// <param name="delta">物理帧时间间隔</param>
    public void ProcessPhysics(double delta)
    {
        if (Body == null)
        {
            return;
        }
        
        Vector3 velocity = Body.Velocity;
        
        // 1. 应用重力
        if (!Body.IsOnFloor())
        {
            velocity.Y -= Gravity * (float)delta;
        }
        
        // 2. 处理跳跃
        if (_jumpRequested && Body.IsOnFloor())
        {
            velocity.Y = JumpVelocity;
            _jumpRequested = false; // 消耗跳跃请求
            
            // TODO: 触发跳跃事件供动画/音效使用
            // OnJumped?.Invoke();
        }
        else if (_jumpRequested && !Body.IsOnFloor())
        {
            // 在空中时清除跳跃请求（防止落地瞬间跳跃）
            _jumpRequested = false;
        }
        
        // 3. 处理水平移动（基于相机方向）
        Vector3 direction;
        
        if (Camera != null)
        {
            // 使用相机方向计算移动
            Vector3 forward = Camera.GlobalTransform.Basis.Z;
            Vector3 right = Camera.GlobalTransform.Basis.X;
            forward.Y = 0;
            right.Y = 0;
            forward = forward.Normalized();
            right = right.Normalized();
            
            direction = (right * _currentInputDirection.X + forward * _currentInputDirection.Y).Normalized();
        }
        else
        {
            // 没有相机时使用角色本地坐标系
            direction = (Body.Transform.Basis * new Vector3(_currentInputDirection.X, 0, _currentInputDirection.Y)).Normalized();
        }
        
        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;
        }
        else
        {
            // 停止移动时快速减速
            velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(velocity.Z, 0, Speed);
        }
        
        // 4. 应用速度并移动
        Body.Velocity = velocity;
        Body.MoveAndSlide();
    }
    
    #endregion

    #region TODO: 高级移动功能
    
    // TODO: 土狼时间 (Coyote Time)
    // 离开地面后短时间内仍可跳跃
    // private double _timeSinceLeftGround = 0.0;
    // private const double COYOTE_TIME = 0.1;
    
    // TODO: 冲刺 (Dash)
    // 短时间内快速移动，可能带无敌帧
    // private bool _isDashing = false;
    // private double _dashTimeRemaining = 0.0;
    // public void StartDash(Vector3 direction) { }
    
    // TODO: 受击击退 (Knockback)
    // 受到攻击时被推开
    // public void ApplyKnockback(Vector3 force, float duration) { }
    
    // TODO: 移动事件
    // public event Action OnJumped;
    // public event Action OnLanded;
    // public event Action<float> OnSpeedChanged;
    
    #endregion
}
