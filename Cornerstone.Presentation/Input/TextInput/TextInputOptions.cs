using System.Collections.Generic;

namespace Cornerstone.Presentation.Input.TextInput;

public class TextInputOptions
{
    public static TextInputOptions FromStyledElement(StyledElement cornerstoneObject)
    {
        var result = new TextInputOptions
        {
            ContentType = GetContentType(cornerstoneObject),
            ReturnKeyType = GetReturnKeyType(cornerstoneObject),
            Multiline = GetMultiline(cornerstoneObject),
            AutoCapitalization = GetAutoCapitalization(cornerstoneObject),
            IsSensitive = GetIsSensitive(cornerstoneObject),
            Lowercase = GetLowercase(cornerstoneObject),
            Uppercase = GetUppercase(cornerstoneObject),
            ShowSuggestions = GetShowSuggestions(cornerstoneObject),
            LocaleHints = GetLocaleHints(cornerstoneObject),
        };

        return result;
    }

    public static readonly TextInputOptions Default = new();
    
    /// <summary>
    /// Defines the <see cref="ContentType"/> property.
    /// </summary>
    public static readonly AttachedProperty<TextInputContentType> ContentTypeProperty =
        PresentationProperty.RegisterAttached<TextInputOptions, StyledElement, TextInputContentType>(
            "ContentType",
            defaultValue: TextInputContentType.Normal,
            inherits: true);
    
    /// <summary>
    /// Sets the value of the attached <see cref="ContentTypeProperty"/> on a control.
    /// </summary>
    /// <param name="cornerstoneObject">The control.</param>
    /// <param name="value">The property value to set.</param>
    public static void SetContentType(StyledElement cornerstoneObject, TextInputContentType value)
    {
        cornerstoneObject.SetValue(ContentTypeProperty, value);
    }

    /// <summary>
    /// Gets the value of the attached <see cref="ContentTypeProperty"/>.
    /// </summary>
    /// <param name="cornerstoneObject">The target.</param>
    /// <returns>TextInputContentType</returns>
    public static TextInputContentType GetContentType(StyledElement cornerstoneObject)
    {
        return cornerstoneObject.GetValue(ContentTypeProperty);
    }
    
    /// <summary>
    /// The content type (mostly for determining the shape of the virtual keyboard)
    /// </summary>
    public TextInputContentType ContentType { get; set; }
    
    
    /// <summary>
    /// Defines the <see cref="ReturnKeyType"/> property.
    /// </summary>
    public static readonly AttachedProperty<TextInputReturnKeyType> ReturnKeyTypeProperty =
        PresentationProperty.RegisterAttached<TextInputOptions, StyledElement, TextInputReturnKeyType>(
            "ReturnKeyType",
            defaultValue: TextInputReturnKeyType.Default,
            inherits: true);
    
    /// <summary>
    /// Sets the value of the attached <see cref="ReturnKeyTypeProperty"/> on a control.
    /// </summary>
    /// <param name="cornerstoneObject">The control.</param>
    /// <param name="value">The property value to set.</param>
    public static void SetReturnKeyType(StyledElement cornerstoneObject, TextInputReturnKeyType value)
    {
        cornerstoneObject.SetValue(ReturnKeyTypeProperty, value);
    }

    /// <summary>
    /// Gets the value of the attached <see cref="ReturnKeyTypeProperty"/>.
    /// </summary>
    /// <param name="cornerstoneObject">The target.</param>
    /// <returns>TextInputReturnKeyType</returns>
    public static TextInputReturnKeyType GetReturnKeyType(StyledElement cornerstoneObject)
    {
        return cornerstoneObject.GetValue(ReturnKeyTypeProperty);
    }
    
    /// <summary>
    /// Determines what the Return key says and how it behaves.
    /// </summary>
    public TextInputReturnKeyType ReturnKeyType { get; set; }
    
    /// <summary>
    /// Defines the <see cref="Multiline"/> property.
    /// </summary>
    public static readonly AttachedProperty<bool> MultilineProperty =
        PresentationProperty.RegisterAttached<TextInputOptions, StyledElement, bool>(
            "Multiline",
            inherits: true);
    
