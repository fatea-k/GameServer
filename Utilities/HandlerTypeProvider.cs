namespace GameServer.Utilities
{
    public static class HandlerTypeProvider
    {
        // 保存 action 到 Handler 类型的映射
        private static readonly Dictionary<string, Type> ActionToHandlerMap = new Dictionary<string, Type>();

        // 假设你已经注册了所有 Handler
        public static void RegisterActionHandler(string action, Type handlerType)
        {
            ActionToHandlerMap[action] = handlerType;
        }

        public static Type GetHandlerTypeForAction(string action)
        {
            ActionToHandlerMap.TryGetValue(action, out var handlerType);
            return handlerType;
        }
    }
}
