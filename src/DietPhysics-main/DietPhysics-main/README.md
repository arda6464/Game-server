# 🧲 DietPhysics
DietPhysics is a simple collision and sweep testing library written for C#
![Logo](https://github.com/xRa7en/DietPhysics/blob/main/preview/logo.png) 

## Why DietPhysics?
Provides world-based collision detection for dedicated servers and projects without physics support, eliminating the need for complex calculations.

## 📚 Key Features

* Fully compatible with Unity’s Collider parameters.
* Supports three collider types: `Sphere`,`Capsule`, and `Box`
* Physics calculation iterations can be adjusted based on requirements.
---

## ❓ How it works?

The collider is iterated in the desired direction using a step size and collision checks are performed at each step.  
For example, for a sphere with a radius of 0.5, the step size should not exceed 0.5.  
Movement Distance / Step Size = Minimum Iteration Count.

Example:
```
Walk Distance = 5.0f  
Step Size = 0.5f  
Min. Iterations = 5.0 / 0.5 = 10
```


## ⚙ Example & Usage
![SweepTest](https://github.com/xRa7en/DietPhysics/blob/main/preview/1.png) 
![Result](https://github.com/xRa7en/DietPhysics/blob/main/preview/2.png) 

```c#

    Vec3 spherePosition = new Vec3(0F,0F,-2F);
    Vec3 spherePosition2 = new Vec3(0F,0F,2F);
    Vec3 sphereCenter = new Vec3(0F,0F,0F);
    float radius = 0.5F;

    DietSphere sphere = new DietSphere(spherePosition, sphereCenter, radius);
    DietSphere sphere2 = new DietSphere(spherePosition2, sphereCenter, radius);

    DietWorld world = new DietWorld();

    world.AddCollider(sphere);
    world.AddCollider(sphere2);

    bool isCollided = world.SweepTest(sphere, Vec3.forward, walkDistance, iterations, out Vec3 collidedPosition);

```

## 📋 TODO

- [x] ~~Sphere to -> Sphere/Capsule/Box~~ **(DONE)**
- [x] ~~Capsule to -> Sphere/Capsule/Box~~ **(DONE)**
- [ ] Box to -> Sphere/Capsule/Box
- [ ] Simplified method overloads for faster project development and testing
- [ ] Area of Interest (AoI) system for managing a large number of colliders within a physics world
