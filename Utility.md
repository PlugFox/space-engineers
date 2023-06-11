=====================================

Найти блоки определенного типа и выполнить действие

```csharp
private delegate void DoWhenBlockOfTypeCallback<T>(T block);
void DoWhenBlockOfType<T>(IMyTerminalBlock Block, DoWhenBlockOfTypeCallback<T> callback)
{ try { if (Block is T) callback((T)Block); } catch (Exception e) { Echo($"! Ошибка выполнения скрипта: {e.Message}"); } }

void DoWithAllBlocksOfType<T>(DoWhenBlockOfTypeCallback<T> callback) where T : class, IMyTerminalBlock
{ var blocks = GetBlocksOfType<T>(); blocks.ForEach(block => callback(block)); }

List<T> GetBlocksOfType<T>() where T : class, IMyTerminalBlock
{ var blocks = new List<T>(); GridTerminalSystem.GetBlocksOfType<T>(blocks, block => block.CubeGrid == currentGrid); return blocks; }
```
