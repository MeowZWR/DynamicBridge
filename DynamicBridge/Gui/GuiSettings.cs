using DynamicBridge.Configuration;
using DynamicBridge.IPC.Glamourer;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicBridge.Gui;
public static class GuiSettings
{
    public static Dictionary<GlamourerNoRuleBehavior, string> GlamourerNoRuleBehaviorNames = new()
    {
        [GlamourerNoRuleBehavior.RevertToNormal] = "恢复到游戏状态",
        [GlamourerNoRuleBehavior.RevertToAutomation] = "恢复到Glamourer的自动执行",
        [GlamourerNoRuleBehavior.StoreRestore] = "[Beta] 恢复到应用规则前的外观",
    };

    public static Dictionary<ImGuiComboFlags, string> ComboFlagNames = new()
    {
        [ImGuiComboFlags.HeightSmall] = "小",
        [ImGuiComboFlags.HeightRegular] = "标准",
        [ImGuiComboFlags.HeightLarge] = "大",
        [ImGuiComboFlags.HeightLargest] = "尽可能最大",
    };

    public static void Draw()
    {
        ImGui.Checkbox($"启用插件", ref C.Enable);
        if(ImGuiGroup.BeginGroupBox("常规"))
        {
            ImGuiEx.CheckboxInverted("隐藏教程", ref C.ShowTutorial);
            ImGui.Checkbox($"允许使用否定条件", ref C.AllowNegativeConditions);
            ImGuiEx.HelpMarker("如果启用此选项，你可以使用×标记任何条件。如果一条动态规则中匹配到任何×标的条件，则忽略整条规则。");
            ImGui.Checkbox("在预设编辑器中显示Glamourer角色设计的完整路径（如果有）", ref C.GlamourerFullPath);
            ImGuiEx.SetNextItemWidthScaled(150f);
            ImGui.Checkbox("Glamourer 下拉菜单选项有更改时重新应用规则和预设", ref C.AutoApplyOnChange);
            ImGuiEx.EnumCombo("下拉菜单尺寸", ref C.ComboSize, ComboFlagNames.ContainsKey, ComboFlagNames);
            if(ImGui.Checkbox($"在职业或套装改变时强制更新外观。", ref C.UpdateJobGSChange))
            {
                if (C.UpdateJobGSChange)
                {
                    P.Memory.EquipGearsetHook.Enable();
                }
                else
                {
                    P.Memory.EquipGearsetHook.Disable();
                }
            }
            /*ImGui.Checkbox($"Force update appearance on manual gear changes", ref C.UpdateGearChange);
            ImGuiEx.HelpMarker("This option impacts performance", EColor.OrangeBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());*/
            ImGui.Separator();
            ImGui.Checkbox($"[Beta]启用匿名模式（正在开发，仅在某些位置匿名）", ref C.NoNames);
            ImGuiEx.HelpMarker($"将角色名称替换为随机动物名称，将服务器名称替换为任意幻想世界名称。相同的对象总是会产生相同的名字，对你来说是这样，但对其他人来说却不是。此外，隐藏输入字段中的文本，并显示临时配置文件/预设ID来代替。");
            ImGuiEx.HelpMarker($"警告！在日志和调试选项卡以及卫月日志中仍会显示真实名称！\n警告！如果要分享配置文件并且启用了匿名，拿到文件的人[可以恢复真实名称]。如果你需要向他人发送配置文件并保持匿名状态，请单击“重新生成匿名种子”按钮，发送配置文件，并再次单击该按钮。", ImGuiColors.DalamudOrange);
            ImGuiEx.Spacing();
            ImGui.Checkbox($"尽可能只使用首字母替换原文字。", ref C.LesserCensor);
            ImGuiEx.Spacing();
            if (ImGui.Button("重新生成匿名种子"))
            {
                C.CensorSeed = Guid.NewGuid().ToString();
            }
            ImGuiEx.HelpMarker($"按下此按钮后，隐匿的名称将发生更改。");

            ImGuiEx.CheckboxInverted($"拆分职业类型和职业", ref C.UnifyJobs);

            ImGui.Checkbox("选择后，自动使用第一个选定插件的选项名称填充空预设名称", ref C.AutofillFromGlam);
            ImGuiGroup.EndGroupBox();
        }

        if(ImGuiGroup.BeginGroupBox("配置规则条件"))
        {
            ImGuiEx.TextWrapped($"在动态规则里启用或禁用条件选项，以优化使用体验并提高性能。");
            ImGuiEx.EzTableColumns("extras", [
                () => ImGui.Checkbox($"状态", ref C.Cond_State),
                () => ImGui.Checkbox($"生物群系", ref C.Cond_Biome),
                () => ImGui.Checkbox($"天气", ref C.Cond_Weather),
                () => ImGui.Checkbox($"时间", ref C.Cond_Time),
                () => ImGui.Checkbox($"区域类型", ref C.Cond_ZoneGroup),
                () => ImGui.Checkbox($"区域", ref C.Cond_Zone),
                () => ImGui.Checkbox($"住宅", ref C.Cond_House),
                () => ImGui.Checkbox($"情感动作", ref C.Cond_Emote),
                () => ImGui.Checkbox($"职业", ref C.Cond_Job),
                () => ImGui.Checkbox($"服务器", ref C.Cond_World),
                () => ImGui.Checkbox($"套装模板", ref C.Cond_Gearset),
            ],
                (int)(ImGui.GetContentRegionAvail().X / 180f), ImGuiTableFlags.BordersInner);
            ImGuiGroup.EndGroupBox();
        }

        if(ImGuiGroup.BeginGroupBox("集成"))
        {
            ImGuiEx.Text($"在此处，您可以单独启用/禁用插件集成，并配置相关设置。");
            //glam

            ImGui.Checkbox("Glamourer", ref C.EnableGlamourer);
            DrawPluginCheck("Glamourer", "1.2.2.2");
            ImGuiEx.Spacing();
            ImGuiEx.TextV($"未找到Glamourer规则时，DynamicBridge的行为：");
            ImGui.SameLine();
            ImGuiEx.Spacing();
            ImGuiEx.SetNextItemWidthScaled(200f);
            ImGuiEx.EnumCombo("##dbglamdef", ref C.GlamNoRuleBehaviour, GlamourerNoRuleBehaviorNames);
            if(C.ManageGlamourerAutomation)
            {
                if(C.GlamNoRuleBehaviour != GlamourerNoRuleBehavior.RevertToAutomation)
                {
                    ImGuiEx.HelpMarker("如果您正在使用 Glamourer 的自动执行，建议选择“恢复到 Glamourer 的自动执行”。", ImGuiColors.DalamudRed, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                }
            }
            ImGuiEx.Spacing();
            ImGui.Checkbox("允许 DynamicBridge 管理 Glamourer 的自动执行设置", ref C.ManageGlamourerAutomation);
            ImGuiEx.HelpMarker("如果启用此设置，Glamourer 的全局自动执行将在应用任何规则时自动禁用，并在找不到规则时自动启用。");
            if(P.GlamourerManager.Reflector.GetAutomationGlobalState() && P.GlamourerManager.Reflector.GetAutomationStatusForChara())
            {
                if(!C.ManageGlamourerAutomation)
                {
                    ImGuiEx.HelpMarker("您【必须】启用此设置或禁用Glamourer的自动执行，否则Glamourer或DynamicBridge的动态规则将无法正常工作。", ImGuiColors.DalamudRed, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                }
            }
            ImGuiEx.Spacing();
            ImGui.Checkbox("在恢复自动执行前还原角色", ref C.RevertBeforeAutomationRestore);
            ImGuiEx.Spacing();
            ImGui.Checkbox("在应用规则前还原角色", ref C.RevertGlamourerBeforeApply);


            ImGui.Separator();

            //customize

            ImGui.Checkbox("Customize+", ref C.EnableCustomize);
            DrawPluginCheck("CustomizePlus", "2.0.2.3");

            //honorific

            ImGui.Checkbox("Honorific", ref C.EnableHonorific);
            DrawPluginCheck("Honorific", "1.4.2.0");
            ImGuiEx.Spacing();
            ImGui.Checkbox($"允许选择为其他角色添加的称号", ref C.HonotificUnfiltered);

            //penumbra
            ImGui.Checkbox("Penumbra", ref C.EnablePenumbra);
            DrawPluginCheck("Penumbra", "1.0.1.0");

            //moodles
            ImGui.Checkbox("Moodles", ref C.EnableMoodles);
            DrawPluginCheck("Moodles", "1.0.0.15");

            ImGuiGroup.EndGroupBox();
        }

        if (ImGuiGroup.BeginGroupBox("关于"))
        {
            GuiAbout.Draw();
            ImGuiGroup.EndGroupBox();
        }
    }

    private static void DrawPluginCheck(string name, string minVersion = "0.0.0.0")
    {
        ImGui.SameLine();
        var plugin = Svc.PluginInterface.InstalledPlugins.FirstOrDefault(x => x.InternalName == name && x.IsLoaded);
        if(plugin == null)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGuiEx.Text(EColor.RedBright, "\uf00d");
            ImGui.PopFont();
            ImGui.SameLine();
            ImGuiEx.Text($"未安装");
        }
        else
        {
            if(plugin.Version < Version.Parse(minVersion))
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGuiEx.Text(EColor.RedBright, "\uf00d");
                ImGui.PopFont();
                ImGui.SameLine();
                ImGuiEx.Text($"不支持的版本");
            }
            else
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGuiEx.Text(EColor.GreenBright, FontAwesomeIcon.Check.ToIconString());
                ImGui.PopFont();
            }
        }
    }
}
