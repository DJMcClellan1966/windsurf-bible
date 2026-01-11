using AI_Bible_App.Core.Models;
using System.Globalization;

namespace AI_Bible_App.Maui.Converters;

#region Speech Converters

/// <summary>
/// Converts IsListening bool to microphone icon.
/// </summary>
public class BoolToMicIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isListening)
        {
            return isListening ? "⏹" : "🎤";
        }
        return "🎤";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts IsListening bool to button color.
/// </summary>
public class BoolToMicColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isListening)
        {
            return isListening ? Colors.Red : Color.FromArgb("#512BD4");
        }
        return Color.FromArgb("#512BD4");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts IsListening bool to placeholder text.
/// </summary>
public class BoolToPlaceholderConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isListening)
        {
            return isListening ? "Listening... tap 🎤 to stop" : "Type or tap 🎤 to speak...";
        }
        return "Type or tap 🎤 to speak...";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

#endregion

#region Reflection Converters

/// <summary>
/// Converts ReflectionType enum to emoji.
/// </summary>
public class ReflectionTypeToEmojiConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ReflectionType type)
        {
            return type switch
            {
                ReflectionType.Chat => "💬",
                ReflectionType.Prayer => "🙏",
                ReflectionType.BibleVerse => "📖",
                ReflectionType.Custom => "✏️",
                _ => "📝"
            };
        }
        return "📝";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to star emoji for favorites.
/// </summary>
public class BoolToStarConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isFavorite)
        {
            return isFavorite ? "⭐" : "☆";
        }
        return "☆";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

#endregion

#region Rating Converters

/// <summary>
/// Converts a rating value to opacity for visual feedback on rating buttons.
/// Returns 1.0 if the rating matches the parameter (selected), 0.4 if not (unselected).
/// </summary>
public class RatingOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int rating && parameter is string paramStr && int.TryParse(paramStr, out int targetRating))
        {
            return rating == targetRating ? 1.0 : 0.4;
        }
        return 0.4;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a ChatMessage and rating parameter to a tuple for the RateMessage command.
/// </summary>
public class MessageToRatingTupleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ChatMessage message && parameter is string paramStr && int.TryParse(paramStr, out int rating))
        {
            return (message, rating);
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

#endregion
