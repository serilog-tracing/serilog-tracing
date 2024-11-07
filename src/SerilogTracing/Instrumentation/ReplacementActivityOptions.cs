namespace SerilogTracing.Instrumentation;

/// <summary>
/// 
/// </summary>
public record struct ReplacementActivityOptions
{
    /// <summary>
    /// 
    /// </summary>
    public bool InheritTags { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public bool InheritParent { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public bool InheritFlags { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public bool InheritBaggage { get; set; }
}