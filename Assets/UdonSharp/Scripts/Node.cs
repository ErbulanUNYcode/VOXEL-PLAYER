using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class Node : UdonSharpBehaviour
{
	[SerializeField] private AnimationCurve tCorrector;

	public int id;

	private Transform coordinator;
	[SerializeField]
	private RectTransform delete;
	[SerializeField]
	private RectTransform copy;
	[SerializeField]
	private RectTransform view;

	public RectTransform Delete => delete;
	public RectTransform Copy => copy;
	public RectTransform View => view;

	public RectTransform[] Outputs { get; private set; }
	public RectTransform[] Inputs { get; set; }

	public LineRenderer[] InputLines { get; set; }
	public Node[] InputNodes { get; set; }
	public int[] InputNodeStartIndex { get; set; }

	public LineRenderer[] OutputLines { get; private set; } = new LineRenderer[1];
	public Node[] OutputNodes { get; private set; } = new Node[1];
	public int[] OutputNodeStartIndexes { get; private set; } = new int[1];
	public int[] OutputNodeEndIndexes { get; private set; } = new int[1];

	private void OnDestroy()
	{
		for (int i = 0; i < OutputLines.Length; i++)
		{
			if (OutputLines[i] != null)
			{
				Destroy(OutputLines[i].gameObject);
			}
		}

		for (int i = 0; i < InputLines.Length; i++)
		{
			if (InputLines[i] != null)
			{
				Destroy(InputLines[i].gameObject);
			}
		}
	}

	public void UpdateLines()
	{
		for (int i = 0; i < InputLines.Length; i++)
		{
			if (InputLines[i] != null)
			{
				UpdateLine(InputLines[i], InputNodes[i].Outputs[InputNodeStartIndex[i]], Inputs[i]);
			}
		}

		for (int i = 0; i < OutputLines.Length; i++)
		{
			if (OutputLines[i] != null)
			{
				UpdateLine(OutputLines[i], Outputs[OutputNodeStartIndexes[i]], OutputNodes[i].Inputs[OutputNodeEndIndexes[i]]);
			}
		}
	}

	private void UpdateLine(LineRenderer _line, RectTransform _start, RectTransform _end)
	{
		coordinator.position = _start.position;
		var start = coordinator.localPosition;
		coordinator.position = _end.position;
		var end = coordinator.localPosition;

		start.z = 0;
		end.z = 0;

		var distance = Mathf.Min(Vector3.Distance(start, end) / 3, 150);
		var startTurnPos = start + Vector3.right * distance;
		var endTurnPos = end - Vector3.right * distance;
		for (int i = 0; i < _line.positionCount; i++)
		{
			float t = i / (float)(_line.positionCount - 1);
			t = tCorrector.Evaluate(t);
			Vector3 pointOnCurve = Mathf.Pow(1 - t, 3) * start +
				3 * Mathf.Pow(1 - t, 2) * t * startTurnPos +
				3 * (1 - t) * Mathf.Pow(t, 2) * endTurnPos +
				Mathf.Pow(t, 3) * end;
			_line.SetPosition(i, pointOnCurve);
		}
	}

	public void AddOutput(Node node, int startIndex, int endIndex, LineRenderer line)
	{
		int oldLen = OutputNodes.Length;

		for (int i = 0; i < oldLen; i++)
		{
			if (OutputLines[i] == null)
			{
				OutputLines[i] = line;
				OutputNodes[i] = node;
				OutputNodeStartIndexes[i] = startIndex;
				OutputNodeEndIndexes[i] = endIndex;
				return;
			}
		}

		var ols = OutputLines;
		var ons = OutputNodes;
		var osns = OutputNodeStartIndexes;
		var onens = OutputNodeEndIndexes;

		OutputLines = new LineRenderer[oldLen * 2];
		OutputNodes = new Node[oldLen * 2];
		OutputNodeStartIndexes = new int[oldLen * 2];
		OutputNodeEndIndexes = new int[oldLen * 2];

		Array.Copy(ols, OutputLines, oldLen);
		Array.Copy(ons, OutputNodes, oldLen);
		Array.Copy(osns, OutputNodeStartIndexes, oldLen);
		Array.Copy(onens, OutputNodeEndIndexes, oldLen);

		Debug.Log(OutputLines.Length + "/" + oldLen);
		OutputLines[oldLen] = line;
		OutputNodes[oldLen] = node;
		OutputNodeStartIndexes[oldLen] = startIndex;
		OutputNodeEndIndexes[oldLen] = endIndex;
	}

	[SerializeField] private TextMeshProUGUI _name;
	[SerializeField] private TextMeshProUGUI _outputText;
	[SerializeField] private GameObject _output;
	[SerializeField] private Transform _outputsParent;
	[SerializeField] private TextMeshProUGUI _inputText;
	[SerializeField] private GameObject _input;
	[SerializeField] private Transform _inputsParent;
	[SerializeField] private Image viewer;

	public NodeCreator creator { get; private set; }

	public void UpdateViewer()
	{
		if (!viewer.gameObject.activeSelf) return;
		viewer.canvasRenderer.SetMaterial(viewer.material, null);
	}

	public Node Create(NodeCreator data, Transform _coordinator)
	{
		coordinator = _coordinator;
		creator = data;
		_name.text = data.name;
		id = data.id;
		Outputs = new RectTransform[data.outputs.Length];
		for (var i = 0; i < data.outputs.Length; i++)
		{
			var output = data.outputs[i];
			_outputText.text = output;
			Outputs[i] = Instantiate(_output, _outputsParent).transform.GetChild(1).GetComponent<RectTransform>();
		}
		Destroy(_output);
		Inputs = new RectTransform[data.inputs.Length];
		InputNodes = new Node[data.inputs.Length];
		InputNodeStartIndex = new int[data.inputs.Length];
		InputLines = new LineRenderer[data.inputs.Length];
		for (int i = 0; i < data.inputs.Length; i++)
		{
			var input = data.inputs[i];
			_inputText.text = input;
			var inp = Instantiate(_input, _inputsParent).transform.GetChild(0).GetChild(0);
			Inputs[i] = inp.transform.parent.GetComponent<RectTransform>();
			switch (data.inputsTypes[i])
			{
				case InputType.Float:
					Destroy(inp.GetChild(2).gameObject);
					Destroy(inp.GetChild(3).gameObject);
					Destroy(inp.GetChild(4).gameObject);
					Destroy(inp.GetChild(5).gameObject);
					Destroy(inp.GetChild(6).gameObject);
					Destroy(inp.GetChild(7).gameObject);
					break;
				case InputType.Float2:
					Destroy(inp.GetChild(4).gameObject);
					Destroy(inp.GetChild(5).gameObject);
					Destroy(inp.GetChild(6).gameObject);
					Destroy(inp.GetChild(7).gameObject);
					break;
				case InputType.Float3:
					Destroy(inp.GetChild(6).gameObject);
					Destroy(inp.GetChild(7).gameObject);
					break;
				case InputType.Float4:
					break;
			}
		}
		Destroy(_input);

		return this;
	}


}
