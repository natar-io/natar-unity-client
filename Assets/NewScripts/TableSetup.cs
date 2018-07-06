﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TeamDev.Redis;

[ExecuteInEditMode]
public class TableSetup : MonoBehaviour {
	public ComponentState State = ComponentState.DISCONNECTED;
	public Camera ARCamera;
	public string Key = ":table:position";

	private string className;
	private QuickCameraSetup ARCameraSetup;
	private RedisDataAccessProvider redis;
	private RedisConnection connection;

	private Matrix4x4 pose;

	private bool isConnected = false;

	void Start () {
		className = transform.gameObject.name;
		ARCameraSetup = ARCamera.GetComponent<QuickCameraSetup> ();
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
		if (ARCameraSetup.State != ComponentState.WORKING) {
			Utils.Log (className, "Failed to initialize " + this.GetType ().Name + ". Attached camera is not working.");
			return;
		}

		redis = connection.GetDataAccessProvider ();
		if (SetupExtrinsics()) {
			Utils.Log (className, "Successfully initialized " + this.GetType ().Name + ".");
			State = ComponentState.WORKING;
		}
	}

	bool SetupExtrinsics () {
		ExtrinsicsParameters ExtrinsicsParameters = Utils.RedisTryGetExtrinsics (connection.GetDataAccessProvider (), ARCameraSetup.BaseKey + Key);
		if (ExtrinsicsParameters == null) {
			Utils.Log (className, "Failed to load (and set) table position.");
			State = ComponentState.CONNECTED;
			return false;
		}

		Matrix4x4 transform = new Matrix4x4 ();
		transform.SetRow (0, new Vector4 (ExtrinsicsParameters.matrix[0], ExtrinsicsParameters.matrix[1], ExtrinsicsParameters.matrix[2], ExtrinsicsParameters.matrix[3]));
		transform.SetRow (1, new Vector4 (ExtrinsicsParameters.matrix[4], ExtrinsicsParameters.matrix[5], ExtrinsicsParameters.matrix[6], ExtrinsicsParameters.matrix[7]));
		transform.SetRow (2, new Vector4 (ExtrinsicsParameters.matrix[8], ExtrinsicsParameters.matrix[9], ExtrinsicsParameters.matrix[10], ExtrinsicsParameters.matrix[11]));
		transform.SetRow (3, new Vector4 (ExtrinsicsParameters.matrix[12], ExtrinsicsParameters.matrix[13], ExtrinsicsParameters.matrix[14], ExtrinsicsParameters.matrix[15]));

		this.transform.localPosition = Utils.ExtractTranslation ((Matrix4x4) transform);
		this.transform.localRotation = Utils.ExtractRotation ((Matrix4x4) transform);

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