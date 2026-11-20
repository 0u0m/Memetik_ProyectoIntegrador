# K´eltik: rama `Correcciones-sugeridas`

> Este README describe **solo los cambios de esta rama**. El README del equipo (integrantes, controles, build, créditos) está en la rama base: [README original en `main`](https://github.com/0u0m/Memetik_ProyectoIntegrador/blob/main/README.md).

**Commit base:** `1c43cc7` (`main`, "Corrijo instrucciones de descarga del build en el README")
**Unity:** 6000.3.23f1 · URP 17.3 (PC, Forward+)

| Commit | Bloque |
|---|---|
| `3fdafbf` | Código: portón en el build, pausa/salir, nota perdida y scripts huérfanos |
| `1da5a32` | Escala y colisiones: límite del mapa, troncos, faroles y gravedad |
| `b631946` | Iluminación y sombras: distancia de sombras a la escala del mundo |
| `994c8d8` | Materiales: tiling de las capas de terreno a la escala del mundo |
| `889a243` | Post procesado y calidad: MSAA 4x y VSync en PC |
| `a9576f0` | Iluminación: GI dinámico (Enlighten realtime) y shaders toon que lo aceptan |
| `a010143` | Repositorio: se elimina basura evidente (≈38 MB) |
| `ece394b` | README de la rama y capturas de comparación |
| `3b72f40` | Post procesado: tonemapping AgX como pase de pantalla completa |
| `5947fc2` | Iluminación: GI más intenso y realista (direccional + SSAO en la luz indirecta) |
| `83da099` | README y capturas: AgX y GI más intenso |
| `3ea1a45` | Post procesado y gráficos: SSAO a escala, sombras de faroles, DOF, motion blur, grano, flares |
| `bcfc61e` | Shaders: modo realista (PBR) con `_Realismo`, normal maps y rugosidad |
| `877be13` | README y capturas: modo realista, post procesado y costo |
| `7319032` | Neblina: quita los anillos duros alrededor del jugador |

Cada commit es un bloque temático. Se puede revisar o revertir por separado (`git revert <commit>`).

---

## Comparación: base vs. corregido

Las dos columnas se renderizaron con la misma cámara y el mismo encuadre: a la izquierda `main`, a la derecha esta rama. La escala del mundo no cambió (1 m = 3.4 u), así que las posiciones son idénticas.

> **No hay menú en el proyecto** (el juego arranca directo en la escena). En su lugar se muestra la vista de un tablero de acertijo, que es la UI principal del juego.
> Las capturas se renderizaron en batch mode desde la cámara del jugador, con Enlighten ya convergido. Luminancia media de toda la imagen entre paréntesis (base → corregido).

### Noche (estado inicial)

| Vista | Qué se ve |
|---|---|
| ![](Docs/Comparacion/01_camara_jugador.jpg) | **Cámara del jugador** en el spawn (0.180 → 0.184). Las fachadas toman el rebote cálido de los faroles y la madera muestra relieve (normal map); AgX conserva el cielo nocturno sin quemar las ventanas |
| ![](Docs/Comparacion/02_vista_general.jpg) | **Vista general** del pueblo, el bosque y el lago. El límite invisible sigue la orilla de la meseta (no se ve; se comprobó con física) |
| ![](Docs/Comparacion/03_plaza_acertijos.jpg) | **Plaza con la fuente y una jaula** (0.190 → 0.200). La piedra de la fuente tiene sombreado real, los faroles proyectan sombra en el piso y el poste de la jaula ya no es un color plano |
| ![](Docs/Comparacion/04_recinto_porton.jpg) | **Recinto del portón** (0.168 → 0.204). Muros con el rebote de los faroles de la entrada (sin quemarse), rejas de hierro con brillo metálico; el pasto bajo la sombra del tronco queda oscuro, como corresponde a la sombra de la luna |
| ![](Docs/Comparacion/05_calle_casas.jpg) | **Calle entre casas** (0.168 → 0.173). Tablones con relieve y rugosidad variable; SSAO en la unión de muros y suelo |
| ![](Docs/Comparacion/06_detalle_materiales.jpg) | **Detalle de materiales** (0.150 → 0.148). Puerta con normal map y reflejos suaves; pasto con translucidez |
| ![](Docs/Comparacion/07_bosque_borde.jpg) | **Bosque cerca del borde** (0.167 → 0.171). Copas con variación de color por árbol, troncos con volumen y collider; profundidad de campo suave al fondo |
| ![](Docs/Comparacion/08_tablero_acertijo.jpg) | **UI del acertijo** (vista forzada del tablero 1) (0.210 → 0.229). Sin cambios de diseño |

### Hora dorada (las 5 notas recogidas: `Amanecer` al 100%)

Aquí la GI dinámica se nota más, porque el sol rasante tiene más energía para rebotar. Es el caso para el que se eligió GI dinámica en vez de un bake.

| Vista | Qué se ve |
|---|---|
| ![](Docs/Comparacion/09_dorada_recinto_porton.jpg) | **Recinto** (0.310 → 0.366): rebote cálido en los muros, hierro metálico en las rejas |
| ![](Docs/Comparacion/09_dorada_detalle_materiales.jpg) | **Detalle** (0.258 → 0.282): la puerta y su marco muestran la veta de la madera; el pasto deja pasar la luz del sol rasante |
| ![](Docs/Comparacion/09_dorada_bosque_borde.jpg) | **Bosque** (0.316 → 0.363): troncos con volumen y copas con variación de color en lugar de un verde plano |
| ![](Docs/Comparacion/09_dorada_vista_general.jpg) | **Vista general** (0.363 → 0.396) |

Cada imagen tiene a la izquierda `main` y a la derecha esta rama.

---

## 1. Código

| Cambio | Por qué |
|---|---|
| `PortonFinal`: el shader de los círculos pasa a un campo serializado (`shaderCirculo`), asignado en la escena, con validación | **Causa de "las luces del arco no se encienden" y "el portón no se abre".** `Shader.Find("Porton/Circulo Nota")` devolvía `null` en el build porque ningún material usa ese shader y Unity lo quita. `Awake` lanzaba `ArgumentNullException` (confirmado en `Player.log` de un build de `main`), los círculos se quedaban sin material y las rotaciones de la reja quedaban como `Quaternion` (0,0,0,0), así que no abría. En el editor sí funcionaba |
| `PortonFinal`: las rotaciones de la reja, el portal y su luz se inicializan antes de crear materiales | Si algo falla con los círculos, la reja igual puede abrir |
| Nuevo `PausaJuego` (objeto `Pausa`): **P** o **botón D** pausa; en pausa, **X** o **botón C** sale del juego | `EntradaJugador.Pausa` estaba mapeado pero ningún script lo usaba, y no había `Application.Quit()`. El build corre en pantalla completa, así que solo se podía cerrar con Alt+F4. Usa la misma UI (`UIJuego.Panel`) y respeta `UIJuego.ocupadoPor` |
| `ProgresoNotas`: se quita el antirrebote de 0.5 s | Si ignoraba una llamada, la nota ya estaba marcada `Recogida` y oculta: se perdía y el panel nunca se habilitaba |
| Se borra `Scripts/CleanFPSController.cs` | Huérfano (no está en la escena) y duplica al jugador. Usa `Input.GetAxis` con *Active Input Handling = Input System*, así que lanzaría `InvalidOperationException` |
| Se borra `Editor/TmpClaudeRecinto.cs` | Script temporal de IA ("TmpClaude"); su menú borra objetos de la escena (`DestroyImmediate`) |

## 2. Escala y colisiones

La escala del proyecto (1 m = 3.4 u, `JugadorDoom.cs`) es coherente: puertas de 2.0 × 2.9 m, muros de 3 m, cápsula de 1.71 m, ojos a 1.59 m y escalón de 35 cm. No se reescaló nada.

| Cambio | Por qué |
|---|---|
| Nuevo `Limites_Mapa`: 96 muros invisibles (`BoxCollider`) siguiendo la orilla de la meseta | En la prueba de física (el `CharacterController` caminando desde el spawn en 16 direcciones), en **12 de 16** el jugador bajaba de la meseta, caminaba **por debajo del plano del lago** (que no tiene collider) y salía del terreno. Ahora las 16 terminan contra una casa o contra el límite |
| `CapsuleCollider` en los 512 troncos (`Pino_Tronco_Toon`) y en los 24 faroles | Se atravesaban. Solo se pusieron en el tronco y el poste, no en las copas |
| Gravedad -60 → **-33.4** | -60 equivalía a 1.8 g. -33.4 = 9.81 m/s² × 3.4 u/m, la escala del propio proyecto |
| Se quita el `MeshCollider` sin malla de la raíz `Porton` | No hacía nada |

## 3. Iluminación

Las luces se dejan **Realtime**: `Amanecer.cs` cambia el sol, el cielo y el ambiente con cada nota, y un bake estático no seguiría esos cambios. En su lugar se activó **GI dinámico**.

| Cambio | Por qué |
|---|---|
| `shadowDistance` 50 → 150 u (`PC_RPAsset`) | 50 u eran 14.7 m: la sombra de la luna aparecía y desaparecía junto al jugador. 150 u son 44 m |
| **Realtime GI (Enlighten)** con `GI_Dinamico.lighting` (Baked GI apagado, 0.4 texels/u, entorno en tiempo real) | El rebote de luz se recalcula en runtime cuando `Amanecer` mueve el sol (ya llamaba a `DynamicGI.UpdateEnvironment()`) |
| Shaders toon (`Casas/Toon Texturizado`, `Casas/Toon Degradado`, `Bosque/Toon`, `Bosque/Toon Pasto`) **aceptan GI**: suman `albedo * ToonGI() * _GIIntensidad` | Antes **no leían ni luz ambiente ni GI**: la cara en sombra era solo `_ShadowTint * luz`, por eso todo se veía plano. `ToonGI()` (en `Casas/ToonLuces.hlsl`) usa el lightmap realtime en estáticos y las light probes en dinámicos. `_GIIntensidad` (0 a 8, por defecto 3) se ajusta por material |
| Lightmaps realtime **direccionales** (`DIRLIGHTMAP_COMBINED`) | El rebote varía según la normal de cada cara en vez de ser un tono plano por objeto |
| La luz indirecta recibe el **SSAO** que ya tenía `PC_Renderer` | Los shaders toon lo ignoraban. Ahora la GI se oscurece en esquinas, aleros y contacto con el suelo |
| *Indirect Intensity* 2 y *Albedo Boost* 1.5 en `GI_Dinamico.lighting` | GI exagerada a propósito: el color de las casas tiñe más el rebote |
| Pase `Meta` en los dos shaders de casas | Enlighten necesita el color de la superficie para calcular el rebote (las casas rojas tiñen el piso, etc.) |
| *Contribute GI* en 1,618 objetos estáticos y en el terreno, con UV2 generadas en 8 FBX y 20 mallas de `TechosNuevos` | Solo los objetos estáticos con UV de lightmap entran a Enlighten. Se excluyen las partes que se mueven: rejas, jaulas, notas, hada y caras de los tableros |
| `Sondas_Luz`: 1,405 light probes dentro del límite del mapa | Árboles, pasto, hada y notas reciben GI por sondas |
| *Indirect multiplier*: faroles 8, luna 2 | El término directo toon está exagerado respecto al rebote físico; con 1 el rebote casi no se notaba. Se probó la luna en 4 y `_GIIntensidad` en 4: de noche los muros claros quedaban casi blancos y las hojas verde neón |

## 4. Materiales y modelos

| Cambio | Por qué |
|---|---|
| `tileSize` 2 → 6.8 u en las capas `NewLayer` (grass_07) y `NewLayer 1` (ground_tiles_24) | 2 u eran 0.6 m reales: la muestra de Poly Haven (unos 2 m) se repetía 3.4 veces de más y se veía como ruido. 6.8 u son 2 m |
| UV2 (*Generate Lightmap UVs*) en los FBX de casas, farol, fuente, poste, portón y tablero | Requisito del GI dinámico (ver bloque 3) |

## 5. Post procesado y calidad

| Cambio | Por qué |
|---|---|
| MSAA 1 → 4x (`PC_RPAsset`) | No había antialiasing (la cámara tiene AA *None*): pinos, marcos y rejas se veían dentados |
| VSync encendido en el nivel *PC* | Sin VSync los FPS no tenían tope: tearing y GPU al 100% en un recorrido que no lo necesita |

| **Tonemapping AgX** como *Full Screen Pass Renderer Feature* (`Shaders/AgxTonemapping.shader` + `Settings/AgX_Tonemapping.mat`) en `PC_Renderer` y `Mobile_Renderer` | URP 17.3 solo trae Neutral y ACES. AgX lleva los brillos (ventanas, faroles, rebote) suavemente al blanco sin quemar ni torcer el tono, y deja más rango en las sombras. Look: contraste 1.15, saturación 1.05; la exposición se ajusta en el material |
| `Noche_Volume`: Tonemapping Neutral → *None*; Color Grading en **HDR** (`PC_RPAsset` y `Mobile_RPAsset`) | URP hace bloom y gradeo en lineal y AgX hace la transformación a pantalla (con Neutral activo se aplicarían dos tonemappers) |
| Neblina de volumen continua: la mezcla de bandas toon pasa a `_MezclaBandas` (por defecto 0) en `Neblina_Bosque.mat` | Las bandas dependían de la distancia a la cámara y se veían como **anillos con borde duro alrededor del jugador** ("niebla cortada"). Medido: la distancia al suelo plano era continua, pero la neblina saltaba de 13.5 a 6.1 niveles en 25 px; ahora baja de forma continua |
| AgX: exposición **+0.35 EV** | Sin el tinte de sombra toon (modo realista) la escena perdía 10-25% de luminancia |
| **SSAO** a escala: radio 0.3 → 1.5 u, intensidad 0.4 → 1.2, muestras y blur en *High* | 0.3 u eran 9 cm con 1 m = 3.4 u: no se veía. 1.5 u = 45 cm oscurece esquinas, aleros y contacto con el suelo |
| **Depth of Field** gaussiano (140–700 u) | El fondo lejano se suaviza como en una cámara real, sin tocar lo que el jugador tiene cerca |
| **Motion Blur** de cámara (0.25) | Suaviza el giro rápido tipo Doom (110°/s) |
| **Film Grain** fino (0.18) y **aberración cromática** (0.06) | Rompen la limpieza "digital" de la imagen nocturna |
| **Screen Space Lens Flare** (0.35) y Bloom 1.0 / 1.0 / 0.7 | Destellos de lente en faroles y ventanas |
| Sombras: principal 4096, atlas de adicionales 4096, sombras suaves *High* | Más definición con la nueva distancia de sombras |
| Los **24 faroles proyectan sombra** (suave, tier Low); la pieza de la lámpara que contiene la luz no proyecta | Postes, jaulas y personajes ya cortan la luz de los faroles; sin excluir la lámpara, su carcasa tapaba la propia luz |

## 6. Shaders: modo realista (PBR)

Los 4 shaders toon (`Casas/Toon Texturizado`, `Casas/Toon Degradado`, `Bosque/Toon`, `Bosque/Toon Pasto`) mezclan la versión toon del equipo con iluminación física de URP según **`_Realismo`** (0 = toon original, 1 = realista; por defecto 1). Para volver al estilo original basta con poner `_Realismo` en 0 en los materiales.

| Cambio | Por qué |
|---|---|
| `IluminacionReal()` en `Casas/ToonLuces.hlsl`: la BRDF de URP/Lit (difusa + especular GGX) para la luz principal y las adicionales, con sus sombras; `GlobalIllumination` con reflejos del entorno y Fresnel | El toon cortaba la luz en 2 bandas con un tinte de sombra fijo: todo se veía plano y "de plástico" |
| Casas: **normal map**, **mapa de rugosidad**, lisura y metálico | Se asignaron los `normal_gl` y `roughness` de Poly Haven **que ya estaban en el repo sin usar** (tablones, madera, tejas). Lisura y metálico por tipo: cristal 0.92, hierro y metal 0.85 metálico, piedra 0.2, papel 0.12, madera 0.3 |
| Bosque: variación de color por árbol y por zona + **luz a través de las hojas** | Las copas eran un solo verde plano |
| Pasto: translucidez, normal inclinada hacia la cámara y un piso de luz del cielo | Con la normal vertical, el Fresnel reflejaba el cielo en las puntas; las sondas junto a muros dejaban el pasto negro sobre un suelo más claro |
| `_GIReal` (2; pasto 2.5): luz indirecta del modo realista | Independiente de `_GIIntensidad` (3), que sigue siendo la del modo toon |
| Tangentes en las 25 mallas de `TechosNuevos` y normal maps en las capas de terreno con textura | Requisito del normal map |

## 7. Repositorio

| Cambio | Por qué |
|---|---|
| Se borran `Assets/_Recovery/0.unity` (31.8 MB), 3 `TerrainData_*.asset` huérfanos, la imagen de WhatsApp y `New Actions.inputactions` (vacío) | Ninguno es dependencia de la escena (verificado con `AssetDatabase.GetDependencies`) |
| `LightingData.asset` a Git LFS | Pesa 89 MB; en git normal se acerca al límite de 100 MB de GitHub |
| `.DS_Store` en `.gitignore` | Archivos de macOS |

---

## Cómo probar

1. `git fetch && git checkout Correcciones-sugeridas` y `git lfs pull` (para `LightingData.asset`).
2. Abrir con Unity **6000.3.23f1** y cargar `Assets/Scenes/SampleScene.unity`. Si la GI se ve apagada en el editor: *Window → Rendering → Lighting → Generate Lighting* (el precompute de Enlighten tarda unos 6 min).
3. En Play:
   - Caminar hacia la orilla del bosque: un muro invisible detiene al jugador antes del agua. Ya no se atraviesan troncos ni faroles.
   - **P** pausa; **X** sale del juego (en el editor detiene el Play).
   - Resolver un tablero, recoger las 5 notas y escribir **KELTIK** en el panel: los círculos se encienden con cada nota y la reja abre.
4. **Build:** generar el build y repetir el paso del portón. Es el caso que fallaba: el error solo aparecía en el build.
5. Para ajustar el realismo: `_Realismo`, `_Smoothness`, `_Metallic`, `_BumpScale` y `_GIReal` en cada material. Los efectos de post están en `Ambiente/Noche_Volume`.
6. Para ajustar la GI: `_GIIntensidad` en cada material toon, *Indirect Multiplier* en cada luz, *Indirect Intensity* y *Albedo Boost* en `GI_Dinamico.lighting` (estos dos requieren *Generate Lighting*). Para ajustar AgX: exposición, contraste y saturación en `Settings/AgX_Tonemapping.mat`.

### Qué se verificó y qué no

| Verificación | Resultado |
|---|---|
| Compilación (batch mode) | ✅ sin errores |
| Build de macOS de `main` | ✅ reproduce `ArgumentNullException` en `PortonFinal.Awake` |
| Build de macOS de esta rama (modo realista, AgX, GI direccional, SSAO, post) | ✅ compila (475 MB; antes 377 MB: +59 MB de `LightingData`, +39 MB de normal maps y rugosidad) y en `Player.log` no hay excepciones ni errores de shader |
| Costo aproximado (render + lectura de GPU, 1600×900, Mac de la revisión, en el editor) | ⚠️ 15.6 ms de noche y 20.8 ms en hora dorada. Es una referencia: no es el FPS del build en Windows |
| Física: 16 caminatas desde el spawn con el `CharacterController` | ✅ 16/16 se detienen en casas o en `Limites_Mapa` (antes 12/16 salían del mapa) |
| Puertas de las casas | ✅ siguen cerradas (son fachadas; el collider de la pared tapa el hueco, como en `main`) |
| Enlighten | ✅ precompute de 135 sistemas; 1,618/1,618 estáticos con lightmap realtime; converge en unos 100 frames |
| GI visible | ✅ con lightmaps direccionales y más intensidad se ve en fachadas, troncos y puertas; el recinto sube +25% de luminancia de noche y +19% en hora dorada |
| **Play Mode interactivo** (tableros, notas, pausa, apertura de la reja) | ❌ no verificado; se necesita alguien jugando |
| **FPS reales** en el build (Windows y la PC de la escuela) | ❌ no medidos: el modo realista, las sombras de 24 faroles, SSAO High y DOF cuestan más que el toon original |
| **Build de Windows** | ❌ no generado (se probó con macOS) |

---

## Pendientes sugeridos (no aplicados)

Necesitan una decisión del equipo:

0. **Toon o realista.** El modo realista se pidió durante la revisión y cambia la dirección de arte del equipo (que era toon). Todo es reversible por material con `_Realismo`: 0 regresa al toon original con GI y AgX. Los shaders de agua (`Agua/Toon Lago`, `Agua/Toon Cascada`), el cielo y la neblina siguen siendo toon.

1. **Versiones viejas del pueblo dentro de la escena.** `Pueblo`, `Pueblo_B`, `Laberinto` y `Pueblo_Plaza_v1` están desactivados, pero suman unos 9,800 objetos y 2,265 colliders y van incluidos en el build. Conviene moverlos a otra escena o borrarlos (por eso `SampleScene.unity` pesa 31 MB).
2. **~310 MB de assets sin usar:** sets PBR completos de Poly Haven (con `normal_dx` **y** `normal_gl`; solo se usa uno), `Ventanas_puertas.fbx` (24 MB), `Puerta 2.fbx`, `Ventana2.fbx`, `Material_pared/` y `Respaldo_Materiales/`.
3. **Pantalla de notas:** `EntradaJugador.Notas` (Q, Tab o botón B) está mapeado pero no hace nada.
4. **Menú principal:** el juego arranca directo en la escena.
5. **Nombre de la escena:** sigue siendo `SampleScene` (nombre de plantilla). Al renombrarla hay que actualizar *Build Profiles* y el README.
6. **`mostrarSolucionEnConsola`** está activo en los 3 tableros: la solución queda escrita en `Player.log` del build.
7. **`AcertijoTablero`** usa `Shader.Find("Universal Render Pipeline/Unlit")`. Funciona en el build solo porque `Portal.mat` usa ese shader. Conviene un campo serializado, como se hizo en `PortonFinal`.
8. **`Casa_Mansion`** tiene escala no uniforme (6.09, 7.97, 6.09): la puerta mide 4.9 m.
9. **Capa de terreno sin textura:** `New Terrain Layer` cubre el 42% del terreno.
10. **Paquetes:** `unity-mcp` se instala desde `#main` (sin versión fija) y hay paquetes *pre/exp* (`ai.assistant`, `pipeline`). Conviene fijar un commit o una versión.
11. **FBX estáticos con `Import Animation` activo.** Restos de plantilla: `TutorialInfo/`, `SampleSceneProfile.asset`.
12. **GI:** para más detalle en el rebote se puede subir la resolución indirecta, pero `LightingData.asset` crece (hoy pesa 89 MB a 0.4 texels/u). Los muros del recinto quedan muy claros de noche por los faroles de la entrada; se puede bajar su *Indirect Multiplier*.
13. **`ControlArduino`** toma el *último* puerto serie. En macOS fue un puerto que no era el Arduino (`Inappropriate ioctl for device`). Conviene filtrar por nombre (`usbmodem` o `COM`).
