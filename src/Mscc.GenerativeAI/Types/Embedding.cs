#if NET472_OR_GREATER || NETSTANDARD2_0
using System.Text.Json.Serialization;
#endif
using System.Collections.Generic;

namespace Mscc.GenerativeAI
{
    /// <summary>
    /// A list of floats representing an embedding.
    /// Ref: https://ai.google.dev/api/rest/v1beta/Embedding
    /// </summary>
    public class Embedding
    {
        /// <summary>
        /// The embedding values.
        /// </summary>
        public List<float> Values { get; set; }
    }
    
    /// <summary>
    /// A list of floats representing an embedding.
    /// Ref: https://ai.google.dev/api/rest/v1beta/ContentEmbedding
    /// </summary>
    public class ContentEmbedding
    {
        /// <summary>
        /// The embedding values.
        /// </summary>
        public List<float> Values { get; set; }
    }
}
