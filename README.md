# Unity Client
Simple see through AR application made with Unity using Nectar plateform services.

## Prerequisites
- [Unity](https://unity3d.com/fr)
- [Redis](https://redis.io/)

## TODO
- Setup Unity's main camera with intrinsecs camera parameters & update projection.
- Get 3D pose matrix from redis.
- Estimate 3D pos of markers with 3D pose matrix.
- Create 2nd camera that will act as a projector.
- Place the projector using a 3D transformation matrix. The matrix should be applied to the camera matrix.
- Play application in fullscreen.

## VSCode for Unity
[Configuration steps](https://gist.github.com/nicpalard/9fc8ce61a17dc5518bea11d79efdefb2)