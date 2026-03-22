using Godot;
using Godot.Composition;

/// <summary>
/// 飞行移动组件 - 三维全向移动，无重力
/// 绑定到 StateChart 的 "FlyMode" 状态
/// 只有在飞行模式下才会被激活（Power Switch 模式）
/// 
/// 注意：此组件作为 FlyMode 状态节点的子节点，使用 AutoBindToParentState() 自动绑定
/// </summary>
[GlobalClass]
public partial class FlyMovementComponent : Node
{
    #region Export Properties

    [Export] public float FlySpeed { get; set; } = 8.0f;
    [Export] public float FlyAcceleration { get; set; } = 20.0f;
    [Export] public float FlyDeceleration { get; set; } = 15.0f;
    [Export] public NodePath PhantomCameraPath { get; set; } = "PhantomCamera3D";

    #endregion

    #region Private State

    private Vector2 _currentInputDirection = Vector2.Zero;
    private bool _ascendPressed = false;  // 上升（空格）
    private bool _descendPressed = false; // 下降（Ctrl）
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
            GD.PushError("FlyMovementComponent: 无法找到 CharacterBody3D 实体！");
            return;
        }

        // 注意：不调用 InitializeComponent()，因为父节点不是实体
        // InitializeComponent();
        
        _phantomCamera = _entity.GetNodeOrNull<Node3D>(PhantomCameraPath);
        
        if (_phantomCamera == null)
        {
            GD.PushWarning("FlyMovementComponent: PhantomCamera not found");
        }
        
        // 订阅输入事件（直接在这里订阅，不等待OnEntityReady）
        _inputComponent = _entity.FindAndSubscribeInput(
            HandleMovementInput,
            null // 飞行模式下跳跃键用于上升，不需要单独的跳跃处理
        );
        
        // 【Power Switch】自动绑定到父状态节点
        this.AutoBindToParentState();
        
        GD.Print("FlyMovementComponent: 已完成初始化");
    }

    // 不再需要 OnEntityReady，因为不使用 Composition 框架
    // public void OnEntityReady() { }

    public override void _Process(double delta)
    {
        // 读取上升/下降输入（飞行模式特有）
        _ascendPressed = Input.IsActionPressed("jump");      // 空格上升
        _descendPressed = Input.IsActionPressed("crouch");   // Ctrl下降
    }

    public override void _PhysicsProcess(double delta)
    {
        // 【纯粹的飞行物理】无需状态判断
        // 因为组件默认休眠，只有 StateChart 激活 FlyMode 时才会执行
        ProcessFlyPhysics(delta);
    }

    public override void _ExitTree()
    {
        _inputComponent?.UnsubscribeInput(HandleMovementInput, null);
    }

    #endregion

    #region Event Handlers

    private void HandleMovementInput(Vector2 inputDir)
    {
        _currentInputDirection = inputDir;
    }

    #endregion

    #region Physics Logic

    private void ProcessFlyPhysics(double delta)
    {
        Vector3 velocity = _entity.Velocity;
        Vector3 targetVelocity = CalculateFlyTargetVelocity();

        // 平滑加速/减速
        float acceleration = targetVelocity.Length() > 0.1f ? FlyAcceleration : FlyDeceleration;
        velocity = velocity.Lerp(targetVelocity, acceleration * (float)delta);

        // 应用速度并移动
        _entity.Velocity = velocity;
        _entity.MoveAndSlide();
    }

    private Vector3 CalculateFlyTargetVelocity()
    {
        Vector3 targetVelocity = Vector3.Zero;

        if (_phantomCamera != null)
        {
            // 根据相机方向计算三维移动
            Vector3 forward = -_phantomCamera.GlobalTransform.Basis.Z; // 相机前方
            Vector3 right = _phantomCamera.GlobalTransform.Basis.X;    // 相机右方
            Vector3 up = Vector3.Up;                                    // 世界上方

            // 水平移动（WASD）
            Vector3 horizontalMove = Vector3.Zero;
            if (_currentInputDirection != Vector2.Zero)
            {
                // 只取相机的水平分量
                Vector3 forwardFlat = forward;
                forwardFlat.Y = 0;
                forwardFlat = forwardFlat.Normalized();
                
                Vector3 rightFlat = right;
                rightFlat.Y = 0;
                rightFlat = rightFlat.Normalized();

                // 修复：inputDir.Y 是负值表示向前（W键），所以需要取反
                horizontalMove = (rightFlat * _currentInputDirection.X - 
                                 forwardFlat * _currentInputDirection.Y).Normalized();
            }

            // 垂直移动（空格/Ctrl）
            float verticalMove = 0;
            if (_ascendPressed)
                verticalMove += 1.0f;
            if (_descendPressed)
                verticalMove -= 1.0f;

            // 合成目标速度
            targetVelocity = horizontalMove * FlySpeed + up * verticalMove * FlySpeed;
        }
        else
        {
            // 无相机时使用角色本地坐标系
            Vector3 horizontalMove = new Vector3(_currentInputDirection.X, 0, -_currentInputDirection.Y);
            float verticalMove = 0;
            if (_ascendPressed) verticalMove += 1.0f;
            if (_descendPressed) verticalMove -= 1.0f;

            targetVelocity = (_entity.Transform.Basis * horizontalMove).Normalized() * FlySpeed +
                            Vector3.Up * verticalMove * FlySpeed;
        }

        return targetVelocity;
    }

    #endregion
}
