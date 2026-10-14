## Bucles controlados en el diagrama (FOR, WHILE, UNTIL)

El editor del diagrama ahora incluye el bloque **«+ Bucle ↺»**. Es un control reutilizable para **cualquier comando**, no solo `adduser`. Seleccioná el bloque y configurá:

- **FOR:** ejecuta el cuerpo hasta una cantidad fija de veces.
- **WHILE:** antes de cada repetición, ejecuta el comando de condición y sigue mientras devuelva código `0`.
- **UNTIL:** antes de cada repetición, ejecuta el comando de condición y sigue mientras NO devuelva código `0`.
- **Máximo de iteraciones:** valor obligatorio entre **1 y 1000**, también para WHILE y UNTIL. Evita repetir indefinidamente por una condición que nunca cambie.

**Cómo unir los bloques:** desde **Bucle ↺**, usá la flecha **«Cuerpo del bucle»** hacia el primer bloque de comando. Encadená otros comandos del cuerpo con **«Conectar →»**. Desde el último comando, usá **«Retorno ↺»** hacia el mismo bloque Bucle. Para continuar después de las repeticiones, conectá una flecha normal **«Conectar →» desde el bloque Bucle** hacia el siguiente paso.

Ejemplo de bucle `FOR` (tres repeticiones): `Bucle FOR 3 →(Cuerpo) echo 'hola' →(Retorno) Bucle` y `Bucle →(Siguiente) Fin`. Para WHILE y UNTIL, el campo de comando del bloque Bucle contiene la condición (p.ej. `test -f /tmp/seguir`); el código de salida se interpreta sin ejecutar el cuerpo si no se cumple la regla.

**Exportación real:** una cadena lineal que contenga bucles estructurados como los descritos puede convertirse en un script POSIX `.sh`, respetando las variables de la tabla del automatizador. Los límites se comprueban también en el script generado. La exportación no ejecuta el script ni abre SSH.

**Restricciones actuales:** los bucles deben tener un cuerpo de comandos conectado y retorno al mismo bloque, **sin anidamiento, ramificaciones IF/OR/XOR ni paralelismo interno**. Pueden existir varios bucles independientes y consecutivos dentro de una cadena. El guardado de JSON permite diseños con conexiones pendientes; solo se exportan los diagramas cuya ejecución sea inequívoca. Los ciclos arbitrarios entre bloques siguen prohibidos. No hay todavía simulación visual de pasos, depuración ni ejecución remota del script desde esta pantalla. Si un comando de cuerpo falla y está activada la política predeterminada de detener al primer error, el script termina.

## Editor de diagrama en ventana externa

La vista de diagrama **no ocupa espacio al abrir la pestaña Automatizaciones**. Para visualizarla, pulsá **«Abrir diagrama ↗»**, junto a la vista previa y exportación. Aparece una ventana independiente que se puede mover a otro monitor, maximizar con su botón o con `F11`, restaurar y cerrar con **«Volver a AntiCLI ↙»**.

AntiCLI **reutiliza el mismo editor y grafo de esa sesión**: cerrar y reabrir la ventana no reinicia los nodos ni las conexiones; los cambios quedan en el proyecto en memoria hasta guardarlos explícitamente como JSON. No se genera otra conexión SSH y la ventana externa no ejecuta comandos. Si se cierra la sesión/pestaña de origen, el diagrama desacoplado también se cierra. Abrir el botón dos veces enfoca la ventana existente, sin crear copias paralelas.

## Teclado de la consola manual

La entrada de comandos fija funciona como un CLI: **Enter ejecuta inmediatamente** el texto por SSH (según los permisos de la sesión); **Tab aplica el autocompletado seleccionado**. Las flechas ↑ ↓ recorren sugerencias; `Ctrl+Enter` o el atajo de ejecución configurado también pueden ejecutar, y el botón Ejecutar sigue funcionando. Tab sin sugerencia respeta la navegación normal de controles. Los colores de riesgo indican precaución, **no piden confirmaciones adicionales**.

# AntiCLI: automatizaciones por lotes y diagrama de flujo (prototipo)

## Modelo universal: no se programa un lote por comando

