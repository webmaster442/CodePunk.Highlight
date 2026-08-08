using CodePunk.Highlight.Core.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.Core.SyntaxHighlighting.Tokenization;
using System.Text;
using System.Web;

namespace CodePunk.Highlight.Core.SyntaxHighlighting;

/// <summary>
/// A token renderer that generates HTML with CSS classes for syntax highlighting.
/// </summary>
public sealed class HtmlCssClassesTokenRenderer : ITokenRenderer
{
    private readonly StringBuilder _html;

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlCssClassesTokenRenderer"/> class.
    /// </summary>
    public HtmlCssClassesTokenRenderer()
    {
        _html = new StringBuilder();
    }

    /// <inheritdoc/>
    public void BeginRender()
    {
        _html.AppendLine("<pre class=\"code-block\">");
        _html.AppendLine("<code>");
    }

    /// <inheritdoc/>
    public void EndRender()
    {
        _html.AppendLine("</code>");
        _html.AppendLine("</pre>");
    }

    /// <inheritdoc/>
    public void RenderToken(Token token)
    {
        var classToEmit = GetClass(token.Type);
        var htmlTokenValue = HttpUtility.HtmlEncode(token.Value);
        if (!string.IsNullOrEmpty(classToEmit))
        {
            _html.Append($"<span class=\"{classToEmit}\">{htmlTokenValue}</span>");
        }
        else
        {
            _html.Append(htmlTokenValue);
        }
    }

    private static string GetClass(TokenType type)
    {
        return type switch
        {
            TokenType.Text => "text",
            TokenType.Keyword => "keyword",
            TokenType.Type => "type",
            TokenType.String => "string",
            TokenType.Comment => "comment",
            TokenType.Number => "number",
            TokenType.Operator => "operator",
            TokenType.Punctuation => "punctuation",
            TokenType.Identifier => "identifier",
            TokenType.Preprocessor => "preprocessor",
            _ => string.Empty,
        };
    }

    /// <summary>
    /// Returns the rendered HTML as a string.
    /// </summary>
    /// <returns>The rendered HTML.</returns>
    public override string ToString()
        => _html.ToString();
}