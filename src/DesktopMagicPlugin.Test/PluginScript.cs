﻿using DesktopMagicPluginAPI;
using DesktopMagicPluginAPI.Inputs;
using DesktopMagicPluginAPI.Settings;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace DesktopMagicPlugin.Test;

public class GifPlugin : Plugin
{
    private const string SaveFilePath = "gifPath.txt";

    [Setting("Gif path:")]
    private readonly TextBox input = new TextBox("");

    [Setting]
    private readonly Label info = new Label("");

    private readonly List<Bitmap> bitmaps = [];

    private int frameCount = -1;

    public override void Start()
    {
        input.OnValueChanged += Input_OnValueChanged;
        if (File.Exists(SaveFilePath))
        {
            input.Value = File.ReadAllText(SaveFilePath);
        }
    }

    public override Bitmap Main()
    {
        if (bitmaps.Count == 0)
        {
            return new Bitmap(1, 1);
        }

        frameCount++;

        if (frameCount >= bitmaps.Count)
        {
            frameCount = 0;
        }

        return bitmaps[frameCount];
    }

    private void Input_OnValueChanged()
    {
        _ = Task.Run(() =>
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
                        _ = gif.SelectActiveFrame(FrameDimension.Time, i);

                        bitmaps.Add(new Bitmap(gif));
                    }
                    File.WriteAllText(SaveFilePath, input.Value);
                    info.Value = string.Empty;
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
        });
    }
}