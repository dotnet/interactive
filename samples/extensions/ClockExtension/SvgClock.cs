// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// SVG clock adapted from: https://medium.com/the-andela-way/create-a-pure-css-clock-with-svg-f123bcc41e46

using System;

using Microsoft.AspNetCore.Html;

using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace ClockExtension
{
    public static class SvgClock
    {
        public static IHtmlContent DrawSvgClock(this DateTimeOffset datetime) =>
            datetime.DateTime.DrawSvgClock();

        public static IHtmlContent DrawSvgClock(this DateTime datetime)
        {
            var hours = datetime.Hour % 12;
            var minutes = datetime.Minute;
            var seconds = datetime.Second;

            return DrawSvgClock(hours, minutes, seconds);
        }

        public static IHtmlContent DrawSvgClock(int hours, int minutes, int seconds)
        {
            var id = "clockExtension" + Guid.NewGuid().ToString("N");
            return div[id: id](
                svg[viewBox: "0 0 40 40"](
                    _.defs(
                        _.radialGradient[id: "grad1", cx: "50%", cy: "50%", r: "50%", fx: "50%", fy: "50%"](
                            _.stop[offset: "0%", style: "stop-color:#512bd4;stop-opacity:0"],
                            _.stop[offset: "100%", style: "stop-color:#512bd4;stop-opacity:.5"])),
                    circle[cx: "20", cy: "20", r: "19", fill: "#dedede"],
                    circle[cx: "20", cy: "20", r: "19", fill: "url(#grad1)"],
                    g[@class: "marks"](
                        line[x1: 15, y1: 0, x2: 16, y2: 0],
                        line[x1: 15, y1: 0, x2: 16, y2: 0],
                        line[x1: 15, y1: 0, x2: 16, y2: 0],
                        line[x1: 15, y1: 0, x2: 16, y2: 0],
                        line[x1: 15, y1: 0, x2: 16, y2: 0],
                        line[x1: 15, y1: 0, x2: 16, y2: 0],
                        line[x1: 15, y1: 0, x2: 16, y2: 0],
                        line[x1: 15, y1: 0, x2: 16, y2: 0],
                        line[x1: 15, y1: 0, x2: 16, y2: 0],
                        line[x1: 15, y1: 0, x2: 16, y2: 0],
                        line[x1: 15, y1: 0, x2: 16, y2: 0],
                        line[x1: 15, y1: 0, x2: 16, y2: 0]),
                    text[x: 0, y: 0, @class: "text"](".NET Interactive"),
                    line[x1: 0, y1: 0, x2: 9, y2: 0, @class: "hour"],
                    line[x1: 0, y1: 0, x2: 13, y2: 0, @class: "minute"],
                    line[x1: 0, y1: 0, x2: 16, y2: 0, @class: "seconds"],
                    circle[cx: 20, cy: 20, r: 0.7, @class: "pin"]
                ),
                style[type: "text/css"](Css()),
                script(@"
let svg = document.querySelector('svg');
"
                ));

            IHtmlContent Css() =>
                new HtmlString($@"
#{id} svg {{
  width: 400px;
  fill: white;
  stroke: black;
  stroke-width: 1;
  stroke-linecap: round;
  transform: rotate(-90deg);
  --start-seconds: {seconds};
  --start-minutes: {minutes};
  --start-hours: {hours};
}}

#{id} .marks {{
  transform: translate(20px, 20px);
  stroke-width: 0.2;
}}
#{id} .marks > line:nth-child(1) {{
  transform: rotate(30deg); 
}}
#{id} .marks > line:nth-child(2) {{
  transform: rotate(calc(2 * 30deg));
}}
#{id} .marks > line:nth-child(3) {{
  transform: rotate(calc(3 * 30deg));
  stroke-width: 0.5;
}}
#{id} .marks > line:nth-child(4) {{
  transform: rotate(calc(4 * 30deg));
}}
#{id} .marks > line:nth-child(5) {{
  transform: rotate(calc(5 * 30deg));
}}
#{id} .marks > line:nth-child(6) {{
  transform: rotate(calc(6 * 30deg));
  stroke-width: 0.5;
}}
#{id} .marks > line:nth-child(7) {{
  transform: rotate(calc(7 * 30deg));
}}
#{id} .marks > line:nth-child(8) {{
  transform: rotate(calc(8 * 30deg));
}}
#{id} .marks > line:nth-child(9) {{
  transform: rotate(calc(9 * 30deg));
  stroke-width: 0.5;
}}
#{id} .marks > line:nth-child(10) {{
  transform: rotate(calc(10 * 30deg));
}}
#{id} .marks > line:nth-child(11) {{
  transform: rotate(calc(11 * 30deg));
}}
#{id} .marks > line:nth-child(12) {{
  transform: rotate(calc(12 * 30deg));
  stroke-width: 0.5;
}}
#{id} .seconds,
#{id} .minute,
#{id} .hour
{{
  transform: translate(20px, 20px) rotate(0deg);
}}
#{id} .seconds {{
  stroke-width: 0.3;
  stroke: #d00505;
  transform: translate(20px, 20px) rotate(calc(var(--start-seconds) * 6deg));

}}
#{id} .minute {{
  stroke-width: 0.6;
  transform: translate(20px, 20px) rotate(calc(var(--start-minutes) * 6deg));
}}
#{id} .hour {{
  stroke: #512bd4;
  stroke-width: 1;
  transform: translate(20px, 20px) rotate(calc(var(--start-hours) * 30deg));
}}
#{id} .pin {{
  stroke: #d00505;
  stroke-width: 0.2;
}}
#{id} .text {{
  font-size: 2px;
  font-family: ""Segoe UI"",Helvetica,Arial,sans-serif;
  transform: rotate(90deg) translate(13.5px, -12px);
  fill: #512bd4;
  stroke: none;
}}");
        }
    }
}