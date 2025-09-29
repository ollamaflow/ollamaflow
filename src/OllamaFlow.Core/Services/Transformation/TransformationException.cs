namespace OllamaFlow.Core.Services.Transformation
{
    using System;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Models;

    /// <summary>
    /// Exception thrown when transformation between API formats fails.
    /// </summary>
    public class TransformationException : Exception
    {
        /// <summary>
        /// The source API format being transformed from.
        /// </summary>
        public ApiFormatEnum SourceFormat { get; }

        /// <summary>
        /// The target API format being transformed to.
        /// </summary>
        public ApiFormatEnum TargetFormat { get; }

        /// <summary>
        /// The stage of transformation where the error occurred.
        /// </summary>
        public string TransformationStage { get; }

        /// <summary>
        /// The original object that caused the transformation failure.
        /// </summary>
        public object OriginalObject { get; }

        /// <summary>
        /// Initializes a new instance of the TransformationException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="sourceFormat">The source API format.</param>
        /// <param name="targetFormat">The target API format.</param>
        /// <param name="stage">The transformation stage.</param>
        /// <param name="originalObject">The original object causing the error.</param>
        /// <param name="innerException">The inner exception.</param>
        public TransformationException(
            string message,
            ApiFormatEnum sourceFormat,
            ApiFormatEnum targetFormat,
            string stage = null,
            object originalObject = null,
            Exception innerException = null)
            : base(message, innerException)
        {
            SourceFormat = sourceFormat;
            TargetFormat = targetFormat;
            TransformationStage = stage ?? "Unknown";
            OriginalObject = originalObject;
        }

        /// <summary>
        /// Generate an appropriate error response for the client based on their expected API format.
        /// </summary>
        /// <returns>API-specific error response object.</returns>
        public object GenerateErrorResponse()
        {
            // This will be implemented with specific error formats for each API
            switch (TargetFormat)
            {
                case ApiFormatEnum.OpenAI:
                    return new
                    {
                        error = new
                        {
                            message = Message,
                            type = "transformation_error",
                            code = "request_transformation_failed"
                        }
                    };

                case ApiFormatEnum.Ollama:
                default:
                    return new
                    {
                        error = Message
                    };
            }
        }
    }
}