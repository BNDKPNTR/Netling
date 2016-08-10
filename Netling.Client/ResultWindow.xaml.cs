using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Netling.Core.Models;
using OxyPlot;
using System.Windows.Controls;
using Netling.Client.Properties;

namespace Netling.Client
{
    public partial class ResultWindow
    {
        private const string IsColorBlindFriendly = "IsColorBlindFriendly";
        private ResultWindowItem _resultWindowItem;
        private readonly MainWindow _sender;

        public ResultWindow(MainWindow sender)
        {
            _sender = sender;
            InitializeComponent();
            colorBlindnessCB.IsChecked = (bool)Settings.Default[IsColorBlindFriendly];
        }

        public async Task Load(WorkerResult workerResult)
        {
            var taskResult = await GenerateAsync(workerResult);
            _resultWindowItem = taskResult.ResultWindowItem;

            RequestsPerSecondGraph.Draw(taskResult.Throughput, "{Y:#,0} rps");
            var dataPoints = workerResult.Histogram.Select((count, i) => new DataPoint(i / 80.0 * (_resultWindowItem.Max - _resultWindowItem.Min) + _resultWindowItem.Min, count)).ToList();
            HistogramGraph.Draw(dataPoints, "{X:0.000} ms");

            Title = "Netling - " + _resultWindowItem.Url;
            ThreadsValueUserControl.Value = _resultWindowItem.Threads.ToString();
            PipeliningValueUserControl.Value = _resultWindowItem.Pipelining.ToString();
            ThreadAffinityValueUserControl.Value = _resultWindowItem.ThreadAffinity ? "ON" : "OFF";

            RequestsValueUserControl.Value = _resultWindowItem.JobsPerSecond.ToString("#,0");
            ElapsedValueUserControl.Value = $"{_resultWindowItem.ElapsedSeconds:0}";
            BandwidthValueUserControl.Value = _resultWindowItem.Bandwidth.ToString("#,0");
            ErrorsValueUserControl.Value = _resultWindowItem.Errors.ToString("#,0");
            MedianValueUserControl.Value = string.Format(_resultWindowItem.Median > 5 ? "{0:#,0}" : "{0:0.000}", _resultWindowItem.Median);
            StdDevValueUserControl.Value = string.Format(_resultWindowItem.StdDev > 5 ? "{0:#,0}" : "{0:0.000}", _resultWindowItem.StdDev);
            MinValueUserControl.Value = string.Format(_resultWindowItem.Min > 5 ? "{0:#,0}" : "{0:0.000}", _resultWindowItem.Min);
            MaxValueUserControl.Value = string.Format(_resultWindowItem.Max > 5 ? "{0:#,0}" : "{0:0.000}", _resultWindowItem.Max);
            MinTextBlock.Text = MinValueUserControl.Value + " ms";
            MaxTextBlock.Text = MaxValueUserControl.Value + " ms";

            var errors = new Dictionary<string, string>();

            foreach (var statusCode in workerResult.StatusCodes)
            {
                errors.Add(statusCode.Key.ToString(), statusCode.Value.ToString("#,0"));
            }

            foreach (var exception in workerResult.Exceptions)
            {
                errors.Add(exception.Key.ToString(), exception.Value.ToString("#,0"));
            }

            ErrorsListView.ItemsSource = errors;

            if (_sender.ResultWindowItem != null)
                LoadBaseline(_sender.ResultWindowItem);
        }

        private Task<JobTaskResult> GenerateAsync(WorkerResult workerResult)
        {
            return Task.Run(() =>
            {
                var result = ResultWindowItem.Parse(workerResult);
                var max = (int)Math.Floor(workerResult.Elapsed.TotalMilliseconds / 1000);

                var throughput = workerResult.Seconds
                    .Where(r => r.Key < max && r.Value.Count > 0)
                    .OrderBy(r => r.Key)
                    .Select(r => new DataPoint(r.Key, r.Value.Count));

                return new JobTaskResult
                {
                    ResultWindowItem = result,
                    Throughput = throughput
                };
            });
        }

        private void UseBaseline(object sender, RoutedEventArgs e)
        {
            _sender.ResultWindowItem = _resultWindowItem;
            LoadBaseline(_sender.ResultWindowItem);
        }

