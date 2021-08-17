
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
namespace SpaceEngineers.UWBlockPrograms.Renamer {
    public sealed class Program : MyGridProgram {
    // Your code goes between the next #endregion and #region


public Program()
{
    // Конструктор, вызванный единожды в каждой сессии и
    //  всегда перед вызовом других методов. Используйте его,
    // чтобы инициализировать ваш скрипт.
    //  
    // Конструктор опционален и может быть удалён, 
    // если в нём нет необходимости.
    // 
    // Рекомендуется использовать его, чтобы установить RuntimeInfo.UpdateFrequency
    // , что позволит перезапускать ваш скрипт
    // автоматически, без нужды в таймере.
}

public void Save()
{
    // Вызывается, когда программе требуется сохранить своё состояние.
    // Используйте этот метод, чтобы сохранить состояние программы в поле Storage,
    // или в другое место.
    // 
    // Этот метод опционален и может быть удалён,
    // если не требуется.
}

public void Main(string argument, UpdateType updateSource)
{
    //GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(cargos, cargoFilter);
    
}

    }
}