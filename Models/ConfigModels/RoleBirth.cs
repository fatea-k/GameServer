

using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Newtonsoft.Json;
using System;

namespace GameServer.Models
{
    public class RoleBirth
    {
        /// <summary>
        /// 出身id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 出身
        /// </summary>
        public string ?Title { get; set; }


        /// <summary>
        /// 品质
        /// </summary>
        public int Quality { get; set; }

        /// <summary>
        /// 性别
        /// </summary>
        public int Sex { get; set; }

        /// <summary>
        /// 随机概率
        /// </summary>
        public int Odds { get; set; }

    }
}