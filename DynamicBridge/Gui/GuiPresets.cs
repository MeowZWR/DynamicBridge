﻿using DynamicBridge.Configuration;
using DynamicBridge.IPC;
using ECommons.ExcelServices;
using Newtonsoft.Json;
using System.Data;
using System.Windows.Forms;

namespace DynamicBridge.Gui
{
    public static class GuiPresets
    {
        static string[] Filters = ["", "", "", "", "", "", "", "", "", "", "", "", "", "", ""];
        static bool[] OnlySelected = new bool[15];
        public static void Draw()
        {
            UI.ProfileSelectorCommon();
            if (Utils.Profile(UI.CurrentCID) != null)
            {
                var Profile = Utils.Profile(UI.CurrentCID);
                Profile.GetPresetsListUnion().Each(f => f.RemoveAll(x => x == null));
                if (ImGui.Button("Add new preset"))
                {
                    Profile.Presets.Add(new());
                }
                ImGui.SameLine();
                if (ImGui.Button("Paste from clipboard"))
                {
                    try
                    {
                        Profile.Presets.Add(JsonConvert.DeserializeObject<Preset>(Clipboard.GetText()));
                    }
                    catch (Exception e)
                    {
                        Notify.Error(e.Message);
                    }
                }
                DrawPresets(Profile, Profile.Presets);

                foreach(var x in Profile.PresetFolders)
                {
                    ImGui.PushID(x.GUID);
                    if (ImGui.CollapsingHeader($"{x.Name}"))
                    {
                        DrawPresets(Profile, x.Presets);
                    }
                    ImGui.PopID();
                }
            }
        }

