namespace OllamaFlow.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint group.
    /// </summary>
    public class EndpointGroup
    {
        #region Public-Members

        /// <summary>
        /// Key is the upper-case HTTP method.
        /// Value is a list of parameterized URLs to match, e.g. /{version}/foo/bar/{id}.
        /// </summary>
        public Dictionary<string, List<string>> ParameterizedUrls
        {
            get
            {
                return _ParameterizedUrls;
            }
            set
            {
                if (value == null) value = new Dictionary<string, List<string>>();
                _ParameterizedUrls = value;
            }
        }

        #endregion

        #region Private-Members

        private Dictionary<string, List<string>> _ParameterizedUrls = new Dictionary<string, List<string>>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Endpoint group.
        /// </summary>
        public EndpointGroup()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
