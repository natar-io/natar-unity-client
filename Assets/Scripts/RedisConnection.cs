using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Net.Sockets;

using UnityEngine;

// using StackExchange.Redis;
using TeamDev.Redis;

public class RedisConnection : MonoBehaviour {
    
    // Set this as a public variable so it can be edited from Unity Editor
    public string ServerIp = "127.0.0.1";
    public int ServerPort = 6379;
    public string ServerKey = "custom:image";

    private RedisDataAccessProvider redis;
    private Texture2D videoTexture;
    
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
        //* Load texture from raw data
        videoTexture.LoadRawTextureData(imageData);
        /*/ //Load texture creating a color array 
        Color32[] image = Utils.ByteArrayToColor(imageData);
        videoTexture.SetPixels32(image);
        //*/

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
}
