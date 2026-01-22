using DesktopMagic.Api;
using DesktopMagic.Api.Settings;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopMagic.PluginTest;

public class GifPlugin : Plugin
{
    [Setting("gif-path", "GIF path")]
    private readonly FileSelector input = new FileSelector();

    [Setting("info")]
    private readonly Label info = new Label("Select a GIF file to load.");

    private readonly List<Bitmap> bitmaps = [];

    private readonly CancellationTokenSource cancellationTokenSource;
    private int frameCount = -1;

    public GifPlugin()
    {
        cancellationTokenSource = new CancellationTokenSource();
    }

    public override async void Start()
    {
        input.OnValueChanged += Input_OnValueChanged;

        await LoadGif();
    }

    public override void Stop()
    {
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();

        bitmaps.Clear();
    }

    public override Bitmap Main()
    {
        if (bitmaps.Count == 0)
        {
            return null;
        }

        frameCount++;

        if (frameCount >= bitmaps.Count)
        {
            frameCount = 0;
        }

        return bitmaps[frameCount];
    }

    private async void Input_OnValueChanged()
    {
        await LoadGif();
    }

    private Task LoadGif()
    {
        return Task.Run(() =>
        {
            try
            {
                if (File.Exists(input.Value))
                {
                    info.Value = "Loading...";
                    Image gif = Image.FromFile(input.Value);

                    PropertyItem item = gif.GetPropertyItem(0x5100); // FrameDelay in libgdiplus

                    UpdateInterval = (item.Value[0] + (item.Value[1] * 256)) * 10; //FrameDelay in ms
                    bitmaps.Clear();
                    for (int i = 0; i < gif.GetFrameCount(FrameDimension.Time); i++)
                    {
                        cancellationTokenSource.Token.ThrowIfCancellationRequested();

                        _ = gif.SelectActiveFrame(FrameDimension.Time, i);

                        bitmaps.Add(new Bitmap(gif));

                        info.Value = $"Loading frame {i + 1} of {gif.GetFrameCount(FrameDimension.Time)}";
                    }

                    info.Value = $"Loaded {bitmaps.Count} frames.";
                }
                else
                {
                    info.Value = "File not found!";
                }
            }
            catch (Exception ex)
            {
                info.Value = $"Error: {ex.Message}";
            }
        }, cancellationTokenSource.Token);
    }
}