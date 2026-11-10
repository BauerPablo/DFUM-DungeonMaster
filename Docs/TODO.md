# DFU-Multiplayer — Backlog & TODO

Este archivo registra las funcionalidades, mejoras estéticas y optimizaciones planificadas para fases posteriores.

---

## 1. Animaciones y Sprites de Jugador

- [ ] **Animaciones de Caminar Agachado (Crouch-Walk):**
  - Actualmente, el estado agachado utiliza sprites estáticos en cuclillas (`25-0.PNG` de frente y `25-1.PNG` de espaldas).
  - Diseñar e incorporar los frames de animación de pasos agachados (caminar en cuclillas) para las distintas clases/archivos de texturas (`TEXTURE.488`, etc.).
  - Sincronizar el ciclo de fotogramas cuando el jugador se desplaza mientras mantiene la tecla de agachado presionada (`isCrouching && isMoving`).

- [ ] **Sprites de Agachado para Otras Clases:**
  - Incorporar frames de agachado para el resto de los arquetipos de personaje (`Ladrón/484`, `Mago/476`, `Caballero/490`, etc.) y sus variantes femeninas.



v1.0.0
- [ ] **Menu de DM para crear Histoprias de Rol**
  - Menu de dialogos de NPCs
  - Menu de Spawn de enemigos ampliado a todos los mosntruos y NPCs posibles de DF
  - Menu de teletransporte
  - Menu de Loot mas completo pudiendo elegir items y caracteristicas del item
  - Menu de Quest creator, como crear quest, objetivo, textos, recompensa
  - DM NPC controler, como poseer a un NPC de los que se mueven. DEsde un mosntruo/enemigo a un NPC de ciudad.
  - Menu de eventos
  - Comandos para generar dinero o evitar cobros como personaje. No es parte de la party de los jugadores pero si esta conectado al mismo servidor. 
    Es parte de la party pero con un rol especial de DM.
- [ ] **Chats**
- [ ] **Lenguajes**
- [ ] Modding (climates & Calories)
- [ ] Mods catcher, un sistema de verificacion de mods. El host es el que define la configuracion de los mods, se toma la configuracion de el host y se replica para esa partida.
- [ ] **God fly**
  - Teletransportarse
  - Volar y tener una vista aerea, tambien una vista cenital
- [ ] Mejorar y parametrizar como ver los nombres de los personajes
- [ ] Tutorial de instalacion con los mods

- Refactorizar el menu viejo con la nueva estetica, agregando tambien los controles del DM y el keymap.
v1.1.0
- [ ] **Army Sys**
  - Crear la posibilidad de contratar mercenarios para tener mini ejercitos
  - Sistema de contratacion
  - Mecanica de seguimiento
  - Mecanica de control y ordenes de batalla (listar todas las ordenes de batalla)
  - Sistema de equipos dentro del ejercito (colores/nombres)
  - Mapa de batalla
  - Menu de ejercito
- [ ] Estilo de menus mas lindos estilo pixelart
- [ ] Mejorar el mapa con rutas y trazos, zoom y mas amigable.
- [ ] Crear campamentos
- [ ] Visualizar tiradas
- [ ] Mascotas
---

## 2. Audio & Efectos

- [ ] **Efectos de Hechizos en Tercera Persona:**
  - Replicar partículas y luces de lanzamiento de magia (`EntityEffectManager`) en los avatares remotos cuando disparan o lanzan conjuros.

---

## 3. Optimización & Red

- [ ] **Interpolación de Rotación Suavizada (Head / Pitch Look):**
  - Añadir inclinación vertical o cabeceo a los carteles de nombre y proyectiles en base al pitch del jugador remoto.

---

## 4. Hoja de Ruta de Mods por Tiers de Riesgo

### 🟢 Tier 1 — v0.9.22 (Cosméticos, UI, Audio y Cámara: Cliente Puro)
> **Riesgo Netcode: Cero.** No afectan simulación, física de red ni spawns. Se ejecutan 100% en el cliente local.

- [ ] **Vanilla Enhanced:** Mejora visual preservando el pixel art original.
- [ ] **Daggerfall Expanded Textures:** Prerrequisito de texturas y assets base.
- [ ] **Diverse Weapons:** Modelos y sprites visuales para armas.
- [ ] **Real Grass:** Hierba y vegetación procedural en terreno.
- [ ] **Dynamic Skies:** Cielos procedurales con nubes, lunas y estrellas.
- [ ] **Convenient Questlog:** Interfaz de misiones mejorada y más legible.
- [ ] **Daggerstats:** Pantalla de estadísticas detalladas y progreso.
- [ ] **Shield Widget:** Icono/widget de escudo equipado en primera persona.
- [ ] **Paperdoll Overhaul:** Correcciones a la interfaz visual del inventario/muñeco.
- [ ] **Atlas of the Iliac Bay:** Ajustes de nombres y etiquetas en mapas/interfaz.
- [ ] **Better Ambience:** Efectos de audio ambiental inmersivo.
- [ ] **Drafty Secret Doors:** Pistas de audio (viento) cerca de puertas secretas.
- [ ] **Dramatic Deaths:** Animaciones de muerte más orgánicas en clientes.
- [ ] **Bloodfall:** Efectos visuales de sangre y partículas de combate.
- [ ] **Readied Spellcasting Hands:** Gráficos de manos preparadas al lanzar hechizos.
- [ ] **Eye of the Beholder:** Perspectiva de cámara en tercera persona local.
- [ ] **Selfie Cam:** Cámara libre para fotos del personaje.
- [ ] **Falling Down:** Protección local contra caídas al vacío por bugs de colisión.

