using Newtonsoft.Json;
using GameServer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using GameServer.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;



namespace GameServer.Managers
{
    [Service(ServiceLifetime.Singleton)]
    public class GameConfigManager
    {
        private readonly string _configFilePath = @"Configs\GameConfig.json";
        private static JObject _configData;

        public GameConfigManager( )
        {

            Console.WriteLine("开始加载GameConfig");

#if DEBUG
            _configFilePath = Path.Combine(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\")), @"Configs\GameConfig.json"); ;
#endif
            LoadConfig();
            Console.WriteLine("GameConfig加载完成");
        }

        private void LoadConfig()
        {


            try
            {
                if (File.Exists(_configFilePath))
                {
                  
                    var json = File.ReadAllText(_configFilePath);
                    _configData = JObject.Parse(json);


                    if (_configData == null)
                    {
                        throw new InvalidOperationException("GameConfig加载失败。");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log出异常情况，根据您的日志系统，这里可能需要调整
                Console.WriteLine("无法加载GameConfig: " + ex.Message);
            }
        }

        public static T GetConfigValue<T>(params string[] pathKeys)
        {
            JToken currentToken = _configData;

            foreach (var key in pathKeys)
            {
                if (currentToken[key] == null)
                    throw new KeyNotFoundException($"找不到指定的Key: {key}");

                currentToken = currentToken[key];
            }

            try
            {
                return currentToken.ToObject<T>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"无法将配置值转换为类型 {typeof(T).Name}: {ex.Message}", ex);
            }
        }


    }
}