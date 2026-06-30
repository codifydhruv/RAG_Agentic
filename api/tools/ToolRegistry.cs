public class ToolRegistry
{
    private readonly Dictionary<string, ToolDefinition> _tools = new();

    public void Register(ToolDefinition definition)
    {
        _tools[definition.Name] = definition;
    }

    public ToolDefinition? Get(string name) =>
        _tools.TryGetValue(name, out var def) ? def : null;

    public IEnumerable<ToolDefinition> GetAll() => _tools.Values;
}
