﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace DesktopMagic.Api.Drawing;

internal class FontComparer : IEqualityComparer<Font>
{
    private const float Tolerance = 0.01f;

    public bool Equals(Font? font1, Font? font2)
    {
        if (font1 is null || font2 is null)
        {
            return false;
        }

        if (font1.Name != font2.Name)
        {
            return false;
        }

        if (Math.Abs(font1.SizeInPoints - font2.SizeInPoints) > Tolerance)
        {
            return false;
        }

        if (font1.Style != font2.Style)
        {
            return false;
        }

        return true;
    }

    public int GetHashCode([DisallowNull] Font obj)
    {
        return obj.GetHashCode();
    }
}