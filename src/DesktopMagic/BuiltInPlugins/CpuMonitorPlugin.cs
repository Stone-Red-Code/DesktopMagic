using DesktopMagic.Api;
using DesktopMagic.Api.Settings;

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;

using SettingAttribute = DesktopMagic.Api.Settings.SettingAttribute;

namespace DesktopMagic.BuiltInPlugins;

internal class CpuMonitorPlugin : Plugin
{
    [Setting("usage-circle", "Show pie")]
    private readonly CheckBox usageCircleCheckBox = new CheckBox(false);

    private PerformanceCounter cpuCounter = null!;
    public override int UpdateInterval => 1000;

    public override void Start()
    {
        cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
    }

    public override Bitmap Main()
    {
        float cpuUsage = cpuCounter.NextValue();

        string cpuUsageString = $"{cpuUsage:000}%";

        Font font = new Font(Application.Theme.Font, 200);

        Bitmap bmp = new Bitmap(1, 1);
        bmp.SetResolution(100, 100);
        using Graphics tmpGr = Graphics.FromImage(bmp);
        tmpGr.TextRenderingHint = TextRenderingHint.AntiAlias;

        SizeF size = tmpGr.MeasureString(cpuUsageString, font);

        bmp = new Bitmap((int)size.Width, usageCircleCheckBox.Value ? (int)size.Width : (int)size.Height);
        bmp.SetResolution(100, 100);

        using Graphics gr = Graphics.FromImage(bmp);

        gr.TextRenderingHint = TextRenderingHint.AntiAlias;

        if (usageCircleCheckBox.Value)
        {
            gr.FillPie(new SolidBrush(Application.Theme.SecondaryColor), 0, 0, size.Width, size.Width, 0, 360f / 100f * cpuUsage);

            gr.DrawArc(new Pen(Color.FromArgb(Application.Theme.PrimaryColor.A, Color.Green), 10), 5, 5, size.Width - 10, size.Width - 10, 0, 120);
            gr.DrawArc(new Pen(Color.FromArgb(Application.Theme.PrimaryColor.A, Color.Orange), 10), 5, 5, size.Width - 10, size.Width - 10, 120, 120);
            gr.DrawArc(new Pen(Color.FromArgb(Application.Theme.PrimaryColor.A, Color.Red), 10), 5, 5, size.Width - 10, size.Width - 10, 240, 120);

            gr.DrawString(cpuUsageString, font, new SolidBrush(Application.Theme.PrimaryColor), 0, (size.Width / 2) - (size.Height / 2));
        }
        else
        {
            gr.DrawString(cpuUsageString, font, new SolidBrush(Application.Theme.PrimaryColor), 0, (size.Width / 2) - size.Height);
        }

        return bmp;
    }
}