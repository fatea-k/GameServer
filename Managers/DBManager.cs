using GameServer.Attributes;
using GameServer.Utilities;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace GameServer.Managers
{
    // GenericRepository 泛型类，封装了通用的CRUD操作方法
    //自动服务注册
    [Service(ServiceLifetime.Scoped)]
    public class DBManager<T> where T : class
    {
        // 定义一个只读的 IMongoCollection<T> 类型的私有字段，存储 MongoDB 集合引用
        private readonly IMongoCollection<T> _collection;

        // 构造函数，参数 context 提供了数据库操作的上下文环境，collectionName 指定操作的集合名称
        public DBManager(DataContext context)
        {
            // 从 DataContext 获取特定名称的集合
            _collection = context.Database.GetCollection<T>(typeof(T).Name.ToLowerInvariant());
        }

        // 异步方法 GetAllAsync，返回集合中所有文档的列表
        public  async Task<IEnumerable<T>> GetAllAsync()
        {
            // 使用 MongoDB 驱动的 Find 方法查找所有文档，并转换成列表
            return await _collection.Find(_ => true).ToListAsync();
        }

        // 异步方法 GetAsync，接受一个表达式作为过滤条件，返回符合条件的单个文档
        public async Task<T> GetAsync(Expression<Func<T, bool>> predicate)
        {
            // 使用 Find 方法并传入表达式筛选符合条件的文档，然后返回第一个或默认值
            return await _collection.Find(predicate).FirstOrDefaultAsync();
        }

        // 异步方法 CreateAsync，用于在数据库中创建新文档
        public async Task CreateAsync(T entity)
        {
            // 使用 InsertOneAsync 方法向集合中插入一个新文档
            await _collection.InsertOneAsync(entity);
        }

        // 异步方法 UpdateAsync，使用表达式作为过滤条件更新数据库中的文档
        public async Task<bool> UpdateAsync(Expression<Func<T, bool>> predicate, T entity)
        {
            // 使用 ReplaceOneAsync 方法根据条件替换文档
            var updateResult = await _collection.ReplaceOneAsync(predicate, entity);
            // 根据操作结果返回布尔值，表示是否成功
            return updateResult.IsAcknowledged && updateResult.ModifiedCount > 0;
        }

        // 异步方法 DeleteAsync，使用表达式作为过滤条件删除数据库中的文档
        public async Task<bool> DeleteAsync(Expression<Func<T, bool>> predicate)
        {
            // 使用 DeleteOneAsync 方法根据条件删除文档
            var deleteResult = await _collection.DeleteOneAsync(predicate);
            // 根据操作结果返回布尔值，表示是否成功
            return deleteResult.IsAcknowledged && deleteResult.DeletedCount > 0;
        }

        // 这里可以根据需要添加更多泛型CRUD操作
    }
}