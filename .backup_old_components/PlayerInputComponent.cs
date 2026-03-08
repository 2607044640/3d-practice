using Godot;
using System;

/// <summary>
/// 玩家输入组件 - 仅负责读取输入并通过事件向外广播
/// 遵循单一职责原则：只处理输入，不处理移动逻辑
/// </summary>
[GlobalClass]
public partial class PlayerInputComponent : Node
{
    #region Events (向上传递信息)
    
    /// <summary>
    /// 移动输入事件 (WASD/方向键)
    /// Vector2: X = 左右 (-1 到 1), Y = 前后 (-1 到 1)
    /// </summary>
    public event Action<Vector2> OnMovementInput;
    
    /// <summary>
    /// 跳跃按键刚按下事件
    /// </summary>
    public event Action OnJumpJustPressed;
    
    // TODO: 添加更多输入事件
    // public event Action OnSprintPressed;
    // public event Action OnSprintReleased;
    // public event Action OnCrouchToggled;
    // public event Action OnInteractPressed;
    
    #endregion

    #region Export Properties (可在编辑器中配置)
    
    /// <summary>
    /// 是否启用输入处理
    /// </summary>
    [Export] public bool InputEnabled { get; set; } = true;
    
    #endregion

    #region Godot Lifecycle
    
    public override void _Ready()
    {
        GD.Print($"PlayerInputComponent: 已初始化，InputEnabled={InputEnabled}");
    }
    
    public override void _Process(double delta)
    {
        if (!InputEnabled) return;
        
        // 读取移动输入 (WASD / 方向键)
        Vector2 inputDir = Input.GetVector(
            "move_left",    // A / 左箭头
            "move_right",   // D / 右箭头
            "move_forward", // W / 上箭头
            "move_backward" // S / 下箭头
        );
        
        // 广播移动输入（即使是 Vector2.Zero 也广播，让 Movement 组件决定如何处理）
        OnMovementInput?.Invoke(inputDir);
    }
    
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!InputEnabled) return;
        
        // 跳跃输入 (空格键)
        if (Input.IsActionJustPressed("jump"))
        {
            GD.Print("PlayerInputComponent: 跳跃按键按下！");
            OnJumpJustPressed?.Invoke();
        }
        
        // TODO: 其他按键事件
        // if (Input.IsActionPressed("sprint")) OnSprintPressed?.Invoke();
        // if (Input.IsActionJustReleased("sprint")) OnSprintReleased?.Invoke();
    }
    
    #endregion

    #region TODO: 高级输入功能
    
    // TODO: 输入缓冲 (Input Buffering)
    // 记录最近 N 帧的输入，允许玩家提前按下跳跃键
    // private Queue<(string action, double timestamp)> _inputBuffer;
    
    // TODO: 长按蓄力 (Charge Input)
    // 检测按键按住时长，用于蓄力跳跃、重击等
    // private double _jumpHoldTime;
    
    // TODO: 按键映射动态修改 (Key Rebinding)
    // 运行时修改 InputMap，保存到配置文件
    // public void RemapAction(string actionName, InputEvent newKey) { }
    
    #endregion
}
