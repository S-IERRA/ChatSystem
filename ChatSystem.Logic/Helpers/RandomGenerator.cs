namespace ChatSystem.Logic.Helpers;

public static class RandomGenerator
{
    private static readonly Random Random = new Random();
    private const string CharSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    private static int Number(uint low = 0, uint max = 1) => Random.Next((int)low, (int)max);
    private static char Char() => CharSet[Number(0, (uint)CharSet.Length)];
    
    public static string String(uint max) => new string(Enumerable.Repeat(CharSet, (int)max)
        .Select(s => s[Number(0, (uint)s.Length)])
        .ToArray());
}