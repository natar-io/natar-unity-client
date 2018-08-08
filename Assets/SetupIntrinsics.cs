using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class SetupIntrinsics : MonoBehaviour {
	[Tooltip("The redis key where to look for intrinsics parameters.")]
	public string Key = "intrisics";

	private RedisConnection connection;
	private bool isConnected = false;

	private string objectName;
	
    private Camera camera;
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
		bool intrinsicsUpdated = SetIntrinsics();
		Utils.Log(objectName, "Intrinsics parameters initialization " + (intrinsicsUpdated ? "succeed" : "failed") + ".");
		state = intrinsicsUpdated ? ComponentState.WORKING : ComponentState.CONNECTED;
	}

	bool SetIntrinsics() {
		IntrinsicsParameters intrinsicsParameters = Utils.RedisTryGetIntrinsics(connection.GetDataAccessProvider(), Key);
		if (intrinsicsParameters == null) {
			return false;
		}
        
		float near = camera.nearClipPlane;
		float far = camera.farClipPlane;

		Matrix4x4 projectionMatrix = new Matrix4x4 ();
		Vector4 row0 = new Vector4 ((2f * intrinsicsParameters.fx / intrinsicsParameters.width), 0f, -((float) intrinsicsParameters.cx / (float) intrinsicsParameters.width * 2f - 1f), 0f);
		Vector4 row1 = new Vector4 (0f, 2f * intrinsicsParameters.fy / intrinsicsParameters.height, -((float) intrinsicsParameters.cy / (float) intrinsicsParameters.height * 2f - 1f),0f);
		Vector4 row2 = new Vector4 (0, 0, -(far + near) / (far - near), -near * (1 + (far + near) / (far - near)));
		Vector4 row3 = new Vector4 (0, 0, -1, 0);

		projectionMatrix.SetRow (0, row0);
		projectionMatrix.SetRow (1, row1);
		projectionMatrix.SetRow (2, row2);
		projectionMatrix.SetRow (3, row3);
		camera.projectionMatrix = projectionMatrix;

		state = ComponentState.WORKING;
		return true;
	}
	
	// Update is called once per frame
	void Update () {
        switch (state) {
            case ComponentState.DISCONNECTED:
                Connect();
                break;
            case ComponentState.CONNECTED:
                Initialize();
                break;
            default:
                break;
        }
	}
}
