using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using Unity.Jobs;
using System.Linq;

namespace GraphProcessor
{
	[Serializable]
	public abstract class BaseNode
	{
		[SerializeField]
		internal string nodeCustomName = null; // The name of the node in case it was renamed by a user

		/// <summary>
		/// Name of the node, it will be displayed in the title section
		/// </summary>
		/// <returns></returns>
		public virtual string       name => GetType().Name;
		
		/// <summary>
		/// The accent color of the node
		/// </summary>
		public virtual Color color => Color.clear;
		
		/// <summary>
		/// Set a custom uss file for the node. We use a Resources.Load to get the stylesheet so be sure to put the correct resources path
		/// https://docs.unity3d.com/ScriptReference/Resources.Load.html
		/// </summary>
        public virtual string       layoutStyle => string.Empty;

		/// <summary>
		/// If the node can be locked or not
		/// </summary>
        public virtual bool         unlockable => true; 

		/// <summary>
		/// Is the node is locked (if locked it can't be moved)
		/// </summary>
        public virtual bool         isLocked => nodeLock; 

        //id
        public string				GUID;

		/// <summary>
		/// GUID of containing StackNode
		/// </summary>
		[SerializeField]
		internal string parentGUID;

		public int					computeOrder = -1;

		/// <summary>Tell wether or not the node can be processed. Do not check anything from inputs because this step happens before inputs are sent to the node</summary>
		public virtual bool			canProcess => true;

		/// <summary>Show the node controlContainer only when the mouse is over the node</summary>
		public virtual bool			showControlsOnHover => false;

		/// <summary>True if the node can be deleted, false otherwise</summary>
		public virtual bool			deletable => true;

		/// <summary>
		/// Container of input ports
		/// </summary>
		[NonSerialized]
		public readonly NodeInputPortContainer	inputPorts;
		/// <summary>
		/// Container of output ports
		/// </summary>
		[NonSerialized]
		public readonly NodeOutputPortContainer	outputPorts;

		//Node view datas
		public Rect					position;
		/// <summary>
		/// Is the node expanded
		/// </summary>
		public bool					expanded;
		/// <summary>
		/// Is debug visible
		/// </summary>
		public bool					debug;
		/// <summary>
		/// Node locked state
		/// </summary>
        public bool                 nodeLock;

        public delegate void		ProcessDelegate();

		/// <summary>
		/// Triggered when the node is processes
		/// </summary>
		public event ProcessDelegate	onProcessed;
		public event Action< string, NodeMessageType >	onMessageAdded;
		public event Action< string >					onMessageRemoved;
		/// <summary>
		/// Triggered after an edge was connected on the node
		/// </summary>
		public event Action< SerializableEdge >			onAfterEdgeConnected;
		/// <summary>
		/// Triggered after an edge was disconnected on the node
		/// </summary>
		public event Action< SerializableEdge >			onAfterEdgeDisconnected;

		/// <summary>
		/// Triggered after a single/list of port(s) is updated, the parameter is the field name
		/// </summary>
		public event Action					onPortsUpdated;

		[NonSerialized]
		bool _needsInspector = false;

		/// <summary>
		/// Does the node needs to be visible in the inspector (when selected).
		/// </summary>
		public virtual bool			needsInspector => _needsInspector;

		/// <summary>
		/// Can the node be renamed in the UI. By default a node can be renamed by double clicking it's name.
		/// </summary>
		public virtual bool			isRenamable => false;

		/// <summary>
		/// Is the node created from a duplicate operation (either ctrl-D or copy/paste).
		/// </summary>
		public bool					createdFromDuplication {get; internal set; } = false;

		/// <summary>
		/// True only when the node was created from a duplicate operation and is inside a group that was also duplicated at the same time. 
		/// </summary>
		public bool					createdWithinGroup {get; internal set; } = false;

		protected virtual bool hasCustomInputs => false;
        protected virtual bool hasCustomOutputs => false;

        [NonSerialized]
		internal Dictionary< string, NodeFieldInformation >	nodeFields = new Dictionary< string, NodeFieldInformation >();

		[NonSerialized]
		List< string >				messages = new List<string>();

		[NonSerialized]
		protected BaseGraph			graph;

		internal class NodeFieldInformation
		{
			public string						name;
			public string						fieldName;
			public FieldInfo					info;
			public bool							input;
			public bool							isMultiple;
			public string						tooltip;
			public bool							vertical;

