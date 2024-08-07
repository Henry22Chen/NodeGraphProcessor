using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System;

public class RuntimeGraph : MonoBehaviour
{
	public BaseGraph	graph;
	[NonSerialized]
	public BaseGraph runtimeGraph;
	public ProcessGraphProcessor	processor;

	public GameObject	assignedGameObject;

	private void Start()
	{
		if (graph != null)
		{
			runtimeGraph = Instantiate(graph);
			runtimeGraph.hideFlags = HideFlags.HideAndDontSave;
			processor = new ProcessGraphProcessor(runtimeGraph);
		}
	}

	int i = 0;

    void Update()
    {
		if (runtimeGraph != null)
		{
            runtimeGraph.SetParameterValue("Input", (float)i++);
			runtimeGraph.SetParameterValue("GameObject", assignedGameObject);
			processor.Run();
			Debug.Log("Output: " + runtimeGraph.GetParameterValue("Output"));
		}
    }
}
