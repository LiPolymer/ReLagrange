using Lagrange.Core;

namespace IReLaPluginLib;

public interface IReLaPlugin
{
    string Id { get; }
    string Name { get;  }
    string Description { get; }
    string[]? RegCommands { get; }
    void Init(BotContext bot);
    void Shutdown();
    void RunCommand(string[] cmd);
}