			public NodeFieldInformation(FieldInfo info, string name, bool input, bool isMultiple, string tooltip, bool vertical)
			{
				this.input = input;
				this.isMultiple = isMultiple;
				this.info = info;
				this.name = name;
				this.fieldName = info.Name;
				this.tooltip = tooltip;
				this.vertical = vertical;
			}
		}

		struct PortUpdate
		{
			public List<string>	fieldNames;
			public BaseNode		node;

			public void Deconstruct(out List<string> fieldNames, out BaseNode node)
			{
				fieldNames = this.fieldNames;
				node = this.node;
			}
		}

		// Used in port update algorithm
		Stack<PortUpdate> fieldsToUpdate = new Stack<PortUpdate>();
		HashSet<PortUpdate> updatedFields = new HashSet<PortUpdate>();

		/// <summary>
		/// Creates a node of type T at a certain position
		/// </summary>
		/// <param name="position">position in the graph in pixels</param>
		/// <typeparam name="T">type of the node</typeparam>
		/// <returns>the node instance</returns>
		public static T CreateFromType< T >(Vector2 position) where T : BaseNode
		{
			return CreateFromType(typeof(T), position) as T;
		}

		/// <summary>
		/// Creates a node of type nodeType at a certain position
		/// </summary>
		/// <param name="position">position in the graph in pixels</param>
		/// <typeparam name="nodeType">type of the node</typeparam>
		/// <returns>the node instance</returns>
		public static BaseNode CreateFromType(Type nodeType, Vector2 position)
		{
			if (!nodeType.IsSubclassOf(typeof(BaseNode)))
				return null;

			var node = Activator.CreateInstance(nodeType) as BaseNode;

			node.position = new Rect(position, new Vector2(100, 100));

			ExceptionToLog.Call(() => node.OnNodeCreated());

			return node;
		}

		#region Initialization

		// called by the BaseGraph when the node is added to the graph
		public virtual void Initialize(BaseGraph graph)
		{
			this.graph = graph;

			ExceptionToLog.Call(() => Enable());

			InitializePorts();
		}

		/// <summary>
		/// Use this function to initialize anything related to ports generation in your node
		/// This will allow the node creation menu to correctly recognize ports that can be connected between nodes
		/// </summary>
		public virtual void InitializePorts()
		{
			bool allCustom = true;
			if (hasCustomInputs)
			{
				int identifier = 0;
				foreach (var port in GetCustomInputPorts())
				{
					port.identifier = identifier++;
					AddPort(true, port);
				}
			}
			else
				allCustom = false;
			if (hasCustomOutputs)
			{
                int identifier = 1000;
                foreach (var port in GetCustomOutputPorts())
				{
                    port.identifier = identifier++;
                    AddPort(false, port);
				}
			}
            else
                allCustom = false;
            if (!allCustom)
			{
				foreach (var key in OverrideFieldOrder(nodeFields.Values.Select(k => k.info)))
				{
					var nodeField = nodeFields[key.Name];
					if (nodeField.input && hasCustomInputs)
						continue;
					if (!nodeField.input && hasCustomOutputs)
						continue;

					// If we don't have a custom behavior on the node, we just have to create a simple port 
					AddPort(nodeField.input, new PortData { acceptMultipleEdges = nodeField.isMultiple, isField = true, fieldName = nodeField.fieldName, displayName = nodeField.name, tooltip = nodeField.tooltip, vertical = nodeField.vertical });
				}
            }
		}

		/// <summary>
		/// Override the field order inside the node. It allows to re-order all the ports and field in the UI.
		/// </summary>
		/// <param name="fields">List of fields to sort</param>
		/// <returns>Sorted list of fields</returns>
		public virtual IEnumerable<FieldInfo> OverrideFieldOrder(IEnumerable<FieldInfo> fields)
		{
			long GetFieldInheritanceLevel(FieldInfo f)
			{
				int level = 0;
				var t = f.DeclaringType;
				while (t != null)
				{
					t = t.BaseType;
					level++;
				}

				return level;
			}

			// Order by MetadataToken and inheritance level to sync the order with the port order (make sure FieldDrawers are next to the correct port)
			return fields.OrderByDescending(f => (long)(((GetFieldInheritanceLevel(f) << 32)) | (long)f.MetadataToken));
		}

		protected BaseNode()
		{
            inputPorts = new NodeInputPortContainer(this);
            outputPorts = new NodeOutputPortContainer(this);

			InitializeInOutDatas();
		}

