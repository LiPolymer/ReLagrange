using System.Text.Json;
using System.Text.RegularExpressions;
using IReLaPluginLib;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;

namespace ReLaEssential;

public class Plugin : IReLaPlugin
{
    public string Id => "ink.lipoly.relagrange.ess";
    public string Name => "ReLaESS";
    public string Description => "SimplePlugin :)";
    public string[]? RegCommands => ["ess"];

    private BotContext? Bot;
    private Config? Cfg;
    
    public void Init(BotContext bot)
    {
        Bot = bot;
        if (!Directory.Exists("./config/Essential"))
        {
            Directory.CreateDirectory("./config/Essential");
        }
        LoadConfig();
        InitPlugin();
        Console.WriteLine("[i]ReLaEssential Initialized!");
    }

    private void LoadConfig()
    {
        if (!File.Exists("./config/Essential/config.json"))
        {
            List<uint?> eg = new List<uint?>();
            List<uint?> aid = new List<uint?>();
            eg.Add(114514);
            aid.Add(1919810);
            Dictionary<string, string> apl = new Dictionary<string, string>();
            apl["/help"] = "helper!";
            File.WriteAllText("./config/Essential/config.json",JsonSerializer
                .Serialize(new Config()
                {
                    EnableGroups = eg,
                    AdminId = aid,
                    AutoReply = apl
                },new JsonSerializerOptions()
                {
                    WriteIndented = true
                }));
        }
        Cfg = JsonSerializer.Deserialize<Config>(File.ReadAllText("./config/Essential/config.json"))!;
    }
    
    private void InitPlugin()
    {
        //注册事件
        Bot.Invoker.OnGroupMessageReceived += (context, @event) =>
        {
            if (Cfg.EnableGroups!.Contains(@event.Chain.GroupUin))
            {
                foreach(var entity in @event.Chain)
                {
                    if (entity is TextEntity te)
                    {
                        if (Cfg.AutoReply!.Keys.Contains(te.Text))
                        {
                            var c = MessageBuilder.Group(Convert.ToUInt32(@event.Chain.GroupUin));
                            c.Text(Cfg.AutoReply[te.Text]);
                            Console.WriteLine($"[{@event.Chain.GroupUin}<Bot]{Cfg.AutoReply[te.Text]}");
                            Bot.SendMessage(c.Build());
                        }
                    }
                }
            }
        };
        Bot.Invoker.OnFriendMessageReceived += (context, @event) =>
        {
            foreach(var entity in @event.Chain)
            {
                if (entity is TextEntity te)
                {
                    if (Cfg.AutoReply!.Keys.Contains(te.Text))
                    {
                        var c = MessageBuilder.Friend(Convert.ToUInt32(@event.Chain.FriendUin));
                        Console.WriteLine($"[({@event.Chain.FriendUin}){@event.Chain.FriendInfo!.Nickname}<Bot]{Cfg.AutoReply[te.Text]}");
                        c.Text(Cfg.AutoReply[te.Text]);
                        Bot.SendMessage(c.Build());
                    }
                }
            }
        };
    }
    
    public void Shutdown()
    {
        
    }
    
    public void RunCommand(string[] cmd)
    {
        if (cmd[0] == "ess" & cmd[1] == "reload")
        {
            Console.WriteLine("[i]正在重载配置文件");
            LoadConfig();
            Console.WriteLine("[i]重载完成");
        }
    }
}

internal class Config()
{
    public List<uint?>? EnableGroups { get; set; }
    public List<uint?>? AdminId { get; set; }
    public Dictionary<string,string>? AutoReply { get; set; }
}