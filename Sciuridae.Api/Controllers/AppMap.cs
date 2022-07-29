namespace Sciuridae.Api.Controllers;

public static class AppMap
{
    public static string? GetRepo(string appName)
    {
        if (string.Equals("SimplyBudget", appName, StringComparison.OrdinalIgnoreCase))
        {
            return "https://github.com/Keboo/SimplyBudget/";
        }
        return null;
    }
}
