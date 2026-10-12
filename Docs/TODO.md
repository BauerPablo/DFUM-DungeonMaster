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

## 4. Lista de Mods que quiero que sean compatibles

1. World of Daggerfall Project (1:08): Mejora la inmersión añadiendo nueva topografía, ruinas y campamentos en la naturaleza.
2. Beautiful Cities / Villages of Daggerfall (3:34): Transformación visual completa de ciudades y pueblos con mejores diseños y decoración.
3. Finding My Religion (5:01): Mejora el diseño interior de los templos según su deidad.
4. Dramatic Deaths (5:44): Añade animaciones de muerte más naturales para los enemigos.
5. Lives of Raiders (7:51): Añade eventos de asaltos y batallas a gran escala cerca de granjas.
6. Basic Roads (8:55): Conecta ciudades y pueblos mediante caminos transitables.
7. Windmills of Daggerfall (9:33): Añade molinos detallados en exteriores.
8. Bloodfall (10:10): Efectos visuales de sangre más detallados y posibilidad de decapitación.
9. Convenient Questlog (10:50): Interfaz mejorada para gestionar misiones con cronómetros.
10. Harvestable Crops (11:32): Permite recolectar ingredientes de los cultivos en granjas.
11. Daggerstats (12:16): Pantalla de estadísticas detalladas del personaje y progreso.
12. Paints and Dyes (12:58): Tintes para personalizar los colores de la armadura y ropa.
13. Village of Fishguard (13:21): Añade una ubicación idílica con su propia mazmorra secreta.
14. Shield Widget (14:47): Muestra el escudo equipado en la vista en primera persona.
15. Decorator (16:34): Herramienta para decorar espacios con diversos objetos.
16. (A CHEQUEAR) Useful Console Commands (17:33): Añade comandos de consola útiles (vuelo rápido, cambio de cara, tiendas).
17. Airships (18:43): Añade aeronaves pilotables y un sistema de transporte aéreo.
18. Drafty Secret Doors (21:02): Añade sonidos de viento para localizar puertas secretas.
19. Dynamic Skies (21:53): Cielos procedimentales con nubes, lunas y estrellas.
20. Fixed Desert Architecture (22:45): Cambia la arquitectura de edificios desérticos y ajusta los NPCs locales.
21. Roleplay and Realism Items (23:27): Reequilibra el botín, añade objetos y añade pociones a tiendas.
22. Seat of the Direnni (24:29): Mejora el diseño histórico de la icónica torre Direnni.
23. Language Skills Overhaul (26:08): Mejora la diplomacia, permitiendo reclutar enemigos pacificados.
24. (Analizar y refactorizar para el Army) Readied Spellcasting Hands (27:18): Muestra las manos preparadas al lanzar hechizos.
25. Sit and Loiter (28:02): Permite sentarse en muebles y descansar.
26. Orsinium Belongs to the Orcs! (28:37): Población de Orsinium basada principalmente en orcos.
27. Atlas of the Iliac Bay (29:27): Renombra ubicaciones de forma coherente según lore y geografía.
28. Unleveled Loot (31:06): Elimina el escalado por nivel del botín en tiendas y enemigos.
29. **(Refactorizar)** Piece of Chalk (32:18): Herramienta para marcar caminos o interruptores en mazmorras.
30. Paperdoll Overhaul (33:36): Correcciones para la vista del personaje (paperdoll).
31. **(Refactorizar)** Come Sail Away (33:57): Sistema completo de navegación y barcos a vela.
32. Bards and Balladeers (35:51): Añade la facción de bardos y mecánicas de actuación.
33. Free Rein (37:01): Mejora los controles y mecánicas al montar a caballo.
34. Daggerfall Expanded Textures (37:40): Prerrequisito con nuevos activos y texturas.
35. **(Refactorizar para que se coordinen las entidades)** Detailed Ships (38:10): Rediseño interior y exterior de barcos.
36. Eye of the Beholder (39:16): Añade una perspectiva en tercera persona.
37. Arrow Variety (40:13): Añade flechas con efectos especiales (veneno, explosivas).
38. **(Refactorizar para poder meterlo en el menu de DM)** Daggerfall Enemy Expansion (40:59): Añade casi 50 enemigos nuevos al juego.
39. Selfie Cam (41:47): Cámara para capturar imágenes del personaje.
40. Vanilla Enhanced (42:22): Mejora visual que preserva el estilo pixel art original.
41. Diverse Weapons (42:55): Nuevos modelos únicos para las armas.
42. Things Betwixt (43:35): Añade encuentros aleatorios durante los viajes rápidos.
43. Skulduggery (44:50): Overhaul para habilidades de ladrón y sigilo.
44. Guaranteed Ingredient Lootdrops (46:56): Los monstruos dropean ingredientes lógicos y constantes.
45. Falling Down (48:37): Previene morir si caes a través del mapa.
46. Random Little Quests (49:08): Añade pequeñas misiones emergentes.
47. Fixed Dungeon Exteriors (49:43): Ajusta los exteriores de las mazmorras a su nombre/tipo.
48. Killer Instincts (50:42): Mejora la IA enemiga con nuevas tácticas de combate.
49. Warm Ashes Wilderness Events (51:44): Añade eventos, encuentros y vida salvaje en el mundo exterior.
50. Climate & Calories
51. Real grass
52. Divers weapons
53. Better ambience
54. Dragon Rider, hay que ver como se ve el puppet desde otros jugadores.
55. Borrow skin, mod de druida! un espectaculo.
56. Astrid's Guide to Necromancy ver el numero de party.
57. Weathermancy
58. a chequear: Astrid's Guide to Vampirism