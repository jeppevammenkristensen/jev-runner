# JRK.JevRunner.Image

Optional SkiaSharp helpers for images used with Jev. The core client does not depend
on this project.

## Draw a Box answer

```csharp
using JRK.JevRunner.Image;
using TruePath;

var box = response.GetRequiredBoxAnswer("Find_Head");
var renderer = new BoxRenderer();
byte[] png = renderer.DrawBox(
    AbsolutePath.Create(@"C:\images\photo.jpg"), box);
```

`DrawBox` returns PNG bytes without saving or overwriting any file. It preserves
the image dimensions and draws a red, three-pixel outline by default. Supply
`color: SKColors.Blue` and/or `strokeWidth: 5` to customize it (with `using SkiaSharp;`).
The stroke is centered on the box edges and clipped at image boundaries.

Use the same image pixel layout you submitted to Jev: this method does not resize
the source or apply EXIF orientation. Coordinates must be finite fractions between
zero and one, with `Top < Bottom` and `Left < Right`. Confidence does not gate
rendering; check target presence and confidence in your application first.

Pass an `IFileSystem` to the renderer constructor for injectable file access.
Native SkiaSharp assets for Windows and macOS arrive with SkiaSharp; this project
also references `SkiaSharp.NativeAssets.Linux.NoDependencies` for supported Linux
targets.
