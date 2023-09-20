using System;
using System.Collections;
using UnityEngine;

using TeamDev.Redis;

namespace Natar
{
	[RequireComponent(typeof(Camera))]
	[ExecuteInEditMode]
	public class SetupIntrinsics : MonoBehaviour {

		private RedisHandler rHandler;
		private RedisDataAccessProvider redis;

		public string Key = "intrinsics";

		public ServiceStatus state = ServiceStatus.DISCONNECTED;

		private delegate void OnServiceConnectionStateChangedHandler(bool connected);
		private event OnServiceConnectionStateChangedHandler ServiceConnectionStateChanged;

		private Camera targetCamera;
    public IntrinsicsParameters intrinsicsParameters;

		public void Awake() {
			targetCamera = GetComponent<Camera>();
		}

		public void Start() {

			state = ServiceStatus.DISCONNECTED;

			rHandler = RedisHandler.Instance;
			rHandler.ConnectionStatusChanged += OnRedisHandlerConnectionStateChanged;
			rHandler.ConnectionStatusNotification += OnRedisHandlerConnectionStatusNotification;
			rHandler.NewService("intrinsics");
			
			ServiceConnectionStateChanged += OnServiceConnectionStateChanged;
		}

		public void Update() {
			#if UNITY_EDITOR
			if (!CheckScriptCurrentState()) { this.Start(); }
			#endif
		}

	#region event

		public void OnRedisHandlerConnectionStatusNotification(bool handlerConnected) {
			if (this.state == ServiceStatus.DISCONNECTED && handlerConnected) {
				this.connect();
			}
		}
		
		public void OnRedisHandlerConnectionStateChanged(bool handlerConnected) {
			if (handlerConnected) { this.connect(); }
			else { this.disconnect(); }
		}

		private void OnServiceConnectionStateChanged(bool connected) {
			Debug.Log("[" + transform.gameObject.name + "] Service " + (connected ? "connected" : "disconnected"));
			this.state = connected ? ServiceStatus.CONNECTED : ServiceStatus.DISCONNECTED;

			if (connected) {
				init();
			}
			else {
				kill();
			}
		}

	#endregion

	#region core
	
		private void connect() {
			redis = rHandler.CreateConnection();
			try {
				redis.Connect();
			} catch (Exception) {
				OnServiceConnectionStateChanged(false);
				return;
			}
			OnServiceConnectionStateChanged(true);
		}

		private void disconnect() {
			OnServiceConnectionStateChanged(false);
		}

		private IntrinsicsParameters load() {
			if (redis == null) { return null; }
			return Utils.RedisTryGetIntrinsics(redis, Key);
		}

    public IntrinsicsParameters GetIntrinsicsParameters(){
      return this.intrinsicsParameters;
    }

		public void init() {
			IntrinsicsParameters parameters = load();
      this.intrinsicsParameters = parameters;

      if(parameters != null){
        Debug.Log("Intrinsics: " + parameters.fx + " " + parameters.fy + " "  + parameters.cx + " " + parameters.cy);
			
      } else{
        Debug.Log("No Instrinics on: " + Key);
      }
      
      if (applyIntrinsics(parameters)) {
				this.state = ServiceStatus.WORKING;
			}
			else {
				this.state = ServiceStatus.CONNECTED;
			}
		}

    

		private void kill() {}

	#endregion

		public bool applyIntrinsics(IntrinsicsParameters intrinsics) {
			if (intrinsics == null) { return false; }
			float near = targetCamera.nearClipPlane;
			float far = targetCamera.farClipPlane;

			Matrix4x4 projectionMatrix = new Matrix4x4 ();
			Vector4 row0 = new Vector4 ((2f * intrinsics.fx / intrinsics.width), 0f, -((float) intrinsics.cx / (float) intrinsics.width * 2f - 1f), 0f);
			Vector4 row1 = new Vector4 (0f, 2f * intrinsics.fy / intrinsics.height, -((float) intrinsics.cy / (float) intrinsics.height * 2f - 1f),0f);
			Vector4 row2 = new Vector4 (0, 0, -(far + near) / (far - near), -near * (1 + (far + near) / (far - near)));
			Vector4 row3 = new Vector4 (0, 0, -1, 0);

			projectionMatrix.SetRow (0, row0);
			projectionMatrix.SetRow (1, row1);
			projectionMatrix.SetRow (2, row2);
			projectionMatrix.SetRow (3, row3);
			targetCamera.projectionMatrix = projectionMatrix;

			return true;
		}

		public Matrix4x4 GetProjectionMatrix() {
			return targetCamera.projectionMatrix;
		}


		public bool CheckScriptCurrentState() {
			return this.rHandler != null && 
					this.redis != null;
		}
	}
}
