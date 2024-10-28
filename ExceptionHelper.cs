using System;

namespace DirectXEngine
{
    public static class ExceptionHelper
    {
        public static void ThrowByCondition<T>(T argument, string message, Func<T, bool> throwCondition)
        {
            if (throwCondition.Invoke(argument))
                throw new Exception(message);
        }

        public static void ThrowByCondition<T>(T argument, Func<T, bool> throwCondition) => 
            ThrowByCondition(argument, "", throwCondition);

        public static void ThrowByCondition(bool throwCondition, string message)
        {
            if (throwCondition)
                throw new Exception(message);
        }

        public static void ThrowByCondition(bool throwCondition) => ThrowByCondition(throwCondition, "");

        public static void ThrowIfNull(object argument, string message)
        {
            if (argument == null)
                throw new ArgumentNullException(nameof(argument), message);
        }

        public static void ThrowIfNull(object argument) => 
            ThrowIfNull(argument, "");

        public static void ThrowIfOutOfRange(double argument, string message, double minValue, double maxValue)
        {
            if (argument < minValue || argument > maxValue)
                throw new ArgumentOutOfRangeException(nameof(argument), message);
        }

        public static void ThrowIfOutOfRange(double argument, double minValue, double maxValue) => 
            ThrowIfOutOfRange(argument, "", minValue, maxValue);

        public static void ThrowIfOutOfRange01(double argument, string message) => 
            ThrowIfOutOfRange(argument, message, 0, 1);

        public static void ThrowIfOutOfRange01(double argument) => ThrowIfOutOfRange01(argument, "");
    }

}
