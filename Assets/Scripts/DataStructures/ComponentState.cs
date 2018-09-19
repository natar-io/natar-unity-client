using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ComponentState {
    DISCONNECTED, // When the component failed to connect to redis
    CONNECTED, // When the component successfulyl connected but failed to initialize
    WORKING, // When the component successfully connected and intialized
}