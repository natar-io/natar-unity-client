using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ClientCounterUpdater : MonoBehaviour {

	private Text counter;
	// Use this for initialization
	void Start () {
		counter = this.GetComponent<Text>();
	}
	
	// Update is called once per frame
	void Update () {
		counter.text = "Redis Client Count: " + ApplicationParameters.RedisClientCount;
	}
}
