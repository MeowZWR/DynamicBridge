using Dalamud.Interface.Components;
using DynamicBridge.Configuration;
using DynamicBridge.IPC.Glamourer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Gui;
public static unsafe class ComplexGlamourer
{
    private static string Filter = "";
    private static bool OnlySelected = false;
    public static void Draw()
    {
        if(!C.EnableGlamourer)
        {
            ImGuiEx.Text(EColor.RedBright, "Glamourer在插件设置中被禁用。功能不可用。");
            return;
        }
        ImGuiEx.TextWrapped($"在此处，您可以创建一个基于Glamourer模板的叠加组合设计。一旦生效，它们将按顺序被依次应用（比如在Glamourer里第一个模板规则是仅应用外貌，第二个模板仅应用装备，他们将组合起来），越靠下优先级越高。在规则里，可以同时选择层组设计和Glamourer模板。");
        if(ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Plus, "添加新条目"))
        {
            C.ComplexGlamourerEntries.Add(new());
        }
        foreach(var gEntry in C.ComplexGlamourerEntries)
        {
            ImGui.PushID(gEntry.GUID);
            if(ImGui.CollapsingHeader($"{gEntry.Name}###entry"))
            {
                ImGuiEx.TextV($"1. 命名层组设计：");
                ImGui.SameLine();
                ImGuiEx.SetNextItemFullWidth();
                ImGui.InputText($"##name", ref gEntry.Name, 100);
                ImGuiEx.TextV($"2. 选择 Glamourer 设计:");
                ImGui.SameLine();
                if(ImGui.BeginCombo("##glamour", gEntry.Designs.Select(P.GlamourerManager.TransformName).PrintRange(out var fullList, "- 未选择 -"), C.ComboSize))
                {
                    if(ImGui.IsWindowAppearing()) Utils.ResetCaches();
                    FiltersSelection();
                    var designs = P.GlamourerManager.GetDesigns().OrderBy(x => x.Name);
                    foreach(var x in designs)
                    {
                        var name = x.Name;
                        var id = x.Identifier.ToString();
                        var transformedName = P.GlamourerManager.TransformName(id);
                        if(Filter.Length > 0 && !transformedName.Contains(Filter, StringComparison.OrdinalIgnoreCase)) continue;
                        if(OnlySelected && !gEntry.Designs.Contains(id)) continue;
                        ImGuiEx.CollectionCheckbox($"{transformedName}##{x.Identifier}", id, gEntry.Designs);
                    }
                    foreach(var x in gEntry.Designs)
                    {
                        if(designs.Any(d => d.Identifier.ToString() == x)) continue;
                        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                        ImGuiEx.CollectionCheckbox($"{x}", x, gEntry.Designs, false, true);
                        ImGui.PopStyleColor();
                    }
                    ImGui.EndCombo();
                }
                ImGuiEx.Text($"3. 更改顺序（如果需要）");
                for(var i = 0; i < gEntry.Designs.Count; i++)
                {
                    var design = gEntry.Designs[i];
                    ImGui.PushID(design);
                    if(ImGui.ArrowButton("up", ImGuiDir.Up) && i > 0)
                    {
                        (gEntry.Designs[i - 1], gEntry.Designs[i]) = (gEntry.Designs[i], gEntry.Designs[i - 1]);
                    }
                    ImGui.SameLine();
                    if(ImGui.ArrowButton("down", ImGuiDir.Down) && i < gEntry.Designs.Count - 1)
                    {
                        (gEntry.Designs[i + 1], gEntry.Designs[i]) = (gEntry.Designs[i], gEntry.Designs[i + 1]);
                    }
                    ImGui.SameLine();
                    ImGuiEx.Text($"{P.GlamourerManager.TransformName(design)}");
                    ImGui.PopID();
                }
                if (ImGuiEx.ButtonCtrl("删除"))
                {
                    new TickScheduler(() => C.ComplexGlamourerEntries.RemoveAll(x => x.GUID == gEntry.GUID));
                }
            }
            ImGui.PopID();
        }

        void FiltersSelection()
        {
            ImGui.SetWindowFontScale(0.8f);
            ImGuiEx.SetNextItemFullWidth();
            ImGui.InputTextWithHint($"##fltr", "筛选...", ref Filter, 50);
            ImGui.Checkbox($"仅显示已选择项", ref OnlySelected);
            ImGui.SetWindowFontScale(1f);
            ImGui.Separator();
        }
    }
}
