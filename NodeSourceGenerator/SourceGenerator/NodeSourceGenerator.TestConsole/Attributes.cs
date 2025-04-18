using System;

namespace GraphProcessor
{
    /// <summary>
    /// Tell that this field is will generate an input port
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class InputAttribute : Attribute
    {
        public string name;
        public bool allowMultiple = false;

        /// <summary>
        /// Mark the field as an input port
        /// </summary>
        /// <param name="name">display name</param>
        /// <param name="allowMultiple">is connecting multiple edges allowed</param>
        public InputAttribute(string name = null, bool allowMultiple = false)
        {
            this.name = name;
            this.allowMultiple = allowMultiple;
        }
    }

    /// <summary>
    /// Tell that this field is will generate an output port
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class OutputAttribute : Attribute
    {
        public string name;
        public bool allowMultiple = true;

        /// <summary>
        /// Mark the field as an output port
        /// </summary>
        /// <param name="name">display name</param>
        /// <param name="allowMultiple">is connecting multiple edges allowed</param>
        public OutputAttribute(string name = null, bool allowMultiple = true)
        {
            this.name = name;
            this.allowMultiple = allowMultiple;
        }
    }

    /// <summary>
    /// Creates a vertical port instead of the default horizontal one
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class VerticalAttribute : Attribute
    {
    }

    /// <summary>
    /// Register the node in the NodeProvider class. The node will also be available in the node creation window.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class NodeMenuItemAttribute : Attribute
    {
        public string menuTitle;

        /// <summary>
        /// Register the node in the NodeProvider class. The node will also be available in the node creation window.
        /// </summary>
        /// <param name="menuTitle">Path in the menu, use / as folder separators</param>
        public NodeMenuItemAttribute(string menuTitle = null)
        {
            this.menuTitle = menuTitle;
        }
    }

    /// <summary>
    /// Set a custom drawer for a field. It can then be created using the FieldFactory
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    [Obsolete("You can use the standard Unity CustomPropertyDrawer instead.")]
    public class FieldDrawerAttribute : Attribute
    {
        public Type fieldType;

        /// <summary>
        /// Register a custom view for a type in the FieldFactory class
        /// </summary>
        /// <param name="fieldType"></param>
        public FieldDrawerAttribute(Type fieldType)
        {
            this.fieldType = fieldType;
        }
    }

    /// <summary>
    /// Allow you to have a custom view for your stack nodes
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CustomStackNodeView : Attribute
    {
        public Type stackNodeType;

        /// <summary>
        /// Allow you to have a custom view for your stack nodes
        /// </summary>
        /// <param name="stackNodeType">The type of the stack node you target</param>
        public CustomStackNodeView(Type stackNodeType)
        {
            this.stackNodeType = stackNodeType;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class VisibleIf : Attribute
    {
        public string fieldName;
        public object value;

        public VisibleIf(string fieldName, object value)
        {
            this.fieldName = fieldName;
            this.value = value;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ShowInInspector : Attribute
    {
        public bool showInNode;

        public ShowInInspector(bool showInNode = false)
        {
            this.showInNode = showInNode;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ShowAsDrawer : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SettingAttribute : Attribute
    {
        public string name;

        public SettingAttribute(string name = null)
        {
            this.name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class IsCompatibleWithGraph : Attribute
    {
        public Type GraphType { get; private set; }

        public IsCompatibleWithGraph(Type graphType)
        {
            GraphType = graphType;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class IsCompatibleWithStack : Attribute
    {
        public Type StackNodeType { get; private set; }

        public IsCompatibleWithStack(Type stackNodeType)
        {
            StackNodeType = stackNodeType;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class PartialNodeAttribute : Attribute
    {
        public PartialNodeAttribute()
        {
            
        }
    }
}

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public abstract class PropertyAttribute : Attribute
    {
        /// <summary>
        ///   <para>Optional field to specify the order that multiple DecorationDrawers should be drawn in.</para>
        /// </summary>
        public int order { get; set; }
    }

    /// <summary>
    ///   <para>Specify a tooltip for a field in the Inspector window.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
    public class TooltipAttribute : PropertyAttribute
    {
        /// <summary>
        ///   <para>The tooltip text.</para>
        /// </summary>
        public readonly string tooltip;

        /// <summary>
        ///   <para>Specify a tooltip for a field.</para>
        /// </summary>
        /// <param name="tooltip">The tooltip text.</param>
        public TooltipAttribute(string tooltip) => this.tooltip = tooltip;
    }
}