		protected Func<T> CreateOutputReader<T>(Func<T> field)
		{
			return field;
		}

		/// <summary>
		/// Update all ports of the node
		/// </summary>
		public bool UpdateAllPorts()
		{
            bool changed = false;

            bool allCustom = true;
			List<int> finalPorts = new List<int>();

			void AddPort(bool input, PortData data)
			{
				NodePortContainer nodePorts = input ? inputPorts : outputPorts;
                var port = nodePorts.FirstOrDefault(n => n.fieldName == data.fieldName && n.portData.identifier == data.identifier);
                // Guard using the port identifier so we don't duplicate identifiers
                if (port == null)
                {
                    this.AddPort(input, data);
                    changed = true;
                }
                else
                {
                    if (data.displayType == null)
                    {
                        var ft = nodeFields[data.fieldName].info.FieldType;
                        data.displayType = ft;
                    }
                    // in case the port type have changed for an incompatible type, we disconnect all the edges attached to this port
                    if (!BaseGraph.TypesAreConnectable(port.portData.displayType, data.displayType))
                    {
                        foreach (var edge in port.GetEdges().ToList())
                            graph.Disconnect(edge.GUID);
                    }

                    // patch the port data
                    if (port.portData != data)
                    {
                        port.portData.CopyFrom(data);
                        changed = true;
                    }
                }

                finalPorts.Add(data.identifier);
			}
            if (hasCustomInputs)
            {
                int identifier = 0;
                foreach (var port in GetCustomInputPorts())
                {
                    port.identifier = identifier++;
                    AddPort(true, port);
                }
            }
            else
                allCustom = false;
            if (hasCustomOutputs)
            {
                int identifier = 1000;
                foreach (var port in GetCustomOutputPorts())
                {
                    port.identifier = identifier++;
                    AddPort(false, port);
                }
            }
            else
                allCustom = false;
            if (!allCustom)
            {
                foreach (var key in OverrideFieldOrder(nodeFields.Values.Select(k => k.info)))
                {
                    var nodeField = nodeFields[key.Name];
                    if (nodeField.input && hasCustomInputs)
                        continue;
                    if (!nodeField.input && hasCustomOutputs)
                        continue;

                    // If we don't have a custom behavior on the node, we just have to create a simple port 
                    AddPort(nodeField.input, new PortData { acceptMultipleEdges = nodeField.isMultiple, isField = true, fieldName = nodeField.fieldName, displayName = nodeField.name, tooltip = nodeField.tooltip, vertical = nodeField.vertical });
                }
            }

			void ClearPorts(bool input)
			{
				NodePortContainer nodePorts = input ? inputPorts : outputPorts;
                // Remove only the ports that are no more in the list
                if (nodePorts != null)
				{
					var currentPortsCopy = nodePorts.ToList();
					foreach (var currentPort in currentPortsCopy)
					{
						// If the current port does not appear in the list of final ports, we remove it
						if (!finalPorts.Any(id => id == currentPort.portData.identifier))
						{
							RemovePort(input, currentPort);
							changed = true;
						}
					}
					//Index correction
					int idx = 0;
					foreach(var i in nodePorts)
					{
						i.index = idx++;
					}
				}
			}
            ClearPorts(true);
            ClearPorts(false);

			if(changed)
                onPortsUpdated?.Invoke();
            return changed;
		}

		internal void DisableInternal()
		{
			// port containers are initialized in the OnEnable
			inputPorts.Clear();
			outputPorts.Clear();

			ExceptionToLog.Call(() => Disable());
		}

		internal void DestroyInternal() => ExceptionToLog.Call(() => Destroy());

		/// <summary>
		/// Called only when the node is created, not when instantiated
		/// </summary>
		public virtual void	OnNodeCreated() => GUID = Guid.NewGuid().ToString();

		public virtual FieldInfo[] GetNodeFields()
			=> GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

