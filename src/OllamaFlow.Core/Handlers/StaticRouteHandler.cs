namespace OllamaFlow.Core.Handlers
{
    using OllamaFlow.Core;
    using OllamaFlow.Core.Serialization;
    using SyslogLogging;
    using System;
    using System.IO;
    using System.Reflection.PortableExecutable;
    using System.Threading;
    using System.Threading.Tasks;
    using WatsonWebserver.Core;

    /// <summary>
    /// Handler for static routes like root, favicon, and other simple HTTP responses.
    /// </summary>
    public class StaticRouteHandler : IDisposable
    {
        private string _Header = "[StaticRouteHandler] ";
        private OllamaFlowSettings _Settings = null;
        private LoggingModule _Logging = null;
        private Serializer _Serializer = null;
        private CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private bool _Disposed = false;

        /// <summary>
        /// Handler for static routes like root, favicon, and other simple HTTP responses.
        /// </summary>
        public StaticRouteHandler(OllamaFlowSettings settings, LoggingModule logging, Serializer serializer, CancellationTokenSource tokenSource)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _TokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));
        }

        /// <summary>
        /// Initialize.
        /// </summary>
        public void Initialize()
        {
            _Logging.Debug(_Header + "initialized");
        }

        /// <summary>
        /// Handle GET request to root path.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task GetRootRoute(HttpContextBase ctx, CancellationToken token = default)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = Constants.HtmlContentType;
            await ctx.Response.Send(Constants.HtmlHomepage, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Handle HEAD request to root path.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task HeadRootRoute(HttpContextBase ctx, CancellationToken token = default)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = Constants.TextContentType;
            await ctx.Response.Send(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Handle GET request to favicon.ico.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task GetFaviconRoute(HttpContextBase ctx, CancellationToken token = default)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = Constants.FaviconContentType;
            await ctx.Response.Send(File.ReadAllBytes(Constants.FaviconFilename), token).ConfigureAwait(false);
        }

        /// <summary>
        /// Handle HEAD request to favicon.ico.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task HeadFaviconRoute(HttpContextBase ctx, CancellationToken token = default)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = Constants.FaviconContentType;
            await ctx.Response.Send(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _Disposed = true;
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}