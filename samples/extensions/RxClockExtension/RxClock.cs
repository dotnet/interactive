// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// SVG clock adapted from: https://medium.com/the-andela-way/create-a-pure-css-clock-with-svg-f123bcc41e46

using System;
using Microsoft.AspNetCore.Html;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace RxClockExtension
{
    public static class RxClock
    {
        public static IHtmlContent DrawSvgClock(this DateTimeOffset datetime) =>
            datetime.DateTime.DrawSvgClock();

        public static IHtmlContent DrawSvgClock(this DateTime datetime)
        {
            var hours = datetime.Hour;
            var minutes = datetime.Minute;
            var seconds = datetime.Second;

            return div(
                svg[viewBox: "0 0 40 40"](
                    circle[cx: "20", cy: "20", r: "19"],
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
                style[type: "text/css"](Css(hours, minutes, seconds)),
                script(@"
let svg = document.querySelector('svg');
"
                ));

            string Css(int hours, int minutes, int seconds) =>
                $@"
html {{
  background: #dedede !important;
}}
svg {{
  width: 400px;
  fill: white;
  stroke: black;
  stroke-width: 1;
  stroke-linecap: round;
  --start-seconds: {hours};
  --start-minutes: {minutes};
  --start-hours: {seconds};
}}

.marks {{
  transform: translate(20px, 20px);
  stroke-width: 0.2;
}}
.marks > line:nth-child(1) {{
  transform: rotate(30deg); 
}}
.marks > line:nth-child(2) {{
  transform: rotate(calc(2 * 30deg));
}}
.marks > line:nth-child(3) {{
  transform: rotate(calc(3 * 30deg));
  stroke-width: 0.5;
}}
.marks > line:nth-child(4) {{
  transform: rotate(calc(4 * 30deg));
}}
.marks > line:nth-child(5) {{
  transform: rotate(calc(5 * 30deg));
}}
.marks > line:nth-child(6) {{
  transform: rotate(calc(6 * 30deg));
  stroke-width: 0.5;
}}
.marks > line:nth-child(7) {{
  transform: rotate(calc(7 * 30deg));
}}
.marks > line:nth-child(8) {{
  transform: rotate(calc(8 * 30deg));
}}
.marks > line:nth-child(9) {{
  transform: rotate(calc(9 * 30deg));
  stroke-width: 0.5;
}}
.marks > line:nth-child(10) {{
  transform: rotate(calc(10 * 30deg));
}}
.marks > line:nth-child(11) {{
  transform: rotate(calc(11 * 30deg));
}}
.marks > line:nth-child(12) {{
  transform: rotate(calc(12 * 30deg));
  stroke-width: 0.5;
}}
.seconds,
.minute,
.hour
{{
  transform: translate(20px, 20px) rotate(0deg);
}}
.seconds {{
  stroke-width: 0.3;
  stroke: #d00505;
  transform: translate(20px, 20px) rotate(calc(var(--start-seconds) * 6deg));

}}
.minute {{
  stroke-width: 0.6;
  transform: translate(20px, 20px) rotate(calc(var(--start-minutes) * 6deg));
}}
.hour {{
  stroke-width: 1;
   transform: translate(20px, 20px) rotate(calc(var(--start-hours) * 30deg));
}}
.pin {{
  stroke: #d00505;
  stroke-width: 0.2;
}}
.text {{
  font-size: 1.5px;
  font-family: sans-serif;
  transform: translate(15px, 30px) rotate(0deg);
  fill: #5f2fd9;
  stroke: none;
}}";
        }
    }
}