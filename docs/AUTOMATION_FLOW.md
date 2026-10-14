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
