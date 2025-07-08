namespace DataIntegrityTool.Schema
{
    public class ToolParameters
    {
        public Int16  Id                { get; set; }
        public byte[] publicKey         { get; set; } 
        public byte[] privateKey        { get; set; }
        public DateTime usageSince      { get; set; } = DateTime.MinValue;
    }
}
