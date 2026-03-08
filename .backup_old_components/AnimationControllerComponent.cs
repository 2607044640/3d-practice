using Godot;

/// <summary>
/// 动画控制器组件 - 仅负责动画播放逻辑
/// 遵循单一职责原则：只处理动画，不处理输入或移动
/// </summary>
[GlobalClass]
public partial class AnimationControllerComponent : Node
{
    #region Export Properties (依赖注入)
    
    /// <summary>
    /// 需要读取状态的物理身体（会自动获取父节点）
    /// </summary>
    [Export] public CharacterBody3D Body { get; set; }
    
    /// <summary>
    /// 需要读取移动状态的移动组件（可选）
    /// </summary>
    [Export] public MovementComponent Movement { get; set; }
    
    /// <summary>
    /// 角色模型节点路径
    /// </summary>
    [Export] public NodePath CharacterModelPath { get; set; } = "KunoSkin";
    
    /// <summary>
    /// AnimationPlayer路径（相对于角色模型）
    /// </summary>
    [Export] public NodePath AnimationPlayerPath { get; set; } = "AnimationPlayer";
    
    /// <summary>
    /// 动画过渡时间（秒）
    /// </summary>
    [Export(PropertyHint.Range, "0.0,1.0,0.05")]
    public float AnimationBlendTime { get; set; } = 0.2f;
    
    /// <summary>
    /// 动画配置（类似UE的Data Asset）
    /// </summary>
    [Export] public CharacterAnimationConfig AnimConfig { get; set; }
    
    #endregion

    #region Private Fields
    
    private AnimationPlayer _animPlayer;
    private string _currentAnimation = "";
    private AnimationSet _animSet; // 缓存 AnimationSet 引用
    
    #endregion

    #region Godot Lifecycle
    
    public override void _Ready()
    {
        // 自动获取 Body 引用
        if (Body == null)
        {
            Body = GetParent<CharacterBody3D>();
        }
        
        if (Body == null)
        {
            GD.PushError("AnimationControllerComponent: 无法找到 CharacterBody3D！");
            return;
        }
        
        // 自动获取 Movement 组件（如果存在）
        if (Movement == null)
        {
            Movement = Body.GetNodeOrNull<MovementComponent>("MovementComponent");
        }
        
        // 初始化动画系统
        InitializeAnimation();
    }
    
    public override void _Process(double delta)
    {
        // 更新动画状态
        UpdateAnimation();
    }
    
    #endregion

    #region Initialization
    
    /// <summary>
    /// 初始化动画系统
    /// </summary>
    private void InitializeAnimation()
    {
        // 获取角色模型节点
        var characterModel = Body.GetNodeOrNull<Node3D>(CharacterModelPath);
        if (characterModel == null)
        {
            GD.PushWarning($"AnimationControllerComponent: 角色模型未找到: {CharacterModelPath}");
            return;
        }

        // 获取AnimationPlayer
        var animPlayerFullPath = CharacterModelPath + "/" + AnimationPlayerPath;
        _animPlayer = Body.GetNodeOrNull<AnimationPlayer>(animPlayerFullPath);
        
        if (_animPlayer == null)
        {
            GD.PushWarning($"AnimationControllerComponent: AnimationPlayer 未找到: {animPlayerFullPath}");
            return;
        }

        // 应用动画配置
        if (AnimConfig != null)
        {
            AnimConfig.ApplyToAnimationPlayer(_animPlayer);
            _animSet = AnimConfig.AnimationSet; // 缓存 AnimationSet 引用
            GD.Print($"AnimationControllerComponent: AnimationConfig 已应用。可用动画: {string.Join(", ", _animPlayer.GetAnimationList())}");
        }
        else
        {
            GD.PushWarning("AnimationControllerComponent: 未设置 AnimConfig！请在 Inspector 中指定 CharacterAnimationConfig 资源。");
            GD.Print($"AnimationControllerComponent: 使用现有动画: {string.Join(", ", _animPlayer.GetAnimationList())}");
        }
    }
    
    #endregion

    #region Animation Logic
    
    /// <summary>
    /// 更新动画状态（根据物理状态自动选择动画）
    /// </summary>
    private void UpdateAnimation()
    {
        if (_animPlayer == null || Body == null) return;
        
        string targetAnim = "";
        
        // 获取当前速度和输入状态
        Vector3 velocity = Body.Velocity;
        float horizontalSpeed = new Vector2(velocity.X, velocity.Z).Length();
        bool isMoving = horizontalSpeed > 0.1f;
        bool isSprinting = Input.IsActionPressed("sprint");
        
        // 优先级：空中 > 移动 > 静止
        if (!Body.IsOnFloor())
        {
            // 在空中 - 尝试多种跳跃动画名称
            if (_animPlayer.HasAnimation("JumpStart"))
            {
                targetAnim = "JumpStart";
            }
            else if (_animPlayer.HasAnimation("Jump Start"))
            {
                targetAnim = "Jump Start";
            }
            else if (_animPlayer.HasAnimation("JumpLoop"))
            {
                targetAnim = "JumpLoop";
            }
            else if (_animPlayer.HasAnimation("Jump Loop"))
            {
                targetAnim = "Jump Loop";
            }
            else if (_animPlayer.HasAnimation("Jump"))
            {
                targetAnim = "Jump";
            }
            else
            {
                GD.PushWarning("AnimationControllerComponent: 跳跃动画未找到！尝试的名称: JumpStart, Jump Start, JumpLoop, Jump Loop, Jump");
            }
        }
        else if (isMoving)
        {
            // 移动中
            if (isSprinting && _animPlayer.HasAnimation("Sprint"))
            {
                targetAnim = "Sprint";
            }
            else if (isSprinting && _animPlayer.HasAnimation("Sprint Animation"))
            {
                targetAnim = "Sprint Animation";
            }
            else if (isSprinting && _animPlayer.HasAnimation("FastRun"))
            {
                targetAnim = "FastRun";
            }
            else if (_animPlayer.HasAnimation("Run"))
            {
                targetAnim = "Run";
            }
            else if (_animPlayer.HasAnimation("Run Animation"))
            {
                targetAnim = "Run Animation";
            }
        }
        else
        {
            // 静止
            if (_animPlayer.HasAnimation("Idle"))
            {
                targetAnim = "Idle";
            }
            else if (_animPlayer.HasAnimation("Idle Animation"))
            {
                targetAnim = "Idle Animation";
            }
        }
        
        // 切换动画
        if (!string.IsNullOrEmpty(targetAnim) && _currentAnimation != targetAnim)
        {
            // 获取动画速度
            float animSpeed = 1.0f;
            if (_animSet != null)
            {
                animSpeed = _animSet.GetAnimationSpeed(targetAnim);
            }
            
            GD.Print($"AnimationControllerComponent: 切换动画 {_currentAnimation} -> {targetAnim} (速度: {animSpeed})");
            _animPlayer.Play(targetAnim, customBlend: AnimationBlendTime, customSpeed: animSpeed);
            _currentAnimation = targetAnim;
        }
    }
    
    #endregion
    
    #region TODO: 高级动画功能
    
    // TODO: 动画事件
    // public event Action<string> OnAnimationChanged;
    // public event Action OnAnimationFinished;
    
    // TODO: 动画层混合
    // 支持上半身和下半身独立动画（如边走边射击）
    
    // TODO: IK (Inverse Kinematics)
    // 脚步 IK、手部 IK 等
    
    // TODO: 动画状态机集成
    // 如果需要更复杂的动画逻辑，可以集成 AnimationTree
    
    #endregion
}
