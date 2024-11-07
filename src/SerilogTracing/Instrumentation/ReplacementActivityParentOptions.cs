/// <summary>
/// 
/// </summary>
public record ReplacementActivityParentOptions
{
    /// <summary>
    /// 
    /// </summary>
    public static ReplacementActivityParentOptions InheritAll =>
        new ReplacementActivityParentOptions
        {
            InheritTags = true,
            InheritParent = true,
            InheritFlags = true,
            InheritBaggage = true
        };

    /// <summary>
    /// 
    /// </summary>
    public static ReplacementActivityParentOptions InheritNone =>
        new ReplacementActivityParentOptions
        {
            InheritTags = false,
            InheritParent = false,
            InheritFlags = false,
            InheritBaggage = false
        };

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
