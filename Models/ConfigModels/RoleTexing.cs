

using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Newtonsoft.Json;
using System;

namespace GameServer.Models
{
    /// <summary>
    /// 角色特性
    /// </summary>
    public class RoleTexing
    {
        /// <summary>
        /// 出身id
        /// </summary>
        public int Id { get; set; }


        /// <summary>
        /// 特性类型(1增益，2减益)
        /// </summary>
        public int BuffType { get; set; }

        /// <summary>
        /// 根基类型分组
        /// </summary>
        public int GenJiType { get; set; }

        /// <summary>
        /// 根骨
        /// </summary>
        public int GenGu { get; set; }

        /// <summary>
        /// 力道
        /// </summary>
        public int LiDao { get; set; }

        /// <summary>
        /// 灵觉
        /// </summary>
        public int LingJue { get; set; }

        ///<summary>
        ///身法
        /// </summary>
        public int ShenFa { get; set; }

        /// <summary>
        /// 神识
        /// </summary>
        public int ShenShi { get; set; }

        /// <summary>
        ///悟性
        /// </summary>
        public int WuXing { get; set; }

        /// <summary>
        /// 最小品质需求
        /// </summary>
        public int MinQuality { get; set; }

        /// <summary>
        /// 最大品质需求
        /// </summary>
        public int MaxQuality { get; set; }

      

    }
}