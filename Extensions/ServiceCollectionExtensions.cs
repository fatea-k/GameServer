using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using GameServer.Attributes;
using GameServer.Utilities;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAutoRegisteredServices(this IServiceCollection services, Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var typesWithServiceAttribute = assembly.GetTypes()
                .Where(type => type.GetCustomAttribute<ServiceAttribute>() != null);

            foreach (var type in typesWithServiceAttribute)
            {


                // 现在检查 HandlerMapping 并注册映射
                var handlerMappings = type.GetCustomAttributes<HandlerMappingAttribute>();
                foreach (var mapping in handlerMappings)
                {
                    HandlerTypeProvider.RegisterActionHandler(mapping.Action, type);
                }

                //检查 Service 并注册
                var attribute = type.GetCustomAttribute<ServiceAttribute>();
                switch (attribute.Lifetime)
                {
                    case ServiceLifetime.Singleton:
                        services.AddSingleton(type);
                        break;
                    case ServiceLifetime.Scoped:
                        services.AddScoped(type);
                        break;
                    case ServiceLifetime.Transient:
                        services.AddTransient(type);
                        break;
                    default:
                        throw new ArgumentException($"未知的服务器生命周期: {attribute.Lifetime}");
                }
            }
        }

        return services;
    }
}