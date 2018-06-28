using System.Collections;
using System.Collections.Generic;
using TeamDev.Redis;
using UnityEngine;

public class RedisSubscriptionHandler : Singleton<RedisSubscriptionHandler> {

	protected RedisSubscriptionHandler () { }

	void Start () {
		ApplicationParameters.RedisConnection.redis.ChannelSubscribed += new ChannelSubscribedHandler (OnChannelSubscribed);
	}

	public void Sub (string channelName) {
		ApplicationParameters.RedisConnection.redis.Messaging.Subscribe (channelName);
	}

	public void Unsub (string channelName) {
		ApplicationParameters.RedisConnection.redis.SendCommand(RedisCommand.UNSUBSCRIBE, channelName);
	}

	void OnChannelSubscribed (string channelName) {
		Debug.Log ("[SUB HANDLER] Registered sub to " + channelName);
	}

	void OnChannelUnsubscribed (string channelName) {
		Debug.Log ("[SUB HANDLER] Registered unsub to " + channelName);
	}
}