namespace WorkplaceIQ.AspNet.Rendering;

public sealed record ComponentInteractionOptions(
    bool AllowAdd = true,
    bool AllowEdit = true,
    bool AllowDelete = true,
    bool AllowComment = true,
    bool AllowLabel = true)
{
    public static ComponentInteractionOptions SystemManaged(
        bool allowComment = true,
        bool allowLabel = true)
    {
        return new ComponentInteractionOptions(
            AllowAdd: false,
            AllowEdit: false,
            AllowDelete: false,
            AllowComment: allowComment,
            AllowLabel: allowLabel);
    }
}
