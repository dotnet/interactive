// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SkiaSharp;

namespace Microsoft.DotNet.Interactive.OpenAI;

internal static class SkiaUtils
{
    public static async Task<SKSurface> ShowImage(string url, int width, int height)
    {
        SKImageInfo info = new SKImageInfo(width, height);
        SKSurface surface = SKSurface.Create(info);
        SKCanvas canvas = surface.Canvas;
        canvas.Clear(SKColors.White);
        var httpClient = new HttpClient();
        using (Stream stream = await httpClient.GetStreamAsync(url))
        using (MemoryStream memStream = new MemoryStream())
        {
            await stream.CopyToAsync(memStream);
            memStream.Seek(0, SeekOrigin.Begin);
            SKBitmap webBitmap = SKBitmap.Decode(memStream);
            canvas.DrawBitmap(webBitmap, 0, 0, null);
            surface.Draw(canvas, 0, 0, null);
        };
        return surface;
    }
}