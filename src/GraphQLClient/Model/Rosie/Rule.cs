namespace GraphQLClient.Model.Rosie
{
    /// <summary>
    /// Represents the structure of a Codiga Rule
    /// </summary>
    public class Rule
    {
        public long? Id { get; set; }
        public string? Name { set; get; }
        public string? Content { get; set; }
        public string? RuleType { get; set; }
        public string? Language { get; set; }
        public string? Pattern { get; set; }
        public string? ElementChecked { get; set; }
    }
}
