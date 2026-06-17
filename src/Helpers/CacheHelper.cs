using System.Runtime.CompilerServices;
using System.Text.Json;

public static class CacheHelper
{
    /// <summary>
    /// Generates a cache key based on the method name and its parameters. This is useful for caching results of methods with varying parameters.
    /// </summary>
    /// <param name="parameters">The parameters of the method for which the cache key is being generated.</param>
    /// <param name="methodName">The name of the method for which the cache key is being generated. This is automatically provided by the compiler.</param>
    /// <returns>A string representing the cache key.</returns>
    public static string GetCacheKey(object[] parameters, [CallerMemberName] string methodName = "")
    {
        return $"{methodName}_{JsonSerializer.Serialize(parameters)}";
    }
}