using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Utility;
using Xunit;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public class LocalizationTests
    {

        [Fact]
        public void Culture_Is_Set_Base_On_Environment_Variable()
        {
            Environment.SetEnvironmentVariable("DOTNET_CLI_CULTURE", "es-ES");

            Microsoft.DotNet.Interactive.App.Program.SetCultureFromEnvironmentVariables();

            var culture = new CultureInfo("es-ES");
            CultureInfo.CurrentCulture.Name.Should().Be(culture.Name);
        }

        [Fact]
        public void UICulture_Is_Set_Base_On_Environment_Variable()
        {
            Environment.SetEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", "es-ES");

            Microsoft.DotNet.Interactive.App.Program.SetCultureFromEnvironmentVariables();

            var culture = new CultureInfo("es-ES");
            CultureInfo.CurrentUICulture.Name.Should().Be(culture.Name);
        }

        [Fact]
        public async Task Kernel_Execute_Code_Using_Culture_From_Parent_Thread_That_Created_It()
        {
            using var kernel = new CSharpKernel();
            CultureInfo.CurrentCulture = new CultureInfo("es-ES");
            CultureInfo.CurrentUICulture = new CultureInfo("es-ES");

            var result = await kernel.SendAsync(new SubmitCode("System.Globalization.CultureInfo.CurrentCulture.Name"));
            result.Events.Should().ContainSingle<ReturnValueProduced>().Which.Value.Should().Be("es-ES");
        }
    }
}
