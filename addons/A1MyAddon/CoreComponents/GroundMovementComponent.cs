using Godot;
using Godot.Composition;

/// <summary>
/// 地面移动组件 - 处理重力、跳跃、地面移动
/// 绑定到 StateChart 的 "GroundMode" 状态
/// 只有在地面模式下才会被激活（Power Switch 模式）
/// 
/// 注意：此组件作为 GroundMode 状态节点的子节点，使用 AutoBindToParentState() 自动绑定
/// </summary>
[GlobalClass]
public partial class GroundMovementComponent : Node
{
    #region Export Properties

    [Export] public float Speed { get; set; } = 5.0f;
    [Export] public float JumpVelocity { get; set; } = 4.5f;
    [Export] public float Gravity { get; set; } = 9.8f;
    [Export] public NodePath PhantomCameraPath { get; set; } = "PhantomCamera3D";

    #endregion

    #region Private State

    private Vector2 _currentInputDirection = Vector2.Zero;
    private bool _jumpRequested = false;
    private BaseInputComponent _inputComponent;
    private Node3D _phantomCamera;
    private CharacterBody3D _entity; // 真实的实体引用

    #endregion

    #region Godot Lifecycle

    public override void _Ready()
    {
        // 获取真实的实体（因为现在组件是State节点的子节点）
        _entity = this.GetEntity<CharacterBody3D>();
        if (_entity == null)
        {
            GD.PushError("GroundMovementComponent: 无法找到 CharacterBody3D 实体！");
            return;
        }

        // 注意：不调用 InitializeComponent()，因为父节点不是实体
        // InitializeComponent();
        
        _phantomCamera = _entity.GetNodeOrNull<Node3D>(PhantomCameraPath);
        
        if (_phantomCamera == null)
        {
            GD.PushWarning("GroundMovementComponent: PhantomCamera not found");
        }
        
        // 订阅输入事件（直接在这里订阅，不等待OnEntityReady）
        _inputComponent = _entity.FindAndSubscribeInput(
            HandleMovementInput,
            HandleJumpInput
        );
        
        // 【Power Switch】自动绑定到父状态节点
        this.AutoBindToParentState();
        
        GD.Print("GroundMovementComponent: 已完成初始化");
    }

    // 不再需要 OnEntityReady，因为不使用 Composition 框架
    // public void OnEntityReady() { }

    public override void _PhysicsProcess(double delta)
    {
        // 【纯粹的物理计算】无需状态判断
        // 因为组件默认休眠，只有 StateChart 激活 GroundMode 时才会执行
        ProcessGroundPhysics(delta);
    }

    public override void _ExitTree()
    {
        _inputComponent?.UnsubscribeInput(HandleMovementInput, HandleJumpInput);
    }

    #endregion

    #region Event Handlers

    private void HandleMovementInput(Vector2 inputDir)
    {
        _currentInputDirection = inputDir;
    }

    private void HandleJumpInput()
    {
        _jumpRequested = true;
    }

    #endregion

    #region Physics Logic

    private void ProcessGroundPhysics(double delta)
    {
        Vector3 velocity = _entity.Velocity;

        // 1. 应用重力
        if (!_entity.IsOnFloor())
        {
            velocity.Y -= Gravity * (float)delta;
        }

        // 2. 处理跳跃
        if (_jumpRequested && _entity.IsOnFloor())
        {
            velocity.Y = JumpVelocity;
            _jumpRequested = false;
            
            // 【事件驱动】通知 StateChart 跳跃发生
            // 可用于触发跳跃动画或其他状态变化
            _entity.SendStateEvent("jumped");
        }
        else if (_jumpRequested)
        {
            _jumpRequested = false;
        }

        // 3. 处理水平移动
        Vector3 direction = CalculateMovementDirection();

        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(velocity.Z, 0, Speed);
        }

        // 4. 应用速度并移动
        _entity.Velocity = velocity;
        _entity.MoveAndSlide();
    }

    private Vector3 CalculateMovementDirection()
    {
        if (_phantomCamera != null)
        {
            // 使用相机方向计算移动
            Vector3 forward = _phantomCamera.GlobalTransform.Basis.Z;
            Vector3 right = _phantomCamera.GlobalTransform.Basis.X;
            forward.Y = 0;
            right.Y = 0;
            forward = forward.Normalized();
            right = right.Normalized();

            return (right * _currentInputDirection.X + forward * _currentInputDirection.Y).Normalized();
        }
        else
        {
            // 使用角色本地坐标系
            return (_entity.Transform.Basis * new Vector3(_currentInputDirection.X, 0, _currentInputDirection.Y))
                .Normalized();
        }
    }

    #endregion
}
