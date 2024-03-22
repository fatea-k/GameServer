using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Models
{
    /// <summary>
    /// 角色
    /// </summary>
    public class S2C_CreateRole
    {
        /// <summary>
        /// 角色ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 角色性别
        /// </summary>
        public int Sex { get; set; }

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
