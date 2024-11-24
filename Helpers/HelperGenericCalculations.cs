using System;
using System.Collections.Generic;
using System.Globalization;

namespace GeoTagNinja.Helpers;

/// <summary>
///     A set of equations to calculate data related to image field of views and coordinate pairs for GPSImgDirection
///     Courtesy of ChatGPT....
/// </summary>
internal static class HelperGenericCalculations
{
    /// <summary>
    ///     Estimates the sensor size for an image.
    /// </summary>
    /// <param name="focalLength"></param>
    /// <param name="focalLengthIn35mmFilm"></param>
    /// <returns></returns>
    internal static double EstimateSensorSize(double focalLength,
                                              double focalLengthIn35mmFilm)
    {
        // Calculate the crop factor
        double cropFactor = focalLengthIn35mmFilm / focalLength;

        // Estimate the sensor size (diagonal) based on the crop factor
        double fullFrameSensorDiagonal = 43.3; // Approximate diagonal of a full-frame sensor in millimeters
        double estimatedSensorSize = fullFrameSensorDiagonal / cropFactor;

        return estimatedSensorSize;
    }

    /// <summary>
    ///     Calculates the coordinate-pair for the GPSImgDirection
    /// </summary>
    /// <param name="startLatitude"></param>
    /// <param name="startLongitude"></param>
    /// <param name="gpsImgDirection"></param>
    /// <param name="distance"></param>
    /// <returns>Coordinate-pair where "the photographer was looking"</returns>
    internal static (double, double) CalculateTargetCoordinates(double startLatitude,
                                                                double startLongitude,
                                                                double gpsImgDirection,
                                                                double distance)
    {
        // Earth's radius in kilometers
        double earthRadius = 6371.0;

        // Convert latitude and longitude from degrees to radians
        double startLatitudeRad = ToRadians(degrees: startLatitude);
        double startLongitudeRad = ToRadians(degrees: startLongitude);
        double bearingRad = ToRadians(degrees: gpsImgDirection);

        // Calculate the target latitude and longitude
        double targetLatitudeRad = Math.Asin(d: Math.Sin(a: startLatitudeRad) * Math.Cos(d: distance / earthRadius) +
                                                Math.Cos(d: startLatitudeRad) * Math.Sin(a: distance / earthRadius) * Math.Cos(d: bearingRad));

        double targetLongitudeRad = startLongitudeRad +
                                    Math.Atan2(y: Math.Sin(a: bearingRad) * Math.Sin(a: distance / earthRadius) * Math.Cos(d: startLatitudeRad),
                                               x: Math.Cos(d: distance / earthRadius) - Math.Sin(a: startLatitudeRad) * Math.Sin(a: targetLatitudeRad));

        // Convert the target latitude and longitude from radians back to degrees
        double targetLatitude = ToDegrees(radians: targetLatitudeRad);
        double targetLongitude = ToDegrees(radians: targetLongitudeRad);

        return (targetLatitude, targetLongitude);
    }

