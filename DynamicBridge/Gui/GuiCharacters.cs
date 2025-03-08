using ECommons.GameHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Gui;
public static class GuiCharacters
{
    private static string[] Filters = ["", "", "", ""];
    public static void Draw()
    {
        ImGuiEx.SetNextItemFullWidth();
        ImGui.InputTextWithHint($"##Filter1", "按角色名称搜索...", ref Filters[1], 100, Utils.CensorFlags);

        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, Utils.CellPadding);
        if(ImGui.BeginTable($"##characters", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedSame))
        {
            ImGui.TableSetupColumn("角色名称", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("分配的配置文件", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(" ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableHeadersRow();

            foreach(var x in C.SeenCharacters)
            {
                if(C.Blacklist.Contains(x.Key)) continue;
                if(Filters[1].Length > 0 && !x.Value.ContainsAny(StringComparison.OrdinalIgnoreCase, Filters[1])) continue;

                ImGui.PushID(x.Key.ToString());
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.TextV(Player.CID == x.Key ? ImGuiColors.HealerGreen : null, $"{Censor.Character(x.Value)}");
                ImGui.TableNextColumn();

                var currentProfile = C.ProfilesL.FirstOrDefault(z => z.Characters.Contains(x.Key));
                ImGuiEx.SetNextItemFullWidth();
                if(ImGui.BeginCombo($"selProfile", currentProfile?.CensoredName ?? "- 不分配 -", C.ComboSize))
                {
                    if (ImGui.Selectable("- 不分配 -"))
                    {
                        C.ProfilesL.Each(z => z.Characters.Remove(x.Key));
                    }
                    ImGui.SetNextItemWidth(350f);
                    ImGui.InputTextWithHint($"##selProfileFltr", "筛选...", ref Filters[2], 100, Utils.CensorFlags);
                    foreach(var profile in C.ProfilesL)
                    {
                        if(Filters[2].Length > 0 && !profile.Name.Contains(Filters[2], StringComparison.OrdinalIgnoreCase)) continue;
                        if(currentProfile == profile && ImGui.IsWindowAppearing()) ImGui.SetScrollHereY();
                        if(ImGui.Selectable($"{profile.CensoredName}##{profile.GUID}", currentProfile == profile))
                        {
                            if(profile.IsStaticExists() && (currentProfile == null || currentProfile.IsStaticExists()))
                            {
                                P.ForceUpdate = true;
                            }
                            profile.SetCharacter(x.Key);
                        }
                    }
                    ImGui.EndCombo();
                }

                ImGui.TableNextColumn();

                if(ImGuiEx.IconButton(FontAwesomeIcon.Ban))
                {
                    C.Blacklist.Add(x.Key);
                    C.ProfilesL.Each(z => z.Characters.Remove(x.Key));
                }
                ImGuiEx.Tooltip($"将{Censor.Character(x.Value)}加入黑名单。这将阻止它出现在配置文件分配中。这也将撤消{Censor.Character(x.Value)}的配置文件分配。");
                ImGui.SameLine();

                if(ImGuiEx.IconButton(FontAwesomeIcon.Trash, enabled: ImGuiEx.Ctrl))
                {
                    new TickScheduler(() => C.SeenCharacters.Remove(x));
                    C.ProfilesL.Each(z => z.Characters.Remove(x.Key));
                }
                ImGuiEx.Tooltip($"按住CTRL键并单击可删除有关{x.Value}的信息。这也将撤消对该角色的配置文件分配，但一旦您重新登记该角色，{x.Value}将再次在插件中注册。");

                ImGui.PopID();
            }

            foreach(var x in C.Blacklist)
            {
                var name = C.SeenCharacters.TryGetValue(x, out var n) ? n : $"{x:X16}";
                if(Filters[1].Length > 0 && !name.ContainsAny(StringComparison.OrdinalIgnoreCase, Filters[1])) continue;
                ImGui.PushID(x.ToString());
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.TextV(ImGuiColors.DalamudGrey3, $"{Censor.Character(name)}");
                ImGui.TableNextColumn();

                ImGui.TableNextColumn();
                if(ImGuiEx.IconButton(FontAwesomeIcon.ArrowCircleUp))
                {
                    var item = x;
                    new TickScheduler(() => C.Blacklist.Remove(item));
                }
                ImGuiEx.Tooltip("将该角色从黑名单中移出");
                ImGui.PopID();
            }

            ImGui.EndTable();
        }
        ImGui.PopStyleVar();
    }
}
