using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SetAxes : MonoBehaviour {

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

			/*
			TableSetup table = baseObject.GetComponent<TableSetup>();
			if (table == null) {
				Utils.Log(className, "TODO");
				return;
			}
			*/
			
			// Mega cheat 
			this.transform.position = baseObject.transform.localPosition;
			this.transform.localRotation = Quaternion.Inverse(baseObject.transform.localRotation);
			this.transform.position = -1  * baseObject.transform.position + baseObject.transform.localPosition;
			
			/*
			Quaternion rotation = table.GetWorldRotation();
			Matrix4x4 mat = table.GetTransformMatrix();
			//Utils.Log(className, "Received matrix: " + table.GetTransformMatrix().ToString());
			//Utils.Log(className, "Inversed matrix: " + tInverse.ToString());
			//Quaternion rotation = Utils.ExtractRotation(tInverse);
			Vector3 position = Utils.ExtractTranslation(mat);
			//mat = mat.transpose;
			//Quaternion rotation = Utils.ExtractRotation(mat);

			this.gameObject.transform.localPosition = -position;
			this.gameObject.transform.rotation = Quaternion.Inverse(rotation);
			*/
		}
	}
}
