using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demos.DiskBowling
{
    internal class PinStateSetting : IParamSetting
    {
        /// <summary>
        /// 投射回数
        /// </summary>
        public int ShootCount = 0;

        /// <summary>立っているピンの情報</summary>
        public int[] StandPins = new int[10] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };

        /// <summary>ファイル名</summary>
        public const string FileName = "PinStateSetting.json";

        /// <inheritdoc/>
        public void Init()
        {
            this.ShootCount = 0;
            this.StandPins = new int[10] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
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
                var ret = Newtonsoft.Json.JsonConvert.DeserializeObject<PinStateSetting>(readJsonStr.ToString());
                this.ShootCount = ret.ShootCount;
                this.StandPins = ret.StandPins;
            }
        }
    }
}
