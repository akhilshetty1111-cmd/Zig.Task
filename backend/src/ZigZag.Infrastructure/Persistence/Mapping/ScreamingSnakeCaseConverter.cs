using System.Text;

namespace ZigZag.Infrastructure.Persistence.Mapping;

/// <summary>
/// Converts between PascalCase enum member names (<c>InProgress</c>) and the
/// SCREAMING_SNAKE_CASE strings PostgreSQL stores (<c>IN_PROGRESS</c>).
/// See docs/architecture.md, decision 9, for why enums are stored as checked
/// strings rather than integers or native PostgreSQL enum types.
/// </summary>
internal static class ScreamingSnakeCaseConverter
{
    internal static string ToScreamingSnakeCase(string pascalCase)
    {
        var builder = new StringBuilder(pascalCase.Length + 4);

        for (var i = 0; i < pascalCase.Length; i++)
        {
            var c = pascalCase[i];
            if (i > 0 && char.IsUpper(c))
            {
                builder.Append('_');
            }
            builder.Append(char.ToUpperInvariant(c));
        }

        return builder.ToString();
    }
}
