using System.Reflection;

namespace GraphProcessor
{
    public class BaseNode
    {
        protected bool _needsInspector = false;
        public virtual string name => GetType().Name;
        
        protected virtual bool fieldsSortedDescending => true;

        protected internal Dictionary<string, NodeFieldInformation> nodeFields =
            new Dictionary<string, NodeFieldInformation>();

        protected internal class NodeFieldInformation
        {
            public string name;
            public string fieldName;
            public bool input;
            public bool isMultiple;
            public string tooltip;
            public bool vertical;
            public int sortingOrder;

            public FieldInfo info
            {
                get
                {
                    if (_info == null)
                        _info = _node.GetType().GetField(fieldName,
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    return _info;
                }
            }

            private BaseNode _node;
            private FieldInfo _info;

            public NodeFieldInformation(FieldInfo info, string name, bool input, bool isMultiple, string tooltip,
                bool vertical)
            {
                this.input = input;
                this.isMultiple = isMultiple;
                this._info = info;
                this.name = name;
                this.fieldName = info.Name;
                this.tooltip = tooltip;
                this.vertical = vertical;
            }

            public NodeFieldInformation(BaseNode node, string fieldName, string name, bool input, bool isMultiple,
                string tooltip, bool vertical, int sortingOrder)
            {
                this._node = node;
                this.input = input;
                this.isMultiple = isMultiple;
                this.name = name;
                this.fieldName = fieldName;
                this.tooltip = tooltip;
                this.vertical = vertical;
                this.sortingOrder = sortingOrder;
            }
        }

        protected virtual void InitializeFieldData()
        {
        }

        protected virtual bool TryGetOutputValue<T>(int index, out T value, int edgeIndex)
        {
            // Debug.LogWarning($"{GetType()} didn't override TryGetOutputValue, returning default value");
            value = default;
            return false;
        }

        public static bool TryConvertValue<T, T2>(ref T value, out T2 output)
        {
            if (value is T2 finalValue)
            {
                output = finalValue;
                return true;
            }
            else
            {
                output = default;
                return false;
            }
        }

        protected bool TryReadInputValue<T>(int inputIdx, ref T field, BaseNode prevNode)
        {
            return false;
        }

        protected bool TryReadInputValue<T>(int index, ref T field, int edgeIdx = 0)
        {
            return false;
        }

        /// <summary>
        /// Get input value from the previous node, and convert it's type from <typeparamref name="T"/> to <typeparamref name="T2"/>
        /// </summary>
        /// <param name="index">input port index</param>
        /// <param name="field"></param>
        /// <param name="prevNode"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        protected bool TryReadInputValue<T, T2>(int index, ref T2 field, BaseNode prevNode)
        {
            return false;
        }

        /// <summary>
        /// Get input value from the previous node, and convert it's type from <typeparamref name="T"/> to <typeparamref name="T2"/>
        /// </summary>
        /// <param name="index">input port index</param>
        /// <param name="field"></param>
        /// <param name="edgeIdx"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        protected bool TryReadInputValue<T, T2>(int index, ref T2 field, int edgeIdx = 0)
        {
            return false;
        }

        protected virtual void Process()
        {
        }

        protected virtual void PrepareInputsGenerated(BaseNode prevNode = null)
        {
        }
    }
}