        private void ClearBaseline(object sender, RoutedEventArgs e)
        {
            _sender.ResultWindowItem = null;
            ThreadsValueUserControl.BaselineValue = null;
            PipeliningValueUserControl.BaselineValue = null;
            ThreadAffinityValueUserControl.BaselineValue = null;

            RequestsValueUserControl.BaselineValue = null;
            RequestsValueUserControl.ValueBrush = null;

            ElapsedValueUserControl.BaselineValue = null;
            ElapsedValueUserControl.ValueBrush = null;

            BandwidthValueUserControl.BaselineValue = null;
            BandwidthValueUserControl.ValueBrush = null;

            ErrorsValueUserControl.BaselineValue = null;
            ErrorsValueUserControl.ValueBrush = null;

            MedianValueUserControl.BaselineValue = null;
            MedianValueUserControl.ValueBrush = null;

            StdDevValueUserControl.BaselineValue = null;
            StdDevValueUserControl.ValueBrush = null;

            MinValueUserControl.BaselineValue = null;
            MinValueUserControl.ValueBrush = null;

            MaxValueUserControl.BaselineValue = null;
            MaxValueUserControl.ValueBrush = null;
        }

        private void LoadBaseline(ResultWindowItem baseline)
        {
            ThreadsValueUserControl.BaselineValue = baseline.Threads.ToString();
            PipeliningValueUserControl.BaselineValue = baseline.Pipelining.ToString();
            ThreadAffinityValueUserControl.BaselineValue = baseline.ThreadAffinity ? "ON" : "OFF";

            RequestsValueUserControl.BaselineValue = $"{baseline.JobsPerSecond:#,0}";

            ElapsedValueUserControl.BaselineValue = $"{baseline.ElapsedSeconds:#,0}";

            BandwidthValueUserControl.BaselineValue = $"{baseline.Bandwidth:0}";

            ErrorsValueUserControl.BaselineValue = baseline.Errors.ToString();

            MedianValueUserControl.BaselineValue = string.Format(baseline.Median > 5 ? "{0:#,0}" : "{0:0.000}", baseline.Median);

            StdDevValueUserControl.BaselineValue = string.Format(baseline.StdDev > 5 ? "{0:#,0}" : "{0:0.000}", baseline.StdDev);

            MinValueUserControl.BaselineValue = string.Format(baseline.Min > 5 ? "{0:#,0}" : "{0:0.000}", baseline.Min);

            MaxValueUserControl.BaselineValue = string.Format(baseline.Max > 5 ? "{0:#,0}" : "{0:0.000}", baseline.Max);

            LoadValueUserControlBrushes(baseline);
        }

        private void LoadValueUserControlBrushes(ResultWindowItem baseline)
        {
            RequestsValueUserControl.ValueBrush = GetValueBrush(_resultWindowItem.JobsPerSecond, baseline.JobsPerSecond);
            BandwidthValueUserControl.ValueBrush = GetValueBrush(_resultWindowItem.Bandwidth, baseline.Bandwidth);
            ErrorsValueUserControl.ValueBrush = GetValueBrush(baseline.Errors, _resultWindowItem.Errors);
            MedianValueUserControl.ValueBrush = GetValueBrush(baseline.Median, _resultWindowItem.Median);
            StdDevValueUserControl.ValueBrush = GetValueBrush(baseline.StdDev, _resultWindowItem.StdDev);
            MinValueUserControl.ValueBrush = GetValueBrush(baseline.Min, _resultWindowItem.Min);
            MaxValueUserControl.ValueBrush = GetValueBrush(baseline.Max, _resultWindowItem.Max);
        }

        private Brush GetValueBrush(double v1, double v2)
        {
            if (Math.Abs(v1 - v2) < 0.001)
                return null;

            var betterValueBrush = (bool)Settings.Default[IsColorBlindFriendly] ?
                new SolidColorBrush(Color.FromRgb(0, 121, 197)) : new SolidColorBrush(Colors.Green);

            return v1 > v2 ? betterValueBrush : new SolidColorBrush(Colors.Red);
        }

        private void colorBlindnessCB_Click(object sender, RoutedEventArgs e)
        {
            var isChecked = (sender as CheckBox).IsChecked ?? false;

            if (isChecked)
            {
                Settings.Default[IsColorBlindFriendly] = true;
            }
            else
            {
                Settings.Default[IsColorBlindFriendly] = false;
            }

            Settings.Default.Save();

            if (_sender.ResultWindowItem != null)
            {
                LoadValueUserControlBrushes(_sender.ResultWindowItem);
            }
        }
    }

    internal class JobTaskResult
    {
        public ResultWindowItem ResultWindowItem { get; set; }
        public IEnumerable<DataPoint> Throughput { get; set; }
    }
}
