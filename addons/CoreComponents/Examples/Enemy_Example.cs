using Godot;
using Godot.Composition;
using System;

/// <summary>
/// Enemy 实体示例
/// 
/// 展示如何复用 Player 的组件来创建 AI 控制的敌人。
/// 
/// 功能：
/// - AI 自动移动（巡逻/追击）
/// - 自动转向移动方向
/// - 动画自动切换
/// - 生命值系统
/// 
/// 复用的组件：
/// - MovementComponent - 完全复用，无需修改
/// - CharacterRotationComponent - 完全复用，无需修改
/// - AnimationControllerComponent - 完全复用，无需修改
/// 
/// 新增的组件：
/// - AIInputComponent - 替代 PlayerInputComponent，发出相同的事件
/// - HealthComponent - 处理生命值
/// </summary>
[Entity]
public partial class Enemy : CharacterBody3D
{
    public override void _Ready()
    {
        InitializeEntity();
    }
}

/* 
 * 场景结构：
 * 
 * Enemy (CharacterBody3D) - 本脚本
 * ├── AIInputComponent (Node) - 新增：AI 决策
 * ├── MovementComponent (Node) - 复用
 * ├── CharacterRotationComponent (Node) - 复用
 * ├── AnimationControllerComponent (Node) - 复用
 * ├── HealthComponent (Node) - 新增：生命值
 * ├── CollisionShape3D
 * └── CharacterModel (Node3D)
 *     └── AnimationPlayer
 * 
 * 关键点：
 * 1. AIInputComponent 发出与 PlayerInputComponent 相同的事件
 * 2. MovementComponent 不知道输入来自玩家还是 AI
 * 3. 这就是组件化的威力：完全解耦！
 */

// ============================================
// AIInputComponent 示例实现
// ============================================

/// <summary>
/// AI 输入组件
/// 
/// 模拟 PlayerInputComponent 的接口，但输入来自 AI 决策。
/// 继承 BaseInputComponent，实现与 PlayerInputComponent 相同的接口。
/// 这样就可以复用所有依赖 BaseInputComponent 的组件。
/// </summary>
[GlobalClass]
public partial class AIInputComponent : BaseInputComponent
{
    #region Export Properties
    
    /// <summary>
    /// AI 类型
    /// </summary>
    [Export] public AIType Type { get; set; } = AIType.Patrol;
    
    /// <summary>
    /// 巡逻点
    /// </summary>
    [Export] public Node3D[] PatrolPoints { get; set; }
    
    /// <summary>
    /// 追击目标（通常是玩家）
    /// </summary>
    [Export] public Node3D Target { get; set; }
    
    /// <summary>
    /// 检测范围
    /// </summary>
    [Export] public float DetectionRange { get; set; } = 10.0f;
    
    #endregion

    #region Private State
    
    private int _currentPatrolIndex = 0;
    private AIState _state = AIState.Patrol;
    
    #endregion

    #region Godot Lifecycle
    
    public override void _Ready()
    {
        base._Ready(); // 调用基类的 InitializeComponent
    }
    
    public override void _Process(double delta)
    {
        if (!InputEnabled) return;
        
        // AI 决策逻辑
        UpdateAI(delta);
    }
    
    #endregion

    #region AI Logic
    
    private void UpdateAI(double delta)
    {
        switch (_state)
        {
            case AIState.Patrol:
                UpdatePatrol();
                break;
            case AIState.Chase:
                UpdateChase();
                break;
            case AIState.Idle:
                TriggerMovementInput(Vector2.Zero);
                break;
        }
        
        // 检测玩家
        CheckForTarget();
    }
    
    private void UpdatePatrol()
    {
        if (PatrolPoints == null || PatrolPoints.Length == 0)
        {
            TriggerMovementInput(Vector2.Zero);
            return;
        }
        
        // 计算到巡逻点的方向
        Node3D targetPoint = PatrolPoints[_currentPatrolIndex];
        Vector3 direction = (targetPoint.GlobalPosition - parent.GlobalPosition).Normalized();
        
        // 转换为 2D 输入（X, Z）
        Vector2 input = new Vector2(direction.X, direction.Z);
        TriggerMovementInput(input);
        
        // 到达巡逻点，切换到下一个
        if (parent.GlobalPosition.DistanceTo(targetPoint.GlobalPosition) < 1.0f)
        {
            _currentPatrolIndex = (_currentPatrolIndex + 1) % PatrolPoints.Length;
        }
    }
    
