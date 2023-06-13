/*
 * ┌──[AirLock]────────────────────────────────┐
 * │               AirVent & Led               │
 * │                 ┌───────┐                 │
 * │                 │       │                 │
 * │      Int Door   └───────┘   Ext Door      │
 * │      ┌──────┐               ┌──────┐      │
 * │      │      │      ┌─┐      │      │      │
 * │      │      │      │┼│      │      │      │
 * │      │      │    ┌┬┴─┴┬┐    │      │      │
 * │      │      │    ││   ││    │      │      │
 * │ ┌┐   │      │    ││   ││    │      │   ┌┐ │
 * │ └┤   │      │    └┼─┬─┼┘    │      │   ├┘ │
 * │  │   │      │     │ │ │     │      │   │  │
 * │  │   └──────┘     │ │ │     └──────┘   │  │
 * │  Button with       └─┴─┘     Button with  │
 * │ "out" action                "in" action   │
 * └───────────────────────────────────────────┘
 */

using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceEngineers.UWBlockPrograms.ExtendedGateway
{
    /// <summary>
    /// Должно быть 2 двери и вентиляция (опцинально):
    /// Дверь внтуренняя: название должно включать переменную AirLockIntDoor
    /// Дверь внешняя: название должно включать переменную AirLockExtDoor
    /// Вентиляция: название должно начинаться с AirLockPrefix
    /// Кнопка внутренняя вызывает программу с аргументом AirLockGoingOutsideActionArg
    /// Кнопка внешняя вызывает программу с аргументом AirLockGoingInsideActionArg
    /// </summary>
    partial class Program : MyGridProgram
    {
        /// КОД ДЛЯ ПРОГРАММИРУЕМОГО БЛОКА - НАЧАЛО

        // -- CONFIG -- //

        /// Префикс для шлюза
        private const string AirLockPrefix = "[AirLock]";

        /// Аргумент для внутренней кнопки
        private const string AirLockGoingOutsideActionArg = "out";

        /// Аргумент для внешней кнопки
        private const string AirLockGoingInsideActionArg = "in";

        /// Внутренняя дверь
        private const string AirLockIntDoor = "Int Door";

        /// Внешняя дверь
        private const string AirLockExtDoor = "Ext Door";

        /// Время автоматического закрытия дверей в секундах
        private const int AutomaticCloseDelay = 15;

        // -- DO NOT EDIT BELOW THIS LINE -- //

        #region additional_classes

        class CustomDataClass
        {
            /// <summary>
            /// For button: last time opened (in ticks)
            /// </summary>
            public int? TimeX { get; set; }

            /// <summary>
            /// For button: last time closed (in ticks)
            /// </summary>
            public int? TimeY { get; set; }
        }

        class DoorClass : CustomDataClass
        {
            public bool EnoughTimeToGoIn { get { return (ticks - (TimeX ?? 0)) >= DoorsDelayTicks; } }

            private ConsoleLogDelegate LogDelegate { get; set; }

            public DoorStatus? ExpectedStatus { get; set; }

            public IMyDoor Door { get; set; }

            public DoorClass(IMyDoor door, ConsoleLogDelegate logDelegate)
            {
                LogDelegate = logDelegate;
                Door = door;
            }

            public void OpenDoor()
            {
                LogDelegate($"Открываю дверь {Door.CustomName}!");
                TimeX = ticks;
                if (Door.Status != DoorStatus.Closed)
                {
                    LogDelegate($"Дверь {Door.CustomName} не закрыта!");
                    return;
                }
                Door.Enabled = true;

                ExpectedStatus = DoorStatus.Open;
                Door.OpenDoor();
            }

            public void CloseDoor()
            {
                LogDelegate($"Закрываю дверь {Door.CustomName}!");
                TimeY = ticks;

                if (Door.Status != DoorStatus.Open)
                    return;

                Door.Enabled = true;
                ExpectedStatus = DoorStatus.Closed;
                Door.CloseDoor();
            }

            public void AutomaticClose()
            {
                LogDelegate($"AutomaticClose {Door.CustomName}!");
                if (Door.Status != DoorStatus.Open)
                {
                    DisableIfNeeded();
                    return;
                }

                var lastTimeOpened = TimeX ?? 0;
                var diffTicks = ticks - lastTimeOpened;
                LogDelegate($"Разница с последним открытием в тиках: {diffTicks}!");

                if (diffTicks >= DoorsDelayTicks)
                    CloseDoor();
                else
                    DisableIfNeeded();

            }

            public void DisableIfNeeded()
            {
                //if (Door.Status == ExpectedStatus)
                //    Door.Enabled = false;
            }
        }

        enum GatewayStatus
        {
            Idle,
            GoingOutside,
            GoingInside,
        }

        /// Используется для автоматического закрытия дверей
        /// Дверь : Время когда она последний раз была замечена открытой или null
        static private Dictionary<IMyDoor, DateTime> OpenedDoors = new Dictionary<IMyDoor, DateTime>();

        class GatewayState
        {
            private ConsoleLogDelegate LogDelegate { get; set; }

            private GatewayStatus _Status = GatewayStatus.Idle;
            public GatewayStatus Status
            {
                get { return _Status; }
                private set
                {
                    if (value == _Status) { return; }
                    else if (value == GatewayStatus.Idle)
                    {
                        Lights?.ForEach(x => x.Color = VRageMath.Color.Green);
                        Sounds?.ForEach(x => x.Stop());
                    }
                    else
                    {
                        Lights?.ForEach(x => x.Color = VRageMath.Color.Red);
                        Sounds?.ForEach(x => x.Play());
                    }
                    _Status = value;
                }
            }

            private int step = 1;

            public DoorClass InsideDoor { get; private set; }

            public DoorClass OutsideDoor { get; private set; }

            public IMyAirVent AirVent { get; private set; }

            public List<IMyLightingBlock> Lights { get; private set; }

            public List<IMySoundBlock> Sounds { get; private set; }

            private DoorClass NearDoor
            {
                get
                {
                    switch (Status)
                    {
                        case GatewayStatus.GoingOutside:
                            return InsideDoor;
                        case GatewayStatus.GoingInside:
                            return OutsideDoor;
                        default:
                            return InsideDoor;
                    }
                }
            }

            private DoorClass FarDoor
            {
                get
                {
                    switch (Status)
                    {
                        case GatewayStatus.GoingOutside:
                            return OutsideDoor;
                        case GatewayStatus.GoingInside:
                            return InsideDoor;
                        default:
                            return OutsideDoor;
                    }
                }
            }

            public GatewayState(DoorClass insideDoor, DoorClass outsideDoor, ConsoleLogDelegate logDelegate, IMyAirVent airVent, List<IMyLightingBlock> lights, List<IMySoundBlock> sounds)
            {
                LogDelegate = logDelegate;
                InsideDoor = insideDoor;
                OutsideDoor = outsideDoor;
                AirVent = airVent;
                Lights = lights;
                Sounds = sounds;
            }

            public void RequestAction(GatewayStatus status)
            {
                LogDelegate($"Requesting actions for status: {status} (current status is {Status})");
                switch (status)
                {
                    case GatewayStatus.Idle:
                        return;
                    case GatewayStatus.GoingOutside:
                    case GatewayStatus.GoingInside:
                        if (Status != GatewayStatus.Idle)
                            return;
                        Status = status;
                        break;
                }
                OnTick();
            }

            private void DoStepFirst()
            {

                AirVent?.ApplyAction("Depressurize_On");
                if (FarDoor.Door.Status != DoorStatus.Closed)
                {
                    FarDoor.CloseDoor();
                    return;
                }
                FarDoor.Door.Enabled = false;
                NearDoor.OpenDoor();
                step = 2;
            }

            private void DoStepSecond()
            {
                if (!NearDoor.EnoughTimeToGoIn)
                    return;
                if (NearDoor.Door.Status != DoorStatus.Closed)
                {
                    NearDoor.CloseDoor();
                    return;
                }
                NearDoor.Door.Enabled = false;
                FarDoor.OpenDoor();
                step = 3;
            }

            private void DoStepThird()
            {
                if (!FarDoor.EnoughTimeToGoIn)
                    return;
                AirVent?.ApplyAction("Depressurize_Off");
                if (FarDoor.Door.Status != DoorStatus.Closed)
                {
                    FarDoor.CloseDoor();
                    return;
                }
                FarDoor.Door.Enabled = false;
                step = 1;
                Status = GatewayStatus.Idle;
            }

            private void OpenIndoorIfIdle()
            {
                if (FarDoor.Door.Status != DoorStatus.Closed)
                {
                    FarDoor.CloseDoor();
                    return;
                }
                AirVent?.ApplyAction("Depressurize_Off");
                FarDoor.Door.Enabled = false;
                NearDoor.OpenDoor();
            }

            public void OnTick()
            {
                if (Status == GatewayStatus.Idle)
                {
                    LogDelegate("Gateway is Idle");
                    OpenIndoorIfIdle();
                    return;
                }
                LogDelegate($"Gateway Step {step}");
                switch (step)
                {
                    case 2:
                        DoStepSecond();
                        break;
                    case 3:
                        DoStepThird();
                        break;
                    default:
                        DoStepFirst();
                        break;
                }
            }
        }

        private static class CustomProperties
        {
            public const string LastOpened = "opened";
            public const string LastClosed = "closed";
        }

        #endregion

        #region script

        private GatewayState gatewayState;

        public Program()
        {
            try
            {
                ConsoleLog($"Конструктор.");
                if (GridTerminalSystem == null)
                {
                    ConsoleLog($"Ошибка! GridTerminalSystem не найден!");
                    return;
                }

                var groupPrefix = AirLockPrefix.ToLowerInvariant();
                var insideDoor = GetDoorByName(AirLockIntDoor, groupPrefix);
                var outsideDoor = GetDoorByName(AirLockExtDoor, groupPrefix);
                if (insideDoor == null || outsideDoor == null)
                {
                    ConsoleLog($"Ошибка! Без внутренней или внешней двери невозможно создать шлюз.");
                    return;
                }

                var lights = new List<IMyLightingBlock>();
                GridTerminalSystem.GetBlocksOfType<IMyLightingBlock>(lights, (e) => e.CustomName.ToLowerInvariant().StartsWith(groupPrefix));
                ConsoleLog($"Найдено {lights.Count} ламп.");
                foreach (var light in lights)
                {
                    light.Color = VRageMath.Color.Green;
                    light.BlinkIntervalSeconds = 0;
                    light.Enabled = true;
                    light.Radius = 10;
                    light.Intensity = 5;
                }

                var sounds = new List<IMySoundBlock>();
                GridTerminalSystem.GetBlocksOfType<IMySoundBlock>(sounds, (e) => e.CustomName.ToLowerInvariant().StartsWith(groupPrefix));
                ConsoleLog($"Найдено {sounds.Count} динамиков.");
                foreach (var sound in sounds)
                {
                    sound.Enabled = true;
                    sound.LoopPeriod = 3;
                    sound.Range = 10;
                    sound.Volume = 1;
                    sound.SelectedSound = "Alert 1";
                    sound.Stop();
                }

                var airVents = new List<IMyAirVent>();
                GridTerminalSystem.GetBlocksOfType<IMyAirVent>(airVents, (e) => e.CustomName.ToLowerInvariant().StartsWith(groupPrefix));
                ConsoleLog($"Найдено {airVents.Count} вытяжек.");

                // Настроим найденные блоки
                TuneBlocksWithAirLockPrefix();

                // Зададим состояние шлюза
                gatewayState = new GatewayState(insideDoor, outsideDoor, ConsoleLog, airVents.FirstOrDefault(), lights, sounds);

                // Установим частоту обновления тиков скрипта
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
            }
            catch
            {
                Echo("Initilization failed.");
            }
        }

        public void Save()
        {
        }

        /// Спрятать все с префиксом AirLockPrefix
        private void TuneBlocksWithAirLockPrefix()
        {
            var prefix = AirLockPrefix.ToLowerInvariant();
            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks, (e) => e.CustomName.ToLowerInvariant().StartsWith(prefix));
            foreach (var block in blocks)
            {
                block.ShowOnHUD = false;
                block.ShowInInventory = false;
                block.ShowInTerminal = false;
                if (block is IMyProgrammableBlock) continue;
                block.ShowInToolbarConfig = false;
            }
        }

        private Dictionary<string, DoorClass> dataStorage = new Dictionary<string, DoorClass>();
        private DoorClass GetCustomData(IMyDoor door)
        {
            if (dataStorage.ContainsKey(door.CustomName))
                return dataStorage[door.CustomName];

            var data = new DoorClass(door, ConsoleLog);
            dataStorage[door.CustomName] = data;
            return data;
        }

        private DoorClass GetDoorByName(string name, string groupPrefix)
        {
            var doors = new List<IMyDoor>();
            name = name.ToLowerInvariant();
            GridTerminalSystem.GetBlocksOfType<IMyDoor>(
                doors,
                (e) =>
                {
                    var eName = e.CustomName.ToLowerInvariant();
                    return eName.StartsWith(groupPrefix) && eName.EndsWith(name);
                });
            var door = doors.FirstOrDefault();
            if (door == null)
            {
                ConsoleLog($"Ошибка! ДВЕРЬ С ИМЕНЕМ {name} НЕ НАЙДЕНА!");
                return null;
            }

            var doorClass = GetCustomData(door);
            return doorClass;
        }

        private const int SaveResourcesModifier = 2; // Runs every Xth call
        private const int DoorsDelayTicks = 3;
        static int ticks = 0; // Local time

        /// Закрыть двери, если они открыты слишком долго
        void AutoCloseDoors()
        {
            // Получение всех дверей (исключая двери шлюза)
            var allDoors = new List<IMyTerminalBlock>();
            var prefix = AirLockPrefix.ToLowerInvariant();
            GridTerminalSystem.GetBlocksOfType<IMyDoor>(allDoors, (e) => !e.CustomName.ToLowerInvariant().StartsWith(prefix));

            // Инициализация списка дверей для закрытия
            var doorsToClose = new List<IMyDoor>();

            foreach (IMyDoor door in allDoors)
            {
                // Если дверь открыта и ещё не в словаре, добавьте её
                if (door.Status == DoorStatus.Open && !OpenedDoors.ContainsKey(door))
                {
                    OpenedDoors.Add(door, DateTime.Now);
                }
                // Если дверь закрыта, удалите её из словаря
                else if (door.Status != DoorStatus.Open && OpenedDoors.ContainsKey(door))
                {
                    OpenedDoors.Remove(door);
                }

                // Если дверь в словаре открыта дольше, чем AutomaticCloseDelay, добавьте её в список doorsToClose
                if (OpenedDoors.ContainsKey(door) && (DateTime.Now - OpenedDoors[door]).TotalSeconds > AutomaticCloseDelay)
                {
                    doorsToClose.Add(door);
                }
            }

            // Закрываем двери и удаляем их из словаря
            foreach (IMyDoor door in doorsToClose)
            {
                if (!door.Enabled) door.Enabled = true;
                door.CloseDoor();
                OpenedDoors.Remove(door);
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            try
            {
                AutoCloseDoors();

                if (argument.Length > 0)
                    ConsoleLog($"Вызов с аргументом [{argument}]");
                if (GridTerminalSystem == null)
                {
                    ConsoleLog($"Ошибка! GridTerminalSystem НЕ НАЙДЕН (NULL)!");
                    return;
                }
                switch (argument.ToLowerInvariant())
                {
                    case AirLockGoingOutsideActionArg:
                        gatewayState.RequestAction(GatewayStatus.GoingOutside);
                        //OpenDoor(CustomNames.Door_1);
                        return;
                    case AirLockGoingInsideActionArg:
                        gatewayState.RequestAction(GatewayStatus.GoingInside);
                        //OpenDoor(CustomNames.Door_2);
                        return;
                    default:
                        ++ticks;
                        if (ticks % SaveResourcesModifier != 0)
                        {
                            ConsoleLog(null);
                            return;
                        }

                        ConsoleLog($"Вызов auto #{ticks}");
                        gatewayState.OnTick();
                        //EndlessCycle();
                        return;
                }
            }
            catch
            {
                ConsoleLog($"Ошибка!");
            }
        }

        public delegate void ConsoleLogDelegate(string value);
        private Dictionary<int, string> logs = new Dictionary<int, string>();

        int logIdx = 0;
        private void ConsoleLog(string value)
        {
            return; // Do not log anything
            /* if (string.IsNullOrEmpty(value))
            {
                var buffer = new StringBuilder();
                foreach (var v in logs.Values.Take(10))
                    buffer.AppendLine(v);
                Echo(buffer.ToString());
            }
            else
            {
                ++logIdx;
                logs[logIdx % 10] = value;
            } */
        }

        #endregion

        /// КОД ДЛЯ ПРОГРАММИРУЕМОГО БЛОКА - КОНЕЦ

    }


}
