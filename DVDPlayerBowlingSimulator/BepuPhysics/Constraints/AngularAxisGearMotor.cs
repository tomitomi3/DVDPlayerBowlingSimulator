﻿using BepuPhysics.CollisionDetection;
using BepuUtilities;
using BepuUtilities.Memory;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using static BepuUtilities.GatherScatter;

namespace BepuPhysics.Constraints
{
    /// <summary>
    /// Constrains body B's angular velocity around an axis anchored to body A to equal body A's velocity around that axis with a scaling factor applied.
    /// </summary>
    public struct AngularAxisGearMotor : ITwoBodyConstraintDescription<AngularAxisGearMotor>
    {
        /// <summary>
        /// Axis of rotation in body A's local space.
        /// </summary>
        public Vector3 LocalAxisA;
        /// <summary>
        /// <para>Scale to apply to body A's velocity around the axis to get body B's target velocity.</para>
        /// <para>In other words, a VelocityScale of 2 means that body A could have a velocity of 3 while body B has a velocity of 6.</para>
        /// </summary>
        public float VelocityScale;
        /// <summary>
        /// Motor control parameters.
        /// </summary>
        public MotorSettings Settings;

        public readonly int ConstraintTypeId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return AngularAxisGearMotorTypeProcessor.BatchTypeId;
            }
        }

        public readonly Type TypeProcessorType => typeof(AngularAxisGearMotorTypeProcessor);

        public readonly void ApplyDescription(ref TypeBatch batch, int bundleIndex, int innerIndex)
        {
            ConstraintChecker.AssertUnitLength(LocalAxisA, nameof(AngularAxisGearMotor), nameof(LocalAxisA));
            ConstraintChecker.AssertValid(Settings, nameof(AngularAxisGearMotor));
            Debug.Assert(ConstraintTypeId == batch.TypeId, "The type batch passed to the description must match the description's expected type.");
            ref var target = ref GetOffsetInstance(ref Buffer<AngularAxisGearMotorPrestepData>.Get(ref batch.PrestepData, bundleIndex), innerIndex);
            Vector3Wide.WriteFirst(LocalAxisA, ref target.LocalAxisA);
            GetFirst(ref target.VelocityScale) = VelocityScale;
            MotorSettingsWide.WriteFirst(Settings, ref target.Settings);
        }

        public readonly void BuildDescription(ref TypeBatch batch, int bundleIndex, int innerIndex, out AngularAxisGearMotor description)
        {
            Debug.Assert(ConstraintTypeId == batch.TypeId, "The type batch passed to the description must match the description's expected type.");
            ref var source = ref GetOffsetInstance(ref Buffer<AngularAxisGearMotorPrestepData>.Get(ref batch.PrestepData, bundleIndex), innerIndex);
            Vector3Wide.ReadFirst(source.LocalAxisA, out description.LocalAxisA);
            description.VelocityScale = GetFirst(ref source.VelocityScale);
            MotorSettingsWide.ReadFirst(source.Settings, out description.Settings);
        }
    }

    public struct AngularAxisGearMotorPrestepData
    {
        public Vector3Wide LocalAxisA;
        public Vector<float> VelocityScale;
        public MotorSettingsWide Settings;
    }

    public struct AngularAxisGearMotorProjection
    {
        public Vector3Wide NegatedVelocityToImpulseB;
        public Vector<float> VelocityScale;
        public Vector<float> SoftnessImpulseScale;
        public Vector<float> MaximumImpulse;
        public Vector3Wide ImpulseToVelocityA;
        public Vector3Wide NegatedImpulseToVelocityB;
    }


    public struct AngularAxisGearMotorFunctions : IConstraintFunctions<AngularAxisGearMotorPrestepData, AngularAxisGearMotorProjection, Vector<float>>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Prestep(Bodies bodies, ref TwoBodyReferences bodyReferences, int count, float dt, float inverseDt, ref BodyInertias inertiaA, ref BodyInertias inertiaB,
            ref AngularAxisGearMotorPrestepData prestep, out AngularAxisGearMotorProjection projection)
        {
            //Velocity level constraint that acts directly on the given axes. Jacobians just the axes, nothing complicated. 1DOF, so we do premultiplication.
            //This is mildly more complex than the AngularAxisMotor:
            //dot(wa, axis) - dot(wb, axis) * velocityScale = 0, so jacobianB is actually -axis * velocityScale, not just -axis.
            bodies.GatherOrientation(ref bodyReferences, count, out var orientationA, out var orientationB);
            QuaternionWide.TransformWithoutOverlap(prestep.LocalAxisA, orientationA, out var axis);
            Vector3Wide.Scale(axis, prestep.VelocityScale, out var jA);
            Symmetric3x3Wide.TransformWithoutOverlap(jA, inertiaA.InverseInertiaTensor, out projection.ImpulseToVelocityA);
            Vector3Wide.Dot(jA, projection.ImpulseToVelocityA, out var contributionA);
            Symmetric3x3Wide.TransformWithoutOverlap(axis, inertiaB.InverseInertiaTensor, out projection.NegatedImpulseToVelocityB);
            Vector3Wide.Dot(axis, projection.NegatedImpulseToVelocityB, out var contributionB);
            MotorSettingsWide.ComputeSoftness(prestep.Settings, dt, out var effectiveMassCFMScale, out projection.SoftnessImpulseScale, out projection.MaximumImpulse);
            var effectiveMass = effectiveMassCFMScale / (contributionA + contributionB);

            Vector3Wide.Scale(axis, effectiveMass, out projection.NegatedVelocityToImpulseB);
            projection.VelocityScale = prestep.VelocityScale;

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ApplyImpulse(ref Vector3Wide angularVelocityA, ref Vector3Wide angularVelocityB, in AngularAxisGearMotorProjection projection, in Vector<float> csi)
        {
            Vector3Wide.Scale(projection.ImpulseToVelocityA, csi, out var velocityChangeA);
            Vector3Wide.Scale(projection.NegatedImpulseToVelocityB, csi, out var negatedVelocityChangeB);
            Vector3Wide.Add(angularVelocityA, velocityChangeA, out angularVelocityA);
            Vector3Wide.Subtract(angularVelocityB, negatedVelocityChangeB, out angularVelocityB);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WarmStart(ref BodyVelocities velocityA, ref BodyVelocities velocityB, ref AngularAxisGearMotorProjection projection, ref Vector<float> accumulatedImpulse)
        {
            ApplyImpulse(ref velocityA.Angular, ref velocityB.Angular, projection, accumulatedImpulse);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Solve(ref BodyVelocities velocityA, ref BodyVelocities velocityB, ref AngularAxisGearMotorProjection projection, ref Vector<float> accumulatedImpulse)
        {
            //csi = projection.BiasImpulse - accumulatedImpulse * projection.SoftnessImpulseScale - (csiaLinear + csiaAngular + csibLinear + csibAngular);
            Vector3Wide.Dot(velocityA.Angular, projection.NegatedVelocityToImpulseB, out var unscaledCSIA);
            Vector3Wide.Dot(velocityB.Angular, projection.NegatedVelocityToImpulseB, out var negatedCSIB);
            var csi = -accumulatedImpulse * projection.SoftnessImpulseScale - (unscaledCSIA * projection.VelocityScale - negatedCSIB);
            ServoSettingsWide.ClampImpulse(projection.MaximumImpulse, ref accumulatedImpulse, ref csi);
            ApplyImpulse(ref velocityA.Angular, ref velocityB.Angular, projection, csi);

        }

    }

    public class AngularAxisGearMotorTypeProcessor : TwoBodyTypeProcessor<AngularAxisGearMotorPrestepData, AngularAxisGearMotorProjection, Vector<float>, AngularAxisGearMotorFunctions>
    {
        public const int BatchTypeId = 54;
    }
}

