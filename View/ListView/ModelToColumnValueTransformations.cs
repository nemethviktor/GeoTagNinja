using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoTagNinja.View.ListView
{
    internal class ModelToColumnValueTransformations
    {
        public static string M2C_GPSAltitude(string modelValue)
        {
            if (modelValue.Contains(value: "m"))
            {
                modelValue = modelValue.Split('m')[0]
                    .Trim()
                    .Replace(oldChar: ',', newChar: '.');
            }

            if (modelValue.Contains(value: "/"))
            {
                if (modelValue.Contains(value: ",") || modelValue.Contains(value: "."))
                {
                    modelValue = modelValue.Split('/')[0]
                        .Trim()
                        .Replace(oldChar: ',', newChar: '.');
                }
                else // attempt to convert it to decimal
                {
                    try
                    {
                        bool parseBool = double.TryParse(s: modelValue.Split('/')[0], style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out double numerator);
                        parseBool = double.TryParse(s: modelValue.Split('/')[1], style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out double denominator);
                        modelValue = Math.Round(value: numerator / denominator, digits: 2)
                            .ToString(provider: CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        modelValue = "0.0";
                    }
                }
            }

            return modelValue;
        }
    }
}
