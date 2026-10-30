using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace Semi.Avalonia.Recolor;

/// <summary>
///     Recursively walks every resource provider reachable from the application:
///     application resources, every style tree (including included styles), merged dictionaries,
///     theme dictionaries, control themes and their setters.
/// </summary>
internal static class ResourceWalker
{
    internal interface IRecolorVisitor
    {
        /// <summary>
        ///     <paramref name="source" /> is the containing <see cref="IResourceDictionary" />
        ///     or the <see cref="Setter" /> owning the value.
        /// </summary>
        void VisitValue(object source, object key, object? value, ThemeVariant? variant);

        /// <summary>Called when a Semi palette dictionary is encountered.</summary>
        void VisitPalette(IResourceDictionary palette, ThemeVariant? variant);

        /// <summary>Applies deferred dictionary entry changes.</summary>
        void Complete();
    }

    public static void WalkApplication(Application application, IRecolorVisitor visitor)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        WalkStyles(application.Styles, visitor, null, visited);

        if (application.Resources is { } resources)
        {
            WalkDictionary(resources, visitor, null, visited);
        }

        visitor.Complete();
    }

    private static void WalkStyles(Styles styles, IRecolorVisitor visitor, ThemeVariant? variant,
        HashSet<object> visited)
    {
        if (!visited.Add(styles))
        {
            return;
        }

        if (styles.Resources is { } resources)
        {
            WalkDictionary(resources, visitor, variant, visited);
        }

        foreach (var child in styles)
        {
            WalkStyle(child, visitor, variant, visited);
        }
    }

    private static void WalkStyle(IStyle style, IRecolorVisitor visitor, ThemeVariant? variant,
        HashSet<object> visited)
    {
        switch (style)
        {
            case Styles styles:
                WalkStyles(styles, visitor, variant, visited);
                break;
            case StyleInclude include:
                if (include.Loaded is { } loaded)
                {
                    WalkStyle(loaded, visitor, variant, visited);
                }

                break;
            case StyleBase styleBase:
                if (!visited.Add(styleBase))
                {
                    return;
                }

                WalkSetters(styleBase.Setters, visitor, variant, visited);
                if (styleBase.Resources is { } resources)
                {
                    WalkDictionary(resources, visitor, variant, visited);
                }

                if (styleBase is ControlTheme { BasedOn: { } basedOn })
                {
                    WalkStyle(basedOn, visitor, variant, visited);
                }

                foreach (var child in styleBase.Children)
                {
                    WalkStyle(child, visitor, variant, visited);
                }

                break;
        }
    }

    private static void WalkSetters(IList<SetterBase> setters, IRecolorVisitor visitor, ThemeVariant? variant,
        HashSet<object> visited)
    {
        foreach (var setter in setters)
        {
            if (setter is Setter { Value: not null } s)
            {
                visitor.VisitValue(s, s.Property!, s.Value, variant);

                // A setter may carry a nested control theme / style (e.g. a template setter).
                if (s.Value is IStyle nested)
                {
                    WalkStyle(nested, visitor, variant, visited);
                }
            }
        }
    }

    private static void WalkDictionary(IResourceDictionary dictionary, IRecolorVisitor visitor, ThemeVariant? variant,
        HashSet<object> visited)
    {
        if (!visited.Add(dictionary))
        {
            return;
        }

        if (dictionary.ContainsKey(SemiThemeRecolor.PaletteMarkerKey))
        {
            visitor.VisitPalette(dictionary, variant);
        }

        // Materialize deferred entries first: Semi theme dictionaries store aliases
        // (<StaticResource x:Key="..." ResourceKey="..."/>) that only resolve on lookup.
        foreach (var key in dictionary.Keys.ToArray())
        {
            object? value;
            try
            {
                if (!dictionary.TryGetValue(key, out value) || value is null)
                {
                    continue;
                }
            }
            catch
            {
                // A broken/host-dependent resource must not abort the whole walk.
                continue;
            }

            visitor.VisitValue(dictionary, key!, value, variant);

            if (value is IStyle style)
            {
                WalkStyle(style, visitor, variant, visited);
            }
            else if (value is IResourceProvider provider)
            {
                WalkProvider(provider, visitor, variant, visited);
            }
        }

        foreach (var merged in dictionary.MergedDictionaries)
        {
            WalkProvider(merged, visitor, variant, visited);
        }

        foreach (var theme in dictionary.ThemeDictionaries)
        {
            switch (theme.Value)
            {
                case ResourceInclude { Loaded: { } loaded }:
                    WalkDictionary(loaded, visitor, theme.Key, visited);
                    break;
                case IResourceDictionary themeDictionary:
                    WalkDictionary(themeDictionary, visitor, theme.Key, visited);
                    break;
            }
        }
    }

    private static void WalkProvider(IResourceProvider provider, IRecolorVisitor visitor, ThemeVariant? variant,
        HashSet<object> visited)
    {
        switch (provider)
        {
            case ResourceInclude { Loaded: { } loaded }:
                WalkDictionary(loaded, visitor, variant, visited);
                break;
            case IResourceDictionary dictionary:
                WalkDictionary(dictionary, visitor, variant, visited);
                break;
        }
    }
}
