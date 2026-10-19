# JRK.JevRunner.Image

Optional SkiaSharp helpers for images used with Jev. The core client does not depend
on this project.

Targets .NET 11 and uses preview C# union support for `AppliedBox`.

## Validate an image before uploading

```csharp
using JRK.JevRunner.Image;
using TruePath;

var path = AbsolutePath.Create(@"C:\images\photo.jpg");
var validator = new ImageValidator();
var validation = validator.Validate(path);
var suggestedScaling = validation.SuggestedScaling;
foreach (var error in validation.Errors)
    Console.WriteLine(error.GetErrorMessage());
validation.ThrowIfInvalid(); // Throws InvalidOperationException containing the errors if invalid.
// validation.Format (png or jpg), Width, Height, and FileSizeBytes describe the file.
```

`ImageValidator` checks actual contents, not the filename: PNG or JPEG only,
280–1568 pixels on **each** axis (inclusive), at most 16 MiB of encoded file data,
no animated PNG (including single-frame APNG), and successful pixel decoding.
Non-default EXIF orientation is rejected: rotate the pixels upright and reset the
orientation before uploading. Visually sideways pixels without orientation metadata
cannot be detected. Progressive JPEG is not rejected; baseline JPEG is recommended.

Validation does not modify, rotate, or resize the file. Supply an `IFileSystem` to
the constructor for injectable file access. Invalid contents return errors; missing
or inaccessible files throw normal file-access exceptions. Oversized files are
rejected before decoding and have no detected format or dimensions in the result.

