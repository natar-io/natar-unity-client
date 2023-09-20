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
    public bool Use16BitDepth = false;
		public ServiceStatus state = ServiceStatus.DISCONNECTED;


		private ImageData imageData = new ImageData();
		public Texture2D preallocatedTexture = null;
		private byte[] preallocatedData = null;

		public RawImage OutImage;

		private ImageInformations currentImageInformations = new ImageInformations(), 
									previousImageInformations = new ImageInformations();
		private bool imageNeedsUpdate = false;

		private delegate void OnServiceConnectionStateChangedHandler(bool connected);

		private event OnServiceConnectionStateChangedHandler ServiceConnectionStateChanged;

    public GameObject DepthPrefab;
    public CameraPlayback ColorImage;
    public PointCloudManager PointCloudManager;
    private GameObject[] DepthObjects;

		public void Start() {
			this.state = ServiceStatus.DISCONNECTED;

			rHandler = RedisHandler.Instance;
			rHandler.ConnectionStatusChanged += OnRedisHandlerConnectionStateChanged;
			rHandler.ConnectionStatusNotification += OnRedisHandlerConnectionNotificationStatus;
			rHandler.NewService("camera");
			ServiceConnectionStateChanged += OnServiceConnectionStateChanged;
		}

       void OnApplicationQuit()
    {
        Debug.Log("Application ending after " + Time.time + " seconds");
        if(DepthObjects == null){
          DepthObjects = new GameObject[imageData.Width * imageData.Height]; 
          for(int i = 0; i < (imageData.Width * imageData.Height) - 200; i += 200){
            Destroy(DepthObjects[i]);
          }
        }
    }

		public void Update() {
			#if UNITY_EDITOR
			if (!CheckScriptCurrentState()) { this.Start(); }
			#endif
			if (imageNeedsUpdate) {
        if(Use16BitDepth){


          // TODO: do this only once.
          IntrinsicsParameters intrinsics = this.GetComponent<SetupIntrinsics>().GetIntrinsicsParameters();

          float[] depthImage = Utils.DecodeDepthImage(redis, Key, preallocatedData, 
                                              imageData.Width, imageData.Height, 1); 
          Utils.loadDepthImageToIntoPreallocatedTexture(depthImage, ref preallocatedTexture, preallocatedData, imageData.Width, imageData.Height, imageData.Channels); 


          if(intrinsics != null){   

            if(DepthObjects == null){
              DepthObjects = new GameObject[imageData.Width * imageData.Height]; 
              for(int i = 0; i < depthImage.Length - 100; i += 100){
                DepthObjects[i] = Instantiate(DepthPrefab);
              }
            }

            float ifx =  1f / intrinsics.fx;
            float ify =  1f / intrinsics.fy;
            float cx = intrinsics.cx;
            float cy = intrinsics.cy; 
            float depth = depthImage[320 * 640 + 240];
            
            Vector3 result;
            float x1 = 300; 
            float y1 = 100;
              result.x = (float) ((x1 - cx) * depth * ifx);
              result.y = (float) ((y1 - cy) * depth * ify);

            result.z = depth;    

            Vector3[] depthPoints = new Vector3[depthImage.Length];
            Color[] colorPoints = new Color[depthImage.Length];

            for(int i = 0; i < depthImage.Length - 100; i += 100){
              int x = i % imageData.Width;
              int y = i / imageData.Height;
              depthPoints[i] = Utils.PixelToWorld(intrinsics, x, y, depthImage[i]);

            //  PointCloudManager.createMesh(depthPoints, colorPoints);

              // if(DepthObjects[i] != null){
              //   // Debug.Log("depthPoints[i] " + x + " " + y + " "+  depthImage[i]);  
              //   DepthObjects[i].transform.position = depthPoints[i];
              // }
            }
            PointCloudManager.CreateMesh(depthPoints, colorPoints, depthImage.Length);


          }else {
            Debug.Log("No intrinsics...");
          }
          
        }else {
				  Utils.GetImageIntoPreallocatedTexture(redis, Key, ref preallocatedTexture, preallocatedData, imageData.Width, imageData.Height, imageData.Channels);
				}
        if (OutImage != null) { OutImage.texture = preallocatedTexture; }
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

     //  Debug.Log("OnImageReceived " + channelName);
			string json = Utils.ByteToString(message);

			// currentImageInformations = Utils.JSONTo<ImageInformations>(json);
			// imageNeedsUpdate = currentImageInformations != null && currentImageInformations.imageCount != previousImageInformations.imageCount;
			imageNeedsUpdate = true;
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

		public void init() {
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

		public Texture2D GetCurrentTexture() {
			return this.preallocatedTexture;
		}

		public bool CheckScriptCurrentState() {
			return this.rHandler != null && 
					this.redis != null &&
					this.redisSubscriber != null &&
					this.preallocatedTexture != null &&
					this.preallocatedData != null &&
					this.imageData != null;
		}
	}
}
