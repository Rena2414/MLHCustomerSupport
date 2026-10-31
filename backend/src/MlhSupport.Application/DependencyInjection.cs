using Microsoft.Extensions.DependencyInjection;
using MlhSupport.Application.Analytics;
using MlhSupport.Application.Auth;
using MlhSupport.Application.Classes;
using MlhSupport.Application.Concerns;
using MlhSupport.Application.Conversations;
using MlhSupport.Application.Families;
using MlhSupport.Application.Students;
using MlhSupport.Application.Subscriptions;
using MlhSupport.Application.Users;

namespace MlhSupport.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<ClassService>();
        services.AddScoped<FamilyService>();
        services.AddScoped<SubscriptionService>();
        services.AddScoped<ConcernService>();
        services.AddScoped<StudentContextService>();
        services.AddScoped<TeacherAnalyticsService>();
        services.AddScoped<ConversationService>();
        return services;
    }
}
