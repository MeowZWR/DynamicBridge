using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DynamicBridge.Gui;
public static class GuiTutorial
{
    static readonly string Content = @"
欢迎使用DynamicBridge插件！
此插件允许您根据各种规则动态地更改Glamourer、Customize+和Honorific的自定义设计。您可以使用此插件只进行简单的操作，例如手动切换外观，也可以创建非常复杂和精确的规则合集。
---
如果您以前使用过旧版本的插件，您的配置已被转换为更易于使用的配置文件系统。为了防止出现问题，旧配置文件也已进行了备份。
现在可以创建配置文件并将其分配给角色，而不是强制给每个角色执行对应的配置文件。这样您就可以为多个角色使用同一个配置文件，也可以轻松地修改用于您的角色的配置文件。
---
+快速了解
我将按您开始使用此插件所需的最少步骤来描述。
首先，假设您以前从未创建过配置文件，在游戏中登录到您的角色，切换到“动态规则”或“独立预设”选项卡，然后单击“+”按钮：
image=1
这将为您创建新的配置文件，并将其与当前角色关联。现在，您可以根据自己的喜好创建规则和预设。
+规则和预设
DynamicBridge会一直跟踪您的角色状态，并从上到下依次检查动态规则。一旦找到正确的规则，所选规则中的相应设置将应用到您的角色，其他规则将停止处理。当前应用的规则以绿色突出显示。
预设则是一套将Glamourer、Customize+和Honorific的自定义选项应用到您的角色的一套组合模板。 
规则和预设标题部分功能概述：
image=2

fai=Plus
用于创建新的、空的动态规则或预设，并将其添加到列表的末尾。 

fai=FolderPlus
用于在“预设”部分中创建新的折叠组。折叠组仅用于在视觉上分离预设。

fai=Paste
用于从剪贴板粘贴现有规则或预设（如果您以前复制过）。

fai=Tshirt
用于将规则和预设重新应用于角色。如果您希望更改立即生效，则应在修改规则或预设后使用它。

fai=Unlink
用于取消配置文件与当前角色的链接。配置文件不会被删除，可以再次链接到您的角色。当您编辑未分配给当前角色的配置文件时，此按钮将更改为“链接配置文件”。

标题中间部分提示当前被选中正在进行编辑的配置文件，以及它是否链接到您的角色。单击中间部分可以在下拉菜单中选择要编辑的配置文件。
---
+动态规则
您可以转到“插件设置”选项卡来启用或禁用额外条件。请注意：您启用的条件越多，插件所需的处理时间就越多。规则也是如此——规则越多，处理时间就越长。幸运的是，您将在仔细设置几百条规则后才会感知到这个影响。

fai=Check
表明规则是否已启用。禁用的规则将被完全忽略。

fai=ArrowsUpDownLeftRight
向上或向下拖拽这个按钮以对规则重新排序。

fai=f103
表示在依次应用规则预设时跳过匹配此规则。当两个或多个规则匹配时，它们的预设会被依次应用。

fai=Copy
将此规则复制到剪贴板以备将来使用。您可以保存它并与他人分享。
+预设
您可以转到“插件设置”选项卡禁用不使用的插件。

fai=Circle
使用此按钮可将预设设置为静态。当预设设置为静态时，规则和基础预设将被完全忽略，您的外观将始终被设置为指定预设。

如果您选择在基础预设中设置任何值，则当相应的插件部分中没有其他值时，将使用这些值。
";
    public static void Draw()
    {
        ImGuiEx.CheckboxInverted("隐藏教程", ref C.ShowTutorial);
        var array = Content.ReplaceLineEndings().Split(Environment.NewLine);
        for(var i = 0; i < array.Length; i++)
        {
            var s = array[i];
            if(s.StartsWith("+"))
            {
                if(ImGui.TreeNode(s[1..]))
                {
                    do
                    {
                        DrawLine(array[i + 1]);
                        i++;
                    }
                    while(i + 1 < array.Length && !array[i + 1].StartsWith("+"));
                    ImGui.TreePop();
                }
                else
                {
                    do
                    {
                        i++;
                    }
                    while(i + 1 < array.Length && !array[i + 1].StartsWith("+"));
                }
            }
            else
            {
                DrawLine(s);
            }
        }
    }

    private static void DrawLine(string s)
    {
        if(s.StartsWith("fai="))
        {
            var chr = s[4..];
            var success = false;
            foreach(var x in Enum.GetValues<FontAwesomeIcon>())
            {
                if(x.ToString() == chr)
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGuiEx.Text($"{x.ToIconString()}");
                    ImGui.PopFont();
                    ImGui.SameLine();
                    success = true;
                    break;
                }
            }
            if(!success && uint.TryParse(chr, System.Globalization.NumberStyles.HexNumber, null, out var num))
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGuiEx.Text($"{(char)num}");
                ImGui.PopFont();
                ImGui.SameLine();
            }
            else
            {
                //ImGuiEx.Text($"Parse error: {chr}");
            }
        }
        else if(s.StartsWith("image="))
        {
            if(ThreadLoadImageHandler.TryGetTextureWrap($"{Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, "res", "tutorial", $"{s[6..]}.png")}", out var tex))
            {
                ImGui.Image(tex.ImGuiHandle, new(tex.Width, tex.Height));
            }
        }
        else if(s == "---")
        {
            ImGui.Separator();
        }
        else if(s == "")
        {
            ImGui.Dummy(new Vector2(5));
        }
        else
        {
            ImGuiEx.TextWrapped(s);
        }
    }
}
