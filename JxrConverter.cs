using System;

public static class JxrConverter
{
    public static byte[] Process(float[] rgba, int w, int h, bool tonemap)
    {
        int n = w * h;
        byte[] outBuf = new byte[n * 3];
        int o = 0;
        for (int i = 0; i < n; i++)
        {
            int b = i * 4;
            for (int c = 0; c < 3; c++)
            {
                double v = rgba[b + c];
                if (v < 0) v = 0;
                if (tonemap)
                {
                    v = v / (1.0 + v * 0.35);
                    if (v > 1.0) v = 1.0;
                }
                else
                {
                    if (v > 1.0) v = 1.0;
                }
                if (v <= 0.0031308) v = 12.92 * v;
                else v = 1.055 * Math.Pow(v, 1.0 / 2.4) - 0.055;
                outBuf[o++] = (byte)Math.Round(v * 255.0);
            }
        }
        return outBuf;
    }
}
