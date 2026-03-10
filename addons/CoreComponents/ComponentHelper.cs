using Godot;
using System.Linq;

/// <summary>
/// 组件辅助方法 - 简化接口类型的组件查找
/// 由于 Godot.Composition 不支持接口的自动依赖注入，
/// 我们提供一个简单的辅助方法来查找实现了特定接口的组件
/// </summary>
public static class ComponentHelper
{
    /// <summary>
    /// 在子节点中查找实现了指定接口的组件
    /// </summary>
    /// <typeparam name="T">接口类型</typeparam>
    /// <param name="parent">父节点</param>
    /// <returns>找到的组件，未找到返回 null</returns>
    public static T GetComponent<T>(this Node parent) where T : class
    {
        return parent.GetChildren().OfType<T>().FirstOrDefault();
    }
}
