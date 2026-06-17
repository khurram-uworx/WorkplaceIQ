namespace WorkplaceIQ.Tests.TestDoubles;

using Microsoft.AspNetCore.Razor.TagHelpers;

internal static class TagHelperOutputFactory
{
    public static TagHelperOutput Create(string tagName = "iq-feed")
    {
        return new TagHelperOutput(
            tagName,
            new TagHelperAttributeList(),
            (_, _) =>
            {
                var content = new DefaultTagHelperContent();
                return Task.FromResult<TagHelperContent>(content);
            });
    }
}
