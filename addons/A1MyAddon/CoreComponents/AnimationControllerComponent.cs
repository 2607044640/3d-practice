using Godot;
using Godot.Composition;

/// <summary>
/// 动画控制器组件 - 数值驱动的动画系统
/// 
/// 架构原则：
/// - 宏观状态由外部信号控制（StateChart 在编辑器中连接信号到公开方法）
/// - 微观状态由 Velocity 数值驱动（无 Input 依赖）
/// - 动画可用性在 _Ready 中一次性缓存（零每帧查询）
/// - 使用分组Export属性，方便编辑器折叠/展开
/// 
/// 使用方法：
/// 1. 在 Godot 编辑器中，将 StateChart 的 state_entered 信号连接到：
///    - GroundMode.state_entered → AnimationControllerComponent.EnterGroundMode()
///    - FlyMode.state_entered → AnimationControllerComponent.EnterFlyMode()
/// 2. 组件会根据 parent.Velocity 自动选择合适的动画
/// </summary>
[GlobalClass]
[Component(typeof(CharacterBody3D))]
public partial class AnimationControllerComponent : Node
{
    #region Export Properties - 基础配置
    
    [ExportGroup("基础配置")]
    [Export] public NodePath CharacterModelPath { get; set; } = "KunoSkin";
    
    [Export] public NodePath AnimationPlayerPath { get; set; } = "AnimationPlayer";
    
    [Export(PropertyHint.Range, "0.0,1.0,0.05")]
    public float AnimationBlendTime { get; set; } = 0.2f;
    
    [Export] public CharacterAnimationConfig AnimConfig { get; set; }
    
    #endregion

    #region Export Properties - 速度阈值
    
    [ExportGroup("速度阈值")]
    
    /// <summary>速度阈值：超过此值播放移动动画</summary>
    [Export(PropertyHint.Range, "0.0,2.0,0.1")]
    public float MoveThreshold { get; set; } = 0.1f;
    
    /// <summary>速度阈值：超过此值播放冲刺动画（地面模式）</summary>
    [Export(PropertyHint.Range, "0.0,20.0,0.5")]
    public float SprintThreshold { get; set; } = 6.0f;
    
    /// <summary>速度阈值：超过此值播放快速飞行动画（飞行模式）</summary>
    [Export(PropertyHint.Range, "0.0,20.0,0.5")]
    public float FlyFastThreshold { get; set; } = 10.0f;
    
    #endregion

    #region Export Properties - 地面动画
    
    [ExportGroup("地面动画")]
    
    [Export] public string GroundIdleAnimation { get; set; } = AnimationNames.Idle;
    [Export(PropertyHint.Range, "0.0,5.0,0.1")]
    public float GroundIdleSpeed { get; set; } = 1.0f;
    
    [Export] public string GroundMoveAnimation { get; set; } = AnimationNames.Run;
    [Export(PropertyHint.Range, "0.0,5.0,0.1")]
    public float GroundMoveSpeed { get; set; } = 1.0f;
    
    [Export] public string GroundSprintAnimation { get; set; } = AnimationNames.Sprint;
    [Export(PropertyHint.Range, "0.0,5.0,0.1")]
    public float GroundSprintSpeed { get; set; } = 1.0f;
    
    [Export] public string GroundJumpAnimation { get; set; } = AnimationNames.JumpStart;
    [Export(PropertyHint.Range, "0.0,5.0,0.1")]
    public float GroundJumpSpeed { get; set; } = 1.0f;
    
    #endregion

    #region Export Properties - 飞行动画
    
    [ExportGroup("飞行动画")]
    
    [Export] public string FlyIdleAnimation { get; set; } = AnimationNames.FlyIdle;
    [Export(PropertyHint.Range, "0.0,5.0,0.1")]
    public float FlyIdleSpeed { get; set; } = 1.0f;
    