		void InitializeInOutDatas()
		{
			var fields = GetNodeFields();
			var methods = GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			foreach (var field in fields)
			{
				var inputAttribute = field.GetCustomAttribute< InputAttribute >();
				var outputAttribute = field.GetCustomAttribute< OutputAttribute >();
				var tooltipAttribute = field.GetCustomAttribute< TooltipAttribute >();
				var showInInspector = field.GetCustomAttribute< ShowInInspector >();
				var vertical = field.GetCustomAttribute< VerticalAttribute >();
				bool isMultiple = false;
				bool input = false;
				string name = field.Name;
				string tooltip = null;

				if (showInInspector != null)
					_needsInspector = true;

				if (inputAttribute == null && outputAttribute == null)
					continue ;

				//check if field is a collection type
				isMultiple = (inputAttribute != null) ? inputAttribute.allowMultiple : (outputAttribute.allowMultiple);
				input = inputAttribute != null;
				tooltip = tooltipAttribute?.tooltip;

				if (!String.IsNullOrEmpty(inputAttribute?.name))
					name = inputAttribute.name;
				if (!String.IsNullOrEmpty(outputAttribute?.name))
					name = outputAttribute.name;

				// By default we set the behavior to null, if the field have a custom behavior, it will be set in the loop just below
				nodeFields[field.Name] = new NodeFieldInformation(field, name, input, isMultiple, tooltip, vertical != null);
			}
        }

		#endregion

		#region Events and Processing

		public virtual void OnFieldValueChanged(string fieldName)
		{

		}

		public virtual void OnEdgeConnected(SerializableEdge edge)
		{
			bool input = edge.inputNode == this;
			NodePortContainer portCollection = (input) ? (NodePortContainer)inputPorts : outputPorts;

			portCollection.Add(edge);

			onAfterEdgeConnected?.Invoke(edge);
		}

		protected virtual bool CanResetPort(NodePort port) => true;

		public virtual void OnEdgeDisconnected(SerializableEdge edge)
		{
			if (edge == null)
				return ;

			bool input = edge.inputNode == this;
			NodePortContainer portCollection = (input) ? (NodePortContainer)inputPorts : outputPorts;

			portCollection.Remove(edge);

			// Reset default values of input port:
			bool haveConnectedEdges = edge.inputNode.inputPorts.Where(p => p.fieldName == edge.inputFieldName).Any(p => p.GetEdges().Count != 0);
			if (edge.inputNode == this && !haveConnectedEdges && CanResetPort(edge.inputPort))
				edge.inputPort?.ResetToDefault();

			onAfterEdgeDisconnected?.Invoke(edge);
		}

