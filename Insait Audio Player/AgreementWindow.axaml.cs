using System;
using Avalonia.Controls;
using Insait_Audio_Player.Localization;
using Insait_Audio_Player.Services;

namespace Insait_Audio_Player;

public partial class AgreementWindow : Window
{
    private readonly UiLocalizer _localizer = UiLocalizer.Create(new AppSettingsRepository().Load().LanguageCode);

    public event EventHandler? Accepted;

    public event EventHandler? Declined;

    public AgreementWindow()
    {
        InitializeComponent();

        var titleBar = this.FindControl<Border>("TitleBar")
                       ?? throw new InvalidOperationException("Missing TitleBar.");
        var titleBarText = this.FindControl<TextBlock>("AgreementWindowTitleBarText")
                           ?? throw new InvalidOperationException("Missing AgreementWindowTitleBarText.");
        var titleText = this.FindControl<TextBlock>("AgreementTitleText")
                        ?? throw new InvalidOperationException("Missing AgreementTitleText.");
        var subtitleText = this.FindControl<TextBlock>("AgreementSubtitleText")
                           ?? throw new InvalidOperationException("Missing AgreementSubtitleText.");
        var bodyText = this.FindControl<TextBlock>("AgreementBodyText")
                       ?? throw new InvalidOperationException("Missing AgreementBodyText.");
        var footerText = this.FindControl<TextBlock>("AgreementFooterText")
                         ?? throw new InvalidOperationException("Missing AgreementFooterText.");
        var acceptButtonText = this.FindControl<TextBlock>("AcceptButtonText")
                               ?? throw new InvalidOperationException("Missing AcceptButtonText.");
        var declineButtonText = this.FindControl<TextBlock>("DeclineButtonText")
                                ?? throw new InvalidOperationException("Missing DeclineButtonText.");
        var acceptButton = this.FindControl<Button>("AcceptButton")
                           ?? throw new InvalidOperationException("Missing AcceptButton.");
        var declineButton = this.FindControl<Button>("DeclineButton")
                            ?? throw new InvalidOperationException("Missing DeclineButton.");
        var minimizeButton = this.FindControl<Button>("MinimizeButton")
                             ?? throw new InvalidOperationException("Missing MinimizeButton.");
        var closeButton = this.FindControl<Button>("CloseButton")
                          ?? throw new InvalidOperationException("Missing CloseButton.");

        Title = _localizer[UiLocalizer.Keys.AgreementWindowTitle];
        titleBarText.Text = _localizer[UiLocalizer.Keys.AgreementWindowTitle];
        titleText.Text = _localizer[UiLocalizer.Keys.AgreementTitle];
        subtitleText.Text = _localizer[UiLocalizer.Keys.AgreementSubtitle];
        bodyText.Text = _localizer[UiLocalizer.Keys.AgreementBody];
        footerText.Text = _localizer[UiLocalizer.Keys.AgreementFooter];
        acceptButtonText.Text = _localizer[UiLocalizer.Keys.AgreementAccept];
        declineButtonText.Text = _localizer[UiLocalizer.Keys.AgreementDecline];

        ToolTip.SetTip(minimizeButton, _localizer[UiLocalizer.Keys.Minimize]);
        ToolTip.SetTip(closeButton, _localizer[UiLocalizer.Keys.Close]);

        titleBar.PointerPressed += (_, e) => BeginMoveDrag(e);
        minimizeButton.Click += (_, _) => WindowState = WindowState.Minimized;
        closeButton.Click += (_, _) => Declined?.Invoke(this, EventArgs.Empty);

        acceptButton.Click += (_, _) => Accepted?.Invoke(this, EventArgs.Empty);
        declineButton.Click += (_, _) => Declined?.Invoke(this, EventArgs.Empty);
    }
}
