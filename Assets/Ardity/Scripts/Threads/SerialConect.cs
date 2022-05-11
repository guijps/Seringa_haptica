
using UnityEngine;
using System.Threading;
using System.IO.Ports;
using System.Globalization;
using System;
using System.Diagnostics;
//using GetUSBDevices;

public class SerialConect : MonoBehaviour
{
    [Tooltip("Port name with which the SerialPort object will be created.")]
    [SerializeField]
    string portName = "COM6";
    string[] portNames = SerialPort.GetPortNames();


    float startTime = 0;
    float timeOutArduino = 2;
    string receivedValue = "";
    int portNumTest = 0;
    public bool funcionando = false;
    [SerializeField]
    GameObject embolo;
    public float totalDistanceEmbolo = 0.5f;
    public float startPositionEmbolo = 0f;
    public float anim_step = 0.1f;
    public float distanceEmbolo = 0;
    public TextMesh messageCalibrateArduino;
    bool b_arduinoCalibration = false;
    bool usingArduino = false;
    [SerializeField]
    float movimento = 0.0001f;
    Vector3 positionInput;

    [Tooltip("Baud rate that the serial device is using to transmit data.")]
    public int baudRate = 9600;

    [Tooltip("Reference to an scene object that will receive the events of connection, " +
             "disconnection and the messages from the serial device.")]
    public GameObject messageListener;

    [Tooltip("After an error in the serial communication, or an unsuccessful " +
             "connect, how many milliseconds we should wait.")]
    public int reconnectionDelay = 1000;

    [Tooltip("Maximum number of unread data messages in the queue. " +
             "New messages will be discarded.")]
    public int maxUnreadMessages = 100;

    // Constants used to mark the start and end of a connection. There is no
    // way you can generate clashing messages from your serial device, as I
    // compare the references of these strings, no their contents. So if you
    // send these same strings from the serial device, upon reconstruction they
    // will have different reference ids.
    public const string SERIAL_DEVICE_CONNECTED = "__Connected__";
    public const string SERIAL_DEVICE_DISCONNECTED = "__Disconnected__";

    // Internal reference to the Thread and the object that runs in it.
    protected Thread thread;
    protected SerialThreadLines serialThread;
    CultureInfo ci;
 
    bool turnedOnVibra = false;
    bool turnedOffVibra = false;
    bool onTrigEntered = false;
    class USBDeviceInfo
    {
        public USBDeviceInfo(string deviceID, string pnpDeviceID, string description)
        {
            this.DeviceID = deviceID;
            this.PnpDeviceID = pnpDeviceID;
            this.Description = description;
        }
        public string DeviceID { get; private set; }
        public string PnpDeviceID { get; private set; }
        public string Description { get; private set; }
    }
    // ------------------------------------------------------------------------
    // Invoked whenever the SerialController gameobject is activated.
    // It creates a new thread that tries to connect to the serial device
    // and start reading from it.
    // ------------------------------------------------------------------------
    private void Awake()
    {
        portName = "COM6";
        onTrigEntered = false;
        ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
        ci.NumberFormat.CurrencyDecimalSeparator = ".";
        // string timeOut = GameManager.getConfig("config_timeOut_arduino", GameManager.Type.flutuante).ToString();
        // if(timeOut != "" && timeOut !="0")
        //    timeOutArduino = float.Parse(timeOut, NumberStyles.Any, ci);

        //GameManager.manager_instance.startFindArduinoPort();


        //  var usbDevices = new USBdevices().GetUSBDevices();

        // foreach (var usbDevice in usbDevices)
        // {
        //   print("Device ID: "+usbDevice.DeviceID +" ;PNP Device ID: "+ usbDevice.PnpDeviceID+" ;Description: "+ usbDevice.Description);
        //}


    }

    private void OnTriggerEnter(Collider other)
    {
        if (onTrigEntered == false)
        {
            UnityEngine.Debug.Log("Entrei");
            SendSerialMessage("a");
            onTrigEntered = true;
        }
        
    }
    private void OnTriggerExit(Collider other)
    {
        if (onTrigEntered)
        {
            UnityEngine.Debug.Log("Sai");
            SendSerialMessage("b");
            onTrigEntered = false;
        }
        
    }

    void OnEnable()
    {

        portName = portName;


            startTime = Time.time;
            serialThread = new SerialThreadLines(portName,
                                                 baudRate,
                                                 reconnectionDelay,
                                                 maxUnreadMessages);
            thread = new Thread(new ThreadStart(serialThread.RunForever));
            thread.Start();

  

    }

    // ------------------------------------------------------------------------
    // Invoked whenever the SerialController gameobject is deactivated.
    // It stops and destroys the thread that was reading from the serial device.
    // ------------------------------------------------------------------------
    void OnDisable()
    {
        // If there is a user-defined tear-down function, execute it before
        // closing the underlying COM port.
        if (userDefinedTearDownFunction != null)
            userDefinedTearDownFunction();

        // The serialThread reference should never be null at this point,
        // unless an Exception happened in the OnEnable(), in which case I've
        // no idea what face Unity will make.
        if (serialThread != null)
        {
            serialThread.RequestStop();
            serialThread = null;
        }

        // This reference shouldn't be null at this point anyway.
        if (thread != null)
        {
            thread.Join();
            thread = null;
        }
    }

