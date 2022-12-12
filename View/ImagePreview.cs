using System;
using System.Drawing;
using System.Windows.Forms;

namespace GeoTagNinja
{
    /// <summary>
    /// PictureBox that can also show an informative text when there is no image (Image = null).
    /// Alternatively, an error message can be displayed (when set, holds until updat to porperty Image).
    /// </summary>
    public partial class ImagePreview : PictureBox
    {
        private string _emptyText = "No image to show";
        private Font _font = new Font("Arial", 14);
        private string _textToShow = null;

        public ImagePreview()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Text to show when there is no image to be shown (is null).
        /// </summary>
        public string EmptyText
        {
            get => _emptyText;
            set
            {
                _emptyText = value;
            }
        }

        /// <summary>
        /// Image to show.
        /// </summary>
        public new Image Image {
            get => base.Image;
            set
            {
                base.Image = value;
                if (value == null)
                {
                    this._textToShow = this._emptyText;
                } else
                {
                    this._textToShow = null;
                }
            }
        }

        /// <summary>
        /// The font to show the informative text with
        /// </summary>
        public new Font Font {
            get => this._font;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("The font set may not be empty");
                } else
                {
                    this._font = value;
                }
            }
        }

        /// <summary>
        /// Instead of an image or the default message, show a specific (error)
        /// message. This is shown until a new message is set or the property Image
        /// is updated.
        /// </summary>
        public void SetErrorMessage( string message)
        {
            this.Image = null;
            this._textToShow = message;
        }

        private void ImagePreview_Paint(object sender, PaintEventArgs e)
        {
            // Write text, if no image is present and text is set
            if (this.Image == null && !(string.IsNullOrEmpty(this._textToShow))) {
                // Place in the middle
                SizeF sz = e.Graphics.MeasureString(this._textToShow, this._font);
                Point loc = new Point( 
                    (this.Width - (int)Math.Round(sz.Width))/2,
                    (this.Height - (int)Math.Round(sz.Height)) / 2
                    );
                e.Graphics.DrawString(this._textToShow, this._font, Brushes.DarkGray, loc);
            }
        }
    }
}
