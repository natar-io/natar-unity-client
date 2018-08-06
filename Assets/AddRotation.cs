using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class AddRotation : MonoBehaviour {

	public GameObject baseObject;
	public bool forceUpdate = false;

	private string className = "undefined";
	private bool axesSet = false;
	// Use this for initialization
	void Start () {
		className = transform.gameObject.name;
	}
	
	// Update is called once per frame
	void Update () {
		if (forceUpdate || !axesSet) {
			if (baseObject == null) {
				Utils.Log(className, "Please provide the game object that should be replaced at the origin");
				return;
			}

			Quaternion rotation = baseObject.transform.localRotation;
			Utils.Log(className, rotation.ToString());
			this.transform.rotation = Quaternion.Inverse(rotation);
			axesSet = true;
		}
	}
}
