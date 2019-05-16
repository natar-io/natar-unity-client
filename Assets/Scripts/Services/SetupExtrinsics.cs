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
				bool isInit = init();
			}
			else {
				bool isKilled = kill();
			}
		}
			
		private void OnExtrinsicsReceived(string channelName, byte[] message) {
			if (channelName == "unsub") {
				redisSubscriber.Unsubscribe(Key, "unsub");
			}

			string extrinsics = Utils.ByteToString(message);
			if (extrinsics == "") return;
			ExtrinsicsParameters parameters = Utils.JSONTo<ExtrinsicsParameters>(extrinsics);
		}

	#endregion

	#region core
		private void connect() {
			redis = rHandler.CreateConnection();
			try {
				redis.Connect();
			} catch (Exception) {
				state = ServiceStatus.DISCONNECTED;
				return;
			}
			state = ServiceStatus.CONNECTED;
			OnServiceConnectionStateChanged(true);
		}

		private void disconnect() {
			this.state = ServiceStatus.DISCONNECTED;
			OnServiceConnectionStateChanged(false);
		}

		private ExtrinsicsParameters load() {
			if (redis == null) {
				return null;
			}
			return Utils.RedisTryGetExtrinsics(redis, Key);
		}

		private bool init() {
			if (LiveUpdate) {
				redisSubscriber = new Subscriber(redis);
				redisSubscriber.Subscribe(OnExtrinsicsReceived, Key, "unsub");
				return true;
			}
			else {
				ExtrinsicsParameters parameters = load();
				applyExtrinsics(parameters);
				return parameters != null;
			}
		}

		private bool kill() {
			redisSubscriber = null;
			return true;
		}
	#endregion

		public void applyExtrinsics(ExtrinsicsParameters extrinsics) {
			if (extrinsics == null) return;
			Matrix4x4 transRot = new Matrix4x4();
			transRot = Utils.FloatArrayToMatrix4x4(extrinsics.matrix);

			if (ReverseYAxis) {
				Matrix4x4 scale = Matrix4x4.Scale(new Vector3(1, -1, 1));
				transRot = scale * transRot;
			}

			transform.localPosition = Utils.ExtractTranslation((Matrix4x4)transRot);
			transform.localRotation = Utils.ExtractRotation((Matrix4x4)transRot);
		}
	}
}