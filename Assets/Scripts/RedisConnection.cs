using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Net.Sockets;
// To use "." in floats
using System.Globalization;

using UnityEngine;

// using StackExchange.Redis;
using TeamDev.Redis;

public class RedisConnection : MonoBehaviour {
    
    // Set this as a public variable so it can be edited from Unity Editor
    public string ServerIp = "127.0.0.1";
    public int ServerPort = 6379;
    public string ServerKey = "custom:image";
    public Camera originCamera;

    private RedisDataAccessProvider redis;
    private Texture2D videoTexture;
    
    private static string OUTPUT_PREFIX = "nectar:";
    private static string OUTPUT_PREFIX2 = ":camera-server:camera";
    private static string REDIS_PORT = "6379";

    static string defaultHost = "jiii-mi";
    static string defaultName = OUTPUT_PREFIX + defaultHost + OUTPUT_PREFIX2 + "#0";
    
    void Start() {
        Debug.Log("Redis Connection start");

        redis = new RedisDataAccessProvider ();
        redis.Configuration.Host = ServerIp;
        redis.Configuration.Port = ServerPort;
        try {
            redis.Connect();
        }
        catch (SocketException e) {
            Debug.LogError(e.StackTrace);
            Debug.Log("Connection failed. Exiting ...");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        // Subscription works as event
        redis.ChannelSubscribed += new ChannelSubscribedHandler(OnChannelSubscribed);
        redis.MessageReceived += new MessageReceivedHandler(OnMessageRecieved);
        
        // Messaging is an helper class that provides helper functions for easy pub/sub usage.
        // redis.Messaging.Subscribe("message");

        CameraParameters camParams = RedisGetCameraParameters();
        ApplicationParameters.camParams = (CameraParameters) camParams;
        SetupCamera(ApplicationParameters.camParams);

        Matrix4x4? pose3D = RedisGetPose();
        if (pose3D != null) {            
            ApplicationParameters.pose3D = (Matrix4x4) pose3D;
        }

    }

    /// <summary>
    /// Event triggered when a subscribtion is done.
    /// </summary>
    /// <param name="channelname">The subscribed channel name.</param>
    void OnChannelSubscribed(string channelname) {
        Debug.Log("[SUB] " + channelname);
    }

    /// <summary>
    /// Event triggered when a message arrive on a subscribed channel.
    /// </summary>
    /// <param name="channelname">The channel into which the message arrive</param>
    /// <param name="message">The message that was published</param>
    void OnMessageRecieved(string channelname, string message) {
        Debug.Log(string.Format("[PUB] {0} - {1} ", channelname, message));
    }

    Matrix4x4? RedisGetPose()
    {
        int commandId = redis.SendCommand(RedisCommand.GET, ServerKey + ":detected-pose");
        string jsonPose = Utils.RedisTryReadString(redis, commandId);
        if (jsonPose != null) {
            // Hand made parsing
            var charstoRemove = new string[] { ",", "[", "]", "\n", "\t"};
            foreach (var c in charstoRemove) {
                jsonPose = jsonPose.Replace(c, string.Empty);
            }
            Matrix4x4 poseMatrix = new Matrix4x4();
            string[] poseValue = jsonPose.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            int cpt = 0;
            foreach (var value in poseValue) {
                if (cpt >= 16)
                    cpt = cpt - 16 + 1;
                poseMatrix[cpt] = float.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
                cpt += 4;
            }
            return poseMatrix;
        }
        return null;
    }

    CameraParameters RedisGetCameraParameters()
    {
        int commandId = redis.SendCommand(RedisCommand.GET, OUTPUT_PREFIX + defaultHost + ":calibration:astra-s-rgb");
        string cameraParameters = Utils.RedisTryReadString(redis, commandId);
        if (cameraParameters != null) {
            CameraParameters camParams = JsonUtility.FromJson<CameraParameters>(cameraParameters);
            return camParams;
        }
        return new CameraParameters();
    }   

    /// <summary>
    /// Get image data from redis server and update texture with it
    /// </sumary>
    void RedisImageToTexture() {
        int commandId;
        commandId = redis.SendCommand(RedisCommand.GET, ServerKey + ":width");
        int? width = Utils.RedisTryReadInt(redis, commandId);

        commandId = redis.SendCommand(RedisCommand.GET, ServerKey + ":height");
        int? height = Utils.RedisTryReadInt(redis, commandId);

        if (!width.HasValue || !height.HasValue) {
            throw new ArgumentException("Could not fetch image width or height from redis server. Please check connection settings.");
        }

        // Get this particular commandId
        commandId = redis.SendCommand(RedisCommand.GET, ServerKey);
        // Get image data from this particular command to avoid unexpected results
        byte[] imageData = Utils.RedisTryReadData(redis, commandId);

        if (videoTexture == null || videoTexture.width != (int)width || videoTexture.height != (int)height) {
            videoTexture = new Texture2D((int)width, (int)height, TextureFormat.RGB24, false);
        }
        videoTexture.LoadRawTextureData(imageData);

        // Get markers informations
        commandId = redis.SendCommand(RedisCommand.GET, ServerKey + ":detected-markers");
        string jsonObj = Utils.RedisTryReadString(redis, commandId);
        if (jsonObj != null) {

            Markers markers = JsonUtility.FromJson<Markers>(jsonObj);
            // Debug code (print red circles on markers corners position)
            for (int i = 0 ; i < markers.markers.Length; ++i)
            {
                Marker m = markers.markers[i];
                for (int j = 0 ; j < m.corners.Length ; j+= 2)
                {
                    Utils.Circle(this.videoTexture, (int)m.corners[j], (int)m.corners[j+1], 5, Color.red);    
                }
            }

            // Getting 3D Pose
            Matrix4x4 poseMat = new Matrix4x4();
            for (int i = 0 ; i < markers.pose.Length; i++) {
                poseMat[i] = markers.pose[i];
            }
        }

        // Render the image on the texture
        videoTexture.Apply();
        this.GetComponent<Renderer>().material.mainTexture = videoTexture;
    }
    
    // Update is called once per frame
    void Update() {
        RedisImageToTexture();
    }

    void SetupCamera(CameraParameters camParams) //int w, int h, float cx, float cy, float fx, float fy)
    {
        float dx = camParams.cx - camParams.width / 2;
        float dy = camParams.cy - camParams.height / 2;

        float near = originCamera.nearClipPlane;
        float far = originCamera.farClipPlane;


        Matrix4x4 projectionMatrix  = new Matrix4x4();

        Vector4 row0 = new Vector4((2f * camParams.fx / camParams.width), 0, (2f * dx / camParams.width), 0);
        Vector4 row1 = new Vector4(0, 2f * camParams.fy / camParams.height, -2f * (dy + 1f) / camParams.height,  0);
        Vector4 row2 = new Vector4(0, 0, -(far + near) / (far - near), -near * ( 1 + (far + near) / (far - near)));
        Vector4 row3 = new Vector4(0, 0, -1, 0);

        projectionMatrix.SetRow(0, row0);
        projectionMatrix.SetRow(1, row1);
        projectionMatrix.SetRow(2, row2);
        projectionMatrix.SetRow(3, row3);
        
        originCamera.projectionMatrix = projectionMatrix;
    }
}
