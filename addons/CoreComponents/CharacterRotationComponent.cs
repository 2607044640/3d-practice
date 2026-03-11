using Godot;
using Godot.Composition;

/// <summary>
/// 角色旋转组件 - 负责让角色模型面向移动方向
/// 依赖抽象的 BaseInputComponent，可复用于玩家和 AI
/// </summary>
[GlobalClass]
[Component(typeof(CharacterBody3D))]
public partial class CharacterRotationComponent : Node
{
    #region Export Properties
    
    /// <summary>
    /// 角色模型节点路径
    /// </summary>
    [Export] public NodePath CharacterModelPath { get; set; } = "KunoSkin";
    
    /// <summary>
    /// 相机引用（用于计算移动方向）
    /// </summary>
    [Export] public Camera3D Camera { get; set; }
    
    /// <summary>
    /// 旋转平滑速度
    /// </summary>
    [Export] public float RotationSpeed { get; set; } = 10.0f;
    
    #endregion

    #region Private Fields
    
    private Node3D _characterModel;
    private Vector2 _currentInputDir = Vector2.Zero;
    private BaseInputComponent _inputComponent;
    
    #endregion

    #region Godot Lifecycle
    
    public override void _Ready()
    {
        InitializeComponent();
        
        // 初始化角色模型引用
        _characterModel = parent.GetNodeOrNull<Node3D>(CharacterModelPath);
        if (_characterModel == null)
        {
            GD.PushWarning($"CharacterRotationComponent: 角色模型未找到: {CharacterModelPath}");
        }
        else
        {
            GD.Print("CharacterRotationComponent: 角色模型已连接 ✓");
        }
        
        // 自动查找相机
        if (Camera == null)
        {
            Camera = parent.GetNodeOrNull<Camera3D>("CameraPivot/SpringArm3D/Camera3D");
        }
        
        if (Camera == null)
        {
            GD.PushWarning("CharacterRotationComponent: 未找到相机，旋转功能将不可用。");
        }
        else
        {
            GD.Print("CharacterRotationComponent: 相机已连接 ✓");
        }
    }
    
    /// <summary>
    /// Entity 初始化完成后自动调用
    /// </summary>
    public void OnEntityReady()
    {
        // 使用扩展方法：一行代码搞定！
        _inputComponent = parent.FindAndSubscribeInput(HandleMovementInput);
    }
    
    public override void _Process(double delta)
    {
        UpdateCharacterRotation(delta);
    }
    
    public override void _ExitTree()
    {
        // 使用扩展方法取消订阅
        _inputComponent?.UnsubscribeInput(HandleMovementInput);
    }
    
    #endregion

    #region Event Handlers
    
    /// <summary>
    /// 处理移动输入
    /// </summary>
    private void HandleMovementInput(Vector2 inputDir)
    {
        _currentInputDir = inputDir;
    }
    
    #endregion

    #region Rotation Logic
    
    /// <summary>
    /// 更新角色朝向（面向移动方向）
    /// </summary>
    private void UpdateCharacterRotation(double delta)
    {
        if (_characterModel == null || Camera == null) return;
        if (_currentInputDir == Vector2.Zero) return;
        
        // 基于相机方向计算移动方向
        Vector3 forward = Camera.GlobalTransform.Basis.Z;
        Vector3 right = Camera.GlobalTransform.Basis.X;
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
            _characterModel.Quaternion = _characterModel.Quaternion.Slerp(targetRotation, (float)delta * RotationSpeed);
        }
    }
    
    #endregion
}
