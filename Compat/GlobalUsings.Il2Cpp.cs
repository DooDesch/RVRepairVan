// IL2CPP backend (net6.0) global usings.
//
// Strategy: import the Il2Cpp* game namespaces globally so the rest of the source
// uses UNQUALIFIED game type names (RV, Marco, DialogueController, MoneyManager, ...)
// that will resolve identically under the Mono backend (which imports the plain
// ScheduleOne.* namespaces in Compat/GlobalUsings.Mono.cs). UnityEngine is NOT
// prefixed in il2cpp interop, so it is backend-agnostic.
//
// NOTE: because UnityEngine is imported here and System is imported implicitly,
// the bare identifier `Object` is ambiguous - always write `UnityEngine.Object`.

global using UnityEngine;
global using Il2CppScheduleOne.Property;
global using Il2CppScheduleOne.Money;
global using Il2CppScheduleOne.Dialogue;
global using Il2CppScheduleOne.NPCs;
global using Il2CppScheduleOne.NPCs.CharacterClasses;
global using Il2CppScheduleOne.Persistence;
global using Il2CppScheduleOne.DevUtilities;
