using Godot;
using Godot.Composition;

/// <summary>
/// 移动组件 - 仅负责物理计算与位移
/// 遵循单一职责原则：只处理移动，不处理输入或动画
/// 依赖抽象的 BaseInputComponent，可复用于玩家和 AI
/// </summary>
[GlobalClass]
[Component(typeof(CharacterBody3D))]
public partial class MovementComponent : Node
{
    #region Export Properties

    /// <summary>
    /// PhantomCamera3D 引用（用于计算相对于相机的移动方向）
    /// </summary>
    [Export]
    public NodePath PhantomCameraPath { get; set; } = "PhantomCamera3D";

    /// <summary>
    /// 移动速度 (米/秒)
    /// </summary>
    [Export]
    public float Speed { get; set; } = 5.0f;

    /// <summary>
    /// 跳跃初速度 (米/秒)
    /// </summary>
    [Export]
    public float JumpVelocity { get; set; } = 4.5f;

    /// <summary>
    /// 重力加速度
    /// </summary>
    [Export]
    public float Gravity { get; set; } = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");

    #endregion

    #region Private State

    // 当前移动方向
    private Vector2 _currentInputDirection = Vector2.Zero;

    // 是否请求跳跃
    private bool _jumpRequested = false;

    // 输入组件引用（手动查找）
    private BaseInputComponent _inputComponent;

    // PhantomCamera3D 引用
    private Node3D _phantomCamera;

    #endregion

    #region Godot Lifecycle

    public override void _Ready()
    {
        InitializeComponent();

        // 获取 PhantomCamera3D 引用
        _phantomCamera = parent.GetNodeOrNull<Node3D>(PhantomCameraPath);

        GD.Print($"MovementComponent: 已连接到 Body: {parent.Name}");
        GD.Print($"MovementComponent: Speed={Speed}, JumpVelocity={JumpVelocity}, Gravity={Gravity}");

        if (_phantomCamera == null)
        {
            GD.PushWarning("MovementComponent: 未找到 PhantomCamera3D，将使用角色本地坐标系移动。");
        }
        else
        {
            GD.Print("MovementComponent: PhantomCamera3D 已连接 ✓");
        }
    }

    /// <summary>
    /// Entity 初始化完成后自动调用
    /// 在这里订阅 InputComponent 的事件
    /// </summary>
    public void OnEntityReady()
    {
        // 使用扩展方法：一行代码搞定查找和订阅！
        _inputComponent = parent.FindAndSubscribeInput(
            HandleMovementInput,
            HandleJumpInput
        );
    }

    public override void _PhysicsProcess(double delta)
    {
        ProcessPhysics(delta);
    }

    public override void _ExitTree()
    {
        // 使用扩展方法取消订阅
        _inputComponent?.UnsubscribeInput(HandleMovementInput, HandleJumpInput);
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// 处理移动输入
    /// </summary>
    private void HandleMovementInput(Vector2 inputDir)
    {
        _currentInputDirection = inputDir;
    }

    /// <summary>
    /// 处理跳跃输入
    /// </summary>
    private void HandleJumpInput()
    {
        _jumpRequested = true;
    }

    #endregion

    #region Physics Logic

    /// <summary>
    /// 物理帧处理
    /// </summary>
    private void ProcessPhysics(double delta)
    {
        Vector3 velocity = parent.Velocity;

        // 1. 应用重力
        if (!parent.IsOnFloor())
        {
            velocity.Y -= Gravity * (float)delta;
        }

        // 2. 处理跳跃
        if (_jumpRequested && parent.IsOnFloor())
        {
            velocity.Y = JumpVelocity;
            _jumpRequested = false;
        }
        else if (_jumpRequested && !parent.IsOnFloor())
        {
            _jumpRequested = false;
        }

        // 3. 处理水平移动
        Vector3 direction;

        if (_phantomCamera != null)
        {
            // 使用 PhantomCamera 的全局变换计算移动方向
            // 注意：Godot 中 -Z 是前方，X 是右方
            // 修复：Input.GetVector 的 Y 轴是反的（forward 是负值，backward 是正值）
            Vector3 forward = _phantomCamera.GlobalTransform.Basis.Z; // 使用 Z 而不是 -Z
            Vector3 right = _phantomCamera.GlobalTransform.Basis.X;
            forward.Y = 0;
            right.Y = 0;
            forward = forward.Normalized();
            right = right.Normalized();

            // 调试输出
            if (_currentInputDirection != Vector2.Zero)
            {
                //GD.Print($"[MovementComponent] Input: {_currentInputDirection}, Forward: {forward}, " +$"Right: {right}");
            }

            direction = (right * _currentInputDirection.X + forward * _currentInputDirection.Y).Normalized();

            if (_currentInputDirection != Vector2.Zero)
            {
                //GD.Print($"[MovementComponent] Final Direction: {direction}");
            }
        }
        else
        {
            // 没有相机时使用角色本地坐标系
            direction = (parent.Transform.Basis * new Vector3(_currentInputDirection.X, 0, _currentInputDirection.Y))
                .Normalized();
        }

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
        parent.Velocity = velocity;
        parent.MoveAndSlide();
    }

    #endregion
}