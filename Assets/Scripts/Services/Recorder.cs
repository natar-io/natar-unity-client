using System;
using System.Text;

using UnityEngine;

using TeamDev.Redis;

namespace Natar {

    [ExecuteInEditMode]
    public class Recorder : MonoBehaviour
    {
        private RedisHandler rHandler;
		private RedisDataAccessProvider redis;

		public string Key = "unity:render";

        public ServiceStatus state = ServiceStatus.DISCONNECTED;

		private delegate void OnServiceConnectionStateChangedHandler(bool connected);
		private event OnServiceConnectionStateChangedHandler ServiceConnectionStateChanged;

        private int screenWidth = Screen.width;
        private int screenHeight = Screen.height;

        private Rect rect;
        private RenderTexture renderTexture;
        private Texture2D texture;

        public Camera camera;

        public void Awake() {
            rect = new Rect(0, 0, screenWidth, screenHeight);
            renderTexture = new RenderTexture(screenWidth, screenHeight, 24);
            texture = new Texture2D(screenWidth, screenHeight, TextureFormat.RGB24, false);
        }

        void Start() {
            state = ServiceStatus.DISCONNECTED;

			rHandler = RedisHandler.Instance;
			rHandler.ConnectionStatusChanged += OnRedisHandlerConnectionStateChanged;
			rHandler.ConnectionStatusNotification += OnRedisHandlerConnectionStatusNotification;
			rHandler.NewService("recorder");
			
			ServiceConnectionStateChanged += OnServiceConnectionStateChanged;
        }

        void Update() {
            #if UNITY_EDITOR
			if (!CheckScriptCurrentState()) { this.Start(); }
			#endif

            ScreenShot();
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

		public void init() {
            this.state = ServiceStatus.WORKING;
		}

		private void kill() {}

	#endregion

        public void ScreenShot() {
			if (camera == null)
				return; 
            camera.targetTexture = renderTexture;
            camera.Render();

            RenderTexture.active = renderTexture;
            texture.ReadPixels(rect, 0, 0);
            texture.Apply();

            camera.targetTexture = null;
            RenderTexture.active = null;

            // 3 channels : RGB | Total size: 3 * width * height
            byte[] data = texture.GetRawTextureData();
			char[] encoded = Encoding.UTF8.GetChars(data);
			string encodedDefault = Encoding.Default.GetString(data);
			string encodedASCII = Encoding.ASCII.GetString(data);
			// byte[] decoded = Encoding.UTF8.GetBytes(encoded);
			Debug.LogFormat("{0}x{1}x{2} - ASCII {3} - UTF8 {4}", screenWidth, screenHeight, 3, encodedASCII.Length, encodedDefault.Length );
			// Debug.LogFormat("{0} vs {1} - {2}", data.Length, encoded.Length, decoded.Length);
            redis.SendCommand(RedisCommand.SET, Key, /* new string(encoded) */ encodedASCII);
            redis.SendCommand(RedisCommand.SET, Key + ":width", screenWidth.ToString());
            redis.SendCommand(RedisCommand.SET, Key + ":height", screenHeight.ToString());
            redis.SendCommand(RedisCommand.SET, Key + ":channels", "3");

        }

        public bool CheckScriptCurrentState() {
			return this.rHandler != null && 
					this.redis != null;
		}
	}
}
