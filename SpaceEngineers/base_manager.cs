#region Prelude
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using VRageMath;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;

namespace SpaceEngineers.UWBlockPrograms.BaseManager
{
    public sealed class Program : MyGridProgram
    {
        #endregion
        /// КОД ДЛЯ ПРОГРАММИРУЕМОГО БЛОКА - НАЧАЛО

        #region Program

        public void Save()
        {
            //Storage = "...";
        }

        #region Settings
        private const string cargoStatusPanelName = "display.storage.status";
        //private const string debugPanelName = "DebugPanel";
        #endregion

        /// Блоки для управления
        private List<IMyShipController> shipControllers = new List<IMyShipController>();
        private IMyTextPanel cargoStatusPanel;

        /// Задачи на каждый тик
        private int tickNumber = 0;

        private IMyShipController shipController
        {
            get
            {
                foreach (IMyShipController ctrl in this.shipControllers)
                {
                    if (ctrl.IsUnderControl)
                    {
                        return ctrl;
                    }
                }
                return this.shipControllers.FirstOrDefault();
            }
        }

        public Program()
        {
            try
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
                GridTerminalSystem.GetBlocksOfType<IMyShipController>(this.shipControllers);

                // Получим панель для вывода статусов
                this.cargoStatusPanel = GridTerminalSystem.GetBlockWithName(cargoStatusPanelName) as IMyTextPanel;
                if (cargoStatusPanel != null)
                {
                    Echo($"Дисплей {cargoStatusPanelName} найден");
                    this.cargoStatusPanel.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
                    this.cargoStatusPanel.Font = "Monospace";
                    this.cargoStatusPanel.FontSize = (float)1.25;
                    this.cargoStatusPanel.TextPadding = (float)2;
                    this.cargoStatusPanel.BackgroundAlpha = 1;
                    this.cargoStatusPanel.BackgroundColor = new Color(0, 0, 255);
                    this.cargoStatusPanel.FontColor = new Color(255, 255, 255);
                    this.cargoStatusPanel.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    this.cargoStatusPanel.WriteText("\n\n\n\nO N L I N E\n\n\n\n");
                }
                else
                {
                    Echo($"Дисплей {cargoStatusPanelName} не найден");
                }
            }
            catch (Exception exception)
            {
                Echo($"Ошибка инициализации:\n{exception.Message}");
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument.Length == 0)
            {
                tick();
            }
            else
            {
                string[] arguments = argument.Replace(" ", String.Empty).ToLowerInvariant().Split(';');
                foreach (string arg in arguments)
                {

                }
            }
        }

        private void tick()
        {
            if (tickNumber % 20 == 0)
            {
                tickNumber = 0;
                if (cargoStatusPanel is IMyTextPanel)
                {
                    cargoStatusPanel.WriteText($"{DateTime.Now.ToString("H:mm")}\n\n"); // :ss
                }
                sortItems();
            }
            tickNumber++;
        }