    // ------------------------------------------------------------------------
    // Polls messages from the queue that the SerialThread object keeps. Once a
    // message has been polled it is removed from the queue. There are some
    // special messages that mark the start/end of the communication with the
    // device.
    // ------------------------------------------------------------------------
    public void print(string s)
    {
        UnityEngine.Debug.Log(s);
    }
    public KeyCode cima = KeyCode.UpArrow;
    void Update()
    {
        MovementInputs();
          print("recebido do arduino: " + ReadSerialMessage());
        
       
        // If the user prefers to poll the messages instead of receiving them
        // via SendMessage, then the message listener should be null.       
      
            if (Input.GetKeyDown("0"))
            {
                print("enviado a mensagem para o arduino: 0");
                SendSerialMessage("0");


            }
            if (Input.GetKeyDown("1"))
            {
                print("enviado a mensagem para o arduino: 2");

                SendSerialMessage("1");

            }
            if (Input.GetKeyDown("2"))
            {

                print("enviado a mensagem para o arduino: 3");
                SendSerialMessage("2");

            }
            if (Input.GetKeyDown("3"))
            {
                SendSerialMessage("3");


            }
      
        if (Input.GetKeyDown(KeyCode.Z))
        {
            SendSerialMessage("a");
            print("a:enviado");

        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            SendSerialMessage("s");
            print("z:enviado");

        }


        // Read the next message from the queue
        string message = (string)serialThread.ReadMessage();
            /*if (message != null)
                print(message);
            else
                return;*/


            //verifica se a mesagem lida contem alguma informacao vinda do arduino. Se sim, encontramos a port COM correta.
            if (message != null && message != "__Disconnected__" && message != "__Connected__" && message != "WaitSleepJoin" && message != "")
            {
                receivedValue = message;
            }
            if (Time.time - startTime > timeOutArduino && receivedValue == "")
            {

                //  Reconnect();
            }

            if (message == null)
                return;
            //print(portNames[portNumTest] + ": " + message);

            //Inicio da calibragem da seringa...fazer a mensagem ficar visivel para o usuario na cena da calibragem. 
            if (message == "Calibragem resetada!")
            {
                if (messageCalibrateArduino)
                    messageCalibrateArduino.text = message + ". Posicione a ampola na marca zero ml e clique no botão amarelo para continuar...";
                b_arduinoCalibration = true;
                return;
            }
            if (message == "Seringa calibrada!")
            {
                b_arduinoCalibration = false;
                if (messageCalibrateArduino)
                    messageCalibrateArduino.text = message;
                return;
            }
            if (b_arduinoCalibration)
            {
                if (messageCalibrateArduino)
                    messageCalibrateArduino.text = message;
                return;
            }


        
    }
    private void MovementInputs()
    {
        // heading += Input.GetAxis("Mouse X")*Time.deltaTime*velocidadeMouse;
        //camPivot.rotation = Quaternion.Euler(0, heading, 0);


        positionInput = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), Input.GetAxis("Mouse ScrollWheel"));
        positionInput = positionInput.normalized;

        //Vector3 camF = cam.forward;
        //Vector3 camR = cam.right;

        //camF = camF.normalized;
        //camR = camR.normalized;

        transform.position += new Vector3(positionInput.x, positionInput.y, positionInput.z *3) * Time.deltaTime * movimento;
        //transform.position += (camF*positionInput.y + camR*positionInput.x)*Time.deltaTime;
    }
    // ------------------------------------------------------------------------
    // Returns a new unread message from the serial device. You only need to
    // call this if you don't provide a message listener.
    // ------------------------------------------------------------------------
    public string ReadSerialMessage()
    {
        // Read the next message from the queue
        return (string)serialThread.ReadMessage();
    }

    // ------------------------------------------------------------------------
    // Puts a message in the outgoing queue. The thread object will send the
    // message to the serial device when it considers it's appropriate.
    // ------------------------------------------------------------------------
    public void SendSerialMessage(object message)
    {
        if (serialThread != null)
            serialThread.SendMessage(message);

    }


    public void Reconnect()
    {
        print("reconnect");
        OnDisable();

        portNumTest++;
        if (portNumTest + 1 > portNames.Length)
        {
            portNumTest = 0;
        }

        OnEnable();
        GameManager.saveConfig("arduinoPortName", portNames[portNumTest], GameManager.Type.texto);
    }

    // ------------------------------------------------------------------------
    // Executes a user-defined function before Unity closes the COM port, so
    // the user can send some tear-down message to the hardware reliably.
    // ------------------------------------------------------------------------
    public delegate void TearDownFunction();
    private TearDownFunction userDefinedTearDownFunction;
    public void SetTearDownFunction(TearDownFunction userFunction)
    {
        this.userDefinedTearDownFunction = userFunction;
    }

    public void OnApplicationQuit()
    {
        serialThread.RequestStop();
        thread.Abort();
    }

}