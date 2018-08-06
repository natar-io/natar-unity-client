using System.Collections;
using System.Collections.Generic;
using TeamDev.Redis;
using UnityEngine;

[ExecuteInEditMode]
public class SheetFollower : MonoBehaviour {
	public ComponentState State = ComponentState.DISCONNECTED;
	public Camera ARCamera;
	public string Key = "pose";

	private string className;
	private QuickCameraSetup ARCameraSetup;
	private RedisDataAccessProvider redis;
	private RedisConnection connection;
	private Subscriber subscriber;

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

		redis = connection.GetDataAccessProvider ();
		if (Application.isPlaying) {
			subscriber = new Subscriber (redis);
			subscriber.Subscribe (ARCameraSetup.BaseKey + ":" + this.Key, OnPoseReceived);
		}

		Utils.Log (className, "Successfully initialized " + this.GetType ().Name + ".");
		State = ComponentState.WORKING;
	}

	void OnPoseReceived (string channelName, byte[] message) {
		if (channelName != ARCameraSetup.BaseKey + ":" + Key) {
			return;
		}
		string poseString = Utils.ByteToString (message);
		float[] poseArray = JsonUtility.FromJson<ExtrinsicsParameters> (poseString).matrix;
		pose = Utils.FloatArrayToMatrix4x4(poseArray);
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

		if (!Application.isPlaying && redis != null) {
			int commandId = redis.SendCommand (RedisCommand.GET, ARCameraSetup.BaseKey + ":" + this.Key);
			byte[] poseData = Utils.RedisTryReadData (redis, commandId);
			if (poseData != null) {
				string poseString = Utils.ByteToString (poseData);
				float[] poseArray = JsonUtility.FromJson<ExtrinsicsParameters> (poseString).matrix;
				pose = Utils.FloatArrayToMatrix4x4(poseArray);
				// Reverse y axis
				/*
				pose.m00 = -pose.m00;
				pose.m02 = 0;
				pose.m20 = 0;
				pose.m22 = -pose.m22;
				*/
			}
		}

		if (pose != null) {
			Debug.Log("Updating pose");
			this.transform.localPosition = Utils.ExtractTranslation ((Matrix4x4) pose);
			this.transform.localRotation = Utils.ExtractRotation ((Matrix4x4) pose);
		}
	}
}

/*
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

		if (!Application.isPlaying && redis != null) {
			ExtrinsicsParameters ExtrinsicsParameters = Utils.RedisTryGetExtrinsics(redis, ARCameraSetup.BaseKey + ":" + this.Key);
			if (ExtrinsicsParameters == null) {
				return;
			}
			pose.SetRow (0, new Vector4 (-ExtrinsicsParameters.matrix[0], 	ExtrinsicsParameters.matrix[1], 	0 * ExtrinsicsParameters.matrix[2], ExtrinsicsParameters.matrix[3]));
			pose.SetRow (1, new Vector4 (ExtrinsicsParameters.matrix[4], 	ExtrinsicsParameters.matrix[5], 	ExtrinsicsParameters.matrix[6], ExtrinsicsParameters.matrix[7]));
			pose.SetRow (2, new Vector4 (0 * ExtrinsicsParameters.matrix[8], ExtrinsicsParameters.matrix[9], 	-ExtrinsicsParameters.matrix[10], ExtrinsicsParameters.matrix[11]));
			pose.SetRow (3, new Vector4 (ExtrinsicsParameters.matrix[12], 	ExtrinsicsParameters.matrix[13], 	ExtrinsicsParameters.matrix[14], ExtrinsicsParameters.matrix[15]));
		}

		if (pose != null) {
			Debug.Log("Updating pose");
			this.transform.localRotation = Utils.ExtractRotation ((Matrix4x4) pose);
			this.transform.localPosition = Utils.ExtractTranslation ((Matrix4x4) pose);
		} 
*/