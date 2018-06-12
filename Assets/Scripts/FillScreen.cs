using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FillScreen : MonoBehaviour {

	public bool shouldFill = false;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (shouldFill)
		{
			Camera cam = Camera.main;
			float pos = (cam.nearClipPlane + 0.01f);
			transform.position = cam.transform.position + cam.transform.forward * pos;
			float h = Mathf.Tan(cam.fieldOfView*Mathf.Deg2Rad*0.5f)*pos*2f; 
			transform.localScale = new Vector3(h*cam.aspect,h,0f);
		}
	}
}
