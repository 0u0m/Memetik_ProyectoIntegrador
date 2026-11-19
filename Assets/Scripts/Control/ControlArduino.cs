using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

// Lee el control fisico (Arduino Uno) por el puerto serial.
// El Arduino manda lineas "x,y,a,b,c,d" (ver ControlPueblo.ino).
// Si no hay Arduino conectado, el juego sigue funcionando con teclado.
public class ControlArduino : MonoBehaviour
{
    public static ControlArduino Instancia { get; private set; }

    [Header("Conexion")]
    [Tooltip("Ej. COM3. Vacio = usar el primer puerto que encuentre")]
    public string puerto = "";
    public int baudios = 115200;

    [Header("Joystick")]
    [Range(0f, 0.5f)] public float zonaMuerta = 0.15f;
    public bool invertirX = false;
    public bool invertirY = false;

    // Estado que lee el resto del juego
    public bool Conectado { get; private set; }
    public float EjeX { get; private set; }  // -1 izquierda .. 1 derecha
    public float EjeY { get; private set; }  // -1 atras .. 1 adelante
    readonly bool[] boton = new bool[4];
    readonly bool[] botonAnterior = new bool[4];
    readonly bool[] botonBajado = new bool[4];

    SerialPort serial;
    Thread hilo;
    volatile bool leyendo;
    // datos crudos que escribe el hilo
    volatile int rawX = 512, rawY = 512, rawBotones = 0;
    volatile float ultimoDato = -1f;
    float ahora;

    void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start() { Conectar(); }

    void Conectar()
    {
        try
        {
            string nombre = puerto;
            if (string.IsNullOrEmpty(nombre))
            {
                var puertos = SerialPort.GetPortNames();
                if (puertos.Length == 0) { Debug.Log("[ControlArduino] No hay puertos serial. Usando teclado."); return; }
                nombre = puertos[puertos.Length - 1]; // normalmente el Arduino es el ultimo
            }
            serial = new SerialPort(nombre, baudios) { ReadTimeout = 100, NewLine = "\n", DtrEnable = true };
            serial.Open();
            leyendo = true;
            hilo = new Thread(Leer) { IsBackground = true };
            hilo.Start();
            Debug.Log("[ControlArduino] Conectado en " + nombre);
        }
        catch (Exception e)
        {
            Debug.Log("[ControlArduino] No se pudo abrir el puerto (" + e.Message + "). Usando teclado.");
            serial = null;
        }
    }

    void Leer()
    {
        while (leyendo)
        {
            try
            {
                string linea = serial.ReadLine();
                var p = linea.Trim().Split(',');
                if (p.Length < 6) continue;
                int x = int.Parse(p[0]), y = int.Parse(p[1]);
                int b = 0;
                for (int i = 0; i < 4; i++) if (p[2 + i] == "1") b |= 1 << i;
                rawX = x; rawY = y; rawBotones = b;
                ultimoDato = ahora;
            }
            catch (TimeoutException) { }
            catch (FormatException) { }
            catch (Exception) { if (!leyendo) break; Thread.Sleep(50); }
        }
    }

    float Normalizar(int v, bool invertir)
    {
        float f = Mathf.Clamp((v - 512f) / 512f, -1f, 1f);
        if (Mathf.Abs(f) < zonaMuerta) f = 0f;
        else f = Mathf.Sign(f) * (Mathf.Abs(f) - zonaMuerta) / (1f - zonaMuerta);
        return invertir ? -f : f;
    }

    void Update()
    {
        ahora = Time.unscaledTime;
        Conectado = serial != null && ultimoDato >= 0f && ahora - ultimoDato < 1f;
        EjeX = Conectado ? Normalizar(rawX, invertirX) : 0f;
        EjeY = Conectado ? Normalizar(rawY, invertirY) : 0f;
        int b = Conectado ? rawBotones : 0;
        for (int i = 0; i < 4; i++)
        {
            botonAnterior[i] = boton[i];
            boton[i] = (b & (1 << i)) != 0;
            botonBajado[i] = boton[i] && !botonAnterior[i];
        }
    }

    /// <summary>Boton presionado (0=A, 1=B, 2=C, 3=D).</summary>
    public bool Boton(int i) => boton[i];
    /// <summary>Boton recien presionado este frame.</summary>
    public bool BotonBajado(int i) => botonBajado[i];

    void OnDestroy() { Cerrar(); }
    void OnApplicationQuit() { Cerrar(); }

    void Cerrar()
    {
        leyendo = false;
        try { if (serial != null && serial.IsOpen) serial.Close(); } catch { }
        serial = null;
    }
}