    /// <summary>
    ///     Calculates the 2 points of a triangle for FOV purposes (with the 3rd being the position of the image/photographer)
    /// </summary>
    /// <param name="startLatitude"></param>
    /// <param name="startLongitude"></param>
    /// <param name="gpsImgDirection"></param>
    /// <param name="fovAngle"></param>
    /// <param name="distance"></param>
    /// <returns>"Left" coordinate-pair and "Right" coordinate-pair</returns>
    internal static (List<(double, double)>, List<(double, double)>) CalculateFovCoordinates(double startLatitude,
                                                                                             double startLongitude,
                                                                                             double gpsImgDirection,
                                                                                             double fovAngle,
                                                                                             double distance)
    {
        // Earth's radius in kilometers
        double earthRadius = 6371.0;

        // Convert latitude and longitude from degrees to radians
        double startLatitudeRad = ToRadians(degrees: startLatitude);
        double startLongitudeRad = ToRadians(degrees: startLongitude);
        double bearingRad = ToRadians(degrees: gpsImgDirection);

        // Calculate the target latitude and longitude
        (double targetLatitude, double targetLongitude) = CalculateTargetCoordinates(startLatitude: startLatitude, startLongitude: startLongitude, gpsImgDirection: gpsImgDirection, distance: distance);

        // Calculate the FOV coordinates
        double halfFovRad = ToRadians(degrees: fovAngle / 2.0);

        double leftBearingRad = bearingRad - halfFovRad;
        double rightBearingRad = bearingRad + halfFovRad;

        double leftLatitudeRad = Math.Asin(d: Math.Sin(a: startLatitudeRad) * Math.Cos(d: distance / earthRadius) +
                                              Math.Cos(d: startLatitudeRad) * Math.Sin(a: distance / earthRadius) * Math.Cos(d: leftBearingRad));
        double leftLongitudeRad = startLongitudeRad +
                                  Math.Atan2(y: Math.Sin(a: leftBearingRad) * Math.Sin(a: distance / earthRadius) * Math.Cos(d: startLatitudeRad),
                                             x: Math.Cos(d: distance / earthRadius) - Math.Sin(a: startLatitudeRad) * Math.Sin(a: leftLatitudeRad));

        double rightLatitudeRad = Math.Asin(d: Math.Sin(a: startLatitudeRad) * Math.Cos(d: distance / earthRadius) +
                                               Math.Cos(d: startLatitudeRad) * Math.Sin(a: distance / earthRadius) * Math.Cos(d: rightBearingRad));
        double rightLongitudeRad = startLongitudeRad +
                                   Math.Atan2(y: Math.Sin(a: rightBearingRad) * Math.Sin(a: distance / earthRadius) * Math.Cos(d: startLatitudeRad),
                                              x: Math.Cos(d: distance / earthRadius) - Math.Sin(a: startLatitudeRad) * Math.Sin(a: rightLatitudeRad));

        // Convert the FOV coordinates from radians back to degrees
        double leftLatitude = ToDegrees(radians: leftLatitudeRad);
        double leftLongitude = ToDegrees(radians: leftLongitudeRad);
        List<(double, double)> leftCoords = new()
        {
            (leftLatitude, leftLongitude)
        };

        double rightLatitude = ToDegrees(radians: rightLatitudeRad);
        double rightLongitude = ToDegrees(radians: rightLongitudeRad);
        List<(double, double)> rightCoords = new()
        {
            (rightLatitude, rightLongitude)
        };

        return (leftCoords, rightCoords);
    }

    public static (List<(double, double)>, List<(double, double)>) CalculateFovCoordinatesFromSensorSize(double startLatitude,
                                                                                                         double startLongitude,
                                                                                                         double gpsImgDirection,
                                                                                                         double sensorSize,
                                                                                                         double focalLength,
                                                                                                         double distance)
    {
        // Calculate FOV in radians
        double fovRad = 2 * Math.Atan(d: sensorSize / (2 * focalLength));

        // Convert FOV to degrees
        double fovDegrees = fovRad * 180 / Math.PI;

        // Calculate FOV coordinates using the previously defined method
        return CalculateFovCoordinates(startLatitude: startLatitude, startLongitude: startLongitude, gpsImgDirection: gpsImgDirection, fovAngle: fovDegrees, distance: distance);
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    private static double ToDegrees(double radians)
    {
        return radians * 180.0 / Math.PI;
    }

    /// <summary>
    ///     Converts the FOV-output into a string that can be consumed by the map.
    /// </summary>
    /// <param name="sourceList"></param>
    /// <returns></returns>
    internal static string ConvertFOVCoordListsToString(List<(double, double)> sourceList)
    {
        string tmpLat = sourceList[index: 0]
                       .Item1.ToString(provider: CultureInfo.InvariantCulture);
        string tmpLng = sourceList[index: 0]
                       .Item2.ToString(provider: CultureInfo.InvariantCulture);
        return $"{tmpLat}, {tmpLng}";
    }
}