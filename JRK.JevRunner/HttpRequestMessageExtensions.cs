namespace JRK.JevRunner;

/// <summary>Applies and detects request-scoped experimental feature headers.</summary>
internal static class HttpRequestMessageExtensions
{
    /// <param name="message">The HTTP request whose feature headers are inspected or modified.</param>
    extension(HttpRequestMessage message)
    {
        /// <summary>
        /// Adds the image feature opt-in header to the request.
        /// </summary>
        /// <returns>The same request for further configuration.</returns>
        internal HttpRequestMessage AddImageDangerZone()
        {
            message.Headers.Add(DangerZones.Images, "true");
            return message;
        }

        /// <summary>
        /// Adds the long-context feature opt-in header to the request.
        /// </summary>
        /// <returns>The same request for further configuration.</returns>
        internal HttpRequestMessage AddLongContext()
        {
            message.Headers.Add(DangerZones.LongContext, "true");
            return message;
        }

        /// <summary>Checks whether any request header uses the experimental feature prefix.</summary>
        /// <returns>True when at least one danger-zone header is present.</returns>
        internal bool HasDangerRequestMessage()
        {
            return message.Headers.Any(x => DangerZones.DangerZoneHeaderRegex().IsMatch(x.Key ?? string.Empty));
        }
    }
    
}
