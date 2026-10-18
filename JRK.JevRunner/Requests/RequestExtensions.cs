using Microsoft.Extensions.Logging;

namespace JRK.JevRunner.Requests;

/// <summary>
/// Provides helpers for configuring and executing requests and checking for assets.
/// </summary>
public static class RequestExtensions
{
    extension(Request request)
    {
        /// <summary>
        /// Gets whether the request contains any assets.
        /// </summary>
        public bool HasAssets => request.Assets.Count > 0;
        
        /// <summary>
        /// Appends a question to the request and returns the same request for further configuration.
        /// </summary>
        public Request AddQuestion(Question question)
        {
            request.Questions.Add(question);
            return request;
        }

        /// <summary>
        /// Builds and appends a question to the request and returns the same request for further configuration.
        /// </summary>
        public Request AddQuestion<TQuestion>(IQuestionBuilder<TQuestion> questionBuilder) where TQuestion : IQuestion
        {
            return request.AddQuestion(questionBuilder.BuildQuestion());
        }

        /// <summary>
        /// Executes the request using the supplied runner.
        /// </summary>
        /// <returns>The response returned by the runner.</returns>
        public async Task<Response> Execute(JevRunner runner)
        {
            return await runner.Execute(request);
        }
        
        /// <summary>
        /// Creates a runner with the supplied API key and optional logger, then executes the request.
        /// </summary>
        /// <returns>The response returned by the runner.</returns>
        public async Task<Response> Execute(string apiKey, ILogger<JevRunner>? logger = null)
        {
            return await JevRunner.Init(apiKey, logger).Execute(request);
        }
    }
}
