namespace DataIntegrityTool.Schema
{
    public class AuthorizedToolsCustomer
    {
        public Int32 Id          { get; set; }
        public Int32 CustomerId  { get; set; }
        public ToolTypes tooltype { get; set; }
    }

    public class AuthorizedToolsUser
    {
        public Int32 Id         { get; set; }
        public Int32 UserId     { get; set; }
        public ToolTypes tooltype { get; set; }
    }
}