using Clockwise;
using Pocket;

namespace Recipes
{
    public static class ConfirmationLoggerExtensions
    {
        internal static void Complete(
            this ConfirmationLogger logger,
            Budget budget) =>
            logger.Succeed("Completed with {budget}", budget);
    }
}
