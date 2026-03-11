using Godot;

/// <summary>
/// 玩家输入组件 - 读取玩家键盘/手柄输入并通过事件向外广播
/// 继承 BaseInputComponent，实现玩家特定的输入逻辑
/// </summary>
[GlobalClass]
public partial class PlayerInputComponent : BaseInputComponent
{
    #region Godot Lifecycle
    
    public override void _Ready()
    {
        base._Ready(); // 调用基类的 InitializeComponent
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
        
        // 触发移动输入事件
        TriggerMovementInput(inputDir);
    }
    
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!InputEnabled) return;
        
        // 跳跃输入
        if (Input.IsActionJustPressed("jump"))
        {
            GD.Print("PlayerInputComponent: 跳跃按键按下！");
            TriggerJumpInput();
        }
    }
    
    #endregion
}
