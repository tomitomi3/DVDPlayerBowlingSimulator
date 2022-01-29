﻿using DemoContentLoader;
using DemoRenderer;
using Demos.Demos;
using Demos.Demos.Cars;
using Demos.Demos.Characters;
using Demos.Demos.Sponsors;
using Demos.Demos.Tanks;
using Demos.SpecializedTests;
using Demos.SpecializedTests.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace Demos
{
    /// <summary>
    /// Constructs a demo from the set of available demos on demand.
    /// </summary>
    public class DemoSet
    {
        struct Option
        {
            public string Name;
            public Func<ContentArchive, Camera, RenderSurface, Demo> Builder;
        }

        List<Option> options = new();
        void AddOption<T>() where T : Demo, new()
        {
            options.Add(new Option
            {
                Builder = (content, camera, surface) =>
                {
                    //Note that the actual work is done in the Initialize function rather than a constructor.
                    //The 'new T()' syntax actually uses reflection and repackages exceptions in an inconvenient way.
                    //By using Initialize instead, the stack trace and debugger will go right to the source.
                    var demo = new T();
                    demo.LoadGraphicalContent(content, surface);
                    demo.Initialize(content, camera);
                    return demo;
                },
                Name = typeof(T).Name
            });
        }

        public DemoSet()
        {
            AddOption<Demos.DiskBowling>();
            AddOption<CarDemo>();
            AddOption<CharacterDemo>();
            AddOption<TankDemo>();
            AddOption<ColosseumDemo>(); //3
            AddOption<PyramidDemo>();
            AddOption<RagdollDemo>();
            AddOption<ContinuousCollisionDetectionDemo>();
            AddOption<ClothDemo>();     //7
            AddOption<NewtDemo>();
            AddOption<PlanetDemo>();
            AddOption<CompoundTestDemo>();  //10
            AddOption<BlockChainDemo>();
            AddOption<RayCastingDemo>();
            AddOption<SweepDemo>();
            AddOption<RopeStabilityDemo>();
            AddOption<SubsteppingDemo>();   //15
            AddOption<ContactEventsDemo>();
            AddOption<CollisionQueryDemo>();
            AddOption<SolverContactEnumerationDemo>();
            AddOption<CustomVoxelCollidableDemo>();
            AddOption<SponsorDemo>();
            AddOption<BouncinessDemo>();//add
            AddOption<ConstraintTestDemo>();    //23
            AddOption<GyroscopeTestDemo>();//24
            AddOption<CylinderTestDemo>();//25
            AddOption<ConvexHullTestDemo>();
            AddOption<MeshMeshTestDemo>();
            AddOption<ShapePileTestDemo>();
            AddOption<PlanetDemo>();
        }

        public int Count { get { return options.Count; } }

        public string GetName(int index)
        {
            return options[index].Name;
        }

        public Demo Build(int index, ContentArchive content, Camera camera, RenderSurface surface)
        {
            return options[index].Builder(content, camera, surface);
        }
    }
}
