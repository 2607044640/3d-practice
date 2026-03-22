using Godot;
using GodotStateCharts;
using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable All

/// <summary>
/// 组件查找扩展方法 - 简化组件依赖查找
/// 提供类似 Unity 的 GetComponent API
/// </summary>
public static class ComponentExtensions
{
    #region 基础查找方法

    /// <summary>
    /// 在子节点中查找指定类型的组件（支持多态）
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    /// <param name="node">父节点</param>
    /// <returns>找到的组件，未找到返回 null</returns>
    private static T GetComponentInChildren<T>(this Node node) where T : Node
    {
        return node.GetChildren().OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// 在子节点中查找所有指定类型的组件
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    /// <param name="node">父节点</param>
    /// <returns>找到的所有组件列表</returns>
    public static List<T> GetComponentsInChildren<T>(this Node node) where T : Node
    {
        return node.GetChildren().OfType<T>().ToList();
    }

    /// <summary>
    /// 在子节点中查找指定类型的组件，未找到则报错
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    /// <param name="node">父节点</param>
    /// <returns>找到的组件</returns>
    public static T GetRequiredComponentInChildren<T>(this Node node) where T : Node
    {
        var component = node.GetComponentInChildren<T>();
        if (component == null)
        {
            GD.PushError($"Required component {typeof(T).Name} not found in {node.Name}");
        }

        return component;
    }

    #endregion

    #region 输入组件专用方法

    /// <summary>
    /// 查找并订阅输入组件（专用辅助方法）
    /// 自动查找 BaseInputComponent 并订阅事件
    /// </summary>
    /// <param name="parent">父节点</param>
    /// <param name="onMovement">移动输入回调</param>
    /// <param name="onJump">跳跃输入回调（可选）</param>
    /// <returns>找到的输入组件</returns>
    public static BaseInputComponent FindAndSubscribeInput(
        this Node parent,
        Action<Vector2> onMovement,
        Action onJump = null)
    {
        var input = parent.GetRequiredComponentInChildren<BaseInputComponent>();

        if (input == null)
        {
            return null;
        }

        // 订阅事件
        if (onMovement != null)
        {
            input.OnMovementInput += onMovement;
        }

        if (onJump != null)
        {
            input.OnJumpJustPressed += onJump;
        }

        GD.Print($"✓ 已订阅 {input.GetType().Name} 事件");
        return input;
    }

    /// <summary>
    /// 取消订阅输入组件
    /// </summary>
    /// <param name="input">输入组件</param>
    /// <param name="onMovement">移动输入回调</param>
    /// <param name="onJump">跳跃输入回调（可选）</param>
    public static void UnsubscribeInput(
        this BaseInputComponent input,
        Action<Vector2> onMovement,
        Action onJump = null)
    {
        if (input == null) return;

        input.OnMovementInput -= onMovement;
        if (onJump != null)
        {
            input.OnJumpJustPressed -= onJump;
        }
    }

    #endregion

    #region StateChart 扩展方法 - 极致解耦

    /// <summary>
    /// 核心方法1：发送状态事件（黑盒路由）
    /// StateChart 对组件完全透明，组件只需要知道"发送事件"这个动作
    /// </summary>
    /// <param name="node">实体节点（通常是 parent）</param>
    /// <param name="eventName">事件名称</param>
    public static void SendStateEvent(this Node node, string eventName)
    {
        // 查找 StateChart 节点（假设在实体的子节点中）
        var stateChartNode = node.GetNodeOrNull("StateChart");
        if (stateChartNode == null)
        {
            GD.PushWarning($"[StateChart] StateChart not found in {node.Name}");
            return;
        }

        // 使用 GodotStateCharts 插件的包装类
        var stateChart = StateChart.Of(stateChartNode);
        stateChart.SendEvent(eventName);
    }

    /// <summary>
    /// 核心方法2：电源开关！将组件的生命周期与特定状态强制绑定
    /// 
    /// 工作原理：
    /// 1. 默认休眠组件（所有 Process 方法禁用）
    /// 2. 状态进入时 → 通电唤醒（启用所有 Process 方法）
    /// 3. 状态退出时 → 断电休眠（禁用所有 Process 方法）
    /// 
    /// 这样组件内部无需任何状态判断，纯粹执行逻辑！
    /// </summary>
    /// <param name="component">要绑定的组件（通常是 this）</param>
    /// <param name="parentEntity">父实体节点（通常是 parent）</param>
    /// <param name="stateNodePath">状态节点路径，例如 "StateChart/Root/GameFlow/Exploration"</param>
    public static void BindComponentToStateNode(this Node component, Node parentEntity, string stateNodePath)
    {
        // 获取状态节点
        var stateNode = parentEntity.GetNodeOrNull(stateNodePath);
        if (stateNode == null)
        {
            GD.PushError($"[StateChart] 未找到状态节点: {stateNodePath} in {parentEntity.Name}");
            return;
        }

        // 使用 GodotStateCharts 插件的包装类
        var state = StateChartState.Of(stateNode);

        // 【关键】默认休眠组件，等待状态机唤醒
        component.SetProcess(false);
        component.SetPhysicsProcess(false);
        component.SetProcessInput(false);
        component.SetProcessUnhandledInput(false);

        GD.Print($"[StateChart] {component.Name} 已绑定到状态 '{stateNodePath}'（默认休眠）");

        // 状态进入：通电唤醒
        state.Connect(StateChartState.SignalName.StateEntered, Callable.From(() =>
        {
            component.SetProcess(true);
            component.SetPhysicsProcess(true);
            component.SetProcessInput(true);
            component.SetProcessUnhandledInput(true);
            GD.Print($"[StateChart] ⚡ {component.Name} 已唤醒");
        }));

        // 状态退出：断电休眠
        state.Connect(StateChartState.SignalName.StateExited, Callable.From(() =>
        {
            component.SetProcess(false);
            component.SetPhysicsProcess(false);
            component.SetProcessInput(false);
            component.SetProcessUnhandledInput(false);
            GD.Print($"[StateChart] 💤 {component.Name} 已休眠");
        }));
    }

    #endregion

    #region 调试辅助方法

    /// <summary>
    /// 打印节点的所有子组件（调试用）
    /// </summary>
    public static void PrintComponents(this Node node)
    {
        GD.Print($"=== Components in {node.Name} ===");
        foreach (var child in node.GetChildren())
        {
            GD.Print($"  - {child.Name} ({child.GetType().Name})");
        }
    }

    #endregion
}