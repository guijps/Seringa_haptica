#define POTENTIOMETER_PIN A0
int i = 0;
int red;
int b;
bool availab;
int minimum,maximum;

void setup() 
{
  minimum=0;
  maximum=1;
  availab=true;
  Serial.begin(9600);
  pinMode(11,OUTPUT);
  pinMode(4,OUTPUT);
  pinMode(3,OUTPUT);
  pinMode(2,OUTPUT);
  pinMode(A0,INPUT);
  pinMode(13,INPUT);
  analogWrite(11,0);
  digitalWrite(4,0);
  digitalWrite(3,0);
  digitalWrite(2,1);
}
int calib(int anRead){
  if(anRead<minimum){
    minimum = anRead;
  }else if (maximum<anRead){
    maximum = anRead;
  }
  return ( int)anRead*1023/(maximum - minimum);
  
}
void controle(int intLeitura){
   if(intLeitura  >=48 and intLeitura <=57 and availab== true){
        if(intLeitura  >=49 and intLeitura <=57){
        
        int velocidade = 255- 57*4;
        velocidade = velocidade + intLeitura*4;
     
        analogWrite(11,velocidade);
        if(intLeitura ==57){
          digitalWrite(4,0);
        digitalWrite(3,0);
        digitalWrite(2,1);
        }else{
           digitalWrite(4,0);
        digitalWrite(3,1);
        digitalWrite(2,0);
        }
       
      }else if (intLeitura == 48){
      
       analogWrite(11,0);
         digitalWrite(4,1);
        digitalWrite(3,0);
        digitalWrite(2,0);
      }   
   }else if(intLeitura == 97 ){//a
    availab=false;
    analogWrite(11,255);
    digitalWrite(2,1);
    digitalWrite(3,1);
    delay(300);
    analogWrite(11,0);
    digitalWrite(2,1);
    digitalWrite(3,1);
    
  availab=true;
    
   }else if(intLeitura == 98 ){
    analogWrite(11,0);
    digitalWrite(3,0);
    digitalWrite(2,0); 
    
    }else if (intLeitura ==115){
    //s
   availab=false;
   availab=true;
   }
  



  
}
void loop()
{
  int anaRead= analogRead(A0);
 red = Serial.read();
 int a = digitalRead(13);

 if(a==0){
 
     analogWrite(11,0);
  digitalWrite(4,1);
  digitalWrite(3,1);
  digitalWrite(2,1);
  }else if(red>0){
     
    controle(red);
  }
  Serial.println(calib(anaRead));
  delay(100);
}
  
