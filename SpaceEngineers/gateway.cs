#region Prelude
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using VRageMath;
using VRage.Game;
using VRage.Collections;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;

// Change this namespace for each script you create.
namespace SpaceEngineers.UWBlockPrograms.Gateway
{
    public sealed class Program : MyGridProgram
    {

        // Your code goes between the next #endregion and #region
        #endregion

        /// КОД ДЛЯ ПРОГРАММИРУЕМОГО БЛОКА - НАЧАЛО

        #region Settings
        /// Коллбэк таймера перед закрытием всех дверей
        private const string timerCallbackOnLock = "ПередЗакрытием";

        /// Коллбэк таймера перед открытием дверей
        private const string timerCallbackOnUnlock = "ПередОткрытием";

        /// Задержка в секундах перед открытием/закрытием дверей
        private const int delay = 3;
        #endregion


        public Program()
        {
            state = GatewayState.idle;
            operationTime = DateTime.Now;
            Runtime.UpdateFrequency = UpdateFrequency.None;
            _lockAll();
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (state != GatewayState.idle) {
                // Если у нас есть запланированная задача - игнорируем ввод пользователя
                // это своеобразный мьютекс
                _delayedOperation();
                return;
            }

            if (argument.Length == 0)
            {
                _toggle();
            }
            else
            {
                string[] arguments = argument.Replace(" ", String.Empty).ToLower().Split(';');
                foreach (string arg in arguments)
                {
                    switch (arg)
                    {
                        case "switch":
                        case "toggle":
                            _toggle();
                            break;
                        case "close":
                        case "lock":
                            _lockAll();
                            break;
                        case "open":
                        case "unlock":
                            _unlockAll();
                            break;
                    }
                }
            }
        }

        /// Текущее состояние
        private GatewayState state = GatewayState.idle;

        /// Время не раньше которого должна отработать отложенная операция
        private DateTime operationTime = DateTime.Now;

        /// idle      - принимает команды пользователя
        /// locking   - готовится закрыть двери
        /// unlocking - готовится открыть двери
        /// shutdown  - готовиться выключить питание
        private enum GatewayState {
            idle,
            locking,
            unlocking,
            shutdown,
        }

        private void _toggle()
        {
            foreach (IMyDoor door in doors) {
                DoorStatus status = door.Status;
                if (status == DoorStatus.Open || status == DoorStatus.Opening) {
                    _lockAll();
                    return;
                }
            }
            _unlockAll();
            return;
        }

        private List<IMyDoor> doors {
            get {

                List<IMyDoor> doors = new List<IMyDoor>();
                GridTerminalSystem.GetBlocksOfType<IMyDoor>(doors);
                return doors;
            }
        }

        private void _unlockAll() {
            IMyTerminalBlock timer = GridTerminalSystem.GetBlockWithName(timerCallbackOnUnlock);
            if (timer != null && timer is IMyTimerBlock)
            {
                (timer as IMyTimerBlock).Trigger();
            }
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            state = GatewayState.unlocking;
            operationTime = DateTime.Now.AddSeconds(delay);
        }


        private void _lockAll() {
            foreach (IMyDoor door in doors)
            {
                door.Enabled = true;
                door.CloseDoor();
            }
            IMyTerminalBlock timer = GridTerminalSystem.GetBlockWithName(timerCallbackOnLock);
            if (timer != null && timer is IMyTimerBlock)
            {
                (timer as IMyTimerBlock).Trigger();
            }
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            state = GatewayState.locking;
            operationTime = DateTime.Now.AddSeconds(delay);
        }

        private void _delayedOperation() {
            if (state == GatewayState.shutdown)
            {
                bool done = true;
                foreach (IMyDoor door in doors)
                {
                    if (door.Status == DoorStatus.Closing || door.Status == DoorStatus.Opening)
                    {
                        done = false;
                    }
                    else
                    {
                        door.Enabled = false;

                    }
                }
                if (done)
                {
                    operationTime = DateTime.Now;
                    state = GatewayState.idle;
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    return;
                }
            }
            if (state == GatewayState.locking && DateTime.Now > operationTime)
            {
                state = GatewayState.shutdown;
                return;
            }
            else if (state == GatewayState.unlocking && DateTime.Now > operationTime)
            {
                foreach (IMyDoor door in doors)
                {
                    door.Enabled = true;
                    door.OpenDoor();
                }
                state = GatewayState.shutdown;
                return;
            }
        }

        /// КОД ДЛЯ ПРОГРАММИРУЕМОГО БЛОКА - КОНЕЦ

        #region PreludeFooter
    }
}
#endregion