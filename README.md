# K´eltik — Proyecto Integrador

**Equipo:** Mémeti´k Estudio
**Materia:** Sistemas Interactivos · Proyecto integrador
**Universidad Pablo Guardado Chávez (UPGCH)**

Recorrido virtual en 3D de un pueblo nocturno rodeado de bosque. El jugador explora el pueblo, resuelve acertijos y recoge notas para abrir el portón. Este repositorio contiene el **prototipo** (vertical slice) del primer semestre.

---

## Integrantes

| Integrante | Rol |
|---|---|
| Lucía Collazo López | Guionista y modeladora 3D |
| Bernardo Fonseca Suárez | Modelador 3D y programador |
| Diego Antonio Nangularí Manuel | Modelador 3D y animador |
| Helen Arce Álvarez | Ilustradora |
| Erick Alejandro Roque Archila | Ilustrador y modelador 3D |
| Hagit Leshareth Nazar Briones | Guionista e ilustradora |

La evidencia de programación e integración está en los commits de este repositorio. La evidencia de arte, modelado, animación y guion está en la carpeta de preproducción del equipo (una subcarpeta por integrante).

---

## Motor

- **Unity 6.3 LTS — versión exacta `6000.3.23f1`**
- Render pipeline: URP
- Escena principal: `Assets/Scenes/SampleScene.unity`

---

## Cómo ejecutar el build

1. Ir a **Releases** de este repositorio y descargar el `.zip` que aparece en el Release.
2. Descomprimir el `.zip` completo (no abrirlo desde dentro del zip).
3. Abrir **`Recorrido virtual.exe`**.

Plataforma: **Windows 10/11, 64 bits.** Todos los archivos de la carpeta son necesarios (`Recorrido virtual_Data`, `MonoBleedingEdge`, `UnityPlayer.dll`, etc.).

## Cómo abrir el proyecto en Unity

1. Clonar el repositorio (requiere **Git LFS**; GitHub Desktop lo incluye).
2. Instalar **Git para Windows** (git-scm.com): el proyecto usa un paquete que Unity descarga desde GitHub.
3. En Unity Hub: *Add → Add project from disk* → seleccionar la carpeta del repositorio.
4. Abrir con Unity **6000.3.23f1** y cargar `Assets/Scenes/SampleScene.unity`.

> No guardar el proyecto dentro de OneDrive ni en un disco casi lleno: Unity genera varios GB en la carpeta `Library`.

---

## Controles

| Tecla | Acción |
|---|---|
| **W** | Avanzar |
| **S** | Retroceder |
| **A / D** | Girar la cámara a la izquierda / derecha (estilo *Doom*) |
| **E** | Recoger notas |

---

## Estado actual

### ✅ Qué funciona
- Movimiento del jugador y control de cámara
- Exploración del pueblo nocturno, bosque y casas
- Acertijos
- Recolección de notas
- El hada
- Animación de las luces

### ⏳ Qué falta
- Animación de la habitación del inicio
- Animación final
- El portal
- Acomodo final de todos los elementos del escenario y los bancos
- Revisión general de la iluminación
- Sonidos y música

### ⚠️ Qué falla todavía (en este build)
- Las luces del arco no se encienden
- El portón no se abre al recoger todas las notas

---

## Créditos de assets externos

| Asset | Fuente | Licencia |
|---|---|---|
| Texturas `grass_07`, `wood_planks_26`, `wood_10`, `roots_bricks_wall_01` | [Poly Haven](https://polyhaven.com) | CC0 |

Los modelos 3D, ilustraciones y animaciones del proyecto fueron hechos por el equipo; las texturas externas están acreditadas arriba. Se usó IA como apoyo en programación, para acomodar piezas propias (kit modular de casas) dentro de prefabs y para configurar materiales de Unity.
