using System.Collections.Generic;

namespace Jolt.LanguageServer.Preview
{
    /// <summary>
    /// Parameters of the Jolt-specific "jolt/transform" request, which runs a transformer against a preview input file.
    /// </summary>
    internal sealed class TransformParams
    {
        public TransformerDocument Transformer { get; set; } = new TransformerDocument();

        public PreviewInputDocument Input { get; set; } = new PreviewInputDocument();
    }

    internal sealed class TransformerDocument
    {
        /// <summary>
        /// Gets or sets the transformer's URI, which identifies the document context (custom methods and transformers)
        /// that the client has sent for it.
        /// </summary>
        public string Uri { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;
    }

    internal sealed class PreviewInputDocument
    {
        public string Uri { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value chosen by the client that changes whenever the input file's text does.
        /// </summary>
        public string Revision { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the input file's text. Since input files may be large, the client leaves this out when it has
        /// already sent the text for this revision, and sends it again if the result says that it's required.
        /// </summary>
        public string? Text { get; set; }
    }

    internal static class TransformOutcome
    {
        public const string Success = "success";

        /// <summary>
        /// The transformer has errors, so it wasn't run.
        /// </summary>
        public const string Skipped = "skipped";

        /// <summary>
        /// The input file's text wasn't sent and the server doesn't have this revision of it.
        /// </summary>
        public const string InputRequired = "inputRequired";

        public const string InvalidInput = "invalidInput";

        public const string Error = "error";

        public const string TimedOut = "timedOut";
    }

    internal sealed class TransformResult
    {
        public string Outcome { get; set; } = TransformOutcome.Success;

        public string? Output { get; set; }

        public TransformError? Error { get; set; }

        /// <summary>
        /// Gets or sets the number of errors in the transformer, when it was skipped because of them.
        /// </summary>
        public int? TransformerErrorCount { get; set; }

        public List<string> Warnings { get; set; } = new List<string>();

        public double? ElapsedMilliseconds { get; set; }
    }

    internal sealed class TransformError
    {
        public string? Code { get; set; }

        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the inner exceptions, outermost first, since the outer one often hides the actual cause.
        /// </summary>
        public List<TransformError> InnerErrors { get; set; } = new List<TransformError>();
    }
}
