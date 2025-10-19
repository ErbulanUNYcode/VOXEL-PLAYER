using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon.Common;

public class ShaderNode : UdonSharpBehaviour
{
	[SerializeField] private RectTransform colorPoint;
	[SerializeField] private RectTransform heightPoint;
	[SerializeField] private Transform coursor;
	[SerializeField] private LineRenderer laser;
	[SerializeField] private Transform coordinator;
	[SerializeField] private Transform hand;
	[SerializeField] private Transform order;
	[SerializeField] private Canvas canvas;
	[SerializeField] private Transform nodesParent;
	[SerializeField] private UToggle openCreator;
	[SerializeField] private Transform creator;
	[SerializeField] private GameObject NodePrefab;
	[SerializeField] private Node[] nodes = new Node[1];
	[SerializeField] private Material shaderNodeMat;
	private RectTransform[] childs;
	private HandType handType = HandType.RIGHT;
	private Vector2 posInCanvas;

	private NodeCreator[] creators;
	private UToggle[] toggles;

	private void Start()
	{
		creators = GetComponentsInChildren<NodeCreator>(true);
		toggles = GetComponentsInChildren<UToggle>(true);
		if (!Networking.LocalPlayer.IsUserInVR()) order.localPosition = Vector3.forward;

		childs = new RectTransform[canvas.transform.childCount];
		for (int i = 0; i < canvas.transform.childCount; i++) childs[i] = canvas.transform.GetChild(i).GetComponent<RectTransform>();
	}
	private float[] data = new float[512];
	private void LoadData()
	{
		data[0] = Time.time;
		data[511] = Time.time;
		shaderNodeMat.SetFloatArray("_Data", data);
		if (nodesParent.gameObject.activeSelf)
			foreach (var n in nodes)
			{
				if (n != null)
				{
					n.UpdateViewer();
				}
			}
	}
	private void Update()
	{
		CoursorToCanvas();
		//LoadData();
	}

	private void CoursorToCanvas()
	{
		VRCPlayerApi.TrackingDataType type =
			!Networking.LocalPlayer.IsUserInVR() ?
				VRCPlayerApi.TrackingDataType.Head
			: handType == HandType.RIGHT ?
				VRCPlayerApi.TrackingDataType.RightHand
			:
				VRCPlayerApi.TrackingDataType.LeftHand;

		var data = Networking.LocalPlayer.GetTrackingData(type);
		hand.SetPositionAndRotation(data.position, data.rotation);

		//if hand position is behind the canvas, hide the coursor and laser
		if (Vector3.Dot(canvas.transform.forward, hand.position - canvas.transform.position) > 0)
		{
			Grab(false, handType);
			coursor.gameObject.SetActive(false);
			laser.enabled = false;
			if (lastNodeFuncs != null)
			{
				lastNodeFuncs.IsOn = false;
				lastNodeFuncs = null;
			}
			return;
		}

		Plane plane = new Plane(canvas.transform.forward, canvas.transform.position);
		Ray ray = new Ray(hand.position, order.position - hand.position);

		if (plane.Raycast(ray, out float distance))
		{
			Vector3 hitPoint = ray.GetPoint(distance);
			Vector3 localHit = canvas.transform.InverseTransformPoint(hitPoint);

			var oldPos = coursor.transform.position;
			var oldLocalPos = coursor.transform.localPosition;
			coursor.transform.localPosition = localHit;
			if (grabPlain)
			{
				var delta = coursor.transform.position - oldPos;
				canvas.transform.position += delta;
				coursor.transform.localPosition = oldLocalPos;
			}

			if (Vector3.SqrMagnitude(coursor.position - hand.position) > 0.5)
			{
				Grab(false, handType);
				coursor.gameObject.SetActive(false);
				laser.enabled = false;
				if (lastNodeFuncs != null)
				{
					lastNodeFuncs.IsOn = false;
					lastNodeFuncs = null;
				}
				return;
			}
			coursor.gameObject.SetActive(true);

			if (Networking.LocalPlayer.IsUserInVR())
			{
				laser.enabled = true;
				laser.SetPosition(1, Vector3.right * Vector3.Distance(hand.position, coursor.position));
			}
			return;
		}

		coursor.gameObject.SetActive(false);
		laser.enabled = false;
	}

	public override void InputDrop(bool value, UdonInputEventArgs args)
	{
		if (!value) return;
		if (Networking.LocalPlayer.IsUserInVR()) return;
		Click(args.handType);
	}
	public override void InputUse(bool value, UdonInputEventArgs args)
	{
		if (Networking.LocalPlayer.IsUserInVR())
		{
			if (!value) return;
			Click(args.handType);
		}
		else
			Grab(value, args.handType);
	}
	public override void InputGrab(bool value, UdonInputEventArgs args)
	{
		if (Networking.LocalPlayer.IsUserInVR())
			Grab(value, args.handType);
	}
	private void Click(HandType _handType)
	{
		if (grabPlain || coursor.childCount == 1 || hand.transform.childCount == 3) return;

		if (Networking.LocalPlayer.IsUserInVR() && handType != _handType)
		{
			handType = _handType;
			return;
		}

		if (!coursor.gameObject.activeSelf) return;

		foreach (var toggle in toggles)
		{
			if (IsCursorOverRect(toggle.GetComponent<RectTransform>()))
			{
				toggle.IsOn = !toggle.IsOn;
				if (lastNodeFuncs != null)
				{
					lastNodeFuncs.IsOn = false;
					lastNodeFuncs = null;
				}
				return;
			}
		}

		foreach (var creator in creators)
		{
			if (IsCursorOverRect(creator.GetComponent<RectTransform>()))
			{
				AddNode(creator, openCreator.transform.position);
				openCreator.IsOn = false;
				return;
			}
		}

		if (!nodesParent.gameObject.activeSelf) return;

		if (DontTouchUI())
		{
			openCreator.IsOn = false;
			openCreator.IsOn = true;
			creator.localPosition = coursor.transform.localPosition;
		}
		else
		{
			openCreator.IsOn = false;
		}
	}

