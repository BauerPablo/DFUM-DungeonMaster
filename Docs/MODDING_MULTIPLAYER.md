# Daggerfall Unity Multiplayer - Native Sync API

Esta guía explica cómo implementar sincronización de red nativa para tus mods de DFU, utilizando el `DFUBridgeComponent`.

## Filosofía: Servidor Autoritativo / Cliente Pasivo

1. **Servidor (Host)**: Posee la verdad absoluta del estado. Procesa lógica y llama a `SendStateUpdate()`.
2. **Cliente**: Es pasivo en `Update()`. Solo reacciona cuando el servidor envía datos a través de `ApplyState()`.

## Cómo crear un componente sincronizable

Para que tu objeto se sincronice automáticamente, seguí estos pasos:

1. **Heredá de `DFUBridgeComponent`**:

   ```csharp
   public class TuComponente : DFUBridgeComponent
   {
       // ... lógica del mod ...
   }
   ```

2. **Implementá la serialización**:
   Sobrescribí `GetState()` para enviar tus datos y `ApplyState()` para recibirlos.

   ```csharp
   public override byte[] GetState()
   {
       // Convertí tus datos a byte[]
       return System.BitConverter.GetBytes(tuVariable);
   }

   public override void ApplyState(byte[] state)
   {
       // Convertí byte[] a tus datos y aplicalos
       tuVariable = System.BitConverter.ToInt32(state, 0);
   }
   ```

3. **Enviá actualizaciones desde el servidor**:
   Cuando detectes un cambio en el servidor, llamá a `SendStateUpdate()`:

   ```csharp
   if (isServer)
   {
       SendStateUpdate(GetState());
   }
   ```

## Ventajas
- **Agnóstico de transporte**: Funciona hoy con Mirror, funcionará mañana con NGO.
- **Robustez**: Manejo automático de registro de objetos en el `DFUNetworkManager`.
