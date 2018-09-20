using UnityEngine;
using System.Collections;

public class FloatingAnimation : MonoBehaviour {

    public float RotationSpeed = 30.0f;
    public float FloatingMultiplier = 30f;
    public float Speed = 1f;
 
    // Position Storage Variables
    private Vector3 initialPosition;
	private Vector3 currentPosition;
 
    // Use this for initialization
    void Start () {
        initialPosition = this.transform.position;
    }
     
    // Update is called once per frame
    void Update () {
        transform.Rotate(new Vector3(0f, Time.deltaTime * RotationSpeed, 0f), Space.World);

        currentPosition = initialPosition;
        currentPosition.y += Mathf.Sin (Time.fixedTime * Mathf.PI * Speed) * FloatingMultiplier;
        transform.position = currentPosition;
    }
}