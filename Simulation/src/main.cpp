#include <Arduino.h>
#include <WiFi.h>
#include <PubSubClient.h>
#include <LiquidCrystal_I2C.h>

// Red y MQTT
const char* SSID   = "Wokwi-GUEST";
const char* PASS   = "";
const char* BROKER = "test.mosquitto.org";
const char* TOPIC  = "iabd2425/esp32/led";
const char* TELTOP = "iabd2425/esp32/telemetry";
const char* CLID   = "ESP32_Semaforo_IoTChat";

// Pines
const int LED_R = 25, LED_Y = 26, LED_G = 27; // LEDs
const int LDR = 34;   // Sensor luz
const int BZ = 4;     // Buzzer
const int PIR = 5;    // Sensor movimiento
const int BTN_R = 12, BTN_Y = 13, BTN_G = 14; // Botones

LiquidCrystal_I2C lcd(0x27, 16, 2);
WiFiClient espClient;
PubSubClient mqtt(espClient);

// Estado
bool blinkR, blinkY, blinkG;
bool staticR, staticY, staticG;
bool blinkTick, isNight, buzzerOn;
unsigned long lastBlink;
int lastPinR = -1, lastPinY = -1, lastPinG = -1;

// Muestra dos lineas en LCD
void lcdPrint(const char* l1, const char* l2) {
    lcd.clear();
    lcd.setCursor(0, 0); lcd.print(l1);
    lcd.setCursor(0, 1); lcd.print(l2);
}

// Apaga todo
void apagaTodo() {
    blinkR = blinkY = blinkG = false;
    staticR = staticY = staticG = false;
}

// Activa parpadeo en LEDs indicados
void modoBlink(bool r, bool y, bool g) {
    apagaTodo();
    blinkR = r; blinkY = y; blinkG = g;
}

// Publica estado de pines por MQTT solo si cambio
void publicarEstado() {
    int r = digitalRead(LED_R), y = digitalRead(LED_Y), g = digitalRead(LED_G);
    if (r != lastPinR || y != lastPinY || g != lastPinG) {
        char p[10]; sprintf(p, "%d,%d,%d", r, y, g);
        mqtt.publish(TELTOP, p);
        lastPinR = r; lastPinY = y; lastPinG = g;
    }
}

// Interpreta comandos MQTT recibidos
void onMqttMessage(char* topic, byte* payload, unsigned int len) {
    String cmd = "";
    for (int i = 0; i < len; i++) cmd += (char)payload[i];

    if      (cmd == "encender_rojo")           { apagaTodo(); staticR = true;               lcdPrint("Semaforo IoT    ", "Roja encendida  "); }
    else if (cmd == "encender_amarillo")        { apagaTodo(); staticY = true;               lcdPrint("Semaforo IoT    ", "Amarilla encend."); }
    else if (cmd == "encender_verde")           { apagaTodo(); staticG = true;               lcdPrint("Semaforo IoT    ", "Verde encendida "); }
    else if (cmd == "apagar_rojo")              { blinkR = staticR = false;                  lcdPrint("Semaforo IoT    ", "Roja apagada    "); }
    else if (cmd == "apagar_amarillo")          { blinkY = staticY = false;                  lcdPrint("Semaforo IoT    ", "Amarilla apagada"); }
    else if (cmd == "apagar_verde")             { blinkG = staticG = false;                  lcdPrint("Semaforo IoT    ", "Verde apagada   "); }
    else if (cmd == "apagar_todas")             { apagaTodo();                               lcdPrint("Semaforo IoT    ", "Todo apagado    "); }
    else if (cmd == "parpadear_rojo")           { modoBlink(1,0,0);                          lcdPrint("Semaforo IoT    ", "Roja parpadea   "); }
    else if (cmd == "parpadear_amarillo")       { modoBlink(0,1,0);                          lcdPrint("Semaforo IoT    ", "Amarilla parpad."); }
    else if (cmd == "parpadear_verde")          { modoBlink(0,0,1);                          lcdPrint("Semaforo IoT    ", "Verde + pitido  "); }
    else if (cmd == "parpadear_todas")          { modoBlink(1,1,1);                          lcdPrint("Semaforo IoT    ", "Todas parpadean "); }
    else if (cmd == "encender_rojo_amarillo")   { apagaTodo(); staticR = staticY = true;     lcdPrint("Semaforo IoT    ", "Roja + Amarilla "); }
    else if (cmd == "encender_rojo_verde")      { apagaTodo(); staticR = staticG = true;     lcdPrint("Semaforo IoT    ", "Roja + Verde    "); }
    else if (cmd == "encender_amarillo_verde")  { apagaTodo(); staticY = staticG = true;     lcdPrint("Semaforo IoT    ", "Amar. + Verde   "); }
    else if (cmd == "encender_todas")           { apagaTodo(); staticR=staticY=staticG=true; lcdPrint("Semaforo IoT    ", "Todas encendidas"); }
    else if (cmd == "apagar_rojo_amarillo")     { blinkR=blinkY=staticR=staticY=false;       lcdPrint("Semaforo IoT    ", "Roja+Amar OFF   "); }
    else if (cmd == "apagar_rojo_verde")        { blinkR=blinkG=staticR=staticG=false;       lcdPrint("Semaforo IoT    ", "Roja+Verde OFF  "); }
    else if (cmd == "apagar_amarillo_verde")    { blinkY=blinkG=staticY=staticG=false;       lcdPrint("Semaforo IoT    ", "Amar.+Verde OFF "); }
    else if (cmd == "parpadear_rojo_amarillo")  { modoBlink(1,1,0);                          lcdPrint("Semaforo IoT    ", "Roja+Amar parp. "); }
    else if (cmd == "parpadear_rojo_verde")     { modoBlink(1,0,1);                          lcdPrint("Semaforo IoT    ", "Roja+Verde parp."); }
    else if (cmd == "parpadear_amarillo_verde") { modoBlink(0,1,1);                          lcdPrint("Semaforo IoT    ", "Amar.+Verd parp."); }
}

