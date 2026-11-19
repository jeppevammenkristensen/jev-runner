using Microsoft.Extensions.Logging;

namespace JRK.JevRunner.Requests;

/// <summary>
/// Provides helpers for configuring and executing requests and checking for assets.
/// </summary>
public static class RequestExtensions
{
    /// <summary>Provides configuration and execution helpers for a request.</summary>
    /// <param name="request">The request to inspect, configure, or execute.</param>
    extension(Request request)
    {
        /// <summary>
        /// Gets whether the request contains any assets.
        /// </summary>
        public bool HasAssets => request.Assets.Count > 0;
        
        /// <summary>
        /// Appends a question to the request and returns the same request for further configuration.
        /// </summary>
        /// <param name="question">The question to append to the request.</param>
        public Request AddQuestion(Question question)
        {
            request.Questions.Add(question);
            return request;
        }

        /// <summary>
        /// Builds and appends a question to the request and returns the same request for further configuration.
        /// </summary>
        /// <typeparam name="TQuestion">The question type produced by the builder.</typeparam>
        /// <param name="questionBuilder">The builder whose current configuration supplies the question.</param>
        public Request AddQuestion<TQuestion>(IQuestionBuilder<TQuestion> questionBuilder) where TQuestion : IQuestion
        {
            return request.AddQuestion(questionBuilder.BuildQuestion());
        }

        /// <summary>
        /// Executes the request using the supplied runner.
        /// </summary>
        /// <returns>The response returned by the runner.</returns>
        /// <param name="runner">The runner used to send the request.</param>
        public async Task<Response> Execute(JevRunner runner)
        {
            return await runner.Execute(request);
        }
        
        /// <summary>
        /// Creates a runner with the supplied API key, configures logging if a factory is supplied, then executes the request.
        /// </summary>
        /// <returns>The response returned by the runner.</returns>
        /// <param name="apiKey">The API key used to authenticate the request.</param>
        /// <param name="loggerFactory">The factory used to create the logger, or null for no-op logging.</param>
        public async Task<Response> Execute(string apiKey, ILoggerFactory? loggerFactory = null)
        {
            var runner = JevRunner.Init(apiKey);
            if (loggerFactory is not null)
            {
                runner.WithLoggerFactory(loggerFactory);
            }

            return await runner.Execute(request);
        }
    }
}
