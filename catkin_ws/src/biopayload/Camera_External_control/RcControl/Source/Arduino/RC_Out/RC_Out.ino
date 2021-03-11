#include <Firmata.h>

// -------------------------------------------------------------
// FirmataRC - Version 1.0 Copyright richard.prinz@min.at 2013
// This code is under Creative Commons License V3.0
// See:
// http://creativecommons.org/licenses/by/3.0/
// http://creativecommons.org/licenses/by/3.0/at/
//
// You are allowed to use and modify this code (private and 
// commercial) as long as you reference the origin of it in
// any end user documentation, EULA's etc.

// You can configure the following parameters:

// define port which ouputs the PPM signal
// PPM = pulse pause modulation
#define PPM_PORT                4

// define port which controls the trainer switch
// PPT = push to transmit
#define PPT_PORT                13

// These are the default parameters customized for
// a Walkere Devention Devo-7. They may work for other
// transmitters. You can configure the values for other
// transmitters either by changeing this parameters or
// by using the SetConfiguration FirmatRC command
#define NUMBER_OF_CHANNELS      7
#define LENGTH_OF_FRAME         30000
#define MINIMUM_PULSE_LENGTH    670
#define MAXIMUM_PULSE_LENGTH    1465

// -------------------------------------------------------------

// DONT CHANGE ANYTHING AFTER THIS LINE - EXCEPT YOU KNOW WHAT
// YOU ARE DOING ..............


#define RC_SEND_CONFIG          0x50
#define RC_READ_CONFIG          0x51

#define RC_SET_CHANNEL          0x52
#define RC_READ_CHANNEL         0x53
#define RC_READ_ALL_CHANNELS    0x54

#define RC_PTT                  0x55

#define RC_RESET                0x5F

byte NUM_CHANNELS;
long FRAME_SIZE;
int PULSE_MIN;
int PULSE_MAX;
const int SYNC = 400;
int PULSE_LEN;
float PULSE_STEP;

int startFrame;
byte *ppm;



void systemResetCallback()
{
  pinMode(4, OUTPUT);
  pinMode(13, OUTPUT);
  digitalWrite(13, LOW);
  
  NUM_CHANNELS = NUMBER_OF_CHANNELS;
  FRAME_SIZE = LENGTH_OF_FRAME;
  PULSE_MIN = MINIMUM_PULSE_LENGTH;
  PULSE_MAX = MAXIMUM_PULSE_LENGTH;
  PULSE_LEN = PULSE_MAX - PULSE_MIN;
  PULSE_STEP = PULSE_LEN / 255.0;

  initPPM(NUM_CHANNELS);
}



