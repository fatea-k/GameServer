using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using GameServer.Attributes;
using GameServer.Utilities;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAutoRegisteredServices(this IServiceCollection services, Assembly[] assemblies)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("开始自动注册服务。");
        Console.ResetColor();
        foreach (var assembly in assemblies)
        {
            RegisterServicesFromAssembly(services, assembly);
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("所有服务自动注册完成。");
        Console.ResetColor();
        return services;
    }

    private static void RegisterServicesFromAssembly(IServiceCollection services, Assembly assembly)
    {
        var typesWithServiceAttribute = assembly.GetTypes()
            .Where(type => type.GetCustomAttribute<ServiceAttribute>() != null);

        foreach (var type in typesWithServiceAttribute)
        {
            RegisterService(services, type);
            RegisterHandlers(type);
        }
    }

    private static void RegisterService(IServiceCollection services, Type type)
    {
        var attribute = type.GetCustomAttribute<ServiceAttribute>();
        if (attribute != null)
        {
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
                    throw new ArgumentException($"未知的服务生命周期: {attribute.Lifetime}");
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"    注册服务: {type.Name} -> {attribute.Lifetime}");
            Console.ResetColor();
        }
    }

    private static void RegisterHandlers(Type type)
    {
        var handlerMappings = type.GetCustomAttributes<HandlerMappingAttribute>();
        foreach (var mapping in handlerMappings)
        {
            try
            {
                HandlerTypeProvider.RegisterActionHandler(mapping.Action, type);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"    注册命令处理器映射: {mapping.Action} -> {type.Name}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"    注册命令处理器映射失败: {mapping.Action} -> {type.Name}, 错误: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}