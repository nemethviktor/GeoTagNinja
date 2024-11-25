using System;
using System.Windows.Forms;
using GeoTagNinja.View.DialogAndMessageBoxes;
using static GeoTagNinja.Helpers.HelperControlAndMessageBoxHandling;

namespace GeoTagNinja.Helpers;

internal class HelperControlAndMessageBoxCustomMessageBoxManager
{
    internal enum MessageBoxTextSource
    {
        CONTROLTYPE,
        MANUAL
    }

    /// <summary>
    ///     Unified method to call the custom messageboxes.
    /// </summary>
    /// <param name="controlName">Basically the reference to the element in the Resource file</param>
    /// <param name="captionType">Error/Question/Info/etc <see cref="MessageBoxCaption" /></param>
    /// <param name="buttons">OK/Cancel/YesNo/etc see <see cref="MessageBoxButtons" /></param>
    /// <param name="icon">Optional <see cref="MessageBoxIcon" />. Attempts to default from the captionType param.</param>
    /// <param name="extraMessage">Optional extra message to be stamped at the end.</param>
    /// <param name="textSource">
    ///     Either MessageBoxTextSource.CONTROLTYPE or MANUAL. The latter basically means that we want to
    ///     feed it a manual text value rather than query the resource. Shouldn't generally be used.
    /// </param>
    internal static void ShowMessageBox(string controlName,
                                        MessageBoxCaption captionType,
                                        MessageBoxButtons buttons,
                                        MessageBoxIcon icon = MessageBoxIcon.None,
                                        string extraMessage = "",
                                        MessageBoxTextSource textSource = MessageBoxTextSource.CONTROLTYPE)
    {
        _ = ShowMessageBoxWithResult(controlName: controlName, captionType: captionType, buttons: buttons, icon: icon,
            extraMessage: extraMessage, textSource: textSource);
    }

    /// <summary>
    ///     Version of the <see cref="ShowMessageBox"></see> that sends back a <see cref="DialogResult" />
    /// </summary>
    /// <param name="controlName">
    ///     <inheritdoc cref="ShowMessageBox" />
    /// </param>
    /// <param name="captionType">
    ///     <inheritdoc cref="ShowMessageBox" />
    /// </param>
    /// <param name="buttons">
    ///     <inheritdoc cref="ShowMessageBox" />
    /// </param>
    /// <param name="icon">
    ///     <inheritdoc cref="ShowMessageBox" />
    /// </param>
    /// <param name="extraMessage">
    ///     <inheritdoc cref="ShowMessageBox" />
    /// </param>
    /// <param name="textSource">
    ///     <inheritdoc cref="ShowMessageBox" />
    /// </param>
    /// <returns>A <see cref="DialogResult" /></returns>
    internal static DialogResult ShowMessageBoxWithResult(string controlName,
                                                          MessageBoxCaption captionType,
                                                          MessageBoxButtons buttons,
                                                          MessageBoxIcon icon = MessageBoxIcon.None,
                                                          string extraMessage = "",
                                                          MessageBoxTextSource textSource =
                                                              MessageBoxTextSource.CONTROLTYPE)
    {
        // we don't otherwise use MessageBoxIcon.None so...
        if (icon == MessageBoxIcon.None)
        {
            icon = captionType switch
            {
                MessageBoxCaption.None => MessageBoxIcon.None,
                MessageBoxCaption.Information => MessageBoxIcon.Information,
                MessageBoxCaption.Warning => MessageBoxIcon.Warning,
                MessageBoxCaption.Error => MessageBoxIcon.Error,
                MessageBoxCaption.Question => MessageBoxIcon.Question,
                MessageBoxCaption.Exclamation => MessageBoxIcon.Exclamation,
                MessageBoxCaption.Stop => MessageBoxIcon.Stop,
                MessageBoxCaption.Hand => MessageBoxIcon.Hand,
                MessageBoxCaption.Asterisk => MessageBoxIcon.Asterisk,
                _ => MessageBoxIcon.None
            };
        }

        // Pad extraMessage with a newline.
        if (!string.IsNullOrWhiteSpace(value: extraMessage))
        {
            extraMessage = Environment.NewLine + extraMessage;
        }

        CustomMessageBox customMessageBox = new(
            text:
            $"{(textSource == MessageBoxTextSource.CONTROLTYPE ? ReturnControlText(controlName: controlName, fakeControlType: FakeControlTypes.MessageBox) : string.Empty)}{extraMessage}",
            caption: ReturnControlText(controlName: captionType.ToString(),
                fakeControlType: FakeControlTypes.MessageBoxCaption),
            buttons: buttons,
            icon: icon);
        DialogResult dialogResult = customMessageBox.ShowDialog();

        return dialogResult;
    }
}