---

### 🟡 Tier 2 — v0.9.23 (Mundo, Ciudades y Caminos: Geometría Determinista)
> **Riesgo Netcode: Medio.** Requieren paridad 1:1 en el manifest de red (SHA-256) para evitar que los jugadores caminen sobre paredes o choquen con terreno invisible.

- [ ] **Basic Roads:** Conexión de ciudades y pueblos mediante caminos transitables.
- [ ] **Beautiful Cities / Villages of Daggerfall:** Transformación visual y arquitectónica completa de poblados.
- [ ] **World of Daggerfall Project:** Campamentos, ruinas y topografía en la naturaleza.
- [ ] **Finding My Religion:** Rediseño interior de templos según su deidad.
- [ ] **Windmills of Daggerfall:** Molinos detallados en exteriores.
- [ ] **Village of Fishguard:** Nueva aldea costera y mazmorra propia.
- [ ] **Fixed Desert Architecture:** Variantes arquitectónicas en regiones desérticas.
- [ ] **Fixed Dungeon Exteriors:** Exteriores de mazmorras adaptados a su temática.
- [ ] **Seat of the Direnni:** Rediseño histórico y visual de la Torre Direnni.
- [ ] **Orsinium Belongs to the Orcs!:** Modificación visual y poblacional de Orsinium.
- [ ] **Harvestable Crops:** Cultivos cosechables en granjas del mapa.
- [ ] **Detailed Ships:** Rediseño interior y exterior de embarcaciones.

---

### 🔴 Tier 3 — v0.9.24 (Simulación, Spawns, Supervivencia y Mecánicas Complejas)
> **Riesgo Netcode: Alto.** Requieren componentes de sincronización (`DFUBridgeComponent`), autoridad del Host y/o integración con la suite del DM.

- [ ] **Climates & Calories:** Supervivencia, hambre, sed y temperatura (integrado a Fase 18).
- [ ] **Daggerfall Enemy Expansion:** 50 enemigos nuevos (requiere integrar con Spawner del DM).
- [ ] **Warm Ashes Wilderness Events:** Eventos de asalto y patrullas aleatorias en el mapa.
- [ ] **Lives of Raiders:** Batallas a gran escala cerca de granjas.
- [ ] **Things Betwixt:** Encuentros aleatorios durante viajes rápidos.
- [ ] **Random Little Quests:** Sistema de misiones emergentes generadas en runtime.
- [ ] **Decorator:** Muebles y objetos colocables (requiere sincronización de entidades).
- [ ] **Piece of Chalk:** Marcas de tiza en mazmorras (requiere sincronización en red).
- [ ] **Airships:** Naves aéreas pilotables con control de tripulación.
- [ ] **Come Sail Away:** Barcos y navegación a vela en tiempo real.
- [ ] **Dragon Rider:** Monturas aéreas (requiere sincronizar puppet con otros jugadores).
- [ ] **Roleplay and Realism Items:** Modificación de loot, pociones y economías de tiendas.
- [ ] **Unleveled Loot:** Desactivación de loot escalado (lógica autoritativa).
- [ ] **Guaranteed Ingredient Lootdrops:** Fórmulas de botín de ingredientes.
- [ ] **Language Skills Overhaul:** Diplomacia y reclutamiento de enemigos pacificados.
- [ ] **Skulduggery:** Habilidades de sigilo y forzado de cerraduras.
- [ ] **Killer Instincts:** Tácticas de combate e inteligencia artificial enemiga.
- [ ] **Bards and Balladeers:** Facción y mecánicas musicales/actuación.
- [ ] **Free Rein:** Físicas y control avanzado de caballos.
- [ ] **Arrow Variety:** Flechas con efectos especiales de área/daño.
- [ ] **Borrow Skin:** Mod de druida / metamorfosis.
- [ ] **Astrid's Guide to Necromancy:** Summons y secuaces (validar límite de entidades en party).
- [ ] **Astrid's Guide to Vampirism:** Mecánicas de vampirismo.
- [ ] **Weathermancy:** Control y conjuros meteorológicos.
- [ ] **Sit and Loiter:** Sentarse en muebles (requiere animaciones/sincronización de estado).
- [ ] **Paints and Dyes:** Tintes de ropa y armadura (integrar con sync de avatares).
- [ ] **Useful Console Commands:** Comandos de consola útiles para testing y DM.


---

## 5. Plan de Migración Arquitectónica a DFU 1.1.1 (Largo Plazo)

Plan para absorber las mejoras oficiales de Daggerfall Unity 1.1.1 sin desestabilizar el netcode de las 21 fases:

1. **Fase A (Aislamiento de Netcode):** Verificar que los componentes de sincronización (`DFUBridgeComponent`, `DFUNetworkManager`, RPCs y Hooks) tengan límites limpios y desacoplados del código base de DFU.
2. **Fase B (Fork & Rebase en rama experimental):** Crear una rama dedicada (`refactor/dfu-1.1.1-port`) y rebasar sobre la etiqueta oficial `v1.1.1` de DFU.
3. **Fase C (Resolución de Conflictos Core):** Adaptar las inyecciones de red en `PlayerEntity`, `GameManager`, `QuestMachine`, `DaggerfallUI` y el nuevo sistema de localización/CSV de 1.1.1.
4. **Fase D (Testing de Paridad de Fases):** Ejecutar pruebas integrales de las 21 fases de TTRPG y multiplayer para certificar cero regresiones antes de mergear a `master`.
