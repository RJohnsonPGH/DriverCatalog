namespace DriverCatalog.Models;

internal static class ProductBits
{
    public const int TypeShift = 24;
    public const int TypeMask = 0xF << TypeShift;

    public const int TypeUnknown = 0 << TypeShift;
    public const int TypeWinPE = 1 << TypeShift;
    public const int TypeWindows = 2 << TypeShift;
    public const int TypeServer = 3 << TypeShift;

    public const int ProductMask = 0x00FFFFFF;

    public const int ProductUnknown = 0;
    public const int ProductWin10 = 2;
    public const int ProductWin11 = 3;
    public const int ProductXp = 4;
    public const int ProductVista = 5;
    public const int ProductWin7 = 6;
    public const int ProductWin8 = 7;
    public const int ProductWin81 = 8;
    public const int ProductServer19 = 9;
    public const int ProductServer22 = 10;
    public const int ProductServer25 = 11;
    public const int ProductWinPE3 = 12;
    public const int ProductWinPE4 = 13;
    public const int ProductWinPE5 = 14;
}

/// <summary>
/// Windows products that driver packages can target.
/// Values are bit-packed so the product type (WinPE/Windows/Server) and the specific product can be derived from a single value.
/// </summary>
public enum Product
{
    Unknown      = ProductBits.TypeUnknown | ProductBits.ProductUnknown,

    WinPE3       = ProductBits.TypeWinPE   | ProductBits.ProductWinPE3,
    WinPE4       = ProductBits.TypeWinPE   | ProductBits.ProductWinPE4,
    WinPE5       = ProductBits.TypeWinPE   | ProductBits.ProductWinPE5,
    WinPE10      = ProductBits.TypeWinPE   | ProductBits.ProductWin10,
    WinPE11      = ProductBits.TypeWinPE   | ProductBits.ProductWin11,

    Xp           = ProductBits.TypeWindows | ProductBits.ProductXp,
    Vista        = ProductBits.TypeWindows | ProductBits.ProductVista,
    Windows7     = ProductBits.TypeWindows | ProductBits.ProductWin7,
    Windows8     = ProductBits.TypeWindows | ProductBits.ProductWin8,
    Windows81    = ProductBits.TypeWindows | ProductBits.ProductWin81,
    Windows10    = ProductBits.TypeWindows | ProductBits.ProductWin10,
    Windows11    = ProductBits.TypeWindows | ProductBits.ProductWin11,

    Server2019   = ProductBits.TypeServer  | ProductBits.ProductServer19,
    Server2022   = ProductBits.TypeServer  | ProductBits.ProductServer22,
    Server2025   = ProductBits.TypeServer  | ProductBits.ProductServer25,
}

/// <summary>
/// Extension methods for the <see cref="Product"/> enum.
/// </summary>
public static class ProductExtensions
{
    /// <summary>Determines whether the product is a Windows client OS.</summary>
    public static bool IsWindows(this Product product) =>
        (((int)product) & ProductBits.TypeMask) == ProductBits.TypeWindows;

    /// <summary>Determines whether the product is a Windows Server OS.</summary>
    public static bool IsServer(this Product product) =>
        (((int)product) & ProductBits.TypeMask) == ProductBits.TypeServer;

    /// <summary>Determines whether the product is a WinPE variant.</summary>
    public static bool IsWinPE(this Product product) =>
        (((int)product) & ProductBits.TypeMask) == ProductBits.TypeWinPE;

    /// <summary>Determines whether the product is unknown.</summary>
    public static bool IsUnknown(this Product product) =>
        (((int)product) & ProductBits.TypeMask) == ProductBits.TypeUnknown;
}
