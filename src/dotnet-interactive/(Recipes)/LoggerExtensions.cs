using System;
using System.Runtime.CompilerServices;
using Pocket;

namespace Recipes
{
    internal static class LoggerExtensions
    {
        internal static ConfirmationLogger OnEnterAndConfirmOnExit(
            this Logger logger,
            object[] properties, 
            [CallerMemberName] string name = null) =>
            new ConfirmationLogger(
                name,
                logger.Category,
                null,
                null,
                true,
                properties);
    }
}
