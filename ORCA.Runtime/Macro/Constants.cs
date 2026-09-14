namespace ORCA.Runtime.Macro
{
    static class Constants
    {
        // GBAのフレームレートは59.7275、GCのフレームレートは59.94と言われているが、
        // GBPは事情が複雑らしいので、実測したところ、
        // 59.7292254802831 ~ 59.7292255301323
        // というデータが取れたため、これを採用している。
        internal static double FPS = 59.7292255;
    }
}
