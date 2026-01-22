using System;
using System.Globalization;
using Avalonia.Data.Converters;
using DMINLauncher.Enums;

namespace DMINLauncher.Converters;

public class DifficultyDisplayConverter : IValueConverter
{
    public static readonly DifficultyDisplayConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.VeryEasy => "Very Easy",
                Difficulty.Easy => "Easy",
                Difficulty.Normal => "Normal",
                Difficulty.Hard => "Hard",
                Difficulty.Nightmare => "Nightmare",
                _ => difficulty.ToString()
            };
        }
        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class GameTypeDisplayConverter : IValueConverter
{
    public static readonly GameTypeDisplayConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is GameType gameType)
        {
            return gameType switch
            {
                GameType.SinglePlayer => "Single Player",
                GameType.Cooperative => "Cooperative",
                GameType.Deathmatch => "Deathmatch",
                _ => gameType.ToString()
            };
        }
        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class NetworkModeDisplayConverter : IValueConverter
{
    public static readonly NetworkModeDisplayConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is NetworkMode mode)
        {
            return mode switch
            {
                NetworkMode.None => "None",
                NetworkMode.HostLAN => "Host LAN",
                NetworkMode.HostInternet => "Host Internet",
                NetworkMode.Connect => "Connect",
                _ => mode.ToString()
            };
        }
        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class HexenClassDisplayConverter : IValueConverter
{
    public static readonly HexenClassDisplayConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is HexenClass hexenClass)
        {
            return hexenClass switch
            {
                HexenClass.Fighter => "Fighter - Baratus (melee strength)",
                HexenClass.Cleric => "Cleric - Parias (balanced support)",
                HexenClass.Mage => "Mage - Daedolon (ranged magic)",
                HexenClass.Random => "Random",
                _ => hexenClass.ToString()
            };
        }
        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
