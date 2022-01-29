using BepuUtilities;
using LibOptimization.MathUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Demos.DiskBowling
{
    /// <summary>
    /// 回転変換行列
    /// </summary>
    public  class MyRotation
    {
        /// <summary>
        /// ラジアンに変換
        /// </summary>
        /// <param name="deg"></param>
        /// <returns></returns>
        private static double ToRadians(double deg)
        {
            return deg * Math.PI / 180.0;
        }

        /// <summary>
        /// 度に変換
        /// </summary>
        /// <param name="rad"></param>
        /// <returns></returns>
        private static double ToDegree(double rad)
        {
            return rad * 180.0 / Math.PI;
        }

        /// <summary>
        /// 回転
        /// </summary>
        /// <param name="orientation"></param>
        /// <param name="tgt"></param>
        /// <returns></returns>
        private static DenseVector Rotate(DenseVector orientation, DenseVector tgt)
        {
            //precalculate sin, cos
            var s_x = Math.Sin(MyRotation.ToRadians(orientation[0]));
            var c_x = Math.Cos(MyRotation.ToRadians(orientation[0]));
            var s_y = Math.Sin(MyRotation.ToRadians(orientation[1]));
            var c_y = Math.Cos(MyRotation.ToRadians(orientation[1]));
            var s_z = Math.Sin(MyRotation.ToRadians(orientation[2]));
            var c_z = Math.Cos(MyRotation.ToRadians(orientation[2]));

            //Rx Ry Rz
            var rx = new DenseMatrix(new double[3][] { new double[] { 1, 0, 0 }, new double[] { 0, c_x, -s_x }, new double[] { 0, s_x, c_x } });
            var ry = new DenseMatrix(new double[3][] { new double[] { c_y, 0, s_y }, new double[] { 0, 1, 0 }, new double[] { -s_y, 0, c_y } });
            var rz = new DenseMatrix(new double[3][] { new double[] { c_z, -s_z, 0 }, new double[] { s_z, c_z, 0 }, new double[] { 0, 0, 1 } });
            var r = rx * ry * rz;

            //乗算
            return r * tgt;
        }

        /// <summary>
        /// 回転
        /// </summary>
        /// <returns></returns>
        public static Vector3 Rotate(Vector3 orientation, Vector3 tgt)
        {
            var temp = Rotate(new DenseVector(new double[] { orientation.X, orientation.Y, orientation.Z }),
                              new DenseVector(new double[] { tgt.X, tgt.Y, tgt.Z }));

            return new Vector3()
            {
                X = (float)temp[0],
                Y = (float)temp[1],
                Z = (float)temp[2]
            };
        }

        /// <summary>
        /// クオータニオンからオイラー角へ
        /// </summary>
        /// <param name="rotation"></param>
        /// <returns></returns>
        public static Vector3 ToEuler(Quaternion rotation)
        {
            var res = new Vector3();

            double q0 = rotation.W;
            double q1 = rotation.Y;
            double q2 = rotation.X;
            double q3 = rotation.Z;

            res.X = (float)Math.Atan2(2 * (q0 * q1 + q2 * q3), 1 - 2 * (q1 * q1 + q2 * q2));
            res.Y = (float)Math.Asin(2 * (q0 * q2 - q3 * q1));
            res.Z = (float)Math.Atan2(2 * (q0 * q3 + q1 * q2), 1 - 2 * (q2 * q2 + q3 * q3));

            return res;
        }
    }
}
