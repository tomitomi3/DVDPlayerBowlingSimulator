﻿using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using Demos;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SimpleDiskBowling
{
    /// <summary>
    /// Shows a completely isolated usage of the engine without using any of the other demo types.
    /// </summary>
    public static class SimpleDiskBowling
    {
        public unsafe struct DemoNarrowPhaseCallbacks : INarrowPhaseCallbacks
        {
            public SpringSettings ContactSpringiness;
            public float MaximumRecoveryVelocity;
            public float FrictionCoefficient;

            public void Initialize(Simulation simulation)
            {
                //Use a default if the springiness value wasn't initialized... at least until struct field initializers are supported outside of previews.
                if (ContactSpringiness.AngularFrequency == 0 && ContactSpringiness.TwiceDampingRatio == 0)
                {
                    ContactSpringiness = new(30, 1);
                    MaximumRecoveryVelocity = 2f;
                    FrictionCoefficient = 1f;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b)
            {
                //While the engine won't even try creating pairs between statics at all, it will ask about kinematic-kinematic pairs.
                //Those pairs cannot emit constraints since both involved bodies have infinite inertia. Since most of the demos don't need
                //to collect information about kinematic-kinematic pairs, we'll require that at least one of the bodies needs to be dynamic.
                return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
            {
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
            {
                pairMaterial.FrictionCoefficient = FrictionCoefficient;
                pairMaterial.MaximumRecoveryVelocity = MaximumRecoveryVelocity;
                pairMaterial.SpringSettings = ContactSpringiness;
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
            {
                return true;
            }

            public void Dispose()
            {
            }
        }

        /// <summary>
        /// 移植
        /// </summary>
        public struct DemoPoseIntegratorCallbacks : IPoseIntegratorCallbacks
        {
            /// <summary>
            /// Gravity to apply to dynamic bodies in the simulation.
            /// </summary>
            public Vector3 Gravity;
            /// <summary>
            /// Fraction of dynamic body linear velocity to remove per unit of time. Values range from 0 to 1. 0 is fully undamped, while values very close to 1 will remove most velocity.
            /// </summary>
            public float LinearDamping;
            /// <summary>
            /// Fraction of dynamic body angular velocity to remove per unit of time. Values range from 0 to 1. 0 is fully undamped, while values very close to 1 will remove most velocity.
            /// </summary>
            public float AngularDamping;

            Vector3 gravityDt;
            float linearDampingDt;
            float angularDampingDt;

            public readonly AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;

            public void Initialize(Simulation simulation)
            {
                //In this demo, we don't need to initialize anything.
                //If you had a simulation with per body gravity stored in a CollidableProperty<T> or something similar, having the simulation provided in a callback can be helpful.
            }

            /// <summary>
            /// Creates a new set of simple callbacks for the demos.
            /// </summary>
            /// <param name="gravity">Gravity to apply to dynamic bodies in the simulation.</param>
            /// <param name="linearDamping">Fraction of dynamic body linear velocity to remove per unit of time. Values range from 0 to 1. 0 is fully undamped, while values very close to 1 will remove most velocity.</param>
            /// <param name="angularDamping">Fraction of dynamic body angular velocity to remove per unit of time. Values range from 0 to 1. 0 is fully undamped, while values very close to 1 will remove most velocity.</param>
            public DemoPoseIntegratorCallbacks(Vector3 gravity, float linearDamping = .03f, float angularDamping = .03f) : this()
            {
                Gravity = gravity;
                LinearDamping = linearDamping;
                AngularDamping = angularDamping;
            }

            public void PrepareForIntegration(float dt)
            {
                //No reason to recalculate gravity * dt for every body; just cache it ahead of time.
                gravityDt = Gravity * dt;
                //Since these callbacks don't use per-body damping values, we can precalculate everything.
                linearDampingDt = MathF.Pow(MathHelper.Clamp(1 - LinearDamping, 0, 1), dt);
                angularDampingDt = MathF.Pow(MathHelper.Clamp(1 - AngularDamping, 0, 1), dt);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void IntegrateVelocity(int bodyIndex, in RigidPose pose, in BodyInertia localInertia, int workerIndex, ref BodyVelocity velocity)
            {
                //Note that we avoid accelerating kinematics. Kinematics are any body with an inverse mass of zero (so a mass of ~infinity). No force can move them.
                if (localInertia.InverseMass > 0)
                {
                    velocity.Linear = (velocity.Linear + gravityDt) * linearDampingDt;
                    velocity.Angular = velocity.Angular * angularDampingDt;
                }
                //Implementation sidenote: Why aren't kinematics all bundled together separately from dynamics to avoid this per-body condition?
                //Because kinematics can have a velocity- that is what distinguishes them from a static object. The solver must read velocities of all bodies involved in a constraint.
                //Under ideal conditions, those bodies will be near in memory to increase the chances of a cache hit. If kinematics are separately bundled, the the number of cache
                //misses necessarily increases. Slowing down the solver in order to speed up the pose integrator is a really, really bad trade, especially when the benefit is a few ALU ops.

                //Note that you CAN technically modify the pose in IntegrateVelocity by directly accessing it through the Simulation.Bodies.ActiveSet.Poses, it just requires a little care and isn't directly exposed.
                //If the PositionFirstTimestepper is being used, then the pose integrator has already integrated the pose.
                //If the PositionLastTimestepper or SubsteppingTimestepper are in use, the pose has not yet been integrated.
                //If your pose modification depends on the order of integration, you'll want to take this into account.

                //This is also a handy spot to implement things like position dependent gravity or per-body damping.
            }

        }


        //The simulation has a variety of extension points that must be defined. 
        //The demos tend to reuse a few types like the DemoNarrowPhaseCallbacks, but this demo will provide its own (super simple) versions.
        //If you're wondering why the callbacks are interface implementing structs rather than classes or events, it's because 
        //the compiler can specialize the implementation using the compile time type information. That avoids dispatch overhead associated
        //with delegates or virtual dispatch and allows inlining, which is valuable for extremely high frequency logic like contact callbacks.
        unsafe struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
        {
            /// <summary>
            /// Performs any required initialization logic after the Simulation instance has been constructed.
            /// </summary>
            /// <param name="simulation">Simulation that owns these callbacks.</param>
            public void Initialize(Simulation simulation)
            {
                //Often, the callbacks type is created before the simulation instance is fully constructed, so the simulation will call this function when it's ready.
                //Any logic which depends on the simulation existing can be put here.
            }

            /// <summary>
            /// Chooses whether to allow contact generation to proceed for two overlapping collidables.
            /// </summary>
            /// <param name="workerIndex">Index of the worker that identified the overlap.</param>
            /// <param name="a">Reference to the first collidable in the pair.</param>
            /// <param name="b">Reference to the second collidable in the pair.</param>
            /// <returns>True if collision detection should proceed, false otherwise.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b)
            {
                //Before creating a narrow phase pair, the broad phase asks this callback whether to bother with a given pair of objects.
                //This can be used to implement arbitrary forms of collision filtering. See the RagdollDemo or NewtDemo for examples.
                //Here, we'll make sure at least one of the two bodies is dynamic.
                //The engine won't generate static-static pairs, but it will generate kinematic-kinematic pairs.
                //That's useful if you're trying to make some sort of sensor/trigger object, but since kinematic-kinematic pairs
                //can't generate constraints (both bodies have infinite inertia), simple simulations can just ignore such pairs.
                return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
            }

            /// <summary>
            /// Chooses whether to allow contact generation to proceed for the children of two overlapping collidables in a compound-including pair.
            /// </summary>
            /// <param name="pair">Parent pair of the two child collidables.</param>
            /// <param name="childIndexA">Index of the child of collidable A in the pair. If collidable A is not compound, then this is always 0.</param>
            /// <param name="childIndexB">Index of the child of collidable B in the pair. If collidable B is not compound, then this is always 0.</param>
            /// <returns>True if collision detection should proceed, false otherwise.</returns>
            /// <remarks>This is called for each sub-overlap in a collidable pair involving compound collidables. If neither collidable in a pair is compound, this will not be called.
            /// For compound-including pairs, if the earlier call to AllowContactGeneration returns false for owning pair, this will not be called. Note that it is possible
            /// for this function to be called twice for the same subpair if the pair has continuous collision detection enabled; 
            /// the CCD sweep test that runs before the contact generation test also asks before performing child pair tests.</remarks>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
            {
                //This is similar to the top level broad phase callback above. It's called by the narrow phase before generating
                //subpairs between children in parent shapes. 
                //This only gets called in pairs that involve at least one shape type that can contain multiple children, like a Compound.
                return true;
            }

            /// <summary>
            /// Provides a notification that a manifold has been created for a pair. Offers an opportunity to change the manifold's details. 
            /// </summary>
            /// <param name="workerIndex">Index of the worker thread that created this manifold.</param>
            /// <param name="pair">Pair of collidables that the manifold was detected between.</param>
            /// <param name="manifold">Set of contacts detected between the collidables.</param>
            /// <param name="pairMaterial">Material properties of the manifold.</param>
            /// <returns>True if a constraint should be created for the manifold, false otherwise.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
            {
                //The IContactManifold parameter includes functions for accessing contact data regardless of what the underlying type of the manifold is.
                //If you want to have direct access to the underlying type, you can use the manifold.Convex property and a cast like Unsafe.As<TManifold, ConvexContactManifold or NonconvexContactManifold>(ref manifold).

                //The engine does not define any per-body material properties. Instead, all material lookup and blending operations are handled by the callbacks.
                //For the purposes of this demo, we'll use the same settings for all pairs.
                //(Note that there's no bounciness property! See here for more details: https://github.com/bepu/bepuphysics2/issues/3 and check out the BouncinessDemo for some options.)
                pairMaterial.FrictionCoefficient = 1f;
                pairMaterial.MaximumRecoveryVelocity = 2f;
                pairMaterial.SpringSettings = new SpringSettings(30, 1);
                //For the purposes of the demo, contact constraints are always generated.
                return true;
            }

            /// <summary>
            /// Provides a notification that a manifold has been created between the children of two collidables in a compound-including pair.
            /// Offers an opportunity to change the manifold's details. 
            /// </summary>
            /// <param name="workerIndex">Index of the worker thread that created this manifold.</param>
            /// <param name="pair">Pair of collidables that the manifold was detected between.</param>
            /// <param name="childIndexA">Index of the child of collidable A in the pair. If collidable A is not compound, then this is always 0.</param>
            /// <param name="childIndexB">Index of the child of collidable B in the pair. If collidable B is not compound, then this is always 0.</param>
            /// <param name="manifold">Set of contacts detected between the collidables.</param>
            /// <returns>True if this manifold should be considered for constraint generation, false otherwise.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
            {
                return true;
            }

            /// <summary>
            /// Releases any resources held by the callbacks. Called by the owning narrow phase when it is being disposed.
            /// </summary>
            public void Dispose()
            {
            }
        }

        //Note that the engine does not require any particular form of gravity- it, like all the contact callbacks, is managed by a callback.
        public struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
        {
            public Vector3 Gravity;
            Vector3 gravityDt;

            /// <summary>
            /// Performs any required initialization logic after the Simulation instance has been constructed.
            /// </summary>
            /// <param name="simulation">Simulation that owns these callbacks.</param>
            public void Initialize(Simulation simulation)
            {
                //In this demo, we don't need to initialize anything.
                //If you had a simulation with per body gravity stored in a CollidableProperty<T> or something similar, having the simulation provided in a callback can be helpful.
            }

            /// <summary>
            /// Gets how the pose integrator should handle angular velocity integration.
            /// </summary>
            public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving; //Don't care about fidelity in this demo!

            public PoseIntegratorCallbacks(Vector3 gravity) : this()
            {
                Gravity = gravity;
            }

            /// <summary>
            /// Called prior to integrating the simulation's active bodies. When used with a substepping timestepper, this could be called multiple times per frame with different time step values.
            /// </summary>
            /// <param name="dt">Current time step duration.</param>
            public void PrepareForIntegration(float dt)
            {
                //No reason to recalculate gravity * dt for every body; just cache it ahead of time.
                gravityDt = Gravity * dt;
            }

            /// <summary>
            /// Callback called for each active body within the simulation during body integration.
            /// </summary>
            /// <param name="bodyIndex">Index of the body being visited.</param>
            /// <param name="pose">Body's current pose.</param>
            /// <param name="localInertia">Body's current local inertia.</param>
            /// <param name="workerIndex">Index of the worker thread processing this body.</param>
            /// <param name="velocity">Reference to the body's current velocity to integrate.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void IntegrateVelocity(int bodyIndex, in RigidPose pose, in BodyInertia localInertia, int workerIndex, ref BodyVelocity velocity)
            {
                //Note that we avoid accelerating kinematics. Kinematics are any body with an inverse mass of zero (so a mass of ~infinity). No force can move them.
                if (localInertia.InverseMass > 0)
                {
                    velocity.Linear = velocity.Linear + gravityDt;
                }
            }

        }

        public static void Run()
        {
            var bufferPool = new BufferPool();
            var simulation = Simulation.Create(bufferPool, new NarrowPhaseCallbacks(), new PoseIntegratorCallbacks(new Vector3(0, -10, 0)), new PositionLastTimestepper());

            //Drop a ball on a big static box.
            var sphere = new Sphere(1);
            sphere.ComputeInertia(1, out var sphereInertia);
            simulation.Bodies.Add(BodyDescription.CreateDynamic(new Vector3(0, 5, 0), sphereInertia, new CollidableDescription(simulation.Shapes.Add(sphere), 0.1f), new BodyActivityDescription(0.01f)));

            simulation.Statics.Add(new StaticDescription(new Vector3(0, 0, 0), new CollidableDescription(simulation.Shapes.Add(new Box(500, 1, 500)), 0.1f)));

            var threadDispatcher = new SimpleThreadDispatcher(Environment.ProcessorCount);

            //Now take 100 time steps!
            for (int i = 0; i < 100; ++i)
            {
                //Multithreading is pretty pointless for a simulation of one ball, but passing a IThreadDispatcher instance is all you have to do to enable multithreading.
                //If you don't want to use multithreading, don't pass a IThreadDispatcher.
                simulation.Timestep(0.01f, threadDispatcher);
            }

            //If you intend to reuse the BufferPool, disposing the simulation is a good idea- it returns all the buffers to the pool for reuse.
            //Here, we dispose it, but it's not really required; we immediately thereafter clear the BufferPool of all held memory.
            //Note that failing to dispose buffer pools can result in memory leaks.
            simulation.Dispose();
            threadDispatcher.Dispose();
            bufferPool.Clear();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            //最適化を試みる用に画面表示しないシミュレータを作る
            SimpleDiskBowling.Run();

            Console.WriteLine("Hello World!");
        }
    }
}
