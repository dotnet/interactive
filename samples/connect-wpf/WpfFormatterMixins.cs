using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Formatting;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace WpfConnect
{
    public static class WpfFormatterMixins
    {
        public static CSharpKernel UseWpf(this CSharpKernel kernel)
        {
            UseSolidColorBrushFormatter();
            UseFrameworkElementFormatter();
            return kernel;
        }

        private static void UseSolidColorBrushFormatter()
        {
            Formatter.Register<SolidColorBrush>((brush, writer) =>
            {
                var color = brush.Color;
                string stringValue = $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";

                PocketView colorDiv = div(
                    div[style: $"border:2px solid #FFFFFF;background-color:{stringValue};width:15px;height:15px"](),
                    div(b(stringValue))
                );
                writer.Write(colorDiv);

            }, "text/html");
        }

        private static void UseFrameworkElementFormatter()
        {
            Formatter.Register(type: typeof(IFrameworkInputElement), formatter: (visual, writer) => {
                if (visual is FrameworkElement element)
                {
                    writer.Write(GetImage(element));
                }
            }, "text/html");
        }

        private static PocketView GetImage(FrameworkElement visual)
        {
            var rect = new Rect(visual.RenderSize);
            var drawingVisual = new DrawingVisual();

            using (var dc = drawingVisual.RenderOpen())
            {
                dc.DrawRectangle(new VisualBrush(visual), null, rect);
            }

            var bitmap = new RenderTargetBitmap(
                (int)rect.Width, (int)rect.Height, 96, 96, PixelFormats.Default);
            bitmap.Render(drawingVisual);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using var ms = new MemoryStream();
            encoder.Save(ms);
            ms.Flush();
            var data = ms.ToArray();
            var imageSource = $"data:image/png;base64, {Convert.ToBase64String(data)}";

            PocketView png = img[src: imageSource, width: rect.Width, height: rect.Height]();
            return png;
        }
    }
}
