using System;
using System.Runtime.Serialization;

namespace PartWizard
{
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "GUI")]
    public class GUIControlsException : Exception
    {
        public GUIControlsException()
            : base()
        {
        }

        public GUIControlsException(string message)
            : base(message)
        {
        }

        public GUIControlsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected GUIControlsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
