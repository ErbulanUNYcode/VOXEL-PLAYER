using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class Node : UdonSharpBehaviour
{
	public int id;
	public RectTransform delete;
	public RectTransform copy;
	public RectTransform view;
	[SerializeField] private TextMeshProUGUI _name;
	[SerializeField] private TextMeshProUGUI _outputText;
	[SerializeField] private GameObject _output;
	[SerializeField] private Transform _outputsParent;
	[SerializeField] private TextMeshProUGUI _inputText;
	[SerializeField] private GameObject _input;
	[SerializeField] private Transform _inputsParent;
	[SerializeField] private Image viewer;

	public NodeCreator creator;

	public void UpdateViewer()
	{
		if (!viewer.gameObject.activeSelf) return;
		viewer.canvasRenderer.SetMaterial(viewer.material, null);
	}

	public Node Create(NodeCreator data)
	{
		creator = data;
		_name.text = data.name;
		id = data.id;
		foreach (var output in data.outputs)
		{
			_outputText.text = output;
			Instantiate(_output, _outputsParent);
		}
		Destroy(_output);
		for (int i = 0; i < data.inputs.Length; i++)
		{
			var input = data.inputs[i];
			_inputText.text = input;
			var inp = Instantiate(_input, _inputsParent).transform.GetChild(0).GetChild(0);
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
