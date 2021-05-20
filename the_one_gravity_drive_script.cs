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

namespace SpaceEngineers.UWBlockPrograms.GDrive2 {
    public sealed class Program : MyGridProgram {
#endregion
/// КОД ДЛЯ ПРОГРАММИРУЕМОГО БЛОКА - НАЧАЛО

#region Program

/*        
DifferentLevelDan's    
The One Gravity Drive Script [TOG]       
Version 1.5.2

All-in-one fly-by-wire 3-axis gravity drive.    
Responds to user input thorugh normal WASD controls.    

**KNOWN ISSUES**
* Thrusterless ships cannot toggle inertial dampeners with the SE keybind.
    To toggle TOG-controlled inertial dampeners on thrusterless ships, run the program with    
    the "dampener" argument. 
* Unbalanced ships may tumble during gravity drive operation.
    TOG has rudimentry gyroscopic stabilization to help stabilize slightly offset gravity drives
    in straight-line acceleration. Turning while accelerating in an unbalanced ship is not supported
    by TOG at this time.
* Accelleration Limits are currently broken and disabled.

Features:    
    Automatic Gravity Generator Field Size Setup.    
    Automatic gyroscopic stabilization to prevent unwanted rotations from offset gravity drives.    
    Easy Toggles    
    Inertial Dampener supported!*    
    No thrusters required!*    
    Supports multiple drives per ship    
    Autopilot support    
    Acceleration Limits    

    * Thrusterless ships cannot toggle inertial dampeners with the SE keybind.    
        To toggle inertial dampeners on thrusterless ships, run the program with    
        the "dampener" argument.    

Usage:    
    Creating a Gravity Drive:    
        Place Gravity Generators and Artifical mass blocks.    
        Tag Gravity Generators and artifical mass blocks with "[GD " + uniqueName + "]"    
        Example:    
            Gravity Generator 15 [GD Main]    
            Artificial Mass 13 [GD Main]    
            (This creates a gravity drive named "Main")    
        Run this programming block with the argument "reset".    

    Setting up the Programming Block/Timer    
        Set a Timer to "TriggerNow" itself as the first action, and "Start" itself as its second action.    
        Set this programming block as "Run" for the third action. Do not use "Run with default argument".    
        Start the timer block.    
        Check the Programming block for output.    

    Available Commands:    
        Arguments that can be passed to the programming block during operation.    

        Reset:    
            Resets the script. Script will rescan for thrusters, gyros, and cockpits.    

        Stop:    
        Disable:    
            Disables the script. Gravity drives will no longer respond to WASD/Inertial Dampener input.    

        Start:    
        Enable:    
            Enables the script. Gravity drives will respond to WASD/Inertial Dampener input.    

        Switch:
        Toggle:
            Toggles the script between disabled and enabled.    

        Dampener:    
        Damp:   
            Toggles inertial dampeners. (Used for thrusterless ships)    

        Gyrostab:
        Gyrostabilizer:    
        Gyro:  
            Toggles Gyrostabilization.

        Gear:
        Shift:
            Switches to the next higher gear (or first gear if on highest gear).

Future Plans:    
    Warning system for undesired overlapping gravity fields.    
    Connected-grid support.    
    Max Speed limits.    
    Automatic counter-thrust for off-center drives.    
    Read Gears from CustomData    

ChangeLog:
    1.5.1=>1.5.2
        Added gravity drive idle mode, to prevent unnecessary gravity drive operations.
        Changed ZeroEpsilon from 0.0001 to 0.001
        Enhanced initialization logging.
        Added shortcut arguments "gyrostab" for gyrostabilizer and "gear" for switch gear.
    1.5=>1.5.1
        Fixed some PID tunings.
        Refactored some logic
        Broke Accelleration limits
    1.4.2=>1.5 
        Added PID controls again for natural gravity stability. 
    1.4.1=>1.4.2  
        Formatted Speed output to N2  
    1.4=>1.4.1  
        Added LCD output.  
    1.3=>1.4  
        Added Manual cockpit overrides (SEE: User-Configuration options)  
        Fixed legacy code warnings.  
        Fixed inputs arguments being case sensitive.  
    1.2.1=>1.3   
        Fixed typos in changelog.   
        Implemented auto-shutoff in natural gravity.    
        Implemented better floating point epsilon checking.   
        Set more sane defaults.   
        Fixed issue with manual dampeners overriding ship dampeners.   
    1.2=>1.2.1   
        Fixed bug related to case sensitivity of tag. (Thanks procrastinator daniel)    
        Changed order of ChangeLog to newest to oldest.    
    1.1.1=>1.2    
        Removed PID stabilization - Proportional control is all that's required.    
        Cleaned up main function.    
        Implemented Acceleration Limits    
    1.1=>1.1.1    
        Hotfixed syntax error.    
        Changed PID default values.    
    1.0=>1.1    
        Refactored PID class to conform with SE coding style.    
        Fixed bug in PID initialization.    
        Implemented Save/Load support.    
*/

// User-Configuration options. //    

private bool AUTODETECT_COCKPIT = false; // Script attempts to find current piloted cockpit. Best for use on SP or on ships that will not carry multiple people.  
private const string COCKPIT_NAME = "[GDrive] Кокпит пилота"; // If AUTODETECT_COCKPIT is false, set this to the name of your main cockpit.  

private bool SIMPLE_OUTPUT = true; // Displayed only program status/Speed. Full output displays much more.  
private const string OUTPUT_LCD = "[GDrive] Дисплей пилота"; // Name for the output LCD.  

private const string THRUSTER_TAG = "[GDrive"; // Gravity Drive identification tag.    
private bool gyroStabilizerEnabled = true; // Recommended. Prevents unwanted rotations.    
private const bool initializeEnabled = true; // Recommended. Initializes the GravityDrives as enabled.    
private int currentGear = 0; // Starting gear. Starts in 3rd gear (index 2) which is unlimited.   
private bool dampenersEnabled = true; // Default dampeners setting. Suggested true for thrusterless ships, false for others.   

private const float MAX_SERVER_SPEED = 104.38f;

// Initial PID tuning.  
private static double Kp = 0.8;
private static double Ki = 0.2;
private static double Kd = 0;

// BROKEN FOR THIS VERSION
private readonly List<Vector3> AccelerationLimits = new List<Vector3>
    { 
        // Gear Units are in m/s along the desired axis. (Always cockpit relative)   
        //  new Vector3(X, Y, Z)    
        //  X == LeftRight    
        //  Y == UpDown    
        //  Z == ForwardBack    
 
        // First Gear (PRECISION MODE)    
        // 1 m/s on each axis    
        new Vector3(1, 1, 1), 
 
        // Second Gear (CRUISE MODE)    
        // 15 m/s on each axis    
        new Vector3(15, 15, 15), 
 
        // Third Gear (OVERDRIVE MODE)    
        // 3.4*10^38 m/s on each axis xD    
        new Vector3(float.MaxValue, float.MaxValue, float.MaxValue),
    };

// DO NOT EDIT BELOW //    

private const float G = 9.81f;
private const float ZeroEpsilon = 0.001f; // Floats smaller than this will be equivilent to 0.  
readonly List<GravityThruster> _gravThrusters = new List<GravityThruster>();
readonly List<IMyShipController> _shipControllers = new List<IMyShipController>();
private IMyShipController _controller;
readonly List<IMyGyro> _gyros = new List<IMyGyro>();
IMyTextPanel _outputLCD = null;
StringBuilder _outputString = new StringBuilder();
private readonly PID xAxisPID = new PID(Kp, Ki, Kd, -1, 1);
private readonly PID yAxisPID = new PID(Kp, Ki, Kd, -1, 1);
private readonly PID zAxisPID = new PID(Kp, Ki, Kd, -1, 1);
private readonly bool[] _setVelocityOnAxis = new bool[3];
private DateTime _driveIdleStart;
private TimeSpan _driveIdleMinLength = new TimeSpan(0, 0, 5);

private bool initialized;
private bool driveEnabled = initializeEnabled;

public Program() {
    SetUpdateFrequency();
}

public void Main(string argument)
{
    HandleArgument(argument);

    // Clear output 
    _outputString.Clear();
    _outputString.AppendLine("TOG v1.5.2 Running");
    _outputString.AppendLine($"LastRun: {DateTime.Now:HH:mm:ss.ff}");

    if (!initialized)
    {
        _outputString.AppendLine("Initializing...");
        Initialize();
    }

    if (!driveEnabled)
    {
        _outputString.AppendLine("GravityDrives Disabled.");
        _outputString.AppendLine("Run with arguments \"start\",\n  or \"toggle\" to enable.");
        FinalizeOutput();
        return;
    }
    _outputString.AppendLine("GravityDrives Enabled.");
    _outputString.AppendLine($"Current Gear: {currentGear + 1}");

    // Find the active ship controller.
    if (!_controller.IsUnderControl)
    {
        _controller = GetShipController();
    }

    bool dampenersOverride = _controller.DampenersOverride;
    if (dampenersOverride && dampenersEnabled)
    {
        // Disable manual dampeners if cockpit is capable of toggling dampeners.   
        dampenersEnabled = false;
    }
    bool dampeners = dampenersEnabled || dampenersOverride;
    if (dampeners && currentGear != 0) {
        currentGear = 0;
        SetUpdateFrequency();
    }
    _outputString.AppendLine($"Inertial Dampeners: {dampeners}");

    // Get velocity and convert it from World-relative to cockpit-relative.    
    Vector3 velocity = Vector3D.Transform(_controller.GetShipVelocities().LinearVelocity, MatrixD.Transpose(_controller.WorldMatrix.GetOrientation()));
    velocity.X = Math.Abs(velocity.X) > ZeroEpsilon ? velocity.X : 0;
    velocity.Y = Math.Abs(velocity.Y) > ZeroEpsilon ? velocity.Y : 0;
    velocity.Z = Math.Abs(velocity.Z) > ZeroEpsilon ? velocity.Z : 0;

    if (SIMPLE_OUTPUT)
    {
        _outputString.AppendLine($"Speed: {velocity.Length():N2}");
    }
    else
    {
        _outputString.AppendLine($"Velocity: {velocity.X:N2} | {velocity.Y:N2} | {velocity.Z:N2}");
    }

    // Desired Velocity    
    Vector3 velocityCommand;
    if (_controller.IsUnderControl)
    {
        velocityCommand = _controller.MoveIndicator;
    }
    else
    {
        _controller = GetShipController();
        velocityCommand = Vector3.Zero;
    }
    velocityCommand *= MAX_SERVER_SPEED; // velocityCommand is now the "desired velocity"

    if (!SIMPLE_OUTPUT)
    {
        _outputString.AppendLine($"Input: {velocityCommand.X:N2} | {velocityCommand.Y:N2} | {velocityCommand.Z:N2}");
    }

    // Check if within natural gravity   
    double naturalGravity = _controller.GetNaturalGravity().Length();
    if (naturalGravity > 0)
    {
        // naturalGravity is given as m/s/s, convert to G's   
        naturalGravity /= G;
        if (naturalGravity >= .5)
        {
            _outputString.AppendLine($"Natural Gravity of {naturalGravity:N2}G detected. Drives idle.");
            velocityCommand = Vector3D.Zero;
        }
        else
        {
            _outputString.AppendLine($"Natural Gravity of {naturalGravity:N2}G detected.\nDrives hampered by {naturalGravity * 2:P}.");
        }
    }
    else
    {
        // Check if drives should be set to idle.
        if (velocityCommand == Vector3.Zero)
        {
            if (DateTime.Now - _driveIdleStart < _driveIdleMinLength)
            {
                _outputString.AppendLine("GravityDrives Idle.");
                FinalizeOutput();
                return;
            }
            if (velocity == Vector3.Zero)
            {
                _driveIdleStart = DateTime.Now;
                _outputString.AppendLine("GravityDrives Idle.");
                foreach (GravityThruster gravThruster in _gravThrusters)
                {
                    gravThruster.ApplyThrustPct(Vector3.Zero);
                    EnableGyrostabilizer(false);
                }
                FinalizeOutput();
                return;
            }
        }
    }

    _driveIdleStart = DateTime.MinValue;

    if (dampeners)
    {
        _setVelocityOnAxis[0] = true;
        _setVelocityOnAxis[1] = true;
        _setVelocityOnAxis[2] = true;
    }
    else
    {
        _setVelocityOnAxis[0] = false;
        _setVelocityOnAxis[1] = false;
        _setVelocityOnAxis[2] = false;

        if (Math.Abs(velocityCommand.X) > ZeroEpsilon)
        {
            _setVelocityOnAxis[0] = true;
        }
        if (Math.Abs(velocityCommand.Y) > ZeroEpsilon)
        {
            _setVelocityOnAxis[1] = true;
        }
        if (Math.Abs(velocityCommand.Z) > ZeroEpsilon)
        {
            _setVelocityOnAxis[2] = true;
        }
    }
    Vector3 velocityError = velocity - velocityCommand;
    if (!SIMPLE_OUTPUT)
    {
        _outputString.AppendLine($"Error: {velocityError.X:N2} | {velocityError.Y:N2} | {velocityError.Z:N2}");
    }

    xAxisPID.InAuto = _setVelocityOnAxis[0];
    yAxisPID.InAuto = _setVelocityOnAxis[1];
    zAxisPID.InAuto = _setVelocityOnAxis[2];
    xAxisPID.Input = velocityError.X;
    yAxisPID.Input = velocityError.Y;
    zAxisPID.Input = velocityError.Z;

    xAxisPID.Compute();
    yAxisPID.Compute();
    zAxisPID.Compute();
    velocityCommand.X = (float)xAxisPID.Output;
    velocityCommand.Y = (float)yAxisPID.Output;
    velocityCommand.Z = (float)zAxisPID.Output;
    if (!SIMPLE_OUTPUT)
    {
        _outputString.AppendLine($"PID: {velocityCommand.X:N2} | {velocityCommand.Y:N2} | {velocityCommand.Z:N2}");
    }

    //ApplyAccelerationLimits(ref velocityCommand, controller, dampeners);

    if (gyroStabilizerEnabled)
    {
        EnableGyrostabilizer(Math.Abs(_controller.RollIndicator) < ZeroEpsilon && _controller.RotationIndicator.LengthSquared() < ZeroEpsilon);
    }

    // Convert from cockpit oriented velocityCommand to grid-oriented    
    Matrix controllerOrientation;
    _controller.Orientation.GetMatrix(out controllerOrientation);
    if (!SIMPLE_OUTPUT)
    {
        _outputString.AppendLine($"Thrust:   {velocityCommand.X:N2} | {velocityCommand.Y:N2} | {velocityCommand.Z:N2}");
    }
    velocityCommand = Vector3.Transform(velocityCommand, controllerOrientation);

    for (int index = 0; index < _gravThrusters.Count; index++)
    {
        GravityThruster gravThruster = _gravThrusters[index];
        gravThruster.ApplyThrustPct(velocityCommand);
    }
    FinalizeOutput();
}

private void FinalizeOutput()
{
    string finalOutput = _outputString.ToString();
    _outputLCD?.WriteText(finalOutput);
    Echo(finalOutput);
}

private void HandleArgument(string argument)
{
    if (argument == "") return;
    switch (argument.ToLower())
    {
        case "reset":
            initialized = false;
            break;
        case "stop":
        case "disable":
            EnableDrive(false);
            break;
        case "start":
        case "enable":
            EnableDrive(true);
            break;
        case "toggle":
        case "switch":
            EnableDrive(!driveEnabled);
            break;
        case "dampener":
        case "damp":
            dampenersEnabled = !dampenersEnabled;
            break;
        case "gyrostab":
        case "gyrostabilizer":
        case "gyro":
            gyroStabilizerEnabled = !gyroStabilizerEnabled;
            break;
        case "gear":
        case "shift":
            currentGear++;
            if (currentGear >= AccelerationLimits.Count)
            {
                currentGear = 0;
            }
            break;
    }
    SetUpdateFrequency();
}

private void ApplyAccelerationLimits(ref Vector3 velocityCommand, IMyShipController controller, bool dampeners)
{
    float shipMass = controller.CalculateShipMass().TotalMass;
    Vector3 maxThrust = Vector3.Zero;
    foreach (GravityThruster gravThruster in _gravThrusters)
    {
        foreach (KeyValuePair<Base6Directions.Axis, float> kvp in gravThruster.MaxThrust)
        {
            switch (kvp.Key)
            {
                case Base6Directions.Axis.LeftRight:
                    maxThrust.X += kvp.Value;
                    break;
                case Base6Directions.Axis.UpDown:
                    maxThrust.Y += kvp.Value;
                    break;
                case Base6Directions.Axis.ForwardBackward:
                    maxThrust.Z += kvp.Value;
                    break;
            }
        }
    }

    // TODO: Figure out what the fuck I was thinking here. I don't think I'm applying Acceleration limits right at all.
    Vector3 multiplier = AccelerationLimits[currentGear] / (maxThrust / shipMass);

    if (dampeners)
    {
        // Don't apply limit multiplier on dampeners    
        if (Math.Abs(velocityCommand.X) < ZeroEpsilon)
        {
            multiplier.X = 1;
        }
        if (Math.Abs(velocityCommand.Y) < ZeroEpsilon)
        {
            multiplier.Y = 1;
        }
        if (Math.Abs(velocityCommand.Z) < ZeroEpsilon)
        {
            multiplier.Z = 1;
        }
    }

    velocityCommand *= multiplier;

    velocityCommand.X = Clamp(velocityCommand.X, 1, -1);
    velocityCommand.Y = Clamp(velocityCommand.Y, 1, -1);
    velocityCommand.Z = Clamp(velocityCommand.Z, 1, -1);
}

private void Initialize()
{
    _gravThrusters.Clear();
    _shipControllers.Clear();
    _gyros.Clear();
    _outputLCD = null;

    GridTerminalSystem.GetBlocksOfType(_shipControllers, controller => controller.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType(_gyros, gyro => gyro.CubeGrid == Me.CubeGrid);
    _outputLCD = GridTerminalSystem.GetBlockWithName(OUTPUT_LCD) as IMyTextPanel;

    if (_shipControllers.Count == 0)
    {
        throw new Exception("No ship controllers found.");
    }

    _outputString.AppendLine($"Found: {_shipControllers.Count} ship controllers.");

    // Initialize lists for gravity generators.       
    var tempBlocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.SearchBlocksOfName(THRUSTER_TAG, tempBlocks, block => block is IMyGravityGenerator || block is IMyVirtualMass);

    _outputString.AppendLine($"Found {tempBlocks.Count} {THRUSTER_TAG}] tagged blocks.");

    var thrusterGravBlocks = new Dictionary<string, List<IMyGravityGenerator>>();
    var thrusterMassBlocks = new Dictionary<string, List<IMyVirtualMass>>();
    var thrusterNames = new List<string>();

    for (int i = 0; i < tempBlocks.Count; i++)
    {
        IMyTerminalBlock block = tempBlocks[i];
        int tagPos = block.CustomName.IndexOf(THRUSTER_TAG, 0, StringComparison.OrdinalIgnoreCase);
        string thrusterName = block.CustomName.ToLower().Substring(tagPos).Remove(0, THRUSTER_TAG.Length).Split(']')[0];
        var gravityGenerator = block as IMyGravityGenerator;
        if (gravityGenerator != null)
        {
            if (thrusterGravBlocks.ContainsKey(thrusterName))
            {
                thrusterGravBlocks[thrusterName].Add(gravityGenerator);
            }
            else
            {
                thrusterGravBlocks.Add(thrusterName,
                                        new List<IMyGravityGenerator>
                                        {
gravityGenerator
                                        });
                if (!thrusterNames.Contains(thrusterName))
                {
                    thrusterNames.Add(thrusterName);
                }
            }
        }
        else
        {
            if (thrusterMassBlocks.ContainsKey(thrusterName))
            {
                thrusterMassBlocks[thrusterName].Add((IMyVirtualMass)block);
            }
            else
            {
                thrusterMassBlocks.Add(thrusterName,
                                        new List<IMyVirtualMass>
                                        {
(IMyVirtualMass)block
                                        });
                if (!thrusterNames.Contains(thrusterName))
                {
                    thrusterNames.Add(thrusterName);
                }
            }
        }
    }

    for (int index = 0; index < thrusterNames.Count; index++)
    {
        string thrusterName = thrusterNames[index];
        var thruster = new GravityThruster(thrusterName, thrusterGravBlocks[thrusterName], thrusterMassBlocks[thrusterName]);
        _gravThrusters.Add(thruster);
    }

    _outputString.AppendLine($"GravityThruster Dectection: {_gravThrusters.Count} Thrusters");

    if (Storage != "")
    {
        _outputString.AppendLine("Settings loaded from storage.");
        Load();
    }

    _controller = GetShipController();

    _outputString.AppendLine("Initialized!");

    initialized = true;
}

public void Save() { Storage = $"{driveEnabled},{dampenersEnabled},{gyroStabilizerEnabled},{currentGear}"; }

public void Load()
{
    string[] parameters = Storage.Split(',');
    driveEnabled = bool.Parse(parameters[0]);
    dampenersEnabled = bool.Parse(parameters[1]);
    gyroStabilizerEnabled = bool.Parse(parameters[2]);
    currentGear = int.Parse(parameters[3]);
}

private float Clamp(float value, float max, float min)
{
    if (value > max)
        value = max;
    else
        if (value < min)
        value = min;
    return value;
}

private IMyShipController GetShipController()
{
    if (AUTODETECT_COCKPIT)
    {
        foreach (IMyShipController shipController in _shipControllers)
        {
            if (shipController.IsUnderControl)
            {
                return shipController;
            }
        }


        // Get a default controller    
        // TODO: Get main remote/seat    
        return _shipControllers[0];
    }

    var cockpit = GridTerminalSystem.GetBlockWithName(COCKPIT_NAME) as IMyShipController;

    if (cockpit == null)
    {
        throw new Exception($"Could not find cockpit named {COCKPIT_NAME}");
    }

    return cockpit;
}

private void EnableDrive(bool enable)
{
    driveEnabled = enable;

    if (!driveEnabled)
    {
        // Turn off each thruster.    
        foreach (GravityThruster thruster in _gravThrusters)
        {
            thruster.ApplyThrustPct(Vector3D.Zero);
        }
        EnableGyrostabilizer(false);
    }
}

private void EnableGyrostabilizer(bool enable)
{
    if (enable)
    {
        foreach (IMyGyro gyro in _gyros)
        {
            gyro.SetValueBool("Override", true);
            gyro.SetValueFloat("Pitch", 0);
            gyro.SetValueFloat("Yaw", 0);
            gyro.SetValueFloat("Roll", 0);
        }
    }
    else
    {
        foreach (IMyGyro gyro in _gyros)
        {
            gyro.SetValueBool("Override", false);
        }
    }
}

private void SetUpdateFrequency() {
    if (!driveEnabled) {
        Runtime.UpdateFrequency = UpdateFrequency.None;
        return;
    }
    if (dampenersEnabled) {
        Runtime.UpdateFrequency = UpdateFrequency.Update1;
        currentGear = 0;
        return;
    }
    switch (currentGear) {
        case 0:
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            break;
        case 1:
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            break;
        case 2:
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            break;
    }
}

class GravityThruster
{

    // Center of Thrust for this Thruster (Ship-Relative)       
    public readonly Vector3D CoT;

    public string ThrusterName;
    public IReadOnlyDictionary<Base6Directions.Axis, float> MaxThrust => m_maxThrust;

    // Virtual Thrust this thruster provides.    
    float vitrualMass = 0f;

    // Private variables    
    readonly Dictionary<Base6Directions.Axis, List<IMyGravityGenerator>> m_gravGens = new Dictionary<Base6Directions.Axis, List<IMyGravityGenerator>>();
    private readonly List<IMyVirtualMass> m_massBlocks = new List<IMyVirtualMass>();
    private readonly Dictionary<Base6Directions.Axis, float> m_maxThrust = new Dictionary<Base6Directions.Axis, float>();

    public GravityThruster(string thrusterName, IReadOnlyCollection<IMyGravityGenerator> gravityGenerators, IReadOnlyCollection<IMyVirtualMass> massBlocks)
    {
        ThrusterName = thrusterName;

        // Calculate Center of Thrust (CoT) and massBlock BoundingBox.    
        // CoT is the average of the mass block positions.    
        // TODO: Calculate CoT properly for SpaceBalls with variable mass.   
        CoT = new Vector3D();
        var massPositions = new List<Vector3I>(massBlocks.Count);
        foreach (IMyVirtualMass massBlock in massBlocks)
        {
            vitrualMass += massBlock.VirtualMass;
            CoT += massBlock.Position;
            massPositions.Add(massBlock.Position);
        }
        CoT /= massBlocks.Count;

        BoundingBoxI massBlockBB = BoundingBoxI.CreateFromPoints(massPositions);
        EstablishGravFields(massBlockBB, gravityGenerators);

        m_massBlocks.AddRange(massBlocks);
        // Allocate lists for each axis.       
        for (int i = 0; i < 3; i++)
        {
            var axis = (Base6Directions.Axis)i;
            m_gravGens.Add(axis, new List<IMyGravityGenerator>());
        }

        // Sort each gravity generator by axis.       
        foreach (IMyGravityGenerator gravGen in gravityGenerators)
        {
            m_gravGens[Base6Directions.GetAxis(gravGen.Orientation.Up)].Add(gravGen);
        }

        // Calculate thrust per axis.    
        foreach (Base6Directions.Axis axis in Enum.GetValues(typeof(Base6Directions.Axis)))
        {
            m_maxThrust.Add(axis, vitrualMass * (G * m_gravGens[axis].Count));
        }
    }

    public void ApplyThrustPct(Vector3 thrustVector)
    {
        if (Math.Abs(thrustVector.Z) < ZeroEpsilon)
        {
            foreach (IMyGravityGenerator gravGen in m_gravGens[Base6Directions.Axis.ForwardBackward])
            {
                gravGen.Enabled = false;
            }
        }
        else
        {
            Base6Directions.Direction dir = thrustVector.Z < 0 ? Base6Directions.Direction.Forward : Base6Directions.Direction.Backward;
            SetGenerators(dir, Math.Abs(thrustVector.Z));
        }

        if (Math.Abs(thrustVector.X) < ZeroEpsilon)
        {
            foreach (IMyGravityGenerator gravGen in m_gravGens[Base6Directions.Axis.LeftRight])
            {
                gravGen.Enabled = false;
            }
        }
        else
        {
            Base6Directions.Direction dir = thrustVector.X < 0 ? Base6Directions.Direction.Left : Base6Directions.Direction.Right;
            SetGenerators(dir, Math.Abs(thrustVector.X));
        }

        if (Math.Abs(thrustVector.Y) < ZeroEpsilon)
        {
            foreach (IMyGravityGenerator gravGen in m_gravGens[Base6Directions.Axis.UpDown])
            {
                gravGen.Enabled = false;
            }
        }
        else
        {
            Base6Directions.Direction dir = thrustVector.Y > 0 ? Base6Directions.Direction.Up : Base6Directions.Direction.Down;
            SetGenerators(dir, Math.Abs(thrustVector.Y));
        }

        bool enableMass = !Vector3.IsZero(thrustVector);
        foreach (IMyVirtualMass massBlock in m_massBlocks)
        {
            massBlock.Enabled = enableMass;
        }
    }

    /// <summary>    
    /// Gets the max thrust of this gravity drive along the provided axis.    
    /// </summary>    
    /// <returns>    
    /// Thrust along axis, in N    
    /// </returns>    
    public float GetMaxThrust(Base6Directions.Axis axis)
    {
        return m_maxThrust[axis];
    }

    private void SetGenerators(Base6Directions.Direction direction, float percent)
    {
        Base6Directions.Axis axis = Base6Directions.GetAxis(direction);
        foreach (IMyGravityGenerator gravGen in m_gravGens[axis])
        {
            float grav = gravGen.Orientation.Up == direction ? -percent : percent;
            gravGen.SetValueFloat("Gravity", grav * G);
            gravGen.Enabled = true;
        }
    }

    /// <summary>    
    /// Sets the provided gravity generator's field sizes to cover the provided BoundingBox.    
    /// </summary>    
    private void EstablishGravFields(BoundingBoxI massBB, IEnumerable<IMyGravityGenerator> gravGens)
    {
        foreach (IMyGravityGenerator gravGen in gravGens)
        {
            Matrix gravGenOrientationMatrix;
            gravGen.Orientation.GetMatrix(out gravGenOrientationMatrix);
            Vector3I dist = Vector3I.Max(Vector3I.Abs(gravGen.Position - massBB.Min), Vector3I.Abs(gravGen.Position - massBB.Max));
            Vector3 fieldSize = Vector3.Abs(Vector3.Transform(dist, Matrix.Transpose(gravGenOrientationMatrix))) * 5;
            gravGen.SetValue("Width", fieldSize.X + 6);
            gravGen.SetValue("Height", fieldSize.Y + 6);
            gravGen.SetValue("Depth", fieldSize.Z + 6);
        }
    }
}


class PID
{
    public double SetPoint { get; set; }
    public double Input { get; set; }
    public double Output { get; private set; }

    public double OutputMax
    {
        get { return _outputMax; }
        set
        {
            if (value < OutputMin)
                return;
            _outputMax = value;

            if (Output > value)
                Output = value;

            if (_iTerm > value)
                _iTerm = value;
        }
    }

    public double OutputMin
    {
        get { return _outputMin; }
        set
        {
            if (value > OutputMax)
                return;
            _outputMin = value;

            if (Output < value)
                Output = value;

            if (_iTerm < value)
                _iTerm = value;
        }
    }

    public bool InAuto
    {
        get { return _inAuto; }
        set
        {
            if (value && !_inAuto)
            {
                // Manual to auto  
                _lastInput = Input;
                _iTerm = Output;
                if (_iTerm > OutputMax)
                    _iTerm = OutputMax;
                else
                    if (_iTerm < OutputMin)
                    _iTerm = OutputMin;
            }
            _inAuto = value;
        }
    }

    private ulong SampleTime = 1000 / 60;

    private double _iTerm,
                    _lastInput;
    private double _Kp,
                    _Ki,
                    _Kd;
    private DateTime _lastTime = DateTime.MinValue;
    private double _outputMax;
    private double _outputMin;
    private bool _inAuto;

    public PID(double Kp, double Ki, double Kd, double outMin, double outMax)
    {
        SetTunings(Kp, Ki, Kd);
        _Kp = Kp;
        _Ki = Ki;
        _Kd = Kd;
        OutputMin = outMin;
        OutputMax = outMax;
    }

    public void Compute()
    {
        if (!InAuto)
        {
            Output = 0;
            return;
        }
        DateTime currentTime = DateTime.Now;
        if (currentTime - _lastTime <= TimeSpan.FromMilliseconds(SampleTime))
            return;

        double error = SetPoint - Input;
        double errorDeriv = Input - _lastInput;

        _iTerm += (_Ki * error);
        if (_iTerm > OutputMax)
            _iTerm = OutputMax;
        else
            if (_iTerm < OutputMin)
            _iTerm = OutputMin;

        Output = (_Kp * error) + _iTerm + (_Kd * errorDeriv);
        if (Output > OutputMax)
            Output = OutputMax;
        else
            if (Output < OutputMin)
            Output = OutputMin;

        _lastInput = Input;
        _lastTime = currentTime;
    }

    public void SetTunings(double Kp, double Ki, double Kd)
    {
        double SampleTimeInSec = (double)SampleTime / 1000;
        _Kp = Kp;
        _Ki = Ki * SampleTimeInSec;
        _Kd = Kd / SampleTimeInSec;
    }

    public void SetSampleTime(ulong NewSampleTime)
    {
        double ratio = (double)NewSampleTime / SampleTime;
        _Ki *= ratio;
        _Kd /= ratio;
        SampleTime = NewSampleTime;
    }
}


#endregion

/// КОД ДЛЯ ПРОГРАММИРУЕМОГО БЛОКА - КОНЕЦ
#region PreludeFooter
    }
}
#endregion