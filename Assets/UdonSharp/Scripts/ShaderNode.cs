using UdonSharp;
using VRC.Udon.Common;

public class ShaderNode : UdonSharpBehaviour
{
	private NodeCreator[] creators;
	private UToggle[] toggles;

	private void Start()
	{
		creators = GetComponentsInChildren<NodeCreator>();
		toggles = GetComponentsInChildren<UToggle>();
	}

	private void Update()
	{

	}

	public override void InputUse(bool value, UdonInputEventArgs args)
	{
		if (value)
		{

		}
	}
}