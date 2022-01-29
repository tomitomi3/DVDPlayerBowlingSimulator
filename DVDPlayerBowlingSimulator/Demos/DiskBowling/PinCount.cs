using BepuPhysics;
using BepuPhysics.Collidables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Demos.DiskBowling
{
    /// <summary>
    /// ピンカウントクラス
    /// </summary>
    internal class PinCount
    {
        /// <summary>
        /// ピンの状態遷移
        /// </summary>
        private enum HandleState
        {
            /// <summary>ピンは立っている</summary>
            STANDPIN_EXIST_HANDLE,
            /// <summary>ピンは倒れており、世界に存在</summary>
            KNOCKPIN_EXIST_HANDLE,
            /// <summary>ピンは倒れており、世界に存在しない</summary>
            KNOCKPIN_NOT_EXIST_HANDLE
        }

        private List<List<BodyHandle>> pinHandles = null;
        private List<List<TypedIndex>> pinShapes = null;
        private List<HandleState> pinState = null;
        private List<Vector3> initialPinTopPos = null;
        Simulation simulation = null;

        /// <summary>
        /// ctor
        /// </summary>
        public PinCount(Simulation simulation)
        {
            Init();
            this.simulation = simulation;
        }

        /// <summary>
        /// initialize
        /// </summary>
        public void Init()
        {
            //pin handle
            this.pinHandles = new List<List<BodyHandle>>();
            this.initialPinTopPos = new List<Vector3>();
            this.pinShapes = new List<List<TypedIndex>>();

            //pin state
            this.pinState = new List<HandleState>();
            for (int i = 0; i < 10; i++) pinState.Add(HandleState.STANDPIN_EXIST_HANDLE);
        }

        /// <summary>
        /// add handle
        /// </summary>
        /// <param name="bodyHandles"></param>
        public void AddShapeHandle(List<BodyHandle> bodyHandles)
        {
            this.pinHandles.Add(bodyHandles);
            this.initialPinTopPos.Add(simulation.Bodies.GetBodyReference(bodyHandles[bodyHandles.Count - 1]).Pose.Position);
        }

        /// <summary>
        /// 残っているピン数を調べる
        /// </summary>
        /// <returns></returns>
        public int GetKnockPinCount(double topYPram = 11.0, double moveXZ = 5.0)
        {
            this.UpdatePinState(topYPram);
            int count = 10;
            foreach (var state in pinState)
            {
                if (state == HandleState.KNOCKPIN_EXIST_HANDLE & state == HandleState.KNOCKPIN_EXIST_HANDLE)
                {
                    count--;
                }
            }
            return count;
        }

        /// <summary>
        /// 倒れたピンのハンドルを取得
        /// </summary>
        /// <param name="knockParam"></param>
        /// <returns></returns>
        public Tuple<List<List<BodyHandle>>,List<List<TypedIndex>>, List<int>> GetKnockPinHandle(double topYPram = 11.0, double moveXZ = 5.0)
        {
            this.UpdatePinState(topYPram, moveXZ);

            var handles = new List<List<BodyHandle>>();
            var shapes = new List<List<TypedIndex>>();
            var knockPinNo = new List<int>();
            for (int i = 0; i < this.pinState.Count; i++)
            {
                if (this.pinState[i] == HandleState.KNOCKPIN_EXIST_HANDLE)
                {
                    handles.Add(this.pinHandles[i]);
                    shapes.Add(this.pinShapes[i]);
                    this.pinState[i] = HandleState.KNOCKPIN_NOT_EXIST_HANDLE;
                }
                if (this.pinState[i] != HandleState.STANDPIN_EXIST_HANDLE)
                {
                    knockPinNo.Add(i);
                }
            }
            return new Tuple<List<List<BodyHandle>>, List<List<TypedIndex>>, List<int>>(handles, shapes, knockPinNo);
        }

        /// <summary>
        /// ピン倒れた判定
        /// </summary>
        private void UpdatePinState(double topYPram = 11.0, double moveXZ = 5.0)
        {
            for (int i = 0; i < pinHandles.Count; i++)
            {
                if (this.pinState[i] == HandleState.KNOCKPIN_NOT_EXIST_HANDLE)
                {
                    continue;
                }

                //倒された判定1
                var top = pinHandles[i][pinHandles[i].Count - 1];
                var pos = simulation.Bodies.GetBodyReference(top).Pose.Position;
                var y = pos.Y;
                if (y < (float)topYPram)
                {
                    this.pinState[i] = HandleState.KNOCKPIN_EXIST_HANDLE;
                    continue;
                }

                //倒された判定2 5cmも動いたらピンは倒れたと判定
                var diffPos = this.initialPinTopPos[i] - pos;
                if (MathF.Abs(diffPos.X) > (float)moveXZ || MathF.Abs(diffPos.Z) > (float)moveXZ)
                {
                    this.pinState[i] = HandleState.KNOCKPIN_EXIST_HANDLE;
                    continue;
                }
            }
        }

        public void AddShape(List<TypedIndex> pinShapesPerPin)
        {
            this.pinShapes.Add(pinShapesPerPin);
        }
    }
}
