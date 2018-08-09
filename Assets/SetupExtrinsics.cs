using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: Find a proper way to deal with parent key -> needed in redis

[ExecuteInEditMode]
public class SetupExtrinsics : MonoBehaviour {
	[Tooltip("The redis key where to look for extrinsics parameters.")]
	public string Key = "extrinsics";
	[Tooltip("If set to true, object extrinsics parameters are going to be updated in every update loop. This is used for tracked object where the pose 3d is constantly changing.")]
	public bool KeepTracking = false;
	[Tooltip("If set to true, a scale of -1 in Y will be applied to the object")]
	public bool ReverseY = true;
	
	private RedisConnection connection;
	private bool isConnected = false;

	private string objectName;
	
	private ComponentState state = ComponentState.DISCONNECTED;

	// Use this for initialization
	void Start () {
		objectName = transform.gameObject.name;
		connection = new RedisConnection();
		Connect();
	}

	void Connect() {
		if (connection == null) {
			connection = new RedisConnection();
			Connect();
		}
		isConnected = connection.TryConnection();
		state = isConnected ? ComponentState.CONNECTED : ComponentState.DISCONNECTED;
		Utils.Log(objectName, (isConnected ? "Redis connection succeed." : "Redis connection failed."));
		if (isConnected)
			Initialize();
	}

	void Initialize() {
		bool extrinsicsUpdated = UpdateExtrinsics();
		Utils.Log(objectName, "Extrinsics parameters initialization " + (extrinsicsUpdated ? "succeed" : "failed") + ".");
		state = extrinsicsUpdated ? ComponentState.WORKING : ComponentState.CONNECTED;
	}

	bool UpdateExtrinsics() {
		ExtrinsicsParameters extrinsicsParameters = Utils.RedisTryGetExtrinsics(connection.GetDataAccessProvider(), Key);
		if (extrinsicsParameters == null) {
			Utils.Log(objectName, "Failed to updated object extrinsics parameters.");
			return false;
		}

		Matrix4x4 transform = new Matrix4x4();
		transform.SetRow(0, new Vector4(extrinsicsParameters.matrix[0],		extrinsicsParameters.matrix[1],		extrinsicsParameters.matrix[2],		extrinsicsParameters.matrix[3]));
		transform.SetRow(1, new Vector4(extrinsicsParameters.matrix[4],		extrinsicsParameters.matrix[5],		extrinsicsParameters.matrix[6],		extrinsicsParameters.matrix[7]));
		transform.SetRow(2, new Vector4(extrinsicsParameters.matrix[8],		extrinsicsParameters.matrix[9],		extrinsicsParameters.matrix[10],	extrinsicsParameters.matrix[11]));
		transform.SetRow(3, new Vector4(extrinsicsParameters.matrix[12],	extrinsicsParameters.matrix[13],	extrinsicsParameters.matrix[14],	extrinsicsParameters.matrix[15]));

		if (ReverseY) {
			Matrix4x4 scale = Matrix4x4.Scale(new Vector3(1, -1, 1));
			transform = scale * transform;
		}

		this.transform.localPosition = Utils.ExtractTranslation((Matrix4x4)transform);
		this.transform.localRotation = Utils.ExtractRotation((Matrix4x4)transform);

		Utils.Log(objectName, "Successfully updated object extrinsics parameters.");
		state = ComponentState.WORKING;
		return true;
	}
	
	// Update is called once per frame
	void Update () {
		if (!KeepTracking && state == ComponentState.WORKING) {
			return;
		}

		switch(state) {
			case ComponentState.DISCONNECTED:
				Connect();
				break;
			case ComponentState.CONNECTED:
				Initialize();
				break;
			case ComponentState.WORKING:
				UpdateExtrinsics();
				break;
			default:
				break;
		}
	}
}
