﻿using System.Collections;
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
    public Camera originCamera;

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

        //SetupCamera(640, 480, new Vector2(300, 400), new Vector2(1000, 1000));
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


    void SetupCamera(uint width, uint height, Vector2 opticalCenter, Vector2 focal)
    {
        float dx = opticalCenter.x - width / 2;
        float dy = opticalCenter.y - height / 2;

        float near = originCamera.nearClipPlane;
        float far = originCamera.farClipPlane;

        Matrix4x4 projectionMatrix  = new Matrix4x4();

        Vector4 row0 = new Vector4((2f * focal.x / width), 0,(2f * dx / width), 0);
        Vector4 row1 = new Vector4(0, 2f * focal.y / height, -2f * (dy + 1f) / height, 0);
        Vector4 row2 = new Vector4(0, 0, -(far + near) / (far - near), -near * (1 + (far + near) / (far - near)));
        Vector4 row3 = new Vector4(0, 0, -1, 0);

        projectionMatrix.SetRow(0, row0);
        projectionMatrix.SetRow(1, row1);
        projectionMatrix.SetRow(2, row2);

        originCamera.projectionMatrix = projectionMatrix;    
    }
}
