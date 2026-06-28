public static class DepartmentResolver
{
    private static readonly string[] KnownDepartments = { "hr", "it", "finance", "engineering" };

    public static string Resolve(string fileName)
    {
        var prefix = fileName.Split('-').FirstOrDefault()?.ToLowerInvariant();

        if (prefix != null && KnownDepartments.Contains(prefix))
        {
            return prefix;
        }

        Console.WriteLine($"WARNING: Could not resolve department for '{fileName}'. Defaulting to 'general'.");
        return "general";
    }
}
