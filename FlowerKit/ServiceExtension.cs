using Microsoft.Extensions.DependencyInjection;

namespace FlowerKit;

/// <summary>
/// Wrapper to hide Microsoft.Extensions.DependencyInjection.
/// </summary>
public static class ServiceExtension
{
    /// <summary>
    /// Adds a scoped service of the type specified in <typeparamref name="T"/> with an
    /// implementation type specified in <typeparamref name="I"/> to the
    /// specified <see cref="IServiceCollection"/>.
    /// </summary>
    public static IServiceCollection AddScoped<T, I>(this IServiceCollection services)
        where T : class
        where I : class, T
        => ServiceCollectionServiceExtensions.AddScoped<T, I>(services);
    
    /// <summary>
    /// Adds a singleton service of the type specified in <typeparamref name="T"/> with an
    /// implementation type specified in <typeparamref name="I"/> to the
    /// specified <see cref="IServiceCollection"/>.
    /// </summary>
    public static IServiceCollection AddSingelton<T, I>(this IServiceCollection services)
        where T : class
        where I : class, T
        => ServiceCollectionServiceExtensions.AddSingleton<T, I>(services);
    
    /// <summary>
    /// Adds a transient service of the type specified in <typeparamref name="T"/> with an
    /// implementation type specified in <typeparamref name="I"/> to the
    /// specified <see cref="IServiceCollection"/>.
    /// </summary>
    public static IServiceCollection AddTransient<T, I>(this IServiceCollection services)
        where T : class
        where I : class, T
        => ServiceCollectionServiceExtensions.AddTransient<T, I>(services);
}