// Reconecta a MQTT si se pierde conexion
void reconnectMqtt() {
    while (!mqtt.connected()) {
        if (mqtt.connect(CLID)) mqtt.subscribe(TOPIC);
        else delay(2000);
    }
}

// Toggle boton fisico con debounce
void handleButton(int pin, bool& state, bool& blinkFlag, const char* onMsg, const char* offMsg) {
    static bool prevR = HIGH, prevY = HIGH, prevG = HIGH;
    bool* prev = (pin == BTN_R) ? &prevR : (pin == BTN_Y) ? &prevY : &prevG;
    bool pressed = (digitalRead(pin) == LOW);
    if (pressed && !*prev) {
        blinkFlag = false;
        state = !state;
        lcdPrint("Semaforo IoT    ", state ? onMsg : offMsg);
    }
    *prev = pressed;
}

void setup() {
    Serial.begin(115200);
    pinMode(LED_R, OUTPUT); pinMode(LED_Y, OUTPUT); pinMode(LED_G, OUTPUT);
    pinMode(LDR, INPUT); pinMode(BZ, OUTPUT); pinMode(PIR, INPUT);
    pinMode(BTN_R, INPUT_PULLUP); pinMode(BTN_Y, INPUT_PULLUP); pinMode(BTN_G, INPUT_PULLUP);

    lcd.init(); lcd.backlight();
    lcdPrint("Semaforo IoT    ", "Conectando...   ");

    WiFi.begin(SSID, PASS);
    while (WiFi.status() != WL_CONNECTED) delay(500);

    mqtt.setServer(BROKER, 1883);
    mqtt.setCallback(onMqttMessage);
    reconnectMqtt();

    lcdPrint("Semaforo IoT    ", "    Listo!      ");
}

void loop() {
    if (!mqtt.connected()) reconnectMqtt();
    mqtt.loop();

    // Sensor de luz: activa parpadeo amarillo de noche
    int luz = analogRead(LDR);
    if (luz > 2000 && !isNight)      { isNight = true;  modoBlink(0,1,0); lcdPrint("  MODO NOCHE    ", "Amarilla parpad."); }
    else if (luz < 1000 && isNight)  { isNight = false; apagaTodo();      lcdPrint("   MODO DIA     ", "Luces al dia    "); }

    // Reloj de parpadeo cada 500ms
    if (millis() - lastBlink >= 500) { blinkTick = !blinkTick; lastBlink = millis(); }

    // Botones fisicos
    handleButton(BTN_R, staticR, blinkR, "Roja encendida  ", "Roja apagada    ");
    handleButton(BTN_Y, staticY, blinkY, "Amarilla encend.", "Amarilla apagada");
    handleButton(BTN_G, staticG, blinkG, "Verde encendida ", "Verde apagada   ");

    // Sensor de movimiento: override rojo parpadeante
    static bool pirActivo = false;
    bool pirOn = digitalRead(PIR) == HIGH;
    if (pirOn) {
        if (!pirActivo) { pirActivo = true; lcdPrint(" ALERTA COCHE!  ", "  ALTO TOTAL!   "); }
        digitalWrite(LED_R, blinkTick); digitalWrite(LED_Y, LOW); digitalWrite(LED_G, LOW);
    } else {
        // Estado normal segun configuracion
        digitalWrite(LED_R, blinkR ? blinkTick : staticR);
        digitalWrite(LED_Y, blinkY ? blinkTick : staticY);
        digitalWrite(LED_G, blinkG ? blinkTick : staticG);
        if (pirActivo) { pirActivo = false; lcdPrint("Semaforo IoT    ", "   Modo normal  "); }
    }

    // Buzzer: solo cuando verde parpadea solo
    bool quiereTono = (blinkG && !blinkR && !blinkY && !pirOn && blinkTick);
    if (quiereTono != buzzerOn) {
        buzzerOn = quiereTono;
        if (buzzerOn) tone(BZ, 1000); else noTone(BZ);
    }

    publicarEstado();
}
