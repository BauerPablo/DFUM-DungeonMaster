# Manual Testing Guide: Party Justice, World Stasis & Narrative Prison Escapes (Phase 21)

## Overview
This testing guide covers all features introduced in **Phase 21 (v0.9.21)** of the DFUM TTRPG Master Suite:
1. **Guard Arrest & Combat Stasis (Stand Down):**
   - Guards within 25m halt attacks and enter pacified stand-down mode when surrender dialogue is open.
   - Accused player gains combat stasis (immunity to guard damage while resolving dialogue/trial).
2. **Synchronized Courtroom & Party Broadcast:**
   - The defendant player alone interacts with `DaggerfallCourtWindow`.
   - Connected peers receive an ambient UI broadcast: `✦ TRIBUNAL EN SESIÓN ✦` displaying charges, defense, and verdict.
3. **Imprisonment, Inmate Escape & "Choose Your Own Adventure" Party Jailbreak:**
   - Suppresses single-player destructive `RaiseTime` skips during jail sentences.
   - 3 narrative paths via Pixel Art theme:
     - **Serve Sentence:** Communal rest/wait with party voting.
     - **Inmate Solo Escape Roll:** D20 check inside cell (success breaks out as fugitive, failure doubles penalty).
     - **Party Jailbreak:** 3-stage branching adventure with DM difficulty (DC); failure imprisons the whole party.
4. **Vampirism & Lycanthropy Anti-Skip Lifecycle:**
   - Local conversion cutscenes with damage stasis.
   - Day-by-day progression without the classic 14-day time leap.
   - Placeholder screen during lethargy: `[Acá va video en loop - Letargo Vampírico]`.
5. **Shared Daedric Invocations & Cutscene Stasis:**
   - Daedric prince and quest cutscenes synchronized across all party members with damage immunity.
6. **Silent Dungeon Teleport Traps:**
   - Teleport pads silently move victim without chat spoilers.
7. **Guild Training & Temple Wait Voting:**
   - Multi-hour services require unanimous party vote to advance world time.
8. **Communal Rest Ambush Awakening:**
   - Hostiles wake up all sleeping players with a 3-second defensive reaction stasis.

---

## Testing Status Dashboard

| Status | Total Cases | Cases | Result |
| :--- | :---: | :---: | :---: |
| **SPECIFIED (Phase 21)** | **8** | Cases 1–8 | **Documented & Ready for Implementation** |

---

## Test Cases Outline

### [ ] Case 1: Guard Stand Down & Arrest Stasis
- [ ] Commit a crime in a city with guards present.
- [ ] When attacked, verify guards in radius cease attacking once `SurrenderToGuardsDialogue` opens.
- [ ] Verify player takes no damage while dialogue is open.

### [ ] Case 2: Defendant Trial & Party Broadcast
- [ ] Enter trial (`DaggerfallCourtWindow`).
- [ ] Verify other party members see `✦ TRIBUNAL EN SESIÓN ✦` with defendant's charges and verdict in real time.

### [ ] Case 3: Inmate Solo Escape (3-Step Choose Your Own Adventure with DM DC & Interactive D20)
- [ ] Al ser condenado a prisión, el recluso abre el dilema y selecciona Opción 2: "Fuga Solitaria (3 Pasos)".
- [ ] Elige una táctica para el Paso 1 (p. ej., forzar barrotes, robar llaves o remover losa).
- [ ] La ventana de dilema se cierra/minimiza y al DM le aparece la ventana modal de Petición de Tirada con slider de DC (8 a 25).
- [ ] El DM fija la DC y presiona "Exigir Tirada al Jugador".
- [ ] Se despliega la bandeja D20 3D superior del recluso (`DMDiceTrayController`), quien realiza la tirada interactiva con suspense.
- [ ] Si acierta: avanza al Paso 2, reabre el dilema con feedback verde.
- [ ] Si falla: la condena se duplica inmediatamente, vuelve al Paso 1 y reabre el dilema con feedback rojo.
- [ ] Superar el Paso 3 libera exitosamente al prisionero.

### [ ] Case 4: Party Jailbreak (3-Step Choose Your Own Adventure with DM DC & Interactive D20)
- [ ] El recluso o aliados seleccionan Opción 3: "Fuga Grupal (3 Pasos)".
- [ ] Eligen la táctica del grupo para el paso activo (Paso 1 Infiltración, Paso 2 Asalto/Extracción, Paso 3 Huida Coordinada).
- [ ] La ventana se minimiza y al DM le aparece la solicitud para definir la DC.
- [ ] El jugador ejecuta la tirada en el D20 interactivo.
- [ ] Si acierta: avanza al siguiente paso hasta la victoria en el Paso 3.
- [ ] Si falla: suena alarma, toda la party es capturada, la condena se duplica y el progreso se reinicia al Paso 1.

