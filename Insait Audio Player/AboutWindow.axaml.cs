using System;
using Avalonia.Controls;
using Insait_Audio_Player.Localization;
using Insait_Audio_Player.Services;

namespace Insait_Audio_Player;

public partial class AboutWindow : Window
{
    private readonly UiLocalizer _localizer = UiLocalizer.Create(new AppSettingsRepository().Load().LanguageCode);

    public AboutWindow()
    {
        InitializeComponent();

        var titleBar = this.FindControl<Border>("TitleBar")
                       ?? throw new InvalidOperationException("Missing TitleBar.");
        var titleBarText = this.FindControl<TextBlock>("AboutWindowTitleBarText")
                           ?? throw new InvalidOperationException("Missing AboutWindowTitleBarText.");
        var versionText = this.FindControl<TextBlock>("AboutVersionText")
                          ?? throw new InvalidOperationException("Missing AboutVersionText.");
        var createdByText = this.FindControl<TextBlock>("AboutCreatedByText")
                            ?? throw new InvalidOperationException("Missing AboutCreatedByText.");
        var createdDateText = this.FindControl<TextBlock>("AboutCreatedDateText")
                              ?? throw new InvalidOperationException("Missing AboutCreatedDateText.");
        var descriptionText = this.FindControl<TextBlock>("AboutDescriptionText")
                              ?? throw new InvalidOperationException("Missing AboutDescriptionText.");
        var stackText = this.FindControl<TextBlock>("AboutStackText")
                        ?? throw new InvalidOperationException("Missing AboutStackText.");
        var closeText = this.FindControl<TextBlock>("AboutCloseText")
                        ?? throw new InvalidOperationException("Missing AboutCloseText.");
        var closeButton = this.FindControl<Button>("CloseButton")
                          ?? throw new InvalidOperationException("Missing CloseButton.");
        var closeTitleButton = this.FindControl<Button>("CloseTitleButton")
                               ?? throw new InvalidOperationException("Missing CloseTitleButton.");

        Title = _localizer[UiLocalizer.Keys.AboutInfo];
        titleBarText.Text = _localizer[UiLocalizer.Keys.AboutInfo];
        versionText.Text = _localizer[UiLocalizer.Keys.AboutVersion];
        createdByText.Text = _localizer[UiLocalizer.Keys.AboutCreatedBy];
        createdDateText.Text = _localizer[UiLocalizer.Keys.AboutCreatedDate];
        descriptionText.Text = _localizer[UiLocalizer.Keys.AboutDescription];
        stackText.Text = _localizer[UiLocalizer.Keys.AboutStack];
        closeText.Text = _localizer[UiLocalizer.Keys.AboutClose];

        ToolTip.SetTip(closeTitleButton, _localizer[UiLocalizer.Keys.Close]);
        ToolTip.SetTip(closeButton, _localizer[UiLocalizer.Keys.AboutClose]);

        titleBar.PointerPressed += (_, e) => BeginMoveDrag(e);
        closeButton.Click += (_, _) => Close();
        closeTitleButton.Click += (_, _) => Close();
    }
}
