/// <summary>
/// 动画名称常量类 - 消除魔法字符串
/// 集中管理所有动画名称，提供类型安全和 IDE 自动补全
/// </summary>
public static class AnimationNames
{
    #region 基础移动动画
    
    /// <summary>待机动画</summary>
    public const string Idle = "Idle";
    
    /// <summary>行走动画</summary>
    public const string Walk = "Walk";
    
    /// <summary>跑步动画</summary>
    public const string Run = "Run";
    
    /// <summary>冲刺动画</summary>
    public const string Sprint = "Sprint";
    
    #endregion

    #region 跳跃和空中动画
    
    /// <summary>跳跃起始动画</summary>
    public const string JumpStart = "JumpStart";
    
    /// <summary>跳跃循环动画（空中）</summary>
    public const string JumpLoop = "JumpLoop";
    
    /// <summary>下落动画</summary>
    public const string Fall = "Fall";
    
    /// <summary>落地动画</summary>
    public const string Land = "Land";
    
    #endregion

    #region 飞行动画
    
    /// <summary>飞行待机动画</summary>
    public const string FlyIdle = "FlyIdle";
    
    /// <summary>飞行移动动画</summary>
    public const string FlyMove = "FlyMove";
    
    /// <summary>飞行加速动画</summary>
    public const string FlyFast = "FlyFast";
    
    #endregion

    #region 战斗动画
    
    /// <summary>攻击1动画</summary>
    public const string Attack1 = "Attack1";
    
    /// <summary>攻击2动画</summary>
    public const string Attack2 = "Attack2";
    
    /// <summary>攻击3动画</summary>
    public const string Attack3 = "Attack3";
    
    /// <summary>闪避动画</summary>
    public const string Dodge = "Dodge";
    
    /// <summary>受击动画</summary>
    public const string Hit = "Hit";
    
    /// <summary>死亡动画</summary>
    public const string Death = "Death";
    
    #endregion

    #region 备用名称（兼容性）
    
    /// <summary>跳跃起始动画（备用名称）</summary>
    public const string JumpStartAlt = "Jump Start";
    
    /// <summary>跳跃循环动画（备用名称）</summary>
    public const string JumpLoopAlt = "Jump Loop";
    
    /// <summary>跳跃动画（通用名称）</summary>
    public const string Jump = "Jump";
    
    /// <summary>冲刺动画（备用名称）</summary>
    public const string SprintAlt = "Sprint Animation";
    
    /// <summary>快速跑步动画</summary>
    public const string FastRun = "FastRun";
    
    /// <summary>待机动画（备用名称）</summary>
    public const string IdleAlt = "Idle Animation";
    
    /// <summary>跑步动画（备用名称）</summary>
    public const string RunAlt = "Run Animation";
    
    #endregion
}
