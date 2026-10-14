# Motor universal de formularios de AntiCLI

**Estado:** motor de fichas JSON integrado en la versión de desarrollo 0.1.0. La compilación automática prueba que el código compila, no que cada comando sea compatible con todos los servidores.

## La regla principal

**No se crea una pantalla nueva por comando.** El componente `SessionView.Guided.cs` genera la misma interfaz para cualquier ficha de comando válida:

1. El administrador busca una intención en español, por ejemplo **crear 20 usuarios**.
2. El buscador consulta el título, el ejecutable y las palabras clave de cada ficha.
3. Al seleccionar una tarea, el motor muestra **flags seleccionables**, descripción original/española, campos de valores, ↑ ↓ para ordenar, parámetros requeridos y configuración de lote cuando la ficha lo admite.
4. El motor genera un **comando editable** y conserva 🟢🟡🔴 como clasificación orientativa de riesgo. El botón Ejecutar cambia de color. No hay una segunda confirmación.
5. El interruptor `sudo` es independiente del formulario y aplica a la ejecución Linux si la cuenta remota dispone de `sudo -n`.

La aplicación **no intenta adivinar si cualquier secuencia arbitraria de flags es válida**: esa información se define por comando. La plantilla determina dónde va el ejecutable, dónde van las opciones y dónde van los argumentos. El motor es único y extensible.

## De dónde salen los datos

| Fuente | Ubicación | Uso |
|---|---|---|
| Acciones base | `src/SolarisPKN.AntiCli/Data/admin-commands.json` | 146 acciones Linux y 7 Windows; todas muestran al menos un formulario genérico editable |
| Fichas ricas integradas | `src/SolarisPKN.AntiCli/Data/guided-commands.json` | 20 tareas con flags documentadas, parámetros tipados y plantilla específica, incluidos `uname`, `adduser`, APT y Yocto/BitBake |
| Fichas del usuario | `%APPDATA%\SolarisPKN\AntiCLI\modules\*.json` | Extender el catálogo sin recompilar, sin sobrescribir IDs de origen; el riesgo de estas fichas se marca rojo inicialmente |
| Flags observadas en un servidor SSH | `%APPDATA%\SolarisPKN\AntiCLI\command-help\` | Caché por host:puerto:ejecutable; importa opciones de `--help` y descripciones, no genera automáticamente plantillas ejecutables |

Las definiciones locales utilizan `SchemaVersion: 1`. La aplicación consulta las fichas del usuario al iniciar; después de añadir un archivo nuevo, reiniciá AntiCLI. El comando `--help` se solicita **solo al pulsar importar ayuda**, en el servidor autenticado, sin `sudo`, con timeout. Su texto se trata como fuente no confiable. La importación no certifica validez ni seguridad de una flag.

## Ejemplo de una ficha mínima

```json
{
  "SchemaVersion": 1,
  "Commands": [
    {
      "Id": "ejemplo-eco",
      "Executable": "printf",
      "TitleEs": "Imprimir un texto",
      "ExplanationEs": "Envía una cadena a la salida estándar.",
      "SearchTerms": "mostrar texto mensaje imprimir",
      "Risk": "Green",
      "Template": "printf {flags} '%s\\n' {message}",
      "Flags": [],
      "Operands": [
        {
          "Name": "message",
          "LabelEs": "Mensaje",
          "DescriptionEs": "Texto a imprimir.",
          "ValueKind": "Text",
          "Placeholder": "Hola servidor",
          "Required": true,
          "InitialValue": ""
        }
      ],
      "SupportsBatch": false,
      "TimeoutSeconds": 30,
      "Platforms": ["Linux"]
    }
  ]
}
```

La clave `Template` usa marcadores como `{flags}` y `{message}`. El generador valida y cita los operandos, pero **el contenido de la plantilla es código de shell**, por lo que solo deben instalarse fichas provenientes de personas de confianza. Un formulario visual no es un sistema de permisos ni una caja de arena.

## Ejemplo de lote con `adduser`

Buscá **crear 20 usuarios**, abrí la tarea **Crear usuario o lote**, activá **Sin contraseña inicial**, elegí un prefijo `alumno`, cantidad `20`, inicio `1` y ancho `2`. La vista previa genera una sola operación remota de shell que itera los sufijos `01` a `20`, usa `adduser` para cada uno y se detiene si alguno devuelve error.

No asigna claves SSH ni contraseñas, no hace rollback si falla a mitad de lote y requiere permisos remotos suficientes. Usá un servidor de pruebas antes de ejecutar lotes reales.

## Ejemplo de flags con `uname`

Buscá **información del sistema** y abrí `uname`. Marcá `-n`, `-r` o `-m`, reordenalas con ↑ ↓ y mirá la vista previa. **GNU `uname` imprime los campos en un orden predefinido**, aunque el orden de flags en el comando cambie. El formulario lo explica para evitar confusiones.

## Limitaciones actuales y siguientes mejoras

- Los 20 esquemas ricos tienen campos especialmente documentados; las otras acciones aparecen en formularios genéricos mientras se amplían las fichas.
- No existe un intérprete semántico universal capaz de deducir automáticamente todas las combinaciones, opciones mutuamente excluyentes, subcomandos o efectos secundarios de una herramienta. Esas reglas se deben expresar mediante metadatos verificables.
- `--help` sirve para sugerir flags, pero puede ser incompleto, localizarse en otro idioma o tener efectos secundarios.
- La creación en lote con plantillas está limitada a módulos que declaran expresamente `SupportsBatch` y `BatchOperand`.
- No hay reversión automática ni transacciones distribuidas: una orden roja puede cambiar o eliminar datos de verdad.
- El tipo de dato y la clasificación de riesgo son ayuda al usuario, no garantizan que una acción sea segura.

**Objetivo de evolución:** enriquecer las fichas a partir de manuales, páginas de ayuda y contribuciones revisadas, sin añadir vistas programadas por comando. Mantener separadas las definiciones y la interfaz hace que la base de datos pueda crecer sin disparar la complejidad del programa.
