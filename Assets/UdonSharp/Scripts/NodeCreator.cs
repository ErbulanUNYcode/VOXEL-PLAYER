using UdonSharp;

public class NodeCreator : UdonSharpBehaviour
{
	public int id;
	public string[] inputs;
	public InputType[] inputsTypes;
	public string[] outputs;
}
public enum InputType
{
	Float,
	Float2,
	Float3,
	Float4,
}
