using System.Reflection;
using System.Text.Json;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Common.Interface.Api;
using IReLaPluginLib;
using Lagrange.Core;

namespace ReLagrange;

class Program
{
    public static BotContext? Bot;
    static async Task Main(string[] args)
    {
        Console.WriteLine("[i]正在检查配置结构...");
        if (!Directory.Exists("./config"))
        {
            Directory.CreateDirectory("./config");
        }
        if (!Directory.Exists("./config/ReLa"))
        {
            Directory.CreateDirectory("./config/ReLa");
        }
        if (!File.Exists("./config/ReLa/device.json"))
        {
            Console.WriteLine("[!]检测到设备配置文件缺失,正在生成...");
            var bds = new BotDeviceStorage();
            File.WriteAllText("./config/ReLa/device.json",bds.ExportToJson());
        }
        Console.WriteLine("[i]正在读取配置...");
        BotDeviceInfo deviceInfo = BotDeviceStorage
            .LoadFromJson(File.ReadAllText("./config/ReLa/device.json"))
            .GetDeviceInfo();
        BotKeystore bks = new BotKeystore();
        if (File.Exists("./config/ReLa/keystore.json"))
        {
            bks = LoadKsFromJson(File.ReadAllText("./config/ReLa/keystore.json"));
        }
        Console.WriteLine("[i]正在加载插件...");
        LoadPlugins();
        
        Console.WriteLine("[i]正在创建机器人实例...");
        var bc = new BotConfig()
        {
            Protocol = Protocols.Linux,
            AutoReLogin = false,
            AutoReconnect = true,
            UseIPv6Network = false,
            GetOptimumServer = true
        };
        Bot = BotFactory.Create(bc, deviceInfo, bks);
        Console.WriteLine("[i]正在注册核心事件...");
        bool isOnline = false;
        Bot.Invoker.OnBotOnlineEvent += (context, @event) =>
        {
            isOnline = true;
            Console.WriteLine($"[i]Bot[{context.BotName}]登录成功");
        };
        Bot.Invoker.OnBotCaptchaEvent += (context, @event) =>
        {
            Console.WriteLine("[!]BOT CAPTCHA");
            //ReLaUtils.ConsoleQr(@event.Url);
            Console.WriteLine(@event.Url);
        };
        Bot.Invoker.OnBotOfflineEvent += (context, @event) =>
        {
            isOnline = false;
            Console.WriteLine("BOT OFFLINE");
        };
                
        //MSG Listener
        Bot.Invoker.OnFriendMessageReceived += (context, @event) =>
        {
            Console.WriteLine($"[i][MSG FRIEND][({@event.Chain.FriendUin}){@event.Chain.FriendInfo!.Nickname}]{@event.Chain.ToPreviewText()}");
        };
        Bot.Invoker.OnGroupMessageReceived += (context, @event) =>
        {
            Console.WriteLine($"[i][MSG GROUP][G{@event.Chain.GroupUin}|S({@event.Chain.FriendUin}){@event.Chain.FriendInfo!.Nickname}]{@event.Chain.ToPreviewText()}");
        };
        Console.WriteLine("[i]正在初始化插件...");
        InitPlugins();

        Console.WriteLine("[i]开始登录");
        if (File.Exists("./config/ReLa/keystore.json"))
        {
            Console.WriteLine("[i]正在尝试使用凭据登录");
            await Bot.LoginByPassword();
            if (!isOnline)
            {
                Console.WriteLine("[i]失败,请使用扫码登陆");
                var qrCode = await Bot.FetchQrCode();
                ReLaUtils.ConsoleQr(qrCode!.Value.Url);
                await Bot.LoginByQrCode();
                Console.Clear();
            }
            Console.WriteLine("[i]完成");
        }
        else
        {
            Console.WriteLine("[i]使用扫码登陆");
            var qrCode = await Bot.FetchQrCode();
            ReLaUtils.ConsoleQr(qrCode!.Value.Url);
            await Bot.LoginByQrCode();
            Console.Clear();
            Console.WriteLine("[i]完成");
        }
        Console.WriteLine("[i]正在更新KeyStore文件");
        File.WriteAllText("./config/ReLa/keystore.json", ExportKsToJson(Bot.UpdateKeystore()));
        Thread tt = new Thread(TerminalHandler);
        tt.Start();
    }

    public static BotKeystore LoadKsFromJson(string jsonText)
    {
        return JsonSerializer.Deserialize<BotKeystore>(jsonText)!;
    }

