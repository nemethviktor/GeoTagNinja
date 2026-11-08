using GeoTagNinja.Helpers;
using GeoTagNinja.Helpers.API;
using GeoTagNinja.Helpers.Geo;
using GeoTagNinja.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using static GeoTagNinja.Helpers.UI.HelperControlAndMessageBoxHandling;
using Themer = WinFormsDarkThemerNinja.Themer;

namespace GeoTagNinja.View.Forms;

/// <summary>
///     What the Location tab gained from the old Edit Data dialog: the coordinates also as degrees-minutes-seconds,
///     image direction (with a compass), destination latitude/longitude, and "Get from Web" for the altitude.
/// </summary>
/// <remarks>
///     Added in code on top of the designer layout for the same reason as the other new tabs (see
///     <see cref="FrmEditFileData.BuildMetadataTabs" />): it makes room by moving the existing controls down at run
///     time, which would otherwise mean re-laying-out a designer file by hand. The decimal boxes remain the only thing
///     that is staged; the degrees-minutes-seconds boxes are a second way of typing into them.
/// </remarks>
public partial class FrmEditFileData
{
    /// <summary>How much taller the Location tab, and with it the form, becomes.</summary>
    private const int LocationExtraHeight = 58;

    private static readonly string[] CompassPoints =
        ["N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"];

    private readonly List<(NumericUpDown Decimal, TextBox Sexagesimal, bool IsLatitude)> _coordinatePairs = [];
    private NumericUpDown nud_GPSImgDirection;
    private NumericUpDown nud_GPSDestLatitude;
    private NumericUpDown nud_GPSDestLongitude;
    private ComboBox cbx_Compass;
    private Button btn_GetAltitudeFromWeb;
    private bool _syncingCoordinates;

    private void BuildLocationExtras()
    {
        // make room: everything below the tabs moves down, and the tabs and the form grow to match
        int boundary = tcr_EditData.Bottom;
        foreach (Control control in Controls)
        {
            if (control != tcr_EditData &&
                control.Top >= boundary - 60)
            {
                control.Top += LocationExtraHeight;
            }
        }

        tcr_EditData.Height += LocationExtraHeight;
        ClientSize = new Size(width: ClientSize.Width, height: ClientSize.Height + LocationExtraHeight);

        gbx_GPSData.Height += LocationExtraHeight;
        gbx_LocationData.Top += LocationExtraHeight;
        btn_RemoveGeoData.Top += LocationExtraHeight;

        // the existing altitude row slides down to make room for the three new rows above it
        lbl_GPSAltitude.Top = 182;
        nud_GPSAltitude.Top = 180;

        AddGpsLabel(name: "lbl_Sexagesimal", english: "Sexagesimal", x: 260, y: 16);

        nud_GPSImgDirection = NewCoordinateNud(name: "nud_GPSImgDirection", y: 93, decimalPlaces: 2, limit: 360,
            lowest: 0);
        nud_GPSDestLatitude = NewCoordinateNud(name: "nud_GPSDestLatitude", y: 122, decimalPlaces: 6, limit: 90,
            lowest: -90);
        nud_GPSDestLongitude = NewCoordinateNud(name: "nud_GPSDestLongitude", y: 151, decimalPlaces: 6, limit: 180,
            lowest: -180);

        AddGpsLabel(name: "lbl_ImageDirection", english: "Image Direction [°]:", x: 14, y: 95);
        AddGpsLabel(name: "lbl_DestLatitude", english: "Dest. Latitude:", x: 14, y: 124);
        AddGpsLabel(name: "lbl_DestLongitude", english: "Dest. Longitude:", x: 14, y: 153);

        PairWithSexagesimal(decimalBox: nud_GPSLatitude, y: 35, isLatitude: true);
        PairWithSexagesimal(decimalBox: nud_GPSLongitude, y: 64, isLatitude: false);
        PairWithSexagesimal(decimalBox: nud_GPSDestLatitude, y: 122, isLatitude: true);
        PairWithSexagesimal(decimalBox: nud_GPSDestLongitude, y: 151, isLatitude: false);

        cbx_Compass = new ComboBox
        {
            Name = "cbx_Compass",
            Tag = NoAttributeTag,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(x: 254, y: 93),
            Size = new Size(width: 150, height: 21)
        };
        cbx_Compass.Items.AddRange(items: CompassPoints);
        cbx_Compass.SelectedIndexChanged += (_,
                                             _) => OnCompassChosen();
        gbx_GPSData.Controls.Add(value: cbx_Compass);
        nud_GPSImgDirection.ValueChanged += (_,
                                             _) => ShowCompassFor();
        nud_GPSImgDirection.TextChanged += (_,
                                            _) => ShowCompassFor();

        string buttonText = ReturnControlText(controlName: "btn_GetAltitudeFromWeb",
            fakeControlType: FakeControlTypes.Button);
        btn_GetAltitudeFromWeb = new Button
        {
            Name = "btn_GetAltitudeFromWeb",
            Text = string.IsNullOrWhiteSpace(value: buttonText)
                ? "Get from Web"
                : buttonText,
            Location = new Point(x: 254, y: 179),
            Size = new Size(width: 150, height: 23),
            UseVisualStyleBackColor = true
        };
        btn_GetAltitudeFromWeb.Click += (_,
                                         _) => GetAltitudeFromWeb();
        gbx_GPSData.Controls.Add(value: btn_GetAltitudeFromWeb);
    }

