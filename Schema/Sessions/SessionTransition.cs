using DataIntegrityTool.Shared;
using System.Diagnostics.Contracts;
using Amazon.S3.Model.Internal.MarshallTransformations;

namespace DataIntegrityTool.Schema
{
    public class SessionTransition
    {   
        public Int32        Id          { get; set; }  
        public Int32       SessionId    { get; set; }
		public Int32       UserId       { get; set; }
		public Int32       CustomerId   { get; set; }
		public LicenseTypes Licensetype { get; set; }
		public ToolTypes   ToolType     { get; set; }
		public DateTime    DateTime      { get; set; }
        public Int32       FrameOrdinal  { get; set; }
        public Int32       LayerOrdinal  { get; set; }
        public ErrorCodes  ErrorCode     { get; set; } = ErrorCodes.errorNone;
    }
}