    private void UpdateChase()
    {
        if (Target == null)
        {
            _state = AIState.Patrol;
            return;
        }
        
        // 计算到目标的方向
        Vector3 direction = (Target.GlobalPosition - parent.GlobalPosition).Normalized();
        Vector2 input = new Vector2(direction.X, direction.Z);
        TriggerMovementInput(input);
        
        // 如果目标太远，返回巡逻
        if (parent.GlobalPosition.DistanceTo(Target.GlobalPosition) > DetectionRange * 1.5f)
        {
            _state = AIState.Patrol;
        }
    }
    
    private void CheckForTarget()
    {
        if (Target == null) return;
        
        float distance = parent.GlobalPosition.DistanceTo(Target.GlobalPosition);
        
        if (distance < DetectionRange && _state != AIState.Chase)
        {
            _state = AIState.Chase;
            GD.Print("Enemy: 发现目标！");
        }
    }
    
    #endregion
}

public enum AIType
{
    Patrol,    // 巡逻
    Guard,     // 守卫
    Aggressive // 主动攻击
}

public enum AIState
{
    Idle,   // 待机
    Patrol, // 巡逻
    Chase,  // 追击
    Attack  // 攻击
}

// ============================================
// HealthComponent 示例实现
// ============================================

/// <summary>
/// 生命值组件
/// 
/// 处理生命值、伤害、死亡等逻辑。
/// 可用于 Enemy、Player、可破坏物体等。
/// </summary>
[GlobalClass]
[Component(typeof(Node3D))]
public partial class HealthComponent : Node
{
    #region Events
    
    /// <summary>
    /// 受到伤害事件
    /// </summary>
    public event Action<float> OnDamaged;
    
    /// <summary>
    /// 治疗事件
    /// </summary>
    public event Action<float> OnHealed;
    
    /// <summary>
    /// 死亡事件
    /// </summary>
    public event Action OnDied;
    
    #endregion

    #region Export Properties
    
    /// <summary>
    /// 最大生命值
    /// </summary>
    [Export] public float MaxHealth { get; set; } = 100.0f;
    
    /// <summary>
    /// 当前生命值
    /// </summary>
    [Export] public float CurrentHealth { get; set; } = 100.0f;
    
    /// <summary>
    /// 是否无敌
    /// </summary>
    [Export] public bool IsInvincible { get; set; } = false;
    
    #endregion

    #region Properties
    
    /// <summary>
    /// 是否存活
    /// </summary>
    public bool IsAlive => CurrentHealth > 0;
    
    /// <summary>
    /// 生命值百分比
    /// </summary>
    public float HealthPercent => CurrentHealth / MaxHealth;
    
    #endregion

    #region Godot Lifecycle
    
    public override void _Ready()
    {
        InitializeComponent();
        CurrentHealth = MaxHealth;
    }
    
    #endregion

    #region Public API
    
    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (!IsAlive || IsInvincible) return;
        
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        OnDamaged?.Invoke(amount);
        
        GD.Print($"HealthComponent: 受到 {amount} 伤害，剩余 {CurrentHealth}/{MaxHealth}");
        
        if (!IsAlive)
        {
            Die();
        }
    }
    
    /// <summary>
    /// 治疗
    /// </summary>
    public void Heal(float amount)
    {
        if (!IsAlive) return;
        
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        OnHealed?.Invoke(amount);
        
        GD.Print($"HealthComponent: 治疗 {amount}，当前 {CurrentHealth}/{MaxHealth}");
    }
    
    /// <summary>
    /// 死亡
    /// </summary>
    private void Die()
    {
        GD.Print("HealthComponent: 死亡！");
        OnDied?.Invoke();
        
        // TODO: 播放死亡动画
        // TODO: 禁用碰撞
        // TODO: 延迟后删除节点
    }
    
    #endregion
}