    private void AddGpsLabel(string name,
                             string english,
                             int x,
                             int y)
    {
        gbx_GPSData.Controls.Add(value: new Label
        {
            Name = name,
            Text = FieldCaption(controlName: name, fallback: english),
            Location = new Point(x: x, y: y),
            AutoSize = true
        });
    }

    /// <summary>A decimal box like the existing latitude one, named so the form's generic handlers pick it up.</summary>
    private NumericUpDown NewCoordinateNud(string name,
                                           int y,
                                           int decimalPlaces,
                                           decimal limit,
                                           decimal lowest)
    {
        NumericUpDown nud = new()
        {
            Name = name,
            DecimalPlaces = decimalPlaces,
            Minimum = lowest,
            Maximum = limit,
            Location = new Point(x: nud_GPSLatitude.Left, y: y),
            Size = nud_GPSLatitude.Size
        };
        nud.ValueChanged += tbx_cbx_dtp_nud_Any_TextChanged;
        nud.Enter += any_nud_Enter;
        nud.Leave += any_nud_Leave;
        gbx_GPSData.Controls.Add(value: nud);
        return nud;
    }

    /// <summary>Puts a degrees-minutes-seconds box beside a decimal one and keeps the two in step.</summary>
    private void PairWithSexagesimal(NumericUpDown decimalBox,
                                     int y,
                                     bool isLatitude)
    {
        TextBox box = new()
        {
            Name = $"tbx_Sexagesimal_{decimalBox.Name.Substring(startIndex: 4)}",
            Tag = NoAttributeTag,
            Location = new Point(x: 254, y: y),
            Size = new Size(width: 150, height: 20)
        };
        gbx_GPSData.Controls.Add(value: box);
        _coordinatePairs.Add(item: (decimalBox, box, isLatitude));

        decimalBox.ValueChanged += (_,
                                    _) => ShowSexagesimalFor(decimalBox: decimalBox);
        decimalBox.TextChanged += (_,
                                   _) => ShowSexagesimalFor(decimalBox: decimalBox);

        box.Enter += any_nud_Enter;
        box.Leave += (_,
                      _) =>
        {
            any_nud_Leave(sender: box, e: EventArgs.Empty);
            ReadSexagesimal(decimalBox: decimalBox);
        };
        box.KeyDown += (_,
                        e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
                ReadSexagesimal(decimalBox: decimalBox);
            }
        };
    }

    private (NumericUpDown Decimal, TextBox Sexagesimal, bool IsLatitude) PairOf(NumericUpDown decimalBox)
    {
        return _coordinatePairs.Find(match: pair => pair.Decimal == decimalBox);
    }

    private void ShowSexagesimalFor(NumericUpDown decimalBox)
    {
        if (_syncingCoordinates)
        {
            return;
        }

        (NumericUpDown _, TextBox box, bool isLatitude) = PairOf(decimalBox: decimalBox);
        if (box == null)
        {
            return;
        }

        // a blank decimal box (no coordinate in the file) shows a blank here too
        box.Text = string.IsNullOrWhiteSpace(value: decimalBox.Text)
            ? string.Empty
            : Sexagesimal.Format(degrees: (double)decimalBox.Value, isLatitude: isLatitude);
    }

    /// <summary>Takes what was typed into a degrees-minutes-seconds box and puts it into the decimal one.</summary>
    private void ReadSexagesimal(NumericUpDown decimalBox)
    {
        (NumericUpDown _, TextBox box, bool isLatitude) = PairOf(decimalBox: decimalBox);
        if (box == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value: box.Text) ||
            !Sexagesimal.TryParse(text: box.Text, isLatitude: isLatitude, degrees: out double degrees))
        {
            // nothing usable typed: put back what the decimal box says
            ShowSexagesimalFor(decimalBox: decimalBox);
            return;
        }

        decimal value = Math.Round(d: (decimal)degrees, decimals: decimalBox.DecimalPlaces);
        value = Math.Max(val1: decimalBox.Minimum, val2: Math.Min(val1: decimalBox.Maximum, val2: value));
        if (decimalBox.Text.Length == 0 ||
            decimalBox.Value != value)
        {
            decimalBox.Value = value;
        }

        ShowSexagesimalFor(decimalBox: decimalBox);
    }

    private void ShowCompassFor()
    {
        if (_syncingCoordinates ||
            cbx_Compass == null)
        {
            return;
        }

        _syncingCoordinates = true;
        try
        {
            cbx_Compass.SelectedIndex = string.IsNullOrWhiteSpace(value: nud_GPSImgDirection.Text)
                ? -1
                : (int)Math.Round(a: (double)nud_GPSImgDirection.Value / 22.5) % CompassPoints.Length;
        }
        finally
        {
            _syncingCoordinates = false;
        }
    }

    private void OnCompassChosen()
    {
        if (_syncingCoordinates ||
            cbx_Compass.SelectedIndex < 0)
        {
            return;
        }

        nud_GPSImgDirection.Value = (decimal)(cbx_Compass.SelectedIndex * 22.5);
    }

    private static string AltitudeText(string key,
                                       string fallback)
    {
        string text = ReturnControlText(controlName: key, fakeControlType: FakeControlTypes.MessageBox);
        return string.IsNullOrWhiteSpace(value: text)
            ? fallback
            : text;
    }

    /// <summary>Fills the altitude from GeoNames' elevation model for the coordinates in the boxes above it.</summary>
    private async void GetAltitudeFromWeb()
    {
        if (string.IsNullOrWhiteSpace(value: nud_GPSLatitude.Text) ||
            string.IsNullOrWhiteSpace(value: nud_GPSLongitude.Text))
        {
            _ = Themer.ShowMessageBoxWithResult(
                message: AltitudeText(key: "mbx_FrmEditFileData_InfoCoordinatesNeededForAltitude",
                    fallback: "Enter a latitude and a longitude first."),
                icon: MessageBoxIcon.Information,
                buttons: MessageBoxButtons.OK);
            return;
        }

        string latitude = nud_GPSLatitude.Value.ToString(provider: CultureInfo.InvariantCulture);
        string longitude = nud_GPSLongitude.Value.ToString(provider: CultureInfo.InvariantCulture);

        btn_GetAltitudeFromWeb.Enabled = false;
        Cursor = Cursors.WaitCursor;
        double? metres;
        try
        {
            metres = await Task.Run(function: () => GeoNamesToponomyExtractor.API_GetAltitudeFromWeb(
                latitude: latitude,
                longitude: longitude));
        }
        finally
        {
            Cursor = Cursors.Default;
            btn_GetAltitudeFromWeb.Enabled = true;
        }

        if (metres == null)
        {
            _ = Themer.ShowMessageBoxWithResult(
                message: AltitudeText(key: "mbx_FrmEditFileData_WarningNoAltitudeFromWeb",
                    fallback: "The web service has no altitude for this position."),
                icon: MessageBoxIcon.Warning,
                buttons: MessageBoxButtons.OK);
            return;
        }

        // the altitude box is held in the user's unit (metres or feet), never converted behind their back
        double shown = HelperVariables.UserSettingUseImperial
            ? metres.Value * HelperVariables.MetreToFeet
            : metres.Value;
        nud_GPSAltitude.Value = Math.Max(val1: nud_GPSAltitude.Minimum,
            val2: Math.Min(val1: nud_GPSAltitude.Maximum, val2: (decimal)Math.Round(value: shown, digits: 2)));
    }
}
