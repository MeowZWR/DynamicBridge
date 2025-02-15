using Dalamud.Interface.Components;
using DynamicBridge.Configuration;
using DynamicBridge.IPC.Glamourer;
using ECommons;
using ECommons.Configuration;
using ECommons.Funding;
using ECommons.GameHelpers;
using ECommons.Reflection;
using ECommons.SimpleGui;
using Lumina.Excel.Sheets;
using Newtonsoft.Json.Linq;
using System.Runtime.ConstrainedExecution;
using System.Xml.Linq;
using Action = System.Action;

namespace DynamicBridge.Gui;

public static unsafe class UI
{

    public static Profile SelectedProfile = null;
    public static Profile Profile => SelectedProfile ?? Utils.GetProfileByCID(Player.CID);
    public const string RandomNotice = "将在以下选项中随机选择：\n";
    public const string AnyNotice = "满足以下任何条件都将触发规则：\n";
    private static string PSelFilter = "";
    public static string RequestTab = null;

    public static void DrawMain()
    {
        var resolution = "";
        if(Player.CID == 0) resolution = "未登录";
        else if(C.Blacklist.Contains(Player.CID)) resolution = "角色已列入黑名单";
        else if(Utils.GetProfileByCID(Player.CID) == null) resolution = "没有关联的配置文件";
        else resolution = $"Profile {Utils.GetProfileByCID(Player.CID).CensoredName}";
        if(!C.Enable && Environment.TickCount64 % 2000 > 1000) resolution = "插件已被设置禁用";
        EzConfigGui.Window.WindowName = $"{DalamudReflector.GetPluginName()} v{P.GetType().Assembly.GetName().Version} [{resolution}]###{DalamudReflector.GetPluginName()}";
        if(ImGui.IsWindowAppearing())
        {
            Utils.ResetCaches();
            foreach(var x in Svc.Data.GetExcelSheet<Weather>()) ThreadLoadImageHandler.TryGetIconTextureWrap((uint)x.Icon, false, out _);
            foreach(var x in Svc.Data.GetExcelSheet<Emote>()) ThreadLoadImageHandler.TryGetIconTextureWrap(x.Icon, false, out _);
        }
        PatreonBanner.DrawRight();
        ImGuiEx.EzTabBar("TabsNR2", PatreonBanner.Text, RequestTab, ImGuiTabBarFlags.Reorderable, [
            //("Settings", Settings, null, true),
            (C.ShowTutorial?"教程":null, GuiTutorial.Draw, null, true),
            ("动态规则", GuiRules.Draw, Colors.TabGreen, true),
            ("独立预设", GuiPresets.DrawUser, Colors.TabGreen, true),
            ("全局预设", GuiPresets.DrawGlobal, Colors.TabYellow, true),
            ("层组设计", ComplexGlamourer.Draw, Colors.TabPurple, true),
            ("住宅登记", HouseReg.Draw, Colors.TabPurple, true),
            ("配置文件", GuiProfiles.Draw, Colors.TabBlue, true),
            ("角色管理", GuiCharacters.Draw, Colors.TabBlue, true),
            ("插件设置", GuiSettings.Draw, null, true),
            InternalLog.ImGuiTab(),
            (C.Debug?"调试":null, Debug.Draw, ImGuiColors.DalamudGrey3, true),
            ]);
        RequestTab = null;
    }

