using System.Collections;
using System.Collections.Generic;
using TeamDev.Redis;
using UnityEngine;

public class RedisSubscriptionHandler : Singleton<RedisSubscriptionHandler> {

	protected RedisSubscriptionHandler () { }

	void Start () {
		RedisConnectionHandler.Instance.redis.ChannelSubscribed += new ChannelSubscribedHandler (OnChannelSubscribed);
	}

	public void Sub (string channelName) {
		RedisConnectionHandler.Instance.redis.Messaging.Subscribe (channelName);
	}

	public void Unsub (string channelName) {
		RedisConnectionHandler.Instance.redis.SendCommand(RedisCommand.UNSUBSCRIBE, channelName);
	}

	void OnChannelSubscribed (string channelName) {
		Debug.Log ("[SUB HANDLER] Registered sub to " + channelName);
	}

	void OnChannelUnsubscribed (string channelName) {
		Debug.Log ("[SUB HANDLER] Registered unsub to " + channelName);
	}
}