    /// <summary>
    /// Sets the value of the attached <see cref="MultilineProperty"/> on a control.
    /// </summary>
    /// <param name="cornerstoneObject">The control.</param>
    /// <param name="value">The property value to set.</param>
    public static void SetMultiline(StyledElement cornerstoneObject, bool value)
    {
        cornerstoneObject.SetValue(MultilineProperty, value);
    }

    /// <summary>
    /// Gets the value of the attached <see cref="MultilineProperty"/>.
    /// </summary>
    /// <param name="cornerstoneObject">The target.</param>
    /// <returns>true if multiline</returns>
    public static bool GetMultiline(StyledElement cornerstoneObject)
    {
        return cornerstoneObject.GetValue(MultilineProperty);
    }
        
    /// <summary>
    /// Text is multiline
    /// </summary>
    public bool Multiline { get; set; }
    
    /// <summary>
    /// Defines the <see cref="Lowercase"/> property.
    /// </summary>
    public static readonly AttachedProperty<bool> LowercaseProperty =
        PresentationProperty.RegisterAttached<TextInputOptions, StyledElement, bool>(
            "Lowercase",
            inherits: true);
    
    /// <summary>
    /// Sets the value of the attached <see cref="LowercaseProperty"/> on a control.
    /// </summary>
    /// <param name="cornerstoneObject">The control.</param>
    /// <param name="value">The property value to set.</param>
    public static void SetLowercase(StyledElement cornerstoneObject, bool value)
    {
        cornerstoneObject.SetValue(LowercaseProperty, value);
    }

    /// <summary>
    /// Gets the value of the attached <see cref="LowercaseProperty"/>.
    /// </summary>
    /// <param name="cornerstoneObject">The target.</param>
    /// <returns>true if Lowercase</returns>
    public static bool GetLowercase(StyledElement cornerstoneObject)
    {
        return cornerstoneObject.GetValue(LowercaseProperty);
    }
        
    /// <summary>
    /// Text is in lower case
    /// </summary>
    public bool Lowercase { get; set; }
    
    /// <summary>
    /// Defines the <see cref="Uppercase"/> property.
    /// </summary>
    public static readonly AttachedProperty<bool> UppercaseProperty =
        PresentationProperty.RegisterAttached<TextInputOptions, StyledElement, bool>(
            "Uppercase",
            inherits: true);
    
    /// <summary>
    /// Sets the value of the attached <see cref="UppercaseProperty"/> on a control.
    /// </summary>
    /// <param name="cornerstoneObject">The control.</param>
    /// <param name="value">The property value to set.</param>
    public static void SetUppercase(StyledElement cornerstoneObject, bool value)
    {
        cornerstoneObject.SetValue(UppercaseProperty, value);
    }

    /// <summary>
    /// Gets the value of the attached <see cref="UppercaseProperty"/>.
    /// </summary>
    /// <param name="cornerstoneObject">The target.</param>
    /// <returns>true if Uppercase</returns>
    public static bool GetUppercase(StyledElement cornerstoneObject)
    {
        return cornerstoneObject.GetValue(UppercaseProperty);
    }
        
    /// <summary>
    /// Text is in upper case
    /// </summary>
    public bool Uppercase { get; set; }
        
    /// <summary>
    /// Defines the <see cref="AutoCapitalization"/> property.
    /// </summary>
    public static readonly AttachedProperty<bool> AutoCapitalizationProperty =
        PresentationProperty.RegisterAttached<TextInputOptions, StyledElement, bool>(
            "AutoCapitalization",
            inherits: true);
    
    /// <summary>
    /// Sets the value of the attached <see cref="AutoCapitalizationProperty"/> on a control.
    /// </summary>
    /// <param name="cornerstoneObject">The control.</param>
    /// <param name="value">The property value to set.</param>
    public static void SetAutoCapitalization(StyledElement cornerstoneObject, bool value)
    {
        cornerstoneObject.SetValue(AutoCapitalizationProperty, value);
    }

    /// <summary>
    /// Gets the value of the attached <see cref="AutoCapitalizationProperty"/>.
    /// </summary>
    /// <param name="cornerstoneObject">The target.</param>
    /// <returns>true if AutoCapitalization</returns>
    public static bool GetAutoCapitalization(StyledElement cornerstoneObject)
    {
        return cornerstoneObject.GetValue(AutoCapitalizationProperty);
    }
    
