using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;

namespace SpaceEngineers.Outpost
{
    partial class Program : MyGridProgram
    {
        // -- BEGIN -- //

        // -- CONFIG -- //

        /// Тэг, который будет добавлен ко всем блокам
        const string OUTPOST_TAG = "[Outpost]";

        /// Радиус оповещения о неисправностях
        const int WARNING_BROADCAST_RADIUS = 10000;

        /// Текущая сетка
        private IMyCubeGrid currentGrid;

        // -- ENDPOINTS -- //

        public Program()
        {
            try
            {
                // Получим текущую сетку
                currentGrid = (IMyCubeGrid)Me.CubeGrid;

                // Установим частоту обновления тиков скрипта
                Runtime.UpdateFrequency = UpdateFrequency.Update100;

                // Установим значения по умолчанию
                SetDefaultSettings();
            }
            catch (Exception e)
            {
                Echo($"! Ошибка инициализации скрипта: {e.Message}");
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            try
            {
                BroadcastWarnings(CheckAll());
            }
            catch (Exception e)
            {
                Echo($"! Ошибка выполнения скрипта: {e.Message}");
            }
        }

        void Save() { }

        // -- LOGIC -- //

        /// Установить значения по умолчанию
        void SetDefaultSettings()
        {
            DoWithAllBlocksOfType<IMyTerminalBlock>(block =>
            {
                block.CustomName = $"{OUTPOST_TAG} {block.DefinitionDisplayNameText}";
                block.ShowOnHUD = false;
                block.ShowInInventory = false;
                block.ShowInTerminal = false;
                block.ShowInToolbarConfig = false;
            });

            // Включим все функциональные блоки
            DoWithAllBlocksOfType<IMyFunctionalBlock>(block => { if (!block.Enabled) block.Enabled = true; });

            // Установим максимальный радиус обзора турелей
            DoWithAllBlocksOfType<IMyLargeTurretBase>(block => { block.Range = Math.Max(800, block.Range); });
        }

        /// Проверить все блоки на предмет неисправностей
        HashSet<WariningType> CheckAll()
        {
            HashSet<WariningType> warnings = new HashSet<WariningType>();
            var blocks = GetBlocksOfType<IMyFunctionalBlock>();
            foreach (var block in blocks)
            {
                CheckForDamage(block, warnings);
                CheckForAmmo(block, warnings);
                CheckForFuel(block, warnings);
                CheckForPower(block, warnings);
            }
            return warnings;
        }

        /// Проверить уровень повреждений
        void CheckForDamage(IMyFunctionalBlock block, HashSet<WariningType> warnings)
        {
            if (!block.IsFunctional) warnings.Add(WariningType.Damaged);
        }

        /// Проверить уровень боеприпасов
        void CheckForAmmo(IMyFunctionalBlock block, HashSet<WariningType> warnings)
        {
            DoWhenBlockOfType<IMyLargeTurretBase>(block, turret =>
            {
                var inventory = turret.GetInventory();
                if (inventory.CurrentMass.RawValue == 0) warnings.Add(WariningType.NoAmmo);
            });
        }

        /// Проверить уровень топлива
        void CheckForFuel(IMyFunctionalBlock block, HashSet<WariningType> warnings)
        {
            DoWhenBlockOfType<IMyReactor>(block, reactor =>
            {
                var inventory = reactor.GetInventory();
                if (inventory.CurrentMass.RawValue == 0) warnings.Add(WariningType.NoFuel);
            });
        }

        /// Проверить уровень энергии
        void CheckForPower(IMyFunctionalBlock block, HashSet<WariningType> warnings)
        {
            DoWhenBlockOfType<IMyBatteryBlock>(block, battery =>
            {
                if (battery.CurrentStoredPower / battery.MaxStoredPower < 0.15) warnings.Add(WariningType.LowPower);
            });
        }

        /// Отправить сообщение о предупреждениях
        void BroadcastWarnings(HashSet<WariningType> warnings)
        {
            var beacons = GetBlocksOfType<IMyBeacon>();
            var hasWarnings = warnings.Count > 0;
            var time = DateTime.Now;
            var timeText = $"{time.Hour.ToString().PadLeft(2, '0')}:{time.Minute.ToString().PadLeft(2, '0')}:{time.Second.ToString().PadLeft(2, '0')}";
            var warningText = hasWarnings ? $"{timeText} | {GetWarningText(warnings): noWarnings}" : $"{timeText} | No warnings";
            Echo(warningText);
            UpdateBeacons(beacons, hasWarnings, warningText);
        }

        /// Получить текст предупреждений
        string GetWarningText(HashSet<WariningType> warnings) =>
            string.Join(", ", warnings.Select(warning => warningsMap.ContainsKey(warning) ? warningsMap[warning] : "Unknown warning"));

        /// Обновить маяки
        void UpdateBeacons(List<IMyBeacon> beacons, bool hasWarnings, string warningText)
        {
            foreach (var beacon in beacons)
            {
                if (hasWarnings)
                {
                    beacon.Radius = WARNING_BROADCAST_RADIUS;
                    beacon.CustomName = $"{OUTPOST_TAG} Warnings";
                    beacon.HudText = $"{OUTPOST_TAG} | {warningText}";
                }
                else
                {
                    beacon.Radius = 800; // 800 is default value linked to turret range
                    beacon.CustomName = $"{OUTPOST_TAG} No warnings";
                    beacon.HudText = $"{OUTPOST_TAG} | No warnings";
                }
            }
        }

        /// Возможные предупреждения
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
        private Dictionary<WariningType, string> warningsMap = new Dictionary<WariningType, string>()
        {{WariningType.NoAmmo, "No ammo"}, {WariningType.LowPower, "Low power"}, {WariningType.Damaged, "Damaged"}, {WariningType.NoFuel, "No fuel"}};

        // -- UTILS -- //
        private delegate void DoWhenBlockOfTypeCallback<T>(T block);
        void DoWhenBlockOfType<T>(IMyTerminalBlock Block, DoWhenBlockOfTypeCallback<T> callback)
        { try { if (Block is T) callback((T)Block); } catch (Exception e) { Echo($"! Ошибка выполнения скрипта: {e.Message}"); } }

        void DoWithAllBlocksOfType<T>(DoWhenBlockOfTypeCallback<T> callback) where T : class, IMyTerminalBlock
        { var blocks = GetBlocksOfType<T>(); blocks.ForEach(block => callback(block)); }

        List<T> GetBlocksOfType<T>() where T : class, IMyTerminalBlock
        { var blocks = new List<T>(); GridTerminalSystem.GetBlocksOfType<T>(blocks, block => block.CubeGrid == currentGrid); return blocks; }

        // -- END -- //
    }
}