Los comandos del catálogo son fichas reutilizables y **cualquier comando** puede agregarse como paso de automatización. El motor de lotes tiene:
- **Pasos ordenables:** crear usuario, crear directorio, cambiar permisos, ejecutar herramientas propias, borrar archivos, etc.
- **Variables fijas:** `grupo=estudiantes` se reutiliza en todas las ejecuciones.
- **Variables por fila:** una tabla (columnas separadas por punto y coma o tabulador) puede establecer distintos usuarios, rutas, grupos y parámetros en cada fila.
- **Series numeradas:** `alumno{n}` o `/home/alumno{n}`, con cantidad, inicio y ancho del número configurables.
- **Orden:** ejecutar todos los pasos para cada fila o ejecutar un paso sobre todas las filas y después pasar al siguiente.
- **Política de errores:** detener al primer fallo (predeterminada) o continuar.
- **Exportación:** guardar proyecto JSON, vista previa de shell POSIX y archivo `.sh`. No se ejecuta ni agenda automáticamente desde esta pantalla.

Ejemplo de plantilla de paso: `mkdir -p -- {{carpeta}}` usando una tabla `usuario;carpeta`. Sustituye el valor de cada celda entrecomillado para el shell. **Los pasos son comandos definidos por un administrador y pueden modificar el servidor**: nunca deben ejecutarse sin revisar la vista previa y el alcance. Los valores por fila no son shell: son datos.

## Nueva pestaña «Diagrama visual»

En **4 · Automatizaciones** podés alternar entre:
1. **Diagrama visual:** agregar nodos **Comando**, **IF**, **AND**, **OR**, **XOR**, **Paralelo** y **Unión**, arrastrarlos en el lienzo y conectarlos seleccionando un tipo de flecha seguido del bloque de origen y destino. Las flechas distinguen orden normal, verdadero, falso y paralelo.
2. **Script / proyecto:** crear o editar la secuencia de pasos, tabla de variables, guardar JSON o exportar un script POSIX.

El editor del diagrama permite modificar nombre y comando de cada bloque, borrar nodos y sus conexiones, y guardar/abrir un archivo JSON del grafo con posiciones y flechas. Los nodos de una secuencia simple se crean automáticamente desde los pasos.

**Estado semántico:** la generación de scripts desde una cadena estrictamente lineal de comandos está implementada. Los nodos lógicos, ramificaciones y paralelismo **se pueden diseñar y guardar, pero NO se exportan aún a comandos ejecutables**. Si el grafo se modifica para incluir esas estructuras, la exportación `.sh` se bloquea con un mensaje. No se simula que una flecha IF o una flecha «Paralelo» ejecute el comportamiento requerido.

### ¿Qué faltaría para convertir el diagrama en una automatización real?

- Evaluación explícita del resultado de las condiciones con puertos verdaderos/falsos y control de errores.
- Definir AND/OR como condiciones booleanas y XOR como exclusión lógica con operandos claros; no son simplemente conectores.
- Paralelismo real: máximo de ejecuciones simultáneas, sincronización en un nodo de unión y política de fallos parciales.
- Gestión de variables entre nodos, salidas de procesos, temporizadores, reintentos, acciones de reversión cuando correspondan y control de secretos.
- Guardar diagrama y tabla de variables en un único proyecto versionado; ejecución/scheduler SSH opcionales y con autorización explícita.

**La compilación CI no valida arrastres, UX o lógica de ejecución real**. Probar visualmente la pantalla en Windows, cargar/guardar un grafo, mover nodos y verificar que no se exporten ramas incompletas.

## Prueba manual mínima

1. Añadí dos comandos sin valores peligrosos; generá el diagrama lineal, mové los nodos, conectalos con flecha «Después».
2. Generá un archivo `.sh` y verificá el orden de los dos pasos.
3. Agregá un nodo IF, enlazá una flecha «Verdadero» y probá exportar: debe **rechazar la exportación**, explicando que la semántica está pendiente.
4. Guardá el grafo JSON, cerralo, abrilo nuevamente y verificá posiciones y conexiones.
5. Armá 20 filas numeradas con dos columnas y revisá el script sin ejecutarlo.
