
// Comandos disponibles: ISDARK, READID, LEDON, LEDOFF, SALIR
//
// Nota sobre baudrate: la Pico se conecta como puerto serial USB (CDC-ACM),
// por lo que el valor de baudrate no afecta la velocidad real de transferencia
// (la maneja el propio USB). Cualquier valor funciona, EXCEPTO 2400, que puede
// forzar a la Pico a entrar en modo BOOTSEL. Se deja 115200 por convencion.

using System;
using System.IO.Ports;

class Program
{
    static void Main()
    {
        // Ajustar el nombre del puerto segun el sistema:
        //   Windows -> revisar en Administrador de Dispositivos (ej. "COM3")
        //   Linux   -> normalmente "/dev/ttyACM0"
        //   macOS   -> algo como "/dev/tty.usbmodemXXXX"
        
        string nombrePuerto = "COM3";

        using SerialPort puerto = new SerialPort(nombrePuerto, 115200)
        {
            ReadTimeout = 15000,  // un poco mas que los 10s del READID
            NewLine = "\n"
        };

        puerto.Open();
        Console.WriteLine($"Conectado a {nombrePuerto}.");
        Console.WriteLine("Comandos: ISDARK, READID, LEDON, LEDOFF, SALIR");

        while (true)
        {
            Console.Write("> ");
            string? comando = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(comando))
            {
                continue;
            }

            comando = comando.Trim().ToUpper();

            if (comando == "SALIR")
            {
                break;
            }

            try
            {
                puerto.WriteLine(comando);
                string respuesta = puerto.ReadLine();
                Console.WriteLine("Respuesta: " + respuesta.Trim());
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Sin respuesta de la Pico (timeout).");
            }
        }

        puerto.Close();
        Console.WriteLine("Conexion cerrada.");
    }
}