using Godot;
using Godot.Composition;
using System;

/// <summary>
/// 玩家输入组件 - 读取玩家键盘/手柄输入并通过事件向外广播
/// 实现 IEntityInput 接口，支持 Godot.Composition 的自动依赖注入
/// </summary>
[GlobalClass]
[Component(typeof(CharacterBody3D))]
public partial class PlayerInputComponent : Node, IEntityInput
{
    #region IEntityInput Implementation
    
    /// <summary>
    /// 移动输入事件
    /// </summary>
    public event Action<Vector2> OnMovementInput;
    
    /// <summary>
    /// 跳跃输入事件
    /// </summary>
    public event Action OnJumpJustPressed;
    
    /// <summary>
    /// 是否启用输入处理
    /// </summary>
    [Export] public bool InputEnabled { get; set; } = true;
    
    #endregion

    #region Godot Lifecycle
    
    public override void _Ready()
    {
        InitializeComponent();
        GD.Print($"PlayerInputComponent: 已初始化，InputEnabled={InputEnabled}");
    }
    
    public override void _Process(double delta)
    {
        if (!InputEnabled) return;
        
        // 读取移动输入
        Vector2 inputDir = Input.GetVector(
            "move_left",
            "move_right",
            "move_forward",
            "move_backward"
        );
        
        // 触发移动输入事件（使用 null-conditional 操作符安全调用）
        OnMovementInput?.Invoke(inputDir);
    }
    
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!InputEnabled) return;
        
        // 跳跃输入
        if (Input.IsActionJustPressed("jump"))
        {
            GD.Print("PlayerInputComponent: 跳跃按键按下！");
            OnJumpJustPressed?.Invoke();
        }
    }
    
    #endregion
}
