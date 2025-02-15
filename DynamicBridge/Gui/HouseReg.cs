using Dalamud.Interface.Components;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Gui
{
    public static unsafe class HouseReg
    {
        public static void Draw()
        {
            ImGuiEx.TextWrapped($"在此处，您可以登记住宅。登记后可以在“动态规则”选项卡中选择它作为条件。");
            var CurrentHouse = HousingManager.Instance()->GetCurrentHouseId();
            if(CurrentHouse > 0)
            {
                ImGuiEx.Text($"当前住宅：{Censor.Hide($"{CurrentHouse:X16}")}");
                if (!C.Houses.TryGetFirst(x => x.ID == CurrentHouse, out var record))
                {
                    if(ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Home, "登记此住宅"))
                    {
                        C.Houses.Add(new() { ID = CurrentHouse, Name = Utils.GetHouseDefaultName() });
                    }
                }
            }
            else
            {
                ImGuiEx.Text($"您未处于住宅中");
            }
            if(ImGui.BeginTable("##houses", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("名称", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("ID");
                ImGui.TableSetupColumn(" ");
                ImGui.TableHeadersRow();
                foreach(var x in C.Houses)
                {
                    ImGui.PushID(x.GUID);
                    var col = x.ID == CurrentHouse;
                    if(col) ImGui.PushStyleColor(ImGuiCol.Text, EColor.GreenBright);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    ImGuiEx.SetNextItemFullWidth();
                    ImGui.InputText("##name", ref x.Name, 100, Utils.CensorFlags);

                    ImGui.TableNextColumn();
                    ImGuiEx.Text($"{Censor.Hide($"{x.ID:X16}")}");

                    ImGui.TableNextColumn();
                    if(ImGuiEx.IconButton(FontAwesomeIcon.Trash, enabled: ImGuiEx.Ctrl))
                    {
                        new TickScheduler(() => C.Houses.RemoveAll(z => z.GUID == x.GUID));
                    }
                    ImGuiEx.Tooltip($"按住CTRL+点击来删除");

                    if(col) ImGui.PopStyleColor();
                    ImGui.PopID();
                }
                ImGui.EndTable();
            }
        }
    }
}
