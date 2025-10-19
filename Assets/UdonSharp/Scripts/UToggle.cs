using UdonSharp;
using UnityEngine;

public class UToggle : UdonSharpBehaviour
{
	[SerializeField] private bool _isOn;
	[SerializeField] private GameObject[] objectsToOn;
	[SerializeField] private GameObject[] objectsToOff;
	[SerializeField] private UToggle[] togglesToOff;

	private void Start()
	{
		IsOn = _isOn;
	}

	public bool IsOn
	{
		get { return _isOn; }
		set
		{
			_isOn = value;


			foreach (GameObject obj in objectsToOn)
			{
				if (obj != null) obj.SetActive(value);
			}

			foreach (GameObject obj in objectsToOff)
			{
				if (obj != null) obj.SetActive(!value);
			}

			if (!_isOn)
			{
				foreach (UToggle toggle in togglesToOff)
				{
					if (toggle != null)
						toggle.IsOn = false;
				}
			}
		}
	}
}
