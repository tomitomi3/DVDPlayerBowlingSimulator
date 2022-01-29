using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demos.DiskBowling
{
    /// <summary>
    /// 条件の読み込みのインターフェース
    /// </summary>
    interface IParamSetting
    {
        /// <summary>
        /// 初期化
        /// </summary>
        public void Init();

        /// <summary>
        /// 更新
        /// </summary>
        public void Update();

        /// <summary>
        /// 読み込み
        /// </summary>
        public void Load();
    }
}