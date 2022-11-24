using System.Collections.Generic;

namespace GraphQLClient.Model.Rosie
{
    /// <summary>
    /// Represents the structure of a Codiga Ruleset
    /// </summary>
    public class RuleSetsForClient
    {
        public long? Id { get; set; }
        public string? Name { get; set; }
        public IReadOnlyList<Rule>? Rules { get; set; }
    }
}
