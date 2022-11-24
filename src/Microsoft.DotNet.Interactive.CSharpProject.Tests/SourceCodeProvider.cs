// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public static class SourceCodeProvider
{
    public static string ConsoleProgramCollidingRegions =>
        @"using System;

namespace ConsoleProgramSingleRegion
{
    public class Program
    {
        public static void Main(string[] args)
        {
            #region alpha
            var a = 10;
            #endregion

            #region alpha
            var b = 10;
            #endregion
        }
    }
}".EnforceLF();

    public static string ConsoleProgramMultipleRegions =>
        @"using System;

namespace ConsoleProgramSingleRegion
{
    public class Program
    {
        public static void Main(string[] args)
        {
            #region alpha
            var a = 10;
            var c = a * 10;
            #endregion

            #region beta
            var b = 10;
            var d = c - Math.Min(b,a);
            #endregion
        }
    }
}".EnforceLF();

    public static string ConsoleProgramSingleRegion =>
        @"using System;

namespace ConsoleProgramSingleRegion
{
    public class Program
    {
        public static void Main(string[] args)
        {
            #region alpha
            var a = 10;
            #endregion
        }
    }
}".EnforceLF();

    public static string ConsoleProgramNoRegion => @"using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleProgramSingleRegion
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var a = 10;
        }
    }
}".EnforceLF();

    public static string CodeWithNoRegions => @"
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace jsonDotNetExperiment
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""jsonDotNet workspace"");
            var simpleObject = new JObject
            {
                {""property"", 4}
            };
            Console.WriteLine(simpleObject.ToString(Formatting.Indented));
            Console.WriteLine(""Bye!"");
        }
    }
}";
      
    public static string CodeWithTwoRegions => @"
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace jsonDotNetExperiment
{
    class Program
    {
        static void Main(string[] args)
        {
            #region workspaceIdentifier
            Console.WriteLine(""jsonDotNet workspace"");
            #endregion
            #region objetConstruction
            var simpleObject = new JObject
            {
                {""property"", 4}
            };
            #endregion
            Console.WriteLine(simpleObject.ToString(Formatting.Indented));
            Console.WriteLine(""Bye!"");
        }
    }
}";
      
    public static string GistWithRegion => @"using System;
using NodaTime;

namespace TryNodaTime
{
    class Program
    {  
        static void Main(string[] args)
        {
            #region fragment
            // Instant represents time from epoch
            Instant now = SystemClock.Instance.GetCurrentInstant();
            Console.WriteLine($""now: {now}"");

            // Convert an instant to a ZonedDateTime
            ZonedDateTime nowInIsoUtc = now.InUtc();
            Console.WriteLine($""nowInIsoUtc: {nowInIsoUtc}"");

            // Create a duration
            Duration duration = Duration.FromMinutes(3);
            Console.WriteLine($""duration: {duration}"");

            // Add it to our ZonedDateTime
            ZonedDateTime thenInIsoUtc = nowInIsoUtc + duration;
            Console.WriteLine($""thenInIsoUtc: {thenInIsoUtc}"");

            // Time zone support (multiple providers)
            var london = DateTimeZoneProviders.Tzdb[""Europe/London""];
            Console.WriteLine($""london: {london}"");

            // Time zone conversions
            var localDate = new LocalDateTime(2012, 3, 27, 0, 45, 00);
            var before = london.AtStrictly(localDate);
            Console.WriteLine($""before: {before}"");
            #endregion
        }
    }
}";

    public static string ConsoleProgramSingleRegionExtraUsing =>
        @"using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleProgramSingleRegion
{
    public class Program
    {
        public static void Main(string[] args)
        {
            #region alpha
            var a = 10;
            #endregion
        }
    }
}".EnforceLF();
}