	private bool grabPlain = false;
	private void Grab(bool value, HandType _handType)
	{
		if (lastNodeFuncs != null)
		{
			lastNodeFuncs.IsOn = false;
			lastNodeFuncs = null;
		}
		if (openCreator.IsOn) openCreator.IsOn = false;
		if (!value && handType != _handType) return;

		if (Networking.LocalPlayer.IsUserInVR() && handType != _handType)
		{
			handType = _handType;
			if (grabPlain)
			{
				grabPlain = false;
			}
			CoursorToCanvas();
			if (coursor.gameObject.activeSelf)
				grabPlain = true;
			return;
		}

		if (!coursor.gameObject.activeSelf) return;

		if (!value)
		{
			if (canvas.transform.parent == hand)
			{
				var worldPos = canvas.transform.position;
				var worldRot = canvas.transform.rotation;
				canvas.transform.SetParent(null);
				canvas.transform.position = worldPos;
				canvas.transform.rotation = worldRot;
				return;
			}
			if (coursor.childCount > 0)
			{
				var n = coursor.GetChild(0);
				n.SetParent(nodesParent);
				return;
			}
			grabPlain = false;
			return;
		}

		if (IsCursorOverRect(toggles[0].GetComponent<RectTransform>()))
		{
			canvas.transform.SetParent(hand);
			return;
		}

		for (int i = nodesParent.childCount; i > 0; i--)
		{
			var n = nodesParent.GetChild(i - 1).GetComponent<RectTransform>();
			if (IsCursorOverRect(n))
			{
				n.transform.SetParent(coursor);
				return;
			}
		}

		grabPlain = true;
	}

	private void AddNode(NodeCreator data, Vector3 pos)
	{
		for (int i = 0; i < nodes.Length; i++)
		{
			if (nodes[i] == null)
			{
				nodes[i] = Instantiate(NodePrefab, nodesParent).GetComponent<Node>().Create(data);
				nodes[i].transform.position = pos;
				pos = nodes[i].transform.localPosition;
				pos.z = 0;
				nodes[i].transform.localPosition = pos;
				return;
			}
		}

		var ns = nodes;
		nodes = new Node[nodes.Length * 2];
		Array.Copy(ns, nodes, ns.Length);

		nodes[ns.Length] = Instantiate(NodePrefab, nodesParent).GetComponent<Node>().Create(data);
		nodes[ns.Length].transform.position = pos;
		pos = nodes[ns.Length].transform.localPosition;
		pos.z = 0;
		nodes[ns.Length].transform.localPosition = pos;
	}

	private UToggle lastNodeFuncs;
	private bool DontTouchUI()
	{
		foreach (RectTransform rect in childs)
		{
			var go = rect.gameObject;
			if (!go.activeSelf) continue;
			if (go == coursor.gameObject) continue;
			if (IsCursorOverRect(rect)) return false;
		}

		for (int i = nodesParent.childCount; i > 0; i--)
		{
			var n = nodesParent.GetChild(i - 1).GetComponent<Node>();
			if (n.GetComponent<UToggle>().IsOn)
			{
				if (IsCursorOverRect(n.delete))
				{
					Destroy(n.gameObject);
					return false;
				}

				if (IsCursorOverRect(n.copy))
				{
					AddNode(n.creator, n.copy.position);
					n.GetComponent<UToggle>().IsOn = false;
					return false;
				}

				n.GetComponent<UToggle>().IsOn = false;
			}
			if (IsCursorOverRect(n.GetComponent<RectTransform>()))
			{
				n.transform.SetAsLastSibling();
				if (IsCursorOverRect(n.view))
				{
					var toggle = n.view.GetComponent<UToggle>();
					toggle.IsOn = !toggle.IsOn;
					return false;
				}
				lastNodeFuncs = n.GetComponent<UToggle>();
				lastNodeFuncs.IsOn = true;
				LayoutRebuilder.ForceRebuildLayoutImmediate(n.GetComponent<RectTransform>());
				n.delete.parent.position = coursor.position;
				return false;
			}
		}

		return true;
	}

	private bool IsCursorOverRect(RectTransform targetRect)
	{
		if (!targetRect.gameObject.activeSelf) return false;
		Vector3[] corners = new Vector3[4];
		targetRect.GetLocalCorners(corners);

		coordinator.position = targetRect.parent.position;
		var pos = coordinator.localPosition;
		pos.z = 0;

		for (int i = 0; i < corners.Length; i++)
		{
			corners[i] += pos;
		}

		Rect rect = new Rect(corners[0].x, corners[0].y,
				corners[2].x - corners[0].x,
				corners[2].y - corners[0].y);

		return rect.Contains((Vector2)(coursor.transform.localPosition) - (Vector2)targetRect.localPosition);
	}

	private void UpdateData()
	{

	}

	private Node[] sNodes;
	private int[] dNodes;
}