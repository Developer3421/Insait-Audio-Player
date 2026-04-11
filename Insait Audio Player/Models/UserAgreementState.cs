using System;

namespace Insait_Audio_Player.Models;

public sealed class UserAgreementState
{
    public bool Accepted { get; set; }

    public string AgreementVersion { get; set; } = "1.0";

    public DateTimeOffset? AcceptedAtUtc { get; set; }
}

