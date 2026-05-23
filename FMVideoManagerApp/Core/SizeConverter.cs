namespace FMVideoManagerApp.Core
{
    public static class SizeConverter
    {
        private static readonly string[] Units =
        {
        "B", "KB", "MB", "GB", "TB"
    };

        public static string ToHumanReadable(double bytes, int decimals = 2)
        {
            if (bytes < 0)
                return "-" + ToHumanReadable(Math.Abs(bytes), decimals);

            if (bytes == 0)
                return "0 B";

            var unitIndex = 0;
            var size = bytes;

            while (size >= 1024 && unitIndex < Units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{Math.Round(size, decimals)} {Units[unitIndex]}";
        }

        public static double FromKilobytes(double kilobytes) =>
            From(kilobytes, 1);

        public static double FromMegabytes(double megabytes) =>
            From(megabytes, 2);

        public static double FromGigabytes(double gigabytes) =>
            From(gigabytes, 3);

        public static double FromTerabytes(double terabytes) =>
            From(terabytes, 4);

        public static double ToKilobytes(double bytes) =>
            To(bytes, 1);

        public static double ToMegabytes(double bytes) =>
            To(bytes, 2);

        public static double ToGigabytes(double bytes) =>
            To(bytes, 3);

        public static double ToTerabytes(double bytes) =>
            To(bytes, 4);

        private static double To(double value, int power) =>
            value / Math.Pow(1024, power);

        private static double From(double value, int power) =>
            value * Math.Pow(1024, power);
    }
}