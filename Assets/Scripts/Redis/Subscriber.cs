using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TeamDev.Redis;

public class Subscriber {

	private RedisDataAccessProvider redis;

	public Subscriber(RedisDataAccessProvider redis) {
		this.redis = redis;
		this.redis.ChannelSubscribed += new ChannelSubscribedHandler(OnChannelSubscribed);
	}

	public void Subscribe(string channelName, Action<string, byte[]> callback) {
		this.redis.Messaging.Subscribe(channelName);
		this.redis.BinaryMessageReceived += new BinaryMessageReceivedHandler(callback);
	}

	public void Unsubscribe(string channelName, Action<string> callback) {
		this.redis.ChannelUnsubscribed += new ChannelUnsubscribedHandler(OnChannelUnsubscribed);
		this.redis.Messaging.Unsubscribe(channelName);
	}


	void OnChannelSubscribed(string channelName) {
		Debug.Log("[SUBSCRIBER] SUB to " + channelName);
	}

	void OnChannelUnsubscribed(string channelName) {
		Debug.Log("[SUBCRIBER] UNSUB to " + channelName);
	}
}
