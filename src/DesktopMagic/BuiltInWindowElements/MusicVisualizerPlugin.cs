using DesktopMagicPluginAPI;

using NAudio.Wave;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace DesktopMagic.BuiltInWindowElements
{
    internal class MusicVisualizerPlugin : Plugin
    {
        private IWaveIn waveIn;
        private const int fftLength = 1024; // NAudio fft wants powers of two!
        private readonly SampleAggregator sampleAggregator = new SampleAggregator(fftLength);
        private volatile bool calculate = true;
        private Bitmap output = new Bitmap(880, 300);

        public override int UpdateInterval => 0;

        public override void Start()
        {
            sampleAggregator.FftCalculated += FftCalculated;
            sampleAggregator.PerformFFT = true;

            waveIn = new WasapiLoopbackCapture();

            waveIn.DataAvailable += OnDataAvailable;
            waveIn.StartRecording();
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (calculate)
            {
                byte[] buffer = e.Buffer;
                int bytesRecorded = e.BytesRecorded;
                int bufferIncrement = waveIn.WaveFormat.BlockAlign;

                for (int index = 0; index < bytesRecorded; index += bufferIncrement)
                {
                    float sample32 = BitConverter.ToSingle(buffer, index);
                    sampleAggregator.Add(sample32);
                }
            }
        }

        private List<double> lastFft = new List<double>();

        private void FftCalculated(object sender, FftEventArgs e)
        {
            if (!calculate)
            {
                return;
            }

            List<double> fft = new List<double>();
            for (int i = 0; i < (e.Result.Length / 2) - 70; i++)
            {
                int v = i;
                if (v < 20)
                {
                    v = 20;
                }

                int multiplier = 100 - (MainWindow.AmplifierLevel * 2);
                multiplier = multiplier == 0 ? 1 : multiplier;
                fft.Add(Math.Abs(e.Result[i].Y * v / multiplier));
            }

            if (lastFft.Count != 0)
            {
                for (int i = 0; i < fft.Count; i++)
                {
                    if (fft[i] > lastFft[i])
                    {
                        fft[i] = (fft[i] + lastFft[i]) / 2.05;
                    }
                    else
                    {
                        fft[i] = lastFft[i] - 0.0005;
                    }
                }
            }

            int barCount = 0;
            List<double> scaledFft = new List<double>();

            if (barCount > 0)
            {
                int count = fft.Count / barCount;
                Console.WriteLine(count);
                for (int i = 0; i < barCount; i++)
                {
                    double temp = 0;
                    int j;
                    for (j = i * count; j < count + (i * count); j++)
                    {
                        temp += fft[j];
                    }

                    scaledFft.Add(temp);
                }
            }
            else
            {
                scaledFft.AddRange(fft);
            }

            #region flatten

            int flattenValue = 4;

            for (int i = 0; i < scaledFft.Count; i++)
            {
                if (flattenValue > 0)
                {
                    double temp = 0;
                    for (int j = 0; j < flattenValue; j++)
                    {
                        if (i + j < scaledFft.Count)
                        {
                            temp += scaledFft[i + j];
                        }
                    }
                    scaledFft[i] = temp / flattenValue;
                }
            }

            for (int i = 0; i < scaledFft.Count; i++)
            {
                if (flattenValue > 0)
                {
                    double temp = 0;
                    for (int j = 0; j < flattenValue; j++)
                    {
                        if (i - j >= 0)
                        {
                            temp += scaledFft[i - j];
                        }
                    }
                    scaledFft[i] = temp / flattenValue;
                }
            }

            #endregion flatten

            lastFft = fft;
            try
            {
                int offset = 0;

                if (!MainWindow.MirrorMode && MainWindow.SpectrumMode != 1)
                {
                    scaledFft.Insert(0, 0);
                }

                if (!MainWindow.LineMode)
                {
                    offset = 1;
                }

                using (Graphics gr = Graphics.FromImage(output))
                {
                    gr.Clear(Color.Transparent);
                    PointF[] points = new PointF[scaledFft.Count + 2];

                    int fftIndex = 0;
                    bool fftIndexReverse = false;

                    if (MainWindow.MirrorMode || MainWindow.SpectrumMode == 1)
                    {
                        fftIndex = scaledFft.Count - 1;
                    }
                    for (int pointIndex = 0; pointIndex < scaledFft.Count; pointIndex += 1)
                    {
                        int value = (int)Math.Max(scaledFft[fftIndex] * 50000, 0);

                        switch (MainWindow.SpectrumMode)
                        {
                            case 1:

                                if (!fftIndexReverse)
                                {
                                    points[pointIndex + 1] = new PointF(output.Width - (4 * pointIndex), (output.Height / 2) + value);
                                    points[(points.Length / 2) + pointIndex + 1] = new PointF(output.Width - (4 * pointIndex), (output.Height / 2) - value);
                                }
                                break;

                            case 2:
                                points[pointIndex + 1] = new PointF(2 * pointIndex, value);
                                break;

                            default:
                                points[pointIndex + 1] = new PointF(2 * pointIndex, output.Height - value - 1 + offset);
                                break;
                        }

                        if (MainWindow.MirrorMode || MainWindow.SpectrumMode == 1)
                        {
                            if (!fftIndexReverse)
                            {
                                fftIndex -= 2;
                            }
                            if (fftIndex <= 0 || fftIndexReverse)
                            {
                                fftIndexReverse = true;
                                fftIndex += 2;
                            }
                        }
                        else
                        {
                            fftIndex++;
                        }
                    }

                    SetPoints(points, output.Width, output.Height, offset);

                    Brush brush = MainWindow.MusicVisualzerColor.HasValue
                        ? new SolidBrush(MainWindow.MusicVisualzerColor.Value)
                        : new SolidBrush(MainWindow.Theme.PrimaryColor);

                    if (MainWindow.LineMode)
                    {
                        if (MainWindow.SpectrumMode == 1)
                        {
                            gr.DrawLines(new Pen(brush), points.Take(points.Length / 2).ToArray());
                            gr.DrawLines(new Pen(brush), points.Skip(points.Length / 2).ToArray());
                        }
                        else
                        {
                            gr.DrawLines(new Pen(brush), points);
                        }
                    }
                    else
                    {
                        gr.FillPolygon(brush, points);
                    }
                }

                Application.UpdateWindow();
            }
            catch { }
        }

        private void SetPoints(PointF[] points, int width, int height, int offset)
        {
            switch (MainWindow.SpectrumMode)
            {
                case 1:
                    points[0] = new PointF(width, height / 2);
                    points[^1] = new PointF(0, height / 2);

                    points[points.Length / 2] = new PointF(width, height / 2);
                    points[(points.Length / 2) - 1] = new PointF(0, height / 2);
                    break;

                case 2:
                    points[0] = new PointF(0, 0 - offset);
                    points[^1] = new PointF(points[^2].X, 0 - offset);
                    break;

                default:
                    points[0] = new PointF(0, height);
                    points[^1] = new PointF(points[^2].X, height);
                    break;
            }
        }

        public override Bitmap Main()
        {
            return output;
        }

        public override void Stop()
        {
            calculate = false;
            waveIn?.StopRecording();
        }
    }
}