using System;
using System.Collections;
using UnityEngine;

using TeamDev.Redis;

namespace Natar
{
	[ExecuteInEditMode]
	public class SetupExtrinsics : MonoBehaviour {

		private RedisHandler rHandler;
		private RedisDataAccessProvider redis;
		private Subscriber redisSubscriber;

		public string Key = "extrinsics";
		public bool LiveUpdate = false;
		public bool ReverseYAxis = false;

		private ExtrinsicsParameters eParameters;
		private bool extrinsicsNeedsUpdate = false;

		public ServiceStatus state = ServiceStatus.DISCONNECTED;

		private delegate void OnServiceConnectionStateChangedHandler(bool connected);
		private event OnServiceConnectionStateChangedHandler ServiceConnectionStateChanged;

		public void Start() {
			this.state = ServiceStatus.DISCONNECTED;

			rHandler = RedisHandler.Instance;
			rHandler.ConnectionStatusChanged += OnRedisHandlerConnectionStateChanged;
			rHandler.ConnectionStatusNotification += OnRedisHandlerConnectionNotificationStatus;
			rHandler.NewService("extrinsics");
			ServiceConnectionStateChanged += OnServiceConnectionStateChanged;
		}

		public void Update() {
			#if UNITY_EDITOR
			if (!CheckScriptCurrentState()) { this.Start(); }
			#endif
			if (extrinsicsNeedsUpdate) {
				applyExtrinsics(eParameters);
				extrinsicsNeedsUpdate = false;
			}
		}

	#region event

		public void OnRedisHandlerConnectionNotificationStatus(bool handlerConnected) {
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
			
		private void OnExtrinsicsReceived(string channelName, byte[] message) {
			if (channelName == "unsub") {
				redisSubscriber.Unsubscribe(Key, "unsub");
			}

			string extrinsics = Utils.ByteToString(message);
			if (extrinsics == "") return;
			eParameters = Utils.JSONTo<ExtrinsicsParameters>(extrinsics);
			extrinsicsNeedsUpdate = true;
			//applyExtrinsics(parameters);
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

		private ExtrinsicsParameters load() {
			if (redis == null) {
				return null;
			}
			return Utils.RedisTryGetExtrinsics(redis, Key);
		}

		public void init() {
			if (LiveUpdate) {
				if (redisSubscriber != null) {
					redisSubscriber.Unsubscribe(Key, "unsub");
				}
				redisSubscriber = new Subscriber(redis);
				redisSubscriber.Subscribe(OnExtrinsicsReceived, Key, "unsub");
				this.state = ServiceStatus.WORKING;
			}
			else {
				eParameters = load();
				if (applyExtrinsics(eParameters)) {
					this.state = ServiceStatus.WORKING;
				}
				else {
					this.state = ServiceStatus.CONNECTED;
				}
			}
		}

		private void kill() {
			if (redisSubscriber != null) {
				redisSubscriber.Unsubscribe(Key, "unsub");
				redisSubscriber = null;
			}
		}

	#endregion
		private Matrix4x4 currentTR;

		public bool applyExtrinsics(ExtrinsicsParameters extrinsics) {
			if (extrinsics == null) { return false; }
			Matrix4x4? transRot = new Matrix4x4();
			transRot = Utils.FloatArrayToMatrix4x4(extrinsics.matrix);
			if (transRot == null) { return false; }

			if (ReverseYAxis) {
				Matrix4x4 scale = Matrix4x4.Scale(new Vector3(1, -1, 1));
				transRot = scale * transRot;
			}

			transform.localPosition = Utils.ExtractTranslation((Matrix4x4)transRot);
			transform.localRotation = Utils.ExtractRotation((Matrix4x4)transRot);
			this.currentTR = (Matrix4x4) transRot;
			return true;
		}

		public Matrix4x4 GetTransformationMatrix() {
			return this.currentTR;
		}

		public bool CheckScriptCurrentState() {
			return this.rHandler != null && 
					this.redis != null &&
					this.redisSubscriber != null;
		}
	}
}