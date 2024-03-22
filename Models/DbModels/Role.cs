//using Newtonsoft.Json;
//using System;

namespace GameServer.Models
{
    public class Role
    {
        /// <summary>
        /// 角色ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 角色名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 角色创建时间
        /// <summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 最后登录时间
        /// <summary>
        public DateTime LastLoginTime { get; set; }

        /// <summary>
        /// 最后下线时间
        /// <summary>
        public DateTime LastOfflineTime { get; set; }

        /// <summary>
        /// 角色等级
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// 角色修为
        /// </summary>
        public int Exp { get; set; }

        #region 初始根基属性

        /// <summary>
        /// 初始根骨
        /// </summary>
        public int InitialGenGu { get; set; }

        /// <summary>
        /// 初始力道
        /// </summary>
        public int InitialLiDao { get; set; }

        /// <summary>
        /// 初始灵觉
        /// </summary>
        public int InitialLingJue { get; set; }

        ///<summary>
        ///初始身法
        /// </summary>
        public int InitialShenFa { get; set; }

        /// <summary>
        /// 初始神识
        /// </summary>
        public int InitialShenShi { get; set; }

       
        /// <summary>
        /// 初始悟性
        /// </summary>
        public int InitialWuXing { get; set; }

        #endregion

        #region 当前根基

        /// <summary>
        /// 当前根骨
        /// </summary>
       public int GenGu { get; set; }

        /// <summary>
        /// 当前力道
        /// </summary>
        public int LiDao { get; set; }

        /// <summary>
        /// 当前灵觉
        /// </summary>
        public int LingJue { get; set; }

        ///<summary>
        ///当前身法
        /// </summary>
        public int ShenFa { get; set; }

        /// <summary>
        /// 当前神识
        /// </summary>
        public int ShenShi { get; set; }

        /// <summary>
        ///当前悟性
        /// </summary>
        public int WuXing { get; set; }


        #endregion

        /// <summary>
        /// 增益特性id列表
        /// </summary>
        public List<int> BenefitsIds { get; set; }

        /// <summary>
        /// 减益特性id列表
        /// </summary>
        public List<int> PenaltiesIds { get; set; }


    }
}