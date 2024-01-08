

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Pocket;
using SkiaSharp;


namespace Microsoft.DotNet.Interactive.AIUtilities;

using Logger = Pocket.Logger<SKImage>;

public static class Image {
    public static string ToBase64(this SKImage image)
    {
        Logger.Log.Event();
        var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return Convert.ToBase64String(encoded.ToArray());
    }

    public static string ToDataUrl(this SKImage image)
    {
        Logger.Log.Event();
        var base64Image = image.ToBase64();
        return $"data:image/jpeg; base64,{base64Image}";
    }
}