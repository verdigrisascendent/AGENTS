using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Mega Agent - Orchestrates all specialized agents for Lights in the Dark
/// </summary>
public partial class MegaAgent : Node
{
    private Dictionary<string, ISpecializedAgent> agents = new();
    private List<AgentLog> agentLogs = new();
    private Dictionary<string, object> contextIndex = new();
    
    public override void _Ready()
    {
        // Register all specialized agents
        RegisterAgent("GameStateGuardian", new GameStateGuardian());
        RegisterAgent("AmigaAestheticEnforcer", new AmigaAestheticEnforcer());
        RegisterAgent("HardwareBridgeEngineer", new HardwareBridgeEngineer());
        
        GD.Print("[MegaAgent] All agents registered and ready");
    }
    
    public void RegisterAgent(string name, ISpecializedAgent agent)
    {
        agents[name] = agent;
        agent.Initialize(this);
    }
    
    public T RouteToAgent<T>(string agentName, string task, Dictionary<string, object> parameters)
    {
        if (agents.TryGetValue(agentName, out var agent))
        {
            var result = agent.Execute(task, parameters);
            LogAgentAction(agentName, task, result);
            return (T)result;
        }
        
        GD.PrintErr($"[MegaAgent] Agent {agentName} not found");
        return default(T);
    }
    
    private void LogAgentAction(string agent, string task, object result)
    {
        agentLogs.Add(new AgentLog
        {
            Timestamp = Time.GetUnixTimeFromSystem(),
            Agent = agent,
            Task = task,
            Result = result
        });
    }
}

public interface ISpecializedAgent
{
    void Initialize(MegaAgent mega);
    object Execute(string task, Dictionary<string, object> parameters);
}

public class AgentLog
{
    public double Timestamp { get; set; }
    public string Agent { get; set; }
    public string Task { get; set; }
    public object Result { get; set; }
}
