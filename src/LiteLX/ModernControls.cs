using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LiteLX
{
    internal sealed class MenuToggle : Button
    {
        public readonly List<MenuEntry> Entries;
        private readonly string displayName;
        private readonly SolidColorBrush trackBrush = new SolidColorBrush();
        private readonly TranslateTransform thumbPosition = new TranslateTransform();
        private bool? displayedState;

        public MenuToggle(string name, List<MenuEntry> entries)
        {
            displayName = name;
            Entries = entries;
            SetResourceReference(StyleProperty, typeof(Button));
            Background = Brushes.Transparent;
            Padding = new Thickness(5);
            Content = new Border {
                Width = 34, Height = 20, CornerRadius = new CornerRadius(10), Background = trackBrush,
                Child = new Border {
                    Width = 14, Height = 14, CornerRadius = new CornerRadius(7), Background = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(3), RenderTransform = thumbPosition
                }
            };
            RefreshState(false);
        }

        public void ShowPending(bool enabled)
        {
            AnimateState(enabled, true);
            IsHitTestVisible = false;
            ToolTip = enabled ? "正在启用…" : "正在禁用…";
        }

        public void RefreshState(bool animate)
        {
            bool enabled = Entries.Any(x => !x.Disabled);
            AnimateState(enabled, animate);
            IsEnabled = enabled ? Entries.Any(x => x.CanDisable) : Entries.Any(x => x.CanEnable);
            IsHitTestVisible = true;
            ToolTip = enabled ? "禁用此入口" : "启用此入口";
            AutomationProperties.SetName(this, displayName + (enabled ? "，禁用此入口" : "，启用此入口"));
        }

        private void AnimateState(bool enabled, bool animate)
        {
            bool useAnimation = animate && SystemParameters.ClientAreaAnimation;
            if (useAnimation && displayedState == enabled) return;
            double start = thumbPosition.X;
            Color startColor = trackBrush.Color;
            double destination = enabled ? 14 : 0;
            Color color = enabled ? Color.FromRgb(82, 100, 245) : Color.FromRgb(205, 208, 219);
            thumbPosition.BeginAnimation(TranslateTransform.XProperty, null);
            trackBrush.BeginAnimation(SolidColorBrush.ColorProperty, null);
            displayedState = enabled;
            thumbPosition.X = useAnimation ? start : destination;
            trackBrush.Color = useAnimation ? startColor : color;
            if (!useAnimation) return;
            // 从当前动画位置继续过渡，快速反向操作时保持连贯。
            var duration = new Duration(TimeSpan.FromMilliseconds(160));
            thumbPosition.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(start, destination, duration) {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.HoldEnd
            });
            trackBrush.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(startColor, color, duration) {
                FillBehavior = FillBehavior.HoldEnd
            });
        }
    }

    internal static class ModernStyle
    {
        public static void EnableDpi()
        {
            try { SetProcessDpiAwarenessContext(new IntPtr(-4)); } catch (EntryPointNotFoundException) { }
        }
        public static void WindowCorners(IntPtr window)
        {
            try { int rounded = 2; DwmSetWindowAttribute(window, 33, ref rounded, 4); } catch (DllNotFoundException) { }
        }
        [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(IntPtr value);
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
    }
}
