using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ORCA.Headless
{
    public sealed record Request(string Command, string[] Args, string Body = null);

    public sealed record Progress(int Line, string Text);

    public sealed record Response(bool Ok, string[] Lines, object Data = null)
    {
        public static Response Text(params string[] lines) => new(true, lines);
        public static Response Fail(string message) => new(false, [message]);
    }

    static class Protocol
    {
        public const string PipeName = "ORCA.GCController";

        public static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
    }
}
