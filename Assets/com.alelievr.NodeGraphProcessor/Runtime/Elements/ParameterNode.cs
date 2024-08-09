using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System.Linq;
using System;

namespace GraphProcessor
{
	[System.Serializable]
	public class ParameterNode : BaseNode
	{
		[Output]
		public object output;

		public override string name => "Parameter";

		// We serialize the GUID of the exposed parameter in the graph so we can retrieve the true ExposedParameter from the graph
		[SerializeField, HideInInspector]
		public string parameterGUID;

		public ExposedParameter parameter { get; private set; }

		public event Action onParameterChanged;

		public ParameterAccessor accessor;

		protected override bool hasCustomInputs => true;
		protected override bool hasCustomOutputs => true;

		protected override void Enable()
		{
			// load the parameter
			LoadExposedParameter();

			graph.onExposedParameterModified += OnParamChanged;
			if (onParameterChanged != null)
				onParameterChanged?.Invoke();
		}

		void LoadExposedParameter()
		{
			parameter = graph.GetExposedParameterFromGUID(parameterGUID);

			if (parameter == null)
			{
				Debug.Log("Property \"" + parameterGUID + "\" Can't be found !");

				// Delete this node as the property can't be found
				graph.RemoveNode(this);
				return;
			}

			output = parameter.value;
		}

		void OnParamChanged(ExposedParameter modifiedParam)
		{
			if (parameter == modifiedParam)
			{
				onParameterChanged?.Invoke();
			}
		}

		protected override IEnumerable<PortData> GetCustomOutputPorts()
		{
			if (accessor == ParameterAccessor.Get)
			{
				yield return BuildCustomPort(nameof(output), (parameter == null) ? typeof(object) : parameter.GetValueType(), "Value", true, true);
			}
		}

		protected override IEnumerable<PortData> GetCustomInputPorts()
		{
			if (accessor == ParameterAccessor.Set)
			{
				yield return BuildCustomPort(null, (parameter == null) ? typeof(object) : parameter.GetValueType(), "Value", false);
			}
		}

		protected override bool TryGetOutputValue<T>(int index, out T value, int edgeIdx)
		{
			return TryConvertValue(ref output, out value);
		}

		protected override void Process()
		{
#if UNITY_EDITOR // In the editor, an undo/redo can change the parameter instance in the graph, in this case the field in this class will point to the wrong parameter
			parameter = graph.GetExposedParameterFromGUID(parameterGUID);
#endif

			ClearMessages();
			if (parameter == null)
			{
				AddMessage($"Parameter not found: {parameterGUID}", NodeMessageType.Error);
				return;
			}

			if (accessor == ParameterAccessor.Get)
				output = parameter.value;
			else
			{
				object input = null;
				if (TryReadInputValue(0, ref input))
					graph.UpdateExposedParameter(parameter.guid, input);
			}
		}
	}

	public enum ParameterAccessor
	{
		Get,
		Set
	}
}
