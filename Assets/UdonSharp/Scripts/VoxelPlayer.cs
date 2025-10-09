using UdonSharp;
using UnityEngine;

public class VoxelPlayer : UdonSharpBehaviour
{
	[SerializeField] private SpriteRenderer renderer;
	[SerializeField] private Material image;

	private void Update()
	{
		image.SetTexture("_SMainTex", renderer.sprite.texture);
	}
}
