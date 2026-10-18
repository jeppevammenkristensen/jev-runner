using System.Text.Json.Nodes;

namespace JRK.JevRunner.Responses;

/// <summary>
/// Contains the normalized edges of a target's bounding box and the model's confidence.
/// </summary>
/// <param name="Top">The top edge as a fraction of image height, measured from the top.</param>
/// <param name="Bottom">The bottom edge as a fraction of image height, measured from the top.</param>
/// <param name="Left">The left edge as a fraction of image width, measured from the left.</param>
/// <param name="Right">The right edge as a fraction of image width, measured from the left.</param>
/// <param name="Confidence">The confidence reported for the bounding box.</param>
/// <remarks>A box does not establish that the target is present; use a companion Noul question to check presence.</remarks>
public record BoxAnswer(double Top, double Bottom, double Left, double Right, Confidence Confidence)
{
    /// <summary>
    /// Parses the edges from the nested <c>box</c> object and confidence from the answer object.
    /// </summary>
    /// <param name="obj">The box answer containing <c>box</c> and <c>confidence</c>.</param>
    /// <returns>The bounding box and confidence reported by the API.</returns>
    public static BoxAnswer From(JsonObject obj)
    {
        var box = obj["box"]!.AsObject();
        var top = box["top"]!.GetValue<double>();
        var bottom = box["bottom"]!.GetValue<double>();
        var left = box["left"]!.GetValue<double>();
        var right = box["right"]!.GetValue<double>();
        
        Confidence confidence = obj["confidence"]!.GetValue<double>();
        return  new BoxAnswer(top, bottom, left, right, confidence);
    }
}
