/**
 * Ardity (Serial Communication for Arduino + Unity)
 * Author: Daniel Wilches <dwilches@gmail.com>
 *
 * This work is released under the Creative Commons Attributions license.
 * https://creativecommons.org/licenses/by/2.0/
 */

using UnityEngine;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

/**
 * This class allows a Unity program to continually check for messages from a
 * serial device.
 *
 * It creates a Thread that communicates with the serial port and continually
 * polls the messages on the wire.
 * That Thread puts all the messages inside a Queue, and this SerialController
 * class polls that queue by means of invoking SerialThread.GetSerialMessage().
 *
 * The serial device must send its messages separated by a newline character.
 * Neither the SerialController nor the SerialThread perform any validation
 * on the integrity of the message. It's up to the one that makes sense of the
 * data.
 */
public class SerialController : MonoBehaviour
{

    [SerializeField]float movimento = 1000f;                //velocidade da seringa 
    [SerializeField]float deslocamentoMinimo = 1f;          //determina deslocamento minimo para ativacao do vibracall quando a seringa esta inserida
    bool seringaDentro = false;
    Vector3 posicao;
    float diferenca;
    bool novaPosicao = true;                                //evita que a co-rotina seja chamada indiscriminadamente
    

    float ultimaPosicao;


    [Tooltip("Port name with which the SerialPort object will be created.")]
    public string portName = "COM3";

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
    public int maxUnreadMessages = 1;

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


    // ------------------------------------------------------------------------
    // Invoked whenever the SerialController gameobject is activated.
    // It creates a new thread that tries to connect to the serial device
    // and start reading from it.
    // ------------------------------------------------------------------------
    void Start()
    {

    }

    void OnEnable()
    {
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
    void Update()
    {
        RespondtoCommands();
        /*
        if(seringaDentro){
            if(novaPosicao)
                StartCoroutine(ColisaoCoroutine());
        }
        */
        if(seringaDentro){
            if((Mathf.Abs(ultimaPosicao - transform.position.magnitude)*100) > deslocamentoMinimo){
                if(seringaDentro){
                    SendSerialMessage("2");
                    ultimaPosicao = transform.position.magnitude;
                }
            }
        }
        
        // If the user prefers to poll the messages instead of receiving them
        // via SendMessage, then the message listener should be null.
        if (messageListener == null)
            return;

        // Read the next message from the queue
        string message = (string)serialThread.ReadMessage();
        if (message == null)
            return;

        // Check if the message is plain data or a connect/disconnect event.
        if (ReferenceEquals(message, SERIAL_DEVICE_CONNECTED))
            messageListener.SendMessage("OnConnectionEvent", true);
        else if (ReferenceEquals(message, SERIAL_DEVICE_DISCONNECTED))
            messageListener.SendMessage("OnConnectionEvent", false);
        else
            messageListener.SendMessage("OnMessageArrived", message);
    }


    private void RespondtoCommands(){                                 //modela o movimento
        float movimentoNesseFrame = movimento * Time.deltaTime;
        if(Input.GetKey(KeyCode.A))
            this.transform.Translate(Vector3.back * movimentoNesseFrame);
        if(Input.GetKey(KeyCode.D))
            this.transform.Translate(Vector3.forward * movimentoNesseFrame);
    }

    void OnTriggerEnter(Collider collider){                                  //detecta a insercao da seringa
            SendSerialMessage("1");
            seringaDentro = true;
            ultimaPosicao = transform.position.magnitude;
    }       

    void OnTriggerExit(Collider collider){                                  //detecta a retirada da seringa
            SendSerialMessage("3");
            seringaDentro = false;
    }     
/*
    IEnumerator ColisaoCoroutine(){
        posicao = transform.position;                       //salva a posicao da seringa antes da pausa
        novaPosicao = false;

        yield return new WaitForSeconds(0.45f);              //pausa para captar diferenca do posicionamento da seringa e desacelerar as chamas da funcao
        
        diferenca = transform.position.x - posicao.x;
        novaPosicao = true;
        if(Mathf.Abs(diferenca*100) > deslocamentoMinimo)   //modela a diferenca minima de distancia, pode ser ajustada no fator ou no SerialField
            SendSerialMessage("2");
    }
*/




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
    public void SendSerialMessage(string message)
    {
        serialThread.SendMessage(message);
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

}