    [Export] public string FlyMoveAnimation { get; set; } = AnimationNames.FlyMove;
    [Export(PropertyHint.Range, "0.0,5.0,0.1")]
    public float FlyMoveSpeed { get; set; } = 1.0f;
    
    [Export] public string FlyFastAnimation { get; set; } = AnimationNames.FlyFast;
    [Export(PropertyHint.Range, "0.0,5.0,0.1")]
    public float FlyFastSpeed { get; set; } = 1.0f;
    
    #endregion

    #region Private Fields
    
    private AnimationPlayer _animPlayer;
    private string _currentAnimation = "";
    private string _currentMode = "Ground";
    
    // 缓存动画可用性（避免每帧查询）
    private bool _hasGroundIdle = false;
    private bool _hasGroundMove = false;
    private bool _hasGroundSprint = false;
    private bool _hasGroundJump = false;
    private bool _hasFlyIdle = false;
    private bool _hasFlyMove = false;
    private bool _hasFlyFast = false;
    
    #endregion

    #region Godot Lifecycle
    
    public override void _Ready()
    {
        InitializeComponent();
        InitializeAnimation();
        CacheAvailableAnimations();
    }
    
    public override void _Process(double delta)
    {
        UpdateAnimation();
    }
    
    #endregion

    #region Public Methods (For StateChart Signal Connections)
    
    /// <summary>
    /// 进入地面模式
    /// 在 Godot 编辑器中连接：GroundMode.state_entered → 此方法
    /// </summary>
    public void EnterGroundMode()
    {
        _currentMode = "Ground";
        GD.Print("[AnimationController] 进入地面模式");
    }
    
    /// <summary>
    /// 进入飞行模式
    /// 在 Godot 编辑器中连接：FlyMode.state_entered → 此方法
    /// </summary>
    public void EnterFlyMode()
    {
        _currentMode = "Fly";
        GD.Print("[AnimationController] 进入飞行模式");
    }
    
    #endregion

    #region Initialization
    
    private void InitializeAnimation()
    {
        var characterModel = parent.GetNodeOrNull<Node3D>(CharacterModelPath);
        if (characterModel == null)
        {
            GD.PushWarning($"AnimationControllerComponent: 角色模型未找到: {CharacterModelPath}");
            return;
        }

        var animPlayerFullPath = CharacterModelPath + "/" + AnimationPlayerPath;
        _animPlayer = parent.GetNodeOrNull<AnimationPlayer>(animPlayerFullPath);
        
        if (_animPlayer == null)
        {
            GD.PushWarning($"AnimationControllerComponent: AnimationPlayer 未找到: {animPlayerFullPath}");
            return;
        }

        if (AnimConfig != null)
        {
            AnimConfig.ApplyToAnimationPlayer(_animPlayer);
        }
    }
    
    /// <summary>
    /// 缓存可用动画（避免每帧调用 HasAnimation）
    /// </summary>
    private void CacheAvailableAnimations()
    {
        if (_animPlayer == null) return;
        
        // 缓存地面动画
        _hasGroundIdle = !string.IsNullOrEmpty(GroundIdleAnimation) && 
                         _animPlayer.HasAnimation(GroundIdleAnimation);
        _hasGroundMove = !string.IsNullOrEmpty(GroundMoveAnimation) && 
                         _animPlayer.HasAnimation(GroundMoveAnimation);
        _hasGroundSprint = !string.IsNullOrEmpty(GroundSprintAnimation) && 
                           _animPlayer.HasAnimation(GroundSprintAnimation);
        _hasGroundJump = !string.IsNullOrEmpty(GroundJumpAnimation) && 
                         _animPlayer.HasAnimation(GroundJumpAnimation);
        
        // 缓存飞行动画
        _hasFlyIdle = !string.IsNullOrEmpty(FlyIdleAnimation) && 
                      _animPlayer.HasAnimation(FlyIdleAnimation);
        _hasFlyMove = !string.IsNullOrEmpty(FlyMoveAnimation) && 
                      _animPlayer.HasAnimation(FlyMoveAnimation);
        _hasFlyFast = !string.IsNullOrEmpty(FlyFastAnimation) && 
                      _animPlayer.HasAnimation(FlyFastAnimation);
        
        GD.Print($"[AnimationController] 缓存地面动画: Idle={_hasGroundIdle}, Move={_hasGroundMove}, Sprint={_hasGroundSprint}, Jump={_hasGroundJump}");
        GD.Print($"[AnimationController] 缓存飞行动画: Idle={_hasFlyIdle}, Move={_hasFlyMove}, Fast={_hasFlyFast}");
    }
    