    public static string ExportKsToJson(BotKeystore ks)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(ks, options);
    }

    static void TerminalHandler()
    {
        while (true)
        {
            var i = Console.ReadLine();
            if (i == null) continue;
            CommandConductor(i);
        }
        // ReSharper disable once FunctionNeverReturns
    }

    static void CommandConductor(string c)
    {
        //TODO:Potential Move (ReLaUtils => DawnUtils)
        var cs = ReLaUtils.ResolveArgs(c);
        //END
        switch (cs[0])
        {
            case "":
                break;
            case "pm":
                switch (cs[1])
                {
                    case "l":
                        foreach (var plug in Plugins.Values)
                        {
                            Console.WriteLine($"{plug.Name}|{plug.Id}|{plug.Description}");
                        }
                        break;
                }
                break;
            case "exit":
                Bot!.Dispose();
                Environment.Exit(0);
                break;
            default:
                if (ExCommands.Keys.Contains(cs[0]))
                {
                    Plugins[ExCommands[cs[0]]].RunCommand(cs);
                }
                break;
        }
    }


    //Plugin System
#if DEBUG
    private static string[] additionalExtPath = 
        [
            "C:\\Users\\lithium\\RiderProjects\\ReLagrange\\ReLaEssential\\bin\\Debug\\net8.0\\ReLaEssential.dll"
        ];
#endif
    private static Dictionary<string,IReLaPlugin> Plugins = new Dictionary<string, IReLaPluginLib.IReLaPlugin>();
    private static Dictionary<string, string> ExCommands = new Dictionary<string, string>();
    static Assembly LoadPlug(string relativePath)
    {
        string pluginLocation = Path.GetFullPath(relativePath.Replace('\\', Path.DirectorySeparatorChar));
        PluginLoadContext loadContext = new PluginLoadContext(pluginLocation);
        return loadContext.LoadFromAssemblyName(AssemblyName.GetAssemblyName(pluginLocation));
    }
    static IReLaPlugin LoadIPlug(Assembly assembly)
    {
        foreach (Type type in assembly.GetTypes())
        {
            if (typeof(IReLaPlugin).IsAssignableFrom(type))
            {
                return (Activator.CreateInstance(type) as IReLaPluginLib.IReLaPlugin)!;
            }
        }
        return null!;
    }
    
    static void LoadPlugins()
    {
        //scan
        if (!Directory.Exists("./plugins"))
        {
            Directory.CreateDirectory("./plugins");
        }
        string[] exts = Directory.GetFiles("./plugins");
        #if DEBUG
        if (additionalExtPath.Length > 0)
        {
            foreach (var ext in additionalExtPath)
            {
                try
                {
                    if (ext.EndsWith(".dll"))
                    {
                        Console.WriteLine($"[P]正在载入[{ext}]");
                        IReLaPlugin ei = LoadIPlug(LoadPlug(ext));
                        Plugins.Add(ei.Id,ei);
                        if (ei.RegCommands != null)
                        {
                            foreach (var rc in ei.RegCommands)
                            {
                                ExCommands.Add(rc,ei.Id);
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }  
        #endif
        foreach (var ext in exts)
        {
            try
            {
                if (ext.EndsWith(".dll"))
                {
                    Console.WriteLine($"[R]正在载入[{ext}]");
                    IReLaPlugin ei = LoadIPlug(LoadPlug(ext));
                    Plugins.Add(ei.Id,ei);
                    if (ei.RegCommands != null)
                    {
                        foreach (var rc in ei.RegCommands)
                        {
                            ExCommands.Add(rc,ei.Id);
                        }
                    }
                    Console.WriteLine($"[P]完成");
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
    
    static void InitPlugins()
    {
        Console.WriteLine($"[P]{Plugins.Count}插件已载入");
        foreach (var extension in Plugins.Values)
        {
            extension.Init(Bot!);
        }
    }
}

class BotDeviceStorage
{
    public BotDeviceStorage()
    {
        Guid g = Guid.NewGuid();
        Id = g.ToString();
        Mac = ReLaUtils.GenRandomMac();
        Device = "ReLaBMS";
        System = "Windows 10.0.19042";
        KernelVer = "10.0.19042.0";
    }

    public static BotDeviceStorage LoadFromJson(string jsonText)
    {
        return JsonSerializer.Deserialize<BotDeviceStorage>(jsonText)!;
    }
    
    public string ExportToJson()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(this, options);
    }
    
    public BotDeviceInfo GetDeviceInfo()
    {
        return new BotDeviceInfo()
        {
            Guid = Guid.Parse(Id),
            MacAddress = Mac,
            DeviceName = Device,
            SystemKernel = System,
            KernelVersion = KernelVer
        };
    }
    
    public string Id { get; set; }
    public byte[] Mac { get; set; }
    public string Device { get; set; }
    public string System { get; set; }
    public string KernelVer { get; set; }
}