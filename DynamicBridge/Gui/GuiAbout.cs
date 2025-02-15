using Dalamud.Interface.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Gui;
public static class GuiAbout
{
    public static void Draw()
    {
        if(!Utils.IsDisguise())
        {
            ImGuiEx.LineCentered("about1", () =>
            {
                ImGuiEx.Text($"Made by NightmareXIV in collaboration with AsunaTsuki");
            });
            ImGuiEx.LineCentered("about2", () =>
            {
                if(ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Comments, "加入Discord获取支持和更新日志"))
                {
                    ShellStart("https://discord.gg/m8NRt4X8Gf");
                }
            });
            ImGuiEx.LineCentered("about3", () =>
            {
                if(ImGuiComponents.IconButtonWithText(FontAwesomeIcon.QuestionCircle, "阅读说明和常见问题解答"))
                {
                    ShellStart("https://github.com/NightmareXIV/DynamicBridge/tree/main/docs");
                }
            });
        }
        else
        {
            ImGuiEx.LineCentered("about4", () =>
            {
                if(ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Bug, "在GitHub上报告问题、请求功能或提问"))
                {
                    ShellStart("https://github.com/Limiana/DynamicBridgeStandalone/issues");
                }
            });
        }
    }
}