        static void DrawPresets(Profile currentProfile, List<Preset> presetList)
        {
            var cnt = 3;
            if (C.EnableHonorific) cnt++;
            if (C.EnablePalette) cnt++;
            if (C.EnableCustomize) cnt++;
            if (C.EnableGlamourer) cnt++;
            if (ImGui.BeginTable("##presets", cnt, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable))
            {
                ImGui.TableSetupColumn("  ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort);
                ImGui.TableSetupColumn("Name");
                if(C.EnableGlamourer) ImGui.TableSetupColumn("Glamourer");
                if (C.EnablePalette) ImGui.TableSetupColumn("Palette+");
                if (C.EnableCustomize) ImGui.TableSetupColumn("Customize+");
                if (C.EnableHonorific) ImGui.TableSetupColumn("Honorific");
                ImGui.TableSetupColumn(" ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableHeadersRow();

                var isStaticExists = currentProfile.IsStaticExists();

                for (int i = 0; i < presetList.Count; i++)
                {
                    var filterCnt = 0;

                    void FiltersSelection()
                    {
                        ImGui.SetWindowFontScale(0.8f);
                        ImGuiEx.SetNextItemFullWidth();
                        ImGui.InputTextWithHint($"##fltr{filterCnt}", "Filter...", ref Filters[filterCnt], 50);
                        ImGui.Checkbox($"Only selected##{filterCnt}", ref OnlySelected[filterCnt]);
                        ImGui.SetWindowFontScale(1f);
                        ImGui.Separator();
                    }

                    var preset = presetList[i];

                    ImGui.PushID(preset.GUID);

                    if (isStaticExists)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, preset.IsStatic ? ImGuiColors.DalamudOrange : ImGuiColors.DalamudGrey);
                    }

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    //Sorting
                    if (ImGuiEx.CheckboxBullet("##static", ref preset.IsStatic))
                    {
                        if (preset.IsStatic)
                        {
                            currentProfile.SetStatic(preset);
                        }
                        P.ForceUpdate = true;
                    }
                    ImGuiEx.Tooltip("Set this preset as static, applying it unconditionally on this character disregarding any rules.");
                    ImGui.SameLine();
                    if (ImGui.ArrowButton("up", ImGuiDir.Up) && i > 0)
                    {
                        (presetList[i-1], presetList[i]) = (presetList[i], presetList[i-1]);
                    }
                    ImGui.SameLine();
                    if (ImGui.ArrowButton("down", ImGuiDir.Down) && i < presetList.Count - 2)
                    {
                        (presetList[i+1], presetList[i]) = (presetList[i], presetList[i+1]);
                    }

                    ImGui.TableNextColumn();

                    //name

                    var isEmpty = preset.Name == "";
                    var isNonUnique = currentProfile.GetPresetsUnion().Count(x => x.Name == preset.Name) > 1;
                    if (isEmpty)
                    {
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGuiEx.Text(ImGuiColors.DalamudRed, Utils.IconWarning);
                        ImGui.PopFont();
                        ImGuiEx.Tooltip("Name can not be empty");
                        ImGui.SameLine();
                    }
                    else if (isNonUnique)
                    {
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGuiEx.Text(ImGuiColors.DalamudRed, Utils.IconWarning);
                        ImGui.PopFont();
                        ImGuiEx.Tooltip("Name must be unique");
                        ImGui.SameLine();
                    }
                    ImGuiEx.SetNextItemFullWidth();
                    ImGui.InputTextWithHint("##name", "Preset name", ref preset.Name, 100);


                    //Glamourer
                    {
                        if (C.EnableGlamourer)
                        {
                            ImGui.TableNextColumn();
                            ImGuiEx.SetNextItemFullWidth();
                            if (ImGui.BeginCombo("##glamour", ((string[])[.. preset.Glamourer, .. preset.ComplexGlamourer]).PrintRange(out var fullList, "- None -")))
                            {
                                FiltersSelection();

                                // normal
                                {
                                    var designs = GlamourerManager.GetDesigns().OrderBy(x => x.Name);
                                    foreach (var x in designs)
                                    {
                                        var name = x.Name;
                                        if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                        if (OnlySelected[filterCnt] && !preset.Glamourer.Contains(name)) continue;
                                        ImGuiEx.CollectionCheckbox($"{name}##{x.Identifier}", x.Name, preset.Glamourer);
                                    }
                                    foreach (var x in preset.Glamourer)
                                    {
                                        if (designs.Any(d => d.Name == x)) continue;
                                        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                                        ImGuiEx.CollectionCheckbox($"{x}", x, preset.Glamourer, false, true);
                                        ImGui.PopStyleColor();
                                    }
                                }

                                //complex
                                {
                                    var designs = C.ComplexGlamourerEntries;
                                    ImGui.PushStyleColor(ImGuiCol.Text, EColor.YellowBright);
                                    foreach (var x in designs)
                                    {
                                        var name = x.Name;
                                        if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                        if (OnlySelected[filterCnt] && !preset.ComplexGlamourer.Contains(name)) continue;
                                        FontAwesome.Layers.ImGuiText("Complex Glamourer entry");
                                        ImGui.SameLine();
                                        ImGuiEx.CollectionCheckbox($"{name}##{x.GUID}", x.Name, preset.ComplexGlamourer);
                                    }
                                    ImGui.PopStyleColor();
                                    foreach (var x in preset.ComplexGlamourer)
                                    {
                                        if (designs.Any(d => d.Name == x)) continue;
                                        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                                        FontAwesome.Layers.ImGuiText("Complex Glamourer entry");
                                        ImGuiEx.CollectionCheckbox($"{x}", x, preset.ComplexGlamourer, false, true);
                                        ImGui.PopStyleColor();
                                    }
                                }

                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.RandomNotice + fullList);
                            filterCnt++;
                        }
                    }


                    //palette+
                    {
                        if (C.EnablePalette)
                        {
                            ImGui.TableNextColumn();
                            ImGuiEx.SetNextItemFullWidth();
                            if (ImGui.BeginCombo("##palette", preset.Palette.PrintRange(out var fullList, "- None -")))
                            {
                                FiltersSelection();
                                var palettes = PalettePlusManager.GetPalettes().OrderBy(x => x.Name);
                                var index = 0;
                                foreach (var x in palettes)
                                {
                                    index++;
                                    ImGui.PushID(index);
                                    var name = x.Name;
                                    if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                    if (OnlySelected[filterCnt] && !preset.Palette.Contains(name)) continue;
                                    ImGuiEx.CollectionCheckbox($"{name}", x.Name, preset.Palette);
                                    ImGui.PopID();
                                }
                                foreach (var x in preset.Palette)
                                {
                                    if (palettes.Any(d => d.Name == x)) continue;
                                    ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                                    ImGuiEx.CollectionCheckbox($"{x}", x, preset.Palette, false, true);
                                    ImGui.PopStyleColor();
                                }
                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.RandomNotice + fullList);
                            filterCnt++;
                        }
                    }


                    //customize+
                    {
                        if (C.EnableCustomize)
                        {
                            ImGui.TableNextColumn();
                            ImGuiEx.SetNextItemFullWidth();
                            if (ImGui.BeginCombo("##customize", preset.Customize.PrintRange(out var fullList, "- None -")))
                            {
                                FiltersSelection();
                                var profiles = CustomizePlusManager.GetProfiles(currentProfile.Name.Split("@")[0]).OrderBy(x => x.Name);
                                var index = 0;
                                foreach (var x in profiles)
                                {
                                    index++;
                                    ImGui.PushID(index);
                                    var name = x.Name;
                                    if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                    if (OnlySelected[filterCnt] && !preset.Customize.Contains(name)) continue;
                                    ImGuiEx.CollectionCheckbox($"{name}", x.Name, preset.Customize);
                                    ImGui.PopID();
                                }
                                foreach (var x in preset.Customize)
                                {
                                    if (profiles.Any(d => d.Name == x)) continue;
                                    ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                                    ImGuiEx.CollectionCheckbox($"{x}", x, preset.Customize, false, true);
                                    ImGui.PopStyleColor();
                                }
                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.RandomNotice + fullList);
                            filterCnt++;
                        }
                    }


                    //Honorific
                    {
                        if (C.EnableHonorific)
                        {
                            ImGui.TableNextColumn();
                            ImGuiEx.SetNextItemFullWidth();
                            if (ImGui.BeginCombo("##honorific", preset.Honorific.PrintRange(out var fullList, "- None -")))
                            {
                                FiltersSelection();
                                var titles = HonorificManager.GetTitleData(currentProfile.Name.Split("@")[0], ExcelWorldHelper.Get(currentProfile.Name.Split("@")[1]).RowId).OrderBy(x => x.Title);
                                var index = 0;
                                foreach (var x in titles)
                                {
                                    index++;
                                    ImGui.PushID(index);
                                    var name = x.Title;
                                    if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                    if (OnlySelected[filterCnt] && !preset.Honorific.Contains(name)) continue;
                                    if (x.Color != null) ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(x.Color.Value, 1f));
                                    ImGuiEx.CollectionCheckbox($"{name}", x.Title, preset.Honorific);
                                    if (x.Color != null) ImGui.PopStyleColor();
                                    ImGui.PopID();
                                }
                                foreach (var x in preset.Honorific)
                                {
                                    if (titles.Any(d => d.Title == x)) continue;
                                    ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                                    ImGuiEx.CollectionCheckbox($"{x}", x, preset.Honorific, false, true);
                                    ImGui.PopStyleColor();
                                }
                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.RandomNotice + fullList);
                            filterCnt++;
                        }
                    }

                    ImGui.TableNextColumn();

                    //Delete
                    if (ImGuiEx.IconButton(FontAwesomeIcon.Copy))
                    {
                        Safe(() => Clipboard.SetText(JsonConvert.SerializeObject(preset)));
                    }
                    ImGui.SameLine();
                    if (ImGuiEx.IconButton(FontAwesomeIcon.Trash) && ImGui.GetIO().KeyCtrl)
                    {
                        new TickScheduler(() => presetList.RemoveAll(x => x.GUID == preset.GUID));
                    }
                    ImGuiEx.Tooltip("Hold CTRL+Click to delete");

                    if (isStaticExists) ImGui.PopStyleColor();
                    ImGui.PopID();
                }
                ImGui.EndTable();
            }
        }
    }
}