using JRK.JevRunner.Responses;

namespace JRK.JevRunner.Image;

/// <summary>Supplies either one bounding-box answer or a collection of answers for image annotation.</summary>
/// <remarks>Collections must support repeated enumeration for validation and drawing.</remarks>
public union AppliedBox(BoxAnswer, IEnumerable<BoxAnswer>)
{
    /// <summary>Exposes either union case as an enumerable of bounding-box answers.</summary>
    public IEnumerable<BoxAnswer> GetAll()
    {
        return this switch
        {
            BoxAnswer a => [a],
            IEnumerable<BoxAnswer> a => a
        };
    }
}