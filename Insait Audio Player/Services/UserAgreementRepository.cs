using System;
using System.IO;
using System.Text.Json;
using Insait_Audio_Player.Models;

namespace Insait_Audio_Player.Services;

public sealed class UserAgreementRepository
{
    private readonly string _filePath = Path.Combine(AppDataPaths.DataDirectory, "user-agreement.json");

    public UserAgreementState Load(string agreementVersion)
    {
        if (!File.Exists(_filePath))
        {
            return new UserAgreementState { AgreementVersion = agreementVersion };
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            var state = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.UserAgreementState)
                        ?? new UserAgreementState();

            if (!string.Equals(state.AgreementVersion, agreementVersion, StringComparison.Ordinal))
            {
                return new UserAgreementState { AgreementVersion = agreementVersion };
            }

            return state;
        }
        catch
        {
            RecreateStateFile();
            return new UserAgreementState { AgreementVersion = agreementVersion };
        }
    }

    public void SaveAccepted(string agreementVersion)
    {
        var state = new UserAgreementState
        {
            Accepted = true,
            AgreementVersion = agreementVersion,
            AcceptedAtUtc = DateTimeOffset.UtcNow
        };

        var json = JsonSerializer.Serialize(state, AppJsonSerializerContext.Default.UserAgreementState);
        File.WriteAllText(_filePath, json);
    }

    private void RecreateStateFile()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
        catch
        {
            // ignored
        }
    }
}

