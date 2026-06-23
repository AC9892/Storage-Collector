using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Control = System.Windows.Controls.Control;
using WpfApplication = System.Windows.Application;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfButton = System.Windows.Controls.Button;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfGroupBox = System.Windows.Controls.GroupBox;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using WpfListBox = System.Windows.Controls.ListBox;
using WpfListView = System.Windows.Controls.ListView;
using WpfPanel = System.Windows.Controls.Panel;
using WpfRadioButton = System.Windows.Controls.RadioButton;
using WpfRichTextBox = System.Windows.Controls.RichTextBox;
using WpfTabControl = System.Windows.Controls.TabControl;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace SafeStorageScanner.Services;

public enum AppTheme
{
    Light,
    Dark
}

public static class ThemeManager
{
    public static AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

    private static readonly Color WindowDark = Color.FromRgb(0x0F, 0x11, 0x15);
    private static readonly Color SurfaceDark = Color.FromRgb(0x16, 0x1B, 0x22);
    private static readonly Color CardDark = Color.FromRgb(0x1C, 0x21, 0x28);
    private static readonly Color BorderDark = Color.FromRgb(0x2D, 0x33, 0x3B);
    private static readonly Color TextDark = Colors.White;
    private static readonly Color SecondaryTextDark = Color.FromRgb(0xB3, 0xBA, 0xC5);
    private static readonly Color AccentDark = Color.FromRgb(0x3B, 0x82, 0xF6);
    private static readonly Color HeaderDark = Color.FromRgb(0x25, 0x2B, 0x34);

    private static readonly Color WindowLight = Color.FromRgb(0xF3, 0xF6, 0xFA);
    private static readonly Color SurfaceLight = Colors.White;
    private static readonly Color CardLight = Colors.White;
    private static readonly Color BorderLight = Color.FromRgb(0xDC, 0xE3, 0xEC);
    private static readonly Color TextLight = Color.FromRgb(0x11, 0x18, 0x27);
    private static readonly Color SecondaryTextLight = Color.FromRgb(0x64, 0x74, 0x8B);
    private static readonly Color AccentLight = Color.FromRgb(0x25, 0x63, 0xEB);

    public static void ApplyTheme(AppTheme theme)
    {
        CurrentTheme = theme;
        foreach (Window window in WpfApplication.Current.Windows)
        {
            ApplyToWindow(window);
        }
    }

    public static void ApplyToWindow(Window window)
    {
        ApplyResources(window.Resources, CurrentTheme);
        InstallStyles(window.Resources, CurrentTheme);
        window.Background = Brush("AppBackground", window);
        window.Foreground = Brush("TextBrush", window);
        window.Dispatcher.BeginInvoke(() =>
        {
            ApplyRecursive(window);
            ValidateNoDefaultWhite(window);
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    public static void ApplyResources(ResourceDictionary resources, AppTheme theme)
    {
        var dark = theme == AppTheme.Dark;
        resources["AppBackground"] = Solid(dark ? WindowDark : WindowLight);
        resources["PanelBackground"] = Solid(dark ? SurfaceDark : SurfaceLight);
        resources["SubtlePanelBackground"] = Solid(dark ? CardDark : WindowLight);
        resources["CardBackground"] = Solid(dark ? CardDark : CardLight);
        resources["InputBackground"] = Solid(dark ? CardDark : Colors.White);
        resources["HeaderBackground"] = Solid(dark ? HeaderDark : Color.FromRgb(0xF1, 0xF5, 0xF9));
        resources["TextBrush"] = Solid(dark ? TextDark : TextLight);
        resources["MutedTextBrush"] = Solid(dark ? SecondaryTextDark : SecondaryTextLight);
        resources["BorderBrushSoft"] = Solid(dark ? BorderDark : BorderLight);
        resources["AccentBrush"] = Solid(dark ? AccentDark : AccentLight);
        resources["SelectionBrush"] = Solid(dark ? AccentDark : AccentLight);
        resources["ButtonBackground"] = Solid(dark ? HeaderDark : Color.FromRgb(0xF8, 0xFA, 0xFC));
        resources["ButtonHoverBackground"] = Solid(dark ? AccentDark : Color.FromRgb(0xDB, 0xE8, 0xFF));
    }

    private static void InstallStyles(ResourceDictionary resources, AppTheme theme)
    {
        resources[typeof(TextBlock)] = TextBlockStyle();
        resources[typeof(WpfTextBox)] = TextBoxStyle();
        resources[typeof(PasswordBox)] = PasswordBoxStyle();
        resources[typeof(WpfComboBox)] = ComboBoxStyle();
        resources[typeof(WpfButton)] = ButtonStyle();
        resources[typeof(WpfListBox)] = ListBoxStyle();
        resources[typeof(ListBoxItem)] = ListBoxItemStyle();
        resources[typeof(WpfTabControl)] = TabControlStyle();
        resources[typeof(TabItem)] = TabItemStyle();
        resources[typeof(DataGrid)] = DataGridStyle();
        resources[typeof(DataGridColumnHeader)] = DataGridHeaderStyle();
        resources[typeof(WpfCheckBox)] = CheckStyle<WpfCheckBox>();
        resources[typeof(WpfRadioButton)] = CheckStyle<WpfRadioButton>();
        resources[typeof(WpfGroupBox)] = GroupBoxStyle();
        resources[typeof(StatusBar)] = StatusBarStyle();
        resources[typeof(Expander)] = ExpanderStyle();
    }

    private static Style TextBlockStyle()
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, Ref("TextBrush")));
        return style;
    }