    public static void ProfileSelectorCommon(Action before = null, Action after = null)
    {
        if(SelectedProfile != null && !C.ProfilesL.Contains(SelectedProfile)) SelectedProfile = null;
        var currentCharaProfile = Utils.GetProfileByCID(Player.CID);

        before?.Invoke();

        if(SelectedProfile == null)
        {
            if(currentCharaProfile == null)
            {
                if(C.Blacklist.Contains(Player.CID))
                {
                    ImGuiEx.InputWithRightButtonsArea(() => Utils.BannerCombo("blisted", $"\"{Censor.Character(Player.NameWithWorld)}\" 已被列入黑名单。选择另一个配置文件进行编辑。", ProfileSelectable), () =>
                    {
                        after?.Invoke();
                        if(ImGuiEx.IconButton(FontAwesomeIcon.ArrowCircleUp))
                        {
                            C.Blacklist.Remove(Player.CID);
                        }
                        ImGuiEx.Tooltip("将该角色移出黑名单。");
                    });
                }
                else if(Player.CID != 0)
                {
                    ImGuiEx.InputWithRightButtonsArea(() => Utils.BannerCombo("noprofile", $"\"{Censor.Character(Player.NameWithWorld)}\" 没有关联的配置文件。选择其他配置文件进行编辑或在“角色管理”选项卡中进行关联。", ProfileSelectable), () =>
                    {
                        after?.Invoke();
                        if(ImGuiEx.IconButton(FontAwesomeIcon.PlusCircle))
                        {
                            var profile = new Profile();
                            C.ProfilesL.Add(profile);
                            profile.Characters = [Player.CID];
                            profile.Name = $"为{Player.Name}自动生成的配置文件";
                        }
                        ImGuiEx.Tooltip($"创建新的空白配置文件并将其分配给当前角色");
                    });
                }
                else
                {
                    ImGuiEx.InputWithRightButtonsArea(() => Utils.BannerCombo("nlg", $"您尚未登录。请选择要编辑的配置文件。", ProfileSelectable), () =>
                    {
                        after?.Invoke();
                        ImGui.Dummy(Vector2.Zero);
                    });
                }
            }
            else
            {
                UsedByCurrent();
            }
        }
        else
        {
            if(currentCharaProfile == SelectedProfile)
            {
                UsedByCurrent();
            }
            else
            {
                ImGuiEx.InputWithRightButtonsArea(() => Utils.BannerCombo("EditNotify", $"您正在编辑配置文件[{SelectedProfile.CensoredName}]。" + (Player.Available?$"它未被[{Censor.Character(Player.NameWithWorld)}]使用。":""), ProfileSelectable, EColor.YellowDark), () =>
                {
                    after?.Invoke();
                    if(!C.Blacklist.Contains(Player.CID))
                    {
                        if(ImGuiEx.IconButton(FontAwesomeIcon.Link))
                        {
                            new TickScheduler(() => SelectedProfile.SetCharacter(Player.CID));
                        }
                        ImGuiEx.Tooltip($"分配配置文件“{SelectedProfile?.CensoredName}”给“{Censor.Character(Player.NameWithWorld)}”");
                    }
                    else
                    {
                        ImGuiEx.HelpMarker("您当前的角色已列入黑名单", null, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                    }
                });
            }
        }

        void UsedByCurrent()
        {
            ImGuiEx.InputWithRightButtonsArea(() => Utils.BannerCombo("EditNotify", $"您正在编辑配置文件“{currentCharaProfile.CensoredName}”，它被用于“{Censor.Character(Player.NameWithWorld)}”。", ProfileSelectable, EColor.GreenDark), () =>
            {
                after?.Invoke();
                if(ImGuiEx.IconButton(FontAwesomeIcon.Unlink, enabled: ImGuiEx.Ctrl))
                {
                    new TickScheduler(() => currentCharaProfile.Characters.Remove(Player.CID));
                }
                ImGuiEx.Tooltip($"按住CTRL键并点击来取消分配配置文件“{currentCharaProfile?.CensoredName}”给“{Censor.Character(Player.NameWithWorld)}”。");
            });
        }

        void ProfileSelectable()
        {
            if(ImGui.Selectable("- 当前角色 -", SelectedProfile == null))
            {
                SelectedProfile = null;
            }
            ImGui.Separator();
            ImGuiEx.SetNextItemWidthScaled(150f);
            ImGui.InputTextWithHint($"##SearchCombo", "筛选...", ref PSelFilter, 50, Utils.CensorFlags);
            foreach(var x in C.ProfilesL)
            {
                if(PSelFilter.Length > 0 && !x.Name.Contains(PSelFilter, StringComparison.OrdinalIgnoreCase)) continue;
                if(SelectedProfile == x && ImGui.IsWindowAppearing()) ImGui.SetScrollHereY();
                if(ImGui.Selectable($"{x.CensoredName}##{x.GUID}", SelectedProfile == x))
                {
                    new TickScheduler(() => SelectedProfile = x);
                }
            }
        }
    }

    public static void ForceUpdateButton()
    {
        if(ImGuiEx.IconButton(FontAwesomeIcon.Tshirt))
        {
            P.ForceUpdate = true;
        }
        ImGuiEx.Tooltip("强制更新角色，重新应用所有规则并重置");
    }
}
