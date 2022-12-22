using System;
using System.Drawing;
using System.Windows.Forms;

namespace GeoTagNinja;

/// <summary>
///     PictureBox that can also show an informative text when there is no image (Image = null).
///     Alternatively, an error message can be displayed (when set, holds until updat to porperty Image).
/// </summary>
public partial class ImagePreview : PictureBox
{
    private Font _font = new(familyName: "Arial", emSize: 14);
    private string _textToShow;

    public ImagePreview()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     Text to show when there is no image to be shown (is null).
    /// </summary>
    public string EmptyText { get; set; } = "No image to show";

    /// <summary>
    ///     Image to show.
    /// </summary>
    public new Image Image
    {
        get => base.Image;
        set
        {
            base.Image = value;
            if (value == null)
            {
                _textToShow = EmptyText;
            }
            else
            {
                _textToShow = null;
            }
        }
    }

    /// <summary>
    ///     The font to show the informative text with
    /// </summary>
    public new Font Font
    {
        get => _font;
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName: "The font set may not be empty");
            }

            _font = value;
        }
    }

    /// <summary>
    ///     Instead of an image or the default message, show a specific (error)
    ///     message. This is shown until a new message is set or the property Image
    ///     is updated.
    /// </summary>
    public void SetErrorMessage(string message)
    {
        Image = null;
        _textToShow = message;
    }

    private void ImagePreview_Paint(object sender,
                                    PaintEventArgs e)
    {
        // Write text, if no image is present and text is set
        if (Image == null && !string.IsNullOrEmpty(value: _textToShow))
        {
            // Place in the middle
            SizeF sz = e.Graphics.MeasureString(text: _textToShow, font: _font);
            Point loc = new(
                x: (Width - (int)Math.Round(a: sz.Width)) / 2,
                y: (Height - (int)Math.Round(a: sz.Height)) / 2
            );
            e.Graphics.DrawString(s: _textToShow, font: _font, brush: Brushes.DarkGray, point: loc);
        }
    }
}