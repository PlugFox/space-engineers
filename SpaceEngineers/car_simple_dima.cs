using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.Entities.Blocks;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
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

        private static class CustomNames
        {
            public const string Start = "start";
            public const string Stop = "stop";

            public const string DimaProgram = "dima";
            public const string BackWheel = "back";
            public const string FrontWheel = "front";
        }

        private static class CustomProperties
        {
            public const string LastOpened = "opened";
            public const string LastClosed = "closed";
        }


        class MyCar
        {
            private ConsoleLogDelegate Logger { get; }
            private IList<IMyMotorSuspension> Motors { get; }
            private IList<IMyMotorSuspension> MotorsBack { get; }

            public MyCar(ConsoleLogDelegate logger, IList<IMyMotorSuspension> motors)
            {
                Logger = logger;
                Motors = motors;
                MotorsBack = motors.Where(e => e.CustomName.ToLowerInvariant().Contains(CustomNames.BackWheel)).ToList();
                Logger($"Found {MotorsBack.Count} wheels!");
            }

            public void Start()
            {
                foreach (var e in Motors)
                {
                    //TerminalActionExtensions.ApplyAction(e, "IncreaseTorque");
                    //
                    e.PropulsionOverride = 0.3F;
                    //e.SetValue("Torque", 100);
                }
            }

            public void Stop()
            {
                foreach (var e in Motors)
                {
                    e.PropulsionOverride = 0.0F;
                }
            }
        }

        #endregion

        #region script

        private MyCar car = null;
        /// <summary>
        /// 
        /// </summary>
        public Program()
        {
            ConsoleLog($"Конструктор.");
            if (GridTerminalSystem == null)
            {
                ConsoleLog($"Ошибка! GridTerminalSystem НЕ НАЙДЕН (NULL)!");
                return;
            }
            InitCar();
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        void InitCar()
        {
            var motors = new List<IMyMotorSuspension>();
            GridTerminalSystem.GetBlocksOfType(motors);
            car = new MyCar(ConsoleLog, motors);

        }

        public void Save()
        {
        }

        
        /*
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
        */

        private const int SaveResourcesModifier = 2; // Runs every Xth call
        private const int DoorsDelayTicks = 3;
        static int ticks = 0; // Local time

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument.Length > 0)
                ConsoleLog($"Вызов с аргументом [{argument}]");
            if (GridTerminalSystem == null || car == null)
            {
                ConsoleLog($"Ошибка! GridTerminalSystem НЕ НАЙДЕН (NULL)!");
                return;
            }
            switch (argument.ToLowerInvariant())
            {
                case CustomNames.Start:
                    car.Start();
                    return;
                case CustomNames.Stop:
                    car.Stop();
                    return;
                default:
                    ++ticks;
                    if (ticks % SaveResourcesModifier != 0)
                    {
                        ConsoleLog(null);
                        return;
                    }

                    ConsoleLog($"Вызов auto #{ticks}");
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
