using ECommons.GameHelpers;

namespace DynamicBridge.Gui;
public static class GuiCharacters
{
    private static string[] Filters = ["", "", "", ""];
    public static void Draw()
    {
        ImGuiEx.SetNextItemFullWidth();
        ImGuiEx.InputWithRightButtonsArea(() =>
        {
            ImGui.InputTextWithHint($"##Filter1", "按角色名称搜索...", ref Filters[1], 100, Utils.CensorFlags);
        }, () =>
        {
            if(ImGuiEx.IconButton(FontAwesomeIcon.UserPlus))
            {
                ImGui.OpenPopup("NewChara");
            }
            ImGuiEx.Tooltip("手动注册新角色");
        });

        if(ImGui.BeginPopup("NewChara"))
        {
            ImGui.SetNextItemWidth(150f);
            ImGui.InputTextWithHint("##name2", "角色名称@世界", ref NewChara, 50);
            ImGui.SetNextItemWidth(150f);
            ImGui.InputTextWithHint("##cid", "角色/内容 ID", ref NewCID, 50);
            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.UserPlus, "添加新角色"))
            {
                if(NewChara.Length > 2 && NewChara.Split(" ").Length == 2 && NewChara.Contains('@') && ulong.TryParse(NewCID, out var cid) && cid > 0)
                {
                    if(C.SeenCharacters.ContainsKey(cid))
                    {
                        Notify.Error("此角色 ID 已存在");
                    }
                    else if(C.SeenCharacters.Values.Select(x => x.ToLower()).Contains(NewChara.ToLower()))
                    {
                        Notify.Error("此角色名称已存在");
                    }
                    else
                    {
                        C.SeenCharacters[cid] = NewChara;
                        NewChara = "";
                        NewCID = "";
                        ImGui.CloseCurrentPopup();
                        Notify.Success("角色成功添加");
                    }
                }
                else
                {
                    Notify.Error("无效的名称或角色/内容 ID");
                }
            }
            ImGui.EndPopup();
        }

        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, Utils.CellPadding);
        if(ImGui.BeginTable($"##characters", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("名称");
            ImGui.TableSetupColumn("CID");
            ImGui.TableSetupColumn("分配的配置文件", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(" ");
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
                if(!C.NoNames)
                {
                    ImGuiEx.TextCopy($"{x.Key}");
                }
                else
                {
                    ImGuiEx.Text("被设置隐藏");
                }
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

    private static string NewChara = "";
    private static string NewCID = "";
}
