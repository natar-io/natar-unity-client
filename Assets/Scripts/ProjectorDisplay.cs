using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ProjectorDisplay : MonoBehaviour {

	public int displayNumber = -1;

	// Use this for initialization
	void Start () {
		Debug.Log("Number of connected display(s): " + Display.displays.Length);
		displayNumber = Display.displays.Length;
		if (Display.displays.Length > 1)
            Display.displays[1].Activate();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
