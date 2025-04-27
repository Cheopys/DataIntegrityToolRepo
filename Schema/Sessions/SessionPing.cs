using System.Diagnostics.Contracts;
using Amazon.S3.Model.Internal.MarshallTransformations;

namespace DataIntegrityTool.Schema
{
    public class SessionPing
    {
        public  Int32 SessionId { get; set; }
        public DateTime DateTime { get; set; }
    }
}
