using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using GeoTagNinja.Helpers;

namespace GeoTagNinja.View.DialogAndMessageBoxes;

public class CustomMessageBox : Form
{
    private Label _lblMessage;
    private PictureBox _pictureBox;
    private const string NAMEANDTEXT = "CustomMessageBox";

    /// <summary>
    ///     Initializes a new instance of the CustomMessageBox class.
    /// </summary>
    /// <param name="text">The text to display in the message box.</param>
    /// <param name="caption">The text to display in the title bar of the message box.</param>
    /// <param name="buttons">Specifies which buttons to display in the message box.</param>
    /// <param name="icon">Specifies which icon to display in the message box.</param>
    /// <remarks>
    ///     This constructor initializes the message box with the specified message, title, buttons, and icon. It applies
    ///     the theme color based on the user's settings and creates the appropriate buttons and icon based on the specified
    ///     MessageBoxButtons and MessageBoxIcon values.
    /// </remarks>
    public CustomMessageBox(string text,
                            string caption,
                            MessageBoxButtons buttons,
                            MessageBoxIcon icon)
    {
        InitializeComponent();
        Text = $"GeoTagNinja - {caption}";
        _lblMessage.Text = text;
        // Apply theme
        HelperControlThemeManager.SetThemeColour(
            themeColour: HelperVariables.UserSettingUseDarkMode
                ? ThemeColour.Dark
                : ThemeColour.Light,
            parentControl: this);
        // Create buttons based on MessageBoxButtons value
        switch (buttons)
        {
            case MessageBoxButtons.OK:
                CreateButton(text: "OK", result: DialogResult.OK);
                break;
            case MessageBoxButtons.OKCancel:
                CreateButton(text: "OK", result: DialogResult.OK);
                CreateButton(text: "Cancel", result: DialogResult.Cancel);
                break;
            case MessageBoxButtons.YesNo:
                CreateButton(text: "&Yes", result: DialogResult.Yes);
                CreateButton(text: "&No", result: DialogResult.No);
                break;
            case MessageBoxButtons.YesNoCancel:
                CreateButton(text: "&Yes", result: DialogResult.Yes);
                CreateButton(text: "&No", result: DialogResult.No);
                CreateButton(text: "Cancel", result: DialogResult.Cancel);
                break;
            case MessageBoxButtons.RetryCancel:
                CreateButton(text: "&Retry", result: DialogResult.Retry);
                CreateButton(text: "Cancel", result: DialogResult.Cancel);
                break;
            case MessageBoxButtons.AbortRetryIgnore:
                CreateButton(text: "&Abort", result: DialogResult.Abort);
                CreateButton(text: "&Retry", result: DialogResult.Retry);
                CreateButton(text: "&Ignore", result: DialogResult.Ignore);
                break;
            default:
                throw new ArgumentOutOfRangeException(paramName: nameof(buttons),
                                                      actualValue: buttons,
                                                      message: null);
        }

        PositionButtons();
        // Set icon in PictureBox based on MessageBoxIcon value
        SetPictureBox(icon: icon);
        TopMost = true;
    }

    [Localizable(isLocalizable: false)]
    public sealed override string Text
    {
        get => base.Text;
        set => base.Text = value;
    }


    /// <summary>
    ///     Sets the picture box (icon) for the Form
    /// </summary>
    /// <param name="icon"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private void SetPictureBox(MessageBoxIcon icon)
    {
        if (icon == MessageBoxIcon.None)
        {
            _pictureBox.Image = null;
        }
        else if (icon == MessageBoxIcon.Information)
        {
            _pictureBox.Image = SystemIcons.Information.ToBitmap();
        }
        else if (icon == MessageBoxIcon.Warning)
        {
            _pictureBox.Image = SystemIcons.Warning.ToBitmap();
        }
        else if (icon == MessageBoxIcon.Error)
        {
            _pictureBox.Image = SystemIcons.Error.ToBitmap();
        }
        else if (icon == MessageBoxIcon.Exclamation)
        {
            _pictureBox.Image = SystemIcons.Exclamation.ToBitmap();
        }
        else if (icon == MessageBoxIcon.Question)
        {
            _pictureBox.Image = SystemIcons.Question.ToBitmap();
        }
        else if (icon == MessageBoxIcon.Stop)
        {
            _pictureBox.Image = SystemIcons.Error.ToBitmap();
        }
        else if (icon == MessageBoxIcon.Hand)
        {
            _pictureBox.Image = SystemIcons.Hand.ToBitmap();
        }
        else if (icon == MessageBoxIcon.Asterisk)
        {
            _pictureBox.Image = SystemIcons.Asterisk.ToBitmap();
        }
        else
        {
            throw new ArgumentOutOfRangeException(paramName: nameof(icon),
                                                  actualValue: icon, message: null);
        }
    }


    /// <summary>
    ///     Initializes the components of the CustomMessageBox form.
    /// </summary>
    /// <remarks>
    ///     This method initializes and configures the properties of the form and its controls, including a Label for
    ///     displaying messages and a PictureBox for displaying icons. It also sets the form's properties such as size, name,
    ///     and layout settings.
    /// </remarks>
    private void InitializeComponent()
    {
        // Initialize Label
        _lblMessage = new Label();
        _lblMessage.AutoSize = true;
        _lblMessage.Location = new Point(x: 63, y: 13);
        _lblMessage.Name = "_lblMessage";
        _lblMessage.MinimumSize = new Size(width: 20, height: 13);
        _lblMessage.MaximumSize = new Size(width: 250, height: 300);
        _lblMessage.TabIndex = 0;
        // Initialize PictureBox
        _pictureBox = new PictureBox();
        _pictureBox.Location = new Point(x: 13, y: 13);
        _pictureBox.Name = "_pictureBox";
        _pictureBox.Size = new Size(width: 50, height: 50);
        _pictureBox.TabIndex = 1;
        // Add controls to the form
        Controls.Add(value: _lblMessage);
        Controls.Add(value: _pictureBox);
        // Set form properties
        AutoScaleDimensions = new SizeF(width: 6F, height: 13F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(width: 284, height: 261);
        Name = NAMEANDTEXT;
        Text = NAMEANDTEXT;
        // Set additional properties
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ControlBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
    }

    private int _buttonCount;
    private const int ButtonWidth = 75;
    private const int ButtonHeight = 25;
    private const int ButtonSpacing = 10;
    private readonly List<Button> _buttons = new();

    private void CreateButton(string text,
                              DialogResult result)
    {
        Button button = new();
        button.Text = text;
        button.DialogResult = result;
        button.Click += (sender,
                         e) => DialogResult = result;
        button.Size = new Size(width: ButtonWidth, height: ButtonHeight);
        button.UseMnemonic = true;
        if (result == DialogResult.OK)
        {
            AcceptButton = button;
        }

        else if (result == DialogResult.Cancel)
        {
            CancelButton = button;
        }

        _buttons.Add(item: button);
        _buttonCount++;
    }

    private void PositionButtons()
    {
        int totalWidth = _buttonCount * ButtonWidth + (_buttonCount - 1) * ButtonSpacing;
        int startX = (Width - totalWidth) / 2;
        for (int i = 0; i < _buttonCount; i++)
        {
            _buttons[index: i].Location = new Point(
                x: startX + i * (ButtonWidth + ButtonSpacing),
                y: Math.Max(val1: _lblMessage.Bottom, val2: _pictureBox.Bottom) + 10);
            Controls.Add(value: _buttons[index: i]);
        }
    }
}
