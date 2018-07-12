namespace SLA
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;

    class Program
    {
        private const string Days = "days";
        private const string Seconds = "secs";
        private const string Minutes = "mins";
        private const string Hours = "hrs";
        private const string Format = "{0,5:##.00}";
        private const string Colon = ":";

        private static readonly decimal StepStart = decimal.Parse(ConfigurationManager.AppSettings["StepStart"]);
        private static readonly decimal StepEnd = decimal.Parse(ConfigurationManager.AppSettings["StepEnd"]);
        private static readonly string[] DowntimeExpectations = ConfigurationManager.AppSettings["DowntimeExpectations"]?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        private const int Padding = 16;
        private const int Cent = 100;

        private const int DaysPerWeek = 7;
        private const int DaysPerMonth = 30;
        private const int DaysPerYear = 365;

        private const int HoursPerDay = 24;
        private const int MinutesPerHour = 60;
        private const int SecondsPerMinute = 60;

        private static readonly int SecondsPerDay = HoursPerDay * MinutesPerHour * SecondsPerMinute;
        private static readonly int SecondsPerWeek = SecondsPerDay * DaysPerWeek;
        private static readonly int SecondsPerMonth = SecondsPerDay * DaysPerMonth;
        private static readonly int SecondsPerYear = SecondsPerDay * DaysPerYear;

        private static readonly IEnumerable<decimal> Intervals = ConfigurationManager.AppSettings["Intervals"].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(decimal.Parse);
        private static readonly Dictionary<string, double> SecondsPerUnit = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            { "D", SecondsPerDay },
            { "W", SecondsPerWeek },
            { "M", SecondsPerMonth },
            { "Y", SecondsPerYear }
        };

        static void Main(string[] args)
        {
            var output = new StringBuilder();
            if (DowntimeExpectations?.Length > 0)
            {
                var divide = Center("-".PadRight(Padding, '-'));
                output.AppendLine("## CALCULATED SLAS:");
                output.AppendLine();
                output.AppendLine($"| {"EXPECTED DOWNTIME".PadRight(20)} | {"UNIT".PadRight(Padding)} | {"SLA".PadRight(Padding)} |");
                output.AppendLine($"| {Center("-".PadRight(20, '-'))} | {divide} | {divide} |");
                foreach (var item in DowntimeExpectations)
                {
                    var downtimeExpectation = item.Split('/');
                    var expectedDowntimeDuration = XmlConvert.ToTimeSpan(downtimeExpectation.FirstOrDefault()); // ?.Remove(DowntimeExpectation.FirstOrDefault().Length - 1));
                    var expectedDowntimeInterval = downtimeExpectation.LastOrDefault();

                    var sla = GetSla(expectedDowntimeDuration, expectedDowntimeInterval);
                    output.AppendLine($"| {expectedDowntimeDuration.ToString().PadRight(20)} | {expectedDowntimeInterval.PadRight(Padding)} | {sla.PadRight(Padding)} |");
                }

                output.AppendLine(Environment.NewLine);
            }

            var steps = new List<decimal>();
            for (var step = StepStart; step < StepEnd; step++)
            {
                foreach (var increment in Intervals)
                {
                    steps.Add(step + increment);
                }
            }

            var divider = Center("-".PadRight(Padding, '-'));
            output.AppendLine("## [AVAILABILITY SLAS](https://docs.microsoft.com/en-us/azure/architecture/resiliency/index#slas)");
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
            File.WriteAllText(ConfigurationManager.AppSettings["Output"], result);
            Console.WriteLine("Done!");
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

            return $"{string.Format(Format, downtime)} {units}";
        }

        private static string GetSla(TimeSpan duration, string unit)
        {
            var durationInSec = duration.TotalSeconds;
            var sla = (durationInSec / SecondsPerUnit[unit]) * 100;
            return $"{(100 - sla).ToString("00.000")} %";
        }

        private static string Center(string divider)
        {
            return Colon + divider.Substring(1, divider.Length - 2) + Colon;
        }
    }
}
