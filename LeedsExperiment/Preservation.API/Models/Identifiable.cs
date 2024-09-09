using System.Text;

namespace Preservation.API.Models;

// https://github.com/tomcrane/id-minter/blob/main/IdMinter/IdMinter/Identifiable.cs
public class Identifiable
{
    private static readonly char[] Numbers = "23456789".ToCharArray();                   // not 1
    private static readonly char[] Letters = "abcdefghjkmnpqrstuvwxyz".ToCharArray();    // not i, l, o
    private static readonly char[] All = [.. Numbers, .. Letters];

    public static string Generate(int length = 8, bool letterFirst = true)
    {
        Random random = new Random();
        var sb = new StringBuilder(length);
        for(int i = 0; i < length; i++)
        {
            if(letterFirst && i==0)
            {
                sb.Append(Letters[random.Next(Letters.Length)]);
            }
            else
            {
                sb.Append(All[random.Next(All.Length)]);
            }
        }
        return sb.ToString();
    }
}