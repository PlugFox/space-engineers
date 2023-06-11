using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceEngineers.Outpost
{
    partial class Program : MyGridProgram
    {
        // -- BEGIN -- //

        // -- CONFIG -- //
        const string AIR_DEFFENSE_TAG = "[Outpost]";

        // -- ENDPOINTS -- //

        public Program()
        {
            try
            {
                // Установим частоту обновления тиков скрипта
                Runtime.UpdateFrequency = UpdateFrequency.Update100;

                // Установим значения по умолчанию
                var currentGrid = Me.CubeGrid;
                var blocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks, block => block.CubeGrid == currentGrid);
                foreach (var block in blocks)
                {
                    block.CustomName = $"{AIR_DEFFENSE_TAG} {block.DefinitionDisplayNameText}";
                    block.ShowOnHUD = false;
                    block.ShowInInventory = false;
                    block.ShowInTerminal = false;
                    block.ShowInToolbarConfig = false;
                }

                // Включим все функциональные блоки
                var fnBlocks = new List<IMyFunctionalBlock>();
                GridTerminalSystem.GetBlocksOfType<IMyFunctionalBlock>(blocks, block => block.CubeGrid == currentGrid && !(block is IMyCargoContainer));
                foreach (var block in fnBlocks)
                {
                    if (!block.Enabled) block.Enabled = true;
                }

                var turrents = new List<IMyLargeTurretBase>();
                GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(turrents, block => block.CubeGrid == currentGrid);
                foreach (var block in turrents)
                {
                    block.EnableIdleRotation = true;
                    block.Range = Math.Max(800, block.Range);
                }
            }
            catch (Exception e)
            {
                Echo($"Ошибка инициализации скрипта: {e.Message}");
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            BroadcastWarnings(CheckAirDeffense());
        }

        void Save() { }

        // -- LOGIC -- //

        HashSet<WariningType> CheckAirDeffense()
        {
            HashSet<WariningType> warnings = new HashSet<WariningType>();
            var currentGrid = Me.CubeGrid;
            var blocks = new List<IMyFunctionalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyFunctionalBlock>(blocks, block => block.CubeGrid == currentGrid);
            foreach (var block in blocks)
            {
                // Проверить уровень повреждений
                if (!block.IsFunctional) warnings.Add(WariningType.Damaged);

                // Проверить уровень боеприпасов
                DoWithBlockOfType<IMyLargeTurretBase>(block, turret =>
                {
                    var inventory = turret.GetInventory();
                    if (inventory.CurrentMass.RawValue == 0) warnings.Add(WariningType.NoAmmo);
                });

                // Проверить уровень топлива
                DoWithBlockOfType<IMyReactor>(block, reactor =>
                {
                    var inventory = reactor.GetInventory();
                    if (inventory.CurrentMass.RawValue == 0) warnings.Add(WariningType.NoFuel);
                });

                // Проверить уровень энергии
                DoWithBlockOfType<IMyBatteryBlock>(block, battery =>
                {
                    if (battery.CurrentStoredPower / battery.MaxStoredPower < 0.15) warnings.Add(WariningType.LowPower);
                });
            }
            return warnings;
        }

        void BroadcastWarnings(HashSet<WariningType> warnings)
        {
            var currentGrid = Me.CubeGrid;
            var beacons = new List<IMyBeacon>();
            var hasWarnings = warnings.Count > 0;
            GridTerminalSystem.GetBlocksOfType<IMyBeacon>(beacons, block => block.CubeGrid == currentGrid);
            var warningText = hasWarnings ? string.Join(", ", warnings.Select(warning =>
                {
                    switch (warning)
                    {
                        case WariningType.NoAmmo: return "No ammo";
                        case WariningType.LowPower: return "Low power";
                        case WariningType.Damaged: return "Damaged";
                        case WariningType.NoFuel: return "No fuel";
                        default: return "Unknown warning";
                    }
                })) : "";
            Echo($"Warnings: {warningText}");
            foreach (var beacon in beacons)
            {
                if (hasWarnings)
                {
                    beacon.Radius = 15000;
                    beacon.CustomName = $"{AIR_DEFFENSE_TAG} Warnings";
                    beacon.HudText = $"{AIR_DEFFENSE_TAG} {warningText}";
                }
                else
                {
                    beacon.Radius = 800;
                    beacon.CustomName = $"{AIR_DEFFENSE_TAG} No warnings";
                    beacon.HudText = $"{AIR_DEFFENSE_TAG} No warnings";
                }
            }
        }

        enum WariningType
        {
            /// No ammo in turrets
            NoAmmo,
            /// Battery power is low (< 15%)
            LowPower,
            /// Some functional blocks are damaged
            Damaged,
            /// Reactors are out of fuel
            NoFuel,
        }

        // -- UTILS -- //
        private delegate void DoWithBlockOfTypeCallback<T>(T block);
        void DoWithBlockOfType<T>(IMyTerminalBlock Block, DoWithBlockOfTypeCallback<T> callback)
        {
            try
            {
                if (Block is T) callback((T)Block);
            }
            catch (Exception e)
            {
                Echo($"Ошибка выполнения скрипта: {e.Message}");
            }
        }

        // -- END -- //
    }
}