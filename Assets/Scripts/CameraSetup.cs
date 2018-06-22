using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gets the Camera parameters in Redis and set them to the camera of which the script is attached
/// </summary>
public class CameraSetup : MonoBehaviour {

	public int f = 920;
	public string CameraParameterKey = "nectar:jiii-mi:calibration:astra-s-rgb";

	private new Camera camera;

	// Use this for initialization
	void Start () {
		camera = GetComponent<Camera> ();

		if (!RedisConnectionHandler.Instance.isConnected) {
			RedisConnectionHandler.Instance.TryConnection ();
			if (!RedisConnectionHandler.Instance.isConnected) {
				Debug.LogError ("Could not connect to redis server. Exiting..");
#if UNITY_EDITOR
				UnityEditor.EditorApplication.isPlaying = false;
#else
				Application.Quit ();
#endif
			}
		}

		CameraParameters camParams = Utils.RedisTryGetCameraParameters (RedisConnectionHandler.Instance.redis, CameraParameterKey);
		if (camParams != null) {
			ApplicationParameters.camParams = camParams;
			SetupCamera (camParams);
		}
	}

	void Update () {
		SetupCamera (ApplicationParameters.camParams);
	}

	void SetupCamera (CameraParameters camParams) {
		float dx = camParams.cx - camParams.width / 2;
		float dy = camParams.cy - camParams.height / 2;

		float near = camera.nearClipPlane;
		float far = camera.farClipPlane;

		Matrix4x4 projectionMatrix = new Matrix4x4 ();

		Vector4 row0 = new Vector4 ((2f * f / camParams.width), 0, (2f * dx / camParams.width), 0);
		Vector4 row1 = new Vector4 (0, 2f * f / camParams.height, -2f * (dy + 1f) / camParams.height, 0);
		Vector4 row2 = new Vector4 (0, 0, -(far + near) / (far - near), -near * (1 + (far + near) / (far - near)));
		Vector4 row3 = new Vector4 (0, 0, -1, 0);

		projectionMatrix.SetRow (0, row0);
		projectionMatrix.SetRow (1, row1);
		projectionMatrix.SetRow (2, row2);
		projectionMatrix.SetRow (3, row3);

		camera.projectionMatrix = projectionMatrix;
	}
}