    /// <summary>
    /// Automatically capitalize letters at the start of the sentence
    /// </summary>
    public bool AutoCapitalization { get; set; }
        
    /// <summary>
    /// Defines the <see cref="IsSensitive"/> property.
    /// </summary>
    public static readonly AttachedProperty<bool> IsSensitiveProperty =
        PresentationProperty.RegisterAttached<TextInputOptions, StyledElement, bool>(
            "IsSensitive",
            inherits: true);
    
    /// <summary>
    /// Sets the value of the attached <see cref="IsSensitiveProperty"/> on a control.
    /// </summary>
    /// <param name="cornerstoneObject">The control.</param>
    /// <param name="value">The property value to set.</param>
    public static void SetIsSensitive(StyledElement cornerstoneObject, bool value)
    {
        cornerstoneObject.SetValue(IsSensitiveProperty, value);
    }

    /// <summary>
    /// Gets the value of the attached <see cref="IsSensitiveProperty"/>.
    /// </summary>
    /// <param name="cornerstoneObject">The target.</param>
    /// <returns>true if IsSensitive</returns>
    public static bool GetIsSensitive(StyledElement cornerstoneObject)
    {
        return cornerstoneObject.GetValue(IsSensitiveProperty);
    }
    
    /// <summary>
    /// Text contains sensitive data like card numbers and should not be stored  
    /// </summary>
    public bool IsSensitive { get; set; }
    
    /// <summary>
    /// Defines the <see cref="ShowSuggestions"/> property.
    /// </summary>
    public static readonly AttachedProperty<bool?> ShowSuggestionsProperty =
        PresentationProperty.RegisterAttached<TextInputOptions, StyledElement, bool?>(
            "ShowSuggestions",
            inherits: true);
    
    /// <summary>
    /// Sets the value of the attached <see cref="ShowSuggestionsProperty"/> on a control.
    /// </summary>
    /// <param name="cornerstoneObject">The control.</param>
    /// <param name="value">The property value to set.</param>
    public static void SetShowSuggestions(StyledElement cornerstoneObject, bool? value)
    {
        cornerstoneObject.SetValue(ShowSuggestionsProperty, value);
    }

    /// <summary>
    /// Gets the value of the attached <see cref="ShowSuggestionsProperty"/>.
    /// </summary>
    /// <param name="cornerstoneObject">The target.</param>
    /// <returns>true if ShowSuggestions</returns>
    public static bool? GetShowSuggestions(StyledElement cornerstoneObject)
    {
        return cornerstoneObject.GetValue(ShowSuggestionsProperty);
    }
    
    /// <summary>
    /// Show virtual keyboard suggestions
    /// </summary>
    public bool? ShowSuggestions { get; set; }

    /// <summary>
    /// Defines the <see cref="LocaleHints"/> property.
    /// </summary>
    public static readonly AttachedProperty<IReadOnlyList<string>?> LocaleHintsProperty =
        PresentationProperty.RegisterAttached<TextInputOptions, StyledElement, IReadOnlyList<string>?>(
            "LocaleHints",
            inherits: true);

    /// <summary>
    /// Sets the value of the attached <see cref="LocaleHintsProperty"/> on a control.
    /// </summary>
    /// <param name="cornerstoneObject">The control.</param>
    /// <param name="value">The property value to set.</param>
    public static void SetLocaleHints(StyledElement cornerstoneObject, IReadOnlyList<string>? value)
    {
        cornerstoneObject.SetValue(LocaleHintsProperty, value);
    }

    /// <summary>
    /// Gets the value of the attached <see cref="LocaleHintsProperty"/>.
    /// </summary>
    /// <param name="cornerstoneObject">The target.</param>
    /// <returns>BCP47 locale values</returns>
    public static IReadOnlyList<string>? GetLocaleHints(StyledElement cornerstoneObject)
    {
        return cornerstoneObject.GetValue(LocaleHintsProperty);
    }

    /// <summary>
    /// Gets or sets the locale hints, using BCP47 values.
    /// </summary>
    public IReadOnlyList<string>? LocaleHints { get; set; }
}
