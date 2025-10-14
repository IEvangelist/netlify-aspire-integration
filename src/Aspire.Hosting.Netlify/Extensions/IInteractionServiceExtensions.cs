namespace Aspire.Hosting;

internal static class IInteractionServiceExtensions
{
    public static bool IsAvailable(
        this IInteractionService interactionService,
        IConfiguration configuration)
    {
        if (interactionService.IsAvailable)
        {
            // We cannot interact when running in CI/CD.
            return CiDetector.IsRunningInCi(configuration) is false;
        }

        return false;
    }
}