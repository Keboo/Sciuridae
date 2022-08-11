using System.Text.Json.Serialization;

namespace Sciuridae.Cli;

[JsonSerializable(typeof(string[]))]
internal partial class StringArrayContext : JsonSerializerContext
{
}