void sysexCallback(byte command, byte argc, byte *argv)
{
  byte mode;
  byte slaveAddress;
  byte slaveRegister;
  byte data;
  unsigned int delayTime; 
  
  switch(command)
  {
    case CAPABILITY_QUERY:
      Serial.write(START_SYSEX);
      Serial.write(CAPABILITY_RESPONSE);

      Serial.write((byte)INPUT);
      Serial.write(1);
      Serial.write((byte)OUTPUT);
      Serial.write(1);

      Serial.write(127);
      
      Serial.write(END_SYSEX);
      break;


    // Set configuration (0x50)
    case RC_SEND_CONFIG:
      if(argc > 8)
      {
        NUM_CHANNELS = argv[0];
        PULSE_MIN = argv[1] + (argv[2] << 7);
        PULSE_MAX = argv[3] + (argv[4] << 7);
        FRAME_SIZE = argv[5] + (argv[6] << 7) + (argv[7] << 14) + (argv[8] << 28);

        initPPM(NUM_CHANNELS);
        
        writeResult(RC_SEND_CONFIG, 0x00, 0, NULL);
      }
      else
        writeResult(RC_SEND_CONFIG, 0x01, 0, NULL);
      break;


    // Read configuration (0x51)    
    case RC_READ_CONFIG:
      byte buffer[9];
      buffer[0] = NUM_CHANNELS;
      buffer[1] = PULSE_MIN & 0x7F;
      buffer[2] = (PULSE_MIN >> 7) & 0x7F;
      buffer[3] = PULSE_MAX & 0x7F;
      buffer[4] = (PULSE_MAX >> 7) & 0x7F;
      
      buffer[5] = FRAME_SIZE & 0x7F;
      buffer[6] = (FRAME_SIZE >> 7) & 0x7F;
      buffer[7] = (FRAME_SIZE >> 14) & 0x7F;
      buffer[8] = (FRAME_SIZE >> 28) & 0x7F;
      
      writeResult(RC_READ_CONFIG, 0x00, 9, buffer);
      break;
    
    
    // Set channel value (0x52)
    case RC_SET_CHANNEL:
      if(argc > 2)
      {
        byte buffer[1];
        byte channel = argv[0];
        int value = (argv[1] + (argv[2] << 7)) & 0xFF;
        buffer[0] = channel & 0x7F;
        
        if(channel > NUM_CHANNELS)
          writeResult(RC_SET_CHANNEL, 0x02, 1, buffer);
        else
        {
          ppm[channel] = value;
          writeResult(RC_SET_CHANNEL, 0x00, 1, buffer);
        }
      }
      else
        writeResult(RC_SET_CHANNEL, 0x01, 1, buffer);
      break;
    
    
    // Read channel value (0x53)
    case RC_READ_CHANNEL:
      if(argc > 0)
      {
        byte channel = argv[0];

        if(channel > NUM_CHANNELS)
          writeResult(RC_READ_CHANNEL, 0x02, 0, NULL);
        else
        {
          byte buffer[3];
          buffer[0] = channel & 0x7F;
          buffer[1] = ppm[channel] & 0x7F;
          buffer[2] = (ppm[channel] >> 7) & 0x7F;
          writeResult(RC_READ_CHANNEL, 0x00, 3, buffer);
        }
      }
      else
        writeResult(RC_READ_CHANNEL, 0x01, 0, NULL);
      break;
    
    
    // Read all channel values (0x54)
    case RC_READ_ALL_CHANNELS:
      writeResult(RC_READ_ALL_CHANNELS, 0x01, 0, NULL);
      break;
    
    
    // Set PTT (Push To Transmit) (0x55)
    case RC_PTT:
      if(argc > 0)
      {
        byte ptt = argv[0];

        digitalWrite(13, (ptt > 0 ? HIGH : LOW));

        byte buffer[1];
        buffer[0] = ptt & 0x7F;
        writeResult(RC_PTT, 0x00, 1, buffer);
      }
      else
        writeResult(RC_PTT, 0x01, 0, NULL);
      break;
    
    
    // Reset (0x5F)  
    case RC_RESET:
      systemResetCallback();
      writeResult(RC_RESET, 0x00, 0, NULL);
      break;
  }
}



void writeResult(byte command, byte result, byte argc, byte *argv)
{
  Serial.write(START_SYSEX);
  Serial.write(command);
  Serial.write(result);
  
  if(argc > 0 && argv != NULL)
    for(int i=0; i < argc; i++)
      Serial.write(argv[i]);
  
  Serial.write(END_SYSEX);
  //Firmata.sendString("Not enough data");
}

void initPPM(byte channels)
{
  clearPPM();
  
  ppm = (byte *)malloc(sizeof(*ppm) * channels);
  for (int i = 0; i < channels; i++)
    ppm[i] = 0;
}

void clearPPM()
{
  if(ppm != NULL)
  {
    free(ppm);
    ppm = NULL;
  }    
}



void setup()
{
  //Firmata.setFirmwareVersion(FIRMATA_MAJOR_VERSION, FIRMATA_MINOR_VERSION);
  Firmata.setFirmwareNameAndVersion("FirmataRC", FIRMATA_MAJOR_VERSION, FIRMATA_MINOR_VERSION);
  Firmata.attach(START_SYSEX, sysexCallback);
  Firmata.attach(SYSTEM_RESET, systemResetCallback);
  Firmata.begin(57600);
  
  systemResetCallback();
}



void loop()
{
  // output pulse train
  startFrame = micros();
  
  while(Firmata.available())
      Firmata.processInput();
  
  for (int i = 0; i < NUM_CHANNELS; i++)
  {
    digitalWrite(4, LOW);
    delayMicroseconds(SYNC);
    digitalWrite(4, HIGH);
    
    delayMicroseconds(PULSE_MIN + (PULSE_STEP * ppm[i]));
  }
  
  digitalWrite(4, LOW);
  delayMicroseconds(SYNC);
  digitalWrite(4, HIGH);

  long diff = FRAME_SIZE - (micros() - startFrame);
  delayMicroseconds(diff);
}