    #endregion

    #region Animation Logic
    
    /// <summary>
    /// 更新动画（基于当前模式和物理状态）
    /// 数值驱动，无硬编码if/else
    /// </summary>
    private void UpdateAnimation()
    {
        if (_animPlayer == null) return;
        
        string targetAnim = "";
        float targetSpeed = 1.0f;
        
        if (_currentMode == "Ground")
        {
            (targetAnim, targetSpeed) = SelectGroundAnimation();
        }
        else if (_currentMode == "Fly")
        {
            (targetAnim, targetSpeed) = SelectFlyAnimation();
        }
        
        PlayAnimation(targetAnim, targetSpeed);
    }
    
    /// <summary>
    /// 选择地面模式动画（基于 Velocity，无 Input 依赖）
    /// 返回：(动画名称, 播放速度)
    /// </summary>
    private (string, float) SelectGroundAnimation()
    {
        Vector3 velocity = parent.Velocity;
        float horizontalSpeed = new Vector2(velocity.X, velocity.Z).Length();
        
        // 优先级：空中 > 冲刺 > 移动 > 静止
        if (!parent.IsOnFloor() && _hasGroundJump)
        {
            return (GroundJumpAnimation, GroundJumpSpeed);
        }
        
        if (horizontalSpeed > SprintThreshold && _hasGroundSprint)
        {
            return (GroundSprintAnimation, GroundSprintSpeed);
        }
        
        if (horizontalSpeed > MoveThreshold && _hasGroundMove)
        {
            return (GroundMoveAnimation, GroundMoveSpeed);
        }
        
        if (_hasGroundIdle)
        {
            return (GroundIdleAnimation, GroundIdleSpeed);
        }
        
        return ("", 1.0f);
    }
    
    /// <summary>
    /// 选择飞行模式动画（基于 Velocity）
    /// 返回：(动画名称, 播放速度)
    /// </summary>
    private (string, float) SelectFlyAnimation()
    {
        Vector3 velocity = parent.Velocity;
        float speed = velocity.Length();
        
        // 优先级：快速飞行 > 移动 > 静止
        if (speed > FlyFastThreshold && _hasFlyFast)
        {
            return (FlyFastAnimation, FlyFastSpeed);
        }
        
        if (speed > MoveThreshold && _hasFlyMove)
        {
            return (FlyMoveAnimation, FlyMoveSpeed);
        }
        
        if (_hasFlyIdle)
        {
            return (FlyIdleAnimation, FlyIdleSpeed);
        }
        
        // 兜底：如果没有飞行动画，使用地面Idle
        if (_hasGroundIdle)
        {
            return (GroundIdleAnimation, GroundIdleSpeed);
        }
        
        return ("", 1.0f);
    }
    
    /// <summary>
    /// 播放动画（带过渡和速度控制）
    /// </summary>
    private void PlayAnimation(string targetAnim, float targetSpeed)
    {
        if (string.IsNullOrEmpty(targetAnim) || _currentAnimation == targetAnim)
            return;
        
        _animPlayer.Play(targetAnim, customBlend: AnimationBlendTime, customSpeed: targetSpeed);
        _currentAnimation = targetAnim;
    }
    
    #endregion
}
