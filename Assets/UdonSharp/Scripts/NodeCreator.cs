using UdonSharp;
using UnityEngine;

public class NodeCreator : UdonSharpBehaviour
{
	[SerializeField] private string[] inputs;
	[SerializeField] private string[] outputs;
	[SerializeField] private string[] parameters;
	[SerializeField] private ParametersType[] parametersTypes;

}

public enum ParametersType
{
	Float
}
