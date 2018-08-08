using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TeamDev.Redis;

[ExecuteInEditMode]
public class TableSetup : MonoBehaviour {
	public ComponentState State = ComponentState.DISCONNECTED;
	public Camera ARCamera;
	public string Key = "table:position";

	public bool GetTableTexture = false;
	public string TextureKey = "camera0:view1";

	private string className;
	private QuickCameraSetup ARCameraSetup;
	private RedisConnection connection;

	private Matrix4x4 currentTransform;
	private Matrix4x4 pose;

	private bool isConnected = false;

	void Start () {
		className = transform.gameObject.name;
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
		if (ARCamera == null) {
			Utils.Log(className, "Camera component needs to be attached to this script. Please add a camera in the correct field and try again.");
			return;
		}

		ARCameraSetup = ARCamera.GetComponent<QuickCameraSetup> ();
		if (ARCameraSetup == null) {
			Utils.Log(className, "Attached camera should contain the QuickCameraSetup script. Add the scripts to your camera and start again.");
			return;
		}
		
		if (ARCameraSetup.State != ComponentState.WORKING) {
			Utils.Log (className, "Failed to initialize " + this.GetType ().Name + ". Attached camera is not working.");
			return;
		}

		if (!SetupExtrinsics()) {
			Utils.Log (className, "Failed to setup extrinsincs parameters.");
			return;
		}

		if (GetTableTexture) {
			if (!SetupTexture()) {
				Utils.Log(className, "Failed to set table texture while required.");
				return;
			}
			Utils.Log(className, "Successfully setup table texture.");
		}

		Utils.Log (className, "Successfully initialized " + this.GetType ().Name + ".");
		State = ComponentState.WORKING;
	}

	bool SetupTexture() {
		RedisDataAccessProvider redis = connection.GetDataAccessProvider ();
		int commandId = redis.SendCommand (RedisCommand.GET, ARCameraSetup.BaseKey + ":" + TextureKey + ":width");
		int? width = Utils.RedisTryReadInt(redis, commandId);
		if (width == null) {
			Utils.Log(className, "Width null during texture setup.");
			return false;
		}

		commandId = redis.SendCommand (RedisCommand.GET, ARCameraSetup.BaseKey + ":" + TextureKey + ":height");
		int? height = Utils.RedisTryReadInt(redis, commandId);
		if (height == null) {
			Utils.Log(className, "Height null during texture setup.");
			return false;
		}
		commandId = redis.SendCommand (RedisCommand.GET, ARCameraSetup.BaseKey + ":" + TextureKey + ":channels");
		int? channels = Utils.RedisTryReadInt(redis, commandId);
		if (channels == null) {
			Utils.Log(className, "Channels null during texture setup.");
			return false;
		}

		commandId = redis.SendCommand (RedisCommand.GET, ARCameraSetup.BaseKey + ":" + TextureKey);
		byte[] imageData = new byte[(int)width * (int)height * (int)channels];
		imageData = Utils.RedisTryReadData (redis, commandId);
		Texture2D tableTexture = new Texture2D((int)width, (int)height, TextureFormat.RGB24, false);
		tableTexture.LoadRawTextureData (imageData);
		tableTexture.Apply();

		this.GetComponent<Renderer>().sharedMaterial.mainTexture = tableTexture;
		return true;
	}

	bool SetupExtrinsics () {
		ExtrinsicsParameters ExtrinsicsParameters = Utils.RedisTryGetExtrinsics (connection.GetDataAccessProvider (), ARCameraSetup.BaseKey + ":" + Key);
		if (ExtrinsicsParameters == null) {
			Utils.Log (className, "Failed to load (and set) table position.");
			State = ComponentState.CONNECTED;
			return false;
		}

		currentTransform = new Matrix4x4 ();
		currentTransform.SetRow (0, new Vector4 (-ExtrinsicsParameters.matrix[0], 	ExtrinsicsParameters.matrix[1], 	0 * ExtrinsicsParameters.matrix[2], ExtrinsicsParameters.matrix[3]));
		currentTransform.SetRow (1, new Vector4 (ExtrinsicsParameters.matrix[4], 	ExtrinsicsParameters.matrix[5], 	ExtrinsicsParameters.matrix[6], ExtrinsicsParameters.matrix[7]));
		currentTransform.SetRow (2, new Vector4 (0 * ExtrinsicsParameters.matrix[8], ExtrinsicsParameters.matrix[9], 	-ExtrinsicsParameters.matrix[10], ExtrinsicsParameters.matrix[11]));
		currentTransform.SetRow (3, new Vector4 (ExtrinsicsParameters.matrix[12], 	ExtrinsicsParameters.matrix[13], 	ExtrinsicsParameters.matrix[14], ExtrinsicsParameters.matrix[15]));
	
		this.transform.localRotation = Utils.ExtractRotation (currentTransform);
		this.transform.localPosition = Utils.ExtractTranslation (currentTransform);

		Utils.Log (className, "Successfully loaded and setup table position.");
		State = ComponentState.WORKING;
		return true;
	}

	// Update is called once per frame
	void Update () {
		if (State != ComponentState.WORKING) {
			if (State != ComponentState.CONNECTED) {
				Utils.Log (className, "Retrying to connect to the redis server.");
				Connect ();
			} else {
				Utils.Log (className, "Retrying to initialize sheet following.");
				Initialize ();
			}
			return;
		}
	}
}
