using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System; // Exception
using TeamDev.Redis;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Networking;


namespace Natar
{	
	[ExecuteInEditMode]
	public class CameraPlayback : MonoBehaviour {
		private RedisHandler rHandler;
		private RedisDataAccessProvider redis;
		private Subscriber redisSubscriber;

		public String Key = "camera";
		public ServiceStatus state = ServiceStatus.DISCONNECTED;

		private ImageData imageData;
		public Texture2D preallocatedTexture;
		private byte[] preallocatedData;

		private ImageInformations currentImageInformations = new ImageInformations(), 
									previousImageInformations = new ImageInformations();
		private bool imageNeedsUpdate = false;

		private delegate void OnServiceConnectionStateChangedHandler(bool connected);

		#pragma warning disable 169, 414
		private event OnServiceConnectionStateChangedHandler ServiceConnectionStateChanged;
		#pragma warning restore 169, 414

		public void Start() {
			this.state = ServiceStatus.DISCONNECTED;

			rHandler = RedisHandler.Instance;
			rHandler.ConnectionStatusChanged += OnRedisHandlerConnectionStateChanged;
			rHandler.ConnectionStatusNotification += OnRedisHandlerConnectionNotificationStatus;
			rHandler.NewService("camera");
			ServiceConnectionStateChanged += OnServiceConnectionStateChanged;
		}

		public void Update() {
			if (imageNeedsUpdate) {
				Utils.GetImageIntoPreallocatedTexture(redis, Key, ref preallocatedTexture, preallocatedData, imageData.Width, imageData.Height, imageData.Channels);
				imageNeedsUpdate = false;
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

		private void OnImageReceived(string channelName, byte[] message) {
			if (channelName == "unsub") {
				redisSubscriber.Unsubscribe(Key, "unsub");
				return;
			}
			string json = Utils.ByteToString(message);
			currentImageInformations = Utils.JSONTo<ImageInformations>(json);
			imageNeedsUpdate = currentImageInformations != null && currentImageInformations.imageCount != previousImageInformations.imageCount;
			previousImageInformations = currentImageInformations;
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

		private void init() {
			imageData = load();
			if (imageData.Width == -1 || imageData.Height == -1 || imageData.Channels == -1) {
				return;
			}

			preallocatedTexture = new Texture2D(imageData.Width, imageData.Height, TextureFormat.RGB24, false);
			preallocatedData = new byte[imageData.Width * imageData.Height * imageData.Channels];
			
			if (redisSubscriber != null) {
				redisSubscriber.Unsubscribe(Key, "unsub");
			}
			redisSubscriber = new Subscriber(redis);
			redisSubscriber.Subscribe(OnImageReceived, Key, "unsub");
			this.state = ServiceStatus.WORKING;
		}

		private ImageData load() {
			int commandId =  redis.SendCommand (RedisCommand.GET, Key + ":width");
			int? width = Utils.RedisTryReadInt(redis, commandId);

			commandId =  redis.SendCommand (RedisCommand.GET, Key + ":height");
			int? height = Utils.RedisTryReadInt(redis, commandId);

			commandId =  redis.SendCommand (RedisCommand.GET, Key + ":channels");
			int? channels = Utils.RedisTryReadInt(redis, commandId);

			commandId = redis.SendCommand(RedisCommand.GET, Key + ":pixelformat");
			string pixelformat = Utils.RedisTryReadString(redis, commandId);

			if (width == null || height == null || channels == null) {
				return new ImageData();
			}

			return new ImageData((int)width, (int)height, (int)channels, pixelformat);
		}

		private void kill() {
			redisSubscriber.Unsubscribe(Key, "unsub");
			redisSubscriber = null;
		}
		#endregion
	}
}