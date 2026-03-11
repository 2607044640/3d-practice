using Godot;
using Godot.Composition;
using PhantomCamera;

/// <summary>
/// 相机控制组件 - 负责处理相机旋转和鼠标输入（使用 PhantomCamera）
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
    /// PhantomCamera3D 节点路径
    /// </summary>
    [Export] public NodePath PCamPath { get; set; } = "PhantomCamera3D";
    
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
    
    private PhantomCamera3D _pCam;
    
    #endregion

    #region Godot Lifecycle
    
    public override void _Ready()
    {
        InitializeComponent();
        
        // 初始化 PhantomCamera3D 引用
        var pCamNode = parent.GetNodeOrNull<Node3D>(PCamPath);
        
        if (pCamNode == null)
        {
            GD.PushError("CameraControlComponent: PhantomCamera3D 节点未找到，相机控制将不可用。");
            return;
        }
        
        // 使用扩展方法转换为 PhantomCamera3D 包装类
        _pCam = pCamNode.AsPhantomCamera3D();
        
        if (_pCam == null)
        {
            GD.PushError("CameraControlComponent: 无法将节点转换为 PhantomCamera3D。");
            return;
        }
        
        GD.Print("CameraControlComponent: PhantomCamera 系统已初始化 ✓");
        
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
        if (_pCam == null) return;
        
        // 处理鼠标移动
        if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            // 获取当前第三人称旋转（欧拉角，弧度）
            Vector3 currentRotation = _pCam.GetThirdPersonRotation();
            
            // 计算新的 Yaw (Y轴，左右) 和 Pitch (X轴，上下)
            float newYaw = currentRotation.Y - mouseMotion.Relative.X * MouseSensitivity;
            float newPitch = currentRotation.X + mouseMotion.Relative.Y * MouseSensitivity;
            
            // 限制 Pitch 角度
            newPitch = Mathf.Clamp(newPitch, MinPitch, MaxPitch);
            
            // 设置新的旋转
            Vector3 newRotation = new Vector3(newPitch, newYaw, currentRotation.Z);
            _pCam.SetThirdPersonRotation(newRotation);
        }
        
        // ESC 释放鼠标
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }
    
    #endregion
}
