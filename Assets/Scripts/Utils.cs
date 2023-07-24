public static class Utils
{
    public static void Swap<T>(ref T a, ref T b)
    {
        (a, b) = (b, a);
    }
}