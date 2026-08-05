using Markdig;
using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Services;

public static class MarkdownRenderer {
    private static readonly MarkdownPipeline SafeMarkdownPipeline =
        new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .DisableHtml()
            .Build();

    public static MarkupString ToSafeHtml(string text) => (MarkupString)Markdown.ToHtml(text, SafeMarkdownPipeline);
}

