using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ComponentController : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}

[CustomEditor(typeof(ComponentController))]
public class ComponentControllerEditor : Editor 
{
    public override void OnInspectorGUI() {
		
	}
}