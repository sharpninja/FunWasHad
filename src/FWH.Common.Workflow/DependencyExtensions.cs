using FWH.Common.Workflow.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace FWH.Common.Workflow;

public static class DependencyExtensions
{
    public static IServiceCollection AddWorkflowService(this IServiceCollection services)
    {
        services.AddSingleton<WorkflowService>();
        services.AddSingleton<WorkflowController>();
        return services;
    }
}
