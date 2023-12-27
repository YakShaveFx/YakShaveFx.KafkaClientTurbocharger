namespace YakShaveFx.KafkaClientTurbocharger.Core.Consumer;

/// <summary>
/// Possible strategies to be used for record handling parallelization.
/// </summary>
public enum ParallelismStrategy
{
    /// <summary>
    /// Records from different partitions may be processed in parallel, but records from the same partition will be processed sequentially.
    /// </summary>
    PerPartition,
    
    /// <summary>
    /// Records with different keys may be processed in parallel, but records with the same key will be processed sequentially.
    /// </summary>
    PerKey,
    
    /// <summary>
    /// Records may be processed in parallel without any restrictions.
    /// </summary>
    LeeroyJenkins
}