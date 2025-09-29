namespace OllamaFlow.Core.Services
{
    using System.IO;
    using System.Threading.Tasks;
    using OllamaFlow.Core;
    using WatsonWebserver.Core;

    /// <summary>
    /// Handler for static routes like root, favicon, and other simple HTTP responses.
    /// </summary>
    public class StaticRouteHandler
    {
        #region Public-Methods

        /// <summary>
        /// Handle GET request to root path.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task GetRootRoute(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = Constants.HtmlContentType;
            await ctx.Response.Send(Constants.HtmlHomepage);
        }

        /// <summary>
        /// Handle HEAD request to root path.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task HeadRootRoute(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = Constants.TextContentType;
            await ctx.Response.Send();
        }

        /// <summary>
        /// Handle GET request to favicon.ico.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task GetFaviconRoute(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = Constants.FaviconContentType;
            await ctx.Response.Send(File.ReadAllBytes(Constants.FaviconFilename));
        }

        /// <summary>
        /// Handle HEAD request to favicon.ico.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <returns>Task.</returns>
        public async Task HeadFaviconRoute(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = Constants.FaviconContentType;
            await ctx.Response.Send();
        }

        #endregion
    }
}