using System;
using System.Collections;
using UnityEngine;

using TeamDev.Redis;

namespace Natar
{
	[ExecuteInEditMode]
	public class Texturer : MonoBehaviour {
		private RedisHandler rHandler;
		private RedisDataAccessProvider redis;

		public string Key = "camera0:view1";

		public ServiceStatus state = ServiceStatus.DISCONNECTED;

		public GameObject targetModel;
		private Texture2D currentTexture = null;

		private delegate void OnServiceConnectionStateChangedHandler(bool connected);
		private event OnServiceConnectionStateChangedHandler ServiceConnectionStateChanged;


		public void Start() {
			state = ServiceStatus.DISCONNECTED;
			
			rHandler = RedisHandler.Instance;
			rHandler.ConnectionStatusChanged += OnRedisHandlerConnectionStateChanged;
			rHandler.ConnectionStatusNotification += OnRedisHandlerConnectionStatusNotification;

			rHandler.NewService("texturer");
			
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

		private Texture2D load() {
			if (redis == null) { return null; }
			return Utils.GetImageAsTexture(redis, Key);	
		}

		public void init() {
			Texture2D texture = load();
			if (applyTexture(texture, targetModel)) {
				this.state = ServiceStatus.WORKING;
			}
			else {
				this.state = ServiceStatus.CONNECTED;
			}
		}

		private void kill() {}
	
	#endregion

		private bool applyTexture(Texture2D texture, GameObject target) {
			if (texture == null) { return false; }
			Renderer targetRenderer = target.GetComponent<Renderer>();
			if (targetRenderer == null) { return false; }
			targetRenderer.sharedMaterial.mainTexture = texture;
			currentTexture = texture;
			return true;
		}

		public Texture2D GetCurrentTexture() {
			return this.currentTexture;
		}


		public bool CheckScriptCurrentState() {
			return this.rHandler != null && 
					this.redis != null;
		}
	}
}