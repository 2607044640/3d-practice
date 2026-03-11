using Godot;
using Godot.Composition;

/// <summary>
/// 动画控制器组件 - 仅负责动画播放逻辑
/// 遵循单一职责原则：只处理动画，不处理输入或移动
/// </summary>
[GlobalClass]
[Component(typeof(CharacterBody3D))]
public partial class AnimationControllerComponent : Node
{
    #region Export Properties
    
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
    private AnimationSet _animSet;
    
    #endregion

    #region Godot Lifecycle
    
    public override void _Ready()
    {
        InitializeComponent();
        
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
        var characterModel = parent.GetNodeOrNull<Node3D>(CharacterModelPath);
        if (characterModel == null)
        {
            GD.PushWarning($"AnimationControllerComponent: 角色模型未找到: {CharacterModelPath}");
            return;
        }

        // 获取AnimationPlayer
        var animPlayerFullPath = CharacterModelPath + "/" + AnimationPlayerPath;
        _animPlayer = parent.GetNodeOrNull<AnimationPlayer>(animPlayerFullPath);
        
        if (_animPlayer == null)
        {
            GD.PushWarning($"AnimationControllerComponent: AnimationPlayer 未找到: {animPlayerFullPath}");
            return;
        }

        // 应用动画配置
        if (AnimConfig != null)
        {
            AnimConfig.ApplyToAnimationPlayer(_animPlayer);
            _animSet = AnimConfig.AnimationSet;
            GD.Print($"AnimationControllerComponent: AnimationConfig 已应用。可用动画: {string.Join(", ", _animPlayer.GetAnimationList())}");
        }
        else
        {
            GD.PushWarning("AnimationControllerComponent: 未设置 AnimConfig！");
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
        if (_animPlayer == null) return;
        
        string targetAnim = "";
        
        // 使用 parent 访问 CharacterBody3D
        Vector3 velocity = parent.Velocity;
        float horizontalSpeed = new Vector2(velocity.X, velocity.Z).Length();
        bool isMoving = horizontalSpeed > 0.1f;
        bool isSprinting = Input.IsActionPressed("sprint");
        
        // 优先级：空中 > 移动 > 静止
        if (!parent.IsOnFloor())
        {
            // 在空中 - 尝试多种跳跃动画名称
            if (_animPlayer.HasAnimation(AnimationNames.JumpStart))
            {
                targetAnim = AnimationNames.JumpStart;
            }
            else if (_animPlayer.HasAnimation(AnimationNames.JumpStartAlt))
            {
                targetAnim = AnimationNames.JumpStartAlt;
            }
            else if (_animPlayer.HasAnimation(AnimationNames.JumpLoop))
            {
                targetAnim = AnimationNames.JumpLoop;
            }
            else if (_animPlayer.HasAnimation(AnimationNames.JumpLoopAlt))
            {
                targetAnim = AnimationNames.JumpLoopAlt;
            }
            else if (_animPlayer.HasAnimation(AnimationNames.Jump))
            {
                targetAnim = AnimationNames.Jump;
            }
        }
        else if (isMoving)
        {
            // 移动中
            if (isSprinting && _animPlayer.HasAnimation(AnimationNames.Sprint))
            {
                targetAnim = AnimationNames.Sprint;
            }
            else if (isSprinting && _animPlayer.HasAnimation(AnimationNames.SprintAlt))
            {
                targetAnim = AnimationNames.SprintAlt;
            }
            else if (isSprinting && _animPlayer.HasAnimation(AnimationNames.FastRun))
            {
                targetAnim = AnimationNames.FastRun;
            }
            else if (_animPlayer.HasAnimation(AnimationNames.Run))
            {
                targetAnim = AnimationNames.Run;
            }
            else if (_animPlayer.HasAnimation(AnimationNames.RunAlt))
            {
                targetAnim = AnimationNames.RunAlt;
            }
        }
        else
        {
            // 静止
            if (_animPlayer.HasAnimation(AnimationNames.Idle))
            {
                targetAnim = AnimationNames.Idle;
            }
            else if (_animPlayer.HasAnimation(AnimationNames.IdleAlt))
            {
                targetAnim = AnimationNames.IdleAlt;
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
            
            _animPlayer.Play(targetAnim, customBlend: AnimationBlendTime, customSpeed: animSpeed);
            _currentAnimation = targetAnim;
        }
    }
    
    #endregion
}