		protected virtual bool TryGetOutputValue<T>(int index, out T value, int edgeIndex)
		{
			Debug.LogWarning($"{GetType()} didn't override TryGetOutputValue, returning default value");
			value = default(T);
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

        internal bool TryReadInputValueInternal<T>(int index, out T value, int edgeIndex)
        {
			value = default;
            return TryReadInputValue(index, ref value, edgeIndex);
        }
        protected bool TryReadInputValue<T>(int index, ref T field, int edgeIdx = 0)
		{
			var port = inputPorts[index];
			var edges = port.GetEdges();
			if (edges.Count > 0)
			{
				var edge = edges[edgeIdx];
				var inputPort = edge.outputPort;
				int outputEdgeIdx = edge.inputEdgeIndex;
				if (inputPort.owner.TryGetOutputValue(inputPort.index, out field, outputEdgeIdx))
					return true;
			}
			return false;
		}

        protected bool TryReadInputValue<T, T2>(int index, ref T2 field, int edgeIdx = 0)
        {
            var port = inputPorts[index];
            var edges = port.GetEdges();
            if (edges.Count > 0)
            {
                var edge = edges[edgeIdx];
                var inputPort = edge.outputPort;
                int outputEdgeIdx = edge.inputEdgeIndex; 
                if (inputPort.owner.TryGetOutputValue<T>(inputPort.index, out var val, outputEdgeIdx))
				{
                    field = TypeAdapter.Convert<T, T2>(val);
					return true;
                }
            }
            return false;
        }

        public void OnProcess()
		{
			ExceptionToLog.Call(() => Process());

			InvokeOnProcessed();
		}

		public void InvokeOnProcessed() => onProcessed?.Invoke();

		/// <summary>
		/// Called when the node is enabled
		/// </summary>
		protected virtual void Enable() {}
		/// <summary>
		/// Called when the node is disabled
		/// </summary>
		protected virtual void Disable() {}
		/// <summary>
		/// Called when the node is removed
		/// </summary>
		protected virtual void Destroy() {}

		/// <summary>
		/// Override this method to implement custom processing
		/// </summary>
		protected virtual void Process() {}

		#endregion

		#region API and utils

        protected virtual IEnumerable<PortData> GetCustomInputPorts() { yield break; }
        protected virtual IEnumerable<PortData> GetCustomOutputPorts() { yield break; }

        protected void AddPort(bool input, string fieldName, Type displayType, string displayName = null, bool vertical = false, bool allowMultiple = false, string tooltip = null)
		{
			if (string.IsNullOrEmpty(displayName))
				displayName = fieldName;
			PortData portData = new PortData
			{
				acceptMultipleEdges = allowMultiple,
				displayName = displayName,
				displayType = displayType,
				tooltip = tooltip,
				vertical = vertical,
				identifier = -1
			};
			AddPort(input, portData);
        }

		protected PortData BuildCustomPort(string fieldName, Type displayType, string displayName = null, bool isField = true, bool allowMultiple = false, string tooltip = null, int sizeInPixel = 0, bool vertical = false)
		{
			if (string.IsNullOrEmpty(displayName))
				displayName = fieldName;
			//Guard the invalid combination
			if (isField && string.IsNullOrEmpty(fieldName))
				isField = false;
			PortData portData = new PortData
			{
				isField = isField,
				acceptMultipleEdges = allowMultiple,
				fieldName = fieldName,
				displayName = displayName,
				displayType = displayType,
				tooltip = tooltip,
				vertical = vertical,
				sizeInPixel = sizeInPixel,
				identifier = -1
			};
			return portData;
		}

        /// <summary>
        /// Add a port
        /// </summary>
        /// <param name="input">is input port</param>
        /// <param name="fieldName">C# field name</param>
        /// <param name="portData">Data of the port</param>
        protected void AddPort(bool input, PortData portData)
		{
			var fieldName = portData.fieldName;
			// Fixup port data info if needed:
			if (portData.displayType == null)
			{
				var ft = nodeFields[fieldName].info.FieldType;
                portData.displayType = ft;
			}

			if (input)
				inputPorts.Add(new NodePort(this, fieldName, portData, inputPorts.Count, true));
			else
				outputPorts.Add(new NodePort(this, fieldName, portData, outputPorts.Count, false));
		}

		/// <summary>
		/// Remove a port
		/// </summary>
		/// <param name="input">is input port</param>
		/// <param name="port">the port to delete</param>
		public void RemovePort(bool input, NodePort port)
		{
			if (input)
				inputPorts.Remove(port);
			else
				outputPorts.Remove(port);
		}

		/// <summary>
		/// Remove port(s) from field name
		/// </summary>
		/// <param name="input">is input</param>
		/// <param name="fieldName">C# field name</param>
		public void RemovePort(bool input, string fieldName)
		{
			if (input)
				inputPorts.RemoveAll(p => p.fieldName == fieldName);
			else
				outputPorts.RemoveAll(p => p.fieldName == fieldName);
		}

		/// <summary>
		/// Get all the nodes connected to the specified input port
		/// </summary>
		/// <param name="portId"></param>
		/// <returns></returns>
        public IEnumerable<BaseNode> GetInputNodes(int portId)
        {
			if (portId < inputPorts.Count)
			{
				var port = inputPorts[portId];
				foreach (var edge in port.GetEdges())
					yield return edge.outputNode;
			}
        }

        /// <summary>
        /// Get all the nodes connected to the input ports of this node
        /// </summary>
        /// <returns>an enumerable of node</returns>
        public IEnumerable< BaseNode > GetInputNodes()
		{
			foreach (var port in inputPorts)
				foreach (var edge in port.GetEdges())
					yield return edge.outputNode;
		}

		/// <summary>
		/// Get all the nodes connected to the specified output port
		/// </summary>
		/// <param name="portId"></param>
		/// <returns></returns>
		public IEnumerable<BaseNode> GetOutputNodes(int portId)
		{
			if (portId < outputPorts.Count)
			{
				var port = outputPorts[portId];
				foreach (var edge in port.GetEdges())
					yield return edge.inputNode;
			}
        }

        /// <summary>
        /// Get all the nodes connected to the output ports of this node
        /// </summary>
        /// <returns>an enumerable of node</returns>
        public IEnumerable< BaseNode > GetOutputNodes()
		{
			foreach (var port in outputPorts)
				foreach (var edge in port.GetEdges())
					yield return edge.inputNode;
		}

		/// <summary>
		/// Return a node matching the condition in the dependencies of the node
		/// </summary>
		/// <param name="condition">Condition to choose the node</param>
		/// <returns>Matched node or null</returns>
		public BaseNode FindInDependencies(Func<BaseNode, bool> condition)
		{
			Stack<BaseNode> dependencies = new Stack<BaseNode>();

			dependencies.Push(this);

			int depth = 0;
			while (dependencies.Count > 0)
			{
				var node = dependencies.Pop();

				// Guard for infinite loop (faster than a HashSet based solution)
				depth++;
				if (depth > 2000)
					break;

				if (condition(node))
					return node;
				
				foreach (var dep in node.GetInputNodes())
					dependencies.Push(dep);
			}
			return null;
		}

		/// <summary>
		/// Get the port from field name and identifier
		/// </summary>
		/// <param name="fieldName">C# field name</param>
		/// <param name="identifier">Unique port identifier</param>
		/// <returns></returns>
		public NodePort	GetPort(string fieldName, int identifier = -1)
		{
			return inputPorts.Concat(outputPorts).FirstOrDefault(p => {
				var bothNull = identifier == -1 && p.portData.identifier == -1;
				return p.fieldName == fieldName && (bothNull || identifier == p.portData.identifier);
			});
		}

		/// <summary>
		/// Return all the ports of the node
		/// </summary>
		/// <returns></returns>
		public IEnumerable<NodePort> GetAllPorts()
		{
			foreach (var port in inputPorts)
				yield return port;
			foreach (var port in outputPorts)
				yield return port;
		}

		/// <summary>
		/// Return all the connected edges of the node
		/// </summary>
		/// <returns></returns>
		public IEnumerable<SerializableEdge> GetAllEdges()
		{
			foreach (var port in GetAllPorts())
				foreach (var edge in port.GetEdges())
					yield return edge;
		}

		public int GetEdgeCount(bool input, int portId)
		{
			NodePortContainer container = input?inputPorts : outputPorts;
			if (portId < container.Count)
			{
				return container[portId].GetEdges().Count;
			}
			return 0;
		}

		public List<SerializableEdge> GetAllEdgesForPort(bool input, int portId)
		{
			NodePortContainer container = input ? inputPorts : outputPorts;
			if (portId < container.Count)
			{
				return container[portId].GetEdges();
			}
			return null;
		}

        public SerializableEdge GetEdge(bool input, int portId, int edgeId = 0)
		{
            NodePortContainer container = input ? inputPorts : outputPorts;
            if (portId < container.Count)
            {
                var edges = container[portId].GetEdges();
				if (edgeId < edges.Count)
					return edges[edgeId];
            }
			return null;
        }

        /// <summary>
        /// Is the port an input
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public bool IsFieldInput(string fieldName) => nodeFields[fieldName].input;

		/// <summary>
		/// Add a message on the node
		/// </summary>
		/// <param name="message"></param>
		/// <param name="messageType"></param>
		public void AddMessage(string message, NodeMessageType messageType)
		{
			if (messages.Contains(message))
				return;

			onMessageAdded?.Invoke(message, messageType);
			messages.Add(message);
		}

		/// <summary>
		/// Remove a message on the node
		/// </summary>
		/// <param name="message"></param>
		public void RemoveMessage(string message)
		{
			onMessageRemoved?.Invoke(message);
			messages.Remove(message);
		}

		/// <summary>
		/// Remove a message that contains
		/// </summary>
		/// <param name="subMessage"></param>
		public void RemoveMessageContains(string subMessage)
		{
			string toRemove = messages.Find(m => m.Contains(subMessage));
			messages.Remove(toRemove);
			onMessageRemoved?.Invoke(toRemove);
		}

		/// <summary>
		/// Remove all messages on the node
		/// </summary>
		public void ClearMessages()
		{
			foreach (var message in messages)
				onMessageRemoved?.Invoke(message);
			messages.Clear();
		}

		/// <summary>
		/// Set the custom name of the node. This is intended to be used by renamable nodes.
		/// This custom name will be serialized inside the node.
		/// </summary>
		/// <param name="customNodeName">New name of the node.</param>
		public void SetCustomName(string customName) => nodeCustomName = customName;

		/// <summary>
		/// Get the name of the node. If the node have a custom name (set using the UI by double clicking on the node title) then it will return this name first, otherwise it returns the value of the name field.
		/// </summary>
		/// <returns>The name of the node as written in the title</returns>
		public string GetCustomName() => String.IsNullOrEmpty(nodeCustomName) ? name : nodeCustomName; 

		#endregion
	}
}
