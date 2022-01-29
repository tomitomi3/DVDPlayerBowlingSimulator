using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using BepuUtilities;

namespace Demos.DiskBowling
{
    /// <summary>
    /// ディスク射出条件
    /// </summary>
    internal class ShootDiskSettings : IParamSetting
    {
        /// <summary>射出初期位置 [cm]</summary>
        public Vector3 Pos = new Vector3(-9, 7, -100);

        /// <summary>ディスク初期速度 [cm/s]</summary>
        public Vector3 Velocity = new Vector3(40, 0, 1000);

        /// <summary>ディスク初期回転速度 [rad/s]</summary>
        public Vector3 Rotate = new Vector3(0, -100, 0);

        /// <summary>ディスクの向き [deg]</summary>
        public Vector3 OrientationPitchYawRollDegree = new Vector3(0, 0, 0);

        /// <summary>カメラ位置</summary>
        public Vector3 CameraPos = new Vector3(0, 40, -50);

        /// <summary>カメラ位置 ピッチ</summary>
        public float CameraPitch = 0.5f; // MathHelper.Pi / 2f;

        /// <summary>カメラ位置 ヨー</summary>
        public float CameraYaw = 3.1415925f; // - MathHelper.Pi;

        /// <summary>FPS</summary>
        public float FPS = 60;

        /// <summary>時間分解能比率 1/fps*timeResolutionCoeff</summary>
        public float timeResolutionCoeff = 4;

        /// <summary>timestepper</summary>
        public int TimeStepper = 1;

        /// <summary>timestepper</summary>
        public int SubStepCount = 10;

        /// <summary>pin damping</summary>
        public float PinDampingratioWeld = 0.0f;

        /// <summary>pin mass</summary>
        public float PinTotalMass = 30.0f;

        /// <summary>質量比</summary>
        public float PinBowlingMassRatio = 1.0f;

        /// <summary>抵抗</summary>
        public float linearDamping = 0.2f;

        /// <summary>抵抗</summary>
        public float angularDamping = 0.2f;

        /// <summary>ボウリングっぽく</summary>
        public bool IsBowllIsDisk = true;

        /// <summary>質量と速度の比率</summary>
        public float MassSpeedRatio = 1;

        /// <summary>2投目の復元</summary>
        public bool Is2ndShot = false;

        /// <summary>ファイル名</summary>
        public const string FileName = "ShootDiskSettings.json";

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ShootDiskSettings() { }

        /// <inheritdoc/>
        public void Init()
        {
            //nop
        }

        /// <inheritdoc/>
        public void Update()
        {
            //設定ファイルが無かった
            using (var sw = new System.IO.StreamWriter(FileName, false))
            {
                var outStr = Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
                sw.Write(outStr);
            }
        }

        /// <inheritdoc/>
        public void Load()
        {
            //ディスクの初期状態
            //設定ファイルで読み込み
            var readJsonStr = new System.Text.StringBuilder();

            if (System.IO.File.Exists(FileName) == true)
            {
                using (var sw = new System.IO.StreamReader(FileName))
                {
                    while (sw.EndOfStream == false)
                    {
                        readJsonStr.Append(sw.ReadLine());
                    }
                }
            }

            if (readJsonStr.Length == 0)
            {
                //設定ファイルが無かった
                using (var sw = new System.IO.StreamWriter(FileName, false))
                {
                    var outStr = Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
                    sw.Write(outStr);
                }
            }
            else
            {
                var ret = Newtonsoft.Json.JsonConvert.DeserializeObject<ShootDiskSettings>(readJsonStr.ToString());
                this.Pos = ret.Pos;
                this.Velocity = ret.Velocity;
                this.Rotate = ret.Rotate;
                this.OrientationPitchYawRollDegree = ret.OrientationPitchYawRollDegree;
                this.CameraPos = ret.CameraPos;
                this.CameraPitch = ret.CameraPitch;
                this.CameraYaw = ret.CameraYaw;
                this.FPS = ret.FPS;
                this.timeResolutionCoeff = ret.timeResolutionCoeff;
                this.TimeStepper = ret.TimeStepper;
                this.SubStepCount = ret.SubStepCount;
                this.PinDampingratioWeld = ret.PinDampingratioWeld;
                this.PinTotalMass = ret.PinTotalMass;
                this.PinBowlingMassRatio = ret.PinBowlingMassRatio;
                this.linearDamping = ret.linearDamping;
                this.angularDamping = ret.angularDamping;
                this.IsBowllIsDisk = ret.IsBowllIsDisk;
                this.MassSpeedRatio = ret.MassSpeedRatio;
                this.Is2ndShot = ret.Is2ndShot;
            }
        }
    }
}
