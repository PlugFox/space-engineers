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
namespace SpaceEngineers.UWBlockPrograms.Template {
public sealed class Program : MyGridProgram {
// Your code goes between the next #endregion and #region
#endregion

/// КОД ДЛЯ ПРОГРАММИРУЕМОГО БЛОКА - НАЧАЛО

#region Settings

#endregion

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
    //Storage = "...";
}

public void Main(string argument, UpdateType updateSource)
{
    // Главная точка входа в скрипт вызывается каждый раз,
    // когда действие Запуск программного блока активируется,
    // или скрипт самозапускается. Аргумент updateSource описывает,
    // откуда поступило обновление.
    // 
    // Метод необходим сам по себе, но аргументы
    // ниже могут быть удалены, если не требуются.
}


/// КОД ДЛЯ ПРОГРАММИРУЕМОГО БЛОКА - КОНЕЦ

#region PreludeFooter
    }
}
#endregion