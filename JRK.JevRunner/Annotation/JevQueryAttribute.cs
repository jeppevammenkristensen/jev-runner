namespace JRK.JevRunner.Annotation;

/// <summary>Enables request construction and typed answer generation for a partial query class or record class.</summary>
[AttributeUsage(AttributeTargets.Class)]
public class JevQueryAttribute : Attribute
{
}

/// <summary>
/// Inherit this in a JevQuery class and the logic will run prior sending this request
/// </summary>
public interface IJevQueryProcessRequestBeforeExecution
{
    /// <summary>
    /// Do modifications to Request and return it. This will contain the request with added questions
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    Request ProcessRequestBeforeExecution(Request request);
    
}