﻿using Dalamud.Interface.Components;
using DynamicBridge.Configuration;
using DynamicBridge.IPC.Customize;
using DynamicBridge.IPC.Glamourer;
using DynamicBridge.IPC.Honorific;
using ECommons;
using ECommons.Configuration;
using ECommons.ExcelServices;
using Newtonsoft.Json;
using System.Data;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace DynamicBridge.Gui
{
    public static class GuiPresets
    {
        static string[] Filters = ["", "", "", "", "", "", "", "", "", "", "", "", "", "", ""];
        static bool[] OnlySelected = new bool[15];
        static string CurrentDrag = null;
        static bool Focus = false;
        static string Open = null;
        public static void DrawUser()
        {
            if (UI.Profile != null)
            {
                DrawProfile(UI.Profile, true, true);
            }
            else
            {
                UI.ProfileSelectorCommon();
            }
        }

        public static void DrawGlobal()
        {
            ImGuiEx.TextWrapped($"Global presets are available for use on each of your characters.");
            DrawProfile(C.GlobalProfile, false, false);
        }

        static void DrawProfile(Profile Profile, bool drawFallback, bool drawHeader)
        {
            Profile.GetPresetsListUnion().Each(f => f.RemoveAll(x => x == null));
            
            void Buttons()
            {
                if (ImGuiEx.IconButton(FontAwesomeIcon.Plus))
                {
                    if (Open != null && Profile.PresetsFolders.TryGetFirst(x => x.GUID == Open, out var open))
                    {
                        open.Presets.Add(new());
                    }
                    else
                    {
                        Profile.Presets.Add(new());
                    }
                }
                ImGuiEx.Tooltip("Add new empty preset into default or focused folder");
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.Paste))
                {
                    try
                    {
                        var str = (EzConfig.DefaultSerializationFactory.Deserialize<Preset>(Paste()));
                        if (Open != null && Profile.PresetsFolders.TryGetFirst(x => x.GUID == Open, out var open))
                        {
                            open.Presets.Add(str);
                        }
                        else
                        {
                            Profile.Presets.Add(str);
                        }
                    }
                    catch (Exception e)
                    {
                        Notify.Error(e.Message);
                    }
                }
                ImGuiEx.Tooltip($"Paste previously copied preset from clipboard");
                ImGui.SameLine();

                if (ImGuiEx.IconButton(FontAwesomeIcon.FolderPlus))
                {
                    Profile.PresetsFolders.Add(new() { Name = $"Preset folder {Profile.PresetsFolders.Count + 1}" });
                }
                ImGuiEx.Tooltip("Add new preset folder");

                ImGui.SameLine();

            }

            void RightButtons()
            {
                /*ImGui.PushFont(UiBuilder.IconFont);
                ImGuiEx.ButtonCheckbox(FontAwesomeIcon.SearchPlus.ToIconString(), ref Focus);
                ImGui.PopFont();
                ImGuiEx.Tooltip("Toggle focus mode. While focus mode active, only one selected folder will be visible.");
                ImGui.SameLine();*/
                UI.ForceUpdateButton();
                ImGui.SameLine();
            }

            if(drawHeader) UI.ProfileSelectorCommon(Buttons, RightButtons);

            string newOpen = null;

            if (!Focus || Open == "" || Open == null)
            {
                if (ImGui.TreeNodeEx($"Main presets{UI.BunchOfSpaces}##global", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    newOpen = "";
                    DragDrop.AcceptFolderDragDrop(Profile, Profile.Presets, ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoDrawDefaultRect);
                    DrawPresets(Profile, Profile.Presets, out var postAction);
                    ImGui.TreePop();
                    postAction?.Invoke();
                }
                else
                {
                    DragDrop.AcceptFolderDragDrop(Profile, Profile.Presets);
                }
            }

            for (int presetFolderIndex = 0; presetFolderIndex < Profile.PresetsFolders.Count; presetFolderIndex++)
            {
                var presetFolder = Profile.PresetsFolders[presetFolderIndex];
                if (Focus && Open != presetFolder.GUID && Open != null) continue;
                if (presetFolder.HiddenFromSelection)
                {
                    ImGuiEx.RightFloat($"RFCHP{presetFolder.GUID}", () => ImGuiEx.TextV(ImGuiColors.DalamudGrey, "Hidden from rules"));
                }
                if (ImGui.TreeNodeEx($"{presetFolder.Name}{UI.BunchOfSpaces}###presetfolder{presetFolder.GUID}"))
                {
                    newOpen = presetFolder.GUID;
                    CollapsingHeaderClicked();
                    DragDrop.AcceptFolderDragDrop(Profile, presetFolder.Presets, ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoDrawDefaultRect);
                    DrawPresets(Profile, presetFolder.Presets, out var postAction, presetFolder.GUID);
                    ImGui.TreePop();
                    postAction?.Invoke();
                }
                else
                {
                    CollapsingHeaderClicked();
                    DragDrop.AcceptFolderDragDrop(Profile, presetFolder.Presets);
                }
                void CollapsingHeaderClicked()
                {
                    if (ImGui.IsItemHovered() && ImGui.IsItemClicked(ImGuiMouseButton.Right)) ImGui.OpenPopup($"Folder{presetFolder.GUID}");
                    if (ImGui.BeginPopup($"Folder{presetFolder.GUID}"))
                    {
                        ImGuiEx.SetNextItemWidthScaled(150f);
                        ImGui.InputTextWithHint("##name", "Folder name", ref presetFolder.Name, 200);
                        if (ImGui.Selectable("Export to clipboard"))
                        {
                            Copy(EzConfig.DefaultSerializationFactory.Serialize(presetFolder, false));
                        }
                        if (presetFolder.HiddenFromSelection)
                        {
                            if (ImGui.Selectable("Show in Rules section")) presetFolder.HiddenFromSelection = false;
                        }
                        else
                        {
                            if (ImGui.Selectable("Hide from Rules section")) presetFolder.HiddenFromSelection = true;
                        }
                        if (ImGui.Selectable("Move up", false, ImGuiSelectableFlags.DontClosePopups) && presetFolderIndex > 0)
                        {
                            (Profile.PresetsFolders[presetFolderIndex], Profile.PresetsFolders[presetFolderIndex - 1]) = (Profile.PresetsFolders[presetFolderIndex - 1], Profile.PresetsFolders[presetFolderIndex]);
                        }
                        if (ImGui.Selectable("Move down", false, ImGuiSelectableFlags.DontClosePopups) && presetFolderIndex < Profile.PresetsFolders.Count - 1)
                        {
                            (Profile.PresetsFolders[presetFolderIndex], Profile.PresetsFolders[presetFolderIndex + 1]) = (Profile.PresetsFolders[presetFolderIndex + 1], Profile.PresetsFolders[presetFolderIndex]);
                        }
                        ImGui.Separator();

                        if (ImGui.BeginMenu("Delete folder..."))
                        {
                            if (ImGui.Selectable("...and move profiles to default folder (Hold CTRL)"))
                            {
                                if (ImGuiEx.Ctrl)
                                {
                                    new TickScheduler(() =>
                                    {
                                        foreach (var x in presetFolder.Presets)
                                        {
                                            Profile.Presets.Add(x);
                                        }
                                        Profile.PresetsFolders.Remove(presetFolder);
                                    });
                                }
                            }
                            if (ImGui.Selectable("...and delete included profiles (Hold CTRL+SHIFT)"))
                            {
                                if (ImGuiEx.Ctrl && ImGuiEx.Shift)
                                {
                                    new TickScheduler(() => Profile.PresetsFolders.Remove(presetFolder));
                                }
                            }
                            ImGui.EndMenu();
                        }

                        ImGui.EndPopup();
                    }
                    else
                    {
                        if (!ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                        {
                            ImGuiEx.Tooltip("Right-click to open context menu");
                        }
                    }
                }
            }
            if (drawFallback)
            {
                if (ImGui.TreeNode($"Fallback preset" + UI.BunchOfSpaces))
                {
                    DrawPresets(Profile, [Profile.FallbackPreset], out _, $"FallbackPreset-8c680b09-acd0-43ab-9413-26a4e38841fc", true);
                    Open = newOpen;
                    ImGui.TreePop();
                }
            }
        }

        static void DrawPresets(Profile currentProfile, List<Preset> presetList, out Action postAction, string extraID = "", bool isFallback = false)
        {
            postAction = null;
            var cnt = 3;
            if (C.EnableHonorific) cnt++;
            if (C.EnableCustomize) cnt++;
            if (C.EnableGlamourer) cnt++;
            List<(Vector2 RowPos, Vector2 ButtonPos, Action BeginDraw, Action AcceptDraw)> MoveCommands = [];
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, Utils.CellPadding);
            if (ImGui.BeginTable($"##presets{extraID}", cnt, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable))
            {
                ImGui.TableSetupColumn("  ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort);
                ImGui.TableSetupColumn("Name");
                if(C.EnableGlamourer) ImGui.TableSetupColumn("Glamourer");
                if (C.EnableCustomize) ImGui.TableSetupColumn("Customize+");
                if (C.EnableHonorific) ImGui.TableSetupColumn("Honorific");
                ImGui.TableSetupColumn(" ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableHeadersRow();

                var isStaticExists = currentProfile.IsStaticExists() && !isFallback;

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
                    if(CurrentDrag == preset.GUID)
                    {
                        var col = GradientColor.Get(EColor.Green, EColor.Green with { W = EColor.Green.W/4}, 500).ToUint();
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, col);
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, col);
                    }
                    ImGui.TableNextColumn();

                    //Sorting
                    if (isFallback)
                    {
                        ImGuiEx.TextV(" ");
                        ImGuiEx.HelpMarker($"Values from this preset will be used as default in current profile.");
                    }
                    else
                    {
                        var rowPos = ImGui.GetCursorPos();
                        if (ImGui.RadioButton("##static", preset.IsStatic))
                        {
                            preset.IsStatic = !preset.IsStatic;
                            if (preset.IsStatic)
                            {
                                currentProfile.SetStatic(preset);
                            }
                            P.ForceUpdate = true;
                        }
                        ImGuiEx.Tooltip("Set this preset as static, applying it unconditionally on this character disregarding any rules.");
                        ImGui.SameLine();
                        ImGui.PushFont(UiBuilder.IconFont);
                        var cur = ImGui.GetCursorPos();
                        var size = ImGuiHelpers.GetButtonSize(FontAwesomeIcon.ArrowsUpDownLeftRight.ToIconString());
                        ImGui.Dummy(size);
                        ImGui.PopFont();
                        var moveIndex = i;
                        MoveCommands.Add((rowPos, cur, delegate
                        {
                            ImGui.PushFont(UiBuilder.IconFont);
                            ImGui.Button($"{FontAwesomeIcon.ArrowsUpDownLeftRight.ToIconString()}##Move{preset.GUID}");
                            ImGui.PopFont();
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
                            }
                            if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceNoPreviewTooltip))
                            {
                                ImGuiDragDrop.SetDragDropPayload("MovePreset", preset.GUID);
                                CurrentDrag = preset.GUID;
                                InternalLog.Verbose($"DragDropSource = {preset.GUID}");
                                ImGui.EndDragDropSource();
                            }
                            else if (CurrentDrag == preset.GUID)
                            {
                                InternalLog.Verbose($"Current drag reset!");
                                CurrentDrag = null;
                            }
                        }, delegate { DragDrop.AcceptProfileDragDrop(currentProfile, presetList, moveIndex); }
                        ));

                        ImGui.SameLine();
                        if (ImGuiEx.IconButton(FontAwesomeIcon.CaretDown))
                        {
                            ImGui.OpenPopup($"MoveTo##{preset.GUID}");
                        }
                        if (ImGui.BeginPopup($"MoveTo##{preset.GUID}"))
                        {
                            if (ImGui.Selectable("- Main folder -", currentProfile.Presets.Any(x => x.GUID == preset.GUID)))
                            {
                                DragDrop.MovePresetToList(currentProfile, preset.GUID, currentProfile.Presets);
                            }
                            foreach (var x in currentProfile.PresetsFolders)
                            {
                                if (ImGui.Selectable($"{x.Name}##{x.GUID}", x.Presets.Any(x => x.GUID == preset.GUID)))
                                {
                                    DragDrop.MovePresetToList(currentProfile, preset.GUID, x.Presets);
                                }
                            }
                            ImGui.EndPopup();
                        }
                    }
                    
                    ImGui.TableNextColumn();

                    //name
                    if (isFallback)
                    {
                        ImGuiEx.TextV("Base Preset");
                    }
                    else
                    {
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
                        ImGui.InputTextWithHint("##name", "Preset name", ref preset.Name, 100, Utils.CensorFlags);
                    }


                    //Glamourer
                    {
                        if (C.EnableGlamourer)
                        {
                            ImGui.TableNextColumn();
                            ImGuiEx.SetNextItemFullWidth();
                            if (ImGui.BeginCombo("##glamour", ((string[])[.. preset.Glamourer.Select(P.GlamourerManager.TransformName), .. preset.ComplexGlamourer]).PrintRange(out var fullList, "- None -"), C.ComboSize))
                            {
                                if (ImGui.IsWindowAppearing()) Utils.ResetCaches();
                                FiltersSelection();

                                // normal
                                {
                                    var designs = P.GlamourerManager.GetDesigns().OrderBy(x => P.GlamourerManager.TransformName(x.Identifier.ToString()));
                                    foreach (var x in designs)
                                    {
                                        var name = x.Name;
                                        var id = x.Identifier.ToString();
                                        var transformedName = P.GlamourerManager.TransformName(x.Identifier.ToString());
                                        if (C.GlamourerFullPath && currentProfile.Pathes.Count > 0 && !transformedName.StartsWithAny(currentProfile.Pathes)) continue;
                                        if (Filters[filterCnt].Length > 0 && !transformedName.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                        if (OnlySelected[filterCnt] && !preset.Glamourer.Contains(id)) continue;
                                        ImGuiEx.CollectionCheckbox($"{transformedName}##{x.Identifier}", id, preset.Glamourer);
                                    }
                                    foreach (var x in preset.Glamourer)
                                    {
                                        if (designs.Any(d => d.Identifier.ToString() == x)) continue;
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
                                        FontAwesome.Print(null, FontAwesome.Layers);
                                        ImGui.SameLine();
                                        ImGuiEx.Text("Layered Design");
                                        ImGui.SameLine();
                                        ImGuiEx.CollectionCheckbox($"{name}##{x.GUID}", x.Name, preset.ComplexGlamourer);
                                    }
                                    ImGui.PopStyleColor();
                                    foreach (var x in preset.ComplexGlamourer)
                                    {
                                        if (designs.Any(d => d.Name == x)) continue;
                                        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                                        FontAwesome.Print(null, FontAwesome.Layers);
                                        ImGui.SameLine();
                                        ImGuiEx.Text("Layered Design");
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


                    //customize+
                    {
                        if (C.EnableCustomize)
                        {
                            ImGui.TableNextColumn();
                            ImGuiEx.SetNextItemFullWidth();
                            if (ImGui.BeginCombo("##customize", preset.Customize.Select(P.CustomizePlusManager.TransformName).PrintRange(out var fullList, "- None -"), C.ComboSize))
                            {
                                FiltersSelection();
                                var profiles = P.CustomizePlusManager.GetProfiles(currentProfile.Characters.Select(Utils.GetCharaNameFromCID).Select(z => z.Split("@")[0])).OrderBy(x => P.CustomizePlusManager.TransformName(x.ID.ToString()));
                                var index = 0;
                                foreach (var x in profiles)
                                {
                                    index++;
                                    ImGui.PushID(index);
                                    var name = P.CustomizePlusManager.TransformName(x.ID.ToString());
                                    if (C.GlamourerFullPath && currentProfile.Pathes.Count > 0 && !name.StartsWithAny(currentProfile.Pathes)) continue;
                                    if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                    if (OnlySelected[filterCnt] && !preset.Customize.Contains(x.ID.ToString())) continue;
                                    ImGuiEx.CollectionCheckbox($"{name}", x.ID.ToString(), preset.Customize);
                                    ImGui.PopID();
                                }
                                foreach (var x in preset.Customize)
                                {
                                    if (profiles.Any(d => d.ID.ToString() == x)) continue;
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
                            if (ImGui.BeginCombo("##honorific", preset.Honorific.PrintRange(out var fullList, "- None -"), C.ComboSize))
                            {
                                FiltersSelection();
                                var titles = HonorificManager.GetTitleData(currentProfile.Characters).OrderBy(x => x.Title);
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
                        Safe(() => Copy(JsonConvert.SerializeObject(preset)));
                    }
                    ImGuiEx.Tooltip("Copy to clipboard");
                    if (!isFallback)
                    {
                        ImGui.SameLine();
                        if (ImGuiEx.IconButton(FontAwesomeIcon.Trash) && ImGui.GetIO().KeyCtrl)
                        {
                            new TickScheduler(() => presetList.RemoveAll(x => x.GUID == preset.GUID));
                        }
                        ImGuiEx.Tooltip("Hold CTRL+Click to delete");
                    }

                    if (isStaticExists) ImGui.PopStyleColor();
                    ImGui.PopID();
                }
                ImGui.EndTable();
                postAction = () =>
                {
                    foreach (var x in MoveCommands)
                    {
                        ImGui.SetCursorPos(x.ButtonPos);
                        x.BeginDraw();
                        x.AcceptDraw();
                        ImGui.SetCursorPos(x.RowPos);
                        ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, ImGuiHelpers.GetButtonSize(" ").Y));
                        x.AcceptDraw();
                    }
                };
            }
            ImGui.PopStyleVar();
        }
    }
}