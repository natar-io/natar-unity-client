using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TeamDev.Redis;

public class Subscriber {

	private RedisDataAccessProvider redis;

	public Subscriber(RedisDataAccessProvider redis) {
		RedisConnection connection = new RedisConnection(redis.Configuration.Host, redis.Configuration.Port);
		this.redis = connection.GetDataAccessProvider();
		this.redis.ChannelSubscribed += new ChannelSubscribedHandler(OnChannelSubscribed);
	}

	public void Subscribe(Action<string, byte[]> callback, params string[] channelNames) {
		this.redis.Messaging.Subscribe(channelNames);
		this.redis.BinaryMessageReceived += new BinaryMessageReceivedHandler(callback);
		Debug.Log("Event handler successfully set...");
	}

	public void Unsubscribe(params string[] channelNames) {
		this.redis.ChannelUnsubscribed += new ChannelUnsubscribedHandler(OnChannelUnsubscribed);
		this.redis.Messaging.Unsubscribe(channelNames);
	}

	void OnChannelSubscribed(string channelName) {
		Debug.Log("[SUBSCRIBER] SUB to " + channelName);
	}

	void OnChannelUnsubscribed(string channelName) {
		Debug.Log("[SUBCRIBER] UNSUB to " + channelName);
	}

	void DefaultEvent(string name, byte[] data) {
		Debug.Log("Data were lost cause no handler were found.");
	}
}
