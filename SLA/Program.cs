namespace SLA
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Text;

    class Program
    {
        private const string Days = "days";
        private const string Seconds = "seconds";
        private const string Minutes = "minutes";
        private const string Hours = "hours";
        private const string Format = "0.00";

        private static readonly decimal StepStart = decimal.Parse(ConfigurationManager.AppSettings["StepStart"]);
        private static readonly decimal StepEnd = decimal.Parse(ConfigurationManager.AppSettings["StepEnd"]);

        private const int Padding = 16;
        private const int Cent = 100;

        private const int HoursPerDay = 24;
        private const int MinutesPerHour = 60;
        private const int SecondsPerMinute = 60;
        private static readonly int SecondsPerDay = HoursPerDay * MinutesPerHour * SecondsPerMinute;
        private static readonly int SecondsPerWeek = SecondsPerDay * 7; // Days per Week
        private static readonly int SecondsPerMonth = SecondsPerDay * 30; // Days per Month
        private static readonly int SecondsPerYear = SecondsPerDay * 365; // Dats per Year

        private static readonly decimal[] Increments = new decimal[] { 0.000M, 0.500M, 0.900M, 0.950M, 0.990M, 0.999M };

        static void Main(string[] args)
        {
            var steps = new List<decimal>();
            for (var step = StepStart; step < StepEnd; step++)
            {
                foreach (var increment in Increments)
                {
                    steps.Add(step + increment);
                }
            }

            var divider = Center("-".PadRight(Padding, '-'));
            var output = new StringBuilder();
            output.AppendLine("# AVAILABILITY SLAS");
            output.AppendLine();
            output.AppendLine($"| {"SLA".PadRight(10)} | {"DOWNTIME / WEEK".PadRight(Padding)} | {"DOWNTIME / MONTH".PadRight(Padding)} | {"DOWNTIME / YEAR".PadRight(Padding)} |");
            output.AppendLine($"| {Center("-".PadRight(10, '-'))} | {divider} | {divider} | {divider} |");
            foreach (var step in steps.OrderByDescending(x => x))
            {
                var downtimePerYear = (SecondsPerYear * ((100.000M - step) / Cent) / SecondsPerDay);
                var downtimePerMonth = (SecondsPerMonth * ((100.000M - step) / Cent) / SecondsPerDay);
                var downtimePerWeek = (SecondsPerWeek * ((100.000M - step) / Cent) / SecondsPerDay);
                output.AppendLine($"| {step}{" %".PadRight(4)} | {GetDowntime(downtimePerWeek).PadRight(Padding)} | {GetDowntime(downtimePerMonth).PadRight(Padding)} | {GetDowntime(downtimePerYear).PadRight(Padding)} |");
            }

            var result = output.ToString();
            Console.WriteLine(result);
            File.WriteAllText("./Readme.md", result);
            Console.ReadLine();
        }

        private static string GetDowntime(decimal downtime)
        {
            var units = Days;
            if (Math.Truncate(downtime).Equals(0))
            {
                downtime = downtime * HoursPerDay;
                if (Math.Truncate(downtime).Equals(0))
                {
                    downtime = downtime * MinutesPerHour;
                    if (Math.Truncate(downtime).Equals(0))
                    {
                        downtime = downtime * SecondsPerMinute;
                        units = Seconds;
                    }
                    else
                    {
                        units = Minutes;
                    }
                }
                else
                {
                    units = Hours;
                }
            }

            return $"{downtime.ToString(Format)} {units}";
        }

        private static string Center(string divider)
        {
            return ":" + divider.Substring(1, divider.Length - 2) + ":";
        }
    }
}
