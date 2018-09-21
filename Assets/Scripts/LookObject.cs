using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class LookObject : MonoBehaviour {

	public GameObject Object;
	public bool KeepLooking = false;

	private GameObject objectCheck;

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
		if (Object == null) {
			return;
		}
		
		if (KeepLooking || objectCheck != Object) {
			this.transform.LookAt(Object.transform.position);
			objectCheck = Object;
		}
	}
}