### [ ] Case 5: Communal Prison Wait (Rest Window Countdown & Client Sync)
- [ ] La party acuerda cumplir la condena (Opción 1: Espera Colectiva).
- [ ] Tras la votación unánime, inicia el descanso comunal autoritativo.
- [ ] Verificar que en el Host el botón "Stop" está deshabilitado/oculto para impedir cancelaciones unilaterales.
- [ ] Verificar que en todos los clientes conectados se replica la ventana de descanso con el contador pergamino en cuenta regresiva idéntico y sin botón de Stop.

### [ ] Case 6: Vampirism & Lycanthropy Anti-Skip
- [ ] Contract disease. Verify dreams play locally with stasis.
- [ ] At day 3, verify time DOES NOT leap 14 days; player enters lethargy screen with loop placeholder.
- [ ] Verify player can vote on party rests and awakes in cemetery without time desync.

### [ ] Case 7: Shared Daedric Invocations & Cutscene Stasis
- [ ] Summon Daedric prince at coven.
- [ ] Verify all party members see the video simultaneously and are immune to attacks.

### [ ] Case 8: Silent Dungeon Teleport Traps & Guild Training Vote
- [ ] Step on dungeon teleport trap; verify no chat spoiler is emitted.
- [ ] Use guild training; verify `MsgPartyWaitVote` prompts the party.

---

## Menú de Pruebas DM: Pestaña "Eventos & Justicia"

Para facilitar la verificación de todos estos escenarios sin tener que esperar eventos aleatorios en el mundo, se agregó la pestaña **"Eventos & Justicia"** en la **TTRPG Master Suite (Ctrl+F10 / Ctrl+F11)**:

1. **Selector de Personaje Objetivo:**
   - Permite elegir a cualquier jugador de la party: `[X] Host (Nombre)` o `[ ] Cliente 1 (Nombre)`.
   - Muestra tarjeta de estado en tiempo real (HP, crimen actual, stasis de combate/justicia).

2. **Acciones Rápidas de Prueba:**
   - **Caso 1: Disparar Arresto & Juicio:** Fuerza el Stand-Down de guardias, otorga inmunidad por stasis al jugador seleccionado y abre el diálogo de rendición / juicio local. Botón adicional para forzar Stand-Down en la zona.
   - **Caso 2: Encarcelamiento & Dilema de Fuga:** Despliega la ventana con las 3 opciones (Espera de Party con votación, Fuga Solitaria D20 con duplicación de pena por fallo, y Fuga Cooperativa en 3 fases con DC configurable por slider del DM).
   - **Caso 3: Transformación Licántropo / Vampiro:** Muestra la ventana con `[ ACÁ VA VIDEO EN LOOP ]` sin saltar días en el calendario del mundo.
   - **Caso 4: Invocación Daédrica Compartida:** Sincroniza la cinemática en toda la party y activa stasis de daño.
   - **Caso 5: Trampa de Teletransporte:** Mueve al jugador silenciosamente sin spoilers en el chat.
   - **Caso 6: Avance Temporal / Descanso por Gremio:** Lanza la votación comunal de descanso.

---

## Sistema Universal de Ventanas IMGUI (Minimizar, Expandir, Cerrar & Dock Superior Derecho)

Todas las ventanas y modales de la interfaz gráfica (TTRPG Suite, Multiplayer, DM Tool, DM Loot, Quests, Interacción, Comercio, Invitación de Comercio, Dilema de Calabozo, Transmutación, y Tirada DM) cuentan con controles de ventana universales en su barra superior derecha:

1. **Botón Minimizar (_):**
   - Oculta el cuerpo de la ventana y coloca un badge interactivo en la **esquina superior derecha** de la pantalla.
   - A medida que se minimizan más ventanas, los badges se alinean horizontalmente de derecha a izquierda uno al lado del otro ([ 🗝 Calabozo ] [ 👑 TTRPG Suite ]).
   - Hacer clic sobre cualquier badge del dock restaura inmediatamente la ventana a su posición y estado anterior.

2. **Botón Maximizar / Restaurar (□ / ❐):**
   - Alterna entre el tamaño estándar de la ventana y pantalla completa (`new Rect(20, 20, Screen.width - 40, Screen.height - 40)`), permitiendo una inspección holgada de listados largos (spawns, quests, loot) y restaurando su posición original al volver a presionar.

3. **Botón Cerrar (✕):**
   - Cierra la ventana y limpia de forma segura su estado o dispara sus callbacks de cancelación (ej. cancelar comercio o rechazar invitación de trade).
