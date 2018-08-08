using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SetAxes : MonoBehaviour {
	[Tooltip("This is the gameobject onto which the world will be centered.")]
	public GameObject baseObject;
	[Tooltip("When set to true new axes will be computed every update call.")]
	public bool ShouldUpdate = false;

	private string objectName;
	private bool axesSet = false;
	// Use this for initialization
	void Start () {
		objectName = transform.gameObject.name;
	}
	
	// Update is called once per frame
	void Update () {
		if (ShouldUpdate || !axesSet) {
			if (baseObject == null) {
				Utils.Log(objectName, "Please provide the game object that should be replaced at the origin");
				return;
			}

			this.transform.position = baseObject.transform.localPosition;
			this.transform.localRotation = Quaternion.Inverse(baseObject.transform.localRotation);
			this.transform.position = -1  * baseObject.transform.position + baseObject.transform.localPosition;
			axesSet = true;
		}
	}
}
