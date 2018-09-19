using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class SetAxes : MonoBehaviour {
	[Tooltip("This is the gameobject onto which the world will be centered.")]
	public GameObject baseObject;
	[Tooltip("When set to true transformation will be performed every update loop. This will cause the base object to ALWAYS be at the center of the world even when moving it.")]
	public bool KeepCentered = false;

	private string objectName;
	private bool transformationUpdated = false;

	// Use this for initialization
	void Start () {
		Init();
	}

	public void Init() {
		objectName = transform.gameObject.name;
		UpdateTransformation();
	}

	public void UpdateTransformation() {
		if (baseObject == null) {
				Utils.Log(objectName, "Please provide the game object that should be replaced at the origin");
				transformationUpdated = false;
				return;
			}

			Matrix4x4 baseMat = Matrix4x4.TRS(baseObject.transform.localPosition, baseObject.transform.localRotation, Vector3.one);
			
			// Reversing object transform to set it at the center of the world
			Matrix4x4 inverseMatrix = baseMat.inverse;
			// Flipping everything by 90 degre to have up matching the Y axis
			Matrix4x4 rotateYZ = Matrix4x4.Rotate(Quaternion.Euler(90, 0, 0));
			inverseMatrix = rotateYZ * inverseMatrix ;
			
			this.transform.position = Utils.ExtractTranslation (inverseMatrix);			
			this.transform.rotation = Utils.ExtractRotation (inverseMatrix);
			this.transform.localScale = Utils.ExtractScale (inverseMatrix);
			transformationUpdated = true;
	}
	
	// Update is called once per frame
	void Update () {
		if (!transformationUpdated || KeepCentered) {
			UpdateTransformation();
		}
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(SetAxes))]
public class SetAxesEditor : Editor 
{
    public override void OnInspectorGUI()
    {
		base.OnInspectorGUI();
        SetAxes script = (SetAxes)target;

		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Restart")) {
			script.Init();
		}

		if (GUILayout.Button("Update")) {
			script.UpdateTransformation();
		}
		
		GUILayout.EndHorizontal();
    }
}
#endif