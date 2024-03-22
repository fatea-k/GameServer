using GameServer.Attributes;
using GameServer.Managers;
using GameServer.Models;
using GameServer.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Services
{
    [Service(ServiceLifetime.Scoped)]
    public class RoleService
    {
        //注入configManager
        private readonly ConfigManager _configManager;
        //构造方法
        public RoleService(
            ConfigManager configManager

            )
        {
            _configManager = configManager;
        }


        //随机一个角色出身
        public async Task<S2C_CreateRole> GetRoleBirth()
        {
            //创建出身
            S2C_CreateRole createRole = new S2C_CreateRole();

            //可以获得的增益效果数量
            int addCount = 0;
            //可以获得的减益效果数量
            int reduceCount = 0;


            //通过权重获得角色出身
            var rolebirth = _configManager.GetConfig<RoleBirth>().Where(t => t.Odds > 0).Random(t => t.Odds);
            //当sex为0时,为女性,1为男性,-1为未知
            if (rolebirth.Sex < 0)
            {
                //随机性别
                // 实例化随机数生成器
                Random rand = new Random();
                rolebirth.Sex = rand.Next(0, 2);
            }

            createRole.Id = rolebirth.Id;
            createRole.Sex = rolebirth.Sex;

            //根据出身的品质,获得增益和减益效果的数量
            var roleQuality = GameConfigManager.GetConfigValue<PinzhitexingConfig>("pinzhitexingConfig", rolebirth.Quality.ToString());
            var GenJiType= new List<int>();

            //获得增益效果
            var BenefitsIds = _configManager.GetConfig<RoleTexing>()
                //按照条件筛选
                .Where(t =>
                t.MinQuality <= rolebirth.Quality       /*角色品质大于等于最小要求品质*/
                && t.MaxQuality >= rolebirth.Quality    /*角色品质小于等于最大要求品质*/
                && t.BuffType == 1);                   /*特性类型:增益*/

            if (BenefitsIds.Count() > 0 && roleQuality.BenefitsCount > 0)
            {

                //随机获得指定数量的效果
                BenefitsIds = BenefitsIds.Random(
                 null,                                   /*无权重*/
                 roleQuality.BenefitsCount,              /*配置:当前品质允许的增益数量*/
                 t => t.GenJiType);                       /*限定不重复字段:根基类型*/


                createRole.BenefitsIds = BenefitsIds.Select(t => t.Id).ToList();//返回效果Id列表,转换为List<int>

                GenJiType = BenefitsIds.Select(t => t.GenJiType).Distinct().ToList();

            }

            //获得减益效果
            var PenaltiesIds = _configManager.GetConfig<RoleTexing>()
                 //按照条件筛选
                 .Where(t =>
                  t.MinQuality <= rolebirth.Quality      /*角色品质大于等于最小要求品质*/
                  && t.MaxQuality >= rolebirth.Quality   /*角色品质小于等于最大要求品质*/
                  && t.BuffType == 2&& !GenJiType.Contains(t.GenJiType));               /*特性类型:减益 */

            if (PenaltiesIds.Count() > 0 && roleQuality.PenaltiesCount > 0)
            {
                //随机获得指定数量的效果
                PenaltiesIds = PenaltiesIds.Random(
                  null,                                  /*无权重字段*/
                  roleQuality.PenaltiesCount,            /*配置:当前品质允许的减益数量*/
                  t => t.GenJiType);                    /*限定不重复字段:根基类型*/

                createRole.PenaltiesIds = PenaltiesIds.Select(t => t.Id).ToList();//返回效果Id列表,转换为List<int>
            }





            return createRole;
        }


    }
}
