using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Gets the Camera parameters in Redis and set them to the camera of which the script is attached
/// </summary>
public class CameraSetup : MonoBehaviour {

	public ApplicationParameters.CameraType Type = ApplicationParameters.CameraType.RGB;
	public string IntrinsicsKey = "camera0:calibration";
	public bool HasExtrinsics = false;
	public string ExtrinsicsKey = "";

	private Camera camera;
	// Use this for initialization
	void Start () {
		camera = this.GetComponent<Camera> ();

		if (!RedisConnectionHandler.Instance.IsConnected) {
			RedisConnectionHandler.Instance.TryConnection ();
			if (!RedisConnectionHandler.Instance.IsConnected) {
				Debug.LogError ("Could not connect to redis server. Exiting..");
#if UNITY_EDITOR
				UnityEditor.EditorApplication.isPlaying = false;
#else
				Application.Quit ();
#endif
			}
		}

		IntrinsicsParameters intrinsics = Utils.RedisTryGetIntrinsics (RedisConnectionHandler.Instance.redis, IntrinsicsKey);
		ExtrinsicsParameters extrinsics = HasExtrinsics ? Utils.RedisTryGetExtrinsics (RedisConnectionHandler.Instance.redis, ExtrinsicsKey) : null;
		if (intrinsics != null) {
			if (Type == ApplicationParameters.CameraType.RGB) {
				if (ApplicationParameters.RGBCameraIntrinsics != null) {
					Debug.LogError("Only one RGB Camera is supported for now.");
					Destroy(this.gameObject);
					return;
				}
				ApplicationParameters.RGBCameraIntrinsics = intrinsics;
				ApplicationParameters.RGBCameraAvailable = true;
				Debug.Log ("Successfully loaded RGB camera parameters.");
			} 
			else if (Type == ApplicationParameters.CameraType.DEPTH) {
				if (ApplicationParameters.DepthCameraIntrinsics != null) {
					Debug.LogError("Only one DEPTH Camera is supported for now.");
					Destroy(this.gameObject);
					return;
				}
				ApplicationParameters.DepthCameraIntrinsics = intrinsics;
				ApplicationParameters.DepthCameraExtrinsics = extrinsics;
				ApplicationParameters.DepthCameraAvailable = true;
				Debug.Log ("Successfully loaded DEPTH camera parameters.");
			}
			else if (Type == ApplicationParameters.CameraType.PROJECTOR) {
				if (ApplicationParameters.ProjectorIntrinsics != null) {
					Debug.LogError("Only one PROJECTOR is supported for now.");
					Destroy(this.gameObject);
					return;
				}
				ApplicationParameters.ProjectorIntrinsics = intrinsics;
				ApplicationParameters.ProjectorExtrinsics = extrinsics;
				ApplicationParameters.ProjectorAvailable = true;
				Debug.Log("Succesfully loaded PROJECTOR camera parameters.");
			}
			else {
				Debug.LogError("Could not load " + Type.ToString() + " device. Not supported yet.");
				Destroy(this.gameObject);
				return;
			}

			SetupIntrinsics (intrinsics);
			if (extrinsics != null) {
				SetupExtrinsics(extrinsics);
			}
		}
	}

	void Update () { }

	void SetupIntrinsics (IntrinsicsParameters intrinsics) {
		float dx = intrinsics.cx - intrinsics.width / 2;
		float dy = intrinsics.cy - intrinsics.height / 2;

		float near = camera.nearClipPlane;
		float far = camera.farClipPlane;

		Matrix4x4 projectionMatrix = new Matrix4x4 ();

		Vector4 row0 = new Vector4 ((2f * intrinsics.fx / intrinsics.width), 0, (2f * dx / intrinsics.width), 0);
		Vector4 row1 = new Vector4 (0, 2f * intrinsics.fy / intrinsics.height, -2f * (dy + 1f) / intrinsics.height, 0);
		Vector4 row2 = new Vector4 (0, 0, -(far + near) / (far - near), -near * (1 + (far + near) / (far - near)));
		Vector4 row3 = new Vector4 (0, 0, -1, 0);

		projectionMatrix.SetRow (0, row0);
		projectionMatrix.SetRow (1, row1);
		projectionMatrix.SetRow (2, row2);
		projectionMatrix.SetRow (3, row3);
		Debug.Log("fx: " + intrinsics.fx);
		Debug.Log("fy: " + intrinsics.fy);
		
		camera.projectionMatrix = projectionMatrix;
	}

	void SetupExtrinsics(ExtrinsicsParameters extrinsics) {
		Matrix4x4 transform = new Matrix4x4 ();
		Debug.Log("Extriniscs: " + extrinsics.matrix[0]);
		transform.SetRow(0, new Vector4(extrinsics.matrix[0], extrinsics.matrix[1], extrinsics.matrix[2], extrinsics.matrix[3]));
		transform.SetRow(1, new Vector4(extrinsics.matrix[4], extrinsics.matrix[5], extrinsics.matrix[6], extrinsics.matrix[7]));
		transform.SetRow(2, new Vector4(extrinsics.matrix[8], extrinsics.matrix[9], extrinsics.matrix[10], extrinsics.matrix[11]));
		transform.SetRow(3, new Vector4(extrinsics.matrix[12], extrinsics.matrix[13], extrinsics.matrix[14], extrinsics.matrix[15]));

		this.transform.localPosition = Utils.ExtractTranslation ((Matrix4x4) transform);
		this.transform.localRotation = Utils.ExtractRotation ((Matrix4x4) transform);
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(CameraSetup))]
public class CameraSetupEditor : Editor 
{
	override public void OnInspectorGUI()
	{
		var cameraSetup = target as CameraSetup;
		cameraSetup.IntrinsicsKey = EditorGUILayout.TextField("Intrinsics Key:", cameraSetup.IntrinsicsKey);
		cameraSetup.Type = (ApplicationParameters.CameraType)EditorGUILayout.EnumPopup("Camera Type:", cameraSetup.Type);
		cameraSetup.HasExtrinsics = cameraSetup.Type != ApplicationParameters.CameraType.RGB ? true : false;

		if (cameraSetup.HasExtrinsics) {
			cameraSetup.ExtrinsicsKey = EditorGUILayout.TextField("Extrinsics Key:", cameraSetup.ExtrinsicsKey);
		}
	}

}
#endif
