using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components;
using VRC.SDKBase;
using VRC.Udon.Common;

public class VoxelPlayer : UdonSharpBehaviour
{
	[SerializeField] private AnimationCurve curve;

	[SerializeField] private VRCUrlInputField urlInputField;
	[SerializeField] private Transform urlInputFieldCanvas;

	[SerializeField] private Transform hand;
	[SerializeField] private Transform order;//is child of hand
	[SerializeField] private Canvas canvas;//canvas must be in world space
	[SerializeField] private Image coursor;//is child of canvas
	[SerializeField] private Image progressBar;//is child of canvas
	[SerializeField] private Slider progressSlider;//is child of canvas

	[SerializeField] private RawImage voxelView;
	[SerializeField] private RawImage simpleView;

	#region animation
	//coursor animation
	private bool coursorOrient = true;
	[SerializeField] private Sprite[] coursorGhostRight;
	[SerializeField] private Sprite[] coursorGhostLeft;

	//progress bar animation
	[SerializeField] private Sprite[] pacman;
	#endregion
	[SerializeField] private VRCUnityVideoPlayer videoPlayer;
	private Vector2 posInCanvas = new Vector2(0.5f, 0.5f);
	private float oldCoursorX = 0;
	private HandType handType = HandType.RIGHT;
	private bool UIActive = false;
	private float UIstate = 0;
	private bool voxelMode = true;
	private float voxelState = 1;
	private bool isCoursorInCanvas = false;

	[SerializeField] private Transform playButton;
	[SerializeField] private Transform pauseButton;
	[SerializeField] private Transform stopButton;
	[SerializeField] private Transform linkButton;
	[SerializeField] private Transform nextButton;
	[SerializeField] private Transform prevButton;
	[SerializeField] private Transform loopButton;
	[SerializeField] private Transform shuffleButton;
	[SerializeField] private Transform settingsButton;

	private VRCUrl[] playlist = new VRCUrl[]
	{
		new VRCUrl("https://youtu.be/6pAYuJHiZEc?si=SJ8kIs-tjXYUrfyh")
	};

	private void Start()
	{
		if (!Networking.LocalPlayer.IsUserInVR())
		{
			order.localPosition = Vector3.forward;
			urlInputFieldCanvas.transform.localPosition = Vector3.forward * 0.08f;
			urlInputFieldCanvas.transform.localRotation = Quaternion.Euler(0, 0, 0);
		}
	}

	private void Update()
	{
		if (stopButton.gameObject.activeSelf)
			progressSlider.value = 120 - (float)(videoPlayer.GetTime() / videoPlayer.GetDuration()) * 120;
		else
			progressSlider.value = 120;

		if (pauseButton.gameObject.activeSelf)
			progressBar.sprite = pacman[(int)(Time.time * 10) % pacman.Length];

		UIstate += (UIActive ? 1 : -1) * Time.deltaTime;
		UIstate = Mathf.Clamp01(UIstate);
		voxelState += (voxelMode ? 1 : -1) * Time.deltaTime;
		voxelState = Mathf.Clamp01(voxelState);

		if (!Networking.LocalPlayer.IsUserInVR()) coursor.color = new Color(1, 1, 1, UIstate);
		voxelView.color = new Color(1, 1, 1, voxelState * ((1 - UIstate) * 0.9f + (stopButton.gameObject.activeSelf ? 0.1f : 0)));
		simpleView.color = new Color(1, 1, 1, (1 - voxelState) * ((1 - UIstate) * 0.9f + (stopButton.gameObject.activeSelf ? 0.1f : 0)));

		/*urlInputField.transform.localPosition = linkButton.transform.localPosition + linkButton.parent.localPosition;
		urlInputField.gameObject.SetActive(UIActive && linkButton.gameObject.activeSelf);*/


		if (!CoursorToCanvas()) return;
	}

	private bool CoursorToCanvas()
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

		Plane plane = new Plane(canvas.transform.forward, canvas.transform.position);
		Ray ray = new Ray(hand.position, order.position - hand.position);

