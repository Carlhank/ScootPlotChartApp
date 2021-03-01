using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using ScottPlot;
using ScottPlot.Statistics;
using ScottPlot.Statistics.Interpolation;

namespace ChartApp
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var data = GenerateGroupData("A").Concat(GenerateGroupData("B")).ToList();

            // var boxplotBase64 = GetChartBase64(GetBoxplot(data, "A").GetBitmap());
            var trendChartBase64 = GetChartBase64(GetTrendChart(data, "A", "Time").GetBitmap());
            Console.WriteLine(trendChartBase64);
        }

        private static Plot GetTrendChart(IEnumerable<PlotData> data, string referenceFactor, string timeFactor)
        {
            var plot = new Plot();
            var orderedGroupingData = data
                .OrderBy(d => d.Time)
                .GroupBy(bd => bd.Group);
            
            foreach (var grouping in orderedGroupingData)
            {
                var (xs, ys) = grouping.Aggregate((xs: new List<double>(), ys: new List<double>()), (seed, group) =>
                {
                    if (@group.Value.HasValue)
                    {
                        seed.ys.Add(group.Value.Value);
                        seed.xs.Add(group.Time.Value.ToOADate());
                    }

                    return seed;
                }, tuple => (tuple.xs.ToArray(), tuple.ys.ToArray()));

                var naturalSpline = new NaturalSpline(xs, ys, 20);
                
                var randomColor = DataGen.RandomColor(new Random());
                plot.PlotScatter(xs, ys, randomColor, markerSize: 10, lineWidth: 0);
                plot.PlotScatter(naturalSpline.interpolatedXs, naturalSpline.interpolatedYs, randomColor, markerSize: 3, label: grouping.Key);
            }

            return plot;
        }
        

        private static Plot GetBoxplot(IEnumerable<PlotData> data, string referenceFactor)
        {
            var plot = new Plot(200, 100);
            var orderedGroupingData = data
                .ToLookup(bd => bd.Group)
                .OrderBy(x => x.Key == referenceFactor ? -1 : 1)
                .ToList();

            var populationMultiSeries = new PopulationMultiSeries(orderedGroupingData.Select(grouping =>
            {
                var plottingData = grouping.Where(g => g.Value.HasValue).Select(g => g.Value.Value).ToArray();
                return new PopulationSeries(new Population[] {new Population(plottingData)}, grouping.Key);
            }).ToArray());
            
            var populationChart = plot.PlotPopulations(populationMultiSeries);
            populationChart.displayDistributionCurve = false;
            populationChart.displayItems = PlottablePopulations.DisplayItems.BoxOnly;
            populationChart.boxStyle = PlottablePopulations.BoxStyle.BoxMedianQuartileOutlier;

            plot.Frame(false);
            plot.Ticks(false);
            return plot;
        }

        private static List<PlotData> GenerateGroupData(string group)
        {
            return DataGen.Random(new Random(), 30)
                .Select(value => new PlotData
                {
                    Group = group,
                    Value = value,
                    Time = DateTime.Now.AddDays(-value),
                }).ToList();
        }

        private static string GetChartBase64(Image bitmap)
        {
            var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            return Convert.ToBase64String(ms.ToArray());
        }
    }

    public class PlotData
    {
        public string Group { get; set; }
        public double? Value { get; set; }
        public DateTime? Time { get; set; }
    }
}