This is a **per-file** check, not a guarantee of API acceptance. It cannot check
the complete request's 32 MiB body, processed-image and expanded-reference limits,
asset references, base64 representation, or model context length. `AddImageAsset`
does not run validation automatically and still uses the filename extension as
the upload format: use a matching lowercase `.png`, `.jpg`, or `.jpeg` extension.
See the [current API requirements](https://docs.typesafe.ai/images#limits).

`validation.Errors` contains an `ImageValidationError` union: either a typed
`ImageDimensionError` with `Dimension`, `Violation`, `Limit`, and `Actual`, or a string
for other failures. `ImageDimensionError.GetErrors(info, minimum, maximum)` can also
check dimensions independently, returning width violations before height violations.

`validation.SuggestedScaling` belongs to that result, not the validator. It reports
`Upscale` when dimension errors are only `ToSmall`, `Downscale` when they are only
`ToHigh`, and `ScalingNotPossible` when both occur. `None` means **no dimension
errors**, not necessarily a valid image. Non-dimension errors do not affect the
suggestion, and aspect-ratio feasibility is not checked. Always inspect `IsValid`
and revalidate resized output: out-of-range images have not undergone full pixel
decoding, and scaling can leave the other side outside the allowed range.

## Upscale or downscale an image

```csharp
var scaler = new ImageScaler();
byte[] enlargedPng = scaler.Upscale(path);     // Shorter side becomes 280 if smaller.
byte[] reducedPng = scaler.Downscale(path);    // Longer side becomes 1568 if larger.
// Optional custom bounds:
byte[] customPng = scaler.Upscale(path, minimumDimension: 400);
```

Both axes use the same proportional scale factor. Upscaling rounds fractional
dimensions up; downscaling rounds down, keeping at least one pixel. Images already
meeting the chosen bound keep their dimensions. Results are always PNG bytes,
including when no resizing is needed, and the source file is never changed.

The operations are independent: they do not crop, pad, normalize EXIF orientation,
or validate the result. An extreme aspect ratio may still violate API limits, and
**that does not throw**: 200 × 1600 upscales to 280 × 2240; 2000 × 200 downscales
to 1568 × 156. Revalidate the saved output if API readiness is required; save it
with a `.png` extension. Decode, file-access, resource-allocation, and invalid-target
errors can still fail the operation. Supply an `IFileSystem` to the constructor
for injectable file access.

## Add an asset from resized bytes

```csharp
using JRK.JevRunner.Infrastructure;
using JRK.JevRunner.Requests;

byte[] png = new ImageScaler().Downscale(path);
var asset = ImageAsset.FromByteArray(Guid.NewGuid().ToString(), "png", png);
var state = new AssetState().AddAsset("photo", asset);
```

`FromByteArray` encodes bytes as standard base64; it does not detect the format,
convert pixels, or validate them. The supplied format must match the actual bytes.
Populate `AssetState` before constructing the request or executing a generated query.

## Validate and add an image asset

```csharp
using JRK.JevRunner.Image;
using JRK.JevRunner.Requests;
using TruePath;

var state = new AssetState();
await state.AddImageWithValidation(
    "photo", Guid.NewGuid().ToString(),
    AbsolutePath.Create(@"C:\images\photo.jpg"));
```

`AddImageWithValidation` validates before registering the asset and throws
`InvalidOperationException` containing the validation errors if the image is invalid.
It does not resize or modify the image: valid files retain their original bytes and
detected PNG or JPEG format, regardless of the filename extension. `stateName` names
the state field; `assetName` identifies the image in the request's assets map.

The method returns the same state instance, preserving its derived type. Optional
`token` and `fileSystem` arguments support cancellation and injectable file access;
validation and byte loading use the same file system. File-access failures propagate
without registering an asset. Populate the state before constructing a request or
executing a generated query.

Use this helper when invalid images should be rejected rather than resized. For a
best-effort resize, use the automatic adjustment helper below; neither helper checks
request-wide API limits.

## Automatic image adjustment (experimental)

```csharp
var state = new AssetState();
await state.AddImageAssetWithAutoAdjustment(
    "photo", Guid.NewGuid().ToString(),
    AbsolutePath.Create(@"C:\images\photo.png"));
```

The helper validates the file, attempts one `Upscale` or `Downscale` when suggested,
and registers the resulting bytes. Unchanged images retain their detected format
and original bytes, while resized images are encoded as PNG. Validation, byte loading,
and scaling all use the supplied `IFileSystem` (or the local file system by default).
If validation fails with `None` or
`ScalingNotPossible`, it calls `ThrowIfInvalid()` instead. Source files are unchanged.

**Current limitations:** adjusted output is not revalidated, and suggestions ignore
non-dimension errors. A resize can leave the other axis outside the limits, so this
helper is not a guarantee of API acceptance. For strict validation, revalidate the
prepared bytes before registering them explicitly.

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

## Draw multiple Box answers

`DrawBox` takes an `AppliedBox` union, accepting either a single `BoxAnswer` or
an `IEnumerable<BoxAnswer>`. All boxes use the same color and stroke width:

```csharp
using JRK.JevRunner.Responses;
using SkiaSharp;

var boxes = new List<BoxAnswer>
{
    response.GetRequiredBoxAnswer("Head"),
    response.GetRequiredBoxAnswer("Glasses")
};
byte[] png = renderer.DrawBox(
    AbsolutePath.Create(@"C:\images\photo.jpg"), boxes,
    color: SKColors.Green, strokeWidth: 4);
```

Generated query response properties also return `BoxAnswer`, so they can be passed
directly or collected into the list. Supply a repeatable collection such as an array
or list: rendering enumerates the boxes once for validation and again for drawing.
An empty collection returns the source image re-encoded as PNG without outlines.

## Coordinates and file access

Use the same image pixel layout you submitted to Jev: this method does not resize
the source or apply EXIF orientation. Coordinates must be finite fractions between
zero and one, with `Top < Bottom` and `Left < Right`. Confidence does not gate
rendering; check target presence and confidence in your application first.

Pass an `IFileSystem` to the renderer constructor for injectable file access.
Native SkiaSharp assets for Windows and macOS arrive with SkiaSharp; this project
also references `SkiaSharp.NativeAssets.Linux.NoDependencies` for supported Linux
targets.
