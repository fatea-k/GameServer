using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Utilities
{
    /// <summary>
    /// IEnumerable<T> 集合的扩展方法
    /// 基于权重的随机选择元素
    /// </summary>
    public static class IEnumerableExtensions
    {

        /// <summary>
        /// 随机选择一个元素，权重由weightSelector委托定义
        /// </summary>
        /// <typeparam name="T"> 元素类型 </typeparam>
        /// <param name="source"> 要随机选择的源集合 </param>
        /// <param name="weightSelector"> 一个委托，用于定义每个元素的权重是多少 </param>
        /// <returns> 随机选择的元素 </returns>
        /// <exception cref="ArgumentException"> 源集合为空或没有元素 </exception>
        public static T Random<T>(this IEnumerable<T> source, Func<T, int> weightSelector = null)
        {
            // 若weightSelector为空，则默认为每个元素权重为1
            if (weightSelector == null)
            {
                weightSelector = item => 1;
            }
            // 若源集合为空或没有元素，则抛出异常
            if (source == null || !source.Any())
            {
                Console.WriteLine("源集合为空或没有元素");
                throw new ArgumentException("源集合为空或没有元素。");
            }

            // 实例化随机数生成器
            Random rand = new Random();

            // 创建一个列表存储原集合中每个元素及其权重
            // 使用Select LINQ 方法，将每个元素转换成一个匿名对象，包括元素本身和其权重
            var weightedList = source.Select(item => new
            {
                Value = item, // 元素值
                Weight = weightSelector(item) // 该元素的权重
            }).ToList();

            // 计算所有元素权重的总和
            int totalWeight = weightedList.Sum(i => i.Weight);

            // 生成一个0到总权重之间的随机数
            int randomValue = rand.Next(totalWeight);

            // 初始化当前累积权重为0
            int runningTotal = 0;

            // 遍历加权列表的每个元素
            foreach (var item in weightedList)
            {
                // 将当前元素的权重加入到累积权重中
                runningTotal += item.Weight;

                // 如果随机值小于当前的累积权重，则返回当前的元素
                if (randomValue < runningTotal)
                {
                    return item.Value;
                }
            }

            // 如果由于四舍五入等原因未能选择元素，则默认返回列表中的最后一个元素
            return weightedList.Last().Value;
        }



        /// <summary>
        /// 随机选择N个元素，权重由weightSelector委托定义。
        /// 如果提供distinctSelector，则确保按distinctSelector提取的字段值不重复。
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <typeparam name="TKey">用于确保唯一性的字段的类型</typeparam>
        /// <param name="source">要随机选择的源集合</param>
        /// <param name="weightSelector">一个委托，用于定义每个元素的权重是多少</param>
        /// <param name="count">要选择的元素数量</param>
        /// <param name="distinctSelector">（可选）如果指定，用于从每个元素提取一个值，以确保结果中此值的唯一性</param>
        /// <returns>随机选择的元素列表</returns>
        /// <exception cref="ArgumentException">源集合为空或没有元素，或请求的数量不合理</exception>
        public static List<T> Random<T, TKey>(this IEnumerable<T> source, Func<T, int> weightSelector, int count, Func<T, TKey> distinctSelector = null)
        {
            if (source == null || count <= 0 || !source.Any())
            {
                // 抛出异常，源数据为空或数量不合理
                throw new ArgumentException("源集合为空、没有元素或请求的数量不合理。");
            }

            // 当weightSelector为空时，默认给每个元素相同的权重
            weightSelector ??= item => 1;

            var rand = new Random(); // 用于生成随机数
            var results = new List<T>(); // 存储最终结果

            while (results.Count < count)
            {
                // 构建一个包含元素和其权重的列表
                var weightedList = source
                    .Select(item => new { Value = item, Weight = weightSelector(item) })
                    .ToList();

                // 如果distinctSelector不为空，使用它确保结果中的元素根据指定的值唯一
                var distinctItems = distinctSelector == null
                    ? weightedList
                    : weightedList
                        .GroupBy(item => distinctSelector(item.Value))
                        .Select(group => group.First())
                        .ToList();

                int totalWeight = distinctItems.Sum(i => i.Weight); // 计算总权重

                if (totalWeight == 0) break; // 如果总权重为0，则结束选择

                int randomValue = rand.Next(totalWeight); // 生成一个随机数
                int runningTotal = 0; // 用于累加权重并找到随机选中的元素

                foreach (var item in distinctItems)
                {
                    runningTotal += item.Weight; // 累加权重
                    if (randomValue < runningTotal)
                    {
                        results.Add(item.Value); // 添加选中的元素到结果列表
                                                 // 更新源集合以排除已选择的元素，以避免重复选择
                        if (distinctSelector != null)
                        {
                            source = source.Where(x => !results.Any(r => distinctSelector(r).Equals(distinctSelector(x))));
                        }
                        break;
                    }
                }

                // 如果由于distinctSelector的限制而无法达到所需数量，提前终止
                if (results.Count >= source.Count()) break;
            }

            return results; // 返回随机选择的元素列表
        }

    }

}
