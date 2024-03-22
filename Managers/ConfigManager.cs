using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Reflection;
using GameServer.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using GameServer.Models;
using GameServer.Utilities;


namespace GameServer.Managers
{
    [Service(ServiceLifetime.Singleton)]
    public class ConfigManager
    {
        private string ConfigDirectory = @"Configs\DataConfig";
        private FileSystemWatcher _fileWatcher;
        private Dictionary<string, object> _configInstances = new Dictionary<string, object>();
        private ConcurrentDictionary<string, Timer> _pendingOperations = new ConcurrentDictionary<string, Timer>();
        private const int DebounceTime = 500; // 去抖时间设定为500ms

        private static readonly ConfigManager _instance = new ConfigManager();
        public static ConfigManager Instance => _instance;

        public ConfigManager()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("开始加载配置文件");
            Console.ResetColor();
         

#if DEBUG
            ConfigDirectory = Path.Combine(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\")), @"Configs\DataConfig"); ;
#endif
            //启动文件监视器
            SetupFileWatcher();
            //加载所有配置文件
            LoadAllConfigFiles();


            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("配置文件加载结束");
            Console.ResetColor();

        }

        /// <summary>
        ///  设置文件监视器
        /// </summary>
        private void SetupFileWatcher()
        {
            _fileWatcher = new FileSystemWatcher
            {
                Path = ConfigDirectory,
                Filter = "*.json",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
            };

            _fileWatcher.Changed += OnConfigFileChanged;
            _fileWatcher.Created += OnConfigFileChanged;
            _fileWatcher.Deleted += OnConfigFileChanged;
            _fileWatcher.Renamed += OnConfigFileChanged;

            _fileWatcher.EnableRaisingEvents = true;
        }

        /// <summary>
        ///  加载所有配置文件
        /// </summary>
        private void LoadAllConfigFiles()
        {
            DirectoryInfo dirInfo = new DirectoryInfo(ConfigDirectory);
            foreach (FileInfo file in dirInfo.GetFiles("*.json"))
            {
                ProcessConfigFile(file);
            }
        }


        /// <summary>
        ///  处理配置文件
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="fileWatcher"></param>
        private void ProcessConfigFile(FileInfo fileInfo,string fileWatcher = null)
        {
            string modelName = Path.GetFileNameWithoutExtension(fileInfo.Name);
            Type configModelType = Assembly.GetExecutingAssembly().GetTypes()
                .FirstOrDefault(t => t.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));
            //输出内容临时字符串
            string output = string.Format("    已加载: {0}", modelName);
           
            switch (fileWatcher)
            {
                case "Created":
                    output=$"发现新配置文件{fileInfo.Name}";
                    break;
                case "Renamed":
                    output = $"发现重命名配置文件 {fileInfo.Name} ";
                    break;
                case "Changed":
                    output = $"配置文件 {fileInfo.Name} 被修改";
                    break;
                default:
                    output = $"配置文件 {fileInfo.Name} ";
                    break;
            }

            if (configModelType != null)
            {

                try
                {
                    string jsonData = File.ReadAllText(fileInfo.FullName);
                    var listType = typeof(IEnumerable<>).MakeGenericType([configModelType]);
                    object configInstance = JsonConvert.DeserializeObject(jsonData, listType);
                    _configInstances[modelName] = configInstance;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{output}已加载,已实例化配置数据{modelName}");
                    Console.ResetColor();

                

                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"配置文件 {fileInfo.Name} 加载失败,失败原因: {ex.Message}");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"已跳过,{output},找不到对应的模型");
                Console.ResetColor();
            }
           
        }

        /// <summary>
        ///  文件监视器事件处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            // 使用文件路径作为键来标记定时器
            _pendingOperations.AddOrUpdate(e.FullPath,
                // 如果键不存在，则添加新定时器
                (path) => new Timer((o) => ProcessFileEvent(o as FileSystemEventArgs), e, DebounceTime, Timeout.Infinite),
                // 如果键存在，则重置定时器
                (path, existingTimer) =>
                {
                    existingTimer.Change(DebounceTime, Timeout.Infinite);
                    return existingTimer;
                });
        }
       /// <summary>
       ///  处理文件事件
       /// </summary>
       /// <param name="e"></param>
        private void ProcessFileEvent(FileSystemEventArgs e)
        {
            // 移除定时器
            if (_pendingOperations.TryRemove(e.FullPath, out Timer timer))
            {
                timer.Dispose(); // 释放定时器资源
                FileInfo fileInfo = new FileInfo(e.FullPath);
                if (e.ChangeType != WatcherChangeTypes.Deleted && fileInfo.Exists)
                {
                    ProcessConfigFile(fileInfo, e.ChangeType.ToString()); // 处理文件更改
                }
                else if (e.ChangeType == WatcherChangeTypes.Deleted)
                {
                   // ProcessConfigFile(null, e.ChangeType.ToString()); // 处理文件删除

                    // 删除对应的配置实例
                    string modelName = Path.GetFileNameWithoutExtension(e.Name);
                    // _configInstances.Remove(modelName); // 注释掉，因为删除配置文件后，对应的配置实例暂时不能删除,防止找到实例而报错
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"配置文件{e.Name}被删除");
                    Console.ResetColor();

                }
            }
        }
        /// <summary>
        ///  获取配置实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> GetConfig<T>() where T : class
        {
            string modelName = typeof(T).Name;
            //尝试从_configInstances中获取对应的配置列表
            if (_configInstances.TryGetValue(modelName, out object configInstance))
            {
                //类型转换
                return configInstance as IEnumerable<T>;
            }
            return null;
        }
    }
}
