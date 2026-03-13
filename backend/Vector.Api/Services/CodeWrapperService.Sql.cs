using System.Text.Json;
using Vector.Api.Models;

namespace Vector.Api.Services;

public partial class CodeWrapperService
{
    /// <summary>
    /// Wraps SQL user code by prepending the test-case schema and seed data.
    /// The test-case JSON must contain <c>"schema"</c> and <c>"data"</c> string fields.
    /// Judge0 SQLite executes statements separated by semicolons.
    /// </summary>
    private static string WrapSqlCode(string userCode, string testCaseInput, InterviewQuestion? question)
    {
        try
        {
            var root = JsonDocument.Parse(testCaseInput).RootElement;

            if (!root.TryGetProperty("schema", out var schemaEl) ||
                !root.TryGetProperty("data",   out var dataEl))
            {
                throw new ArgumentException("SQL test case must contain 'schema' and 'data' fields.");
            }

            var schema = (schemaEl.GetString() ?? string.Empty).Trim();
            var data   = (dataEl.GetString()   ?? string.Empty).Trim();
            var query  = userCode.Trim();

            // Ensure each section ends with a semicolon.
            if (!string.IsNullOrEmpty(schema) && !schema.TrimEnd().EndsWith(';'))
                schema = schema.TrimEnd() + ";";
            if (!string.IsNullOrEmpty(data) && !data.TrimEnd().EndsWith(';'))
                data = data.TrimEnd() + ";";

            return $"{schema}\n\n{data}\n\n{query}";
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid SQL test case JSON format: {ex.Message}", ex);
        }
    }
}