		if (plane.Raycast(ray, out float distance))
		{
			Vector3 hitPoint = ray.GetPoint(distance);
			Vector3 localHit = canvas.transform.InverseTransformPoint(hitPoint);
			Rect rect = ((RectTransform)canvas.transform).rect;

			posInCanvas.x = Mathf.InverseLerp(rect.xMin, rect.xMax, localHit.x);
			posInCanvas.y = Mathf.InverseLerp(rect.yMin, rect.yMax, localHit.y);

			if (posInCanvas.x == 0 || posInCanvas.x == 1 || posInCanvas.y == 0 || posInCanvas.y == 1)
			{
				coursor.gameObject.SetActive(false);
				urlInputFieldCanvas.gameObject.SetActive(false);
				isCoursorInCanvas = false;
				return false;
			}

			coursor.gameObject.SetActive(true);
			isCoursorInCanvas = true;

			Vector3 newLocalPos = new Vector3(
				Mathf.Lerp(rect.xMin, rect.xMax, posInCanvas.x),
				Mathf.Lerp(rect.yMin, rect.yMax, posInCanvas.y),
				0
			);

			coursor.transform.localPosition = newLocalPos;

			if (IsCursorOverRect((RectTransform)linkButton))
			{
				urlInputFieldCanvas.gameObject.SetActive(UIActive && linkButton.gameObject.activeSelf);
			}
			else
			{
				urlInputFieldCanvas.gameObject.SetActive(false);
			}

			if (coursor.gameObject.activeSelf)
			{
				if (oldCoursorX < coursor.transform.localPosition.x - 0.1)
				{
					coursorOrient = true;
					oldCoursorX = coursor.transform.localPosition.x;
				}
				else if (oldCoursorX > coursor.transform.localPosition.x + 0.1)
				{
					coursorOrient = false;
					oldCoursorX = coursor.transform.localPosition.x;
				}
				coursor.sprite = coursorOrient ? coursorGhostRight[(int)(Time.time * 4) & 1] : coursorGhostLeft[(int)(Time.time * 4) & 1];
			}

			return true;
		}

		coursor.gameObject.SetActive(false);
		isCoursorInCanvas = false;
		return false;
	}

	public override void InputUse(bool value, UdonInputEventArgs args)
	{
		if (value) return;

		if (args.handType != handType)
		{
			handType = args.handType;
			return;
		}

		if (!isCoursorInCanvas) return;

		//if coursor local position is hit link button return
		if (IsCursorOverRect((RectTransform)linkButton))
		{
			return;
		}

		if (IsCursorOverRect((RectTransform)playButton))
		{
			if (stopButton.gameObject.activeSelf)
			{
				videoPlayer.Play();
				playButton.gameObject.SetActive(false);
				pauseButton.gameObject.SetActive(true);
			}
			else
			{
				videoPlayer.PlayURL(playlist[playlist.Length - 1]);
				playButton.gameObject.SetActive(false);
				linkButton.gameObject.SetActive(false);
			}
			return;
		}

		else if (IsCursorOverRect((RectTransform)pauseButton))
		{
			videoPlayer.Pause();
			playButton.gameObject.SetActive(true);
			pauseButton.gameObject.SetActive(false);
			return;
		}

		else if (IsCursorOverRect((RectTransform)stopButton))
		{
			videoPlayer.Stop();
			playButton.gameObject.SetActive(true);
			pauseButton.gameObject.SetActive(false);
			stopButton.gameObject.SetActive(false);
			return;
		}

		UIActive = (!UIActive || !stopButton.gameObject.activeSelf);
	}

	public void OnInputUrlChanged()
	{
		Debug.LogWarning("characterValidation:" + urlInputField.characterValidation.ToString());
		videoPlayer.Stop();
		videoPlayer.PlayURL(urlInputField.GetUrl());
		urlInputField.textComponent.text = "";


		//try to add url to playlist
		int index = System.Array.IndexOf(playlist, urlInputField.GetUrl());
		if (index == -1)
		{
			var newPlaylist = new VRCUrl[playlist.Length + 1];
			playlist.CopyTo(newPlaylist, 0);
			newPlaylist[playlist.Length] = urlInputField.GetUrl();
			playlist = newPlaylist;
		}
		else//shuffle to top
		{
			for (int i = index; i > 0; i--)
			{
				var temp = playlist[i];
				playlist[i] = playlist[i - 1];
				playlist[i - 1] = temp;
			}
		}

		playButton.gameObject.SetActive(false);
		pauseButton.gameObject.SetActive(false);
		stopButton.gameObject.SetActive(false);
		linkButton.gameObject.SetActive(false);
	}

	public override void OnVideoReady()
	{
		UIActive = false;
		playButton.gameObject.SetActive(false);
		pauseButton.gameObject.SetActive(true);
		stopButton.gameObject.SetActive(true);
		linkButton.gameObject.SetActive(true);
	}

	public override void OnVideoError(VideoError videoError)
	{
		linkButton.gameObject.SetActive(true);
		Debug.LogError($"[VoxelPlayer] Video Error: {videoError}");
	}

	private bool IsCursorOverRect(RectTransform targetRect)
	{
		if (!UIActive) return false;
		if (!targetRect.gameObject.activeSelf) return false;
		Vector3[] corners = new Vector3[4];
		targetRect.GetLocalCorners(corners);
		for (int i = 0; i < corners.Length; i++)
		{
			corners[i] += targetRect.parent.localPosition;
		}

		Rect rect = new Rect(corners[0].x, corners[0].y,
			corners[2].x - corners[0].x,
			corners[2].y - corners[0].y);

		return rect.Contains((Vector2)(coursor.transform.localPosition) - (Vector2)targetRect.localPosition);
	}

}