    private static Style TextBoxStyle()
    {
        var style = new Style(typeof(WpfTextBox));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Ref("InputBackground")));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Ref("TextBrush")));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Ref("BorderBrushSoft")));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 7, 10, 7)));
        style.Setters.Add(new Setter(Control.TemplateProperty, RoundedControlTemplate(typeof(WpfTextBox), true)));
        return style;
    }

    private static Style PasswordBoxStyle()
    {
        var style = new Style(typeof(PasswordBox));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Ref("InputBackground")));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Ref("TextBrush")));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Ref("BorderBrushSoft")));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 7, 10, 7)));
        style.Setters.Add(new Setter(Control.TemplateProperty, RoundedControlTemplate(typeof(PasswordBox), true)));
        return style;
    }

    private static Style ComboBoxStyle()
    {
        var style = new Style(typeof(WpfComboBox));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Ref("InputBackground")));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Ref("TextBrush")));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Ref("BorderBrushSoft")));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 5, 8, 5)));
        style.Setters.Add(new Setter(Control.TemplateProperty, ComboBoxTemplate()));
        return style;
    }

    private static Style ButtonStyle()
    {
        var style = new Style(typeof(WpfButton));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Ref("ButtonBackground")));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Ref("TextBrush")));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Ref("BorderBrushSoft")));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(16, 10, 16, 10)));
        style.Setters.Add(new Setter(Control.TemplateProperty, ButtonTemplate()));
        return style;
    }

    private static Style ListBoxStyle()
    {
        var style = new Style(typeof(WpfListBox));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Ref("PanelBackground")));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Ref("TextBrush")));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Ref("BorderBrushSoft")));
        return style;
    }

    private static Style ListBoxItemStyle()
    {
        var style = new Style(typeof(ListBoxItem));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Ref("TextBrush")));
        style.Setters.Add(new Setter(Control.BackgroundProperty, WpfBrushes.Transparent));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 10, 12, 10)));
        style.Setters.Add(new Setter(Control.TemplateProperty, ListBoxItemTemplate()));
        return style;
    }

    private static Style TabControlStyle()
    {
        var style = new Style(typeof(WpfTabControl));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Ref("PanelBackground")));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Ref("TextBrush")));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Ref("BorderBrushSoft")));
        return style;
    }

    private static Style TabItemStyle()
    {
        var style = new Style(typeof(TabItem));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Ref("InputBackground")));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Ref("TextBrush")));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Ref("BorderBrushSoft")));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 7, 12, 7)));
        style.Setters.Add(new Setter(Control.TemplateProperty, TabItemTemplate()));
        return style;
    }

    private static Style DataGridStyle()
    {
        var style = new Style(typeof(DataGrid));
        style.Setters.Add(new Setter(DataGrid.AutoGenerateColumnsProperty, false));
        style.Setters.Add(new Setter(DataGrid.IsReadOnlyProperty, true));
        style.Setters.Add(new Setter(DataGrid.CanUserAddRowsProperty, false));
        style.Setters.Add(new Setter(DataGrid.CanUserDeleteRowsProperty, false));
        style.Setters.Add(new Setter(DataGrid.SelectionModeProperty, DataGridSelectionMode.Single));
        style.Setters.Add(new Setter(DataGrid.GridLinesVisibilityProperty, DataGridGridLinesVisibility.Horizontal));
        style.Setters.Add(new Setter(DataGrid.HeadersVisibilityProperty, DataGridHeadersVisibility.Column));
        style.Setters.Add(new Setter(DataGrid.RowHeightProperty, 34.0));
        style.Setters.Add(new Setter(Control.FontSizeProperty, 13.0));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Ref("InputBackground")));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Ref("TextBrush")));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Ref("BorderBrushSoft")));
        style.Setters.Add(new Setter(DataGrid.RowBackgroundProperty, Ref("InputBackground")));
        style.Setters.Add(new Setter(DataGrid.AlternatingRowBackgroundProperty, Ref("PanelBackground")));
        style.Setters.Add(new Setter(DataGrid.HorizontalGridLinesBrushProperty, Ref("BorderBrushSoft")));
        style.Setters.Add(new Setter(DataGrid.VerticalGridLinesBrushProperty, Ref("BorderBrushSoft")));
        return style;
    }

    private static Style DataGridHeaderStyle()
    {
        var style = new Style(typeof(DataGridColumnHeader));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Ref("HeaderBackground")));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Ref("TextBrush")));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Ref("BorderBrushSoft")));
        return style;
    }

    private static Style CheckStyle<T>() where T : Control
    {
        var style = new Style(typeof(T));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Ref("TextBrush")));
        style.Setters.Add(new Setter(Control.BackgroundProperty, WpfBrushes.Transparent));
        if (typeof(T) == typeof(WpfRadioButton))
        {
            style.Setters.Add(new Setter(Control.BorderBrushProperty, Ref("BorderBrushSoft")));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(14, 8, 14, 8)));
            style.Setters.Add(new Setter(Control.TemplateProperty, RadioButtonTemplate()));
        }
        return style;
    }

    private static Style GroupBoxStyle()
    {
        var style = new Style(typeof(WpfGroupBox));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Ref("CardBackground")));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Ref("TextBrush")));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Ref("BorderBrushSoft")));
        return style;
    }

    private static Style StatusBarStyle()
    {
        var style = new Style(typeof(StatusBar));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Ref("PanelBackground")));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Ref("TextBrush")));
        return style;
    }

    private static Style ExpanderStyle()
    {
        var style = new Style(typeof(Expander));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Ref("TextBrush")));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Ref("PanelBackground")));
        return style;
    }

    private static ControlTemplate RoundedControlTemplate(Type targetType, bool scrollContent)
    {
        var template = new ControlTemplate(targetType);
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        border.SetValue(Border.SnapsToDevicePixelsProperty, true);
        var host = new FrameworkElementFactory(typeof(ScrollViewer));
        host.SetValue(FrameworkElement.NameProperty, "PART_ContentHost");
        host.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
        border.AppendChild(host);
        template.VisualTree = border;
        return template;
    }

    private static ControlTemplate ButtonTemplate()
    {
        var template = new ControlTemplate(typeof(WpfButton));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(ContentPresenter.HorizontalAlignmentProperty, WpfHorizontalAlignment.Center);
        content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        content.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
        border.AppendChild(content);
        template.VisualTree = border;
        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(Control.BackgroundProperty, Ref("ButtonHoverBackground")));
        template.Triggers.Add(hover);
        return template;
    }

    private static ControlTemplate ListBoxItemTemplate()
    {
        var template = new ControlTemplate(typeof(ListBoxItem));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(0));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        content.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
        border.AppendChild(content);
        template.VisualTree = border;

        var selected = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
        selected.Setters.Add(new Setter(Control.BackgroundProperty, Ref("SelectionBrush")));
        selected.Setters.Add(new Setter(Control.ForegroundProperty, WpfBrushes.White));
        template.Triggers.Add(selected);

        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(Control.BackgroundProperty, Ref("ButtonHoverBackground")));
        template.Triggers.Add(hover);
        return template;
    }

    private static ControlTemplate TabItemTemplate()
    {
        var template = new ControlTemplate(typeof(TabItem));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(0));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(ContentPresenter.ContentSourceProperty, "Header");
        content.SetValue(ContentPresenter.HorizontalAlignmentProperty, WpfHorizontalAlignment.Center);
        content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        content.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
        border.AppendChild(content);
        template.VisualTree = border;

        var selected = new Trigger { Property = TabItem.IsSelectedProperty, Value = true };
        selected.Setters.Add(new Setter(Control.BackgroundProperty, Ref("HeaderBackground")));
        selected.Setters.Add(new Setter(Control.ForegroundProperty, Ref("TextBrush")));
        selected.Setters.Add(new Setter(Control.BorderBrushProperty, Ref("AccentBrush")));
        template.Triggers.Add(selected);

        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(Control.BackgroundProperty, Ref("ButtonHoverBackground")));
        template.Triggers.Add(hover);
        return template;
    }

    private static ControlTemplate CheckBoxTemplate()
    {
        var template = new ControlTemplate(typeof(WpfCheckBox));
        var root = new FrameworkElementFactory(typeof(StackPanel));
        root.SetValue(StackPanel.OrientationProperty, System.Windows.Controls.Orientation.Horizontal);

        var box = new FrameworkElementFactory(typeof(Border));
        box.SetValue(FrameworkElement.NameProperty, "CheckBoxSquare");
        box.SetValue(FrameworkElement.WidthProperty, 16.0);
        box.SetValue(FrameworkElement.HeightProperty, 16.0);
        box.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 0));
        box.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
        box.SetValue(Border.BackgroundProperty, Ref("InputBackground"));
        box.SetValue(Border.BorderBrushProperty, Ref("BorderBrushSoft"));
        box.SetValue(Border.BorderThicknessProperty, new Thickness(1));

        var mark = new FrameworkElementFactory(typeof(TextBlock));
        mark.SetValue(FrameworkElement.NameProperty, "CheckMark");
        mark.SetValue(TextBlock.TextProperty, "✓");
        mark.SetValue(TextBlock.ForegroundProperty, WpfBrushes.White);
        mark.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
        mark.SetValue(TextBlock.FontSizeProperty, 12.0);
        mark.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
        mark.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
        mark.SetValue(FrameworkElement.HorizontalAlignmentProperty, WpfHorizontalAlignment.Center);
        mark.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        box.AppendChild(mark);
        root.AppendChild(box);

        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        content.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(ContentControl.ContentProperty));
        root.AppendChild(content);
        template.VisualTree = root;

        var checkedTrigger = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
        checkedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, Ref("SelectionBrush"), "CheckBoxSquare"));
        checkedTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, Ref("SelectionBrush"), "CheckBoxSquare"));
        checkedTrigger.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible, "CheckMark"));
        template.Triggers.Add(checkedTrigger);

        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(Border.BorderBrushProperty, Ref("AccentBrush"), "CheckBoxSquare"));
        template.Triggers.Add(hover);
        return template;
    }

    private static ControlTemplate RadioButtonTemplate()
    {
        var template = new ControlTemplate(typeof(WpfRadioButton));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(ContentPresenter.HorizontalAlignmentProperty, WpfHorizontalAlignment.Center);
        content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        content.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
        border.AppendChild(content);
        template.VisualTree = border;

        var checkedTrigger = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
        checkedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, Ref("SelectionBrush")));
        checkedTrigger.Setters.Add(new Setter(Control.BorderBrushProperty, Ref("SelectionBrush")));
        checkedTrigger.Setters.Add(new Setter(Control.ForegroundProperty, WpfBrushes.White));
        template.Triggers.Add(checkedTrigger);

        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(Control.BorderBrushProperty, Ref("AccentBrush")));
        template.Triggers.Add(hover);
        return template;
    }

    private static ControlTemplate ComboBoxTemplate()
    {
        var template = new ControlTemplate(typeof(WpfComboBox));
        var root = new FrameworkElementFactory(typeof(Grid));
        var toggle = new FrameworkElementFactory(typeof(ToggleButton));
        toggle.SetValue(FrameworkElement.NameProperty, "ToggleButton");
        toggle.SetValue(Control.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        toggle.SetValue(Control.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        toggle.SetValue(Control.ForegroundProperty, new TemplateBindingExtension(Control.ForegroundProperty));
        toggle.SetBinding(ToggleButton.IsCheckedProperty, new System.Windows.Data.Binding("IsDropDownOpen")
        {
            RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent),
            Mode = System.Windows.Data.BindingMode.TwoWay
        });
        toggle.SetValue(Control.TemplateProperty, ComboToggleTemplate());
        root.AppendChild(toggle);

        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(WpfComboBox.SelectionBoxItemProperty));
        content.SetValue(ContentPresenter.MarginProperty, new Thickness(10, 0, 30, 0));
        content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        content.SetValue(ContentPresenter.HorizontalAlignmentProperty, WpfHorizontalAlignment.Left);
        content.SetValue(UIElement.IsHitTestVisibleProperty, false);
        root.AppendChild(content);

        var popup = new FrameworkElementFactory(typeof(Popup));
        popup.SetValue(FrameworkElement.NameProperty, "PART_Popup");
        popup.SetValue(Popup.PlacementProperty, PlacementMode.Bottom);
        popup.SetBinding(Popup.IsOpenProperty, new System.Windows.Data.Binding("IsDropDownOpen")
        {
            RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)
        });
        var popupBorder = new FrameworkElementFactory(typeof(Border));
        popupBorder.SetValue(Border.BackgroundProperty, Ref("HeaderBackground"));
        popupBorder.SetValue(Border.BorderBrushProperty, Ref("BorderBrushSoft"));
        popupBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        popupBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        var scroll = new FrameworkElementFactory(typeof(ScrollViewer));
        var presenter = new FrameworkElementFactory(typeof(ItemsPresenter));
        scroll.AppendChild(presenter);
        popupBorder.AppendChild(scroll);
        popup.AppendChild(popupBorder);
        root.AppendChild(popup);
        template.VisualTree = root;
        return template;
    }

    private static ControlTemplate ComboToggleTemplate()
    {
        var template = new ControlTemplate(typeof(ToggleButton));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        var arrow = new FrameworkElementFactory(typeof(TextBlock));
        arrow.SetValue(TextBlock.TextProperty, "⌄");
        arrow.SetValue(TextBlock.ForegroundProperty, new TemplateBindingExtension(Control.ForegroundProperty));
        arrow.SetValue(FrameworkElement.HorizontalAlignmentProperty, WpfHorizontalAlignment.Right);
        arrow.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        arrow.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 10, 2));
        border.AppendChild(arrow);
        template.VisualTree = border;
        return template;
    }

    private static void ApplyRecursive(DependencyObject root)
    {
        if (root is Window window)
        {
            window.Background = Brush("AppBackground", window);
            window.Foreground = Brush("TextBrush", window);
        }
        else if (root is Border border)
        {
            if (border.TemplatedParent is not null)
            {
                // Template-owned borders drive selected/hover states; do not flatten them during recursive theming.
            }
            else if (CurrentTheme == AppTheme.Dark)
            {
                border.Background = Brush("CardBackground", border);
                border.BorderBrush = Brush("BorderBrushSoft", border);
            }
            else if (IsWhiteOrDefault(border.Background))
            {
                border.Background = Brush("CardBackground", border);
            }
            if (border.BorderBrush is null || IsWhiteOrDefault(border.BorderBrush))
            {
                border.BorderBrush = Brush("BorderBrushSoft", border);
            }
        }
        else if (root is WpfPanel panel)
        {
            if (panel.Background is null || IsWhiteOrDefault(panel.Background))
            {
                panel.Background = CurrentTheme == AppTheme.Dark ? Brush("PanelBackground", panel) : WpfBrushes.Transparent;
            }
        }
        else if (root is Control control)
        {
            ApplyControl(control);
        }
        else if (root is TextBlock textBlock)
        {
            if (IsWhiteOrDefault(textBlock.Foreground) || textBlock.Foreground == WpfBrushes.Black)
            {
                textBlock.Foreground = Brush("TextBrush", textBlock);
            }
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            ApplyRecursive(VisualTreeHelper.GetChild(root, i));
        }
    }

    private static void ApplyControl(Control control)
    {
        control.Foreground = Brush("TextBrush", control);
        switch (control)
        {
            case WpfTextBox or PasswordBox or WpfRichTextBox:
                control.Background = Brush("InputBackground", control);
                control.BorderBrush = Brush("BorderBrushSoft", control);
                break;
            case WpfComboBox:
                control.Background = Brush("InputBackground", control);
                control.BorderBrush = Brush("BorderBrushSoft", control);
                break;
            case DataGrid grid:
                grid.Background = Brush("InputBackground", grid);
                grid.RowBackground = Brush("InputBackground", grid);
                grid.AlternatingRowBackground = Brush("PanelBackground", grid);
                grid.HorizontalGridLinesBrush = Brush("BorderBrushSoft", grid);
                grid.VerticalGridLinesBrush = Brush("BorderBrushSoft", grid);
                break;
            case WpfListBox or WpfListView:
                control.Background = Brush("InputBackground", control);
                control.BorderBrush = Brush("BorderBrushSoft", control);
                break;
            case WpfButton:
                control.Background = Brush("ButtonBackground", control);
                control.BorderBrush = Brush("BorderBrushSoft", control);
                break;
            case WpfTabControl:
                control.Background = Brush("PanelBackground", control);
                control.BorderBrush = Brush("BorderBrushSoft", control);
                break;
            case WpfGroupBox:
                control.Background = Brush("CardBackground", control);
                control.BorderBrush = Brush("BorderBrushSoft", control);
                break;
            case StatusBar:
                control.Background = Brush("PanelBackground", control);
                break;
        }
    }

    private static void ValidateNoDefaultWhite(DependencyObject root)
    {
        if (CurrentTheme != AppTheme.Dark)
        {
            return;
        }

        if (root is Control control && IsPureWhite(control.Background))
        {
            control.Background = Brush("InputBackground", control);
        }
        if (root is Border border && IsPureWhite(border.Background))
        {
            border.Background = Brush("CardBackground", border);
        }
        if (root is WpfPanel panel && IsPureWhite(panel.Background))
        {
            panel.Background = Brush("PanelBackground", panel);
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            ValidateNoDefaultWhite(VisualTreeHelper.GetChild(root, i));
        }
    }

    private static bool IsWhiteOrDefault(Brush? brush) => brush is null || IsPureWhite(brush);

    private static bool IsPureWhite(Brush? brush)
    {
        return brush is SolidColorBrush solid &&
               solid.Color.R == 255 &&
               solid.Color.G == 255 &&
               solid.Color.B == 255;
    }

    private static SolidColorBrush Solid(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static DynamicResourceExtension Ref(string key) => new(key);
    private static Brush Brush(string key, FrameworkElement element) => (Brush)element.FindResource(key);
}

