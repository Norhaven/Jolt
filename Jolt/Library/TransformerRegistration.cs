using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Library
{
    /// <summary>
    /// Represents the registration information for a transformer that may be referenced and evaluated within Jolt.
    /// </summary>
    public sealed class TransformerRegistration
    {
        /// <summary>
        /// The name of the transformer. This is how it will be referenced within another transformer.
        /// </summary>
        public string TransformerName { get; }
        
        /// <summary>
        /// The transformer itself.
        /// </summary>
        public string Transformer { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformerRegistration"/> class with the specified transformer name and transformer.
        /// </summary>
        /// <param name="transformerName">The name of the transformer.</param>
        /// <param name="transformer">The transformer itself.</param>
        public TransformerRegistration(string transformerName, string transformer)
        {
            TransformerName = transformerName;
            Transformer = transformer;
        }
    }
}
