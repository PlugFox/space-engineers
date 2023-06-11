using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Linq;

namespace SpaceEngineers.UWBlockPrograms.ExtendedGateway
{

    partial class Program : MyGridProgram
    {
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project,
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.
        //
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.

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

        class GatewayState
        {
            private ConsoleLogDelegate LogDelegate { get; set; }

            public GatewayStatus Status { get; private set; } = GatewayStatus.Idle;

            private int step = 1;

            public DoorClass InsideDoor { get; private set; }

            public DoorClass OutsideDoor { get; private set; }

            public IMyAirVent AirVent { get; private set; }


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

            public GatewayState(DoorClass insideDoor, DoorClass outsideDoor, ConsoleLogDelegate logDelegate, IMyAirVent airVent)
            {
                LogDelegate = logDelegate;
                InsideDoor = insideDoor;
                OutsideDoor = outsideDoor;
                AirVent = airVent;
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

        private static class CustomNames
        {
            public const string ButtonOutdoors_1 = "btn_outdoors_1";
            public const string ButtonOutdoors_2 = "btn_outdoors_2";

            public const string DimaProgram = "dima";
            public const string DoorForProgram = "door_dima";
            public const string Door_1 = "door_dima_1";
            public const string Door_2 = "door_dima_2";
        }

        private static class CustomProperties
        {
            public const string LastOpened = "opened";
            public const string LastClosed = "closed";
        }

        #endregion

        #region script

        private GatewayState gatewayState;

        /// <summary>
        /// Должно быть 2 двери и вентиляция (опцинально):
        /// Дверь внтуренняя: название должно включать переменную Door_1 [door_dima_1]
        /// Дверь внешняя: название должно включать переменную Door_2 [door_dima_2]
        /// Вентиляция: название должно включать переменную DimaProgram [dima]
        /// Кнопка внутренняя вызывает программу с аргументом ButtonOutdoors_1 [btn_outdoors_1]
        /// Кнопка внешняя вызывает программу с аргументом ButtonOutdoors_2 [btn_outdoors_2]
        /// </summary>
        public Program()
        {
            ConsoleLog($"Конструктор.");
            if (GridTerminalSystem == null)
            {
                ConsoleLog($"Ошибка! GridTerminalSystem НЕ НАЙДЕН (NULL)!");
                return;
            }

            var insideDoor = GetDoorByName(CustomNames.Door_1);
            var outsideDoor = GetDoorByName(CustomNames.Door_2);
            if (insideDoor == null || outsideDoor == null)
                return;

            var airVents = new List<IMyAirVent>();
            GridTerminalSystem.GetBlocksOfType(airVents, (e) => e.CustomName.ToLowerInvariant().Contains(CustomNames.DimaProgram));

            ConsoleLog($"Конструктор нашёл {airVents.Count} вентиляторов.");
            var airVent = airVents.FirstOrDefault();

            gatewayState = new GatewayState(insideDoor, outsideDoor, ConsoleLog, airVent);
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Save()
        {
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

        private DoorClass GetDoorByName(string name)
        {
            var doors = new List<IMyDoor>();
            GridTerminalSystem.GetBlocksOfType(doors, (e) => e.CustomName.ToLowerInvariant().Contains(name));
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

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument.Length > 0)
                ConsoleLog($"Вызов с аргументом [{argument}]");
            if (GridTerminalSystem == null)
            {
                ConsoleLog($"Ошибка! GridTerminalSystem НЕ НАЙДЕН (NULL)!");
                return;
            }
            switch (argument.ToLowerInvariant())
            {
                case CustomNames.ButtonOutdoors_1:
                    gatewayState.RequestAction(GatewayStatus.GoingOutside);
                    //OpenDoor(CustomNames.Door_1);
                    return;
                case CustomNames.ButtonOutdoors_2:
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

        public delegate void ConsoleLogDelegate(string value);
        private Dictionary<int, string> logs = new Dictionary<int, string>();

        int logIdx = 0;
        private void ConsoleLog(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                foreach (var v in logs.Values)
                    Echo(v);
            }
            else
            {
                ++logIdx;
                logs[logIdx % 10] = value;
            }
        }

        #endregion


    }


}
