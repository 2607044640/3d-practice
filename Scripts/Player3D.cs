using Godot;
using Godot.Composition;

/// <summary>
/// Player3D - 纯粹的 Entity 容器
/// 职责：仅作为组件的挂载点，不包含任何业务逻辑
/// </summary>
[Entity]
public partial class Player3D : CharacterBody3D
{
    public override void _Ready()
    {
        // Godot.Composition 会自动收集所有子组件并解析依赖
        InitializeEntity();
        
        GD.Print("Player3D Entity: 初始化完成 ✓");
    }
}
