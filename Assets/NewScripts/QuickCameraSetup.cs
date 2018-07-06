using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class QuickCameraSetup : MonoBehaviour {
	private string className;

	private RedisConnection connection;
	private bool isConnected = false;

	private Camera setupCamera;

	public ComponentState State = ComponentState.DISCONNECTED;
	public CameraType Type = CameraType.RGB;
	public string BaseKey = "camera0";
	public string IntrinsicsKey = "calibration";
	public bool HasData = true;
	public string DataKey = "";
	public bool HasExtrinsics = false;
	public string ExtrinsicsKey = "extrinsics";

	[HideInInspector]
	public IntrinsicsParameters IntrinsicsParameters;
	public ExtrinsicsParameters ExtrinsicsParameters;

	// Use this for initialization
	void Start () {
		className = transform.gameObject.name;
		setupCamera = this.GetComponent<Camera> ();
		Connect ();
	}

	void Connect () {
		if (connection == null) {
			connection = new RedisConnection ();
		}
		isConnected = connection.TryConnection ();
		Utils.Log (className, (isConnected ? "Connection succeed." : "Connection failed."));
		if (!isConnected) {
			State = ComponentState.DISCONNECTED;
			return;
		}
		State = ComponentState.CONNECTED;
		Initialize ();
	}

	void Initialize () {
		if (!SetupIntrinsics ()) {
			return;
		}
		if (Type != CameraType.RGB) {
			if (!SetupExtrinsics ()) {
				return;
			}
		}
	}

	bool SetupIntrinsics () {
		this.IntrinsicsParameters = Utils.RedisTryGetIntrinsics (connection.GetDataAccessProvider (), BaseKey + ":" + IntrinsicsKey);
		if (this.IntrinsicsParameters == null) {
			Utils.Log (className, "Failed to load (and set) camera intrinsics parameters.");
			State = ComponentState.CONNECTED;
			return false;
		}

		float dx = this.IntrinsicsParameters.cx;
		float dy = this.IntrinsicsParameters.cy;
		float near = setupCamera.nearClipPlane;
		float far = setupCamera.farClipPlane;

		Matrix4x4 projectionMatrix = new Matrix4x4 ();
		Vector4 row0 = new Vector4 ((2f * this.IntrinsicsParameters.fx / this.IntrinsicsParameters.width), 0f, -((float) this.IntrinsicsParameters.cx / (float) this.IntrinsicsParameters.width * 2f - 1f), 0f);
		Vector4 row1 = new Vector4 (0f, 2f * this.IntrinsicsParameters.fy / this.IntrinsicsParameters.height, -((float) this.IntrinsicsParameters.cy / (float) this.IntrinsicsParameters.height * 2f - 1f),0f);
		Vector4 row2 = new Vector4 (0, 0, -(far + near) / (far - near), -near * (1 + (far + near) / (far - near)));
		Vector4 row3 = new Vector4 (0, 0, -1, 0);

		projectionMatrix.SetRow (0, row0);
		projectionMatrix.SetRow (1, row1);
		projectionMatrix.SetRow (2, row2);
		projectionMatrix.SetRow (3, row3);
		setupCamera.projectionMatrix = projectionMatrix;

		Utils.Log (className, "Successfully loaded and setup camera intrinsics parameters.");
		State = ComponentState.WORKING;
		return true;
	}

	bool SetupExtrinsics () {
		this.ExtrinsicsParameters = Utils.RedisTryGetExtrinsics (connection.GetDataAccessProvider (), BaseKey + ":" + ExtrinsicsKey);
		if (this.ExtrinsicsParameters == null) {
			Utils.Log (className, "Failed to load (and set) camera extrinsics parameters.");
			State = ComponentState.CONNECTED;
			return false;
		}

		Matrix4x4 transform = new Matrix4x4 ();
		transform.SetRow (0, new Vector4 (this.ExtrinsicsParameters.matrix[0], this.ExtrinsicsParameters.matrix[1], this.ExtrinsicsParameters.matrix[2], this.ExtrinsicsParameters.matrix[3]));
		transform.SetRow (1, new Vector4 (this.ExtrinsicsParameters.matrix[4], this.ExtrinsicsParameters.matrix[5], this.ExtrinsicsParameters.matrix[6], this.ExtrinsicsParameters.matrix[7]));
		transform.SetRow (2, new Vector4 (this.ExtrinsicsParameters.matrix[8], this.ExtrinsicsParameters.matrix[9], this.ExtrinsicsParameters.matrix[10], this.ExtrinsicsParameters.matrix[11]));
		transform.SetRow (3, new Vector4 (this.ExtrinsicsParameters.matrix[12], this.ExtrinsicsParameters.matrix[13], this.ExtrinsicsParameters.matrix[14], this.ExtrinsicsParameters.matrix[15]));

		this.transform.localPosition = Utils.ExtractTranslation ((Matrix4x4) transform);
		this.transform.localRotation = Utils.ExtractRotation ((Matrix4x4) transform);

		Utils.Log (className, "Successfully loaded camera and setup camera extrinsics parameters.");
		State = ComponentState.WORKING;
		return true;
	}

	void Update () {
		if (State != ComponentState.WORKING) {
			if (State != ComponentState.CONNECTED) {
				Utils.Log (className, "Retrying to connect to the redis server.");
				Connect ();
			} else {
				Utils.Log (className, "Retrying to initialize camera parameters.");
				Initialize ();
			}
		}
	}
}