        // Сортировка вещей по контейнерам
        private void sortItems()
        {
            List<IMyTerminalBlock> cargos = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(cargos, cargoFilter);

            // Заполним хэш таблицу инвентарями для хранения
            Dictionary<String, List<IMyInventory>> storages = new Dictionary<String, List<IMyInventory>>();
            storages.Add("ore", new List<IMyInventory>());
            storages.Add("component", new List<IMyInventory>());
            storages.Add("ingot", new List<IMyInventory>());
            storages.Add("any", new List<IMyInventory>());
            storages.Add("other", new List<IMyInventory>());
            //storages.Add("consumable", new List<IMyInventory>());
            //storages.Add("ammomagazine", new List<IMyInventory>());
            //storages.Add("physicalobject", new List<IMyInventory>());
            //storages.Add("datapad", new List<IMyInventory>());
            //storages.Add("physicalgunobject", new List<IMyInventory>());
            //storages.Add("production", new List<IMyInventory>());

            //IMyInventory inventory;

            String cargoName;
            IMyInventory inventory;
            int cargosCount = 0;
            bool isOther;
            foreach (IMyTerminalBlock cargo in cargos)
            {
                cargosCount++;
                inventory = cargo.GetInventory();
                cargoName = cargo.CustomName.ToLowerInvariant();
                isOther = true;
                foreach (String key in storages.Keys)
                {
                    if (!cargoName.Contains(key)) continue;
                    storages[key].Add(inventory);
                    isOther = false;
                    break;
                }
                if (isOther)
                {
                    inventory = cargo.GetInventory();
                    if (inventory.IsFull) continue;
                    storages["other"].Add(inventory);
                }
            }

            String rootType;
            List<MyInventoryItem> items = new List<MyInventoryItem>();
            Dictionary<MyItemType, int> itemsCount = new Dictionary<MyItemType, int>(); // Хэш таблица с числом вещей
            foreach (var cargo in cargos)
            {
                cargoName = cargo.CustomName.ToLowerInvariant();
                if (cargoName.StartsWith("[ignore]"))
                {
                    // Игнорирую емкости начинающиеся с [ignore]
                    continue;
                }
                else if (cargo is IMyProductionBlock)
                {
                    // Если это производственный блок - беру выходной инвентарь
                    inventory = (cargo as IMyProductionBlock).OutputInventory;
                }
                else
                {
                    // В противном случае беру основной инвентарь
                    inventory = cargo.GetInventory();
                }
                inventory.GetItems(items); // , (item) => true
                foreach (MyInventoryItem item in items)
                {
                    if (itemsCount.ContainsKey(item.Type))
                    {
                        itemsCount[item.Type]++;
                    }
                    else
                    {
                        itemsCount[item.Type] = 1;
                    }
                    rootType = getRootItemType(item);
                    // Если вещь уже в правильном контейнере - пропускаем
                    if (cargoName.Contains(rootType)) continue;
                    List<IMyInventory> to; // Список куда перемещать
                    bool toAny = false; // Нету специально отведенного ящика?
                    if (storages.ContainsKey(rootType))
                    {
                        to = storages[rootType];
                    }
                    else
                    {
                        to = storages["any"];
                        toAny = true;
                    }
                    // Попробуем перенести вещь
                    bool success = !transferItem(inventory, to, item);
                    // Если переместили не успешно и это не ящик для всякой всячины - перенесем в последний
                    if (!success && !toAny)
                    {
                        transferItem(inventory, storages["any"], item);
                    }
                }
                items.Clear();
            }

            // Добавим информацию о состоянии
            if (cargoStatusPanel is IMyTextPanel)
            {
                long volume;
                long maxVolume;
                var builder = new StringBuilder("");
                foreach (var storage in storages)
                {
                    volume = 0;
                    maxVolume = 0;
                    foreach (IMyInventory inv in storage.Value)
                    {
                        maxVolume += inv.MaxVolume.RawValue;
                        volume += inv.CurrentVolume.RawValue;
                    }
                    if (maxVolume > 0)
                    {
                        builder.Append($"{storage.Key.ToUpper().PadRight(12, ' ')}   {(volume * 100 / maxVolume).ToString("0").PadLeft(3, ' ')}%\n");
                    }
                }
                cargoStatusPanel.WriteText(builder.ToString(), true);
            }
        }

        private bool cargoFilter(IMyTerminalBlock entity) =>
            entity.HasInventory
            && !(entity is IMyGasGenerator) // Исключаем генераторы O2/H2
            && !(entity is IMyPowerProducer) // Исключаем Реакторы и генераторы
            && !(entity is IMyUserControllableGun) // Исключаем турели
            && !(entity is IMyRefinery) // Исключаем перерабатывающие заводы
            && !(entity is IMySafeZoneBlock); // Исключаем Сейф зоны

        private String getRootItemType(MyInventoryItem item) =>
            item.Type.TypeId.Split('_').Last().ToLowerInvariant();

        private bool transferItem(IMyInventory from, List<IMyInventory> to, MyInventoryItem item)
        {
            if (to.Count == 0) return false;
            bool success = false;
            MyItemType itemType = item.Type;
            foreach (IMyInventory dst in to)
            {
                try
                {
                    if (dst.IsFull && !from.CanTransferItemTo(dst, itemType)) continue;
                    if (from.TransferItemTo(dst, item))
                    {
                        success = true;
                        break;
                    }
                }
                catch (Exception e)
                {
                    Echo($"Transfer item error: {e.Message}");
                    continue;
                }
            }
            return success;
        }


        #endregion

        /// КОД ДЛЯ ПРОГРАММИРУЕМОГО БЛОКА - КОНЕЦ
        #region PreludeFooter
    }
}
#endregion
