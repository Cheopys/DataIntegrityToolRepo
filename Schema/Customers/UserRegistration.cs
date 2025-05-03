namespace DataIntegrityTool.Schema
{
    public class UserRegistration
    {
        public Int32 CustomerId         { get; set; }
        public string          Token    { get; set; }
        public List<ToolTypes> Tools    { get; set; }
    }
}
