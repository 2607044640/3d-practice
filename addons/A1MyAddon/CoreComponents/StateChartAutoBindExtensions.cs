using Godot;
using GodotStateCharts;

/// <summary>
/// StateChart 自动绑定扩展 - 极致优雅的组件挂载方式
/// 
/// 核心思想：
/// - 将组件直接作为 AtomicState 节点的子节点
/// - 组件通过读取父节点自动绑定生命周期
/// - 完全消除字符串路径依赖
/// - 场景树结构一目了然：GroundMode 下直接挂 GroundMovementComponent
/// </summary>
public static class StateChartAutoBindExtensions
{
    /// <summary>
    /// 组件自动挂载：当组件作为 AtomicState 节点的直接子节点时，自动绑定生命周期
    /// 
    /// 工作原理：
    /// 1. 获取父节点（应该是 StateChart 的状态节点）
    /// 2. 默认休眠组件（所有 Process 方法禁用）
    /// 3. 状态进入时 → 通电唤醒（启用所有 Process 方法）
    /// 4. 状态退出时 → 断电休眠（禁用所有 Process 方法）
    /// </summary>
    public static void AutoBindToParentState(this Node component)
    {
        // 1. 获取父节点，并验证它是否是 godot-statecharts 的状态节点
        var stateNode = component.GetParent();
        if (stateNode == null)
        {
            GD.PushError($"[架构错误] {component.Name} 尝试自动绑定，但没有父节点！");
            return;
        }

        // 使用 GodotStateCharts 的包装类尝试包装父节点
        var state = StateChartState.Of(stateNode);
        if (state == null)
        {
            GD.PushError($"[架构错误] {component.Name} 尝试自动绑定，但其父节点 {stateNode.Name} 不是一个合法的 StateChart 状态节点！");
            return;
        }

        // 2. 【关键】默认休眠组件，等待状态机唤醒
        component.SetProcess(false);
        component.SetPhysicsProcess(false);
        component.SetProcessInput(false);
        component.SetProcessUnhandledInput(false);

        GD.Print($"[PowerSwitch] 💤 {component.Name} 已挂载到状态 '{stateNode.Name}' 下，进入默认休眠。");

        // 3. 状态进入：通电唤醒
        state.Connect(StateChartState.SignalName.StateEntered, Callable.From(() =>
        {
            component.SetProcess(true);
            component.SetPhysicsProcess(true);
            component.SetProcessInput(true);
            component.SetProcessUnhandledInput(true);
            GD.Print($"[PowerSwitch] ⚡ {component.Name} 已通电唤醒！");
        }));

        // 4. 状态退出：断电休眠
        state.Connect(StateChartState.SignalName.StateExited, Callable.From(() =>
        {
            component.SetProcess(false);
            component.SetPhysicsProcess(false);
            component.SetProcessInput(false);
            component.SetProcessUnhandledInput(false);
            GD.Print($"[PowerSwitch] 💤 {component.Name} 已断电休眠。");
        }));
    }

    /// <summary>
    /// 向上查找获取真实的实体控制器 (Player3D)
    /// 
    /// 因为现在的结构是：
    /// Player3D -> StateChart -> Root -> Movement -> GroundMode -> GroundMovementComponent
    /// 
    /// 组件的 GetParent() 返回的是 GroundMode，而不是 Player3D
    /// 所以需要这个方法来获取真正的实体
    /// </summary>
    /// <typeparam name="T">实体类型（如 CharacterBody3D 或 Player3D）</typeparam>
    /// <param name="component">组件节点</param>
    /// <returns>找到的实体，未找到返回 null</returns>
    public static T GetEntity<T>(this Node component) where T : Node
    {
        // Owner 通常是打包场景的根节点 (Player3D)，这是最高效的获取方式
        if (component.Owner is T entity)
        {
            return entity;
        }

        // 兜底方案：顺着树往上爬，直到找到 T 类型的节点
        Node current = component.GetParent();
        while (current != null)
        {
            if (current is T match)
            {
                return match;
            }
            current = current.GetParent();
        }

        GD.PushError($"[架构错误] {component.Name} 无法找到类型为 {typeof(T).Name} 的实体节点！");
        return null;
    }
}
