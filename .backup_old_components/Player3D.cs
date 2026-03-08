using Godot;

/// <summary>
/// Player3D - 使用组件化架构的玩家控制器
/// 职责：
/// 1. 作为 Mediator 协调各个组件
/// 2. 处理相机控制
/// 3. 处理角色朝向
/// </summary>
public partial class Player3D : CharacterBody3D
{
    #region Export Properties - 组件引用
    
    /// <summary>
    /// 输入组件（会自动查找子节点）
    /// </summary>
    [Export] public PlayerInputComponent InputComponent { get; set; }
    
    /// <summary>
    /// 移动组件（会自动查找子节点）
    /// </summary>
    [Export] public MovementComponent MovementComponent { get; set; }
    
    #endregion
    
    #region Export Properties - 相机设置
    
    [Export] public float MouseSensitivity { get; set; } = 0.002f;
    
    #endregion
    
    #region Export Properties - 角色模型
    
    /// <summary>
    /// 角色模型节点路径（用于旋转朝向）
    /// </summary>
    [Export] public NodePath CharacterModelPath { get; set; } = "KunoSkin";
    
    #endregion

    #region Private Fields
    
    // 相机引用
    private Node3D _cameraPivot;
    private Camera3D _camera;
    private SpringArm3D _springArm;
    
    // 角色模型引用
    private Node3D _characterModel;
    
    // 移动状态（用于角色旋转）
    private Vector2 _currentInputDir = Vector2.Zero;
    
    #endregion

    #region Godot Lifecycle
    
    public override void _Ready()
    {
        // 1. 自动查找组件
        InitializeComponents();
        
        // 2. 初始化相机
        InitializeCamera();
        
        // 3. 初始化角色模型引用
        InitializeCharacterModel();
        
        // 4. 订阅组件事件
        SubscribeToComponentEvents();
        
        // 5. 捕获鼠标
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }
    
    public override void _ExitTree()
    {
        // 取消订阅事件
        UnsubscribeFromComponentEvents();
    }
    
    public override void _PhysicsProcess(double delta)
    {
        // 调用移动组件的物理处理
        MovementComponent?.ProcessPhysics(delta);
        
        // 更新角色朝向
        UpdateCharacterRotation(delta);
    }
    
    public override void _UnhandledInput(InputEvent @event)
    {
        // 处理相机旋转
        HandleCameraInput(@event);
        
        // ESC 释放鼠标
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }
    
    #endregion

    #region Initialization
    
    /// <summary>
    /// 初始化组件引用（自动查找子节点）
    /// </summary>
    private void InitializeComponents()
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
        
        // 验证组件
        if (InputComponent == null)
        {
            GD.PushError("Player3D: PlayerInputComponent 未找到！");
        }
        else
        {
            GD.Print("Player3D: InputComponent 已连接 ✓");
        }
        
        if (MovementComponent == null)
        {
            GD.PushError("Player3D: MovementComponent 未找到！");
        }
        else
        {
            GD.Print("Player3D: MovementComponent 已连接 ✓");
        }
    }
    
    /// <summary>
    /// 初始化相机引用
    /// </summary>
    private void InitializeCamera()
    {
        _cameraPivot = GetNodeOrNull<Node3D>("CameraPivot");
        _camera = GetNodeOrNull<Camera3D>("CameraPivot/SpringArm3D/Camera3D");
        _springArm = GetNodeOrNull<SpringArm3D>("CameraPivot/SpringArm3D");
        
        if (_cameraPivot == null || _camera == null || _springArm == null)
        {
            GD.PushWarning("Player3D: 相机节点未找到，相机控制将不可用。");
        }
        else
        {
            GD.Print("Player3D: 相机系统已初始化 ✓");
        }
    }
    
    /// <summary>
    /// 初始化角色模型引用
    /// </summary>
    private void InitializeCharacterModel()
    {
        _characterModel = GetNodeOrNull<Node3D>(CharacterModelPath);
        if (_characterModel == null)
        {
            GD.PushWarning($"Player3D: 角色模型未找到: {CharacterModelPath}");
        }
        else
        {
            GD.Print("Player3D: 角色模型已连接 ✓");
        }
    }
    
    #endregion

    #region Component Event Handling
    
    /// <summary>
    /// 订阅组件事件
    /// </summary>
    private void SubscribeToComponentEvents()
    {
        if (InputComponent != null)
        {
            InputComponent.OnMovementInput += HandleMovementInput;
            InputComponent.OnJumpJustPressed += HandleJumpInput;
            GD.Print("Player3D: 已订阅 InputComponent 事件 ✓");
        }
    }
    
    /// <summary>
    /// 取消订阅组件事件
    /// </summary>
    private void UnsubscribeFromComponentEvents()
    {
        if (InputComponent != null)
        {
            InputComponent.OnMovementInput -= HandleMovementInput;
            InputComponent.OnJumpJustPressed -= HandleJumpInput;
        }
    }
    
    /// <summary>
    /// 处理移动输入（从 InputComponent 接收）
    /// </summary>
    private void HandleMovementInput(Vector2 inputDir)
    {
        _currentInputDir = inputDir;
        
        // 将输入传递给移动组件
        MovementComponent?.UpdateMovementDirection(inputDir);
    }
    
    /// <summary>
    /// 处理跳跃输入（从 InputComponent 接收）
    /// </summary>
    private void HandleJumpInput()
    {
        MovementComponent?.PerformJump();
    }
    
    #endregion

    #region Character Rotation
    
    /// <summary>
    /// 更新角色朝向（面向移动方向）
    /// </summary>
    private void UpdateCharacterRotation(double delta)
    {
        if (_characterModel == null || _camera == null) return;
        if (_currentInputDir == Vector2.Zero) return;
        
        // 基于相机方向计算移动方向
        Vector3 forward = _camera.GlobalTransform.Basis.Z;
        Vector3 right = _camera.GlobalTransform.Basis.X;
        forward.Y = 0;
        right.Y = 0;
        forward = forward.Normalized();
        right = right.Normalized();
        
        Vector3 direction = (right * _currentInputDir.X + forward * _currentInputDir.Y).Normalized();
        
        if (direction != Vector3.Zero)
        {
            // 平滑旋转角色面向移动方向
            float targetAngle = Mathf.Atan2(direction.X, direction.Z);
            Quaternion targetRotation = new(Vector3.Up, targetAngle);
            _characterModel.Quaternion = _characterModel.Quaternion.Slerp(targetRotation, (float)delta * 10.0f);
        }
    }
    
    #endregion

    #region Camera Control
    
    /// <summary>
    /// 处理相机输入
    /// </summary>
    private void HandleCameraInput(InputEvent @event)
    {
        if (_cameraPivot == null || _springArm == null) return;
        
        if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            // 旋转相机枢轴（左右）
            _cameraPivot.RotateY(-mouseMotion.Relative.X * MouseSensitivity);
            
            // 旋转SpringArm（上下）
            _springArm.RotateX(mouseMotion.Relative.Y * MouseSensitivity);

            // 限制上下角度
            Vector3 springArmRotation = _springArm.Rotation;
            springArmRotation.X = Mathf.Clamp(springArmRotation.X, -Mathf.Pi / 3, Mathf.Pi / 8);
            _springArm.Rotation = springArmRotation;
        }
    }
    
    #endregion
}