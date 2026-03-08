using Godot;
using Godot.Composition;

/// <summary>
/// 相机控制组件 - 负责处理相机旋转和鼠标输入
/// </summary>
[GlobalClass]
[Component(typeof(CharacterBody3D))]
public partial class CameraControlComponent : Node
{
    #region Export Properties
    
    /// <summary>
    /// 鼠标灵敏度
    /// </summary>
    [Export] public float MouseSensitivity { get; set; } = 0.002f;
    
    /// <summary>
    /// 相机枢轴节点路径
    /// </summary>
    [Export] public NodePath CameraPivotPath { get; set; } = "CameraPivot";
    
    /// <summary>
    /// SpringArm 节点路径（相对于 CameraPivot）
    /// </summary>
    [Export] public NodePath SpringArmPath { get; set; } = "CameraPivot/SpringArm3D";
    
    /// <summary>
    /// 上下视角限制（最小角度，弧度）
    /// </summary>
    [Export] public float MinPitch { get; set; } = -Mathf.Pi / 3; // -60度
    
    /// <summary>
    /// 上下视角限制（最大角度，弧度）
    /// </summary>
    [Export] public float MaxPitch { get; set; } = Mathf.Pi / 8; // 22.5度
    
    #endregion

    #region Private Fields
    
    private Node3D _cameraPivot;
    private SpringArm3D _springArm;
    
    #endregion

    #region Godot Lifecycle
    
    public override void _Ready()
    {
        InitializeComponent();
        
        // 初始化相机引用
        _cameraPivot = parent.GetNodeOrNull<Node3D>(CameraPivotPath);
        _springArm = parent.GetNodeOrNull<SpringArm3D>(SpringArmPath);
        
        if (_cameraPivot == null || _springArm == null)
        {
            GD.PushWarning("CameraControlComponent: 相机节点未找到，相机控制将不可用。");
        }
        else
        {
            GD.Print("CameraControlComponent: 相机系统已初始化 ✓");
        }
        
        // 捕获鼠标
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }
    
    public override void _UnhandledInput(InputEvent @event)
    {
        HandleCameraInput(@event);
    }
    
    #endregion

    #region Camera Control
    
    /// <summary>
    /// 处理相机输入
    /// </summary>
    private void HandleCameraInput(InputEvent @event)
    {
        if (_cameraPivot == null || _springArm == null) return;
        
        // 处理鼠标移动
        if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            // 旋转相机枢轴（左右）
            _cameraPivot.RotateY(-mouseMotion.Relative.X * MouseSensitivity);
            
            // 旋转SpringArm（上下）
            _springArm.RotateX(mouseMotion.Relative.Y * MouseSensitivity);

            // 限制上下角度
            Vector3 springArmRotation = _springArm.Rotation;
            springArmRotation.X = Mathf.Clamp(springArmRotation.X, MinPitch, MaxPitch);
            _springArm.Rotation = springArmRotation;
        }
        
        // ESC 释放鼠标
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }
    
    #endregion
}
