using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using BepuUtilities;
using DemoContentLoader;
using DemoRenderer;
using DemoRenderer.UI;
using Demos.DiskBowling;
using DemoUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Demos.Demos
{
    /// <summary>
    /// ディスクボーリングシミュレータ
    /// </summary>
    internal class DiskBowling : Demo
    {
        #region "constant parameters"
        /*
         * パラメータ実験
         * 初速度 v_x=0, v_y=0, v_z=10000 -> 0.1s後  x=0  , y=0  , z=989（抵抗による減速）
         * BodyVelocityで設定した値は微小時間dtを累積して1（秒）で進む距離。角速度も同様
         */

        /// <summary>重力加速度 m/s^2</summary>
        float G = 9.8665f;

        /// <summary>ディスク半径</summary>
        float DISK_RAIUS = 6.0f;

        /// <summary>ディスク厚さ</summary>
        float DISK_THICKNESS = 0.12f;

        /// <summary>ディスク重さ</summary>
        float DISK_MASS = 16.0f;

        /// <summary>ピン質量</summary>
        double pinMass = 0.0;

        /// <summary>ピン配置 正三角形の辺の長さ</summary>
        double triangleEdgeLength = 15.0;

        /// <summary>ピンのみの重さ</summary>
        const double PIN_WEIGHT = 18.0;
        #endregion

        #region "parameters"
        /// <summary>ディスクハンドル</summary>
        public BodyHandle diskHandle;

        /// <summary>ディスク形状Index</summary>
        public TypedIndex diskShape;

        /// <summary>カメラ位置No</summary>
        int isSwitchCameraAngle = 0;

        /// <summary>シミュレータ設定条件</summary>
        private ShootDiskSettings shootConditions = new ShootDiskSettings();

        /// <summary>ピン設定</summary>
        private PinStateSetting standPinState = new PinStateSetting();

        /// <summary>ピン状態判定</summary>
        public PinCount pinCount = null;

        /// <summary>ループカウント デバッグ</summary>
        int outputCount = 0;

        /// <summary>ディスクの存在フラグ</summary>
        public bool isExistDisk = false;

        /// <summary>微小時間dtの積分 デバッグ</summary>
        public float integralTime = 0.0f;
        #endregion

        /// <summary>
        /// カメラ位置
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="pattern"></param>
        private void SetCameraPos(ref Camera camera, int pattern)
        {
            if (pattern == 0)
            {
                camera.Position = new Vector3(0, 80, 0);
                camera.Pitch = MathHelper.Pi / 2f;
                camera.Yaw = -MathHelper.Pi;
            }
            else if (pattern == 1)
            {
                camera.Position = new Vector3(-80, 30, 0);
                camera.Pitch = 0f;
                camera.Yaw = MathHelper.Pi / 2f;
            }
            else if (pattern == 2)
            {
                camera.Position = new Vector3(0, 40, -50);
                camera.Pitch = 0.5f;
                camera.Yaw = -MathHelper.Pi;
            }
        }

        /// <summary>
        /// gen pin posisitons
        /// </summary>
        /// <returns></returns>
        private List<Vector3> GenPinPos()
        {
            //正三角形の頂点に対応する
            //   /|　直角三角形
            //  /_|  斜辺^2 = 底辺^2 + 高さ^2 -> 高さ = sqrt(斜辺^2-底辺^2)
            var pinPoss = new List<Vector3>();
            var zPich = Math.Sqrt(Math.Pow(this.triangleEdgeLength, 2) - Math.Pow(this.triangleEdgeLength / 2.0, 2));
            var xPich = (float)(this.triangleEdgeLength / 2.0);
            float yPos = 0f;
            //1段目
            {
                pinPoss.Add(new Vector3(0, yPos, 0));
            }
            //2段目 2
            {
                float zPos = (float)(zPich);
                pinPoss.Add(new Vector3(xPich, yPos, zPos));    //2
                pinPoss.Add(new Vector3(-xPich, yPos, zPos));   //3
            }
            //3段目 3
            {
                float zPos = (float)(zPich * 2.0);
                float xPosStart = (float)triangleEdgeLength;
                pinPoss.Add(new Vector3(xPosStart, yPos, zPos));
                xPosStart -= (float)triangleEdgeLength;
                pinPoss.Add(new Vector3(xPosStart, yPos, zPos));
                xPosStart -= (float)triangleEdgeLength;
                pinPoss.Add(new Vector3(xPosStart, yPos, zPos));
            }
            //4段目 4
            {
                float xPosStart = (float)(triangleEdgeLength + triangleEdgeLength / 2.0);
                float zPos = (float)(zPich * 3.0);
                pinPoss.Add(new Vector3(xPosStart, yPos, zPos));
                xPosStart -= (float)triangleEdgeLength;
                pinPoss.Add(new Vector3(xPosStart, yPos, zPos));
                xPosStart -= (float)triangleEdgeLength;
                pinPoss.Add(new Vector3(xPosStart, yPos, zPos));
                xPosStart -= (float)triangleEdgeLength;
                pinPoss.Add(new Vector3(xPosStart, yPos, zPos));
            }
            return pinPoss;
        }

        /// <summary>
        /// ピン初期化
        /// </summary>
        private void InitPin()
        {
            //ピンの内部は均一出は無いため円柱を連結してそれぞれ円柱の重さを変えることで模倣
            //円柱階層構造
            var pinShapes = new List<Cylinder>();
            var collidableDescriptions = new List<CollidableDescription>();
            var pinMasss = new List<float>();

            //ピン本体のみの重さから「重り」の重さを計算
            var weight = shootConditions.PinTotalMass - PIN_WEIGHT;
            var cylinderSurfaceWeight = PIN_WEIGHT / 4.0; //4分割 表面積が違うので厳密ではないが均等とする
            weight = weight + cylinderSurfaceWeight;

            //ピン階層構造定義 下から円柱を積み上げ
            //  |   |
            // |     |
            //   | |
            //  |   |
            pinShapes.Add(new Cylinder(1.75f, 3f));
            pinMasss.Add((float)(weight * shootConditions.MassSpeedRatio));
            pinShapes.Add(new Cylinder(3f, 7f));
            pinMasss.Add((float)(cylinderSurfaceWeight * shootConditions.MassSpeedRatio));
            pinShapes.Add(new Cylinder(1.25f, 3f));
            pinMasss.Add((float)(cylinderSurfaceWeight * shootConditions.MassSpeedRatio));
            pinShapes.Add(new Cylinder(1.5f, 5f));
            pinMasss.Add((float)(cylinderSurfaceWeight * shootConditions.MassSpeedRatio));

            //重さ
            this.pinMass = pinMasss.Sum();
            var pinheight = 0.0f;
            foreach (var h in pinShapes) pinheight += h.Length;
            Console.WriteLine("Pin Height = {0}", pinheight);
            Console.WriteLine("Pin Mas    = {0}", pinMass);

            //物体の重さ、慣性などを定義
            var bodyInertias = new List<BodyInertia>();
            for (int i = 0; i < pinShapes.Count; i++)
            {
                collidableDescriptions.Add(new CollidableDescription(Simulation.Shapes.Add(pinShapes[i]), 0.1f));//投機的 default 0.1f
                pinShapes[i].ComputeInertia(pinMasss[i], out var tempInertia);
                bodyInertias.Add(tempInertia);
            }

            //設定
            var activity = new BodyActivityDescription(0.5f);//sleepになる閾値 default 0.01
                                                             //0.5f FirstTimeStepper, Spring 240で静止状態は安定 接触で安定

            //ピン位置リスト取得
            var pinPoss = this.GenPinPos();

            //ピン位置設定
            for (int pinNo = 0; pinNo < pinPoss.Count; pinNo++)
            {
                if (!(standPinState.StandPins[pinNo] == 1))
                {
                    continue;
                }

                //1本ピンを定義
                var bodyDescriptions = new List<BodyDescription>();
                var pinShapesPerPin = new List<TypedIndex>();
                var sumHeight = 0f;
                for (int i = 0; i < pinShapes.Count; i++)
                {
                    var dHeight = pinShapes[i].HalfLength + sumHeight;
                    var shapePos = new Vector3(pinPoss[pinNo].X, dHeight, pinPoss[pinNo].Z);

                    bodyDescriptions.Add(BodyDescription.CreateDynamic(shapePos, bodyInertias[i], collidableDescriptions[i], activity));
                    pinShapesPerPin.Add(bodyDescriptions[i].Collidable.Shape);

                    //次の高さ
                    sumHeight += pinShapes[i].Length;
                }

                //ハンドル取得
                var bodyHandles = new List<BodyHandle>();
                foreach (var description in bodyDescriptions)
                {
                    bodyHandles.Add(Simulation.Bodies.Add(description));
                }

                //拘束条件の追加⇒2つの物体に拘束条件を追加 物体はつながっている
                var baseHandle = bodyHandles[0];
                sumHeight = 0f;
                for (int i = 1; i < bodyHandles.Count; i++)
                {
                    var weldOffset = (pinShapes[0].Length + pinShapes[i].Length) / 2f + sumHeight;
                    var tempWeld = new Weld
                    {
                        LocalOffset = new Vector3(0, weldOffset, 0),
                        LocalOrientation = Quaternion.Identity,
                        //上げすぎると発振 FPSの半分程度か、TimeStepperによる
                        //SpringSettings = new SpringSettings(Demo.FPS / 1.5f, 0)
                        SpringSettings = new SpringSettings(this.FPS * this.timeResolutionCoeff / 2f, shootConditions.PinDampingratioWeld)
                    };
                    Simulation.Solver.Add(baseHandle, bodyHandles[i], tempWeld);

                    //次の高さ
                    sumHeight += pinShapes[i].Length;
                }

                //ハンドルの保存
                pinCount.AddShapeHandle(bodyHandles);
                pinCount.AddShape(pinShapesPerPin);
            }
        }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="content"></param>
        /// <param name="camera"></param>
        public unsafe override void Initialize(ContentArchive content, Camera camera)
        {
            //https://github.com/bepu/bepuphysics2/blob/master/Documentation/GettingStarted.md
            //https://github.com/bepu/bepuphysics1/blob/master/Documentation/JointsAndConstraints.md

            //設定値の反映
            {
                //Load
                shootConditions.Load();

                //カメラ位置の保存
                camera.Position = shootConditions.CameraPos;
                camera.Pitch = shootConditions.CameraPitch;
                camera.Yaw = shootConditions.CameraYaw;

                //初期化
                this.isExistDisk = false;
                this.integralTime = 0.0f;

                //シミュレータのFPS
                this.FPS = shootConditions.FPS;
                this.timeResolutionCoeff = shootConditions.timeResolutionCoeff;

                //カメラ位置
                camera.Position = shootConditions.CameraPos;
                camera.Pitch = shootConditions.CameraPitch;
                camera.Yaw = shootConditions.CameraYaw;

                //ピン状態の再現
                this.standPinState.Init();
                if (this.shootConditions.Is2ndShot == true)
                {
                    this.standPinState.Load();
                    if (this.standPinState.ShootCount != 1)
                    {
                        this.standPinState.Init();
                        this.standPinState.Update();
                    }
                }
            }

            //シミュレータの設定 重力加速度 並進移動に対する抵抗、角度（回転に対する）抵抗->摩擦係数、空気抵抗がすべて丸め込まれている
            float linearDamping = shootConditions.linearDamping;
            float angularDamping = shootConditions.angularDamping;
            var integratorCallback = new DemoPoseIntegratorCallbacks(new Vector3(0, -G, 0), linearDamping, angularDamping);
            if (shootConditions.TimeStepper == 0)
            {
                Simulation = Simulation.Create(BufferPool, new DemoNarrowPhaseCallbacks(), integratorCallback, new PositionFirstTimestepper());
            }
            else if (shootConditions.TimeStepper == 1)
            {
                Simulation = Simulation.Create(BufferPool, new DemoNarrowPhaseCallbacks(), integratorCallback, new PositionLastTimestepper());
            }
            else
            {
                Simulation = Simulation.Create(BufferPool, new DemoNarrowPhaseCallbacks(), integratorCallback, new SubsteppingTimestepper(shootConditions.SubStepCount));
            }

            //ピン判定クラス
            this.pinCount = new PinCount(Simulation);

            //ピン初期化
            this.InitPin();

            this.DISK_MASS = (float)(DISK_MASS / shootConditions.PinBowlingMassRatio);

            //世界の設定
            float worldThickness = 1.0f;
            var worldPos = new Vector3(0, -worldThickness / 2.0f, 0);//世界の厚さは1 -0.5しておく
            Simulation.Statics.Add(
                    new StaticDescription(worldPos, new CollidableDescription(Simulation.Shapes.Add(new Box(2500, worldThickness, 2500)), 0.1f))
                    );
        }

        /// <summary>
        /// シミュレーション世界の更新
        /// </summary>
        /// <param name="window"></param>
        /// <param name="camera"></param>
        /// <param name="input"></param>
        /// <param name="dt">微小時間</param>
        public override void Update(Window window, Camera camera, Input input, float dt)
        {
            //4
            //Window.Run() while updateHandler -> GameLoop.Update() DemoHarness.Update(dt) -> demo.Update
            //startPos = new Vector3(0, 8, -100);
            if (input != null && input.WasPushed(OpenTK.Input.Key.C))
            {
                //disk shoot count
                this.standPinState.ShootCount++;
                this.standPinState.Update();

                //load shoot condistions
                this.shootConditions.Load();

                //camera position save
                shootConditions.CameraPos = camera.Position;
                shootConditions.CameraPitch = camera.Pitch;
                shootConditions.CameraYaw = camera.Yaw;
                shootConditions.Update();

                //bow conditions
                var pitch = MathHelper.ToRadians(shootConditions.OrientationPitchYawRollDegree.X);
                var yaw = MathHelper.ToRadians(shootConditions.OrientationPitchYawRollDegree.Y);
                var roll = MathHelper.ToRadians(shootConditions.OrientationPitchYawRollDegree.Z);
                var q = Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll);
                RigidPose tempPose = new RigidPose(shootConditions.Pos, q);
                var rotateVelocity = MyRotation.Rotate(shootConditions.OrientationPitchYawRollDegree, shootConditions.Rotate);

                //verocity
                var bodyVelocity = new BodyVelocity(shootConditions.Velocity, rotateVelocity);

                //body
                float diskMasswithRatio = DISK_MASS * shootConditions.MassSpeedRatio;
                BodyDescription bodyDescription;
                if (shootConditions.IsBowllIsDisk)
                {
                    var bowl = new Cylinder(DISK_RAIUS, DISK_THICKNESS);
                    bodyDescription = BodyDescription.CreateConvexDynamic(tempPose, bodyVelocity, diskMasswithRatio, Simulation.Shapes, bowl);
                }
                else
                {
                    var bowl = new Sphere(DISK_RAIUS);
                    bodyDescription = BodyDescription.CreateConvexDynamic(tempPose, bodyVelocity, diskMasswithRatio, Simulation.Shapes, bowl);
                }
                this.diskHandle = Simulation.Bodies.Add(bodyDescription);
                this.diskShape = bodyDescription.Collidable.Shape;
                isExistDisk = true;
            }

            //カメラ位置
            if (input != null && input.WasPushed(OpenTK.Input.Key.V))
            {
                switch (isSwitchCameraAngle)
                {
                    case 0:
                        SetCameraPos(ref camera, 0);
                        isSwitchCameraAngle = 1;
                        break;
                    case 1:
                        SetCameraPos(ref camera, 1);
                        isSwitchCameraAngle = 2;
                        break;
                    case 2:
                        SetCameraPos(ref camera, 2);
                        isSwitchCameraAngle = 0;
                        break;
                }
            }

            //適当なタイミングでコンソール出力
            if ((outputCount++ % 100) == 0)
            {
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("ShootCount       " + this.standPinState.ShootCount.ToString());
                Console.WriteLine("dt               " + dt.ToString());
                Console.WriteLine("DiskPosition:    " + shootConditions.Pos.ToString());
                Console.WriteLine("DiskOrientation  " + shootConditions.OrientationPitchYawRollDegree.ToString());
                Console.WriteLine("DiskVelocity:    " + shootConditions.Velocity.ToString());
                Console.WriteLine("DiskRotate:      " + shootConditions.Rotate.ToString());
                Console.WriteLine("CameraPos:       " + camera.Position.ToString());
                Console.WriteLine("CameraPitch:     " + camera.Pitch.ToString());
                Console.WriteLine("CameraYaw:       " + camera.Yaw.ToString());
                Console.WriteLine("Knock Pin        " + (10 - this.pinCount.GetKnockPinCount()));
            }

            //1s
            {
                //var p = Simulation.Bodies.GetBodyReference(diskHandle).Pose;
                //var str = p.Position.ToString();
                //Console.WriteLine("Disk positions:{0}", str);
                //var euler = this.ToEuler(p.Orientation);
                //Console.WriteLine("Disk orientation:{0} {1} {2}", MathHelper.ToDegrees(euler.X), MathHelper.ToDegrees(euler.Y), MathHelper.ToDegrees(euler.Z));
            }
            if (isExistDisk == true)
            {
                this.integralTime += dt;
                if (integralTime > 1.0f)
                {
                    var p = Simulation.Bodies.GetBodyReference(diskHandle).Pose;
                    var str = p.Position.ToString();
                    Console.WriteLine("Disk positions:{0}", str);
                    str = p.Orientation.ToString();
                    Console.WriteLine("Disk orientation:{0}", str);
                    //航空関係では z-y-x系。Tait-Bryan
                    var euler = MyRotation.ToEuler(Simulation.Bodies.GetBodyReference(diskHandle).Pose.Orientation);
                    Console.WriteLine("Disk orientation Euler:{0} {1} {2}", MathHelper.ToDegrees(euler.X), MathHelper.ToDegrees(euler.Y), MathHelper.ToDegrees(euler.Z));

                    integralTime = 0.0f;
                }
            }

            //倒れたピンを削除
            if (input != null && input.WasPushed(OpenTK.Input.Key.F6))
            {
                if (isExistDisk == true)
                {
                    var t = this.pinCount.GetKnockPinHandle();
                    var handles = t.Item1;
                    var shapes = t.Item2;
                    var knockpin = t.Item3;
                    for (int i = 0; i < t.Item1.Count; i++)
                    {
                        foreach (var temp in shapes[i])
                        {
                            Simulation.Shapes.Remove(temp);
                        }
                        foreach (var temp in handles[i])
                        {
                            Simulation.Bodies.Remove(temp);
                        }
                    }
                    foreach (var index in knockpin)
                    {
                        standPinState.StandPins[index] = 0;
                    }
                    standPinState.Update();

                    //disk
                    Simulation.Shapes.Remove(this.diskShape);
                    Simulation.Bodies.Remove(this.diskHandle);
                    isExistDisk = false;
                    integralTime = 0.0f;
                }
            }

            base.Update(window, camera, input, dt);
        }

        /// <summary>
        /// 画面表示
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="camera"></param>
        /// <param name="input"></param>
        /// <param name="text"></param>
        /// <param name="font"></param>
        public override void Render(Renderer renderer, Camera camera, Input input, TextBuilder text, Font font)
        {
            //text.Clear().Append("Press C to launch a ball!");
            text.Clear().Append("Press some key. Eject Disk:C, Change CameraPos:V, Remove knock Pin:F6, Reset:F5");
            renderer.TextBatcher.Write(text, new Vector2(20, renderer.Surface.Resolution.Y - 20), 16, new Vector3(1, 1, 1), font);
            base.Render(renderer, camera, input, text, font);
        }
    }
}
