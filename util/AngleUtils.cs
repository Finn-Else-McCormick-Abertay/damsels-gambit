using System;

namespace DamselsGambit.Util;

static class Angle
{
    public static double ToRadians(double degrees) => degrees * (Math.PI / 180d);
    public static double ToDegrees(double radians) => radians * (180d / Math.PI);

    public static float ToRadians(float degrees) => (float)ToRadians((double)degrees);
    public static float ToDegrees(float radians) => (float)ToDegrees((double)radians);
}