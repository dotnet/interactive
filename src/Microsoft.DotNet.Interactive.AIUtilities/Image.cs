// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Pocket;
using SkiaSharp;

namespace Microsoft.DotNet.Interactive.AIUtilities;

using Logger = Pocket.Logger<SKImage>;

public static class Image {
    /// <summary>
    /// Generates a base64 encoded string for the image.
    /// </summary>
    /// <param name="image"></param>
    /// <returns>The base64 encoding of the image as png</returns>
    public static string ToBase64(this SKImage image)
    {
        Logger.Log.Event();
        var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return Convert.ToBase64String(encoded.ToArray());
    }

    /// <summary>
    /// Generates a data URI for the image.
    /// This can be used in the src attribute of an img tag to display the image in a browser.
    /// </summary>
    /// <param name="image"></param>
    /// <returns>A string in the format data:image/jpeg;base64,{base64EncodedImage}</returns>
    public static string ToImageSrc(this SKImage image)
    {
        Logger.Log.Event();
        var base64Image = image.ToBase64();
        return $"data:image/png;base64,{base64Image}";
    }
}