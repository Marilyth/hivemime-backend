using System.Text.Json.Serialization;

public class Bbox
{
    [JsonPropertyName("xmin")]
    public double Xmin { get; set; }

    [JsonPropertyName("xmax")]
    public double Xmax { get; set; }

    [JsonPropertyName("ymin")]
    public double Ymin { get; set; }

    [JsonPropertyName("ymax")]
    public double Ymax { get; set; }
}

public class CapitalOfDivision
{
    [JsonPropertyName("division_id")]
    public string DivisionId { get; set; }

    [JsonPropertyName("subtype")]
    public string Subtype { get; set; }
}

public class Cartography
{
    [JsonPropertyName("prominence")]
    public int Prominence { get; set; }

    [JsonPropertyName("min_zoom")]
    public object MinZoom { get; set; }

    [JsonPropertyName("max_zoom")]
    public object MaxZoom { get; set; }

    [JsonPropertyName("sort_key")]
    public object SortKey { get; set; }
}

public class LocalType
{
    [JsonPropertyName("en")]
    public string En { get; set; }
}

public class Names
{
    [JsonPropertyName("primary")]
    public string Primary { get; set; }

    [JsonPropertyName("common")]
    public Dictionary<string, string> Common { get; set; }

    // [JsonPropertyName("rules")]
    // public List<Rule> Rules { get; set; }
}

public class Division
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("geometry")]
    public string Geometry { get; set; }

    [JsonPropertyName("country")]
    public string Country { get; set; }

    // [JsonPropertyName("sources")]
    // public List<Source> Sources { get; set; }

    [JsonPropertyName("subtype")]
    public string Subtype { get; set; }

    [JsonPropertyName("admin_level")]
    public int? AdminLevel { get; set; }

    [JsonPropertyName("class")]
    public string Class { get; set; }

    [JsonPropertyName("names")]
    public Names Names { get; set; }

    // [JsonPropertyName("wikidata")]
    // public string Wikidata { get; set; }

    [JsonPropertyName("perspectives")]
    public object Perspectives { get; set; }

    [JsonPropertyName("local_type")]
    public LocalType LocalType { get; set; }

    [JsonPropertyName("region")]
    public string Region { get; set; }

    [JsonPropertyName("hierarchies")]
    public List<List<Hierarchy>> Hierarchies { get; set; }

    [JsonPropertyName("parent_division_id")]
    public string ParentDivisionId { get; set; }

    // [JsonPropertyName("norms")]
    // public object Norms { get; set; }

    [JsonPropertyName("population")]
    public int? Population { get; set; }

    [JsonPropertyName("capital_division_ids")]
    public List<string> CapitalDivisionIds { get; set; }

    [JsonPropertyName("capital_of_divisions")]
    public List<CapitalOfDivision> CapitalOfDivisions { get; set; }

    // [JsonPropertyName("cartography")]
    // public Cartography Cartography { get; set; }

    // [JsonPropertyName("version")]
    // public int Version { get; set; }

    [JsonPropertyName("bbox")]
    public Bbox Bbox { get; set; }

    // [JsonPropertyName("theme")]
    // public string Theme { get; set; }

    // [JsonPropertyName("type")]
    // public string Type { get; set; }
}

public class Hierarchy
{
    [JsonPropertyName("division_id")]
    public string DivisionId { get; set; }

    [JsonPropertyName("subtype")]
    public string Subtype { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class Rule
{
    [JsonPropertyName("variant")]
    public string Variant { get; set; }

    [JsonPropertyName("language")]
    public object Language { get; set; }

    [JsonPropertyName("perspectives")]
    public object Perspectives { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonPropertyName("between")]
    public object Between { get; set; }

    [JsonPropertyName("side")]
    public object Side { get; set; }
}

public class Source
{
    [JsonPropertyName("property")]
    public string Property { get; set; }

    [JsonPropertyName("dataset")]
    public string Dataset { get; set; }

    [JsonPropertyName("license")]
    public string License { get; set; }

    [JsonPropertyName("record_id")]
    public string RecordId { get; set; }

    [JsonPropertyName("update_time")]
    public DateTime UpdateTime { get; set; }

    [JsonPropertyName("confidence")]
    public object Confidence { get; set; }

    [JsonPropertyName("between")]
    public object Between { get; set; }
}

