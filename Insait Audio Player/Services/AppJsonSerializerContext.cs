using System.Text.Json.Serialization;
using Insait_Audio_Player.Models;

namespace Insait_Audio_Player.Services;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppSettingsState))]
[JsonSerializable(typeof(UserAgreementState))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}

