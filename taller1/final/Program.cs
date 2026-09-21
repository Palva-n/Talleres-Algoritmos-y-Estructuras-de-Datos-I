// Version INTEGRADA con los 5 retos aplicados en secuencia.
//
// Comandos disponibles via consola: ISDARK, READID, LEDON, LEDOFF, SALIR
//
// Nota sobre baudrate: la Pico se conecta como puerto serial USB (CDC-ACM),
// por lo que el valor de baudrate no afecta la velocidad real de transferencia.
// Cualquier valor funciona, EXCEPTO 2400 (puede forzar modo BOOTSEL).

using System;
using System.Collections.Generic;
using System.IO.Ports;


// RETO 1. Encapsulamiento: la conexion serial queda oculta
// detras de una clase con un solo metodo publico util.
public class ConexionSerial
{
    private SerialPort puerto;

    public ConexionSerial(string nombrePuerto)
    {
        puerto = new SerialPort(nombrePuerto, 115200)
        {
            ReadTimeout = 15000, // un poco mas que los 10s del READID
            NewLine = "\n"
        };
        puerto.Open();
    }

    public string EnviarComando(string comando)
    {
        puerto.WriteLine(comando);
        return puerto.ReadLine().Trim();
    }

    public void Cerrar()
    {
        if (puerto.IsOpen)
        {
            puerto.Close();
        }
    }
}


// RETO 2. Herencia: reutiliza EnviarComando() de ConexionSerial
// y agrega la logica de comparar contra IDs conocidos.

public class LectorControlAcceso : ConexionSerial
{
    private string[] idsPermitidos =
    {
        "C23B0307", // tarjeta blanca de prueba
        "24962107", // llavero azul de prueba
    };

    public LectorControlAcceso(string puerto) : base(puerto) { }

    public bool VerificarAcceso(out string uidLeido)
    {
        uidLeido = EnviarComando("READID");
        string uidCapturado = uidLeido; // copia local, sin "out"
        return Array.Exists(idsPermitidos, id => id == uidCapturado);
    }
}


// RETO 3. Polimorfismo: ISensor define un contrato comun;
// SensorLuz y SensorRFID lo implementan cada uno a su manera.
//
// Nota de diseno: ambos RECIBEN una ConexionSerial ya abierta
// (composicion) en vez de heredar de ella. Asi los dos sensores
// pueden compartir el mismo puerto COM sin pelear por abrirlo
// dos veces (el puerto serial solo admite un dueño a la vez).

public interface ISensor
{
    string Leer();
}

public class SensorLuz : ISensor
{
    private ConexionSerial conexion;
    public SensorLuz(ConexionSerial conexion) => this.conexion = conexion;
    public string Leer() => conexion.EnviarComando("ISDARK");
}

public class SensorRFID : ISensor
{
    private ConexionSerial conexion;
    public SensorRFID(ConexionSerial conexion) => this.conexion = conexion;
    public string Leer() => conexion.EnviarComando("READID");
}


// RETO 4 + 5. Composicion + limite de capacidad: el Controlador
// reune sensores (List<ISensor>) y aplica la regla de maximo 2.

public class Controlador
{
    private const int MAX_SENSORES = 2;
    private List<ISensor> sensores = new();

    public void Agregar(ISensor s)
    {
        if (sensores.Count < MAX_SENSORES)
        {
            sensores.Add(s);
            Console.WriteLine($"Sensor registrado ({sensores.Count}/{MAX_SENSORES}).");
        }
        else
        {
            Console.WriteLine("Capacidad máxima alcanzada.");
        }
    }

    public void LeerTodos()
    {
        foreach (var s in sensores)
        {
            Console.WriteLine($"  [{s.GetType().Name}] -> {s.Leer()}");
        }
    }
}

// PROGRAMA PRINCIPAL. Menu para probar cada reto por separado
// contra el hardware real, sin perder el modo consola original.

class Program
{
    static void Main()
    {
        // Ajustar el nombre del puerto segun el sistema:
        //   Windows -> revisar en Administrador de Dispositivos (ej. "COM3")
        //   Linux   -> normalmente "/dev/ttyACM0"
        //   macOS   -> algo como "/dev/tty.usbmodemXXXX"
        string nombrePuerto = "COM3";

        Console.WriteLine("Integracion de retos");
        Console.WriteLine("1) Modo consola simple (comandos crudos, como el template original)");
        Console.WriteLine("2) Probar Reto 2 (LectorControlAcceso)");
        Console.WriteLine("3) Probar Retos 3-4-5 (Controlador con sensores polimorficos)");
        Console.Write("Elegir opcion: ");
        string? opcion = Console.ReadLine();

        switch (opcion?.Trim())
        {
            case "1":
                ModoConsolaSimple(nombrePuerto);
                break;
            case "2":
                ProbarControlAcceso(nombrePuerto);
                break;
            case "3":
                ProbarControlador(nombrePuerto);
                break;
            default:
                Console.WriteLine("Opcion invalida.");
                break;
        }
    }

    // Reto 1 en accion: modo consola usando solo ConexionSerial 
    static void ModoConsolaSimple(string nombrePuerto)
    {
        var conexion = new ConexionSerial(nombrePuerto);
        Console.WriteLine($"Conectado a {nombrePuerto}.");
        Console.WriteLine("Comandos: ISDARK, READID, LEDON, LEDOFF, SALIR");

        while (true)
        {
            Console.Write("> ");
            string? comando = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(comando)) continue;

            comando = comando.Trim().ToUpper();
            if (comando == "SALIR") break;

            try
            {
                string respuesta = conexion.EnviarComando(comando);
                Console.WriteLine("Respuesta: " + respuesta);
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Sin respuesta de la Pico (timeout).");
            }
        }

        conexion.Cerrar();
        Console.WriteLine("Conexion cerrada.");
    }

    // Reto 2 en accion: verificar acceso contra IDs conocidos
    static void ProbarControlAcceso(string nombrePuerto)
    {
        var lector = new LectorControlAcceso(nombrePuerto);
        Console.WriteLine("Acerque una tarjeta o llavero (10s)...");

        bool acceso = lector.VerificarAcceso(out string uid);
        Console.WriteLine($"UID leido: {uid}");
        Console.WriteLine(acceso ? "Acceso PERMITIDO" : "Acceso DENEGADO");

        lector.Cerrar();
    }

    //  Retos 3, 4 y 5 en accion: Controlador con sensores ISensor 
    // Ambos sensores comparten UNA sola ConexionSerial (ver nota en Reto 3),
    // asi que ahora si podemos registrar LDR + RFID a la vez sin conflicto
    // de puerto, y de paso probamos el limite de capacidad (Reto 5).
    static void ProbarControlador(string nombrePuerto)
    {
        var conexion = new ConexionSerial(nombrePuerto);
        var controlador = new Controlador();

        controlador.Agregar(new SensorLuz(conexion));
        controlador.Agregar(new SensorRFID(conexion));

        // Este tercer intento debe ser rechazado (Reto 5: maximo 2 sensores).
        controlador.Agregar(new SensorLuz(conexion));

        Console.WriteLine("Leyendo sensores registrados:");
        controlador.LeerTodos();

        conexion.Cerrar();
    }
}
