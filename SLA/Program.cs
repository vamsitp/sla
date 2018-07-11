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

        private static readonly int Padding = 16;
        private static readonly int SecondsPerDay = 24 * 60 * 60;
        private static readonly int SecondsPerWeek = SecondsPerDay * 7;
        private static readonly int SecondsPerMonth = SecondsPerDay * 30;
        private static readonly int SecondsPerYear = SecondsPerDay * 365;

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
            output.AppendLine($"| {"SLA".PadRight(10)} | {"DOWNTIME / WEEK".PadRight(Padding)} | {"DOWNTIME / MONTH".PadRight(Padding)} | {"DOWNTIME / YEAR".PadRight(Padding)} |");
            output.AppendLine($"| {Center("-".PadRight(10, '-'))} | {divider} | {divider} | {divider} |");
            foreach (var step in steps.OrderByDescending(x => x))
            {
                var downtimePerYear = (SecondsPerYear * ((100.000M - step) / 100) / SecondsPerDay);
                var downtimePerMonth = (SecondsPerMonth * ((100.000M - step) / 100) / SecondsPerDay);
                var downtimePerWeek = (SecondsPerWeek * ((100.000M - step) / 100) / SecondsPerDay);
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
                downtime = downtime * 24;
                if (Math.Truncate(downtime).Equals(0))
                {
                    downtime = downtime * 60;
                    if (Math.Truncate(downtime).Equals(0))
                    {
                        downtime = downtime * 60;
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
