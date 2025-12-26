using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaUI.Common;
using AvaloniaUI.Pages._HomePage;
using AvaloniaUI.Pages._HomePage.WallpaperProperties;

namespace AvaloniaUI.Pages.Common;

public partial class Common_ItemViewer : UserControl
{
    public Common_ItemViewer()
    {
        InitializeComponent();
    }

    public void Setup(UserControl formatter, UserControl? sidePanel)
    {
        cont_Formatter.Child = formatter;

        if (sidePanel != null)
        {
            cont_SidePanel.Children.Add(sidePanel);
        }
    }

    public void RegisterAction(string name, Action callback)
    {
        Common_Button btn = new Common_Button();
        btn.Label = name;
        btn.RegisterClick(callback);

        cont_sidePanel_Actions.Children.Add(btn);
    }

    public void RegisterAction(string name, Func<Task> callback)
    {
        Common_Button btn = new Common_Button();
        btn.Label = name;
        btn.RegisterClick(callback);

        cont_sidePanel_Actions.Children.Add(btn);
    }



    public void OpenSidePanel()
    {
        scroll_SidePanel.ScrollToHome();
        cont_SidePanel_Tags.Children.Clear();
        img_SidePanel_Icon.Background = null;
        lbl_SidePanel_Title.Content = "";
    }

    public void DrawTags(string[]? tags)
    {
        if (tags == null)
            return;

        foreach (string tag in tags)
        {
            Common_Tag tagUI = new Common_Tag();
            tagUI.tagName.Content = tag;

            cont_SidePanel_Tags.Children.Add(tagUI);
        }
    }
}