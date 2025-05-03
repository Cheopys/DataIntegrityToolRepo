namespace DataIntegrityTool.Schema
{
    public class ToolParameters
    {
        public Int16  Id                { get; set; }
        public byte[] publicKey         { get; set; } 
        public byte[] privateKey        { get; set; }
        public Int32 MinimumInterval    { get; set; }
        public byte[] AesKey            { get; set; }
        public DateTime usageSince      { get; set; } = DateTime.MinValue;
    }
}
