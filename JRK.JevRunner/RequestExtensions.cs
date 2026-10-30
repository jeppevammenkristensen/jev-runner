namespace JRK.JevRunner;

public static class RequestExtensions
{
    extension(Request request)
    {
        /// <summary>
        /// Chainable convenience method for enabling long context for the request
        /// </summary>
        /// <returns></returns>
        public Request EnableLongContext()
        {
            request.LongContextEnabled = true;
            return request;
        }
        
        /// <summary>
        /// Return trues if the <see cref="Request"/> has a Box Question
        /// </summary>
        /// <remarks>Box questions are only supported from jev-1.14.0 and forward</remarks>
        public bool HasBoxQuestion => request.Questions.Any(question => question is Requests.BoxQuestion);

        /// <summary>
        /// Returns true if the request has 1 or more Assets or if it has at least one BoxQuestion
        /// </summary>
        public bool HasImages => request.Assets.Count > 0 || request.HasBoxQuestion;
    }
}